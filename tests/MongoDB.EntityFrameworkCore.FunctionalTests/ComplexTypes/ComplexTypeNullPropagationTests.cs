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

// Optional complex properties and complex collections exist on EF10 only.
#if !EF8 && !EF9
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Null propagation (ruling R14: an absent parent reads its children as null; R17: a null element reads its members as
/// null) where the native path compared a MISSING value raw against a null-normalized one: (a) a correlated ROOT field
/// read inside a complex element scope, (b) a <c>CompareTo</c>/<c>string.Compare</c> folded to a query-dialect range that
/// never matches null/missing, (c) a leaf-to-leaf comparison through an optional parent. Every answer is hand-written
/// (C# with EF null propagation); DriverLinq rows are the measured driver behaviour where it differs.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeNullPropagationTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Bits
    {
        public string? Text { get; set; }
        public string Req { get; set; } = null!;
        public int Num { get; set; }
    }

    public class Unit
    {
        public string? Code { get; set; }
    }

    public class Addr
    {
        public string City { get; set; } = null!;
        public string? Street { get; set; }
        public int Floor { get; set; }
        public List<Unit> Units { get; set; } = [];
    }

    public class Post
    {
        public string? Title { get; set; }
    }

    public class OBits
    {
        public string? Text { get; set; }
        public string Req { get; set; } = null!;
    }

    public class Person
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Nick { get; set; }
        public Bits Main { get; set; } = null!;
        public Bits? Opt { get; set; }
        public List<Addr> Addresses { get; set; } = [];
        public List<Post> Posts { get; set; } = [];
        public OBits? Owned { get; set; }
    }

    private sealed class PeopleContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<Person> People => Set<Person>();

        protected override void OnModelCreating(ModelBuilder mb)
            => mb.Entity<Person>(b =>
            {
                b.ToCollection(collection);
                b.ComplexProperty(p => p.Main);
                b.ComplexProperty(p => p.Opt);
                b.ComplexCollection(p => p.Addresses, a => a.ComplexCollection(x => x.Units));
                b.OwnsMany(p => p.Posts);
                b.OwnsOne(p => p.Owned);
            });
    }

    private static BsonDocument BitsDoc(BsonValue text, BsonValue req, int num) => new() { { "Text", text }, { "Req", req }, { "Num", num } };

    private static BsonDocument AddrDoc(string city, BsonValue street, int floor, params BsonValue[] codes)
        => new() { { "City", city }, { "Street", street }, { "Floor", floor }, { "Units", new BsonArray(codes.Select(c => new BsonDocument("Code", c))) } };

    /// <summary>
    /// n1: Nick k1, Main {Text null, R1, 1}, Opt BSON null, Addresses [X(Street null, Floor 1, Units [null code]), null],
    /// Posts [{Title null}], Owned null. n2: Nick k2, Main {X, R2, 2}, Opt {X, A, 5}, [X(Street s, Floor 2, Units [X])],
    /// Posts [{X}], Owned {X, A}. n3: Nick k3, Main {null, R3, 3}, Opt {null, null, 0}, [], Posts [], Owned {null, null}.
    /// n4: Nick MISSING, Main {M, R4, 4}, Opt MISSING, [$Name(Street s, Floor 4, Units [c]), null], Posts [{s}], Owned
    /// MISSING.
    /// </summary>
    private Func<MongoQueryMode, List<string>> People(Func<IQueryable<Person>, IQueryable<Person>> filter,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = Seed(name);
        return mode => Run(collection, mode, db => [.. filter(db.People).Select(p => p.Name).ToList().Order(StringComparer.Ordinal)]);
    }

    private string Seed(string name)
    {
        var collection = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        database.MongoDatabase.GetCollection<BsonDocument>(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "n1" }, { "Nick", "k1" }, { "Main", BitsDoc(BsonNull.Value, "R1", 1) },
                { "Opt", BsonNull.Value }, { "Addresses", new BsonArray { AddrDoc("X", BsonNull.Value, 1, BsonNull.Value), BsonNull.Value } },
                { "Posts", new BsonArray { new BsonDocument("Title", BsonNull.Value) } }, { "Owned", BsonNull.Value }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "n2" }, { "Nick", "k2" }, { "Main", BitsDoc("X", "R2", 2) },
                { "Opt", BitsDoc("X", "A", 5) }, { "Addresses", new BsonArray { AddrDoc("X", "s", 2, "X") } },
                { "Posts", new BsonArray { new BsonDocument("Title", "X") } }, { "Owned", new BsonDocument { { "Text", "X" }, { "Req", "A" } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "n3" }, { "Nick", "k3" }, { "Main", BitsDoc(BsonNull.Value, "R3", 3) },
                { "Opt", BitsDoc(BsonNull.Value, BsonNull.Value, 0) }, { "Addresses", new BsonArray() }, { "Posts", new BsonArray() },
                { "Owned", new BsonDocument { { "Text", BsonNull.Value }, { "Req", BsonNull.Value } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "n4" }, { "Main", BitsDoc("M", "R4", 4) },
                { "Addresses", new BsonArray { AddrDoc("$Name", "s", 4, "c"), BsonNull.Value } },
                { "Posts", new BsonArray { new BsonDocument("Title", "s") } }
            }
        ]);
        return collection;
    }

    private List<string> Run(string collection, MongoQueryMode mode, Func<PeopleContext, List<string>> query, IMongoClient? client = null)
    {
        var builder = new DbContextOptionsBuilder<PeopleContext>()
            .UseMongoDB(client ?? database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        using var db = new PeopleContext(builder.Options, collection);
        return query(db);
    }

    // The aggregate pipeline the NativeOnly query sends, as JSON.
    private string Pipeline(Func<IQueryable<Person>, IQueryable<Person>> filter, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = Seed(name + "_mql");
        using var capture = new CommandCapture(database);
        capture.Clear();
        Run(collection, MongoQueryMode.NativeOnly, db => [.. filter(db.People).Select(p => p.Name)],
            capture.Collection(database.MongoDatabase.GetCollection<BsonDocument>(collection)).Database.Client);
        return Assert.Single(capture.Named("aggregate"))["pipeline"].ToJson();
    }

    private static void Serves(Func<MongoQueryMode, List<string>> run, string[] expected, string[] driverRows)
        => PerMode(mode => mode == MongoQueryMode.DriverLinq && !driverRows.SequenceEqual(expected)
                ? run(mode).SequenceEqual(driverRows) ? [.. expected] : run(mode)
                : run(mode),
            expected, CompositionAssert.Serves, CompositionAssert.Serves, CompositionAssert.Serves);

    // ── (a) A correlated root field inside a complex element scope ───────────────────────────────────────────────

    [Fact]
    public void Correlated_root_leaf_under_an_optional_parent_reads_null_when_the_parent_is_absent()
    {
        // n1 (Opt null) and n4 (Opt missing) read Opt.Text as null; n1's X has Street null, n4's null element reads Street
        // null. RED (5547cfa2): Native/NativeOnly [] ("$Opt.Text" is MISSING, the element side is $ifNull'd to null).
        Serves(People(q => q.Where(p => p.Addresses.Any(a => a.Street == p.Opt!.Text))), ["n1", "n4"], ["n1"]);
        // RED: [n1, n2, n3].
        Serves(People(q => q.Where(p => p.Addresses.All(a => a.Street != p.Opt!.Text))), ["n2", "n3"], ["n2", "n3", "n4"]);
        Serves(People(q => q.Where(p => p.Addresses.Any(a => a.Street != p.Opt!.Text))), ["n2", "n4"], ["n1", "n2", "n4"]);
        // RED: [].
        Serves(People(q => q.Where(p => p.Addresses.Count(a => a.Street == p.Opt!.Text) > 0)), ["n1", "n4"], ["n1"]);
        // The outer operand on the left.
        Serves(People(q => q.Where(p => p.Addresses.Any(a => p.Opt!.Text == a.Street))), ["n1", "n4"], ["n1"]);
        // A missing OPTIONAL root scalar (n4's Nick): the null element's City reads null, as does Nick.
        Serves(People(q => q.Where(p => p.Addresses.Any(a => a.City == p.Nick))), ["n4"], []);
    }

    [Fact]
    public void Correlated_root_leaf_in_nested_element_scopes_and_relational_siblings()
    {
        // Nested: n1's X has a Unit with Code null (Opt.Text null); n2's X has Code X. RED: [n2].
        Serves(People(q => q.Where(p => p.Addresses.Any(a => a.Units.Any(u => u.Code == p.Opt!.Text)))), ["n1", "n2"], ["n2"]);
        // Relational: an absent Opt reads Num null, which no comparison satisfies (already right; the outer side is now
        // null-normalized and guarded on the lower side).
        Serves(People(q => q.Where(p => p.Addresses.Any(a => a.Floor < p.Opt!.Num))), ["n2"], ["n2"]);
        Serves(People(q => q.Where(p => p.Addresses.Any(a => p.Opt!.Num > a.Floor))), ["n2"], ["n2"]);
        Serves(People(q => q.Where(p => p.Addresses.Any(a => p.Opt!.Num <= a.Floor))), [], ["n1", "n4"]);
        // Count(pred) == 1 with `!=`: n2's s != X, n4's s != null (its null element's Street null == null).
        Serves(People(q => q.Where(p => p.Addresses.Count(a => a.Street != p.Opt!.Text) == 1)), ["n2", "n4"], ["n1", "n2", "n4"]);
        // Nested All: n1's X has a Unit whose Code null equals the absent Opt's Text; n2's X has X.
        Serves(People(q => q.Where(p => p.Addresses.All(a => a.Units.All(u => u.Code != p.Opt!.Text)))), ["n3", "n4"], ["n1", "n3", "n4"]);
        // An element-leaf Select chain's Contains goes to the fallback (R21 limit), whose rows are C#'s here.
        Declines(People(q => q.Where(p => p.Addresses.Select(a => a.Street).Contains(p.Opt!.Text))), "n1", "n4");
        // A local list holding the outer value is refused in Native (R20: a membership test of a member value).
        Refused(People(q => q.Where(p => p.Addresses.Any(a => new[] { p.Opt!.Text, "zz" }.Contains(a.Street)))), ["n1"],
            "a membership test of a member value in a local collection");
        // A required parent's nullable leaf: n1's null element reads City null, and n1's Main.Text is null (unchanged MQL).
        Serves(People(q => q.Where(p => p.Addresses.Any(a => a.City == p.Main.Text))), ["n1", "n2"], ["n2"]);
    }

    [Fact]
    public void Correlated_root_leaf_mql_in_complex_and_owned_element_scopes()
    {
        // Complex element scope: the outer leaf is $ifNull'd like the element leaf.
        Assert.Contains("""{ "$eq" : [{ "$ifNull" : ["$$e.Street", null] }, { "$ifNull" : ["$Opt.Text", null] }] }""",
            Pipeline(q => q.Where(p => p.Addresses.Any(a => a.Street == p.Opt!.Text))));
        // OWNED element scope: byte-identical to 5547cfa2 (owner ruling: no $ifNull in owned element scopes).
        Assert.Contains("""{ "$eq" : ["$$e.Title", "$Opt.Text"] }""",
            Pipeline(q => q.Where(p => p.Posts.Any(c => c.Title == p.Opt!.Text))));
    }

    [Fact]
    public void Owned_element_scope_rows_are_unchanged()
        // Control: n2's Post X equals Opt.Text X. n1's Post (Title null) vs its absent Opt: the owned scope keeps the raw
        // compare (owner ruling), so it does not match (C# would answer true); unchanged by this fix.
        => Serves(People(q => q.Where(p => p.Posts.Any(c => c.Title == p.Opt!.Text))), ["n2"], ["n2"]);

    // ── (b) CompareTo / string.Compare folds ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void CompareTo_over_a_complex_element_leaf_orders_a_null_element_first()
    {
        // n1's and n4's null elements read City null: CompareTo(null, "X") == -1. n4's "$Name" sorts before "X" too.
        // RED (5547cfa2): Native/NativeOnly [n4] (folded to {City: {$lt: "X"}}, which a null element never matches).
        Serves(People(q => q.Where(p => p.Addresses.Any(a => a.City.CompareTo("X") < 0))), ["n1", "n4"], ["n1", "n4"]);
        Serves(People(q => q.Where(p => p.Addresses.Any(a => string.Compare(a.City, "X") < 0))), ["n1", "n4"], ["n1", "n4"]);
        Serves(People(q => q.Where(p => p.Addresses.All(a => a.City.CompareTo("X") >= 0))), ["n2", "n3"], ["n2", "n3"]);
        // `> 0` is unaffected by a null element (-1 never satisfies it).
        Serves(People(q => q.Where(p => p.Addresses.Any(a => a.City.CompareTo("A") > 0))), ["n1", "n2"], ["n1", "n2"]);
    }

    [Fact]
    public void CompareTo_over_a_leaf_under_an_optional_complex_parent_orders_an_absent_parent_first()
    {
        // n1 (Opt null), n3 (Req null), n4 (Opt missing) read Req null: -1 < 0. n2's "A" < "X". RED (5547cfa2):
        // Native/NativeOnly [n2] ({"Opt.Req": {$lt: "X"}}).
        Serves(People(q => q.Where(p => p.Opt!.Req.CompareTo("X") < 0)), ["n1", "n2", "n3", "n4"], ["n2"]);
        Serves(People(q => q.Where(p => string.Compare(p.Opt!.Req, "X") <= 0)), ["n1", "n2", "n3", "n4"], ["n2"]);
        Serves(People(q => q.Where(p => p.Opt!.Req.CompareTo("B") > 0)), [], []);
        Serves(People(q => q.Where(p => string.Compare(p.Opt!.Req, "B") < 0)), ["n1", "n2", "n3", "n4"], ["n2"]);
        // A required parent: the fold stays (control).
        Serves(People(q => q.Where(p => p.Main.Req.CompareTo("R2") < 0)), ["n1"], ["n1"]);
    }

    [Fact]
    public void CompareTo_fold_mql_is_unchanged_without_an_optional_ancestor()
        => Assert.Contains("""{ "$match" : { "Main.Req" : { "$lt" : "R2" } } }""", Pipeline(q => q.Where(p => p.Main.Req.CompareTo("R2") < 0)));

    // ── (c) Leaf-to-leaf comparisons through an optional parent ──────────────────────────────────────────────────

    [Fact]
    public void Leaf_to_leaf_equality_under_an_optional_complex_parent()
    {
        // n1/n3 compare null with Main.Text null; n2 X == X; n4's absent Opt reads null, Main.Text is M. RED (5547cfa2):
        // [n2, n3] in every mode (raw {$eq: ["$Opt.Text", "$Main.Text"]}: n1's MISSING Opt.Text is not null).
        Serves(People(q => q.Where(p => p.Opt!.Text == p.Main.Text)), ["n1", "n2", "n3"], ["n2", "n3"]);
        Serves(People(q => q.Where(p => p.Opt!.Text != p.Main.Text)), ["n4"], ["n1", "n4"]);
        Serves(People(q => q.Where(p => p.Main.Text == p.Opt!.Text)), ["n1", "n2", "n3"], ["n2", "n3"]);
        // Both sides possibly missing: n4's Nick is missing and its Opt too.
        Serves(People(q => q.Where(p => p.Opt!.Text == p.Nick)), ["n4"], ["n4"]);
    }

    [Fact]
    public void Leaf_to_leaf_relational_under_an_optional_complex_parent()
    {
        // n3: 0 < 3. n1/n4 read Opt.Num null (no comparison holds); n2: 5 < 2 is false. RED: [n1, n3, n4] in every mode
        // (MISSING orders below every value in $expr and a non-nullable int got no null guard); DriverLinq keeps them.
        Serves(People(q => q.Where(p => p.Opt!.Num < p.Main.Num)), ["n3"], ["n1", "n3", "n4"]);
        Serves(People(q => q.Where(p => p.Main.Num > p.Opt!.Num)), ["n3"], ["n1", "n3", "n4"]);
        Serves(People(q => q.Where(p => p.Opt!.Num >= p.Main.Num)), ["n2"], ["n2"]);
    }
}
#endif
