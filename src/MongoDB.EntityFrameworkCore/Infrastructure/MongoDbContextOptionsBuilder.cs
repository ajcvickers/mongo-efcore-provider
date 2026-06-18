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
    /// Configures whether the provider translates queries to native MQL with a streaming materializer
    /// (the default) or uses the MongoDB driver's LINQ provider with BsonDocument materialization.
    /// </summary>
    /// <param name="useNativeQuery"><see langword="true"/> (default) to use native MQL + streaming materialization; <see langword="false"/> to use the driver's LINQ provider.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public virtual MongoDbContextOptionsBuilder UseNativeQuery(bool useNativeQuery = true)
    {
        var extension = (OptionsBuilder.Options.FindExtension<MongoOptionsExtension>()
                         ?? new MongoOptionsExtension())
            .WithUseNativeQuery(useNativeQuery);

        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension);
        return this;
    }
}
