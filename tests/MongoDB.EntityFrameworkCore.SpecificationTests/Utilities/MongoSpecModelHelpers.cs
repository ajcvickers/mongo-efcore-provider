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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

/// <summary>
/// Helpers for adapting EF Core's relational-flavored specification test models to MongoDB. Fixtures call these
/// from <c>OnModelCreating</c> / <c>ConfigureConventions</c> so the upstream models pass provider validation and the
/// tests exercise real behavior. Each workaround is tied to a ticket in docs/failing-spec-tests.md and should be
/// removed once the provider no longer needs it.
/// </summary>
public static class MongoSpecModelHelpers
{
    /// <summary>
    /// Maps the single-property primary key of every root (non-owned, non-derived) entity type to the <c>_id</c>
    /// element, which the provider requires. Upstream test models name their keys arbitrarily (<c>BuildingId</c>, ...).
    /// </summary>
    /// <param name="modelBuilder">The model builder; call after the upstream model has been built.</param>
    /// <param name="generateIntegerKeys">
    /// When <see langword="true"/>, also opts non-converted, non-foreign-key <c>int</c>/<c>long</c>/<c>short</c> keys into a
    /// client-side sequential generator, because the provider generates no integer keys while the relational-style upstream
    /// models expect identity columns. Pass <see langword="false"/> to leave key generation as the provider has it, as
    /// when the failure of key generation is itself what a test class asserts.
    /// </param>
    public static void MapSingleKeysToId(ModelBuilder modelBuilder, bool generateIntegerKeys)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(e => !e.IsOwned() && e.BaseType == null).ToList())
        {
            var key = entityType.FindPrimaryKey();
            if (key is not { Properties.Count: 1 })
            {
                continue;
            }

            var property = key.Properties[0];
            var propertyBuilder = modelBuilder.Entity(entityType.Name).Property(property.Name);
            propertyBuilder.HasElementName("_id");

            if (!generateIntegerKeys || property.GetValueConverter() != null || property.IsForeignKey())
            {
                continue;
            }

            if (property.ClrType == typeof(int))
            {
                propertyBuilder.HasValueGenerator(typeof(TestIntegerValueGenerator<int>));
            }
            else if (property.ClrType == typeof(long))
            {
                propertyBuilder.HasValueGenerator(typeof(TestIntegerValueGenerator<long>));
            }
            else if (property.ClrType == typeof(short))
            {
                propertyBuilder.HasValueGenerator(typeof(TestIntegerValueGenerator<short>));
            }
        }
    }

    /// <summary>
    /// Makes every unique index that includes a nullable property non-unique. Relational providers create unique indexes
    /// on optional one-to-one foreign keys with a "not null" filter; a MongoDB unique index treats all null/missing
    /// values as duplicates, so <c>createIndexes</c> would otherwise fail.
    /// </summary>
    public static void RelaxNullableUniqueIndexes(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var index in entityType.GetDeclaredIndexes())
            {
                if (index.IsUnique && index.Properties.Any(p => p is IReadOnlyProperty { IsNullable: true }))
                {
                    ((IMutableIndex)index).IsUnique = false;
                }
            }
        }
    }

    /// <summary>
    /// Makes unique indexes that cover a foreign key non-unique: MongoDB unique indexes treat null as a value, so two
    /// documents with a null foreign key collide (EF-X207). Call after the model is built.
    /// </summary>
    public static ModelBuilder RelaxUniqueForeignKeyIndexes(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var index in entityType.GetDeclaredIndexes())
            {
                if (index.IsUnique && entityType.GetForeignKeys().Any(fk => fk.Properties.SequenceEqual(index.Properties)))
                {
                    index.IsUnique = false;
                }
            }
        }

        return modelBuilder;
    }

    /// <summary>
    /// For models built outside the normal pipeline (e.g. via <c>UseModel</c>): maps the single-property key of each
    /// document-root type to "_id" (EF-X201) and optionally drops concurrency tokens on owned types (EF-X214).
    /// </summary>
    public static ModelBuilder FixExternalModel(this ModelBuilder modelBuilder, bool dropOwnedConcurrencyTokens = false)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!entityType.IsDocumentRoot())
            {
                if (dropOwnedConcurrencyTokens)
                {
                    foreach (var token in entityType.GetProperties().Where(p => p.IsConcurrencyToken).ToList())
                    {
                        token.IsConcurrencyToken = false;
                    }
                }

                continue;
            }

            if (entityType.FindPrimaryKey() is { Properties.Count: 1 } key && key.Properties[0] is { } property
                && property.DeclaringType == entityType && property.GetElementName() != "_id")
            {
                property.SetElementName("_id");
            }
        }

        return modelBuilder;
    }

    /// <summary>
    /// Adds a model-finalizing convention that (a) maps the single-property primary key of every non-owned
    /// entity type to element "_id" (EF-X201: upstream test models that configure keys explicitly or by
    /// constructor/field binding otherwise fail model validation) and optionally (b) gives int keys a client-side
    /// generator (EF-X205: the provider generates no values for int keys, which the relational-style upstream
    /// test models expect to be store-generated). Unlike <see cref="MapSingleKeysToId"/> this runs as a convention, so
    /// it also covers models created by upstream code paths that never reach <c>OnModelCreating</c>, and it can
    /// opt owned-collection int key parts into generation and drop concurrency tokens on owned types (EF-X214).
    /// </summary>
    public static ModelConfigurationBuilder MapSinglePropertyKeysToElementId(
        this ModelConfigurationBuilder configurationBuilder, bool generateIntKeys = false, bool dropOwnedConcurrencyTokens = false)
    {
        configurationBuilder.Conventions.Add(_ => new MapKeysToElementIdConvention(generateIntKeys, dropOwnedConcurrencyTokens));
        return configurationBuilder;
    }

    private sealed class MapKeysToElementIdConvention(bool generateIntKeys, bool dropOwnedConcurrencyTokens) : IModelFinalizingConvention
    {
        public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
            {
                if (!entityType.IsDocumentRoot())
                {
                    if (dropOwnedConcurrencyTokens)
                    {
                        // Concurrency tokens on owned types are rejected by model validation (EF-X214).
                        foreach (var token in entityType.GetProperties().Where(p => p.IsConcurrencyToken).ToList())
                        {
                            ((IMutableProperty)token).IsConcurrencyToken = false;
                        }
                    }

                    if (generateIntKeys && entityType.FindPrimaryKey() is { } ownedKey)
                    {
                        // Owned collection items with a declared int key part (e.g. Id) expect it to be generated.
                        foreach (var keyPart in ownedKey.Properties.Where(
                                     p => p.ClrType == typeof(int) && !p.IsForeignKey()
                                          && p.GetValueGeneratedConfigurationSource() != ConfigurationSource.Explicit
                                          && p.GetValueGeneratorFactoryConfigurationSource() == null))
                        {
                            keyPart.SetValueGenerated(ValueGenerated.OnAdd);
                            keyPart.SetValueGeneratorFactory((_, _) => new TestIntegerValueGenerator<int>());
                        }
                    }

                    continue;
                }

                var key = entityType.FindPrimaryKey();
                if (key is not { Properties.Count: 1 } || key.Properties[0] is not { } property
                    || property.DeclaringType != entityType)
                {
                    continue;
                }

                if (property.GetElementName() != "_id")
                {
                    property.SetElementName("_id");
                }

                if (generateIntKeys && property.ClrType == typeof(int)
                    && property.GetValueGeneratedConfigurationSource() != ConfigurationSource.Explicit
                    && property.GetValueGeneratorFactoryConfigurationSource() == null)
                {
                    property.SetValueGenerated(ValueGenerated.OnAdd);
                    property.SetValueGeneratorFactory((_, _) => new TestIntegerValueGenerator<int>());
                }
            }
        }
    }
}

/// <summary>
/// A test-only sequential integer generator, since the provider has no integer key generation. Values start well
/// above the small explicit ids that upstream seed data uses, and are unique across the whole test process.
/// </summary>
public class TestIntegerValueGenerator<TValue> : ValueGenerator<TValue>
    where TValue : struct
{
    // short cannot hold a large offset, and its keys are not seeded with large values.
    private static long _current = typeof(TValue) == typeof(short) ? 0 : 1_000_000;

    public override bool GeneratesTemporaryValues
        => false;

    public override TValue Next(EntityEntry entry)
        => (TValue)Convert.ChangeType(Interlocked.Increment(ref _current), typeof(TValue));
}
