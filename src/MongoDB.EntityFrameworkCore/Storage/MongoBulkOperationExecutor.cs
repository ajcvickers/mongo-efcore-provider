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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Query;

namespace MongoDB.EntityFrameworkCore.Storage;

/// <summary>
/// Executes a <see cref="MongoBulkPlan"/> (server-side <c>ExecuteDelete</c>/<c>ExecuteUpdate</c>): runs the
/// <c>deleteMany</c>/<c>updateMany</c> driver writes, orchestrates the two-phase transaction, materializes target
/// ids, and emits the bulk diagnostics events. Translation of the filter/update/target query is deferred to the
/// plan's delegates (owned by the query pipeline).
/// </summary>
internal static class MongoBulkOperationExecutor
{
    // Explicit arms plus throw: a new MongoBulkStrategy must add an arm rather than fall through to single-command/delete.
    public static int Execute(QueryContext queryContext, MongoBulkPlan plan)
        => (plan.Strategy, plan.Kind) switch
        {
            (MongoBulkStrategy.SingleCommand, MongoBulkOperationKind.Delete) => ExecuteDelete(queryContext, plan),
            (MongoBulkStrategy.SingleCommand, MongoBulkOperationKind.Update) => ExecuteUpdate(queryContext, plan),
            (MongoBulkStrategy.TwoPhase, MongoBulkOperationKind.Delete) => ExecuteTwoPhaseDelete(queryContext, plan),
            (MongoBulkStrategy.TwoPhase, MongoBulkOperationKind.Update) => ExecuteTwoPhaseUpdate(queryContext, plan),
            _ => throw new ArgumentOutOfRangeException(nameof(plan), $"Unsupported bulk plan: {plan.Strategy}/{plan.Kind}.")
        };

    public static Task<int> ExecuteAsync(QueryContext queryContext, MongoBulkPlan plan)
        => (plan.Strategy, plan.Kind) switch
        {
            (MongoBulkStrategy.SingleCommand, MongoBulkOperationKind.Delete) => ExecuteDeleteAsync(queryContext, plan),
            (MongoBulkStrategy.SingleCommand, MongoBulkOperationKind.Update) => ExecuteUpdateAsync(queryContext, plan),
            (MongoBulkStrategy.TwoPhase, MongoBulkOperationKind.Delete) => ExecuteTwoPhaseDeleteAsync(queryContext, plan),
            (MongoBulkStrategy.TwoPhase, MongoBulkOperationKind.Update) => ExecuteTwoPhaseUpdateAsync(queryContext, plan),
            _ => throw new ArgumentOutOfRangeException(nameof(plan), $"Unsupported bulk plan: {plan.Strategy}/{plan.Kind}.")
        };

