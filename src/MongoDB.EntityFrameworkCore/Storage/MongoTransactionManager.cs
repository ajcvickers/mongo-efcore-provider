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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;

namespace MongoDB.EntityFrameworkCore.Storage;

/// <summary>
/// Explicit transaction manager for MongoDB EF Core provider.
/// </summary>
public class MongoTransactionManager : IMongoTransactionManager, IDisposable, IAsyncDisposable
{
    private readonly IMongoClientWrapper _client;
    private readonly DbContext _context;
    private readonly IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> _logger;
    private readonly TransactionOptions _defaultTransactionOptions = new();

    /// <summary>
    /// Create a <see cref="MongoTransactionManager"/>.
    /// </summary>
    public MongoTransactionManager(
        IMongoClientWrapper client,
        ICurrentDbContext currentContext,
        IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger)
    {
        _client = client;
        _context = currentContext.Context;
        _logger = logger;
    }

    /// <inheritdoc />
    public void ResetState()
    {
        // The transaction finishes disposing itself even when its rollback throws (see MongoTransaction.Dispose), so the manager
        // lets go of it whatever happens and the exception still reaches the caller.
        try
        {
            CurrentTransaction?.Dispose();
        }
        finally
        {
            CurrentTransaction = null;
        }
    }

    /// <inheritdoc />
    public async Task ResetStateAsync(CancellationToken cancellationToken = new())
    {
        try
        {
            if (CurrentTransaction != null)
            {
                await CurrentTransaction.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            CurrentTransaction = null;
        }
    }

    /// <summary>
    /// Ends the use of this transaction manager when its context is disposed. As with relational providers, a transaction the context
    /// owns and has not committed or rolled back is rolled back (and its session disposed), so the server does not keep it open; a
    /// transaction the context only joined (see <see cref="UseTransaction(IClientSessionHandle)"/>) is detached and left to its owner.
    /// </summary>
    /// <remarks>
    /// This is virtual so that a derived manager can add to it. EF disposes the manager through <see cref="IDisposable"/>, which
    /// reaches an override but not a method that merely hides this one; call the base implementation from an override.
    /// </remarks>
    public virtual void Dispose()
        => ResetState();

    /// <summary>
    /// Asynchronous version of <see cref="Dispose"/>.
    /// </summary>
    public virtual async ValueTask DisposeAsync()
        => await ResetStateAsync().ConfigureAwait(false);

    /// <inheritdoc />
    public IDbContextTransaction BeginTransaction()
    {
        EnsureNoTransactions();
        var session = _client.StartSession();
        return CurrentTransaction = MongoTransaction.Start(session, _context, async: false, _defaultTransactionOptions, this, _logger);
    }

    /// <inheritdoc />
    public IDbContextTransaction BeginTransaction(TransactionOptions transactionOptions)
    {
        EnsureNoTransactions();
        var session = _client.StartSession();
        return CurrentTransaction = MongoTransaction.Start(session, _context, async: false, transactionOptions, this, _logger);
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = new())
    {
        EnsureNoTransactions();
        var session = await _client.StartSessionAsync(cancellationToken).ConfigureAwait(false);
        return CurrentTransaction = MongoTransaction.Start(session, _context, async: true, _defaultTransactionOptions, this, _logger);
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(TransactionOptions transactionOptions, CancellationToken cancellationToken = new())
    {
        EnsureNoTransactions();
        var session = await _client.StartSessionAsync(cancellationToken).ConfigureAwait(false);
        return CurrentTransaction = MongoTransaction.Start(session, _context, async: true, transactionOptions, this, _logger);
    }

    /// <inheritdoc />
    public void CommitTransaction()
    {
        if (GetRequiredCurrentTransaction() is { } acquiredTransaction)
        {
            acquiredTransaction.Commit();
            acquiredTransaction.Dispose();
            CurrentTransaction = null;
        }
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = new())
    {
        if (GetRequiredCurrentTransaction() is { } acquiredTransaction)
        {
            await acquiredTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await acquiredTransaction.DisposeAsync().ConfigureAwait(false);
            CurrentTransaction = null;
        }
    }

    /// <inheritdoc />
    public void RollbackTransaction()
    {
        if (GetRequiredCurrentTransaction() is { } acquiredTransaction)
        {
            acquiredTransaction.Rollback();
            acquiredTransaction.Dispose();
            CurrentTransaction = null;
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = new())
    {
        if (GetRequiredCurrentTransaction() is { } acquiredTransaction)
        {
            await acquiredTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            await acquiredTransaction.DisposeAsync().ConfigureAwait(false);
            CurrentTransaction = null;
        }
    }

    /// <inheritdoc />
    public IDbContextTransaction UseTransaction(IClientSessionHandle session)
    {
        ArgumentNullException.ThrowIfNull(session);

        EnsureNoTransactions();
        if (!session.IsInTransaction)
        {
            throw new InvalidOperationException(
                "The supplied session does not have an active transaction. Start a transaction on the session with "
                + "'StartTransaction' before enlisting a context in it.");
        }

        return CurrentTransaction = MongoTransaction.Enlist(
            session, _context, "the code that supplied the session", this, _logger);
    }

    /// <inheritdoc />
    public IDbContextTransaction UseTransaction(IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        EnsureNoTransactions();
        if (transaction is not MongoTransaction owner)
        {
            throw new InvalidOperationException(
                $"Only a MongoDB transaction can be shared with a MongoDB context, but a transaction of type '{transaction.GetType().Name}' was supplied.");
        }

        return CurrentTransaction = MongoTransaction.Enlist(
            owner.ActiveSession, _context, owner.OwnerDescription, this, _logger);
    }

    private IDbContextTransaction GetRequiredCurrentTransaction()
        => CurrentTransaction
           ?? throw new InvalidOperationException("No transaction is in progress. Call BeginTransaction to start a transaction.");

    /// <inheritdoc />
    public IDbContextTransaction? CurrentTransaction { get; internal set; }

    private void EnsureNoTransactions()
    {
        if (CurrentTransaction != null)
        {
            throw new InvalidOperationException(
                "The connection is already in a transaction and cannot participate in another transaction.");
        }

        if (System.Transactions.Transaction.Current != null)
        {
            throw new InvalidOperationException(
                "An ambient transaction has been detected. The ambient transaction needs to be completed before starting a new transaction on this connection.");
        }
    }
}
