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
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Metadata.Conventions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Materializing entities that have complex properties, and whole complex values (bare, inside anonymous types, beside
/// the entity), on the native path: the one-pass streaming materializer and the DOM shaper. Each query shape runs under
/// <see cref="MongoQueryMode.NativeOnly"/>, <see cref="MongoQueryMode.Native"/> and <see cref="MongoQueryMode.DriverLinq"/>
/// against a hand-written answer.
/// </summary>
/// <remarks>
/// <para>
/// Read rules (decided for this slice and documented in Query <c>AGENTS.md</c>): a complex value is structural, so it is
/// read strictly, like a whole entity. A REQUIRED complex property whose element is missing or BSON null throws
/// <see cref="InvalidOperationException"/> (never a null instance or a default struct); an OPTIONAL one (EF10) reads
/// <see langword="null"/>. A required leaf inside a complex value follows the entity-member rules (missing throws,
/// explicit null of a reference type throws). A wrong BSON type throws <see cref="FormatException"/>, as a wrong-typed
/// scalar does. Element order inside the stored subdocument does not matter and unmapped elements are ignored.
/// </para>
/// <para>
/// Documents are seeded as raw BSON so that missing, null, reordered and extra elements can be expressed.
/// </para>
/// </remarks>
[XUnitCollection("QueryTests")]
public class ComplexTypeMaterializationTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public enum Tier
    {
        Bronze,
        Silver,
        Gold
    }

    public class MAddress
    {
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public GeoPoint Location { get; set; }
        public int Floor { get; set; }
        public int? Zip { get; set; }
        public Tier Tier { get; set; }
    }

    public class MCustomer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public MAddress Address { get; set; } = null!;
        public GeoPoint Pin { get; set; }
    }

    private static void ConfigureCustomer(ModelBuilder mb)
    {
        mb.Entity<MCustomer>().ComplexProperty(c => c.Address, a => a.ComplexProperty(x => x.Location));
        mb.Entity<MCustomer>().ComplexProperty(c => c.Pin);
    }

    private static BsonDocument Geo(double lat, double lon) => new() { { "Lat", lat }, { "Lon", lon } };

    // Three well-formed rows. Bob's subdocuments store their elements in a different order (nested document first,
    // leaves reversed) and carry unmapped elements; his Zip is missing and Cid's is BSON null.
    private IMongoCollection<MCustomer> SeedCustomers(string name)
    {
        var collection = database.CreateCollection<MCustomer>(Unique(name));
        Raw(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Ann" }, { "Rank", 3 },
                {
                    "Address", new BsonDocument
                    {
                        { "Street", "Main" }, { "City", "Paris" }, { "Location", Geo(1.5, 10) }, { "Floor", 2 }, { "Zip", 7 },
                        { "Tier", (int)Tier.Gold }
                    }
                },
                { "Pin", Geo(0.25, 0.5) }
            },
            new BsonDocument
            {
                { "Pin", new BsonDocument { { "Lon", 2.0 }, { "Extra", true }, { "Lat", 1.0 } } },
                {
                    "Address", new BsonDocument
                    {
                        { "Location", new BsonDocument { { "Lon", 20.0 }, { "Unmapped", "u" }, { "Lat", 0.5 } } },
                        { "Unmapped", new BsonDocument { { "a", 1 } } }, { "Tier", (int)Tier.Bronze }, { "Floor", 5 },
                        { "City", "London" }, { "Street", "High" }
                    }
                },
                { "Rank", 1 }, { "Name", "Bob" }, { "_id", ObjectId.GenerateNewId() }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Cid" }, { "Rank", 2 },
                {
                    "Address", new BsonDocument
                    {
                        { "Street", "Low" }, { "City", "Rome" }, { "Location", Geo(2.5, 30) }, { "Floor", 1 },
                        { "Zip", BsonNull.Value }, { "Tier", (int)Tier.Silver }
                    }
                },
                { "Pin", Geo(3, 4) }
            }
        ]);
        return collection;
    }

    private static readonly string[] AllCustomers =
    [
        "Ann|Main|Paris|1.5,10|2|7|Gold|pin:0.25,0.5",
        "Bob|High|London|0.5,20|5|-|Bronze|pin:1,2",
        "Cid|Low|Rome|2.5,30|1|-|Silver|pin:3,4"
    ];

    private static string Fmt(MCustomer c) => c.Name + "|" + Fmt(c.Address) + "|pin:" + Fmt(c.Pin);

    private static string Fmt(MAddress? a)
        => a == null
            ? "<null>"
            : $"{a.Street}|{a.City}|{Fmt(a.Location)}|{a.Floor}|{a.Zip?.ToString() ?? "-"}|{a.Tier}";

    private static string Fmt(GeoPoint p) => $"{p.Lat},{p.Lon}";

    private List<T> Run<TEntity, T>(
        IMongoCollection<TEntity> collection,
        MongoQueryMode mode,
        Func<IQueryable<TEntity>, IEnumerable<T>> query,
        Action<ModelBuilder> configure,
        QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTracking,
        Action<ModelConfigurationBuilder>? conventions = null,
        Action<DbContext>? inspect = null)
        where TEntity : class
    {
        using var db = Context(collection, mode, configure, tracking, conventions);
        var result = query(db.Entities).ToList();
        inspect?.Invoke(db);
        return result;
    }

    private static SingleEntityDbContext<TEntity> Context<TEntity>(
        IMongoCollection<TEntity> collection,
        MongoQueryMode mode,
        Action<ModelBuilder> configure,
        QueryTrackingBehavior tracking = QueryTrackingBehavior.TrackAll,
        Action<ModelConfigurationBuilder>? conventions = null)
        where TEntity : class
        => SingleEntityDbContext.Create(collection, configure, conventions, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            b.UseQueryTrackingBehavior(tracking);
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    private List<T> Customers<T>(
        IMongoCollection<MCustomer> collection, MongoQueryMode mode, Func<IQueryable<MCustomer>, IEnumerable<T>> query,
        QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTracking, Action<DbContext>? inspect = null)
        => Run(collection, mode, query, ConfigureCustomer, tracking, inspect: inspect);

    // ── Whole entities ──────────────────────────────────────────────────────────────────────────────────────────

    public static TheoryData<QueryTrackingBehavior> TrackingBehaviors
        => [QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking, QueryTrackingBehavior.NoTrackingWithIdentityResolution];

    [Theory]
    [MemberData(nameof(TrackingBehaviors))]
    public void Entity_query_materializes_class_struct_and_nested_complex_values(QueryTrackingBehavior tracking)
    {
        var collection = SeedCustomers(nameof(Entity_query_materializes_class_struct_and_nested_complex_values) + tracking);

        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name).ToList().Select(Fmt), tracking,
                db => Assert.Equal(tracking == QueryTrackingBehavior.TrackAll ? 3 : 0, db.ChangeTracker.Entries().Count())),
            [.. AllCustomers]);
    }

    [Fact]
    public void One_pass_and_DOM_materializers_agree()
    {
        var collection = SeedCustomers(nameof(One_pass_and_DOM_materializers_agree));

        // The plain whole-entity ToList is streaming-eligible, so NativeOnly runs the one-pass materializer (an
        // un-streamable shape would throw under NativeOnly instead of falling back to DOM).
        using (var db = Context(collection, MongoQueryMode.NativeOnly, ConfigureCustomer))
        {
            Assert.True(StreamingEligibility.IsEligible(db.Model.FindEntityType(typeof(MCustomer))!));
        }

        // One pass (ToList), native DOM (First and the $$ROOT leaf of an anonymous projection) and driver-LINQ DOM.
        var onePass = Customers(collection, MongoQueryMode.NativeOnly, q => q.ToList().Select(Fmt).Order());
        var domFirst = Customers(collection, MongoQueryMode.NativeOnly,
            q => new[] { "Ann", "Bob", "Cid" }.Select(n => Fmt(q.First(c => c.Name == n))));
        var domRoot = Customers(collection, MongoQueryMode.NativeOnly,
            q => q.Select(c => new { c, c.Rank }).ToList().Select(x => Fmt(x.c)).Order());
        var driver = Customers(collection, MongoQueryMode.DriverLinq, q => q.ToList().Select(Fmt).Order());

        Assert.Equal(AllCustomers, onePass);
        Assert.Equal(AllCustomers, domFirst);
        Assert.Equal(AllCustomers, domRoot);
        Assert.Equal(AllCustomers, driver);
    }

    [Fact]
    public void Cardinality_and_paging_return_materialized_entities()
    {
        var collection = SeedCustomers(nameof(Cardinality_and_paging_return_materialized_entities));

        NativeModeAssert.NativeAndExpected(m => Customers(collection, m, q => new[] { Fmt(q.OrderBy(c => c.Name).First()) }), [AllCustomers[0]]);
        NativeModeAssert.NativeAndExpected(m => Customers(collection, m, q => new[] { Fmt(q.Single(c => c.Address.City == "Rome")) }), [AllCustomers[2]]);
        NativeModeAssert.NativeAndExpected(m => Customers(collection, m, q => new[] { Fmt(q.FirstOrDefault(c => c.Address.Location.Lat > 2)!) }), [AllCustomers[2]]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderByDescending(c => c.Rank).Take(2).ToList().Select(Fmt)), [AllCustomers[0], AllCustomers[2]]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Address.Floor).Skip(1).ToList().Select(Fmt)), [AllCustomers[0], AllCustomers[1]]);
    }

    // ── Whole complex values ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Whole_complex_value_projection_is_not_tracked()
    {
        var collection = SeedCustomers(nameof(Whole_complex_value_projection_is_not_tracked));

        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name).Select(c => c.Address).ToList().Select(Fmt),
                QueryTrackingBehavior.TrackAll, db => Assert.Empty(db.ChangeTracker.Entries())),
            ["Main|Paris|1.5,10|2|7|Gold", "High|London|0.5,20|5|-|Bronze", "Low|Rome|2.5,30|1|-|Silver"]);
    }

    [Fact]
    public void Anonymous_projection_with_a_complex_value()
    {
        var collection = SeedCustomers(nameof(Anonymous_projection_with_a_complex_value));

        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name).Select(c => new { c.Name, c.Address, c.Pin }).ToList()
                .Select(x => x.Name + "|" + Fmt(x.Address) + "|" + Fmt(x.Pin))),
            ["Ann|Main|Paris|1.5,10|2|7|Gold|0.25,0.5", "Bob|High|London|0.5,20|5|-|Bronze|1,2", "Cid|Low|Rome|2.5,30|1|-|Silver|3,4"]);
    }

    [Fact]
    public void Struct_complex_value_projection()
    {
        var collection = SeedCustomers(nameof(Struct_complex_value_projection));

        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name).Select(c => c.Address.Location).ToList().Select(Fmt)),
            ["1.5,10", "0.5,20", "2.5,30"]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name).Select(c => c.Pin).ToList().Select(Fmt)),
            ["0.25,0.5", "1,2", "3,4"]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name).Select(c => new { c.Address.Location }).ToList().Select(x => Fmt(x.Location))),
            ["1.5,10", "0.5,20", "2.5,30"]);
    }

    [Fact]
    public void EF_Property_spellings_of_whole_complex_values()
    {
        var collection = SeedCustomers(nameof(EF_Property_spellings_of_whole_complex_values));

        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name).Select(c => EF.Property<MAddress>(c, "Address")).ToList().Select(a => a.City)),
            ["Paris", "London", "Rome"]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name)
                .Select(c => new { A = EF.Property<MAddress>(c, "Address") }).ToList().Select(x => x.A.Street)),
            ["Main", "High", "Low"]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name)
                .Select(c => EF.Property<GeoPoint>(EF.Property<MAddress>(c, "Address"), "Location")).ToList().Select(Fmt)),
            ["1.5,10", "0.5,20", "2.5,30"]);
    }

    [Fact]
    public void Paging_and_cardinality_over_a_complex_value_projection()
    {
        var collection = SeedCustomers(nameof(Paging_and_cardinality_over_a_complex_value_projection));

        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => new[] { Fmt(q.OrderBy(c => c.Name).Select(c => c.Address).First()) }),
            ["Main|Paris|1.5,10|2|7|Gold"]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => new[] { Fmt(q.Where(c => c.Rank == 2).Select(c => c.Address.Location).Single()) }),
            ["2.5,30"]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderByDescending(c => c.Rank).Select(c => c.Address).Take(2).ToList().Select(a => a.City)),
            ["Paris", "Rome"]);
    }

    public static TheoryData<string> ValueReadingOperatorShapes =>
    [
        "distinct_bare", "distinct_anon", "distinct_nested_anon", "distinct_count", "distinct_after_source_ops",
        "concat_bare", "union_bare", "union_anon", "intersect_bare", "except_bare", "concat_struct"
    ];

    // Every mode's outcome as one line, so a failure reports all three at once.
    private static string Outcomes(Func<MongoQueryMode, List<string>> run)
        => string.Join(" || ", new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq }.Select(mode =>
        {
            try
            {
                return $"{mode}: rows [{string.Join("; ", run(mode))}]";
            }
            catch (Exception e)
            {
                return $"{mode}: {e.GetType().Name}: {e.Message}";
            }
        }));

    private const string ComplexOperandRefusal = "cannot be the operand of";

    private static void AssertRefusedInEveryMode(Func<MongoQueryMode, List<string>> run, string operatorName)
    {
        var outcomes = Outcomes(run).Split(" || ");
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            var outcome = outcomes.Single(o => o.StartsWith(mode + ":"));
            Assert.True(
                outcome.StartsWith($"{mode}: NotSupportedException: ")
                && outcome.Contains($"cannot be the operand of '{operatorName}'"),
                $"{mode}: expected the R7 NotSupportedException for '{operatorName}', got: {outcome}");
        }
    }

    public record PairRecord(string Name, MAddress Address);

    public class PairCtor(string name, MAddress address)
    {
        public string Name { get; } = name;
        public MAddress Address { get; } = address;
    }

    public class PairDto
    {
        public string Name { get; set; } = null!;
        public MAddress Address { get; set; } = null!;
    }

    public static TheoryData<string, string> ConstructionOperatorShapes()
    {
        var data = new TheoryData<string, string>();
        foreach (var construction in new[] { "ctor", "record", "memberinit", "anon", "nested_anon" })
        foreach (var op in new[] { "Distinct", "Union", "Concat", "Intersect", "Except" })
        {
            data.Add(construction, op);
        }

        return data;
    }

    private static IEnumerable<string> ApplyOperator<T>(IQueryable<T> left, IQueryable<T> right, string op)
        => (op switch
        {
            "Distinct" => left.Distinct(),
            "Union" => left.Union(right),
            "Concat" => left.Concat(right),
            "Intersect" => left.Intersect(right),
            "Except" => left.Except(right),
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        }).ToList().Select(x => x!.ToString()!);

    [Theory]
    [MemberData(nameof(ConstructionOperatorShapes))]
    public void Operators_over_a_construction_holding_a_complex_value_are_refused_in_every_mode(string construction, string op)
    {
        // Every construction arm of the projection binder (positional ctor, record, member-init, anonymous, nested) must
        // set the refusal flag (fix round 2: the positional-ctor arm used to bypass it and fall back unguarded).
        var collection = SeedCustomers(nameof(Operators_over_a_construction_holding_a_complex_value_are_refused_in_every_mode) + construction + op);
        AssertRefusedInEveryMode(m => Customers(collection, m, q => construction switch
        {
            "ctor" => ApplyOperator(q.Select(c => new PairCtor(c.Name, c.Address)), q.Select(c => new PairCtor(c.Name, c.Address)), op),
            "record" => ApplyOperator(q.Select(c => new PairRecord(c.Name, c.Address)), q.Select(c => new PairRecord(c.Name, c.Address)), op),
            "memberinit" => ApplyOperator(q.Select(c => new PairDto { Name = c.Name, Address = c.Address }),
                q.Select(c => new PairDto { Name = c.Name, Address = c.Address }), op),
            "anon" => ApplyOperator(q.Select(c => new { c.Name, c.Address }), q.Select(c => new { c.Name, c.Address }), op),
            _ => ApplyOperator(q.Select(c => new { c.Name, Inner = new { c.Address } }), q.Select(c => new { c.Name, Inner = new { c.Address } }), op)
        }), op);
    }

    [Fact]
    public void Member_init_and_anonymous_constructions_holding_a_complex_value_materialize()
    {
        var collection = SeedCustomers(nameof(Member_init_and_anonymous_constructions_holding_a_complex_value_materialize));
        NativeModeAssert.NativeAndExpected(m => Customers(collection, m, q => q.OrderBy(c => c.Name)
            .Select(c => new PairDto { Name = c.Name, Address = c.Address }).ToList().Select(x => x.Name + "|" + Fmt(x.Address))),
            ["Ann|Main|Paris|1.5,10|2|7|Gold", "Bob|High|London|0.5,20|5|-|Bronze", "Cid|Low|Rome|2.5,30|1|-|Silver"]);
        // A nested construction only admits plain top-level fields natively (TryGetDocumentConstructionLeaf), so it declines;
        // the mixed shaper reads the complex value off the whole document.
        Assert.Equal(["Ann|Paris", "Bob|London", "Cid|Rome"], NativeModeAssert.DeclinesCleanly(m => Customers(collection, m, q => q.OrderBy(c => c.Name)
            .Select(c => new { c.Name, Inner = new { c.Address } }).ToList().Select(x => x.Name + "|" + x.Inner.Address.City))));
    }

    [Fact]
    public void Positional_constructor_with_a_complex_argument_is_refused_clearly_in_every_mode()
    {
        // The native positional-ctor shaper reads arguments by index through a driver class map, which would misread a
        // complex value (model element names ignored; FormatException on unmapped elements), so the positional arm declines
        // a complex argument (NativeProjectionBinder.IsScalarPositionalConstruction). The fallback then refuses the
        // memberless construction with its existing clear message. Never rows. Follow-up: positional ctors over complex values.
        var collection = SeedCustomers(nameof(Positional_constructor_with_a_complex_argument_is_refused_clearly_in_every_mode));
        foreach (var run in new Func<MongoQueryMode, List<string>>[]
                 {
                     m => Customers(collection, m, q => q.Select(c => new PairCtor(c.Name, c.Address)).ToList().Select(x => x.Name)),
                     m => Customers(collection, m, q => q.Select(c => new PairRecord(c.Name, c.Address)).ToList().Select(x => x.Name))
                 })
        {
            Assert.Throws<NativeTranslationNotSupportedException>(() => run(MongoQueryMode.NativeOnly));
            foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.DriverLinq })
            {
                var ex = Assert.Throws<InvalidOperationException>(() => run(mode));
                Assert.Contains("from arguments that can't each be read from the document", ex.Message);
            }
        }
    }

    public static TheoryData<string> OtherValueReadingOperatorShapes => ["contains", "all", "max_selector", "cast_distinct", "distinct_count"];

    [Theory]
    [MemberData(nameof(OtherValueReadingOperatorShapes))]
    public void Other_value_reading_operators_over_a_complex_value_never_return_wrong_rows(string shape)
    {
        // Outcomes measured and pinned per mode (clear and loud; never rows built from a missing element).
        var collection = SeedCustomers(nameof(Other_value_reading_operators_over_a_complex_value_never_return_wrong_rows) + shape);
        var outcome = Outcomes(m => Customers(collection, m, q => shape switch
        {
            "contains" => new[] { q.Select(c => c.Address).Contains(new MAddress()).ToString() },
            "all" => new[] { q.Select(c => c.Address).All(a => a.Floor > 0).ToString() },
            "max_selector" => new[] { q.Select(c => c.Address).Max(a => a.Floor).ToString() },
            "cast_distinct" => q.Select(c => c.Address).Cast<object>().Distinct().ToList().Select(x => x.ToString()!),
            _ => new[] { q.Select(c => c.Address).Distinct().Count().ToString() }
        }));
        ExpectedOtherOperatorOutcomes.TryGetValue(shape, out var expected);
        Assert.True(expected != null && expected.All(e => outcome.Contains(e)), $"{shape}: {outcome}");
    }

    // Per-shape fragments every mode's outcome must contain, measured (fix round 2): EF folds All/Max(selector) over the
    // source (correct rows); Cast is folded away so Distinct is refused. GroupBy shapes: GroupBy_over_a_whole_complex_value….
    private static readonly Dictionary<string, string[]> ExpectedOtherOperatorOutcomes = new()
    {
        ["contains"] = R7("Contains"),
        ["cast_distinct"] = R7("Distinct"),
        ["distinct_count"] = R7("Distinct"),
        ["all"] = ["NativeOnly: rows [True]", "Native: rows [True]", "DriverLinq: rows [True]"],
        ["max_selector"] = ["NativeOnly: rows [5]", "Native: rows [5]", "DriverLinq: rows [5]"],
    };

    private static string[] R7(string op)
        => [.. new[] { "NativeOnly", "Native", "DriverLinq" }.Select(m => $"{m}: NotSupportedException: A projected whole complex value cannot be the operand of '{op}'")];

    [Theory]
    [MemberData(nameof(ValueReadingOperatorShapes))]
    public void Operators_that_read_a_projected_complex_value_are_refused_in_every_mode(string shape)
    {
        // A projected complex value is its STORED subdocument, not the CLR value, and no path reads it back correctly as an
        // operand: the native pipeline would dedupe/combine stored forms, and the driver-LINQ push-down can't deserialize a
        // complex value at all (ComplexTypeSerializer). So a value-reading operator over it is refused with a clear message
        // in every mode (ruling R7), never answered from the reader's missing-element rule.
        var collection = SeedCustomers(nameof(Operators_that_read_a_projected_complex_value_are_refused_in_every_mode) + shape);
        var (query, op) = shape switch
        {
            "distinct_bare" => ((Func<IQueryable<MCustomer>, IEnumerable<string>>)(q => q.Select(c => c.Address).Distinct().ToList().Select(Fmt)), "Distinct"),
            "distinct_anon" => (q => q.Select(c => new { c.Name, c.Address }).Distinct().ToList().Select(x => x.Name), "Distinct"),
            "distinct_nested_anon" => (q => q.Select(c => new { c.Name, Inner = new { c.Address } }).Distinct().ToList().Select(x => x.Name), "Distinct"),
            "distinct_count" => (q => new[] { q.Select(c => c.Address).Distinct().Count().ToString() }, "Distinct"),
            "distinct_after_source_ops" => (q => q.Where(c => c.Rank > 0).OrderBy(c => c.Name).Select(c => c.Address).Distinct().ToList().Select(Fmt), "Distinct"),
            "concat_bare" => (q => q.Select(c => c.Address).Concat(q.Select(c => c.Address)).ToList().Select(Fmt), "Concat"),
            "union_bare" => (q => q.Select(c => c.Address).Union(q.Select(c => c.Address)).ToList().Select(Fmt), "Union"),
            "union_anon" => (q => q.Select(c => new { c.Name, c.Address }).Union(q.Select(c => new { c.Name, c.Address })).ToList().Select(x => x.Name), "Union"),
            "intersect_bare" => (q => q.Select(c => c.Address).Intersect(q.Select(c => c.Address)).ToList().Select(Fmt), "Intersect"),
            "except_bare" => (q => q.Select(c => c.Address).Except(q.Select(c => c.Address)).ToList().Select(Fmt), "Except"),
            "concat_struct" => (q => q.Select(c => c.Pin).Concat(q.Select(c => c.Pin)).ToList().Select(Fmt), "Concat"),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };

        AssertRefusedInEveryMode(m => Customers(collection, m, q => query(q).ToList()), op);
    }

    public static TheoryData<string> GroupByOverComplexValueShapes
        => ["key", "anon_key_part", "grouped_select_complex", "grouped_first_complex"];

    [Theory]
    [MemberData(nameof(GroupByOverComplexValueShapes))]
    public void GroupBy_over_a_whole_complex_value_is_refused_in_every_mode(string shape)
    {
        // Ruling R8: a whole complex value taking part in a grouping (the key, a key part, or read off the grouped elements in
        // the result) is refused with the R7 message naming GroupBy in every mode. Before, these reached EF's
        // ConstantVerifyingExpressionVisitor over an unbound GroupByShaperExpression ("Calling
        // 'ShapedQueryExpression.VisitChildren' is not allowed"; stack in the fix round 3 report).
        var collection = SeedCustomers(nameof(GroupBy_over_a_whole_complex_value_is_refused_in_every_mode) + shape);
        AssertRefusedInEveryMode(m => Customers(collection, m, q => shape switch
        {
            "key" => q.GroupBy(c => c.Address).Select(g => g.Count()).ToList().Select(x => x.ToString()),
            "anon_key_part" => q.GroupBy(c => new { c.Name, c.Address }).Select(g => g.Count()).ToList().Select(x => x.ToString()),
            "grouped_select_complex" => q.GroupBy(c => c.Name).Select(g => new { g.Key, A = g.Select(x => x.Address) }).Distinct().ToList().Select(x => x.Key),
            _ => q.GroupBy(c => c.Name).Select(g => new { g.Key, A = g.First().Address }).ToList().Select(x => x.Key)
        }), "GroupBy");
    }

    [Fact]
    public void GroupBy_whose_complex_value_EF_erases_behaves_like_its_scalar_analogue()
    {
        // `Select(c => c.Address).GroupBy(a => a.City).Select(g => g.Key)`: EF rewrites it to GroupBy(c => c.Address.City) over
        // the ENTITY (a complex LEAF key) and reads only the key, so no complex value reaches translation; it is the
        // provider's pre-existing "grouping without an aggregate" shape, which fails exactly like its scalar analogue
        // (`Select(c => new { c.Name, c.Rank }).GroupBy(a => a.Name).Select(g => g.Key)`, measured identical). Not wrong rows.
        // And an element selector that is never read is dropped by EF: Count answers correctly.
        var collection = SeedCustomers(nameof(GroupBy_whose_complex_value_EF_erases_behaves_like_its_scalar_analogue));
        var complex = Outcomes(m => Customers(collection, m, q => q.Select(c => c.Address).GroupBy(a => a.City).Select(g => g.Key).ToList()));
        var scalar = Outcomes(m => Customers(collection, m, q => q.Select(c => new { c.Name, c.Rank }).GroupBy(a => a.Name).Select(g => g.Key).ToList()));
        Assert.Equal(scalar, complex);
        Assert.DoesNotContain("rows [", complex);

        // The GroupBy(key, resultSelector) overload is never translated by the provider (pre-existing: it never reaches
        // TranslateGroupBy; NativeOnly declines, the fallback fails building the executor with an ArgumentException), with
        // or without a complex value in the result selector (measured identical).
        var complexResult = Outcomes(m => Customers(collection, m, q => q.GroupBy(c => c.Rank, (k, g) => new { k, A = g.Select(x => x.Address) })
            .ToList().Select(x => x.k.ToString())));
        var scalarResult = Outcomes(m => Customers(collection, m, q => q.GroupBy(c => c.Rank, (k, g) => new { k, A = g.Select(x => x.Name) })
            .ToList().Select(x => x.k.ToString())));
        Assert.DoesNotContain("rows [", complexResult);
        Assert.Equal(scalarResult.Split(" || ").Select(o => o.Split(':')[0] + ":" + o.Split(':')[1]), complexResult.Split(" || ").Select(o => o.Split(':')[0] + ":" + o.Split(':')[1]));
        NativeModeAssert.NativeAndExpected(m => Customers(collection, m, q => q.GroupBy(c => c.Rank, c => c.Address).Select(g => g.Count()).ToList()
            .Select(x => x.ToString())), ["1", "1", "1"]);
    }

    [Fact]
    public void GroupBy_by_a_complex_leaf_stays_native()
    {
        var collection = SeedCustomers(nameof(GroupBy_by_a_complex_leaf_stays_native));
        NativeModeAssert.NativeAndExpected(m => Customers(collection, m, q => q.GroupBy(c => c.Address.City)
            .Select(g => new { g.Key, C = g.Count() }).ToList().Select(x => x.Key + ":" + x.C).Order()), ["London:1", "Paris:1", "Rome:1"]);
        NativeModeAssert.NativeAndExpected(m => Customers(collection, m, q => q.GroupBy(c => c.Address.Location.Lat)
            .Select(g => g.Count()).ToList().Select(x => x.ToString())), ["1", "1", "1"]);
    }

    [Fact]
    public void Projected_value_free_operators_over_a_complex_value_stay_native()
    {
        var collection = SeedCustomers(nameof(Projected_value_free_operators_over_a_complex_value_stay_native));
        NativeModeAssert.NativeAndExpected(m => Customers(collection, m, q => new[] { q.Select(c => c.Address).Count().ToString() }), ["3"]);
        NativeModeAssert.NativeAndExpected(m => Customers(collection, m, q => new[] { q.Select(c => c.Address).Any().ToString() }), ["True"]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name).Select(c => c.Address).Skip(1).Take(1).ToList().Select(a => a.City)), ["London"]);
    }

    [Fact]
    public void Where_and_OrderBy_after_a_complex_value_projection_fold_onto_the_source()
    {
        // EF folds `Select(c => c.Address).Where(a => a.City == ...)` into a predicate over the source before the provider
        // sees it, so these stay native and correct.
        var collection = SeedCustomers(nameof(Where_and_OrderBy_after_a_complex_value_projection_fold_onto_the_source));
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.Select(c => c.Address).Where(a => a.City == "Rome").ToList().Select(Fmt)),
            ["Low|Rome|2.5,30|1|-|Silver"]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.Select(c => c.Address).OrderBy(a => a.Floor).ToList().Select(a => a.City)),
            ["Rome", "Paris", "London"]);
    }

    [Fact]
    public void Whole_complex_values_survive_a_late_native_factory_decline()
    {
        // A parameterized StartsWith has no native rendering, so under Native the pipeline factory declines after the
        // gate and the same shaper reads driver-LINQ's documents: the bare leaf's Select is stripped (whole documents),
        // the dotted bare leaf keeps the driver's `_v`, and the wrapped leaf keeps its member-name alias.
        var collection = SeedCustomers(nameof(Whole_complex_values_survive_a_late_native_factory_decline));
        var prefix = "R";

        Assert.Equal(["Low|Rome|2.5,30|1|-|Silver"],
            Customers(collection, MongoQueryMode.Native, q => q.Where(c => c.Address.City.StartsWith(prefix)).Select(c => c.Address)).Select(Fmt));
        Assert.Equal(["2.5,30"],
            Customers(collection, MongoQueryMode.Native, q => q.Where(c => c.Address.City.StartsWith(prefix)).Select(c => c.Address.Location)).Select(Fmt));
        Assert.Equal(["Cid|Low|Rome|2.5,30|1|-|Silver"],
            Customers(collection, MongoQueryMode.Native, q => q.Where(c => c.Address.City.StartsWith(prefix))
                .Select(c => new { c.Name, c.Address }).ToList().Select(x => x.Name + "|" + Fmt(x.Address))));
        Assert.Equal([AllCustomers[2]],
            Customers(collection, MongoQueryMode.Native, q => q.Where(c => c.Address.City.StartsWith(prefix)).ToList().Select(Fmt)));
    }

    // ── Entity beside complex leaves (Task 9's interim pins), client-evaluated leaves ─────────────────────────

    [Fact]
    public void Entity_beside_a_complex_leaf_in_member_and_EF_Property_spellings()
    {
        var collection = SeedCustomers(nameof(Entity_beside_a_complex_leaf_in_member_and_EF_Property_spellings));

        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name).Select(c => new { c, c.Address.City }).ToList()
                .Select(x => Fmt(x.c) + "#" + x.City)),
            [AllCustomers[0] + "#Paris", AllCustomers[1] + "#London", AllCustomers[2] + "#Rome"]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name)
                .Select(c => new { c, City = EF.Property<string>(EF.Property<MAddress>(c, "Address"), "City") }).ToList()
                .Select(x => Fmt(x.c) + "#" + x.City)),
            [AllCustomers[0] + "#Paris", AllCustomers[1] + "#London", AllCustomers[2] + "#Rome"]);
        NativeModeAssert.NativeAndExpected(
            m => Customers(collection, m, q => q.OrderBy(c => c.Name).Select(c => new { c, c.Address }).ToList()
                .Select(x => x.c.Name + "#" + Fmt(x.Address))),
            ["Ann#Main|Paris|1.5,10|2|7|Gold", "Bob#High|London|0.5,20|5|-|Bronze", "Cid#Low|Rome|2.5,30|1|-|Silver"]);
    }

    public static string Tag(string value) => "<" + value + ">";

    [Fact]
    public void Client_evaluated_leaf_reads_the_stored_element_name_not_the_CLR_name()
    {
        // HasElementName model with a DECOY: the CLR-named element `Address` holds a different City. A complex leaf under a
        // client method is read by the mixed shaper off the whole document (Task 9 mutation MG: the read side's segment is
        // the complex property's element name, not its CLR name).
        var collection = database.CreateCollection<MCustomer>(Unique(nameof(Client_evaluated_leaf_reads_the_stored_element_name_not_the_CLR_name)));
        Raw(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Ann" }, { "Rank", 1 },
                {
                    "addr", new BsonDocument
                    {
                        { "Street", "Main" }, { "town", "Paris" }, { "geo", Geo(1, 2) }, { "Floor", 2 }, { "Tier", 0 }
                    }
                },
                { "Address", new BsonDocument { { "City", "Decoy" }, { "town", "Decoy" }, { "Street", "Decoy" } } },
                { "Pin", Geo(5, 6) }
            }
        ]);

        static void Configure(ModelBuilder mb)
        {
            mb.Entity<MCustomer>().ComplexProperty(c => c.Address, a =>
            {
                a.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "addr");
                a.Property(x => x.City).Metadata.SetElementName("town");
                a.ComplexProperty(x => x.Location).HasPropertyAnnotation(MongoAnnotationNames.ElementName, "geo");
            });
            mb.Entity<MCustomer>().ComplexProperty(c => c.Pin);
        }

        // A client method beside the entity is never native (NativeOnly declines it); Native and DriverLinq run the mixed
        // shaper, which reads the complex leaf and the entity's complex values off the whole document by element name.
        Assert.Equal(["<Paris>|Ann|Main|Paris|1,2|2|-|Bronze|pin:5,6"], NativeModeAssert.DeclinesCleanly(
            m => Run(collection, m, q => q.Select(c => new { c, T = Tag(c.Address.City) }).ToList().Select(x => x.T + "|" + Fmt(x.c)), Configure)));

        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.Select(c => c.Address).ToList().Select(Fmt), Configure),
            ["Main|Paris|1,2|2|-|Bronze"]);
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.Select(c => new { c.Name, c.Address.Location }).ToList().Select(x => Fmt(x.Location)), Configure),
            ["1,2"]);
    }

    // ── Malformed stored data (Review Focus 1) ──────────────────────────────────────────────────────────────────

    private IMongoCollection<MCustomer> SeedOne(string name, BsonDocument document)
    {
        var collection = database.CreateCollection<MCustomer>(Unique(name));
        Raw(collection).InsertOne(document);
        return collection;
    }

    private static BsonDocument Customer(BsonValue? address, BsonValue? pin)
    {
        var document = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "X" }, { "Rank", 1 } };
        if (address != null)
        {
            document["Address"] = address;
        }

        if (pin != null)
        {
            document["Pin"] = pin;
        }

        return document;
    }

    private static BsonDocument GoodAddress()
        => new() { { "Street", "s" }, { "City", "c" }, { "Location", Geo(1, 2) }, { "Floor", 1 }, { "Tier", 0 } };

    // Every read of the row: one pass (NativeOnly ToList), native DOM (First), driver-LINQ DOM, and the whole-value
    // projections. Each must throw `TException` with a message containing `message`.
    private void AssertEveryReadThrows<TException>(IMongoCollection<MCustomer> collection, string message, bool includeAddressProjection = true)
        where TException : Exception
    {
        var reads = new List<(string, Func<MongoQueryMode, object>)>
        {
            ("ToList", m => Customers(collection, m, q => q.ToList().Select(Fmt))),
            ("First", m => Customers(collection, m, q => new[] { Fmt(q.First()) })),
            ("Tracked", m => Customers(collection, m, q => q.ToList().Select(Fmt), QueryTrackingBehavior.TrackAll)),
            ("Root leaf", m => Customers(collection, m, q => q.Select(c => new { c, c.Rank }).ToList().Select(x => Fmt(x.c))))
        };
        if (includeAddressProjection)
        {
            reads.Add(("Address", m => Customers(collection, m, q => q.Select(c => c.Address).ToList().Select(Fmt))));
            reads.Add(("Anon Address", m => Customers(collection, m, q => q.Select(c => new { c.Name, c.Address }).ToList().Select(x => Fmt(x.Address)))));
        }

        foreach (var (name, read) in reads)
        {
            foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
            {
                var ex = Assert.ThrowsAny<Exception>(() => read(mode));
                Assert.True(ex is TException, $"{name} [{mode}]: expected {typeof(TException).Name}, got {ex.GetType().Name}: {ex.Message}");
                Assert.True(ex.Message.Contains(message), $"{name} [{mode}]: '{ex.Message}' does not contain '{message}'");
            }
        }
    }

    [Fact]
    public void Required_complex_property_missing_throws()
        => AssertEveryReadThrows<InvalidOperationException>(
            SeedOne(nameof(Required_complex_property_missing_throws), Customer(null, Geo(1, 2))),
            "Document element 'Address' is missing for required complex property 'MCustomer.Address'");

    [Fact]
    public void Required_complex_property_null_throws()
        => AssertEveryReadThrows<InvalidOperationException>(
            SeedOne(nameof(Required_complex_property_null_throws), Customer(BsonNull.Value, Geo(1, 2))),
            "Document element 'Address' is null for required complex property 'MCustomer.Address'");

    [Fact]
    public void Required_struct_complex_property_missing_or_null_throws_instead_of_reading_a_default_struct()
    {
        AssertEveryReadThrows<InvalidOperationException>(
            SeedOne(nameof(Required_struct_complex_property_missing_or_null_throws_instead_of_reading_a_default_struct) + "m",
                Customer(GoodAddress(), null)),
            "Document element 'Pin' is missing for required complex property 'MCustomer.Pin'", includeAddressProjection: false);
        AssertEveryReadThrows<InvalidOperationException>(
            SeedOne(nameof(Required_struct_complex_property_missing_or_null_throws_instead_of_reading_a_default_struct) + "n",
                Customer(GoodAddress(), BsonNull.Value)),
            "Document element 'Pin' is null for required complex property 'MCustomer.Pin'", includeAddressProjection: false);

        var collection = SeedOne(nameof(Required_struct_complex_property_missing_or_null_throws_instead_of_reading_a_default_struct) + "p",
            Customer(GoodAddress(), null));
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Customers(collection, mode, q => q.Select(c => c.Pin).ToList().Select(Fmt)));
            Assert.Contains("Document element 'Pin' is missing for required complex property 'MCustomer.Pin'", ex.Message);
        }
    }

    [Fact]
    public void Nested_required_complex_property_missing_throws()
    {
        var address = GoodAddress();
        address.Remove("Location");
        AssertEveryReadThrows<InvalidOperationException>(
            SeedOne(nameof(Nested_required_complex_property_missing_throws), Customer(address, Geo(1, 2))),
            "Document element 'Location' is missing for required complex property 'MCustomer.Address#MAddress.Location'");
    }

    [Fact]
    public void Required_leaf_missing_or_null_inside_a_complex_value_follows_the_entity_member_rules()
    {
        var missingCity = GoodAddress();
        missingCity.Remove("City");
        AssertEveryReadThrows<InvalidOperationException>(
            SeedOne(nameof(Required_leaf_missing_or_null_inside_a_complex_value_follows_the_entity_member_rules) + "m",
                Customer(missingCity, Geo(1, 2))),
            "Document element is missing for required non-nullable property 'City'");

        var nullCity = GoodAddress();
        nullCity["City"] = BsonNull.Value;
        AssertEveryReadThrows<InvalidOperationException>(
            SeedOne(nameof(Required_leaf_missing_or_null_inside_a_complex_value_follows_the_entity_member_rules) + "n",
                Customer(nullCity, Geo(1, 2))),
            "Document element is null for required non-nullable property 'City'");

        var missingFloor = GoodAddress();
        missingFloor.Remove("Floor");
        AssertEveryReadThrows<InvalidOperationException>(
            SeedOne(nameof(Required_leaf_missing_or_null_inside_a_complex_value_follows_the_entity_member_rules) + "v",
                Customer(missingFloor, Geo(1, 2))),
            "Document element is missing for required non-nullable property 'Floor'");
    }

    [Fact]
    public void Wrong_bson_type_throws_FormatException_like_a_wrong_typed_scalar()
    {
        var wrongLeaf = GoodAddress();
        wrongLeaf["City"] = new BsonDocument("x", 1);
        AssertEveryReadThrows<FormatException>(
            SeedOne(nameof(Wrong_bson_type_throws_FormatException_like_a_wrong_typed_scalar) + "leaf", Customer(wrongLeaf, Geo(1, 2))),
            "Cannot deserialize a 'String' from BsonType 'Document'");

        AssertEveryReadThrows<FormatException>(
            SeedOne(nameof(Wrong_bson_type_throws_FormatException_like_a_wrong_typed_scalar) + "value", Customer(5, Geo(1, 2))),
            "Cannot deserialize complex property 'MCustomer.Address' from BsonType 'Int32'");
    }

    // ── Element names ───────────────────────────────────────────────────────────────────────────────────────────

    public class TwoAddresses
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public MAddress Billing { get; set; } = null!;
        public MAddress Shipping { get; set; } = null!;
    }

    [Fact]
    public void Same_complex_clr_type_at_two_paths_with_different_element_names()
    {
        var collection = database.CreateCollection<TwoAddresses>(Unique(nameof(Same_complex_clr_type_at_two_paths_with_different_element_names)));
        var billing = GoodAddress();
        billing["City"] = "BillCity";
        billing["bc"] = "BillCity2";
        var shipping = GoodAddress();
        shipping["City"] = "ShipCity";
        shipping["loc"] = Geo(7, 8);
        shipping.Remove("Location");
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "Ann" }, { "bill", billing }, { "ship", shipping }
        });

        static void Configure(ModelBuilder mb)
        {
            mb.Entity<TwoAddresses>().ComplexProperty(c => c.Billing, a =>
            {
                a.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "bill");
                a.Property(x => x.City).Metadata.SetElementName("bc");
                a.ComplexProperty(x => x.Location);
            });
            mb.Entity<TwoAddresses>().ComplexProperty(c => c.Shipping, a =>
            {
                a.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "ship");
                a.ComplexProperty(x => x.Location).HasPropertyAnnotation(MongoAnnotationNames.ElementName, "loc");
            });
        }

        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.ToList().Select(c => Fmt(c.Billing) + "/" + Fmt(c.Shipping)), Configure),
            ["s|BillCity2|1,2|1|-|Bronze/s|ShipCity|7,8|1|-|Bronze"]);
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.Select(c => new { c.Billing, c.Shipping.Location }).ToList()
                .Select(x => Fmt(x.Billing) + "/" + Fmt(x.Location)), Configure),
            ["s|BillCity2|1,2|1|-|Bronze/7,8"]);
    }

    [Fact]
    public void Camel_case_convention_element_names()
    {
        var collection = database.CreateCollection<MCustomer>(Unique(nameof(Camel_case_convention_element_names)));
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "name", "Ann" }, { "rank", 1 },
            {
                "address", new BsonDocument
                {
                    { "street", "s" }, { "city", "Paris" }, { "location", new BsonDocument { { "lat", 1.0 }, { "lon", 2.0 } } },
                    { "floor", 3 }, { "zip", 9 }, { "tier", 2 }
                }
            },
            { "pin", new BsonDocument { { "lat", 5.0 }, { "lon", 6.0 } } }
        });

        static void Conventions(ModelConfigurationBuilder cb) => cb.Conventions.Add(_ => new CamelCaseElementNameConvention());

        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.ToList().Select(Fmt), ConfigureCustomer, conventions: Conventions),
            ["Ann|s|Paris|1,2|3|9|Gold|pin:5,6"]);
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.Select(c => c.Address).ToList().Select(Fmt), ConfigureCustomer, conventions: Conventions),
            ["s|Paris|1,2|3|9|Gold"]);
    }

    // ── Leaves read exactly like entity scalars ────────────────────────────────────────────────────────────────

    public class TypedLeaves
    {
        public int Converted { get; set; }
        public int Represented { get; set; }
        public Tier Tier { get; set; }
        public Guid Tag { get; set; }
        public DateTime Seen { get; set; }
        public decimal Price { get; set; }
        public string? Note { get; set; }
    }

    public class TypedHolder
    {
        public ObjectId Id { get; set; }
        public TypedLeaves Leaves { get; set; } = null!;
    }

    [Fact]
    public void Converted_represented_enum_guid_datetime_and_decimal_leaves_round_trip()
    {
        var collection = database.CreateCollection<TypedHolder>(Unique(nameof(Converted_represented_enum_guid_datetime_and_decimal_leaves_round_trip)));

        static void Configure(ModelBuilder mb)
            => mb.Entity<TypedHolder>().ComplexProperty(h => h.Leaves, l =>
            {
                l.Property(h => h.Converted).HasConversion<string>();
                l.Property(h => h.Represented).Metadata.SetBsonRepresentation(BsonType.String, null, null);
                l.Property(h => h.Seen).Metadata.SetDateTimeKind(DateTimeKind.Local);
            });

        var tag = Guid.NewGuid();
        var seen = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Local);
        using (var db = Context(collection, MongoQueryMode.NativeOnly, Configure))
        {
            db.Entities.Add(new TypedHolder
            {
                Leaves = new TypedLeaves
                {
                    Converted = 42, Represented = 7, Tier = Tier.Silver, Tag = tag, Seen = seen, Price = 12.34m, Note = null
                }
            });
            db.SaveChanges();
        }

        var stored = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).Single()["Leaves"].AsBsonDocument;
        Assert.Equal("42", stored["Converted"].AsString);
        Assert.Equal("7", stored["Represented"].AsString);

        var expected = $"42|7|Silver|{tag}|{seen:O}|Local|12.34|-";
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.ToList().Select(h => FmtLeaves(h.Leaves)), Configure), [expected]);
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.Select(h => h.Leaves).ToList().Select(FmtLeaves), Configure), [expected]);

        static string FmtLeaves(TypedLeaves l)
            => $"{l.Converted}|{l.Represented}|{l.Tier}|{l.Tag}|{l.Seen:O}|{l.Seen.Kind}|{l.Price}|{l.Note ?? "-"}";
    }

    // ── Constructor binding ─────────────────────────────────────────────────────────────────────────────────────

    public record CtorAddress(string City, int Floor)
    {
        public string? Note { get; set; }
    }

    public class CtorHolder
    {
        public ObjectId Id { get; set; }
        public CtorAddress Address { get; set; } = null!;
    }

    [Fact]
    public void Constructor_bound_complex_type()
    {
        var collection = database.CreateCollection<CtorHolder>(Unique(nameof(Constructor_bound_complex_type)));
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Address", new BsonDocument { { "Note", "n" }, { "Floor", 4 }, { "City", "Oslo" } } }
        });

        static void Configure(ModelBuilder mb) => mb.Entity<CtorHolder>().ComplexProperty(h => h.Address);

        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.ToList().Select(h => $"{h.Address.City}|{h.Address.Floor}|{h.Address.Note}"), Configure),
            ["Oslo|4|n"]);
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.Select(h => h.Address).ToList().Select(a => $"{a.City}|{a.Floor}|{a.Note}"), Configure),
            ["Oslo|4|n"]);
    }

    // ── Owned navigation, TPH, Include ──────────────────────────────────────────────────────────────────────────

    public class OwnedHome
    {
        public string City { get; set; } = null!;
        public Spot Spot { get; set; } = null!;
    }

    [ComplexType]
    public class Spot
    {
        public int Row { get; set; }
        public string Label { get; set; } = null!;
    }

    public class OwnerWithBoth
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public OwnedHome Home { get; set; } = null!;
        public MAddress Address { get; set; } = null!;
    }

    [Fact]
    public void Entity_with_an_owned_navigation_and_complex_properties_including_complex_inside_owned()
    {
        var collection = database.CreateCollection<OwnerWithBoth>(Unique(nameof(Entity_with_an_owned_navigation_and_complex_properties_including_complex_inside_owned)));
        Raw(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Ann" },
                { "Home", new BsonDocument { { "City", "Paris" }, { "Spot", new BsonDocument { { "Label", "A1" }, { "Row", 1 } } } } },
                { "Address", GoodAddress() }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Bob" },
                { "Address", GoodAddress() },
                { "Home", new BsonDocument { { "Spot", new BsonDocument { { "Row", 2 }, { "Label", "B2" } } }, { "City", "Rome" } } }
            }
        ]);

        static void Configure(ModelBuilder mb)
        {
            mb.Entity<OwnerWithBoth>().OwnsOne(o => o.Home);
            mb.Entity<OwnerWithBoth>().ComplexProperty(o => o.Address, a => a.ComplexProperty(x => x.Location));
        }

        using (var db = Context(collection, MongoQueryMode.NativeOnly, Configure))
        {
            Assert.True(StreamingEligibility.IsEligible(db.Model.FindEntityType(typeof(OwnerWithBoth))!));
        }

        foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking })
        {
            NativeModeAssert.NativeAndExpected(
                m => Run(collection, m, q => q.OrderBy(o => o.Name).ToList()
                    .Select(o => $"{o.Name}|{o.Home.City}|{o.Home.Spot.Label}:{o.Home.Spot.Row}|{Fmt(o.Address)}"), Configure, tracking),
                ["Ann|Paris|A1:1|s|c|1,2|1|-|Bronze", "Bob|Rome|B2:2|s|c|1,2|1|-|Bronze"]);
        }

        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.OrderBy(o => o.Name).Select(o => new { o.Name, o.Home }).ToList()
                .Select(x => $"{x.Name}|{x.Home.Spot.Label}"), Configure),
            ["Ann|A1", "Bob|B2"]);
    }

    [Fact]
    public void Whole_complex_value_inside_an_owned_reference()
    {
        // owned -> complex: `o.Home.Spot` is a dotted whole value (NativeProjectionBinder gate 1g, under `_v`).
        var collection = database.CreateCollection<OwnerWithBoth>(Unique(nameof(Whole_complex_value_inside_an_owned_reference)));
        Raw(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Ann" },
                { "Home", new BsonDocument { { "City", "Paris" }, { "Spot", new BsonDocument { { "Label", "A1" }, { "Row", 1 } } } } },
                { "Address", GoodAddress() }
            }
        ]);

        static void Configure(ModelBuilder mb)
        {
            mb.Entity<OwnerWithBoth>().OwnsOne(o => o.Home);
            mb.Entity<OwnerWithBoth>().ComplexProperty(o => o.Address, a => a.ComplexProperty(x => x.Location));
        }

        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.Select(o => o.Home.Spot).ToList().Select(x => $"{x.Label}:{x.Row}"), Configure), ["A1:1"]);
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.Select(o => new { o.Name, o.Home.Spot }).ToList().Select(x => $"{x.Name}|{x.Spot.Label}"), Configure),
            ["Ann|A1"]);
    }

    public class Order
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<OrderLine> Lines { get; set; } = [];
    }

    public class OrderLine
    {
        public string Sku { get; set; } = null!;
        public Spot Origin { get; set; } = null!;
        public Spot Bin { get; set; } = null!;
    }

    [Fact]
    public void Owned_collection_elements_with_complex_properties()
    {
        // Each element's complex values are read afresh: the one-pass materializer resets its per-element locals, so a
        // missing element in one line can't inherit the previous line's value. ([ComplexType] classes: an owned type has
        // no ComplexProperty builder and the attribute is class-only.)
        var collection = database.CreateCollection<Order>(Unique(nameof(Owned_collection_elements_with_complex_properties)));
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "o" },
            {
                "Lines", new BsonArray
                {
                    new BsonDocument { { "Sku", "a" }, { "Origin", Spot(1, "o1") }, { "Bin", Spot(1, "x") } },
                    new BsonDocument { { "Bin", new BsonDocument { { "Label", "y" }, { "Row", 2 } } }, { "Origin", Spot(2, "o2") }, { "Sku", "b" } }
                }
            }
        });

        static void Configure(ModelBuilder mb) => mb.Entity<Order>().OwnsMany(o => o.Lines);

        static BsonDocument Spot(int row, string label) => new() { { "Row", row }, { "Label", label } };

        using (var db = Context(collection, MongoQueryMode.NativeOnly, Configure))
        {
            Assert.True(StreamingEligibility.IsEligible(db.Model.FindEntityType(typeof(Order))!));
        }

        foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking })
        {
            NativeModeAssert.NativeAndExpected(
                m => Run(collection, m, q => q.ToList().SelectMany(o => o.Lines).Select(l => $"{l.Sku}|{l.Origin.Label}|{l.Bin.Label}:{l.Bin.Row}"),
                    Configure, tracking),
                ["a|o1|x:1", "b|o2|y:2"]);
        }

        // An element missing its required struct complex value throws, in the element as at the root.
        var broken = database.CreateCollection<Order>(Unique(nameof(Owned_collection_elements_with_complex_properties) + "b"));
        Raw(broken).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "o" },
            {
                "Lines", new BsonArray
                {
                    new BsonDocument { { "Sku", "a" }, { "Origin", Spot(1, "o1") }, { "Bin", Spot(1, "x") } },
                    new BsonDocument { { "Sku", "b" }, { "Bin", Spot(2, "y") } }
                }
            }
        });
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Run(broken, mode, q => q.ToList().Select(o => o.Name), Configure));
            Assert.Contains("Document element 'Origin' is missing for required complex property", ex.Message);
        }
    }

    public class Person
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public MAddress Address { get; set; } = null!;
    }

    public class Employee : Person
    {
        public GeoPoint Desk { get; set; }
        public int Level { get; set; }
    }

    [Fact]
    public void TPH_with_complex_properties_on_base_and_derived_types()
    {
        var collection = database.CreateCollection<Person>(Unique(nameof(TPH_with_complex_properties_on_base_and_derived_types)));
        Raw(collection).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "_t", "Person" }, { "Name", "Ann" }, { "Address", GoodAddress() } },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "_t", "Employee" }, { "Name", "Bob" }, { "Address", GoodAddress() },
                { "Desk", Geo(9, 8) }, { "Level", 4 }
            }
        ]);

        static void Configure(ModelBuilder mb)
        {
            mb.Entity<Person>().HasDiscriminator<string>("_t").HasValue<Person>("Person").HasValue<Employee>("Employee");
            mb.Entity<Person>().ComplexProperty(p => p.Address, a => a.ComplexProperty(x => x.Location));
            mb.Entity<Employee>().ComplexProperty(e => e.Desk);
        }

        static string FmtPerson(Person p) => p is Employee e ? $"E:{p.Name}|{p.Address.City}|{Fmt(e.Desk)}" : $"P:{p.Name}|{p.Address.City}";

        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.OrderBy(p => p.Name).ToList().Select(FmtPerson), Configure, QueryTrackingBehavior.TrackAll),
            ["P:Ann|c", "E:Bob|c|9,8"]);
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.OfType<Employee>().ToList().Select(FmtPerson), Configure),
            ["E:Bob|c|9,8"]);
        // Projecting a member declared only on the derived type after OfType declines natively, a scalar (`e.Level`) as
        // much as a complex value (`e.Desk`): the selector is translated against the collection's root type
        // (pre-existing; follow-up). The fallback answers correctly.
        Assert.Equal([4], NativeModeAssert.DeclinesCleanly(m => Run(collection, m, q => q.OfType<Employee>().Select(e => e.Level), Configure)));
        Assert.Equal(["9,8"], NativeModeAssert.DeclinesCleanly(
            m => Run(collection, m, q => q.OfType<Employee>().Select(e => e.Desk).ToList().Select(Fmt), Configure)));
        // A complex value declared on the base type projects natively over the hierarchy.
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.OrderBy(p => p.Name).Select(p => p.Address.City), Configure), ["c", "c"]);
    }

    public class Shop
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public MAddress Address { get; set; } = null!;
        public List<Sale> Sales { get; set; } = [];
    }

    public class Sale
    {
        public ObjectId Id { get; set; }
        public ObjectId ShopId { get; set; }
        public Shop? Shop { get; set; }
        public GeoPoint Where { get; set; }
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
                b.ComplexProperty(s => s.Address, a => a.ComplexProperty(x => x.Location));
                b.HasMany(s => s.Sales).WithOne(s => s.Shop).HasForeignKey(s => s.ShopId);
            });
            mb.Entity<Sale>(b =>
            {
                b.ToCollection(sales);
                b.ComplexProperty(s => s.Where);
            });
        }
    }

    [Fact]
    public void Complex_properties_alongside_reference_and_collection_Include()
    {
        var shops = Unique(nameof(Complex_properties_alongside_reference_and_collection_Include)) + "_shops";
        var sales = Unique(nameof(Complex_properties_alongside_reference_and_collection_Include)) + "_sales";
        var shopId = ObjectId.GenerateNewId();
        database.MongoDatabase.GetCollection<BsonDocument>(shops).InsertOne(
            new BsonDocument { { "_id", shopId }, { "Name", "S1" }, { "Address", GoodAddress() } });
        database.MongoDatabase.GetCollection<BsonDocument>(sales).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "ShopId", shopId }, { "Where", Geo(1, 1) } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "ShopId", shopId }, { "Where", Geo(2, 2) } }
        ]);

        ShopContext Create(MongoQueryMode mode)
        {
            var builder = new DbContextOptionsBuilder<ShopContext>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
            return new ShopContext(builder.Options, shops, sales);
        }

        NativeModeAssert.NativeAndExpected(m =>
        {
            using var db = Create(m);
            return db.Sales.Include(s => s.Shop).OrderBy(s => s.Where.Lat).ToList()
                .Select(s => $"{Fmt(s.Where)}|{s.Shop!.Name}|{s.Shop.Address.City}|{Fmt(s.Shop.Address.Location)}").ToList();
        }, ["1,1|S1|c|1,2", "2,2|S1|c|1,2"]);

        NativeModeAssert.NativeAndExpected(m =>
        {
            using var db = Create(m);
            return db.Shops.Include(s => s.Sales).ToList()
                .Select(s => $"{s.Name}|{s.Address.City}|{string.Join(",", s.Sales.Select(x => x.Where.Lat).Order())}").ToList();
        }, ["S1|c|1,2"]);
    }

    // ── Tracking: load by query, then mutate (closes the Task 5/6 deferred items) ─────────────────────────────

    [Fact]
    public void Query_loaded_entity_leaf_change_is_detected_and_saved_as_a_whole_subdocument()
    {
        var name = nameof(Query_loaded_entity_leaf_change_is_detected_and_saved_as_a_whole_subdocument);
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.DriverLinq })
        {
            using var capture = new CommandCapture(database);
            var collection = capture.Collection(SeedCustomers(name + mode));
            using var db = Context(collection, mode, ConfigureCustomer);

            var bob = db.Entities.Single(c => c.Name == "Bob");
            Assert.Equal(EntityState.Unchanged, db.Entry(bob).State);

            // Nothing changed: DetectChanges finds nothing and SaveChanges sends no command (the snapshot holds the leaves).
            db.ChangeTracker.DetectChanges();
            Assert.Equal(EntityState.Unchanged, db.Entry(bob).State);
            capture.Clear();
            Assert.Equal(0, db.SaveChanges());
            Assert.Empty(capture.Named("update"));

            bob.Address.City = "Leeds";
            var location = bob.Address.Location;
            location.Lat = 9.5;
            bob.Address.Location = location;
            db.ChangeTracker.DetectChanges();

            var address = db.Entry(bob).ComplexProperty(c => c.Address);
            Assert.True(address.Property(a => a.City).IsModified);
            Assert.True(address.ComplexProperty(a => a.Location).Property(l => l.Lat).IsModified);
            Assert.False(address.Property(a => a.Street).IsModified);
            Assert.False(db.Entry(bob).Property(c => c.Name).IsModified);

            Assert.Equal(1, db.SaveChanges());
            var set = capture.SingleSet();
            Assert.Equal(new[] { "_id", "Address" }, set.Names.ToArray());

            var stored = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "Bob")).Single();
            // The whole subdocument is rewritten by the writer (unmapped elements of the stored subdocument are dropped).
            Assert.Equal(
                new BsonDocument
                {
                    { "City", "Leeds" }, { "Floor", 5 }, { "Street", "High" }, { "Tier", 0 }, { "Zip", BsonNull.Value },
                    { "Location", new BsonDocument { { "Lat", 9.5 }, { "Lon", 20.0 } } }
                },
                stored["Address"].AsBsonDocument);
        }
    }

    [Fact]
    public void Query_loaded_entity_with_complex_values_can_be_deleted()
    {
        var collection = SeedCustomers(nameof(Query_loaded_entity_with_complex_values_can_be_deleted));
        using (var db = Context(collection, MongoQueryMode.NativeOnly, ConfigureCustomer))
        {
            db.Entities.Remove(db.Entities.Single(c => c.Name == "Ann"));
            Assert.Equal(1, db.SaveChanges());
        }

        Assert.Equal(["Bob", "Cid"], Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d["Name"].AsString).Order());
    }

