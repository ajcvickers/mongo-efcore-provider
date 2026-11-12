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

using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Storage;

public class TransactionTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    class SimpleEntity
    {
        public ObjectId _id { get; set; }
        public string name { get; set; }
    }

    [Fact]
    public void Explicit_transactions_can_be_used_around_SaveChanges()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using (var db = SingleEntityDbContext.Create(collection))
        {
            db.Entities.Add(new SimpleEntity { name = "test" });
            db.SaveChanges();
        }

        using (var db = SingleEntityDbContext.Create(collection))
        {
            db.Database.BeginTransaction();
            var entity = db.Entities.First();
            entity.name = "updated";
            db.SaveChanges();
            db.Database.CommitTransaction();
        }

        using (var db = SingleEntityDbContext.Create(collection))
        {
            var entity = db.Entities.First();
            Assert.Equal("updated", entity.name);
        }
    }

    [Fact]
    public async Task Explicit_transactions_can_be_used_around_SaveChangesAsync()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using (var db = SingleEntityDbContext.Create(collection))
        {
            await db.Entities.AddAsync(new SimpleEntity { name = "test" });
            await db.SaveChangesAsync();
        }

        await using (var db = SingleEntityDbContext.Create(collection))
        {
            await db.Database.BeginTransactionAsync();
            var entity = await db.Entities.FirstOrDefaultAsync();
            entity!.name = "updated";
            await db.SaveChangesAsync();
            await db.Database.CommitTransactionAsync();
        }

        await using (var db = SingleEntityDbContext.Create(collection))
        {
            var entity = await db.Entities.FirstOrDefaultAsync();
            Assert.Equal("updated", entity!.name);
        }
    }

    [Fact]
    public void Explicit_transactions_with_TransactionOptions_can_be_used_around_SaveChanges()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();

        using (var db = SingleEntityDbContext.Create(collection))
        {
            db.Entities.Add(new SimpleEntity { name = "test" });
            db.SaveChanges();
        }

        using (var db = SingleEntityDbContext.Create(collection, loggerFactory))
        {
            var options = new Driver.TransactionOptions(readConcern: new Optional<ReadConcern>(ReadConcern.Majority));
            using var transaction = db.Database.BeginTransaction(options);
            var entity = db.Entities.First();
            entity.name = "updated";
            db.SaveChanges();
            transaction.Commit();
        }

        // Verify logging including read concern
        var beginTransactionMessage = spyLogger.GetLogMessageByEventId(MongoEventId.TransactionStarting);
        Assert.Contains("Beginning transaction", beginTransactionMessage);
        Assert.Contains(" ReadConcern '{ \"level\" : \"majority\" }'", beginTransactionMessage);
        Assert.Contains("Began transaction", spyLogger.GetLogMessageByEventId(MongoEventId.TransactionStarted));
        Assert.Contains("Committing transaction.", spyLogger.GetLogMessageByEventId(MongoEventId.TransactionCommitting));
        Assert.Contains("Committed transaction.", spyLogger.GetLogMessageByEventId(MongoEventId.TransactionCommitted));

        using (var db = SingleEntityDbContext.Create(collection))
        {
            var entity = db.Entities.First();
            Assert.Equal("updated", entity.name);
        }
    }

    [Fact]
    public async Task Explicit_transactions_with_TransactionOptions_can_be_used_around_SaveChangesAsync()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();

        await using (var db = SingleEntityDbContext.Create(collection))
        {
            await db.Entities.AddAsync(new SimpleEntity { name = "test" });
            await db.SaveChangesAsync();
        }

        await using (var db = SingleEntityDbContext.Create(collection, loggerFactory))
        {
            var options = new Driver.TransactionOptions(readConcern: new Optional<ReadConcern>(ReadConcern.Majority));
            var transaction = await db.Database.BeginTransactionAsync(options);
            var entity = await db.Entities.FirstOrDefaultAsync();
            entity!.name = "updated";
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Verify logging including read concern
        var beginTransactionMessage = spyLogger.GetLogMessageByEventId(MongoEventId.TransactionStarting);
        Assert.Contains("Beginning transaction", beginTransactionMessage);
        Assert.Contains(" ReadConcern '{ \"level\" : \"majority\" }'", beginTransactionMessage);
        Assert.Contains("Began transaction", spyLogger.GetLogMessageByEventId(MongoEventId.TransactionStarted));
        Assert.Contains("Committing transaction.", spyLogger.GetLogMessageByEventId(MongoEventId.TransactionCommitting));
        Assert.Contains("Committed transaction.", spyLogger.GetLogMessageByEventId(MongoEventId.TransactionCommitted));

        await using (var db = SingleEntityDbContext.Create(collection))
        {
            var entity = await db.Entities.FirstOrDefaultAsync();
            Assert.Equal("updated", entity!.name);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Explicit_transactions_with_manual_accept_changes_are_supported(bool shouldSucceed)
    {
        var collection = database.CreateCollection<SimpleEntity>(values: [shouldSucceed]);

        using var db = SingleEntityDbContext.Create(collection);
        var transaction = db.Database.BeginTransaction();
        var entity = new SimpleEntity { name = "test" };
        db.Entities.Add(entity);
        db.SaveChanges(false);

        if (shouldSucceed)
        {
            transaction.Commit();
            db.ChangeTracker.AcceptAllChanges();
            using var dbCheck = SingleEntityDbContext.Create(collection);
            Assert.Equal("test", Assert.Single(db.Entities).name);
        }
        else
        {
            transaction.Rollback();
            using var dbCheck = SingleEntityDbContext.Create(collection);
            Assert.Empty(db.Entities);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Explicit_transactions_with_manual_accept_changes_are_supported_async(bool shouldSucceed)
    {
        var collection = database.CreateCollection<SimpleEntity>(values: [shouldSucceed]);

        await using var db = SingleEntityDbContext.Create(collection);
        var transaction = await db.Database.BeginTransactionAsync();
        var entity = new SimpleEntity { name = "test" };
        db.Entities.Add(entity);
        await db.SaveChangesAsync(false);

        if (shouldSucceed)
        {
            await transaction.CommitAsync();
            db.ChangeTracker.AcceptAllChanges();
            await using var dbCheck = SingleEntityDbContext.Create(collection);
            Assert.Equal("test", Assert.Single(db.Entities).name);
        }
        else
        {
            await transaction.RollbackAsync();
            await using var dbCheck = SingleEntityDbContext.Create(collection);
            Assert.Empty(db.Entities);
        }
    }

    [Fact]
    public void Implicit_transactions_are_used_for_multiple_root_operations_and_rollback_on_error()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        // Pre-insert a conflicting document to cause duplicate key on the second insert
        var conflictingId = ObjectId.GenerateNewId();
        collection.InsertOne(new SimpleEntity { _id = conflictingId, name = "existing" });

        // Ensure implicit transactions are enabled when needed (default is WhenNeeded)
        using (var db = SingleEntityDbContext.Create(collection))
        {
            db.Database.AutoTransactionBehavior = AutoTransactionBehavior.WhenNeeded;

            db.Entities.AddRange(
                new SimpleEntity { _id = ObjectId.GenerateNewId(), name = "ok-1" }, // will succeed if executed alone
                new SimpleEntity { _id = conflictingId, name = "dup-key-should-fail" }); // will throw duplicate key

            // SaveChanges should throw and roll back the successful first insert due to implicit transaction
            Assert.ThrowsAny<Exception>(() => db.SaveChanges());
        }

        // Only the original pre-insert should remain; the attempted batch should be fully rolled back
        using (var dbCheck = SingleEntityDbContext.Create(collection))
        {
            var all = dbCheck.Entities.ToList();
            Assert.Single(all);
            Assert.Equal("existing", all[0].name);
        }
    }

    [Fact]
    public async Task Implicit_transactions_are_used_for_multiple_root_operations_and_rollback_on_error_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        var conflictingId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new SimpleEntity { _id = conflictingId, name = "existing" });

        await using (var db = SingleEntityDbContext.Create(collection))
        {
            db.Database.AutoTransactionBehavior = AutoTransactionBehavior.WhenNeeded;

            await db.Entities.AddRangeAsync(
                new SimpleEntity { _id = ObjectId.GenerateNewId(), name = "ok-1" },
                new SimpleEntity { _id = conflictingId, name = "dup-key-should-fail" });

            await Assert.ThrowsAnyAsync<Exception>(async () => await db.SaveChangesAsync());
        }

        await using (var dbCheck = SingleEntityDbContext.Create(collection))
        {
            var all = await dbCheck.Entities.ToListAsync();
            Assert.Single(all);
            Assert.Equal("existing", all[0].name);
        }
    }

    [Fact]
    public void Ambient_TransactionScope_blocks_begin_explicit_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var db = SingleEntityDbContext.Create(collection);
        using var scope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);

        var ex = Assert.Throws<InvalidOperationException>(() => db.Database.BeginTransaction());
        Assert.Contains("ambient transaction", ex.Message, StringComparison.OrdinalIgnoreCase);

        scope.Complete();
    }

    [Fact]
    public void Database_EnlistTransaction_is_not_supported()
    {
        using var db = SingleEntityDbContext.Create(database.CreateCollection<SimpleEntity>());

        using var committable = new CommittableTransaction();
        var ex = Assert.Throws<NotSupportedException>(() => db.Database.EnlistTransaction(committable));
        Assert.Equal("The current provider doesn't support System.Transaction.", ex.Message);
    }

    [Fact]
    public void Nested_explicit_transactions_are_rejected()
    {
        using var db = SingleEntityDbContext.Create(database.CreateCollection<SimpleEntity>());
        using var transaction = db.Database.BeginTransaction();

        var ex = Assert.Throws<InvalidOperationException>(() => db.Database.BeginTransaction());
        Assert.Contains("already in a transaction", ex.Message, StringComparison.OrdinalIgnoreCase);

        transaction.Rollback();
    }

    [Fact]
    public void Multiple_SaveChanges_inside_single_explicit_transaction_are_atomic_until_commit()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var db = SingleEntityDbContext.Create(collection);
        using var transaction = db.Database.BeginTransaction();

        db.Entities.Add(new SimpleEntity { name = "first" });
        db.SaveChanges();

        db.Entities.Add(new SimpleEntity { name = "second" });
        db.SaveChanges();

        // Changes should not be visible outside the transaction
        using (var other = SingleEntityDbContext.Create(collection))
        {
            Assert.Empty(other.Entities);
        }

        transaction.Commit();

        using var dbCheck = SingleEntityDbContext.Create(collection);
        var names = dbCheck.Entities.Select(e => e.name).OrderBy(n => n).ToList();
        Assert.Equal(["first", "second"], names);
    }

    [Fact]
    public async Task Multiple_SaveChanges_inside_single_explicit_transaction_are_atomic_until_commit_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using var db = SingleEntityDbContext.Create(collection);
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.Entities.AddAsync(new SimpleEntity { name = "first" });
        await db.SaveChangesAsync();

        await db.Entities.AddAsync(new SimpleEntity { name = "second" });
        await db.SaveChangesAsync();

        await using (var other = SingleEntityDbContext.Create(collection))
        {
            Assert.Empty(await other.Entities.ToListAsync());
        }

        await transaction.CommitAsync();

        await using var dbCheck = SingleEntityDbContext.Create(collection);
        var names = (await dbCheck.Entities.Select(e => e.name).ToListAsync()).OrderBy(n => n).ToList();
        Assert.Equal(["first", "second"], names);
    }

    [Fact]
    public void SaveChanges_throw_inside_explicit_transaction_does_not_affect_db_until_commit_and_rollback_discards()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        // Pre-insert to cause duplicate key on second insert inside the transaction
        var conflictingId = ObjectId.GenerateNewId();
        collection.InsertOne(new SimpleEntity { _id = conflictingId, name = "existing" });

        using var db = SingleEntityDbContext.Create(collection);
        using var transaction = db.Database.BeginTransaction();

        db.Entities.AddRange(
            new SimpleEntity { _id = ObjectId.GenerateNewId(), name = "transaction-ok" },
            new SimpleEntity { _id = conflictingId, name = "transaction-dup" });

        // Act: SaveChanges should throw, but nothing should become visible to other contexts yet.
        Assert.ThrowsAny<Exception>(() => db.SaveChanges());

        // The same DbContext (same transaction) can still see its own pending changes if it were to query
        // (MongoDB read-your-own-writes within a transaction). We only assert external visibility here.
        using (var outsider = SingleEntityDbContext.Create(collection))
        {
            var names = outsider.Entities.Select(e => e.name).ToList();
            Assert.Contains("existing", names); // original
            Assert.DoesNotContain("transaction-ok", names); // not visible outside the transaction
            Assert.DoesNotContain("transaction-dup", names);
        }

        // Rollback explicitly; the staged writes should be discarded.
        transaction.Rollback();

        using var check = SingleEntityDbContext.Create(collection);
        var final = check.Entities.Select(e => e.name).OrderBy(n => n).ToList();
        Assert.Equal(["existing"], final);
    }

    [Fact]
    public async Task SaveChangesAsync_throw_inside_explicit_transaction_does_not_affect_db_until_commit_and_rollback_discards()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        var conflictingId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new SimpleEntity { _id = conflictingId, name = "existing" });

        await using var db = SingleEntityDbContext.Create(collection);
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.Entities.AddRangeAsync(
            new SimpleEntity { _id = ObjectId.GenerateNewId(), name = "transaction-ok" },
            new SimpleEntity { _id = conflictingId, name = "transaction-dup" });

        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());

        await using (var outsider = SingleEntityDbContext.Create(collection))
        {
            var names = await outsider.Entities.Select(e => e.name).ToListAsync();
            Assert.Contains("existing", names);
            Assert.DoesNotContain("transaction-ok", names);
            Assert.DoesNotContain("transaction-dup", names);
        }

        await transaction.RollbackAsync();

        await using var check = SingleEntityDbContext.Create(collection);
        var final = (await check.Entities.Select(e => e.name).ToListAsync()).OrderBy(n => n).ToList();
        Assert.Equal(["existing"], final);
    }

    [Fact]
    public void Same_context_reads_its_own_writes_inside_explicit_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var db = SingleEntityDbContext.Create(collection);
        using var transaction = db.Database.BeginTransaction();

        db.Entities.Add(new SimpleEntity { name = "first" });
        db.SaveChanges();

        // Same context in same transaction sees the inserted doc
        Assert.Equal("first", Assert.Single(db.Entities).name);

        db.Entities.Add(new SimpleEntity { name = "second" });
        db.SaveChanges();

        // Projections visible inside same transaction
        Assert.Equal(["first", "second"], db.Entities.Select(e => e.name).OrderBy(n => n));
        Assert.Equal(2, db.Entities.Count());

        // Entity reads visible inside same transaction
        Assert.Single(db.Entities, e => e.name == "first");
        Assert.Single(db.Entities, e => e.name == "second");

        // Other context still does not see them
        using (var outsider = SingleEntityDbContext.Create(collection))
        {
            Assert.Empty(outsider.Entities);
        }

        transaction.Rollback();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Empty(check.Entities);
    }

    [Fact]
    public async Task Same_context_reads_its_own_writes_inside_explicit_transaction_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using var db = SingleEntityDbContext.Create(collection);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Entities.AddAsync(new SimpleEntity { name = "first" });
        await db.SaveChangesAsync();

        // Same context in same transaction sees the inserted doc
        Assert.Equal("first", Assert.Single(db.Entities).name);

        await db.Entities.AddAsync(new SimpleEntity { name = "second" });
        await db.SaveChangesAsync();

        // Projections visible inside same transaction
        Assert.Equal(["first", "second"], db.Entities.Select(e => e.name).OrderBy(n => n));
        Assert.Equal(2, await db.Entities.CountAsync());

        // Entity reads visible inside same transaction
        Assert.Single(db.Entities, e => e.name == "first");
        Assert.Single(db.Entities, e => e.name == "second");

        // Other context still does not see them
        await using (var outsider = SingleEntityDbContext.Create(collection))
        {
            Assert.Empty(outsider.Entities);
        }

        await transaction.RollbackAsync();

        await using var check = SingleEntityDbContext.Create(collection);
        Assert.Empty(check.Entities);
    }

    [Fact]
    public void Disposing_explicit_transaction_without_commit_or_rollback_does_not_commit()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var db = SingleEntityDbContext.Create(collection);
        using var transaction = db.Database.BeginTransaction();
        db.Entities.Add(new SimpleEntity { name = "pending" });
        db.SaveChanges();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Empty(check.Entities);
    }

    [Fact]
    public async Task Disposing_explicit_transaction_without_commit_or_rollback_does_not_commit_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using var db = SingleEntityDbContext.Create(collection);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Entities.AddAsync(new SimpleEntity { name = "pending" });
        await db.SaveChangesAsync();

        await using var check = SingleEntityDbContext.Create(collection);
        Assert.Empty(await check.Entities.ToListAsync());
    }

    [Fact]
    public void Can_reuse_context_without_transaction_after_committed_explicit_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var db = SingleEntityDbContext.Create(collection);

        // Seed one entity outside any transaction
        db.Entities.Add(new SimpleEntity { name = "seed" });
        db.SaveChanges();

        // First explicit transaction - commit
        using (var transaction = db.Database.BeginTransaction())
        {
            db.Entities.Add(new SimpleEntity { name = "transaction-committed" });
            db.SaveChanges();
            transaction.Commit();
        }

        // Reuse the same context without an explicit transaction
        db.Entities.Add(new SimpleEntity { name = "post-commit-no-transaction" });
        db.SaveChanges();

        // Validate all expected entities are present
        using var check = SingleEntityDbContext.Create(collection);
        var names = check.Entities.Select(e => e.name).OrderBy(n => n).ToList();
        Assert.Equal(["post-commit-no-transaction", "seed", "transaction-committed"], names);
    }

    [Fact]
    public void Can_reuse_context_without_transaction_after_rolled_back_explicit_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var db = SingleEntityDbContext.Create(collection);

        // Start explicit transaction - rollback
        using (var transaction = db.Database.BeginTransaction())
        {
            db.Entities.Add(new SimpleEntity { name = "transaction-rolled-back" });
            db.SaveChanges();
            transaction.Rollback();
        }

        // Reuse the same context without an explicit transaction
        db.Entities.Add(new SimpleEntity { name = "post-rollback-no-transaction" });
        db.SaveChanges();

        using var check = SingleEntityDbContext.Create(collection);
        var names = check.Entities.Select(e => e.name).OrderBy(n => n).ToList();
        Assert.Equal(["post-rollback-no-transaction"], names);
    }

    [Fact]
    public void Can_start_new_explicit_transaction_on_same_context_after_commit()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var db = SingleEntityDbContext.Create(collection);

        using (var transaction = db.Database.BeginTransaction())
        {
            db.Entities.Add(new SimpleEntity { name = "transaction1-committed" });
            db.SaveChanges();
            transaction.Commit();
        }

        using (var transaction = db.Database.BeginTransaction())
        {
            db.Entities.Add(new SimpleEntity { name = "transaction2-committed" });
            db.SaveChanges();
            transaction.Commit();
        }

        using var check = SingleEntityDbContext.Create(collection);
        var names = check.Entities.Select(e => e.name).OrderBy(n => n).ToList();
        Assert.Equal(["transaction1-committed", "transaction2-committed"], names);
    }

    [Fact]
    public void Can_start_new_explicit_transaction_on_same_context_after_rollback()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var db = SingleEntityDbContext.Create(collection);

        using (var transaction = db.Database.BeginTransaction())
        {
            db.Entities.Add(new SimpleEntity { name = "transaction1-rolled-back" });
            db.SaveChanges();
            transaction.Rollback();
        }

        using (var transaction = db.Database.BeginTransaction())
        {
            db.Entities.Add(new SimpleEntity { name = "transaction2-committed" });
            db.SaveChanges();
            transaction.Commit();
        }

        using var check = SingleEntityDbContext.Create(collection);
        var names = check.Entities.Select(e => e.name).OrderBy(n => n).ToList();
        Assert.Equal(["transaction2-committed"], names);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Can_execute_transaction_via_execution_strategy(bool succeeds)
    {
        var expectedCount = succeeds ? 2 : 0;
        var collection = database.CreateCollection<SimpleEntity>(values: [succeeds]);

        using (var db = SingleEntityDbContext.Create(collection))
        {
            var strategy = db.Database.CreateExecutionStrategy();

            try
            {
                strategy.ExecuteInTransaction(
                    db,
                    operation: context =>
                    {
                        Assert.NotNull(context.Entities);
                        context.Entities.Add(new SimpleEntity { name = "First" });
                        context.Entities.Add(new SimpleEntity { name = "Second" });
                        context.SaveChanges();
                        if (!succeeds) throw new Exception("Expected failure");
                    },
                    verifySucceeded: _ => true);
            }
            catch (Exception ex) when (ex.Message == "Expected failure")
            {
                // We expect this to fail
            }
        }

        using (var db = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(expectedCount, db.Entities.Count());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Can_execute_transaction_via_execution_strategy_async(bool succeeds)
    {
        var expectedCount = succeeds ? 2 : 0;
        var collection = database.CreateCollection<SimpleEntity>(values: [succeeds]);

        await using (var db = SingleEntityDbContext.Create(collection))
        {
            var strategy = db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteInTransactionAsync(
                    db,
                    operation: async (context, cancellationToken) =>
                    {
                        Assert.NotNull(context.Entities);
                        context.Entities.Add(new SimpleEntity { name = "First" });
                        context.Entities.Add(new SimpleEntity { name = "Second" });
                        await context.SaveChangesAsync(cancellationToken);
                        if (!succeeds) throw new Exception("Expected failure");
                    },
                    verifySucceeded: (_, _) => Task.FromResult(true));
            }
            catch (Exception ex) when (ex.Message == "Expected failure")
            {
                // We expect this to fail
            }
        }

        await using (var db = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(expectedCount, db.Entities.Count());
        }
    }

    // Ends the session's transaction without telling the MongoTransaction or the context, as happens when something else
    // holding the same session aborts or commits it.
    private static void EndTransactionBehindTheContextsBack(DbContext db)
        => ((MongoTransaction)db.Database.CurrentTransaction!).ActiveSession.AbortTransaction();

    [Fact]
    public void SaveChanges_after_the_transaction_session_was_aborted_throws_instead_of_writing_outside_a_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using (var db = SingleEntityDbContext.Create(collection))
        {
            db.Database.BeginTransaction();
            db.Entities.Add(new SimpleEntity { name = "test" });
            EndTransactionBehindTheContextsBack(db);

            var ex = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
            Assert.Contains("is no longer active", ex.Message);
            Assert.Contains(db.Database.CurrentTransaction!.TransactionId.ToString(), ex.Message);
        }

        using (var check = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, check.Entities.Count());
        }
    }

    [Fact]
    public async Task SaveChanges_after_the_transaction_session_was_aborted_throws_instead_of_writing_outside_a_transaction_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using (var db = SingleEntityDbContext.Create(collection))
        {
            await db.Database.BeginTransactionAsync();
            db.Entities.Add(new SimpleEntity { name = "test" });
            EndTransactionBehindTheContextsBack(db);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
            Assert.Contains("is no longer active", ex.Message);
            Assert.Contains(db.Database.CurrentTransaction!.TransactionId.ToString(), ex.Message);
        }

        await using (var check = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, await check.Entities.CountAsync());
        }
    }

    [Fact]
    public void Query_after_the_transaction_session_was_aborted_throws()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var db = SingleEntityDbContext.Create(collection);
        db.Database.BeginTransaction();
        EndTransactionBehindTheContextsBack(db);

        var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.ToList());
        Assert.Contains("is no longer active", ex.Message);
    }

