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
using Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Loading;

public class ManyToManyLoadMongoTest(ManyToManyLoadMongoTest.ManyToManyLoadMongoFixture fixture)
    : ManyToManyLoadTestBase<ManyToManyLoadMongoTest.ManyToManyLoadMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection(state, queryTrackingBehavior, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query(state, async), "could not be translated");

    public override void Attached_collections_are_not_marked_as_loaded(EntityState state, bool lazy)
        => base.Attached_collections_are_not_marked_as_loaded(state, lazy);

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_already_loaded(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_already_loaded(state, async), "is not supported");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_using_Query_already_loaded(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_already_loaded(state, async), "is not supported");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_partially_loaded(EntityState state, bool forceIdentityResolution, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_partially_loaded(state, forceIdentityResolution, async), "is not supported");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_partially_loaded_no_explicit_join(EntityState state, bool forceIdentityResolution, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_partially_loaded_no_explicit_join(state, forceIdentityResolution, async), "is not supported");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override void Load_collection_partially_loaded_no_tracking(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Load_collection_partially_loaded_no_tracking(queryTrackingBehavior), "is not supported");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_untyped(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_untyped(state, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_untyped(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_untyped(state, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_not_found_untyped(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_not_found_untyped(state, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_not_found_untyped(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_not_found_untyped(state, async), "could not be translated");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_already_loaded_untyped(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_already_loaded_untyped(state, async, deleteOrphansTiming), "is not supported");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_using_Query_already_loaded_untyped(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_already_loaded_untyped(state, async, deleteOrphansTiming), "is not supported");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_composite_key(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_composite_key(state, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_composite_key(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_composite_key(state, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_for_detached_throws(bool async, QueryTrackingBehavior queryTrackingBehavior)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_for_detached_throws(async, queryTrackingBehavior), "could not be translated");

    public override void Query_collection_for_detached_throws(QueryTrackingBehavior queryTrackingBehavior)
        => base.Query_collection_for_detached_throws(queryTrackingBehavior);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_Include(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_Include(async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_Include_for_inverse(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_Include_for_inverse(async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_Include_for_same_collection(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_Include_for_same_collection(async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_filtered_Include(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_filtered_Include(async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_filtered_Include_and_projection(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_filtered_Include_and_projection(async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_join(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_join(async), "could not be translated");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Query_with_Include_marks_only_left_as_loaded(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Query_with_Include_marks_only_left_as_loaded(async), "is not supported");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Query_with_filtered_Include_marks_only_left_as_loaded(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Query_with_filtered_Include_marks_only_left_as_loaded(async), "is not supported");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_unidirectional(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_unidirectional(state, queryTrackingBehavior, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_unidirectional(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_unidirectional(state, async), "could not be translated");

    public override void Attached_collections_are_not_marked_as_loaded_unidirectional(EntityState state, bool lazy)
        => base.Attached_collections_are_not_marked_as_loaded_unidirectional(state, lazy);

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_already_loaded_unidirectional(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_already_loaded_unidirectional(state, async), "is not supported");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_using_Query_already_loaded_unidirectional(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_already_loaded_unidirectional(state, async), "is not supported");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_untyped_unidirectional(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_untyped_unidirectional(state, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_untyped_unidirectional(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_untyped_unidirectional(state, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_not_found_untyped_unidirectional(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_not_found_untyped_unidirectional(state, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_not_found_untyped_unidirectional(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_not_found_untyped_unidirectional(state, async), "could not be translated");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_already_loaded_untyped_unidirectional(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_already_loaded_untyped_unidirectional(state, async, deleteOrphansTiming), "is not supported");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_using_Query_already_loaded_untyped_unidirectional(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_already_loaded_untyped_unidirectional(state, async, deleteOrphansTiming), "is not supported");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_composite_key_unidirectional(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_composite_key_unidirectional(state, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_composite_key_unidirectional(EntityState state, bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_composite_key_unidirectional(state, async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_for_detached_throws_unidirectional(bool async, QueryTrackingBehavior queryTrackingBehavior)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_for_detached_throws_unidirectional(async, queryTrackingBehavior), "could not be translated");

    public override void Query_collection_for_detached_throws_unidirectional(QueryTrackingBehavior queryTrackingBehavior)
        => base.Query_collection_for_detached_throws_unidirectional(queryTrackingBehavior);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_Include_unidirectional(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_Include_unidirectional(async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_Include_for_inverse_unidirectional(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_Include_for_inverse_unidirectional(async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_filtered_Include_unidirectional(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_filtered_Include_unidirectional(async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_filtered_Include_and_projection_unidirectional(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_filtered_Include_and_projection_unidirectional(async), "could not be translated");

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_join_unidirectional(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_join_unidirectional(async), "could not be translated");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Query_with_Include_marks_only_left_as_loaded_unidirectional(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Query_with_Include_marks_only_left_as_loaded_unidirectional(async), "is not supported");

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Query_with_filtered_Include_marks_only_left_as_loaded_unidirectional(bool async)
        => await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Query_with_filtered_Include_marks_only_left_as_loaded_unidirectional(async), "is not supported");

    public class ManyToManyLoadMongoFixture : ManyToManyLoadFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("ManyToManyLoadTest");

        public TestServer TestServer { get; private set; }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.MapSinglePropertyKeysToElementId(generateIntKeys: true); // EF-X201, EF-X205, EF-X207
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            // Types reached only through navigations are auto-owned by the provider; register the model types as
            // independent entities first so join entities etc. get their own collections (EF-X207).
            foreach (var type in typeof(EntityOne).Assembly.GetTypes().Where(
                         t => t.Namespace == "Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel"
                              && t.IsClass && !t.IsAbstract && !t.IsNested && !t.IsGenericType && t.Name != "ProxyableSharedType" && !typeof(DbContext).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null))
            {
                modelBuilder.Entity(type);
            }

            base.OnModelCreating(modelBuilder, context);
            modelBuilder.RelaxUniqueForeignKeyIndexes();
        }
    }
}
