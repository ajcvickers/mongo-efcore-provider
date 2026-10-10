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
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// A member read through a DOWNCAST of a TPH root (<c>((Derived)b).Detours</c>, <c>(a as Cat)!.Vet.Code</c>) must be
/// resolved against the CAST type, never the operand's (base) type: the walkers that key the R20/R25 refusal net, the bulk
/// allow-list and the EF-337 stored-ordering refusal stripped the cast and looked the member up on the base type, found
/// nothing, and let the driver serve (or write) wrong rows. One shared resolver (<c>MemberOwnerType</c>) now decides the
/// owner type for all three. Every row is hand-written.
/// </summary>
[XUnitCollection("QueryTests")]
public partial class ComplexTypeDowncastTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private DbContextOptions Options<TContext>(MongoQueryMode mode) where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        return builder.Options;
    }

    private string Collection(string name, params BsonDocument[] documents)
    {
        var collection = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        database.MongoDatabase.GetCollection<BsonDocument>(collection).InsertMany(documents.Select(d => d.DeepClone().AsBsonDocument));
        return collection;
    }

    private List<string> RawNames(string collection)
        => [.. database.MongoDatabase.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList()
            .Select(d => d["Name"].AsString + ":" + d["Rank"].ToString()).Order(StringComparer.Ordinal)];

    private void AssertRawUnchanged(string collection, BsonDocument[] seed)
        => Assert.Equal(
            seed.Select(d => d.ToJson()).Order(StringComparer.Ordinal),
            database.MongoDatabase.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList()
                .Select(d => d.ToJson()).Order(StringComparer.Ordinal));

    // ── EF-337 stored-ordering refusal through a downcast (every EF version) ──────────────────────────────────────

    public class Vet
    {
        public int Code { get; set; }
    }

    public class Animal
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
    }

    public class Cat : Animal
    {
        public Vet Vet { get; set; } = null!;
        public int Lives { get; set; }
    }

    private sealed class ZooContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<Animal> Animals => Set<Animal>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Animal>(b =>
            {
                b.ToCollection(collection);
                b.HasDiscriminator<string>("_t").HasValue<Animal>("A").HasValue<Cat>("C");
            });
            mb.Entity<Cat>(b =>
            {
                // Stored as strings: "10" sorts below "2", so a server-side comparison of the stored form is wrong.
                b.ComplexProperty(c => c.Vet, v => v.Property(x => x.Code).HasConversion<string>());
                b.Property(c => c.Lives).HasConversion<string>();
            });
        }
    }

    private const string StoredOrderingRefusalMessage = "cannot be translated to a MongoDB query: the property is stored through a value converter";


    private static readonly Outcome StoredOrderingRefusal = Throws<NotSupportedException>(StoredOrderingRefusalMessage);
    // tom (Cat, Vet.Code "10", Lives "9"), kit (Cat, Vet.Code "2", Lives "3"), rex (Animal).
    private static BsonDocument[] ZooSeed()
        =>
        [
            new() { { "_id", ObjectId.GenerateNewId() }, { "_t", "C" }, { "Name", "tom" }, { "Rank", 1 }, { "Vet", new BsonDocument("Code", "10") }, { "Lives", "9" } },
            new() { { "_id", ObjectId.GenerateNewId() }, { "_t", "C" }, { "Name", "kit" }, { "Rank", 2 }, { "Vet", new BsonDocument("Code", "2") }, { "Lives", "3" } },
            new() { { "_id", ObjectId.GenerateNewId() }, { "_t", "A" }, { "Name", "rex" }, { "Rank", 3 } }
        ];

    private Func<MongoQueryMode, List<string>> Zoo(Func<ZooContext, IEnumerable<string>> query,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = Collection(name, ZooSeed());
        return mode =>
        {
            using var db = new ZooContext(Options<ZooContext>(mode), collection);
            return query(db).ToList();
        };
    }

    [Fact]
    public void Relational_comparison_of_a_converted_complex_leaf_through_a_downcast_is_refused()
    {
        // RED (5547cfa2): Native and DriverLinq returned [] (the stored strings compared with an int); C# answers [tom]. The
        // OfType<Cat>() spelling was already refused; the cast spellings now are too, in every mode.
        PerMode(Zoo(db => [.. db.Animals.Where(a => a is Cat && ((Cat)a).Vet.Code > 5).Select(a => a.Name)]),
            [], NotNative, StoredOrderingRefusal, StoredOrderingRefusal);
        PerMode(Zoo(db => [.. db.Animals.Where(a => a is Cat && (a as Cat)!.Vet.Code > 5).Select(a => a.Name)]),
            [], NotNative, StoredOrderingRefusal, StoredOrderingRefusal);
        PerMode(Zoo(db => [.. db.Animals.Where(a => a is Cat).OrderBy(a => ((Cat)a).Vet.Code).Select(a => a.Name)]),
            [], NotNative, StoredOrderingRefusal, StoredOrderingRefusal);
        PerMode(Zoo(db => [.. db.Animals.Where(a => a is Cat && ((Cat)(object)a).Vet.Code > 5).Select(a => a.Name)]),
            [], NotNative, StoredOrderingRefusal, StoredOrderingRefusal);
        PerMode(Zoo(db => [db.Animals.Where(a => a is Cat).Max(a => ((Cat)a).Vet.Code).ToString()]),
            [], NotNative, StoredOrderingRefusal, StoredOrderingRefusal);
        // The OfType spelling (unchanged control).
        PerMode(Zoo(db => [.. db.Animals.OfType<Cat>().Where(c => c.Vet.Code > 5).Select(a => a.Name)]),
            [], NotNative, StoredOrderingRefusal, StoredOrderingRefusal);
    }

    [Fact]
    public void Equality_of_a_converted_member_through_a_downcast_is_a_known_pre_existing_driver_misread()
    {
        // NOT fixed here (pre-existing, non-complex too): the native path declines a member read through a cast, and the
        // driver serializes the comparand of a member read through `(Cat)a` with the default class map (an int), not the
        // EF property's converter (a string), so the equality never matches. C# answers [tom] for each. The OfType spelling
        // (control) is served. Listed for the owner; pinned so a fix is noticed.
        PerMode(Zoo(db => [.. db.Animals.Where(a => a is Cat && ((Cat)a).Vet.Code == 10).Select(a => a.Name)]), [], NotNative, Serves, Serves);
        PerMode(Zoo(db => [.. db.Animals.Where(a => a is Cat && ((Cat)a).Lives == 9).Select(a => a.Name)]), [], NotNative, Serves, Serves);
        Declines(Zoo(db => [.. db.Animals.OfType<Cat>().Where(c => c.Vet.Code == 10).Select(a => a.Name)]), "tom");
    }

    [Fact]
    public void Relational_comparison_of_a_converted_root_scalar_through_a_downcast_is_refused()
        // Non-complex, pre-existing at origin/EF-322c (returned []; C# [tom]): the shared owner-type resolver fixes it too.
        => PerMode(Zoo(db => [.. db.Animals.Where(a => a is Cat && ((Cat)a).Lives > 5).Select(a => a.Name)]),
            [], NotNative, StoredOrderingRefusal, StoredOrderingRefusal);