#if !EF8
    [Fact]
    public void ExecuteDelete_after_the_transaction_session_was_aborted_throws()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        collection.InsertOne(new SimpleEntity { name = "test" });

        using (var db = SingleEntityDbContext.Create(collection))
        {
            db.Database.BeginTransaction();
            EndTransactionBehindTheContextsBack(db);

            var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.ExecuteDelete());
            Assert.Contains("is no longer active", ex.Message);
        }

        using (var check = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(1, check.Entities.Count());
        }
    }
#endif

    [Fact]
    public void Disposing_a_transaction_whose_session_was_already_aborted_does_not_throw()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var db = SingleEntityDbContext.Create(collection);
        var transaction = db.Database.BeginTransaction();
        EndTransactionBehindTheContextsBack(db);

        transaction.Dispose();

        // The context must have let go of the disposed transaction and be usable again.
        Assert.Null(db.Database.CurrentTransaction);
        using var next = db.Database.BeginTransaction();
        next.Rollback();
    }

    [Fact]
    public async Task Disposing_a_transaction_whose_session_was_already_aborted_does_not_throw_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using var db = SingleEntityDbContext.Create(collection);
        var transaction = await db.Database.BeginTransactionAsync();
        EndTransactionBehindTheContextsBack(db);

        await transaction.DisposeAsync();

        Assert.Null(db.Database.CurrentTransaction);
        await using var next = await db.Database.BeginTransactionAsync();
        await next.RollbackAsync();
    }

    private static IClientSessionHandle StartSessionWithTransaction(IMongoClient client)
    {
        var session = client.StartSession();
        session.StartTransaction();
        return session;
    }

    private sealed class StubTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public void Commit() { }
        public void Rollback() { }
        public void Dispose() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public void UseTransaction_with_a_session_enlists_the_context_and_leaves_the_session_to_its_owner()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var session = StartSessionWithTransaction(database.Client);

        using (var db = SingleEntityDbContext.Create(collection))
        {
            var transaction = db.Database.UseTransaction(session);
            Assert.Same(transaction, db.Database.CurrentTransaction);

            db.Entities.Add(new SimpleEntity { name = "enlisted" });
            db.SaveChanges();

            using var outsider = SingleEntityDbContext.Create(collection);
            Assert.Equal(0, outsider.Entities.Count());
        }

        Assert.True(session.IsInTransaction);
        session.CommitTransaction();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(1, check.Entities.Count());
    }

    [Fact]
    public void Two_contexts_can_share_one_transaction_and_disposing_one_does_not_end_it()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();

        using (var second = SingleEntityDbContext.Create(collection))
        {
            second.Database.UseTransaction(owner.Database.CurrentTransaction!);
            second.Entities.Add(new SimpleEntity { name = "second" });
            second.SaveChanges();
        }

        owner.Entities.Add(new SimpleEntity { name = "owner" });
        owner.SaveChanges();

        using (var third = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, third.Entities.Count());
        }

        owner.Database.CommitTransaction();

        using var fourth = SingleEntityDbContext.Create(collection);
        Assert.Equal(["owner", "second"], fourth.Entities.Select(e => e.name).OrderBy(n => n).ToList());
    }

    [Fact]
    public void Rolling_back_the_owner_discards_writes_of_enlisted_contexts_and_their_next_SaveChanges_throws()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();

        using var enlisted = SingleEntityDbContext.Create(collection);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);
        enlisted.Entities.Add(new SimpleEntity { name = "enlisted" });
        enlisted.SaveChanges();

        owner.Database.RollbackTransaction();

        using (var check = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, check.Entities.Count());
        }

        enlisted.Entities.Add(new SimpleEntity { name = "too-late" });
        var ex = Assert.Throws<InvalidOperationException>(() => enlisted.SaveChanges());
        Assert.Contains("is no longer active", ex.Message);

        using (var check = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, check.Entities.Count());
        }
    }

    [Fact]
    public void Commit_and_Rollback_are_rejected_on_an_enlisted_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var session = StartSessionWithTransaction(database.Client);

        using var db = SingleEntityDbContext.Create(collection);
        var transaction = db.Database.UseTransaction(session);
        db.Entities.Add(new SimpleEntity { name = "enlisted" });
        db.SaveChanges();

        var ex = Assert.Throws<InvalidOperationException>(() => transaction.Commit());
        Assert.Contains("not started by this context", ex.Message);
        Assert.Contains("the code that supplied the session", ex.Message);
        ex = Assert.Throws<InvalidOperationException>(() => transaction.Rollback());
        Assert.Contains("not started by this context", ex.Message);
        ex = Assert.Throws<InvalidOperationException>(() => db.Database.CommitTransaction());
        Assert.Contains("not started by this context", ex.Message);
        ex = Assert.Throws<InvalidOperationException>(() => db.Database.RollbackTransaction());
        Assert.Contains("not started by this context", ex.Message);

        Assert.Same(transaction, db.Database.CurrentTransaction);
        Assert.True(session.IsInTransaction);

        session.CommitTransaction();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(1, check.Entities.Count());
    }

    [Fact]
    public async Task Commit_and_Rollback_are_rejected_on_an_enlisted_transaction_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using var owner = SingleEntityDbContext.Create(collection);
        await owner.Database.BeginTransactionAsync();

        await using var db = SingleEntityDbContext.Create(collection);
        var transaction = db.Database.UseTransaction(owner.Database.CurrentTransaction!);
        db.Entities.Add(new SimpleEntity { name = "enlisted" });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        Assert.Contains("not started by this context", ex.Message);
        Assert.Contains(nameof(SingleEntityDbContext<SimpleEntity>), ex.Message);
        Assert.Contains(owner.Database.CurrentTransaction!.TransactionId.ToString(), ex.Message);
        ex = await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.RollbackAsync());
        Assert.Contains("not started by this context", ex.Message);
        ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Database.CommitTransactionAsync());
        Assert.Contains("not started by this context", ex.Message);
        ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Database.RollbackTransactionAsync());
        Assert.Contains("not started by this context", ex.Message);

        Assert.Same(transaction, db.Database.CurrentTransaction);

        await owner.Database.CommitTransactionAsync();

        await using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(1, await check.Entities.CountAsync());
    }

    [Fact]
    public void Disposing_the_returned_transaction_detaches_without_ending_the_session()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var session = StartSessionWithTransaction(database.Client);

        using var db = SingleEntityDbContext.Create(collection);
        using (db.Database.UseTransaction(session))
        {
            Assert.NotNull(db.Database.CurrentTransaction);
        }

        Assert.Null(db.Database.CurrentTransaction);
        Assert.True(session.IsInTransaction);

        // The context can enlist again, and the session is still usable by its owner.
        using (db.Database.UseTransaction(session))
        {
        }

        session.CommitTransaction();
    }

    [Fact]
    public async Task Disposing_the_returned_transaction_detaches_without_ending_the_session_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var session = StartSessionWithTransaction(database.Client);

        await using var db = SingleEntityDbContext.Create(collection);
        await using (db.Database.UseTransaction(session))
        {
            Assert.NotNull(db.Database.CurrentTransaction);
        }

        Assert.Null(db.Database.CurrentTransaction);
        Assert.True(session.IsInTransaction);

        session.CommitTransaction();
    }

    [Fact]
    public void UseTransaction_rejects_invalid_arguments_without_changing_state()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var db = SingleEntityDbContext.Create(collection);

        // A session with no transaction in progress
        using (var plainSession = database.Client.StartSession())
        {
            var ex = Assert.Throws<InvalidOperationException>(() => db.Database.UseTransaction(plainSession));
            Assert.Contains("active transaction", ex.Message);
            Assert.Null(db.Database.CurrentTransaction);
        }

        // A transaction that is not a MongoDB transaction
        var stubEx = Assert.Throws<InvalidOperationException>(() => db.Database.UseTransaction(new StubTransaction()));
        Assert.Contains("MongoDB transaction", stubEx.Message);
        Assert.Null(db.Database.CurrentTransaction);

        // Null arguments
        Assert.Throws<ArgumentNullException>(() => db.Database.UseTransaction((IClientSessionHandle)null!));
        Assert.Throws<ArgumentNullException>(() => db.Database.UseTransaction((IDbContextTransaction)null!));
        Assert.Null(db.Database.CurrentTransaction);

        // A transaction that has already ended
        using (var ownerDb = SingleEntityDbContext.Create(collection))
        {
            var ended = ownerDb.Database.BeginTransaction();
            ownerDb.Database.RollbackTransaction();

            var ex = Assert.Throws<InvalidOperationException>(() => db.Database.UseTransaction(ended));
            Assert.Contains("is no longer active", ex.Message);
            Assert.Null(db.Database.CurrentTransaction);
        }

        // The context is already in a transaction
        using var session = StartSessionWithTransaction(database.Client);
        var own = db.Database.BeginTransaction();

        var nestedEx = Assert.Throws<InvalidOperationException>(() => db.Database.UseTransaction(session));
        Assert.Contains("already in a transaction", nestedEx.Message);
        Assert.Same(own, db.Database.CurrentTransaction);

        nestedEx = Assert.Throws<InvalidOperationException>(() => db.Database.UseTransaction(own));
        Assert.Contains("already in a transaction", nestedEx.Message);
        Assert.Same(own, db.Database.CurrentTransaction);

        Assert.True(session.IsInTransaction);
        session.AbortTransaction();
        own.Rollback();
    }

    [Fact]
    public void Contexts_on_different_client_instances_can_share_a_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var otherClient = new MongoClient(database.Client.Settings.Clone());
        var otherCollection = otherClient
            .GetDatabase(collection.Database.DatabaseNamespace.DatabaseName)
            .GetCollection<SimpleEntity>(collection.CollectionNamespace.CollectionName);

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();

        using var second = SingleEntityDbContext.Create(otherCollection);
        second.Database.UseTransaction(owner.Database.CurrentTransaction!);

        owner.Entities.Add(new SimpleEntity { name = "owner" });
        owner.SaveChanges();
        second.Entities.Add(new SimpleEntity { name = "second" });
        second.SaveChanges();

        owner.Database.CommitTransaction();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["owner", "second"], check.Entities.Select(e => e.name).OrderBy(n => n).ToList());
    }

    [Fact]
    public void Enlisted_context_queries_run_inside_the_shared_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        owner.Entities.Add(new SimpleEntity { name = "uncommitted" });
        owner.SaveChanges();

        using var enlisted = SingleEntityDbContext.Create(collection);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);
        Assert.Equal(1, enlisted.Entities.Count());

        using (var outsider = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, outsider.Entities.Count());
        }

        owner.Database.RollbackTransaction();
    }

