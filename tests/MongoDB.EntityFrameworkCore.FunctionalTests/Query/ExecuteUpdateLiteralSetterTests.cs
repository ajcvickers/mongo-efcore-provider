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

// Bulk ExecuteUpdate exists from EF9.
#if !EF8
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

#nullable enable

/// <summary>
/// A constant/captured <c>SetProperty</c> value must be stored as DATA. In the pipeline form of an update (used as soon as
/// one setter reads the row, <c>SetProperty(c =&gt; c.Rank, c =&gt; c.Rank + 1)</c>) <c>$set</c> evaluates its values as
/// aggregation expressions, so an unwrapped string <c>"$Secret"</c> is a FIELD PATH: <c>Name</c> received the document's
/// <c>Secret</c> (a cross-field read of user input). Every constant setter value of the pipeline form is now
/// <c>$literal</c>-wrapped, root scalars included (complex targets already were); the plain <c>$set</c> form (no
/// self-referencing setter) stores values as data and is unchanged. Every stored document is hand-written.
/// </summary>
[XUnitCollection("UpdateTests")]
public class ExecuteUpdateLiteralSetterTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public enum Level
    {
        Low,
        High
    }

    public class Addr
    {
        public string City { get; set; } = null!;
    }

    public class Account
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public string Secret { get; set; } = null!;
        public int Rank { get; set; }
        public string Coded { get; set; } = null!;
        public int Represented { get; set; }
        public Level Level { get; set; }
        public List<string> Tags { get; set; } = [];
        public DateTime When { get; set; }
        public bool Flag { get; set; }
        public Addr Billing { get; set; } = null!;
    }

    private static readonly DateTime May = new(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static void Configure(ModelBuilder mb)
        => mb.Entity<Account>(b =>
        {
            // A converter whose provider value is a string that may begin with "$".
            b.Property(a => a.Coded).HasConversion(v => "$" + v, v => v.Substring(1));
            b.Property(a => a.Represented).Metadata.SetBsonRepresentation(BsonType.String, null, null);
            b.Property(a => a.Level).HasConversion<string>();
            b.ComplexProperty(a => a.Billing);
        });

    private static BsonDocument SeedDoc()
        => new()
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "ann" }, { "Secret", "TOPSECRET-PII" }, { "Rank", 2 }, { "Coded", "$c" },
            { "Represented", "7" }, { "Level", "Low" }, { "Tags", new BsonArray { "t" } }, { "When", May }, { "Flag", false },
            { "Billing", new BsonDocument("City", "Paris") }
        };

    private (IMongoCollection<Account> Collection, BsonDocument Seed) Seed(string name)
    {
        var collection = database.CreateCollection<Account>(name + Guid.NewGuid().ToString("N")[..6]);
        var seed = SeedDoc();
        Raw(collection).InsertOne(seed.DeepClone().AsBsonDocument);
        return (collection, seed);
    }

    private static IMongoCollection<BsonDocument> Raw(IMongoCollection<Account> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    private static SingleEntityDbContext<Account> Context(IMongoCollection<Account> collection)
        => SingleEntityDbContext.Create(collection, Configure, null, b => b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

    // The stored document equals the seed with exactly these edits (and Rank + 1 when `bumped`).
    private static void AssertStored(IMongoCollection<Account> collection, BsonDocument seed, bool bumped, Action<BsonDocument> edit)
    {
        var expected = seed.DeepClone().AsBsonDocument;
        edit(expected);
        if (bumped)
        {
            expected["Rank"] = expected["Rank"].AsInt32 + 1;
        }

        Assert.Equal(expected.ToJson(), Assert.Single(Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList()).ToJson());
    }

    public static TheoryData<string> DollarStrings => ["$Secret", "$$ROOT", "$$NOW", "$", "$$", "", "a.b", "$Billing.City", "{ \"$literal\": 1 }"];

    [Theory]
    [MemberData(nameof(DollarStrings))]
    public void A_root_string_setter_beside_a_self_referencing_setter_stores_the_string(string userInput)
    {
        // RED (5547cfa2): "$Secret" stored Name = "TOPSECRET-PII"; "$$ROOT" stored the whole document; "$$NOW" a date.
        var (collection, seed) = Seed(nameof(A_root_string_setter_beside_a_self_referencing_setter_stores_the_string));
        using (var db = Context(collection))
        {
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s.SetProperty(a => a.Name, userInput).SetProperty(a => a.Rank, a => a.Rank + 1)));
        }

        AssertStored(collection, seed, bumped: true, d => d["Name"] = userInput);
    }

    [Theory]
    [MemberData(nameof(DollarStrings))]
    public void A_root_string_setter_alone_stores_the_string(string userInput)
    {
        // The plain $set form (no self-referencing setter): already data; unchanged.
        var (collection, seed) = Seed(nameof(A_root_string_setter_alone_stores_the_string));
        using (var db = Context(collection))
        {
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s.SetProperty(a => a.Name, userInput)));
        }

        AssertStored(collection, seed, bumped: false, d => d["Name"] = userInput);
    }

    [Fact]
    public void A_literal_dollar_string_constant_beside_a_self_referencing_setter()
    {
        // RED (5547cfa2): `SetProperty(r => r.Name, "$Rank")` stored Name = 2.
        var (collection, seed) = Seed(nameof(A_literal_dollar_string_constant_beside_a_self_referencing_setter));
        using (var db = Context(collection))
        {
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s.SetProperty(a => a.Name, "$Rank").SetProperty(a => a.Rank, a => a.Rank + 1)));
        }

        AssertStored(collection, seed, bumped: true, d => d["Name"] = "$Rank");
    }

    [Fact]
    public void Converted_represented_and_enum_setters_beside_a_self_referencing_setter_store_their_provider_values()
    {
        // The provider value of `Coded` begins with "$" ("$x" for "x"): RED (5547cfa2) read the missing field "x" (removed).
        // BsonRepresentation(String) and enum-as-string values are strings too; a list is an array of strings ("$Secret" in
        // it was a path). Numbers, bools and dates keep their BSON types.
        var (collection, seed) = Seed(nameof(Converted_represented_and_enum_setters_beside_a_self_referencing_setter_store_their_provider_values));
        var tags = new List<string> { "$Secret", "plain" };
        using (var db = Context(collection))
        {
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s
                .SetProperty(a => a.Coded, "Secret")
                .SetProperty(a => a.Represented, 42)
                .SetProperty(a => a.Level, Level.High)
                .SetProperty(a => a.Tags, tags)
                .SetProperty(a => a.When, May.AddDays(1))
                .SetProperty(a => a.Flag, true)
                .SetProperty(a => a.Rank, a => a.Rank + 1)));
        }

        AssertStored(collection, seed, bumped: true, d =>
        {
            d["Coded"] = "$Secret";
            d["Represented"] = "42";
            d["Level"] = "High";
            d["Tags"] = new BsonArray { "$Secret", "plain" };
            d["When"] = May.AddDays(1);
            d["Flag"] = true;
        });
    }

    [Fact]
    public void Complex_leaf_and_whole_complex_value_setters_with_dollar_strings()
    {
        // Complex targets were already wrapped (control), alone and beside a self-referencing setter.
        var (collection, seed) = Seed(nameof(Complex_leaf_and_whole_complex_value_setters_with_dollar_strings));
        using (var db = Context(collection))
        {
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s.SetProperty(a => a.Billing.City, "$Secret").SetProperty(a => a.Rank, a => a.Rank + 1)));
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s.SetProperty(a => a.Billing, new Addr { City = "$$ROOT" }).SetProperty(a => a.Rank, a => a.Rank + 1)));
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s.SetProperty(a => a.Billing, new Addr { City = "$Name" })));
        }

        var expected = seed.DeepClone().AsBsonDocument;
        expected["Rank"] = 4;
        expected["Billing"] = new BsonDocument("City", "$Name");
        Assert.Equal(expected.ToJson(), Assert.Single(Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList()).ToJson());
    }

    [Fact]
    public void A_null_string_setter_beside_a_self_referencing_setter_stores_null()
    {
        var (collection, seed) = Seed(nameof(A_null_string_setter_beside_a_self_referencing_setter_stores_null));
        string? none = null;
        using (var db = Context(collection))
        {
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s.SetProperty(a => a.Name, none!).SetProperty(a => a.Rank, a => a.Rank + 1)));
        }

        AssertStored(collection, seed, bumped: true, d => d["Name"] = BsonNull.Value);
    }
}
#endif
