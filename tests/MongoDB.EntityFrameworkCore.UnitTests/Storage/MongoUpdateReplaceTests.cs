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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.UnitTests.Storage;

/// <summary>
/// An entity removed and another added with the same primary key in one <c>SaveChanges</c> is saved as one
/// <c>ReplaceOne</c> (#109). These tests build the pair in a change tracker and look at what the provider makes of it:
/// <see cref="MongoUpdate"/> for the write and its filter, and <see cref="MongoUniqueValueOrderer"/> for where it goes among the
/// other updates of the collection.
/// </summary>
public class MongoUpdateReplaceTests : IDisposable
{
    private class Item
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public int Version { get; set; }
        public string? Note { get; set; }
    }

    private class ItemContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Item> Items { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Item>().HasIndex(i => i.Code).IsUnique();
            modelBuilder.Entity<Item>().Property(i => i.Version).IsConcurrencyToken();
        }
    }

    private readonly ItemContext _db = new(
        new DbContextOptionsBuilder<ItemContext>()
            .UseMongoDB("mongodb://localhost:27017", "UnitTests")
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options);

    public void Dispose() => _db.Dispose();

    private IUpdateEntry EntryFor(object entity)
        => (IUpdateEntry)_db.Entry(entity).GetInfrastructure();

    private static BsonDocument Render(FilterDefinition<BsonDocument> filter)
        => filter.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));

    /// <summary>Loads (attaches) <paramref name="stored"/>, removes it and adds <paramref name="replacement"/>.</summary>
    private MongoUpdate Replace(Item stored, Item replacement)
    {
        replacement.Id = stored.Id;
        _db.Attach(stored);
        _db.Remove(stored);
        _db.Add(replacement);
        return Assert.Single(MongoUpdate.CreateAll([EntryFor(replacement)]));
    }

    /// <summary>Loads (attaches) an item with <paramref name="from"/> and changes its code to <paramref name="to"/>.</summary>
    private MongoUpdate ChangeCode(string? from, string? to)
    {
        var item = new Item { Id = Guid.NewGuid(), Code = from };
        _db.Attach(item);
        item.Code = to;
        _db.ChangeTracker.DetectChanges();
        return Assert.Single(MongoUpdate.CreateAll([EntryFor(item)]));
    }

    private MongoUpdate ReplaceCode(string? from, string? to)
        => Replace(new Item { Id = Guid.NewGuid(), Code = from }, new Item { Code = to });

    [Fact]
    public void An_added_entry_paired_with_a_deleted_one_is_a_replace_of_the_deleted_ones_document()
    {
        var stored = new Item { Id = Guid.NewGuid(), Code = "old", Note = "old note" };
        var replacement = new Item { Code = "new" };

        var update = Replace(stored, replacement);

        Assert.Equal(WriteModelType.ReplaceOne, update.Model.ModelType);
        Assert.True(update.IsUpdate);
        Assert.Same(replacement, update.Entry.ToEntityEntry().Entity);
        Assert.Same(stored, update.Replaced!.ToEntityEntry().Entity);

        var model = (ReplaceOneModel<BsonDocument>)update.Model;
        Assert.False(model.IsUpsert);
        Assert.Equal("new", model.Replacement["Code"].AsString);
        Assert.True(model.Replacement["Note"].IsBsonNull);
    }

    [Fact]
    public void The_filter_of_a_replace_is_the_key_and_the_removed_entitys_original_concurrency_token_values()
    {
        var stored = new Item { Id = Guid.NewGuid(), Code = "old", Version = 5 };
        var replacement = new Item { Code = "new", Version = 9 };

        var update = Replace(stored, replacement);

        var filter = Render(((ReplaceOneModel<BsonDocument>)update.Model).Filter);
        Assert.Equal(2, filter.ElementCount);
        Assert.Equal(new BsonBinaryData(stored.Id, GuidRepresentation.Standard), filter["_id"]);
        Assert.Equal(5, filter["Version"].AsInt32);

        // The document written is the new entity's.
        Assert.Equal(9, ((ReplaceOneModel<BsonDocument>)update.Model).Replacement["Version"].AsInt32);
    }

    [Fact]
    public void An_added_entry_alone_is_an_insert_and_has_nothing_replaced()
    {
        var update = Assert.Single(MongoUpdate.CreateAll([EntryFor(_db.Add(new Item { Id = Guid.NewGuid() }).Entity)]));

        Assert.Equal(WriteModelType.InsertOne, update.Model.ModelType);
        Assert.False(update.IsUpdate);
        Assert.Null(update.Replaced);
    }

    [Fact]
    public void A_modified_entry_paired_with_a_deleted_one_is_still_a_set_of_its_modified_properties()
    {
        // Remove then Update of the same key: EF pairs the entries too, but only an Added one is a replace. This is as before #109.
        var stored = new Item { Id = Guid.NewGuid(), Code = "old" };
        _db.Attach(stored);
        _db.Remove(stored);
        var updated = new Item { Id = stored.Id, Code = "new" };
        _db.Update(updated);

        var update = Assert.Single(MongoUpdate.CreateAll([EntryFor(updated)]));

        Assert.Equal(WriteModelType.UpdateOne, update.Model.ModelType);
        Assert.True(update.IsUpdate);
        Assert.Null(update.Replaced);
    }

    [Fact]
    public void A_replace_that_frees_a_value_goes_before_the_update_that_takes_it()
    {
        var takes = ChangeCode("x", "a");
        var frees = ReplaceCode("a", "b");

        Assert.Equal([frees, takes], MongoUniqueValueOrderer.Order([takes, frees]));
    }

    [Fact]
    public void A_replace_that_takes_a_value_goes_after_the_update_that_frees_it()
    {
        var takes = ReplaceCode("x", "b");
        var frees = ChangeCode("b", "c");

        Assert.Equal([frees, takes], MongoUniqueValueOrderer.Order([takes, frees]));
    }

    [Fact]
    public void A_value_a_replace_keeps_is_neither_freed_nor_taken()
    {
        // The replaced document holds "a" before and after, so the update that takes "a" has no reason to wait for it (and would
        // collide whichever came first): the order is left as given, where treating it as freed would put the replace first.
        var update = ChangeCode("x", "a");
        var keeps = ReplaceCode("a", "a");

        Assert.Equal([update, keeps], MongoUniqueValueOrderer.Order([update, keeps]));
    }

    [Fact]
    public void A_replace_that_takes_a_value_frees_nothing_for_a_null_it_replaces()
    {
        // null is not a value to conflict on: the update that frees "b" still goes first.
        var takes = ReplaceCode(null, "b");
        var frees = ChangeCode("b", "c");

        Assert.Equal([frees, takes], MongoUniqueValueOrderer.Order([takes, frees]));
    }

    [Fact]
    public void A_replace_to_null_still_frees_its_old_value()
    {
        var takes = ChangeCode("x", "a");
        var frees = ReplaceCode("a", null);

        Assert.Equal([frees, takes], MongoUniqueValueOrderer.Order([takes, frees]));
    }

    [Fact]
    public void A_replace_and_an_update_that_swap_values_keep_their_order()
    {
        // A cycle cannot be ordered: the server reports the duplicate key, as for two updates that swap values.
        var replace = ReplaceCode("a", "b");
        var update = ChangeCode("b", "a");

        Assert.Equal([replace, update], MongoUniqueValueOrderer.Order([replace, update]));
        Assert.Equal([update, replace], MongoUniqueValueOrderer.Order([update, replace]));
    }
}
