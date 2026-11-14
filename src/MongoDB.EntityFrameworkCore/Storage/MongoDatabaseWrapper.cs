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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.Storage;

/// <summary>
/// Provides EF Core database-level operations using the underlying <see cref="IMongoClientWrapper"/>.
/// </summary>
public class MongoDatabaseWrapper : Database
{
    private readonly ICurrentDbContext _currentDbContext;
    private readonly IMongoClientWrapper _mongoClient;
    private readonly IDbContextTransactionManager _transactionManager;
    private readonly IDiagnosticsLogger<DbLoggerCategory.Update> _updateLogger;
    private readonly IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> _transactionLogger;
    private readonly TransactionOptions _transactionOptions = new();

    /// <summary>
    /// Creates a <see cref="MongoDatabaseWrapper"/> with the required dependencies, client wrapper and logging options.
    /// </summary>
    /// <param name="dependencies">The <see cref="DatabaseDependencies"/> this object should use.</param>
    /// <param name="currentDbContext">The <see cref="ICurrentDbContext"/> this should use to interact with the current context.</param>
    /// <param name="mongoClient">The <see cref="IMongoClientWrapper"/> this should use to interact with MongoDB.</param>
    /// <param name="transactionManager">The <see cref="IDbContextTransactionManager"/> this should use when determining transaction behavior.</param>
    /// <param name="updateLogger">The <see cref="IDiagnosticsLogger"/> for <see cref="DbLoggerCategory.Update"/>.</param>
    /// <param name="transactionLogger">The <see cref="IDiagnosticsLogger"/> for <see cref="DbLoggerCategory.Database.Transaction"/>.</param>
    public MongoDatabaseWrapper(
        DatabaseDependencies dependencies,
        ICurrentDbContext currentDbContext,
        IMongoClientWrapper mongoClient,
        IDbContextTransactionManager transactionManager,
        IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger,
        IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> transactionLogger)
        : base(dependencies)
    {
        _currentDbContext = currentDbContext;
        _mongoClient = mongoClient;
        _transactionManager = transactionManager;
        _updateLogger = updateLogger;
        _transactionLogger = transactionLogger;
    }

