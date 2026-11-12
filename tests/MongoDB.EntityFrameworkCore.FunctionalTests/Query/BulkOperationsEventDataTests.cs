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

// The rendered log message is a convenience; MongoBulkDeleteEventData / MongoBulkUpdateEventData are the contract external
// DiagnosticSource subscribers consume (the public TargetCount, MatchedCount, Strategy and Command members). These tests assert the
// payload itself. Each type backs BOTH its Executing and its Executed event, with different members populated: Executing carries the
// target count and the (redaction-gated) command, Executed the totals and no command.

#if !EF8

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

    private static string Describe(BulkEventCollector collector)
        => string.Join(", ", collector.Events.Select(e => e.Name));

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

    [Fact]
    public void Two_phase_update_payloads_carry_strategy_counts_and_a_redacted_command()
    {
        var collection = Seed(nameof(Two_phase_update_payloads_carry_strategy_counts_and_a_redacted_command));
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection);

            var updated = db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));

            Assert.Equal(2, updated);
            var data = collector.Events.Select(e => e.Data).OfType<MongoBulkUpdateEventData>().ToList();
            Assert.True(data.Count == 2, $"expected an Executing and an Executed event; saw: {Describe(collector)}");

            // Executing: the target count and the command (redacted here) are populated.
            var executing = data.Single(d => d.Command != null);
            Assert.Equal("TwoPhase", executing.Strategy);
            Assert.Equal(2, executing.TargetCount);
            Assert.False(executing.LogSensitiveData);
            Assert.Equal("?", executing.Command);

            // Executed: the totals are populated and the command is not carried at all.
            var executed = data.Single(d => d.Command == null);
            Assert.Equal("TwoPhase", executed.Strategy);
            Assert.Equal(2, executed.MatchedCount);
            Assert.Equal(2, executed.ModifyCount);
            Assert.True(executed.Elapsed > TimeSpan.Zero, "elapsed spans both phases");
        }
        finally
        {
            collector.Stop();
        }
    }

    [Fact]
    public void Two_phase_delete_payloads_carry_strategy_counts_and_the_phase_one_pipeline_when_sensitive_logging_is_on()
    {
        var collection = Seed(nameof(Two_phase_delete_payloads_carry_strategy_counts_and_the_phase_one_pipeline_when_sensitive_logging_is_on));
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection, optionsBuilderAction: o => o.EnableSensitiveDataLogging());

            var deleted = db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDelete();

            Assert.Equal(1, deleted);
            var data = collector.Events.Select(e => e.Data).OfType<MongoBulkDeleteEventData>().ToList();
            Assert.True(data.Count == 2, $"expected an Executing and an Executed event; saw: {Describe(collector)}");

            var executing = data.Single(d => d.Command != null);
            Assert.Equal("TwoPhase", executing.Strategy);
            Assert.Equal(1, executing.TargetCount);
            Assert.True(executing.LogSensitiveData);
            var command = BsonDocument.Parse(executing.Command!);
            Assert.Equal(new BsonDocument("$project", new BsonDocument("_id", 1)), command["phase1Pipeline"].AsBsonArray.Last());

            var executed = data.Single(d => d.Command == null);
            Assert.Equal("TwoPhase", executed.Strategy);
            Assert.Equal(1, executed.DeleteCount);
        }
        finally
        {
            collector.Stop();
        }
    }

    [Fact]
    public void Single_command_delete_payloads_have_no_target_count()
    {
        var collection = Seed(nameof(Single_command_delete_payloads_have_no_target_count));
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection);

            var deleted = db.Entities.Where(o => o.Status == "open").ExecuteDelete();

            Assert.Equal(2, deleted);
            var data = collector.Events.Select(e => e.Data).OfType<MongoBulkDeleteEventData>().ToList();
            Assert.True(data.Count == 2, $"expected an Executing and an Executed event; saw: {Describe(collector)}");

            var executing = data.Single(d => d.Command != null);
            Assert.Equal("SingleCommand", executing.Strategy);
            Assert.Null(executing.TargetCount);

            var executed = data.Single(d => d.Command == null);
            Assert.Equal("SingleCommand", executed.Strategy);
            Assert.Equal(2, executed.DeleteCount);
        }
        finally
        {
            collector.Stop();
        }
    }

    // A large two-phase operation is written in batches: one Executing event per batch, each reporting ITS batch's id count as TargetCount,
    // then a single Executed event carrying the totals.
    [Fact]
    public void Multi_batch_two_phase_update_raises_one_Executing_event_per_batch_and_one_Executed_event()
    {
        var collection = Seed(nameof(Multi_batch_two_phase_update_raises_one_Executing_event_per_batch_and_one_Executed_event));
        var collector = new BulkEventCollector(collection.CollectionNamespace);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var db = SingleEntityDbContext.Create(collection);

            int updated;
            using (MongoBulkOperationExecutor.OverrideMaxIdsPerBulkCommand(1))
            {
                updated = db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));
            }

            Assert.Equal(2, updated);
            var data = collector.Events.Select(e => e.Data).OfType<MongoBulkUpdateEventData>().ToList();
            var executing = data.Where(d => d.Command != null).ToList();
            var executed = Assert.Single(data, d => d.Command == null);

            Assert.Equal(2, executing.Count);
            Assert.All(executing, d => Assert.Equal(1, d.TargetCount));
            Assert.Equal(2, executed.MatchedCount);
            Assert.Equal(2, executed.ModifyCount);
            Assert.Null(executed.TargetCount);
        }
        finally
        {
            collector.Stop();
        }
    }

}

#endif
