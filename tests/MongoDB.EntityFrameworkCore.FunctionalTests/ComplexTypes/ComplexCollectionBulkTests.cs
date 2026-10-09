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

// Complex collections exist on EF10 only.
#if !EF8 && !EF9
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// <c>ExecuteUpdate</c>/<c>ExecuteDelete</c> over complex COLLECTIONS. The filter and every <c>SetProperty</c> value run on
/// the driver-LINQ bridge, which evaluates a NULL element's members as MISSING (ordered below every value, never equal to
/// null) where a query reads them as null (ruling R17): a relational comparison over an element leaf selected and DELETED
/// rows the query excludes (measured at 6740680c, see task-14-report). Bulk operations therefore admit only element
/// predicates built from atoms the driver answers exactly as R17 (equality with a non-null value, a bare bool member, a
/// non-nullable list's <c>Contains</c>, and <c>&amp;&amp;</c>/<c>||</c>/<c>!</c> over them) and refuse everything else before
/// any document is written. Every row asserts the RAW stored documents and the affected count.
/// </summary>
[XUnitCollection("UpdateTests")]
public class ComplexCollectionBulkTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private const string Refusal = "ExecuteUpdate and ExecuteDelete run their filter and SetProperty values on driver-LINQ";

    public class Tag
    {
        public string Label { get; set; } = null!;
    }

    public class Stop
    {
        public string City { get; set; } = null!;
        public string? Note { get; set; }
        public int Floor { get; set; }
        public int? Zip { get; set; }
        public int Code { get; set; }
        public DateTime When { get; set; }
        public bool Verified { get; set; }
        public GeoPoint Location { get; set; }
        public List<Tag> Tags { get; set; } = [];
    }

    public class Route
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public int? NullRoot { get; set; }
        public List<Stop> Stops { get; set; } = [];
        public List<Stop>? Detours { get; set; }
    }

    private static void Configure(ModelBuilder mb)
        => mb.Entity<Route>(e =>
        {
            e.ComplexCollection(r => r.Stops, s =>
            {
                s.Property(x => x.City).Metadata.SetElementName("city");
                s.Property(x => x.Code).HasConversion<string>();
                s.ComplexProperty(x => x.Location);
                s.ComplexCollection(x => x.Tags);
            });
            e.ComplexCollection(r => r.Detours, s =>
            {
                s.ComplexProperty(x => x.Location);
                s.ComplexCollection(x => x.Tags);
            });
        });

    private static readonly DateTime May = new(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BsonDocument StopDoc(string cityElement = "city")
        => new()
        {
            { cityElement, "Oslo" }, { "Note", "n" }, { "Floor", 5 }, { "Zip", 5 }, { "Code", "5" }, { "When", May.AddDays(5) },
            { "Verified", true }, { "Location", new BsonDocument { { "Lat", 5.0 }, { "Lon", 0.0 } } },
            { "Tags", new BsonArray { new BsonDocument("Label", "x") } }
        };

    /// <summary>
    /// Stops: r-null [null]; r-mixed [Oslo(5, verified), null]; r-full [Oslo]; r-empty []; r-nullarr BSON null; r-missing
    /// (no element); r-emptyelem [{}] (a present element whose members are all missing). Detours (OPTIONAL): r-full [Oslo],
    /// r-null [null], r-nullarr null, every other row missing.
    /// </summary>
    private static BsonDocument[] SeedDocs()
    {
        BsonDocument Row(string name, int rank, BsonValue? stops, BsonValue? detours = null)
        {
            var d = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", name }, { "Rank", rank }, { "NullRoot", BsonNull.Value } };
            if (stops is not null)
            {
                d["Stops"] = stops;
            }

            if (detours is not null)
            {
                d["Detours"] = detours;
            }

            return d;
        }

        return
        [
            Row("r-null", 1, new BsonArray { BsonNull.Value }, new BsonArray { BsonNull.Value }),
            Row("r-mixed", 2, new BsonArray { StopDoc(), BsonNull.Value }),
            Row("r-full", 3, new BsonArray { StopDoc() }, new BsonArray { StopDoc("City") }),
            Row("r-empty", 4, new BsonArray()),
            Row("r-nullarr", 5, BsonNull.Value, BsonNull.Value),
            Row("r-missing", 6, null),
            Row("r-emptyelem", 7, new BsonArray { new BsonDocument() })
        ];
    }

    private static readonly string[] AllRows = ["r-empty", "r-emptyelem", "r-full", "r-missing", "r-mixed", "r-null", "r-nullarr"];

    private sealed class Store(IMongoCollection<Route> collection, BsonDocument[] seed)
    {
        public BsonDocument[] Seed { get; } = seed;

        public IMongoCollection<BsonDocument> Raw { get; }
            = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

        public SingleEntityDbContext<Route> Context(MongoQueryMode mode = MongoQueryMode.Native)
            => SingleEntityDbContext.Create(collection, Configure, null, b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

        public List<BsonDocument> Stored() => Raw.Find(FilterDefinition<BsonDocument>.Empty).ToList().OrderBy(d => d["_id"]).ToList();

        /// <summary>Every stored document equals its seed byte for byte (nothing written, nothing deleted).</summary>
        public void AssertUnchanged()
            => Assert.Equal(Seed.OrderBy(d => d["_id"]).Select(d => d.ToJson()), Stored().Select(d => d.ToJson()));

        public string[] Names() => [.. Stored().Select(d => d["Name"].AsString).Order(StringComparer.Ordinal)];
    }

    private Store Seed([CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<Route>(name + Guid.NewGuid().ToString("N")[..6]);
        var seed = SeedDocs();
        collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName).InsertMany(seed.Select(d => d.DeepClone().AsBsonDocument));
        return new Store(collection, seed);
    }

    private static string[] QueryRows(Store store, Expression<Func<Route, bool>> predicate, MongoQueryMode mode = MongoQueryMode.NativeOnly)
    {
        using var db = store.Context(mode);
        return [.. db.Entities.AsNoTracking().Where(predicate).Select(r => r.Name).ToList().Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// A supported shape: the query (NativeOnly) returns the hand-written rows, and ExecuteUpdate and ExecuteDelete each
    /// select exactly those rows (affected count + raw documents; untouched documents byte-identical).
    /// </summary>
    private void Supported(Expression<Func<Route, bool>> predicate, string[] expected, [CallerMemberName] string name = "")
        => Supported(predicate, expected, MongoQueryMode.NativeOnly, name);

    private void Supported(Expression<Func<Route, bool>> predicate, string[] expected, MongoQueryMode queryMode, string name)
    {
        Assert.Equal(expected, QueryRows(Seed(name), predicate, queryMode));

        var update = Seed(name);
        using (var db = update.Context())
        {
            Assert.Equal(expected.Length, db.Entities.Where(predicate).ExecuteUpdate(s => s.SetProperty(r => r.Rank, -1)));
        }

        foreach (var (stored, seeded) in update.Stored().Zip(update.Seed.OrderBy(d => d["_id"])))
        {
            var copy = seeded.DeepClone().AsBsonDocument;
            if (expected.Contains(copy["Name"].AsString))
            {
                copy["Rank"] = -1;
            }

            Assert.Equal(copy.ToJson(), stored.ToJson());
        }

        var delete = Seed(name);
        using (var db = delete.Context())
        {
            Assert.Equal(expected.Length, db.Entities.Where(predicate).ExecuteDelete());
        }

        Assert.Equal(AllRows.Except(expected).ToArray(), delete.Names());
    }

    /// <summary>
    /// A refused shape: the query (NativeOnly) returns <paramref name="queryRows"/> (C#'s answer), and ExecuteDelete,
    /// ExecuteUpdate and the two-phase (ordered/paged) forms are refused with the bulk message before any write, leaving
    /// every document byte-identical.
    /// </summary>
    private void Refused(Expression<Func<Route, bool>> predicate, string[]? queryRows, string shape, [CallerMemberName] string name = "")
    {
        var store = Seed(name);
        if (queryRows is not null)
        {
            Assert.Equal(queryRows, QueryRows(store, predicate));
        }

        using (var db = store.Context())
        {
            AssertRefused(() => db.Entities.Where(predicate).ExecuteDelete(), shape);
            AssertRefused(() => db.Entities.Where(predicate).ExecuteUpdate(s => s.SetProperty(r => r.Rank, -1)), shape);
            AssertRefused(() => db.Entities.Where(predicate).OrderBy(r => r.Name).Take(10).ExecuteDelete(), shape);
            AssertRefused(() => db.Entities.Where(predicate).OrderBy(r => r.Name).Skip(0).ExecuteUpdate(s => s.SetProperty(r => r.Rank, -1)), shape);
        }

        store.AssertUnchanged();
    }

    private static BsonValue Canonical(BsonValue value)
        => value switch
        {
            BsonDocument d => new BsonDocument(d.Elements.OrderBy(e => e.Name, StringComparer.Ordinal).Select(e => new BsonElement(e.Name, Canonical(e.Value)))),
            BsonArray a => new BsonArray(a.Select(Canonical)),
            _ => value
        };

    private static void AssertRefused(Func<int> operation, string shape)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => operation());
        Assert.Contains("could not be translated", ex.Message);
        var refusal = Assert.IsType<NativeTranslationNotSupportedException>(ex.InnerException);
        Assert.Contains(Refusal, refusal.Message);
        Assert.Contains(shape, refusal.Message);
        Assert.Contains("No document was modified", refusal.Message);
    }

    // ── A. The data-loss shapes: refused (measured RED at 6740680c: the bulk operation deleted/updated the rows in the
    //    comment; the query's rows are asserted beside the refusal) ─────────────────────────────────────────────────

    [Fact]
    public void Relational_comparisons_over_element_leaves_are_refused()
    {
        // RED: ExecuteDelete deleted [r-emptyelem, r-mixed, r-null] (missing Floor < 1 on the server); the query returns [].
        Refused(r => r.Stops.Any(s => s.Floor < 1), [], "the element predicate");
        // RED: deleted all 7; the query keeps r-emptyelem, r-mixed, r-null (a null element fails `< 9`).
        Refused(r => r.Stops.All(s => s.Floor < 9), ["r-empty", "r-full", "r-missing", "r-nullarr"], "the element predicate");
        // Rows would agree here (missing > 1 is false on both paths), but `>` is still refused: the allow-list has no
        // relational atom (cost of a positive list; see report).
        Refused(r => r.Stops.Any(s => s.Floor > 1), ["r-full", "r-mixed"], "the element predicate");
        Refused(r => r.Stops.Any(s => !(s.Floor > 9)), ["r-emptyelem", "r-full", "r-mixed", "r-null"], "the element predicate");
        Refused(r => r.Stops.Any(s => s.Location.Lat < 1), [], "the element predicate");
    }

    [Fact]
    public void Null_tests_over_element_leaves_are_refused()
    {
        int? none = null;
        // RED: `!= null` deleted [r-emptyelem, r-full, r-mixed, r-null]; the query returns [r-full, r-mixed].
        Refused(r => r.Stops.Any(s => s.Zip != null), ["r-full", "r-mixed"], "the element predicate");
        // RED: `== null` deleted nothing; the query returns [r-emptyelem, r-mixed, r-null] (no data loss, wrong rows).
        Refused(r => r.Stops.Any(s => s.Zip == null), ["r-emptyelem", "r-mixed", "r-null"], "the element predicate");
        Refused(r => r.Stops.Any(s => s.Note == null), ["r-emptyelem", "r-mixed", "r-null"], "the element predicate");
        // The query net's documented limits (R21) are refused here: other null-test spellings, a null parameter, a null
        // root member as the comparand.
        Refused(r => r.Stops.Any(s => s.Zip.HasValue), ["r-full", "r-mixed"], "the element predicate");
        Refused(r => r.Stops.Any(s => s.Zip == none), ["r-emptyelem", "r-mixed", "r-null"], "the element predicate");
        Refused(r => r.Stops.Any(s => string.IsNullOrEmpty(s.Note)), ["r-emptyelem", "r-mixed", "r-null"], "the element predicate");
        Refused(r => r.Stops.Any(s => s.Zip == r.NullRoot), ["r-emptyelem", "r-mixed", "r-null"], "the element predicate");
    }

    [Fact]
    public void Bool_equalities_local_lists_with_null_and_date_arithmetic_are_refused()
    {
        var no = false;
        var withNull = new int?[] { null, 7 };
        // R19 reads a null element's bool as false; the driver's `$eq: [missing, false]` is false. RED: selected nothing.
        Refused(r => r.Stops.Any(s => s.Verified == false), ["r-emptyelem", "r-mixed", "r-null"], "the element predicate");
        Refused(r => r.Stops.Any(s => s.Verified == no), ["r-emptyelem", "r-mixed", "r-null"], "the element predicate");
        // `$in` of a missing value never matches null. RED: `!Contains` deleted [r-emptyelem, r-full, r-mixed, r-null].
        Refused(r => r.Stops.Any(s => withNull.Contains(s.Zip)), ["r-emptyelem", "r-mixed", "r-null"], "the element predicate");
        Refused(r => r.Stops.Any(s => !withNull.Contains(s.Zip)), ["r-full", "r-mixed"], "the element predicate");
        // RED: deleted [r-emptyelem, r-mixed, r-null].
        Refused(r => r.Stops.Any(s => s.When.AddDays(1) < May), [], "the element predicate");
        Refused(r => r.Stops.Any(s => s.When.Year < 2030), ["r-full", "r-mixed"], "the element predicate");
    }

    [Fact]
    public void Counts_with_a_null_guard_requiring_predicate_are_refused()
    {
        // RED: deleted [r-emptyelem, r-mixed, r-null].
        Refused(r => r.Stops.Count(s => s.Floor < 1) > 0, [], "the element predicate");
        Refused(r => r.Stops.Count(s => !(s.Zip > 0)) > 0, ["r-emptyelem", "r-mixed", "r-null"], "the element predicate");
        Refused(r => r.Stops.LongCount(s => s.Zip != null) == 1, ["r-full", "r-mixed"], "the element predicate");
    }

    [Fact]
    public void Shapes_outside_the_allow_list_are_refused()
    {
        // The R21 limit shapes of the query net, and every operator outside the allow-list: element-leaf Select chains,
        // nested element lambdas, indexers/First/aggregates, string methods, nested collections inside an element.
        // (EF folds the Select into the predicate before the provider sees it: `Any(s => s.Floor < 1)`.)
        Refused(r => r.Stops.Select(s => s.Floor).Any(f => f < 1), [], "the element predicate");
        // (These two have no native query translation; `null` = the query declines, the rows aren't compared.)
        Refused(r => r.Stops.Any(s => r.Stops.Select(s2 => s.Floor).First() < 1), null, "the element predicate");
        Refused(r => r.Stops.Max(s => s.Floor) < 1, null, "the operator 'Max'");
        Refused(r => r.Stops.Any(s => s.City.StartsWith("O")), ["r-full", "r-mixed"], "the element predicate");
        Refused(r => r.Stops.Any(s => s.Tags.Any(t => t.Label == "x")), ["r-full", "r-mixed"], "the element predicate");
        // Member vs member: a null element (and {}) reads both as null, `null == null` is true (C#); Oslo has City != Note.
        Refused(r => r.Stops.Any(s => s.City == s.Note), ["r-emptyelem", "r-mixed", "r-null"], "the element predicate");
    }

    [Fact]
    public void Shapes_the_query_declines_are_refused_too()
    {
        // `Stops[0]`, `First()` (no native translation; the query falls back): refused for bulk as reads of the collection
        // outside an allowed operator.
        var store = Seed();
        using (var db = store.Context())
        {
            // (EF lowers `Stops[0].Floor` and `First().Floor` to a Select over the collection before the provider sees them.)
            AssertRefused(() => db.Entities.Where(r => r.Stops[0].Floor < 1).ExecuteDelete(), "the operator 'Select'");
            AssertRefused(() => db.Entities.Where(r => r.Stops.First().Floor < 1).ExecuteDelete(), "the operator 'Select'");
            AssertRefused(() => db.Entities.Where(r => r.Stops.Select(s => s.Verified).Contains(false)).ExecuteDelete(), "the operator 'Select'");
        }

        store.AssertUnchanged();
    }

    [Fact]
    public void Operators_over_an_optional_complex_collection_are_refused()
    {
        // The bridge does not normalize a null OPTIONAL array, so `$anyElementTrue`/`$size` over r-nullarr's null Detours is
        // a server error (measured at 6740680c: "$anyElementTrue's argument must be an array" mid-operation). Refused up
        // front; the optional collection's own null check is supported (below).
        var store = Seed();
        using (var db = store.Context())
        {
            AssertRefused(() => db.Entities.Where(r => r.Detours!.Any(s => s.City == "Oslo")).ExecuteDelete(), "over an optional complex collection");
            // (EF rewrites `Count > 0` to `Any()` and `Count` to the Count operator.)
            AssertRefused(() => db.Entities.Where(r => r.Detours!.Count > 0).ExecuteDelete(), "'Any' over an optional complex collection");
            AssertRefused(() => db.Entities.Where(r => r.Detours!.Count == 1).ExecuteDelete(), "over an optional complex collection");
            AssertRefused(() => db.Entities.Where(r => r.Detours!.Any()).ExecuteUpdate(s => s.SetProperty(r => r.Rank, 0)), "over an optional complex collection");
        }

        store.AssertUnchanged();
    }

    [Fact]
    public void Setter_values_that_read_elements_are_checked_like_filters()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            AssertRefused(() => db.Entities.ExecuteUpdate(s => s.SetProperty(r => r.Rank, r => r.Stops.Count(st => st.Floor < 1))), "the element predicate");
            AssertRefused(() => db.Entities.ExecuteUpdate(s => s.SetProperty(r => r.Rank, r => r.Stops.Max(st => st.Floor))), "the operator 'Max'");
        }

        store.AssertUnchanged();
    }

    [Fact]
    public void Explicit_DriverLinq_mode_runs_the_driver_evaluation()
    {
        // The documented opt-out: the user asked for driver-LINQ semantics (the matching DriverLinq query returns the same
        // rows), so the bulk operation runs them: the null-element rows ARE deleted.
        var store = Seed();
        Assert.Equal(["r-emptyelem", "r-mixed", "r-null"], QueryRows(store, r => r.Stops.Any(s => s.Floor < 1), MongoQueryMode.DriverLinq));
        using (var db = store.Context(MongoQueryMode.DriverLinq))
        {
            Assert.Equal(3, db.Entities.Where(r => r.Stops.Any(s => s.Floor < 1)).ExecuteDelete());
        }

        Assert.Equal(["r-empty", "r-full", "r-missing", "r-nullarr"], store.Names());
    }

    [Fact]
    public void A_parameter_is_checked_per_execution()
    {
        // One compiled shape, run twice in one context: a comparand that is non-null at run time is admitted, a null one
        // refused (the equality atom needs a non-null value).
        var store = Seed();
        using (var db = store.Context())
        {
            string? city = "Oslo";
            Assert.Equal(2, db.Entities.Where(r => r.Stops.Any(s => s.City == city)).ExecuteUpdate(s => s.SetProperty(r => r.Rank, 0)));
            city = null;
            AssertRefused(() => db.Entities.Where(r => r.Stops.Any(s => s.City == city)).ExecuteUpdate(s => s.SetProperty(r => r.Rank, 9)), "the element predicate");

            var list = new int?[] { 5, 7 };
            Assert.Equal(2, db.Entities.Where(r => r.Stops.Any(s => list.Contains(s.Zip))).ExecuteUpdate(s => s.SetProperty(r => r.Rank, 1)));
            list = [null, 7];
            AssertRefused(() => db.Entities.Where(r => r.Stops.Any(s => list.Contains(s.Zip))).ExecuteUpdate(s => s.SetProperty(r => r.Rank, 9)), "the element predicate");
        }

        Assert.Equal(["r-empty:4", "r-emptyelem:7", "r-full:1", "r-missing:6", "r-mixed:1", "r-null:1", "r-nullarr:5"],
            store.Stored().Select(d => $"{d["Name"]}:{d["Rank"]}").Order(StringComparer.Ordinal));
    }

    // ── B. Supported element predicates: bulk selects exactly the query's rows ─────────────────────────────────────

    [Fact]
    public void Equality_with_a_non_null_value()
    {
        // `city` is stored as "city" (HasElementName); Code through a string converter; Location a nested struct.
        Supported(r => r.Stops.Any(s => s.City == "Oslo"), ["r-full", "r-mixed"]);
        Supported(r => r.Stops.Any(s => s.City != "Oslo"), ["r-emptyelem", "r-mixed", "r-null"]);
        Supported(r => r.Stops.All(s => s.City == "Oslo"), ["r-empty", "r-full", "r-missing", "r-nullarr"]);
        Supported(r => !r.Stops.All(s => s.City == "Oslo"), ["r-emptyelem", "r-mixed", "r-null"]);
        Supported(r => r.Stops.Any(s => s.Zip == 5), ["r-full", "r-mixed"]);
        Supported(r => r.Stops.Any(s => s.Zip != 5), ["r-emptyelem", "r-mixed", "r-null"]);
        Supported(r => r.Stops.Any(s => s.Code == 5), ["r-full", "r-mixed"]);
        Supported(r => r.Stops.Any(s => s.Location.Lat == 5.0), ["r-full", "r-mixed"]);
        Supported(r => r.Stops.Any(s => s.When == May.AddDays(5)), ["r-full", "r-mixed"]);
        Supported(r => r.Stops.Any(s => s.Verified == true), ["r-full", "r-mixed"]);
        Supported(r => r.Stops.Any(s => s.Verified != true), ["r-emptyelem", "r-mixed", "r-null"]);
    }

    [Fact]
    public void Bool_members_lists_and_combinations()
    {
        var floors = new[] { 5, 7 };
        Supported(r => r.Stops.Any(s => s.Verified), ["r-full", "r-mixed"]);
        Supported(r => r.Stops.Any(s => !s.Verified), ["r-emptyelem", "r-mixed", "r-null"]);
        Supported(r => r.Stops.Any(s => floors.Contains(s.Floor)), ["r-full", "r-mixed"]);
        Supported(r => r.Stops.Any(s => !floors.Contains(s.Floor)), ["r-emptyelem", "r-mixed", "r-null"]);
        Supported(r => r.Stops.Any(s => s.Verified && s.City == "Oslo"), ["r-full", "r-mixed"]);
        Supported(r => r.Stops.Any(s => !(s.City == "Oslo" || s.Verified)), ["r-emptyelem", "r-mixed", "r-null"]);
    }

    [Fact]
    public void Counts_quantifiers_and_null_or_missing_arrays()
    {
        // A required collection that is null or missing reads EMPTY on both paths (the bridge's coalesce to [], Task 13).
        Supported(r => r.Stops.Any(), ["r-emptyelem", "r-full", "r-mixed", "r-null"]);
        Supported(r => !r.Stops.Any(), ["r-empty", "r-missing", "r-nullarr"]);
        Supported(r => r.Stops.Count == 0, ["r-empty", "r-missing", "r-nullarr"]);
        Supported(r => r.Stops.Count > 1, ["r-mixed"]);
        Supported(r => r.Stops.Count() > 1, ["r-mixed"]);
        Supported(r => r.Stops.Count(s => s.City == "Oslo") >= 1, ["r-full", "r-mixed"]);
        Supported(r => r.Stops.Count == 0 || r.Stops.Any(s => s.City == "Oslo"), ["r-empty", "r-full", "r-missing", "r-mixed", "r-nullarr"]);
        Supported(r => r.Stops.Any(s => s.City == "Oslo") && r.Rank > 2, ["r-full"]);
    }

    [Fact]
    public void Optional_collection_null_checks_are_refused()
    {
        // FOUND IN THIS TASK: the driver renders `r.Detours == null` as `{Detours: null}`, which ALSO matches an array that
        // CONTAINS a null element: r-null's Detours [null] is selected though it is a non-null collection (C#: the rows
        // whose Detours is null or missing). The query's Native-mode fallback serves the same wrong row (pinned here; a
        // pre-existing query gap, Jira candidate); a bulk operation refuses it.
        var store = Seed();
        Assert.Equal(["r-empty", "r-emptyelem", "r-missing", "r-mixed", "r-null", "r-nullarr"], QueryRows(store, r => r.Detours == null, MongoQueryMode.Native));
        Assert.Equal(["r-full"], QueryRows(store, r => r.Detours != null, MongoQueryMode.Native));
        using (var db = store.Context())
        {
            AssertRefused(() => db.Entities.Where(r => r.Detours == null).ExecuteDelete(), "a read of the collection");
            AssertRefused(() => db.Entities.Where(r => r.Detours != null).ExecuteUpdate(s => s.SetProperty(r => r.Rank, 0)), "a read of the collection");
        }

        store.AssertUnchanged();
    }

    [Fact]
    public void A_setter_value_counting_elements_with_a_supported_predicate()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            Assert.Equal(7, db.Entities.ExecuteUpdate(s => s.SetProperty(r => r.Rank, r => r.Stops.Count(st => st.City == "Oslo") * 10 + r.Stops.Count)));
        }

        // Hand-written: Oslo stops x10 + stops (a null element is not Oslo; null/missing arrays count 0). (The equivalent
        // count projection is not translatable as a query: pre-existing, Task 13.)
        Assert.Equal(["r-empty:0", "r-emptyelem:1", "r-full:11", "r-missing:0", "r-mixed:12", "r-null:1", "r-nullarr:0"],
            store.Stored().Select(d => $"{d["Name"]}:{d["Rank"]}").Order(StringComparer.Ordinal));
    }

    // ── Setting a complex collection ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetProperty_on_a_whole_collection_to_a_captured_list_and_to_an_empty_list()
    {
        var store = Seed();
        var stops = new List<Stop>
        {
            new() { City = "Rome", Note = null, Floor = 1, Zip = null, Code = 9, When = May, Verified = false, Location = new GeoPoint { Lat = 1, Lon = 2 }, Tags = [new Tag { Label = "t" }] }
        };
        using (var db = store.Context())
        {
            Assert.Equal(1, db.Entities.Where(r => r.Name == "r-full").ExecuteUpdate(s => s.SetProperty(r => r.Stops, stops)));
            Assert.Equal(2, db.Entities.Where(r => r.Rank == 1 || r.Rank == 2).ExecuteUpdate(s => s.SetProperty(r => r.Stops, new List<Stop>())));
            Assert.Equal(1, db.Entities.Where(r => r.Name == "r-full").ExecuteUpdate(s => s.SetProperty(r => r.Detours, (List<Stop>?)null)));
        }

        var stored = store.Stored().ToDictionary(d => d["Name"].AsString);
        // Element names, converter and nested complex values exactly as SaveChanges writes them (compared with element
        // names sorted: order inside a subdocument is not semantic).
        Assert.Equal(
            """[{ "Code" : "9", "Floor" : 1, "Location" : { "Lat" : 1.0, "Lon" : 2.0 }, "Note" : null, "Tags" : [{ "Label" : "t" }], "Verified" : false, "When" : { "$date" : "2024-05-01T00:00:00Z" }, "Zip" : null, "city" : "Rome" }]""",
            Canonical(stored["r-full"]["Stops"]).ToJson());
        Assert.Equal(BsonNull.Value, stored["r-full"]["Detours"]);
        Assert.Equal(new BsonArray(), stored["r-null"]["Stops"]);
        Assert.Equal(new BsonArray(), stored["r-mixed"]["Stops"]);

        using var read = store.Context(MongoQueryMode.NativeOnly);
        Assert.Equal(["Rome/9/t"], read.Entities.AsNoTracking().Where(r => r.Name == "r-full").ToList().Single().Stops.Select(s => $"{s.City}/{s.Code}/{s.Tags.Single().Label}"));
    }

    [Fact]
    public void SetProperty_on_a_required_collection_to_null_is_refused()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(r => r.Stops, (List<Stop>)null!)));
            Assert.Contains("cannot set the required complex property", ex.Message);
        }

        store.AssertUnchanged();
    }

    [Fact]
    public void SetProperty_on_an_element_leaf_is_refused()
    {
        // An element of a collection has no single stored path; the selector is not a member chain to a complex leaf.
        var store = Seed();
        using (var db = store.Context())
        {
            var indexed = Assert.Throws<InvalidOperationException>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(r => r.Stops[0].City, "x")));
            Assert.Contains("could not be translated", indexed.Message);
            var first = Assert.Throws<InvalidOperationException>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(r => r.Stops.First().City, "x")));
            Assert.Contains("could not be translated", first.Message);
        }

        store.AssertUnchanged();
    }

    [Fact]
    public void SetProperty_on_a_whole_collection_from_the_row_is_refused()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(r => r.Detours, r => r.Stops)));
            Assert.Contains("from a value that reads the entity being updated", ex.Message);
        }

        store.AssertUnchanged();
    }
    // ── Navigation root (a documented R21 limit of the query net) ───────────────────────────────────────────────────

    public class Shop
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Tag> Tags { get; set; } = [];
    }

    public class Sale
    {
        public ObjectId Id { get; set; }
        public ObjectId ShopId { get; set; }
        public Shop Shop { get; set; } = null!;
        public int Amount { get; set; }
    }

    private sealed class ShopContext(DbContextOptions options, string shops, string sales) : DbContext(options)
    {
        public DbSet<Shop> Shops => Set<Shop>();
        public DbSet<Sale> Sales => Set<Sale>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Shop>(b =>
            {
                b.ToCollection(shops);
                b.ComplexCollection(s => s.Tags);
            });
            mb.Entity<Sale>(b =>
            {
                b.ToCollection(sales);
                b.HasOne(s => s.Shop).WithMany().HasForeignKey(s => s.ShopId);
            });
        }
    }

    [Fact]
    public void Element_predicates_through_a_navigation_are_refused()
    {
        // The query net keys lambdas by root entity type, so `s.Shop.Tags.Any(...)` is a known R21 limit for queries. Bulk
        // refuses any lambda over complex elements it can't bind, and a cross-collection bulk source is refused anyway
        // (pre-existing: single-collection bulk only). Either way nothing is written.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var shops = "bulkshops" + suffix;
        var sales = "bulksales" + suffix;
        var shop = ObjectId.GenerateNewId();
        database.MongoDatabase.GetCollection<BsonDocument>(shops).InsertOne(
            new BsonDocument { { "_id", shop }, { "Name", "s" }, { "Tags", new BsonArray { BsonNull.Value } } });
        var sale = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "ShopId", shop }, { "Amount", 9 } };
        var rawSales = database.MongoDatabase.GetCollection<BsonDocument>(sales);
        rawSales.InsertOne(sale.DeepClone().AsBsonDocument);

        var builder = new DbContextOptionsBuilder<ShopContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        using (var db = new ShopContext(builder.Options, shops, sales))
        {
            // C#: the shop's only tag is null, so `Label != null` is false: the sale must not be deleted.
            var ex = Assert.Throws<InvalidOperationException>(() => db.Sales.Where(s => s.Shop.Tags.Any(t => t.Label != null)).ExecuteDelete());
            Assert.Contains("could not be translated", ex.Message);
            ex = Assert.Throws<InvalidOperationException>(() => db.Sales.Where(s => s.Shop.Tags.Any(t => t.Label == "x")).ExecuteUpdate(u => u.SetProperty(x => x.Amount, 0)));
            Assert.Contains("could not be translated", ex.Message);
        }

        Assert.Equal(sale.ToJson(), rawSales.Find(FilterDefinition<BsonDocument>.Empty).Single().ToJson());
    }
    // ── Fix round 1: collection CLR types the bridge cannot normalise (I1) ───────────────────────────────────────────

    public class Crate
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public Tag[] Items { get; set; } = [];
    }

    public class ObservableCrate
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public System.Collections.ObjectModel.ObservableCollection<Tag> Items { get; set; } = [];
    }

    public class IListCrate
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public IList<Tag> Items { get; set; } = [];
    }

    // The first scanned document matches, a LATER one has a null array: measured at 5eece0ae, ExecuteUpdate over a T[] /
    // ObservableCollection<T> wrote a-match (Rank 1) and then failed on b-null ("$anyElementTrue's argument must be an
    // array, but is null"): a partial operation. IList<T> is normalised by the bridge and correct.
    private static BsonDocument[] CrateSeed() =>
    [
        new() { { "_id", ObjectId.GenerateNewId() }, { "Name", "a-match" }, { "Rank", 0 }, { "Items", new BsonArray { new BsonDocument("Label", "l") } } },
        new() { { "_id", ObjectId.GenerateNewId() }, { "Name", "b-null" }, { "Rank", 0 }, { "Items", BsonNull.Value } },
        new() { { "_id", ObjectId.GenerateNewId() }, { "Name", "c-match" }, { "Rank", 0 }, { "Items", new BsonArray { new BsonDocument("Label", "l") } } }
    ];

    private (IMongoCollection<BsonDocument> Raw, SingleEntityDbContext<T> Db, BsonDocument[] Seed) Crates<T>(string name, Action<ModelBuilder> configure)
        where T : class
    {
        var collection = database.CreateCollection<T>(name + Guid.NewGuid().ToString("N")[..6]);
        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        var seed = CrateSeed();
        raw.InsertMany(seed.Select(d => d.DeepClone().AsBsonDocument));
        var db = SingleEntityDbContext.Create(collection, configure, null,
            b => b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));
        return (raw, db, seed);
    }

    private static void AssertSeedUnchanged(IMongoCollection<BsonDocument> raw, BsonDocument[] seed)
        => Assert.Equal(seed.OrderBy(d => d["_id"]).Select(d => d.ToJson()),
            raw.Find(FilterDefinition<BsonDocument>.Empty).ToList().OrderBy(d => d["_id"]).Select(d => d.ToJson()));

    [Fact]
    public void Operators_over_an_array_typed_collection_are_refused_and_write_nothing()
    {
        var (raw, db, seed) = Crates<Crate>(nameof(Operators_over_an_array_typed_collection_are_refused_and_write_nothing),
            mb => mb.Entity<Crate>().ComplexCollection(c => c.Items));
        using (db)
        {
            const string shape = "whose CLR type cannot hold a List<T>";
            AssertRefused(() => db.Entities.Where(c => c.Items.Any(i => i.Label == "l")).ExecuteUpdate(s => s.SetProperty(c => c.Rank, 1)), shape);
            AssertRefused(() => db.Entities.Where(c => c.Items.Any(i => i.Label == "l")).ExecuteDelete(), shape);
            AssertRefused(() => db.Entities.Where(c => c.Items.Any(i => i.Label == "l")).OrderBy(c => c.Name).Take(5).ExecuteUpdate(s => s.SetProperty(c => c.Rank, 1)), shape);
            AssertRefused(() => db.Entities.Where(c => c.Items.Any(i => i.Label == "l")).OrderBy(c => c.Name).Take(5).ExecuteDelete(), shape);
            // `Count()` / `Length` (the `$size` form): EF lowers both to the Count operator.
            AssertRefused(() => db.Entities.Where(c => c.Items.Count() > 0).ExecuteUpdate(s => s.SetProperty(c => c.Rank, 1)), shape);
            AssertRefused(() => db.Entities.Where(c => c.Items.Length > 0).ExecuteDelete(), shape);
        }

        AssertSeedUnchanged(raw, seed);
    }

    [Fact]
    public void Operators_over_an_ObservableCollection_typed_collection_are_refused_and_write_nothing()
    {
        var (raw, db, seed) = Crates<ObservableCrate>(nameof(Operators_over_an_ObservableCollection_typed_collection_are_refused_and_write_nothing),
            mb => mb.Entity<ObservableCrate>().ComplexCollection(c => c.Items));
        using (db)
        {
            AssertRefused(() => db.Entities.Where(c => c.Items.Any(i => i.Label == "l")).ExecuteUpdate(s => s.SetProperty(c => c.Rank, 1)), "whose CLR type cannot hold a List<T>");
            AssertRefused(() => db.Entities.Where(c => c.Items.Count == 0).ExecuteDelete(), "whose CLR type cannot hold a List<T>");
        }

        AssertSeedUnchanged(raw, seed);
    }

    [Fact]
    public void An_IList_typed_collection_is_normalised_and_served()
    {
        var (raw, db, _) = Crates<IListCrate>(nameof(An_IList_typed_collection_is_normalised_and_served),
            mb => mb.Entity<IListCrate>().ComplexCollection(c => c.Items));
        using (db)
        {
            Assert.Equal(2, db.Entities.Where(c => c.Items.Any(i => i.Label == "l")).ExecuteUpdate(s => s.SetProperty(c => c.Rank, 1)));
            Assert.Equal(1, db.Entities.Where(c => c.Items.Count == 0).ExecuteDelete());
        }

        Assert.Equal(["a-match:1", "c-match:1"], raw.Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => $"{d["Name"]}:{d["Rank"]}").Order(StringComparer.Ordinal));
    }

    // ── Fix round 1: model-level query filters (M2) ───────────────────────────────────────────────────────────────

    private SingleEntityDbContext<Route> FilteredContext(Store store, Expression<Func<Route, bool>> filter, MongoQueryMode mode = MongoQueryMode.Native)
        => SingleEntityDbContext.Create(
            store.Raw.Database.GetCollection<Route>(store.Raw.CollectionNamespace.CollectionName),
            mb => { Configure(mb); mb.Entity<Route>().HasQueryFilter(filter); }, null, b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

    [Fact]
    public void A_query_filter_with_an_unsafe_element_predicate_is_refused_unless_ignored()
    {
        // EF applies the model filter to the bulk source before the provider translates it, so the scanner sees it.
        var store = Seed();
        using (var db = FilteredContext(store, r => r.Stops.Any(s => s.Floor < 1)))
        {
            AssertRefused(() => db.Entities.ExecuteDelete(), "the element predicate");
            AssertRefused(() => db.Entities.Where(r => r.Rank > 0).ExecuteUpdate(s => s.SetProperty(r => r.Rank, -1)), "the element predicate");
            AssertRefused(() => db.Entities.OrderBy(r => r.Name).Take(3).ExecuteDelete(), "the element predicate");
        }

        store.AssertUnchanged();

        // IgnoreQueryFilters removes the filter: a plain update of every row.
        using (var db = FilteredContext(store, r => r.Stops.Any(s => s.Floor < 1)))
        {
            Assert.Equal(7, db.Entities.IgnoreQueryFilters().ExecuteUpdate(s => s.SetProperty(r => r.Rank, -1)));
        }

        Assert.All(store.Stored(), d => Assert.Equal(-1, d["Rank"].AsInt32));
    }

    [Fact]
    public void A_query_filter_with_a_safe_element_predicate_selects_the_query_rows()
    {
        var queryStore = Seed();
        using (var db = FilteredContext(queryStore, r => r.Stops.Any(s => s.City == "Oslo"), MongoQueryMode.NativeOnly))
        {
            Assert.Equal(["r-full", "r-mixed"], db.Entities.AsNoTracking().Select(r => r.Name).ToList().Order(StringComparer.Ordinal));
            Assert.Equal(AllRows, db.Entities.IgnoreQueryFilters().AsNoTracking().Select(r => r.Name).ToList().Order(StringComparer.Ordinal));
        }

        var store = Seed();
        using (var db = FilteredContext(store, r => r.Stops.Any(s => s.City == "Oslo")))
        {
            Assert.Equal(2, db.Entities.ExecuteUpdate(s => s.SetProperty(r => r.Rank, -1)));
            Assert.Equal(2, db.Entities.ExecuteDelete());
        }

        Assert.Equal(["r-empty", "r-emptyelem", "r-missing", "r-null", "r-nullarr"], store.Names());

        var ignored = Seed();
        using (var db = FilteredContext(ignored, r => r.Stops.Any(s => s.City == "Oslo")))
        {
            Assert.Equal(7, db.Entities.IgnoreQueryFilters().ExecuteDelete());
        }

        Assert.Empty(ignored.Names());
    }

    // ── Fix round 1: two-phase with an unsafe SETTER value (unverified check b) ───────────────────────────────────

    [Fact]
    public void A_two_phase_update_whose_setter_value_reads_elements_unsafely_writes_nothing()
    {
        // Two-phase: the executor calls BuildTargetIdQuery (which runs the check over filter AND setter values) inside the
        // transaction before reading any target id or writing; the refusal aborts before UpdateMany.
        var store = Seed();
        using (var db = store.Context())
        {
            AssertRefused(() => db.Entities.OrderBy(r => r.Name).Take(3)
                .ExecuteUpdate(s => s.SetProperty(r => r.Rank, r => r.Stops.Count(st => st.Floor < 1))), "the element predicate");
            AssertRefused(() => db.Entities.Where(r => r.Stops.Any(st => st.City == "Oslo")).OrderBy(r => r.Name).Skip(0)
                .ExecuteUpdate(s => s.SetProperty(r => r.Rank, r => r.Stops.Count(st => st.Zip != null))), "the element predicate");
        }

        store.AssertUnchanged();
    }

    // ── Fix round 1: a required collection under an OPTIONAL complex hop (M4) ─────────────────────────────────────

    public class Hop
    {
        public List<Stop> Stops { get; set; } = [];
    }

    public class HopRoute
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public Hop? Opt { get; set; }
    }

    [Fact]
    public void A_required_collection_under_an_optional_hop_is_served_when_the_predicate_is_safe()
    {
        // Admitted by the allow-list: the collection itself is REQUIRED (the bridge coalesces it to [], and under an absent
        // parent the driver reads the missing path as empty too), and the predicate is an allowed atom. Measured at 5eece0ae
        // and now: the query (NativeOnly) and both bulk operations select the same rows, including h-null (Opt BSON null),
        // h-missing (no Opt) and h-empty (Opt {}).
        BsonDocument Row(string name, BsonValue? opt)
        {
            var d = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", name }, { "Rank", 0 } };
            if (opt is not null)
            {
                d["Opt"] = opt;
            }

            return d;
        }

        BsonDocument Rome()
        {
            var rome = StopDoc();
            rome["city"] = "Rome";
            return rome;
        }

        BsonDocument[] Seed() =>
        [
            Row("h-val", new BsonDocument("Stops", new BsonArray { StopDoc(), BsonNull.Value })),
            Row("h-null", BsonNull.Value), Row("h-missing", null), Row("h-empty", new BsonDocument()),
            Row("h-rome", new BsonDocument("Stops", new BsonArray { Rome() }))
        ];

        void Check(Expression<Func<HopRoute, bool>> predicate, string[] expected)
        {
            IMongoCollection<HopRoute> New()
            {
                var collection = database.CreateCollection<HopRoute>("hop" + Guid.NewGuid().ToString("N")[..8]);
                collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName).InsertMany(Seed());
                return collection;
            }

            SingleEntityDbContext<HopRoute> Ctx(IMongoCollection<HopRoute> c, MongoQueryMode mode)
                => SingleEntityDbContext.Create(c, mb => mb.Entity<HopRoute>().ComplexProperty(r => r.Opt, o => o.ComplexCollection(x => x.Stops, st =>
                {
                    st.Property(x => x.City).Metadata.SetElementName("city");
                    st.Property(x => x.Code).HasConversion<string>();
                    st.ComplexProperty(x => x.Location);
                    st.ComplexCollection(x => x.Tags);
                })), null, b =>
                {
                    b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                    new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
                });

            using (var db = Ctx(New(), MongoQueryMode.NativeOnly))
            {
                Assert.Equal(expected, db.Entities.AsNoTracking().Where(predicate).Select(r => r.Name).ToList().Order(StringComparer.Ordinal));
            }

            var update = New();
            using (var db = Ctx(update, MongoQueryMode.Native))
            {
                Assert.Equal(expected.Length, db.Entities.Where(predicate).ExecuteUpdate(s => s.SetProperty(r => r.Rank, 1)));
            }

            var raw = update.Database.GetCollection<BsonDocument>(update.CollectionNamespace.CollectionName);
            Assert.Equal(expected, raw.Find(new BsonDocument("Rank", 1)).ToList().Select(d => d["Name"].AsString).Order(StringComparer.Ordinal));
            // Only Rank changed: every Opt (null, missing, {}, populated) is stored exactly as seeded.
            Assert.Equal(Seed().Select(d => d.Contains("Opt") ? d["Opt"].ToJson() : "<missing>").Order(StringComparer.Ordinal),
                raw.Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d.Contains("Opt") ? d["Opt"].ToJson() : "<missing>").Order(StringComparer.Ordinal));

            var delete = New();
            using (var db = Ctx(delete, MongoQueryMode.Native))
            {
                Assert.Equal(expected.Length, db.Entities.Where(predicate).ExecuteDelete());
            }

            Assert.Equal(new[] { "h-empty", "h-missing", "h-null", "h-rome", "h-val" }.Except(expected),
                delete.Database.GetCollection<BsonDocument>(delete.CollectionNamespace.CollectionName).Find(FilterDefinition<BsonDocument>.Empty).ToList()
                    .Select(d => d["Name"].AsString).Order(StringComparer.Ordinal));
        }

        Check(r => r.Opt!.Stops.Any(s => s.City == "Oslo"), ["h-val"]);
        Check(r => r.Opt!.Stops.Any(s => s.City != "Oslo"), ["h-rome", "h-val"]);
        Check(r => r.Opt!.Stops.Any(), ["h-rome", "h-val"]);
        Check(r => r.Opt!.Stops.Count() == 0, ["h-empty", "h-missing", "h-null"]);
    }
}
#endif
