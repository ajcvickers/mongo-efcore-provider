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

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Update;

/// <summary>
/// One <c>SaveChanges</c> that removes an entity and adds another with the same primary key. EF Core pairs the two entries
/// (the Added entry's <c>SharedIdentityEntry</c> is the Deleted one) and the relational providers send one <c>UPDATE</c>.
/// This provider sends one <c>ReplaceOne</c>, filtered on the key and the removed entity's original concurrency token
/// values, so the document is never missing and the new entity's whole document is written (#109).
/// </summary>
public class ReplaceDocumentTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    public class Thing
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string? Note { get; set; }
        public int Version { get; set; }
    }

    public class Basket
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public List<BasketItem> Items { get; set; } = [];
        public Address? Address { get; set; }
    }

    public class BasketItem
    {
        public string Name { get; set; } = null!;
    }

    public class Address
    {
        public string City { get; set; } = null!;
    }

    public class Versioned
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public long Version { get; set; }
    }

    public class Pair
    {
        public int Part { get; set; }
        public string Name { get; set; } = null!;
        public string? Note { get; set; }
    }

    public class Converted
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class Stamped
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public byte[] Version { get; set; } = null!;
    }

    public abstract class Animal
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class Cat : Animal
    {
        public int Lives { get; set; }
    }

    public class Dog : Animal
    {
        public string? Breed { get; set; }
    }

    private class ReplaceContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Thing> Things { get; set; } = null!;
        public DbSet<Basket> Baskets { get; set; } = null!;
        public DbSet<Versioned> Versioneds { get; set; } = null!;
        public DbSet<Animal> Animals { get; set; } = null!;
        public DbSet<Pair> Pairs { get; set; } = null!;
        public DbSet<Stamped> Stampeds { get; set; } = null!;
        public DbSet<Converted> Converteds { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pair>().HasKey(p => new { p.Part, p.Name });
            modelBuilder.Entity<Stamped>().Property(s => s.Version).IsRowVersion();
            modelBuilder.Entity<Converted>().Property(c => c.Id).HasConversion<string>();
            modelBuilder.Entity<Versioned>().Property(v => v.Version).IsRowVersion();
            modelBuilder.Entity<Animal>();
            modelBuilder.Entity<Cat>();
            modelBuilder.Entity<Dog>();
            modelBuilder.Entity<Thing>().HasIndex(t => t.Code).IsUnique();
            modelBuilder.Entity<Thing>().Property(t => t.Version).IsConcurrencyToken();
            modelBuilder.Entity<Basket>().OwnsMany(b => b.Items);
            modelBuilder.Entity<Basket>().OwnsOne(b => b.Address);
        }
    }

    private ReplaceContext CreateContext(Action<string>? log = null, IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<ReplaceContext>()
            .UseMongoDB(database.MongoDatabase.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        if (log != null)
        {
            builder.LogTo(log);
        }

        if (interceptor != null)
        {
            builder.AddInterceptors(interceptor);
        }

        var db = new ReplaceContext(builder.Options);
        db.GetService<IMongoDatabaseCreator>().EnsureCreated();
        return db;
    }

    private static string NewCode() => Guid.NewGuid().ToString();

    private static async Task<int> Save(DbContext db, bool async)
        => async ? await db.SaveChangesAsync() : db.SaveChanges();

    private BsonDocument RawDocument(string collection, Guid id)
        => database.MongoDatabase.GetCollection<BsonDocument>(collection)
            .Find(new BsonDocument("_id", new BsonBinaryData(id, GuidRepresentation.Standard))).Single();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Removing_a_document_and_adding_another_with_the_same_key_replaces_the_document(bool async)
    {
        var id = Guid.NewGuid();
        var code = NewCode();
        var newCode = NewCode();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = id, Code = code, Note = "only the old one has this" });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            var old = db.Things.Single(t => t.Id == id);
            var replacement = new Thing { Id = id, Code = newCode };

            // EF Core only accepts the same key again once the old entity is marked deleted.
            db.Remove(old);
            db.Add(replacement);

            // One replaced document, as the relational providers count the one UPDATE.
            Assert.Equal(1, await Save(db, async));

            Assert.Equal(EntityState.Unchanged, db.Entry(replacement).State);

            // EF drops the entry of the entity it replaces once the save is accepted.
            Assert.Equal(EntityState.Detached, db.Entry(old).State);
        }

        using var check = CreateContext();
        var stored = check.Things.Single(t => t.Id == id);
        Assert.Equal(newCode, stored.Code);
        Assert.Null(stored.Note);
        Assert.Equal(1, check.Things.Count(t => t.Id == id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_document_with_a_composite_key_can_be_replaced(bool async)
    {
        var name = NewCode();

        using (var seed = CreateContext())
        {
            seed.Pairs.Add(new Pair { Part = 1, Name = name, Note = "old" });
            seed.Pairs.Add(new Pair { Part = 2, Name = name, Note = "other part" });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Remove(db.Pairs.Single(p => p.Part == 1 && p.Name == name));
            db.Add(new Pair { Part = 1, Name = name });
            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.Null(check.Pairs.Single(p => p.Part == 1 && p.Name == name).Note);
        Assert.Equal("other part", check.Pairs.Single(p => p.Part == 2 && p.Name == name).Note);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_document_with_a_value_converted_key_can_be_replaced(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Converteds.Add(new Converted { Id = id, Name = "old" });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Remove(db.Converteds.Single(c => c.Id == id));
            db.Add(new Converted { Id = id, Name = "new" });
            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.Equal("new", check.Converteds.Single(c => c.Id == id).Name);
        Assert.Equal(1, check.Converteds.Count(c => c.Id == id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_conflicting_replace_rolls_back_the_other_replaces_in_the_save(bool async)
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var firstCode = NewCode();

        using (var seed = CreateContext())
        {
            seed.Things.AddRange(new Thing { Id = firstId, Code = firstCode }, new Thing { Id = secondId, Code = NewCode() });
            await Save(seed, async);
        }

        using var db = CreateContext();
        var first = db.Things.Single(t => t.Id == firstId);
        var second = db.Things.Single(t => t.Id == secondId);

        using (var other = CreateContext())
        {
            other.Things.Single(t => t.Id == secondId).Version = 1;
            await Save(other, async);
        }

        db.Remove(first);
        db.Add(new Thing { Id = firstId, Code = NewCode() });
        db.Remove(second);
        db.Add(new Thing { Id = secondId, Code = NewCode() });

        if (async)
        {
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync());
        }
        else
        {
            Assert.Throws<DbUpdateConcurrencyException>(() => db.SaveChanges());
        }

        // Two documents are written, so the save runs in a transaction and the replace that did match is rolled back.
        using var check = CreateContext();
        Assert.Equal(firstCode, check.Things.Single(t => t.Id == firstId).Code);
        Assert.Equal(1, check.Things.Single(t => t.Id == secondId).Version);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_replaced_document_has_only_the_new_entitys_elements(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = id, Code = NewCode(), Note = "old" });
            await Save(seed, async);
        }

        // Something only the stored document has, which an $set of the new entity's properties would leave behind.
        await database.MongoDatabase.GetCollection<BsonDocument>("Things").UpdateOneAsync(
            new BsonDocument("_id", new BsonBinaryData(id, GuidRepresentation.Standard)),
            new BsonDocument("$set", new BsonDocument("Unmapped", 42)));

        using (var db = CreateContext())
        {
            db.Remove(db.Things.Single(t => t.Id == id));
            db.Add(new Thing { Id = id, Code = NewCode() });
            await Save(db, async);
        }

        var document = RawDocument("Things", id);
        Assert.False(document.Contains("Unmapped"));
        Assert.True(document["Note"].IsBsonNull);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replacing_a_document_writes_the_new_entitys_owned_entities_not_the_old_ones(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Baskets.Add(new Basket
            {
                Id = id,
                Name = "old",
                Items = [new BasketItem { Name = "a" }, new BasketItem { Name = "b" }, new BasketItem { Name = "c" }],
                Address = new Address { City = "Old City" }
            });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            var old = db.Baskets.Single(b => b.Id == id);
            db.Remove(old);
            db.Add(new Basket { Id = id, Name = "new", Items = [new BasketItem { Name = "x" }] });

            // The old basket's owned entities go with it: they must not turn into extra writes.
            await Save(db, async);
        }

        using var check = CreateContext();
        var stored = check.Baskets.Single(b => b.Id == id);
        Assert.Equal("new", stored.Name);
        Assert.Equal(["x"], stored.Items.Select(i => i.Name));
        Assert.Null(stored.Address);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_replaced_entity_can_be_changed_by_the_next_save(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = id, Code = NewCode() });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Remove(db.Things.Single(t => t.Id == id));
            var replacement = new Thing { Id = id, Code = NewCode(), Version = 3 };
            db.Add(replacement);
            await Save(db, async);

            replacement.Note = "changed";
            replacement.Version = 4;
            await Save(db, async);
        }

        using var check = CreateContext();
        var stored = check.Things.Single(t => t.Id == id);
        Assert.Equal("changed", stored.Note);
        Assert.Equal(4, stored.Version);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_replace_whose_concurrency_token_changed_reports_a_concurrency_exception_and_leaves_the_document(bool async)
    {
        var id = Guid.NewGuid();
        var code = NewCode();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = id, Code = code });
            await Save(seed, async);
        }

        using var db = CreateContext();
        var old = db.Things.Single(t => t.Id == id);

        using (var other = CreateContext())
        {
            other.Things.Single(t => t.Id == id).Version = 1;
            await Save(other, async);
        }

        db.Remove(old);
        var replacement = new Thing { Id = id, Code = NewCode() };
        db.Add(replacement);

        var exception = async
            ? await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync())
            : Assert.Throws<DbUpdateConcurrencyException>(() => db.SaveChanges());

        // One replace, one entry: the new entity, as the relational providers report the one UPDATE.
        Assert.Same(replacement, Assert.Single(exception.Entries).Entity);

        using var check = CreateContext();
        var stored = check.Things.Single(t => t.Id == id);
        Assert.Equal(code, stored.Code);
        Assert.Equal(1, stored.Version);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_replace_filter_uses_the_removed_entitys_original_token_not_the_new_entitys_value(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = id, Code = NewCode(), Version = 5 });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Remove(db.Things.Single(t => t.Id == id));
            db.Add(new Thing { Id = id, Code = NewCode(), Version = 9 });
            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.Equal(9, check.Things.Single(t => t.Id == id).Version);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_replace_of_a_document_someone_else_deleted_reports_a_concurrency_exception(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = id, Code = NewCode() });
            await Save(seed, async);
        }

        using var db = CreateContext();
        var old = db.Things.Single(t => t.Id == id);

        using (var other = CreateContext())
        {
            other.Remove(other.Things.Single(t => t.Id == id));
            await Save(other, async);
        }

        db.Remove(old);
        db.Add(new Thing { Id = id, Code = NewCode() });

        // A replace never inserts: relational providers' UPDATE matches no row, so this is a conflict, not an upsert.
        if (async)
        {
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync());
        }
        else
        {
            Assert.Throws<DbUpdateConcurrencyException>(() => db.SaveChanges());
        }

        using var check = CreateContext();
        Assert.False(check.Things.Any(t => t.Id == id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_replace_is_saved_with_other_inserts_updates_and_deletes_in_one_collection(bool async)
    {
        var replacedId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();
        var modifiedId = Guid.NewGuid();
        var insertedId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.AddRange(
                new Thing { Id = replacedId, Code = NewCode() },
                new Thing { Id = deletedId, Code = NewCode() },
                new Thing { Id = modifiedId, Code = NewCode() });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Remove(db.Things.Single(t => t.Id == replacedId));
            db.Add(new Thing { Id = replacedId, Code = NewCode(), Note = "replaced" });
            db.Remove(db.Things.Single(t => t.Id == deletedId));
            db.Things.Single(t => t.Id == modifiedId).Note = "modified";
            db.Add(new Thing { Id = insertedId, Code = NewCode() });

            Assert.Equal(4, await Save(db, async));
        }

        using var check = CreateContext();
        Assert.Equal("replaced", check.Things.Single(t => t.Id == replacedId).Note);
        Assert.False(check.Things.Any(t => t.Id == deletedId));
        Assert.Equal("modified", check.Things.Single(t => t.Id == modifiedId).Note);
        Assert.True(check.Things.Any(t => t.Id == insertedId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_replace_that_gives_up_a_unique_value_and_another_document_taking_it_succeeds_whatever_order(bool async)
    {
        var replacedId = Guid.NewGuid();
        var takerId = Guid.NewGuid();
        var taken = NewCode();

        using (var seed = CreateContext())
        {
            seed.Things.AddRange(new Thing { Id = replacedId, Code = taken }, new Thing { Id = takerId, Code = NewCode() });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            // The taker is tracked first, so left alone it is written before the replace that frees its new value.
            var taker = db.Things.Single(t => t.Id == takerId);
            var old = db.Things.Single(t => t.Id == replacedId);
            taker.Code = taken;
            db.Remove(old);
            db.Add(new Thing { Id = replacedId, Code = NewCode() });
            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.Equal(takerId, check.Things.Single(t => t.Code == taken).Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_replace_that_takes_a_unique_value_another_document_gives_up_succeeds_whatever_order(bool async)
    {
        var replacedId = Guid.NewGuid();
        var giverId = Guid.NewGuid();
        var taken = NewCode();

        using (var seed = CreateContext())
        {
            seed.Things.AddRange(new Thing { Id = replacedId, Code = NewCode() }, new Thing { Id = giverId, Code = taken });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Remove(db.Things.Single(t => t.Id == replacedId));
            db.Add(new Thing { Id = replacedId, Code = taken });
            db.Things.Single(t => t.Id == giverId).Code = NewCode();
            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.Equal(replacedId, check.Things.Single(t => t.Code == taken).Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_replaced_documents_row_version_continues_from_the_one_replaced(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Versioneds.Add(new Versioned { Id = id, Name = "v1" });
            await Save(seed, async);
        }

        using var db = CreateContext();
        var old = db.Versioneds.Single(v => v.Id == id);
        Assert.Equal(1, old.Version);

        using (var other = CreateContext())
        {
            // Another context loaded the document at version 1 as well.
            Assert.Equal(1, other.Versioneds.Single(v => v.Id == id).Version);
        }

        db.Remove(old);
        var replacement = new Versioned { Id = id, Name = "v2" };
        db.Add(replacement);
        await Save(db, async);

        // Starting again at 1 would let an update by that other context (which still holds version 1) through.
        Assert.Equal(2, replacement.Version);
        using var check = CreateContext();
        Assert.Equal(2, check.Versioneds.Single(v => v.Id == id).Version);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_replaced_documents_byte_array_row_version_continues_from_the_one_replaced(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Stampeds.Add(new Stamped { Id = id, Name = "v1" });
            await Save(seed, async);
        }

        using var db = CreateContext();
        var old = db.Stampeds.Single(v => v.Id == id);
        Assert.Equal(1UL, System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(old.Version));

        db.Remove(old);
        var replacement = new Stamped { Id = id, Name = "v2" };
        db.Add(replacement);
        await Save(db, async);

        // The counter carries on from the replaced document's, so a context still holding version 1 cannot match.
        Assert.Equal(2UL, System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(replacement.Version));
        using var check = CreateContext();
        var stored = check.Stampeds.Single(v => v.Id == id);
        Assert.Equal("v2", stored.Name);
        Assert.Equal(2UL, System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(stored.Version));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_replace_whose_row_version_changed_reports_a_concurrency_exception(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Versioneds.Add(new Versioned { Id = id, Name = "v1" });
            await Save(seed, async);
        }

        using var db = CreateContext();
        var old = db.Versioneds.Single(v => v.Id == id);

        using (var other = CreateContext())
        {
            other.Versioneds.Single(v => v.Id == id).Name = "changed by other";
            await Save(other, async);
        }

        db.Remove(old);
        db.Add(new Versioned { Id = id, Name = "v2" });

        if (async)
        {
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync());
        }
        else
        {
            Assert.Throws<DbUpdateConcurrencyException>(() => db.SaveChanges());
        }

        using var check = CreateContext();
        Assert.Equal("changed by other", check.Versioneds.Single(v => v.Id == id).Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_document_can_be_replaced_by_an_entity_of_another_type_in_the_hierarchy(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Animals.Add(new Cat { Id = id, Name = "Tom", Lives = 9 });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Remove(db.Animals.Single(a => a.Id == id));
            db.Add(new Dog { Id = id, Name = "Rex", Breed = "Lab" });
            await Save(db, async);
        }

        using var check = CreateContext();
        var dog = Assert.IsType<Dog>(check.Animals.Single(a => a.Id == id));
        Assert.Equal("Lab", dog.Breed);
        var document = RawDocument("Animals", id);
        Assert.False(document.Contains("Lives"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replacing_a_document_with_an_identical_one_counts_as_one_write(bool async)
    {
        var id = Guid.NewGuid();
        var code = NewCode();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = id, Code = code });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Remove(db.Things.Single(t => t.Id == id));
            db.Add(new Thing { Id = id, Code = code });

            // The driver reports a replace that changes nothing as unmodified, but it matched the document: counted, as the
            // relational providers count the matched row of an UPDATE.
            Assert.Equal(1, await Save(db, async));
        }

        using var check = CreateContext();
        Assert.Equal(code, check.Things.Single(t => t.Id == id).Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_update_that_writes_the_values_already_stored_counts_as_one_write(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = id, Code = NewCode(), Note = "same" });
            await Save(seed, async);
        }

        using var db = CreateContext();
        var thing = db.Things.Single(t => t.Id == id);

        // Marked modified but holding what is stored: the $set changes nothing, so the driver reports it unmodified.
        db.Entry(thing).State = EntityState.Modified;
        Assert.Equal(1, await Save(db, async));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replacing_a_stub_for_a_document_that_does_not_exist_reports_a_concurrency_exception(bool async)
    {
        var id = Guid.NewGuid();

        using var db = CreateContext();

        // Attach, remove and add the same key without reading: the stub stands for a document that is not there.
        var stub = new Thing { Id = id, Code = NewCode() };
        db.Things.Attach(stub);
        db.Remove(stub);
        db.Add(new Thing { Id = id, Code = NewCode() });

        // Relational providers send an UPDATE that matches no row, so the same code is a conflict there too.
        if (async)
        {
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync());
        }
        else
        {
            Assert.Throws<DbUpdateConcurrencyException>(() => db.SaveChanges());
        }

        using var check = CreateContext();
        Assert.False(check.Things.Any(t => t.Id == id));
    }

    // DiagnosticListener.AllListeners is process-wide, so the events are filtered to this fixture's (unique) database.
    private sealed class BulkWriteCollector : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        private readonly object _lock = new();
        private readonly List<(string Name, MongoBulkWriteEventData Data)> _events = [];
        private readonly List<IDisposable> _subscriptions = [];

        public IReadOnlyList<(string Name, MongoBulkWriteEventData Data)> EventsIn(string databaseName)
        {
            lock (_lock)
            {
                return _events.Where(e => e.Data.CollectionNamespace.DatabaseNamespace.DatabaseName == databaseName).ToList();
            }
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == DbLoggerCategory.Name)
            {
                lock (_lock)
                {
                    _subscriptions.Add(listener.Subscribe(this));
                }
            }
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Value is MongoBulkWriteEventData data)
            {
                lock (_lock)
                {
                    _events.Add((value.Key, data));
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void Stop()
        {
            lock (_lock)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.Dispose();
                }
            }
        }
    }

    [Fact]
    public void A_replace_is_reported_as_one_modified_write()
    {
        var id = Guid.NewGuid();
        var databaseName = database.MongoDatabase.DatabaseNamespace.DatabaseName;
        var collector = new BulkWriteCollector();
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using (var seed = CreateContext())
            {
                seed.Things.Add(new Thing { Id = id, Code = NewCode() });
                seed.SaveChanges();
            }

            using (var db = CreateContext())
            {
                db.Remove(db.Things.Single(t => t.Id == id));
                db.Add(new Thing { Id = id, Code = NewCode() });
                db.SaveChanges();
            }
        }
        finally
        {
            collector.Stop();
        }

        var events = collector.EventsIn(databaseName);

        // The seeding insert and the replace: one write each, the replace counted as a modification, not an insert and a delete.
        var executing = events.Where(e => e.Name == MongoEventId.ExecutingBulkWrite.Name).Select(e => e.Data).ToList();
        Assert.Equal(2, executing.Count);
        Assert.Equal((1L, 0L, 0L), (executing[0].InsertCount, executing[0].DeleteCount, executing[0].ModifyCount));
        Assert.Equal((0L, 0L, 1L), (executing[1].InsertCount, executing[1].DeleteCount, executing[1].ModifyCount));
        Assert.Equal("Things", executing[1].CollectionNamespace.CollectionName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Removing_and_then_updating_an_entity_with_the_same_key_sets_its_properties_and_keeps_the_rest(bool async)
    {
        var id = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = id, Code = NewCode(), Note = "old" });
            await Save(seed, async);
        }

        // Something only the stored document has: Update (unlike Add) is a $set, so it stays.
        await database.MongoDatabase.GetCollection<BsonDocument>("Things").UpdateOneAsync(
            new BsonDocument("_id", new BsonBinaryData(id, GuidRepresentation.Standard)),
            new BsonDocument("$set", new BsonDocument("Unmapped", 42)));

        var newCode = NewCode();
        using (var db = CreateContext())
        {
            db.Remove(db.Things.Single(t => t.Id == id));

            // Only an entity that is Added is a replace: this one is Modified, as before #109.
            db.Update(new Thing { Id = id, Code = newCode });
            await Save(db, async);
        }

        var document = RawDocument("Things", id);
        Assert.Equal(newCode, document["Code"].AsString);
        Assert.Equal(42, document["Unmapped"].AsInt32);
    }
}
