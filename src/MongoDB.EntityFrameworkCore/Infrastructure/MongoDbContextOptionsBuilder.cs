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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Allows MongoDB-specific configuration to be performed on <see cref="DbContextOptions"/>.
/// </summary>
public class MongoDbContextOptionsBuilder : IMongoDbContextOptionsBuilderInfrastructure
{
    /// <summary>
    /// Creates a <see cref="MongoDbContextOptionsBuilder" /> with the required options builder.
    /// </summary>
    /// <param name="optionsBuilder">The <see cref="DbContextOptionsBuilder"/> to start from.</param>
    public MongoDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
    {
        OptionsBuilder = optionsBuilder;
    }

    /// <summary>
    /// Clones the configuration in this builder.
    /// </summary>
    /// <returns>The cloned configuration.</returns>
    protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

    DbContextOptionsBuilder IMongoDbContextOptionsBuilderInfrastructure.OptionsBuilder => OptionsBuilder;

    /// <summary>
    /// Specifies how the provider translates LINQ queries to MongoDB.
    /// </summary>
    /// <param name="queryMode">The <see cref="MongoQueryMode"/> to use.</param>
    /// <returns>This builder, so that further configuration can be chained.</returns>
    public virtual MongoDbContextOptionsBuilder UseQueryMode(MongoQueryMode queryMode)
    {
        var extension = (OptionsBuilder.Options.FindExtension<MongoOptionsExtension>()
                         ?? new MongoOptionsExtension())
            .WithQueryMode(queryMode);
        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension);
        return this;
    }

    /// <summary>
    /// Enables retrying a unit of work, run through <c>Database.CreateExecutionStrategy()</c> (which <c>SaveChanges</c> does itself),
    /// when MongoDB reports a <c>TransientTransactionError</c>, for example a write conflict between two concurrent transactions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only work that ran inside a transaction is retried, and always from the start. Duplicate-key and other deterministic write
    /// errors, <see cref="DbUpdateConcurrencyException"/> and cancellation are not retried.
    /// </para>
    /// <para>
    /// As with relational providers, a transaction started outside the execution strategy (for example with
    /// <c>Database.BeginTransaction()</c> or <c>Database.UseTransaction(...)</c>) is rejected when a retrying strategy is used: run the
    /// whole unit of work, including starting the transaction, inside <c>strategy.Execute</c>. A retry starts a new session and
    /// transaction, so a context that had joined the failed transaction must join the new one inside the delegate.
    /// </para>
    /// <para>
    /// Retrying the commit of a transaction whose outcome is unknown (<c>UnknownTransactionCommitResult</c>) does not depend on this
    /// option.
    /// </para>
    /// </remarks>
    /// <param name="maxRetryCount">The maximum number of retry attempts. The default is 6. 0 disables retrying.</param>
    /// <param name="maxRetryDelay">The maximum delay between retries. The default is 30 seconds.</param>
    /// <returns>This builder, so that further configuration can be chained.</returns>
    public virtual MongoDbContextOptionsBuilder EnableRetryOnFailure(int maxRetryCount = 6, TimeSpan? maxRetryDelay = null)
    {
        var extension = (OptionsBuilder.Options.FindExtension<MongoOptionsExtension>()
                         ?? new MongoOptionsExtension())
            .WithRetryOnFailure(maxRetryCount, maxRetryDelay ?? MongoOptionsExtension.DefaultMaxRetryDelay);
        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension);
        return this;
    }
}
