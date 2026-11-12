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
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

/// <summary>
/// Shapes that the other complex type test classes do not reach: a complex collection on a TPH-derived type, a complex
/// property reached through a private backing field, the full lifecycle of an optional nullable-struct complex
/// property, a primitive collection inside a complex collection element, and a query filter that reads a complex leaf.
/// Where the shape needs EF10 complex collections or optional complex properties the test is compiled for EF10 and later only.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeCoverageTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private static readonly MongoQueryMode[] AllModes = [MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq];

    private static SingleEntityDbContext<TEntity> Context<TEntity>(
        IMongoCollection<TEntity> collection,
        MongoQueryMode mode,
        Action<ModelBuilder> configure,
        QueryTrackingBehavior tracking = QueryTrackingBehavior.TrackAll)
        where TEntity : class
        => SingleEntityDbContext.Create(collection, configure, null, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            b.UseQueryTrackingBehavior(tracking);
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    private static IMongoCollection<BsonDocument> Raw<T>(IMongoCollection<T> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    private static string Unique(string name)
        => TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];

    // ── A private backing field reached through the complex property (EF8 and up) ───────────────────────────────

    public class FieldAddress
    {
        public string City { get; set; } = "";
        public int Floor { get; set; }
    }

    public class FieldHolder
    {
        private FieldAddress _address = new();

        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;

        public FieldAddress Address
        {
            get => _address;
            set => _address = value;
        }
    }

    private static void ConfigureFieldHolder(ModelBuilder mb)
        => mb.Entity<FieldHolder>().ComplexProperty(h => h.Address, a => a.IsRequired())
            .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

    [Fact]
    public void Complex_property_with_field_access_round_trips_and_detects_a_leaf_change()
    {
        var collection = database.CreateCollection<FieldHolder>(Unique(nameof(Complex_property_with_field_access_round_trips_and_detects_a_leaf_change)));
        using (var db = Context(collection, MongoQueryMode.Native, ConfigureFieldHolder))
        {
            db.Entities.Add(new FieldHolder { Name = "a", Address = new FieldAddress { City = "Oslo", Floor = 2 } });
            db.SaveChanges();
        }

        var stored = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).Single();
        Assert.Equal("Oslo", stored["Address"]["City"].AsString);
        Assert.Equal(2, stored["Address"]["Floor"].AsInt32);

        foreach (var mode in AllModes)
        {
            foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking })
            {
                using var db = Context(collection, mode, ConfigureFieldHolder, tracking);
                var entity = db.Entities.Single();
                Assert.Equal("Oslo", entity.Address.City);
                Assert.Equal(
                    ["Oslo"], db.Entities.Select(h => h.Address.City).ToList());
                Assert.Equal(
                    ["Oslo:2"], db.Entities.AsNoTracking().Select(h => h.Address).ToList().Select(a => $"{a.City}:{a.Floor}").ToList());
            }
        }

        using (var db = Context(collection, MongoQueryMode.Native, ConfigureFieldHolder))
        {
            db.Entities.Single().Address.City = "Bergen";
            db.SaveChanges();
        }

        Assert.Equal("Bergen", Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).Single()["Address"]["City"].AsString);
    }

