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

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// A shaper of a type DERIVED from the query root (a TPH subtype) that belongs to a JOINED row must read the joined
/// sub-document, never the outer root document, even though the derived type is in the root's hierarchy. Every inner row
/// carries values that differ from the outer row's, so reading the wrong document shows.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeDerivedShaperJoinTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Person
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public ObjectId? ReferrerId { get; set; }
        public Employee? Referrer { get; set; }
        public Badge Badge { get; set; } = null!;
    }

    public class Employee : Person
    {
        public int Level { get; set; }
        public GeoPoint Desk { get; set; }
    }

    public class Badge
    {
        public string Code { get; set; } = null!;
    }

    private sealed class PeopleContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<Person> People => Set<Person>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Employee>().HasBaseType<Person>();
            mb.Entity<Person>(b =>
            {
                b.ToCollection(collection);
                b.HasDiscriminator<string>("_t").HasValue<Person>("P").HasValue<Employee>("E");
                b.HasOne(p => p.Referrer).WithMany().HasForeignKey(p => p.ReferrerId);
                b.ComplexProperty(p => p.Badge);
            });
            mb.Entity<Employee>().ComplexProperty(e => e.Desk);
        }
    }

    private static readonly ObjectId BossId = ObjectId.GenerateNewId();

    private string Seed(string name)
    {
        var collectionName = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        database.MongoDatabase.GetCollection<BsonDocument>(collectionName).InsertMany(
        [
            new BsonDocument
            {
                { "_id", BossId }, { "_t", "E" }, { "Name", "Boss" }, { "Level", 9 }, { "ReferrerId", BsonNull.Value },
                { "Badge", new BsonDocument("Code", "B-boss") }, { "Desk", new BsonDocument { { "Lat", 9.0 }, { "Lon", 9.5 } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "_t", "E" }, { "Name", "Dev" }, { "Level", 2 }, { "ReferrerId", BossId },
                { "Badge", new BsonDocument("Code", "B-dev") }, { "Desk", new BsonDocument { { "Lat", 2.0 }, { "Lon", 2.5 } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "_t", "P" }, { "Name", "Guest" }, { "ReferrerId", BossId },
                { "Badge", new BsonDocument("Code", "B-guest") }
            }
        ]);
        return collectionName;
    }

    private PeopleContext Create(string collection, MongoQueryMode mode)
    {
        var builder = new DbContextOptionsBuilder<PeopleContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        return new PeopleContext(builder.Options, collection);
    }

    private List<string> Outcome(string collection, MongoQueryMode mode, Func<PeopleContext, IEnumerable<string>> query)
    {
        using var db = Create(collection, mode);
        return query(db).ToList();
    }

    private const string Serves = "serves";

    // Pins EACH mode: either it serves the hand-written rows (`Serves`), or it throws an exception whose message contains
    // the given fragment. A mode that starts throwing where it served, or serving where it threw, fails the test; a
    // wrong-document read is a value difference in a serving mode.
    private void AssertPerMode(
        string collection, Func<PeopleContext, IEnumerable<string>> query, string[] expected,
        string nativeOnly, string native, string driverLinq)
    {
        foreach (var (mode, want) in new[] { (MongoQueryMode.NativeOnly, nativeOnly), (MongoQueryMode.Native, native), (MongoQueryMode.DriverLinq, driverLinq) })
        {
            List<string>? rows = null;
            Exception? error = null;
            try
            {
                rows = Outcome(collection, mode, query);
            }
            catch (Exception e) when (e is not Xunit.Sdk.XunitException)
            {
                error = e;
            }

            if (want == Serves)
            {
                Assert.True(error == null, $"{mode}: expected rows, got {error}");
                Assert.True(expected.SequenceEqual(rows!), $"{mode}: expected [{string.Join("; ", expected)}], got [{string.Join("; ", rows!)}]");
            }
            else if (want == NotNativeAny)
            {
                Assert.True(error is MongoDB.EntityFrameworkCore.Query.NativeTranslation.NativeTranslationNotSupportedException,
                    $"{mode}: expected NativeTranslationNotSupportedException, got {(error == null ? $"rows [{string.Join("; ", rows!)}]" : error.ToString())}");
            }
            else
            {
                Assert.True(error != null && error.Message.Contains(want),
                    $"{mode}: expected an exception containing '{want}', got {(error == null ? $"rows [{string.Join("; ", rows!)}]" : error.ToString())}");
                // A message fragment alone could match an unrelated exception: the type is pinned too.
                Assert.True(error is MongoDB.Driver.Linq.ExpressionNotSupportedException or InvalidOperationException,
                    $"{mode}: '{want}' with unexpected exception type {error!.GetType().Name}");
            }
        }
    }

    // A NativeOnly decline: asserted by exception TYPE (NativeTranslationNotSupportedException), not by message.
    private const string NotNativeAny = "<NativeTranslationNotSupportedException>";

    [Fact]
    public void Mixed_projection_through_a_derived_reference_reads_the_joined_document()
    {
        var collection = Seed(nameof(Mixed_projection_through_a_derived_reference_reads_the_joined_document));
        AssertPerMode(collection,
            db => db.People.Where(p => p.ReferrerId != null).OrderBy(p => p.Name)
                .Select(p => new { p, R = p.Referrer!.Name, L = p.Referrer.Level }).ToList()
                .Select(x => $"{x.p.Name}|{x.p.Badge.Code}|{x.R}|{x.L}"),
            ["Dev|B-dev|Boss|9", "Guest|B-guest|Boss|9"], NotNativeAny, Serves, Serves);
    }

    [Fact]
    public void Mixed_projection_of_a_derived_reference_entity_reads_its_complex_values_from_the_joined_document()
    {
        var collection = Seed(nameof(Mixed_projection_of_a_derived_reference_entity_reads_its_complex_values_from_the_joined_document));
        AssertPerMode(collection,
            db => db.People.Where(p => p.ReferrerId != null).OrderBy(p => p.Name)
                .Select(p => new { p.Name, p.Referrer, D = p.Referrer!.Desk }).ToList()
                .Select(x => $"{x.Name}|{x.Referrer!.Name}|{x.Referrer.Badge.Code}|{x.Referrer.Desk.Lat}|{x.D.Lat}"),
            ["Dev|Boss|B-boss|9|9", "Guest|Boss|B-boss|9|9"], NotNativeAny, Serves, Serves);
    }

    [Fact]
    public void Explicit_self_join_mixed_projection_of_inner_members_reads_the_inner_document()
    {
        // Was `..._is_a_known_pre_existing_wrong_read` (pinned with Assert.NotEqual): a same-type self-join whose mixed
        // projection holds the whole outer entity beside an inner-side hop leaf read the leaf off the OUTER document (at
        // 5547cfa2 shape 0 answered `Dev|B-dev ; Guest|B-guest` in Native and DriverLinq; reproduced at 040cecdf with an
        // OWNED Badge). The read side recognised the root shaper by CLR type; it is now structural (owner-approved exception
        // (h)), so the inner side reads the joined document. Hand-written answers.
        var collection = Seed(nameof(Explicit_self_join_mixed_projection_of_inner_members_reads_the_inner_document));
        IEnumerable<string> Run(PeopleContext db, int shape)
        {
            var joined = db.People.Join(db.People, p => p.ReferrerId, e => (ObjectId?)e.Id, (p, e) => new { p, e }).OrderBy(x => x.p.Name);
            return shape switch
            {
                0 => joined.Select(x => new { x.p, x.e.Badge.Code }).ToList().Select(x => x.p.Name + "|" + x.Code),
                1 => joined.Select(x => new { x.p.Name, E = x.e.Name, x.e.Badge.Code }).ToList().Select(x => x.Name + "|" + x.E + "|" + x.Code),
                _ => joined.Select(x => new { x.p, x.e }).ToList().Select(x => x.p.Name + "|" + x.e.Name + "|" + x.e.Badge.Code + "|" + (x.e as Employee)?.Desk.Lat)
            };
        }

        AssertPerMode(collection, db => Run(db, 0), ["Dev|B-boss", "Guest|B-boss"], NotNativeAny, Serves, Serves);
        AssertPerMode(collection, db => Run(db, 1), ["Dev|Boss|B-boss", "Guest|Boss|B-boss"], NotNativeAny, Serves, Serves);
        NativeModeAssert.NativeAndExpected(m => Outcome(collection, m, db => Run(db, 2)), ["Dev|Boss|B-boss|9", "Guest|Boss|B-boss|9"]);
    }

    [Fact]
    public void Explicit_join_to_an_OfType_set_is_not_served()
    {
        // PRE-EXISTING (not caused by this slice): a Join whose inner source is OfType<TDerived>() is neither native nor
        // expressible by the driver ("Expression not supported"). Jira candidate (proposed to the owner, not filed): support
        // a discriminator-narrowed join inner. Pinned so it never returns rows unnoticed.
        var collection = Seed(nameof(Explicit_join_to_an_OfType_set_is_not_served));
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            Assert.ThrowsAny<Exception>(() => Outcome(collection, mode,
                db => db.People.Join(db.People.OfType<Employee>(), p => p.ReferrerId, e => (ObjectId?)e.Id, (p, e) => new { p, e })
                    .Select(x => new { x.p, x.e.Level }).ToList().Select(x => x.p.Name + x.Level)));
        }
    }

    [Fact]
    public void Derived_outer_root_in_a_join_reads_its_own_members_from_the_root_document()
    {
        // Reverse direction: the OUTER root is the derived type (OfType<Employee>()) and the join's target is Employee too, so
        // an outer member must not be resolved into the joined (referrer's) sub-document. Dev's referrer is Boss.
        var collection = Seed(nameof(Derived_outer_root_in_a_join_reads_its_own_members_from_the_root_document));
        AssertPerMode(collection,
            db => db.People.OfType<Employee>().Where(e => e.ReferrerId != null)
                .Select(e => new { e.Name, e.Level, R = e.Referrer!.Name }).ToList().Select(x => $"{x.Name}|{x.Level}|{x.R}"),
            ["Dev|2|Boss"], NotNativeAny, Serves, Serves);
        AssertPerMode(collection,
            db => db.People.OfType<Employee>().Where(e => e.ReferrerId != null)
                .Select(e => new { e.Name, e.Desk, R = e.Referrer!.Name }).ToList().Select(x => $"{x.Name}|{x.Desk.Lat}|{x.R}"),
            ["Dev|2|Boss"], NotNativeAny, Serves, Serves);
        AssertPerMode(collection,
            db => db.People.OfType<Employee>().Where(e => e.ReferrerId != null)
                .Select(e => new { e, R = e.Referrer!.Name }).ToList().Select(x => $"{x.e.Name}|{x.e.Desk.Lat}|{x.e.Badge.Code}|{x.R}"),
            ["Dev|2|B-dev|Boss"], NotNativeAny, Serves, Serves);
    }

    // ── The same reverse-direction shape with NO complex properties in the model (ruling R9) ─────────────────────

    public class PlainPerson
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public ObjectId? ReferrerId { get; set; }
        public PlainEmployee? Referrer { get; set; }
    }

    public class PlainEmployee : PlainPerson
    {
        public int Level { get; set; }
    }

    private sealed class PlainPeopleContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<PlainPerson> People => Set<PlainPerson>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<PlainEmployee>().HasBaseType<PlainPerson>();
            mb.Entity<PlainPerson>(b =>
            {
                b.ToCollection(collection);
                b.HasDiscriminator<string>("_t").HasValue<PlainPerson>("P").HasValue<PlainEmployee>("E");
                b.HasOne(p => p.Referrer).WithMany().HasForeignKey(p => p.ReferrerId);
                b.Ignore("Badge");
            });
        }
    }

    private List<string> PlainOutcome(string collection, MongoQueryMode mode, Func<PlainPeopleContext, IEnumerable<string>> query)
    {
        var builder = new DbContextOptionsBuilder<PlainPeopleContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        using var db = new PlainPeopleContext(builder.Options, collection);
        return query(db).ToList();
    }

    [Fact]
    public void Derived_outer_root_in_a_join_without_complex_properties()
    {
        // Ruling R9: the structural root-shaper fix also corrects this NON-complex mixed projection (`x.e` the derived
        // outer root, `e.Name` the OUTER name, `R` the referrer's): at 040cecdf it answered `Dev|Boss|Boss` (the outer Name
        // read off the joined referrer document; observed, see the fix round 3 report). Hand-written answer.
        var collection = Seed(nameof(Derived_outer_root_in_a_join_without_complex_properties));
        foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            Assert.Equal(["Dev|Dev|Boss"], PlainOutcome(collection, mode, db => db.People.OfType<PlainEmployee>().Where(e => e.ReferrerId != null)
                .Select(e => new { e, e.Name, R = e.Referrer!.Name }).ToList().Select(x => $"{x.e.Name}|{x.Name}|{x.R}")));
            Assert.Equal(["Dev|2|Boss"], PlainOutcome(collection, mode, db => db.People.OfType<PlainEmployee>().Where(e => e.ReferrerId != null)
                .Select(e => new { e.Name, e.Level, R = e.Referrer!.Name }).ToList().Select(x => $"{x.Name}|{x.Level}|{x.R}")));
        }

        Assert.Throws<MongoDB.EntityFrameworkCore.Query.NativeTranslation.NativeTranslationNotSupportedException>(() => PlainOutcome(collection,
            MongoQueryMode.NativeOnly, db => db.People.OfType<PlainEmployee>().Where(e => e.ReferrerId != null)
                .Select(e => new { e, e.Name, R = e.Referrer!.Name }).ToList().Select(x => $"{x.e.Name}|{x.Name}|{x.R}")));
    }

    [Fact]
    public void Derived_shaper_with_complex_values_in_a_non_join_query_reads_the_root_document()
    {
        // The non-join case the derived-shaper arm exists for: after OfType<Employee>() the root shaper is the derived type,
        // and the mixed (fallback) reader must resolve its declared complex value off the root document.
        var collection = Seed(nameof(Derived_shaper_with_complex_values_in_a_non_join_query_reads_the_root_document));
        Assert.Equal(["2", "9"], NativeModeAssert.DeclinesCleanly(m => Outcome(collection, m,
            db => db.People.OfType<Employee>().Select(e => e.Desk).ToList().Select(d => d.Lat.ToString()).Order())));
        NativeModeAssert.NativeAndExpected(m => Outcome(collection, m,
                db => db.People.OfType<Employee>().OrderBy(e => e.Name).ToList().Select(e => $"{e.Name}|{e.Badge.Code}|{e.Desk.Lat}")),
            ["Boss|B-boss|9", "Dev|B-dev|2"]);
    }
}
