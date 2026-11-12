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
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;

namespace MongoDB.EntityFrameworkCore.Storage;

/// <summary>
/// Interface that adds MongoDB-specific transaction management support such as
/// exposing <see cref="TransactionOptions"/> when starting new transactions.
/// </summary>
/// <remarks>This interface should be considered internal and may change between minor versions.</remarks>
public interface IMongoTransactionManager : IDbContextTransactionManager
{
    /// <summary>
    /// Begin a new transaction with the supplied <paramref name="transactionOptions"/>.
    /// </summary>
    /// <param name="transactionOptions">The <see cref="TransactionOptions"/> that control the behavior of this transaction.</param>
    /// <returns>The <see cref="MongoTransaction"/> that has begun.</returns>
    IDbContextTransaction BeginTransaction(TransactionOptions transactionOptions);

    /// <summary>
    /// Begin a new async transaction with the supplied <paramref name="transactionOptions"/>.
    /// </summary>
    /// <param name="transactionOptions">The <see cref="TransactionOptions"/> that control the behavior of this transaction.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the newly begun <see cref="IDbContextTransaction"/>.</returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    Task<IDbContextTransaction> BeginTransactionAsync(TransactionOptions transactionOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes the context participate in the transaction already in progress on the supplied <paramref name="session"/>.
    /// The context's writes and queries use that transaction, but the caller that started it remains responsible for committing or
    /// aborting it and for disposing the session. Committing, rolling back or disposing the returned transaction never ends it:
    /// commit and rollback are rejected, and disposal only detaches the context from the transaction.
    /// </summary>
    /// <param name="session">A session on which a transaction has been started.</param>
    /// <returns>The <see cref="IDbContextTransaction"/> through which the context participates in the transaction.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The context is already in a transaction, an ambient transaction exists,
    /// or the session has no active transaction.</exception>
    IDbContextTransaction UseTransaction(IClientSessionHandle session);

    /// <summary>
    /// Makes the context participate in a transaction started by another context, which remains responsible for committing or aborting
    /// it. Committing, rolling back or disposing the returned transaction never ends it: commit and rollback are rejected, and
    /// disposal only detaches the context from the transaction.
    /// </summary>
    /// <param name="transaction">A <see cref="MongoTransaction"/>, such as the <c>CurrentTransaction</c> of another context.</param>
    /// <returns>The <see cref="IDbContextTransaction"/> through which the context participates in the transaction.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transaction"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The context is already in a transaction, an ambient transaction exists,
    /// <paramref name="transaction"/> is not a <see cref="MongoTransaction"/>, or it is no longer active.</exception>
    IDbContextTransaction UseTransaction(IDbContextTransaction transaction);
}
