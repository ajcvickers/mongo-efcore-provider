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
using Microsoft.EntityFrameworkCore.Update;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.UnitTests.Storage;

/// <summary>
/// <see cref="MongoUpdateBatch.CreateBatches"/> groups the updates of a <c>SaveChanges</c> into one batch per collection
/// and writes deletes first, then updates, then inserts, so that an insert cannot reach the server before the delete
/// or update that frees the unique-indexed value it takes (#102).
/// </summary>
public class MongoUpdateBatchTests : IDisposable
{
    private class Apple
    {
        public Guid Id { get; set; }
    }

    private class Banana
    {
        public Guid Id { get; set; }
    }

    private class FruitContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Apple> Apples { get; set; } = null!;
        public DbSet<Banana> Bananas { get; set; } = null!;
    }

    private readonly FruitContext _db = new(
        new DbContextOptionsBuilder<FruitContext>()
            .UseMongoDB("mongodb://localhost:27017", "UnitTests")
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options);

    public void Dispose() => _db.Dispose();

    private string AppleCollection => _db.Model.FindEntityType(typeof(Apple))!.GetCollectionName();
    private string BananaCollection => _db.Model.FindEntityType(typeof(Banana))!.GetCollectionName();

    private IUpdateEntry EntryFor(object entity)
        => (IUpdateEntry)_db.Entry(entity).GetInfrastructure();

    private MongoUpdate Insert(object entity)
        => new(EntryFor(entity), new InsertOneModel<BsonDocument>(new BsonDocument()));

    private MongoUpdate Delete(object entity)
        => new(EntryFor(entity), new DeleteOneModel<BsonDocument>(new BsonDocument()));

    private MongoUpdate Update(object entity)
        => new(EntryFor(entity), new UpdateOneModel<BsonDocument>(
            new BsonDocument(), new BsonDocumentUpdateDefinition<BsonDocument>(new BsonDocument("$set", new BsonDocument()))));

    [Fact]
    public void No_updates_gives_no_batches()
        => Assert.Empty(MongoUpdateBatch.CreateBatches([]));

    [Fact]
    public void Writes_deletes_then_updates_then_inserts_and_keeps_the_order_within_each_kind()
    {
        var i1 = Insert(new Apple());
        var u1 = Update(new Apple());
        var d1 = Delete(new Apple());
        var i2 = Insert(new Apple());
        var d2 = Delete(new Apple());
        var u2 = Update(new Apple());
        var i3 = Insert(new Apple());

        var batch = Assert.Single(MongoUpdateBatch.CreateBatches([i1, u1, d1, i2, d2, u2, i3]));

        Assert.Equal(AppleCollection, batch.CollectionName);
        Assert.Equal([d1, d2, u1, u2, i1, i2, i3], batch.Updates);
    }

    [Fact]
    public void Writes_in_the_order_given_when_there_are_only_inserts()
    {
        var i1 = Insert(new Apple());
        var i2 = Insert(new Apple());
        var i3 = Insert(new Apple());

        var batch = Assert.Single(MongoUpdateBatch.CreateBatches([i1, i2, i3]));

        Assert.Equal([i1, i2, i3], batch.Updates);
    }

    [Fact]
    public void Writes_in_the_order_given_when_there_are_only_deletes()
    {
        var d1 = Delete(new Apple());
        var d2 = Delete(new Apple());
        var d3 = Delete(new Apple());

        var batch = Assert.Single(MongoUpdateBatch.CreateBatches([d1, d2, d3]));

        Assert.Equal([d1, d2, d3], batch.Updates);
    }

    [Fact]
    public void Groups_updates_for_a_collection_that_are_separated_by_updates_for_another_collection()
    {
        // The order EF Core supplies when it adds to two collections and removes from one: the delete of an Apple is
        // separated from the insert of an Apple by the insert of a Banana.
        var insertApple = Insert(new Apple());
        var insertBanana = Insert(new Banana());
        var deleteApple = Delete(new Apple());

        var batches = MongoUpdateBatch.CreateBatches([insertApple, insertBanana, deleteApple]);

        Assert.Equal(2, batches.Count);
        Assert.Equal(AppleCollection, batches[0].CollectionName);
        Assert.Equal([deleteApple, insertApple], batches[0].Updates);
        Assert.Equal(BananaCollection, batches[1].CollectionName);
        Assert.Equal([insertBanana], batches[1].Updates);
    }

    [Fact]
    public void Orders_the_batches_by_where_each_collection_first_appears()
    {
        var banana = Insert(new Banana());
        var apple1 = Insert(new Apple());
        var apple2 = Delete(new Apple());
        var banana2 = Delete(new Banana());

        var batches = MongoUpdateBatch.CreateBatches([banana, apple1, apple2, banana2]);

        Assert.Equal([BananaCollection, AppleCollection], batches.Select(b => b.CollectionName));
        Assert.Equal([banana2, banana], batches[0].Updates);
        Assert.Equal([apple2, apple1], batches[1].Updates);
    }

    [Fact]
    public void Counts_cover_everything_in_the_batch_after_ordering()
    {
        var batch = Assert.Single(MongoUpdateBatch.CreateBatches(
        [
            Insert(new Apple()), Insert(new Apple()), Update(new Apple()), Delete(new Apple()), Delete(new Apple()),
            Delete(new Apple())
        ]));

        Assert.Equal(2, batch.Inserts);
        Assert.Equal(1, batch.Modified);
        Assert.Equal(3, batch.Deletes);
        Assert.Equal(6, batch.Updates.Count);
    }
}
