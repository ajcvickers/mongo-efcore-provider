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

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Robustness of complex types (Task 15): deep nesting, unusual element names, malformed stored BSON, one CLR type at
/// several paths, BSON attributes, and the leaf type matrix. Every query runs in all three modes against a hand-written
/// answer; stored shapes are asserted on the raw BSON.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeRobustnessTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private static readonly MongoQueryMode[] AllModes = [MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq];

    private IMongoCollection<T> Collection<T>([CallerMemberName] string name = "")
        => database.CreateCollection<T>(TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8]);

    private static IMongoCollection<BsonDocument> Raw<T>(IMongoCollection<T> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    private static SingleEntityDbContext<T> Context<T>(
        IMongoCollection<T> collection,
        Action<ModelBuilder> configure,
        MongoQueryMode mode = MongoQueryMode.Native,
        QueryTrackingBehavior tracking = QueryTrackingBehavior.TrackAll,
        Action<ModelConfigurationBuilder>? conventions = null)
        where T : class
        => SingleEntityDbContext.Create(collection, configure, conventions, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            b.UseQueryTrackingBehavior(tracking);
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    private static List<TResult> Query<T, TResult>(
        IMongoCollection<T> collection,
        Action<ModelBuilder> configure,
        MongoQueryMode mode,
        Func<IQueryable<T>, IEnumerable<TResult>> query,
        QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTracking,
        Action<ModelConfigurationBuilder>? conventions = null)
        where T : class
    {
        using var db = Context(collection, configure, mode, tracking, conventions);
        return query(db.Entities).ToList();
    }

    // Element order inside a subdocument is not semantic (the writer emits scalar leaves, then nested values).
    private static BsonValue Sorted(BsonValue value)
        => value switch
        {
            BsonDocument d => new BsonDocument(d.Elements.OrderBy(e => e.Name, StringComparer.Ordinal).Select(e => new BsonElement(e.Name, Sorted(e.Value)))),
            BsonArray a => new BsonArray(a.Select(Sorted)),
            _ => value
        };

    private static void AssertSameDocument(BsonValue expected, BsonValue actual)
        => Assert.Equal(Sorted(expected).ToJson(), Sorted(actual).ToJson());

    // ── (1) Deep nesting: six levels, struct in class in struct ───────────────────────────────────────────────────

    public class L6
    {
        public string Leaf { get; set; } = null!;
        public int Depth { get; set; }
    }

    public struct L5
    {
        public L6 Six { get; set; }
        public double Weight { get; set; }
    }

    public class L4
    {
        public L5 Five { get; set; }
        public string Tag { get; set; } = null!;
    }

    public struct L3
    {
        public L4 Four { get; set; }
        public int Code { get; set; }
    }

    public class L2
    {
        public L3 Three { get; set; }
        public string Name { get; set; } = null!;
    }

    public class L1
    {
        public L2 Two { get; set; } = null!;
        public int Rank { get; set; }
    }

    public class DeepRoot
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public L1 One { get; set; } = null!;
    }

    private static void ConfigureDeep(ModelBuilder mb)
        => mb.Entity<DeepRoot>().ComplexProperty(r => r.One, one =>
        {
            one.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "o");
            one.ComplexProperty(x => x.Two, two =>
                two.ComplexProperty(x => x.Three, three =>
                    three.ComplexProperty(x => x.Four, four =>
                        four.ComplexProperty(x => x.Five, five =>
                            five.ComplexProperty(x => x.Six, six => six.Property(x => x.Leaf).Metadata.SetElementName("lf"))))));
        });

    private static DeepRoot Deep(string name, string leaf, int depth, int rank)
        => new()
        {
            Name = name,
            One = new L1
            {
                Rank = rank,
                Two = new L2
                {
                    Name = name + "-2",
                    Three = new L3
                    {
                        Code = depth * 10,
                        Four = new L4
                        {
                            Tag = name + "-4",
                            Five = new L5 { Weight = depth + 0.5, Six = new L6 { Leaf = leaf, Depth = depth } }
                        }
                    }
                }
            }
        };

    private static string FmtDeep(DeepRoot r)
        => $"{r.Name}|{r.One.Rank}|{r.One.Two.Name}|{r.One.Two.Three.Code}|{r.One.Two.Three.Four.Tag}|"
           + $"{r.One.Two.Three.Four.Five.Weight}|{r.One.Two.Three.Four.Five.Six.Leaf}|{r.One.Two.Three.Four.Five.Six.Depth}";

    private IMongoCollection<DeepRoot> SeedDeep([CallerMemberName] string name = "")
    {
        var collection = Collection<DeepRoot>(name);
        using var db = Context(collection, ConfigureDeep);
        db.Entities.AddRange(Deep("a", "zeta", 3, 1), Deep("b", "alpha", 1, 2), Deep("c", "mid", 2, 3));
        db.SaveChanges();
        return collection;
    }

    [Fact]
    public void Six_level_nesting_is_written_at_the_stored_path()
    {
        var collection = SeedDeep();
        var a = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "a")).Single();

        Assert.Equal(["_id", "Name", "o"], a.Names.ToArray());
        var six = a["o"]["Two"]["Three"]["Four"]["Five"]["Six"].AsBsonDocument;
        Assert.Equal(new BsonDocument { { "Depth", 3 }, { "lf", "zeta" } }, six);
        Assert.Equal(new BsonDouble(3.5), a["o"]["Two"]["Three"]["Four"]["Five"]["Weight"]);
        Assert.Equal(new BsonInt32(30), a["o"]["Two"]["Three"]["Code"]);
    }

    public static TheoryData<QueryTrackingBehavior> TrackingBehaviors
        => [QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking, QueryTrackingBehavior.NoTrackingWithIdentityResolution];

    [Theory]
    [MemberData(nameof(TrackingBehaviors))]
    public void Six_level_nesting_reads_back_under_every_tracking_behavior(QueryTrackingBehavior tracking)
    {
        var collection = SeedDeep(nameof(Six_level_nesting_reads_back_under_every_tracking_behavior) + tracking);

        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureDeep, m, q => q.OrderBy(r => r.Name).ToList().Select(FmtDeep), tracking),
            ["a|1|a-2|30|a-4|3.5|zeta|3", "b|2|b-2|10|b-4|1.5|alpha|1", "c|3|c-2|20|c-4|2.5|mid|2"]);
    }

    [Fact]
    public void Deepest_leaf_in_predicate_ordering_and_projection()
    {
        var collection = SeedDeep();

        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureDeep, m, q => q
                .Where(r => r.One.Two.Three.Four.Five.Six.Depth >= 2)
                .OrderBy(r => r.One.Two.Three.Four.Five.Six.Leaf)
                .Select(r => r.Name + ":" + r.One.Two.Three.Four.Five.Six.Leaf)),
            ["c:mid", "a:zeta"]);

        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureDeep, m, q => q
                .OrderByDescending(r => r.One.Two.Three.Four.Five.Weight)
                .Select(r => new { r.One.Two.Three.Four.Five.Six.Depth, r.One.Two.Three.Code })
                .ToList().Select(x => $"{x.Depth}/{x.Code}")),
            ["3/30", "2/20", "1/10"]);

        // A whole value at depth, projected bare.
        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureDeep, m, q => q.OrderBy(r => r.Name)
                .Select(r => r.One.Two.Three.Four.Five.Six).ToList().Select(s => s.Leaf + s.Depth)),
            ["zeta3", "alpha1", "mid2"]);

        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureDeep, m, q => new[] { q.Max(r => r.One.Two.Three.Four.Five.Six.Depth) }),
            [3]);
    }

    [Fact]
    public void Updating_the_deepest_leaf_rewrites_the_whole_top_level_subdocument()
    {
        var collection = SeedDeep();
        using var capture = new CommandCapture(database);
        var captured = capture.Collection(collection);

        using (var db = Context(captured, ConfigureDeep, MongoQueryMode.NativeOnly))
        {
            var a = db.Entities.Single(r => r.Name == "a");
            capture.Clear();
            var five = a.One.Two.Three.Four.Five;
            five.Six.Leaf = "omega";
            Assert.Equal(1, db.SaveChanges());
        }

        // Ruling R5: the whole top-level complex value is rewritten, never a dotted partial $set.
        var set = capture.SingleSet();
        Assert.Equal(["_id", "o"], set.Names.ToArray());
        Assert.Equal("omega", set["o"]["Two"]["Three"]["Four"]["Five"]["Six"]["lf"].AsString);

        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureDeep, m, q => q.OrderBy(r => r.Name).ToList().Select(FmtDeep)),
            ["a|1|a-2|30|a-4|3.5|omega|3", "b|2|b-2|10|b-4|1.5|alpha|1", "c|3|c-2|20|c-4|2.5|mid|2"]);
    }

#if !EF8
    [Fact]
    public void Bulk_set_of_the_deepest_leaf_writes_the_dotted_stored_path()
    {
        var collection = SeedDeep();
        var before = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["Name"].AsString);

        using (var db = Context(collection, ConfigureDeep))
        {
            Assert.Equal(2, db.Entities.Where(r => r.One.Two.Three.Four.Five.Six.Depth < 3)
                .ExecuteUpdate(s => s.SetProperty(r => r.One.Two.Three.Four.Five.Six.Leaf, "bulk")));
        }

        var after = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["Name"].AsString);
        foreach (var (name, document) in before)
        {
            if (name != "a")
            {
                document["o"]["Two"]["Three"]["Four"]["Five"]["Six"]["lf"] = "bulk";
            }

            Assert.Equal(document, after[name]);
        }
    }