#if !EF8
    [Fact]
    public void Bulk_delete_filtered_by_a_converted_complex_leaf_through_a_downcast_is_refused_before_any_write()
    {
        // RED (5547cfa2): deleted 0 (C# deletes kit: its Code 2 < 5). Now refused like the OfType spelling; nothing deleted.
        var seed = ZooSeed();
        var collection = Collection(nameof(Bulk_delete_filtered_by_a_converted_complex_leaf_through_a_downcast_is_refused_before_any_write), seed);
        using (var db = new ZooContext(Options<ZooContext>(MongoQueryMode.Native), collection))
        {
            var ex = Assert.Throws<InvalidOperationException>(() => db.Animals.Where(a => a is Cat && ((Cat)a).Vet.Code < 5).ExecuteDelete());
            Assert.Contains(StoredOrderingRefusalMessage, ex.Message);
            ex = Assert.Throws<InvalidOperationException>(
                () => db.Animals.Where(a => a is Cat && (a as Cat)!.Vet.Code < 5).ExecuteUpdate(s => s.SetProperty(a => a.Rank, 9)));
            Assert.Contains(StoredOrderingRefusalMessage, ex.Message);
        }

        AssertRawUnchanged(collection, seed);
    }
#endif

#if !EF8 && !EF9
    // ── R20/R25 refusal net and the bulk allow-list through a downcast (complex collections: EF10) ────────────────

    public class Stop
    {
        public string City { get; set; } = null!;
        public int Floor { get; set; }
    }

    public class Crate
    {
        public List<Stop> Items { get; set; } = [];
    }

    public class Base
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public List<Stop> BaseStops { get; set; } = [];
    }

    public class Derived : Base
    {
        public List<Stop> Stops { get; set; } = [];
        public List<Stop>? Detours { get; set; }
        public Crate Box { get; set; } = null!;
    }

    private sealed class FleetContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<Base> Fleet => Set<Base>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Base>(b =>
            {
                b.ToCollection(collection);
                b.HasDiscriminator<string>("_t").HasValue<Base>("B").HasValue<Derived>("D");
                b.ComplexCollection(x => x.BaseStops);
            });
            mb.Entity<Derived>(b =>
            {
                b.ComplexCollection(x => x.Stops);
                b.ComplexCollection(x => x.Detours);
                b.ComplexProperty(x => x.Box, c => c.ComplexCollection(x => x.Items));
            });
        }
    }

    private static BsonDocument Oslo => new() { { "City", "Oslo" }, { "Floor", 5 } };

    /// <summary>
    /// b (Base; BaseStops [null]); d-nullelem (Stops [null], Detours [null], Box.Items [null], BaseStops [Oslo]); d-full
    /// (Stops/Detours/Box.Items [Oslo], BaseStops []); d-nullarr (Stops null, Detours null, Box.Items null, BaseStops [Oslo]).
    /// </summary>
    private static BsonDocument[] FleetSeed()
    {
        BsonValue NullElem() => new BsonArray { BsonNull.Value };
        BsonValue Full() => new BsonArray { Oslo };
        return
        [
            new() { { "_id", ObjectId.GenerateNewId() }, { "_t", "B" }, { "Name", "b" }, { "Rank", 1 }, { "BaseStops", NullElem() } },
            new()
            {
                { "_id", ObjectId.GenerateNewId() }, { "_t", "D" }, { "Name", "d-nullelem" }, { "Rank", 2 }, { "BaseStops", Full() },
                { "Stops", NullElem() }, { "Detours", NullElem() }, { "Box", new BsonDocument("Items", NullElem()) }
            },
            new()
            {
                { "_id", ObjectId.GenerateNewId() }, { "_t", "D" }, { "Name", "d-full" }, { "Rank", 3 }, { "BaseStops", new BsonArray() },
                { "Stops", Full() }, { "Detours", Full() }, { "Box", new BsonDocument("Items", Full()) }
            },
            new()
            {
                { "_id", ObjectId.GenerateNewId() }, { "_t", "D" }, { "Name", "d-nullarr" }, { "Rank", 4 }, { "BaseStops", Full() },
                { "Stops", BsonNull.Value }, { "Detours", BsonNull.Value }, { "Box", new BsonDocument("Items", BsonNull.Value) }
            }
        ];
    }

    private Func<MongoQueryMode, List<string>> Fleet(Func<IQueryable<Base>, IEnumerable<string>> query,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = Collection(name, FleetSeed());
        return mode =>
        {
            using var db = new FleetContext(Options<FleetContext>(mode), collection);
            return query(db.Fleet).ToList();
        };
    }

    private static IEnumerable<string> Names(IQueryable<Base> q) => q.Select(x => x.Name).ToList().Order(StringComparer.Ordinal);

    private const string CollectionNullRefusal = "comparison of the complex collection 'Derived.Detours' with null incorrectly";
    private const string ElementRefusal = "over an element of the complex collection";

    [Fact]
    public void Derived_collection_compared_with_null_through_a_downcast_is_refused()
    {
        // RED (5547cfa2): Native served the driver's [b, d-nullarr, d-nullelem] (C#: [b, d-nullarr]: Detours == null is true
        // only for a null array and for a row that is not a Derived); the OfType spelling was already refused (R25).
        Refused(Fleet(q => Names(q.Where(x => ((Derived)x).Detours == null))), ["b", "d-nullarr", "d-nullelem"], CollectionNullRefusal);
        Refused(Fleet(q => Names(q.Where(x => (x as Derived)!.Detours == null))), ["b", "d-nullarr", "d-nullelem"], CollectionNullRefusal);
        // A nested downcast (through object) resolves to the outermost cast type too.
        Refused(Fleet(q => Names(q.Where(x => ((Derived)(object)x).Detours == null))), ["b", "d-nullarr", "d-nullelem"], CollectionNullRefusal);
        // C#: [d-full, d-nullelem]; the driver's `$ne: null` drops d-nullelem.
        Refused(Fleet(q => Names(q.Where(x => ((Derived)x).Detours != null))), ["d-full"], CollectionNullRefusal);
        // In a projection.
        Refused(Fleet(q => q.OrderBy(x => x.Name).Select(x => ((Derived)x).Detours == null).ToList().Select(v => v.ToString())),
            // DriverLinq (measured): b's missing Detours is not `$eq` null in the aggregation dialect, d-nullelem's [null] is not
            // null either; C# answers [False, True, True, True] (b is not a Derived: EF reads its derived members as null).
            ["False", "False", "True", "False"], CollectionNullRefusal);
    }

    [Fact]
    public void Null_guard_requiring_element_predicate_through_a_downcast_is_refused()
    {
        // RED (5547cfa2): Native served the driver's [d-nullelem] (its null element's missing Floor ordered below 1); C#: [].
        Refused(Fleet(q => Names(q.Where(x => ((Derived)x).Stops.Any(s => s.Floor < 1)))), ["d-nullelem"], ElementRefusal, "'Derived.Stops'");
        Refused(Fleet(q => Names(q.Where(x => (x as Derived)!.Stops.Any(s => s.Floor < 1)))), ["d-nullelem"], ElementRefusal, "'Derived.Stops'");
        // Through a complex hop of the derived type.
        Refused(Fleet(q => Names(q.Where(x => ((Derived)x).Box.Items.Any(s => s.Floor < 1)))), ["d-nullelem"], ElementRefusal, "Crate.Items'");
        // In an ordering key.
        Refused(Fleet(q => q.OrderBy(x => ((Derived)x).Stops.Count(s => s.Floor < 1)).ThenBy(x => x.Name).Select(x => x.Name)),
            ["b", "d-full", "d-nullarr", "d-nullelem"], ElementRefusal, "'Derived.Stops'");
    }

    [Fact]
    public void Base_declared_collection_through_a_downcast_was_already_keyed()
        // Control: a base-declared collection resolved on the base type before the fix too. b's null element and
        // d-nullelem... (BaseStops [Oslo]) don't match `< 1`: C# []. Driver: [b] (b's null element).
        => Refused(Fleet(q => Names(q.Where(x => ((Derived)x).BaseStops.Any(s => s.Floor < 1)))), ["b"], ElementRefusal, "'Base.BaseStops'");

    private const string BulkRefusal = "ExecuteUpdate and ExecuteDelete run their filter and SetProperty values on driver-LINQ";

    private static void AssertBulkRefused(Func<int> operation, string shape)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => operation());
        var refusal = Assert.IsType<NativeTranslationNotSupportedException>(ex.InnerException);
        Assert.Contains(BulkRefusal, refusal.Message);
        Assert.Contains(shape, refusal.Message);
        Assert.Contains("No document was modified", refusal.Message);
    }

    [Fact]
    public void Bulk_operations_reading_a_derived_collection_through_a_downcast_are_refused_before_any_write()
    {
        var seed = FleetSeed();
        var collection = Collection(nameof(Bulk_operations_reading_a_derived_collection_through_a_downcast_are_refused_before_any_write), seed);
        using (var db = new FleetContext(Options<FleetContext>(MongoQueryMode.Native), collection))
        {
            const string read = "a read of the collection other than as the source of";
            // RED (5547cfa2): deleted b, d-nullarr AND d-nullelem (C#: d-nullelem's Detours is not null).
            AssertBulkRefused(() => db.Fleet.Where(x => ((Derived)x).Detours == null).ExecuteDelete(), read);
            AssertBulkRefused(() => db.Fleet.Where(x => (x as Derived)!.Detours == null).ExecuteDelete(), read);
            // RED: updated only d-full (C#: d-nullelem too).
            AssertBulkRefused(() => db.Fleet.Where(x => ((Derived)x).Detours != null).ExecuteUpdate(s => s.SetProperty(x => x.Rank, 9)), read);
            // A setter value reading the collection through a downcast.
            AssertBulkRefused(
                () => db.Fleet.ExecuteUpdate(s => s.SetProperty(x => x.Rank, x => ((Derived)x).Detours == null ? 1 : 0)), read);
            // An operator over the OPTIONAL derived collection.
            AssertBulkRefused(() => db.Fleet.Where(x => ((Derived)x).Detours!.Any(s => s.City == "Oslo")).ExecuteDelete(),
                "'Any' over an optional complex collection");
            // A relational element predicate over the required derived collection: refused by the allow-list (it was refused
            // before only as an element lambda the check could not bind).
            AssertBulkRefused(() => db.Fleet.Where(x => ((Derived)x).Stops.Any(s => s.Floor < 1)).ExecuteDelete(), "the element predicate");
            AssertBulkRefused(() => db.Fleet.Where(x => ((Derived)x).Box.Items.Any(s => s.Floor < 1)).ExecuteDelete(), "the element predicate");
        }

        AssertRawUnchanged(collection, seed);
    }

    [Fact]
    public void Bulk_update_with_an_allowed_element_predicate_through_a_downcast()
    {
        // An allow-listed atom (`== non-null`) over a REQUIRED derived collection is admitted: d-full's Oslo matches; the null
        // element's City is missing on the server, not equal to "Oslo" (R17's `null == "Oslo"` is false too).
        var seed = FleetSeed();
        var collection = Collection(nameof(Bulk_update_with_an_allowed_element_predicate_through_a_downcast), seed);
        using (var db = new FleetContext(Options<FleetContext>(MongoQueryMode.Native), collection))
        {
            Assert.Equal(1, db.Fleet.Where(x => ((Derived)x).Stops.Any(s => s.City == "Oslo")).ExecuteUpdate(s => s.SetProperty(x => x.Rank, 9)));
        }

        Assert.Equal(["b:1", "d-full:9", "d-nullarr:4", "d-nullelem:2"], RawNames(collection));
    }
#endif
}
