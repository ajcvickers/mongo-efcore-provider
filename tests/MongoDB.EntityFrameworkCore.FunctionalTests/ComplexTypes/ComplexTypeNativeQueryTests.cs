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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Metadata.Conventions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Native predicates, ordering, scalar projection, grouping and aggregates over complex-property leaves
/// (<c>c.Address.City</c>, <c>c.Address.Location.Lat</c>). Every shape runs under <see cref="MongoQueryMode.NativeOnly"/>
/// (proves it goes native) and is compared with the <see cref="MongoQueryMode.DriverLinq"/> oracle.
/// </summary>
/// <remarks>
/// Every query here projects scalars, anonymous types of scalars or aggregates; materializing entities and whole complex
/// values is covered by <see cref="ComplexTypeMaterializationTests"/>. Documents are seeded as raw BSON so missing/null
/// states can be expressed.
/// </remarks>
[XUnitCollection("QueryTests")]
public class ComplexTypeNativeQueryTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public enum Grade
    {
        Low,
        Mid,
        High
    }

    public class RichAddress
    {
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public GeoPoint Location { get; set; }
        public int Floor { get; set; }
        public bool Verified { get; set; }
        public Grade Grade { get; set; }
        public int? Zip { get; set; }
    }

    public class RichCustomer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public RichAddress Address { get; set; } = null!;
    }

    public class TwoAddressCustomer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public RichAddress BillingAddress { get; set; } = null!;
        public RichAddress ShippingAddress { get; set; } = null!;
    }

    public class CodeBox
    {
        public int Count { get; set; }
        public int Code { get; set; }
        public string Label { get; set; } = null!;
    }

    public class ConvertedCustomer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public CodeBox Box { get; set; } = null!;
    }

    private static void ConfigureRich(ModelBuilder mb)
        => mb.Entity<RichCustomer>().ComplexProperty(c => c.Address, a => a.ComplexProperty(x => x.Location));

    private static BsonDocument Address(
        string city, double lat, double lon, int floor, bool verified, Grade grade, BsonValue? zip, string? street = null)
    {
        var doc = new BsonDocument
        {
            { "Street", street ?? city + " st" },
            { "City", city },
            { "Location", new BsonDocument { { "Lat", lat }, { "Lon", lon } } },
            { "Floor", floor },
            { "Verified", verified },
            { "Grade", (int)grade }
        };
        if (zip != null)
        {
            doc.Add("Zip", zip);
        }

        return doc;
    }

    // Five well-formed rows (every required leaf present). Zip: missing (Ann), BSON null (Bob), values otherwise.
    // Two rows share City "Paris" and Lat/Lon values overlap so ordering, grouping and Distinct are meaningful.
    private IMongoCollection<RichCustomer> SeedRich([System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<RichCustomer>(UniqueName(name));
        database.GetCollection<BsonDocument>(collection.CollectionNamespace).InsertMany(
        [
            Root("Ann", 3, Address("Paris", 1.5, 10.0, 2, true, Grade.High, null)),
            Root("Bob", 1, Address("London", 0.5, 20.0, 5, false, Grade.Low, BsonNull.Value)),
            Root("Cid", 2, Address("Paris", 2.5, 10.0, 1, true, Grade.Mid, 7)),
            Root("Dee", 5, Address("Berlin", 3.5, 30.0, 7, false, Grade.High, 2)),
            Root("Eve", 4, Address("Amsterdam", 1.0, 40.0, 3, true, Grade.Low, 9, street: "Canal"))
        ]);
        return collection;

        static BsonDocument Root(string name, int rank, BsonDocument address)
            => new() { { "_id", ObjectId.GenerateNewId() }, { "Name", name }, { "Rank", rank }, { "Address", address } };
    }

    private List<T> Run<TEntity, T>(
        IMongoCollection<TEntity> collection,
        MongoQueryMode mode,
        Func<IQueryable<TEntity>, IEnumerable<T>> query,
        Action<ModelBuilder> configure,
        Action<ModelConfigurationBuilder>? conventions = null)
        where TEntity : class
    {
        using var db = SingleEntityDbContext.Create(collection, configure, conventions, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });
        return query(db.Entities.AsNoTracking()).ToList();
    }

    private List<T> Rich<T>(IMongoCollection<RichCustomer> collection, MongoQueryMode mode,
        Func<IQueryable<RichCustomer>, IEnumerable<T>> query)
        => Run(collection, mode, query, ConfigureRich);

    private List<T> Parity<T>(Func<IQueryable<RichCustomer>, IEnumerable<T>> query,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = SeedRich(name);
        return NativeModeAssert.NativeAndParity(m => Rich(collection, m, query));
    }

    private string LoggedMql<TEntity, T>(
        IMongoCollection<TEntity> collection,
        Func<IQueryable<TEntity>, IEnumerable<T>> query,
        Action<ModelBuilder> configure,
        Action<ModelConfigurationBuilder>? conventions = null)
        where TEntity : class
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        using (var db = SingleEntityDbContext.Create(collection, loggerFactory, configure, conventions, b =>
               {
                   b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                   b.EnableSensitiveDataLogging();
                   new MongoDbContextOptionsBuilder(b).UseQueryMode(MongoQueryMode.NativeOnly);
               }))
        {
            _ = query(db.Entities.AsNoTracking()).ToList();
        }

        return spy.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
    }

    // ── Predicates ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Where_equality_on_complex_leaf()
        => Assert.Equal(["Ann", "Cid"], Parity(q => q.Where(c => c.Address.City == "Paris").OrderBy(c => c.Name).Select(c => c.Name)));

    [Fact]
    public void Where_inequality_on_complex_leaf()
        => Assert.Equal(["Amsterdam", "Berlin", "London"],
            Parity(q => q.Where(c => c.Address.City != "Paris").OrderBy(c => c.Address.City).Select(c => c.Address.City)));

    [Fact]
    public void Where_relational_on_complex_leaf()
    {
        Assert.Equal(["Bob", "Dee"], Parity(q => q.Where(c => c.Address.Floor > 3).OrderBy(c => c.Name).Select(c => c.Name)));
        Assert.Equal(["Ann", "Cid", "Eve"], Parity(q => q.Where(c => c.Address.Floor <= 3).OrderBy(c => c.Name).Select(c => c.Name),
            nameof(Where_relational_on_complex_leaf) + "_le"));
    }

    [Fact]
    public void Where_on_nested_struct_leaf()
        => Assert.Equal(["Ann", "Cid", "Dee"], Parity(q => q.Where(c => c.Address.Location.Lat > 1).OrderBy(c => c.Name).Select(c => c.Name)));

    [Fact]
    public void Where_string_methods_on_complex_leaf()
    {
        Assert.Equal(["Ann", "Cid"], Parity(q => q.Where(c => c.Address.City.StartsWith("Par")).OrderBy(c => c.Name).Select(c => c.Name), "sw"));
        Assert.Equal(["Bob", "Eve"], Parity(q => q.Where(c => c.Address.City.Contains("o") || c.Address.City.Contains("m")).OrderBy(c => c.Name).Select(c => c.Name), "co"));
        Assert.Equal(["Dee"], Parity(q => q.Where(c => c.Address.City.EndsWith("lin")).OrderBy(c => c.Name).Select(c => c.Name), "ew"));
        Assert.Equal(["Bob"], Parity(q => q.Where(c => c.Address.City.ToUpper() == "LONDON").Select(c => c.Name), "up"));
        Assert.Equal(["Bob", "Dee", "Eve"], Parity(q => q.Where(c => c.Address.City.Length > 5).OrderBy(c => c.Name).Select(c => c.Name), "len"));
    }

    [Fact]
    public void Where_bool_and_enum_complex_leaves()
    {
        Assert.Equal(["Ann", "Cid", "Eve"], Parity(q => q.Where(c => c.Address.Verified).OrderBy(c => c.Name).Select(c => c.Name), "b"));
        Assert.Equal(["Bob", "Dee"], Parity(q => q.Where(c => !c.Address.Verified).OrderBy(c => c.Name).Select(c => c.Name), "nb"));
        Assert.Equal(["Ann", "Dee"], Parity(q => q.Where(c => c.Address.Grade == Grade.High).OrderBy(c => c.Name).Select(c => c.Name), "e"));
    }

    [Fact]
    public void Where_nullable_complex_leaf_null_missing_and_value()
    {
        // Zip: Ann missing, Bob BSON null, Cid 7, Dee 2, Eve 9.
        Assert.Equal(["Ann", "Bob"], Parity(q => q.Where(c => c.Address.Zip == null).OrderBy(c => c.Name).Select(c => c.Name), "eqnull"));
        Assert.Equal(["Cid", "Dee", "Eve"], Parity(q => q.Where(c => c.Address.Zip != null).OrderBy(c => c.Name).Select(c => c.Name), "nenull"));
        Assert.Equal(["Cid"], Parity(q => q.Where(c => c.Address.Zip == 7).Select(c => c.Name), "eq7"));
        Assert.Equal(["Cid", "Eve"], Parity(q => q.Where(c => c.Address.Zip > 3).OrderBy(c => c.Name).Select(c => c.Name), "gt3"));
        // C#: null < 3 is false, so only Dee.
        Assert.Equal(["Dee"], Parity(q => q.Where(c => c.Address.Zip < 3).OrderBy(c => c.Name).Select(c => c.Name), "lt3"));
        Assert.Equal(["Ann", "Bob", "Cid", "Eve"], Parity(q => q.Where(c => c.Address.Zip != 2).OrderBy(c => c.Name).Select(c => c.Name), "ne2"));
    }

    [Fact]
    public void Where_combined_and_or_not_over_complex_leaves()
    {
        Assert.Equal(["Cid"], Parity(q => q.Where(c => c.Address.City == "Paris" && c.Address.Floor < 2).Select(c => c.Name), "and"));
        Assert.Equal(["Ann", "Cid", "Dee"],
            Parity(q => q.Where(c => c.Address.City == "Paris" || c.Address.Location.Lon >= 30 && c.Address.Grade == Grade.High)
                .OrderBy(c => c.Name).Select(c => c.Name), "or"));
        Assert.Equal(["Ann", "Bob", "Eve"],
            Parity(q => q.Where(c => !(c.Address.City == "Paris" && c.Address.Floor < 2) && !(c.Address.Floor > 6))
                .OrderBy(c => c.Name).Select(c => c.Name), "not"));
        // Negated relational over a nullable leaf: C# !(null > 3) is true, so the null/missing rows are included.
        Assert.Equal(["Ann", "Bob", "Dee"], Parity(q => q.Where(c => !(c.Address.Zip > 3)).OrderBy(c => c.Name).Select(c => c.Name), "notgt"));
        Assert.Equal(["Bob", "Dee"], Parity(q => q.Where(c => !(c.Address.City == "Paris" || c.Address.Verified)).OrderBy(c => c.Name).Select(c => c.Name), "notor"));
    }

    [Fact]
    public void Where_complex_leaf_combined_with_root_scalar()
        => Assert.Equal(["Ann"], Parity(q => q.Where(c => c.Address.City == "Paris" && c.Rank > 2).Select(c => c.Name)));

    [Fact]
    public void Where_local_collection_contains_complex_leaf()
    {
        var cities = new List<string> { "Paris", "Berlin" };
        Assert.Equal(["Ann", "Cid", "Dee"], Parity(q => q.Where(c => cities.Contains(c.Address.City)).OrderBy(c => c.Name).Select(c => c.Name)));
    }

    // ── Ordering and paging ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OrderBy_ThenBy_on_complex_leaves()
    {
        Assert.Equal(["Eve", "Dee", "Bob", "Cid", "Ann"],
            Parity(q => q.OrderBy(c => c.Address.City).ThenByDescending(c => c.Address.Location.Lat).Select(c => c.Name)
                .ToList().Select(n => n), "ob"));
        Assert.Equal(["Dee", "Cid", "Ann", "Eve", "Bob"],
            Parity(q => q.OrderByDescending(c => c.Address.Location.Lat).Select(c => c.Name), "obd"));
        Assert.Equal(["Cid", "Ann", "Bob", "Dee", "Eve"],
            Parity(q => q.OrderBy(c => c.Address.Location.Lon).ThenBy(c => c.Address.Floor).ThenBy(c => c.Name).Select(c => c.Name), "obtb"));
    }

    [Fact]
    public void First_Single_Skip_Take_with_complex_leaf_ordering()
    {
        Assert.Equal(["Bob"], Parity(q => new[] { q.OrderBy(c => c.Address.Location.Lat).Select(c => c.Name).First() }, "first"));
        Assert.Equal(["Dee"], Parity(q => new[] { q.Where(c => c.Address.City == "Berlin").Select(c => c.Name).Single() }, "single"));
        Assert.Equal(["Ann", "Cid"], Parity(q => q.OrderBy(c => c.Address.Location.Lat).Skip(2).Take(2).Select(c => c.Name), "page"));
        Assert.Equal([1.0], Parity(q => new[] { q.OrderByDescending(c => c.Address.Floor).Skip(2).Select(c => c.Address.Location.Lat).First() }, "skipfirst"));
    }

    // ── Projection ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Select_bare_complex_leaf()
    {
        Assert.Equal(["Paris", "London", "Paris", "Berlin", "Amsterdam"], Parity(q => q.OrderBy(c => c.Name).Select(c => c.Address.City), "city"));
        Assert.Equal([1.5, 0.5, 2.5, 3.5, 1.0], Parity(q => q.OrderBy(c => c.Name).Select(c => c.Address.Location.Lat), "lat"));
        Assert.Equal([null, null, 7, 2, 9], Parity(q => q.OrderBy(c => c.Name).Select(c => c.Address.Zip), "zip"));
    }

    [Fact]
    public void Select_bare_complex_leaf_survives_a_late_native_factory_decline()
    {
        // A parameterized StartsWith has no native regex rendering, so under Native the factory declines after the gate
        // and the `_v` push-down runs on driver-LINQ through the same shaper.
        var collection = SeedRich();
        var prefix = "Par";
        Assert.Equal([1.5, 2.5], Rich(collection, MongoQueryMode.Native,
            q => q.Where(c => c.Address.City.StartsWith(prefix)).OrderBy(c => c.Name).Select(c => c.Address.Location.Lat)));
        Assert.Equal([1.5, 2.5], Rich(collection, MongoQueryMode.DriverLinq,
            q => q.Where(c => c.Address.City.StartsWith(prefix)).OrderBy(c => c.Name).Select(c => c.Address.Location.Lat)));
    }

    [Fact]
    public void Select_anonymous_with_complex_leaves()
    {
        var results = Parity(q => q.OrderBy(c => c.Name).Select(c => new { c.Name, c.Address.Location.Lon, c.Address.Grade, c.Address.Zip, c.Address.Verified }));
        Assert.Equal(["Ann", "Bob", "Cid", "Dee", "Eve"], results.Select(r => r.Name));
        Assert.Equal([10.0, 20.0, 10.0, 30.0, 40.0], results.Select(r => r.Lon));
        Assert.Equal([Grade.High, Grade.Low, Grade.Mid, Grade.High, Grade.Low], results.Select(r => r.Grade));
        Assert.Equal([null, null, 7, 2, 9], results.Select(r => r.Zip));
    }

    [Fact]
    public void Select_EF_Property_spellings_of_complex_leaves()
    {
        var collection = SeedRich();
        NativeModeAssert.NativeAndExpected(m => Rich(collection, m, q => q.OrderBy(c => c.Name)
                .Select(c => new { c.Name, City = EF.Property<string>(EF.Property<RichAddress>(c, "Address"), "City") }).ToList()
                .Select(x => x.Name + ":" + x.City)),
            ["Ann:Paris", "Bob:London", "Cid:Paris", "Dee:Berlin", "Eve:Amsterdam"]);
        NativeModeAssert.NativeAndExpected(m => Rich(collection, m, q => q.OrderBy(c => c.Name)
                .Select(c => EF.Property<RichAddress>(c, "Address").Location.Lon)),
            [10.0, 20.0, 10.0, 30.0, 40.0]);
        NativeModeAssert.NativeAndExpected(m => Rich(collection, m, q => q
                .Where(c => EF.Property<string>(EF.Property<RichAddress>(c, "Address"), "City") == "Paris")
                .OrderBy(c => c.Name).Select(c => c.Name)),
            ["Ann", "Cid"]);
    }

    [Fact]
    public void Select_computed_over_complex_leaves()
    {
        var results = Parity(q => q.OrderBy(c => c.Name)
            .Select(c => new { Sum = c.Address.Location.Lat + c.Address.Location.Lon, Big = c.Address.Floor > 2, c.Address.City.Length }));
        Assert.Equal([11.5, 20.5, 12.5, 33.5, 41.0], results.Select(r => r.Sum));
    }

    [Fact]
    public void Distinct_of_complex_leaf()
    {
        Assert.Equal(["Amsterdam", "Berlin", "London", "Paris"],
            Parity(q => q.Select(c => c.Address.City).Distinct().ToList().OrderBy(x => x), "bare"));
        Assert.Equal([10.0, 20.0, 30.0, 40.0],
            Parity(q => q.Select(c => new { c.Address.Location.Lon }).Distinct().ToList().Select(x => x.Lon).OrderBy(x => x), "anon"));
    }

    // ── Grouping and aggregates ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GroupBy_complex_leaf_with_aggregates()
    {
        var results = Parity(q => q.GroupBy(c => c.Address.City)
            .Select(g => new
            {
                g.Key,
                Count = g.Count(),
                Sum = g.Sum(c => c.Address.Floor),
                Min = g.Min(c => c.Address.Location.Lat),
                Max = g.Max(c => c.Address.Location.Lat),
                Avg = g.Average(c => c.Address.Location.Lon)
            })
            .ToList()
            .OrderBy(g => g.Key));

        Assert.Equal(["Amsterdam", "Berlin", "London", "Paris"], results.Select(r => r.Key));
        var paris = results.Single(r => r.Key == "Paris");
        Assert.Equal(2, paris.Count);
        Assert.Equal(3, paris.Sum);
        Assert.Equal(1.5, paris.Min);
        Assert.Equal(2.5, paris.Max);
        Assert.Equal(10.0, paris.Avg);
    }

    [Fact]
    public void GroupBy_nested_struct_leaf_count()
        => Assert.Equal([(10.0, 2), (20.0, 1), (30.0, 1), (40.0, 1)],
            Parity(q => q.GroupBy(c => c.Address.Location.Lon).Select(g => new { g.Key, C = g.Count() }).ToList()
                .Select(x => (x.Key, x.C)).OrderBy(x => x.Key)));

    [Fact]
    public void Terminal_aggregates_over_complex_leaves()
    {
        Assert.Equal([0.5], Parity(q => new[] { q.Min(c => c.Address.Location.Lat) }, "min"));
        Assert.Equal([3.5], Parity(q => new[] { q.Max(c => c.Address.Location.Lat) }, "max"));
        Assert.Equal([22.0], Parity(q => new[] { q.Average(c => c.Address.Location.Lon) }, "avg"));
        Assert.Equal([18], Parity(q => new[] { q.Sum(c => c.Address.Floor) }, "sum"));
        Assert.Equal([18], Parity(q => new[] { q.Sum(c => c.Address.Zip) }, "sumnullable"));
        Assert.Equal([2], Parity(q => new[] { q.Count(c => c.Address.City == "Paris") }, "count"));
        Assert.Equal([true], Parity(q => new[] { q.Any(c => c.Address.Location.Lon > 35) }, "any"));
        Assert.Equal([false], Parity(q => new[] { q.All(c => c.Address.Verified) }, "all"));
        Assert.Equal([true], Parity(q => new[] { q.All(c => c.Address.Floor > 0) }, "alltrue"));
        Assert.Equal([9], Parity(q => new[] { q.Max(c => c.Address.Zip) }, "nullablemax"));

        // Driver-LINQ is wrong for a nullable Min (pre-existing, same as a root property: NativeMalformedAggregateAndDistinctTests
        // "min_nullable"): it reduces {_v: value} documents, so the missing/null Zip wins and it answers null; LINQ skips nulls.
        var minCollection = SeedRich(nameof(Terminal_aggregates_over_complex_leaves) + "_nullablemin");
        NativeModeAssert.NativeAndExpected(m => Rich(minCollection, m, q => new[] { q.Min(c => c.Address.Zip) }), [2], driverKnownWrong: true);
    }

    // ── Element names ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Same_complex_clr_type_at_two_paths_with_different_element_names()
    {
        var collection = database.CreateCollection<TwoAddressCustomer>(UniqueName(nameof(Same_complex_clr_type_at_two_paths_with_different_element_names)));
        // Billing and Shipping deliberately hold DIFFERENT cities/lats per row, so reading the wrong path is visible.
        database.GetCollection<BsonDocument>(collection.CollectionNamespace).InsertMany(
        [
            Two("Ann", Address("Paris", 1, 1, 1, true, Grade.Low, null), Address("Rome", 9, 9, 9, false, Grade.High, null)),
            Two("Bob", Address("Rome", 2, 2, 2, false, Grade.Mid, null), Address("Paris", 8, 8, 8, true, Grade.Low, null))
        ]);

        void Configure(ModelBuilder mb)
        {
            mb.Entity<TwoAddressCustomer>().ComplexProperty(c => c.BillingAddress, a =>
            {
                a.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "bill");
                a.Property(x => x.City).Metadata.SetElementName("billCity");
                a.ComplexProperty(x => x.Location);
            });
            mb.Entity<TwoAddressCustomer>().ComplexProperty(c => c.ShippingAddress, a =>
            {
                a.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "ship");
                a.ComplexProperty(x => x.Location).HasPropertyAnnotation(MongoAnnotationNames.ElementName, "loc");
            });
        }

        List<T> Both<T>(Func<IQueryable<TwoAddressCustomer>, IEnumerable<T>> q)
            => NativeModeAssert.NativeAndParity(m => Run(collection, m, q, Configure));

        Assert.Equal(["Ann"], Both(q => q.Where(c => c.BillingAddress.City == "Paris").Select(c => c.Name)));
        Assert.Equal(["Bob"], Both(q => q.Where(c => c.ShippingAddress.City == "Paris").Select(c => c.Name)));
        Assert.Equal(["Ann"], Both(q => q.Where(c => c.ShippingAddress.Location.Lat > 8.5).Select(c => c.Name)));
        Assert.Equal(["Bob", "Ann"], Both(q => q.OrderBy(c => c.ShippingAddress.Location.Lat).Select(c => c.Name)));
        var both = Both(q => q.OrderBy(c => c.Name).Select(c => new { B = c.BillingAddress.City, S = c.ShippingAddress.City, BL = c.BillingAddress.Location.Lat, SL = c.ShippingAddress.Location.Lat }));
        Assert.Equal([new { B = "Paris", S = "Rome", BL = 1.0, SL = 9.0 }, new { B = "Rome", S = "Paris", BL = 2.0, SL = 8.0 }], both);

        // Stored names, not CLR names, appear in the pipeline.
        var mql = LoggedMql(collection, q => q.Where(c => c.BillingAddress.City == "Paris" && c.ShippingAddress.Location.Lat > 1)
            .Select(c => c.Name), Configure);
        Assert.Contains("bill.billCity", mql);
        Assert.Contains("ship.loc.Lat", mql);
        Assert.DoesNotContain("BillingAddress", mql);
        Assert.DoesNotContain("ShippingAddress", mql);

        static BsonDocument Two(string name, BsonDocument bill, BsonDocument ship)
        {
            bill["billCity"] = bill["City"];
            bill.Remove("City");
            ship["loc"] = ship["Location"];
            ship.Remove("Location");
            return new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", name }, { "bill", bill }, { "ship", ship } };
        }
    }

    [Fact]
    public void Camel_case_convention_element_names()
    {
        var collection = database.CreateCollection<RichCustomer>(UniqueName(nameof(Camel_case_convention_element_names)));
        database.GetCollection<BsonDocument>(collection.CollectionNamespace).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "name", "Ann" }, { "rank", 1 },
                { "address", Camel(Address("Paris", 1.5, 10, 2, true, Grade.High, 5)) }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "name", "Bob" }, { "rank", 2 },
                { "address", Camel(Address("Rome", 0.5, 20, 3, false, Grade.Low, null)) }
            }
        ]);

        static void Conventions(ModelConfigurationBuilder cb) => cb.Conventions.Add(_ => new CamelCaseElementNameConvention());

        List<T> Both<T>(Func<IQueryable<RichCustomer>, IEnumerable<T>> q)
            => NativeModeAssert.NativeAndParity(m => Run(collection, m, q, ConfigureRich, Conventions));

        Assert.Equal(["Ann"], Both(q => q.Where(c => c.Address.City == "Paris").Select(c => c.Name)));
        Assert.Equal(["Bob", "Ann"], Both(q => q.OrderBy(c => c.Address.Location.Lat).Select(c => c.Name)));
        Assert.Equal([(20.0, "Rome")], Both(q => q.Where(c => c.Address.Location.Lon > 15).Select(c => new { c.Address.Location.Lon, c.Address.City }).ToList().Select(x => (x.Lon, x.City))));

        var mql = LoggedMql(collection, q => q.Where(c => c.Address.Location.Lat > 1).Select(c => c.Address.City), ConfigureRich, Conventions);
        Assert.Contains("address.location.lat", mql);
        Assert.Contains("$address.city", mql);

        static BsonDocument Camel(BsonDocument doc)
        {
            var result = new BsonDocument();
            foreach (var element in doc)
            {
                var value = element.Value is BsonDocument nested ? Camel(nested) : element.Value;
                result.Add(char.ToLowerInvariant(element.Name[0]) + element.Name[1..], value);
            }

            return result;
        }
    }

    [Fact]
    public void HasElementName_on_complex_property_and_leaf()
    {
        var collection = database.CreateCollection<RichCustomer>(UniqueName(nameof(HasElementName_on_complex_property_and_leaf)));
        database.GetCollection<BsonDocument>(collection.CollectionNamespace).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Ann" }, { "Rank", 1 }, { "addr", Renamed(Address("Paris", 1.5, 10, 2, true, Grade.High, 5)) } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Bob" }, { "Rank", 2 }, { "addr", Renamed(Address("Rome", 0.5, 20, 3, false, Grade.Low, null)) } },
            // Decoy: a document that only matches if the CLR names were used.
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Decoy" }, { "Rank", 3 },
                { "addr", Renamed(Address("Oslo", 5, 5, 5, false, Grade.Low, null)) },
                { "Address", new BsonDocument { { "City", "Paris" }, { "Location", new BsonDocument { { "Lat", 99.0 } } } } }
            }
        ]);

        static void Configure(ModelBuilder mb)
            => mb.Entity<RichCustomer>().ComplexProperty(c => c.Address, a =>
            {
                a.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "addr");
                a.Property(x => x.City).Metadata.SetElementName("town");
                a.ComplexProperty(x => x.Location).HasPropertyAnnotation(MongoAnnotationNames.ElementName, "geo");
            });

        List<T> Both<T>(Func<IQueryable<RichCustomer>, IEnumerable<T>> q)
            => NativeModeAssert.NativeAndParity(m => Run(collection, m, q, Configure));

        Assert.Equal(["Ann"], Both(q => q.Where(c => c.Address.City == "Paris").Select(c => c.Name)));
        Assert.Equal(["Decoy"], Both(q => q.Where(c => c.Address.Location.Lat > 4).Select(c => c.Name)));
        Assert.Equal(["Paris", "Rome", "Oslo"], Both(q => q.OrderBy(c => c.Rank).Select(c => c.Address.City)));
        Assert.Equal([5.0], Both(q => new[] { q.Max(c => c.Address.Location.Lat) }));

        var mql = LoggedMql(collection, q => q.Where(c => c.Address.City == "Paris").OrderBy(c => c.Address.Location.Lat).Select(c => c.Name), Configure);
        Assert.Contains("addr.town", mql);
        Assert.Contains("addr.geo.Lat", mql);

        static BsonDocument Renamed(BsonDocument address)
        {
            address["town"] = address["City"];
            address.Remove("City");
            address["geo"] = address["Location"];
            address.Remove("Location");
            return address;
        }
    }

    [Fact]
    public void Emitted_mql_uses_dotted_paths_for_central_shapes()
    {
        var collection = SeedRich();

        Assert.Contains("\"Address.City\" : \"Paris\"", LoggedMql(collection, q => q.Where(c => c.Address.City == "Paris").Select(c => c.Name), ConfigureRich));
        Assert.Contains("\"Address.Location.Lat\" : { \"$gt\" : 1", LoggedMql(collection, q => q.Where(c => c.Address.Location.Lat > 1).Select(c => c.Name), ConfigureRich));
        Assert.Contains("\"Address.Location.Lat\" : -1", LoggedMql(collection, q => q.OrderByDescending(c => c.Address.Location.Lat).Select(c => c.Name), ConfigureRich));
        Assert.Contains("\"$Address.Location.Lon\"", LoggedMql(collection, q => q.Select(c => new { c.Name, c.Address.Location.Lon }), ConfigureRich));
        Assert.Contains("\"$Address.City\"", LoggedMql(collection, q => q.GroupBy(c => c.Address.City).Select(g => new { g.Key, C = g.Count() }), ConfigureRich));
        // A bare dotted leaf is staged under the driver's own bare alias (NativeProjectionBinder gate 1f).
        Assert.Contains("\"_v\" : \"$Address.Location.Lat\"", LoggedMql(collection, q => q.Select(c => c.Address.Location.Lat), ConfigureRich));
    }

    // ── Value-converted and BsonRepresentation leaves (stored-alike rule) ────────────────────────────────────

    private static void ConfigureConverted(ModelBuilder mb)
        => mb.Entity<ConvertedCustomer>().ComplexProperty(c => c.Box, b =>
        {
            b.Property(x => x.Count).HasConversion<string>();
            b.Property(x => x.Code).Metadata.SetBsonRepresentation(BsonType.String, null, null);
        });

    private IMongoCollection<ConvertedCustomer> SeedConverted(string name)
    {
        var collection = database.CreateCollection<ConvertedCustomer>(UniqueName(name));
        database.GetCollection<BsonDocument>(collection.CollectionNamespace).InsertMany(
        [
            Box("Ann", "3", "10", "x"),
            Box("Bob", "12", "2", "y"),
            Box("Cid", "3", "7", "z")
        ]);
        return collection;

        static BsonDocument Box(string name, string count, string code, string label)
            => new()
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", name },
                { "Box", new BsonDocument { { "Count", count }, { "Code", code }, { "Label", label } } }
            };
    }

    [Fact]
    public void Value_converted_and_represented_complex_leaves_in_equality_predicates()
    {
        var collection = SeedConverted(nameof(Value_converted_and_represented_complex_leaves_in_equality_predicates));

        List<T> Both<T>(Func<IQueryable<ConvertedCustomer>, IEnumerable<T>> q)
            => NativeModeAssert.NativeAndParity(m => Run(collection, m, q, ConfigureConverted));

        Assert.Equal(["Ann", "Cid"], Both(q => q.Where(c => c.Box.Count == 3).OrderBy(c => c.Name).Select(c => c.Name)));
        Assert.Equal(["Bob"], Both(q => q.Where(c => c.Box.Code == 2).Select(c => c.Name)));
        // Projecting a converted/represented dotted leaf declines (NativeProjectionBinder.TryTranslateLeaf's
        // non-default-serialized dotted-leaf guard, shared with owned hops); the fallback reads it through the converter.
        Assert.Equal([3, 12, 3], NativeModeAssert.DeclinesCleanly(m => Run(collection, m, q => q.OrderBy(c => c.Name).Select(c => c.Box.Count), ConfigureConverted)));
        Assert.Equal([10, 2, 7], NativeModeAssert.DeclinesCleanly(m => Run(collection, m,
            q => q.OrderBy(c => c.Name).Select(c => new { c.Box.Code }).ToList().Select(x => x.Code), ConfigureConverted)));
        // A default-serialized sibling leaf of the same complex property still projects natively.
        Assert.Equal(["x", "y", "z"], Both(q => q.OrderBy(c => c.Name).Select(c => c.Box.Label)));

        var mql = LoggedMql(collection, q => q.Where(c => c.Box.Count == 3 && c.Box.Code == 2).Select(c => c.Name), ConfigureConverted);
        Assert.Contains("\"Box.Count\" : \"3\"", mql);
        Assert.Contains("\"Box.Code\" : \"2\"", mql);
    }

    [Fact]
    public void Value_converted_complex_leaf_relational_and_sort_decline_natively()
    {
        // Relational comparison and ordering of a converted / string-represented leaf run on the stored (string)
        // form: "12" < "3". The native path declines (StoredOrdering) and the driver-LINQ bridge refuses (EF-337),
        // exactly as for a root property.
        var collection = SeedConverted(nameof(Value_converted_complex_leaf_relational_and_sort_decline_natively));

        foreach (var query in new Func<IQueryable<ConvertedCustomer>, IEnumerable<string>>[]
                 {
                     q => q.Where(c => c.Box.Count > 5).Select(c => c.Name),
                     q => q.Where(c => c.Box.Code < 5).Select(c => c.Name),
                     q => q.OrderBy(c => c.Box.Count).Select(c => c.Name),
                     q => q.Where(c => EF.Property<int>(c.Box, "Count") > 5).Select(c => c.Name),
                     q => q.Select(c => c.Box.Code).OrderBy(x => x).Select(x => x.ToString()),
                     q => new[] { q.Max(c => c.Box.Count).ToString() },
                     q => new[] { q.Select(c => c.Box.Count).Max().ToString() },
                     q => new[] { q.Select(c => new { V = c.Box.Count }).Max(x => x.V).ToString() }
                 })
        {
            Assert.Throws<NativeTranslationNotSupportedException>(
                () => Run(collection, MongoQueryMode.NativeOnly, query, ConfigureConverted));
            var driver = Assert.ThrowsAny<Exception>(() => Run(collection, MongoQueryMode.DriverLinq, query, ConfigureConverted));
            Assert.Contains("stored through a value converter or a BsonRepresentation", driver.ToString());
            var native = Assert.ThrowsAny<Exception>(() => Run(collection, MongoQueryMode.Native, query, ConfigureConverted));
            Assert.Contains("stored through a value converter or a BsonRepresentation", native.ToString());
        }
    }

    // ── R-b: nested struct leaf beside a client-mapped ToUpper ──────────────────────────────────────────────────

    [Fact]
    public void Nested_struct_leaf_with_ToUpper_projection_matches_in_all_three_modes()
    {
        // Before this slice driver-LINQ threw "Document element 'Lat' is missing but required" here (the shared
        // projection shaper read the complex leaf by alias, without its IProperty); native was right. Resolving complex
        // hops in MongoProjectionBindingRemovingExpressionVisitor.TryResolveFieldAccessSource fixed both paths, so this is
        // an ordinary three-mode parity row against a hand-written answer.
        var collection = SeedRich();
        Func<IQueryable<RichCustomer>, IEnumerable<(string, double)>> query = q => q.OrderBy(c => c.Name)
            .Select(c => new { Up = c.Address.City.ToUpper(), c.Address.Location.Lat }).ToList().Select(x => (x.Up, x.Lat));

        NativeModeAssert.NativeAndExpected(m => Rich(collection, m, query),
            [("PARIS", 1.5), ("LONDON", 0.5), ("PARIS", 2.5), ("BERLIN", 3.5), ("AMSTERDAM", 1.0)]);
    }

