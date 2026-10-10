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

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Robustness of complex types (Task 15) on the tracking, model-caching, compiled-query and SaveChanges side: tracking
/// behaviours, shared value instances, two models with different element names in one process, concurrency over the
/// new caches, re-executed cached queries with different captured values, and SaveChanges edge cases.
/// </summary>
[XUnitCollection("UpdateTests")]
public class ComplexTypeRobustnessUpdateTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Addr
    {
        public string City { get; set; } = null!;
        public int Zip { get; set; }
        public GeoPoint Geo { get; set; }
    }

    public class Person
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Addr Home { get; set; } = null!;
        public Addr Work { get; set; } = null!;
#if !EF8 && !EF9
        public List<Addr> Past { get; set; } = [];
#endif
    }

    private static void ConfigurePerson(ModelBuilder mb)
        => mb.Entity<Person>(e =>
        {
            e.ComplexProperty(p => p.Home, h => h.ComplexProperty(x => x.Geo));
            e.ComplexProperty(p => p.Work, w =>
            {
                w.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "job");
                w.ComplexProperty(x => x.Geo);
            });
#if !EF8 && !EF9
            e.ComplexCollection(p => p.Past, h => h.ComplexProperty(x => x.Geo));
#endif
        });

    private IMongoCollection<T> Collection<T>([CallerMemberName] string name = "")
        => database.CreateCollection<T>(TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8]);

    private static IMongoCollection<BsonDocument> Raw<T>(IMongoCollection<T> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    private static SingleEntityDbContext<T> Context<T>(
        IMongoCollection<T> collection,
        Action<ModelBuilder> configure,
        MongoQueryMode mode = MongoQueryMode.Native,
        QueryTrackingBehavior tracking = QueryTrackingBehavior.TrackAll)
        where T : class
        => SingleEntityDbContext.Create(collection, configure, null, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            b.UseQueryTrackingBehavior(tracking);
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    private static Addr A(string city, int zip, double lat = 0) => new() { City = city, Zip = zip, Geo = new GeoPoint { Lat = lat, Lon = -lat } };

    private static Person P(string name, Addr home, Addr work)
        => new()
        {
            Name = name, Home = home, Work = work,
#if !EF8 && !EF9
            Past = [A(name + "-p", 1)]
#endif
        };

    private static string Fmt(Person p)
        => $"{p.Name}|{p.Home.City}:{p.Home.Zip}:{p.Home.Geo.Lat}|{p.Work.City}:{p.Work.Zip}"
#if !EF8 && !EF9
           + "|" + string.Join(",", p.Past.Select(x => x.City))
#endif
        ;

    private IMongoCollection<Person> SeedPeople([CallerMemberName] string name = "")
    {
        var collection = Collection<Person>(name);
        using var db = Context(collection, ConfigurePerson);
        db.Entities.AddRange(P("a", A("Oslo", 1, 1.5), A("Rome", 2)), P("b", A("Rome", 3, 2.5), A("Oslo", 4)));
        db.SaveChanges();
        return collection;
    }

    // ── (7) Tracking ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Every_tracking_behavior_returns_identical_complex_values()
    {
        var collection = SeedPeople();
        var results = new Dictionary<QueryTrackingBehavior, List<string>>();
        foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking, QueryTrackingBehavior.NoTrackingWithIdentityResolution })
        {
            foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
            {
                using var db = Context(collection, ConfigurePerson, mode, tracking);
                var rows = db.Entities.OrderBy(p => p.Name).ToList().Select(Fmt).ToList();
                if (results.TryGetValue(tracking, out var first))
                {
                    Assert.Equal(first, rows);
                }
                else
                {
                    results[tracking] = rows;
                }
            }
        }

        Assert.Equal(results[QueryTrackingBehavior.TrackAll], results[QueryTrackingBehavior.NoTracking]);
        Assert.Equal(results[QueryTrackingBehavior.TrackAll], results[QueryTrackingBehavior.NoTrackingWithIdentityResolution]);
        Assert.Equal(
            [
#if !EF8 && !EF9
                "a|Oslo:1:1.5|Rome:2|a-p", "b|Rome:3:2.5|Oslo:4|b-p"
#else
                "a|Oslo:1:1.5|Rome:2", "b|Rome:3:2.5|Oslo:4"
#endif
            ],
            results[QueryTrackingBehavior.TrackAll]);
    }

    [Theory]
    [InlineData(QueryTrackingBehavior.TrackAll)]
    [InlineData(QueryTrackingBehavior.NoTrackingWithIdentityResolution)]
    public void Identity_resolution_resolves_entities_but_never_aliases_complex_values(QueryTrackingBehavior tracking)
    {
        var collection = SeedPeople(nameof(Identity_resolution_resolves_entities_but_never_aliases_complex_values) + tracking);
        using var db = Context(collection, ConfigurePerson, MongoQueryMode.NativeOnly, tracking);

        var first = db.Entities.OrderBy(p => p.Name).ToList();
        var second = db.Entities.OrderBy(p => p.Name).ToList();

        // TrackAll resolves the entity across queries (NoTrackingWithIdentityResolution only within one query).
        if (tracking == QueryTrackingBehavior.TrackAll)
        {
            Assert.Same(first[0], second[0]);
            Assert.Same(first[0].Home, second[0].Home);
        }
        else
        {
            Assert.NotSame(first[0], second[0]);
            Assert.NotSame(first[0].Home, second[0].Home);
        }

        // Two entities, and two complex properties of one entity, never share a complex value instance, even where the
        // stored values are equal (a's Work and b's Home are both Rome/...; they differ in Zip, so compare refs only).
        Assert.NotSame(first[0].Home, first[1].Home);
        Assert.NotSame(first[0].Home, first[0].Work);
        Assert.NotSame(first[0].Work, first[1].Home);
    }

    [Fact]
    public void Load_mutate_leaf_save_rewrites_only_that_entitys_subdocument()
    {
        var collection = SeedPeople();
        var before = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["Name"].AsString);
        using var capture = new CommandCapture(database);
        var captured = capture.Collection(collection);

        using (var db = Context(captured, ConfigurePerson, MongoQueryMode.NativeOnly))
        {
            var people = db.Entities.OrderBy(p => p.Name).ToList();
            capture.Clear();
            people[1].Work.Zip = 99;
            Assert.Equal(1, db.SaveChanges());
        }

        var update = Assert.Single(capture.Named("update"));
        var statement = Assert.Single(update["updates"].AsBsonArray).AsBsonDocument;
        Assert.Equal(before["b"]["_id"], statement["q"]["_id"]);
        Assert.Equal(["_id", "job"], statement["u"]["$set"].AsBsonDocument.Names.ToArray());

        var after = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["Name"].AsString);
        Assert.Equal(before["a"], after["a"]);
        before["b"]["job"]["Zip"] = 99;
        Assert.Equal(before["b"], after["b"]);
    }

    [Fact]
    public void One_complex_instance_assigned_to_two_entities_is_written_as_two_independent_copies()
    {
        // Measured on EF8/EF9/EF10: EF does not reject a complex VALUE shared by reference between entities (complex
        // values have no identity); each entity's snapshot is its own, and each document gets its own copy.
        var collection = Collection<Person>();
        var shared = A("Shared", 5, 9);
        using (var db = Context(collection, ConfigurePerson))
        {
            var a = P("a", shared, A("w", 1));
            var b = P("b", shared, shared);
            db.Entities.AddRange(a, b);
            Assert.Equal(2, db.SaveChanges());

            // Mutating the shared instance changes it for every holder in memory; DetectChanges sees all three slots.
            shared.Zip = 6;
            Assert.Equal(2, db.SaveChanges());
        }

        var docs = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["Name"].AsString);
        Assert.Equal(6, docs["a"]["Home"]["Zip"].AsInt32);
        Assert.Equal(6, docs["b"]["Home"]["Zip"].AsInt32);
        Assert.Equal(6, docs["b"]["job"]["Zip"].AsInt32);
        Assert.Equal(1, docs["a"]["job"]["Zip"].AsInt32);

        // After a reload the values are independent: changing one entity's leaf leaves the other alone.
        using (var db = Context(collection, ConfigurePerson))
        {
            var people = db.Entities.OrderBy(p => p.Name).ToList();
            Assert.NotSame(people[0].Home, people[1].Home);
            people[0].Home.City = "Only-a";
            db.SaveChanges();
        }

        docs = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["Name"].AsString);
        Assert.Equal("Only-a", docs["a"]["Home"]["City"].AsString);
        Assert.Equal("Shared", docs["b"]["Home"]["City"].AsString);
        Assert.Equal("Shared", docs["b"]["job"]["City"].AsString);
    }

    // ── (8) Model caching and serializer staleness ───────────────────────────────────────────────────────────────

    public class NamesA(DbContextOptions options, string collection) : DbContext(options)
    {
        public string CollectionName => collection;
        public DbSet<Person> People { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Person>().ToCollection(collection);
            mb.Entity<Person>(e =>
            {
                e.ComplexProperty(p => p.Home, h =>
                {
                    h.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "homeA");
                    h.Property(x => x.City).Metadata.SetElementName("cityA");
                    h.ComplexProperty(x => x.Geo);
                });
                e.ComplexProperty(p => p.Work, w => w.ComplexProperty(x => x.Geo));
#if !EF8 && !EF9
                e.ComplexCollection(p => p.Past, x => x.ComplexProperty(y => y.Geo));
#endif
            });
        }
    }

    public class NamesB(DbContextOptions options, string collection) : DbContext(options)
    {
        public string CollectionName => collection;
        public DbSet<Person> People { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Person>().ToCollection(collection);
            mb.Entity<Person>(e =>
            {
                e.ComplexProperty(p => p.Home, h =>
                {
                    h.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "homeB");
                    h.Property(x => x.City).Metadata.SetElementName("cityB");
                    h.ComplexProperty(x => x.Geo);
                });
                e.ComplexProperty(p => p.Work, w => w.ComplexProperty(x => x.Geo));
#if !EF8 && !EF9
                e.ComplexCollection(p => p.Past, x => x.ComplexProperty(y => y.Geo));
#endif
            });
        }
    }

    // One options instance (one internal service provider: one BsonSerializerFactory singleton, one compiled-query
    // cache) shared by both context types, so the caches are shared and only model identity separates the two.
    private DbContextOptions SharedOptions(MongoQueryMode mode = MongoQueryMode.Native, bool keyByCollection = false)
    {
        var builder = new DbContextOptionsBuilder()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        if (keyByCollection)
        {
            builder.ReplaceService<IModelCacheKeyFactory, CollectionCacheKeyFactory>();
        }

        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        return builder.Options;
    }

    // NamesA/NamesB bake their collection name into the model; EF caches the model per context type in a service provider
    // that equal options share across tests, so a second test using them must key the model by its collection too.
    private sealed class CollectionCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
            => (context.GetType(), context switch { NamesA a => a.CollectionName, NamesB b => b.CollectionName, _ => null }, designTime);
    }

    // The serializer caches (BsonSerializerFactory._complexTypeSerializersCache / _complexPropertySerializersCache) are
    // used by the driver-LINQ path (queries and the bulk bridge), never by the native translator: so the interleaved
    // two-model test above, which runs natively, cannot see a cache keyed by something weaker than the complex type
    // (e.g. CLR type + property name). Here every query and a bulk update run on driver-LINQ, interleaved so that each
    // model's lookups find the other model's entries first.
    [Theory]
    [InlineData(MongoQueryMode.DriverLinq)]
    [InlineData(MongoQueryMode.Native)]
    public void Two_context_types_with_different_element_names_query_and_bulk_update_through_their_own_serializers(MongoQueryMode mode)
    {
        var collectionA = Collection<Person>("CacheA" + mode);
        var collectionB = Collection<Person>("CacheB" + mode);
        var options = SharedOptions(mode, keyByCollection: true);
        var nameA = collectionA.CollectionNamespace.CollectionName;
        var nameB = collectionB.CollectionNamespace.CollectionName;

        for (var round = 0; round < 2; round++)
        {
            using (var a = new NamesA(options, nameA))
            {
                a.People.Add(P("a" + round, A("Oslo", round), A("w", 0)));
                a.SaveChanges();
                Assert.Equal(Enumerable.Range(0, round + 1).Select(i => "a" + i),
                    a.People.Where(p => p.Home.City == "Oslo").OrderBy(p => p.Name).Select(p => p.Name).ToList());
            }

            using (var b = new NamesB(options, nameB))
            {
                b.People.Add(P("b" + round, A("Oslo", round), A("w", 0)));
                b.SaveChanges();
                Assert.Equal(Enumerable.Range(0, round + 1).Select(i => "b" + i),
                    b.People.Where(p => p.Home.City == "Oslo").OrderBy(p => p.Name).Select(p => p.Name).ToList());
                Assert.Equal(Enumerable.Range(0, round + 1).Select(_ => "Oslo"),
                    b.People.OrderBy(p => p.Name).Select(p => p.Home.City).ToList());
            }
        }

#if !EF8
        using (var a = new NamesA(options, nameA))
        {
            Assert.Equal(2, a.People.Where(p => p.Home.City == "Oslo").ExecuteUpdate(s => s.SetProperty(p => p.Name, p => p.Name + "!")));
        }

        using (var b = new NamesB(options, nameB))
        {
            Assert.Equal(2, b.People.Where(p => p.Home.City == "Oslo").ExecuteUpdate(s => s.SetProperty(p => p.Name, p => p.Name + "?")));
        }

        Assert.Equal(["a0!", "a1!"], Raw(collectionA).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d["Name"].AsString).Order());
        Assert.Equal(["b0?", "b1?"], Raw(collectionB).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d["Name"].AsString).Order());
