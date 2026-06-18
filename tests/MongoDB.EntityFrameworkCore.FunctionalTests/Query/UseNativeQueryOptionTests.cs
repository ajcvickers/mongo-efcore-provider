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

using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// Validates that the per-context <see cref="MongoDbContextOptionsBuilder.UseNativeQuery"/> option is honored:
/// the default (native MQL + streaming) and the opt-out (driver-LINQ + BsonDocument DOM) paths must return
/// identical, correct results against the same seeded database.
/// </summary>
[XUnitCollection("QueryTests")]
public class UseNativeQueryOptionTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    [Fact]
    public void Where_predicate_returns_same_rows_for_default_and_opt_out()
    {
        var collection = SeedCollection();

        var defaultResults = QueryWithPredicate(collection, useNativeQuery: true);
        var optOutResults = QueryWithPredicate(collection, useNativeQuery: false);

        // Default (native) path is correct.
        Assert.Equal(2, defaultResults.Count);
        Assert.All(defaultResults, e => Assert.Equal("planet", e.Kind));

        // Opt-out (driver-LINQ + DOM) path produces identical rows.
        Assert.Equal(
            defaultResults.Select(e => (e.Name, e.Kind, e.Rank)).OrderBy(t => t.Rank),
            optOutResults.Select(e => (e.Name, e.Kind, e.Rank)).OrderBy(t => t.Rank));
    }

    [Fact]
    public void No_predicate_returns_same_rows_for_default_and_opt_out()
    {
        var collection = SeedCollection();

        var defaultResults = QueryAll(collection, useNativeQuery: true);
        var optOutResults = QueryAll(collection, useNativeQuery: false);

        // Default (native) path returns every seeded row.
        Assert.Equal(3, defaultResults.Count);

        // Opt-out (driver-LINQ + DOM) path produces identical rows.
        Assert.Equal(
            defaultResults.Select(e => (e.Name, e.Kind, e.Rank)).OrderBy(t => t.Rank),
            optOutResults.Select(e => (e.Name, e.Kind, e.Rank)).OrderBy(t => t.Rank));
    }

    private static System.Collections.Generic.List<Body> QueryWithPredicate(
        IMongoCollection<Body> collection, bool useNativeQuery)
    {
        using var db = CreateContext(collection, useNativeQuery);
        return db.Entities.Where(e => e.Kind == "planet").ToList();
    }

    private static System.Collections.Generic.List<Body> QueryAll(
        IMongoCollection<Body> collection, bool useNativeQuery)
    {
        using var db = CreateContext(collection, useNativeQuery);
        return db.Entities.ToList();
    }

    private static SingleEntityDbContext<Body> CreateContext(IMongoCollection<Body> collection, bool useNativeQuery)
        => SingleEntityDbContext.Create<Body>(
            collection,
            optionsBuilderAction: o => o.UseMongoDB(
                collection.Database.Client,
                collection.Database.DatabaseNamespace.DatabaseName,
                mongo => mongo.UseNativeQuery(useNativeQuery)));

    private IMongoCollection<Body> SeedCollection([CallerMemberName] string? name = null)
    {
        var collection = database.CreateCollection<Body>(name);

        using var db = SingleEntityDbContext.Create<Body>(collection);
        db.Entities.AddRange(
        [
            new Body { Name = "Mercury", Kind = "planet", Rank = 1 },
            new Body { Name = "Venus", Kind = "planet", Rank = 2 },
            new Body { Name = "Sun", Kind = "star", Rank = 3 }
        ]);
        db.SaveChanges();

        return collection;
    }

    public class Body
    {
        public MongoDB.Bson.ObjectId _id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public int Rank { get; set; }
    }
}
