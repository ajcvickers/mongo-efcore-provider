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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.Query.Factories;

/// <summary>
/// A factory for creating <see cref="MongoQueryContext" /> instances.
/// </summary>
public class MongoQueryContextFactory : IQueryContextFactory
{
    /// <summary>
    /// Create a <see cref="MongoQueryContextFactory"/>.
    /// </summary>
    /// <param name="dependencies">The <see cref="QueryContextDependencies"/> passed to each created <see cref="MongoQueryContext" /> instance.</param>
    /// <param name="mongoClient">The <see cref="IMongoClientWrapper"/> passed to each created <see cref="MongoQueryContext" /> instance.</param>
    /// <param name="updateLogger">The <see cref="DbLoggerCategory.Update"/> logger passed to each created <see cref="MongoQueryContext" /> instance.</param>
    public MongoQueryContextFactory(
        QueryContextDependencies dependencies,
        IMongoClientWrapper mongoClient,
        IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger)
    {
        Dependencies = dependencies;
        Client = mongoClient;
        UpdateLogger = updateLogger;
    }

    /// <summary>
    /// Create a <see cref="MongoQueryContextFactory"/> without supplying the <see cref="DbLoggerCategory.Update"/> logger (the constructor
    /// shipped before bulk operations needed one). Each created <see cref="MongoQueryContext"/> then resolves the logger itself.
    /// </summary>
    /// <param name="dependencies">The <see cref="QueryContextDependencies"/> passed to each created <see cref="MongoQueryContext" /> instance.</param>
    /// <param name="mongoClient">The <see cref="IMongoClientWrapper"/> passed to each created <see cref="MongoQueryContext" /> instance.</param>
    public MongoQueryContextFactory(
        QueryContextDependencies dependencies,
        IMongoClientWrapper mongoClient)
    {
        Dependencies = dependencies;
        Client = mongoClient;
    }

    /// <summary>
    /// The <see cref="QueryContextDependencies"/> passed to each created <see cref="MongoQueryContext"/>.
    /// </summary>
    protected virtual QueryContextDependencies Dependencies { get; }

    /// <summary>
    /// Create a new <see cref="MongoQueryContext"/> with necessary dependencies.
    /// </summary>
    /// <returns>The newly created <see cref="MongoQueryContext"/>.</returns>
    public virtual QueryContext Create()
        => UpdateLogger == null
            ? new MongoQueryContext(Dependencies, Client)
            : new MongoQueryContext(Dependencies, Client, UpdateLogger);

    /// <summary>
    /// The <see cref="IMongoClientWrapper"/> passed to each created <see cref="MongoQueryContext"/>.
    /// </summary>
    public IMongoClientWrapper Client { get; }

    /// <summary>
    /// The <see cref="DbLoggerCategory.Update"/> logger passed to each created <see cref="MongoQueryContext"/>, or
    /// <see langword="null"/> when the factory was built without one (each context then resolves it).
    /// </summary>
    protected virtual IDiagnosticsLogger<DbLoggerCategory.Update>? UpdateLogger { get; }
}
