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
    private sealed class BulkEventCollector(CollectionNamespace collectionNamespace)
        : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        private readonly object _lock = new();
        private readonly List<(string Name, EventData Data)> _events = [];
        private readonly List<IDisposable> _subscriptions = [];

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
            Assert.True(failed.Elapsed >= TimeSpan.Zero);
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
}

#endif