#if !EF8 && !EF9
    // ── R-a: optional complex property whose parent element is MISSING or BSON null (EF10) ───────────────────────

    private static void ConfigureOptional(ModelBuilder mb)
        => mb.Entity<CustomerWithOptionalAddress>().ComplexProperty(c => c.Address, a => a.ComplexProperty(x => x.Location));

    private IMongoCollection<CustomerWithOptionalAddress> SeedOptional(string name)
    {
        var collection = database.CreateCollection<CustomerWithOptionalAddress>(UniqueName(name));
        database.GetCollection<BsonDocument>(collection.CollectionNamespace).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Missing" } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Null" }, { "Address", BsonNull.Value } },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "A" },
                { "Address", new BsonDocument { { "Street", "s" }, { "City", "a" }, { "Location", new BsonDocument { { "Lat", 1.0 }, { "Lon", 2.0 } } } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "D" },
                { "Address", new BsonDocument { { "Street", "s" }, { "City", "d" }, { "Location", new BsonDocument { { "Lat", 3.0 }, { "Lon", 4.0 } } } } }
            }
        ]);
        return collection;
    }

    private List<T> Optional<T>(IMongoCollection<CustomerWithOptionalAddress> collection, MongoQueryMode mode,
        Func<IQueryable<CustomerWithOptionalAddress>, IEnumerable<T>> query)
        => Run(collection, mode, query, ConfigureOptional);

    // Rows: Missing (no Address element), Null (Address: null), A (City "a", Lat 1), D (City "d", Lat 3). Each shape runs
    // under NativeOnly and DriverLinq and must agree on outcome (value or throw) and rows; the hand-written answer
    // guards against a vacuous pass (both modes throwing, or both reading nothing).
    public static readonly Dictionary<string, (Func<IQueryable<CustomerWithOptionalAddress>, IEnumerable<string>> Run, string[] Expected)>
        OptionalParentShapes = new()
        {
            // IsFoldSafe admits the fold to {Address.City: {$lt: "c"}} over the non-nullable City. In-memory C# would also
            // answer Missing and Null (string.Compare(null, "c") is -1), but the oracle is driver-LINQ, which answers [A]
            // like the fold: server-side, no element is $lt "c" when the parent is missing or null. Native matches.
            ["compare_lt"] = (q => q.Where(c => string.Compare(c.Address!.City, "c") < 0).OrderBy(c => c.Name).Select(c => c.Name), ["A"]),
            ["compare_gt"] = (q => q.Where(c => string.Compare(c.Address!.City, "c") > 0).OrderBy(c => c.Name).Select(c => c.Name), ["D"]),
            ["anon_lat"] = (q => q.OrderBy(c => c.Name).Select(c => new { c.Name, c.Address!.Location.Lat }).ToList().Select(x => x.Name + ":" + x.Lat),
                ["A:1", "D:3", "Missing:0", "Null:0"]),
            ["bare_lat"] = (q => q.OrderBy(c => c.Name).Select(c => c.Address!.Location.Lat).ToList().Select(x => x.ToString()), ["1", "3", "0", "0"]),
            ["bare_city"] = (q => q.OrderBy(c => c.Name).Select(c => c.Address!.City).ToList().Select(x => x ?? "<null>"), ["a", "d", "<null>", "<null>"]),
            ["distinct_anon_city"] = (q => q.Select(c => new { c.Address!.City }).Distinct().ToList().Select(x => x.City ?? "<null>").OrderBy(x => x),
                ["<null>", "a", "d"]),
            ["distinct_bare_city"] = (q => q.Select(c => c.Address!.City).Distinct().ToList().Select(x => x ?? "<null>").OrderBy(x => x),
                ["<null>", "a", "d"]),
            ["distinct_lat"] = (q => q.Select(c => c.Address!.Location.Lat).Distinct().ToList().Select(x => x.ToString()).OrderBy(x => x), ["0", "1", "3"]),
            ["sum_lat"] = (q => new[] { q.Sum(c => c.Address!.Location.Lat).ToString() }, ["4"]),
            ["max_lat"] = (q => new[] { q.Max(c => c.Address!.Location.Lat).ToString() }, ["3"]),
            ["min_lat"] = (q => new[] { q.Min(c => c.Address!.Location.Lat).ToString() }, ["0"]),
            ["avg_lat"] = (q => new[] { q.Average(c => c.Address!.Location.Lat).ToString() }, ["2"]),
            ["max_city"] = (q => new[] { q.Max(c => c.Address!.City) ?? "<null>" }, ["d"]),
            ["orderby_city"] = (q => q.OrderBy(c => c.Address!.City).ThenBy(c => c.Name).Select(c => c.Name), ["Missing", "Null", "A", "D"]),
            ["orderbydesc_lat"] = (q => q.OrderByDescending(c => c.Address!.Location.Lat).ThenBy(c => c.Name).Select(c => c.Name), ["D", "A", "Missing", "Null"]),
            ["where_lat_lt"] = (q => q.Where(c => c.Address!.Location.Lat < 2).OrderBy(c => c.Name).Select(c => c.Name), ["A"]),
            ["where_not_lat_gt"] = (q => q.Where(c => !(c.Address!.Location.Lat > 2)).OrderBy(c => c.Name).Select(c => c.Name), ["A", "Missing", "Null"]),
            ["where_city_null"] = (q => q.Where(c => c.Address!.City == null).OrderBy(c => c.Name).Select(c => c.Name), ["Missing", "Null"]),
            ["where_city_ne"] = (q => q.Where(c => c.Address!.City != "a").OrderBy(c => c.Name).Select(c => c.Name), ["D", "Missing", "Null"]),
            ["groupby_city"] = (q => q.GroupBy(c => c.Address!.City).Select(g => new { g.Key, C = g.Count() }).ToList()
                .Select(x => (x.Key ?? "<null>") + ":" + x.C).OrderBy(x => x), ["<null>:2", "a:1", "d:1"]),
            // EF.Property spellings of the hop (MongoProjectionBindingExpressionVisitor binds a complex leaf whole; the read
            // side's TryResolveFieldAccessSource EF.Property arm resolves the complex hop). Before this slice the nested
            // spelling went native and returned NULL ROWS (the binder folded the selector to default(T)).
            ["efprop_hop_anon"] = (q => q.OrderBy(c => c.Name)
                .Select(c => new { c.Name, City = EF.Property<string>(EF.Property<ComplexAddress>(c, "Address"), "City") }).ToList()
                .Select(x => x.Name + ":" + (x.City ?? "<null>")),
                ["A:a", "D:d", "Missing:<null>", "Null:<null>"]),
            ["efprop_hop_bare_lat"] = (q => q.OrderBy(c => c.Name)
                .Select(c => new { EF.Property<ComplexAddress>(c, "Address").Location.Lat }).ToList().Select(x => x.Lat.ToString()),
                ["1", "3", "0", "0"]),
            ["count_lat_lt"] = (q => new[] { q.Count(c => c.Address!.Location.Lat < 2).ToString() }, ["1"]),
        };

    [Fact]
    public void Optional_parent_EF_Property_leaf_over_a_member_hop_declines_cleanly()
    {
        // `EF.Property<double>(c.Address!.Location, "Lat")` has no native field resolution (the member-hop receiver under
        // EF.Property is not a chain TryResolveOwnedFieldPath accepts); it declines and the fallback reads default for the
        // missing/null parent. Task 10 decided to keep it declining (the decline is clean and the fallback right, and the
        // member spelling `c.Address!.Location.Lat` is native); widening the emit-side resolver is a follow-up.
        var collection = SeedOptional(nameof(Optional_parent_EF_Property_leaf_over_a_member_hop_declines_cleanly));
        Assert.Equal(["A:1", "D:3", "Missing:0", "Null:0"], NativeModeAssert.DeclinesCleanly(m => Optional(collection, m,
            q => q.OrderBy(c => c.Name).Select(c => new { c.Name, Lat = EF.Property<double>(c.Address!.Location, "Lat") }).ToList()
                .Select(x => x.Name + ":" + x.Lat))));
    }

    public static TheoryData<string> OptionalParentShapeNames => [.. OptionalParentShapes.Keys];

    [Theory]
    [MemberData(nameof(OptionalParentShapeNames))]
    public void Optional_parent_missing_or_null_matches_driver_linq(string shape)
    {
        var collection = SeedOptional(nameof(Optional_parent_missing_or_null_matches_driver_linq) + shape);
        var (run, expected) = OptionalParentShapes[shape];

        NativeModeAssert.NativeAndExpected(m => Optional(collection, m, run), [.. expected]);
    }