#endif

    // ── (2) Element names: long, unicode, reserved-looking ───────────────────────────────────────────────────────

    public class NamedLeaves
    {
        public string Long { get; set; } = null!;
        public string Unicode { get; set; } = null!;
        public string Underscore { get; set; } = null!;
        public string Typed { get; set; } = null!;
        public int Spaced { get; set; }
    }

    public class NamedHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public NamedLeaves Value { get; set; } = null!;
    }

    private static readonly string LongName = new('n', 300);
    private const string UnicodeName = "名前_ñ_é_🚀";

    private static void ConfigureNamed(ModelBuilder mb)
        => mb.Entity<NamedHolder>().ComplexProperty(h => h.Value, v =>
        {
            v.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "värde ✓");
            v.Property(x => x.Long).Metadata.SetElementName(LongName);
            v.Property(x => x.Unicode).Metadata.SetElementName(UnicodeName);
            // Reserved-looking names are legal INSIDE a subdocument: `_id` and `_t` only mean something at the root.
            v.Property(x => x.Underscore).Metadata.SetElementName("_id");
            v.Property(x => x.Typed).Metadata.SetElementName("_t");
            v.Property(x => x.Spaced).Metadata.SetElementName("with space-and:colon");
        });

    [Fact]
    public void Long_unicode_and_reserved_looking_leaf_names_round_trip_and_query()
    {
        var collection = Collection<NamedHolder>();
        using (var db = Context(collection, ConfigureNamed))
        {
            db.Entities.AddRange(
                new NamedHolder { Name = "a", Value = new NamedLeaves { Long = "L1", Unicode = "ü", Underscore = "u1", Typed = "t1", Spaced = 1 } },
                new NamedHolder { Name = "b", Value = new NamedLeaves { Long = "L2", Unicode = "ß", Underscore = "u2", Typed = "t2", Spaced = 2 } });
            db.SaveChanges();
        }

        var stored = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "a")).Single();
        Assert.Equal(["_id", "Name", "värde ✓"], stored.Names.ToArray());
        Assert.Equal(
            new[] { "_id", "_t", LongName, "with space-and:colon", UnicodeName }.OrderBy(n => n, StringComparer.Ordinal),
            stored["värde ✓"].AsBsonDocument.Names.OrderBy(n => n, StringComparer.Ordinal));
        Assert.Equal("u1", stored["värde ✓"]["_id"].AsString);
        Assert.Equal("t1", stored["värde ✓"]["_t"].AsString);

        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureNamed, m, q => q.OrderBy(h => h.Name).ToList()
                .Select(h => $"{h.Value.Long}|{h.Value.Unicode}|{h.Value.Underscore}|{h.Value.Typed}|{h.Value.Spaced}")),
            ["L1|ü|u1|t1|1", "L2|ß|u2|t2|2"]);
        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureNamed, m, q => q
                .Where(h => h.Value.Underscore == "u2" || h.Value.Typed == "t1" && h.Value.Spaced > 0)
                .OrderByDescending(h => h.Value.Unicode)
                .Select(h => h.Value.Long + "/" + h.Value.Underscore)),
            ["L1/u1", "L2/u2"]);
        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureNamed, m, q => q.Where(h => h.Value.Long == "L2")
                .Select(h => new { h.Value.Underscore, h.Value.Typed, h.Value.Unicode }).ToList()
                .Select(x => x.Underscore + x.Typed + x.Unicode)),
            ["u2t2ß"]);
    }

    public class TaggedValue
    {
        public string Kind { get; set; } = null!;
    }

    public class TphBase
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public TaggedValue Value { get; set; } = null!;
    }

    public class TphDerived : TphBase
    {
        public int Extra { get; set; }
    }

    [Fact]
    public void Leaf_named_like_the_discriminator_inside_a_complex_value_does_not_collide_with_TPH()
    {
        var collection = Collection<TphBase>();

        static void Configure(ModelBuilder mb)
        {
            mb.Entity<TphBase>().ComplexProperty(e => e.Value, v => v.Property(x => x.Kind).Metadata.SetElementName("_t"));
            mb.Entity<TphDerived>();
        }

        using (var db = Context(collection, Configure))
        {
            db.Entities.Add(new TphBase { Name = "base", Value = new TaggedValue { Kind = "TphDerived" } });
            db.Entities.Add(new TphDerived { Name = "derived", Extra = 7, Value = new TaggedValue { Kind = "TphBase" } });
            db.SaveChanges();
        }

        var stored = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().OrderBy(d => d["Name"].AsString).ToList();
        Assert.Equal("TphBase", stored[0]["_t"].AsString);
        Assert.Equal("TphDerived", stored[0]["Value"]["_t"].AsString);
        Assert.Equal("TphDerived", stored[1]["_t"].AsString);
        Assert.Equal("TphBase", stored[1]["Value"]["_t"].AsString);

        NativeModeAssert.NativeAndExpected(
            m => Query(collection, Configure, m, q => q.OrderBy(e => e.Name).ToList().Select(e => $"{e.GetType().Name}|{e.Value.Kind}")),
            ["TphBase|TphDerived", "TphDerived|TphBase"]);
        NativeModeAssert.NativeAndExpected(
            m => Query(collection, Configure, m, q => q.OfType<TphDerived>().Select(e => e.Name + "|" + e.Value.Kind)),
            ["derived|TphBase"]);
        NativeModeAssert.NativeAndExpected(
            m => Query(collection, Configure, m, q => q.Where(e => e.Value.Kind == "TphDerived").Select(e => e.Name)),
            ["base"]);
    }

    [Theory]
    [InlineData("$leaf", "starts with the reserved character '$'")]
    [InlineData("a.b", "contains the reserved character '.'")]
    public void Reserved_characters_in_a_leaf_name_inside_a_complex_value_fail_model_validation(string name, string message)
    {
        var collection = Collection<NamedHolder>(nameof(Reserved_characters_in_a_leaf_name_inside_a_complex_value_fail_model_validation) + name.Length);
        using var db = Context(collection, mb => mb.Entity<NamedHolder>().ComplexProperty(h => h.Value, v => v.Property(x => x.Long).Metadata.SetElementName(name)));

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{name}'", ex.Message);
        Assert.Contains(message, ex.Message);
    }

    // ── (3) Malformed stored BSON, DOM and one-pass ───────────────────────────────────────────────────────────────

    public class MGeo
    {
        public double Lat { get; set; }
        public double? Alt { get; set; }
    }

    public class MValue
    {
        public string City { get; set; } = null!;
        public string? Note { get; set; }
        public int Floor { get; set; }
        public MGeo Geo { get; set; } = null!;
    }

    public class MRoot
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public MValue Value { get; set; } = null!;
    }

    private static void ConfigureMalformed(ModelBuilder mb)
        => mb.Entity<MRoot>().ComplexProperty(r => r.Value, v => v.ComplexProperty(x => x.Geo));

    private static string FmtM(MRoot r)
        => $"{r.Name}|{r.Value.City}|{r.Value.Note ?? "-"}|{r.Value.Floor}|{r.Value.Geo.Lat}|{r.Value.Geo.Alt?.ToString() ?? "-"}";

    // Each read is run so that NativeOnly ToList takes the one-pass streaming materializer (the entity is streaming
    // eligible), First takes the native DOM shaper, and DriverLinq takes the driver DOM path.
    private static IEnumerable<(string Read, MongoQueryMode Mode, Func<IMongoCollection<MRoot>, List<string>> Run)> MalformedReads()
    {
        foreach (var mode in AllModes)
        {
            yield return ("ToList", mode, c => Query(c, ConfigureMalformed, mode, q => q.OrderBy(r => r.Name).ToList().Select(FmtM)));
            yield return ("Tracked", mode, c => Query(c, ConfigureMalformed, mode, q => q.OrderBy(r => r.Name).ToList().Select(FmtM), QueryTrackingBehavior.TrackAll));
            yield return ("First", mode, c => Query(c, ConfigureMalformed, mode, q => new[] { FmtM(q.OrderBy(r => r.Name).First()) }));
            yield return ("Value", mode, c => Query(c, ConfigureMalformed, mode, q => q.OrderBy(r => r.Name).Select(r => r.Value).ToList()
                .Select(v => $"{v.City}|{v.Note ?? "-"}|{v.Floor}|{v.Geo.Lat}|{v.Geo.Alt?.ToString() ?? "-"}")));
        }
    }

    private IMongoCollection<MRoot> SeedRaw(string name, params BsonDocument[] documents)
    {
        var collection = Collection<MRoot>(name);
        Raw(collection).InsertMany(documents);
        return collection;
    }

    [Fact]
    public void Permuted_order_and_unknown_elements_at_every_depth_are_tolerated()
    {
        var collection = SeedRaw(nameof(Permuted_order_and_unknown_elements_at_every_depth_are_tolerated),
            new BsonDocument
            {
                { "Value", new BsonDocument
                    {
                        { "zz", new BsonArray { 1, 2 } },
                        { "Geo", new BsonDocument { { "x", "y" }, { "Alt", 9.5 }, { "Lat", 1.25 }, { "deep", new BsonDocument("q", 1) } } },
                        { "Floor", 4 }, { "unknown", BsonNull.Value }, { "City", "Oslo" }
                    }
                },
                { "extraRoot", 1 }, { "Name", "a" }, { "_id", ObjectId.GenerateNewId() }
            });

        foreach (var (read, mode, run) in MalformedReads())
        {
            var expected = read == "Value" ? "Oslo|-|4|1.25|9.5" : "a|Oslo|-|4|1.25|9.5";
            Assert.True(run(collection).SequenceEqual([expected]), $"{read} [{mode}]");
        }

        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureMalformed, m, q => q.Where(r => r.Value.Geo.Alt > 9).Select(r => r.Value.City + r.Value.Geo.Lat)),
            ["Oslo1.25"]);
    }

    [Fact]
    public void Duplicate_elements_in_a_stored_subdocument_fail_loudly_in_every_path_like_a_duplicate_root_element()
    {
        // BsonDocument refuses duplicate names unless AllowDuplicateNames is set; the server stores them as given.
        var value = new BsonDocument { AllowDuplicateNames = true };
        value.Add("City", "first");
        value.Add("Floor", 1);
        value.Add("City", "second");
        value.Add("Geo", new BsonDocument { { "Lat", 1.0 } });
        var collection = SeedRaw(nameof(Duplicate_elements_in_a_stored_subdocument_fail_loudly_in_every_path_like_a_duplicate_root_element),
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Value", value } });

        // Measured: every materializer (one-pass, native DOM, driver DOM, whole-value projection) refuses the document
        // (the driver's BsonDocument reader rejects the duplicate), never reading the first or last one silently.
        var outcomes = MalformedReads().Select(r => $"{r.Read}[{r.Mode}]={Outcome(() => r.Run(collection))}").ToList();
        Assert.Equal(
            string.Join("\n", MalformedReads().Select(r => $"{r.Read}[{r.Mode}]=InvalidOperationException: Duplicate element name 'City'.")),
            string.Join("\n", outcomes));

        // Contrast (pre-existing, not complex-specific; characterization only): a duplicate ROOT scalar is read
        // inconsistently: the one-pass reader keeps the last value, a server-side projection the first, and the DOM
        // paths throw. The complex value is read whole into a BsonDocument, so every path refuses it.
        var root = new BsonDocument { AllowDuplicateNames = true };
        root.Add("_id", ObjectId.GenerateNewId());
        root.Add("Floor", 1);
        root.Add("Floor", 2);
        var scalars = Collection<ScalarRoot>();
        Raw(scalars).InsertOne(root);
        Assert.Equal(
        [
            "ToList[NativeOnly]=2", "First[NativeOnly]=InvalidOperationException: Duplicate element name 'Floor'.", "Proj[NativeOnly]=1",
            "ToList[Native]=2", "First[Native]=InvalidOperationException: Duplicate element name 'Floor'.", "Proj[Native]=1",
            "ToList[DriverLinq]=InvalidOperationException: Duplicate element name 'Floor'.",
            "First[DriverLinq]=InvalidOperationException: Duplicate element name 'Floor'.", "Proj[DriverLinq]=1"
        ], AllModes.SelectMany(mode => new[]
        {
            $"ToList[{mode}]=" + Outcome(() => Query(scalars, _ => { }, mode, q => q.ToList().Select(s => s.Floor.ToString()))),
            $"First[{mode}]=" + Outcome(() => Query(scalars, _ => { }, mode, q => new[] { q.First().Floor.ToString() })),
            $"Proj[{mode}]=" + Outcome(() => Query(scalars, _ => { }, mode, q => q.Select(s => s.Floor).ToList().Select(f => f.ToString())))
        }).ToList());
    }

    private static string Outcome(Func<List<string>> run)
    {
        try
        {
            return string.Join(";", run());
        }
        catch (Exception e)
        {
            return $"{e.GetType().Name}: {e.Message}";
        }
    }

    public static TheoryData<string> WrongTypeShapes => ["leaf", "nested-leaf", "nested-value"];

    [Theory]
    [MemberData(nameof(WrongTypeShapes))]
    public void Wrong_bson_type_throws_the_same_exception_type_and_message_as_a_wrong_typed_root_scalar(string shape)
    {
        var value = new BsonDocument { { "City", "c" }, { "Floor", 1 }, { "Geo", new BsonDocument("Lat", 1.0) } };
        switch (shape)
        {
            case "leaf":
                value["Floor"] = "not-an-int";
                break;
            case "nested-leaf":
                value["Geo"]["Lat"] = new BsonDocument("x", 1);
                break;
            case "nested-value":
                value["Geo"] = "not-a-document";
                break;
        }

        var collection = SeedRaw(nameof(Wrong_bson_type_throws_the_same_exception_type_and_message_as_a_wrong_typed_root_scalar) + shape,
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Value", value } });

        var expected = shape switch
        {
            "leaf" => RootScalarWrongTypeMessage("not-an-int"),
            "nested-leaf" => "Cannot deserialize a 'Double' from BsonType 'Document'",
            _ => "Cannot deserialize complex property 'MRoot.Value#MValue.Geo' from BsonType 'String'"
        };

        foreach (var (read, mode, run) in MalformedReads())
        {
            var ex = Assert.ThrowsAny<Exception>(() => run(collection));
            Assert.True(ex is FormatException, $"{read} [{mode}]: {ex.GetType().Name}: {ex.Message}");
            Assert.True(ex.Message.Contains(expected), $"{read} [{mode}]: '{ex.Message}' does not contain '{expected}'");
        }
    }

    public class ScalarRoot
    {
        public ObjectId Id { get; set; }
        public int Floor { get; set; }
    }

    // The message a root int scalar gives for the same wrong BSON value; the complex leaf must say the same thing.
    private string RootScalarWrongTypeMessage(string value)
    {
        var collection = Collection<ScalarRoot>(nameof(RootScalarWrongTypeMessage));
        Raw(collection).InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Floor", value } });
        var ex = Assert.Throws<FormatException>(() => Query(collection, _ => { }, MongoQueryMode.DriverLinq, q => q.ToList().Select(s => s.Floor.ToString())));
        return ex.Message;
    }

    public static TheoryData<string, string?> MissingLeafShapes
        => new()
        {
            // D-F10: a missing required leaf throws; a missing nullable leaf reads null; a missing required nested value
            // throws with the complex-property message.
            { "City", "Document element is missing for required non-nullable property 'City'" },
            { "Floor", "Document element is missing for required non-nullable property 'Floor'" },
            { "Geo.Lat", "Document element is missing for required non-nullable property 'Lat'" },
            { "Geo", "Document element 'Geo' is missing for required complex property 'MRoot.Value#MValue.Geo'" },
            { "Note", null },
            { "Geo.Alt", null }
        };

    [Theory]
    [MemberData(nameof(MissingLeafShapes))]
    public void Missing_leaves_follow_the_required_and_nullable_rules_in_every_materializer(string missing, string? message)
    {
        var value = new BsonDocument { { "City", "c" }, { "Note", "n" }, { "Floor", 1 }, { "Geo", new BsonDocument { { "Lat", 1.0 }, { "Alt", 2.0 } } } };
        if (missing.StartsWith("Geo."))
        {
            value["Geo"].AsBsonDocument.Remove(missing[4..]);
        }
        else
        {
            value.Remove(missing);
        }

        var collection = SeedRaw(nameof(Missing_leaves_follow_the_required_and_nullable_rules_in_every_materializer) + missing.Replace(".", ""),
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Value", value } });

        foreach (var (read, mode, run) in MalformedReads())
        {
            if (message == null)
            {
                var expected = missing == "Note" ? "c|-|1|1|2" : "c|n|1|1|-";
                Assert.True(run(collection).SequenceEqual([read == "Value" ? expected : "a|" + expected]), $"{read} [{mode}]");
            }
            else
            {
                var ex = Assert.ThrowsAny<Exception>(() => run(collection));
                Assert.True(ex is InvalidOperationException, $"{read} [{mode}]: {ex.GetType().Name}: {ex.Message}");
                Assert.True(ex.Message.Contains(message), $"{read} [{mode}]: '{ex.Message}' does not contain '{message}'");
            }
        }
    }

    // ── (4) One complex CLR type at several paths, each with its own element names ─────────────────────────────────

    public class Spot
    {
        public string City { get; set; } = null!;
        public int Code { get; set; }
    }

    [System.ComponentModel.DataAnnotations.Schema.ComplexType]
    public class OwnedSpot
    {
        public string City { get; set; } = null!;
        public int Code { get; set; }
    }

    public class Depot
    {
        public string Label { get; set; } = null!;
        public OwnedSpot Spot { get; set; } = null!;
    }

    public class Shipper
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Spot Billing { get; set; } = null!;
        public Spot Shipping { get; set; } = null!;
        public Spot Previous { get; set; } = null!;
        public Depot Depot { get; set; } = null!;
#if !EF8 && !EF9
        public List<Spot> History { get; set; } = [];
#endif
    }

    private static void ConfigureShipper(ModelBuilder mb)
        => mb.Entity<Shipper>(e =>
        {
            e.ComplexProperty(s => s.Billing, b =>
            {
                b.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "bill");
                b.Property(x => x.City).Metadata.SetElementName("bc");
            });
            e.ComplexProperty(s => s.Shipping, b =>
            {
                b.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "ship");
                b.Property(x => x.Code).Metadata.SetElementName("sc");
            });
            // Previous keeps the CLR names.
            e.ComplexProperty(s => s.Previous);
            e.OwnsOne(s => s.Depot, d => d.HasElementName("dep"));
#if !EF8 && !EF9
            e.ComplexCollection(s => s.History, h =>
            {
                h.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "hist");
                h.Property(x => x.City).Metadata.SetElementName("hc");
            });
#endif
        });

    private static Spot S(string city, int code) => new() { City = city, Code = code };

    private IMongoCollection<Shipper> SeedShippers([CallerMemberName] string name = "")
    {
        var collection = Collection<Shipper>(name);
        using var db = Context(collection, ConfigureShipper);
        db.Entities.AddRange(
            new Shipper
            {
                Name = "a", Billing = S("B1", 1), Shipping = S("S1", 10), Previous = S("P1", 100),
                Depot = new Depot { Label = "d1", Spot = new OwnedSpot { City = "D1", Code = 1000 } },
#if !EF8 && !EF9
                History = [S("H1", 7), S("H2", 8)]
#endif
            },
            new Shipper
            {
                Name = "b", Billing = S("S1", 2), Shipping = S("B1", 20), Previous = S("B1", 200),
                Depot = new Depot { Label = "d2", Spot = new OwnedSpot { City = "B1", Code = 2000 } },
#if !EF8 && !EF9
                History = [S("B1", 9)]
#endif
            });
        db.SaveChanges();
        return collection;
    }

    private static string FmtShipper(Shipper s)
        => $"{s.Name}|{s.Billing.City}:{s.Billing.Code}|{s.Shipping.City}:{s.Shipping.Code}|{s.Previous.City}:{s.Previous.Code}"
           + $"|{s.Depot.Label}:{s.Depot.Spot.City}:{s.Depot.Spot.Code}"
