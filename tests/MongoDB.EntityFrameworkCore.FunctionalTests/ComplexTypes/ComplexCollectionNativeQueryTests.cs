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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Metadata.Conventions;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Native queries over complex COLLECTIONS (EF10 only: <c>ComplexCollection</c> does not exist on EF8/EF9): element
/// quantifiers (<c>Any</c>/<c>All</c>), counts, membership (<c>Contains</c> of a whole element, member-wise), ordering by a
/// count, count projections, nested and correlated element predicates, and the shapes that decline to the driver-LINQ
/// fallback (element-leaf projections, indexers, element aggregates). Every row is pinned per mode against a hand-written
/// answer: each mode serves exactly those rows or fails with a named message.
/// </summary>
/// <remarks>
/// <para>
/// Stored shapes (seeded as raw BSON): an array of subdocuments; <c>[]</c>; BSON null; MISSING; a BSON null ELEMENT (EF10
/// writes one for a null element: measured, <c>[{...}, null]</c>); elements with unmapped extra fields (ignored); elements
/// whose members are missing or null.
/// </para>
/// <para>
/// C# answers: a REQUIRED collection that is null/missing reads EMPTY (the complex read rule), so <c>Any()</c> is false,
/// <c>All(...)</c> true, <c>Count</c> 0. A null element's members read as null (EF null propagation, as for an absent
/// complex parent, ruling R14), so it never satisfies a predicate on its members, and makes <c>All</c> false.
/// </para>
/// </remarks>
[XUnitCollection("QueryTests")]
public class ComplexCollectionNativeQueryTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    // The driver-LINQ path can't compare a whole complex value (the complex serializer refuses it, ruling R1).
    private const string WholeValueRefused = "as a whole value is not supported";
    private const string NotTranslated = "could not be translated";

    public class Tag
    {
        public string Label { get; set; } = null!;
    }

    public class Addr
    {
        public string City { get; set; } = null!;
        public string? Street { get; set; }
        public int? Zip { get; set; }
        public bool Verified { get; set; }
        public GeoPoint Location { get; set; }
        public List<Tag> Tags { get; set; } = [];
    }

    public class Customer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public string HomeCity { get; set; } = null!;
        public int Rank { get; set; }
        public List<Addr> Addresses { get; set; } = [];
    }

    private static void ConfigureCustomer(ModelBuilder mb)
        => mb.Entity<Customer>().ComplexCollection(c => c.Addresses, a =>
        {
            a.ComplexProperty(x => x.Location);
            a.ComplexCollection(x => x.Tags);
        });

    private static BsonDocument A(string city, double lat, int? zip = null, bool verified = false, params string[] tags)
        => new()
        {
            { "City", city }, { "Street", BsonNull.Value }, { "Zip", zip.HasValue ? zip.Value : BsonNull.Value }, { "Verified", verified },
            { "Location", new BsonDocument { { "Lat", lat }, { "Lon", 0.0 } } },
            { "Tags", new BsonArray(tags.Select(t => new BsonDocument("Label", t))) }
        };

    private static BsonDocument Row(string name, string home, int rank, BsonValue? addresses)
    {
        var doc = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", name }, { "HomeCity", home }, { "Rank", rank } };
        if (addresses is not null)
        {
            doc["Addresses"] = addresses;
        }

        return doc;
    }

    /// <summary>
    /// a-two: [Paris(1.5, zip 7, verified, tags x), Rome(0.5, tags y, an unmapped Extra field)], home Rome.
    /// b-empty: []. c-null: BSON null. d-missing: no element. e-one: [Paris(3, zip 9, tags y, x)], home Oslo.
    /// f-nullelem: [Oslo(2), null], home Oslo.
    /// </summary>
    private Func<MongoQueryMode, List<string>> Customers(
        Func<IQueryable<Customer>, IEnumerable<string>> query, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<Customer>(Unique(name));
        var rome = A("Rome", 0.5, tags: "y");
        rome["Extra"] = new BsonDocument("Unmapped", 1);
        Raw(collection).InsertMany(
        [
            Row("a-two", "Rome", 3, new BsonArray { A("Paris", 1.5, 7, verified: true, "x"), rome }),
            Row("b-empty", "Paris", 1, new BsonArray()),
            Row("c-null", "Paris", 2, BsonNull.Value),
            Row("d-missing", "Paris", 4, null),
            Row("e-one", "Oslo", 5, new BsonArray { A("Paris", 3, 9, verified: false, "y", "x") }),
            Row("f-nullelem", "Oslo", 6, new BsonArray { A("Oslo", 2), BsonNull.Value })
        ]);
        return mode => Run(collection, mode, ConfigureCustomer, query);
    }

    private static IEnumerable<string> Names(IQueryable<Customer> q) => q.Select(c => c.Name).ToList().Order(StringComparer.Ordinal);

    private static IEnumerable<string> PerRow<T>(IQueryable<Customer> q, System.Linq.Expressions.Expression<Func<Customer, T>> value)
        => q.OrderBy(c => c.Name).Select(value).ToList().Select(v => v?.ToString() ?? "<null>");

    // ── Any / All ───────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Bare_Any_and_its_negation()
    {
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any()))), "a-two", "e-one", "f-nullelem");
        Native(Customers(q => Names(q.Where(c => !c.Addresses.Any()))), "b-empty", "c-null", "d-missing");
    }

    [Fact]
    public void Any_with_a_leaf_predicate()
    {
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City == "Rome")))), "a-two");
        // A null element's members read null (C# null propagation): `null != "Oslo"` is true, so f-nullelem's null element
        // satisfies `!=` although its other element is Oslo.
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City == "Oslo")))), "f-nullelem");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City != "Oslo")))), "a-two", "e-one", "f-nullelem");
    }

    [Fact]
    public void Any_with_compound_and_negated_predicates()
    {
        // Both conditions on the SAME element: a-two's Paris has zip 7, its Rome has no zip.
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City == "Rome" && a.Zip == 7)))));
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City == "Paris" && a.Zip == 7)))), "a-two");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City == "Rome" || a.Zip == 9)))), "a-two", "e-one");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => !(a.City == "Paris"))))), "a-two", "f-nullelem");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.Verified)))), "a-two");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => !a.Verified)))), "a-two", "e-one", "f-nullelem");
    }

    [Fact]
    public void Any_with_string_operators_on_element_leaves()
    {
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City.StartsWith("Pa"))))), "a-two", "e-one");
        // The driver renders EndsWith with $strLenCP, a server error over the null element's missing City (known driver
        // behaviour, loud); native's regex is false for a missing field.
        PerMode(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City.EndsWith("lo"))))), ["f-nullelem"],
            Serves, Serves, "$strLenCP requires a string argument");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City.Contains("om"))))), "a-two");
        // Length over the null element's missing City: native answers C#'s (null == 4 is false; Oslo has 4); the driver's
        // $strLenCP is a server error on a missing string (known driver behaviour, loud).
        PerMode(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City.Length == 4)))), ["a-two", "f-nullelem"],
            Serves, Serves, "$strLenCP requires a string argument");
        // `string.Compare(null, "P") < 0` is true in C#: the null element (members null) matches, as driver-LINQ answers.
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => string.Compare(a.City, "P") < 0)))), "f-nullelem");
    }

    [Fact]
    public void Any_with_a_nested_struct_leaf()
    {
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.Location.Lat > 1)))), "a-two", "e-one", "f-nullelem");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.Location.Lat > 2)))), "e-one");
    }

    [Fact]
    public void Any_with_null_checks_on_element_leaves()
    {
        // a-two's Rome has Zip null; f-nullelem's Oslo has Zip null; e-one's Paris has 9.
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.Zip == null)))), "a-two", "f-nullelem");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.Street == null)))), "a-two", "e-one", "f-nullelem");
        // EXISTING owner-ruled element-scope divergence (Query AGENTS.md: RenderBinary's `$eq`/`$ne` against null is not
        // $ifNull'd inside a $filter/$map scope, matching driver-LINQ): over the null element's MISSING leaf `$ne: [missing,
        // null]` is true, so f-nullelem is included although C# answers `null != null` false (C# rows: a-two, e-one for Zip;
        // none for Street). All three modes agree; the null element is the only row that differs.
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.Zip != null)))), "a-two", "e-one", "f-nullelem");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.Street != null)))), "f-nullelem");
    }

    [Fact]
    public void All_including_vacuous_truth_and_null_elements()
    {
        // Empty, null and missing collections read empty: All is vacuously true. A null element can't satisfy a member
        // predicate, so f-nullelem is excluded even though its other element is Oslo.
        Native(Customers(q => Names(q.Where(c => c.Addresses.All(a => a.City == "Paris")))), "b-empty", "c-null", "d-missing", "e-one");
        Native(Customers(q => Names(q.Where(c => c.Addresses.All(a => a.City == "Oslo")))), "b-empty", "c-null", "d-missing");
        Native(Customers(q => Names(q.Where(c => c.Addresses.All(a => a.Location.Lat >= 1)))), "b-empty", "c-null", "d-missing", "e-one");
        Native(Customers(q => Names(q.Where(c => !c.Addresses.All(a => a.City == "Paris")))), "a-two", "f-nullelem");
    }

    // ── Counts ──────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Count_comparisons()
    {
        Native(Customers(q => Names(q.Where(c => c.Addresses.Count > 1))), "a-two", "f-nullelem");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Count == 0))), "b-empty", "c-null", "d-missing");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Count() == 1))), "e-one");
        Native(Customers(q => Names(q.Where(c => c.Addresses.LongCount() >= 2))), "a-two", "f-nullelem");
        var threshold = 1;
        Native(Customers(q => Names(q.Where(c => c.Addresses.Count > threshold))), "a-two", "f-nullelem");
    }

    [Fact]
    public void Count_with_a_predicate()
    {
        Native(Customers(q => Names(q.Where(c => c.Addresses.Count(a => a.City == "Paris") == 1))), "a-two", "e-one");
        // A null element's members read null: it is not counted by a member predicate.
        Native(Customers(q => PerRow(q, c => c.Addresses.Count(a => a.Zip == null))), "1", "0", "0", "0", "0", "1");
    }

    [Fact]
    public void Count_projections()
    {
        Native(Customers(q => PerRow(q, c => c.Addresses.Count)), "2", "0", "0", "0", "1", "2");
        Native(Customers(q => PerRow(q, c => c.Addresses.LongCount())), "2", "0", "0", "0", "1", "2");
        Native(Customers(q => PerRow(q, c => c.Addresses.Count(a => a.City == "Paris"))), "1", "0", "0", "0", "1", "0");
        Native(Customers(q => q.OrderBy(c => c.Name).Select(c => new { c.Name, N = c.Addresses.Count }).ToList().Select(x => $"{x.Name}:{x.N}")),
            "a-two:2", "b-empty:0", "c-null:0", "d-missing:0", "e-one:1", "f-nullelem:2");
    }

    [Fact]
    public void OrderBy_a_count_combined_with_a_scalar_filter()
        => Native(Customers(q => q.Where(c => c.Rank > 1).OrderByDescending(c => c.Addresses.Count).ThenBy(c => c.Name).Select(c => c.Name).ToList()),
            "a-two", "f-nullelem", "e-one", "c-null", "d-missing");

    [Fact]
    public void Quantifiers_combined_with_scalar_filters_and_paging()
        => Native(Customers(q => q.Where(c => c.Rank >= 3 && c.Addresses.Any(a => a.City == "Paris")).OrderBy(c => c.Rank).Skip(1).Take(1)
                .Select(c => c.Name).ToList()),
            "e-one");

    // ── Nested and correlated element scopes ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Nested_complex_collection_inside_an_element()
    {
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.Tags.Any(t => t.Label == "x"))))), "a-two", "e-one");
        Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.Tags.Count > 1)))), "e-one");
        Native(Customers(q => Names(q.Where(c => c.Addresses.All(a => a.Tags.Any())))), "a-two", "b-empty", "c-null", "d-missing", "e-one");
    }

    [Fact]
    public void Correlated_element_predicate_reads_the_outer_document()
        // Element City vs the customer's HomeCity (an outer field inside the element scope).
        => Native(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.City == c.HomeCity)))), "a-two", "f-nullelem");

    [Fact]
    public void Correlation_from_a_nested_element_scope_declines()
        // The inner scope references the OUTER element (`a.City`), two scopes up from the tag: declined natively.
        => PerMode(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a.Tags.Any(t => t.Label == a.City))))),
            [], NotNative, Serves, Serves);

    // ── Membership: whole elements (member-wise) and element leaves ────────────────────────────────────────────

    public class Spot
    {
        public string City { get; set; } = null!;
        public int? Zip { get; set; }
        public GeoPoint Location { get; set; }
    }

    public class Traveller
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Spot> Spots { get; set; } = [];
    }

    private static void ConfigureTraveller(ModelBuilder mb)
        => mb.Entity<Traveller>().ComplexCollection(t => t.Spots, s => s.ComplexProperty(x => x.Location));

    private static BsonDocument S(string city, int? zip, double lat, bool extra = false)
    {
        var spot = new BsonDocument
        {
            { "Location", new BsonDocument { { "Lon", 0.0 }, { "Lat", lat } } }, { "Zip", zip.HasValue ? zip.Value : BsonNull.Value }, { "City", city }
        };
        if (extra)
        {
            spot["Unmapped"] = "ignored";
        }

        return spot;
    }

    /// <summary>
    /// t-match: [Rome/null/3 with an unmapped field, reordered elements]. t-near: [Rome/null/3.5]. t-nullelem: [null].
    /// t-zipmissing: [Rome (Zip MISSING)/3]. t-empty: []. t-null: BSON null.
    /// </summary>
    private Func<MongoQueryMode, List<string>> Travellers(
        Func<IQueryable<Traveller>, IEnumerable<string>> query, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<Traveller>(Unique(name));
        var zipMissing = S("Rome", null, 3);
        zipMissing.Remove("Zip");
        Raw(collection).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "t-match" }, { "Spots", new BsonArray { S("Rome", null, 3, extra: true) } } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "t-near" }, { "Spots", new BsonArray { S("Rome", null, 3.5) } } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "t-nullelem" }, { "Spots", new BsonArray { BsonNull.Value } } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "t-zipmissing" }, { "Spots", new BsonArray { zipMissing } } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "t-empty" }, { "Spots", new BsonArray() } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "t-null" }, { "Spots", BsonNull.Value } }
        ]);
        return mode => Run(collection, mode, ConfigureTraveller, query);
    }

    private static IEnumerable<string> TNames(IQueryable<Traveller> q) => q.Select(t => t.Name).ToList().Order(StringComparer.Ordinal);

    [Fact]
    public void Contains_of_a_captured_element_is_member_wise()
    {
        // Member-wise: element order and unmapped fields don't matter, a missing Zip equals a null one (C# reads both null).
        var spot = new Spot { City = "Rome", Zip = null, Location = new GeoPoint { Lat = 3, Lon = 0 } };
        PerMode(Travellers(q => TNames(q.Where(t => t.Spots.Contains(spot)))), ["t-match", "t-zipmissing"], Serves, Serves, WholeValueRefused);
        // EF rewrites Any(s => s == spot) to Contains(spot): the same answer.
        PerMode(Travellers(q => TNames(q.Where(t => t.Spots.Any(s => s == spot)))), ["t-match", "t-zipmissing"], Serves, Serves, WholeValueRefused);
        // The exact complement: every element differs (vacuously true for an empty/null array; a null element differs).
        PerMode(Travellers(q => TNames(q.Where(t => !t.Spots.Contains(spot)))), ["t-empty", "t-near", "t-null", "t-nullelem"],
            Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Contains_of_a_captured_element_is_read_per_execution()
    {
        // One query shape, two captured values (the compiled-query cache must not bake the first value in).
        Spot? spot = null;
        var run = Travellers(q => TNames(q.Where(t => t.Spots.Contains(spot!))));
        // A null comparand matches a null ELEMENT (null == null), never a present one.
        PerMode(run, ["t-nullelem"], Serves, Serves, Serves);
        spot = new Spot { City = "Rome", Location = new GeoPoint { Lat = 3.5 } };
        PerMode(run, ["t-near"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Contains_of_an_inline_constructed_element()
        => PerMode(Travellers(q => TNames(q.Where(t => t.Spots.Contains(new Spot { City = "Rome", Zip = null, Location = new GeoPoint { Lat = 3.5, Lon = 0 } })))),
            ["t-near"], Serves, Serves, WholeValueRefused);

    [Fact]
    public void Contains_of_an_element_whose_type_holds_a_nested_collection_declines()
    {
        // Member-wise equality doesn't compare collections (Task 12 rule); the fallback refuses the whole value (R1).
        var addr = new Addr { City = "Rome", Location = new GeoPoint { Lat = 0.5 } };
        PerMode(Customers(q => Names(q.Where(c => c.Addresses.Contains(addr)))), [], NotNative, WholeValueRefused, WholeValueRefused);
    }

    [Fact]
    public void Contains_of_an_element_leaf_declines_with_correct_fallback_rows()
        // `Select(a => a.City).Contains("Rome")` is not native for owned collections either; the fallback (now with the
        // required collection normalized to empty) answers correctly on null and missing arrays.
        => PerMode(Customers(q => Names(q.Where(c => c.Addresses.Select(a => a.City).Contains("Rome")))),
            ["a-two"], NotNative, Serves, Serves);

    [Fact]
    public void Null_element_test_is_refused_by_EF_in_every_mode()
    {
        // EF's own QueryOptimizingExpressionVisitor rewrites `Any(a => a == null)` / `All(a => a != null)` to a Contains with
        // an object-typed null and throws ArgumentException before the provider sees the query (EF limitation, every mode).
        PerMode(Customers(q => Names(q.Where(c => c.Addresses.Any(a => a == null)))), [],
            "cannot be used for parameter of type", "cannot be used for parameter of type", "cannot be used for parameter of type");
        PerMode(Customers(q => Names(q.Where(c => c.Addresses.All(a => a != null)))), [],
            "cannot be used for parameter of type", "cannot be used for parameter of type", "cannot be used for parameter of type");
    }

    public class Note
    {
        public string? Text { get; set; }
        public int? Stars { get; set; }
    }

    public class Review
    {
        public string Author { get; set; } = null!;
        public Note Note { get; set; } = null!;
    }

    public class Book
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Review> Reviews { get; set; } = [];
    }

    [Fact]
    public void Single_complex_value_inside_an_element_compares_member_wise_and_a_null_element_is_absent()
    {
        // `r.Note == new Note()` (all members null): a present Note whose members are null/missing equals it; a NULL
        // element's Note is absent (null propagation, ruling R14) and equals NO instance, however null its members are.
        var collection = database.CreateCollection<Book>(Unique(nameof(Single_complex_value_inside_an_element_compares_member_wise_and_a_null_element_is_absent)));
        Raw(collection).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "k-blank" }, { "Reviews", new BsonArray { new BsonDocument { { "Author", "a" }, { "Note", new BsonDocument() } } } } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "k-nullelem" }, { "Reviews", new BsonArray { BsonNull.Value } } },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "k-value" },
                { "Reviews", new BsonArray { new BsonDocument { { "Author", "b" }, { "Note", new BsonDocument { { "Text", "t" }, { "Stars", 5 } } } } } }
            }
        ]);

        static void Configure(ModelBuilder mb) => mb.Entity<Book>().ComplexCollection(b => b.Reviews, r => r.ComplexProperty(x => x.Note));
        IEnumerable<string> N(IQueryable<Book> q) => q.Select(b => b.Name).ToList().Order();
        var blank = new Note();
        var five = new Note { Text = "t", Stars = 5 };
        PerMode(m => Run(collection, m, Configure, q => N(q.Where(b => b.Reviews.Any(r => r.Note == blank)))), ["k-blank"], Serves, Serves, WholeValueRefused);
        PerMode(m => Run(collection, m, Configure, q => N(q.Where(b => b.Reviews.Any(r => r.Note == five)))), ["k-value"], Serves, Serves, WholeValueRefused);
        // The exact complement: the null element's absent Note differs from every instance.
        PerMode(m => Run(collection, m, Configure, q => N(q.Where(b => b.Reviews.Any(r => r.Note != blank)))), ["k-nullelem", "k-value"],
            Serves, Serves, WholeValueRefused);
        // Driver-LINQ misses the null element's MISSING Note (its `$eq` against null isn't missing-safe in a $map scope):
        // the same driver behaviour Task 12 pinned for owned elements (Jira candidate 11, not filed). Pinned so the test
        // breaks when the driver changes.
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            Assert.Equal(["k-nullelem"], Run(collection, mode, Configure, q => N(q.Where(b => b.Reviews.Any(r => r.Note == null)))));
        }

        Assert.Empty(Run(collection, MongoQueryMode.DriverLinq, Configure, q => N(q.Where(b => b.Reviews.Any(r => r.Note == null)))));
    }

    // ── Indexers and element operators: declined (no native element access), fallback correct ──────────────────

    [Fact]
    public void Indexer_ElementAt_and_First_with_a_predicate_decline()
    {
        // e-one's first element is Paris; a-two's first is Paris; f-nullelem's first is Oslo. (A null/missing/empty array
        // has no first element: the fallback answers false for those rows.)
        PerMode(Customers(q => Names(q.Where(c => c.Addresses[0].City == "Paris"))), ["a-two", "e-one"], NotNative, Serves, Serves);
        PerMode(Customers(q => Names(q.Where(c => c.Addresses.ElementAt(0).City == "Paris"))), ["a-two", "e-one"], NotNative, Serves, Serves);
        PerMode(Customers(q => Names(q.Where(c => c.Addresses.First(a => a.Zip != null).City == "Paris"))), ["a-two", "e-one"],
            NotNative, Serves, Serves);
    }

    // ── Projections ─────────────────────────────────────────────────────────────────────────────────────────────

    private static string Fmt(IEnumerable<Addr?>? list)
        => list == null ? "<null>" : "[" + string.Join(";", list.Select(a => a == null ? "<null>" : $"{a.City}|{a.Zip?.ToString() ?? "-"}|{a.Location.Lat}|{string.Join(",", a.Tags.Select(t => t.Label))}")) + "]";

    [Fact]
    public void Whole_collection_projection_preserves_order_and_every_stored_state()
        => Native(Customers(q => q.OrderBy(c => c.Name).Select(c => c.Addresses).ToList().Select(Fmt)),
            "[Paris|7|1.5|x;Rome|-|0.5|y]", "[]", "[]", "[]", "[Paris|9|3|y,x]", "[Oslo|-|2|;<null>]");

    [Fact]
    public void Whole_collection_beside_scalars_and_a_count()
        // Native. Under DriverLinq the shaper can't read the count member beside a whole complex value (pre-existing bridge
        // limitation for a computed member beside a complex value: loud, never rows).
        => PerMode(Customers(q => q.OrderBy(c => c.Name).Select(c => new { c.Name, N = c.Addresses.Count, c.Addresses }).ToList()
                .Select(x => $"{x.Name}:{x.N}:{Fmt(x.Addresses)}")),
            ["a-two:2:[Paris|7|1.5|x;Rome|-|0.5|y]", "b-empty:0:[]", "c-null:0:[]", "d-missing:0:[]", "e-one:1:[Paris|9|3|y,x]",
                "f-nullelem:2:[Oslo|-|2|;<null>]"],
            Serves, Serves, "The property 'Customer.N' could not be found");

    [Fact]
    public void Paging_after_a_whole_collection_projection()
        => Native(Customers(q => q.OrderBy(c => c.Name).Select(c => c.Addresses).Skip(4).Take(1).ToList().Select(Fmt)), "[Paris|9|3|y,x]");

    [Fact]
    public void Element_leaf_projections_decline_with_correct_fallback_rows()
    {
        // Not native for owned collections either (no element-scope $map projection leaf); the fallback reads a null/missing
        // REQUIRED collection as empty (it read `null` lists before this task) and preserves element order.
        PerMode(Customers(q => q.OrderBy(c => c.Name).Select(c => c.Addresses.Select(a => a.City).ToList()).ToList().Select(l => string.Join(",", l))),
            ["Paris,Rome", "", "", "", "Paris", "Oslo,"], NotNative, Serves, Serves);
        PerMode(Customers(q => q.OrderBy(c => c.Name).Select(c => new { c.Name, Cities = c.Addresses.Select(a => a.City) }).ToList()
                .Select(x => $"{x.Name}:{string.Join(",", x.Cities)}")),
            ["a-two:Paris,Rome", "b-empty:", "c-null:", "d-missing:", "e-one:Paris", "f-nullelem:Oslo,"], NotNative, Serves, Serves);
        // A count BESIDE an element-leaf list: the list declines the native projection, and the fallback's shaper can't
        // rebuild a Count over a complex collection (it can over an owned one), so it is refused in every mode (loud, never
        // rows). Follow-up recorded for Task 16.
        PerMode(Customers(q => q.OrderBy(c => c.Name)
                .Select(c => new { c.Name, Count = c.Addresses.Count, Cities = c.Addresses.Select(a => a.City) }).ToList()
                .Select(x => $"{x.Name}:{x.Count}:{string.Join(",", x.Cities)}")),
            [], NotTranslated, NotTranslated, NotTranslated);
    }

    [Fact]
    public void Element_leaf_aggregates_are_refused_in_every_mode()
    {
        // Sum/Max over element leaves can't be translated by any path (the same as for an owned collection).
        PerMode(Customers(q => PerRow(q, c => c.Addresses.Sum(a => a.Location.Lat))), [], NotTranslated, NotTranslated, NotTranslated);
        PerMode(Customers(q => PerRow(q, c => c.Addresses.Max(a => a.Location.Lat))), [], NotTranslated, NotTranslated, NotTranslated);
    }

    [Fact]
    public void Operators_over_a_whole_collection_projection_are_refused()
    {
        // R7/R8: a projected whole complex value (a collection is one) can't be the operand of a value-reading operator.
        PerMode(Customers(q => q.Select(c => c.Addresses).Distinct().ToList().Select(Fmt)), [],
            "cannot be the operand of 'Distinct'", "cannot be the operand of 'Distinct'", "cannot be the operand of 'Distinct'");
        // Scalar counts are not complex values: Distinct and GroupBy over them are native.
        Native(Customers(q => q.Select(c => c.Addresses.Count).Distinct().ToList().Order().Select(n => n.ToString())), "0", "1", "2");
    }

    // ── Element names, two collections of one element type, stored-alike leaves ──────────────────────────────────

    public class Leg
    {
        public string Code { get; set; } = null!;
        public int Seats { get; set; }
    }

    public class Trip
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Leg> Outbound { get; set; } = [];
        public List<Leg> Inbound { get; set; } = [];
    }

    [Fact]
    public void Same_element_type_on_two_collections_with_different_element_names()
    {
        // Outbound under `out` with Code under `c`; Inbound under its CLR name with Seats stored as a string
        // (BsonRepresentation). Each path resolves independently; the stored-form comparison is equality only.
        var collection = database.CreateCollection<Trip>(Unique(nameof(Same_element_type_on_two_collections_with_different_element_names)));
        Raw(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "x" },
                { "out", new BsonArray { new BsonDocument { { "c", "AB" }, { "Seats", 2 } } } },
                { "Inbound", new BsonArray { new BsonDocument { { "Code", "CD" }, { "Seats", "4" } } } },
                // Decoys under the CLR / other names must never be read.
                { "Outbound", new BsonArray { new BsonDocument { { "Code", "CD" }, { "Seats", 9 } } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "y" },
                { "out", new BsonArray { new BsonDocument { { "c", "CD" }, { "Seats", 1 } } } },
                { "Inbound", new BsonArray() }
            }
        ]);

        static void Configure(ModelBuilder mb)
            => mb.Entity<Trip>(b =>
            {
                b.ComplexCollection(t => t.Outbound, l =>
                {
                    l.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "out");
                    l.Property(x => x.Code).Metadata.SetElementName("c");
                });
                b.ComplexCollection(t => t.Inbound, l => l.Property(x => x.Seats).Metadata.SetBsonRepresentation(BsonType.String, null, null));
            });

        IEnumerable<string> N(IQueryable<Trip> q) => q.Select(t => t.Name).ToList().Order();
        Native(m => Run(collection, m, Configure, q => N(q.Where(t => t.Outbound.Any(l => l.Code == "CD")))), "y");
        Native(m => Run(collection, m, Configure, q => N(q.Where(t => t.Inbound.Any(l => l.Code == "CD")))), "x");
        Native(m => Run(collection, m, Configure, q => N(q.Where(t => t.Inbound.Any(l => l.Seats == 4)))), "x");
        Native(m => Run(collection, m, Configure, q => N(q.Where(t => t.Outbound.Count(l => l.Seats > 1) == 1))), "x");
        // A relational comparison runs on the stored form: over a string-represented leaf it declines natively (EF-337);
        // the fallback refuses it too.
        // (Before this task Native's fallback and DriverLinq compared the stored "4" with 100 and served `x`: wrong rows.)
        PerMode(m => Run(collection, m, Configure, q => N(q.Where(t => t.Inbound.Any(l => l.Seats > 100)))), [],
            NotNative, "stored through a value converter or a BsonRepresentation", "stored through a value converter or a BsonRepresentation");
    }

    [Fact]
    public void Camel_case_convention_names_the_collection_and_element_leaves()
    {
        var collection = database.CreateCollection<Traveller>(Unique(nameof(Camel_case_convention_names_the_collection_and_element_leaves)));
        Raw(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "name", "c1" },
                { "spots", new BsonArray { new BsonDocument { { "city", "Rome" }, { "zip", 1 }, { "location", new BsonDocument { { "lat", 1.0 }, { "lon", 0.0 } } } } } }
            },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "name", "c2" }, { "spots", new BsonArray() } }
        ]);

        static void Conventions(ModelConfigurationBuilder cb) => cb.Conventions.Add(_ => new CamelCaseElementNameConvention());
        IEnumerable<string> N(IQueryable<Traveller> q) => q.Select(t => t.Name).ToList().Order();
        Native(m => Run(collection, m, ConfigureTraveller, q => N(q.Where(t => t.Spots.Any(s => s.City == "Rome" && s.Location.Lat == 1))), Conventions), "c1");
        Native(m => Run(collection, m, ConfigureTraveller, q => q.OrderBy(t => t.Name).Select(t => t.Spots.Count).ToList().Select(n => n.ToString()), Conventions),
            "1", "0");
    }

    // ── Complex collections reached through hops: inside a complex property, inside an owned type ─────────────

    public class Profile
    {
        public string Label { get; set; } = null!;
        public List<Tag> Tags { get; set; } = [];
    }

    [System.ComponentModel.DataAnnotations.Schema.ComplexType]
    public class Badge
    {
        public string Code { get; set; } = null!;
    }

    public class Membership
    {
        public string Club { get; set; } = null!;
        public List<Badge> Badges { get; set; } = [];
    }

    public class Member
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Profile Profile { get; set; } = null!;
        public Membership Membership { get; set; } = null!;
    }

    [Fact]
    public void Collection_inside_a_complex_property_and_inside_an_owned_type()
    {
        var collection = database.CreateCollection<Member>(Unique(nameof(Collection_inside_a_complex_property_and_inside_an_owned_type)));
        Raw(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "m1" },
                { "Profile", new BsonDocument { { "Label", "p" }, { "Tags", new BsonArray { new BsonDocument("Label", "x") } } } },
                { "Membership", new BsonDocument { { "Club", "c" }, { "Badges", new BsonArray { new BsonDocument("Code", "gold") } } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "m2" },
                { "Profile", new BsonDocument { { "Label", "q" } } },
                { "Membership", new BsonDocument { { "Club", "d" }, { "Badges", BsonNull.Value } } }
            }
        ]);

        static void Configure(ModelBuilder mb)
            => mb.Entity<Member>(b =>
            {
                b.ComplexProperty(m => m.Profile, p => p.ComplexCollection(x => x.Tags));
                b.OwnsOne(m => m.Membership);
            });

        IEnumerable<string> N(IQueryable<Member> q) => q.Select(t => t.Name).ToList().Order();
        // Premise: the list on the owned type is a complex collection (EF10 maps it via [ComplexType]).
        using (var db = Context(collection, MongoQueryMode.Native, Configure))
        {
            Assert.True(db.Model.FindEntityType(typeof(Membership))!.FindComplexProperty(nameof(Membership.Badges))!.IsCollection);
        }

        Native(m => Run(collection, m, Configure, q => N(q.Where(x => x.Profile.Tags.Any(t => t.Label == "x")))), "m1");
        Native(m => Run(collection, m, Configure, q => N(q.Where(x => !x.Profile.Tags.Any()))), "m2");
        Native(m => Run(collection, m, Configure, q => N(q.Where(x => x.Membership.Badges.Any(b => b.Code == "gold")))), "m1");
        Native(m => Run(collection, m, Configure, q => N(q.Where(x => x.Membership.Badges.Count == 0))), "m2");
        // A count PROJECTION through an owned hop is refused in every mode (pre-existing: an owned-hop count leaf,
        // `b.Home.Notes.Count`, is declined for owned collections too and the fallback can't bind it).
        PerMode(m => Run(collection, m, Configure, q => q.OrderBy(x => x.Name).Select(x => x.Membership.Badges.Count).ToList().Select(n => n.ToString())),
            [], NotTranslated, NotTranslated, NotTranslated);
    }

    // ── Joins / Include alongside an entity with a complex collection (smoke) ───────────────────────────────────

    public class Shop
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Tag> Tags { get; set; } = [];
        public List<Sale> Sales { get; set; } = [];
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
                b.HasMany(s => s.Sales).WithOne(s => s.Shop).HasForeignKey(s => s.ShopId);
            });
            mb.Entity<Sale>().ToCollection(sales);
        }
    }

    [Fact]
    public void Join_and_Include_beside_a_complex_collection()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var shops = TemporaryDatabaseFixtureBase.CreateCollectionName(nameof(Join_and_Include_beside_a_complex_collection)) + suffix + "_s";
        var sales = TemporaryDatabaseFixtureBase.CreateCollectionName(nameof(Join_and_Include_beside_a_complex_collection)) + suffix + "_l";
        var s1 = ObjectId.GenerateNewId();
        var s2 = ObjectId.GenerateNewId();
        database.MongoDatabase.GetCollection<BsonDocument>(shops).InsertMany(
        [
            new BsonDocument { { "_id", s1 }, { "Name", "s1" }, { "Tags", new BsonArray { new BsonDocument("Label", "hot") } } },
            new BsonDocument { { "_id", s2 }, { "Name", "s2" }, { "Tags", new BsonArray() } }
        ]);
        database.MongoDatabase.GetCollection<BsonDocument>(sales).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "ShopId", s1 }, { "Amount", 5 } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "ShopId", s2 }, { "Amount", 7 } }
        ]);

        Func<MongoQueryMode, List<string>> Shops(Func<ShopContext, IEnumerable<string>> query) => mode =>
        {
            var builder = new DbContextOptionsBuilder<ShopContext>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
            using var db = new ShopContext(builder.Options, shops, sales);
            return query(db).ToList();
        };

        // Reference navigation filter + a complex-collection quantifier on the joined side's owner.
        PerMode(Shops(db => db.Sales.Where(s => s.Shop.Tags.Any(t => t.Label == "hot")).Select(s => s.Amount.ToString()).ToList()),
            ["5"], NotNative, Serves, Serves);
        // Collection Include with the complex collection materialized on the root.
        Native(Shops(db => db.Shops.Include(s => s.Sales).OrderBy(s => s.Name).ToList()
                .Select(s => $"{s.Name}:{string.Join(",", s.Tags.Select(t => t.Label))}:{string.Join(",", s.Sales.Select(x => x.Amount))}")),
            "s1:hot:5", "s2::7");
        // A root quantifier with an Include beside it.
        Native(Shops(db => db.Shops.Include(s => s.Sales).Where(s => s.Tags.Any()).ToList().Select(s => $"{s.Name}:{s.Sales.Count}")), "s1:1");
    }

    // ── Optional complex collection (EF10) ──────────────────────────────────────────────────────────────────────

    public class Itinerary
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Spot>? Extra { get; set; }
    }

    [Fact]
    public void Optional_collection_null_check_and_quantifiers()
    {
        // An OPTIONAL collection reads null when its element is null/missing (it is not normalized to empty).
        var collection = database.CreateCollection<Itinerary>(Unique(nameof(Optional_collection_null_check_and_quantifiers)));
        Raw(collection).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "i-one" }, { "Extra", new BsonArray { S("Rome", 1, 1) } } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "i-empty" }, { "Extra", new BsonArray() } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "i-null" }, { "Extra", BsonNull.Value } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "i-missing" } }
        ]);

        static void Configure(ModelBuilder mb)
            => mb.Entity<Itinerary>().ComplexCollection(i => i.Extra, s =>
            {
                s.IsRequired(false);
                s.ComplexProperty(x => x.Location);
            });

        IEnumerable<string> N(IQueryable<Itinerary> q) => q.Select(i => i.Name).ToList().Order();
        // The null check of the collection itself is not a supported native shape (a whole complex value compared with
        // null is single-valued only); the fallback answers it.
        PerMode(m => Run(collection, m, Configure, q => N(q.Where(i => i.Extra == null))), ["i-missing", "i-null"], NotNative, Serves, Serves);
        // `i.Extra!.Any()` over a null collection has no C# answer (it would throw); native reads the stored null/missing
        // array as empty ($ifNull, as for every array quantifier), while driver-LINQ's $size over the null array is a server
        // error: the optional collection is deliberately NOT normalized on the fallback (it reads null), so it stays loud.
        PerMode(m => Run(collection, m, Configure, q => N(q.Where(i => i.Extra!.Any()))), ["i-one"], Serves, Serves,
            "The argument to $size must be an array");
        Native(m => Run(collection, m, Configure, q => q.OrderBy(i => i.Name).Select(i => i.Extra!.Count).ToList().Select(n => n.ToString())),
            "0", "0", "0", "1");
    }

    // ── Model limits (measured) ─────────────────────────────────────────────────────────────────────────────────

    public class PointHolder
    {
        public ObjectId Id { get; set; }
        public List<GeoPoint> Points { get; set; } = [];
    }

    [Fact]
    public void Struct_element_collections_are_rejected_by_EF10_at_model_building()
    {
        var collection = database.CreateCollection<PointHolder>(Unique(nameof(Struct_element_collections_are_rejected_by_EF10_at_model_building)));
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = Context(collection, MongoQueryMode.Native, mb => mb.Entity<PointHolder>().ComplexCollection(h => h.Points));
            _ = db.Model;
        });
        Assert.Contains("complex value type collections are not supported", ex.Message);
    }

    // ── Plumbing ────────────────────────────────────────────────────────────────────────────────────────────────

    private static List<string> Run<TEntity>(
        IMongoCollection<TEntity> collection, MongoQueryMode mode, Action<ModelBuilder> configure,
        Func<IQueryable<TEntity>, IEnumerable<string>> query, Action<ModelConfigurationBuilder>? conventions = null)
        where TEntity : class
    {
        using var db = Context(collection, mode, configure, conventions);
        return [.. query(db.Entities.AsNoTracking())];
    }

    private static SingleEntityDbContext<TEntity> Context<TEntity>(
        IMongoCollection<TEntity> collection, MongoQueryMode mode, Action<ModelBuilder> configure,
        Action<ModelConfigurationBuilder>? conventions = null)
        where TEntity : class
        => SingleEntityDbContext.Create(collection, configure, conventions, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    private static IMongoCollection<BsonDocument> Raw<T>(IMongoCollection<T> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    private static string Unique(string name)
        => TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
}
#endif