#endif

    [Fact]
    public void Entity_beside_an_EF_Property_complex_leaf()
    {
        // S2 (Task 9): with complex properties materialized (Task 10) the entity beside the leaf reads correctly in every
        // mode, in both spellings; it must never return null rows. The whole-entity matrix is in ComplexTypeMaterializationTests.
        var collection = SeedRich();
        NativeModeAssert.NativeAndExpected(m => Rich(collection, m, q => q.OrderBy(c => c.Name)
                .Select(c => new { c, City = EF.Property<string>(EF.Property<RichAddress>(c, "Address"), "City") }).ToList()
                .Select(x => x.c.Name + ":" + x.c.Address.Location.Lat + ":" + x.City)),
            ["Ann:1.5:Paris", "Bob:0.5:London", "Cid:2.5:Paris", "Dee:3.5:Berlin", "Eve:1:Amsterdam"]);
        NativeModeAssert.NativeAndExpected(m => Rich(collection, m, q => q.OrderBy(c => c.Name)
                .Select(c => new { c, c.Address.City }).ToList().Select(x => x.c.Address.City + ":" + x.City)),
            ["Paris:Paris", "London:London", "Paris:Paris", "Berlin:Berlin", "Amsterdam:Amsterdam"]);
    }

    private static string UniqueName(string name)
        => TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
}
