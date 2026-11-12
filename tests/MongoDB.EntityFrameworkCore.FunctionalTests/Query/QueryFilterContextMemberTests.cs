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
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// EF-X701: a query filter that reads a list member of the context (<c>Foo == 1 ? Baz.Contains(e.Bar) : e.Bar == Bar</c>)
/// threw <see cref="NullReferenceException"/> while building the pipeline whenever that list was unset, even when the
/// branch using it was not the one taken. An unset list is now an empty list, as EF Core treats a null collection
/// parameter: <c>Contains</c> matches nothing, whichever branch is taken.
/// </summary>
[XUnitCollection("QueryTests")]
public class QueryFilterContextMemberTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class FooBar
    {
        public long Id { get; set; }
        public long Bar { get; set; }
    }

    private sealed class FilterContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public int Foo { get; set; }
        public long? Bar { get; set; }
        public List<long>? Baz { get; set; }

        public DbSet<FooBar> FooBars => Set<FooBar>();

        protected override void OnModelCreating(ModelBuilder mb)
            => mb.Entity<FooBar>(b =>
            {
                b.ToCollection(collection);
                b.HasQueryFilter(e => Foo == 1 ? Baz!.Contains(e.Bar) : e.Bar == Bar);
            });
    }

    private string Seed(string name)
    {
        var collection = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        database.MongoDatabase.GetCollection<BsonDocument>(collection).InsertMany(
        [
            new BsonDocument { { "_id", 1L }, { "Bar", 1L } },
            new BsonDocument { { "_id", 2L }, { "Bar", 2L } },
            new BsonDocument { { "_id", 3L }, { "Bar", 3L } }
        ]);
        return collection;
    }

    private FilterContext Context(string collection, MongoQueryMode mode)
    {
        var builder = new DbContextOptionsBuilder<FilterContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        return new FilterContext(builder.Options, collection);
    }

    public static TheoryData<MongoQueryMode> Modes => new() { MongoQueryMode.Native, MongoQueryMode.NativeOnly };

    [Theory]
    [MemberData(nameof(Modes))]
    public void Unset_list_is_harmless_when_its_branch_is_not_taken(MongoQueryMode mode)
    {
        using var db = Context(Seed(nameof(Unset_list_is_harmless_when_its_branch_is_not_taken) + mode), mode);
        db.Foo = 0;
        db.Bar = 2;
        db.Baz = null;

        Assert.Equal([2L], db.FooBars.OrderBy(e => e.Id).Select(e => e.Bar).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void List_is_used_when_its_branch_is_taken(MongoQueryMode mode)
    {
        using var db = Context(Seed(nameof(List_is_used_when_its_branch_is_taken) + mode), mode);
        db.Foo = 1;
        db.Bar = 2;
        db.Baz = [1, 3];

        Assert.Equal([1L, 3L], db.FooBars.OrderBy(e => e.Id).Select(e => e.Bar).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void Unset_list_matches_nothing_when_its_branch_is_taken(MongoQueryMode mode)
    {
        using var db = Context(Seed(nameof(Unset_list_matches_nothing_when_its_branch_is_taken) + mode), mode);
        db.Foo = 1;
        db.Baz = null;

        Assert.Empty(db.FooBars.ToList());
    }
}