#endif
        Assert.All(Raw(collectionA).Find(FilterDefinition<BsonDocument>.Empty).ToList(), d => Assert.Equal("Oslo", d["homeA"]["cityA"].AsString));
        Assert.All(Raw(collectionB).Find(FilterDefinition<BsonDocument>.Empty).ToList(), d => Assert.Equal("Oslo", d["homeB"]["cityB"].AsString));
    }

    [Fact]
    public void Two_context_types_with_different_element_names_in_one_process_each_use_their_own_names()
    {
        var collectionA = Collection<Person>("NamesA");
        var collectionB = Collection<Person>("NamesB");
        var options = SharedOptions();
        var nameA = collectionA.CollectionNamespace.CollectionName;
        var nameB = collectionB.CollectionNamespace.CollectionName;

        // Interleave model building, writes and reads so that each serializer cache is populated by the other model first.
        for (var round = 0; round < 3; round++)
        {
            using (var b = new NamesB(options, nameB))
            {
                b.People.Add(P("b" + round, A("B-city" + round, round), A("w", 0)));
                b.SaveChanges();
            }

            using (var a = new NamesA(options, nameA))
            {
                a.People.Add(P("a" + round, A("A-city" + round, round), A("w", 0)));
                a.SaveChanges();
                Assert.Equal(round + 1, a.People.Count(p => p.Home.City.StartsWith("A-city")));
            }

            using (var b = new NamesB(options, nameB))
            {
                Assert.Equal(
                    Enumerable.Range(0, round + 1).Select(i => "B-city" + i),
                    b.People.OrderBy(p => p.Name).Select(p => p.Home.City).ToList());
                Assert.Equal(round + 1, b.People.AsEnumerable().Count(p => p.Home.City.StartsWith("B-city")));
            }
        }

        var docA = Raw(collectionA).Find(FilterDefinition<BsonDocument>.Empty).First();
        var docB = Raw(collectionB).Find(FilterDefinition<BsonDocument>.Empty).First();
        Assert.Equal("A-city0", docA["homeA"]["cityA"].AsString);
        Assert.False(docA.Contains("homeB") || docA.Contains("Home"));
        Assert.Equal("B-city0", docB["homeB"]["cityB"].AsString);
        Assert.False(docB.Contains("homeA") || docB.Contains("Home"));

        // A document written under A's names, placed in B's collection, is read through B's model: B's names are absent,
        // so B's required complex property is missing (strict read), never A's values read through a stale serializer.
        Raw(collectionB).InsertOne(docA.DeepClone().AsBsonDocument.Set("_id", ObjectId.GenerateNewId()).Set("Name", "zz-from-A"));
        using (var b = new NamesB(options, nameB))
        {
            var ex = Assert.Throws<InvalidOperationException>(() => b.People.Where(p => p.Name == "zz-from-A").ToList());
            Assert.Contains("Document element 'homeB' is missing for required complex property 'Person.Home'", ex.Message);
        }
    }

    // A context type whose model differs by a constructor flag, keyed by a custom IModelCacheKeyFactory.
    public class FlaggedContext(DbContextOptions options, string collection, bool alt) : DbContext(options)
    {
        public bool Alt { get; } = alt;
        public DbSet<Person> People { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Person>().ToCollection(collection);
            mb.Entity<Person>(e =>
            {
                e.ComplexProperty(p => p.Home, h =>
                {
                    h.HasPropertyAnnotation(MongoAnnotationNames.ElementName, Alt ? "alt" : "main");
                    h.ComplexProperty(x => x.Geo);
                });
                e.ComplexProperty(p => p.Work, w => w.ComplexProperty(x => x.Geo));
#if !EF8 && !EF9
                e.ComplexCollection(p => p.Past, x => x.ComplexProperty(y => y.Geo));
#endif
            });
        }
    }

    private sealed class FlagCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
            => (context.GetType(), ((FlaggedContext)context).Alt, designTime);
    }

    // DriverLinq too: only that path reads through the complex serializer caches (see the two-context theory above).
    [Theory]
    [InlineData(MongoQueryMode.Native)]
    [InlineData(MongoQueryMode.DriverLinq)]
    public void Same_context_type_with_two_cached_models_keeps_their_element_names_apart_under_concurrency(MongoQueryMode mode)
    {
        var collection = Collection<Person>(nameof(Same_context_type_with_two_cached_models_keeps_their_element_names_apart_under_concurrency) + mode);
        var name = collection.CollectionNamespace.CollectionName;
        var builder = new DbContextOptionsBuilder()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, FlagCacheKeyFactory>()
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        var options = builder.Options;

        const int iterations = 300;
        var errors = new ConcurrentQueue<string>();
        Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            try
            {
                var alt = i % 2 == 0;
                using var db = new FlaggedContext(options, name, alt);
                var city = (alt ? "alt-" : "main-") + i;
                db.People.Add(P("p" + i, A(city, i, i), A("w" + i, -i)));
                db.SaveChanges();

                // Read back through this model; the other model's documents have no element under our name, so filter
                // on our own prefix: a cross-talking serializer would read the other name and either throw or return it.
                var mine = db.People.AsNoTracking().Where(p => p.Name == "p" + i).Select(p => new { p.Home.City, p.Home.Zip, WorkCity = p.Work.City }).ToList();
                if (mine.Count != 1 || mine[0].City != city || mine[0].Zip != i)
                {
                    errors.Enqueue($"{i}: [{string.Join(",", mine)}]");
                }

                // A captured complex comparand (member-wise equality parameter) re-evaluated per execution. Native only:
                // driver-LINQ refuses a whole complex value comparison (R1); the leaf filter below goes through the
                // serializer caches instead.
                if (mode != MongoQueryMode.DriverLinq)
                {
                    var probe = A(city, i, i);
                    var matched = db.People.AsNoTracking().Where(p => p.Home == probe).Select(p => p.Name).ToList();
                    if (matched.Count != 1 || matched[0] != "p" + i)
                    {
                        errors.Enqueue($"{i} equality: [{string.Join(",", matched)}]");
                    }
                }

                var byLeaf = db.People.AsNoTracking().Where(p => p.Home.City == city).Select(p => p.Name).ToList();
                if (byLeaf.Count != 1 || byLeaf[0] != "p" + i)
                {
                    errors.Enqueue($"{i} leaf: [{string.Join(",", byLeaf)}]");
                }
            }
            catch (Exception e) when (e is not Xunit.Sdk.XunitException)
            {
                errors.Enqueue($"{i}: {e.GetType().Name}: {e.Message}");
            }
        });

        Assert.True(errors.IsEmpty, string.Join(Environment.NewLine, errors.Take(20)));

        var docs = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList();
        Assert.Equal(iterations, docs.Count);
        Assert.All(docs, d =>
        {
            var i = int.Parse(d["Name"].AsString[1..]);
            var element = i % 2 == 0 ? "alt" : "main";
            Assert.True(d.Contains(element) && !d.Contains(i % 2 == 0 ? "main" : "alt"), d.ToJson());
            Assert.Equal(i, d[element]["Zip"].AsInt32);
        });
    }

    // ── (9) Cached query re-execution with different captured complex values ─────────────────────────────────────

    [Fact]
    public void Cached_query_re_executed_with_different_captured_complex_values_null_and_struct()
    {
        var collection = SeedPeople();
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            using var db = Context(collection, ConfigurePerson, mode);

            List<string> ByHome(Addr? value) => db.Entities.Where(p => p.Home == value).Select(p => p.Name).ToList();
            List<string> ByGeo(GeoPoint geo) => db.Entities.Where(p => p.Home.Geo.Equals(geo)).Select(p => p.Name).ToList();

            Assert.Equal(["a"], ByHome(A("Oslo", 1, 1.5)));
            Assert.Equal(["b"], ByHome(A("Rome", 3, 2.5)));
            Assert.Equal([], ByHome(null));
            Assert.Equal([], ByHome(A("Oslo", 1, 9)));
            Assert.Equal(["a"], ByHome(A("Oslo", 1, 1.5)));
            Assert.Equal(["b"], ByGeo(new GeoPoint { Lat = 2.5, Lon = -2.5 }));
            Assert.Equal(["a"], ByGeo(new GeoPoint { Lat = 1.5, Lon = -1.5 }));
            Assert.Equal([], ByGeo(default));
        }

        var compiled = EF.CompileQuery((SingleEntityDbContext<Person> db, Addr value) => db.Entities.Where(p => p.Work == value).Select(p => p.Name));
        using (var db = Context(collection, ConfigurePerson, MongoQueryMode.NativeOnly))
        {
            Assert.Equal(["a"], compiled(db, A("Rome", 2)).ToList());
            Assert.Equal(["b"], compiled(db, A("Oslo", 4)).ToList());
            Assert.Equal([], compiled(db, A("Oslo", 5)).ToList());
        }
    }

    // ── (10) SaveChanges edge cases ──────────────────────────────────────────────────────────────────────────────