#if !EF8 && !EF9
           + "|" + string.Join(",", s.History.Select(h => h.City + ":" + h.Code))
#endif
        ;

    private static readonly string[] AllShippers =
    [
#if !EF8 && !EF9
        "a|B1:1|S1:10|P1:100|d1:D1:1000|H1:7,H2:8", "b|S1:2|B1:20|B1:200|d2:B1:2000|B1:9"
#else
        "a|B1:1|S1:10|P1:100|d1:D1:1000", "b|S1:2|B1:20|B1:200|d2:B1:2000"
#endif
    ];

    [Fact]
    public void Same_clr_type_at_several_paths_is_written_under_each_paths_own_names()
    {
        var collection = SeedShippers();
        var a = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "a")).Single();

        AssertSameDocument(new BsonDocument { { "Code", 1 }, { "bc", "B1" } }, a["bill"]);
        AssertSameDocument(new BsonDocument { { "City", "S1" }, { "sc", 10 } }, a["ship"]);
        AssertSameDocument(new BsonDocument { { "City", "P1" }, { "Code", 100 } }, a["Previous"]);
        AssertSameDocument(new BsonDocument { { "Label", "d1" }, { "Spot", new BsonDocument { { "City", "D1" }, { "Code", 1000 } } } }, a["dep"]);
#if !EF8 && !EF9
        AssertSameDocument(new BsonArray { new BsonDocument { { "Code", 7 }, { "hc", "H1" } }, new BsonDocument { { "Code", 8 }, { "hc", "H2" } } },
            a["hist"]);
