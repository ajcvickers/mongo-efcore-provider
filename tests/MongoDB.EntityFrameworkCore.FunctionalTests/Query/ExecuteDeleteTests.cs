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

#if !EF8

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Storage;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

public class ExecuteDeleteTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!; // mapped to "state" via HasElementName
        public int Quantity { get; set; }
    }

    private static readonly Action<ModelBuilder> ConfigureModel = mb =>
    {
        mb.Entity<Order>().Property(e => e.Status).HasElementName("state");
    };

    private SingleEntityDbContext<Order> CreateSeededContext([CallerMemberName] string? collectionName = null)
    {
        var collection = database.CreateCollection<Order>(collectionName);

        using (var seedDb = SingleEntityDbContext.Create(collection, ConfigureModel))
        {
            seedDb.AddRange(
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "closed", Quantity = 30 });
            seedDb.SaveChanges();
        }

        return SingleEntityDbContext.Create(collection, ConfigureModel);
    }

    [Fact]
    public void ExecuteDelete_with_predicate_deletes_matching_and_returns_count()
    {
        using var db = CreateSeededContext();

        var deleted = db.Entities.Where(o => o.Status == "open").ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(1, db.Entities.Count());
        Assert.Equal("closed", db.Entities.Single().Status);
    }

    [Fact]
    public async Task ExecuteDeleteAsync_with_predicate_deletes_matching_and_returns_count()
    {
        using var db = CreateSeededContext();

        var deleted = await db.Entities.Where(o => o.Quantity >= 20).ExecuteDeleteAsync();

        Assert.Equal(2, deleted);
        Assert.Equal(1, db.Entities.Count());
        Assert.Equal(10, db.Entities.Single().Quantity);
    }

    [Fact]
    public void ExecuteDelete_without_predicate_deletes_everything()
    {
        using var db = CreateSeededContext();

        var deleted = db.Entities.ExecuteDelete();

        Assert.Equal(3, deleted);
        Assert.Equal(0, db.Entities.Count());
    }

    [Fact]
    public void ExecuteDelete_with_OrderBy_and_no_Take_deletes_everything()
    {
        // OrderBy alone (without Skip/Take) triggers the two-phase path but deletes all documents —
        // semantically equivalent to the single-command path.
        using var db = CreateSeededContext();

        var deleted = db.Entities.OrderBy(o => o.Quantity).ExecuteDelete();

        Assert.Equal(3, deleted);
        Assert.Equal(0, db.Entities.Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_logs_ExecutedBulkDelete_event_with_count(bool async)
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(nameof(ExecuteDelete_logs_ExecutedBulkDelete_event_with_count), async);

        using (var seedDb = SingleEntityDbContext.Create(collection, ConfigureModel))
        {
            seedDb.AddRange(
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "closed", Quantity = 30 });
            seedDb.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, loggerFactory, ConfigureModel);

        var deleted = async
            ? await db.Entities.Where(o => o.Status == "open").ExecuteDeleteAsync()
            : db.Entities.Where(o => o.Status == "open").ExecuteDelete();

        Assert.Equal(2, deleted);

        var executingMessage = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete);
        Assert.Contains("Executing Bulk Delete", executingMessage);
        Assert.Contains($"Collection='{collection.CollectionNamespace}'", executingMessage);

        var executedMessage = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedBulkDelete);
        Assert.Contains("Executed Bulk Delete", executedMessage);
        Assert.Contains($"Collection='{collection.CollectionNamespace}'", executedMessage);
        Assert.Contains("Deleted=2", executedMessage);
    }

    [Fact]
    public void ExecuteDelete_with_two_Where_clauses_deletes_matching_both_and_returns_count()
    {
        using var db = CreateSeededContext();

        var deleted = db.Entities
            .Where(o => o.Status == "open")
            .Where(o => o.Quantity >= 20)
            .ExecuteDelete();

        Assert.Equal(1, deleted);
        Assert.Equal(2, db.Entities.Count());
        Assert.DoesNotContain(db.Entities.ToList(), o => o.Status == "open" && o.Quantity >= 20);
    }

    [Fact]
    public void ExecuteDelete_with_OrderBy_Take_deletes_lowest_quantities()
    {
        using var db = CreateSeededContext();

        var deleted = db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete();

        Assert.Equal(2, deleted);
        var remaining = db.Entities.OrderBy(o => o.Quantity).ToList();
        Assert.Single(remaining);
        Assert.Equal(30, remaining[0].Quantity);
    }

    [Fact]
    public void ExecuteDelete_with_Where_and_Skip_Take_scopes_to_window()
    {
        using var db = CreateSeededContext();

        var deleted = db.Entities.Where(o => o.Status == "open").OrderBy(o => o.Quantity).Skip(1).Take(1).ExecuteDelete();

        Assert.Equal(1, deleted);
        Assert.Equal(2, db.Entities.Count());
        Assert.DoesNotContain(db.Entities.ToList(), o => o.Quantity == 20);
    }

    [Fact]
    public void ExecuteDelete_two_phase_empty_target_returns_zero()
    {
        using var db = CreateSeededContext();

        var deleted = db.Entities.Where(o => o.Status == "nonexistent").OrderBy(o => o.Quantity).Take(5).ExecuteDelete();

        Assert.Equal(0, deleted);
        Assert.Equal(3, db.Entities.Count());
    }

    [Fact]
    public void ExecuteDelete_two_phase_rolls_back_inside_user_transaction()
    {
        using var db = CreateSeededContext();
        using (var tx = db.Database.BeginTransaction())
        {
            var deleted = db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete();
            Assert.Equal(2, deleted);
            tx.Rollback();
        }

        Assert.Equal(3, db.Entities.Count());
    }

    [Fact]
    public void ExecuteDelete_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing()
    {
        using var db = CreateSeededContext();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete());

        Assert.Contains("AutoTransactionBehavior", ex.Message);
        Assert.Equal(3, db.Entities.Count()); // nothing deleted
    }

    [Fact]
    public void ExecuteDelete_single_command_with_AutoTransactionBehavior_Never_still_works()
    {
        using var db = CreateSeededContext();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        // Where-only path is single-command and needs no transaction — Never must not affect it.
        var deleted = db.Entities.Where(o => o.Status == "open").ExecuteDelete();

        Assert.Equal(2, deleted);
    }

    [Fact]
    public void ExecuteDelete_with_Distinct_deletes_matching()
    {
        // Distinct on a root entity triggers the two-phase execution path: phase 1 reads the
        // distinct matching _ids, phase 2 deletes by { _id: { $in: [...] } }.
        using var db = CreateSeededContext();

        var deleted = db.Entities.Where(o => o.Status == "open").Distinct().ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(1, db.Entities.Count());
        Assert.Equal("closed", db.Entities.Single().Status);
    }

    [Fact(Skip = "Requires a standalone (non-replica-set) MongoDB to exercise the end-to-end path; CI/local use a replica set "
        + "(atlas-local). The error classification itself is unit-tested server-free in "
        + "UnitTests/Storage/BulkTransactionErrorClassificationTests.cs. EF-107")]
    public void ExecuteDelete_two_phase_on_standalone_throws_actionable_error()
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_two_phase_logs_ExecutingBulkDelete_with_target_count(bool async)
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(
            nameof(ExecuteDelete_two_phase_logs_ExecutingBulkDelete_with_target_count), async);

        using (var seedDb = SingleEntityDbContext.Create(collection, ConfigureModel))
        {
            seedDb.AddRange(
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "closed", Quantity = 30 });
            seedDb.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, loggerFactory, ConfigureModel);

        // Two-phase path: OrderBy + Take triggers phase-1 id selection then phase-2 DeleteByIds.
        var deleted = async
            ? await db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDeleteAsync()
            : db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete();

        Assert.Equal(2, deleted);

        var executingMessage = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete);
        Assert.Contains("Executing Bulk Delete", executingMessage);
        Assert.Contains($"Collection='{collection.CollectionNamespace}'", executingMessage);
        Assert.Contains("Strategy=TwoPhase", executingMessage);
        Assert.Contains("Targets=2", executingMessage);
    }

    [Fact]
    public void ExecuteDelete_two_phase_commits_inside_user_transaction()
    {
        using var db = CreateSeededContext();
        using (var tx = db.Database.BeginTransaction())
        {
            // Two-phase runs on the ambient session; the provider must NOT commit the user's transaction.
            var deleted = db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete();
            Assert.Equal(2, deleted);
            tx.Commit();
        }

        Assert.Equal(1, db.Entities.Count());
        Assert.Equal(30, db.Entities.Single().Quantity);
    }

    private class LineItem
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string Status { get; set; } = null!;
        public int Quantity { get; set; }
    }

    private static readonly Action<ModelBuilder> ConfigureCompositeKeyModel = mb =>
    {
        // Composite primary key maps to a composite _id sub-document in MongoDB.
        mb.Entity<LineItem>().HasKey(e => new { e.OrderId, e.ProductId });
    };

    private SingleEntityDbContext<LineItem> CreateSeededCompositeKeyContext([CallerMemberName] string? collectionName = null)
    {
        var collection = database.CreateCollection<LineItem>(collectionName);

        using (var seedDb = SingleEntityDbContext.Create(collection, ConfigureCompositeKeyModel))
        {
            seedDb.AddRange(
                new LineItem { OrderId = 1, ProductId = 1, Status = "open", Quantity = 10 },
                new LineItem { OrderId = 1, ProductId = 2, Status = "open", Quantity = 20 },
                new LineItem { OrderId = 2, ProductId = 1, Status = "closed", Quantity = 30 });
            seedDb.SaveChanges();
        }

        return SingleEntityDbContext.Create(collection, ConfigureCompositeKeyModel);
    }

    [Fact]
    public void ExecuteDelete_two_phase_with_composite_key_deletes_targeted_rows()
    {
        using var db = CreateSeededCompositeKeyContext();

        // Two-phase on a composite-key entity: phase 1 must collect composite _id sub-documents,
        // phase 2 deletes by { _id: { $in: [ { OrderId, ProductId }, ... ] } }.
        var deleted = db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete();

        Assert.Equal(2, deleted);
        var remaining = db.Entities.Single();
        Assert.Equal(30, remaining.Quantity);
    }

    // Not a MongoTransaction, so it carries no session: joined as-is, both phases would run outside any transaction.
    private sealed class NotAMongoTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public void Commit() { }
        public void Rollback() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected(bool async)
    {
        // One collection per row: the helper names it from the method, and both rows share a method.
        using var db = CreateSeededContext($"{nameof(ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected)}_{async}");
        ((MongoTransactionManager)db.Database.GetService<IDbContextTransactionManager>()).CurrentTransaction = new NotAMongoTransaction();

        var ex = async
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDeleteAsync())
            : Assert.Throws<InvalidOperationException>(() => db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDelete());

        Assert.Contains("MongoDB transaction", ex.Message);

        // Nothing was deleted: the guard fires before either phase runs.
        ((MongoTransactionManager)db.Database.GetService<IDbContextTransactionManager>()).CurrentTransaction = null;
        Assert.Equal(3, db.Entities.Count());
    }

    // Phase 1 is translated by the driver when its query is first enumerated, so a source that only fails there (a GroupBy subquery
    // inside the Where) used to surface as a raw driver ExpressionNotSupportedException instead of EF's canonical translation failure.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_two_phase_with_a_source_the_driver_cannot_translate_reports_a_translation_failure(bool async)
    {
        using var db = CreateSeededContext(
            $"{nameof(ExecuteDelete_two_phase_with_a_source_the_driver_cannot_translate_reports_a_translation_failure)}_{async}");
        var source = db.Entities.Where(o => db.Entities.GroupBy(x => x.Status).Count() > 1).OrderBy(o => o.Quantity).Take(1);

        var ex = async
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => source.ExecuteDeleteAsync())
            : Assert.Throws<InvalidOperationException>(() => source.ExecuteDelete());

        Assert.Contains("could not be translated", ex.Message);
        Assert.Equal(3, db.Entities.Count());
    }

    // The mapping is narrow on purpose: phase 1 is an I/O site, so cancellation must propagate as itself, not be relabelled as a
    // translation failure.
    [Fact]
    public async Task ExecuteDelete_two_phase_cancellation_is_not_reported_as_a_translation_failure()
    {
        using var db = CreateSeededContext();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var ex = await Record.ExceptionAsync(
            () => db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDeleteAsync(cancelled.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
        Assert.Equal(3, db.Entities.Count());
    }

    // Cancelling AFTER phase 1 has succeeded: a token cancelled before the first command means no transaction is ever started
    // server-side, so there would be nothing to abort. The cancel fires from the aggregate's CommandSucceededEvent, which the driver raises
    // synchronously on the receive path before phase 1's result unwinds, so phase 2 then observes a cancelled token and the operation fails
    // with the transaction open. Cleanup must still send abortTransaction, and the server must acknowledge it; with the caller's cancelled
    // token passed to the rollback the driver drops the command before it reaches the wire and the transaction lingers.
    [Fact]
    public async Task ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_the_transaction_on_the_wire()
    {
        var collection = database.CreateCollection<Order>();
        using (var seed = SingleEntityDbContext.Create(collection, ConfigureModel))
        {
            seed.AddRange(
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "closed", Quantity = 30 });
            seed.SaveChanges();
        }

        using var cts = new CancellationTokenSource();
        var cancelled = 0;
        using var capture = new CommandCapture(
            database,
            e =>
            {
                if (e.CommandName == "aggregate" && Interlocked.Exchange(ref cancelled, 1) == 0)
                {
                    cts.Cancel();
                }
            });

        using (var db = SingleEntityDbContext.Create(capture.Collection(collection), ConfigureModel))
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDeleteAsync(cts.Token));
        }

        Assert.True(capture.Named("abortTransaction").Count == 1, $"Commands sent: {string.Join(", ", capture.Names())}");
        Assert.Equal(1, capture.SucceededCount("abortTransaction"));

        // And nothing was deleted.
        using var check = SingleEntityDbContext.Create(collection, ConfigureModel);
        Assert.Equal(3, check.Entities.Count());
    }

    // Phase 2 filters on _id alone and never re-evaluates the predicate, so isolation is what keeps it from acting on a document that no
    // longer matches. That only holds if phase 1 reads INSIDE the transaction: the phase-1 query binds the transaction's session when it
    // is built, so building it before the transaction starts (it was, briefly, hoisted there) silently reads outside it. Counts and
    // results cannot tell the difference, so this checks the command itself.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_two_phase_reads_its_target_ids_inside_the_transaction(bool async)
    {
        var collection = database.CreateCollection<Order>($"{nameof(ExecuteDelete_two_phase_reads_its_target_ids_inside_the_transaction)}_{async}");
        using (var seed = SingleEntityDbContext.Create(collection, ConfigureModel))
        {
            seed.AddRange(
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 });
            seed.SaveChanges();
        }

        using var capture = new CommandCapture(database);
        using var db = SingleEntityDbContext.Create(capture.Collection(collection), ConfigureModel);

        var deleted = async
            ? await db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDeleteAsync()
            : db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDelete();

        Assert.Equal(1, deleted);
        var phase1 = Assert.Single(capture.Named("aggregate"));
        Assert.True(phase1.Contains("txnNumber"), $"The phase-1 read ran outside the transaction. Its fields: {string.Join(", ", phase1.Names)}");
        Assert.True(Assert.Single(capture.Named("delete")).Contains("txnNumber"), "The phase-2 delete ran outside the transaction.");
    }
}

#endif