#if !EF8 && !EF9
    // ── EF10: optional complex properties and complex collections ───────────────────────────────────────────────

    public class Bits
    {
        public string? Note { get; set; }
        public int? Count { get; set; }
    }

    public class OptionalHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Bits? Bits { get; set; }
        public MAddress? Address { get; set; }
    }

    private static void ConfigureOptional(ModelBuilder mb)
    {
        mb.Entity<OptionalHolder>().ComplexProperty(h => h.Bits);
        mb.Entity<OptionalHolder>().ComplexProperty(h => h.Address, a => a.ComplexProperty(x => x.Location));
    }

    [Fact]
    public void Optional_complex_property_null_missing_and_empty_document()
    {
        var collection = database.CreateCollection<OptionalHolder>(Unique(nameof(Optional_complex_property_null_missing_and_empty_document)));
        Raw(collection).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a-missing" } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "b-null" }, { "Bits", BsonNull.Value }, { "Address", BsonNull.Value } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "c-empty" }, { "Bits", new BsonDocument() } },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "d-value" }, { "Bits", new BsonDocument { { "Note", "n" }, { "Count", 3 } } },
                { "Address", GoodAddress() }
            }
        ]);

        static string FmtHolder(OptionalHolder h)
            => $"{h.Name}|{(h.Bits == null ? "<null>" : $"{h.Bits.Note ?? "-"}:{h.Bits.Count?.ToString() ?? "-"}")}|{Fmt(h.Address)}";

        string[] expected = ["a-missing|<null>|<null>", "b-null|<null>|<null>", "c-empty|-:-|<null>", "d-value|n:3|s|c|1,2|1|-|Bronze"];
        foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking })
        {
            NativeModeAssert.NativeAndExpected(
                m => Run(collection, m, q => q.OrderBy(h => h.Name).ToList().Select(FmtHolder), ConfigureOptional, tracking), [.. expected]);
        }

        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.OrderBy(h => h.Name).Select(h => h.Bits).ToList()
                .Select(b => b == null ? "<null>" : $"{b.Note ?? "-"}:{b.Count?.ToString() ?? "-"}"), ConfigureOptional),
            ["<null>", "<null>", "-:-", "n:3"]);
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.OrderBy(h => h.Name).Select(h => new { h.Name, h.Address }).ToList()
                .Select(x => x.Name + "|" + Fmt(x.Address)), ConfigureOptional),
            ["a-missing|<null>", "b-null|<null>", "c-empty|<null>", "d-value|s|c|1,2|1|-|Bronze"]);
    }

    public static TheoryData<string> OptionalAndCollectionOperatorShapes =>
        ["optional_distinct", "optional_union", "optional_concat", "collection_concat", "collection_distinct", "collection_union"];

    [Theory]
    [MemberData(nameof(OptionalAndCollectionOperatorShapes))]
    public void Operators_over_an_optional_or_collection_complex_value_are_refused_in_every_mode(string shape)
    {
        // Every row holds a NON-null value, so reading null (optional) or empty (required collection) would be silently
        // wrong rows: the reader's missing-element rule must never answer for a pushed-down operand (review Critical).
        var optional = database.CreateCollection<OptionalHolder>(Unique(nameof(Operators_over_an_optional_or_collection_complex_value_are_refused_in_every_mode) + shape));
        Raw(optional).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Bits", new BsonDocument { { "Note", "n1" }, { "Count", 1 } } }, { "Address", GoodAddress() } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "b" }, { "Bits", new BsonDocument { { "Note", "n2" }, { "Count", 2 } } }, { "Address", GoodAddress() } }
        ]);
        var carts = database.CreateCollection<Cart>(Unique(nameof(Operators_over_an_optional_or_collection_complex_value_are_refused_in_every_mode) + shape + "c"));
        Raw(carts).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Lines", new BsonArray { new BsonDocument { { "Sku", "s1" }, { "Qty", 1 } } } }, { "Watch", new BsonArray() } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "b" }, { "Lines", new BsonArray { new BsonDocument { { "Sku", "s2" }, { "Qty", 2 } } } }, { "Watch", new BsonArray() } }
        ]);

        static string FmtBits(Bits? b) => b == null ? "<null>" : b.Note ?? "-";

        Func<MongoQueryMode, List<string>> run = shape switch
        {
            "optional_distinct" => m => Run(optional, m, q => q.Select(h => h.Bits).Distinct().ToList().Select(FmtBits), ConfigureOptional),
            "optional_union" => m => Run(optional, m, q => q.Select(h => h.Address).Union(q.Select(h => h.Address)).ToList().Select(Fmt), ConfigureOptional),
            "optional_concat" => m => Run(optional, m, q => q.Select(h => h.Bits).Concat(q.Select(h => h.Bits)).ToList().Select(FmtBits), ConfigureOptional),
            "collection_concat" => m => Run(carts, m, q => q.Select(c => c.Lines).Concat(q.Select(c => c.Lines)).ToList().Select(FmtLines), ConfigureCart),
            "collection_distinct" => m => Run(carts, m, q => q.Select(c => c.Lines).Distinct().ToList().Select(FmtLines), ConfigureCart),
            "collection_union" => m => Run(carts, m, q => q.Select(c => new { c.Name, c.Lines }).Union(q.Select(c => new { c.Name, c.Lines })).ToList().Select(x => FmtLines(x.Lines)), ConfigureCart),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        var op = shape.EndsWith("distinct") ? "Distinct" : shape.EndsWith("union") ? "Union" : "Concat";

        AssertRefusedInEveryMode(run, op);
    }

    public record OptionalPair(string Name, Bits? Bits);

    public class CartDto
    {
        public string Name { get; set; } = null!;
        public List<Line> Lines { get; set; } = null!;
    }

    public static TheoryData<string, string> OptionalAndCollectionConstructionShapes()
    {
        var data = new TheoryData<string, string>();
        foreach (var construction in new[] { "optional_record", "optional_anon", "collection_memberinit", "collection_anon" })
        foreach (var op in new[] { "Distinct", "Union", "Concat", "Intersect", "Except" })
        {
            data.Add(construction, op);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(OptionalAndCollectionConstructionShapes))]
    public void Operators_over_a_construction_holding_an_optional_or_collection_complex_value_are_refused_in_every_mode(string construction, string op)
    {
        var name = nameof(Operators_over_a_construction_holding_an_optional_or_collection_complex_value_are_refused_in_every_mode) + construction + op;
        var optional = database.CreateCollection<OptionalHolder>(Unique(name));
        Raw(optional).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Bits", new BsonDocument { { "Note", "n1" }, { "Count", 1 } } } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "b" }, { "Bits", new BsonDocument { { "Note", "n2" }, { "Count", 2 } } } }
        ]);
        var carts = database.CreateCollection<Cart>(Unique(name + "c"));
        Raw(carts).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Lines", new BsonArray { new BsonDocument { { "Sku", "s1" }, { "Qty", 1 } } } }, { "Watch", new BsonArray() } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "b" }, { "Lines", new BsonArray { new BsonDocument { { "Sku", "s2" }, { "Qty", 2 } } } }, { "Watch", new BsonArray() } }
        ]);

        Func<MongoQueryMode, List<string>> run = construction switch
        {
            "optional_record" => m => Run(optional, m, q => ApplyOperator(q.Select(h => new OptionalPair(h.Name, h.Bits)), q.Select(h => new OptionalPair(h.Name, h.Bits)), op), ConfigureOptional),
            "optional_anon" => m => Run(optional, m, q => ApplyOperator(q.Select(h => new { h.Name, h.Bits }), q.Select(h => new { h.Name, h.Bits }), op), ConfigureOptional),
            "collection_memberinit" => m => Run(carts, m, q => ApplyOperator(q.Select(c => new CartDto { Name = c.Name, Lines = c.Lines }),
                q.Select(c => new CartDto { Name = c.Name, Lines = c.Lines }), op), ConfigureCart),
            _ => m => Run(carts, m, q => ApplyOperator(q.Select(c => new { c.Name, c.Lines }), q.Select(c => new { c.Name, c.Lines }), op), ConfigureCart)
        };

        // optional_record: a positional ctor with a complex argument declines natively (IsScalarPositionalConstruction), so
        // the operator meets the same R7 refusal in every mode.
        AssertRefusedInEveryMode(run, op);
    }

    [Fact]
    public void Optional_complex_property_present_but_missing_required_leaves_throws()
    {
        // `{}` is a present value: it materializes an instance, and a required leaf it lacks follows the entity-member rule.
        var collection = database.CreateCollection<OptionalHolder>(Unique(nameof(Optional_complex_property_present_but_missing_required_leaves_throws)));
        Raw(collection).InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "x" }, { "Address", new BsonDocument() } });

        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Run(collection, mode, q => q.ToList().Select(h => h.Name), ConfigureOptional));
            Assert.Contains("Document element is missing for required non-nullable property", ex.Message);
        }
    }

    [Fact]
    public void Required_complex_value_under_a_missing_optional_parent_throws_when_projected_whole()
    {
        // Whole complex values are read strictly: `h.Address!.Location` is a required struct, and with Address missing
        // its element is missing. (A scalar leaf under the same parent reads default; see ComplexTypeNativeQueryTests.)
        var collection = database.CreateCollection<OptionalHolder>(Unique(nameof(Required_complex_value_under_a_missing_optional_parent_throws_when_projected_whole)));
        Raw(collection).InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "x" } });

        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Run(collection, mode, q => q.Select(h => h.Address!.Location).ToList().Select(Fmt), ConfigureOptional));
            Assert.Contains("Document element 'Location' is missing for required complex property", ex.Message);
        }
    }

    public class Line
    {
        public string Sku { get; set; } = null!;
        public int Qty { get; set; }
        public List<Note> Notes { get; set; } = [];
    }

    public class Note
    {
        public string Text { get; set; } = null!;
    }

    public class Cart
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Line> Lines { get; set; } = [];
        public IList<Note>? Tags { get; set; }
        public System.Collections.ObjectModel.ObservableCollection<Note> Watch { get; set; } = [];
    }

    private static void ConfigureCart(ModelBuilder mb)
    {
        mb.Entity<Cart>().ComplexCollection(c => c.Lines, l => l.ComplexCollection(x => x.Notes));
        mb.Entity<Cart>().ComplexCollection(c => c.Tags, t => t.IsRequired(false));
        mb.Entity<Cart>().ComplexCollection(c => c.Watch);
    }

    private static string FmtCart(Cart c)
        => $"{c.Name}|{FmtLines(c.Lines)}|tags:{(c.Tags == null ? "<null>" : string.Join(",", c.Tags.Select(t => t?.Text ?? "<null>")))}"
           + $"|watch:{c.Watch.GetType().Name}:{string.Join(",", c.Watch.Select(t => t.Text))}";

    private static string FmtLines(IEnumerable<Line?> lines)
        => "[" + string.Join(";", lines.Select(l => l == null ? "<null>" : $"{l.Sku}x{l.Qty}({string.Join(",", l.Notes.Select(n => n.Text))})")) + "]";

    [Fact]
    public void Complex_collections_empty_null_missing_null_element_and_nested()
    {
        var collection = database.CreateCollection<Cart>(Unique(nameof(Complex_collections_empty_null_missing_null_element_and_nested)));
        Raw(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "a" },
                {
                    "Lines", new BsonArray
                    {
                        new BsonDocument { { "Notes", new BsonArray { new BsonDocument("Text", "n1"), new BsonDocument("Text", "n2") } }, { "Qty", 2 }, { "Sku", "s1" } },
                        new BsonDocument { { "Sku", "s2" }, { "Qty", 1 }, { "Notes", new BsonArray() }, { "Extra", 1 } }
                    }
                },
                { "Tags", new BsonArray { new BsonDocument("Text", "t1"), BsonNull.Value } },
                { "Watch", new BsonArray { new BsonDocument("Text", "w") } }
            },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "b" }, { "Lines", new BsonArray() }, { "Tags", BsonNull.Value } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "c" }, { "Lines", BsonNull.Value } },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "d" },
                { "Lines", new BsonArray { new BsonDocument { { "Sku", "s3" }, { "Qty", 3 } } } }
            }
        ]);

        // A required collection that is missing or BSON null reads empty (as an owned collection does); an optional one
        // reads null; a null element reads null; a missing nested collection in an element reads empty.
        string[] expected =
        [
            "a|[s1x2(n1,n2);s2x1()]|tags:t1,<null>|watch:ObservableCollection`1:w",
            "b|[]|tags:<null>|watch:ObservableCollection`1:",
            "c|[]|tags:<null>|watch:ObservableCollection`1:",
            "d|[s3x3()]|tags:<null>|watch:ObservableCollection`1:"
        ];
        foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking })
        {
            NativeModeAssert.NativeAndExpected(
                m => Run(collection, m, q => q.OrderBy(c => c.Name).ToList().Select(FmtCart), ConfigureCart, tracking), [.. expected]);
        }

        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.OrderBy(c => c.Name).Select(c => c.Lines).ToList().Select(FmtLines), ConfigureCart),
            ["[s1x2(n1,n2);s2x1()]", "[]", "[]", "[s3x3()]"]);
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.OrderBy(c => c.Name).Select(c => new { c.Name, c.Tags }).ToList()
                .Select(x => x.Name + ":" + (x.Tags == null ? "<null>" : x.Tags.Count.ToString())), ConfigureCart),
            ["a:2", "b:<null>", "c:<null>", "d:<null>"]);
    }

    [Fact]
    public void Query_loaded_complex_collection_element_change_rewrites_the_whole_array()
    {
        var name = nameof(Query_loaded_complex_collection_element_change_rewrites_the_whole_array);
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<Cart>(Unique(name)));
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "a" },
            {
                "Lines", new BsonArray
                {
                    new BsonDocument { { "Sku", "s1" }, { "Qty", 1 }, { "Notes", new BsonArray() } },
                    new BsonDocument { { "Sku", "s2" }, { "Qty", 2 }, { "Notes", new BsonArray() } }
                }
            },
            { "Watch", new BsonArray() }
        });

        using var db = Context(collection, MongoQueryMode.NativeOnly, ConfigureCart);
        var cart = db.Entities.Single();
        cart.Lines[1].Qty = 20;
        db.ChangeTracker.DetectChanges();
        Assert.True(db.Entry(cart).ComplexCollection(c => c.Lines).IsModified);

        capture.Clear();
        Assert.Equal(1, db.SaveChanges());
        Assert.Contains("Lines", capture.SingleSet().Names);

        var stored = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).Single()["Lines"].AsBsonArray;
        Assert.Equal([1, 20], stored.Select(l => l["Qty"].AsInt32));
        Assert.Equal(["s1", "s2"], stored.Select(l => l["Sku"].AsString));
    }