    private static int ExecuteDelete(QueryContext queryContext, MongoBulkPlan plan)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, plan.CollectionName);
        var filter = plan.BuildFilter!(queryContext);

        var updateLogger = GetUpdateLogger(queryContext);
        var stopwatch = Stopwatch.StartNew();
        updateLogger.ExecutingBulkDelete(
            stopwatch.Elapsed, collection.CollectionNamespace, plan.Strategy.ToString(), targetCount: null,
            () => RenderBulkCommand(updateLogger, filter, update: null, targetIdQuery: null));

        var result = session == null ? collection.DeleteMany(filter) : collection.DeleteMany(session, filter);

        updateLogger.ExecutedBulkDelete(
            stopwatch.Elapsed, collection.CollectionNamespace, result.DeletedCount, plan.Strategy.ToString());
        return checked((int)result.DeletedCount);
    }

    private static async Task<int> ExecuteDeleteAsync(QueryContext queryContext, MongoBulkPlan plan)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, plan.CollectionName);
        var filter = plan.BuildFilter!(queryContext);

        var updateLogger = GetUpdateLogger(queryContext);
        var stopwatch = Stopwatch.StartNew();
        updateLogger.ExecutingBulkDelete(
            stopwatch.Elapsed, collection.CollectionNamespace, plan.Strategy.ToString(), targetCount: null,
            () => RenderBulkCommand(updateLogger, filter, update: null, targetIdQuery: null));

        var cancellationToken = queryContext.CancellationToken;
        var result = session == null
            ? await collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false)
            : await collection.DeleteManyAsync(session, filter, options: null, cancellationToken).ConfigureAwait(false);

        updateLogger.ExecutedBulkDelete(
            stopwatch.Elapsed, collection.CollectionNamespace, result.DeletedCount, plan.Strategy.ToString());
        return checked((int)result.DeletedCount);
    }

    private static int ExecuteUpdate(QueryContext queryContext, MongoBulkPlan plan)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, plan.CollectionName);
        var filter = plan.BuildFilter!(queryContext);
        var update = plan.BuildUpdate!(queryContext);

        var updateLogger = GetUpdateLogger(queryContext);
        var stopwatch = Stopwatch.StartNew();
        updateLogger.ExecutingBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, plan.Strategy.ToString(), targetCount: null,
            () => RenderBulkCommand(updateLogger, filter, update, targetIdQuery: null));

        var result = session == null
            ? collection.UpdateMany(filter, update)
            : collection.UpdateMany(session, filter, update);

        updateLogger.ExecutedBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, result.MatchedCount, result.ModifiedCount, plan.Strategy.ToString());
        return checked((int)result.MatchedCount);
    }

    private static async Task<int> ExecuteUpdateAsync(QueryContext queryContext, MongoBulkPlan plan)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, plan.CollectionName);
        var filter = plan.BuildFilter!(queryContext);
        var update = plan.BuildUpdate!(queryContext);

        var updateLogger = GetUpdateLogger(queryContext);
        var stopwatch = Stopwatch.StartNew();
        updateLogger.ExecutingBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, plan.Strategy.ToString(), targetCount: null,
            () => RenderBulkCommand(updateLogger, filter, update, targetIdQuery: null));

        var cancellationToken = queryContext.CancellationToken;
        var result = session == null
            ? await collection.UpdateManyAsync(filter, update, options: null, cancellationToken).ConfigureAwait(false)
            : await collection.UpdateManyAsync(session, filter, update, options: null, cancellationToken).ConfigureAwait(false);

        updateLogger.ExecutedBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, result.MatchedCount, result.ModifiedCount, plan.Strategy.ToString());
        return checked((int)result.MatchedCount);
    }

    // The update document is built BEFORE the transaction opens: an unsupported setter is a translation error and must be reported as
    // one, not as a transaction problem (on a standalone deployment BeginTransaction fails first), and it should not cost a transaction
    // round trip. The phase-1 query must NOT be: it binds the transaction's session when it is built (TranslateQuery reads
    // CurrentTransaction), so built ahead of BeginTransaction it would read outside the transaction. The driver's lazy translation of it
    // is mapped to the canonical translation failure where it is enumerated (MaterializeIds).
    private static int ExecuteTwoPhaseDelete(QueryContext queryContext, MongoBulkPlan plan)
        => RunInBulkTransaction(queryContext, () =>
        {
            // The stopwatch spans phase 1 (the read that collects the target ids) as well as phase 2.
            var stopwatch = Stopwatch.StartNew();
            var targetIdQuery = plan.BuildTargetIdQuery!(queryContext);
            var ids = MaterializeIds(plan, targetIdQuery);
            return ids.Count == 0 ? 0 : DeleteByIds(queryContext, plan, targetIdQuery, ids, stopwatch);
        });

    private static Task<int> ExecuteTwoPhaseDeleteAsync(QueryContext queryContext, MongoBulkPlan plan)
        => RunInBulkTransactionAsync(queryContext, async () =>
        {
            // The stopwatch spans phase 1 (the read that collects the target ids) as well as phase 2.
            var stopwatch = Stopwatch.StartNew();
            var targetIdQuery = plan.BuildTargetIdQuery!(queryContext);
            var ids = await MaterializeIdsAsync(plan, targetIdQuery, queryContext.CancellationToken).ConfigureAwait(false);
            return ids.Count == 0 ? 0 : await DeleteByIdsAsync(queryContext, plan, targetIdQuery, ids, stopwatch).ConfigureAwait(false);
        });

    private static int ExecuteTwoPhaseUpdate(QueryContext queryContext, MongoBulkPlan plan)
    {
        var update = plan.BuildUpdate!(queryContext);

        return RunInBulkTransaction(queryContext, () =>
        {
            // The stopwatch spans phase 1 (the read that collects the target ids) as well as phase 2.
            var stopwatch = Stopwatch.StartNew();
            var targetIdQuery = plan.BuildTargetIdQuery!(queryContext);
            var ids = MaterializeIds(plan, targetIdQuery);
            return ids.Count == 0 ? 0 : UpdateByIds(queryContext, plan, targetIdQuery, ids, update, stopwatch);
        });
    }

    private static Task<int> ExecuteTwoPhaseUpdateAsync(QueryContext queryContext, MongoBulkPlan plan)
    {
        var update = plan.BuildUpdate!(queryContext);

        return RunInBulkTransactionAsync(queryContext, async () =>
        {
            // The stopwatch spans phase 1 (the read that collects the target ids) as well as phase 2.
            var stopwatch = Stopwatch.StartNew();
            var targetIdQuery = plan.BuildTargetIdQuery!(queryContext);
            var ids = await MaterializeIdsAsync(plan, targetIdQuery, queryContext.CancellationToken).ConfigureAwait(false);
            return ids.Count == 0
                ? 0
                : await UpdateByIdsAsync(queryContext, plan, targetIdQuery, ids, update, stopwatch).ConfigureAwait(false);
        });
    }

    // A single { _id: { $in: [...] } } filter is bounded by the 16 MB BSON command limit (about 500k ObjectIds), and a very large $in
    // costs real query-planning time, so phase 2 is split into batches. Every batch runs on the one session phase 1 opened, so they commit
    // or abort together: atomicity is unchanged. Batching buys NO relief from transactionLifetimeLimitSeconds; more round trips inside the
    // one transaction can only make that limit easier to reach. The ids themselves are still buffered in one list.
    private const int DefaultMaxIdsPerBulkCommand = 50_000;

    private static int MaxIdsPerBulkCommand = DefaultMaxIdsPerBulkCommand;

    internal static IEnumerable<List<BsonValue>> BatchIds(List<BsonValue> ids, int batchSize)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        for (var start = 0; start < ids.Count; start += batchSize)
        {
            yield return ids.GetRange(start, Math.Min(batchSize, ids.Count - start));
        }
    }

    // Test seam: shrinks the ceiling so a handful of documents spans several batches. Internal (reachable only through
    // InternalsVisibleTo) and restored on Dispose even if the test throws. A plain static is acceptable because both test assemblies
    // disable test parallelization.
    internal static IDisposable OverrideMaxIdsPerBulkCommand(int batchSize)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        return new MaxIdsPerBulkCommandOverride(batchSize);
    }

    private sealed class MaxIdsPerBulkCommandOverride : IDisposable
    {
        private readonly int _previous = MaxIdsPerBulkCommand;

        public MaxIdsPerBulkCommandOverride(int batchSize) => MaxIdsPerBulkCommand = batchSize;

        public void Dispose() => MaxIdsPerBulkCommand = _previous;
    }

    private static int DeleteByIds(
        QueryContext queryContext, MongoBulkPlan plan, IQueryable<BsonDocument> targetIdQuery, List<BsonValue> ids, Stopwatch stopwatch)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, plan.CollectionName);
        var updateLogger = GetUpdateLogger(queryContext);

        long deleted = 0;
        foreach (var batch in BatchIds(ids, MaxIdsPerBulkCommand))
        {
            var filter = Builders<BsonDocument>.Filter.In("_id", batch);
            updateLogger.ExecutingBulkDelete(
                stopwatch.Elapsed, collection.CollectionNamespace, plan.Strategy.ToString(), batch.Count,
                () => RenderBulkCommand(updateLogger, filter, update: null, targetIdQuery));

            var result = session == null ? collection.DeleteMany(filter) : collection.DeleteMany(session, filter);
            deleted += result.DeletedCount;
        }

        updateLogger.ExecutedBulkDelete(stopwatch.Elapsed, collection.CollectionNamespace, deleted, plan.Strategy.ToString());
        return checked((int)deleted);
    }

    private static async Task<int> DeleteByIdsAsync(
        QueryContext queryContext, MongoBulkPlan plan, IQueryable<BsonDocument> targetIdQuery, List<BsonValue> ids, Stopwatch stopwatch)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, plan.CollectionName);
        var updateLogger = GetUpdateLogger(queryContext);
        var cancellationToken = queryContext.CancellationToken;

        long deleted = 0;
        foreach (var batch in BatchIds(ids, MaxIdsPerBulkCommand))
        {
            var filter = Builders<BsonDocument>.Filter.In("_id", batch);
            updateLogger.ExecutingBulkDelete(
                stopwatch.Elapsed, collection.CollectionNamespace, plan.Strategy.ToString(), batch.Count,
                () => RenderBulkCommand(updateLogger, filter, update: null, targetIdQuery));

            var result = session == null
                ? await collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false)
                : await collection.DeleteManyAsync(session, filter, options: null, cancellationToken).ConfigureAwait(false);
            deleted += result.DeletedCount;
        }

        updateLogger.ExecutedBulkDelete(stopwatch.Elapsed, collection.CollectionNamespace, deleted, plan.Strategy.ToString());
        return checked((int)deleted);
    }

    private static int UpdateByIds(
        QueryContext queryContext, MongoBulkPlan plan, IQueryable<BsonDocument> targetIdQuery, List<BsonValue> ids,
        UpdateDefinition<BsonDocument> update, Stopwatch stopwatch)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, plan.CollectionName);
        var updateLogger = GetUpdateLogger(queryContext);

        long matched = 0, modified = 0;
        foreach (var batch in BatchIds(ids, MaxIdsPerBulkCommand))
        {
            var filter = Builders<BsonDocument>.Filter.In("_id", batch);
            updateLogger.ExecutingBulkUpdate(
                stopwatch.Elapsed, collection.CollectionNamespace, plan.Strategy.ToString(), batch.Count,
                () => RenderBulkCommand(updateLogger, filter, update, targetIdQuery));

            var result = session == null
                ? collection.UpdateMany(filter, update)
                : collection.UpdateMany(session, filter, update);
            matched += result.MatchedCount;
            modified += result.ModifiedCount;
        }

        updateLogger.ExecutedBulkUpdate(stopwatch.Elapsed, collection.CollectionNamespace, matched, modified, plan.Strategy.ToString());
        return checked((int)matched);
    }

    private static async Task<int> UpdateByIdsAsync(
        QueryContext queryContext, MongoBulkPlan plan, IQueryable<BsonDocument> targetIdQuery, List<BsonValue> ids,
        UpdateDefinition<BsonDocument> update, Stopwatch stopwatch)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, plan.CollectionName);
        var updateLogger = GetUpdateLogger(queryContext);
        var cancellationToken = queryContext.CancellationToken;

        long matched = 0, modified = 0;
        foreach (var batch in BatchIds(ids, MaxIdsPerBulkCommand))
        {
            var filter = Builders<BsonDocument>.Filter.In("_id", batch);
            updateLogger.ExecutingBulkUpdate(
                stopwatch.Elapsed, collection.CollectionNamespace, plan.Strategy.ToString(), batch.Count,
                () => RenderBulkCommand(updateLogger, filter, update, targetIdQuery));

            var result = session == null
                ? await collection.UpdateManyAsync(filter, update, options: null, cancellationToken).ConfigureAwait(false)
                : await collection.UpdateManyAsync(session, filter, update, options: null, cancellationToken).ConfigureAwait(false);
            matched += result.MatchedCount;
            modified += result.ModifiedCount;
        }

        updateLogger.ExecutedBulkUpdate(stopwatch.Elapsed, collection.CollectionNamespace, matched, modified, plan.Strategy.ToString());
        return checked((int)matched);
    }

    // The driver translates the phase-1 LINQ query lazily, when it is first enumerated, so a source it cannot translate surfaces here.
    // ONLY ExpressionNotSupportedException is mapped to EF's canonical translation failure: this is an I/O site, so a broad filter
    // would relabel cancellation and server or network errors (write conflicts, timeouts) as "could not be translated".
    private static List<BsonValue> MaterializeIds(MongoBulkPlan plan, IQueryable<BsonDocument> documents)
    {
        try
        {
            var ids = new List<BsonValue>();
            foreach (var document in documents)
            {
                ids.Add(document["_id"]);
            }

            return ids;
        }
        catch (ExpressionNotSupportedException exception) when (plan.TranslationFailed != null)
        {
            throw plan.TranslationFailed(exception);
        }
    }

    // Streams the cursor, keeping only each document's _id, so peak memory is one batch plus the ids (as the sync path's lazy enumeration
    // is) rather than every matched document held at once by ToListAsync. No capacity hint: the count is unknown until the cursor is
    // drained. The narrow catch wraps only ToCursorAsync, where the driver translates the query (synchronously, before any I/O), so a
    // MoveNextAsync failure (cancellation, network, server) is never relabelled as a translation error.
    private static async Task<List<BsonValue>> MaterializeIdsAsync(
        MongoBulkPlan plan, IQueryable<BsonDocument> documents, CancellationToken cancellationToken)
    {
        IAsyncCursor<BsonDocument> cursor;
        try
        {
            cursor = await ((IAsyncCursorSource<BsonDocument>)documents).ToCursorAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ExpressionNotSupportedException exception) when (plan.TranslationFailed != null)
        {
            throw plan.TranslationFailed(exception);
        }

        using (cursor)
        {
            var ids = new List<BsonValue>();
            while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (var document in cursor.Current)
                {
                    ids.Add(document["_id"]);
                }
            }

            return ids;
        }
    }

    // Both phases share one transaction (one snapshot). A user-opened transaction is joined and never committed here;
    // otherwise one is auto-started, unless AutoTransactionBehavior.Never. Starting via database.BeginTransaction()
    // keeps commit/rollback logging and the transaction id.

    private static int RunInBulkTransaction(QueryContext queryContext, Func<int> body)
    {
        var database = queryContext.Context.Database;
        if (database.CurrentTransaction != null)
        {
            // Joined, never committed: the caller owns it. Validated first so neither phase can run outside a transaction.
            RequireBulkSession(database);
            return body();
        }

        if (database.AutoTransactionBehavior == AutoTransactionBehavior.Never)
        {
            throw NoAutoTransactionError();
        }

        // BeginTransaction is inside the try: on a non-transactional deployment MongoTransaction.Start throws here,
        // and we want that mapped to the actionable StandaloneTransactionError like any other transaction failure.
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = database.BeginTransaction();
            var result = body();
            transaction.Commit();
            return result;
        }
        catch (Exception exception)
        {
            if (transaction != null)
            {
                SafeRollback(transaction);
            }

            if (IsTransactionsUnsupported(exception))
            {
                throw StandaloneTransactionError(exception);
            }

            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    private static async Task<int> RunInBulkTransactionAsync(QueryContext queryContext, Func<Task<int>> body)
    {
        var database = queryContext.Context.Database;
        if (database.CurrentTransaction != null)
        {
            // Joined, never committed: the caller owns it. Validated first so neither phase can run outside a transaction.
            RequireBulkSession(database);
            return await body().ConfigureAwait(false);
        }

        if (database.AutoTransactionBehavior == AutoTransactionBehavior.Never)
        {
            throw NoAutoTransactionError();
        }

        var cancellationToken = queryContext.CancellationToken;
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var result = await body().ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (Exception exception)
        {
            if (transaction != null)
            {
                await SafeRollbackAsync(transaction).ConfigureAwait(false);
            }

            if (IsTransactionsUnsupported(exception))
            {
                throw StandaloneTransactionError(exception);
            }

            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    // Two-phase correctness depends on phase 1 and phase 2 sharing one transaction: phase 2 filters on _id alone and never
    // re-evaluates the user's predicate, so isolation is what stops it acting on a document that no longer matches. The session is
    // read from CurrentTransaction with an `as MongoTransaction` cast (GetBulkCollectionAndSession), which yields null for any other
    // IDbContextTransaction, and a null session means both phases quietly run outside the transaction. Fail closed instead.
    private static IClientSessionHandle RequireBulkSession(DatabaseFacade database)
        => (database.CurrentTransaction as MongoTransaction)?.Session
           ?? throw new InvalidOperationException(
               "This bulk delete or update requires a MongoDB transaction, but the ambient transaction on this DbContext is not a "
               + "MongoDB transaction with an active session. Open the transaction with Database.BeginTransaction() on a "
               + "MongoDB-backed context.");

    private static void SafeRollback(IDbContextTransaction transaction)
    {
        try
        {
            transaction.Rollback();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // A failed or non-transactional session may not be rollback-able; the original error wins. An uncommitted transaction is
            // never durable, so the worst case is that the server reclaims it at transactionLifetimeLimitSeconds.
        }
    }

    // Deliberately takes no CancellationToken: this runs on the failure path, and when the failure IS cancellation, passing the
    // caller's already-cancelled token means the abortTransaction command is dropped before it reaches the wire (the driver's
    // pre-flight cancellation check short-circuits the write) while the driver still marks the transaction Aborted client-side, which
    // also disables its own dispose-time retry. The transaction then looks cleaned up locally but was never aborted on the server, and
    // lingers, holding locks, until transactionLifetimeLimitSeconds. Cleanup must not observe the caller's cancellation.
    private static async Task SafeRollbackAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // See SafeRollback.
        }
    }

    private static InvalidOperationException NoAutoTransactionError()
        => new(
            "This bulk delete or update is executed by the MongoDB provider (for example because it uses 'Skip'/'Take' or an 'OfType' type filter) "
            + "as a two-phase operation requiring a transaction. The context's AutoTransactionBehavior is 'Never', "
            + "so open an explicit transaction (Database.BeginTransaction) around the call.");

    private static InvalidOperationException StandaloneTransactionError(Exception inner)
        => new(
            "This bulk delete or update is executed by the MongoDB provider (for example because it uses 'Skip'/'Take' or an 'OfType' type filter) "
            + "as a two-phase operation requiring a transaction. The current MongoDB deployment does not support "
            + "multi-document transactions (a replica set or sharded cluster is required).", inner);

    // An unsupported deployment fails in two shapes: MongoTransaction.Start intercepts the driver's "Standalone servers do not support
    // transactions." and rethrows a NotSupportedException whose message contains "does not support transactions"; or the server replies
    // "Transaction numbers are only allowed on a replica set member or mongos" (a MongoCommandException with code 20, which is just the
    // concrete type the driver throws; the message is what identifies it). Code 20 is IllegalOperation in general, so it is not
    // matched on its own: an unrelated illegal operation inside the transaction must propagate, not be relabelled. Internal so the
    // unit tests can reach it.
    internal static bool IsTransactionsUnsupported(Exception exception)
        => (exception is MongoException && exception.Message.Contains("Transaction numbers are only allowed", StringComparison.Ordinal))
           || (exception is NotSupportedException && exception.Message.Contains("does not support transactions", StringComparison.Ordinal));

    private static (IMongoCollection<BsonDocument> collection, IClientSessionHandle? session) GetBulkCollectionAndSession(
        QueryContext queryContext, string collectionName)
    {
        var collection = queryContext.Context.GetService<IMongoClientWrapper>().GetCollection<BsonDocument>(collectionName);
        var session = (queryContext.Context.Database.CurrentTransaction as MongoTransaction)?.Session;
        return (collection, session);
    }

    // Server-side ExecuteDelete/ExecuteUpdate bypass SaveChanges, so MongoDatabaseWrapper's bulk-write logging never
    // fires for them. Resolve the Update-category logger from the context's service provider for the dedicated bulk events.
    // Renders the command for diagnostics. Command content includes predicate values and the values being written, so it is
    // gated on ShouldLogSensitiveData() exactly like MQL is on the read path (Diagnostics/AGENTS.md: redaction is single-pointed).
    private static string RenderBulkCommand(
        IDiagnosticsLogger<DbLoggerCategory.Update> logger,
        FilterDefinition<BsonDocument> filter,
        UpdateDefinition<BsonDocument>? update,
        IQueryable<BsonDocument>? targetIdQuery)
    {
        if (!logger.ShouldLogSensitiveData())
        {
            return "?";
        }

        var registry = BsonSerializer.SerializerRegistry;
        var renderArgs = new RenderArgs<BsonDocument>(registry.GetSerializer<BsonDocument>(), registry);
        var rendered = new BsonDocument();
        if (targetIdQuery != null)
        {
            // Phase 1: the pipeline the aggregate command actually carried (the driver's LoggedStages; it is populated once the query has
            // executed, which it has by the time phase 2 logs). JSON null rather than a throw if it is unavailable. Rendered here, behind
            // the one redaction gate above, so it is never disclosed with sensitive-data logging off.
            var stages = (targetIdQuery.Provider as IMongoQueryProvider)?.LoggedStages;
            rendered.Add("phase1Pipeline", stages == null ? BsonNull.Value : new BsonArray(stages));
        }

        rendered.Add("filter", filter.Render(renderArgs));
        if (update != null)
        {
            rendered.Add("update", update.Render(renderArgs));
        }

        return rendered.ToJson();
    }

    // Bulk operations bypass SaveChanges, so the update pipeline's own logging never fires for them. The Update-category logger is
    // supplied by MongoQueryContextFactory, as the read path gets its query logger, rather than resolved from the context's service
    // provider on every execution.
    private static IDiagnosticsLogger<DbLoggerCategory.Update> GetUpdateLogger(QueryContext queryContext)
        => ((MongoQueryContext)queryContext).UpdateLogger;
}

#endif
