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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;

namespace MongoDB.EntityFrameworkCore.Storage;

/// <summary>
/// Provides an EF Core-friendly wrapper around a MongoDB transaction.
/// </summary>
/// <param name="session">A <see cref="IClientSession"/> for handling the transaction in MongoDB.</param>
/// <param name="context">The <see cref="DbContext"/> this transaction is being used with.</param>
/// <param name="transactionId">The unique identifier from EF Core for this transaction.</param>
/// <param name="transactionManager">The <see cref="IMongoTransactionManager"/> this transaction is started from or <see langword="null"/> if it is implicit.</param>
/// <param name="transactionLogger">A <see cref="IDiagnosticsLogger"/> for logging the <see cref="DbLoggerCategory.Database.Transaction"/> messages.</param>
public sealed class MongoTransaction(
    IClientSessionHandle session,
    DbContext context,
    Guid transactionId,
    IMongoTransactionManager? transactionManager,
    IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> transactionLogger)
    : IDbContextTransaction
{
    private enum TransactionState
    {
        Active,
        Committed,
        RolledBack,
        Failed,
        Disposed
    }

    private TransactionState _transactionState = TransactionState.Active;

    // Describes who owns the transaction when this MongoTransaction merely participates in it (see Enlist).
    private string? _ownerDescription;

    /// <summary>
    /// <see langword="true"/> if this context started the transaction and therefore controls how it ends; <see langword="false"/> if
    /// this context only participates in a transaction owned by someone else (see <see cref="Enlist"/>).
    /// </summary>
    internal bool IsOwner { get; private init; } = true;

    /// <summary>
    /// Describes the owner of this transaction for use in messages.
    /// </summary>
    internal string OwnerDescription
        => IsOwner ? $"{Context.GetType().Name} (transaction {TransactionId})" : _ownerDescription!;

    internal static MongoTransaction Start(
        IClientSessionHandle session,
        DbContext context,
        bool async,
        TransactionOptions transactionOptions,
        IMongoTransactionManager? transactionManager,
        IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> transactionLogger)
    {
        var startTime = DateTimeOffset.UtcNow;
        var transactionId = Guid.NewGuid();
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        transactionLogger.TransactionStarting(session, context, transactionOptions, transactionId, true, startTime);
        try
        {
            session.StartTransaction(transactionOptions);
        }
        catch (NotSupportedException ex)
        {
            if (ex.Message == "Standalone servers do not support transactions.")
            {
                throw new NotSupportedException(string.Join(" ", TransactionByDefault,
                        "Your current MongoDB server configuration does not support transactions and you should consider switching to a replica set or load balanced configuration.",
                        DisableTransactions),
                    ex);
            }

            throw new NotSupportedException(string.Join(" ", TransactionByDefault,
                    "Your current MongoDB server version does not support transactions and you should consider upgrading to a newer version.",
                    DisableTransactions),
                ex);
        }

        var transaction = new MongoTransaction(session, context, transactionId, transactionManager, transactionLogger);
        transactionLogger.TransactionStarted(transaction, async, startTime, stopwatch.Elapsed);
        return transaction;
    }

    /// <summary>
    /// Creates a <see cref="MongoTransaction"/> that participates in a transaction already in progress on <paramref name="session"/>,
    /// which is owned (committed, aborted and disposed) by someone else. Commit and rollback are rejected, and disposing only detaches
    /// the transaction from the <paramref name="transactionManager"/>.
    /// </summary>
    internal static MongoTransaction Enlist(
        IClientSessionHandle session,
        DbContext context,
        string ownerDescription,
        IMongoTransactionManager? transactionManager,
        IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> transactionLogger)
    {
        var transaction = new MongoTransaction(session, context, Guid.NewGuid(), transactionManager, transactionLogger)
        {
            IsOwner = false,
            _ownerDescription = ownerDescription
        };

        transactionLogger.TransactionEnlisted(transaction);
        return transaction;
    }

    private const string TransactionByDefault =
        "The MongoDB EF Core Provider now uses transactions to ensure all updates in a SaveChanges operation are applied together or not at all.";

    private const string DisableTransactions =
        "If you are sure you do not need save consistency or optimistic concurrency you can disable transactions by setting 'Database.AutoTransactionBehavior = AutoTransactionBehavior.Never' on your DbContext.";

    /// <summary>
    /// The underlying <see cref="IClientSession"/> this transaction is using which is required
    /// to issue commands against. Throws if the transaction this <see cref="MongoTransaction"/> was started for or enlisted in
    /// has ended (committed or aborted), including when the session has since started a different transaction, because a command
    /// issued then would silently run outside the transaction this context believes it is in.
    /// </summary>
    /// <exception cref="InvalidOperationException">The session is no longer in the transaction.</exception>
    internal IClientSessionHandle ActiveSession
        => SessionIsInOurTransaction
            ? session
            : throw new InvalidOperationException(
                $"MongoTransaction {TransactionId} is no longer active: the transaction on its session has already been committed or aborted. "
                + "Commands can not be issued on this transaction because they would run outside of any transaction.");

    // The number the session gave the transaction this MongoTransaction was created for, which identifies it uniquely on the
    // session. Null if the session was not in a transaction when this was created, in which case there is no transaction for
    // this MongoTransaction to be active in and it never is.
    private readonly long? _sessionTransactionNumber = CurrentTransactionNumber(session);

    // The owner of a session shared with a non-owning context disposes it when it ends the transaction, and asking a disposed
    // session whether it is in a transaction throws ObjectDisposedException. A disposed session is not in a transaction.
    // A session whose owner committed or aborted and then started another transaction is in a transaction, but not ours.
    private bool SessionIsInOurTransaction
        => _sessionTransactionNumber is { } ours
           && CurrentTransactionNumber(session) is { } current
           && current == ours;

    // The number of the transaction in progress on the session, or null if there is none (or the session is disposed).
    // CurrentTransaction is only read while the session is in a transaction.
    private static long? CurrentTransactionNumber(IClientSessionHandle session)
    {
        try
        {
            return session.IsInTransaction
                ? session.WrappedCoreSession.CurrentTransaction?.TransactionNumber
                : null;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public Guid TransactionId { get; } = transactionId;

    /// <summary>
    /// The <see cref="DbContext"/> this transaction is being used with.
    /// </summary>
    public DbContext Context { get; } = context;

    /// <inheritdoc />
    public void Commit()
    {
        AssertCorrectState("Commit", TransactionState.Active);
        AssertIsOwner("Commit");
        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        transactionLogger.TransactionCommitting(this, false, startTime);
        try
        {
            session.CommitTransaction();
        }
        catch (Exception ex)
        {
            _transactionState = TransactionState.Failed;
            transactionLogger.TransactionError(this, "Commit", ex, false, startTime, stopwatch.Elapsed);
            throw;
        }

        _transactionState = TransactionState.Committed;
        transactionLogger.TransactionCommitted(this, false, startTime, stopwatch.Elapsed);
        transactionManager?.ResetState();
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = new())
    {
        AssertCorrectState("Commit", TransactionState.Active);
        AssertIsOwner("Commit");
        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        transactionLogger.TransactionCommitting(this, true, startTime);
        try
        {
            await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _transactionState = TransactionState.Failed;
            transactionLogger.TransactionError(this, "Commit", ex, true, startTime, stopwatch.Elapsed);
            throw;
        }

        _transactionState = TransactionState.Committed;
        transactionLogger.TransactionCommitted(this, true, startTime, stopwatch.Elapsed);
        if (transactionManager is not null)
        {
            await transactionManager.ResetStateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Rollback()
    {
        AssertCorrectState("Rollback", TransactionState.Active);
        AssertIsOwner("Rollback");
        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        transactionLogger.TransactionRollingBack(this, false, startTime);
        try
        {
            session.AbortTransaction();
        }
        catch (Exception ex)
        {
            _transactionState = TransactionState.Failed;
            transactionLogger.TransactionError(this, "Rollback", ex, false, startTime, stopwatch.Elapsed);
            throw;
        }

        _transactionState = TransactionState.RolledBack;
        transactionLogger.TransactionRolledBack(this, false, startTime, stopwatch.Elapsed);
        transactionManager?.ResetState();
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = new())
    {
        AssertCorrectState("Rollback", TransactionState.Active);
        AssertIsOwner("Rollback");
        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        transactionLogger.TransactionRollingBack(this, true, startTime);
        try
        {
            await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _transactionState = TransactionState.Failed;
            transactionLogger.TransactionError(this, "Rollback", ex, true, startTime, stopwatch.Elapsed);
            throw;
        }

        _transactionState = TransactionState.RolledBack;
        transactionLogger.TransactionRolledBack(this, true, startTime, stopwatch.Elapsed);
        if (transactionManager is not null)
        {
            await transactionManager.ResetStateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Obtain the <see cref="TransactionOptions"/> for this transaction.
    /// </summary>
    public TransactionOptions TransactionOptions
        => session.WrappedCoreSession.CurrentTransaction.TransactionOptions;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!IsOwner)
        {
            if (_transactionState == TransactionState.Disposed)
            {
                return;
            }

            // Only detach: the transaction and its session belong to the owner. The state is set first so that the manager's call
            // back into Dispose returns immediately.
            _transactionState = TransactionState.Disposed;
            transactionManager?.ResetState();
            return;
        }

        var endedBehindOurBack = false;
        switch (_transactionState)
        {
            case TransactionState.Disposed:
                return;

            case TransactionState.Active:
                if (SessionTransactionHasEnded)
                {
                    endedBehindOurBack = true;
                }
                else
                {
                    Rollback();
                }
                break;

            case TransactionState.Committed:
            case TransactionState.RolledBack:
            case TransactionState.Failed:
                break;
        }

        _transactionState = TransactionState.Disposed;
        session.Dispose();

        if (endedBehindOurBack)
        {
            // Rollback is what normally detaches this transaction from the manager; it was skipped, so do it here.
            // The state is already Disposed, so the manager's call back into Dispose returns immediately.
            transactionManager?.ResetState();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!IsOwner)
        {
            if (_transactionState == TransactionState.Disposed)
            {
                return;
            }

            // See Dispose.
            _transactionState = TransactionState.Disposed;
            if (transactionManager is not null)
            {
                await transactionManager.ResetStateAsync().ConfigureAwait(false);
            }

            return;
        }

        var endedBehindOurBack = false;
        switch (_transactionState)
        {
            case TransactionState.Disposed:
                return;

            case TransactionState.Active:
                if (SessionTransactionHasEnded)
                {
                    endedBehindOurBack = true;
                }
                else
                {
                    await RollbackAsync().ConfigureAwait(false);
                }
                break;

            case TransactionState.Committed:
            case TransactionState.RolledBack:
            case TransactionState.Failed:
                break;
        }

        _transactionState = TransactionState.Disposed;
        session.Dispose();

        if (endedBehindOurBack && transactionManager is not null)
        {
            // See Dispose.
            await transactionManager.ResetStateAsync().ConfigureAwait(false);
        }
    }

    // The session's transaction may have been committed or aborted by something else holding the session (which may even have
    // started another transaction on it). There is then nothing of ours to roll back (attempting it would make Dispose throw or
    // abort someone else's transaction), but the manager must still be told to let go of us.
    private bool SessionTransactionHasEnded => !SessionIsInOurTransaction;

    private void AssertIsOwner(string action)
    {
        if (!IsOwner)
            throw new InvalidOperationException(
                $"Can not {action} MongoTransaction {TransactionId} because it was not started by this context. "
                + $"It is owned by {_ownerDescription}, which must {(action == "Commit" ? "commit" : "roll back")} it; this context only participates in it.");
    }

    private void AssertCorrectState(string action, TransactionState validState)
    {
        if (_transactionState != validState)
            throw new
                InvalidOperationException($"Can not {action} MongoTransaction {TransactionId} because it is {_transactionState}.");
    }
}
