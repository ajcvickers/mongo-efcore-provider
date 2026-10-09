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

// Bulk ExecuteUpdate/ExecuteDelete exist from EF9 (EF8: BulkOperationsUnsupportedOnEf8Tests pins the unsupported boundary,
// including complex shapes). Optional complex properties are EF10 only.
#if !EF8
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// <c>ExecuteUpdate</c>/<c>ExecuteDelete</c> over single complex properties (bulk operations run on the driver-LINQ bridge:
/// owner decision). Every row is asserted on the RAW stored documents (each untouched document must equal its seed exactly)
/// and on the returned affected count; filters are also checked against the same query (the bulk operation must select
/// exactly the rows the query returns).
/// </summary>
[XUnitCollection("UpdateTests")]
public class ComplexTypeBulkTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class BAddr
    {
        public string City { get; set; } = null!;
        public string? Street { get; set; }
        public int Code { get; set; }
        public int Rep { get; set; }
        public GeoPoint Location { get; set; }
    }

    public class BCustomer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public BAddr Billing { get; set; } = null!;
        public BAddr Shipping { get; set; } = null!;
        public GeoPoint Pin { get; set; }
    }

    // Billing: stored as "bill", its City as "town", its Rep with a string BsonRepresentation. Shipping: Code through a
    // string value converter. Same CLR type at two paths, each configured independently.
    private static void Configure(ModelBuilder mb)
        => mb.Entity<BCustomer>(e =>
        {
            e.ComplexProperty(c => c.Billing, a =>
            {
                a.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "bill");
                a.Property(x => x.City).Metadata.SetElementName("town");
                a.Property(x => x.Rep).Metadata.SetBsonRepresentation(BsonType.String, null, null);
                a.ComplexProperty(x => x.Location);
            });
            e.ComplexProperty(c => c.Shipping, a =>
            {
                a.Property(x => x.Code).HasConversion<string>();
                a.ComplexProperty(x => x.Location);
            });
            e.ComplexProperty(c => c.Pin);
        });

    private static BsonDocument Geo(double lat, double lon) => new() { { "Lat", lat }, { "Lon", lon } };

    private static BsonDocument Bill(string town, int code, int rep, double lat)
        => new() { { "town", town }, { "Street", "b-st" }, { "Code", code }, { "Rep", rep.ToString() }, { "Location", Geo(lat, 0) } };

    private static BsonDocument Ship(string city, int code, double lat)
        => new() { { "City", city }, { "Street", BsonNull.Value }, { "Code", code.ToString() }, { "Rep", 0 }, { "Location", Geo(lat, 0) } };

    /// <summary>
    /// a: bill X (lat 1.5), ship Y, pin (1, 2), rank 1. b: bill Z (lat 3.5), ship X, pin (3, 4), rank 2. c: bill X (lat 2.5),
    /// ship X, rank 3, plus a DECOY element under the CLR names ("Billing": {"City": "Z"}) that only a CLR-name lookup reads.
    /// </summary>
    private static BsonDocument[] SeedDocs()
    {
        var c = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "c" }, { "Rank", 3 }, { "bill", Bill("X", 5, 50, 2.5) },
            { "Shipping", Ship("X", 6, 6.5) }, { "Pin", Geo(5, 6) }, { "Billing", new BsonDocument("City", "Z") }
        };
        return
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Rank", 1 }, { "bill", Bill("X", 1, 10, 1.5) },
                { "Shipping", Ship("Y", 2, 2.5) }, { "Pin", Geo(1, 2) }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "b" }, { "Rank", 2 }, { "bill", Bill("Z", 3, 30, 3.5) },
                { "Shipping", Ship("X", 4, 4.5) }, { "Pin", Geo(3, 4) }
            },
            c
        ];
    }

    private sealed class Store(IMongoCollection<BCustomer> collection, BsonDocument[] seed)
    {
        public BsonDocument[] Seed { get; } = seed;

        public IMongoCollection<BsonDocument> Raw { get; }
            = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

        public SingleEntityDbContext<BCustomer> Context(MongoQueryMode mode = MongoQueryMode.Native)
            => SingleEntityDbContext.Create(collection, Configure, null, b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

        /// <summary>
        /// The stored documents equal the seed with exactly the given per-document edits applied (hand-written), and no
        /// other document changed; documents in <paramref name="deleted"/> are gone.
        /// </summary>
        public void AssertStored(string[]? deleted = null, params (string Name, Action<BsonDocument> Edit)[] edits)
        {
            var expected = new List<BsonDocument>();
            foreach (var document in Seed)
            {
                var name = document["Name"].AsString;
                if (deleted?.Contains(name) == true)
                {
                    continue;
                }

                var copy = document.DeepClone().AsBsonDocument;
                foreach (var edit in edits.Where(e => e.Name == name))
                {
                    edit.Edit(copy);
                }

                expected.Add(copy);
            }

            var actual = Raw.Find(FilterDefinition<BsonDocument>.Empty).ToList().OrderBy(d => d["_id"]).ToList();
            Assert.Equal(expected.OrderBy(d => d["_id"]).Select(Canonical), actual.Select(Canonical));
        }

        // Element order inside a (sub)document is not semantic (a whole-value write emits the writer's order).
        private static string Canonical(BsonDocument document)
        {
            static BsonValue Sort(BsonValue value)
                => value switch
                {
                    BsonDocument d => new BsonDocument(d.Elements.OrderBy(e => e.Name, StringComparer.Ordinal).Select(e => new BsonElement(e.Name, Sort(e.Value)))),
                    BsonArray a => new BsonArray(a.Select(Sort)),
                    _ => value
                };

            return Sort(document).ToJson();
        }

        public void AssertUnchanged() => AssertStored();
    }

    private Store Seed([CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<BCustomer>(name + Guid.NewGuid().ToString("N")[..6]);
        var seed = SeedDocs();
        collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName).InsertMany(seed.Select(d => d.DeepClone().AsBsonDocument));
        return new Store(collection, seed);
    }

    // ── SetProperty over complex leaves ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetProperty_on_a_complex_leaf_writes_the_stored_element_path()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            Assert.Equal(3, db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Billing.City, "Q")));
        }

        // HasElementName on the complex property ("bill") and on the leaf ("town"): the CLR-named decoy is untouched.
        store.AssertStored(null, ("a", d => d["bill"]["town"] = "Q"), ("b", d => d["bill"]["town"] = "Q"), ("c", d => d["bill"]["town"] = "Q"));
    }

    [Fact]
    public void SetProperty_on_a_nested_and_a_struct_leaf()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            Assert.Equal(1, db.Entities.Where(c => c.Name == "a").ExecuteUpdate(s => s.SetProperty(c => c.Billing.Location.Lat, 7.25)));
            Assert.Equal(1, db.Entities.Where(c => c.Name == "b").ExecuteUpdate(s => s.SetProperty(c => c.Pin.Lon, -1.0)));
        }

        store.AssertStored(null, ("a", d => d["bill"]["Location"]["Lat"] = 7.25), ("b", d => d["Pin"]["Lon"] = -1.0));
    }

    [Fact]
    public void Multiple_setters_mixing_complex_leaves_and_root_scalars()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            Assert.Equal(2, db.Entities.Where(c => c.Rank >= 2).ExecuteUpdate(s => s
                .SetProperty(c => c.Billing.Street, "new-st")
                .SetProperty(c => c.Shipping.City, "S")
                .SetProperty(c => c.Rank, 9)));
        }

        store.AssertStored(null,
            ("b", d => { d["bill"]["Street"] = "new-st"; d["Shipping"]["City"] = "S"; d["Rank"] = 9; }),
            ("c", d => { d["bill"]["Street"] = "new-st"; d["Shipping"]["City"] = "S"; d["Rank"] = 9; }));
    }

    [Fact]
    public void Self_referencing_setters_read_and_write_complex_leaves()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            // A leaf from a root scalar, a root scalar from a leaf (read through its stored path), a leaf from another
            // complex property's leaf, and a computed nested leaf.
            Assert.Equal(3, db.Entities.ExecuteUpdate(s => s
                .SetProperty(c => c.Billing.City, c => c.Name)
                .SetProperty(c => c.Name, c => c.Billing.City + "/" + c.Shipping.City)
                .SetProperty(c => c.Shipping.Street, c => c.Billing.Street)
                .SetProperty(c => c.Pin.Lat, c => c.Billing.Location.Lat * 2)));
        }

        // Every value is computed from the PRE-update document ($set in one pipeline stage).
        store.AssertStored(null,
            ("a", d => { d["bill"]["town"] = "a"; d["Name"] = "X/Y"; d["Shipping"]["Street"] = "b-st"; d["Pin"]["Lat"] = 3.0; }),
            ("b", d => { d["bill"]["town"] = "b"; d["Name"] = "Z/X"; d["Shipping"]["Street"] = "b-st"; d["Pin"]["Lat"] = 7.0; }),
            ("c", d => { d["bill"]["town"] = "c"; d["Name"] = "X/X"; d["Shipping"]["Street"] = "b-st"; d["Pin"]["Lat"] = 5.0; }));
    }

    [Fact]
    public void Constant_setters_beside_a_self_referencing_one_are_written_as_literals()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            // The pipeline form: a "$"-prefixed string set on a complex leaf stays data (not a field path).
            Assert.Equal(1, db.Entities.Where(c => c.Name == "a").ExecuteUpdate(s => s
                .SetProperty(c => c.Billing.City, "$Name")
                .SetProperty(c => c.Rank, c => c.Rank + 1)));
        }

        store.AssertStored(null, ("a", d => { d["bill"]["town"] = "$Name"; d["Rank"] = 2; }));
    }

    [Fact]
    public void Converted_and_represented_leaves_are_written_in_their_stored_form()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            Assert.Equal(1, db.Entities.Where(c => c.Name == "a").ExecuteUpdate(s => s
                .SetProperty(c => c.Shipping.Code, 42)
                .SetProperty(c => c.Billing.Rep, 77)));
        }

        // HasConversion<string>() stores "42"; BsonRepresentation(String) stores "77".
        store.AssertStored(null, ("a", d => { d["Shipping"]["Code"] = "42"; d["bill"]["Rep"] = "77"; }));
    }

    [Fact]
    public void Self_referencing_setter_on_a_represented_leaf_is_refused()
    {
        // Measured at 5eece0ae: `SetProperty(c => c.Billing.Rep, c => c.Rank)` wrote the int 1 into "bill.Rep", stored
        // everywhere else as a string (BsonRepresentation String). Now refused like the converter case.
        var store = Seed();
        using (var db = store.Context())
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Billing.Rep, c => c.Rank)));
            Assert.Contains("Self-referencing ExecuteUpdate on property 'Rep'", ex.Message);
            Assert.Contains("BsonRepresentation", ex.Message);
        }

        store.AssertUnchanged();
    }

    public class RepRoot
    {
        public ObjectId Id { get; set; }
        public int Rank { get; set; }
        public int Rep { get; set; }
    }

    [Fact]
    public void Self_referencing_setter_on_a_represented_ROOT_scalar_writes_the_computed_type_PRE_EXISTING()
    {
        // Characterization (pre-existing at 6740680c and before this task; unchanged; Jira candidate 18): a root scalar
        // stored with BsonRepresentation(String) receives the computed value in its own BSON type (int 3, not "3"). Only the
        // complex-leaf case is refused above, so root behaviour is untouched.
        var collection = database.CreateCollection<RepRoot>(nameof(Self_referencing_setter_on_a_represented_ROOT_scalar_writes_the_computed_type_PRE_EXISTING) + Guid.NewGuid().ToString("N")[..6]);
        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        var id = ObjectId.GenerateNewId();
        raw.InsertOne(new BsonDocument { { "_id", id }, { "Rank", 3 }, { "Rep", "1" } });
        using (var db = SingleEntityDbContext.Create(collection, mb => mb.Entity<RepRoot>().Property(r => r.Rep).Metadata.SetBsonRepresentation(BsonType.String, null, null)))
        {
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s.SetProperty(r => r.Rep, r => r.Rank)));
        }

        Assert.Equal(new BsonDocument { { "_id", id }, { "Rank", 3 }, { "Rep", 3 } }.ToJson(), raw.Find(FilterDefinition<BsonDocument>.Empty).Single().ToJson());
    }

    [Fact]
    public void SetProperty_on_a_nullable_leaf_to_null_stores_BSON_null()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            Assert.Equal(1, db.Entities.Where(c => c.Name == "a").ExecuteUpdate(s => s.SetProperty(c => c.Billing.Street, (string?)null)));
        }

        store.AssertStored(null, ("a", d => d["bill"]["Street"] = BsonNull.Value));
    }

    [Fact]
    public void Self_referencing_setter_on_a_converted_leaf_is_refused()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Shipping.Code, c => c.Rank)));
            Assert.Contains("Self-referencing ExecuteUpdate on property 'Code'", ex.Message);
            Assert.Contains("value converter", ex.Message);
        }

        store.AssertUnchanged();
    }

    // ── SetProperty over a whole complex property ───────────────────────────────────────────────────────────────────

    [Fact]
    public void SetProperty_on_a_whole_complex_property_writes_the_value_as_SaveChanges_would()
    {
        var store = Seed();
        var replacement = new BAddr { City = "N", Street = null, Code = 8, Rep = 80, Location = new GeoPoint { Lat = 9.5, Lon = -9.5 } };
        using (var db = store.Context())
        {
            Assert.Equal(2, db.Entities.Where(c => c.Billing.City == "X").ExecuteUpdate(s => s
                .SetProperty(c => c.Billing, replacement)
                .SetProperty(c => c.Shipping, new BAddr { City = "M", Code = 3, Location = new GeoPoint { Lat = 1, Lon = 1 } })));
        }

        // Billing's element names ("town") and representation ("80"), Shipping's converter ("3"); a null leaf is stored as
        // null; the whole subdocument is replaced (element order as the writer emits it).
        var bill = new BsonDocument { { "town", "N" }, { "Street", BsonNull.Value }, { "Code", 8 }, { "Rep", "80" }, { "Location", Geo(9.5, -9.5) } };
        var ship = new BsonDocument { { "City", "M" }, { "Street", BsonNull.Value }, { "Code", "3" }, { "Rep", 0 }, { "Location", Geo(1, 1) } };
        store.AssertStored(null,
            ("a", d => { d["bill"] = bill.DeepClone(); d["Shipping"] = ship.DeepClone(); }),
            ("c", d => { d["bill"] = bill.DeepClone(); d["Shipping"] = ship.DeepClone(); }));

        using var read = store.Context(MongoQueryMode.NativeOnly);
        Assert.Equal(["N/8/80/9.5", "Z/3/30/3.5", "N/8/80/9.5"],
            read.Entities.AsNoTracking().OrderBy(c => c.Name).ToList().Select(c => $"{c.Billing.City}/{c.Billing.Code}/{c.Billing.Rep}/{c.Billing.Location.Lat}"));
    }

    [Fact]
    public void SetProperty_on_a_whole_struct_complex_property()
    {
        var store = Seed();
        var pin = new GeoPoint { Lat = -4, Lon = 4 };
        using (var db = store.Context())
        {
            Assert.Equal(1, db.Entities.Where(c => c.Pin.Lat == 3).ExecuteUpdate(s => s.SetProperty(c => c.Pin, pin)));
        }

        store.AssertStored(null, ("b", d => d["Pin"] = Geo(-4, 4)));
    }

    [Fact]
    public void SetProperty_on_a_required_complex_property_to_null_is_refused_and_writes_nothing()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Billing, (BAddr)null!)));
            Assert.Contains("cannot set the required complex property", ex.Message);
            Assert.Contains("Billing' to null. No document was modified.", ex.Message);
        }

        store.AssertUnchanged();
    }

    [Fact]
    public void SetProperty_on_a_whole_complex_property_from_another_is_refused()
    {
        // Decline ruling: copying the STORED subdocument would carry the source property's element names / converters /
        // representations (Shipping stores City as "City" and Code as a string; Billing expects "town" and an int), and the
        // driver cannot render a whole complex value (R1). Refused at translation, before any write.
        var store = Seed();
        using (var db = store.Context())
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Billing, c => c.Shipping)));
            Assert.Contains("could not be translated", ex.Message);
            Assert.Contains("cannot set the complex property", ex.Message);
            Assert.Contains("from a value that reads the entity being updated", ex.Message);
        }

        store.AssertUnchanged();
    }

    // ── Filters over complex properties ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The bulk invariant: ExecuteUpdate and ExecuteDelete select EXACTLY the rows the same query returns (NativeOnly: the
    /// query runs natively), and those are the hand-written rows. Each operation runs on a fresh seed.
    /// </summary>
    private void AssertBulkSelectsTheQueryRows(Expression<Func<BCustomer, bool>> predicate, string[] expected, [CallerMemberName] string name = "")
    {
        var queryStore = Seed(name);
        using (var db = queryStore.Context(MongoQueryMode.NativeOnly))
        {
            Assert.Equal(expected, db.Entities.AsNoTracking().Where(predicate).Select(c => c.Name).ToList().Order(StringComparer.Ordinal));
        }

        var updateStore = Seed(name);
        using (var db = updateStore.Context())
        {
            Assert.Equal(expected.Length, db.Entities.Where(predicate).ExecuteUpdate(s => s.SetProperty(c => c.Rank, 100)));
        }

        updateStore.AssertStored(null, [.. expected.Select(n => (n, (Action<BsonDocument>)(d => d["Rank"] = 100)))]);

        var deleteStore = Seed(name);
        using (var db = deleteStore.Context())
        {
            Assert.Equal(expected.Length, db.Entities.Where(predicate).ExecuteDelete());
        }

        deleteStore.AssertStored(expected);
    }

    [Fact]
    public void ExecuteDelete_and_ExecuteUpdate_filter_on_complex_leaves()
    {
        // c's CLR-named decoy ("Billing": {"City": "Z"}) would match `== "Z"` if the CLR names were used.
        AssertBulkSelectsTheQueryRows(c => c.Billing.City == "X", ["a", "c"]);
        AssertBulkSelectsTheQueryRows(c => c.Billing.City == "Z", ["b"]);
        AssertBulkSelectsTheQueryRows(c => c.Billing.City != "X", ["b"]);
        AssertBulkSelectsTheQueryRows(c => c.Billing.Location.Lat > 2, ["b", "c"]);
        AssertBulkSelectsTheQueryRows(c => c.Pin.Lat < 2, ["a"]);
        AssertBulkSelectsTheQueryRows(c => c.Shipping.Street == null, ["a", "b", "c"]);
        AssertBulkSelectsTheQueryRows(c => c.Billing.City == c.Shipping.City, ["c"]);
        AssertBulkSelectsTheQueryRows(c => c.Billing.City == "X" && c.Rank > 1, ["c"]);
        AssertBulkSelectsTheQueryRows(c => c.Billing.City.StartsWith("Z") || c.Pin.Lon == 2, ["a", "b"]);
    }

    [Fact]
    public void Filters_on_converted_and_represented_leaves_compare_stored_forms_for_equality()
    {
        AssertBulkSelectsTheQueryRows(c => c.Shipping.Code == 4, ["b"]);
        AssertBulkSelectsTheQueryRows(c => c.Billing.Rep == 50, ["c"]);
    }

    [Fact]
    public void Relational_filters_on_converted_or_represented_leaves_are_refused_and_touch_nothing()
    {
        // EF-337: "2" < "10" as strings; the stored form doesn't order like the int, so the bridge refuses.
        var store = Seed();
        using (var db = store.Context())
        {
            var delete = Assert.Throws<InvalidOperationException>(() => db.Entities.Where(c => c.Shipping.Code > 3).ExecuteDelete());
            Assert.IsType<NotSupportedException>(delete.InnerException);
            Assert.Contains("stored through a value converter or a BsonRepresentation", delete.InnerException!.Message);

            var update = Assert.Throws<InvalidOperationException>(
                () => db.Entities.Where(c => c.Billing.Rep < 40).ExecuteUpdate(s => s.SetProperty(c => c.Rank, 0)));
            Assert.Contains("stored through a value converter or a BsonRepresentation", update.InnerException!.Message);
        }

        store.AssertUnchanged();
    }

    [Fact]
    public void Whole_complex_value_equality_filters_are_refused_and_touch_nothing()
    {
        // Ruling R1: member-wise complex equality is native-only; the bridge's complex serializer refuses it. The bulk path
        // surfaces EF's canonical "could not be translated" with the serializer's NotSupportedException inside.
        var store = Seed();
        var other = new BAddr { City = "X", Street = "b-st", Code = 1, Rep = 10, Location = new GeoPoint { Lat = 1.5 } };
        using (var db = store.Context())
        {
            var captured = Assert.Throws<InvalidOperationException>(() => db.Entities.Where(c => c.Billing == other).ExecuteDelete());
            Assert.Contains("could not be translated", captured.Message);
            Assert.IsType<NotSupportedException>(captured.InnerException);
            Assert.Contains("as a whole value is not supported", captured.InnerException!.Message);

            var stored = Assert.Throws<InvalidOperationException>(
                () => db.Entities.Where(c => c.Billing == c.Shipping).ExecuteUpdate(s => s.SetProperty(c => c.Rank, 0)));
            Assert.Contains("could not be translated", stored.Message);
            Assert.Contains("serialized differently", stored.InnerException!.Message);
        }

        store.AssertUnchanged();

        // The same query is served natively (member-wise): the refusal is the bulk path's limit, not wrong rows.
        using var query = store.Context(MongoQueryMode.NativeOnly);
        Assert.Equal(["a"], query.Entities.AsNoTracking().Where(c => c.Billing == other).Select(c => c.Name).ToList());
    }

    [Fact]
    public void Ordering_and_paging_by_a_complex_leaf_before_a_bulk_operation()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            // Two-phase (target _ids in a transaction, then by _id): ordered by the stored "bill.town", then name.
            Assert.Equal(2, db.Entities.OrderBy(c => c.Billing.City).ThenBy(c => c.Name).Take(2)
                .ExecuteUpdate(s => s.SetProperty(c => c.Billing.Street, "first-two")));
            Assert.Equal(1, db.Entities.OrderByDescending(c => c.Pin.Lat).Take(1).ExecuteDelete());
        }

        // Order by town then name: a (X), c (X), b (Z). By Pin.Lat descending: c (5).
        store.AssertStored(["c"], ("a", d => d["bill"]["Street"] = "first-two"));
    }

    [Fact]
    public async Task ExecuteUpdateAsync_and_ExecuteDeleteAsync_over_complex_leaves()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            Assert.Equal(1, await db.Entities.Where(c => c.Pin.Lon == 4).ExecuteUpdateAsync(s => s.SetProperty(c => c.Billing.City, "async")));
            Assert.Equal(1, await db.Entities.Where(c => c.Billing.Location.Lat < 2).ExecuteDeleteAsync());
        }

        store.AssertStored(["a"], ("b", d => d["bill"]["town"] = "async"));
    }

    // Two setters where one's stored path contains the other's: the server rejects such a `$set` ("would create a
    // conflict", code 40) when the command runs. Refused at translation, in either order, constant or self-referencing.
    public static TheoryData<string> OverlappingSetterShapes => new()
    {
        "whole-then-leaf", "leaf-then-whole", "whole-then-nested-leaf", "nested-whole-then-leaf", "self-referencing-whole-then-leaf"
    };

    [Theory]
    [MemberData(nameof(OverlappingSetterShapes))]
    public void Setters_whose_stored_paths_overlap_are_refused_and_write_nothing(string shape)
    {
        var store = Seed();
        var value = new BAddr { City = "N", Code = 1, Rep = 2, Location = new GeoPoint { Lat = 9, Lon = 9 } };
        using (var db = store.Context())
        {
            var ex = Assert.Throws<InvalidOperationException>(() => shape switch
            {
                "whole-then-leaf" => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Billing, value).SetProperty(c => c.Billing.City, "Q")),
                "leaf-then-whole" => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Billing.City, "Q").SetProperty(c => c.Billing, value)),
                "whole-then-nested-leaf" => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Billing, value).SetProperty(c => c.Billing.Location.Lat, 1.0)),
                "nested-whole-then-leaf" => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Pin, new GeoPoint { Lat = 1, Lon = 1 }).SetProperty(c => c.Pin.Lat, 2.0)),
                "self-referencing-whole-then-leaf" => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Billing, value).SetProperty(c => c.Billing.Street, c => c.Name)),
                _ => throw new ArgumentOutOfRangeException(nameof(shape))
            });
            Assert.Contains("one is stored inside the other", ex.Message);
            Assert.Contains(shape == "nested-whole-then-leaf" ? "'Pin.Lat'" : "'bill'", ex.Message);
        }

        store.AssertUnchanged();
    }

    [Fact]
    public void Setters_on_sibling_paths_and_the_same_leaf_twice_are_not_overlaps()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            // Sibling leaves of one complex property are not an overlap (only a dotted containment is). The same leaf set twice
            // keeps the last value, as before this check (the server accepts it: the update document holds one key).
            Assert.Equal(1, db.Entities.Where(c => c.Name == "a").ExecuteUpdate(s => s
                .SetProperty(c => c.Billing.City, "P")
                .SetProperty(c => c.Billing.Street, "s2")
                .SetProperty(c => c.Billing.City, "Q")));
        }

        store.AssertStored(null, ("a", d => { d["bill"]["town"] = "Q"; d["bill"]["Street"] = "s2"; }));
    }

    [Fact]
    public void Setter_targets_that_are_not_complex_leaves_keep_the_existing_refusal()
    {
        var store = Seed();
        using (var db = store.Context())
        {
            // A method call in the selector is not a member chain: the existing "not a mapped scalar property" refusal.
            var ex = Assert.Throws<InvalidOperationException>(
                () => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Billing.City.Trim(), "x")));
            Assert.Contains("Only mapped root scalar properties can be updated", ex.Message);

            // The setter target is printed as its shape: an inlined literal is replaced by `?`.
            ex = Assert.Throws<InvalidOperationException>(
                () => db.Entities.ExecuteUpdate(s => s.SetProperty(c => c.Billing.City.Substring(0, 3) + "secret-suffix", "x")));
            Assert.DoesNotContain("secret-suffix", ex.Message);
            Assert.Contains(".Billing.City.Substring(?, ?) + ?", ex.Message);
        }

        store.AssertUnchanged();
    }