#endif
        Assert.False(a.Contains("Billing") || a.Contains("Shipping") || a.Contains("Depot"));
    }

    [Fact]
    public void Same_clr_type_at_several_paths_reads_predicates_and_projects_each_path_independently()
    {
        var collection = SeedShippers();

        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureShipper, m, q => q.OrderBy(s => s.Name).ToList().Select(FmtShipper)),
            [.. AllShippers]);

        // The same literal matches a different row through each path: only a path-blind resolver would agree.
        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.Billing.City == "B1").Select(s => s.Name)), ["a"]);
        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.Shipping.City == "B1").Select(s => s.Name)), ["b"]);
        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.Previous.City == "P1").Select(s => s.Name)), ["a"]);
        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.Depot.Spot.City == "B1").Select(s => s.Name)), ["b"]);
        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.Shipping.Code > 15).Select(s => s.Name)), ["b"]);

        // EF8/EF9 wrap the root of an entity with an owned navigation in an auto-include, which the native complex-hop
        // walk does not peel: projections of complex leaves/values DECLINE there (the fallback serves the right rows).
        // EF10 has no such wrapper. Native gap (Task 16 item), never rows.
        PerMode(m => Query(collection, ConfigureShipper, m, q => q.OrderBy(s => s.Name)
                .Select(s => new { B = s.Billing.City, S = s.Shipping.City, P = s.Previous.Code, D = s.Depot.Spot.City }).ToList()
                .Select(x => $"{x.B}/{x.S}/{x.P}/{x.D}")),
            ["B1/S1/100/D1", "S1/B1/200/B1"], OwnedIncludeNativeOnly, Serves, Serves);
        PerMode(m => Query(collection, ConfigureShipper, m, q => q.OrderBy(s => s.Name)
                .Select(s => new { s.Billing, s.Shipping }).ToList().Select(x => $"{x.Billing.City}:{x.Billing.Code}/{x.Shipping.City}:{x.Shipping.Code}")),
            ["B1:1/S1:10", "S1:2/B1:20"], OwnedIncludeNativeOnly, OwnedIncludeWholeValueFallback, OwnedIncludeWholeValueFallback);

        // Member-wise equality between two paths of the same CLR type with different element names: native compares
        // member by member through each path's own names (b's Shipping and Previous are both B1 but differ in Code;
        // a third row below makes them equal); the driver refuses the stored pair (its serializers differ) and the
        // complex serializer refuses an instance comparand (ruling R1).
        PerMode(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.Shipping == s.Previous).Select(s => s.Name)), [],
            Serves, Serves, "because the two arguments are serialized differently");
        PerMode(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.Billing != s.Previous).OrderBy(s => s.Name).Select(s => s.Name)), ["a", "b"],
            Serves, Serves, "because the two arguments are serialized differently");
        var target = S("B1", 20);
        PerMode(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.Shipping == target).Select(s => s.Name)), ["b"],
            Serves, Serves, "as a whole value is not supported");
        var targetForBilling = S("B1", 1);
        PerMode(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.Billing == targetForBilling).Select(s => s.Name)), ["a"],
            Serves, Serves, "as a whole value is not supported");
#if !EF8 && !EF9
        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.History.Any(h => h.City == "B1")).Select(s => s.Name)), ["b"]);
        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureShipper, m, q => q.Where(s => s.History.Any(h => h.Code == 8)).Select(s => s.Name)), ["a"]);
#endif
    }

#if EF8 || EF9
    private const string OwnedIncludeNativeOnly = NotNative;
    private const string OwnedIncludeWholeValueFallback = OwnedIncludeWholeValueFallbackMessage;
#else
    private const string OwnedIncludeNativeOnly = Serves;
    private const string OwnedIncludeWholeValueFallback = Serves;
#endif
    // The fallback cannot read a whole complex value back (the complex serializer does not deserialize; ruling R1/R7
    // family): loud, never rows.
    private const string OwnedIncludeWholeValueFallbackMessage = "property of class <>f__AnonymousType";

    [Fact]
    public void Same_clr_type_at_several_paths_updates_only_the_changed_path()
    {
        var collection = SeedShippers();
        var before = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "a")).Single();

        using (var db = Context(collection, ConfigureShipper))
        {
            var a = db.Entities.Single(s => s.Name == "a");
            a.Shipping.City = "changed";
            db.SaveChanges();
        }

        var after = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "a")).Single();
        before["ship"]["City"] = "changed";
        Assert.Equal(before, after);
    }

#if !EF8
    [Fact]
    public void Same_clr_type_at_several_paths_bulk_filter_and_set_use_each_paths_names()
    {
        var collection = SeedShippers();
        var before = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["Name"].AsString);

        using (var db = Context(collection, ConfigureShipper))
        {
#if EF9
            // EF9 wraps an entity with an owned navigation (Depot) in an auto-include Select that EF's own bulk validation
            // rejects. Pre-existing for ANY owned navigation (measured with no complex type), not complex-specific.
            var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.Where(s => s.Shipping.City == "B1")
                .ExecuteUpdate(u => u.SetProperty(s => s.Billing.City, "bulk").SetProperty(s => s.Previous.Code, 5)));
            Assert.Contains("The 'Select' operator is not supported in a bulk delete or update", ex.Message);
            Assert.Equal(before["b"], Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "b")).Single());
            return;
#else
            Assert.Equal(1, db.Entities.Where(s => s.Shipping.City == "B1")
                .ExecuteUpdate(u => u.SetProperty(s => s.Billing.City, "bulk").SetProperty(s => s.Previous.Code, 5)));
#endif
        }

        var after = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["Name"].AsString);
        Assert.Equal(before["a"], after["a"]);
        before["b"]["bill"]["bc"] = "bulk";
        before["b"]["Previous"]["Code"] = 5;
        Assert.Equal(before["b"], after["b"]);
    }
