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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Update;

/// <summary>
/// A concurrency conflict is reported the way EF Core's relational providers report it (#104): it logs
/// <see cref="CoreEventId.OptimisticConcurrencyException"/> at Debug (and no <see cref="CoreEventId.SaveChangesFailed"/>
/// for the same failure), and gives <see cref="ISaveChangesInterceptor.ThrowingConcurrencyException"/> a chance to
/// observe or suppress it, in both the synchronous and the asynchronous save. A suppressed conflict does not throw, its
/// operations are counted as applied, and the rest of the save carries on.
/// </summary>
public class ConcurrencyLoggingTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    // Distinctive values: none of them may appear in a logged message or exception text (the message is not behind the
    // sensitive-data gate).
    private const string Initial = "SECRET-INITIAL";
    private const string Elsewhere = "SECRET-ELSEWHERE";
    private const string Here = "SECRET-HERE";

    private class ConcurrentEntity
    {
        public ObjectId _id { get; set; }

        [ConcurrencyCheck]
        public string Text { get; set; } = "";
    }

    private class OtherEntity
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; } = "";
    }

    private class OwnedChild
    {
        public string Value { get; set; } = "";
    }

    private class OwnedRoot
    {
        public ObjectId _id { get; set; }

        [ConcurrencyCheck]
        public string Text { get; set; } = "";

        public OwnedChild Child { get; set; } = new();
    }

    private sealed class RecordingInterceptor(bool suppress = false) : SaveChangesInterceptor
    {
        public List<ConcurrencyExceptionEventData> SyncCalls { get; } = [];
        public List<ConcurrencyExceptionEventData> AsyncCalls { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];

        public override InterceptionResult ThrowingConcurrencyException(
            ConcurrencyExceptionEventData eventData, InterceptionResult result)
        {
            SyncCalls.Add(eventData);
            return suppress ? InterceptionResult.Suppress() : result;
        }

        public override ValueTask<InterceptionResult> ThrowingConcurrencyExceptionAsync(
            ConcurrencyExceptionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            AsyncCalls.Add(eventData);
            Tokens.Add(cancellationToken);
            return new ValueTask<InterceptionResult>(suppress ? InterceptionResult.Suppress() : result);
        }
    }

    // One context over three collections (<name>_a, _b, _o), with its own logger and interceptor.
    private sealed class TestContext(DbContextOptions options, string name) : DbContext(options)
    {
        public DbSet<ConcurrentEntity> Entities => Set<ConcurrentEntity>();
        public DbSet<OtherEntity> Others => Set<OtherEntity>();
        public DbSet<OwnedRoot> Roots => Set<OwnedRoot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConcurrentEntity>().ToCollection(name + "_a");
            modelBuilder.Entity<OtherEntity>().ToCollection(name + "_b");
            modelBuilder.Entity<OwnedRoot>().ToCollection(name + "_o").OwnsOne(e => e.Child);
        }
    }

    private TestContext CreateContext(
        string name, SpyLoggerProvider spy, LoggerFactory loggerFactory, RecordingInterceptor interceptor,
        bool sensitiveDataLogging = false, AutoTransactionBehavior? transactionBehavior = null)
    {
        foreach (var suffix in new[] { "_a", "_b", "_o" })
        {
            if (!database.MongoDatabase.ListCollectionNames().ToList().Contains(name + suffix))
            {
                database.MongoDatabase.CreateCollection(name + suffix);
            }
        }

        var builder = new DbContextOptionsBuilder<TestContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .UseLoggerFactory(loggerFactory)
            .AddInterceptors(interceptor)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        if (sensitiveDataLogging) builder.EnableSensitiveDataLogging();

        var db = new TestContext(builder.Options, name);
        if (transactionBehavior != null) db.Database.AutoTransactionBehavior = transactionBehavior.Value;
        return db;
    }

    // A second, plain context, used to change or read the stored data behind the first context's back.
    private TestContext CreateOtherContext(string name)
        => CreateContext(name, new SpyLoggerProvider(), new LoggerFactory(), new RecordingInterceptor());

    private static List<SpyLogRecord> AllRecords(SpyLoggerProvider spy)
        => spy.Loggers.Values.SelectMany(l => l.Records).ToList();

    // Only what the failing save logs: EF keeps the logger instances, so clear them rather than the dictionary.
    private static void ClearLog(SpyLoggerProvider spy)
    {
        foreach (var logger in spy.Loggers.Values) logger.Records.Clear();
    }

    private static async Task SaveAsync(DbContext db, bool async)
    {
        if (async) await db.SaveChangesAsync();
        else db.SaveChanges();
    }

    private static async Task<int> SaveCountAsync(DbContext db, bool async)
        => async ? await db.SaveChangesAsync() : db.SaveChanges();

    // Saves one entity, changes it through a second context, then returns the first context with a stale copy of it
    // modified (or deleted) and not yet saved.
    private async Task<(TestContext Db, ConcurrentEntity Entity)> CreateConflictAsync(
        string name, SpyLoggerProvider spy, LoggerFactory loggerFactory, RecordingInterceptor interceptor, bool delete,
        bool sensitiveDataLogging = false)
    {
        await using (var setup = CreateOtherContext(name))
        {
            setup.Entities.Add(new ConcurrentEntity { Text = Initial });
            await setup.SaveChangesAsync();
        }

        var db = CreateContext(name, spy, loggerFactory, interceptor, sensitiveDataLogging);
        var entity = await db.Entities.FirstAsync();

        await using (var other = CreateOtherContext(name))
        {
            (await other.Entities.FirstAsync()).Text = Elsewhere;
            await other.SaveChangesAsync();
        }

        if (delete) db.Entities.Remove(entity);
        else entity.Text = Here;

        ClearLog(spy);
        return (db, entity);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Conflict_logs_OptimisticConcurrencyException_once_at_Debug_and_not_SaveChangesFailed(bool async, bool delete)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var (db, _) = await CreateConflictAsync(
            $"{nameof(Conflict_logs_OptimisticConcurrencyException_once_at_Debug_and_not_SaveChangesFailed)}_{async}_{delete}",
            spy, loggerFactory, new RecordingInterceptor(), delete);
        await using var __ = db;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => SaveAsync(db, async));

        var records = AllRecords(spy);
        var logged = Assert.Single(records, r => r.EventId == CoreEventId.OptimisticConcurrencyException);
        Assert.Equal(LogLevel.Debug, logged.LogLevel);
        Assert.DoesNotContain(records, r => r.EventId == CoreEventId.SaveChangesFailed);

        // The category is Update, as for the relational providers.
        Assert.Contains(
            spy.Loggers,
            kv => kv.Key == DbLoggerCategory.Update.Name
                  && kv.Value.Records.Any(r => r.EventId == CoreEventId.OptimisticConcurrencyException));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Conflict_calls_ThrowingConcurrencyException_interceptor_with_the_conflicting_entries_and_still_throws(
        bool async, bool delete)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var interceptor = new RecordingInterceptor();
        var (db, entity) = await CreateConflictAsync(
            $"{nameof(Conflict_calls_ThrowingConcurrencyException_interceptor_with_the_conflicting_entries_and_still_throws)}_{async}_{delete}",
            spy, loggerFactory, interceptor, delete);
        await using var __ = db;

        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => SaveAsync(db, async));

        // Only the variant matching the save runs, and only once.
        var calls = async ? interceptor.AsyncCalls : interceptor.SyncCalls;
        Assert.Empty(async ? interceptor.SyncCalls : interceptor.AsyncCalls);
        var data = Assert.Single(calls);

        Assert.Same(db, data.Context);
        Assert.Same(ex, data.Exception);
        Assert.Same(entity, Assert.Single(data.Entries).Entity);
        Assert.Same(entity, Assert.Single(ex.Entries).Entity);
    }

    [Fact]
    public async Task Async_conflict_passes_the_CancellationToken_to_the_interceptor()
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var interceptor = new RecordingInterceptor();
        var (db, _) = await CreateConflictAsync(
            nameof(Async_conflict_passes_the_CancellationToken_to_the_interceptor), spy, loggerFactory, interceptor, delete: false);
        await using var __ = db;

        using var cts = new CancellationTokenSource();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync(cts.Token));

        Assert.Equal(cts.Token, Assert.Single(interceptor.Tokens));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Suppressed_conflict_does_not_throw_counts_the_operation_and_leaves_the_stored_document_alone(
        bool async, bool delete)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var interceptor = new RecordingInterceptor(suppress: true);
        var name = $"{nameof(Suppressed_conflict_does_not_throw_counts_the_operation_and_leaves_the_stored_document_alone)}_{async}_{delete}";
        var (db, entity) = await CreateConflictAsync(name, spy, loggerFactory, interceptor, delete);
        await using var __ = db;

        // As for the relational providers, the suppressed operation is counted as applied.
        Assert.Equal(1, await SaveCountAsync(db, async));

        Assert.Single(AllRecords(spy), r => r.EventId == CoreEventId.OptimisticConcurrencyException);
        Assert.Single(async ? interceptor.AsyncCalls : interceptor.SyncCalls);

        // EF accepts the changes, so the stale entity is no longer pending, but nothing was written.
        Assert.Equal(delete ? EntityState.Detached : EntityState.Unchanged, db.Entry(entity).State);
        await using var check = CreateOtherContext(name);
        Assert.Equal(Elsewhere, Assert.Single(await check.Entities.ToListAsync()).Text);

        // Nothing is left to save.
        Assert.Equal(0, await SaveCountAsync(db, async));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Successful_save_does_not_log_OptimisticConcurrencyException_or_call_the_interceptor(bool async)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var interceptor = new RecordingInterceptor();
        await using var db = CreateContext(
            $"{nameof(Successful_save_does_not_log_OptimisticConcurrencyException_or_call_the_interceptor)}_{async}",
            spy, loggerFactory, interceptor);

        var entity = new ConcurrentEntity { Text = Initial };
        db.Entities.Add(entity);
        await SaveAsync(db, async);

        entity.Text = Here;
        Assert.Equal(1, await SaveCountAsync(db, async));

        Assert.DoesNotContain(AllRecords(spy), r => r.EventId == CoreEventId.OptimisticConcurrencyException);
        Assert.Empty(interceptor.SyncCalls);
        Assert.Empty(interceptor.AsyncCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Logged_event_and_exception_text_never_contain_entity_values_or_keys(bool sensitiveDataLogging)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var name = $"{nameof(Logged_event_and_exception_text_never_contain_entity_values_or_keys)}_{sensitiveDataLogging}";
        var (db, entity) = await CreateConflictAsync(
            name, spy, loggerFactory, new RecordingInterceptor(), delete: false, sensitiveDataLogging);
        await using var __ = db;

        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync());

        var logged = Assert.Single(AllRecords(spy), r => r.EventId == CoreEventId.OptimisticConcurrencyException);
        var id = entity._id.ToString();
        foreach (var text in new[] { logged.Message, logged.Exception?.ToString() ?? "", ex.Message, ex.ToString() })
        {
            Assert.DoesNotContain("SECRET", text);
            Assert.DoesNotContain(id, text);
        }

        Assert.Contains(name + "_a", ex.Message);
    }

    // One bulk write holding a conflicting update, a clean update and an insert.
    private async Task<TestContext> CreateMixedBatchAsync(
        string name, SpyLoggerProvider spy, LoggerFactory loggerFactory, RecordingInterceptor interceptor,
        AutoTransactionBehavior transactionBehavior)
    {
        await using (var setup = CreateOtherContext(name))
        {
            setup.Entities.AddRange(new ConcurrentEntity { Text = "Conflicting" }, new ConcurrentEntity { Text = "Clean" });
            await setup.SaveChangesAsync();
        }

        var db = CreateContext(name, spy, loggerFactory, interceptor, transactionBehavior: transactionBehavior);
        var conflicting = await db.Entities.SingleAsync(e => e.Text == "Conflicting");
        var clean = await db.Entities.SingleAsync(e => e.Text == "Clean");

        await using (var other = CreateOtherContext(name))
        {
            (await other.Entities.SingleAsync(e => e.Text == "Conflicting")).Text = Elsewhere;
            await other.SaveChangesAsync();
        }

        conflicting.Text = Here;
        clean.Text = "Clean, changed";
        db.Entities.Add(new ConcurrentEntity { Text = "Inserted" });
        ClearLog(spy);
        return db;
    }

    private async Task<List<string>> StoredTextsAsync(string name)
    {
        await using var check = CreateOtherContext(name);
        return (await check.Entities.ToListAsync()).Select(e => e.Text).Order().ToList();
    }

    [Theory]
    [InlineData(false, AutoTransactionBehavior.Always)]
    [InlineData(true, AutoTransactionBehavior.Always)]
    [InlineData(false, AutoTransactionBehavior.WhenNeeded)]
    [InlineData(true, AutoTransactionBehavior.WhenNeeded)]
    public async Task Conflict_in_an_implicit_transaction_rolls_back_the_rest_of_the_batch(
        bool async, AutoTransactionBehavior transactionBehavior)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var name = $"{nameof(Conflict_in_an_implicit_transaction_rolls_back_the_rest_of_the_batch)}_{async}_{transactionBehavior}";
        await using var db = await CreateMixedBatchAsync(name, spy, loggerFactory, new RecordingInterceptor(), transactionBehavior);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => SaveAsync(db, async));

        Assert.Equal(["Clean", Elsewhere], await StoredTextsAsync(name));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Conflict_without_a_transaction_keeps_the_writes_that_did_apply(bool async)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var name = $"{nameof(Conflict_without_a_transaction_keeps_the_writes_that_did_apply)}_{async}";
        await using var db = await CreateMixedBatchAsync(
            name, spy, loggerFactory, new RecordingInterceptor(), AutoTransactionBehavior.Never);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => SaveAsync(db, async));

        // The bulk write applied everything but the conflicting update before the conflict was detected.
        Assert.Equal(["Clean, changed", "Inserted", Elsewhere], await StoredTextsAsync(name));
    }

    [Theory]
    [InlineData(false, AutoTransactionBehavior.Always)]
    [InlineData(true, AutoTransactionBehavior.Always)]
    [InlineData(false, AutoTransactionBehavior.WhenNeeded)]
    [InlineData(true, AutoTransactionBehavior.WhenNeeded)]
    [InlineData(false, AutoTransactionBehavior.Never)]
    [InlineData(true, AutoTransactionBehavior.Never)]
    public async Task Suppressed_conflict_counts_it_and_commits_the_rest_of_the_batch(
        bool async, AutoTransactionBehavior transactionBehavior)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var name = $"{nameof(Suppressed_conflict_counts_it_and_commits_the_rest_of_the_batch)}_{async}_{transactionBehavior}";
        await using var db = await CreateMixedBatchAsync(
            name, spy, loggerFactory, new RecordingInterceptor(suppress: true), transactionBehavior);

        // The clean update, the insert and the suppressed conflict.
        Assert.Equal(3, await SaveCountAsync(db, async));

        Assert.Equal(["Clean, changed", "Inserted", Elsewhere], await StoredTextsAsync(name));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task Conflict_in_an_explicit_transaction_follows_the_transaction(bool async, bool suppress)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var name = $"{nameof(Conflict_in_an_explicit_transaction_follows_the_transaction)}_{async}_{suppress}";
        await using var db = await CreateMixedBatchAsync(
            name, spy, loggerFactory, new RecordingInterceptor(suppress), AutoTransactionBehavior.WhenNeeded);

        await using var transaction = async ? await db.Database.BeginTransactionAsync() : db.Database.BeginTransaction();
        if (suppress)
        {
            Assert.Equal(3, await SaveCountAsync(db, async));
            if (async) await transaction.CommitAsync();
            else transaction.Commit();
            Assert.Equal(["Clean, changed", "Inserted", Elsewhere], await StoredTextsAsync(name));
        }
        else
        {
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => SaveAsync(db, async));
            if (async) await transaction.RollbackAsync();
            else transaction.Rollback();
            Assert.Equal(["Clean", Elsewhere], await StoredTextsAsync(name));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Suppressed_conflict_does_not_stop_later_collections_being_written(bool async)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var interceptor = new RecordingInterceptor(suppress: true);
        var name = $"{nameof(Suppressed_conflict_does_not_stop_later_collections_being_written)}_{async}";

        await using (var setup = CreateOtherContext(name))
        {
            setup.Entities.Add(new ConcurrentEntity { Text = Initial });
            await setup.SaveChangesAsync();
        }

        await using var db = CreateContext(name, spy, loggerFactory, interceptor);
        var entity = await db.Entities.SingleAsync();

        await using (var other = CreateOtherContext(name))
        {
            (await other.Entities.SingleAsync()).Text = Elsewhere;
            await other.SaveChangesAsync();
        }

        entity.Text = Here;
        db.Others.Add(new OtherEntity { Name = "Other" });

        // The conflicting update in one collection and the insert into another.
        Assert.Equal(2, await SaveCountAsync(db, async));

        Assert.Single(async ? interceptor.AsyncCalls : interceptor.SyncCalls);
        await using var check = CreateOtherContext(name);
        Assert.Equal("Other", Assert.Single(await check.Others.ToListAsync()).Name);
        Assert.Equal(Elsewhere, Assert.Single(await check.Entities.ToListAsync()).Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Conflict_on_the_root_of_a_modified_owned_entity_is_logged_and_reports_the_root(bool async)
    {
        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        var name = $"{nameof(Conflict_on_the_root_of_a_modified_owned_entity_is_logged_and_reports_the_root)}_{async}";

        await using (var setup = CreateOtherContext(name))
        {
            setup.Roots.Add(new OwnedRoot { Text = Initial, Child = new OwnedChild { Value = "Child" } });
            await setup.SaveChangesAsync();
        }

        await using var db = CreateContext(name, spy, loggerFactory, new RecordingInterceptor());
        var root = await db.Roots.SingleAsync();

        await using (var other = CreateOtherContext(name))
        {
            (await other.Roots.SingleAsync()).Text = Elsewhere;
            await other.SaveChangesAsync();
        }

        // Only the owned child changes: the unchanged root is promoted to Modified, and its token is still checked.
        root.Child.Value = "Changed child";
        ClearLog(spy);

        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => SaveAsync(db, async));

        Assert.Same(root, Assert.Single(ex.Entries).Entity);
        Assert.Single(AllRecords(spy), r => r.EventId == CoreEventId.OptimisticConcurrencyException);
    }
}