#if !EF8
    [Fact]
    public void Enlisted_context_bulk_deletes_run_inside_the_shared_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        collection.InsertMany([new SimpleEntity { name = "committed-1" }, new SimpleEntity { name = "committed-2" }]);

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        owner.Entities.Add(new SimpleEntity { name = "uncommitted" });
        owner.SaveChanges();

        using var enlisted = SingleEntityDbContext.Create(collection);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);
        Assert.Equal(3, enlisted.Entities.ExecuteDelete());
        Assert.Equal(0, enlisted.Entities.Count());

        using (var outsider = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(2, outsider.Entities.Count());
        }

        owner.Database.RollbackTransaction();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["committed-1", "committed-2"], check.Entities.Select(e => e.name).OrderBy(n => n).ToList());
    }
#endif

    [Fact]
    public void A_failed_write_in_a_shared_transaction_aborts_it_and_nothing_is_persisted()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        var id = ObjectId.GenerateNewId();

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        owner.Entities.Add(new SimpleEntity { _id = id, name = "a" });
        owner.SaveChanges();

        using var enlisted = SingleEntityDbContext.Create(collection);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);
        enlisted.Entities.Add(new SimpleEntity { _id = id, name = "duplicate" });
        Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => enlisted.SaveChanges());

        // The server aborted the transaction. The provider can not know that, so the server's own error must surface unchanged.
        owner.Entities.Add(new SimpleEntity { name = "b" });
        var ex = Assert.Throws<MongoCommandException>(() => owner.SaveChanges());
        Assert.Contains("TransientTransactionError", ex.ErrorLabels);
        Assert.DoesNotContain("is no longer active", ex.Message);

        // The server aborted the transaction, so the owner's commit fails with the server's error, not the guard's.
        // (On a sharded cluster the failure of the owner's write may appear only here; this test targets a replica set.)
        Assert.Throws<MongoCommandException>(() => owner.Database.CommitTransaction());

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(0, check.Entities.Count());
    }

    [Fact]
    public void Enlisted_context_throws_when_the_caller_starts_a_new_transaction_on_the_same_session()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var session = StartSessionWithTransaction(database.Client);

        using var db = SingleEntityDbContext.Create(collection);
        db.Database.UseTransaction(session);
        db.Entities.Add(new SimpleEntity { name = "first-transaction" });

        // The caller ends the transaction the context enlisted in and starts another one on the same session.
        session.CommitTransaction();
        session.StartTransaction();
        try
        {
            Assert.True(session.IsInTransaction);

            var ex = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
            Assert.Contains("is no longer active", ex.Message);

            using var check = SingleEntityDbContext.Create(collection);
            Assert.Equal(0, check.Entities.Count());
        }
        finally
        {
            session.AbortTransaction();
        }

        using var after = SingleEntityDbContext.Create(collection);
        Assert.Equal(0, after.Entities.Count());
    }

    [Fact]
    public async Task Enlisted_context_throws_when_the_caller_starts_a_new_transaction_on_the_same_session_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var session = StartSessionWithTransaction(database.Client);

        await using var db = SingleEntityDbContext.Create(collection);
        db.Database.UseTransaction(session);
        db.Entities.Add(new SimpleEntity { name = "first-transaction" });

        await session.CommitTransactionAsync();
        session.StartTransaction();
        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
            Assert.Contains("is no longer active", ex.Message);
        }
        finally
        {
            await session.AbortTransactionAsync();
        }

        await using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(0, await check.Entities.CountAsync());
    }

    [Fact]
    public void Committing_the_owner_ends_the_transaction_for_enlisted_contexts_and_their_next_SaveChanges_throws()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        owner.Entities.Add(new SimpleEntity { name = "owner" });
        owner.SaveChanges();

        using var enlisted = SingleEntityDbContext.Create(collection);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);

        owner.Database.CommitTransaction();

        enlisted.Entities.Add(new SimpleEntity { name = "too-late" });
        var ex = Assert.Throws<InvalidOperationException>(() => enlisted.SaveChanges());
        Assert.Contains("is no longer active", ex.Message);

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["owner"], check.Entities.Select(e => e.name).ToList());
    }

    [Fact]
    public void Disposing_the_owners_transaction_without_committing_rolls_back_and_enlisted_contexts_SaveChanges_throws()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var enlisted = SingleEntityDbContext.Create(collection);
        using (var owner = SingleEntityDbContext.Create(collection))
        {
            // Disposing the transaction without committing rolls it back. (Disposing the owner context rolls it back too: see
            // Disposing_the_owner_context_without_disposing_its_transaction_rolls_it_back.)
            using var ownerTransaction = owner.Database.BeginTransaction();
            owner.Entities.Add(new SimpleEntity { name = "owner" });
            owner.SaveChanges();

            enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);
            enlisted.Entities.Add(new SimpleEntity { name = "enlisted" });
            enlisted.SaveChanges();
        }

        enlisted.Entities.Add(new SimpleEntity { name = "too-late" });
        var ex = Assert.Throws<InvalidOperationException>(() => enlisted.SaveChanges());
        Assert.Contains("is no longer active", ex.Message);

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(0, check.Entities.Count());
    }

    [Fact]
    public void Disposing_the_owner_context_without_disposing_its_transaction_rolls_it_back()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var participant = SingleEntityDbContext.Create(collection);
        var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        owner.Entities.Add(new SimpleEntity { name = "owner" });
        owner.SaveChanges();

        participant.Database.UseTransaction(owner.Database.CurrentTransaction!);
        participant.Entities.Add(new SimpleEntity { name = "participant" });
        participant.SaveChanges();

        // Neither the transaction nor the session is disposed by the caller, as relational EF: disposing the context ends it.
        owner.Dispose();

        using (var check = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, check.Entities.Count());
        }

        participant.Entities.Add(new SimpleEntity { name = "too-late" });
        var ex = Assert.Throws<InvalidOperationException>(() => participant.SaveChanges());
        Assert.Contains("is no longer active", ex.Message);

        using (var check = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, check.Entities.Count());
        }
    }

    [Fact]
    public async Task Disposing_the_owner_context_without_disposing_its_transaction_rolls_it_back_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using var participant = SingleEntityDbContext.Create(collection);
        var owner = SingleEntityDbContext.Create(collection);
        await owner.Database.BeginTransactionAsync();
        owner.Entities.Add(new SimpleEntity { name = "owner" });
        await owner.SaveChangesAsync();

        participant.Database.UseTransaction(owner.Database.CurrentTransaction!);
        participant.Entities.Add(new SimpleEntity { name = "participant" });
        await participant.SaveChangesAsync();

        await owner.DisposeAsync();

        await using (var check = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, await check.Entities.CountAsync());
        }

        participant.Entities.Add(new SimpleEntity { name = "too-late" });
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => participant.SaveChangesAsync());
        Assert.Contains("is no longer active", ex.Message);

        await using (var check = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, await check.Entities.CountAsync());
        }
    }

    [Fact]
    public void Disposing_a_participant_context_does_not_end_the_owner_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        owner.Entities.Add(new SimpleEntity { name = "owner" });
        owner.SaveChanges();

        var participant = SingleEntityDbContext.Create(collection);
        participant.Database.UseTransaction(owner.Database.CurrentTransaction!);
        participant.Entities.Add(new SimpleEntity { name = "participant" });
        participant.SaveChanges();
        participant.Dispose();

        // The owner's transaction is untouched: it can still write, and commit everything.
        owner.Entities.Add(new SimpleEntity { name = "owner-2" });
        owner.SaveChanges();
        owner.Database.CommitTransaction();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["owner", "owner-2", "participant"], check.Entities.Select(e => e.name).OrderBy(n => n).ToList());
    }

    [Fact]
    public async Task Disposing_a_participant_context_does_not_end_the_owner_transaction_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using var owner = SingleEntityDbContext.Create(collection);
        await owner.Database.BeginTransactionAsync();
        owner.Entities.Add(new SimpleEntity { name = "owner" });
        await owner.SaveChangesAsync();

        var participant = SingleEntityDbContext.Create(collection);
        participant.Database.UseTransaction(owner.Database.CurrentTransaction!);
        participant.Entities.Add(new SimpleEntity { name = "participant" });
        await participant.SaveChangesAsync();
        await participant.DisposeAsync();

        owner.Entities.Add(new SimpleEntity { name = "owner-2" });
        await owner.SaveChangesAsync();
        await owner.Database.CommitTransactionAsync();

        await using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["owner", "owner-2", "participant"], (await check.Entities.Select(e => e.name).ToListAsync()).Order().ToList());
    }

    [Fact]
    public void A_MongoTransaction_built_on_a_session_that_had_no_transaction_is_never_active()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var db = SingleEntityDbContext.Create(collection);
        var logger = db.GetService<IDiagnosticsLogger<DbLoggerCategory.Database.Transaction>>();

        using var session = database.Client.StartSession();
        var transaction = new MongoTransaction(session, db, Guid.NewGuid(), null, logger);

        // A transaction the session starts later is not the one the MongoTransaction was built for (it was built for none).
        session.StartTransaction();
        try
        {
            Assert.True(session.IsInTransaction);
            var ex = Assert.Throws<InvalidOperationException>(() => transaction.ActiveSession);
            Assert.Contains("is no longer active", ex.Message);
        }
        finally
        {
            session.AbortTransaction();
        }
    }

    // Dispose order of the owner context and a participant context, in both orders and with the participant's transaction disposed on
    // its own, none of which may throw whatever has already happened to the shared session.

    [Fact]
    public void Owner_disposed_first_then_participant_does_not_throw()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        owner.Entities.Add(new SimpleEntity { name = "owner" });
        owner.SaveChanges();

        var participant = SingleEntityDbContext.Create(collection);
        participant.Database.UseTransaction(owner.Database.CurrentTransaction!);
        participant.Entities.Add(new SimpleEntity { name = "participant" });
        participant.SaveChanges();

        // The owner's disposal rolls the transaction back and disposes the session the participant still refers to.
        owner.Dispose();
        participant.Dispose();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(0, check.Entities.Count());
    }

    [Fact]
    public async Task Owner_disposed_first_then_participant_does_not_throw_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        var owner = SingleEntityDbContext.Create(collection);
        await owner.Database.BeginTransactionAsync();
        owner.Entities.Add(new SimpleEntity { name = "owner" });
        await owner.SaveChangesAsync();

        var participant = SingleEntityDbContext.Create(collection);
        participant.Database.UseTransaction(owner.Database.CurrentTransaction!);
        participant.Entities.Add(new SimpleEntity { name = "participant" });
        await participant.SaveChangesAsync();

        await owner.DisposeAsync();
        await participant.DisposeAsync();

        await using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(0, await check.Entities.CountAsync());
    }

    [Fact]
    public void Participant_first_then_owner()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();

        var participant = SingleEntityDbContext.Create(collection);
        participant.Database.UseTransaction(owner.Database.CurrentTransaction!);
        participant.Entities.Add(new SimpleEntity { name = "participant" });
        participant.SaveChanges();
        participant.Dispose();

        owner.Database.CommitTransaction();
        owner.Dispose();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["participant"], check.Entities.Select(e => e.name).ToList());
    }

    [Fact]
    public async Task Participant_first_then_owner_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        var owner = SingleEntityDbContext.Create(collection);
        await owner.Database.BeginTransactionAsync();

        var participant = SingleEntityDbContext.Create(collection);
        participant.Database.UseTransaction(owner.Database.CurrentTransaction!);
        participant.Entities.Add(new SimpleEntity { name = "participant" });
        await participant.SaveChangesAsync();
        await participant.DisposeAsync();

        await owner.Database.CommitTransactionAsync();
        await owner.DisposeAsync();

        await using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["participant"], await check.Entities.Select(e => e.name).ToListAsync());
    }

    [Fact]
    public void Disposing_participant_transaction_after_the_owner_disposed_the_session_does_not_throw()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();

        using var participant = SingleEntityDbContext.Create(collection);
        var participantTransaction = participant.Database.UseTransaction(owner.Database.CurrentTransaction!);

        owner.Dispose();

        participantTransaction.Dispose();
        Assert.Null(participant.Database.CurrentTransaction);

        // A second dispose is a no-op.
        participantTransaction.Dispose();
        Assert.Null(participant.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Disposing_participant_transaction_after_the_owner_disposed_the_session_does_not_throw_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        var owner = SingleEntityDbContext.Create(collection);
        await owner.Database.BeginTransactionAsync();

        await using var participant = SingleEntityDbContext.Create(collection);
        var participantTransaction = participant.Database.UseTransaction(owner.Database.CurrentTransaction!);

        await owner.DisposeAsync();

        await participantTransaction.DisposeAsync();
        Assert.Null(participant.Database.CurrentTransaction);

        await participantTransaction.DisposeAsync();
        Assert.Null(participant.Database.CurrentTransaction);
    }

    // Async parity of the tests above that only had a synchronous form.

    [Fact]
    public async Task Query_after_the_transaction_session_was_aborted_throws_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using var db = SingleEntityDbContext.Create(collection);
        await db.Database.BeginTransactionAsync();
        EndTransactionBehindTheContextsBack(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Entities.ToListAsync());
        Assert.Contains("is no longer active", ex.Message);
    }

    [Fact]
    public async Task Two_contexts_can_share_one_transaction_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using var owner = SingleEntityDbContext.Create(collection);
        await owner.Database.BeginTransactionAsync();

        await using (var second = SingleEntityDbContext.Create(collection))
        {
            second.Database.UseTransaction(owner.Database.CurrentTransaction!);
            second.Entities.Add(new SimpleEntity { name = "second" });
            await second.SaveChangesAsync();
        }

        owner.Entities.Add(new SimpleEntity { name = "owner" });
        await owner.SaveChangesAsync();

        await using (var third = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(0, await third.Entities.CountAsync());
        }

        await owner.Database.CommitTransactionAsync();

        await using var fourth = SingleEntityDbContext.Create(collection);
        Assert.Equal(["owner", "second"], (await fourth.Entities.Select(e => e.name).ToListAsync()).Order().ToList());
    }

    [Fact]
    public async Task Committing_the_owner_then_SaveChangesAsync_on_the_enlisted_context_throws()
    {
        var collection = database.CreateCollection<SimpleEntity>();

        await using var owner = SingleEntityDbContext.Create(collection);
        await owner.Database.BeginTransactionAsync();
        owner.Entities.Add(new SimpleEntity { name = "owner" });
        await owner.SaveChangesAsync();

        await using var enlisted = SingleEntityDbContext.Create(collection);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);

        await owner.Database.CommitTransactionAsync();

        enlisted.Entities.Add(new SimpleEntity { name = "too-late" });
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => enlisted.SaveChangesAsync());
        Assert.Contains("is no longer active", ex.Message);

        await using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["owner"], await check.Entities.Select(e => e.name).ToListAsync());
    }

    // Pooled contexts are reset, not disposed, when they go back to the pool. A pooled context that enlisted in a transaction must
    // let go of it without ending it, and must not carry it to the next lease.

    private sealed class PooledEntityContext(DbContextOptions<PooledEntityContext> options) : DbContext(options)
    {
        public DbSet<SimpleEntity> Entities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<SimpleEntity>().ToCollection("pooled_entities");
    }

    [Fact]
    public void Pooled_enlisted_context_returned_to_pool_detaches_without_ending_the_transaction()
    {
        var databaseName = TestDatabaseNamer.GetUniqueDatabaseName();
        var rows = database.Client.GetDatabase(databaseName).GetCollection<BsonDocument>("pooled_entities");
        database.Client.GetDatabase(databaseName).CreateCollection("pooled_entities");

        var options = new DbContextOptionsBuilder<PooledEntityContext>()
            .UseMongoDB(database.Client, databaseName)
            .Options;
        var factory = new PooledDbContextFactory<PooledEntityContext>(options);

        using var session = StartSessionWithTransaction(database.Client);

        var first = factory.CreateDbContext();
        first.Database.UseTransaction(session);
        first.Entities.Add(new SimpleEntity { name = "pooled" });
        first.SaveChanges();
        Assert.NotNull(first.Database.CurrentTransaction);

        // Returns the context to the pool, which resets it.
        first.Dispose();

        Assert.True(session.IsInTransaction);

        var second = factory.CreateDbContext();
        try
        {
            Assert.Same(first, second);
            Assert.Null(second.Database.CurrentTransaction);
        }
        finally
        {
            second.Dispose();
        }

        // Still uncommitted and still usable by its owner.
        Assert.Equal(0, rows.CountDocuments(FilterDefinition<BsonDocument>.Empty));
        session.CommitTransaction();
        Assert.Equal(1, rows.CountDocuments(FilterDefinition<BsonDocument>.Empty));
    }

