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

// The rendered log message is a convenience; MongoBulkDeleteEventData / MongoBulkUpdateEventData / MongoBulkOperationErrorEventData are
// the contract external DiagnosticSource subscribers consume (the public Phase, TargetCount, MatchedCount, Strategy and Command
// members). These tests assert the payload itself.
//
// Every database round trip raises an Executing event followed by exactly one of Executed or the matching Failed event. A single-command
// operation has one round trip (Phase "Command"). A two-phase operation has one for the read that collects the target ids (Phase
// "ReadTargets") and one per batch of ids written (Phase "WriteTargets"). Executing carries the (redaction-gated) command; Executed the
// counts and no command.

#if !EF8

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Storage;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

public class BulkOperationsEventDataTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!;
        public int Quantity { get; set; }
    }

    // DiagnosticListener.AllListeners is process-wide, so each test filters the events to its own collection.
    private sealed class BulkEventCollector(CollectionNamespace collectionNamespace, Action<string, EventData>? onEvent = null)
        : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private readonly object _lock = new();
        private readonly List<(string Name, EventData Data)> _events = [];
        private readonly List<IDisposable> _subscriptions = [];
        private IDisposable? _allListeners;

        // Subscribes to every DiagnosticListener (the context's listener is created lazily); disposing stops all subscriptions.
        // onEvent runs on the thread that raised the event, after the event was recorded, so a test can act between two round trips.
        public static BulkEventCollector Start(CollectionNamespace collectionNamespace, Action<string, EventData>? onEvent = null)
        {
            var collector = new BulkEventCollector(collectionNamespace, onEvent);
            collector._allListeners = DiagnosticListener.AllListeners.Subscribe(collector);
            return collector;
        }

        public void Dispose()
        {
            Stop();
            _allListeners?.Dispose();
        }

        public IReadOnlyList<(string Name, EventData Data)> Events
        {
            get
            {
                lock (_lock)
                {
                    return _events.ToList();
                }
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
            var matches = value.Value switch
            {
                MongoBulkDeleteEventData d => d.CollectionNamespace.Equals(collectionNamespace),
                MongoBulkUpdateEventData u => u.CollectionNamespace.Equals(collectionNamespace),
                MongoBulkOperationErrorEventData f => f.CollectionNamespace.Equals(collectionNamespace),
                _ => false
            };

            if (matches)
            {
                lock (_lock)
                {
                    _events.Add((value.Key, (EventData)value.Value!));
                }

                onEvent?.Invoke(value.Key, (EventData)value.Value!);
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

    // The event id name without its category prefix: e.g. "ExecutingBulkUpdate".
    private static string Short(string name) => name[(name.LastIndexOf('.') + 1)..];

    private static string Describe(BulkEventCollector collector)
        => string.Join(", ", collector.Events.Select(e => Short(e.Name)));

    private static void AssertSequence(BulkEventCollector collector, params string[] expected)
        => Assert.True(
            expected.SequenceEqual(collector.Events.Select(e => Short(e.Name))),
            $"expected: {string.Join(", ", expected)}; saw: {Describe(collector)}");

    private IMongoCollection<Order> Seed(string name)
    {
        var collection = database.CreateCollection<Order>(name);
        collection.InsertMany(
        [
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 },
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 2 }
        ]);
        return collection;
    }

    // A unique index on Quantity: setting Quantity to one value on both documents is rejected by the server.
    private IMongoCollection<Order> SeedUniqueQuantity(string name)
    {
        var collection = Seed(name);
        collection.Indexes.CreateOne(
            new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(o => o.Quantity), new CreateIndexOptions { Unique = true }));
        return collection;
    }

    private static async Task<int> Update(IQueryable<Order> source, bool async)
        => async
            ? await source.ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "shipped"))
            : source.ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));

    private static async Task<int> SetQuantity(IQueryable<Order> source, bool async)
        => async
            ? await source.ExecuteUpdateAsync(s => s.SetProperty(o => o.Quantity, 5))
            : source.ExecuteUpdate(s => s.SetProperty(o => o.Quantity, 5));

    private static async Task<int> Delete(IQueryable<Order> source, bool async)
        => async ? await source.ExecuteDeleteAsync() : source.ExecuteDelete();

    private static Task<int> Modify(IQueryable<Order> source, bool update, bool async)
        => update ? Update(source, async) : Delete(source, async);

    // The event names (without category) of a bulk update or delete.
    private static (string Executing, string Executed, string Failed) Names(bool update)
        => update
            ? ("ExecutingBulkUpdate", "ExecutedBulkUpdate", "BulkUpdateFailed")
            : ("ExecutingBulkDelete", "ExecutedBulkDelete", "BulkDeleteFailed");

    private static MongoBulkOperationErrorEventData Failure(BulkEventCollector collector, int index)
        => Assert.IsType<MongoBulkOperationErrorEventData>(collector.Events[index].Data);

    // Two documents, one with Quantity 0: a predicate that divides by Quantity fails on the server ("can't $divide by zero") as soon as it
    // is evaluated, in whichever round trip evaluates it. Deletes cannot be failed with a unique index the way updates can.
    private IMongoCollection<Order> SeedDivideByZero(string name)
    {
        var collection = database.CreateCollection<Order>(name);
        collection.InsertMany(
        [
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 0 },
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 }
        ]);
        return collection;
    }

    private static List<ObjectId> IdsInQuantityOrder(IMongoCollection<Order> collection)
        => collection.Find(FilterDefinition<Order>.Empty).SortBy(o => o.Quantity).Project(o => o._id).ToList();

    // Holds an uncommitted delete of one document in another transaction. Any write to that document by a different transaction then fails
    // at once with a write conflict, which is a way to fail a by-_id write (phase 2) that has no predicate of its own to fail. Disposing the
    // session aborts the transaction.
    private static IClientSessionHandle HoldWriteLock(IMongoCollection<Order> collection, ObjectId id)
    {
        var session = collection.Database.Client.StartSession();
        session.StartTransaction();
        collection.DeleteOne(session, Builders<Order>.Filter.Eq(o => o._id, id));
        return session;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_phase_update_raises_an_Executing_and_Executed_pair_for_each_phase(bool async)
    {
        var collection = Seed(nameof(Two_phase_update_raises_an_Executing_and_Executed_pair_for_each_phase) + async);
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection);

            var updated = await Update(db.Entities.OrderBy(o => o.Quantity).Take(2), async);

            Assert.Equal(2, updated);
            AssertSequence(collector, "ExecutingBulkUpdate", "ExecutedBulkUpdate", "ExecutingBulkUpdate", "ExecutedBulkUpdate");
            var data = collector.Events.Select(e => (MongoBulkUpdateEventData)e.Data).ToList();

            // Phase 1: the read that collects the target ids. It has no command of its own (the pipeline is only known once the query has
            // run; it is reported in the phase-2 command), and its Executed event reports how many ids it found.
            Assert.All(data.Take(2), d => Assert.Equal("TwoPhase", d.Strategy));
            Assert.All(data.Take(2), d => Assert.Equal("ReadTargets", d.Phase));
            Assert.Null(data[0].Command);
            Assert.Null(data[0].TargetCount);
            Assert.Null(data[1].Command);
            Assert.Equal(2, data[1].TargetCount);
            Assert.Equal(0, data[1].MatchedCount);
            Assert.Equal(0, data[1].ModifyCount);

            // Phase 2: the write by _id. Executing carries the (redacted here) command and its batch size; Executed the counts.
            Assert.All(data.Skip(2), d => Assert.Equal("TwoPhase", d.Strategy));
            Assert.All(data.Skip(2), d => Assert.Equal("WriteTargets", d.Phase));
            Assert.Equal("?", data[2].Command);
            Assert.False(data[2].LogSensitiveData);
            Assert.Equal(2, data[2].TargetCount);
            Assert.Null(data[3].Command);
            Assert.Equal(2, data[3].TargetCount);
            Assert.Equal(2, data[3].MatchedCount);
            Assert.Equal(2, data[3].ModifyCount);

            // Each Executed is timed from its own Executing: a per-round-trip duration, not a running total across phases.
            Assert.True(data[1].Elapsed > TimeSpan.Zero);
            Assert.True(data[3].Elapsed > TimeSpan.Zero);
        }
        finally
        {
            collector.Stop();
        }
    }

    [Fact]
    public void Two_phase_delete_carries_the_phase_one_pipeline_in_the_phase_two_command_when_sensitive_logging_is_on()
    {
        var collection = Seed(nameof(Two_phase_delete_carries_the_phase_one_pipeline_in_the_phase_two_command_when_sensitive_logging_is_on));
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection, optionsBuilderAction: o => o.EnableSensitiveDataLogging());

            var deleted = db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDelete();

            Assert.Equal(1, deleted);
            AssertSequence(collector, "ExecutingBulkDelete", "ExecutedBulkDelete", "ExecutingBulkDelete", "ExecutedBulkDelete");
            var data = collector.Events.Select(e => (MongoBulkDeleteEventData)e.Data).ToList();

            Assert.Equal("ReadTargets", data[0].Phase);
            Assert.Equal(1, data[1].TargetCount);

            Assert.Equal("WriteTargets", data[2].Phase);
            Assert.Equal(1, data[2].TargetCount);
            Assert.True(data[2].LogSensitiveData);
            var command = BsonDocument.Parse(data[2].Command!);
            Assert.Equal(new BsonDocument("$project", new BsonDocument("_id", 1)), command["phase1Pipeline"].AsBsonArray.Last());

            Assert.Equal("WriteTargets", data[3].Phase);
            Assert.Equal(1, data[3].DeleteCount);
        }
        finally
        {
            collector.Stop();
        }
    }

    // Phase 2 is never run, so there is no second pair: the operation is exactly one round trip.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_phase_operation_that_finds_no_targets_raises_only_the_phase_one_pair(bool async)
    {
        var collection = Seed(nameof(Two_phase_operation_that_finds_no_targets_raises_only_the_phase_one_pair) + async);
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection);

            var deleted = await Delete(db.Entities.Where(o => o.Quantity > 100).OrderBy(o => o.Quantity).Take(2), async);

            Assert.Equal(0, deleted);
            AssertSequence(collector, "ExecutingBulkDelete", "ExecutedBulkDelete");
            var data = collector.Events.Select(e => (MongoBulkDeleteEventData)e.Data).ToList();
            Assert.All(data, d => Assert.Equal("ReadTargets", d.Phase));
            Assert.Equal(0, data[1].TargetCount);
            Assert.Equal(0, data[1].DeleteCount);
        }
        finally
        {
            collector.Stop();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Single_command_delete_raises_one_pair_with_no_target_count(bool async)
    {
        var collection = Seed(nameof(Single_command_delete_raises_one_pair_with_no_target_count) + async);
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection);

            var deleted = await Delete(db.Entities.Where(o => o.Status == "open"), async);

            Assert.Equal(2, deleted);
            AssertSequence(collector, "ExecutingBulkDelete", "ExecutedBulkDelete");
            var data = collector.Events.Select(e => (MongoBulkDeleteEventData)e.Data).ToList();

            Assert.All(data, d => Assert.Equal("SingleCommand", d.Strategy));
            Assert.All(data, d => Assert.Equal("Command", d.Phase));
            Assert.NotNull(data[0].Command);
            Assert.Null(data[0].TargetCount);
            Assert.Null(data[1].Command);
            Assert.Null(data[1].TargetCount);
            Assert.Equal(2, data[1].DeleteCount);
        }
        finally
        {
            collector.Stop();
        }
    }

    // A large two-phase operation is written in batches. Each batch is a round trip, so each raises its own Executing/Executed pair
    // reporting THAT batch's id count and counts; a subscriber sums them for the total.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Multi_batch_two_phase_update_raises_a_pair_per_batch(bool async)
    {
        var collection = Seed(nameof(Multi_batch_two_phase_update_raises_a_pair_per_batch) + async);
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection);

            int updated;
            using (MongoBulkOperationExecutor.OverrideMaxIdsPerBulkCommand(1))
            {
                updated = await Update(db.Entities.OrderBy(o => o.Quantity).Take(2), async);
            }

            Assert.Equal(2, updated);
            AssertSequence(
                collector,
                "ExecutingBulkUpdate", "ExecutedBulkUpdate", // phase 1
                "ExecutingBulkUpdate", "ExecutedBulkUpdate", // batch 1
                "ExecutingBulkUpdate", "ExecutedBulkUpdate"); // batch 2
            var writes = collector.Events.Select(e => (MongoBulkUpdateEventData)e.Data).Where(d => d.Phase == "WriteTargets").ToList();
            Assert.Equal(4, writes.Count);
            Assert.All(writes, d => Assert.Equal(1, d.TargetCount));
            var executed = writes.Where(d => d.Command == null).ToList();
            Assert.All(executed, d => Assert.Equal(1, d.MatchedCount));
            Assert.All(executed, d => Assert.Equal(1, d.ModifyCount));
            Assert.Equal(updated, executed.Sum(d => d.MatchedCount));
        }
        finally
        {
            collector.Stop();
        }
    }

    // The server rejects the update (the unique index on Quantity makes the second document collide), so the round trip that was open
    // reports a Failed event INSTEAD of Executed, carrying the same exception the caller sees.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_single_command_update_raises_Failed_instead_of_Executed(bool async)
    {
        var collection = SeedUniqueQuantity(nameof(Failed_single_command_update_raises_Failed_instead_of_Executed) + async);
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection);

            var thrown = await Assert.ThrowsAnyAsync<Exception>(() => SetQuantity(db.Entities, async));

            AssertSequence(collector, "ExecutingBulkUpdate", "BulkUpdateFailed");
            var failed = Assert.IsType<MongoBulkOperationErrorEventData>(collector.Events[1].Data);
            Assert.Same(thrown, failed.Exception);
            Assert.Equal("SingleCommand", failed.Strategy);
            Assert.Equal("Command", failed.Phase);
            Assert.Equal(collection.CollectionNamespace, failed.CollectionNamespace);
            Assert.Null(failed.TargetCount);
            Assert.False(failed.LogSensitiveData);
            Assert.True(failed.Elapsed > TimeSpan.Zero);
        }
        finally
        {
            collector.Stop();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_two_phase_update_raises_Failed_for_the_write_and_rolls_back(bool async)
    {
        var collection = SeedUniqueQuantity(nameof(Failed_two_phase_update_raises_Failed_for_the_write_and_rolls_back) + async);
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection);

            var thrown = await Assert.ThrowsAnyAsync<Exception>(() => SetQuantity(db.Entities.OrderBy(o => o.Quantity).Take(2), async));

            // Phase 1 completed; the phase-2 write is the round trip that failed, so there is no Executed for it.
            AssertSequence(collector, "ExecutingBulkUpdate", "ExecutedBulkUpdate", "ExecutingBulkUpdate", "BulkUpdateFailed");
            var failed = Assert.IsType<MongoBulkOperationErrorEventData>(collector.Events[3].Data);
            Assert.Same(thrown, failed.Exception);
            Assert.Equal("TwoPhase", failed.Strategy);
            Assert.Equal("WriteTargets", failed.Phase);
            Assert.Equal(2, failed.TargetCount);
            Assert.False(failed.LogSensitiveData);
            Assert.True(failed.Elapsed > TimeSpan.Zero);

            // The transaction rolled back, so nothing was written.
            Assert.Equal([1, 2], collection.AsQueryable().Select(o => o.Quantity).OrderBy(q => q).ToList());
        }
        finally
        {
            collector.Stop();
        }
    }

    // A cancelled token fails the round trip after its Executing event; the pair must still be closed.
    [Fact]
    public async Task Cancelled_delete_raises_BulkDeleteFailed_after_Executing()
    {
        var collection = Seed(nameof(Cancelled_delete_raises_BulkDeleteFailed_after_Executing));
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection);
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => db.Entities.Where(o => o.Status == "open").ExecuteDeleteAsync(cancellation.Token));

            AssertSequence(collector, "ExecutingBulkDelete", "BulkDeleteFailed");
            var failed = Assert.IsType<MongoBulkOperationErrorEventData>(collector.Events[1].Data);
            Assert.IsAssignableFrom<OperationCanceledException>(failed.Exception);
            Assert.Equal("Command", failed.Phase);
            Assert.Equal(2, collection.CountDocuments(FilterDefinition<Order>.Empty));
        }
        finally
        {
            collector.Stop();
        }
    }

    // A single-command operation has one round trip, so whatever fails it (here the server evaluating the predicate) fails that round trip.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Failed_single_command_operation_raises_Failed_instead_of_Executed(bool update, bool async)
    {
        var collection = SeedDivideByZero(nameof(Failed_single_command_operation_raises_Failed_instead_of_Executed) + update + async);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);
        var (executing, _, failedName) = Names(update);

        var thrown = await Assert.ThrowsAnyAsync<Exception>(() => Modify(db.Entities.Where(o => 10 / o.Quantity > 1), update, async));

        AssertSequence(collector, executing, failedName);
        var failed = Failure(collector, 1);
        Assert.Same(thrown, failed.Exception);
        Assert.Equal("SingleCommand", failed.Strategy);
        Assert.Equal("Command", failed.Phase);
        Assert.Null(failed.TargetCount);
        Assert.Equal(collection.CollectionNamespace, failed.CollectionNamespace);
        Assert.True(failed.Elapsed > TimeSpan.Zero);
        Assert.Equal(2, collection.CountDocuments(FilterDefinition<Order>.Empty));
    }

    // The first round trip of a two-phase operation reads the target ids. When it is the one that fails, no write was started.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Failed_ReadTargets_round_trip_raises_Failed_and_no_write_starts(bool update, bool async)
    {
        var collection = SeedDivideByZero(nameof(Failed_ReadTargets_round_trip_raises_Failed_and_no_write_starts) + update + async);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);
        var (executing, _, failedName) = Names(update);

        var thrown = await Assert.ThrowsAnyAsync<Exception>(
            () => Modify(db.Entities.Where(o => 10 / o.Quantity > 1).OrderBy(o => o.Quantity).Take(2), update, async));

        AssertSequence(collector, executing, failedName);
        var failed = Failure(collector, 1);
        Assert.Same(thrown, failed.Exception);
        Assert.Equal("TwoPhase", failed.Strategy);
        Assert.Equal("ReadTargets", failed.Phase);
        Assert.Null(failed.TargetCount);
        Assert.True(failed.Elapsed > TimeSpan.Zero);
        Assert.Equal(2, collection.CountDocuments(FilterDefinition<Order>.Empty));
        Assert.Equal(0, collection.CountDocuments(o => o.Status == "shipped"));
    }

    // The driver translates the phase-1 query when it is first enumerated, which is inside the ReadTargets round trip: a shape it rejects is
    // reported as a failure of that round trip, carrying the canonical "could not be translated" exception the caller sees.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_phase_source_the_driver_cannot_translate_raises_a_ReadTargets_failure(bool async)
    {
        var collection = Seed(nameof(Two_phase_source_the_driver_cannot_translate_raises_a_ReadTargets_failure) + async);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);
        var source = db.Entities.Where(o => db.Entities.GroupBy(x => x.Status).Count() > 1).OrderBy(o => o.Quantity).Take(1);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => Delete(source, async));

        Assert.Contains("could not be translated", thrown.Message);
        AssertSequence(collector, "ExecutingBulkDelete", "BulkDeleteFailed");
        var failed = Failure(collector, 1);
        Assert.Same(thrown, failed.Exception);
        Assert.Equal("ReadTargets", failed.Phase);
        Assert.Equal(2, collection.CountDocuments(FilterDefinition<Order>.Empty));
    }

    // The cancelled token reaches the read after the explicit transaction (which the caller owns, and which was started without it) is joined.
    [Fact]
    public async Task Cancelled_ReadTargets_in_an_explicit_transaction_raises_a_ReadTargets_failure()
    {
        var collection = Seed(nameof(Cancelled_ReadTargets_in_an_explicit_transaction_raises_a_ReadTargets_failure));
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);
        await using var transaction = await db.Database.BeginTransactionAsync();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDeleteAsync(cancellation.Token));

        AssertSequence(collector, "ExecutingBulkDelete", "BulkDeleteFailed");
        var failed = Failure(collector, 1);
        Assert.IsAssignableFrom<OperationCanceledException>(failed.Exception);
        Assert.Equal("TwoPhase", failed.Strategy);
        Assert.Equal("ReadTargets", failed.Phase);
        Assert.Null(failed.TargetCount);
    }

    // Same, when the operation opens (and owns) its transaction itself: starting that transaction does not observe the token, so the
    // cancellation surfaces in the ReadTargets round trip and the operation's own transaction is rolled back.
    [Fact]
    public async Task Cancelled_ReadTargets_in_the_operations_own_transaction_raises_a_ReadTargets_failure()
    {
        var collection = Seed(nameof(Cancelled_ReadTargets_in_the_operations_own_transaction_raises_a_ReadTargets_failure));
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDeleteAsync(cancellation.Token));

        AssertSequence(collector, "ExecutingBulkDelete", "BulkDeleteFailed");
        var failed = Failure(collector, 1);
        Assert.IsAssignableFrom<OperationCanceledException>(failed.Exception);
        Assert.Equal("ReadTargets", failed.Phase);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(2, collection.CountDocuments(FilterDefinition<Order>.Empty));
    }

    // Batch 1 succeeds (its Executed event is raised) and batch 2 fails. The whole transaction rolls back, including the batch that was
    // reported as executed: a subscriber must not total the counts of an operation that ended in a failure event.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_second_batch_of_a_two_phase_update_raises_Failed_for_that_batch(bool async)
    {
        var collection = SeedUniqueQuantity(nameof(Failed_second_batch_of_a_two_phase_update_raises_Failed_for_that_batch) + async);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);

        Exception thrown;
        using (MongoBulkOperationExecutor.OverrideMaxIdsPerBulkCommand(1))
        {
            thrown = await Assert.ThrowsAnyAsync<Exception>(() => SetQuantity(db.Entities.OrderBy(o => o.Quantity).Take(2), async));
        }

        AssertSequence(
            collector,
            "ExecutingBulkUpdate", "ExecutedBulkUpdate", // ReadTargets
            "ExecutingBulkUpdate", "ExecutedBulkUpdate", // batch 1
            "ExecutingBulkUpdate", "BulkUpdateFailed"); // batch 2
        var batch1 = (MongoBulkUpdateEventData)collector.Events[3].Data;
        Assert.Equal("WriteTargets", batch1.Phase);
        Assert.Equal(1, batch1.TargetCount);
        Assert.Equal(1, batch1.MatchedCount);
        var failed = Failure(collector, 5);
        Assert.Same(thrown, failed.Exception);
        Assert.Equal("WriteTargets", failed.Phase);
        Assert.Equal(1, failed.TargetCount);
        Assert.Equal([1, 2], collection.AsQueryable().Select(o => o.Quantity).OrderBy(q => q).ToList());
    }

    // The by-_id write has no predicate of its own that could fail, so it is failed with a write conflict: another transaction holds an
    // uncommitted delete of one of the targets. The ReadTargets round trip is unaffected (a snapshot read).
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task Failed_two_phase_write_raises_Failed_for_the_conflicting_batch_and_rolls_back(
        bool update, bool async, bool batchOfOne)
    {
        var collection = Seed(nameof(Failed_two_phase_write_raises_Failed_for_the_conflicting_batch_and_rolls_back) + update + async + batchOfOne);
        var ids = IdsInQuantityOrder(collection);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);
        var (executing, executed, failedName) = Names(update);
        using var blocker = HoldWriteLock(collection, ids[1]);

        Exception thrown;
        using (batchOfOne ? MongoBulkOperationExecutor.OverrideMaxIdsPerBulkCommand(1) : null)
        {
            thrown = await Assert.ThrowsAnyAsync<Exception>(() => Modify(db.Entities.OrderBy(o => o.Quantity).Take(2), update, async));
        }

        string[] expected = batchOfOne
            ? [executing, executed, executing, executed, executing, failedName]
            : [executing, executed, executing, failedName];
        AssertSequence(collector, expected);
        if (batchOfOne)
        {
            // Batch 1 (the first id) was written and reported before batch 2 conflicted.
            var batch1 = collector.Events[3].Data;
            Assert.Equal(1, update ? ((MongoBulkUpdateEventData)batch1).MatchedCount : ((MongoBulkDeleteEventData)batch1).DeleteCount);
        }

        var failed = Failure(collector, expected.Length - 1);
        Assert.Same(thrown, failed.Exception);
        Assert.Equal("TwoPhase", failed.Strategy);
        Assert.Equal("WriteTargets", failed.Phase);
        Assert.Equal(batchOfOne ? 1 : 2, failed.TargetCount);

        // Everything the operation did was rolled back with its transaction.
        blocker.AbortTransaction();
        Assert.Equal(2, collection.CountDocuments(FilterDefinition<Order>.Empty));
        Assert.Equal(0, collection.CountDocuments(o => o.Status == "shipped"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Multi_batch_two_phase_delete_raises_a_pair_per_batch_whose_counts_sum_to_the_result(bool async)
    {
        var collection = Seed(nameof(Multi_batch_two_phase_delete_raises_a_pair_per_batch_whose_counts_sum_to_the_result) + async);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);

        int deleted;
        using (MongoBulkOperationExecutor.OverrideMaxIdsPerBulkCommand(1))
        {
            deleted = await Delete(db.Entities.OrderBy(o => o.Quantity).Take(2), async);
        }

        Assert.Equal(2, deleted);
        AssertSequence(
            collector,
            "ExecutingBulkDelete", "ExecutedBulkDelete", // ReadTargets
            "ExecutingBulkDelete", "ExecutedBulkDelete", // batch 1
            "ExecutingBulkDelete", "ExecutedBulkDelete"); // batch 2
        var data = collector.Events.Select(e => (MongoBulkDeleteEventData)e.Data).ToList();
        Assert.Equal(2, data[1].TargetCount);
        var writes = data.Where(d => d.Phase == "WriteTargets").ToList();
        Assert.All(writes, d => Assert.Equal(1, d.TargetCount));
        Assert.Equal(deleted, writes.Where(d => d.Command == null).Sum(d => d.DeleteCount));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_phase_update_that_finds_no_targets_raises_only_the_phase_one_pair(bool async)
    {
        var collection = Seed(nameof(Two_phase_update_that_finds_no_targets_raises_only_the_phase_one_pair) + async);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);

        var updated = await Update(db.Entities.Where(o => o.Quantity > 100).OrderBy(o => o.Quantity).Take(2), async);

        Assert.Equal(0, updated);
        AssertSequence(collector, "ExecutingBulkUpdate", "ExecutedBulkUpdate");
        var data = collector.Events.Select(e => (MongoBulkUpdateEventData)e.Data).ToList();
        Assert.All(data, d => Assert.Equal("ReadTargets", d.Phase));
        Assert.Equal(0, data[1].TargetCount);
        Assert.Equal(0, data[1].MatchedCount);
        Assert.Equal(0, data[1].ModifyCount);
    }

    // Each event's Elapsed is timed from the start of its own round trip. The collector stalls for two seconds between the two phases, so a
    // clock that ran from the start of the operation would put those seconds on every phase-2 event.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Elapsed_is_timed_per_round_trip_not_across_the_operation(bool async)
    {
        var collection = Seed(nameof(Elapsed_is_timed_per_round_trip_not_across_the_operation) + async);
        using var collector = BulkEventCollector.Start(
            collection.CollectionNamespace,
            (name, data) =>
            {
                if (Short(name) == "ExecutedBulkUpdate" && ((MongoBulkUpdateEventData)data).Phase == "ReadTargets")
                {
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            });
        using var db = SingleEntityDbContext.Create(collection);

        await Update(db.Entities.OrderBy(o => o.Quantity).Take(2), async);

        var data = collector.Events.Select(e => (MongoBulkUpdateEventData)e.Data).ToList();
        Assert.Equal(4, data.Count);
        Assert.Equal("WriteTargets", data[2].Phase);
        Assert.True(data[2].Elapsed < TimeSpan.FromSeconds(1), $"Executing: {data[2].Elapsed}");
        Assert.True(data[3].Elapsed < TimeSpan.FromSeconds(1), $"Executed: {data[3].Elapsed}");
    }

    [Fact]
    public void Failed_update_reports_LogSensitiveData_when_sensitive_logging_is_on()
    {
        var collection = SeedUniqueQuantity(nameof(Failed_update_reports_LogSensitiveData_when_sensitive_logging_is_on));
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection, optionsBuilderAction: o => o.EnableSensitiveDataLogging());

        Assert.ThrowsAny<Exception>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Quantity, 5)));

        AssertSequence(collector, "ExecutingBulkUpdate", "BulkUpdateFailed");
        Assert.True(Failure(collector, 1).LogSensitiveData);
        Assert.True(((MongoBulkUpdateEventData)collector.Events[0].Data).LogSensitiveData);
    }

    // An explicit transaction is joined, not owned: the operation uses its session for every round trip and leaves it to the caller.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Single_command_update_inside_an_explicit_transaction_raises_one_pair(bool async)
    {
        var collection = Seed(nameof(Single_command_update_inside_an_explicit_transaction_raises_one_pair) + async);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);
        using var transaction = async ? await db.Database.BeginTransactionAsync() : db.Database.BeginTransaction();

        var updated = await Update(db.Entities, async);

        AssertSequence(collector, "ExecutingBulkUpdate", "ExecutedBulkUpdate");
        var data = collector.Events.Select(e => (MongoBulkUpdateEventData)e.Data).ToList();
        Assert.All(data, d => Assert.Equal("Command", d.Phase));
        Assert.Equal(2, data[1].MatchedCount);
        Assert.Equal(2, updated);

        // Not committed yet, so the write is invisible outside the transaction.
        Assert.Equal(0, collection.CountDocuments(o => o.Status == "shipped"));
        if (async)
        {
            await transaction.CommitAsync();
        }
        else
        {
            transaction.Commit();
        }

        Assert.Equal(2, collection.CountDocuments(o => o.Status == "shipped"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_phase_delete_inside_an_explicit_transaction_raises_the_full_sequence(bool async)
    {
        var collection = Seed(nameof(Two_phase_delete_inside_an_explicit_transaction_raises_the_full_sequence) + async);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);
        using var transaction = async ? await db.Database.BeginTransactionAsync() : db.Database.BeginTransaction();

        var deleted = await Delete(db.Entities.OrderBy(o => o.Quantity).Take(2), async);

        AssertSequence(collector, "ExecutingBulkDelete", "ExecutedBulkDelete", "ExecutingBulkDelete", "ExecutedBulkDelete");
        var data = collector.Events.Select(e => (MongoBulkDeleteEventData)e.Data).ToList();
        Assert.Equal(["ReadTargets", "ReadTargets", "WriteTargets", "WriteTargets"], data.Select(d => d.Phase));
        Assert.Equal(2, data[1].TargetCount);
        Assert.Equal(2, data[3].DeleteCount);
        Assert.Equal(2, deleted);

        // The operation does not commit a transaction it does not own.
        Assert.Equal(2, collection.CountDocuments(FilterDefinition<Order>.Empty));
        if (async)
        {
            await transaction.CommitAsync();
        }
        else
        {
            transaction.Commit();
        }

        Assert.Equal(0, collection.CountDocuments(FilterDefinition<Order>.Empty));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_operation_inside_an_explicit_transaction_raises_Failed_and_leaves_the_transaction_to_the_caller(bool async)
    {
        var collection = SeedUniqueQuantity(
            nameof(Failed_operation_inside_an_explicit_transaction_raises_Failed_and_leaves_the_transaction_to_the_caller) + async);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(collection);
        using var transaction = async ? await db.Database.BeginTransactionAsync() : db.Database.BeginTransaction();

        var thrown = await Assert.ThrowsAnyAsync<Exception>(() => SetQuantity(db.Entities, async));

        AssertSequence(collector, "ExecutingBulkUpdate", "BulkUpdateFailed");
        Assert.Same(thrown, Failure(collector, 1).Exception);
        Assert.Same(transaction, db.Database.CurrentTransaction);
    }

    // The failure's ILogger message and its EventData message (what LogTo and ToString() use) show the exception's message only with
    // sensitive-data logging on, because a duplicate-key error quotes the document's key. Otherwise they show only the exception's type name.
    // The payload carries the exception itself either way. Update fails on a unique index, delete on a predicate the server cannot evaluate.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Failure_text_shows_the_exception_message_only_with_sensitive_logging(bool update, bool sensitive)
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = update
            ? SeedUniqueQuantity(nameof(Failure_text_shows_the_exception_message_only_with_sensitive_logging) + update + sensitive)
            : SeedDivideByZero(nameof(Failure_text_shows_the_exception_message_only_with_sensitive_logging) + update + sensitive);
        using var collector = BulkEventCollector.Start(collection.CollectionNamespace);
        using var db = SingleEntityDbContext.Create(
            collection, loggerFactory, optionsBuilderAction: sensitive ? o => o.EnableSensitiveDataLogging() : null);

        var thrown = update
            ? await Assert.ThrowsAnyAsync<Exception>(() => SetQuantity(db.Entities, async: true))
            : await Assert.ThrowsAnyAsync<Exception>(() => Delete(db.Entities.Where(o => 10 / o.Quantity > 1), async: true));

        var (_, _, failedName) = Names(update);
        AssertSequence(collector, update ? "ExecutingBulkUpdate" : "ExecutingBulkDelete", failedName);
        var failed = Failure(collector, 1);
        Assert.Same(thrown, failed.Exception);
        Assert.Equal(sensitive, failed.LogSensitiveData);

        var logRecord = spyLogger.GetLogRecordByEventId(update ? MongoEventId.BulkUpdateFailed : MongoEventId.BulkDeleteFailed);
        Assert.Equal(LogLevel.Error, logRecord.LogLevel);
        foreach (var text in new[] { logRecord.Message, failed.ToString() })
        {
            if (sensitive)
            {
                Assert.Contains(thrown.Message, text);
            }
            else
            {
                Assert.Contains(thrown.GetType().Name, text);
                Assert.DoesNotContain(thrown.Message, text);
            }
        }
    }
}

#endif
