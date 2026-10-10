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
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// The complex collection CLR types EF10 accepts, end to end: save, raw stored shape, read (no-tracking and tracked),
/// replace, in-place mutation, an element quantifier in every query mode, and a bulk update. The source of the supported
/// collection types table in docs/complex-types.md. EF itself rejects the types that do not implement <c>IList&lt;T&gt;</c>
/// (<c>ICollection&lt;T&gt;</c>, <c>IReadOnlyList&lt;T&gt;</c>, <c>IReadOnlyCollection&lt;T&gt;</c>, <c>IEnumerable&lt;T&gt;</c>,
/// <c>HashSet&lt;T&gt;</c>); the provider refuses <c>T[]</c> and <c>ReadOnlyCollection&lt;T&gt;</c>, which EF accepts but
/// cannot read or save (unit tests: ComplexCollectionClrTypeValidationTests).
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexCollectionClrTypeTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class PTag { public string Label { get; set; } = ""; }
    public class TagList : List<PTag>;

    public class H<TC> where TC : class, IEnumerable<PTag>
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public TC Items { get; set; } = null!;
    }

    private const string BulkRefused = "whose CLR type cannot hold a List<T>";

    [Fact]
    public void List_round_trips_queries_and_bulk_updates()
        => AssertWorks<List<PTag>>(l => l, bulkServed: true, driverQueryServed: true);

    [Fact]
    public void IList_round_trips_queries_and_bulk_updates()
        => AssertWorks<IList<PTag>>(l => l, bulkServed: true, driverQueryServed: true);

    // Bulk operations are refused: the bridge cannot coalesce a null array into these types.
    [Fact]
    public void ObservableCollection_round_trips_and_queries_bulk_is_refused()
        => AssertWorks<ObservableCollection<PTag>>(l => new ObservableCollection<PTag>(l), bulkServed: false, driverQueryServed: true);

    [Fact]
    public void Collection_round_trips_and_queries_bulk_is_refused()
        => AssertWorks<Collection<PTag>>(l => new Collection<PTag>(l), bulkServed: false, driverQueryServed: true);

    // A non-generic List<T> subclass: the driver-LINQ path fails loudly ("only valid on generic types"); native serves.
    [Fact]
    public void List_subclass_round_trips_native_queries_serve_driver_linq_fails_loudly_bulk_is_refused()
        => AssertWorks<TagList>(l => { var t = new TagList(); t.AddRange(l); return t; }, bulkServed: false, driverQueryServed: false);

    private void AssertWorks<TC>(Func<List<PTag>, TC> make, bool bulkServed, bool driverQueryServed) where TC : class, IEnumerable<PTag>
    {
        var collection = database.CreateCollection<H<TC>>(TemporaryDatabaseFixtureBase.CreateCollectionName(typeof(TC).Name) + Guid.NewGuid().ToString("N")[..8]);
        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        SingleEntityDbContext<H<TC>> Ctx(MongoQueryMode m) => SingleEntityDbContext.Create(collection, mb => mb.Entity<H<TC>>().ComplexCollection(h => h.Items), null,
            b => { b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)); new MongoDbContextOptionsBuilder(b).UseQueryMode(m); });
        BsonValue StoredItems(string name) => raw.Find(Builders<BsonDocument>.Filter.Eq("Name", name)).Single()["Items"];

        using (var db = Ctx(MongoQueryMode.Native))
        {
            db.Entities.Add(new H<TC> { Name = "a", Items = make([new PTag { Label = "x" }]) });
            db.SaveChanges();
        }

        Assert.Equal("""[{ "Label" : "x" }]""", StoredItems("a").ToJson());
        raw.InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "b" }, { "Items", new BsonArray { new BsonDocument("Label", "y") } } });

        using (var db = Ctx(MongoQueryMode.Native))
        {
            Assert.Equal(["a=x", "b=y"], db.Entities.AsNoTracking().ToList().Select(h => h.Name + "=" + string.Join("+", h.Items.Select(i => i.Label))).Order());
            var b = db.Entities.Single(h => h.Name == "b");
            Assert.IsType<TC>(b.Items, exactMatch: false);
            b.Items = make([new PTag { Label = "y" }, new PTag { Label = "z" }]);
            db.SaveChanges();
        }

        Assert.Equal("""[{ "Label" : "y" }, { "Label" : "z" }]""", StoredItems("b").ToJson());

        using (var db = Ctx(MongoQueryMode.Native))
        {
            ((ICollection<PTag>)db.Entities.Single(h => h.Name == "b").Items).Add(new PTag { Label = "w" });
            db.SaveChanges();
        }

        Assert.Equal("""[{ "Label" : "y" }, { "Label" : "z" }, { "Label" : "w" }]""", StoredItems("b").ToJson());

        List<string> Query(MongoQueryMode m)
        {
            using var db = Ctx(m);
            return db.Entities.Where(h => h.Items.Any(i => i.Label == "y")).Select(h => h.Name).ToList();
        }

        Assert.Equal(["b"], Query(MongoQueryMode.NativeOnly));
        Assert.Equal(["b"], Query(MongoQueryMode.Native));
        if (driverQueryServed)
        {
            Assert.Equal(["b"], Query(MongoQueryMode.DriverLinq));
        }
        else
        {
            var ex = Assert.ThrowsAny<Exception>(() => Query(MongoQueryMode.DriverLinq));
            Assert.Contains("This operation is only valid on generic types", ex.Message);
        }

        using (var db = Ctx(MongoQueryMode.Native))
        {
            if (bulkServed)
            {
                Assert.Equal(1, db.Entities.Where(h => h.Items.Any(i => i.Label == "y")).ExecuteUpdate(s => s.SetProperty(h => h.Name, h => h.Name + "!")));
                Assert.Equal(["a", "b!"], raw.Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d["Name"].AsString).Order());
            }
            else
            {
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    db.Entities.Where(h => h.Items.Any(i => i.Label == "y")).ExecuteUpdate(s => s.SetProperty(h => h.Name, h => h.Name + "!")));
                Assert.Contains(BulkRefused, ex.Message);
                Assert.Equal(["a", "b"], raw.Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => d["Name"].AsString).Order());
            }
        }
    }
}
#endif
