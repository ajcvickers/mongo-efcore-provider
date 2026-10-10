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

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// A whole complex value compared with <c>null</c>, with a captured or constant instance, and with another stored complex
/// value: member-wise equality (a conjunction of leaf equalities, recursing into nested complex values) with C# null
/// semantics, on the native path. Each shape is pinned per mode against a hand-written answer.
/// </summary>
/// <remarks>
/// <para>
/// Null semantics: a stored element that is BSON null and one that is MISSING both read as null, so they answer alike
/// (<c>c.Opt == null</c> matches both; a nullable leaf equals a null member either way); <c>{}</c> is a present value.
/// </para>
/// <para>
/// Driver-LINQ cannot compare a whole complex value (the complex serializer refuses it, ruling R1; two stored values are
/// "serialized differently"), so wherever the native path now serves an instance comparison, <c>DriverLinq</c> is pinned
/// to that refusal. Documents are seeded as raw BSON so null, missing, reordered and extra elements can be expressed.
/// </para>
/// </remarks>
[XUnitCollection("QueryTests")]
public class ComplexTypeNullAndEqualityTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    // The complex serializer's whole-value refusal (ruling R1) and the driver's refusal of two stored complex values.
    private const string WholeValueRefused = "as a whole value is not supported";
    private const string SerializedDifferently = "serialized differently";

    public enum Grade
    {
        Low,
        High
    }

    public class EqAddress
    {
        public string City { get; set; } = null!;
        public int? Zip { get; set; }
        public GeoPoint Geo { get; set; }
        public Grade Grade { get; set; }
        public string? Scratch { get; set; }
    }

    public class EqCustomer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public EqAddress Home { get; set; } = null!;
        public EqAddress Work { get; set; } = null!;
        public GeoPoint Pin { get; set; }
    }

    // Home under CLR names; Work under `work` with City as `town` (same CLR type, different element names, stored alike).
    // Scratch is ignored: member-wise equality never reads it.
    private static void ConfigureEq(ModelBuilder mb)
    {
        mb.Entity<EqCustomer>().ComplexProperty(c => c.Home, a =>
        {
            a.ComplexProperty(x => x.Geo);
            a.Ignore(x => x.Scratch);
        });
        mb.Entity<EqCustomer>().ComplexProperty(c => c.Work, a =>
        {
            a.HasPropertyAnnotation(MongoDB.EntityFrameworkCore.Metadata.MongoAnnotationNames.ElementName, "work");
            a.Property(x => x.City).Metadata.SetElementName("town");
            a.ComplexProperty(x => x.Geo);
            a.Ignore(x => x.Scratch);
        });
        mb.Entity<EqCustomer>().ComplexProperty(c => c.Pin);
    }

    private static BsonDocument Geo(double lat, double lon) => new() { { "Lat", lat }, { "Lon", lon } };

    /// <summary>
    /// ann: Home (Paris, 7, 1/2, High) = Work; bob: Home (London, Zip MISSING, 3/4, Low) stored reordered with an unmapped
    /// element, Work the same but Zip BSON null; cid: Home "paris" (lower case), Work Rome; dee: Home (Paris, 7, 1/9, High),
    /// Work Zip 8. Pins: ann 1/2, bob 3/4, cid 1/2, dee 1/9.
    /// </summary>
    private IMongoCollection<EqCustomer> SeedEq(string name)
    {
        var collection = database.CreateCollection<EqCustomer>(Unique(name));
        Raw(collection).InsertMany(
        [
            Customer("ann", Home("Paris", 7, Geo(1, 2), Grade.High), Work("Paris", 7, Geo(1, 2), Grade.High), Geo(1, 2)),
            Customer("bob",
                new BsonDocument { { "Grade", (int)Grade.Low }, { "Geo", Geo(3, 4) }, { "Unmapped", "x" }, { "City", "London" } },
                new BsonDocument { { "town", "London" }, { "Zip", BsonNull.Value }, { "Geo", Geo(3, 4) }, { "Grade", (int)Grade.Low } },
                Geo(3, 4)),
            Customer("cid", Home("paris", 7, Geo(1, 2), Grade.High), Work("Rome", 7, Geo(1, 2), Grade.High), Geo(1, 2)),
            Customer("dee", Home("Paris", 7, Geo(1, 9), Grade.High), Work("Paris", 8, Geo(1, 9), Grade.High), Geo(1, 9))
        ]);
        return collection;

        static BsonDocument Home(string city, int zip, BsonDocument geo, Grade grade)
            => new() { { "City", city }, { "Zip", zip }, { "Geo", geo }, { "Grade", (int)grade } };

        static BsonDocument Work(string city, int zip, BsonDocument geo, Grade grade)
            => new() { { "town", city }, { "Zip", zip }, { "Geo", geo }, { "Grade", (int)grade } };

        static BsonDocument Customer(string name, BsonDocument home, BsonDocument work, BsonDocument pin)
            => new() { { "_id", ObjectId.GenerateNewId() }, { "Name", name }, { "Home", home }, { "work", work }, { "Pin", pin } };
    }

    private static readonly EqAddress AnnAddress
        = new() { City = "Paris", Zip = 7, Geo = new GeoPoint { Lat = 1, Lon = 2 }, Grade = Grade.High, Scratch = "never compared" };

    private static readonly EqAddress BobAddress = new() { City = "London", Zip = null, Geo = new GeoPoint { Lat = 3, Lon = 4 }, Grade = Grade.Low };

    private Func<MongoQueryMode, List<string>> Eq(
        Func<IQueryable<EqCustomer>, IEnumerable<string>> query, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = SeedEq(name + Guid.NewGuid().ToString("N")[..4]);
        return mode => Run(collection, mode, ConfigureEq, query);
    }

    private static IEnumerable<string> Names(IQueryable<EqCustomer> q) => q.Select(c => c.Name).ToList().Order(StringComparer.Ordinal);

    // ── Member-wise equality of REQUIRED complex values (EF8, EF9, EF10) ───────────────────────────────────────────

    [Fact]
    public void Equal_to_a_captured_instance()
    {
        var other = AnnAddress;
        PerMode(Eq(q => Names(q.Where(c => c.Home == other))), ["ann"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Not_equal_and_negated_equal_are_exact_complements()
    {
        var other = AnnAddress;
        PerMode(Eq(q => Names(q.Where(c => c.Home != other))), ["bob", "cid", "dee"], Serves, Serves, WholeValueRefused);
        PerMode(Eq(q => Names(q.Where(c => !(c.Home == other)))), ["bob", "cid", "dee"], Serves, Serves, WholeValueRefused);
        PerMode(Eq(q => Names(q.Where(c => !c.Home.Equals(other)))), ["bob", "cid", "dee"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Instance_inlined_as_a_constant()
    {
        // EF.Constant bakes the instance into the tree: its members are read at translation time, not per execution.
        var other = AnnAddress;
        var bob = BobAddress;
        PerMode(Eq(q => Names(q.Where(c => c.Home == EF.Constant(other)))), ["ann"], Serves, Serves, WholeValueRefused);
        PerMode(Eq(q => Names(q.Where(c => c.Home != EF.Constant(bob)))), ["ann", "cid", "dee"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Equality_projected_as_a_value()
    {
        // Driver-LINQ evaluates a projected `c.Home == other` client-side over a materialized copy, by C# reference
        // equality (EqAddress doesn't override Equals): always False, wrong rows. Native compares member-wise on the
        // server. Pinned as known wrong so the test breaks if the driver path changes.
        var other = AnnAddress;
        var run = Eq(q => q.OrderBy(c => c.Name).Select(c => new { c.Name, Same = c.Home == other }).ToList().Select(x => x.Name + ":" + x.Same));
        Assert.Equal(["ann:True", "bob:False", "cid:False", "dee:False"], run(MongoQueryMode.NativeOnly));
        Assert.Equal(["ann:True", "bob:False", "cid:False", "dee:False"], run(MongoQueryMode.Native));
        Assert.Equal(["ann:False", "bob:False", "cid:False", "dee:False"], run(MongoQueryMode.DriverLinq));
        // Driver-LINQ materializes both values client-side and reads Work's City under its CLR name: a loud throw.
        PerMode(Eq(q => q.OrderBy(c => c.Name).Select(c => c.Home == c.Work).ToList().Select(x => x.ToString())),
            ["True", "True", "False", "False"], Serves, Serves, "Document element is missing for required non-nullable property 'City'");
    }

    [Fact]
    public void Equals_method_spelling()
    {
        var other = AnnAddress;
        PerMode(Eq(q => Names(q.Where(c => c.Home.Equals(other)))), ["ann"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Inline_constructed_instance_with_a_null_member_matches_a_missing_leaf_regardless_of_element_order_and_extra_elements()
        // bob's Home has no Zip element, its elements are reordered, and it carries an unmapped element.
        => PerMode(Eq(q => Names(q.Where(c => c.Home
                == new EqAddress { City = "London", Zip = null, Geo = new GeoPoint { Lat = 3, Lon = 4 }, Grade = Grade.Low }))),
            ["bob"], Serves, Serves, WholeValueRefused);

    [Fact]
    public void String_leaves_compare_ordinally()
    {
        // cid's Home City is "paris": not equal to "Paris".
        var other = AnnAddress;
        PerMode(Eq(q => Names(q.Where(c => c.Home == other))), ["ann"], Serves, Serves, WholeValueRefused);
        var lower = new EqAddress { City = "paris", Zip = 7, Geo = new GeoPoint { Lat = 1, Lon = 2 }, Grade = Grade.High };
        PerMode(Eq(q => Names(q.Where(c => c.Home == lower))), ["cid"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Two_stored_values_of_the_same_type_under_different_element_names()
        // bob: Zip MISSING on Home, BSON null on Work: equal, as C#'s null == null.
        => PerMode(Eq(q => Names(q.Where(c => c.Home == c.Work))), ["ann", "bob"], Serves, Serves, SerializedDifferently);

    [Fact]
    public void Two_stored_values_not_equal()
        => PerMode(Eq(q => Names(q.Where(c => c.Home != c.Work))), ["cid", "dee"], Serves, Serves, SerializedDifferently);

    [Fact]
    public void Nested_struct_and_root_struct_complex_values()
    {
        var geo = new GeoPoint { Lat = 1, Lon = 2 };
        var pin = new GeoPoint { Lat = 3, Lon = 4 };
        PerMode(Eq(q => Names(q.Where(c => c.Home.Geo.Equals(geo)))), ["ann", "cid"], Serves, Serves, WholeValueRefused);
        PerMode(Eq(q => Names(q.Where(c => c.Pin.Equals(pin)))), ["bob"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Required_complex_value_compared_with_null()
    {
        // Every row holds Home; `!= null` is the complement.
        PerMode(Eq(q => Names(q.Where(c => c.Home == null))), [], Serves, Serves, Serves);
        PerMode(Eq(q => Names(q.Where(c => c.Home != null))), ["ann", "bob", "cid", "dee"], Serves, Serves, Serves);
    }

    [Fact]
    public void Cardinality_operators_over_a_complex_equality()
    {
        var other = AnnAddress;
        PerMode(Eq(q => [q.Count(c => c.Home == other).ToString()]), ["1"], Serves, Serves, WholeValueRefused);
        PerMode(Eq(q => [q.Any(c => c.Home == BobAddressCopy()).ToString()]), ["True"], Serves, Serves, WholeValueRefused);
        PerMode(Eq(q => [q.Where(c => c.Home != other).OrderBy(c => c.Name).Select(c => c.Name).First()]), ["bob"], Serves, Serves,
            WholeValueRefused);
    }

    private static EqAddress BobAddressCopy()
        => new() { City = BobAddress.City, Zip = BobAddress.Zip, Geo = BobAddress.Geo, Grade = BobAddress.Grade };

    [Fact]
    public void Captured_instance_is_read_per_execution()
    {
        // One lambda, one context per mode: the second run hits the compiled-query cache, so a member value baked into the
        // plan would answer the first run's rows again. A null captured value takes the per-execution null branch.
        var collection = SeedEq(nameof(Captured_instance_is_read_per_execution));
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            using var db = Context(collection, mode, ConfigureEq);
            EqAddress? other = null;
            List<string> RunWith(object? value)
            {
                other = (EqAddress?)value;
                return [.. Names(db.Entities.AsNoTracking().Where(c => c.Home == other))];
            }

            NativeModeAssert.TwiceWithDifferentValues(RunWith, AnnAddress, ["ann"], BobAddress, ["bob"]);
            NativeModeAssert.TwiceWithDifferentValues(RunWith, null, [], AnnAddress, ["ann"]);
        }

        using var driverDb = Context(collection, MongoQueryMode.DriverLinq, ConfigureEq);
        var captured = AnnAddress;
        var ex = Assert.Throws<NotSupportedException>(() => Names(driverDb.Entities.AsNoTracking().Where(c => c.Home == captured)).ToList());
        Assert.Contains(WholeValueRefused, ex.Message);
    }

    // ── Leaf kinds: converters, BsonRepresentation, Guid, DateTime, decimal, enum ──────────────────────────────────

    public class TypedBox
    {
        public Guid Tag { get; set; }
        public DateTime Seen { get; set; }
        public decimal Price { get; set; }
        public int Code { get; set; }
        public int Represented { get; set; }
        public Grade Grade { get; set; }
    }

    public class TypedHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public TypedBox A { get; set; } = null!;
        public TypedBox B { get; set; } = null!;
        public TypedBox C { get; set; } = null!;
    }

    // A and B store Code as a string and Represented as a string; C stores both as ints, so A and C are not stored alike.
    private static void ConfigureTyped(ModelBuilder mb)
    {
        mb.Entity<TypedHolder>().ComplexProperty(h => h.A, a =>
        {
            a.Property(x => x.Code).HasConversion<string>();
            a.Property(x => x.Represented).Metadata.SetBsonRepresentation(BsonType.String, null, null);
        });
        mb.Entity<TypedHolder>().ComplexProperty(h => h.B, a =>
        {
            a.Property(x => x.Code).HasConversion<string>();
            a.Property(x => x.Represented).Metadata.SetBsonRepresentation(BsonType.String, null, null);
        });
        mb.Entity<TypedHolder>().ComplexProperty(h => h.C);
    }

    private static readonly Guid TagX = Guid.Parse("6f1c2a52-6d0e-4c87-9a43-0d4a1e9b7c11");
    private static readonly Guid TagY = Guid.Parse("0b9e7f0c-1f6a-4a2b-8c3d-5e6f7a8b9c0d");

    private static TypedBox BoxX()
        => new() { Tag = TagX, Seen = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc), Price = 1.5m, Code = 5, Represented = 50, Grade = Grade.High };

    private static TypedBox BoxY(int code)
        => new() { Tag = TagY, Seen = new DateTime(2025, 6, 7, 8, 9, 10, DateTimeKind.Utc), Price = 2.5m, Code = code, Represented = 60, Grade = Grade.Low };

    private Func<MongoQueryMode, List<string>> Typed(
        Func<IQueryable<TypedHolder>, IEnumerable<string>> query, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<TypedHolder>(Unique(name));
        using (var db = Context(collection, MongoQueryMode.NativeOnly, ConfigureTyped))
        {
            // x: A = B = C. y: B's Code differs from A's.
            db.Entities.AddRange(
                new TypedHolder { Name = "x", A = BoxX(), B = BoxX(), C = BoxX() },
                new TypedHolder { Name = "y", A = BoxY(6), B = BoxY(7), C = BoxY(6) });
            db.SaveChanges();
        }

        var stored = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().Single(d => d["Name"] == "x");
        Assert.Equal("5", stored["A"]["Code"].AsString);
        Assert.Equal("50", stored["A"]["Represented"].AsString);
        Assert.Equal(5, stored["C"]["Code"].AsInt32);

        return mode => Run(collection, mode, ConfigureTyped, query);
    }

    [Fact]
    public void Converted_represented_guid_datetime_decimal_and_enum_leaves_compare_in_stored_form()
    {
        var x = BoxX();
        var y = BoxY(6);
        PerMode(Typed(q => q.Where(h => h.A == x).Select(h => h.Name)), ["x"], Serves, Serves, WholeValueRefused);
        PerMode(Typed(q => q.Where(h => h.A == y).Select(h => h.Name)), ["y"], Serves, Serves, WholeValueRefused);
        PerMode(Typed(q => q.Where(h => h.C == y).Select(h => h.Name)), ["y"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Two_stored_values_stored_alike_compare_member_wise()
        => PerMode(Typed(q => q.Where(h => h.A == h.B).Select(h => h.Name)), ["x"], Serves, Serves, SerializedDifferently);

    [Fact]
    public void Two_stored_values_not_stored_alike_decline()
        // A stores Code as a string, C as an int: compared in stored form "5" is never 5. The decline falls back to driver
        // LINQ, which refuses the comparison too.
        => PerMode(Typed(q => q.Where(h => h.A == h.C).Select(h => h.Name)), [], NotNative, SerializedDifferently, SerializedDifferently);

    // ── Leaves member-wise equality can't compare ──────────────────────────────────────────────────────────────

    public class LabelBox
    {
        public string Name { get; set; } = null!;
        public List<string> Labels { get; set; } = [];
    }

    public class LabelHolder
    {
        public ObjectId Id { get; set; }
        public LabelBox Box { get; set; } = null!;
    }

    [Fact]
    public void Primitive_collection_leaf_declines()
    {
        // `{ Box.Labels: [..] }` would also match an array CONTAINING the value, and C# compares lists by reference: no
        // member-wise answer, so the comparison declines and the fallback refuses it.
        var collection = database.CreateCollection<LabelHolder>(Unique(nameof(Primitive_collection_leaf_declines)));
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Box", new BsonDocument { { "Name", "n" }, { "Labels", new BsonArray { "a" } } } }
        });
        var other = new LabelBox { Name = "n", Labels = ["a"] };

        PerMode(mode => Run(collection, mode, mb => mb.Entity<LabelHolder>().ComplexProperty(h => h.Box),
                q => q.Where(h => h.Box == other).Select(h => h.Box.Name)),
            [], NotNative, WholeValueRefused, WholeValueRefused);
    }

#if EF8 || EF9
    public class ShadowAddress
    {
        public string City { get; set; } = null!;
    }

    public class ShadowHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public ShadowAddress Address { get; set; } = null!;
    }

    [Fact]
    public void Shadow_leaf_declines_against_an_instance_but_not_against_null()
    {
        // EF8/EF9 allow a shadow leaf on a complex type (EF10 rejects it). A captured instance has no value for it, so a
        // member-wise comparison can't be built: it declines and the fallback refuses (R1). Against null no leaf is read.
        var collection = database.CreateCollection<ShadowHolder>(Unique(nameof(Shadow_leaf_declines_against_an_instance_but_not_against_null)));
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "s" }, { "Address", new BsonDocument { { "City", "c" }, { "Hidden", "h" } } }
        });

        static void Configure(ModelBuilder mb)
            => mb.Entity<ShadowHolder>().ComplexProperty(h => h.Address, a => a.Property<string>("Hidden"));

        var other = new ShadowAddress { City = "c" };
        PerMode(mode => Run(collection, mode, Configure, q => q.Where(h => h.Address == other).Select(h => h.Name)),
            [], NotNative, WholeValueRefused, WholeValueRefused);
        PerMode(mode => Run(collection, mode, Configure, q => q.Where(h => h.Address != null).Select(h => h.Name)),
            ["s"], Serves, Serves, Serves);
    }
#endif

#if !EF8 && !EF9
    // ── EF10: optional complex properties ─────────────────────────────────────────────────────────────────────

    public class InnerBits
    {
        public string? Text { get; set; }
        public int? Num { get; set; }
    }

    public class OptAddress
    {
        public string City { get; set; } = null!;
        public int? Zip { get; set; }
        public GeoPoint Geo { get; set; }
        public InnerBits? Inner { get; set; }
        public InnerBits Req { get; set; } = null!;
    }

    public class OptCustomer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public OptAddress? Opt { get; set; }
        public OptAddress? Alt { get; set; }
        public OptAddress Main { get; set; } = null!;
        public GeoPoint? OptPin { get; set; }
    }

    private static void ConfigureOpt(ModelBuilder mb)
    {
        mb.Entity<OptCustomer>().ComplexProperty(c => c.Opt, a =>
        {
            a.ComplexProperty(x => x.Geo);
            a.ComplexProperty(x => x.Inner);
            a.ComplexProperty(x => x.Req);
        });
        mb.Entity<OptCustomer>().ComplexProperty(c => c.Alt, a =>
        {
            a.ComplexProperty(x => x.Geo);
            a.ComplexProperty(x => x.Inner);
            a.ComplexProperty(x => x.Req);
        });
        mb.Entity<OptCustomer>().ComplexProperty(c => c.Main, a =>
        {
            a.ComplexProperty(x => x.Geo);
            a.ComplexProperty(x => x.Inner);
            a.ComplexProperty(x => x.Req);
        });
        mb.Entity<OptCustomer>().ComplexProperty(c => c.OptPin);
    }

    private static readonly OptAddress ParisAddress = new()
    {
        City = "Paris", Zip = 7, Geo = new GeoPoint { Lat = 1, Lon = 2 }, Inner = new InnerBits { Text = "t", Num = 1 },
        Req = new InnerBits { Text = "r" }
    };

    /// <summary>
    /// Opt per row: a-missing (no element), b-null (BSON null), c-empty (<c>{}</c>: every member, incl. the required Req and
    /// Geo, missing), d-paris (Paris/7/1,2/Inner t,1/Req r), e-rome (Rome, Zip and Inner missing, Req {}), f-oslo (Zip and
    /// Inner BSON null, Req {}), g-lima (Inner <c>{}</c>, Req {}). Alt: d-paris holds
    /// Opt's value reordered with an unmapped element, b-null... missing everywhere else. Main (required): Inner {t} on
    /// a-missing, BSON null on b-null, missing elsewhere. OptPin: c-empty 9,9; d-paris 1,2; f-oslo 5,6; b-null BSON null;
    /// missing elsewhere.
    /// </summary>
    private IMongoCollection<OptCustomer> SeedOpt(string name)
    {
        var collection = database.CreateCollection<OptCustomer>(Unique(name));
        var paris = new BsonDocument
        {
            { "City", "Paris" }, { "Zip", 7 }, { "Geo", Geo(1, 2) }, { "Inner", new BsonDocument { { "Text", "t" }, { "Num", 1 } } },
            { "Req", new BsonDocument("Text", "r") }
        };
        var parisReordered = new BsonDocument
        {
            { "Inner", new BsonDocument { { "Num", 1 }, { "Text", "t" } } }, { "Extra", true }, { "Geo", Geo(1, 2) }, { "Zip", 7 },
            { "Req", new BsonDocument("Text", "r") }, { "City", "Paris" }
        };
        Raw(collection).InsertMany(
        [
            Row("a-missing", null, null, Main(new BsonDocument("Text", "t")), null),
            Row("b-null", BsonNull.Value, null, Main(BsonNull.Value), BsonNull.Value),
            Row("c-empty", new BsonDocument(), null, Main(null), Geo(9, 9)),
            Row("d-paris", paris, parisReordered, Main(null), Geo(1, 2)),
            Row("e-rome", new BsonDocument { { "City", "Rome" }, { "Geo", Geo(3, 4) }, { "Req", new BsonDocument() } }, null, Main(null), null),
            Row("f-oslo", new BsonDocument
            {
                { "City", "Oslo" }, { "Zip", BsonNull.Value }, { "Geo", Geo(5, 6) }, { "Inner", BsonNull.Value }, { "Req", new BsonDocument() }
            }, null, Main(null), Geo(5, 6)),
            Row("g-lima", new BsonDocument { { "City", "Lima" }, { "Geo", Geo(7, 8) }, { "Inner", new BsonDocument() }, { "Req", new BsonDocument() } },
                null, Main(null), null)
        ]);
        return collection;

        static BsonDocument Main(BsonValue? inner)
        {
            var main = new BsonDocument { { "City", "m" }, { "Geo", Geo(0, 0) } };
            if (inner != null)
            {
                main["Inner"] = inner;
            }

            return main;
        }

        static BsonDocument Row(string name, BsonValue? opt, BsonValue? alt, BsonDocument main, BsonValue? pin)
        {
            var row = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", name }, { "Main", main } };
            if (opt != null)
            {
                row["Opt"] = opt;
            }

            if (alt != null)
            {
                row["Alt"] = alt;
            }

            if (pin != null)
            {
                row["OptPin"] = pin;
            }

            return row;
        }
    }

    private Func<MongoQueryMode, List<string>> Opt(
        Func<IQueryable<OptCustomer>, IEnumerable<string>> query, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = SeedOpt(name + Guid.NewGuid().ToString("N")[..4]);
        return mode => Run(collection, mode, ConfigureOpt, query);
    }

    private static IEnumerable<string> Names(IQueryable<OptCustomer> q) => q.Select(c => c.Name).ToList().Order(StringComparer.Ordinal);

    public static readonly Dictionary<string, (Func<IQueryable<OptCustomer>, IEnumerable<string>> Run, string[] Expected)> OptionalNullShapes = new()
    {
        ["eq_null"] = (q => Names(q.Where(c => c.Opt == null)), ["a-missing", "b-null"]),
        ["ne_null"] = (q => Names(q.Where(c => c.Opt != null)), ["c-empty", "d-paris", "e-rome", "f-oslo", "g-lima"]),
        ["not_eq_null"] = (q => Names(q.Where(c => !(c.Opt == null))), ["c-empty", "d-paris", "e-rome", "f-oslo", "g-lima"]),
        ["null_first"] = (q => Names(q.Where(c => null == c.Opt)), ["a-missing", "b-null"]),
        ["ne_null_and_leaf"] = (q => Names(q.Where(c => c.Opt != null && c.Opt.City == "Paris")), ["d-paris"]),
        // A null/missing parent absorbs a leaf comparison to false, as `{ Opt.City: "Paris" }` and C# null propagation do.
        ["leaf_eq"] = (q => Names(q.Where(c => c.Opt!.City == "Paris")), ["d-paris"]),
        // EF's null semantics: a null parent reads City as null, and null != "Paris". `{}` has no City: also null.
        ["leaf_ne"] = (q => Names(q.Where(c => c.Opt!.City != "Paris")), ["a-missing", "b-null", "c-empty", "e-rome", "f-oslo", "g-lima"]),
        ["eq_null_or_leaf"] = (q => Names(q.Where(c => c.Opt == null || c.Opt.City == "Rome")), ["a-missing", "b-null", "e-rome"]),
        ["present_and_null_leaf"] = (q => Names(q.Where(c => c.Opt != null && c.Opt.Zip == null)), ["c-empty", "e-rome", "f-oslo", "g-lima"]),
        ["not_present_and_leaf"] = (q => Names(q.Where(c => !(c.Opt != null && c.Opt.City == "Paris"))),
            ["a-missing", "b-null", "c-empty", "e-rome", "f-oslo", "g-lima"]),
        // A nested optional under an optional parent: the parent's absence makes the inner absent too.
        ["nested_eq_null"] = (q => Names(q.Where(c => c.Opt!.Inner == null)), ["a-missing", "b-null", "c-empty", "e-rome", "f-oslo"]),
        ["nested_ne_null"] = (q => Names(q.Where(c => c.Opt!.Inner != null)), ["d-paris", "g-lima"]),
        // A nested optional under a REQUIRED parent.
        ["required_parent_nested_eq_null"] = (q => Names(q.Where(c => c.Main.Inner == null)),
            ["b-null", "c-empty", "d-paris", "e-rome", "f-oslo", "g-lima"]),
        ["required_parent_nested_ne_null"] = (q => Names(q.Where(c => c.Main.Inner != null)), ["a-missing"]),
        // Optional struct complex property.
        ["struct_eq_null"] = (q => Names(q.Where(c => c.OptPin == null)), ["a-missing", "b-null", "e-rome", "g-lima"]),
        ["struct_has_value"] = (q => Names(q.Where(c => c.OptPin.HasValue)), ["c-empty", "d-paris", "f-oslo"]),
        ["struct_not_has_value"] = (q => Names(q.Where(c => !c.OptPin.HasValue)), ["a-missing", "b-null", "e-rome", "g-lima"]),
        ["count"] = (q => [q.Count(c => c.Opt == null).ToString()], ["2"]),
        ["any"] = (q => [q.Any(c => c.Opt == null).ToString(), q.Any(c => c.Opt != null && c.Opt.City == "Nowhere").ToString()], ["True", "False"]),
        ["first"] = (q => [q.Where(c => c.Opt != null && c.Opt.City == "Rome").Select(c => c.Name).First()], ["e-rome"]),
    };

    public static TheoryData<string> OptionalNullShapeNames => [.. OptionalNullShapes.Keys];

    /// <summary>
    /// The carry-forward requirement (Task 7 review): null and missing answer alike, <c>{}</c> is present, in all three
    /// modes (find-filter <c>{ Opt: null }</c> on the driver path, the same query dialect natively).
    /// </summary>
    [Theory]
    [MemberData(nameof(OptionalNullShapeNames))]
    public void Optional_complex_null_semantics(string shape)
    {
        var (run, expected) = OptionalNullShapes[shape];
        Native(Opt(run, nameof(Optional_complex_null_semantics) + shape), expected);
    }

    [Fact]
    public void Optional_struct_Value_leaf_declines_cleanly()
        // `c.OptPin.Value.Lat` has no native field resolution (a `.Value` hop); the fallback answers it.
        => Declines(Opt(q => Names(q.Where(c => c.OptPin != null && c.OptPin.Value.Lat == 1))), "d-paris");

    [Fact]
    public void Null_check_in_a_projection_renders_null_and_missing_alike()
    {
        // In the aggregation dialect `$eq: ["$Opt", null]` is false for a MISSING Opt; native $ifNull-normalizes it, so
        // a-missing answers like b-null. Driver-LINQ reads a-missing as present (its ternary then reads the missing City as
        // null): pinned as known wrong, so the test breaks when it is fixed.
        AssertNativeServesAndDriverDiffers(
            Opt(q => q.OrderBy(c => c.Name).Select(c => c.Opt == null ? "none" : c.Opt.City).ToList().Select(x => x ?? "<null>")),
            ["none", "none", "<null>", "Paris", "Rome", "Oslo", "Lima"],
            ["<null>", "none", "<null>", "Paris", "Rome", "Oslo", "Lima"]);
        // The bare boolean: driver-LINQ evaluates `c.Opt != null` client-side over the materialized value, which throws for
        // `{}` (a present Opt missing its required City); native computes it on the server.
        PerMode(Opt(q => q.OrderBy(c => c.Name).Select(c => c.Opt != null).ToList().Select(x => x.ToString())),
            ["False", "False", "True", "True", "True", "True", "True"], Serves, Serves,
            "Document element is missing for required non-nullable property 'City'");
    }

    private static void AssertNativeServesAndDriverDiffers(Func<MongoQueryMode, List<string>> run, string[] expected, string[] driverObserved)
    {
        Assert.Equal(expected, run(MongoQueryMode.NativeOnly));
        Assert.Equal(expected, run(MongoQueryMode.Native));
        var driver = run(MongoQueryMode.DriverLinq);
        Assert.NotEqual(expected, driver);
        Assert.Equal(driverObserved, driver);
    }

    [Fact]
    public void Optional_equal_to_a_captured_instance()
    {
        var other = ParisAddress;
        PerMode(Opt(q => Names(q.Where(c => c.Opt == other))), ["d-paris"], Serves, Serves, WholeValueRefused);
        PerMode(Opt(q => Names(q.Where(c => c.Opt != other))), ["a-missing", "b-null", "c-empty", "e-rome", "f-oslo", "g-lima"], Serves,
            Serves, WholeValueRefused);
    }

    [Fact]
    public void Optional_values_compared_with_each_other()
        // a-missing and b-null: both absent, equal (null == null). c-empty: present vs absent. d-paris: equal although Alt is
        // stored reordered with an extra element. Every other row has Opt present and Alt absent.
        => PerMode(Opt(q => Names(q.Where(c => c.Opt == c.Alt))), ["a-missing", "b-null", "d-paris"], Serves, Serves, SerializedDifferently);

    [Fact]
    public void Optional_nested_value_equal_to_an_all_null_instance_requires_presence()
    {
        // `new InnerBits()` has every member null; only a PRESENT Inner equals it (g-lima's `{}`), never an absent one.
        var empty = new InnerBits();
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Inner == empty))), ["g-lima"], Serves, Serves, WholeValueRefused);
        // The same through the translation-time arms: an inline construction and a constant.
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Inner == new InnerBits { Text = null, Num = null }))), ["g-lima"], Serves, Serves,
            WholeValueRefused);
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Inner == EF.Constant(empty)))), ["g-lima"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Required_value_under_an_absent_optional_complex_parent_is_not_equal_to_an_all_null_instance()
    {
        // Opt.Req is required, but Opt is optional: an absent Opt reads Req (and its members) as null.
        // c-empty's Opt is present but its required Req is missing: the compared path is absent, so not equal (R14).
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Req == new InnerBits { Text = null, Num = null }))), ["e-rome", "f-oslo", "g-lima"],
            Serves, Serves, WholeValueRefused);
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Req != new InnerBits { Text = null, Num = null }))), ["a-missing", "b-null", "c-empty", "d-paris"],
            Serves, Serves, WholeValueRefused);
        var empty = new InnerBits();
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Req == empty))), ["e-rome", "f-oslo", "g-lima"], Serves, Serves, WholeValueRefused);
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Req != empty))), ["a-missing", "b-null", "c-empty", "d-paris"], Serves, Serves, WholeValueRefused);
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Req == EF.Constant(empty)))), ["e-rome", "f-oslo", "g-lima"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Stored_pair_under_optional_parents_that_differ_in_presence()
    {
        // Opt.Req vs Main.Req (Main required, no optional ancestor: always a value; its Req is missing, members null): equal
        // only where Opt.Req is present with null members. Opt.Req vs Alt.Req: absent on both sides (a, b, c: null == null),
        // present on both with equal members (d), present on one side only (e, f, g: not equal).
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Req == c.Main.Req))), ["e-rome", "f-oslo", "g-lima"], Serves, Serves,
            SerializedDifferently);
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Req == c.Alt!.Req))), ["a-missing", "b-null", "c-empty", "d-paris"], Serves, Serves,
            SerializedDifferently);
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Req != c.Alt!.Req))), ["e-rome", "f-oslo", "g-lima"], Serves, Serves, SerializedDifferently);
    }

    [Fact]
    public void Present_empty_value_against_a_comparand_with_a_null_required_leaf()
    {
        // `{}` is a present Opt whose required members are all MISSING (materializing it throws, Task 10). Member-wise its
        // required nested values (Geo, Req) are absent under an optional ancestor, so it never equals an instance: no row.
        var nullCity = new OptAddress { City = null!, Zip = null, Geo = new GeoPoint(), Inner = null, Req = new InnerBits() };
        PerMode(Opt(q => Names(q.Where(c => c.Opt == nullCity))), [], Serves, Serves, WholeValueRefused);
    }

    public class SymComplexHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public SymBox A { get; set; } = null!;
        public SymBox B { get; set; } = null!;
        public SymBox? O { get; set; }
        public SymBox? P { get; set; }
    }

    private static void ConfigureSymComplex(ModelBuilder mb)
    {
        foreach (var slot in new[] { "A", "B", "O", "P" })
        {
            mb.Entity<SymComplexHolder>().ComplexProperty(typeof(SymBox), slot, c => c.ComplexProperty(typeof(SymInner), nameof(SymBox.Inner)));
        }
    }

    [Theory]
    [MemberData(nameof(SymPairs))]
    public void Stored_pair_through_complex_slots_is_symmetric(string pair, string form)
    {
        var collection = database.CreateCollection<SymComplexHolder>(Unique(nameof(Stored_pair_through_complex_slots_is_symmetric) + pair + form));
        Raw(collection).InsertMany(SymRows.Select((row, i) =>
        {
            var doc = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", row } };
            foreach (var (slot, states) in SymSlots)
            {
                if (SymValue(states[i]) is { } value)
                {
                    doc[slot] = value;
                }
            }

            return doc;
        }));

        Expression<Func<SymComplexHolder, SymBox?>> left = pair[0] switch { 'A' => h => h.A, _ => h => h.O };
        Expression<Func<SymComplexHolder, SymBox?>> right = pair[1] switch { 'B' => h => h.B, 'P' => h => h.P, _ => h => h.O };
        var predicate = SymPredicate(left, right, form);
        var expected = form.StartsWith("eq") ? SymEqual[pair] : [.. NotIn(SymEqual[pair])];
        PerMode(mode => Run(collection, mode, ConfigureSymComplex, q => q.Where(predicate).Select(h => h.Name).ToList().Order(StringComparer.Ordinal)),
            expected, Serves, Serves, SerializedDifferently);
    }

    public class DeepLeaf
    {
        public int? N { get; set; }
    }

    public class MidBox
    {
        public DeepLeaf Deep { get; set; } = null!;
    }

    public class OuterBox
    {
        public string? City { get; set; }
        public MidBox? Mid { get; set; }
    }

    public class OuterHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public OuterBox L { get; set; } = null!;
        public OuterBox R { get; set; } = null!;
    }

    [Fact]
    public void Optional_intermediate_reaches_grandchildren_on_either_side()
    {
        // L and R are required; Mid is optional; Mid.Deep is required. A present Mid whose Deep is MISSING reads Deep as null
        // (Deep has an optional ancestor, R14). m1: L.Mid {Deep:{}} vs R.Mid {} → Deep value vs Deep null: not equal.
        // m2: both Mid {} → equal. m3: both Mid {Deep:{N:1}} → equal. m4: L.Mid missing, R.Mid {} → null vs value: not equal.
        var collection = database.CreateCollection<OuterHolder>(Unique(nameof(Optional_intermediate_reaches_grandchildren_on_either_side)));
        BsonDocument Box(BsonValue? mid)
        {
            var box = new BsonDocument("City", "c");
            if (mid != null)
            {
                box["Mid"] = mid;
            }

            return box;
        }

        Raw(collection).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "m1" }, { "L", Box(new BsonDocument("Deep", new BsonDocument())) }, { "R", Box(new BsonDocument()) } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "m2" }, { "L", Box(new BsonDocument()) }, { "R", Box(new BsonDocument()) } },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "m3" }, { "L", Box(new BsonDocument("Deep", new BsonDocument("N", 1))) },
                { "R", Box(new BsonDocument("Deep", new BsonDocument("N", 1))) }
            },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "m4" }, { "L", Box(null) }, { "R", Box(new BsonDocument()) } }
        ]);

        static void Configure(ModelBuilder mb)
        {
            foreach (var slot in new[] { "L", "R" })
            {
                mb.Entity<OuterHolder>().ComplexProperty(typeof(OuterBox), slot,
                    o => o.ComplexProperty(typeof(MidBox), nameof(OuterBox.Mid), m => m.ComplexProperty(typeof(DeepLeaf), nameof(MidBox.Deep))));
            }
        }

        PerMode(mode => Run(collection, mode, Configure, q => q.Where(h => h.L == h.R).Select(h => h.Name).ToList().Order(StringComparer.Ordinal)),
            ["m2", "m3"], Serves, Serves, SerializedDifferently);
        PerMode(mode => Run(collection, mode, Configure, q => q.Where(h => h.R == h.L).Select(h => h.Name).ToList().Order(StringComparer.Ordinal)),
            ["m2", "m3"], Serves, Serves, SerializedDifferently);
        PerMode(mode => Run(collection, mode, Configure, q => q.Where(h => h.R != h.L).Select(h => h.Name).ToList().Order(StringComparer.Ordinal)),
            ["m1", "m4"], Serves, Serves, SerializedDifferently);
    }

    [Fact]
    public void Captured_null_instance_takes_the_null_branch()
    {
        // The driver serializes a null comparand as BSON null, so every mode serves it.
        InnerBits? none = null;
        PerMode(Opt(q => Names(q.Where(c => c.Opt!.Inner == none))), ["a-missing", "b-null", "c-empty", "e-rome", "f-oslo"], Serves, Serves,
            Serves);
    }

    [Fact]
    public void Null_and_instance_comparisons_in_one_predicate()
    {
        var empty = new InnerBits();
        PerMode(Opt(q => Names(q.Where(c => c.Opt == null || c.Opt.Inner == empty))), ["a-missing", "b-null", "g-lima"], Serves, Serves,
            WholeValueRefused);
    }

    [Fact]
    public void Optional_struct_equal_to_a_captured_value()
    {
        var pin = new GeoPoint { Lat = 1, Lon = 2 };
        PerMode(Opt(q => Names(q.Where(c => c.OptPin.Equals(pin)))), ["d-paris"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Captured_optional_instance_is_read_per_execution()
    {
        var collection = SeedOpt(nameof(Captured_optional_instance_is_read_per_execution));
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            using var db = Context(collection, mode, ConfigureOpt);
            InnerBits? value = null;
            List<string> RunWith(object? v)
            {
                value = (InnerBits?)v;
                return [.. Names(db.Entities.AsNoTracking().Where(c => c.Opt!.Inner == value))];
            }

            NativeModeAssert.TwiceWithDifferentValues(RunWith, new InnerBits(), ["g-lima"], null,
                ["a-missing", "b-null", "c-empty", "e-rome", "f-oslo"]);
            NativeModeAssert.TwiceWithDifferentValues(RunWith, new InnerBits { Text = "t", Num = 1 }, ["d-paris"], new InnerBits(), ["g-lima"]);
        }
    }

    [Fact]
    public void ExecuteDelete_and_ExecuteUpdate_filtered_on_an_optional_null_check()
    {
        // Bulk operations run on the driver-LINQ bridge, whose find filter `{ Opt: null }` matches null and missing alike.
        // Full bulk coverage is Task 14.
        var deleteCollection = SeedOpt(nameof(ExecuteDelete_and_ExecuteUpdate_filtered_on_an_optional_null_check) + "d");
        using (var db = Context(deleteCollection, MongoQueryMode.Native, ConfigureOpt))
        {
            Assert.Equal(2, db.Entities.Where(c => c.Opt == null).ExecuteDelete());
        }

        Assert.Equal(["c-empty", "d-paris", "e-rome", "f-oslo", "g-lima"],
            Raw(deleteCollection).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d["Name"].AsString).Order(StringComparer.Ordinal));

        var updateCollection = SeedOpt(nameof(ExecuteDelete_and_ExecuteUpdate_filtered_on_an_optional_null_check) + "u");
        using (var db = Context(updateCollection, MongoQueryMode.Native, ConfigureOpt))
        {
            Assert.Equal(5, db.Entities.Where(c => c.Opt != null).ExecuteUpdate(s => s.SetProperty(c => c.Name, c => c.Name + "!")));
        }

        Assert.Equal(["a-missing", "b-null", "c-empty!", "d-paris!", "e-rome!", "f-oslo!", "g-lima!"],
            Raw(updateCollection).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d["Name"].AsString).Order(StringComparer.Ordinal));

        // A NON-null comparand: the bridge can't compare a whole complex value (the serializer refuses, ruling R1); the
        // bulk path reports it as EF's canonical "could not be translated" error, not the raw NotSupportedException.
        var other = ParisAddress;
        using (var db = Context(updateCollection, MongoQueryMode.Native, ConfigureOpt))
        {
            var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.Where(c => c.Opt == other).ExecuteDelete());
            Assert.Contains("could not be translated", ex.Message);
            Assert.Contains(WholeValueRefused, ex.Message);
            Assert.IsType<NotSupportedException>(ex.InnerException);
        }

        Assert.Equal(7, Raw(updateCollection).CountDocuments(FilterDefinition<BsonDocument>.Empty));
    }
