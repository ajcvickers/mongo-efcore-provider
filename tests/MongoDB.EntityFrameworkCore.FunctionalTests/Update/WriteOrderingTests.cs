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
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Update;

/// <summary>
/// One <c>SaveChanges</c> can both free a unique-indexed value (by deleting a document or changing its value) and
/// take it (by adding a document or changing another one). EF Core supplies the entries Added, Modified, Deleted
/// whatever order they were tracked in, so the provider must order the writes itself. The relational providers do
/// the same in <c>CommandBatchPreparer</c> (#102).
/// </summary>
public class WriteOrderingTests(TemporaryDatabaseFixture database, ITestOutputHelper testOutputHelper)
    : IClassFixture<TemporaryDatabaseFixture>
{
    public class Thing
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string? Note { get; set; }
        public int Version { get; set; }
    }

    public class Widget
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
    }

    public class Slot
    {
        public Guid Id { get; set; }
        public string Group { get; set; } = null!;
        public int Position { get; set; }
    }

    public abstract class Animal
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
    }

    public class Cat : Animal;

    public class Dog : Animal;

    public class Basket
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public List<BasketItem> Items { get; set; } = [];
    }

    public class BasketItem
    {
        public string Name { get; set; } = null!;
    }

    private class OrderingContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Thing> Things { get; set; } = null!;
        public DbSet<Widget> Widgets { get; set; } = null!;
        public DbSet<Slot> Slots { get; set; } = null!;
        public DbSet<Animal> Animals { get; set; } = null!;
        public DbSet<Basket> Baskets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Thing>().HasIndex(t => t.Code).IsUnique();
            modelBuilder.Entity<Thing>().Property(t => t.Version).IsConcurrencyToken();
            modelBuilder.Entity<Widget>().HasIndex(w => w.Code).IsUnique();
            modelBuilder.Entity<Slot>().HasIndex(x => new { x.Group, x.Position }).IsUnique();
            modelBuilder.Entity<Animal>().HasIndex(a => a.Code).IsUnique();
            modelBuilder.Entity<Cat>();
            modelBuilder.Entity<Dog>();
            modelBuilder.Entity<Basket>().HasIndex(b => b.Code).IsUnique();
            modelBuilder.Entity<Basket>().OwnsMany(b => b.Items);
        }
    }

    private OrderingContext CreateContext(Action<string>? log = null, IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<OrderingContext>()
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

        var db = new OrderingContext(builder.Options);
        db.GetService<IMongoDatabaseCreator>().EnsureCreated();
        return db;
    }

    private static string NewCode() => Guid.NewGuid().ToString();

    private static async Task Save(DbContext db, bool async)
    {
        if (async)
        {
            await db.SaveChangesAsync();
        }
        else
        {
            db.SaveChanges();
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task Removing_a_document_and_adding_another_with_the_same_unique_value_succeeds(bool addFirst, bool async)
    {
        var code = NewCode();
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = oldId, Code = code });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            var old = db.Things.Single(t => t.Id == oldId);
            var replacement = new Thing { Id = newId, Code = code };

            // EF Core supplies the entries Added then Deleted whichever way they are tracked.
            if (addFirst)
            {
                db.Add(replacement);
                db.Remove(old);
            }
            else
            {
                db.Remove(old);
                db.Add(replacement);
            }

            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.Equal(newId, check.Things.Single(t => t.Code == code).Id);
        Assert.False(check.Things.Any(t => t.Id == oldId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Changing_a_unique_value_and_adding_another_document_with_the_old_value_succeeds(bool async)
    {
        var code = NewCode();
        var movedCode = NewCode();
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = oldId, Code = code });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Things.Single(t => t.Id == oldId).Code = movedCode;
            db.Add(new Thing { Id = newId, Code = code });

            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.Equal(newId, check.Things.Single(t => t.Code == code).Id);
        Assert.Equal(oldId, check.Things.Single(t => t.Code == movedCode).Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Removing_a_document_and_changing_another_to_its_unique_value_succeeds(bool async)
    {
        var code = NewCode();
        var removedId = Guid.NewGuid();
        var changedId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.AddRange(
                new Thing { Id = removedId, Code = code },
                new Thing { Id = changedId, Code = NewCode() });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            var removed = db.Things.Single(t => t.Id == removedId);
            db.Things.Single(t => t.Id == changedId).Code = code;
            db.Remove(removed);

            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.Equal(changedId, check.Things.Single(t => t.Code == code).Id);
        Assert.False(check.Things.Any(t => t.Id == removedId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replacements_in_several_collections_in_one_save_succeed_and_use_one_bulk_write_per_collection(bool async)
    {
        var thingCode = NewCode();
        var widgetCode = NewCode();
        var oldThingId = Guid.NewGuid();
        var oldWidgetId = Guid.NewGuid();
        var newThingId = Guid.NewGuid();
        var newWidgetId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = oldThingId, Code = thingCode });
            seed.Widgets.Add(new Widget { Id = oldWidgetId, Code = widgetCode });
            await Save(seed, async);
        }

        List<string> logs = [];
        using (var db = CreateContext(s =>
               {
                   logs.Add(s);
                   testOutputHelper.WriteLine(s);
               }))
        {
            var oldThing = db.Things.Single(t => t.Id == oldThingId);
            var oldWidget = db.Widgets.Single(w => w.Id == oldWidgetId);

            // Added Thing, Added Widget, then Deleted Thing, Deleted Widget when EF Core supplies them: taken in
            // that order, the writes alternate between the two collections.
            db.Add(new Thing { Id = newThingId, Code = thingCode });
            db.Add(new Widget { Id = newWidgetId, Code = widgetCode });
            db.Remove(oldThing);
            db.Remove(oldWidget);

            await Save(db, async);
        }

        // One bulk write per collection, in the order the collections first appear, each with its delete and its insert.
        var executing = logs.Where(l => l.Contains("MongoEventId.ExecutingBulkWrite")).ToList();
        Assert.Equal(2, executing.Count);
        Assert.Contains(".Things'", executing[0]);
        Assert.Contains(".Widgets'", executing[1]);
        Assert.All(executing, l => Assert.Contains("Insertions=1, Deletions=1, Modifications=0", l));

        using var check = CreateContext();
        Assert.Equal(newThingId, check.Things.Single(t => t.Code == thingCode).Id);
        Assert.Equal(newWidgetId, check.Widgets.Single(w => w.Code == widgetCode).Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_removed_document_that_fails_its_concurrency_check_reports_a_concurrency_exception_not_a_duplicate_key(bool async)
    {
        var code = NewCode();
        var oldId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = oldId, Code = code });
            await Save(seed, async);
        }

        using var db = CreateContext();
        var old = db.Things.Single(t => t.Id == oldId);

        using (var other = CreateContext())
        {
            other.Things.Single(t => t.Id == oldId).Version = 1;
            await Save(other, async);
        }

        db.Remove(old);
        db.Add(new Thing { Id = Guid.NewGuid(), Code = code });

        // The delete matches nothing, so the old document is still there when the insert reaches the server and takes
        // its unique value. The failed concurrency check is the cause, so that is what is reported.
        var exception = async
            ? await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync())
            : Assert.Throws<DbUpdateConcurrencyException>(() => db.SaveChanges());

        Assert.Same(old, Assert.Single(exception.Entries).Entity);
        Assert.IsAssignableFrom<MongoBulkWriteException>(exception.InnerException);

        using var check = CreateContext();
        Assert.Equal(1, check.Things.Single(t => t.Code == code).Version);
        Assert.Equal(oldId, check.Things.Single(t => t.Code == code).Id);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task Changing_a_unique_value_to_one_another_document_gives_up_succeeds_whatever_order_they_were_loaded_in(
        bool giverLoadedFirst, bool async)
    {
        var givenCode = NewCode();
        var giverId = Guid.NewGuid();
        var takerId = Guid.NewGuid();
        var newCode = NewCode();

        using (var seed = CreateContext())
        {
            seed.Things.AddRange(
                new Thing { Id = giverId, Code = givenCode },
                new Thing { Id = takerId, Code = NewCode() });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            Thing giver, taker;
            if (giverLoadedFirst)
            {
                giver = db.Things.Single(t => t.Id == giverId);
                taker = db.Things.Single(t => t.Id == takerId);
            }
            else
            {
                taker = db.Things.Single(t => t.Id == takerId);
                giver = db.Things.Single(t => t.Id == giverId);
            }

            giver.Code = newCode;
            taker.Code = givenCode;

            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.Equal(giverId, check.Things.Single(t => t.Code == newCode).Id);
        Assert.Equal(takerId, check.Things.Single(t => t.Code == givenCode).Id);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task Shifting_a_run_of_unique_values_by_one_succeeds_whatever_order_the_documents_were_loaded_in(
        bool loadAscending, bool async)
    {
        var prefix = NewCode();
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();

        using (var seed = CreateContext())
        {
            for (var i = 0; i < ids.Length; i++)
            {
                seed.Things.Add(new Thing { Id = ids[i], Code = $"{prefix}-{i}" });
            }

            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            var order = loadAscending ? Enumerable.Range(0, ids.Length) : Enumerable.Range(0, ids.Length).Reverse();
            foreach (var i in order.ToList())
            {
                db.Things.Single(t => t.Id == ids[i]).Code = $"{prefix}-{i + 1}";
            }

            await Save(db, async);
        }

        using var check = CreateContext();
        for (var i = 0; i < ids.Length; i++)
        {
            Assert.Equal(ids[i], check.Things.Single(t => t.Code == $"{prefix}-{i + 1}").Id);
        }

        Assert.False(check.Things.Any(t => t.Code == $"{prefix}-0"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Swapping_unique_values_between_two_documents_is_a_duplicate_key_error(bool async)
    {
        // Not supported, as with the relational providers (which report a circular dependency): no order of the two
        // writes avoids a duplicate, and MongoDB has no deferred unique constraint. This test pins that.
        var codeA = NewCode();
        var codeB = NewCode();
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.AddRange(new Thing { Id = idA, Code = codeA }, new Thing { Id = idB, Code = codeB });
            await Save(seed, async);
        }

        using var db = CreateContext();
        db.Things.Single(t => t.Id == idA).Code = codeB;
        db.Things.Single(t => t.Id == idB).Code = codeA;

        if (async)
        {
            await Assert.ThrowsAsync<MongoBulkWriteException<BsonDocument>>(() => db.SaveChangesAsync());
        }
        else
        {
            Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => db.SaveChanges());
        }

        using var check = CreateContext();
        Assert.Equal(idA, check.Things.Single(t => t.Code == codeA).Id);
        Assert.Equal(idB, check.Things.Single(t => t.Code == codeB).Id);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task Shifting_positions_in_a_compound_unique_index_succeeds_whatever_order_the_documents_were_loaded_in(
        bool loadAscending, bool async)
    {
        var group = NewCode();
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();

        using (var seed = CreateContext())
        {
            for (var i = 0; i < ids.Length; i++)
            {
                seed.Slots.Add(new Slot { Id = ids[i], Group = group, Position = i });
            }

            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            var order = loadAscending ? Enumerable.Range(0, ids.Length) : Enumerable.Range(0, ids.Length).Reverse();
            foreach (var i in order.ToList())
            {
                db.Slots.Single(x => x.Id == ids[i]).Position = i + 1;
            }

            await Save(db, async);
        }

        using var check = CreateContext();
        for (var i = 0; i < ids.Length; i++)
        {
            Assert.Equal(ids[i], check.Slots.Single(x => x.Group == group && x.Position == i + 1).Id);
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task Documents_of_different_types_in_one_collection_hand_a_unique_value_over_whatever_order_they_were_loaded_in(
        bool catLoadedFirst, bool async)
    {
        var givenCode = NewCode();
        var catId = Guid.NewGuid();
        var dogId = Guid.NewGuid();
        var newCode = NewCode();

        using (var seed = CreateContext())
        {
            seed.Animals.AddRange(new Cat { Id = catId, Code = givenCode }, new Dog { Id = dogId, Code = NewCode() });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            Animal cat, dog;
            if (catLoadedFirst)
            {
                cat = db.Animals.Single(a => a.Id == catId);
                dog = db.Animals.Single(a => a.Id == dogId);
            }
            else
            {
                dog = db.Animals.Single(a => a.Id == dogId);
                cat = db.Animals.Single(a => a.Id == catId);
            }

            cat.Code = newCode;
            dog.Code = givenCode;

            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.IsType<Cat>(check.Animals.Single(a => a.Code == newCode));
        Assert.IsType<Dog>(check.Animals.Single(a => a.Code == givenCode));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Removing_a_document_of_one_type_and_adding_another_type_with_its_unique_value_succeeds(bool async)
    {
        var code = NewCode();
        var catId = Guid.NewGuid();
        var dogId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Animals.Add(new Cat { Id = catId, Code = code });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Add(new Dog { Id = dogId, Code = code });
            db.Remove(db.Animals.Single(a => a.Id == catId));
            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.IsType<Dog>(check.Animals.Single(a => a.Code == code));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replacing_a_document_in_the_same_save_that_changes_another_documents_owned_collection_succeeds(bool async)
    {
        var code = NewCode();
        var removedId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Baskets.AddRange(
                new Basket { Id = removedId, Code = code },
                new Basket { Id = ownerId, Code = NewCode(), Items = [new BasketItem { Name = "first" }] });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Add(new Basket { Id = newId, Code = code });
            var removed = db.Baskets.Single(b => b.Id == removedId);
            var owner = db.Baskets.Single(b => b.Id == ownerId);

            // Only the owned collection changes, so the owner is promoted to Modified when the changes are saved.
            owner.Items.Add(new BasketItem { Name = "second" });
            db.Remove(removed);

            await Save(db, async);
        }

        using var check = CreateContext();
        Assert.Equal(newId, check.Baskets.Single(b => b.Code == code).Id);
        Assert.Equal(["first", "second"], check.Baskets.Single(b => b.Id == ownerId).Items.Select(i => i.Name));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Without_a_transaction_a_failure_leaves_the_collections_before_it_applied_in_the_order_they_first_appear(bool async)
    {
        var thingCode = NewCode();
        var widgetCode = NewCode();
        var oldThingId = Guid.NewGuid();
        var newThingId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = oldThingId, Code = thingCode });
            seed.Widgets.Add(new Widget { Id = Guid.NewGuid(), Code = widgetCode });
            await Save(seed, async);
        }

        using (var db = CreateContext())
        {
            db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

            // Things appears first, so its delete and insert are written, then the Widget insert fails on its duplicate.
            db.Add(new Thing { Id = newThingId, Code = thingCode });
            db.Add(new Widget { Id = Guid.NewGuid(), Code = widgetCode });
            db.Remove(db.Things.Single(t => t.Id == oldThingId));

            if (async)
            {
                await Assert.ThrowsAsync<MongoBulkWriteException<BsonDocument>>(() => db.SaveChangesAsync());
            }
            else
            {
                Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => db.SaveChanges());
            }
        }

        using var check = CreateContext();
        Assert.Equal(newThingId, check.Things.Single(t => t.Code == thingCode).Id);
        Assert.Single(check.Widgets.Where(w => w.Code == widgetCode));
    }

    private sealed class RecordingInterceptor(bool suppress) : SaveChangesInterceptor
    {
        public List<ConcurrencyExceptionEventData> Calls { get; } = [];

        public override InterceptionResult ThrowingConcurrencyException(
            ConcurrencyExceptionEventData eventData, InterceptionResult result)
        {
            Calls.Add(eventData);
            return suppress ? InterceptionResult.Suppress() : result;
        }

        public override ValueTask<InterceptionResult> ThrowingConcurrencyExceptionAsync(
            ConcurrencyExceptionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Calls.Add(eventData);
            return new ValueTask<InterceptionResult>(suppress ? InterceptionResult.Suppress() : result);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_concurrency_conflict_found_in_a_failed_write_goes_to_the_interceptor_without_the_driver_exception(bool async)
    {
        var code = NewCode();
        var oldId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = oldId, Code = code });
            await Save(seed, async);
        }

        var interceptor = new RecordingInterceptor(suppress: false);
        using var db = CreateContext(interceptor: interceptor);
        var old = db.Things.Single(t => t.Id == oldId);

        using (var other = CreateContext())
        {
            other.Things.Single(t => t.Id == oldId).Version = 1;
            await Save(other, async);
        }

        db.Remove(old);
        db.Add(new Thing { Id = Guid.NewGuid(), Code = code });

        var exception = async
            ? await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync())
            : Assert.Throws<DbUpdateConcurrencyException>(() => db.SaveChanges());

        // The thrown exception keeps the driver's as its inner exception. The interceptor, like the log, gets one without it:
        // the driver's message holds the values of the duplicate key.
        Assert.IsAssignableFrom<MongoBulkWriteException>(exception.InnerException);
        var call = Assert.Single(interceptor.Calls);
        Assert.Null(call.Exception.InnerException);
        Assert.DoesNotContain(code, call.Exception.Message);
        Assert.Single(call.Entries);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_interceptor_that_suppresses_a_concurrency_conflict_found_in_a_failed_write_still_gets_the_write_failure(
        bool async)
    {
        var code = NewCode();
        var oldId = Guid.NewGuid();

        using (var seed = CreateContext())
        {
            seed.Things.Add(new Thing { Id = oldId, Code = code });
            await Save(seed, async);
        }

        var interceptor = new RecordingInterceptor(suppress: true);
        using var db = CreateContext(interceptor: interceptor);
        var old = db.Things.Single(t => t.Id == oldId);

        using (var other = CreateContext())
        {
            other.Things.Single(t => t.Id == oldId).Version = 1;
            await Save(other, async);
        }

        db.Remove(old);
        db.Add(new Thing { Id = Guid.NewGuid(), Code = code });

        // The write failed, so there is nothing to carry on with: the failure itself is reported.
        if (async)
        {
            await Assert.ThrowsAsync<MongoBulkWriteException<BsonDocument>>(() => db.SaveChangesAsync());
        }
        else
        {
            Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => db.SaveChanges());
        }

        Assert.Single(interceptor.Calls);
    }
}
