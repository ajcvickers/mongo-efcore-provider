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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// <c>hay.Contains(s.City)</c> with a captured STRING <c>hay</c> is a substring test, not membership in a list. The bulk
/// allow-list took it for a "non-nullable list's Contains" (a <see cref="string"/> is an <c>IEnumerable&lt;char&gt;</c> of a
/// value type), so a bulk update ran on the server as <c>$indexOfCP</c> over a null element's MISSING City, which errors
/// mid-operation (documents before the failing one already written). It is now refused before any write, and the query
/// net (R20) does not take it for a local-collection membership test either. Every row is hand-written.
/// </summary>
[XUnitCollection("UpdateTests")]
public class ComplexCollectionStringContainsTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private const string Refusal = "ExecuteUpdate and ExecuteDelete run their filter and SetProperty values on driver-LINQ";

    public class Stop
    {
        public string City { get; set; } = null!;
    }

    public class Route
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public List<Stop> Stops { get; set; } = [];
    }

    // r-full [Oslo] first (so a server-side error after it leaves a partial write), then r-null [null], r-two [Bergen, Oslo].
    private static BsonDocument[] SeedDocs()
        =>
        [
            new() { { "_id", ObjectId.GenerateNewId() }, { "Name", "r-full" }, { "Rank", 1 }, { "Stops", new BsonArray { new BsonDocument("City", "Oslo") } } },
            new() { { "_id", ObjectId.GenerateNewId() }, { "Name", "r-null" }, { "Rank", 2 }, { "Stops", new BsonArray { BsonNull.Value } } },
            new()
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "r-two" }, { "Rank", 3 },
                { "Stops", new BsonArray { new BsonDocument("City", "Bergen"), new BsonDocument("City", "Oslo") } }
            }
        ];

    private (IMongoCollection<Route> Collection, BsonDocument[] Seed) Seed(string name)
    {
        var collection = database.CreateCollection<Route>(name + Guid.NewGuid().ToString("N")[..6]);
        var seed = SeedDocs();
        Raw(collection).InsertMany(seed.Select(d => d.DeepClone().AsBsonDocument));
        return (collection, seed);
    }

    private static IMongoCollection<BsonDocument> Raw(IMongoCollection<Route> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    private static SingleEntityDbContext<Route> Context(IMongoCollection<Route> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(collection, mb => mb.Entity<Route>().ComplexCollection(r => r.Stops), null, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    private static void AssertUnchanged(IMongoCollection<Route> collection, BsonDocument[] seed)
        => Assert.Equal(seed.Select(d => d.ToJson()),
            Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().OrderBy(d => d["_id"]).Select(d => d.ToJson()));

    private static void AssertRefused(Func<int> operation)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => operation());
        var refusal = Assert.IsType<NativeTranslationNotSupportedException>(ex.InnerException);
        Assert.Contains(Refusal, refusal.Message);
        Assert.Contains("the element predicate", refusal.Message);
        Assert.Contains("No document was modified", refusal.Message);
    }

    [Fact]
    public void Bulk_operations_with_a_string_Contains_of_an_element_member_are_refused_before_any_write()
    {
        var hay = "Oslo-Bergen";
        var (collection, seed) = Seed(nameof(Bulk_operations_with_a_string_Contains_of_an_element_member_are_refused_before_any_write));
        using (var db = Context(collection, MongoQueryMode.Native))
        {
            // RED (5547cfa2): MongoWriteException "$indexOfCP requires a string as the second argument, found: missing" on
            // r-null, after r-full had been updated (a partial write).
            AssertRefused(() => db.Entities.Where(r => r.Stops.Any(s => hay.Contains(s.City))).ExecuteUpdate(s => s.SetProperty(r => r.Rank, -1)));
            AssertRefused(() => db.Entities.Where(r => r.Stops.Any(s => hay.Contains(s.City))).ExecuteDelete());
            AssertRefused(() => db.Entities.Where(r => r.Stops.Any(s => !hay.Contains(s.City))).ExecuteDelete());
            // The Enumerable.Contains<char> spelling over a string (a char member is not mapped here; a string's chars).
            AssertRefused(() => db.Entities.Where(r => r.Stops.Any(s => "Oslo".Contains(s.City))).ExecuteDelete());
        }

        AssertUnchanged(collection, seed);
    }

    [Fact]
    public void Bulk_operations_with_a_list_Contains_of_an_element_member_are_still_admitted()
    {
        // Control: a real non-nullable list (no null item) keeps the allow-listed atom. r-full and r-two hold Oslo.
        var cities = new List<string> { "Oslo" };
        var (collection, _) = Seed(nameof(Bulk_operations_with_a_list_Contains_of_an_element_member_are_still_admitted));
        using (var db = Context(collection, MongoQueryMode.Native))
        {
            Assert.Equal(2, db.Entities.Where(r => r.Stops.Any(s => cities.Contains(s.City))).ExecuteUpdate(s => s.SetProperty(r => r.Rank, -1)));
        }

        Assert.Equal(["r-full:-1", "r-null:2", "r-two:-1"],
            Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(d => $"{d["Name"]}:{d["Rank"]}").Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Query_with_a_string_Contains_of_an_element_member()
    {
        // C#: r-full's Oslo and r-two's Bergen/Oslo are substrings of "Oslo-Bergen"; r-null's null element reads City null.
        // Native declines the shape; the driver's $indexOfCP over the null element's MISSING City is a server error (loud,
        // never wrong rows), in Native (fallback) and DriverLinq. RED (5547cfa2): Native refused it as "a membership test of
        // a member value in a local collection" (the same string-is-a-list misclassification as the bulk allow-list).
        var hay = "Oslo-Bergen";
        var (collection, _) = Seed(nameof(Query_with_a_string_Contains_of_an_element_member));
        List<string> Run(MongoQueryMode mode)
        {
            using var db = Context(collection, mode);
            return [.. db.Entities.Where(r => r.Stops.Any(s => hay.Contains(s.City))).Select(r => r.Name).ToList().Order(StringComparer.Ordinal)];
        }

        CompositionAssert.PerMode(Run, [], CompositionAssert.NotNative, CompositionAssert.Throws<MongoDB.Driver.MongoCommandException>("$indexOfCP requires a string as the second argument"),
            CompositionAssert.Throws<MongoDB.Driver.MongoCommandException>("$indexOfCP requires a string as the second argument"));
    }
}
#endif