#if !EF8 && !EF9
    // ── TPH: a complex collection on a derived type ─────────────────────────────────────────────────────────────

    public class Toy
    {
        public string Name { get; set; } = null!;
        public int Price { get; set; }
    }

    public abstract class Pet
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class Dog : Pet
    {
        public List<Toy> Toys { get; set; } = [];
    }

    public class Cat : Pet
    {
        public int Lives { get; set; }
    }

    private static void ConfigurePets(ModelBuilder mb)
    {
        mb.Entity<Pet>();
        mb.Entity<Cat>();
        mb.Entity<Dog>().ComplexCollection(d => d.Toys);
    }

    [Fact]
    public void Tph_complex_collection_on_a_derived_type_round_trips_reads_queries_and_edits()
    {
        var collection = database.CreateCollection<Pet>(Unique(nameof(Tph_complex_collection_on_a_derived_type_round_trips_reads_queries_and_edits)));
        using (var db = Context(collection, MongoQueryMode.Native, ConfigurePets))
        {
            db.Entities.Add(new Dog { Name = "rex", Toys = [new Toy { Name = "ball", Price = 3 }, new Toy { Name = "bone", Price = 1 }] });
            db.Entities.Add(new Dog { Name = "fido" });
            db.Entities.Add(new Cat { Name = "tom", Lives = 9 });
            db.SaveChanges();
        }

        var raw = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["Name"].AsString);
        Assert.Equal(2, raw["rex"]["Toys"].AsBsonArray.Count);
        Assert.Equal("ball", raw["rex"]["Toys"][0]["Name"].AsString);
        Assert.Empty(raw["fido"]["Toys"].AsBsonArray);
        Assert.False(raw["tom"].Contains("Toys"));

        foreach (var mode in AllModes)
        {
            foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking })
            {
                using var db = Context(collection, mode, ConfigurePets, tracking);
                var all = db.Entities.OrderBy(p => p.Name).ToList();
                Assert.Equal(["fido", "rex", "tom"], all.Select(p => p.Name));
                var rex = Assert.IsType<Dog>(all[1]);
                Assert.Equal(["ball", "bone"], rex.Toys.Select(t => t.Name));
                Assert.Empty(Assert.IsType<Dog>(all[0]).Toys);
                if (mode == MongoQueryMode.NativeOnly)
                {
                    // A member declared only on the derived type, read after OfType, is not natively translated.
                    Assert.Throws<NativeTranslationNotSupportedException>(
                        () => db.Entities.OfType<Dog>().Where(d => d.Toys.Any(t => t.Name == "ball")).Select(d => d.Name).ToList());
                }
                else
                {
                    Assert.Equal(["rex"], db.Entities.OfType<Dog>().Where(d => d.Toys.Any(t => t.Name == "ball")).Select(d => d.Name).ToList());
                }
            }
        }

        using (var db = Context(collection, MongoQueryMode.Native, ConfigurePets))
        {
            var rex = db.Entities.OfType<Dog>().Single(d => d.Name == "rex");
            rex.Toys.Add(new Toy { Name = "rope", Price = 2 });
            rex.Toys[0].Price = 4;
            db.SaveChanges();
        }

        var edited = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "rex")).Single();
        Assert.Equal(["ball", "bone", "rope"], edited["Toys"].AsBsonArray.Select(t => t["Name"].AsString));
        Assert.Equal(4, edited["Toys"][0]["Price"].AsInt32);
        Assert.False(Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "tom")).Single().Contains("Toys"));
    }

    // ── Optional nullable-struct complex property: the whole lifecycle ──────────────────────────────────────────

    public class PinHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public GeoPoint? Pin { get; set; }
    }

    private static void ConfigurePinHolder(ModelBuilder mb) => mb.Entity<PinHolder>().ComplexProperty(h => h.Pin);

    [Fact]
    public void Optional_nullable_struct_complex_property_null_to_value_to_null_and_bulk_set_to_null()
    {
        var collection = database.CreateCollection<PinHolder>(Unique(nameof(Optional_nullable_struct_complex_property_null_to_value_to_null_and_bulk_set_to_null)));
        BsonValue Pin() => Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).Single()["Pin"];

        using (var db = Context(collection, MongoQueryMode.Native, ConfigurePinHolder))
        {
            db.Entities.Add(new PinHolder { Name = "p" });
            db.SaveChanges();
        }

        Assert.True(Pin().IsBsonNull);

        using (var db = Context(collection, MongoQueryMode.Native, ConfigurePinHolder))
        {
            db.Entities.Single().Pin = new GeoPoint { Lat = 1.5, Lon = 2.5 };
            db.SaveChanges();
        }

        Assert.Equal(1.5, Pin()["Lat"].AsDouble);
        Assert.Equal(2.5, Pin()["Lon"].AsDouble);

        foreach (var mode in AllModes)
        {
            using var db = Context(collection, mode, ConfigurePinHolder, QueryTrackingBehavior.NoTracking);
            Assert.Equal("1.5,2.5", Format(db.Entities.Single().Pin));
            Assert.Equal(["1.5,2.5"], db.Entities.Select(h => h.Pin).ToList().Select(Format).ToList());
            if (mode == MongoQueryMode.NativeOnly)
            {
                // A leaf read through an optional nullable-struct property is not natively translated.
                Assert.Throws<NativeTranslationNotSupportedException>(() => db.Entities.Select(h => h.Pin!.Value.Lat).ToList());
            }
            else
            {
                Assert.Equal([1.5], db.Entities.Select(h => h.Pin!.Value.Lat).ToList());
            }
        }

        using (var db = Context(collection, MongoQueryMode.Native, ConfigurePinHolder))
        {
            db.Entities.Single().Pin = null;
            db.SaveChanges();
        }

        Assert.True(Pin().IsBsonNull);

        foreach (var mode in AllModes)
        {
            using var db = Context(collection, mode, ConfigurePinHolder, QueryTrackingBehavior.NoTracking);
            Assert.Null(db.Entities.Single().Pin);
            Assert.Equal(["<null>"], db.Entities.Select(h => h.Pin).ToList().Select(Format).ToList());
            Assert.Equal(["p"], db.Entities.Where(h => h.Pin == null).Select(h => h.Name).ToList());
        }

        // The stored value is set again, then removed with a bulk update.
        using (var db = Context(collection, MongoQueryMode.Native, ConfigurePinHolder))
        {
            db.Entities.Single().Pin = new GeoPoint { Lat = 3, Lon = 4 };
            db.SaveChanges();
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s.SetProperty(h => h.Pin, (GeoPoint?)null)));
        }

        Assert.True(Pin().IsBsonNull);

        static string Format(GeoPoint? p) => p is { } v ? $"{v.Lat},{v.Lon}" : "<null>";
    }

    // ── A primitive collection inside a complex collection element ──────────────────────────────────────────────

    public class Variant
    {
        public string Label { get; set; } = null!;
        public List<int> Nums { get; set; } = [];
    }

    public class VariantHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Variant> Many { get; set; } = [];
    }

    private static void ConfigureVariantHolder(ModelBuilder mb) => mb.Entity<VariantHolder>().ComplexCollection(h => h.Many);

    [Fact]
    public void Primitive_collection_inside_a_complex_collection_element_contains_runs_in_every_mode()
    {
        var collection = database.CreateCollection<VariantHolder>(Unique(nameof(Primitive_collection_inside_a_complex_collection_element_contains_runs_in_every_mode)));
        BsonDocument Element(string label, params int[] nums) => new() { { "Label", label }, { "Nums", new BsonArray(nums) } };
        Raw(collection).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "has" }, { "Many", new BsonArray { Element("a", 5, 6), Element("b") } } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "empty" }, { "Many", new BsonArray { Element("c") } } }
        ]);

        List<string> Run(MongoQueryMode mode)
        {
            using var db = Context(collection, mode, ConfigureVariantHolder, QueryTrackingBehavior.NoTracking);
            return db.Entities.Where(h => h.Many.Any(m => m.Nums.Contains(5))).Select(h => h.Name).ToList();
        }

        // #40: translated natively ($in over the array coalesced to empty), so every mode serves the data.
        foreach (var mode in AllModes)
        {
            Assert.Equal(["has"], Run(mode));
        }

        // A null element reads its members as missing, so the driver's `$in` fails the WHOLE query; the native modes read a
        // missing array as empty and are unaffected (the full oracle comparison is ComplexElementArrayContainsTests).
        Raw(collection).InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "nullelem" }, { "Many", new BsonArray { BsonNull.Value } } });
        Assert.Equal(["has"], Run(MongoQueryMode.Native));
        Assert.Equal(["has"], Run(MongoQueryMode.NativeOnly));
        Assert.Throws<MongoCommandException>(() => Run(MongoQueryMode.DriverLinq));
    }

    // ── A query filter that reads a complex leaf ────────────────────────────────────────────────────────────────

    public class Place
    {
        public string City { get; set; } = null!;
    }

    public class Line
    {
        public int Qty { get; set; }
    }

    public class FilteredOrder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Place Where { get; set; } = null!;
        public List<Line> Lines { get; set; } = [];
    }

    private static void ConfigureFilteredOrder(ModelBuilder mb)
    {
        mb.Entity<FilteredOrder>(e =>
        {
            e.ComplexProperty(o => o.Where);
            e.ComplexCollection(o => o.Lines);
            e.HasQueryFilter(o => o.Where.City != "Hidden");
        });
    }

    [Fact]
    public void Query_filter_on_a_complex_leaf_combines_with_a_complex_collection_predicate()
    {
        var collection = database.CreateCollection<FilteredOrder>(Unique(nameof(Query_filter_on_a_complex_leaf_combines_with_a_complex_collection_predicate)));
        BsonDocument Doc(string name, string city, params int[] qtys)
            => new()
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", name }, { "Where", new BsonDocument("City", city) },
                { "Lines", new BsonArray(qtys.Select(q => new BsonDocument("Qty", q))) }
            };

        Raw(collection).InsertMany([Doc("shown-big", "Oslo", 1, 5), Doc("shown-small", "Oslo", 1), Doc("hidden-big", "Hidden", 9)]);

        foreach (var mode in AllModes)
        {
            using var db = Context(collection, mode, ConfigureFilteredOrder, QueryTrackingBehavior.NoTracking);
            Assert.Equal(["shown-big", "shown-small"], db.Entities.OrderBy(o => o.Name).Select(o => o.Name).ToList());
            Assert.Equal(["shown-big"], db.Entities.Where(o => o.Lines.Any(l => l.Qty > 2)).Select(o => o.Name).ToList());

            using var unfiltered = Context(collection, mode, ConfigureFilteredOrder, QueryTrackingBehavior.NoTracking);
            Assert.Equal(
                ["hidden-big", "shown-big"],
                unfiltered.Entities.IgnoreQueryFilters().Where(o => o.Lines.Any(l => l.Qty > 2)).OrderBy(o => o.Name).Select(o => o.Name).ToList());
        }
    }
#endif
}
