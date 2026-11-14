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

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Update;

public class RowVersionTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    public class BasicEntity
    {
        public ObjectId _id { get; set; }
        public string Text { get; set; }
    }

    public class TimestampedIntEntity : BasicEntity
    {
        [Timestamp] public int Version { get; set; }
    }

    class TimestampedUIntEntity : BasicEntity
    {
        [Timestamp] public uint Version { get; set; }
    }

    class TimestampedLongEntity : BasicEntity
    {
        [Timestamp] public long Version { get; set; }
    }

    public class TimestampedULongEntity : BasicEntity
    {
        [Timestamp] public ulong Version { get; set; }
    }

    public class UnattributedTimestampedULongEntity : BasicEntity
    {
        public ulong Version { get; set; }
    }

    public class TimestampedByteArrayEntity : BasicEntity
    {
        [Timestamp] public byte[] Version { get; set; }
    }

    public class UnattributedByteArrayEntity : BasicEntity
    {
        public byte[] Version { get; set; }
    }

    // The application sees a ulong; the database holds the same 8 big-endian bytes as a byte[] row version.
    public class ConvertedULongEntity : BasicEntity
    {
        public ulong Version { get; set; }
    }

    private static void ConfigureUnattributedByteArray(ModelBuilder mb)
        => mb.Entity<UnattributedByteArrayEntity>().Property(e => e.Version).IsRowVersion();

    private static void ConfigureConvertedULong(ModelBuilder mb)
        => mb.Entity<ConvertedULongEntity>().Property(e => e.Version).IsRowVersion().HasConversion<byte[]>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_int()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<TimestampedIntEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_uint()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<TimestampedUIntEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_long()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<TimestampedLongEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_ulong()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<TimestampedULongEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_unattributed()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<UnattributedTimestampedULongEntity>(
            mb => mb.Entity<UnattributedTimestampedULongEntity>().Property(e => e.Version).IsRowVersion());

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_byte_array()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<TimestampedByteArrayEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_unattributed_byte_array()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<UnattributedByteArrayEntity>(
            ConfigureUnattributedByteArray);

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_ulong_converted_to_byte_array()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<ConvertedULongEntity>(
            ConfigureConvertedULong);

    private void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<T>(Action<ModelBuilder>? mb = null)
        where T : BasicEntity, new()
    {
        var collection = database.CreateCollection<T>("Modifying " + typeof(T).Name);

        using var db1 = SingleEntityDbContext.Create(collection, mb);
        var entityInstance1 = new T {Text = "Initial state on instance 1"};
        db1.Entities.Add(entityInstance1);
        db1.SaveChanges();
        var firstVersion = db1.Entry(entityInstance1).Property("Version").CurrentValue;

        {
            using var db2 = SingleEntityDbContext.Create(collection, mb);
            var entityInstance2 = db2.Entities.First();
            entityInstance2.Text = "New state on instance 2";
            db2.SaveChanges();
        }

        entityInstance1.Text = "Update via instance 1";
        var ex = Assert.Throws<DbUpdateConcurrencyException>(() => db1.SaveChanges());
        Assert.Contains("1 modification", ex.Message);

        Assert.Equal(firstVersion, db1.Entry(entityInstance1).Property("Version").CurrentValue);
    }

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_int()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<
            TimestampedIntEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_uint()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<
            TimestampedUIntEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_long()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<
            TimestampedLongEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_ulong()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<
            TimestampedULongEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified_byte_array()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<
            TimestampedByteArrayEntity>();

    private async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_entity_already_modified<T>()
        where T : BasicEntity, new()
    {
        var collection = database.CreateCollection<T>("Modifying async " + typeof(T).Name);

        await using var db1 = SingleEntityDbContext.Create(collection);
        var entityInstance1 = new T {Text = "Initial state on instance 1"};
        await db1.Entities.AddAsync(entityInstance1);
        await db1.SaveChangesAsync();
        var firstVersion = db1.Entry(entityInstance1).Property("Version").CurrentValue;

        {
            await using var db2 = SingleEntityDbContext.Create(collection);
            var entityInstance2 = await db2.Entities.FirstAsync();
            entityInstance2.Text = "New state on instance 2";
            await db2.SaveChangesAsync();
        }

        entityInstance1.Text = "Update via instance 1";
        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db1.SaveChangesAsync());
        Assert.Contains("1 modification", ex.Message);

        Assert.Equal(firstVersion, db1.Entry(entityInstance1).Property("Version").CurrentValue);
    }

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity_int()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<TimestampedIntEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity_uint()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<TimestampedUIntEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity_long()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<TimestampedLongEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity_ulong()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<TimestampedULongEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity_byte_array()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<TimestampedByteArrayEntity>();

    private void SaveChanges_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<T>()
        where T : BasicEntity, new()
    {
        var collection = database.CreateCollection<T>("Missing " + typeof(T).Name);

        using var db1 = SingleEntityDbContext.Create(collection);
        var entityInstance1 = new T {Text = "Initial state on instance 1"};
        db1.Entities.Add(entityInstance1);
        db1.SaveChanges();

        {
            using var db2 = SingleEntityDbContext.Create(collection);
            var entityInstance2 = db2.Entities.First();
            db2.Remove(entityInstance2);
            db2.SaveChanges();
        }

        entityInstance1.Text = "Update via instance 1";
        var ex = Assert.Throws<DbUpdateConcurrencyException>(() => db1.SaveChanges());
        Assert.Contains("1 modification", ex.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_missing_entity_int()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<TimestampedIntEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_missing_entity_uint()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<
            TimestampedUIntEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_missing_entity_long()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<
            TimestampedLongEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_missing_entity_ulong()
        => await
            SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<TimestampedULongEntity>();

    private async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_modifying_missing_entity<T>()
        where T : BasicEntity, new()
    {
        var collection = database.CreateCollection<T>("Missing async " + typeof(T).Name);

        await using var db1 = SingleEntityDbContext.Create(collection);
        var entityInstance1 = new T {Text = "Initial state on instance 1"};
        await db1.Entities.AddAsync(entityInstance1);
        await db1.SaveChangesAsync();
        var firstVersion = db1.Entry(entityInstance1).Property("Version").CurrentValue;

        {
            await using var db2 = SingleEntityDbContext.Create(collection);
            var entityInstance2 = await db2.Entities.FirstAsync();
            db2.Remove(entityInstance2);
            await db2.SaveChangesAsync();
        }

        entityInstance1.Text = "Update via instance 1";
        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db1.SaveChangesAsync());
        Assert.Contains("1 modification", ex.Message);

        Assert.Equal("_version", db1.Entities.EntityType.FindProperty("Version")?.GetElementName());
        Assert.Equal(firstVersion, db1.Entry(entityInstance1).Property("Version").CurrentValue);
    }

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_missing_entity_int()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_missing_entity<TimestampedIntEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_missing_entity_uint()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_missing_entity<TimestampedUIntEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_missing_entity_long()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_missing_entity<TimestampedLongEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_missing_entity_ulong()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_missing_entity<TimestampedULongEntity>();

    private void SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_missing_entity<T>()
        where T : BasicEntity, new()
    {
        var collection = database.CreateCollection<T>("Deleting " + typeof(T).Name);

        using var db1 = SingleEntityDbContext.Create(collection);
        var entityInstance1 = new T {Text = "I will be gone"};
        db1.Entities.Add(entityInstance1);
        db1.SaveChanges();

        using var db2 = SingleEntityDbContext.Create(collection);
        var entityInstance2 = db2.Entities.First();

        db1.Remove(entityInstance1);
        db1.SaveChanges();

        db2.Remove(entityInstance2);
        var ex = Assert.Throws<DbUpdateConcurrencyException>(() => db2.SaveChanges());
        Assert.Contains("1 deletion", ex.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_missing_entity_int()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_missing_entity<TimestampedIntEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_missing_entity_uint()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_missing_entity<TimestampedUIntEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_missing_entity_long()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_missing_entity<TimestampedLongEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_missing_entity_ulong()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_missing_entity<
            TimestampedULongEntity>();

    private async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_missing_entity<T>()
        where T : BasicEntity, new()
    {
        var collection = database.CreateCollection<T>("Deleting async " + typeof(T).Name);

        await using var db1 = SingleEntityDbContext.Create(collection);
        var entityInstance1 = new T {Text = "I will be gone"};
        await db1.Entities.AddAsync(entityInstance1);
        await db1.SaveChangesAsync();

        await using var db2 = SingleEntityDbContext.Create(collection);
        var entityInstance2 = db2.Entities.First();

        db1.Remove(entityInstance1);
        await db1.SaveChangesAsync();

        db2.Remove(entityInstance2);
        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        Assert.Contains("1 deletion", ex.Message);
    }

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_modified_entity_int()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_modified_entity<TimestampedIntEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_modified_entity_uint()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_modified_entity<TimestampedUIntEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_modified_entity_long()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_modified_entity<TimestampedLongEntity>();

    [Fact]
    public void SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_modified_entity_ulong()
        => SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_modified_entity<TimestampedULongEntity>();

    private void SaveChanges_throws_DbUpdateConcurrencyException_when_deleting_modified_entity<T>()
        where T : BasicEntity, new()
    {
        var collection = database.CreateCollection<T>("Deleting modified " + typeof(T).Name);

        using var db1 = SingleEntityDbContext.Create(collection);
        var entityInstance1 = new T {Text = "Original"};
        db1.Entities.Add(entityInstance1);
        db1.SaveChanges();

        using var db2 = SingleEntityDbContext.Create(collection);
        var entityInstance2 = db2.Entities.First();

        entityInstance1.Text = "Modified";
        db1.SaveChanges();

        db2.Remove(entityInstance2);
        var ex = Assert.Throws<DbUpdateConcurrencyException>(() => db2.SaveChanges());
        Assert.Contains("1 deletion", ex.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_modified_entity_int()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_modified_entity<
            TimestampedIntEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_modified_entity_uint()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_modified_entity<
            TimestampedUIntEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_modified_entity_long()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_modified_entity<
            TimestampedLongEntity>();

    [Fact]
    public async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_modified_entity_ulong()
        => await SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_modified_entity<
            TimestampedULongEntity>();

    private async Task SaveChangesAsync_throws_DbUpdateConcurrencyException_when_deleting_modified_entity<T>()
        where T : BasicEntity, new()
    {
        var collection = database.CreateCollection<T>("Deleting modified async " + typeof(T).Name);

        await using var db1 = SingleEntityDbContext.Create(collection);
        var entityInstance1 = new T {Text = "Original"};
        await db1.Entities.AddAsync(entityInstance1);
        await db1.SaveChangesAsync();

        await using var db2 = SingleEntityDbContext.Create(collection);
        var entityInstance2 = db2.Entities.First();

        entityInstance1.Text = "Modified";
        await db1.SaveChangesAsync();

        db2.Remove(entityInstance2);
        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        Assert.Contains("1 deletion", ex.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_succeeds_after_rollback_by_setting_rowversion_original_value_to_latest()
    {
        var collection = database.CreateCollection<TimestampedULongEntity>("Applying rollbacked TimestampedULongEntity");

        await using var db1 = SingleEntityDbContext.Create(collection);
        var entityInstance1 = new TimestampedULongEntity {Text = "Original"};
        await db1.Entities.AddAsync(entityInstance1);
        await db1.SaveChangesAsync();

        await using var db2 = SingleEntityDbContext.Create(collection);
        var entityInstance2 = db2.Entities.First();

        entityInstance1.Text = "Modified 1";
        await db1.SaveChangesAsync();

        entityInstance2.Text = "Modified 2";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());

        db2.Entry(entityInstance2).Property("Version").OriginalValue = entityInstance1.Version;
        await db2.SaveChangesAsync();
    }

    private static BsonDocument ReadRaw<T>(IMongoCollection<T> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).Single();

    private static byte[] Counter(ulong value)
    {
        var bytes = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        return bytes;
    }

    [Fact]
    public void Byte_array_row_version_is_stored_as_an_8_byte_big_endian_counter_in__version()
    {
        var collection = database.CreateCollection<TimestampedByteArrayEntity>();

        using (var db = SingleEntityDbContext.Create(collection))
        {
            var entity = new TimestampedByteArrayEntity {Text = "one"};
            db.Entities.Add(entity);
            db.SaveChanges();

            Assert.Equal(Counter(1), entity.Version);
            var raw = ReadRaw(collection);
            Assert.False(raw.Contains("Version"));
            Assert.Equal(BsonBinarySubType.Binary, raw["_version"].AsBsonBinaryData.SubType);
            Assert.Equal(Counter(1), raw["_version"].AsBsonBinaryData.Bytes);

            entity.Text = "two";
            db.SaveChanges();
            Assert.Equal(Counter(2), entity.Version);
        }

        Assert.Equal(Counter(2), ReadRaw(collection)["_version"].AsBsonBinaryData.Bytes);

        using (var db = SingleEntityDbContext.Create(collection))
        {
            var loaded = db.Entities.Single();
            Assert.Equal(Counter(2), loaded.Version);
        }
    }

    [Fact]
    public void Byte_array_and_converted_ulong_row_versions_store_the_same_shape()
    {
        var collection = database.CreateCollection<ConvertedULongEntity>();

        using var db = SingleEntityDbContext.Create(collection, ConfigureConvertedULong);
        var entity = new ConvertedULongEntity {Text = "one"};
        db.Entities.Add(entity);
        db.SaveChanges();

        Assert.Equal(1UL, entity.Version);
        Assert.Equal(Counter(1), ReadRaw(collection)["_version"].AsBsonBinaryData.Bytes);

        entity.Text = "two";
        db.SaveChanges();

        Assert.Equal(2UL, entity.Version);
        Assert.Equal(Counter(2), ReadRaw(collection)["_version"].AsBsonBinaryData.Bytes);
    }

    [Fact]
    public void Byte_array_row_version_counts_on_from_the_stored_value()
    {
        var collection = database.CreateCollection<TimestampedByteArrayEntity>();
        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        raw.InsertOne(new BsonDocument {{"_id", ObjectId.GenerateNewId()}, {"Text", "seeded"}, {"_version", new BsonBinaryData(Counter(0x01FF))}});

        using var db = SingleEntityDbContext.Create(collection);
        var entity = db.Entities.Single();
        entity.Text = "changed";
        db.SaveChanges();

        Assert.Equal(Counter(0x0200), ReadRaw(collection)["_version"].AsBsonBinaryData.Bytes);
    }

    // A document written when a byte[] row version was an ordinary property has no "_version" element, and is not migrated.
    [Fact]
    public void Document_without_a_stored_byte_array_row_version_cannot_be_materialized()
    {
        var collection = database.CreateCollection<TimestampedByteArrayEntity>();
        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        raw.InsertOne(new BsonDocument {{"_id", ObjectId.GenerateNewId()}, {"Text", "legacy"}, {"Version", new BsonBinaryData(Counter(7))}});

        using var db = SingleEntityDbContext.Create(collection);
        var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.Single());
        Assert.Contains("Version", ex.Message);
    }

    // Mapping the row version to the element it was stored under before keeps such documents readable, and counts on from their value.
    [Fact]
    public void Byte_array_row_version_mapped_to_its_old_element_name_reads_and_counts_on_from_legacy_documents()
    {
        var collection = database.CreateCollection<TimestampedByteArrayEntity>();
        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        raw.InsertOne(new BsonDocument {{"_id", ObjectId.GenerateNewId()}, {"Text", "legacy"}, {"Version", new BsonBinaryData(Counter(7))}});

        using var db = SingleEntityDbContext.Create(
            collection, mb => mb.Entity<TimestampedByteArrayEntity>().Property(e => e.Version).HasElementName("Version"));
        var entity = db.Entities.Single();
        Assert.Equal(Counter(7), entity.Version);
        entity.Text = "changed";
        db.SaveChanges();

        var stored = ReadRaw(collection);
        Assert.False(stored.Contains("_version"));
        Assert.Equal(Counter(8), stored["Version"].AsBsonBinaryData.Bytes);
        Assert.Equal("changed", stored["Text"].AsString);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(16)]
    public void Byte_array_row_version_of_the_wrong_length_is_rejected_on_insert(int length)
    {
        var collection = database.CreateCollection<TimestampedByteArrayEntity>("Insert wrong length " + length);

        using var db = SingleEntityDbContext.Create(collection);
        db.Entities.Add(new TimestampedByteArrayEntity {Text = "bad", Version = new byte[length]});

        var ex = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Contains($"{length} bytes", ex.Message);
        Assert.Contains("Version", ex.Message);
        Assert.Equal(0, collection.CountDocuments(FilterDefinition<TimestampedByteArrayEntity>.Empty));
    }

    [Fact]
    public void Byte_array_row_version_of_the_wrong_length_is_rejected_on_update()
    {
        var collection = database.CreateCollection<TimestampedByteArrayEntity>();

        using var db = SingleEntityDbContext.Create(collection);
        var entity = new TimestampedByteArrayEntity {Text = "one"};
        db.Entities.Add(entity);
        db.SaveChanges();

        entity.Text = "two";
        entity.Version = new byte[3];

        var ex = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Contains("3 bytes", ex.Message);
        Assert.Equal(Counter(1), ReadRaw(collection)["_version"].AsBsonBinaryData.Bytes);
        Assert.Equal("one", ReadRaw(collection)["Text"].AsString);
    }

    [Fact]
    public void Byte_array_row_version_is_not_written_by_a_delete_but_guards_it()
    {
        var collection = database.CreateCollection<TimestampedByteArrayEntity>();

        using var db1 = SingleEntityDbContext.Create(collection);
        var entity1 = new TimestampedByteArrayEntity {Text = "one"};
        db1.Entities.Add(entity1);
        db1.SaveChanges();

        using (var db2 = SingleEntityDbContext.Create(collection))
        {
            var entity2 = db2.Entities.Single();
            entity2.Text = "changed elsewhere";
            db2.SaveChanges();
        }

        db1.Entities.Remove(entity1);
        Assert.Throws<DbUpdateConcurrencyException>(() => db1.SaveChanges());
        Assert.Equal(1, collection.CountDocuments(FilterDefinition<TimestampedByteArrayEntity>.Empty));
    }
}