#endif

#if EF8 || EF9
    // ── EF8/EF9: shadow leaves on complex types (EF10 rejects them at model building) ─────────────────────────

    public class ShadowDetails
    {
        public string Value { get; set; } = null!;
    }

    public class ShadowHolder
    {
        public ObjectId Id { get; set; }
        public ShadowDetails Details { get; set; } = null!;
    }

    [Fact]
    public void Shadow_leaf_on_a_complex_type()
    {
        // EF8/EF9 accept a shadow property on a complex type (EF10 rejects it at model building). Measured: EF itself can't
        // track such an entity from a query, in every mode including DriverLinq: EF9 throws ArgumentOutOfRangeException
        // while building the shadow-values snapshot (ShadowValuesFactoryFactory, at query compile), EF8
        // IndexOutOfRangeException in InternalEntityEntry's constructor (StateManager.StartTrackingFromQuery). Both come
        // from EF's shadow-value indexing, not the provider. A no-tracking query materializes the CLR leaves; the shadow
        // leaf has no CLR member and is not read.
        var collection = database.CreateCollection<ShadowHolder>(Unique(nameof(Shadow_leaf_on_a_complex_type)));
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Details", new BsonDocument { { "Hidden", "h" }, { "Value", "v" } } }
        });

        static void Configure(ModelBuilder mb)
            => mb.Entity<ShadowHolder>().ComplexProperty(e => e.Details, d => d.Property<string>("Hidden"));

        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.ToList().Select(h => h.Details.Value), Configure), ["v"]);
        NativeModeAssert.NativeAndExpected(
            m => Run(collection, m, q => q.Select(h => h.Details).ToList().Select(d => d.Value), Configure), ["v"]);

        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTrackingWithIdentityResolution })
            {
                var ex = Assert.ThrowsAny<SystemException>(
                    () => Run(collection, mode, q => q.ToList().Select(h => h.Details.Value), Configure, tracking));
                Assert.True(ex is IndexOutOfRangeException or ArgumentOutOfRangeException, ex.ToString());
            }
        }
    }
#endif

    private IMongoCollection<BsonDocument> Raw<T>(IMongoCollection<T> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    private static string Unique(string name)
        => TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
}
