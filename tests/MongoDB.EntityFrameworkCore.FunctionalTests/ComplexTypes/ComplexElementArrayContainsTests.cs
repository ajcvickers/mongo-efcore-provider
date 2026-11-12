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
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

/// <summary>
/// <c>list.Contains(item)</c> over a primitive collection that is a member of a complex collection's element (#40), compared
/// with an in-memory LINQ oracle over data that holds a NULL element, an element whose lists are missing or BSON null, an
/// empty collection and a missing root list. A null element reads every member as missing inside the aggregation scope, so a
/// bare <c>$in</c> over it is a server error that fails the whole query; the native rendering reads a missing/null array as
/// empty, as <c>Count</c> over the same member does.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexElementArrayContainsTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public enum Hue
    {
        Red,
        Green,
        Blue
    }

    public class TypedVariant
    {
        public string Label { get; set; } = "";
        public List<int> Nums { get; set; } = [];
        public List<long> Longs { get; set; } = [];
        public List<Guid> Ids { get; set; } = [];
        public List<DateTime> Days { get; set; } = [];
        public List<Hue> Hues { get; set; } = [];
        public List<decimal> Prices { get; set; } = [];
        public List<double> Ratios { get; set; } = [];
        public List<bool> Flags { get; set; } = [];
        public List<string> Tags { get; set; } = [];
        public List<int?> Maybe { get; set; } = [];
        public int[] Ints { get; set; } = [];

        [BsonRepresentation(BsonType.String)]
        public List<Hue> NamedHues { get; set; } = [];
    }

    public class TypedHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<TypedVariant> Many { get; set; } = [];
        public List<int> Top { get; set; } = [];
    }

    private static readonly Guid G1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid G2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime D1 = new(2024, 5, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime D2 = new(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    private static void Configure(ModelBuilder mb) => mb.Entity<TypedHolder>().ComplexCollection(h => h.Many);

    private static SingleEntityDbContext<TypedHolder> Context(IMongoCollection<TypedHolder> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(collection, Configure, null, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            b.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    private static IMongoCollection<BsonDocument> Raw(IMongoCollection<TypedHolder> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    private static string Unique(string name)
        => TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];

    private static TypedVariant Full() => new()
    {
        Label = "full",
        Nums = [5, 6],
        Longs = [5L],
        Ids = [G1],
        Days = [D1],
        Hues = [Hue.Green],
        Prices = [1.5m],
        Ratios = [2.5],
        Flags = [true],
        Tags = ["x", "$y"],
        Maybe = [5, null],
        Ints = [5],
        NamedHues = [Hue.Green]
    };

    private static TypedVariant Other() => new()
    {
        Label = "other",
        Nums = [7],
        Longs = [8L],
        Ids = [G2],
        Days = [D2],
        Hues = [Hue.Red],
        Prices = [9.25m],
        Ratios = [3.5],
        Flags = [false],
        Tags = ["z", null!],
        Maybe = [null],
        Ints = [8],
        NamedHues = [Hue.Red]
    };

    // The data every case is judged against. An in-memory holder stands in for each stored document: a null element is `null`.
    // `Many` documents "bare" and "nullarrays" are written raw below (missing / BSON null lists), and read as empty lists.
    private static List<TypedHolder> CleanData() =>
    [
        new() { Name = "a", Many = [Full(), new TypedVariant { Label = "empty" }], Top = [5] },
        new() { Name = "b", Many = [Other()], Top = [] },
        new() { Name = "c", Many = [], Top = [1, 5] },
        new() { Name = "d", Many = [Other(), Full()], Top = [2] }
    ];

    private static List<TypedHolder> NullData() =>
    [
        new() { Name = "e_null", Many = [null!], Top = [] },
        new() { Name = "f_mixed", Many = [null!, Full()], Top = [5] },
        new() { Name = "g_mixed", Many = [Other(), null!], Top = [] },
        new() { Name = "h_bare", Many = [new TypedVariant { Label = "bare" }], Top = [] },
        new() { Name = "i_nullarrays", Many = [new TypedVariant { Label = "nullarrays" }], Top = [] }
    ];

    // Documents EF cannot write: lists that are missing or BSON null, and a missing root list.
    private static void InsertMalformed(IMongoCollection<TypedHolder> collection)
        => Raw(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "h_bare" },
                { "Many", new BsonArray { new BsonDocument { { "Label", "bare" } } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "i_nullarrays" }, { "Top", new BsonArray() },
                {
                    "Many", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "Label", "nullarrays" }, { "Nums", BsonNull.Value }, { "Longs", BsonNull.Value }, { "Ids", BsonNull.Value },
                            { "Days", BsonNull.Value }, { "Hues", BsonNull.Value }, { "Prices", BsonNull.Value },
                            { "Ratios", BsonNull.Value }, { "Flags", BsonNull.Value }, { "Tags", BsonNull.Value },
                            { "Maybe", BsonNull.Value }, { "Ints", BsonNull.Value }, { "NamedHues", BsonNull.Value }
                        }
                    }
                }
            }
        ]);

    private IMongoCollection<TypedHolder> Seed(string name, bool withNullElements)
    {
        var collection = database.CreateCollection<TypedHolder>(Unique(name));
        using (var db = Context(collection, MongoQueryMode.Native))
        {
            db.Entities.AddRange(CleanData());
            if (withNullElements)
            {
                db.Entities.AddRange(NullData().Where(h => !h.Name.StartsWith("h_") && !h.Name.StartsWith("i_")));
            }

            db.SaveChanges();
        }

        if (withNullElements)
        {
            InsertMalformed(collection);
        }

        return collection;
    }

    private static List<TypedHolder> Oracle(bool withNullElements)
        => [.. CleanData(), .. (withNullElements ? NullData() : [])];

    // The oracle reads a null element as an element with empty lists (the native null-element rule).
    private static readonly TypedVariant NoVariant = new();

    private static TypedVariant V(TypedVariant? element) => element ?? NoVariant;

    private sealed record Case(string Label, Expression<Func<TypedHolder, bool>> Query, Func<TypedHolder, bool> Expected);

    private static IEnumerable<Case> ServedCases()
    {
        var five = 5;
        var five64 = 5L;
        var green = Hue.Green;
        var price = 1.5m;
        var ratio = 2.5;
        var yes = true;
        var x = "x";
        var dollar = "$y";
        string? none = null;
        int? maybe = 5;
        int? noInt = null;

        yield return new("int constant", h => h.Many.Any(m => m.Nums.Contains(5)), h => h.Many.Any(m => V(m).Nums.Contains(5)));
        yield return new("int parameter", h => h.Many.Any(m => m.Nums.Contains(five)), h => h.Many.Any(m => V(m).Nums.Contains(5)));
        yield return new("int constant, no match", h => h.Many.Any(m => m.Nums.Contains(99)), h => h.Many.Any(m => V(m).Nums.Contains(99)));
        yield return new("negated", h => h.Many.Any(m => !m.Nums.Contains(5)), h => h.Many.Any(m => !V(m).Nums.Contains(5)));
        yield return new("negated parameter", h => h.Many.Any(m => !m.Nums.Contains(five)), h => h.Many.Any(m => !V(m).Nums.Contains(5)));
        yield return new("All", h => h.Many.All(m => m.Nums.Contains(5)), h => h.Many.All(m => V(m).Nums.Contains(5)));
        yield return new("All negated", h => h.Many.All(m => !m.Nums.Contains(5)), h => h.Many.All(m => !V(m).Nums.Contains(5)));
        yield return new("not Any", h => !h.Many.Any(m => m.Nums.Contains(5)), h => !h.Many.Any(m => V(m).Nums.Contains(5)));
        yield return new("Count", h => h.Many.Count(m => m.Nums.Contains(5)) == 1, h => h.Many.Count(m => V(m).Nums.Contains(5)) == 1);
        yield return new("&& with another member", h => h.Many.Any(m => m.Nums.Contains(5) && m.Label == "full"),
            h => h.Many.Any(m => V(m).Nums.Contains(5) && V(m).Label == "full"));
        yield return new("|| with another member", h => h.Many.Any(m => m.Nums.Contains(5) || m.Label == "other"),
            h => h.Many.Any(m => V(m).Nums.Contains(5) || V(m).Label == "other"));
        yield return new("two Contains", h => h.Many.Any(m => m.Nums.Contains(5) && m.Tags.Contains("x")),
            h => h.Many.Any(m => V(m).Nums.Contains(5) && V(m).Tags.Contains("x")));
        yield return new("with a root predicate", h => h.Name != "a" && h.Many.Any(m => m.Nums.Contains(5)),
            h => h.Name != "a" && h.Many.Any(m => V(m).Nums.Contains(5)));
        yield return new("long parameter", h => h.Many.Any(m => m.Longs.Contains(five64)), h => h.Many.Any(m => V(m).Longs.Contains(5L)));
        yield return new("long constant", h => h.Many.Any(m => m.Longs.Contains(5L)), h => h.Many.Any(m => V(m).Longs.Contains(5L)));
        yield return new("Guid parameter", h => h.Many.Any(m => m.Ids.Contains(G1)), h => h.Many.Any(m => V(m).Ids.Contains(G1)));
        yield return new("DateTime parameter", h => h.Many.Any(m => m.Days.Contains(D1)), h => h.Many.Any(m => V(m).Days.Contains(D1)));
        yield return new("enum constant", h => h.Many.Any(m => m.Hues.Contains(Hue.Green)), h => h.Many.Any(m => V(m).Hues.Contains(Hue.Green)));
        yield return new("enum parameter", h => h.Many.Any(m => m.Hues.Contains(green)), h => h.Many.Any(m => V(m).Hues.Contains(Hue.Green)));
        yield return new("decimal constant", h => h.Many.Any(m => m.Prices.Contains(1.5m)), h => h.Many.Any(m => V(m).Prices.Contains(1.5m)));
        yield return new("decimal parameter", h => h.Many.Any(m => m.Prices.Contains(price)), h => h.Many.Any(m => V(m).Prices.Contains(1.5m)));
        yield return new("double constant", h => h.Many.Any(m => m.Ratios.Contains(2.5)), h => h.Many.Any(m => V(m).Ratios.Contains(2.5)));
        yield return new("double parameter", h => h.Many.Any(m => m.Ratios.Contains(ratio)), h => h.Many.Any(m => V(m).Ratios.Contains(2.5)));
        yield return new("bool constant", h => h.Many.Any(m => m.Flags.Contains(true)), h => h.Many.Any(m => V(m).Flags.Contains(true)));
        yield return new("bool parameter", h => h.Many.Any(m => m.Flags.Contains(yes)), h => h.Many.Any(m => V(m).Flags.Contains(true)));
        yield return new("string constant", h => h.Many.Any(m => m.Tags.Contains("x")), h => h.Many.Any(m => V(m).Tags.Contains("x")));
        // A constant that begins with `$` must not be read as a field path.
        yield return new("string constant starting with $", h => h.Many.Any(m => m.Tags.Contains("$y")), h => h.Many.Any(m => V(m).Tags.Contains("$y")));
        yield return new("string constant, negated", h => h.Many.Any(m => !m.Tags.Contains("x")), h => h.Many.Any(m => !V(m).Tags.Contains("x")));
        yield return new("nullable element, non-null constant", h => h.Many.Any(m => m.Maybe.Contains(5)), h => h.Many.Any(m => V(m).Maybe.Contains(5)));
        yield return new("array member (Enumerable.Contains)", h => h.Many.Any(m => m.Ints.Contains(5)), h => h.Many.Any(m => V(m).Ints.Contains(5)));
        yield return new("array member, parameter", h => h.Many.Any(m => m.Ints.Contains(five)), h => h.Many.Any(m => V(m).Ints.Contains(5)));
        yield return new("enum stored as a string, constant", h => h.Many.Any(m => m.NamedHues.Contains(Hue.Green)),
            h => h.Many.Any(m => V(m).NamedHues.Contains(Hue.Green)));
        yield return new("enum stored as a string, parameter", h => h.Many.Any(m => m.NamedHues.Contains(green)),
            h => h.Many.Any(m => V(m).NamedHues.Contains(Hue.Green)));
        // In an element scope (always the aggregation dialect) `$in: [null, array]` is `list.Contains(null)`, so a null-capable item
        // is translated: a captured string, a null constant, a nullable parameter, a null parameter.
        yield return new("string parameter", h => h.Many.Any(m => m.Tags.Contains(x)), h => h.Many.Any(m => V(m).Tags.Contains("x")));
        yield return new("string parameter, negated", h => h.Many.Any(m => !m.Tags.Contains(x)), h => h.Many.Any(m => !V(m).Tags.Contains("x")));
        yield return new("string parameter starting with $", h => h.Many.Any(m => m.Tags.Contains(dollar)), h => h.Many.Any(m => V(m).Tags.Contains("$y")));
        yield return new("null string parameter (matches a list that holds a null)", h => h.Many.Any(m => m.Tags.Contains(none!)),
            h => h.Many.Any(m => V(m).Tags.Contains(null!)));
        yield return new("null constant", h => h.Many.Any(m => m.Maybe.Contains(null)), h => h.Many.Any(m => V(m).Maybe.Contains(null)));
        yield return new("null constant, negated", h => h.Many.Any(m => !m.Maybe.Contains(null)), h => h.Many.Any(m => !V(m).Maybe.Contains(null)));
        yield return new("nullable parameter", h => h.Many.Any(m => m.Maybe.Contains(maybe)), h => h.Many.Any(m => V(m).Maybe.Contains(5)));
        yield return new("null nullable parameter", h => h.Many.Any(m => m.Maybe.Contains(noInt)), h => h.Many.Any(m => V(m).Maybe.Contains(null)));
        // EF extracts the converted value (int to int?) as a nullable parameter, so this is the nullable-parameter case.
        yield return new("non-nullable captured value against a nullable element type", h => h.Many.Any(m => m.Maybe.Contains(five)),
            h => h.Many.Any(m => V(m).Maybe.Contains(5)));
        yield return new("root list, constant", h => h.Top.Contains(5), h => h.Top.Contains(5));
        yield return new("root list, parameter", h => h.Top.Contains(five), h => h.Top.Contains(5));
        yield return new("root list, negated", h => !h.Top.Contains(5), h => !h.Top.Contains(5));
        yield return new("root list, in a ternary (aggregation dialect)", h => (h.Top.Contains(5) ? 1 : 0) == 1, h => (h.Top.Contains(5) ? 1 : 0) == 1);
        yield return new("root list, beside an element predicate", h => h.Top.Contains(5) || h.Many.Any(m => m.Nums.Contains(7)),
            h => h.Top.Contains(5) || h.Many.Any(m => V(m).Nums.Contains(7)));
    }

    // The native path declines these, so the fallback would run them over a null element: refused under Native, undecided
    // (NotSupported) under NativeOnly, and fine under explicit DriverLinq over data that holds no null element.
    private static IEnumerable<Case> DeclinedCases()
    {
        // Not natively translated whatever its predicate (a Where(..).Count() chain), so it reaches the null-element refusal.
        yield return new("Where(..).Count() chain", h => h.Many.Where(m => m.Nums.Contains(5)).Count() == 1,
            h => h.Many.Where(m => V(m).Nums.Contains(5)).Count() == 1);
        yield return new("element member as the item", h => h.Many.Any(m => m.Nums.Contains(m.Label.Length)),
            h => h.Many.Any(m => V(m).Nums.Contains(V(m).Label.Length)));
    }

    private static List<string> Run(
        IMongoCollection<TypedHolder> collection, MongoQueryMode mode, Expression<Func<TypedHolder, bool>> query, string label = "")
    {
        using var db = Context(collection, mode);
        try
        {
            return db.Entities.Where(query).OrderBy(h => h.Name).Select(h => h.Name).ToList();
        }
        catch (Exception ex) when (label.Length > 0 && ex is not Xunit.Sdk.XunitException)
        {
            throw new InvalidOperationException($"{mode}: {label}: {ex.Message}", ex);
        }
    }

    private static List<string> Expected(bool withNullElements, Func<TypedHolder, bool> expected)
        => [.. Oracle(withNullElements).Where(expected).Select(h => h.Name).OrderBy(n => n, StringComparer.Ordinal)];

    [Fact]
    public void Contains_over_an_element_primitive_collection_is_served_natively_and_matches_in_memory_LINQ()
    {
        var collection = Seed(nameof(Contains_over_an_element_primitive_collection_is_served_natively_and_matches_in_memory_LINQ), withNullElements: true);

        foreach (var (label, query, expected) in ServedCases())
        {
            var rows = Expected(withNullElements: true, expected);
            // NativeOnly proves it did not fall back; Native may not have fallen back either (it would be refused on this data).
            Assert.True(rows.SequenceEqual(Run(collection, MongoQueryMode.NativeOnly, query, label)), $"NativeOnly: {label}");
            Assert.True(rows.SequenceEqual(Run(collection, MongoQueryMode.Native, query, label)), $"Native: {label}");
        }
    }

    [Fact]
    public void The_same_cases_agree_with_driver_LINQ_over_data_with_no_null_element()
    {
        var collection = Seed(nameof(The_same_cases_agree_with_driver_LINQ_over_data_with_no_null_element), withNullElements: false);

        foreach (var (label, query, expected) in ServedCases())
        {
            var rows = Expected(withNullElements: false, expected);
            Assert.True(rows.SequenceEqual(Run(collection, MongoQueryMode.NativeOnly, query, label)), $"NativeOnly: {label}");
            Assert.True(rows.SequenceEqual(Run(collection, MongoQueryMode.Native, query, label)), $"Native: {label}");
            Assert.True(rows.SequenceEqual(Run(collection, MongoQueryMode.DriverLinq, query, label)), $"DriverLinq: {label}");
        }
    }

    [Fact]
    public void A_null_element_still_fails_the_whole_query_on_explicit_driver_LINQ()
    {
        // The reason native translation matters: the driver's `$in` over a null element's missing array.
        var collection = Seed(nameof(A_null_element_still_fails_the_whole_query_on_explicit_driver_LINQ), withNullElements: true);
        Assert.Throws<MongoCommandException>(() => Run(collection, MongoQueryMode.DriverLinq, h => h.Many.Any(m => m.Nums.Contains(5))));
    }

    [Fact]
    public void Shapes_the_native_path_declines_are_refused_over_a_null_element_and_served_by_explicit_driver_LINQ()
    {
        var withNull = Seed(nameof(Shapes_the_native_path_declines_are_refused_over_a_null_element_and_served_by_explicit_driver_LINQ) + "N", withNullElements: true);
        var clean = Seed(nameof(Shapes_the_native_path_declines_are_refused_over_a_null_element_and_served_by_explicit_driver_LINQ) + "C", withNullElements: false);

        foreach (var (label, query, expected) in DeclinedCases())
        {
            Assert.True(
                Throws<NativeTranslationNotSupportedException>(() => Run(withNull, MongoQueryMode.NativeOnly, query)), $"NativeOnly: {label}");
            Assert.True(
                Throws<NativeTranslationNotSupportedException>(() => Run(withNull, MongoQueryMode.Native, query)), $"Native: {label}");

            // The refusal is structural: it applies over data that holds no null element too (a deliberate tightening of Native
            // for shapes the native path cannot serve; explicit DriverLinq is the opt-in).
            Assert.True(
                Throws<NativeTranslationNotSupportedException>(() => Run(clean, MongoQueryMode.Native, query)), $"Native, clean data: {label}");

            // Explicit DriverLinq is the opt-in.
            Assert.True(
                Expected(withNullElements: false, expected).SequenceEqual(Run(clean, MongoQueryMode.DriverLinq, query, label)), $"DriverLinq: {label}");
        }
    }

    [Fact]
    public void Contains_in_a_projection_over_an_element_primitive_collection_matches_in_memory_LINQ()
    {
        var collection = Seed(nameof(Contains_in_a_projection_over_an_element_primitive_collection_matches_in_memory_LINQ), withNullElements: true);
        var oracle = Oracle(withNullElements: true).OrderBy(h => h.Name, StringComparer.Ordinal).ToList();

        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            using var db = Context(collection, mode);

            Project(mode, "Count", oracle.Select(h => h.Many.Count(m => V(m).Nums.Contains(5))),
                () => db.Entities.OrderBy(h => h.Name).Select(h => h.Many.Count(m => m.Nums.Contains(5))).ToList());
        }
    }

    private static void Project<T>(MongoQueryMode mode, string label, IEnumerable<T> expected, Func<List<T>> run)
    {
        try
        {
            Assert.Equal(expected, run());
        }
        catch (Exception ex) when (ex is not Xunit.Sdk.XunitException)
        {
            throw new InvalidOperationException($"{mode}: {label}: {ex.Message}", ex);
        }
    }

    [Fact]
    public void A_cached_plan_reads_the_parameter_per_execution()
    {
        var collection = Seed(nameof(A_cached_plan_reads_the_parameter_per_execution), withNullElements: true);
        var oracle = Oracle(withNullElements: true);

        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            using var db = Context(collection, mode);
            foreach (var n in new[] { 5, 7, 8, 99, 5 })
            {
                Assert.Equal(
                    [.. oracle.Where(h => h.Many.Any(m => V(m).Nums.Contains(n))).Select(h => h.Name).OrderBy(x => x, StringComparer.Ordinal)],
                    db.Entities.Where(h => h.Many.Any(m => m.Nums.Contains(n))).OrderBy(h => h.Name).Select(h => h.Name).ToList());
            }

            foreach (var tag in new[] { "x", "z", null, "$y", "nope" })
            {
                Assert.Equal(
                    [.. oracle.Where(h => h.Many.Any(m => V(m).Tags.Contains(tag!))).Select(h => h.Name).OrderBy(x => x, StringComparer.Ordinal)],
                    db.Entities.Where(h => h.Many.Any(m => m.Tags.Contains(tag!))).OrderBy(h => h.Name).Select(h => h.Name).ToList());
            }
        }
    }

    [Fact]
    public void Contains_as_a_sort_key_matches_in_memory_LINQ()
    {
        var collection = Seed(nameof(Contains_as_a_sort_key_matches_in_memory_LINQ), withNullElements: true);
        var oracle = Oracle(withNullElements: true).OrderBy(h => h.Top.Contains(5)).ThenBy(h => h.Name, StringComparer.Ordinal)
            .Select(h => h.Name).ToList();

        foreach (var mode in new[] { MongoQueryMode.Native })
        {
            using var db = Context(collection, mode);
            Assert.Equal(oracle, db.Entities.OrderBy(h => h.Top.Contains(5)).ThenBy(h => h.Name).Select(h => h.Name).ToList());
        }
    }

    private static bool Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
            return false;
        }
        catch (TException)
        {
            return true;
        }
    }
}
#endif