#endif

    // ── Fix round 1: optional ancestors, symmetric stored pairs, element scope, misc ───────────────────────────────

    // C1 (any version): a REQUIRED complex value under an OPTIONAL OWNED reference. An absent owner reads its members as
    // null, so `c.Owned.Note == new Note()` (every member null) is FALSE for it and `!=` is TRUE.
    [System.ComponentModel.DataAnnotations.Schema.ComplexType]
    public class Note
    {
        public string? Text { get; set; }
        public int? N { get; set; }
    }

    public class NoteOwner
    {
        public Note Note { get; set; } = null!;
    }

    public class NoteHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public NoteOwner? Owned { get; set; }
        public NoteOwner? Other { get; set; }
    }

    private static void ConfigureNotes(ModelBuilder mb)
    {
        // An owned type has no ComplexProperty builder; Note is a [ComplexType] class.
        mb.Entity<NoteHolder>().OwnsOne(h => h.Owned);
        mb.Entity<NoteHolder>().OwnsOne(h => h.Other);
    }

    /// <summary>
    /// Owned per row: o-missing (no element), o-null (BSON null), o-empty-note (Note {}), o-value (Note t/1). Other: absent
    /// except on o-empty-note ({} too) and o-value (Note t/2).
    /// </summary>
    private Func<MongoQueryMode, List<string>> Notes(
        Func<IQueryable<NoteHolder>, IEnumerable<string>> query, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<NoteHolder>(Unique(name + Guid.NewGuid().ToString("N")[..4]));
        Raw(collection).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "o-missing" } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "o-null" }, { "Owned", BsonNull.Value } },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "o-empty-note" }, { "Owned", new BsonDocument("Note", new BsonDocument()) },
                { "Other", new BsonDocument("Note", new BsonDocument()) }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "o-value" },
                { "Owned", new BsonDocument("Note", new BsonDocument { { "Text", "t" }, { "N", 1 } }) },
                { "Other", new BsonDocument("Note", new BsonDocument { { "Text", "t" }, { "N", 2 } }) }
            }
        ]);
        return mode => Run(collection, mode, ConfigureNotes, query);
    }

    private static IEnumerable<string> Names(IQueryable<NoteHolder> q) => q.Select(c => c.Name).ToList().Order(StringComparer.Ordinal);

    [Fact]
    public void Required_value_under_an_absent_optional_owner_is_not_equal_to_an_all_null_instance()
    {
        PerMode(Notes(q => Names(q.Where(c => c.Owned!.Note == new Note { Text = null, N = null }))), ["o-empty-note"], Serves, Serves,
            WholeValueRefused);
        PerMode(Notes(q => Names(q.Where(c => c.Owned!.Note != new Note { Text = null, N = null }))), ["o-missing", "o-null", "o-value"],
            Serves, Serves, WholeValueRefused);
        var empty = new Note();
        PerMode(Notes(q => Names(q.Where(c => c.Owned!.Note == empty))), ["o-empty-note"], Serves, Serves, WholeValueRefused);
        PerMode(Notes(q => Names(q.Where(c => c.Owned!.Note != empty))), ["o-missing", "o-null", "o-value"], Serves, Serves, WholeValueRefused);
        PerMode(Notes(q => Names(q.Where(c => c.Owned!.Note == EF.Constant(empty)))), ["o-empty-note"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void Stored_pair_with_optional_owners_follows_null_propagation()
    {
        // Both owners absent: each side reads null, null == null. One absent, one present: not equal. o-value: N differs.
        PerMode(Notes(q => Names(q.Where(c => c.Owned!.Note == c.Other!.Note))), ["o-empty-note", "o-missing", "o-null"], Serves, Serves,
            SerializedDifferently);
        PerMode(Notes(q => Names(q.Where(c => c.Owned!.Note != c.Other!.Note))), ["o-value"], Serves, Serves, SerializedDifferently);
    }

    // I1: Home ignores Scratch, Work maps it; both operand orders must answer alike.
    public class ScratchAddr
    {
        public string City { get; set; } = null!;
        public string? Scratch { get; set; }
    }

    public class ScratchHolder
    {
        public ObjectId Id { get; set; }
        public ScratchAddr Home { get; set; } = null!;
        public ScratchAddr Work { get; set; } = null!;
    }

    [Fact]
    public void Stored_pair_with_different_mapped_members_declines_in_both_operand_orders()
    {
        var collection = database.CreateCollection<ScratchHolder>(Unique(nameof(Stored_pair_with_different_mapped_members_declines_in_both_operand_orders)));
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Home", new BsonDocument("City", "c") },
            { "Work", new BsonDocument { { "City", "c" }, { "Scratch", "s" } } }
        });

        static void Configure(ModelBuilder mb)
        {
            mb.Entity<ScratchHolder>().ComplexProperty(h => h.Home, a => a.Ignore(x => x.Scratch));
            mb.Entity<ScratchHolder>().ComplexProperty(h => h.Work);
        }

        PerMode(mode => Run(collection, mode, Configure, q => q.Where(h => h.Home == h.Work).Select(h => h.Home.City)),
            [], NotNative, SerializedDifferently, SerializedDifferently);
        PerMode(mode => Run(collection, mode, Configure, q => q.Where(h => h.Work == h.Home).Select(h => h.Home.City)),
            [], NotNative, SerializedDifferently, SerializedDifferently);
    }

    // R15, nested-complex half: Home ignores a NESTED COMPLEX property (Geo), Work maps it. Both operand orders must
    // decline natively (MapsSameMembers compares nested complex properties as well as leaves).
    public class GeoScratch
    {
        public double Lat { get; set; }
    }

    public class GeoScratchAddr
    {
        public string City { get; set; } = null!;
        public GeoScratch Geo { get; set; } = null!;
    }

    public class GeoScratchHolder
    {
        public ObjectId Id { get; set; }
        public GeoScratchAddr Home { get; set; } = null!;
        public GeoScratchAddr Work { get; set; } = null!;
    }

    [Fact]
    public void Stored_pair_with_different_mapped_nested_complex_properties_declines_in_both_operand_orders()
    {
        var collection = database.CreateCollection<GeoScratchHolder>(Unique(nameof(Stored_pair_with_different_mapped_nested_complex_properties_declines_in_both_operand_orders)));
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Home", new BsonDocument("City", "c") },
            { "Work", new BsonDocument { { "City", "c" }, { "Geo", new BsonDocument("Lat", 1.0) } } }
        });

        static void Configure(ModelBuilder mb)
        {
            mb.Entity<GeoScratchHolder>().ComplexProperty(h => h.Home, a => a.Ignore(x => x.Geo));
            mb.Entity<GeoScratchHolder>().ComplexProperty(h => h.Work, a => a.ComplexProperty(x => x.Geo));
        }

        PerMode(mode => Run(collection, mode, Configure, q => q.Where(h => h.Home == h.Work).Select(h => h.Home.City)),
            [], NotNative, SerializedDifferently, SerializedDifferently);
        PerMode(mode => Run(collection, mode, Configure, q => q.Where(h => h.Work == h.Home).Select(h => h.Home.City)),
            [], NotNative, SerializedDifferently, SerializedDifferently);
    }

    // I2: complex values inside owned-collection elements ($elemMatch, $filter/$map scopes).
    [System.ComponentModel.DataAnnotations.Schema.ComplexType]
    public class Pos
    {
        public int? Zip { get; set; }
        public string? Tag { get; set; }
    }

    public class Item
    {
        public string Sku { get; set; } = null!;
        public Pos Pos { get; set; } = null!;
    }

    public class ItemHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Item> Items { get; set; } = [];
        public Pos Home { get; set; } = null!;
    }

    private static void ConfigureItems(ModelBuilder mb)
    {
        mb.Entity<ItemHolder>().OwnsMany(h => h.Items);
        mb.Entity<ItemHolder>().ComplexProperty(h => h.Home);
    }

    /// <summary>
    /// h-missing: one item whose Pos is MISSING (malformed: Pos is required). h-null: Pos BSON null. h-empty: Pos {} (every
    /// member missing). h-value: Pos 1/"t". Home on every row: Zip 1, Tag "t".
    /// </summary>
    private Func<MongoQueryMode, List<string>> Items(
        Func<IQueryable<ItemHolder>, IEnumerable<string>> query, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<ItemHolder>(Unique(name + Guid.NewGuid().ToString("N")[..4]));
        var home = new BsonDocument { { "Zip", 1 }, { "Tag", "t" } };
        Raw(collection).InsertMany(
        [
            Holder("h-missing", new BsonDocument("Sku", "a")),
            Holder("h-null", new BsonDocument { { "Sku", "a" }, { "Pos", BsonNull.Value } }),
            Holder("h-empty", new BsonDocument { { "Sku", "a" }, { "Pos", new BsonDocument() } }),
            Holder("h-value", new BsonDocument { { "Sku", "a" }, { "Pos", new BsonDocument { { "Zip", 1 }, { "Tag", "t" } } } })
        ]);
        return mode => Run(collection, mode, ConfigureItems, query);

        BsonDocument Holder(string n, BsonDocument item)
            => new() { { "_id", ObjectId.GenerateNewId() }, { "Name", n }, { "Items", new BsonArray { item } }, { "Home", home.DeepClone() } };
    }

    private static IEnumerable<string> Names(IQueryable<ItemHolder> q) => q.Select(c => c.Name).ToList().Order(StringComparer.Ordinal);

    [Fact]
    public void Complex_equality_inside_an_owned_SelectMany_inner_filter_declines_and_leaves_are_prefixed()
    {
        // Reachability of MongoFieldPrefixRewriter's NullSafe carry (Task 13 round 3): an owned SelectMany inner filter
        // holding a whole complex-value equality declines natively (the SelectMany inner translator doesn't admit it), and
        // the fallback refuses the whole value (R1); so no NullSafe member-wise leaf reaches the prefix rewriter today. The
        // rewriter carries NullSafe anyway (defensive; owned fields are never NullSafe, so owned output is unchanged).
        PerMode(Items(q => q.SelectMany(h => h.Items.Where(i => i.Pos == new Pos { Zip = null, Tag = null }), (h, i) => h.Name).ToList()
                .Order(StringComparer.Ordinal)), [], NotNative, WholeValueRefused, WholeValueRefused);
        // The leaf spelling through the complex hop declines too (the fallback serves it).
        PerMode(Items(q => q.SelectMany(h => h.Items.Where(i => i.Pos.Zip == 1 && i.Pos.Tag == "t"), (h, i) => h.Name).ToList()
                .Order(StringComparer.Ordinal)), ["h-value"], NotNative, Serves, Serves);
    }

    [Fact]
    public void Complex_null_check_and_all_null_instance_agree_inside_element_scopes()
    {
        // Pos is REQUIRED with no optional ancestor: a missing/null Pos is malformed and reads its members as null, as at the
        // root (`{Pos.Zip: null}` matches it). `{}` reads every member null. The two dialects ($elemMatch, $filter) agree.
        var blank = new Pos();
        // $elemMatch (query dialect). Driver-LINQ misses the element whose Pos is MISSING (pinned observed value).
        var nullCheck = Items(q => Names(q.Where(h => h.Items.Any(i => i.Pos == null))));
        Assert.Equal(["h-missing", "h-null"], nullCheck(MongoQueryMode.NativeOnly));
        Assert.Equal(["h-missing", "h-null"], nullCheck(MongoQueryMode.Native));
        Assert.Equal(["h-null"], nullCheck(MongoQueryMode.DriverLinq));
        PerMode(Items(q => Names(q.Where(h => h.Items.Any(i => i.Pos == new Pos { Zip = null, Tag = null })))), ["h-empty", "h-missing", "h-null"],
            Serves, Serves, WholeValueRefused);
        // A captured class comparand carries a per-execution `{ $expr: <is-null parameter> }` branch, which is illegal in
        // $elemMatch: it declines and the fallback refuses (R1).
        PerMode(Items(q => Names(q.Where(h => h.Items.Any(i => i.Pos == blank)))), [], NotNative, WholeValueRefused, WholeValueRefused);
        // $filter scope (filtered Count).
        // Driver-LINQ's $filter `$eq: ["$$e.Pos", null]` misses the MISSING Pos (pinned observed value).
        var nullCount = Items(q => q.OrderBy(h => h.Name).Select(h => h.Items.Count(i => i.Pos == null)).ToList().Select(x => x.ToString()));
        Assert.Equal(["0", "1", "1", "0"], nullCount(MongoQueryMode.NativeOnly));
        Assert.Equal(["0", "1", "1", "0"], nullCount(MongoQueryMode.Native));
        Assert.Equal(["0", "0", "1", "0"], nullCount(MongoQueryMode.DriverLinq));
        PerMode(Items(q => q.OrderBy(h => h.Name).Select(h => h.Items.Count(i => i.Pos == blank)).ToList().Select(x => x.ToString())),
            ["1", "1", "1", "0"], Serves, Serves, WholeValueRefused);
        PerMode(Items(q => q.OrderBy(h => h.Name).Select(h => h.Items.Count(i => i.Pos == new Pos { Zip = null, Tag = null })).ToList()
            .Select(x => x.ToString())), ["1", "1", "1", "0"], Serves, Serves, WholeValueRefused);

        // PRE-EXISTING, OWNER-RULED characterization (not a regression of this slice): a bare scalar leaf compared with null
        // inside a $filter scope renders `$eq: ["$$e.Pos.Zip", null]` without $ifNull (RenderBinary: missing-vs-null stays
        // distinguished in element scopes, pinned by NativeOwnedCollectionFilteredCountTests), so a MISSING Zip is not null
        // there, while C# (and the two complex predicates above) answer [1, 1, 1, 0]. Breaks loudly if that ruling changes.
        var leafCount = Items(q => q.OrderBy(h => h.Name).Select(h => h.Items.Count(i => i.Pos.Zip == null)).ToList().Select(x => x.ToString()));
        Assert.Equal(LeafCountObserved, leafCount(MongoQueryMode.NativeOnly));
        Assert.Equal(LeafCountObserved, leafCount(MongoQueryMode.Native));
        Assert.Equal(LeafCountObserved, leafCount(MongoQueryMode.DriverLinq));
    }

    // The same in every mode (the owner-ruled element-scope leaf rule), so one array.
    private static readonly string[] LeafCountObserved = ["0", "0", "0", "0"];

    [Fact]
    public void Correlated_element_equality_to_an_outer_complex_value()
    {
        // Two scopes: the element's Pos against the root's Home. Pinned per mode.
        PerMode(Items(q => Names(q.Where(h => h.Items.Any(i => i.Pos == h.Home)))), ["h-value"], NotNative, SerializedDifferently,
            SerializedDifferently);
    }

    [Fact]
    public void All_over_a_complex_equality()
    {
        // All(pred) $matches the complement of pred, and only a query-dialect predicate has one (MongoExpressionNegator). A
        // captured comparand's per-execution null branch is `{ $expr: <bool parameter> }`, so All over it declines and the
        // fallback refuses (R1); a constant comparand is pure query dialect and stays native.
        var other = AnnAddress;
        PerMode(Eq(q => [q.All(c => c.Home == other).ToString()]), [], NotNative, WholeValueRefused, WholeValueRefused);
        PerMode(Eq(q => [q.All(c => c.Home == new EqAddress { City = "Paris", Zip = 7, Geo = new GeoPoint { Lat = 1, Lon = 2 }, Grade = Grade.High })
            .ToString()]), ["False"], Serves, Serves, WholeValueRefused);
        PerMode(Eq(q => [q.All(c => c.Home != new EqAddress { City = "x", Zip = null, Geo = new GeoPoint(), Grade = Grade.Low }).ToString()]),
            ["True"], Serves, Serves, WholeValueRefused);
        PerMode(Eq(q => [q.All(c => c.Home != null).ToString()]), ["True"], Serves, Serves, Serves);
    }

    [Fact]
    public void Default_struct_construction_is_a_known_value()
    {
        // `new GeoPoint()` has no bindings: every member is default (0, 0). No row's Pin is 0/0.
        PerMode(Eq(q => Names(q.Where(c => c.Pin.Equals(new GeoPoint())))), [], Serves, Serves, WholeValueRefused);
        PerMode(Eq(q => Names(q.Where(c => !c.Pin.Equals(new GeoPoint())))), ["ann", "bob", "cid", "dee"], Serves, Serves, WholeValueRefused);
    }

    [Fact]
    public void A_declining_equality_inside_a_disjunction_declines_the_whole_predicate_and_leaves_no_residue()
    {
        // The second disjunct (an inline construction reading the row) declines, so the whole predicate declines; the
        // translator-level residue check is the unit test of the same name.
        var other = AnnAddress;
        PerMode(Eq(q => Names(q.Where(c => c.Home == other || c.Home == new EqAddress { City = c.Name }).Where(c => c.Pin.Lat == 1))),
            ["ann"], NotNative, WholeValueRefused, WholeValueRefused);
    }

    // ── Fix round 2: stored-pair symmetry matrix (each side keeps its own presence flag) ─────────────────────────

    // Slot states, seeded as raw BSON: M missing, N BSON null, L {City:"x"} (Inner missing), E {} , F {City:"x", Inner:{N:1}},
    // F2 {City:"x", Inner:{}}. Interpretation (C# null propagation, ruling R14): a slot with no optional ancestor is never
    // absent (missing/null reads members null, Inner too); an optional slot that is missing/null is null, and its Inner,
    // missing, is null. Rows (A,B required | O,P optional):
    //   r1 M,E | M,N   r2 L,F2 | L,F2   r3 F,F | F,F   r4 E,L | E,E   r5 F2,L | F2,N   r6 L,N | F2,L   r7 N,M | N,E
    // Hand-derived: A==B {r1,r2,r3,r5,r7}; O==P {r1,r3,r4}; A==O {r3,r5,r6} (r6: A.Inner missing under a required A reads a
    // value with null members, O.Inner {} is a value with null members: equal; the leak made A==O and O==A disagree here).
    private static readonly string[] SymRows = ["r1", "r2", "r3", "r4", "r5", "r6", "r7"];

    private static readonly Dictionary<string, string[]> SymSlots = new()
    {
        ["A"] = ["M", "L", "F", "E", "F2", "L", "N"],
        ["B"] = ["E", "F2", "F", "L", "L", "N", "M"],
        ["O"] = ["M", "L", "F", "E", "F2", "F2", "N"],
        ["P"] = ["N", "F2", "F", "E", "N", "L", "E"]
    };

    public static readonly Dictionary<string, string[]> SymEqual = new()
    {
        ["AB"] = ["r1", "r2", "r3", "r5", "r7"],
        ["OP"] = ["r1", "r3", "r4"],
        ["AO"] = ["r3", "r5", "r6"]
    };

    private static BsonValue? SymValue(string state)
        => state switch
        {
            "M" => null,
            "N" => BsonNull.Value,
            "L" => new BsonDocument("City", "x"),
            "E" => new BsonDocument(),
            "F" => new BsonDocument { { "City", "x" }, { "Inner", new BsonDocument("N", 1) } },
            "F2" => new BsonDocument { { "City", "x" }, { "Inner", new BsonDocument() } },
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

    private static IEnumerable<string> NotIn(string[] rows) => SymRows.Except(rows);

    [System.ComponentModel.DataAnnotations.Schema.ComplexType]
    public class SymInner
    {
        public int? N { get; set; }
    }

    [System.ComponentModel.DataAnnotations.Schema.ComplexType]
    public class SymBox
    {
        public string? City { get; set; }
        public SymInner Inner { get; set; } = null!;
    }

    // Owned analogue (any EF version): the slot is an owned reference holding the compared complex value `Box`.
    public class SymOwner
    {
        public SymBox Box { get; set; } = null!;
    }

    public class SymOwnedHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public SymOwner A { get; set; } = null!;
        public SymOwner B { get; set; } = null!;
        public SymOwner? O { get; set; }
        public SymOwner? P { get; set; }
    }

    private static void ConfigureSymOwned(ModelBuilder mb)
    {
        mb.Entity<SymOwnedHolder>(b =>
        {
            b.OwnsOne(h => h.A);
            b.OwnsOne(h => h.B);
            b.OwnsOne(h => h.O);
            b.OwnsOne(h => h.P);
            b.Navigation(h => h.A).IsRequired();
            b.Navigation(h => h.B).IsRequired();
        });
    }

    private Func<MongoQueryMode, List<string>> SymOwned(
        Func<IQueryable<SymOwnedHolder>, IEnumerable<string>> query, string name)
    {
        var collection = database.CreateCollection<SymOwnedHolder>(Unique(name));
        // A slot state applies to Box; the owner element is present except M/N, which apply to the owner itself.
        Raw(collection).InsertMany(SymRows.Select((row, i) =>
        {
            var doc = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", row } };
            foreach (var (slot, states) in SymSlots)
            {
                var state = states[i];
                if (state == "N")
                {
                    doc[slot] = BsonNull.Value;
                }
                else if (state != "M")
                {
                    doc[slot] = new BsonDocument("Box", SymValue(state)!);
                }
            }

            return doc;
        }));
        return mode => Run(collection, mode, ConfigureSymOwned, query);
    }

    public static TheoryData<string, string> SymPairs()
    {
        var data = new TheoryData<string, string>();
        foreach (var pair in new[] { "AB", "OP", "AO" })
        foreach (var form in new[] { "eq", "eq_reversed", "ne", "ne_reversed" })
        {
            data.Add(pair, form);
        }

        return data;
    }

    private static Expression<Func<T, bool>> SymPredicate<T>(Expression<Func<T, SymBox?>> left, Expression<Func<T, SymBox?>> right, string form)
    {
        var parameter = left.Parameters[0];
        var l = left.Body;
        var r = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);
        Expression body = form switch
        {
            "eq" => Expression.Equal(l, r),
            "eq_reversed" => Expression.Equal(r, l),
            "ne" => Expression.NotEqual(l, r),
            _ => Expression.NotEqual(r, l)
        };
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }

    [Theory]
    [MemberData(nameof(SymPairs))]
    public void Stored_pair_through_owned_slots_is_symmetric(string pair, string form)
    {
        Expression<Func<SymOwnedHolder, SymBox?>> left = pair[0] switch { 'A' => h => h.A.Box, _ => h => h.O!.Box };
        Expression<Func<SymOwnedHolder, SymBox?>> right = pair[1] switch { 'B' => h => h.B.Box, 'P' => h => h.P!.Box, _ => h => h.O!.Box };
        var predicate = SymPredicate(left, right, form);
        var expected = form.StartsWith("eq") ? SymEqual[pair] : [.. NotIn(SymEqual[pair])];
        PerMode(SymOwned(q => q.Where(predicate).Select(h => h.Name).ToList().Order(StringComparer.Ordinal),
                nameof(Stored_pair_through_owned_slots_is_symmetric) + pair + form),
            expected, Serves, Serves, SerializedDifferently);
    }

    // ── Plumbing ───────────────────────────────────────────────────────────────────────────────────────────────

    private static List<string> Run<TEntity>(
        IMongoCollection<TEntity> collection, MongoQueryMode mode, Action<ModelBuilder> configure,
        Func<IQueryable<TEntity>, IEnumerable<string>> query)
        where TEntity : class
    {
        using var db = Context(collection, mode, configure);
        return [.. query(db.Entities.AsNoTracking())];
    }

    private static SingleEntityDbContext<TEntity> Context<TEntity>(
        IMongoCollection<TEntity> collection, MongoQueryMode mode, Action<ModelBuilder> configure)
        where TEntity : class
        => SingleEntityDbContext.Create(collection, configure, null, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    private static IMongoCollection<BsonDocument> Raw<T>(IMongoCollection<T> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    private static string Unique(string name)
        => TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
}