    /// <summary>
    /// Save all the changes detected in the entities collection to the underlying MongoDB database.
    /// </summary>
    /// <param name="entries">A list of <see cref="IUpdateEntry"/> indicating which changes to process.</param>
    /// <returns>Number of documents affected during saving changes.</returns>
    public override int SaveChanges(IList<IUpdateEntry> entries)
    {
        var rootEntries = GetAllChangedRootEntries(entries);
        var updates = MongoUpdate.CreateAll(rootEntries).ToList();
        AddEntriesPromotedDuringSave(entries);

        // Explicit transaction mode
        if (_transactionManager.CurrentTransaction is MongoTransaction mongoTransaction)
        {
            return WriteBatches(updates, mongoTransaction.ActiveSession);
        }

        using var session = _mongoClient.StartSession();

        // No transaction mode
        if (!ShouldUseImplicitTransaction(rootEntries.Count))
        {
            return WriteBatches(updates, session);
        }

        // Implicit transaction mode
        using var transaction =
            MongoTransaction.Start(session, _currentDbContext.Context, false, _transactionOptions, null, _transactionLogger);

        int result;
        try
        {
            result = WriteBatches(updates, session);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        transaction.Commit();
        return result;
    }

    /// <summary>
    /// Save all the changes detected in the entities collection to the underlying MongoDB database asynchronously.
    /// </summary>
    /// <param name="entries">A list of <see cref="IUpdateEntry"/> indicating which changes to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that might be cancelled during the save.</param>
    /// <returns>Task that when resolved contains the number of documents affected during saving changes.</returns>
    public override async Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken = default)
    {
        var rootEntries = GetAllChangedRootEntries(entries);
        var updates = MongoUpdate.CreateAll(rootEntries).ToList();
        AddEntriesPromotedDuringSave(entries);

        // Explicit transaction mode
        if (_transactionManager.CurrentTransaction is MongoTransaction mongoTransaction)
        {
            return await WriteBatchesAsync(updates, mongoTransaction.ActiveSession, cancellationToken).ConfigureAwait(false);
        }

        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        // No transaction mode
        if (!ShouldUseImplicitTransaction(rootEntries.Count))
        {
            return await WriteBatchesAsync(updates, session, cancellationToken).ConfigureAwait(false);
        }

        // Implicit transaction mode
        await using var transaction =
            MongoTransaction.Start(session, _currentDbContext.Context, true, _transactionOptions, null, _transactionLogger);

        int result;
        try
        {
            result = await WriteBatchesAsync(updates, session, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private int WriteBatches(IEnumerable<MongoUpdate> updates, IClientSessionHandle session)
    {
        var stopwatch = new Stopwatch();
        var documentsAffected = 0;

        foreach (var batch in MongoUpdateBatch.CreateBatches(updates))
        {
            stopwatch.Restart();
            var collection = _mongoClient.GetCollection<BsonDocument>(batch.CollectionName);
            _updateLogger.ExecutingBulkWrite(stopwatch.Elapsed, collection.CollectionNamespace, batch.Inserts, batch.Deletes,
                batch.Modified);
            BulkWriteResult<BsonDocument> result;
            try
            {
                result = collection.BulkWrite(session, batch.Updates.Select(u => u.Model));
            }
            catch (MongoBulkWriteException<BsonDocument> exception)
            {
                if (!TryCreateConcurrencyExceptionForFailedWrite(
                        batch, exception, collection.CollectionNamespace, out var concurrencyException, out var entries))
                {
                    throw;
                }

                var interception = _updateLogger.OptimisticConcurrencyException(
                    _currentDbContext.Context, entries, WithoutInnerException(concurrencyException, entries), null);
                if (interception.IsSuppressed)
                {
                    // The write failed, so there is nothing to carry on with: report the failure itself.
                    throw;
                }

                throw concurrencyException;
            }

            documentsAffected += AssertWritesApplied(batch, result, collection.CollectionNamespace);
            _updateLogger.ExecutedBulkWrite(stopwatch.Elapsed, collection.CollectionNamespace, result.InsertedCount,
                result.DeletedCount, result.ModifiedCount);
        }

        return documentsAffected;
    }

    private async Task<int> WriteBatchesAsync(IEnumerable<MongoUpdate> updates, IClientSessionHandle session,
        CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        var documentsAffected = 0;

        foreach (var batch in MongoUpdateBatch.CreateBatches(updates))
        {
            stopwatch.Restart();
            var collection = _mongoClient.GetCollection<BsonDocument>(batch.CollectionName);
            _updateLogger.ExecutingBulkWrite(stopwatch.Elapsed, collection.CollectionNamespace, batch.Inserts, batch.Deletes,
                batch.Modified);
            BulkWriteResult<BsonDocument> result;
            try
            {
                result = await collection
                    .BulkWriteAsync(session, batch.Updates.Select(u => u.Model), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoBulkWriteException<BsonDocument> exception)
            {
                if (!TryCreateConcurrencyExceptionForFailedWrite(
                        batch, exception, collection.CollectionNamespace, out var concurrencyException, out var entries))
                {
                    throw;
                }

                var interception = await _updateLogger.OptimisticConcurrencyExceptionAsync(
                    _currentDbContext.Context, entries, WithoutInnerException(concurrencyException, entries), null,
                    cancellationToken).ConfigureAwait(false);
                if (interception.IsSuppressed)
                {
                    // The write failed, so there is nothing to carry on with: report the failure itself.
                    throw;
                }

                throw concurrencyException;
            }

            documentsAffected += await AssertWritesAppliedAsync(batch, result, collection.CollectionNamespace, cancellationToken)
                .ConfigureAwait(false);
            _updateLogger.ExecutedBulkWrite(stopwatch.Elapsed, collection.CollectionNamespace, result.InsertedCount,
                result.DeletedCount, result.ModifiedCount);
        }

        return documentsAffected;
    }

    /// <summary>
    /// Asserts the bulk writes sent to the server matches were actually applied by comparing the counts.
    /// When they do not match either a document was deleted or a concurrency token changed so the filter did not find the document.
    /// </summary>
    /// <param name="batch">The <see cref="MongoUpdateBatch"/> containing details of the updates that were sent to the server.</param>
    /// <param name="result">The <see cref="BulkWriteResult{TDocument}"/> containing the results of the bulk write operation.</param>
    /// <param name="collectionNamespace">The <see class="CollectionNamespace"/> used to build the exception message on failure.</param>
    /// <returns>
    /// Number of documents affected by the bulk write operation. When a conflict was suppressed by an interceptor
    /// (<see cref="ISaveChangesInterceptor.ThrowingConcurrencyException"/>) the operations that did not apply are counted as
    /// well, as the relational providers count a suppressed conflict as applied.
    /// </returns>
    /// <exception cref="DbUpdateConcurrencyException">
    /// Thrown if the counts in <paramref name="batch"/> does not match the counts in <paramref name="result"/>, unless an interceptor
    /// suppresses the exception.
    /// </exception>
    private int AssertWritesApplied(MongoUpdateBatch batch, BulkWriteResult<BsonDocument> result,
        CollectionNamespace collectionNamespace)
    {
        var exception = CreateConcurrencyException(batch, result, collectionNamespace, out var entries, out var unapplied);
        if (exception != null)
        {
            // Like the relational providers: log CoreEventId.OptimisticConcurrencyException (which also runs
            // ISaveChangesInterceptor.ThrowingConcurrencyException) and throw unless an interceptor suppressed it.
            var interception = _updateLogger.OptimisticConcurrencyException(
                _currentDbContext.Context, entries, exception, null);
            if (!interception.IsSuppressed)
            {
                throw exception;
            }
        }

        return CountAffected(result, unapplied);
    }

    /// <summary>
    /// The async counterpart of <see cref="AssertWritesApplied"/>, so asynchronous save-changes interceptors run.
    /// </summary>
    private async Task<int> AssertWritesAppliedAsync(MongoUpdateBatch batch, BulkWriteResult<BsonDocument> result,
        CollectionNamespace collectionNamespace, CancellationToken cancellationToken)
    {
        var exception = CreateConcurrencyException(batch, result, collectionNamespace, out var entries, out var unapplied);
        if (exception != null)
        {
            var interception = await _updateLogger.OptimisticConcurrencyExceptionAsync(
                _currentDbContext.Context, entries, exception, null, cancellationToken).ConfigureAwait(false);
            if (!interception.IsSuppressed)
            {
                throw exception;
            }
        }

        return CountAffected(result, unapplied);
    }

    // 'unapplied' is non-zero only when an interceptor suppressed a conflict: like the relational providers, the suppressed
    // operations are then counted as if they had been applied.
    private static int CountAffected(BulkWriteResult<BsonDocument> result, long unapplied)
        => (int)(result.ModifiedCount + result.DeletedCount + result.InsertedCount + unapplied);

    private static DbUpdateConcurrencyException? CreateConcurrencyException(MongoUpdateBatch batch,
        BulkWriteResult<BsonDocument> result, CollectionNamespace collectionNamespace, out IReadOnlyList<IUpdateEntry> entries,
        out long unapplied)
    {
        entries = [];
        unapplied = 0;

        // Modified count does not include docs that that did not need modification - i.e. no modifications - so we use matched count instead.
        if (result.MatchedCount == batch.Modified && result.DeletedCount == batch.Deletes && result.InsertedCount == batch.Inserts)
        {
            return null;
        }

        var modifiedVariance = batch.Modified - result.MatchedCount;
        var insertedVariance = batch.Inserts - result.InsertedCount;
        var deletedVariance = batch.Deletes - result.DeletedCount;

        var conflictingEntries = GetConflictingEntries(batch.Updates, modifiedVariance, insertedVariance, deletedVariance);

        entries = conflictingEntries;
        unapplied = Math.Max(0, modifiedVariance) + Math.Max(0, insertedVariance) + Math.Max(0, deletedVariance);

        return new DbUpdateConcurrencyException(
            ConcurrencyMessage(collectionNamespace, modifiedVariance, insertedVariance, deletedVariance), conflictingEntries);
    }

    // We can't determine the exact entry that caused the conflict but we can narrow it down to which type(s) of entries
    // within this bulk write batch.
    private static List<IUpdateEntry> GetConflictingEntries(
        IEnumerable<MongoUpdate> updates, long modifiedVariance, long insertedVariance, long deletedVariance)
        => updates.Where(u => u.Model.ModelType == WriteModelType.DeleteOne && deletedVariance != 0
                              || u.Model.ModelType == WriteModelType.InsertOne
                              && insertedVariance != 0
                              || u.Model.ModelType == WriteModelType.UpdateOne
                              && modifiedVariance != 0).Select(u => u.Entry).ToList();

    // This text is logged (CoreEventId.OptimisticConcurrencyException) and returned to callers with no sensitive-data gate:
    // never put entity values, keys or concurrency tokens in it.
    private static string ConcurrencyMessage(
        CollectionNamespace collectionNamespace, long modifiedVariance, long insertedVariance, long deletedVariance)
        => $"Conflicts were detected when performing updates to '{collectionNamespace.FullName}'. " +
           $"Did not perform {modifiedVariance} modifications, {insertedVariance} insertions, and {deletedVariance} deletions.";

    /// <summary>
    /// Writes in a batch are ordered, deletes first, so when one fails (typically a duplicate key) the deletes and updates
    /// before it have already run. If one of those matched nothing, a concurrency check failed, and that is the cause: for
    /// example a delete that did not remove a document still holding the unique value a later insert then collides with.
    /// That is reported as <see cref="DbUpdateConcurrencyException"/>, as it would have been had the write not failed, with
    /// the driver exception as the inner exception.
    /// </summary>
    /// <returns><see langword="false"/> if the failure was not caused by a concurrency check.</returns>
    private static bool TryCreateConcurrencyExceptionForFailedWrite(
        MongoUpdateBatch batch,
        MongoBulkWriteException<BsonDocument> exception,
        CollectionNamespace collectionNamespace,
        [NotNullWhen(true)] out DbUpdateConcurrencyException? concurrencyException,
        out IReadOnlyList<IUpdateEntry> entries)
    {
        concurrencyException = null;
        entries = [];

        var result = exception.Result;
        if (exception.WriteErrors.Count == 0 || result is not { IsAcknowledged: true })
        {
            return false;
        }

        // Only the writes before the first failed one are known to have run.
        var executed = batch.Updates.Take(exception.WriteErrors.Min(e => e.Index)).ToList();
        var modifiedVariance = executed.Count(u => u.Model.ModelType == WriteModelType.UpdateOne) - result.MatchedCount;
        var deletedVariance = executed.Count(u => u.Model.ModelType == WriteModelType.DeleteOne) - result.DeletedCount;
        if (modifiedVariance == 0 && deletedVariance == 0)
        {
            return false;
        }

        entries = GetConflictingEntries(executed, modifiedVariance, 0, deletedVariance);
        concurrencyException = new DbUpdateConcurrencyException(
            ConcurrencyMessage(collectionNamespace, modifiedVariance, 0, deletedVariance), exception, entries);
        return true;
    }

    // The logger and the interceptors get the exception without the driver exception: that one's message holds the values of the
    // duplicate key, and the logged text must not hold entity values (see ConcurrencyMessage). Only the thrown one keeps it.
    private static DbUpdateConcurrencyException WithoutInnerException(
        DbUpdateConcurrencyException exception, IReadOnlyList<IUpdateEntry> entries)
        => new(exception.Message, entries);

    private bool ShouldUseImplicitTransaction(int operationCount)
    {
        if (_transactionManager.CurrentTransaction != null) return false;

        return _currentDbContext.Context.Database.AutoTransactionBehavior switch
        {
            AutoTransactionBehavior.Always => true,
            AutoTransactionBehavior.Never => false,
            AutoTransactionBehavior.WhenNeeded => operationCount > 1,
            _ => throw new InvalidOperationException("Invalid AutoTransactionBehavior value.")
        };
    }

    /// <summary>
    /// We only care about updating root entities as non-root/owned entities must be contained within one.
    /// Convert the list of entries modified by EF Core into a list of modified root entries that were either
    /// directly modified themselves or indirectly modified by one of the entries they own.
    /// </summary>
    /// <param name="entries">The list of modified <see cref="IUpdateEntry"/> as determined by EF Core.</param>
    /// <returns>The actual list of changed root entities as required by MongoDB.</returns>
    private static HashSet<IUpdateEntry> GetAllChangedRootEntries(IList<IUpdateEntry> entries)
    {
        var changedRootEntries = new HashSet<IUpdateEntry>(entries);
        List<IUpdateEntry>? promotedRoots = null;
        foreach (var entry in entries)
        {
            if (!entry.EntityType.IsDocumentRoot())
            {
                var root = GetRootEntry((InternalEntityEntry)entry);
                if (root.EntityState == EntityState.Unchanged)
                {
                    root.EntityState = EntityState.Modified;
                    (promotedRoots ??= []).Add(root);
                }

                changedRootEntries.Remove(entry);
                changedRootEntries.Add(root);
            }
        }

        // Add roots we promoted to Modified into the entries list so that
        // EF Core's AcceptAllChanges will reset them to Unchanged after save.
        if (promotedRoots != null)
        {
            foreach (var root in promotedRoots)
            {
                entries.Add(root);
            }
        }

        return changedRootEntries;
    }

    /// <summary>
    /// Adds any entries that were promoted to Modified during save preparation
    /// (root entity promotion and FK cascade from ordinal key reassignment)
    /// into the entries list so EF Core's AcceptAllChanges will reset them after save.
    /// </summary>
    private static void AddEntriesPromotedDuringSave(IList<IUpdateEntry> entries)
    {
        if (entries.Count == 0) return;

        var existingEntries = new HashSet<IUpdateEntry>(entries);
        var stateManager = ((InternalEntityEntry)entries[0]).StateManager;

        foreach (var entry in stateManager.GetEntriesForState(modified: true))
        {
            if (!existingEntries.Contains(entry) && !entry.EntityType.IsDocumentRoot())
            {
                entries.Add(entry);
            }
        }
    }

    private static IUpdateEntry GetRootEntry(InternalEntityEntry entry)
    {
        while (true)
        {
            var stateManager = entry.StateManager;
            var ownership = entry.EntityType.FindOwnership();
            if (ownership == null) return entry;

            var principal = stateManager.FindPrincipal(entry, ownership);
            if (principal == null)
            {
                throw new InvalidOperationException(
                    $"The entity of type '{entry.EntityType.DisplayName()}' is mapped as a part of the document mapped to '{
                        ownership.PrincipalEntityType.DisplayName()
                    }', but there is no tracked entity of this type with the corresponding key value.");
            }

            if (principal.EntityType.IsDocumentRoot()) return principal;
            entry = principal;
        }
    }
}