#endif

    // ── (5) BSON attributes on complex properties and leaves ─────────────────────────────────────────────────────

    public class AttributedValue
    {
        [MongoDB.Bson.Serialization.Attributes.BsonElement("e")]
        public string Elemented { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Schema.Column("col")]
        public string Columned { get; set; } = null!;

        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        public string Ignored { get; set; } = "default";

        [MongoDB.Bson.Serialization.Attributes.BsonRequired]
        public string? Required { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(BsonType.String)]
        public int Represented { get; set; }
    }

    public class AttributedHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;

        [MongoDB.Bson.Serialization.Attributes.BsonElement("av")]
        public AttributedValue Value { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Schema.Column("cv")]
        public Spot Columned { get; set; } = null!;
    }

    private static void ConfigureAttributed(ModelBuilder mb)
    {
        mb.Entity<AttributedHolder>().ComplexProperty(h => h.Value);
        mb.Entity<AttributedHolder>().ComplexProperty(h => h.Columned);
    }

    [Fact]
    public void Bson_attributes_on_complex_properties_and_leaves_shape_the_stored_document()
    {
        var collection = Collection<AttributedHolder>();
        using (var db = Context(collection, ConfigureAttributed))
        {
            var complexType = db.Model.FindEntityType(typeof(AttributedHolder))!.FindComplexProperty("Value")!.ComplexType;
            Assert.Null(complexType.FindProperty(nameof(AttributedValue.Ignored)));
            Assert.False(complexType.FindProperty(nameof(AttributedValue.Required))!.IsNullable);

            db.Entities.AddRange(
                new AttributedHolder
                {
                    Name = "a", Columned = S("c1", 1),
                    Value = new AttributedValue { Elemented = "e1", Columned = "k1", Ignored = "never-written", Required = "r1", Represented = 5 }
                },
                new AttributedHolder
                {
                    Name = "b", Columned = S("c2", 2),
                    Value = new AttributedValue { Elemented = "e2", Columned = "k2", Ignored = "never-written", Required = "r2", Represented = 12 }
                });
            db.SaveChanges();
        }

        var a = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "a")).Single();
        Assert.Equal(["Name", "_id", "av", "cv"], a.Names.OrderBy(n => n, StringComparer.Ordinal).ToArray());
        AssertSameDocument(new BsonDocument { { "Represented", "5" }, { "Required", "r1" }, { "col", "k1" }, { "e", "e1" } }, a["av"]);
        AssertSameDocument(new BsonDocument { { "City", "c1" }, { "Code", 1 } }, a["cv"]);

        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureAttributed, m, q => q.OrderBy(h => h.Name).ToList()
                .Select(h => $"{h.Value.Elemented}|{h.Value.Columned}|{h.Value.Ignored}|{h.Value.Required}|{h.Value.Represented}|{h.Columned.City}")),
            ["e1|k1|default|r1|5|c1", "e2|k2|default|r2|12|c2"]);
        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureAttributed, m, q => q
                .Where(h => h.Value.Elemented == "e2" && h.Value.Columned == "k2" && h.Columned.City == "c2").Select(h => h.Name)),
            ["b"]);
        // A represented leaf compares in its stored (string) form: "12" < "5" as strings, so `> 5` must not be served by
        // a numeric comparison. Measured below; ordering over the leaf orders by the stored string.
        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureAttributed, m, q => q.Where(h => h.Value.Represented == 12).Select(h => h.Name)),
            ["b"]);
    }

    [Fact]
    public void BsonIgnore_leaf_is_not_queryable_and_not_part_of_member_wise_equality()
    {
        var collection = Collection<AttributedHolder>();
        Raw(collection).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "cv", new BsonDocument { { "City", "c" }, { "Code", 1 } } },
            // A stored element under the ignored member's CLR name is never read.
            { "av", new BsonDocument { { "e", "e1" }, { "col", "k1" }, { "Required", "r" }, { "Represented", "1" }, { "Ignored", "stored" } } }
        });

        NativeModeAssert.NativeAndExpected(m => Query(collection, ConfigureAttributed, m, q => q.Select(h => h.Value).ToList().Select(v => v.Ignored)),
            ["default"]);

        // Equality compares mapped members only: an instance whose ignored member differs is still equal.
        var instance = new AttributedValue { Elemented = "e1", Columned = "k1", Required = "r", Represented = 1, Ignored = "something else" };
        PerMode(m => Query(collection, ConfigureAttributed, m, q => q.Where(h => h.Value == instance).Select(h => h.Name)), ["a"],
            Serves, Serves, "as a whole value is not supported");

        // An ignored member is not part of the model: a predicate over it never reads the stored decoy. Native declines;
        // the driver refuses (the complex serializer has no such member). Loud in every mode, never rows.
        PerMode(m => Query(collection, ConfigureAttributed, m, q => q.Where(h => h.Value.Ignored == "stored").Select(h => h.Name)), [],
            NotNative, "does not have a member named Ignored", "does not have a member named Ignored");
    }

    [Fact]
    public void BsonRequired_leaf_missing_or_null_throws_in_every_materializer()
    {
        foreach (var (state, expected) in new[]
                 {
                     ("missing", "Document element is missing for required non-nullable property 'Required'"),
                     ("null", "Document element is null for required non-nullable property 'Required'")
                 })
        {
            var collection = Collection<AttributedHolder>(nameof(BsonRequired_leaf_missing_or_null_throws_in_every_materializer) + state);
            var value = new BsonDocument { { "e", "e1" }, { "col", "k1" }, { "Represented", "1" } };
            if (state == "null")
            {
                value["Required"] = BsonNull.Value;
            }

            Raw(collection).InsertOne(new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "cv", new BsonDocument { { "City", "c" }, { "Code", 1 } } }, { "av", value }
            });

            foreach (var mode in AllModes)
            {
                foreach (var (read, run) in new (string, Func<List<string>>)[]
                         {
                             ("ToList", () => Query(collection, ConfigureAttributed, mode, q => q.ToList().Select(h => h.Value.Required!))),
                             ("First", () => Query(collection, ConfigureAttributed, mode, q => new[] { q.First().Value.Required! })),
                             ("Value", () => Query(collection, ConfigureAttributed, mode, q => q.Select(h => h.Value).ToList().Select(v => v.Required!)))
                         })
                {
                    var ex = Assert.ThrowsAny<Exception>(run);
                    Assert.True(ex is InvalidOperationException && ex.Message.Contains(expected), $"{state} {read} [{mode}]: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
    }

    // ── (6) Leaf type matrix ────────────────────────────────────────────────────────────────────────────────────

    public enum Grade
    {
        Low,
        Mid,
        High
    }

    public class ScalarLeaves
    {
        public Grade Grade { get; set; }
        public Grade GradeText { get; set; }
        public Guid Guid { get; set; }
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public long Long { get; set; }
        public int Int { get; set; }
        public short Short { get; set; }
        public byte Byte { get; set; }
        public sbyte SByte { get; set; }
        public uint UInt { get; set; }
        public ulong ULong { get; set; }
        public DateTime Utc { get; set; }
        public DateTime Local { get; set; }
        public DateTime Unspecified { get; set; }
        public DateTimeOffset Dto { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
        public TimeSpan Span { get; set; }
        public string Text { get; set; } = null!;
        public bool Flag { get; set; }
        public char Char { get; set; }
        public int? NullableInt { get; set; }
        public Guid? NullableGuid { get; set; }
        public ObjectId Oid { get; set; }
        public int Converted { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(BsonType.String)]
        public int Represented { get; set; }
    }

    public class CollectionLeaves
    {
        public ScalarLeaves Scalars { get; set; } = null!;
        public byte[] Bytes { get; set; } = null!;
        public List<string> Tags { get; set; } = null!;
        public string[] Codes { get; set; } = null!;
        public Dictionary<string, int> Counts { get; set; } = null!;
    }

    public class TypedRoot
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public CollectionLeaves Leaves { get; set; } = null!;
#if !EF8 && !EF9
        public List<ScalarLeaves> Items { get; set; } = [];
#endif
    }

    // The same scalars stored at the ROOT of an entity, configured identically: the storage oracle for the complex leaves.
    public class ScalarEntity : ScalarLeaves
    {
        public ObjectId Id { get; set; }
    }

    private static void ConfigureScalarLeaves<T>(Func<string, Microsoft.EntityFrameworkCore.Metadata.IMutableProperty> property)
    {
        property(nameof(ScalarLeaves.GradeText)).SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToStringConverter<Grade>());
        property(nameof(ScalarLeaves.Converted)).SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.NumberToStringConverter<int>());
        property(nameof(ScalarLeaves.Utc)).SetDateTimeKind(DateTimeKind.Utc);
        property(nameof(ScalarLeaves.Local)).SetDateTimeKind(DateTimeKind.Local);
        property(nameof(ScalarLeaves.Unspecified)).SetDateTimeKind(DateTimeKind.Unspecified);
    }

    private static void ConfigureTyped(ModelBuilder mb)
        => mb.Entity<TypedRoot>(e =>
        {
            e.ComplexProperty(r => r.Leaves, l =>
                l.ComplexProperty(x => x.Scalars, sc => ConfigureScalarLeaves<ScalarLeaves>(n => sc.Metadata.ComplexType.FindProperty(n)!)));
#if !EF8 && !EF9
            e.ComplexCollection(r => r.Items, i => ConfigureScalarLeaves<ScalarLeaves>(n => i.Metadata.ComplexType.FindProperty(n)!));
#endif
        });

    private static void ConfigureScalarEntity(ModelBuilder mb)
        => mb.Entity<ScalarEntity>(e => ConfigureScalarLeaves<ScalarEntity>(n => e.Metadata.FindProperty(n)!));

    private static readonly Guid GuidA = new("00112233-4455-6677-8899-aabbccddeeff");
    private static readonly Guid GuidB = new("ffeeddcc-bbaa-9988-7766-554433221100");
    private static readonly ObjectId OidA = new("0123456789abcdef01234567");
    private static readonly ObjectId OidB = new("fedcba9876543210fedcba98");

    private static T Fill<T>(T s, bool a) where T : ScalarLeaves
    {
        s.Grade = a ? Grade.High : Grade.Low;
        s.GradeText = a ? Grade.Mid : Grade.High;
        s.Guid = a ? GuidA : GuidB;
        s.Decimal = a ? 12.34m : 0.5m;
        s.Double = a ? 2.5 : -1e300;
        s.Float = a ? 1.25f : float.MaxValue;
        s.Long = a ? long.MaxValue : long.MinValue;
        s.Int = a ? 7 : int.MinValue;
        s.Short = a ? short.MaxValue : (short)-3;
        s.Byte = a ? byte.MaxValue : (byte)1;
        s.SByte = a ? sbyte.MinValue : (sbyte)5;
        s.UInt = a ? 2_000_000_000u : 3u;
        s.ULong = a ? 9_000_000_000_000_000_000UL : 4UL;
        s.Utc = a ? new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc) : new DateTime(1999, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        s.Local = a ? new DateTime(2024, 6, 7, 8, 9, 10, DateTimeKind.Local) : new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local);
        s.Unspecified = a ? new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Unspecified) : new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        s.Dto = a ? new DateTimeOffset(2024, 3, 4, 5, 6, 7, TimeSpan.FromHours(2)) : new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero);
        s.Date = a ? new DateOnly(2024, 4, 5) : new DateOnly(1, 1, 1);
        s.Time = a ? new TimeOnly(13, 14, 15) : new TimeOnly(0, 0, 1);
        s.Span = a ? TimeSpan.FromMinutes(90) : TimeSpan.FromTicks(-1);
        s.Text = a ? "日本語 ✓ " + new string('x', 20_000) : "";
        s.Flag = a;
        s.Char = a ? 'Ω' : 'a';
        s.NullableInt = a ? 42 : null;
        s.NullableGuid = a ? null : GuidA;
        s.Oid = a ? OidA : OidB;
        s.Converted = a ? 123 : 9;
        s.Represented = a ? 77 : 8;
        return s;
    }

    private static CollectionLeaves Leaves(bool a)
        => new()
        {
            Scalars = Fill(new ScalarLeaves(), a),
            Bytes = a ? [1, 2, 3] : [],
            Tags = a ? ["x", "y"] : ["z"],
            Codes = a ? ["c1"] : ["c2", "c3"],
            Counts = a ? new Dictionary<string, int> { { "k", 1 }, { "m", 2 } } : new Dictionary<string, int> { { "k", 5 } }
        };

    private IMongoCollection<TypedRoot> SeedTyped([CallerMemberName] string name = "")
    {
        var collection = Collection<TypedRoot>(name);
        using var db = Context(collection, ConfigureTyped);
        db.Entities.AddRange(
            new TypedRoot
            {
                Name = "a", Leaves = Leaves(true),
#if !EF8 && !EF9
                Items = [Fill(new ScalarLeaves(), true)]
#endif
            },
            new TypedRoot
            {
                Name = "b", Leaves = Leaves(false),
#if !EF8 && !EF9
                Items = [Fill(new ScalarLeaves(), false), Fill(new ScalarLeaves(), false)]
#endif
            });
        db.SaveChanges();
        return collection;
    }

    private static string FmtScalars(ScalarLeaves s)
        => string.Join("|",
            s.Grade, s.GradeText, s.Guid, s.Decimal, s.Double.ToString("R"), s.Float.ToString("R"), s.Long, s.Int, s.Short, s.Byte, s.SByte,
            s.UInt, s.ULong, $"{s.Utc:O}/{s.Utc.Kind}", $"{s.Local:O}/{s.Local.Kind}", $"{s.Unspecified:O}/{s.Unspecified.Kind}",
            s.Dto.ToString("O"), s.Date.ToString("O"), s.Time.ToString("O"), s.Span, s.Text.Length + ":" + s.Text[..Math.Min(5, s.Text.Length)],
            s.Flag, s.Char, s.NullableInt?.ToString() ?? "-", s.NullableGuid?.ToString() ?? "-", s.Oid, s.Converted, s.Represented);

    [Fact]
    public void Every_scalar_leaf_type_is_stored_exactly_like_the_same_root_scalar()
    {
        var collection = SeedTyped();
        var scalars = Collection<ScalarEntity>();
        using (var db = Context(scalars, ConfigureScalarEntity))
        {
            db.Entities.Add(Fill(new ScalarEntity { Id = ObjectId.GenerateNewId() }, true));
            db.SaveChanges();
        }

        var root = StoredSingleDocument(scalars);
        var stored = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "a")).Single();
        var leaves = stored["Leaves"]["Scalars"].AsBsonDocument;
        foreach (var element in root.Where(e => e.Name != "_id"))
        {
            Assert.True(leaves.Contains(element.Name), element.Name);
            Assert.True(element.Value.Equals(leaves[element.Name]), $"{element.Name}: root {element.Value.ToJson()} vs leaf {leaves[element.Name].ToJson()}");
        }

        Assert.Equal(root.ElementCount - 1, leaves.ElementCount);

        // Hand-checked anchors for the representation choices the oracle shares.
        Assert.Equal(new BsonBinaryData(GuidA, GuidRepresentation.Standard), leaves["Guid"]);
        Assert.Equal(BsonType.Decimal128, leaves["Decimal"].BsonType);
        Assert.Equal("Mid", leaves["GradeText"].AsString);
        Assert.Equal(2, leaves["Grade"].AsInt32);
        Assert.Equal("123", leaves["Converted"].AsString);
        Assert.Equal("77", leaves["Represented"].AsString);
        Assert.Equal(BsonNull.Value, leaves["NullableGuid"]);
        Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc), leaves["Utc"].ToUniversalTime());
        Assert.Equal(new DateTime(2024, 6, 7, 8, 9, 10, DateTimeKind.Local).ToUniversalTime(), leaves["Local"].ToUniversalTime());

        // Collection-typed leaves: an array, an array, a document, binary.
        Assert.Equal(new BsonArray { "x", "y" }, stored["Leaves"]["Tags"]);
        Assert.Equal(new BsonArray { "c1" }, stored["Leaves"]["Codes"]);
        Assert.Equal(new BsonDocument { { "k", 1 }, { "m", 2 } }, stored["Leaves"]["Counts"]);
        Assert.Equal(new BsonBinaryData([1, 2, 3]), stored["Leaves"]["Bytes"]);
#if !EF8 && !EF9
        var item = stored["Items"][0].AsBsonDocument;
        foreach (var element in root.Where(e => e.Name != "_id"))
        {
            Assert.True(element.Value.Equals(item[element.Name]), $"Items[0].{element.Name}: root {element.Value.ToJson()} vs element {item[element.Name].ToJson()}");
        }