#if !EF8 && !EF9
    // ── Optional complex property (EF10) ────────────────────────────────────────────────────────────────────────────

    public class OAddr
    {
        public string City { get; set; } = null!;
        public int? Zip { get; set; }
    }

    public class OHolder
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public OAddr? Opt { get; set; }
    }

    private sealed class OptionalStore(IMongoCollection<OHolder> collection, BsonDocument[] seed)
    {
        public BsonDocument[] Seed { get; } = seed;

        public IMongoCollection<BsonDocument> Raw { get; }
            = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

        public SingleEntityDbContext<OHolder> Context(MongoQueryMode mode = MongoQueryMode.Native)
            => SingleEntityDbContext.Create(collection, mb => mb.Entity<OHolder>().ComplexProperty(h => h.Opt), null, b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

        public List<string> Stored()
            => Raw.Find(FilterDefinition<BsonDocument>.Empty).ToList().OrderBy(d => d["Name"].AsString)
                .Select(d => d.Contains("Opt") ? $"{d["Name"]}:{d["Opt"].ToJson()}" : $"{d["Name"]}:<missing>").ToList();
    }

    /// <summary>o-null: Opt BSON null. o-missing: no Opt. o-value: {City X, Zip 5}. o-empty: {} (present, members missing).</summary>
    private OptionalStore SeedOptional([CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<OHolder>(name + Guid.NewGuid().ToString("N")[..6]);
        BsonDocument Row(string n, BsonValue? opt)
        {
            var d = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", n }, { "Rank", 0 } };
            if (opt is not null)
            {
                d["Opt"] = opt;
            }

            return d;
        }

        BsonDocument[] seed =
        [
            Row("o-empty", new BsonDocument()), Row("o-missing", null), Row("o-null", BsonNull.Value),
            Row("o-value", new BsonDocument { { "City", "X" }, { "Zip", 5 } })
        ];
        collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName).InsertMany(seed.Select(d => d.DeepClone().AsBsonDocument));
        return new OptionalStore(collection, seed);
    }

    private static readonly string[] OptionalSeedState =
        ["o-empty:{ }", "o-missing:<missing>", "o-null:null", """o-value:{ "City" : "X", "Zip" : 5 }"""];

    [Fact]
    public void SetProperty_on_a_leaf_under_an_optional_complex_property_is_refused_and_writes_nothing()
    {
        // Measured on the server: `$set: {"Opt.City": v}` fails on a document whose Opt is null ("Cannot create field
        // 'City' in element {Opt: null}") AFTER earlier documents were written; the pipeline form silently CREATES
        // {City: v} under a missing/null parent (a present value whose other members are missing). Neither is the answer to
        // "set a member of a null value", so the target is refused at translation.
        var store = SeedOptional();
        using (var db = store.Context())
        {
            var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(h => h.Opt!.City, "Y")));
            Assert.Contains("could not be translated", ex.Message);
            Assert.Contains("is an optional complex property", ex.Message);

            var selfReferencing = Assert.Throws<InvalidOperationException>(
                () => db.Entities.Where(h => h.Opt != null).ExecuteUpdate(s => s.SetProperty(h => h.Opt!.Zip, h => h.Rank)));
            Assert.Contains("is an optional complex property", selfReferencing.Message);
        }

        Assert.Equal(OptionalSeedState, store.Stored());
    }

    [Fact]
    public void SetProperty_on_an_optional_complex_property_to_null_and_to_a_value()
    {
        var store = SeedOptional();
        using (var db = store.Context())
        {
            Assert.Equal(2, db.Entities.Where(h => h.Opt != null).ExecuteUpdate(s => s.SetProperty(h => h.Opt, (OAddr?)null)));
        }

        // Optional null is written as BSON null (as SaveChanges writes it); o-missing stays missing (not selected).
        Assert.Equal(["o-empty:null", "o-missing:<missing>", "o-null:null", "o-value:null"], store.Stored());

        using (var db = store.Context())
        {
            Assert.Equal(4, db.Entities.ExecuteUpdate(s => s.SetProperty(h => h.Opt, new OAddr { City = "N", Zip = null })));
        }

        Assert.Equal(
            ["""o-empty:{ "City" : "N", "Zip" : null }""", """o-missing:{ "City" : "N", "Zip" : null }""",
                """o-null:{ "City" : "N", "Zip" : null }""", """o-value:{ "City" : "N", "Zip" : null }"""],
            store.Stored());
    }

    [Fact]
    public void Filters_on_an_optional_complex_property_select_the_query_rows()
    {
        // Null vs missing vs {}: `== null` is null AND missing (not {}); a leaf under an absent parent reads null.
        AssertOptional(h => h.Opt == null, ["o-missing", "o-null"]);
        AssertOptional(h => h.Opt != null, ["o-empty", "o-value"]);
        AssertOptional(h => h.Opt!.City == "X", ["o-value"]);
        AssertOptional(h => h.Opt!.City != "X", ["o-empty", "o-missing", "o-null"]);
        AssertOptional(h => h.Opt!.Zip == null, ["o-empty", "o-missing", "o-null"]);
        AssertOptional(h => h.Opt!.Zip < 9, ["o-value"]);
    }

    private void AssertOptional(Expression<Func<OHolder, bool>> predicate, string[] expected, [CallerMemberName] string name = "")
    {
        var query = SeedOptional(name);
        using (var db = query.Context(MongoQueryMode.NativeOnly))
        {
            Assert.Equal(expected, db.Entities.AsNoTracking().Where(predicate).Select(h => h.Name).ToList().Order(StringComparer.Ordinal));
        }

        var update = SeedOptional(name);
        using (var db = update.Context())
        {
            Assert.Equal(expected.Length, db.Entities.Where(predicate).ExecuteUpdate(s => s.SetProperty(h => h.Rank, 1)));
        }

        Assert.Equal(expected, update.Raw.Find(new BsonDocument("Rank", 1)).ToList().Select(d => d["Name"].AsString).Order(StringComparer.Ordinal));

        var delete = SeedOptional(name);
        using (var db = delete.Context())
        {
            Assert.Equal(expected.Length, db.Entities.Where(predicate).ExecuteDelete());
        }

        Assert.Equal(
            delete.Seed.Select(d => d["Name"].AsString).Except(expected).Order(StringComparer.Ordinal),
            delete.Raw.Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d["Name"].AsString).Order(StringComparer.Ordinal));
    }
#endif
}
#endif
