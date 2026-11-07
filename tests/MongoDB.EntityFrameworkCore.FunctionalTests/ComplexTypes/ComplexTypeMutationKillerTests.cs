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

#if !EF8 && !EF9
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Tests that kill mutants of the complex type support that survived the rest of the suite (found by mutation testing):
/// the wrong-BSON-type rules for complex collections, the one-pass reader's per-element reset of complex locals, the bulk
/// required-member check below the first level, and the bulk refusal of an operator over an optional complex collection.
/// </summary>
[XUnitCollection("UpdateTests")]
public class ComplexTypeMutationKillerTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class KTag
    {
        public string Label { get; set; } = null!;
    }

    public class KOuter
    {
        public int N { get; set; }
        public KTag Inner { get; set; } = null!;
        public List<KTag> Items { get; set; } = [];
    }

    public class KDoc
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public KOuter Outer { get; set; } = null!;
        public List<KTag>? Opt { get; set; }
    }

    public class KCart
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<KTag> Lines { get; set; } = [];
    }

    [ComplexType]
    public class KSpot
    {
        public int Row { get; set; }
        public string Label { get; set; } = null!;
    }

    public class KLine
    {
        public string Sku { get; set; } = null!;
        public KSpot? Origin { get; set; }
    }

    public class KOrder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<KLine> Lines { get; set; } = [];
    }

    private static void ConfigureDoc(ModelBuilder mb)
        => mb.Entity<KDoc>(e =>
        {
            e.ComplexProperty(d => d.Outer, o =>
            {
                o.ComplexProperty(x => x.Inner);
                o.ComplexCollection(x => x.Items);
            });
            e.ComplexCollection(d => d.Opt);
        });

    private static void ConfigureCart(ModelBuilder mb) => mb.Entity<KCart>().ComplexCollection(c => c.Lines);

    private static void ConfigureOrder(ModelBuilder mb) => mb.Entity<KOrder>().OwnsMany(o => o.Lines);

    private static SingleEntityDbContext<T> Context<T>(
        IMongoCollection<T> collection, MongoQueryMode mode, Action<ModelBuilder> configure,
        QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTracking)
        where T : class
        => SingleEntityDbContext.Create(collection, configure, null, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            b.UseQueryTrackingBehavior(tracking);
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    private static IMongoCollection<BsonDocument> Raw<T>(IMongoCollection<T> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    private IMongoCollection<T> Create<T>(string name)
        => database.CreateCollection<T>(TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..6]);

    private static readonly MongoQueryMode[] Modes = [MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq];

    // Kills ComplexTypeMaterializationBuilder.BuildCollection (wrong BSON type for the collection) and CreateCollection (wrong
    // BSON type for an element): a complex collection whose stored value is not an array of documents/nulls must throw the same
    // FormatException a wrong-typed single complex property does, never read as null or as a list with a default element.
    [Fact]
    public void Complex_collection_stored_with_the_wrong_BSON_type_throws_FormatException()
    {
        var shapes = new (string Tag, BsonValue Lines, string BsonType)[]
        {
            ("int", 5, "Int32"),
            ("doc", new BsonDocument("Label", "x"), "Document"),
            ("string-element", new BsonArray { new BsonDocument("Label", "a"), "x" }, "String"),
            ("int-element", new BsonArray { 7 }, "Int32")
        };

        foreach (var (tag, lines, bsonType) in shapes)
        {
            var collection = Create<KCart>(nameof(Complex_collection_stored_with_the_wrong_BSON_type_throws_FormatException) + tag);
            Raw(collection).InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Lines", lines } });

            foreach (var mode in Modes)
            {
                foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking })
                {
                    var ex = Assert.ThrowsAny<Exception>(() =>
                    {
                        using var db = Context(collection, mode, ConfigureCart, tracking);
                        return db.Entities.ToList();
                    });
                    Assert.True(ex is FormatException, $"{tag} [{mode}/{tracking}]: expected FormatException, got {ex.GetType().Name}: {ex.Message}");
                    Assert.Contains($"Lines' from BsonType '{bsonType}'", ex.Message);
                }
            }
        }
    }

    // Kills MongoStreamingEntityMaterializerRewriter's per-element reset of the complex-property locals: an owned-collection
    // element that lacks an OPTIONAL complex property must read null, not the previous element's value.
    [Fact]
    public void Owned_collection_element_missing_an_optional_complex_property_does_not_inherit_the_previous_elements_value()
    {
        var collection = Create<KOrder>(nameof(Owned_collection_element_missing_an_optional_complex_property_does_not_inherit_the_previous_elements_value));
        BsonDocument Spot(int row, string label) => new() { { "Row", row }, { "Label", label } };
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "o" },
            {
                "Lines", new BsonArray
                {
                    new BsonDocument { { "Sku", "a" }, { "Origin", Spot(1, "o1") } },
                    new BsonDocument { { "Sku", "b" } },
                    new BsonDocument { { "Sku", "c" }, { "Origin", BsonNull.Value } },
                    new BsonDocument { { "Sku", "d" }, { "Origin", Spot(4, "o4") } }
                }
            }
        });

        using (var db = Context(collection, MongoQueryMode.NativeOnly, ConfigureOrder))
        {
            Assert.True(StreamingEligibility.IsEligible(db.Model.FindEntityType(typeof(KOrder))!), "the one-pass shaper must be the one under test");
        }

        foreach (var mode in Modes)
        {
            foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking })
            {
                using var db = Context(collection, mode, ConfigureOrder, tracking);
                var lines = db.Entities.ToList().SelectMany(o => o.Lines).Select(l => $"{l.Sku}:{l.Origin?.Label ?? "<null>"}").ToList();
                Assert.Equal(["a:o1", "b:<null>", "c:<null>", "d:o4"], lines);
            }
        }
    }

    private IMongoCollection<KDoc> SeedDoc(string name)
    {
        var collection = Create<KDoc>(name);
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "d" }, { "Rank", 1 },
            { "Outer", new BsonDocument { { "N", 1 }, { "Inner", new BsonDocument("Label", "i") }, { "Items", new BsonArray { new BsonDocument("Label", "a") } } } }
        });
        return collection;
    }

    // Kills ComplexValueWriter.ThrowIfRequiredMemberIsNull's recursion: a null required member BELOW the first level (a nested
    // complex property, and an element of a nested complex collection) would otherwise be stored as BSON null and fail every
    // later read of the document.
    [Fact]
    public void Bulk_set_of_a_whole_complex_value_with_a_null_required_member_below_the_first_level_is_refused_and_writes_nothing()
    {
        var collection = SeedDoc(nameof(Bulk_set_of_a_whole_complex_value_with_a_null_required_member_below_the_first_level_is_refused_and_writes_nothing));
        var before = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d.ToJson()).ToList();

        using (var db = Context(collection, MongoQueryMode.Native, ConfigureDoc))
        {
            var nested = new KOuter { N = 2, Inner = new KTag { Label = null! } };
            var ex = Assert.ThrowsAny<InvalidOperationException>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(d => d.Outer, nested)));
            Assert.Contains("is null", ex.FullMessage());
            Assert.Contains("Label", ex.FullMessage());

            var inCollection = new KOuter { N = 2, Inner = new KTag { Label = "ok" }, Items = [new KTag { Label = "fine" }, new KTag { Label = null! }] };
            ex = Assert.ThrowsAny<InvalidOperationException>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(d => d.Outer, inCollection)));
            Assert.Contains("is null", ex.FullMessage());
            Assert.Contains("Label", ex.FullMessage());
        }

        Assert.Equal(before, Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d.ToJson()).ToList());
    }

    // Kills ComplexElementNullGuardRefusal's `.Count`-property arm for an OPTIONAL collection: `$size` over a null array is a
    // server error, possibly after earlier writes, so the bulk operation is refused up front.
    [Fact]
    public void Bulk_setter_reading_the_Count_of_an_optional_complex_collection_is_refused_and_writes_nothing()
    {
        var collection = SeedDoc(nameof(Bulk_setter_reading_the_Count_of_an_optional_complex_collection_is_refused_and_writes_nothing));
        var before = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d.ToJson()).ToList();

        using (var db = Context(collection, MongoQueryMode.Native, ConfigureDoc))
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => db.Entities.ExecuteUpdate(s => s.SetProperty(d => d.Rank, d => d.Opt!.Count)));
            Assert.Contains("could not be translated", ex.FullMessage());
            Assert.Contains("No document was modified", ex.FullMessage());
        }

        Assert.Equal(before, Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d.ToJson()).ToList());
    }
}
#endif
