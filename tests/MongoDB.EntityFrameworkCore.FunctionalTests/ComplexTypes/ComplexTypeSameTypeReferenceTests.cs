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
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// A whole complex value read off an entity of the SAME CLR type as the query root, but not the root row: through a
/// self-referencing reference navigation (<c>p.Referrer!.Badge</c>) or from the INNER side of a self-join. The read side
/// recognised the root shaper by its CLR type (<c>shaperEntityType == _rootEntityType</c>), so in a join query the joined
/// row's value was read off the OUTER document (<c>B-dev ; B-guest</c> instead of <c>B-boss ; B-boss</c>). The arm is now
/// structural (outside a join, or the root's own projection binding). Every answer is hand-written.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeSameTypeReferenceTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Badge
    {
        public string Code { get; set; } = null!;
    }

    public class Owned
    {
        public string Tag { get; set; } = null!;
    }

    public class Person
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public ObjectId? ReferrerId { get; set; }
        public Person? Referrer { get; set; }
        public List<Person> Referrals { get; set; } = [];
        public Badge Badge { get; set; } = null!;
        public GeoPoint Desk { get; set; }
        public Owned Card { get; set; } = null!;
#if !EF8 && !EF9
        public Badge? Spare { get; set; }
#endif
    }

    public class Employee : Person
    {
        public int Level { get; set; }
    }

    private sealed class PeopleContext(DbContextOptions options, string collection, bool withOwned) : DbContext(options)
    {
        public DbSet<Person> People => Set<Person>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Employee>().HasBaseType<Person>();
            mb.Entity<Person>(b =>
            {
                b.ToCollection(collection);
                b.HasDiscriminator<string>("_t").HasValue<Person>("P").HasValue<Employee>("E");
                b.HasOne(p => p.Referrer).WithMany(p => p.Referrals).HasForeignKey(p => p.ReferrerId);
                b.ComplexProperty(p => p.Badge);
                b.ComplexProperty(p => p.Desk);
                // EF8/EF9 send complex projections of an entity with an owned navigation to the fallback (ruling R24), so the
                // owned Card is mapped only for the tests that read it.
                if (withOwned)
                {
                    b.OwnsOne(p => p.Card);
                }
                else
                {
                    b.Ignore(p => p.Card);
                }
#if !EF8 && !EF9
                b.ComplexProperty(p => p.Spare);
#endif
            });
        }
    }

    private static readonly ObjectId BossId = ObjectId.GenerateNewId();

    // Boss (E; Badge B-boss, Desk 9, Card c-boss, Spare S-boss); Dev (E; referrer Boss; B-dev, 2, c-dev, Spare null);
    // Guest (P; referrer Boss; B-guest, 1, c-guest, Spare missing).
    private Func<MongoQueryMode, QueryTrackingBehavior, List<string>> Seed(Func<PeopleContext, IEnumerable<string>> query,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        => Seed(query, withOwned: false, name);

    private Func<MongoQueryMode, QueryTrackingBehavior, List<string>> SeedOwned(Func<PeopleContext, IEnumerable<string>> query,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        => Seed(query, withOwned: true, name);

    private Func<MongoQueryMode, QueryTrackingBehavior, List<string>> Seed(Func<PeopleContext, IEnumerable<string>> query, bool withOwned, string name)
    {
        var collection = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        static BsonDocument Desk(double lat) => new() { { "Lat", lat }, { "Lon", lat + 0.5 } };
        database.MongoDatabase.GetCollection<BsonDocument>(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", BossId }, { "_t", "E" }, { "Name", "Boss" }, { "Level", 9 }, { "ReferrerId", BsonNull.Value },
                { "Badge", new BsonDocument("Code", "B-boss") }, { "Desk", Desk(9) }, { "Card", new BsonDocument("Tag", "c-boss") },
                { "Spare", new BsonDocument("Code", "S-boss") }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "_t", "E" }, { "Name", "Dev" }, { "Level", 2 }, { "ReferrerId", BossId },
                { "Badge", new BsonDocument("Code", "B-dev") }, { "Desk", Desk(2) }, { "Card", new BsonDocument("Tag", "c-dev") },
                { "Spare", BsonNull.Value }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "_t", "P" }, { "Name", "Guest" }, { "ReferrerId", BossId },
                { "Badge", new BsonDocument("Code", "B-guest") }, { "Desk", Desk(1) }, { "Card", new BsonDocument("Tag", "c-guest") }
            }
        ]);
        return (mode, tracking) =>
        {
            var builder = new DbContextOptionsBuilder<PeopleContext>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .UseQueryTrackingBehavior(tracking)
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
            using var db = new PeopleContext(builder.Options, collection, withOwned);
            return query(db).ToList();
        };
    }

    private static Func<MongoQueryMode, List<string>> NoTracking(Func<MongoQueryMode, QueryTrackingBehavior, List<string>> run)
        => mode => run(mode, QueryTrackingBehavior.NoTracking);

    private static Func<MongoQueryMode, List<string>> Tracking(Func<MongoQueryMode, QueryTrackingBehavior, List<string>> run)
        => mode => run(mode, QueryTrackingBehavior.TrackAll);

    private static IQueryable<Person> Referred(PeopleContext db) => db.People.Where(p => p.ReferrerId != null).OrderBy(p => p.Name);

    // ── Through a same-type reference navigation ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Whole_complex_value_through_a_same_type_reference_navigation()
    {
        // RED (5547cfa2): `B-dev ; B-guest` (the outer row's Badge) in Native and DriverLinq.
        var run = Seed(db => [.. Referred(db).Select(p => p.Referrer!.Badge).ToList().Select(b => b.Code)]);
        Declines(NoTracking(run), "B-boss", "B-boss");
        Declines(Tracking(run), "B-boss", "B-boss");
    }

    [Fact]
    public void Whole_complex_value_through_a_same_type_reference_beside_root_members()
    {
        var run = Seed(db => [.. Referred(db).Select(p => new { p.Name, p.Referrer!.Badge }).ToList().Select(x => $"{x.Name}|{x.Badge.Code}")]);
        Declines(NoTracking(run), "Dev|B-boss", "Guest|B-boss");
        Declines(Tracking(run), "Dev|B-boss", "Guest|B-boss");
        // The EF.Property spelling.
        Declines(NoTracking(Seed(db => [.. Referred(db).Select(p => new { p.Name, B = EF.Property<Badge>(p.Referrer!, nameof(Person.Badge)) })
            .ToList().Select(x => $"{x.Name}|{x.B.Code}")])), "Dev|B-boss", "Guest|B-boss");
        // The root's own value beside the referrer's (both from the right document).
        Declines(NoTracking(Seed(db => [.. Referred(db).Select(p => new { Mine = p.Badge, Theirs = p.Referrer!.Badge })
            .ToList().Select(x => $"{x.Mine.Code}|{x.Theirs.Code}")])), "B-dev|B-boss", "B-guest|B-boss");
    }

    [Fact]
    public void Struct_complex_value_through_a_same_type_reference_navigation()
        => Declines(NoTracking(Seed(db => [.. Referred(db).Select(p => new { p.Name, p.Referrer!.Desk }).ToList().Select(x => $"{x.Name}|{x.Desk.Lat}")])),
            "Dev|9", "Guest|9");

