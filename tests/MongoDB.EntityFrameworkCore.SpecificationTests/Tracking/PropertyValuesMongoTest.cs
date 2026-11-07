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
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Tracking;

public class PropertyValuesMongoTest(PropertyValuesMongoTest.PropertyValuesMongoFixture fixture)
    : PropertyValuesTestBase<PropertyValuesMongoTest.PropertyValuesMongoFixture>(fixture)
{
    public class PropertyValuesMongoFixture : PropertyValuesFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("PropertyValues");

        public TestServer TestServer { get; private set; } = null!;

        public override async Task InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override PoolableDbContext CreateContext()
        {
            _initFailure.ThrowIfFailed();
            return base.CreateContext();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: false);
        }
    }

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Complex_collection_current_values_can_be_accessed_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Complex_collection_current_values_can_be_accessed_as_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Complex_collection_original_values_can_be_accessed_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Complex_collection_original_values_can_be_accessed_as_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Complex_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Complex_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Complex_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Complex_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Complex_store_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Complex_store_values_can_be_accessed_as_a_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Complex_store_values_can_be_accessed_asynchronously_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Complex_store_values_can_be_accessed_asynchronously_as_a_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_cloned()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_cloned(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_can_be_copied_from_a_non_generic_property_dictionary_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_can_be_copied_from_a_non_generic_property_dictionary_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_can_be_copied_into_a_cloned_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_can_be_copied_into_a_cloned_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_can_be_copied_into_a_non_generic_cloned_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_can_be_copied_into_a_non_generic_cloned_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_can_be_copied_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_copied_to_object_using_ToObject()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_copied_to_object_using_ToObject(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_can_be_read_and_set_for_an_object_in_the_Added_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_can_be_read_and_set_for_an_object_in_the_Added_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_can_be_read_and_set_for_an_object_in_the_Modified_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_can_be_read_and_set_for_an_object_in_the_Modified_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_can_be_read_or_set_for_a_Detached_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_can_be_read_or_set_for_a_Detached_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_can_be_read_or_set_for_an_object_in_the_Deleted_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_can_be_read_or_set_for_an_object_in_the_Deleted_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_set_from_DTO_object_missing_key_using_non_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_set_from_DTO_object_missing_key_using_non_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_set_from_DTO_object_using_non_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_set_from_DTO_object_using_non_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_set_from_an_object_using_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_set_from_an_object_using_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_set_from_an_object_using_non_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_set_from_an_object_using_non_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_set_from_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_set_from_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_set_from_dictionary_some_missing()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_set_from_dictionary_some_missing(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_set_from_dictionary_typed_int()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_set_from_dictionary_typed_int(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_set_from_dictionary_typed_string()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_set_from_dictionary_typed_string(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_set_from_one_generic_dictionary_to_another_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_set_from_one_generic_dictionary_to_another_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Current_values_can_be_set_from_one_non_generic_dictionary_to_another_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Current_values_can_be_set_from_one_non_generic_dictionary_to_another_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_for_derived_object_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_for_derived_object_can_be_copied_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Current_values_for_join_entity_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_for_join_entity_can_be_copied_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task GetDatabaseValuesAsync_for_derived_entity_not_in_the_store_returns_null()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.GetDatabaseValuesAsync_for_derived_entity_not_in_the_store_returns_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task GetDatabaseValuesAsync_for_entity_not_in_the_store_returns_null()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.GetDatabaseValuesAsync_for_entity_not_in_the_store_returns_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task GetDatabaseValuesAsync_for_the_wrong_type_in_the_store_returns_null()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.GetDatabaseValuesAsync_for_the_wrong_type_in_the_store_returns_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task GetDatabaseValues_for_derived_entity_not_in_the_store_returns_null()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.GetDatabaseValues_for_derived_entity_not_in_the_store_returns_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task GetDatabaseValues_for_entity_not_in_the_store_returns_null()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.GetDatabaseValues_for_entity_not_in_the_store_returns_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task GetDatabaseValues_for_the_wrong_type_in_the_store_returns_null()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.GetDatabaseValues_for_the_wrong_type_in_the_store_returns_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task NonGeneric_GetDatabaseValuesAsync_for_derived_entity_not_in_the_store_returns_null()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.NonGeneric_GetDatabaseValuesAsync_for_derived_entity_not_in_the_store_returns_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task NonGeneric_GetDatabaseValuesAsync_for_entity_not_in_the_store_returns_null()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.NonGeneric_GetDatabaseValuesAsync_for_entity_not_in_the_store_returns_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task NonGeneric_GetDatabaseValuesAsync_for_the_wrong_type_in_the_store_throws()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.NonGeneric_GetDatabaseValuesAsync_for_the_wrong_type_in_the_store_throws(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task NonGeneric_GetDatabaseValues_for_derived_entity_not_in_the_store_returns_null()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.NonGeneric_GetDatabaseValues_for_derived_entity_not_in_the_store_returns_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task NonGeneric_GetDatabaseValues_for_entity_not_in_the_store_returns_null()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.NonGeneric_GetDatabaseValues_for_entity_not_in_the_store_returns_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task NonGeneric_GetDatabaseValues_for_the_wrong_type_in_the_store_throws()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.NonGeneric_GetDatabaseValues_for_the_wrong_type_in_the_store_throws(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Non_nullable_property_in_cloned_dictionary_cannot_be_set_to_null()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Non_nullable_property_in_cloned_dictionary_cannot_be_set_to_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Non_nullable_property_in_current_values_results_in_conceptual_null(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Non_nullable_property_in_current_values_results_in_conceptual_null(deleteOrphansTiming), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Non_nullable_property_in_original_values_cannot_be_set_to_null_in_property_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Non_nullable_property_in_original_values_cannot_be_set_to_null_in_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Non_nullable_shadow_property_in_current_values_results_in_conceptual_null(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Non_nullable_shadow_property_in_current_values_results_in_conceptual_null(deleteOrphansTiming), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Non_nullable_shadow_property_in_original_values_cannot_be_set_to_null_in_property_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Non_nullable_shadow_property_in_original_values_cannot_be_set_to_null_in_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Nullable_complex_property_with_null_value_returns_null_when_using_ToObject()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Nullable_complex_property_with_null_value_returns_null_when_using_ToObject(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_cloned()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_cloned(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_can_be_copied_into_a_cloned_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_can_be_copied_into_a_cloned_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_can_be_copied_into_a_non_generic_cloned_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_can_be_copied_into_a_non_generic_cloned_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_can_be_copied_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_can_be_copied_non_generic_property_dictionary_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_can_be_copied_non_generic_property_dictionary_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_copied_to_object_using_ToObject()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_copied_to_object_using_ToObject(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_can_be_read_and_set_for_an_object_in_the_Deleted_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_can_be_read_and_set_for_an_object_in_the_Deleted_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_can_be_read_and_set_for_an_object_in_the_Modified_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_can_be_read_and_set_for_an_object_in_the_Modified_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_can_be_read_or_set_for_a_Detached_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_can_be_read_or_set_for_a_Detached_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_can_be_read_or_set_for_an_object_in_the_Added_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_can_be_read_or_set_for_an_object_in_the_Added_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_set_from_DTO_object_missing_key_using_non_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_set_from_DTO_object_missing_key_using_non_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_set_from_DTO_object_using_non_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_set_from_DTO_object_using_non_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_set_from_an_object_using_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_set_from_an_object_using_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_set_from_an_object_using_non_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_set_from_an_object_using_non_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_set_from_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_set_from_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_set_from_dictionary_some_missing()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_set_from_dictionary_some_missing(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_set_from_dictionary_typed_int()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_set_from_dictionary_typed_int(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_set_from_dictionary_typed_string()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_set_from_dictionary_typed_string(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_set_from_one_generic_dictionary_to_another_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_set_from_one_generic_dictionary_to_another_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Original_values_can_be_set_from_one_non_generic_dictionary_to_another_generic_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Original_values_can_be_set_from_one_non_generic_dictionary_to_another_generic_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_for_derived_object_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_for_derived_object_can_be_copied_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Original_values_for_join_entity_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_for_join_entity_can_be_copied_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Primary_key_in_current_values_cannot_be_changed_by_setting_values_from_another_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Primary_key_in_current_values_cannot_be_changed_by_setting_values_from_another_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Primary_key_in_current_values_cannot_be_changed_by_setting_values_from_object()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Primary_key_in_current_values_cannot_be_changed_by_setting_values_from_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Primary_key_in_current_values_cannot_be_changed_in_property_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Primary_key_in_current_values_cannot_be_changed_in_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Primary_key_in_original_values_cannot_be_changed_by_setting_values_from_another_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Primary_key_in_original_values_cannot_be_changed_by_setting_values_from_another_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Primary_key_in_original_values_cannot_be_changed_by_setting_values_from_object()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Primary_key_in_original_values_cannot_be_changed_by_setting_values_from_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Primary_key_in_original_values_cannot_be_changed_in_property_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Primary_key_in_original_values_cannot_be_changed_in_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Properties_for_cloned_dictionary_returns_properties()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Properties_for_cloned_dictionary_returns_properties(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Properties_for_current_values_returns_properties()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Properties_for_current_values_returns_properties(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Properties_for_original_values_returns_properties()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Properties_for_original_values_returns_properties(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Properties_for_store_values_returns_properties()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Properties_for_store_values_returns_properties(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Properties_for_store_values_returns_properties_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Properties_for_store_values_returns_properties_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Property_in_cloned_dictionary_cannot_be_set_to_instance_of_wrong_type()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Property_in_cloned_dictionary_cannot_be_set_to_instance_of_wrong_type(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Property_in_current_values_cannot_be_set_to_instance_of_wrong_type()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Property_in_current_values_cannot_be_set_to_instance_of_wrong_type(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Property_in_original_values_cannot_be_set_to_instance_of_wrong_type()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Property_in_original_values_cannot_be_set_to_instance_of_wrong_type(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task Reload_when_entity_deleted_in_store_can_happen_for_any_state(global::Microsoft.EntityFrameworkCore.EntityState state, bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_current_values_can_be_accessed_as_a_non_generic_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_current_values_can_be_accessed_as_a_non_generic_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_current_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_current_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_current_values_can_be_accessed_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_current_values_can_be_accessed_as_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Scalar_current_values_can_be_set_using_a_non_generic_property_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Scalar_current_values_can_be_set_using_a_non_generic_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Scalar_current_values_can_be_set_using_a_property_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Scalar_current_values_can_be_set_using_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Scalar_current_values_can_be_set_using_a_property_dictionary_with_IProperty()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Scalar_current_values_can_be_set_using_a_property_dictionary_with_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_current_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_current_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_current_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_current_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_original_values_can_be_accessed_as_a_non_generic_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_original_values_can_be_accessed_as_a_non_generic_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_original_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_original_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_original_values_can_be_accessed_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_original_values_can_be_accessed_as_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Scalar_original_values_can_be_set_using_a_non_generic_property_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Scalar_original_values_can_be_set_using_a_non_generic_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Scalar_original_values_can_be_set_using_a_property_dictionary()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Scalar_original_values_can_be_set_using_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Scalar_original_values_can_be_set_using_a_property_dictionary_with_IProperty()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Scalar_original_values_can_be_set_using_a_property_dictionary_with_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_original_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_original_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_original_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_original_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_can_be_accessed_as_a_non_generic_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_can_be_accessed_as_a_non_generic_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_can_be_accessed_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_can_be_accessed_as_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_can_be_accessed_as_a_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_can_be_accessed_asynchronously_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_can_be_accessed_asynchronously_as_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_can_be_accessed_asynchronously_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_can_be_accessed_asynchronously_as_a_property_dictionary_using_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_of_a_derived_object_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_of_a_derived_object_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Scalar_store_values_of_a_derived_object_can_be_accessed_asynchronously_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Scalar_store_values_of_a_derived_object_can_be_accessed_asynchronously_as_a_property_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void SetValues_throws_for_complex_collection_with_non_dictionary_item()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.SetValues_throws_for_complex_collection_with_non_dictionary_item(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void SetValues_throws_for_complex_collection_with_non_list_value()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.SetValues_throws_for_complex_collection_with_non_list_value(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void SetValues_throws_for_complex_property_with_non_dictionary_value()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.SetValues_throws_for_complex_property_with_non_dictionary_value(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void SetValues_throws_for_nested_complex_collection_with_non_dictionary_item()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.SetValues_throws_for_nested_complex_collection_with_non_dictionary_item(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void SetValues_throws_for_nested_complex_collection_with_non_list_value()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.SetValues_throws_for_nested_complex_collection_with_non_list_value(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Setting_complex_collection_current_values_from_DTO_with_complex_metadata_access_works()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Setting_complex_collection_current_values_from_DTO_with_complex_metadata_access_works(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Setting_complex_collection_current_values_from_dictionary_with_nulls_works()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Setting_complex_collection_current_values_from_dictionary_with_nulls_works(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Setting_complex_collection_current_values_from_dictionary_works()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Setting_complex_collection_current_values_from_dictionary_works(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Setting_complex_collection_current_values_from_object_with_nulls_works()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Setting_complex_collection_current_values_from_object_with_nulls_works(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Setting_complex_collection_original_values_from_dictionary_with_nulls_works()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Setting_complex_collection_original_values_from_dictionary_with_nulls_works(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Setting_complex_collection_original_values_from_object_with_nulls_works()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Setting_complex_collection_original_values_from_object_with_nulls_works(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Setting_complex_collection_values_from_DTO_with_nulls_works()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Setting_complex_collection_values_from_DTO_with_nulls_works(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Setting_complex_collection_values_from_object_works()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Setting_complex_collection_values_from_object_works(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Setting_current_values_from_cloned_values_sets_nullable_complex_property_to_null()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Setting_current_values_from_cloned_values_sets_nullable_complex_property_to_null(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Setting_store_values_does_not_change_current_or_original_values()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Setting_store_values_does_not_change_current_or_original_values(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Shadow_property_in_current_values_cannot_be_set_to_instance_of_wrong_type()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Shadow_property_in_current_values_cannot_be_set_to_instance_of_wrong_type(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Shadow_property_in_original_values_cannot_be_set_to_instance_of_wrong_type()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Shadow_property_in_original_values_cannot_be_set_to_instance_of_wrong_type(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_copied_asynchronously_into_a_non_generic_cloned_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_copied_asynchronously_into_a_non_generic_cloned_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_copied_asynchronously_non_generic_property_dictionary_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_copied_asynchronously_non_generic_property_dictionary_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_copied_into_a_cloned_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_copied_into_a_cloned_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_copied_into_a_cloned_dictionary_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_copied_into_a_cloned_dictionary_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_copied_into_a_non_generic_cloned_dictionary()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_copied_into_a_non_generic_cloned_dictionary(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_copied_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_copied_into_an_object_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_copied_into_an_object_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_copied_non_generic_property_dictionary_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_copied_non_generic_property_dictionary_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_copied_to_object_using_ToObject()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_copied_to_object_using_ToObject(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_copied_to_object_using_ToObject_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_copied_to_object_using_ToObject_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Deleted_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Deleted_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Deleted_state_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Deleted_state_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Modified_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Modified_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Modified_state_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Modified_state_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_read_or_set_for_a_Detached_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_read_or_set_for_a_Detached_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_read_or_set_for_a_Detached_object_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_read_or_set_for_a_Detached_object_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_read_or_set_for_an_object_in_the_Added_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_read_or_set_for_an_object_in_the_Added_state(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_can_be_read_or_set_for_an_object_in_the_Added_state_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_can_be_read_or_set_for_an_object_in_the_Added_state_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_for_derived_object_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_for_derived_object_can_be_copied_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_for_derived_object_can_be_copied_into_an_object_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_for_derived_object_can_be_copied_into_an_object_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_for_join_entity_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_for_join_entity_can_be_copied_into_an_object(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Store_values_for_join_entity_can_be_copied_into_an_object_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_for_join_entity_can_be_copied_into_an_object_asynchronously(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task Store_values_really_are_store_values_not_current_or_original_values()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task Store_values_really_are_store_values_not_current_or_original_values_async()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Using_bad_IProperty_instances_throws()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Using_bad_IProperty_instances_throws(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Using_bad_IProperty_instances_throws_derived()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Using_bad_IProperty_instances_throws_derived(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Using_bad_property_names_throws()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Using_bad_property_names_throws(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Using_bad_property_names_throws_derived()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Using_bad_property_names_throws_derived(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Using_complex_property_value_not_list_throws()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Using_complex_property_value_not_list_throws(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Using_non_collection_complex_property_throws()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Using_non_collection_complex_property_throws(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task Values_can_be_reloaded_from_database_for_entity_in_any_state(global::Microsoft.EntityFrameworkCore.EntityState state, bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Values_can_be_reloaded_from_database_for_entity_in_any_state_with_inheritance(global::Microsoft.EntityFrameworkCore.EntityState state, bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Values_can_be_reloaded_from_database_for_entity_in_any_state_with_inheritance(state, async), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Values_in_cloned_dictionary_can_be_set_with_IProperty()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Values_in_cloned_dictionary_can_be_set_with_IProperty(), "The instance of entity type 'VirtualTeam' cannot be tracked because another instance with ");
}