#if !EF8
    // OrderBy + Take makes the bulk operation two-phase: it reads the target ids, then writes by id. Both phases must run in the
    // transaction the context joined, and the bulk executor must leave it to its owner to end.

    private static async Task<int> SetNamesToUpdated(IQueryable<SimpleEntity> query, bool async)
        => async
            ? await query.ExecuteUpdateAsync(s => s.SetProperty(e => e.name, "updated"))
            : query.ExecuteUpdate(s => s.SetProperty(e => e.name, "updated"));

    private static async Task<int> DeleteAll(IQueryable<SimpleEntity> query, bool async)
        => async ? await query.ExecuteDeleteAsync() : query.ExecuteDelete();

    private static void SeedABC(IMongoCollection<SimpleEntity> collection)
        => collection.InsertMany([new SimpleEntity { name = "a" }, new SimpleEntity { name = "b" }, new SimpleEntity { name = "c" }]);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Enlisted_context_two_phase_ExecuteUpdate_runs_inside_the_shared_transaction(bool async)
    {
        var collection = database.CreateCollection<SimpleEntity>($"{nameof(Enlisted_context_two_phase_ExecuteUpdate_runs_inside_the_shared_transaction)}_{async}");
        SeedABC(collection);

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        using var enlisted = SingleEntityDbContext.Create(collection);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);

        var updated = await SetNamesToUpdated(enlisted.Entities.OrderBy(e => e.name).Take(2), async);

        Assert.Equal(2, updated);
        Assert.Equal(["c", "updated", "updated"], enlisted.Entities.Select(e => e.name).ToList().Order().ToList());

        // The bulk executor joined the transaction and did not end it.
        Assert.NotNull(owner.Database.CurrentTransaction);
        Assert.NotNull(enlisted.Database.CurrentTransaction);
        using (var outsider = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(["a", "b", "c"], outsider.Entities.Select(e => e.name).ToList().Order().ToList());
        }

        owner.Database.CommitTransaction();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["c", "updated", "updated"], check.Entities.Select(e => e.name).ToList().Order().ToList());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Enlisted_context_two_phase_ExecuteUpdate_is_discarded_when_the_owner_rolls_back(bool async)
    {
        var collection = database.CreateCollection<SimpleEntity>($"{nameof(Enlisted_context_two_phase_ExecuteUpdate_is_discarded_when_the_owner_rolls_back)}_{async}");
        SeedABC(collection);

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        using var enlisted = SingleEntityDbContext.Create(collection);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);

        Assert.Equal(2, await SetNamesToUpdated(enlisted.Entities.OrderBy(e => e.name).Take(2), async));
        Assert.NotNull(owner.Database.CurrentTransaction);

        owner.Database.RollbackTransaction();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["a", "b", "c"], check.Entities.Select(e => e.name).ToList().Order().ToList());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Enlisted_context_two_phase_ExecuteDelete_runs_inside_the_shared_transaction(bool async)
    {
        var collection = database.CreateCollection<SimpleEntity>($"{nameof(Enlisted_context_two_phase_ExecuteDelete_runs_inside_the_shared_transaction)}_{async}");
        SeedABC(collection);

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        using var enlisted = SingleEntityDbContext.Create(collection);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);

        Assert.Equal(2, await DeleteAll(enlisted.Entities.OrderBy(e => e.name).Take(2), async));
        Assert.Equal(["c"], enlisted.Entities.Select(e => e.name).ToList());
        Assert.NotNull(owner.Database.CurrentTransaction);

        using (var outsider = SingleEntityDbContext.Create(collection))
        {
            Assert.Equal(3, outsider.Entities.Count());
        }

        owner.Database.RollbackTransaction();

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(3, check.Entities.Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_after_the_owner_committed_throws_no_longer_active(bool async)
    {
        var collection = database.CreateCollection<SimpleEntity>($"{nameof(ExecuteUpdate_after_the_owner_committed_throws_no_longer_active)}_{async}");
        SeedABC(collection);

        using var owner = SingleEntityDbContext.Create(collection);
        owner.Database.BeginTransaction();
        using var enlisted = SingleEntityDbContext.Create(collection);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);

        owner.Database.CommitTransaction();

        var ex = async
            ? await Assert.ThrowsAsync<InvalidOperationException>(
                () => SetNamesToUpdated(enlisted.Entities.OrderBy(e => e.name).Take(2), async))
            : Assert.Throws<InvalidOperationException>(
                () => SetNamesToUpdated(enlisted.Entities.OrderBy(e => e.name).Take(2), async).GetAwaiter().GetResult());
        Assert.Contains("is no longer active", ex.Message);

        // The plain (single command) form is guarded the same way.
        ex = async
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => DeleteAll(enlisted.Entities, async))
            : Assert.Throws<InvalidOperationException>(() => DeleteAll(enlisted.Entities, async).GetAwaiter().GetResult());
        Assert.Contains("is no longer active", ex.Message);

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["a", "b", "c"], check.Entities.Select(e => e.name).ToList().Order().ToList());
    }
