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

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// A <c>SelectMany</c> whose ITEM type is the query root's own type or a TPH type derived from it. The item shaper
/// <c>BuildBareNavWrappedShaper</c> builds is bound to THIS query's empty projection member, so
/// <c>IsRootProjectionShaper</c> (which recognises the root by that binding) answers true for it; were the fallback
/// reader to read the item's members through that arm it would read them off the ROOT document (silent wrong rows).
/// Every seeded item value differs from its root row's, so a wrong-document read shows as a value difference.
/// </summary>
/// <remarks>
/// Pinned outcome (fix round 4 of the complex-type slice): no shape returns wrong rows. The arm is unreachable for a
/// served row: (1) an item type in the root's EF hierarchy needs a cross-collection REFERENCE collection navigation (an
/// owned type never has a base type), and a reference unwind has no driver-LINQ oracle (the bridge throws "Unsupported
/// cross-DbSet query" or "could not be translated"), so every non-native shape over it fails loudly; (2) the native path
/// binds the trailing projection by TransparentIdentifier scope depth
/// (<see cref="NativeSelectManyBinder.TryBindTransparentIdentifierProjection"/>) and reads by alias, never through the
/// shaper. The owned family (the only SelectMany with a working fallback) is covered with a derived element CLR type
/// and a non-derived control: both classify identically.
/// </remarks>
[XUnitCollection("QueryTests")]
public class ComplexTypeDerivedSelectManyItemTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    // ── TPH model: Reports is a reference collection of the DERIVED type, Followers of the SAME type ─────────────

    public class Person
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Badge Badge { get; set; } = null!;
        public List<Employee> Reports { get; set; } = [];
        public ObjectId? LeaderId { get; set; }
        public Person? Leader { get; set; }
        public List<Person> Followers { get; set; } = [];
        public List<Tag> Tags { get; set; } = [];
    }

    public class Employee : Person
    {
        public int Level { get; set; }
        public GeoPoint Desk { get; set; }
        public ObjectId? ManagerId { get; set; }
        public Person? Manager { get; set; }
    }

    public class Badge
    {
        public string Code { get; set; } = null!;
    }

    public class Tag
    {
        public string Label { get; set; } = null!;
    }

    // `withTags` maps the owned Tags collection. It is opt-in: an owned collection on Person is an eager-loaded navigation
    // of every Person/Employee reference element, which the whole-element SelectMany rejects (IsWholeElementRepresentable,
    // pre-existing), so only the OfType-root owned test maps it.
    private sealed class PeopleContext(DbContextOptions options, string collection, bool withTags) : DbContext(options)
    {
        public DbSet<Person> People => Set<Person>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Employee>().HasBaseType<Person>();
            mb.Entity<Person>(b =>
            {
                b.ToCollection(collection);
                b.HasDiscriminator<string>("_t").HasValue<Person>("P").HasValue<Employee>("E");
                b.HasMany(p => p.Reports).WithOne(e => e.Manager).HasForeignKey(e => e.ManagerId);
                b.HasMany(p => p.Followers).WithOne(p => p.Leader).HasForeignKey(p => p.LeaderId);
                b.ComplexProperty(p => p.Badge);
                if (withTags)
                {
                    b.OwnsMany(p => p.Tags);
                }
                else
                {
                    b.Ignore(p => p.Tags);
                }
            });
            mb.Entity<Employee>().ComplexProperty(e => e.Desk);
        }
    }

    private static readonly ObjectId BossId = ObjectId.GenerateNewId();

    // Boss (E, Level 9, Desk 9, B-boss) leads Dev (E, Level 2, Desk 2, B-dev, Tags t-dev1/t-dev2) and Guest (P, B-guest);
    // Dev's manager is Boss. So Boss.Reports = [Dev], Boss.Followers = [Dev, Guest]; every item value differs from Boss's.
    private string Seed(string name)
    {
        var collectionName = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        database.MongoDatabase.GetCollection<BsonDocument>(collectionName).InsertMany(
        [
            new BsonDocument
            {
                { "_id", BossId }, { "_t", "E" }, { "Name", "Boss" }, { "Level", 9 }, { "ManagerId", BsonNull.Value }, { "LeaderId", BsonNull.Value },
                { "Badge", new BsonDocument("Code", "B-boss") }, { "Desk", new BsonDocument { { "Lat", 9.0 }, { "Lon", 9.5 } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "_t", "E" }, { "Name", "Dev" }, { "Level", 2 }, { "ManagerId", BossId }, { "LeaderId", BossId },
                { "Badge", new BsonDocument("Code", "B-dev") }, { "Desk", new BsonDocument { { "Lat", 2.0 }, { "Lon", 2.5 } } },
                { "Tags", new BsonArray { new BsonDocument("Label", "t-dev1"), new BsonDocument("Label", "t-dev2") } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "_t", "P" }, { "Name", "Guest" }, { "LeaderId", BossId },
                { "Badge", new BsonDocument("Code", "B-guest") }
            }
        ]);
        return collectionName;
    }

    private DbContextOptions Options<TContext>(MongoQueryMode mode) where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        return builder.Options;
    }

    private PeopleContext People(string collection, MongoQueryMode mode, bool withTags = false)
        => new(Options<PeopleContext>(mode), collection, withTags);

    // ── Per-mode pins ──────────────────────────────────────────────────────────────────────────────────────────

    private const string Serves = CompositionAssert.Serves;
    private const string NotNativeAny = CompositionAssert.NotNative;
    private const string CrossDbSet = "Unsupported cross-DbSet query";
    private const string NotTranslated = "could not be translated";
    private const string DriverNotSupported = "Expression not supported";
    private const string OuterIdMissing = "missing for required non-nullable property 'Id'";
    private const string BsonDocKey = "'bsonDoc'";
    private const string NotLocated = "could not be located in the document";
    // Nav-expansion differs by EF version: EF10 refuses the shape itself, EF8/EF9 hand the bridge a cross-DbSet source.
    private const string NotTranslatedOrCrossDbSet = NotTranslated + "||" + CrossDbSet;

    private static readonly Dictionary<string, Type> FragmentExceptionTypes = new()
    {
        [CrossDbSet] = typeof(InvalidOperationException),
        [NotTranslated] = typeof(InvalidOperationException),
        [DriverNotSupported] = typeof(MongoDB.Driver.Linq.ExpressionNotSupportedException),
        [OuterIdMissing] = typeof(InvalidOperationException),
        [BsonDocKey] = typeof(KeyNotFoundException),
        [NotLocated] = typeof(InvalidOperationException),
        ["cannot be used for parameter"] = typeof(ArgumentException),
        ["does not match member type"] = typeof(ArgumentException)
    };

    // Pins EACH mode: `Serves` with the hand-written rows, `NotNativeAny` (a NativeTranslationNotSupportedException), or an
    // exception whose message contains the fragment (`||`-separated alternatives). A mode that starts serving where it threw
    // (or the reverse), or serves different rows, fails the test.
    private static void AssertPerMode<TContext>(
        Func<MongoQueryMode, TContext> create, Func<TContext, IEnumerable<string>> query, string[] expected,
        string nativeOnly, string native, string driverLinq)
        where TContext : DbContext
        => CompositionAssert.PerMode(
            mode =>
            {
                using var db = create(mode);
                return query(db).ToList();
            },
            expected, nativeOnly, native, driverLinq, FragmentExceptionTypes);

    // ── Derived item (Reports: List<Employee>) ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Derived_item_scalar_projection_is_native_and_reads_the_item_document()
    {
        // Native: the trailing projection is bound by scope depth (ti.Outer / ti.Inner) and read by alias; the item's Name
        // and Level are Dev's, not Boss's. No driver-LINQ oracle for a cross-collection SelectMany (pre-existing).
        var collection = Seed(nameof(Derived_item_scalar_projection_is_native_and_reads_the_item_document));
        AssertPerMode(m => People(collection, m),
            db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, E = e.Name, e.Level }).ToList().Select(x => $"{x.Name}|{x.E}|{x.Level}"),
            ["Boss|Dev|2"], Serves, Serves, CrossDbSet);
        // The explicit correlated-subquery spelling nav-expansion produces.
        AssertPerMode(m => People(collection, m),
            db => db.People.SelectMany(p => db.Set<Employee>().Where(e => e.ManagerId == p.Id), (p, e) => new { p.Name, E = e.Name, e.Level })
                .ToList().Select(x => $"{x.Name}|{x.E}|{x.Level}"),
            ["Boss|Dev|2"], Serves, Serves, CrossDbSet);
    }

    [Fact]
    public void Derived_item_whole_element_is_native_and_is_the_item()
    {
        var collection = Seed(nameof(Derived_item_whole_element_is_native_and_is_the_item));
        AssertPerMode(m => People(collection, m),
            db => db.People.SelectMany(p => p.Reports).ToList().Select(e => $"{e.Name}|{e.Level}|{e.Desk.Lat}|{e.Badge.Code}"),
            ["Dev|2|2|B-dev"], Serves, Serves, NotTranslated);
    }

    [Fact]
    public void Derived_item_mixed_and_complex_value_projections_never_return_rows()
    {
        // These are the shapes that would reach the fallback reader with the item shaper (IsRootProjectionShaper true, the
        // item type assignable to the root). None is native, and the fallback has no oracle: loud in every mode.
        var collection = Seed(nameof(Derived_item_mixed_and_complex_value_projections_never_return_rows));
        var q = (Func<MongoQueryMode, PeopleContext>)(m => People(collection, m));

        // Whole outer and whole item beside an item member.
        AssertPerMode(q, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p, e, E = e.Name }).ToList().Select(x => $"{x.p.Name}|{x.e.Name}|{x.E}"),
            ["Boss|Dev|Dev"], NotNativeAny, NotTranslated, NotTranslated);
        // Whole item beside outer and item members.
        AssertPerMode(q, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, e, E = e.Name }).ToList().Select(x => $"{x.Name}|{x.e.Name}|{x.e.Level}|{x.e.Desk.Lat}|{x.E}"),
            ["Boss|Dev|2|2|Dev"], NotNativeAny, NotTranslated, NotTranslated);
        // Whole outer beside an item member.
        AssertPerMode(q, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p, E = e.Name }).ToList().Select(x => $"{x.p.Name}|{x.E}"),
            ["Boss|Dev"], NotNativeAny, NotTranslated, NotTranslated);
        // Whole complex values of the item: member spelling, EF.Property spelling, bare.
        AssertPerMode(q, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, e.Desk, e.Badge }).ToList().Select(x => $"{x.Name}|{x.Desk.Lat}|{x.Badge.Code}"),
            ["Boss|2|B-dev"], NotNativeAny, NotTranslated, NotTranslated);
        AssertPerMode(q, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, D = EF.Property<GeoPoint>(e, "Desk"), B = EF.Property<Badge>(e, "Badge") })
                .ToList().Select(x => $"{x.Name}|{x.D.Lat}|{x.B.Code}"),
            ["Boss|2|B-dev"], NotNativeAny, NotTranslated, NotTranslated);
        AssertPerMode(q, db => db.People.SelectMany(p => p.Reports, (p, e) => e.Desk).ToList().Select(d => $"{d.Lat}"),
            ["2"], NotNativeAny, NotTranslated, NotTranslated);
        // Complex LEAVES of the item (member and EF.Property spellings, query syntax): a dotted leaf isn't bound by the
        // SelectMany projection binder, so it declines natively and the bridge rejects the cross-collection source.
        AssertPerMode(q, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Badge.Code, E = e.Badge.Code, L = e.Desk.Lat }).ToList().Select(x => $"{x.Code}|{x.E}|{x.L}"),
            ["B-boss|B-dev|2"], NotNativeAny, CrossDbSet, CrossDbSet);
        AssertPerMode(q, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, L = EF.Property<GeoPoint>(e, "Desk").Lat, B = EF.Property<Badge>(e, "Badge").Code })
                .ToList().Select(x => $"{x.Name}|{x.L}|{x.B}"),
            ["Boss|2|B-dev"], NotNativeAny, CrossDbSet, CrossDbSet);
        AssertPerMode(q, db => (from p in db.People from e in p.Reports select new { p.Name, E = e.Name, C = e.Badge.Code }).ToList().Select(x => $"{x.Name}|{x.E}|{x.C}"),
            ["Boss|Dev|B-dev"], NotNativeAny, CrossDbSet, CrossDbSet);
    }

    [Fact]
    public void OfType_narrowed_correlated_subquery_is_refused_by_EF_in_every_mode()
    {
        // `db.People.OfType<Employee>().Where(e => e.ManagerId == p.Id)` as the collection selector: EF's own translation
        // fails before any provider binder sees it (pre-existing; the shape the Important named). Never rows.
        var collection = Seed(nameof(OfType_narrowed_correlated_subquery_is_refused_by_EF_in_every_mode));
        var q = (Func<MongoQueryMode, PeopleContext>)(m => People(collection, m));
        AssertPerMode(q, db => db.People.SelectMany(p => db.People.OfType<Employee>().Where(e => e.ManagerId == p.Id), (p, e) => new { p.Name, E = e.Name, e.Level })
                .ToList().Select(x => $"{x.Name}|{x.E}|{x.Level}"),
            ["Boss|Dev|2"], NotTranslated, NotTranslated, NotTranslated);
        AssertPerMode(q, db => db.People.SelectMany(p => db.People.OfType<Employee>().Where(e => e.ManagerId == p.Id), (p, e) => new { p, e, E = e.Name })
                .ToList().Select(x => $"{x.p.Name}|{x.e.Name}|{x.E}"),
            ["Boss|Dev|Dev"], NotTranslated, NotTranslated, NotTranslated);
        AssertPerMode(q, db => db.People.SelectMany(p => db.People.OfType<Employee>().Where(e => e.ManagerId == p.Id), (p, e) => new { p.Name, e.Desk, C = e.Badge.Code })
                .ToList().Select(x => $"{x.Name}|{x.Desk.Lat}|{x.C}"),
            ["Boss|2|B-dev"], NotTranslated, NotTranslated, NotTranslated);
    }

    // ── Same-type item (Followers: List<Person>): the pre-existing `shaperEntityType == _rootEntityType` checks ───

    [Fact]
    public void Same_type_item_scalar_projection_is_native_and_reads_the_item_document()
    {
        var collection = Seed(nameof(Same_type_item_scalar_projection_is_native_and_reads_the_item_document));
        AssertPerMode(m => People(collection, m),
            db => db.People.SelectMany(p => p.Followers, (p, f) => new { p.Name, F = f.Name }).ToList().Select(x => $"{x.Name}|{x.F}"),
            ["Boss|Dev", "Boss|Guest"], Serves, Serves, DriverNotSupported);
        AssertPerMode(m => People(collection, m),
            db => db.People.SelectMany(p => p.Followers).ToList().Select(f => $"{f.Name}|{f.Badge.Code}"),
            ["Dev|B-dev", "Guest|B-guest"], Serves, Serves, NotTranslated);
    }

    [Fact]
    public void Same_type_item_mixed_and_complex_projections_never_return_rows()
    {
        // The item shaper has the ROOT's type, so both the binding check and the pre-existing CLR-type checks
        // (`shaperEntityType == _rootEntityType`, `shaper.StructuralType == _rootEntityType`) classify it as the root. The
        // fallback has no oracle, so it never serves a row.
        var collection = Seed(nameof(Same_type_item_mixed_and_complex_projections_never_return_rows));
        var q = (Func<MongoQueryMode, PeopleContext>)(m => People(collection, m));
        AssertPerMode(q, db => db.People.SelectMany(p => p.Followers, (p, f) => new { p.Name, F = f.Name, C = f.Badge.Code }).ToList().Select(x => $"{x.Name}|{x.F}|{x.C}"),
            ["Boss|Dev|B-dev", "Boss|Guest|B-guest"], NotNativeAny, DriverNotSupported, DriverNotSupported);
        AssertPerMode(q, db => db.People.SelectMany(p => p.Followers, (p, f) => new { p.Name, f.Badge }).ToList().Select(x => $"{x.Name}|{x.Badge.Code}"),
            ["Boss|B-dev", "Boss|B-guest"], NotNativeAny, NotTranslated, NotTranslated);
        AssertPerMode(q, db => db.People.SelectMany(p => p.Followers, (p, f) => new { p.Name, f }).ToList().Select(x => $"{x.Name}|{x.f.Name}|{x.f.Badge.Code}"),
            ["Boss|Dev|B-dev", "Boss|Guest|B-guest"], NotNativeAny, NotTranslated, NotTranslated);
    }

    // ── Derived OUTER root (OfType<Employee>()) then SelectMany ────────────────────────────────────────────────

    [Fact]
    public void Derived_outer_root_SelectMany_with_a_derived_member_declines_cleanly()
    {
        // Found while exposing the Important (pre-existing, not complex-specific): the SelectMany projection binder re-roots
        // `ti.Outer.Level` onto a parameter typed as the COLLECTION root (Person), and Expression.MakeMemberAccess threw
        // ArgumentException ("Property 'Int32 Level' is not defined for type 'Person'") in every mode, even DriverLinq.
        // It now declines (NativeOnly) and the fallback serves what the driver can: the owned Tags unwind.
        var collection = Seed(nameof(Derived_outer_root_SelectMany_with_a_derived_member_declines_cleanly));
        var q = (Func<MongoQueryMode, PeopleContext>)(m => People(collection, m, withTags: true));
        AssertPerMode(q, db => db.People.OfType<Employee>().SelectMany(e => e.Tags, (e, t) => new { e.Name, e.Level, T = t.Label }).ToList().Select(x => $"{x.Name}|{x.Level}|{x.T}"),
            ["Dev|2|t-dev1", "Dev|2|t-dev2"], NotNativeAny, Serves, Serves);
        // Base-declared members only: native, and the derived root's own values.
        AssertPerMode(q, db => db.People.OfType<Employee>().SelectMany(e => e.Tags, (e, t) => new { e.Name, T = t.Label }).ToList().Select(x => $"{x.Name}|{x.T}"),
            ["Dev|t-dev1", "Dev|t-dev2"], Serves, Serves, Serves);
        // A derived complex leaf of the root beside the item.
        AssertPerMode(q, db => db.People.OfType<Employee>().SelectMany(e => e.Tags, (e, t) => new { e.Name, D = e.Desk.Lat, T = t.Label }).ToList().Select(x => $"{x.Name}|{x.D}|{x.T}"),
            ["Dev|2|t-dev1", "Dev|2|t-dev2"], NotNativeAny, Serves, Serves);
        // Reference collections of the same hierarchy under a derived root: decline, then the fallback has no oracle.
        AssertPerMode(q, db => db.People.OfType<Employee>().SelectMany(e => e.Followers, (e, f) => new { e.Name, e.Level, F = f.Name, C = f.Badge.Code }).ToList().Select(x => $"{x.Name}|{x.Level}|{x.F}|{x.C}"),
            ["Boss|9|Dev|B-dev", "Boss|9|Guest|B-guest"], NotNativeAny, DriverNotSupported, DriverNotSupported);
        AssertPerMode(q, db => db.People.OfType<Employee>().SelectMany(e => e.Reports, (e, r) => new { e.Name, e.Desk, R = r.Name, RD = r.Desk }).ToList().Select(x => $"{x.Name}|{x.Desk.Lat}|{x.R}|{x.RD.Lat}"),
            ["Boss|9|Dev|2"], NotNativeAny, NotTranslatedOrCrossDbSet, NotTranslatedOrCrossDbSet);
    }

    // ── Owned collection whose ELEMENT CLR type derives from the root's, with a non-derived control ────────────
    //
    // The only SelectMany family with a working driver-LINQ fallback (the mixed reader runs). The owned element type is
    // never in the root's EF hierarchy (an owned type has no base type), so `_rootEntityType.IsAssignableFrom` is false for
    // it and the root arm can't fire; the control shows the derived CLR type changes no classification.

    [ComplexType]
    public class OBadge
    {
        public string Code { get; set; } = null!;
    }

    [ComplexType]
    public class ODesk
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class OwnedRoot
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public OBadge Badge { get; set; } = null!;
        public List<OwnedEmp> Reports { get; set; } = [];
    }

    public class OwnedEmp : OwnedRoot
    {
        public int Level { get; set; }
        public ODesk Desk { get; set; } = null!;
    }

    public class CtlRoot
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public OBadge Badge { get; set; } = null!;
        public List<CtlItem> Reports { get; set; } = [];
    }

    public class CtlItem
    {
        public string Name { get; set; } = null!;
        public int Level { get; set; }
        public OBadge Badge { get; set; } = null!;
        public ODesk Desk { get; set; } = null!;
    }

    private sealed class OwnedDerivedContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<OwnedRoot> People => Set<OwnedRoot>();

        protected override void OnModelCreating(ModelBuilder mb)
            => mb.Entity<OwnedRoot>(b =>
            {
                b.ToCollection(collection);
                // The inherited Id/Reports are the owner's; the element is keyed by owner + ordinal like the control.
                b.OwnsMany(p => p.Reports, r => { r.Ignore(e => e.Id); r.Ignore(e => e.Reports); });
            });
    }

    private sealed class CtlContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<CtlRoot> People => Set<CtlRoot>();

        protected override void OnModelCreating(ModelBuilder mb)
            => mb.Entity<CtlRoot>(b => { b.ToCollection(collection); b.OwnsMany(p => p.Reports); });
    }

    private OwnedDerivedContext Owned(string collection, MongoQueryMode mode) => new(Options<OwnedDerivedContext>(mode), collection);
    private CtlContext Ctl(string collection, MongoQueryMode mode) => new(Options<CtlContext>(mode), collection);

    // Boss (B-boss) with elements Dev (2, B-dev, Desk 2) and Ops (3, B-ops, Desk 3); Guest with none.
    private string SeedOwned(string name)
    {
        var collectionName = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        database.MongoDatabase.GetCollection<BsonDocument>(collectionName).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Boss" }, { "Badge", new BsonDocument("Code", "B-boss") },
                { "Reports", new BsonArray
                    {
                        new BsonDocument { { "Name", "Dev" }, { "Level", 2 }, { "Badge", new BsonDocument("Code", "B-dev") }, { "Desk", new BsonDocument { { "Lat", 2.0 }, { "Lon", 2.5 } } } },
                        new BsonDocument { { "Name", "Ops" }, { "Level", 3 }, { "Badge", new BsonDocument("Code", "B-ops") }, { "Desk", new BsonDocument { { "Lat", 3.0 }, { "Lon", 3.5 } } } }
                    }
                }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Guest" }, { "Badge", new BsonDocument("Code", "B-guest") }, { "Reports", new BsonArray() }
            }
        ]);
        return collectionName;
    }

    [Fact]
    public void Owned_element_type_derived_from_the_root_is_modelled_as_an_owned_type_without_a_base_type()
    {
        using var db = Owned("x", MongoQueryMode.Native);
        var element = db.Model.FindEntityType(typeof(OwnedEmp))!;
        Assert.True(element.IsOwned());
        Assert.Null(element.BaseType);
        Assert.False(db.Model.FindEntityType(typeof(OwnedRoot))!.IsAssignableFrom(element));
        Assert.Equal(["Badge", "Desk"], element.GetComplexProperties().Select(c => c.Name).Order());
    }

    [Fact]
    public void Owned_derived_element_SelectMany_serves_the_element_values_or_fails_loudly_like_the_control()
    {
        var collection = SeedOwned(nameof(Owned_derived_element_SelectMany_serves_the_element_values_or_fails_loudly_like_the_control));
        var owned = (Func<MongoQueryMode, OwnedDerivedContext>)(m => Owned(collection, m));
        var ctl = (Func<MongoQueryMode, CtlContext>)(m => Ctl(collection, m));

        // Scalar: native, every mode.
        AssertPerMode(owned, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, E = e.Name, e.Level }).ToList().Select(x => $"{x.Name}|{x.E}|{x.Level}"),
            ["Boss|Dev|2", "Boss|Ops|3"], Serves, Serves, Serves);
        AssertPerMode(ctl, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, E = e.Name, e.Level }).ToList().Select(x => $"{x.Name}|{x.E}|{x.Level}"),
            ["Boss|Dev|2", "Boss|Ops|3"], Serves, Serves, Serves);

        // Whole element (no driver-LINQ oracle for a whole owned element: pre-existing, same for the control).
        AssertPerMode(owned, db => db.People.SelectMany(p => p.Reports).AsNoTracking().ToList().Select(e => $"{e.Name}|{e.Level}|{e.Desk.Lat}|{e.Badge.Code}"),
            ["Dev|2|2|B-dev", "Ops|3|3|B-ops"], Serves, Serves, "does not match member type");
        AssertPerMode(ctl, db => db.People.SelectMany(p => p.Reports).AsNoTracking().ToList().Select(e => $"{e.Name}|{e.Level}|{e.Desk.Lat}|{e.Badge.Code}"),
            ["Dev|2|2|B-dev", "Ops|3|3|B-ops"], Serves, Serves, "cannot be used for parameter");

        // Complex leaves of the element (member, EF.Property, query syntax, client-computed beside one): the fallback
        // reads the element's values.
        AssertPerMode(owned, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Badge.Code, E = e.Badge.Code, L = e.Desk.Lat }).ToList().Select(x => $"{x.Code}|{x.E}|{x.L}"),
            ["B-boss|B-dev|2", "B-boss|B-ops|3"], NotNativeAny, Serves, Serves);
        AssertPerMode(owned, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, L = EF.Property<ODesk>(e, "Desk").Lat, B = EF.Property<OBadge>(e, "Badge").Code }).ToList().Select(x => $"{x.Name}|{x.L}|{x.B}"),
            ["Boss|2|B-dev", "Boss|3|B-ops"], NotNativeAny, Serves, Serves);
        AssertPerMode(owned, db => (from p in db.People from e in p.Reports select new { p.Name, E = e.Name, C = e.Badge.Code }).ToList().Select(x => $"{x.Name}|{x.E}|{x.C}"),
            ["Boss|Dev|B-dev", "Boss|Ops|B-ops"], NotNativeAny, Serves, Serves);
        AssertPerMode(owned, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, E = e.Name + "!", C = e.Badge.Code }).ToList().Select(x => $"{x.Name}|{x.E}|{x.C}"),
            ["Boss|Dev!|B-dev", "Boss|Ops!|B-ops"], NotNativeAny, Serves, Serves);

        // Pre-existing loud fallback failures, identical for the control: whole outer / whole element beside a member, and
        // a whole complex value of the element.
        AssertPerMode(owned, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p, e, E = e.Name }).AsNoTracking().ToList().Select(x => $"{x.p.Name}|{x.e.Name}|{x.E}"),
            ["Boss|Dev|Dev", "Boss|Ops|Ops"], NotNativeAny, BsonDocKey, BsonDocKey);
        AssertPerMode(ctl, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p, e, E = e.Name }).AsNoTracking().ToList().Select(x => $"{x.p.Name}|{x.e.Name}|{x.E}"),
            ["Boss|Dev|Dev", "Boss|Ops|Ops"], NotNativeAny, BsonDocKey, BsonDocKey);
        AssertPerMode(owned, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p, E = e.Name, e.Level }).ToList().Select(x => $"{x.p.Name}|{x.E}|{x.Level}"),
            ["Boss|Dev|2", "Boss|Ops|3"], NotNativeAny, OuterIdMissing, OuterIdMissing);
        AssertPerMode(ctl, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p, E = e.Name, e.Level }).ToList().Select(x => $"{x.p.Name}|{x.E}|{x.Level}"),
            ["Boss|Dev|2", "Boss|Ops|3"], NotNativeAny, OuterIdMissing, OuterIdMissing);
        AssertPerMode(owned, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, e.Desk, e.Badge }).ToList().Select(x => $"{x.Name}|{x.Desk.Lat}|{x.Badge.Code}"),
            ["Boss|2|B-dev", "Boss|3|B-ops"], NotNativeAny, NotLocated, NotLocated);
        AssertPerMode(ctl, db => db.People.SelectMany(p => p.Reports, (p, e) => new { p.Name, e.Desk, e.Badge }).ToList().Select(x => $"{x.Name}|{x.Desk.Lat}|{x.Badge.Code}"),
            ["Boss|2|B-dev", "Boss|3|B-ops"], NotNativeAny, NotLocated, NotLocated);
        AssertPerMode(owned, db => db.People.SelectMany(p => p.Reports, (p, e) => e.Desk).ToList().Select(d => $"{d.Lat}"),
            ["2", "3"], NotNativeAny, NotLocated, NotLocated);
    }
}