#if !EF8 && !EF9
    [Fact]
    public void Optional_complex_value_through_a_same_type_reference_navigation()
        // Boss's Spare is S-boss; the outer rows' Spare is null / missing, which a wrong read shows as <null>.
        => Declines(NoTracking(Seed(db => [.. Referred(db).Select(p => new { p.Name, p.Referrer!.Spare }).ToList()
                .Select(x => $"{x.Name}|{x.Spare?.Code ?? "<null>"}")])),
            "Dev|S-boss", "Guest|S-boss");
#endif

    [Fact]
    public void Leaf_through_a_same_type_reference_navigation_was_already_correct()
        // Controls (correct before the fix): a complex leaf and an owned leaf through the same navigation.
    {
        Declines(NoTracking(Seed(db => [.. Referred(db).Select(p => new { p.Name, p.Referrer!.Badge.Code }).ToList().Select(x => $"{x.Name}|{x.Code}")])),
            "Dev|B-boss", "Guest|B-boss");
        Declines(NoTracking(SeedOwned(db => [.. Referred(db).Select(p => new { p.Name, p.Referrer!.Card.Tag }).ToList().Select(x => $"{x.Name}|{x.Tag}")])),
            "Dev|c-boss", "Guest|c-boss");
    }

    [Fact]
    public void Owned_whole_value_through_a_same_type_reference_navigation()
        // The owned analogue of the whole-value read (an owned reference navigation's target, not a complex value).
        => Declines(NoTracking(SeedOwned(db => [.. Referred(db).Select(p => new { p.Name, p.Referrer!.Card }).ToList().Select(x => $"{x.Name}|{x.Card.Tag}")])),
            "Dev|c-boss", "Guest|c-boss");

    // ── The inner side of a same-type self-join ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Whole_complex_value_of_the_inner_side_of_a_self_join()
    {
        // RED (5547cfa2): `Dev|B-dev ; Guest|B-guest`.
        var run = Seed(db => [.. db.People.Join(db.People, p => p.ReferrerId, e => (ObjectId?)e.Id, (p, e) => new { p.Name, e.Badge })
            .OrderBy(x => x.Name).ToList().Select(x => $"{x.Name}|{x.Badge.Code}")]);
        Declines(NoTracking(run), "Dev|B-boss", "Guest|B-boss");
        Declines(Tracking(run), "Dev|B-boss", "Guest|B-boss");
        Declines(NoTracking(Seed(db => [.. db.People.Join(db.People, p => p.ReferrerId, e => (ObjectId?)e.Id, (p, e) => e.Badge)
            .ToList().Select(b => b.Code).Order()])), "B-boss", "B-boss");
        Declines(NoTracking(Seed(db => [.. db.People.Join(db.People, p => p.ReferrerId, e => (ObjectId?)e.Id, (p, e) => new { p.Name, e.Desk })
            .OrderBy(x => x.Name).ToList().Select(x => $"{x.Name}|{x.Desk.Lat}")])), "Dev|9", "Guest|9");
        // The outer side's own value in the same join (control: read off the root document).
        Declines(NoTracking(Seed(db => [.. db.People.Join(db.People, p => p.ReferrerId, e => (ObjectId?)e.Id, (p, e) => new { p.Name, p.Badge })
            .OrderBy(x => x.Name).ToList().Select(x => $"{x.Name}|{x.Badge.Code}")])), "Dev|B-dev", "Guest|B-guest");
    }

    [Fact]
    public void Self_join_mixed_projection_of_the_whole_outer_entity_and_inner_leaves()
    {
        // The flipped pin (ComplexTypeDerivedShaperJoinTests.Explicit_self_join_mixed_projection_of_inner_members_reads_the_inner_document)
        // with an OWNED inner leaf and a root-scalar inner leaf too (exception (h)). RED (5547cfa2): `Dev|B-dev ; Guest|B-guest`.
        var run = Seed(db => [.. db.People.Join(db.People, p => p.ReferrerId, e => (ObjectId?)e.Id, (p, e) => new { p, e })
            .OrderBy(x => x.p.Name).Select(x => new { x.p, x.e.Badge.Code }).ToList().Select(x => x.p.Name + "|" + x.Code)]);
        Declines(NoTracking(run), "Dev|B-boss", "Guest|B-boss");
        Declines(NoTracking(SeedOwned(db => [.. db.People.Join(db.People, p => p.ReferrerId, e => (ObjectId?)e.Id, (p, e) => new { p, e })
            .OrderBy(x => x.p.Name).Select(x => new { x.p, x.e.Card.Tag }).ToList().Select(x => x.p.Name + "|" + x.Tag)])),
            "Dev|c-boss", "Guest|c-boss");
        // Served natively (no owned navigation in this model), and on the fallback through the fixed arm.
        Native(NoTracking(Seed(db => [.. db.People.Join(db.People, p => p.ReferrerId, e => (ObjectId?)e.Id, (p, e) => new { p, e })
            .OrderBy(x => x.p.Name).Select(x => new { x.p, x.e.Name }).ToList().Select(x => x.p.Name + "|" + x.Name)])),
            "Dev|Boss", "Guest|Boss");
        // NON-complex (exception (h)): at 5547cfa2 this answered `Dev|Dev|c-dev ; Guest|Guest|c-guest`.
        Declines(NoTracking(SeedOwned(db => [.. db.People.Join(db.People, p => p.ReferrerId, e => (ObjectId?)e.Id, (p, e) => new { p, e })
            .OrderBy(x => x.p.Name).Select(x => new { x.p, E = x.e.Name, x.e.Card.Tag }).ToList().Select(x => x.p.Name + "|" + x.E + "|" + x.Tag)])),
            "Dev|Boss|c-boss", "Guest|Boss|c-boss");
    }

    // ── The other root checks of the read side, probed with the same model ───────────────────────────────────────

    [Fact]
    public void Root_scalar_of_a_same_type_reference_beside_the_whole_root_entity()
    {
        // The mixed reader's `_inner` arm (`shaper.StructuralType == _rootEntityType` declines it) and the field-access arm
        // agree: the referrer's Name and Badge leaf are read off the joined document. NON-complex `R` (exception (h)): at
        // 5547cfa2 `new { p, R = p.Referrer!.Name }` answered the outer Name (`Dev|Dev ; Guest|Guest`).
        Declines(NoTracking(Seed(db => [.. Referred(db).Select(p => new { p, R = p.Referrer!.Name, C = p.Referrer.Badge.Code }).ToList()
            .Select(x => $"{x.p.Name}|{x.p.Badge.Code}|{x.R}|{x.C}")])), "Dev|B-dev|Boss|B-boss", "Guest|B-guest|Boss|B-boss");
        Declines(NoTracking(Seed(db => [.. Referred(db).Select(p => new { p, p.Referrer!.Badge }).ToList()
            .Select(x => $"{x.p.Name}|{x.p.Badge.Code}|{x.Badge.Code}")])), "Dev|B-dev|B-boss", "Guest|B-guest|B-boss");
    }

    [Fact]
    public void Include_of_same_type_navigations_reads_each_document()
    {
        // A collection Include of the same type over this model (it has an OWNED reference) fails loudly in every mode:
        // PRE-EXISTING and not complex-specific (reproduced with no complex property: "variable 'bsonArray3Object' ...
        // referenced from scope '', but it is not defined"); listed for the owner, pinned so a fix is noticed.
        foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Tracking(SeedOwned(db => [.. db.People.Include(p => p.Referrals).ToList()
                .Select(p => p.Name)], "IncludeReferrals" + mode))(mode));
            Assert.Contains("referenced from scope", ex.Message);
        }

        // Without the owned reference the same Include reads each document.
        Native(Tracking(Seed(db => [.. db.People.Include(p => p.Referrals).OrderBy(p => p.Name).ToList()
                .Select(p => $"{p.Name}|{p.Badge.Code}|{string.Join(",", p.Referrals.Select(r => r.Name + ":" + r.Badge.Code).Order())}")])),
            "Boss|B-boss|Dev:B-dev,Guest:B-guest", "Dev|B-dev|", "Guest|B-guest|");
        Native(NoTracking(Seed(db => [.. db.People.Include(p => p.Referrer).Where(p => p.ReferrerId != null).OrderBy(p => p.Name).ToList()
                .Select(p => $"{p.Name}|{p.Badge.Code}|{p.Referrer!.Name}:{p.Referrer.Badge.Code}:{p.Referrer.Desk.Lat}")])),
            "Dev|B-dev|Boss:B-boss:9", "Guest|B-guest|Boss:B-boss:9");
    }
}