#if !EF8 && !EF9
    [Fact]
    public void Deleting_an_entity_with_a_complex_collection_removes_the_document()
    {
        var collection = SeedPeople();
        using (var db = Context(collection, ConfigurePerson))
        {
            var a = db.Entities.Single(p => p.Name == "a");
            Assert.Single(a.Past);
            db.Entities.Remove(a);
            Assert.Equal(1, db.SaveChanges());
        }

        Assert.Equal(["b"], Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d["Name"].AsString));
    }
#endif

    public class Mixed
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Addr Home { get; set; } = null!;
    }

    public class Plain
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class MixedContext(DbContextOptions options, string mixed, string plain) : DbContext(options)
    {
        public DbSet<Mixed> Mixed { get; set; } = null!;
        public DbSet<Plain> Plain { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Mixed>().ToCollection(mixed).ComplexProperty(m => m.Home, h => h.ComplexProperty(x => x.Geo));
            mb.Entity<Plain>().ToCollection(plain);
        }
    }

    [Fact]
    public void One_SaveChanges_mixing_entities_with_and_without_complex_data_and_a_thousand_nested_values()
    {
        var mixed = Collection<Mixed>();
        var plain = Collection<Plain>();
        var options = SharedOptions();

        using (var db = new MixedContext(options, mixed.CollectionNamespace.CollectionName, plain.CollectionNamespace.CollectionName))
        {
            db.Mixed.AddRange(Enumerable.Range(0, 1000).Select(i => new Mixed { Name = "m" + i, Home = A("c" + i, i, i / 10.0) }));
            db.Plain.AddRange(Enumerable.Range(0, 10).Select(i => new Plain { Name = "p" + i }));
            Assert.Equal(1010, db.SaveChanges());
        }

        Assert.Equal(1000, Raw(mixed).CountDocuments(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(10, Raw(plain).CountDocuments(FilterDefinition<BsonDocument>.Empty));
        Assert.All(Raw(plain).Find(FilterDefinition<BsonDocument>.Empty).ToList(), d => Assert.Equal(["_id", "Name"], d.Names.ToArray()));

        using (var db = new MixedContext(options, mixed.CollectionNamespace.CollectionName, plain.CollectionNamespace.CollectionName))
        {
            var all = db.Mixed.AsNoTracking().ToList();
            Assert.Equal(1000, all.Count);
            Assert.All(all, m =>
            {
                var i = int.Parse(m.Name[1..]);
                Assert.Equal(("c" + i, i, i / 10.0), (m.Home.City, m.Home.Zip, m.Home.Geo.Lat));
            });
            Assert.Equal(500, db.Mixed.Count(m => m.Home.Zip >= 500));
            Assert.Equal(99.9, db.Mixed.Max(m => m.Home.Geo.Lat));
        }
    }

    [Fact]
    public void Update_after_reload_in_a_different_context_rewrites_the_subdocument_from_the_new_snapshot()
    {
        var collection = SeedPeople();

        // Context 1 loads; context 2 changes a sibling leaf and saves; context 1 then changes another leaf of the same
        // complex property and saves: whole-subdocument rewrite (ruling R5) is last-writer-wins, documented.
        using var first = Context(collection, ConfigurePerson);
        var stale = first.Entities.Single(p => p.Name == "a");

        using (var second = Context(collection, ConfigurePerson))
        {
            var fresh = second.Entities.Single(p => p.Name == "a");
            fresh.Home.Zip = 50;
            second.SaveChanges();
        }

        stale.Home.City = "Bergen";
        first.SaveChanges();

        var doc = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "a")).Single();
        Assert.Equal("Bergen", doc["Home"]["City"].AsString);
        Assert.Equal(1, doc["Home"]["Zip"].AsInt32); // the other context's Zip = 50 is overwritten (last writer wins)

        // A context that reloads after the other's save sees and keeps both.
        using (var third = Context(collection, ConfigurePerson))
        {
            var reloaded = third.Entities.Single(p => p.Name == "a");
            reloaded.Home.Zip = 77;
            third.SaveChanges();
        }

        doc = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "a")).Single();
        Assert.Equal(("Bergen", 77), (doc["Home"]["City"].AsString, doc["Home"]["Zip"].AsInt32));
    }

    [Theory]
    [InlineData(AutoTransactionBehavior.WhenNeeded)]
    [InlineData(AutoTransactionBehavior.Never)]
    public void Failed_SaveChanges_leaves_no_partial_complex_data_inside_a_transaction(AutoTransactionBehavior behavior)
    {
        var collection = SeedPeople(nameof(Failed_SaveChanges_leaves_no_partial_complex_data_inside_a_transaction) + behavior);
        var before = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().OrderBy(d => d["Name"].AsString).ToList();
        var duplicateId = before[0]["_id"].AsObjectId;

        using (var db = Context(collection, ConfigurePerson))
        {
            db.Database.AutoTransactionBehavior = behavior;
            var b = db.Entities.Single(p => p.Name == "b");
            b.Home.City = "Changed";
            db.Entities.Add(P("new", A("New", 9), A("w", 9)));
            db.Entities.Add(new Person { Id = duplicateId, Name = "dup", Home = A("Dup", 0), Work = A("Dup", 0) });
            Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => db.SaveChanges());
        }

        var after = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().OrderBy(d => d["Name"].AsString).ToList();
        if (behavior == AutoTransactionBehavior.WhenNeeded)
        {
            // Rolled back: no new document, b's subdocument unchanged.
            Assert.Equal(before, after);
        }
        else
        {
            // Without a transaction the writes before the failing one stay (measured, documented EF behaviour); every
            // stored complex value is still whole (never a partial subdocument).
            Assert.Contains(after, d => d["Name"] == "new" && d["Home"]["City"] == "New");
            Assert.All(after, d => Assert.Equal(["City", "Geo", "Zip"], d["Home"].AsBsonDocument.Names.OrderBy(n => n, StringComparer.Ordinal)));
        }
    }

    // ── Part 2(b), Task 11 minor: a composite-key component as a filtered-Include sort key ───────────────────────

    public class Owner
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Line> Lines { get; set; } = [];
    }

    public class Line
    {
        public int Batch { get; set; }
        public int Seq { get; set; }
        public ObjectId OwnerId { get; set; }
        public string Label { get; set; } = null!;
    }

    public class LinesContext(DbContextOptions options, string owners, string lines) : DbContext(options)
    {
        public DbSet<Owner> Owners { get; set; } = null!;
        public DbSet<Line> Lines { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Owner>().ToCollection(owners);
            mb.Entity<Line>(e =>
            {
                e.ToCollection(lines);
                e.HasKey(l => new { l.Batch, l.Seq });
            });
            mb.Entity<Owner>().HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OwnerId);
        }
    }

    [Fact]
    public void Filtered_Include_sorted_by_a_composite_key_component_sorts_on_the_id_subfield()
    {
        // A composite key is stored as `_id: { Batch, Seq }`; the filtered-Include $sort must address `_id.Seq`, not a
        // root `Seq` (absent: every row would tie and come back in arbitrary order).
        var owners = Collection<Owner>("CompositeOwners");
        var lines = Collection<Line>("CompositeLines");
        var ownerId = ObjectId.GenerateNewId();
        Raw(owners).InsertOne(new BsonDocument { { "_id", ownerId }, { "Name", "o" } });
        Raw(lines).InsertMany(new[] { (1, 3, "c"), (2, 1, "a"), (1, 2, "b"), (2, 4, "d") }.Select(x =>
            new BsonDocument { { "_id", new BsonDocument { { "Batch", x.Item1 }, { "Seq", x.Item2 } } }, { "OwnerId", ownerId }, { "Label", x.Item3 } }));

        using var capture = new CommandCapture(database);
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            var options = new DbContextOptionsBuilder()
                .UseMongoDB(capture.Collection(owners).Database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(options).UseQueryMode(mode);
            using var db = new LinesContext(options.Options, owners.CollectionNamespace.CollectionName, lines.CollectionNamespace.CollectionName);

            capture.Clear();
            Assert.Equal(["a,b,c,d"], db.Owners.AsNoTracking().Include(o => o.Lines.OrderBy(l => l.Seq)).ToList().Select(o => string.Join(",", o.Lines.Select(l => l.Label))));
            Assert.Equal(["d,c,b,a"], db.Owners.AsNoTracking().Include(o => o.Lines.OrderByDescending(l => l.Seq)).ToList().Select(o => string.Join(",", o.Lines.Select(l => l.Label))));
            Assert.Equal(["c,b,d,a"], db.Owners.AsNoTracking().Include(o => o.Lines.OrderBy(l => l.Batch).ThenByDescending(l => l.Seq)).ToList()
                .Select(o => string.Join(",", o.Lines.Select(l => l.Label))));

            var pipeline = capture.Named("aggregate").First()["pipeline"].ToJson();
            Assert.Contains("\"$sort\" : { \"_id.Seq\" : 1 }", pipeline);
        }
    }
}
