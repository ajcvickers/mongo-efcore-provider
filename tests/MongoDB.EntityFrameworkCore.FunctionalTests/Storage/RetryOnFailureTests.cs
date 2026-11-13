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

using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Connections;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Storage;

public class RetryOnFailureTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class SimpleEntity
    {
        public ObjectId _id { get; set; }
        public string name { get; set; } = null!;
    }

    private static SingleEntityDbContext<SimpleEntity> CreateContext(
        IMongoCollection<SimpleEntity> collection,
        bool retry = true,
        Action<DbContextOptionsBuilder>? configure = null)
        => SingleEntityDbContext.Create(
            collection,
            optionsBuilderAction: ob =>
            {
                if (retry)
                {
                    new MongoDbContextOptionsBuilder(ob).EnableRetryOnFailure(10, TimeSpan.FromMilliseconds(50));
                }

                configure?.Invoke(ob);
            });

    // A second context holds an uncommitted write to the document, so the first writer to the document in another transaction fails
    // with a write conflict, which MongoDB labels TransientTransactionError.
    private static SingleEntityDbContext<SimpleEntity> HoldDocument(IMongoCollection<SimpleEntity> collection)
    {
        var holder = CreateContext(collection, retry: false);
        holder.Database.BeginTransaction();
        holder.Entities.First().name = "held";
        holder.SaveChanges();
        return holder;
    }

    private static bool HasTransientTransactionErrorLabel(Exception? exception)
        => exception switch
        {
            null => false,
            MongoException mongoException when mongoException.HasErrorLabel("TransientTransactionError") => true,
            _ => HasTransientTransactionErrorLabel(exception.InnerException)
        };

    private static void CreateUniqueNameIndex(IMongoCollection<SimpleEntity> collection)
        => collection.Indexes.CreateOne(
            new CreateIndexModel<SimpleEntity>(
                Builders<SimpleEntity>.IndexKeys.Ascending(e => e.name),
                new CreateIndexOptions { Unique = true }));

    [Fact]
    public void Execute_retries_the_whole_unit_of_work_after_a_write_conflict()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        collection.InsertOne(new SimpleEntity { name = "original" });

        using var holder = HoldDocument(collection);
        using var db = CreateContext(collection);

        var attempts = 0;
        db.Database.CreateExecutionStrategy().Execute(() =>
        {
            // The first attempt conflicts with the held transaction, which then ends so that the retry can succeed.
            if (++attempts == 2)
            {
                holder.Database.CommitTransaction();
            }

            // Failed attempts leave their entities tracked, so start each attempt from a clean tracker, as with relational providers.
            db.ChangeTracker.Clear();
            using var transaction = db.Database.BeginTransaction();
            db.Entities.First().name = "retried";
            db.SaveChanges();
            transaction.Commit();
        });

        Assert.Equal(2, attempts);
        Assert.Equal("retried", collection.Find(FilterDefinition<SimpleEntity>.Empty).Single().name);
    }

    [Fact]
    public async Task ExecuteAsync_retries_the_whole_unit_of_work_after_a_write_conflict()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        await collection.InsertOneAsync(new SimpleEntity { name = "original" });

        await using var holder = HoldDocument(collection);
        await using var db = CreateContext(collection);

        var attempts = 0;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (++attempts == 2)
            {
                await holder.Database.CommitTransactionAsync();
            }

            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync();
            (await db.Entities.FirstAsync()).name = "retried";
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        Assert.Equal(2, attempts);
        Assert.Equal("retried", (await collection.Find(FilterDefinition<SimpleEntity>.Empty).SingleAsync()).name);
    }

    [Fact]
    public void SaveChanges_retries_its_own_implicit_transaction_after_a_write_conflict()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        collection.InsertMany([new SimpleEntity { name = "one" }, new SimpleEntity { name = "two" }]);

        using var holder = HoldDocument(collection);

        // The holder is released from the log event EF raises when it is about to retry, so the first attempt always conflicts with it
        // and the retry always succeeds, however slow the server is.
        var retries = 0;
        using var db = CreateContext(
            collection,
            configure: ob => ob.LogTo(
                _ =>
                {
                    if (retries++ == 0)
                    {
                        holder.Database.CommitTransaction();
                    }
                },
                [CoreEventId.ExecutionStrategyRetrying]));

        foreach (var entity in db.Entities.ToList())
        {
            entity.name += "-updated";
        }

        // Two changed roots are saved in an implicit transaction, which conflicts with the held write.
        db.SaveChanges();

        Assert.Equal(1, retries);
        Assert.Equal(
            ["one-updated", "two-updated"],
            collection.Find(FilterDefinition<SimpleEntity>.Empty).ToList().Select(e => e.name).Order());
    }

    [Fact]
    public void Without_EnableRetryOnFailure_a_write_conflict_is_not_retried()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        collection.InsertOne(new SimpleEntity { name = "original" });

        using var holder = HoldDocument(collection);
        using var db = CreateContext(collection, retry: false);

        var attempts = 0;
        var exception = Assert.ThrowsAny<Exception>(() => db.Database.CreateExecutionStrategy().Execute(() =>
        {
            attempts++;
            using var transaction = db.Database.BeginTransaction();
            db.Entities.First().name = "retried";
            db.SaveChanges();
            transaction.Commit();
        }));

        Assert.Equal(1, attempts);

        // Without the retrying strategy the error surfaces unchanged, with the label the strategy would have retried on.
        Assert.True(HasTransientTransactionErrorLabel(exception));
    }

    [Fact]
    public void A_duplicate_key_error_is_not_retried()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        CreateUniqueNameIndex(collection);
        collection.InsertOne(new SimpleEntity { name = "taken" });

        using var db = CreateContext(collection);

        var attempts = 0;
        Assert.ThrowsAny<Exception>(() => db.Database.CreateExecutionStrategy().Execute(() =>
        {
            attempts++;
            db.ChangeTracker.Clear();
            using var transaction = db.Database.BeginTransaction();
            db.Entities.Add(new SimpleEntity { name = "taken" });
            db.SaveChanges();
            transaction.Commit();
        }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public void A_failed_SaveChanges_without_a_transaction_is_not_retried()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        CreateUniqueNameIndex(collection);
        collection.InsertOne(new SimpleEntity { name = "taken" });

        using var db = CreateContext(collection);
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        var attempts = 0;
        Assert.ThrowsAny<Exception>(() => db.Database.CreateExecutionStrategy().Execute(() =>
        {
            attempts++;
            db.ChangeTracker.Clear();
            db.Entities.AddRange(new SimpleEntity { name = "fresh" }, new SimpleEntity { name = "taken" });
            db.SaveChanges();
        }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public void A_transaction_started_outside_the_execution_strategy_is_rejected_when_retrying_is_enabled()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var db = CreateContext(collection);
        using var transaction = db.Database.BeginTransaction();

        db.Entities.Add(new SimpleEntity { name = "test" });
        var exception = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());

        Assert.Contains("CreateExecutionStrategy", exception.Message);
    }

    [Fact]
    public void A_transaction_started_outside_the_execution_strategy_still_works_when_retrying_is_not_enabled()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var db = CreateContext(collection, retry: false);
        using var transaction = db.Database.BeginTransaction();

        db.Entities.Add(new SimpleEntity { name = "test" });
        db.SaveChanges();
        transaction.Commit();

        Assert.Single(collection.Find(FilterDefinition<SimpleEntity>.Empty).ToList());
    }

    [Fact]
    public void Contexts_that_join_the_transaction_inside_the_delegate_work_across_a_retry()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        collection.InsertOne(new SimpleEntity { name = "original" });

        using var holder = HoldDocument(collection);
        using var owner = CreateContext(collection);

        var attempts = 0;
        owner.Database.CreateExecutionStrategy().Execute(() =>
        {
            if (++attempts == 2)
            {
                holder.Database.CommitTransaction();
            }

            using var transaction = owner.Database.BeginTransaction();

            // Created and joined inside the delegate, so every attempt gets a context in the new transaction.
            using var joined = CreateContext(collection, retry: false);
            joined.Database.UseTransaction(transaction);
            joined.Entities.First().name = "joined";
            joined.SaveChanges();

            transaction.Commit();
        });

        Assert.Equal(2, attempts);
        Assert.Equal("joined", collection.Find(FilterDefinition<SimpleEntity>.Empty).Single().name);
    }

    [Fact]
    public void A_context_that_joined_the_failed_transaction_throws_instead_of_writing_outside_the_retried_transaction()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        collection.InsertOne(new SimpleEntity { name = "original" });

        using var holder = HoldDocument(collection);
        using var owner = CreateContext(collection);
        using var stale = CreateContext(collection, retry: false);

        var attempts = 0;
        owner.Database.CreateExecutionStrategy().Execute(() =>
        {
            owner.ChangeTracker.Clear();
            if (++attempts == 2)
            {
                holder.Database.CommitTransaction();
                using var retried = owner.Database.BeginTransaction();
                owner.Entities.First().name = "retried";
                owner.SaveChanges();

                // 'stale' joined the transaction of the first attempt, which was aborted: it must not write outside a transaction.
                stale.Entities.Add(new SimpleEntity { name = "stale" });
                var exception = Assert.Throws<InvalidOperationException>(() => stale.SaveChanges());
                Assert.Contains("no longer active", exception.Message);

                retried.Commit();
                return;
            }

            using var first = owner.Database.BeginTransaction();
            stale.Database.UseTransaction(first);
            owner.Entities.First().name = "retried";
            owner.SaveChanges();
            first.Commit();
        });

        Assert.Equal(2, attempts);
        Assert.Equal("retried", collection.Find(FilterDefinition<SimpleEntity>.Empty).Single().name);
    }

    // Wraps a real session so that its first commits fail the way the server reports a commit whose result is unknown. (The test
    // containers do not allow fail points, and the retry must not depend on server configuration.)
    private class FailingCommitSession : DispatchProxy
    {
        public IClientSessionHandle Inner { get; set; } = null!;
        public int FailuresRemaining { get; set; }
        public int CommitCalls { get; private set; }
        public Func<Exception> CreateFailure { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name is nameof(IClientSession.CommitTransaction) or nameof(IClientSession.CommitTransactionAsync))
            {
                CommitCalls++;
                if (FailuresRemaining > 0)
                {
                    FailuresRemaining--;
                    var failure = CreateFailure();
                    if (targetMethod.Name == nameof(IClientSession.CommitTransactionAsync))
                    {
                        return Task.FromException(failure);
                    }

                    throw failure;
                }
            }

            try
            {
                return targetMethod.Invoke(Inner, args);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
                throw;
            }
        }

        public static (IClientSessionHandle Session, FailingCommitSession Proxy) Wrap(
            IClientSessionHandle inner, int failures, Func<Exception> createFailure)
        {
            var session = Create<IClientSessionHandle, FailingCommitSession>();
            var proxy = (FailingCommitSession)(object)session;
            proxy.Inner = inner;
            proxy.FailuresRemaining = failures;
            proxy.CreateFailure = createFailure;
            return (session, proxy);
        }
    }

    private static MongoException UnknownCommitResult()
    {
        var exception = new MongoException("The commit result is unknown.");
        exception.AddErrorLabel("UnknownTransactionCommitResult");
        return exception;
    }

    private static void LogCommitRetries(DbContextOptionsBuilder optionsBuilder, List<string> logged)
        => optionsBuilder.LogTo(
            logged.Add,
            [MongoEventId.TransactionCommitRetrying],
            LogLevel.Warning);

    private MongoTransaction StartTransaction(
        SingleEntityDbContext<SimpleEntity> db, IClientSessionHandle session)
        => MongoTransaction.Start(
            session,
            db,
            async: false,
            new TransactionOptions(),
            transactionManager: null,
            db.GetService<IDiagnosticsLogger<DbLoggerCategory.Database.Transaction>>());

    [Fact]
    public void A_commit_with_an_unknown_result_is_retried_without_applying_the_transaction_twice()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        var logged = new List<string>();
        using var db = CreateContext(collection, retry: false, ob => LogCommitRetries(ob, logged));
        using var real = collection.Database.Client.StartSession();
        var (session, proxy) = FailingCommitSession.Wrap(real, failures: 2, UnknownCommitResult);

        using var transaction = StartTransaction(db, session);
        collection.InsertOne(real, new SimpleEntity { name = "once" });
        transaction.Commit();

        Assert.Equal(3, proxy.CommitCalls);
        Assert.Equal(2, logged.Count);
        Assert.Equal(1, collection.CountDocuments(FilterDefinition<SimpleEntity>.Empty));
    }

    [Fact]
    public async Task CommitAsync_with_an_unknown_result_is_retried_without_applying_the_transaction_twice()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        var logged = new List<string>();
        await using var db = CreateContext(collection, retry: false, ob => LogCommitRetries(ob, logged));
        using var real = await collection.Database.Client.StartSessionAsync();
        var (session, proxy) = FailingCommitSession.Wrap(real, failures: 2, UnknownCommitResult);

        await using var transaction = StartTransaction(db, session);
        await collection.InsertOneAsync(real, new SimpleEntity { name = "once" });
        await transaction.CommitAsync();

        Assert.Equal(3, proxy.CommitCalls);
        Assert.Equal(2, logged.Count);
        Assert.Equal(1, await collection.CountDocumentsAsync(FilterDefinition<SimpleEntity>.Empty));
    }

    [Fact]
    public void A_commit_failure_without_the_unknown_result_label_is_not_retried()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var db = CreateContext(collection, retry: false);
        using var real = collection.Database.Client.StartSession();
        var (session, proxy) = FailingCommitSession.Wrap(real, failures: 1, () => new MongoException("some other failure"));

        using var transaction = StartTransaction(db, session);
        collection.InsertOne(real, new SimpleEntity { name = "never" });

        Assert.Equal("some other failure", Assert.Throws<MongoException>(() => transaction.Commit()).Message);
        Assert.Equal(1, proxy.CommitCalls);
    }

    [Fact]
    public void A_commit_whose_server_time_limit_expired_is_not_retried()
    {
        var collection = database.CreateCollection<SimpleEntity>();
        using var db = CreateContext(collection, retry: false);
        using var real = collection.Database.Client.StartSession();
        var (session, proxy) = FailingCommitSession.Wrap(
            real,
            failures: 1,
            () =>
            {
                var exception = new MongoExecutionTimeoutException(
                    new ConnectionId(new ServerId(new ClusterId(), new System.Net.DnsEndPoint("localhost", 27017))),
                    "Operation exceeded time limit",
                    new BsonDocument { { "code", 50 }, { "codeName", "MaxTimeMSExpired" } });
                exception.AddErrorLabel("UnknownTransactionCommitResult");
                return exception;
            });

        using var transaction = StartTransaction(db, session);
        collection.InsertOne(real, new SimpleEntity { name = "never" });

        Assert.Throws<MongoExecutionTimeoutException>(() => transaction.Commit());
        Assert.Equal(1, proxy.CommitCalls);
    }
}
