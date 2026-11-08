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
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Mapping;

public class SpatialMongoTest(SpatialMongoTest.SpatialMongoFixture fixture)
    : SpatialTestBase<SpatialMongoTest.SpatialMongoFixture>(fixture)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
    {
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
#if (EF10 || EF11)
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override void Can_roundtrip_Z_and_M()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_roundtrip_Z_and_M(), "No suitable constructor was found for the type 'LineStringEntity.LineS");
#elif EF9 || EF8
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override void Can_roundtrip_Z_and_M()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_roundtrip_Z_and_M(), "No suitable constructor was found for entity type 'LineStringEntity.Li");
#endif

#if (EF10 || EF11)
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Mutation_of_tracked_values_does_not_mutate_values_in_store()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Mutation_of_tracked_values_does_not_mutate_values_in_store(), "No suitable constructor was found for the type 'LineStringEntity.LineS");
#elif EF9
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Mutation_of_tracked_values_does_not_mutate_values_in_store()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Mutation_of_tracked_values_does_not_mutate_values_in_store(), "No suitable constructor was found for entity type 'LineStringEntity.Li");
#elif EF8
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override void Mutation_of_tracked_values_does_not_mutate_values_in_store()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Mutation_of_tracked_values_does_not_mutate_values_in_store(), "No suitable constructor was found for entity type 'LineStringEntity.Li");
#endif

#if (EF10 || EF11)
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override void Translators_handle_static_members()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Translators_handle_static_members(), "No suitable constructor was found for the type 'LineStringEntity.LineS");
#elif EF9 || EF8
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override void Translators_handle_static_members()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Translators_handle_static_members(), "No suitable constructor was found for entity type 'LineStringEntity.Li");
#endif

#if (EF10 || EF11)
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override void Values_are_copied_into_change_tracker()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Values_are_copied_into_change_tracker(), "No suitable constructor was found for the type 'LineStringEntity.LineS");
#elif EF9 || EF8
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override void Values_are_copied_into_change_tracker()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Values_are_copied_into_change_tracker(), "No suitable constructor was found for entity type 'LineStringEntity.Li");
#endif

#if (EF10 || EF11)
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override void Values_arent_compared_by_reference()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Values_arent_compared_by_reference(), "No suitable constructor was found for the type 'LineStringEntity.LineS");
#elif EF9 || EF8
    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override void Values_arent_compared_by_reference()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Values_arent_compared_by_reference(), "No suitable constructor was found for entity type 'LineStringEntity.Li");
#endif
    // </generated>

    public class SpatialMongoFixture : SpatialFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("Spatial");

        public TestServer TestServer { get; private set; }

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            try
            {
                await base.InitializeAsync();
            }
            catch (InvalidOperationException)
            {
                // Fails: Model/seeding fails during fixture initialization EF-X320
                // The tests then run (and fail) against an uninitialized database.
            }
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
        }
    }
}
