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
using Microsoft.EntityFrameworkCore.TestModels.ManyToManyFieldsModel;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Loading;

public class ManyToManyFieldsLoadMongoTest(ManyToManyFieldsLoadMongoTest.ManyToManyFieldsLoadMongoFixture fixture)
    : ManyToManyFieldsLoadTestBase<ManyToManyFieldsLoadMongoTest.ManyToManyFieldsLoadMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection(state, queryTrackingBehavior, async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query(EntityState state, bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query(state, async))).Message);

    public override void Attached_collections_are_not_marked_as_loaded(EntityState state)
        => base.Attached_collections_are_not_marked_as_loaded(state);

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_already_loaded(EntityState state, bool async)
        => Assert.Contains("is not supported", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_already_loaded(state, async))).Message);

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_using_Query_already_loaded(EntityState state, bool async)
        => Assert.Contains("is not supported", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_already_loaded(state, async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_untyped(EntityState state, bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_untyped(state, async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_untyped(EntityState state, bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_untyped(state, async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_not_found_untyped(EntityState state, bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_not_found_untyped(state, async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_not_found_untyped(EntityState state, bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_not_found_untyped(state, async))).Message);

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_already_loaded_untyped(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => Assert.Contains("is not supported", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_already_loaded_untyped(state, async, deleteOrphansTiming))).Message);

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Load_collection_using_Query_already_loaded_untyped(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => Assert.Contains("is not supported", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_already_loaded_untyped(state, async, deleteOrphansTiming))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_composite_key(EntityState state, bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_composite_key(state, async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_composite_key(EntityState state, bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_composite_key(state, async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_for_detached_throws(bool async, QueryTrackingBehavior queryTrackingBehavior)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_for_detached_throws(async, queryTrackingBehavior))).Message);

    public override void Query_collection_for_detached_throws(QueryTrackingBehavior queryTrackingBehavior)
        => base.Query_collection_for_detached_throws(queryTrackingBehavior);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_Include(bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_Include(async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_Include_for_inverse(bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_Include_for_inverse(async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_Include_for_same_collection(bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_Include_for_same_collection(async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_filtered_Include(bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_filtered_Include(async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_filtered_Include_and_projection(bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_filtered_Include_and_projection(async))).Message);

    // Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209
    public override async Task Load_collection_using_Query_with_join(bool async)
        => Assert.Contains("could not be translated", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection_using_Query_with_join(async))).Message);

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Query_with_Include_marks_only_left_as_loaded(bool async)
        => Assert.Contains("is not supported", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Query_with_Include_marks_only_left_as_loaded(async))).Message);

    // Fails: Include of a many-to-many skip navigation is not supported EF-X208
    public override async Task Query_with_filtered_Include_marks_only_left_as_loaded(bool async)
        => Assert.Contains("is not supported", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Query_with_filtered_Include_marks_only_left_as_loaded(async))).Message);

    public class ManyToManyFieldsLoadMongoFixture : ManyToManyFieldsLoadFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("ManyToManyFieldsLoadTest");

        public TestServer TestServer { get; private set; }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async Task InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await base.InitializeAsync();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.MapSinglePropertyKeysToElementId(generateIntKeys: true); // EF-X201, EF-X205
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            // Types reached only through navigations are auto-owned by the provider; register the model types as
            // independent entities first so join entities etc. get their own collections (EF-X207).
            foreach (var type in typeof(EntityOne).Assembly.GetTypes().Where(
                         t => t.Namespace == "Microsoft.EntityFrameworkCore.TestModels.ManyToManyFieldsModel"
                              && t.IsClass && !t.IsAbstract && !t.IsNested && !t.IsGenericType && t.Name != "ProxyableSharedType" && !typeof(DbContext).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null))
            {
                modelBuilder.Entity(type);
            }

            base.OnModelCreating(modelBuilder, context);
            modelBuilder.RelaxUniqueForeignKeyIndexes(); // EF-X207
        }
    }
}