#endif

    // A Join the native translator would turn into a $lookup runs through the driver's LINQ provider under MongoQueryMode.DriverLinq,
    // with the inner collection as a second source that must also be bound to the shared transaction's session.

    private class JoinLeft
    {
        public ObjectId _id { get; set; }
        public int key { get; set; }
        public string name { get; set; } = null!;
    }

    private class JoinRight
    {
        public ObjectId _id { get; set; }
        public int key { get; set; }
        public string label { get; set; } = null!;
    }

    private sealed class JoinContext(TemporaryDatabaseFixture fixture, string leftCollection, string rightCollection)
        : DbContext(new DbContextOptionsBuilder<JoinContext>()
            .UseMongoDB(
                fixture.Client,
                fixture.MongoDatabase.DatabaseNamespace.DatabaseName,
                b => b.UseQueryMode(MongoQueryMode.DriverLinq))
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options)
    {
        public DbSet<JoinLeft> Lefts { get; set; } = null!;
        public DbSet<JoinRight> Rights { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JoinLeft>().ToCollection(leftCollection);
            modelBuilder.Entity<JoinRight>().ToCollection(rightCollection);
        }

        public List<string> JoinedLabels()
            => Lefts
                .Join(Rights, l => l.key, r => r.key, (l, r) => new { l.name, r.label })
                .AsEnumerable()
                .Select(x => $"{x.name}:{x.label}")
                .OrderBy(x => x)
                .ToList();
    }

    private (string Left, string Right) SetupJoinCollections()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var left = "JoinLefts" + suffix;
        var right = "JoinRights" + suffix;
        database.MongoDatabase.GetCollection<JoinLeft>(left).InsertOne(new JoinLeft { key = 1, name = "L1" });
        database.MongoDatabase.GetCollection<JoinRight>(right).InsertOne(new JoinRight { key = 1, label = "committed" });
        return (left, right);
    }

    [Fact]
    public void Enlisted_context_join_query_sees_uncommitted_inner_rows()
    {
        var (left, right) = SetupJoinCollections();

        using var owner = new JoinContext(database, left, right);
        owner.Database.BeginTransaction();
        owner.Rights.Add(new JoinRight { key = 1, label = "uncommitted" });
        owner.SaveChanges();

        using var enlisted = new JoinContext(database, left, right);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);
        Assert.Equal(["L1:committed", "L1:uncommitted"], enlisted.JoinedLabels());

        using (var outsider = new JoinContext(database, left, right))
        {
            Assert.Equal(["L1:committed"], outsider.JoinedLabels());
        }

        owner.Database.RollbackTransaction();
    }

    [Fact]
    public void Join_query_after_the_owner_ended_throws()
    {
        var (left, right) = SetupJoinCollections();

        using var owner = new JoinContext(database, left, right);
        owner.Database.BeginTransaction();
        using var enlisted = new JoinContext(database, left, right);
        enlisted.Database.UseTransaction(owner.Database.CurrentTransaction!);
        Assert.Equal(["L1:committed"], enlisted.JoinedLabels());

        owner.Database.CommitTransaction();

        var ex = Assert.Throws<InvalidOperationException>(() => enlisted.JoinedLabels());
        Assert.Contains("is no longer active", ex.Message);
    }

    // The driver's WithTransaction retries its callback on a TransientTransactionError by aborting the transaction and starting a new
    // one on the same session. A context enlisted in the aborted one is bound to it (its writes would otherwise leak outside the new
    // transaction), so each attempt must create and enlist its own context.

    private static MongoException TransientError()
    {
        var ex = new MongoException("transient failure");
        ex.AddErrorLabel("TransientTransactionError");
        return ex;
    }

    [Fact]
    public void Context_enlisted_inside_a_WithTransaction_callback_survives_a_retry()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var session = database.Client.StartSession();
        var attempts = 0;

        session.WithTransaction<object?>((s, _) =>
        {
            attempts++;
            using var db = SingleEntityDbContext.Create(collection);
            db.Database.UseTransaction(s);
            db.Entities.Add(new SimpleEntity { name = $"attempt-{attempts}" });
            db.SaveChanges();

            if (attempts == 1)
            {
                throw TransientError();
            }

            return null;
        });

        Assert.Equal(2, attempts);
        Assert.False(session.IsInTransaction);
        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["attempt-2"], check.Entities.Select(e => e.name).ToList());
    }

    [Fact]
    public async Task Context_enlisted_inside_a_WithTransaction_callback_survives_a_retry_async()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var session = database.Client.StartSession();
        var attempts = 0;

        await session.WithTransactionAsync<object?>(async (s, ct) =>
        {
            attempts++;
            await using var db = SingleEntityDbContext.Create(collection);
            db.Database.UseTransaction(s);
            db.Entities.Add(new SimpleEntity { name = $"attempt-{attempts}" });
            await db.SaveChangesAsync(ct);

            if (attempts == 1)
            {
                throw TransientError();
            }

            return null;
        });

        Assert.Equal(2, attempts);
        await using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(["attempt-2"], await check.Entities.Select(e => e.name).ToListAsync());
    }

    [Fact]
    public void Context_enlisted_before_a_WithTransaction_retry_throws_no_longer_active()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var session = database.Client.StartSession();
        using var db = SingleEntityDbContext.Create(collection);
        var attempts = 0;
        InvalidOperationException? staleContextError = null;

        session.WithTransaction<object?>((s, _) =>
        {
            attempts++;
            if (attempts == 1)
            {
                // Enlisted in the first attempt's transaction, and kept across the retry.
                db.Database.UseTransaction(s);
                db.Entities.Add(new SimpleEntity { name = "first-attempt" });
                db.SaveChanges();
                throw TransientError();
            }

            // The retry is a new transaction on the same session; the context is still bound to the aborted one.
            db.Entities.Add(new SimpleEntity { name = "second-attempt" });
            staleContextError = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
            return null;
        });

        Assert.Equal(2, attempts);
        Assert.NotNull(staleContextError);
        Assert.Contains("is no longer active", staleContextError.Message);
        Assert.DoesNotContain("already in a transaction", staleContextError.Message);

        using var check = SingleEntityDbContext.Create(collection);
        Assert.Equal(0, check.Entities.Count());
    }
}
