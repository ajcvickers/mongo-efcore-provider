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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// Unsupported complex shapes decline with a CLEAR exception naming the member, in every query mode —
/// never the driver's bare <c>ExpressionNotSupportedException</c> and never the generic NativeOnly coverage
/// failure. A whole complex value has no scalar representation, so comparing one is the pinned unsupported
/// shape (ComplexTypeDeclines.WholeValueComparison).
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeDeclineMessageTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Address
    {
        public string City { get; set; } = "";
        public string Zip { get; set; } = "";
    }

    private class Customer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public Address Address { get; set; } = null!;
    }

    private static readonly Action<ModelBuilder> CustomerModel =
        mb => mb.Entity<Customer>().ComplexProperty(c => c.Address);

    private IMongoCollection<Customer> SeedCustomers(string name)
    {
        var coll = database.MongoDatabase.GetCollection<BsonDocument>(
            TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8]);
        coll.InsertOne(
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "Name", "Alpha" },
                { "Address", new BsonDocument { { "City", "NYC" }, { "Zip", "10001" } } }
            });
        return database.MongoDatabase.GetCollection<Customer>(coll.CollectionNamespace.CollectionName);
    }

    private static SingleEntityDbContext<Customer> CreateContext(
        IMongoCollection<Customer> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(
            collection,
            modelBuilderAction: CustomerModel,
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

    [Theory]
    [InlineData(MongoQueryMode.Native)]
    [InlineData(MongoQueryMode.NativeOnly)]
    [InlineData(MongoQueryMode.DriverLinq)]
    public void Whole_complex_comparison_throws_clear_exception_naming_the_member_in_every_mode(
        MongoQueryMode mode)
    {
        var collection = SeedCustomers($"{nameof(Whole_complex_comparison_throws_clear_exception_naming_the_member_in_every_mode)}-{mode}");
        using var db = CreateContext(collection, mode);

        var other = new Address { City = "NYC", Zip = "10001" };
        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.AsNoTracking().Where(c => c.Address == other).ToList());

        Assert.Contains("Address", ex.Message, StringComparison.Ordinal);
        Assert.Contains("cannot be compared as a whole", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpressionNotSupportedException", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeOnly_decline_for_whole_complex_comparison_names_the_member_not_the_fallback()
    {
        // Under NativeOnly the same shape must surface the SAME complex-specific message (thrown during
        // translation, before the fallback gate), not the generic "forbids the fallback" coverage failure.
        var collection = SeedCustomers(nameof(NativeOnly_decline_for_whole_complex_comparison_names_the_member_not_the_fallback));
        using var db = CreateContext(collection, MongoQueryMode.NativeOnly);

        var other = new Address { City = "NYC", Zip = "10001" };
        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.AsNoTracking().Where(c => c.Address == other).ToList());

        Assert.Contains("Address", ex.Message, StringComparison.Ordinal);
        Assert.Contains("cannot be compared as a whole", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("forbids the driver-LINQ fallback", ex.Message, StringComparison.Ordinal);
    }
}
