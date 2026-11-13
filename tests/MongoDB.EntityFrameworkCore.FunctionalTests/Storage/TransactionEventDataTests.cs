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

// The rendered log message is a convenience; MongoTransactionEventData is what external DiagnosticSource subscribers consume. These
// tests assert the payload of the events a context raises when it joins and leaves a transaction it does not own.

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Storage;

public class TransactionEventDataTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class SimpleEntity
    {
        public ObjectId _id { get; set; }
        public string name { get; set; } = null!;
    }

    // DiagnosticListener.AllListeners is process-wide, so each test filters the events to the contexts it created.
    private sealed class TransactionEventCollector
        : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        private readonly object _lock = new();
        private readonly List<(string Name, MongoTransactionEventData Data)> _events = [];
        private readonly List<IDisposable> _subscriptions = [];

        public IReadOnlyList<(string Name, MongoTransactionEventData Data)> EventsFor(DbContext context)
        {
            lock (_lock)
            {
                return _events.Where(e => ReferenceEquals(e.Data.Context, context)).ToList();
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
            if (value.Value is MongoTransactionEventData data)
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Enlisting_and_detaching_raise_payloads_that_identify_the_transaction_and_the_context(bool async)
    {
        var collection = database.CreateCollection<SimpleEntity>($"Enlist_{async}");
        var collector = new TransactionEventCollector();
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            using var owner = SingleEntityDbContext.Create(collection);
            var ownerTransaction = owner.Database.BeginTransaction();
            var participant = SingleEntityDbContext.Create(collection);

            var before = DateTimeOffset.UtcNow;
            var participantTransaction = participant.Database.UseTransaction(ownerTransaction);
            var after = DateTimeOffset.UtcNow;

            var enlisted = Assert.Single(
                collector.EventsFor(participant), e => e.Name == MongoEventId.TransactionEnlisted.Name).Data;
            Assert.Same(participantTransaction, enlisted.Transaction);
            Assert.Same(participant, enlisted.Context);
            Assert.Equal(MongoEventId.TransactionEnlisted, enlisted.EventId);
            Assert.Equal(LogLevel.Information, enlisted.LogLevel);
            Assert.False(enlisted.IsAsync);
            Assert.InRange(enlisted.StartTime, before, after);
            Assert.Contains($"Enlisted in transaction {participantTransaction.TransactionId}", enlisted.ToString());
            Assert.Contains($"transaction {ownerTransaction.TransactionId})", enlisted.ToString());

            Assert.DoesNotContain(
                collector.EventsFor(participant), e => e.Name == MongoEventId.TransactionDetached.Name);

            before = DateTimeOffset.UtcNow;
            if (async)
            {
                await participant.DisposeAsync();
            }
            else
            {
                participant.Dispose();
            }

            after = DateTimeOffset.UtcNow;

            var detached = Assert.Single(
                collector.EventsFor(participant), e => e.Name == MongoEventId.TransactionDetached.Name).Data;
            Assert.Same(participantTransaction, detached.Transaction);
            Assert.Same(participant, detached.Context);
            Assert.Equal(MongoEventId.TransactionDetached, detached.EventId);
            Assert.Equal(LogLevel.Information, detached.LogLevel);
            Assert.Equal(async, detached.IsAsync);
            Assert.InRange(detached.StartTime, before, after);
            Assert.Contains($"Detached from transaction {participantTransaction.TransactionId}", detached.ToString());

            // The owner raised neither: it did not join anything.
            Assert.DoesNotContain(collector.EventsFor(owner), e => e.Name == MongoEventId.TransactionEnlisted.Name);
            Assert.DoesNotContain(collector.EventsFor(owner), e => e.Name == MongoEventId.TransactionDetached.Name);

            owner.Database.RollbackTransaction();
        }
        finally
        {
            collector.Stop();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Disposing_an_owned_transaction_that_already_ended_raises_a_rollback_skipped_payload(bool async)
    {
        var collection = database.CreateCollection<SimpleEntity>($"Skipped_{async}");
        var collector = new TransactionEventCollector();
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);
        try
        {
            var db = SingleEntityDbContext.Create(collection);
            var transaction = (MongoTransaction)db.Database.BeginTransaction();

            // Something else holding the session ends the transaction, so there is nothing to roll back.
            transaction.ActiveSession.AbortTransaction();

            var before = DateTimeOffset.UtcNow;
            if (async)
            {
                await db.DisposeAsync();
            }
            else
            {
                db.Dispose();
            }

            var after = DateTimeOffset.UtcNow;

            var skipped = Assert.Single(
                collector.EventsFor(db), e => e.Name == MongoEventId.TransactionRollbackSkipped.Name).Data;
            Assert.Same(transaction, skipped.Transaction);
            Assert.Same(db, skipped.Context);
            Assert.Equal(MongoEventId.TransactionRollbackSkipped, skipped.EventId);
            Assert.Equal(LogLevel.Information, skipped.LogLevel);
            Assert.Equal(async, skipped.IsAsync);
            Assert.InRange(skipped.StartTime, before, after);
            Assert.Contains($"Disposing transaction {transaction.TransactionId} without rolling it back", skipped.ToString());

            // No rollback was attempted, so none was reported.
            Assert.DoesNotContain(collector.EventsFor(db), e => e.Name == MongoEventId.TransactionRollingBack.Name);
            Assert.DoesNotContain(collector.EventsFor(db), e => e.Name == MongoEventId.TransactionRolledBack.Name);
        }
        finally
        {
            collector.Stop();
        }
    }
}
