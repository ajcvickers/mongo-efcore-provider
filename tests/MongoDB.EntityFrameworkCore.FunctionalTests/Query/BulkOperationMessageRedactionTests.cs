/* Copyright 2023-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// Bulk ExecuteUpdate/ExecuteDelete exist from EF9.
#if !EF8
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

#nullable enable

/// <summary>
/// Every bulk-operation failure wraps its detail in EF's "The LINQ expression '...' could not be translated" with the
/// captured query printed; the print redacts inlined literals (<c>?</c>), so a filter or setter literal (a password, PII)
/// never lands in exception text, which <c>EnableSensitiveDataLogging</c> does not gate. One row per wrapper site.
/// </summary>
[XUnitCollection("UpdateTests")]
public class BulkOperationMessageRedactionTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Addr
    {
        public string City { get; set; } = null!;
        public int Code { get; set; }
    }

    public class Account
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public string Secret { get; set; } = null!;
        public int Converted { get; set; }
        public Addr Billing { get; set; } = null!;
        public Addr Shipping { get; set; } = null!;
    }

    private static void Configure(ModelBuilder mb)
        => mb.Entity<Account>(b =>
        {
            b.Property(a => a.Converted).HasConversion<string>();
            b.ComplexProperty(a => a.Billing, c => c.Property(x => x.Code).Metadata.SetBsonRepresentation(BsonType.String, null, null));
            b.ComplexProperty(a => a.Shipping);
        });

    private SingleEntityDbContext<Account> Context()
    {
        var collection = database.CreateCollection<Account>(nameof(BulkOperationMessageRedactionTests) + Guid.NewGuid().ToString("N")[..6]);
        collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "ann" }, { "Secret", "s" }, { "Converted", "1" },
            { "Billing", new BsonDocument { { "City", "Paris" }, { "Code", "1" } } }, { "Shipping", new BsonDocument { { "City", "Rome" }, { "Code", 2 } } }
        });
        return SingleEntityDbContext.Create(collection, Configure, null, b => b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));
    }

    private static void AssertRedacted(Func<int> operation, string detail)
    {
        var ex = Assert.ThrowsAny<InvalidOperationException>(() => operation());
        Assert.Contains("could not be translated", ex.Message);
        Assert.Contains(detail, ex.Message);
        Assert.DoesNotContain("hunter2-literal", ex.Message);
        Assert.DoesNotContain("secret-suffix", ex.Message);
        // The shape stays: the filter's member and the placeholder.
        Assert.Contains("Secret == ?", ex.Message);
    }

    [Fact]
    public void Self_referencing_setter_over_a_converted_property()
    {
        using var db = Context();
        AssertRedacted(() => db.Entities.Where(a => a.Secret == "hunter2-literal")
            .ExecuteUpdate(s => s.SetProperty(a => a.Name, a => a.Name + "secret-suffix").SetProperty(a => a.Converted, a => a.Converted + 1)),
            "uses a value converter");
    }

    [Fact]
    public void Self_referencing_setter_over_a_represented_complex_leaf()
    {
        using var db = Context();
        AssertRedacted(() => db.Entities.Where(a => a.Secret == "hunter2-literal")
            .ExecuteUpdate(s => s.SetProperty(a => a.Name, a => a.Name + "secret-suffix").SetProperty(a => a.Billing.Code, a => a.Shipping.Code)),
            "BsonRepresentation");
    }

    [Fact]
    public void Whole_complex_value_from_the_row()
    {
        using var db = Context();
        AssertRedacted(() => db.Entities.Where(a => a.Secret == "hunter2-literal")
            .ExecuteUpdate(s => s.SetProperty(a => a.Name, a => a.Name + "secret-suffix").SetProperty(a => a.Billing, a => a.Shipping)),
            "from a value that reads the entity being updated");
    }

    [Fact]
    public void Overlapping_setter_paths()
    {
        using var db = Context();
        AssertRedacted(() => db.Entities.Where(a => a.Secret == "hunter2-literal")
            .ExecuteUpdate(s => s.SetProperty(a => a.Name, a => a.Name + "secret-suffix")
                .SetProperty(a => a.Billing, new Addr { City = "x", Code = 1 }).SetProperty(a => a.Billing.City, "y")),
            "one is stored inside the other");
    }

    [Fact]
    public void Unsupported_bulk_source_operator()
    {
        using var db = Context();
        AssertRedacted(() => db.Entities.Where(a => a.Secret == "hunter2-literal").Where(a => a.Name != "secret-suffix").Reverse().ExecuteDelete(),
            "is not supported in a bulk delete or update");
    }
}
#endif
