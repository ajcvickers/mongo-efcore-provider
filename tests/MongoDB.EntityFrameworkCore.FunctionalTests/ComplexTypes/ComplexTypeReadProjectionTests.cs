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
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#if !EF8 && !EF9
/// <summary>
/// Two read-side rules for projected complex values: a REQUIRED complex collection stored missing or null reads empty
/// wherever it is reached from (the root, a navigation, an inner join side), and a conditional that chooses between two
/// whole complex values returns the chosen branch for every row. Answers are hand-written; NativeOnly may decline a shape
/// (a clean refusal) but never returns different rows.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeReadProjectionTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private static readonly MongoQueryMode[] AllModes = [MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq];

    public class RStop
    {
        public string City { get; set; } = null!;
        public int Mins { get; set; }
    }

    public class RRec
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<RStop> Stops { get; set; } = [];
        public RStop Home { get; set; } = null!;
        public RStop Other { get; set; } = null!;
    }

    public class RKid
    {
        public ObjectId Id { get; set; }
        public string Tag { get; set; } = null!;
        public ObjectId RecId { get; set; }
        public RRec? Rec { get; set; }
    }

    public sealed class RContext(DbContextOptions options, string recs, string kids) : DbContext(options)
    {
        public DbSet<RRec> Recs => Set<RRec>();
        public DbSet<RKid> Kids => Set<RKid>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<RRec>(b =>
            {
                b.ToCollection(recs);
                b.ComplexCollection(r => r.Stops);
                b.ComplexProperty(r => r.Home);
                b.ComplexProperty(r => r.Other);
            });
            mb.Entity<RKid>(b =>
            {
                b.ToCollection(kids);
                b.HasOne(k => k.Rec).WithMany().HasForeignKey(k => k.RecId);
            });
        }
    }

    private static DbContextOptions Options(TemporaryDatabaseFixture database, MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        var builder = new DbContextOptionsBuilder<RContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .UseQueryTrackingBehavior(tracking)
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        return builder.Options;
    }

    private static BsonDocument Stop(string city, int mins) => new() { { "City", city }, { "Mins", mins } };

    private static BsonDocument Rec(ObjectId id, string name, BsonValue? stops)
    {
        var document = new BsonDocument
        {
            { "_id", id }, { "Name", name }, { "Home", Stop("H" + name, 1) }, { "Other", Stop("O" + name, 2) }
        };
        if (stops is not null)
        {
            document["Stops"] = stops;
        }

        return document;
    }

    /// <summary>Four recs (Stops: one element, missing, BSON null, empty) and a kid pointing at each.</summary>
    private (string Recs, string Kids) SeedRecs(string name)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var recs = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + suffix + "_r";
        var kids = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + suffix + "_k";
        var ids = Enumerable.Range(0, 4).Select(_ => ObjectId.GenerateNewId()).ToArray();
        database.MongoDatabase.GetCollection<BsonDocument>(recs).InsertMany(
        [
            Rec(ids[0], "a1", new BsonArray { Stop("Oslo", 5) }),
            Rec(ids[1], "a2", null),
            Rec(ids[2], "a3", BsonNull.Value),
            Rec(ids[3], "a4", new BsonArray())
        ]);
        database.MongoDatabase.GetCollection<BsonDocument>(kids).InsertMany(
            ids.Select((id, i) => new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Tag", "k" + (i + 1) }, { "RecId", id } }));
        return (recs, kids);
    }

    private static string Show(List<RStop>? stops) => stops is null ? "null" : stops.Count.ToString();

    /// <summary>NativeOnly returns the answer or declines cleanly; Native and DriverLinq return the answer.</summary>
    private static void AssertRows(Func<MongoQueryMode, QueryTrackingBehavior, List<string>> run, params string[] expected)
    {
        foreach (var mode in AllModes)
        {
            foreach (var tracking in new[] { QueryTrackingBehavior.NoTracking, QueryTrackingBehavior.TrackAll })
            {
                List<string> rows;
                try
                {
                    rows = run(mode, tracking);
                }
                catch (NativeTranslationNotSupportedException) when (mode == MongoQueryMode.NativeOnly)
                {
                    continue;
                }

                Assert.True(
                    expected.SequenceEqual(rows),
                    $"{mode}/{tracking}: expected [{string.Join("; ", expected)}], got [{string.Join("; ", rows)}]");
            }
        }
    }

    /// <summary>Every mode refuses: the fallback cannot read the chosen value back either, so there are no rows to compare.</summary>
    private static void AssertConditionalRefused(Func<MongoQueryMode, QueryTrackingBehavior, List<string>> run)
    {
        foreach (var mode in AllModes)
        {
            foreach (var tracking in new[] { QueryTrackingBehavior.NoTracking, QueryTrackingBehavior.TrackAll })
            {
                var ex = Assert.Throws<NotSupportedException>(() => run(mode, tracking));
                Assert.Contains("chooses between two whole complex values", ex.Message);
            }
        }
    }

    [Fact]
    public void Required_complex_collection_stored_missing_or_null_reads_empty_from_the_root()
    {
        var c = SeedRecs(nameof(Required_complex_collection_stored_missing_or_null_reads_empty_from_the_root));
        AssertRows((mode, tracking) =>
        {
            using var db = new RContext(Options(database, mode, tracking), c.Recs, c.Kids);
            return [.. db.Recs.OrderBy(r => r.Name).Select(r => r.Stops).ToList().Select(Show)];
        }, "1", "0", "0", "0");
    }

    [Fact]
    public void Required_complex_collection_stored_missing_or_null_reads_empty_through_a_navigation()
    {
        // The navigation target EXISTS: a member missing from a present document is ordinary data and reads empty, as on the root.
        var c = SeedRecs(nameof(Required_complex_collection_stored_missing_or_null_reads_empty_through_a_navigation));
        AssertRows((mode, tracking) =>
        {
            using var db = new RContext(Options(database, mode, tracking), c.Recs, c.Kids);
            return [.. db.Kids.OrderBy(k => k.Tag).Select(k => k.Rec!.Stops).ToList().Select(Show)];
        }, "1", "0", "0", "0");
    }

    [Fact]
    public void Required_complex_collection_stored_missing_or_null_reads_empty_from_an_inner_join_side()
    {
        var c = SeedRecs(nameof(Required_complex_collection_stored_missing_or_null_reads_empty_from_an_inner_join_side));
        AssertRows((mode, tracking) =>
        {
            using var db = new RContext(Options(database, mode, tracking), c.Recs, c.Kids);
            return [.. db.Kids.Join(db.Recs, k => k.RecId, r => r.Id, (k, r) => new { k.Tag, r.Stops })
                .OrderBy(x => x.Tag).ToList().Select(x => $"{x.Tag}:{Show(x.Stops)}")];
        }, "k1:1", "k2:0", "k3:0", "k4:0");
    }

    [Fact]
    public void Conditional_between_two_whole_complex_values_is_refused_in_every_mode()
    {
        // Used to fail with "Cannot deserialize a 'Boolean' from BsonType 'Document'" (the staged test collided with the value).
        var c = SeedRecs(nameof(Conditional_between_two_whole_complex_values_is_refused_in_every_mode));
        AssertConditionalRefused((mode, tracking) =>
        {
            using var db = new RContext(Options(database, mode, tracking), c.Recs, c.Kids);
            return [.. db.Recs.OrderBy(r => r.Name).Select(r => r.Name == "a1" || r.Name == "a3" ? r.Home : r.Other)
                .ToList().Select(s => s.City)];
        });

        // A conditional over leaves, and a coalesce of complex values, are served: choose per property or with ??.
        AssertRows((mode, tracking) =>
        {
            using var db = new RContext(Options(database, mode, tracking), c.Recs, c.Kids);
            return [.. db.Recs.OrderBy(r => r.Name).Select(r => r.Name == "a1" || r.Name == "a3" ? r.Home.City : r.Other.City).ToList()];
        }, "Ha1", "Oa2", "Ha3", "Oa4");
    }

    // ── A conditional over complex values of a TPH hierarchy ────────────────────────────────────────────────────

    public class TInner
    {
        public int N { get; set; }
    }

    public class TAnimal
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public TInner Base { get; set; } = null!;
    }

    public class TDog : TAnimal
    {
        public TInner Kennel { get; set; } = null!;
    }

    public class TCat : TAnimal;

    private static void ConfigureAnimals(ModelBuilder mb)
    {
        mb.Entity<TAnimal>().ComplexProperty(a => a.Base);
        mb.Entity<TDog>().ComplexProperty(d => d.Kennel);
        mb.Entity<TCat>();
    }

    [Fact]
    public void Conditional_between_complex_values_of_a_derived_and_a_base_type_is_refused_not_answered_with_one_branch()
    {
        // Used to return the base value for every row ([2, 1, 4]) in Native and DriverLinq.
        var collection = database.CreateCollection<TAnimal>(
            TemporaryDatabaseFixtureBase.CreateCollectionName(nameof(Conditional_between_complex_values_of_a_derived_and_a_base_type_is_refused_not_answered_with_one_branch))
            + Guid.NewGuid().ToString("N")[..6]);
        using (var db = SingleEntityDbContext.Create(collection, ConfigureAnimals))
        {
            db.Entities.Add(new TDog { Name = "d1", Base = new TInner { N = 1 }, Kennel = new TInner { N = 31 } });
            db.Entities.Add(new TDog { Name = "d2", Base = new TInner { N = 4 }, Kennel = new TInner { N = 51 } });
            db.Entities.Add(new TCat { Name = "c1", Base = new TInner { N = 2 } });
            db.SaveChanges();
        }

        AssertConditionalRefused((mode, tracking) =>
        {
            using var db = SingleEntityDbContext.Create(collection, ConfigureAnimals, null, b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                b.UseQueryTrackingBehavior(tracking);
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });
            return [.. db.Entities.OrderBy(a => a.Name).Select(a => a is TDog ? ((TDog)a).Kennel : a.Base).ToList().Select(i => i.N.ToString())];
        });
    }
}
#endif
