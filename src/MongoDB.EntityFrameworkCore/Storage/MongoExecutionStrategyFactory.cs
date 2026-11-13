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
using Microsoft.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.Storage;

/// <summary>
/// Creates the <see cref="IExecutionStrategy"/>: a <see cref="MongoRetryingExecutionStrategy"/> when
/// <see cref="MongoDB.EntityFrameworkCore.Infrastructure.MongoDbContextOptionsBuilder.EnableRetryOnFailure"/> was used, otherwise EF Core's non-retrying strategy.
/// </summary>
internal sealed class MongoExecutionStrategyFactory(ExecutionStrategyDependencies dependencies)
    : IExecutionStrategyFactory
{
    /// <inheritdoc />
    public IExecutionStrategy Create()
    {
        var extension = dependencies.Options.FindExtension<MongoOptionsExtension>();
        return extension?.MaxRetryCount is { } maxRetryCount
            ? new MongoRetryingExecutionStrategy(dependencies, maxRetryCount, extension.MaxRetryDelay)
            : new NonRetryingExecutionStrategy(dependencies);
    }
}