#endif
    }

    [Fact]
    public void Out_of_range_unsigned_leaf_fails_on_save_exactly_like_the_same_root_scalar()
    {
        // The default UInt32 serializer stores Int32; above int.MaxValue it overflows. Pre-existing for root scalars;
        // the complex leaf uses the same serializer, so it fails the same way (loud, nothing written).
        var root = Collection<ScalarEntity>();
        using (var db = Context(root, ConfigureScalarEntity))
        {
            var s = Fill(new ScalarEntity { Id = ObjectId.GenerateNewId() }, true);
            s.UInt = uint.MaxValue;
            db.Entities.Add(s);
            Assert.Throws<OverflowException>(() => db.SaveChanges());
        }

        var complex = Collection<TypedRoot>();
        using (var db = Context(complex, ConfigureTyped))
        {
            var leaves = Leaves(true);
            leaves.Scalars.UInt = uint.MaxValue;
            db.Entities.Add(new TypedRoot { Name = "a", Leaves = leaves });
            Assert.Throws<OverflowException>(() => db.SaveChanges());
        }

        Assert.Equal(0, Raw(root).CountDocuments(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(0, Raw(complex).CountDocuments(FilterDefinition<BsonDocument>.Empty));
    }

    private static BsonDocument StoredSingleDocument<T>(IMongoCollection<T> collection)
        => Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).Single();

    [Theory]
    [MemberData(nameof(TrackingBehaviors))]
    public void Every_leaf_type_reads_back_exactly(QueryTrackingBehavior tracking)
    {
        var collection = SeedTyped(nameof(Every_leaf_type_reads_back_exactly) + tracking);
        var written = new[] { Fill(new ScalarLeaves(), true), Fill(new ScalarLeaves(), false) }.Select(FmtScalars).ToList();

        // The oracle is the same values stored and read back as ROOT scalars: every leaf must read back exactly as its
        // root counterpart does. That is the written value except one pre-existing serializer rule: a DateTime with
        // Kind Unspecified is stored as UTC and reads back with Kind Utc (DateTimeSerializer.Instance; root and leaf alike).
        var roots = Collection<ScalarEntity>(nameof(Every_leaf_type_reads_back_exactly) + "root" + tracking);
        using (var db = Context(roots, ConfigureScalarEntity))
        {
            db.Entities.AddRange(Fill(new ScalarEntity { Id = new ObjectId("000000000000000000000001") }, true),
                Fill(new ScalarEntity { Id = new ObjectId("000000000000000000000002") }, false));
            db.SaveChanges();
        }

        var expected = Query(roots, ConfigureScalarEntity, MongoQueryMode.NativeOnly, q => q.OrderBy(r => r.Id).ToList().Select(FmtScalars), tracking);
        Assert.Equal(
            written.Select(w => w.Replace(":06.0000000/Unspecified", ":06.0000000Z/Utc").Replace("T00:00:00.0000000/Unspecified", "T00:00:00.0000000Z/Utc")),
            expected);
        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureTyped, m, q => q.OrderBy(r => r.Name).ToList().Select(r => FmtScalars(r.Leaves.Scalars)), tracking),
            expected);
        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureTyped, m, q => q.OrderBy(r => r.Name).Select(r => r.Leaves.Scalars).ToList().Select(FmtScalars), tracking),
            expected);
        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureTyped, m, q => q.OrderBy(r => r.Name).ToList()
                .Select(r => $"{string.Join(",", r.Leaves.Bytes)}|{string.Join(",", r.Leaves.Tags)}|{string.Join(",", r.Leaves.Codes)}|"
                             + string.Join(",", r.Leaves.Counts.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value))), tracking),
            ["1,2,3|x,y|c1|k=1,m=2", "|z|c2,c3|k=5"]);
#if !EF8 && !EF9
        NativeModeAssert.NativeAndExpected(
            m => Query(collection, ConfigureTyped, m, q => q.OrderBy(r => r.Name).ToList().SelectMany(r => r.Items.Select(FmtScalars)), tracking),
            [expected[0], expected[1], expected[1]]);
