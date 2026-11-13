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
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;

namespace MongoDB.EntityFrameworkCore.Storage;

/// <summary>
/// An <see cref="ExecutionStrategy"/> that retries a unit of work when MongoDB reports a transient transaction failure.
/// </summary>
/// <remarks>
/// MongoDB labels an error <c>TransientTransactionError</c> when the transaction it occurred in was aborted and the whole transaction
/// can safely be run again from the start (for example after a write conflict with a concurrent transaction). Only an operation that
/// ran inside a transaction carries the label, so work that did not (such as <c>SaveChanges</c> under
/// <c>AutoTransactionBehavior.Never</c>, or with a single root entity under <c>WhenNeeded</c>) is never retried: it could leave
/// earlier writes applied and a retry could apply them twice.
/// </remarks>
internal sealed class MongoRetryingExecutionStrategy(
    ExecutionStrategyDependencies dependencies,
    int maxRetryCount,
    TimeSpan maxRetryDelay)
    : ExecutionStrategy(dependencies, maxRetryCount, maxRetryDelay)
{
    internal const string TransientTransactionErrorLabel = "TransientTransactionError";

    /// <inheritdoc />
    protected override bool ShouldRetryOn(Exception exception)
        => IsTransientTransactionError(exception);

    // The label is on the MongoException, which EF may have wrapped (for example in a DbUpdateException).
    internal static bool IsTransientTransactionError(Exception exception)
        => CallOnWrappedException(exception, static e => e is MongoException mongoException
                                                          && mongoException.HasErrorLabel(TransientTransactionErrorLabel));
}