#endif
    }

    // Each predicate over a single leaf, written against the hand-known seed (a: the first Fill, b: the second).
    public static readonly Dictionary<string, (System.Linq.Expressions.Expression<Func<TypedRoot, bool>> Predicate, string[] Expected)> LeafPredicates = new()
    {
        ["enum"] = (r => r.Leaves.Scalars.Grade == Grade.High, ["a"]),
        ["enum-relational"] = (r => r.Leaves.Scalars.Grade < Grade.Mid, ["b"]),
        ["enum-string"] = (r => r.Leaves.Scalars.GradeText == Grade.High, ["b"]),
        ["guid"] = (r => r.Leaves.Scalars.Guid == GuidB, ["b"]),
        ["decimal"] = (r => r.Leaves.Scalars.Decimal > 1m, ["a"]),
        ["double"] = (r => r.Leaves.Scalars.Double < 0, ["b"]),
        ["float"] = (r => r.Leaves.Scalars.Float == 1.25f, ["a"]),
        ["long"] = (r => r.Leaves.Scalars.Long == long.MinValue, ["b"]),
        ["int"] = (r => r.Leaves.Scalars.Int > 0, ["a"]),
        ["short"] = (r => r.Leaves.Scalars.Short < 0, ["b"]),
        ["byte"] = (r => r.Leaves.Scalars.Byte == 255, ["a"]),
        ["sbyte"] = (r => r.Leaves.Scalars.SByte > 0, ["b"]),
        ["uint"] = (r => r.Leaves.Scalars.UInt == 3u, ["b"]),
        ["ulong"] = (r => r.Leaves.Scalars.ULong > 5UL, ["a"]),
        ["datetime-utc"] = (r => r.Leaves.Scalars.Utc > new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), ["a"]),
        ["datetime-part"] = (r => r.Leaves.Scalars.Utc.Year == 1999, ["b"]),
        ["dto"] = (r => r.Leaves.Scalars.Dto > new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), ["a"]),
        ["dateonly"] = (r => r.Leaves.Scalars.Date == new DateOnly(2024, 4, 5), ["a"]),
        ["timeonly"] = (r => r.Leaves.Scalars.Time < new TimeOnly(1, 0), ["b"]),
        ["timespan"] = (r => r.Leaves.Scalars.Span > TimeSpan.Zero, ["a"]),
        ["string-empty"] = (r => r.Leaves.Scalars.Text == "", ["b"]),
        ["string-method"] = (r => r.Leaves.Scalars.Text.StartsWith("日本"), ["a"]),
        ["string-length"] = (r => r.Leaves.Scalars.Text.Length > 20_000, ["a"]),
        ["bool"] = (r => r.Leaves.Scalars.Flag, ["a"]),
        ["bool-not"] = (r => !r.Leaves.Scalars.Flag, ["b"]),
        ["char"] = (r => r.Leaves.Scalars.Char == 'Ω', ["a"]),
        ["nullable-null"] = (r => r.Leaves.Scalars.NullableInt == null, ["b"]),
        ["nullable-value"] = (r => r.Leaves.Scalars.NullableInt == 42, ["a"]),
        ["nullable-hasvalue"] = (r => r.Leaves.Scalars.NullableGuid.HasValue, ["b"]),
        ["objectid"] = (r => r.Leaves.Scalars.Oid == OidA, ["a"]),
        ["converted-eq"] = (r => r.Leaves.Scalars.Converted == 9, ["b"]),
        ["represented-eq"] = (r => r.Leaves.Scalars.Represented == 77, ["a"]),
        ["list-count"] = (r => r.Leaves.Tags.Count == 2, ["a"]),
        ["list-contains"] = (r => r.Leaves.Tags.Contains("z"), ["b"]),
        ["array-length"] = (r => r.Leaves.Codes.Length > 1, ["b"]),
        ["array-contains"] = (r => r.Leaves.Codes.Contains("c1"), ["a"]),
        ["bytes-length"] = (r => r.Leaves.Bytes.Length == 0, ["b"]),
        ["dictionary-count"] = (r => r.Leaves.Counts.Count == 2, ["a"])
    };

    public static TheoryData<string> LeafPredicateNames => [.. LeafPredicates.Keys];

    [Theory]
    [MemberData(nameof(LeafPredicateNames))]
    public void Every_leaf_type_filters_against_the_hand_written_answer_like_the_same_root_scalar(string name)
    {
        var collection = SeedTyped(nameof(Every_leaf_type_filters_against_the_hand_written_answer_like_the_same_root_scalar) + name.Replace("-", ""));
        var (predicate, expected) = LeafPredicates[name];

        var complexOutcomes = AllModes.Select(m => $"{m}={Outcome(() => Query(collection, ConfigureTyped, m,
            q => q.Where(predicate).OrderBy(r => r.Name).Select(r => r.Name)))}").ToList();

        // The same predicate over ROOT scalars (and root collection-typed properties) of the same types and values.
        var roots = Collection<TypedFlatRoot>(nameof(Every_leaf_type_filters_against_the_hand_written_answer_like_the_same_root_scalar) + "root" + name.Replace("-", ""));
        using (var db = Context(roots, ConfigureTypedFlat))
        {
            db.Entities.AddRange(Flat("a", true), Flat("b", false));
            db.SaveChanges();
        }

        var flatPredicate = (System.Linq.Expressions.Expression<Func<TypedFlatRoot, bool>>)new FlattenToRoot().Visit(predicate);
        var rootOutcomes = AllModes.Select(m => $"{m}={Outcome(() => Query(roots, ConfigureTypedFlat, m,
            q => q.Where(flatPredicate).OrderBy(r => r.Name).Select(r => r.Name)))}").ToList();

        var served = string.Join(";", expected);
        // Messages name the member chain; normalise the complex chain to the root spelling before comparing.
        complexOutcomes = complexOutcomes.Select(o => o.Replace("t.Leaves.Scalars.", "t.").Replace("t.Leaves.", "t.")).ToList();
        Assert.True(rootOutcomes.SequenceEqual(complexOutcomes),
            $"complex: {string.Join(" / ", complexOutcomes)}\nroot:    {string.Join(" / ", rootOutcomes)}");
        var pinned = LeafPredicateOutcomes.TryGetValue(name, out var p) ? p : null;
        Assert.Equal(pinned ?? AllModes.Select(m => $"{m}={served}").ToArray(), complexOutcomes.ToArray());
    }

    // The flat oracle: every leaf of Leaves and Leaves.Scalars at the root of an entity.
    public class TypedFlatRoot : ScalarLeaves
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public byte[] Bytes { get; set; } = null!;
        public List<string> Tags { get; set; } = null!;
        public string[] Codes { get; set; } = null!;
        public Dictionary<string, int> Counts { get; set; } = null!;
    }

    private static void ConfigureTypedFlat(ModelBuilder mb)
        => mb.Entity<TypedFlatRoot>(e => ConfigureScalarLeaves<TypedFlatRoot>(n => e.Metadata.FindProperty(n)!));

    private static TypedFlatRoot Flat(string name, bool a)
    {
        var leaves = Leaves(a);
        var flat = Fill(new TypedFlatRoot { Name = name }, a);
        flat.Bytes = leaves.Bytes;
        flat.Tags = leaves.Tags;
        flat.Codes = leaves.Codes;
        flat.Counts = leaves.Counts;
        return flat;
    }

    // Rewrites `r.Leaves.Scalars.X` / `r.Leaves.X` over TypedRoot to `f.X` over TypedFlatRoot.
    private sealed class FlattenToRoot : System.Linq.Expressions.ExpressionVisitor
    {
        private readonly System.Linq.Expressions.ParameterExpression _flat = System.Linq.Expressions.Expression.Parameter(typeof(TypedFlatRoot), "f");

        protected override System.Linq.Expressions.Expression VisitLambda<T>(System.Linq.Expressions.Expression<T> node)
            => System.Linq.Expressions.Expression.Lambda<Func<TypedFlatRoot, bool>>(Visit(node.Body), _flat);

        protected override System.Linq.Expressions.Expression VisitMember(System.Linq.Expressions.MemberExpression node)
        {
            if (node.Expression is System.Linq.Expressions.MemberExpression { Member.Name: nameof(CollectionLeaves.Scalars) or nameof(TypedRoot.Leaves) } inner
                && (inner.Member.Name == nameof(TypedRoot.Leaves) || inner.Expression is System.Linq.Expressions.MemberExpression { Member.Name: nameof(TypedRoot.Leaves) }))
            {
                return System.Linq.Expressions.Expression.Property(_flat, node.Member.Name);
            }

            return base.VisitMember(node);
        }
    }

    private const string NativeOnlyDecline =
        "NativeOnly=NativeTranslationNotSupportedException: Query projects a non-entity result and MongoQueryMode.NativeOnly forbids the driver-LINQ fallback.";

    // Measured exceptions to "native and right in every mode". Each is IDENTICAL for the same root scalar (asserted
    // above), so none is complex-specific: native gaps (the fallback serves the right rows) and two pre-existing
    // root-scalar problems (a dictionary Count the driver refuses, and an empty byte[] Length that returns NO ROWS:
    // Jira candidate, wrong rows on the driver path for root and leaf alike).
    private static readonly Dictionary<string, string[]> LeafPredicateOutcomes = new()
    {
        ["datetime-utc"] = [NativeOnlyDecline, "Native=a", "DriverLinq=a"],
        ["dto"] = [NativeOnlyDecline, "Native=a", "DriverLinq=a"],
        ["dateonly"] = [NativeOnlyDecline, "Native=a", "DriverLinq=a"],
        ["timeonly"] = [NativeOnlyDecline, "Native=b", "DriverLinq=b"],
        ["list-count"] = [NativeOnlyDecline, "Native=a", "DriverLinq=a"],
        ["array-length"] = [NativeOnlyDecline, "Native=b", "DriverLinq=b"],
        ["bytes-length"] = [NativeOnlyDecline, "Native=", "DriverLinq="],
        ["dictionary-count"] =
        [
            NativeOnlyDecline,
            "Native=ExpressionNotSupportedException: Expression not supported: t.Counts.Count because the expression is not represented as an array in the database.",
            "DriverLinq=ExpressionNotSupportedException: Expression not supported: t.Counts.Count because the expression is not represented as an array in the database."
        ]
    };

    [Fact]
    public void Every_scalar_leaf_type_takes_part_in_member_wise_equality()
    {
        var collection = SeedTyped();

        // A captured instance equal to a's scalars matches a only; one leaf changed (each type in turn) matches nothing.
        var equal = Fill(new ScalarLeaves(), true);
        PerMode(m => Query(collection, ConfigureTyped, m, q => q.Where(r => r.Leaves.Scalars == equal).Select(r => r.Name)), ["a"],
            Serves, Serves, "as a whole value is not supported");
        PerMode(m => Query(collection, ConfigureTyped, m, q => q.Where(r => r.Leaves.Scalars != equal).Select(r => r.Name)), ["b"],
            Serves, Serves, "as a whole value is not supported");

        foreach (var member in typeof(ScalarLeaves).GetProperties())
        {
            var changed = Fill(new ScalarLeaves(), true);
            var other = Fill(new ScalarLeaves(), false);
            member.SetValue(changed, member.Name == nameof(ScalarLeaves.NullableInt) ? 41 : member.GetValue(other));
            var rows = Query(collection, ConfigureTyped, MongoQueryMode.NativeOnly, q => q.Where(r => r.Leaves.Scalars == changed).Select(r => r.Name));
            Assert.True(rows.Count == 0, $"{member.Name}: a leaf that differs must make the values unequal, got [{string.Join(",", rows)}]");
        }
    }

    [Fact]
    public void Collection_typed_leaves_make_member_wise_equality_decline()
    {
        // The compared value holds byte[]/List/array/Dictionary leaves: declined natively (a stored array/document is not
        // compared element-wise); the fallback refuses (ruling R1). Loud in every mode, never rows.
        var collection = SeedTyped();
        var value = Leaves(true);
        PerMode(m => Query(collection, ConfigureTyped, m, q => q.Where(r => r.Leaves == value).Select(r => r.Name)), [],
            NotNative, "as a whole value is not supported", "as a whole value is not supported");
    }

    public class Floats
    {
        public double D { get; set; }
        public float F { get; set; }
    }

    public class FloatHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Floats Value { get; set; } = null!;
    }

    [Fact]
    public void NaN_and_infinities_round_trip_and_compare_like_the_server_orders_them()
    {
        var collection = Collection<FloatHolder>();
        static void Configure(ModelBuilder mb) => mb.Entity<FloatHolder>().ComplexProperty(h => h.Value);
        using (var db = Context(collection, Configure))
        {
            db.Entities.AddRange(
                new FloatHolder { Name = "nan", Value = new Floats { D = double.NaN, F = float.NaN } },
                new FloatHolder { Name = "pinf", Value = new Floats { D = double.PositiveInfinity, F = float.PositiveInfinity } },
                new FloatHolder { Name = "ninf", Value = new Floats { D = double.NegativeInfinity, F = float.NegativeInfinity } },
                new FloatHolder { Name = "zero", Value = new Floats { D = -0.0, F = 0f } });
            db.SaveChanges();
        }

        var stored = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "nan")).Single();
        Assert.True(double.IsNaN(stored["Value"]["D"].AsDouble));

        NativeModeAssert.NativeAndExpected(m => Query(collection, Configure, m, q => q.OrderBy(h => h.Name).ToList()
                .Select(h => $"{h.Name}:{h.Value.D}:{h.Value.F}")),
            // -0.0 is stored as +0 (measured: same for a root double; the BSON writer path, not complex-specific).
            ["nan:NaN:NaN", "ninf:-∞:-∞", "pinf:∞:∞", "zero:0:0"]);
        NativeModeAssert.NativeAndExpected(m => Query(collection, Configure, m, q => q.Where(h => h.Value.D > 1e308).Select(h => h.Name)), ["pinf"]);
        NativeModeAssert.NativeAndExpected(m => Query(collection, Configure, m, q => q.Where(h => h.Value.F < -1e30f).Select(h => h.Name)), ["ninf"]);
        // The server orders NaN below every number (C# compares NaN false both ways): the documented server semantics.
        NativeModeAssert.NativeAndExpected(m => Query(collection, Configure, m, q => q.OrderBy(h => h.Value.D).Select(h => h.Name)),
            ["nan", "ninf", "zero", "pinf"]);
        // double.IsNaN has no translation in any mode (root scalars too); loud, never rows. `D != D` is the NaN test the
        // server cannot express either, so the supported spelling is an ordering bound.
        PerMode(m => Query(collection, Configure, m, q => q.Where(h => double.IsNaN(h.Value.D)).Select(h => h.Name)), [],
            NotNative, "Expression not supported: IsNaN", "Expression not supported: IsNaN");
    }

    public class ConvertedPair
    {
        public int A { get; set; }
        public int B { get; set; }
    }

    public class ConvertedHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public ConvertedPair Left { get; set; } = null!;
        public ConvertedPair Right { get; set; } = null!;
    }

    [Fact]
    public void Value_converted_leaves_compare_stored_alike_or_decline()
    {
        // Stored-alike rule: two stored complex values compare member-wise only when each pair of same-named leaves is
        // stored the same way. Left.A and Right.A share a converter (alike); Left.B is converted and Right.B is not.
        var collection = Collection<ConvertedHolder>();
        static void Configure(ModelBuilder mb)
            => mb.Entity<ConvertedHolder>(e =>
            {
                e.ComplexProperty(h => h.Left, l =>
                {
                    l.Property(x => x.A).HasConversion<string>();
                    l.Property(x => x.B).HasConversion<string>();
                });
                e.ComplexProperty(h => h.Right, r => r.Property(x => x.A).HasConversion<string>());
            });

        using (var db = Context(collection, Configure))
        {
            db.Entities.AddRange(
                new ConvertedHolder { Name = "same", Left = new ConvertedPair { A = 1, B = 2 }, Right = new ConvertedPair { A = 1, B = 2 } },
                new ConvertedHolder { Name = "diff", Left = new ConvertedPair { A = 1, B = 2 }, Right = new ConvertedPair { A = 3, B = 2 } });
            db.SaveChanges();
        }

        var stored = Raw(collection).Find(Builders<BsonDocument>.Filter.Eq("Name", "same")).Single();
        Assert.Equal(BsonType.String, stored["Left"]["B"].BsonType);
        Assert.Equal(BsonType.Int32, stored["Right"]["B"].BsonType);

        // Leaf vs leaf over converted leaves: a field-to-field comparison of a converted leaf declines natively and the
        // driver refuses it, alike or not (measured: identical for two converted ROOT scalars, pre-existing). Never a
        // stored-form answer ("2" vs 2).
        PerMode(m => Query(collection, Configure, m, q => q.Where(h => h.Left.A == h.Right.A).Select(h => h.Name)), ["same"],
            NotNative, "because the two arguments are serialized differently", "because the two arguments are serialized differently");
        PerMode(m => Query(collection, Configure, m, q => q.Where(h => h.Left.B == h.Right.B).OrderBy(h => h.Name).Select(h => h.Name)),
            ["diff", "same"], NotNative, "because the two arguments are serialized differently", "because the two arguments are serialized differently");

        // Whole values: B is not stored alike on both sides, so member-wise equality declines (ruling: stored-alike).
        PerMode(m => Query(collection, Configure, m, q => q.Where(h => h.Left == h.Right).Select(h => h.Name)), ["same"],
            NotNative, "because the two arguments are serialized differently", "because the two arguments are serialized differently");

        // Against a captured instance each leaf is serialized through its own converter: native and right.
        var value = new ConvertedPair { A = 3, B = 2 };
        PerMode(m => Query(collection, Configure, m, q => q.Where(h => h.Right == value).Select(h => h.Name)), ["diff"],
            Serves, Serves, "as a whole value is not supported");
        PerMode(m => Query(collection, Configure, m, q => q.Where(h => h.Left == value).Select(h => h.Name)), [],
            Serves, Serves, "as a whole value is not supported");
    }

    // ── Part 2(a): the projection binder never turns an untranslatable leaf into default(T) ──────────────────────

    public struct AGeo
    {
        public double Lat { get; set; }
        public int? Alt { get; set; }
    }

    public class AValue
    {
        public string City { get; set; } = null!;
        public int Floor { get; set; }
        public int? Zip { get; set; }
        public AGeo Geo { get; set; }
    }

    [System.ComponentModel.DataAnnotations.Schema.ComplexType]
    public class AOwnedSpot
    {
        public string Label { get; set; } = null!;
        public int Row { get; set; }
    }

    public class AOwned
    {
        public string Name { get; set; } = null!;
        public AOwnedSpot Spot { get; set; } = null!;
    }

    public class ARoot
    {
        public ObjectId Id { get; set; }
        public string Title { get; set; } = null!;
        public AValue Value { get; set; } = null!;
        public AGeo Pin { get; set; }
        public AOwned Owned { get; set; } = null!;
#if !EF8 && !EF9
        public AValue? Optional { get; set; }
#endif
    }

    private static void ConfigureAudit(ModelBuilder mb)
        => mb.Entity<ARoot>(e =>
        {
            e.ComplexProperty(r => r.Value, v => v.ComplexProperty(x => x.Geo));
            e.ComplexProperty(r => r.Pin);
            e.OwnsOne(r => r.Owned);
#if !EF8 && !EF9
            e.ComplexProperty(r => r.Optional, v => v.ComplexProperty(x => x.Geo));
#endif
        });

    private IMongoCollection<ARoot> SeedAudit(string name)
    {
        var collection = Collection<ARoot>(name);
        static BsonDocument Value(string city, int floor, BsonValue zip, double lat)
            => new() { { "City", city }, { "Floor", floor }, { "Zip", zip }, { "Geo", new BsonDocument { { "Lat", lat }, { "Alt", BsonNull.Value } } } };
        Raw(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Title", "a" }, { "Value", Value("Oslo", 2, 7, 1.5) },
                { "Pin", new BsonDocument { { "Lat", 9.5 }, { "Alt", 3 } } },
                { "Owned", new BsonDocument { { "Name", "o1" }, { "Spot", new BsonDocument { { "Label", "L1" }, { "Row", 1 } } } } },
                { "Optional", Value("Opt", 4, BsonNull.Value, 4.5) }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Title", "b" }, { "Value", Value("Rome", 5, BsonNull.Value, 2.5) },
                { "Pin", new BsonDocument { { "Lat", 8.5 }, { "Alt", BsonNull.Value } } },
                { "Owned", new BsonDocument { { "Name", "o2" }, { "Spot", new BsonDocument { { "Label", "L2" }, { "Row", 2 } } } } }
                // Optional missing.
            }
        ]);
        return collection;
    }

    // Each spelling: the hand-written answer. A shape either serves exactly these rows in every mode, or declines
    // natively (Native/DriverLinq then serve them or fail loudly); it must never return default/null rows.
    public static readonly Dictionary<string, (Func<IQueryable<ARoot>, IEnumerable<string>> Run, string[] Expected)> AuditShapes = new()
    {
        ["efprop_complex"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = EF.Property<string>(EF.Property<AValue>(r, "Value"), "City") }).ToList().Select(x => x.V), ["Oslo", "Rome"]),
        ["efprop_on_member_hop"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = EF.Property<int>(r.Value, "Floor") }).ToList().Select(x => x.V.ToString()), ["2", "5"]),
        ["efprop_struct_receiver"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = EF.Property<double>(r.Pin, "Lat") }).ToList().Select(x => x.V.ToString()), ["9.5", "8.5"]),
        ["efprop_nested_struct"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = EF.Property<double>(EF.Property<AGeo>(EF.Property<AValue>(r, "Value"), "Geo"), "Lat") }).ToList().Select(x => x.V.ToString()), ["1.5", "2.5"]),
        ["bare_efprop_nested_struct"] = (q => q.OrderBy(r => r.Title).Select(r => EF.Property<double>(EF.Property<AGeo>(EF.Property<AValue>(r, "Value"), "Geo"), "Lat")).ToList().Select(x => x.ToString()), ["1.5", "2.5"]),
        ["widening_cast"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = (long)r.Value.Floor }).ToList().Select(x => x.V.ToString()), ["2", "5"]),
        ["nullable_lift"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = (int?)r.Value.Floor }).ToList().Select(x => x.V?.ToString() ?? "-"), ["2", "5"]),
        ["nullable_leaf"] = (q => q.OrderBy(r => r.Title).Select(r => new { r.Value.Zip, r.Pin.Alt }).ToList().Select(x => $"{x.Zip?.ToString() ?? "-"}/{x.Alt?.ToString() ?? "-"}"), ["7/3", "-/-"]),
        ["nullable_getvalueordefault"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = r.Value.Zip.GetValueOrDefault() }).ToList().Select(x => x.V.ToString()), ["7", "0"]),
        ["nullable_coalesce"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = r.Value.Zip ?? -1 }).ToList().Select(x => x.V.ToString()), ["7", "-1"]),
        ["struct_hop_whole"] = (q => q.OrderBy(r => r.Title).Select(r => new { r.Value.Geo }).ToList().Select(x => x.Geo.Lat.ToString()), ["1.5", "2.5"]),
        ["owned_then_complex"] = (q => q.OrderBy(r => r.Title).Select(r => new { r.Owned.Spot.Label, r.Owned.Name }).ToList().Select(x => x.Label + x.Name), ["L1o1", "L2o2"]),
        ["efprop_owned_then_complex"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = EF.Property<int>(EF.Property<AOwnedSpot>(r.Owned, "Spot"), "Row") }).ToList().Select(x => x.V.ToString()), ["1", "2"]),
        ["efprop_unmapped_leaf"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = EF.Property<string>(r.Value, "Nope") }).ToList().Select(x => x.V ?? "<null>"), ["<null>", "<null>"]),
        ["method_over_leaf"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = r.Value.City.ToUpper() + r.Value.Geo.Lat }).ToList().Select(x => x.V), ["OSLO1.5", "ROME2.5"]),
        ["conditional_over_leaves"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = r.Value.Zip == null ? r.Value.City : r.Owned.Name }).ToList().Select(x => x.V), ["o1", "Rome"]),
#if !EF8 && !EF9
        ["optional_member"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = r.Optional!.City }).ToList().Select(x => x.V ?? "<null>"), ["Opt", "<null>"]),
        ["optional_struct_leaf"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = (double?)r.Optional!.Geo.Lat }).ToList().Select(x => x.V?.ToString() ?? "<null>"), ["4.5", "<null>"]),
        ["optional_efprop"] = (q => q.OrderBy(r => r.Title).Select(r => new { V = EF.Property<string>(EF.Property<AValue>(r, "Optional"), "City") }).ToList().Select(x => x.V ?? "<null>"), ["Opt", "<null>"]),
#endif
    };

    public static TheoryData<string> AuditShapeNames => [.. AuditShapes.Keys];

    [Theory]
    [MemberData(nameof(AuditShapeNames))]
    public void Untranslatable_or_unusual_leaf_spellings_never_read_default_rows(string shape)
    {
        var collection = SeedAudit(nameof(Untranslatable_or_unusual_leaf_spellings_never_read_default_rows) + shape);
        var (run, expected) = AuditShapes[shape];

        var outcomes = new List<string>();
        foreach (var mode in AllModes)
        {
            List<string>? rows = null;
            Exception? error = null;
            try
            {
                rows = Query(collection, ConfigureAudit, mode, run);
            }
            catch (Exception e) when (e is not Xunit.Sdk.XunitException)
            {
                error = e;
            }

            outcomes.Add($"{mode}={(error == null ? string.Join(";", rows!) : error.GetType().Name + ": " + error.Message)}");
            if (error == null)
            {
                Assert.True(expected.SequenceEqual(rows!), $"{shape} [{mode}] served wrong rows [{string.Join(";", rows!)}], expected [{string.Join(";", expected)}]");
            }
            else if (mode == MongoQueryMode.NativeOnly)
            {
                Assert.True(error is MongoDB.EntityFrameworkCore.Query.NativeTranslation.NativeTranslationNotSupportedException,
                    $"{shape} [NativeOnly]: a decline must be NativeTranslationNotSupportedException, got {error.GetType().Name}: {error.Message}");
            }
        }

        var served = AllModes.Select(m => $"{m}={string.Join(";", expected)}").ToArray();
        if (DecliningAuditShapes.TryGetValue(shape, out var fallback))
        {
            Assert.Equal(NativeOnlyDecline, outcomes[0]);
            if (fallback == null)
            {
                Assert.Equal(served[1..], outcomes[1..]);
            }
            else
            {
                Assert.All(outcomes[1..], o => Assert.Contains(fallback, o));
            }
        }
        else
        {
            Assert.Equal(served, outcomes.ToArray());
        }
    }

    // Measured: shapes that decline natively (null = the fallback serves the hand-written rows; otherwise the fallback
    // fails loudly with this fragment). None returns default rows; the rest are native and right in every mode.
    private static readonly Dictionary<string, string?> DecliningAuditShapes = new()
    {
#if EF8 || EF9
        // EF8/EF9 only: the root is wrapped in the owned navigation's auto-include (see OwnedIncludeNativeOnly).
        ["nullable_leaf"] = null,
        ["nullable_coalesce"] = null,
        ["nullable_lift"] = null,
        ["widening_cast"] = null,
        ["conditional_over_leaves"] = null,
        ["struct_hop_whole"] = "FormatException: An error occurred while deserializing the Geo property",
#endif
        ["efprop_struct_receiver"] = null,
        ["efprop_nested_struct"] = null,
        ["bare_efprop_nested_struct"] = null,
        ["efprop_unmapped_leaf"] = null,
        ["method_over_leaf"] = null,
        ["nullable_getvalueordefault"] = "GetValueOrDefault()) because unable to determine which serializer to use for the result."
    };
}
