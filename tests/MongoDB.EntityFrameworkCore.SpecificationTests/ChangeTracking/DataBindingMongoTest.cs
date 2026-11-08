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
using Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.ChangeTracking;

public class DataBindingMongoTest(DataBindingMongoTest.DataBindingMongoFixture fixture)
    : DataBindingTestBase<DataBindingMongoTest.DataBindingMongoFixture>(fixture)
{
    [ConditionalFact(Skip = "The base declares LocalView_is_initialized_with_entities_from_the_context non-virtual, so it is hidden below and the check cannot see it is handled")]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override void DbSet_Local_contains_Unchanged_Modified_and_Added_entities_but_not_Deleted_entities(bool toObservableCollection)
        => base.DbSet_Local_contains_Unchanged_Modified_and_Added_entities_but_not_Deleted_entities(toObservableCollection);

    public override void Adding_entity_to_context_is_reflected_in_local_view(bool toObservableCollection)
        => base.Adding_entity_to_context_is_reflected_in_local_view(toObservableCollection);

    public override void Attaching_entity_to_context_is_reflected_in_local_view(bool toObservableCollection)
        => base.Attaching_entity_to_context_is_reflected_in_local_view(toObservableCollection);

#if !EF11
    // The base declares this test non-virtual, so it cannot be overridden; hide it and skip.
    // Fails: Materialization interception creates the base proxy type (DriverProxy) for derived-type documents, so the cast to TestDriver fails EF-X216
    [ConditionalTheory(Skip = "Base test is non-virtual; fails with InvalidCastException (DriverProxy to TestDriver) because the base proxy type is created for derived-type documents, EF-X216")]
    [InlineData(false)]
    [InlineData(true)]
#pragma warning disable xUnit1024
    public new void LocalView_is_initialized_with_entities_from_the_context(bool toObservableCollection)
    {
    }
#pragma warning restore xUnit1024
#endif

    public override void Entities_materialized_into_context_are_reflected_in_local_view(bool toObservableCollection)
        => base.Entities_materialized_into_context_are_reflected_in_local_view(toObservableCollection);

    public override void Entities_detached_from_context_are_removed_from_local_view(bool toObservableCollection)
        => base.Entities_detached_from_context_are_removed_from_local_view(toObservableCollection);

    public override void Entities_deleted_from_context_are_removed_from_local_view(bool toObservableCollection)
        => base.Entities_deleted_from_context_are_removed_from_local_view(toObservableCollection);

    public override void Entities_with_state_changed_to_deleted_are_removed_from_local_view(bool toObservableCollection)
        => base.Entities_with_state_changed_to_deleted_are_removed_from_local_view(toObservableCollection);

    public override void Entities_with_state_changed_to_detached_are_removed_from_local_view(bool toObservableCollection)
        => base.Entities_with_state_changed_to_detached_are_removed_from_local_view(toObservableCollection);

    public override void Entities_with_state_changed_from_deleted_to_added_are_added_to_local_view(bool toObservableCollection)
        => base.Entities_with_state_changed_from_deleted_to_added_are_added_to_local_view(toObservableCollection);

    public override void Entities_with_state_changed_from_deleted_to_unchanged_are_added_to_local_view(bool toObservableCollection)
        => base.Entities_with_state_changed_from_deleted_to_unchanged_are_added_to_local_view(toObservableCollection);

    public override void Entities_added_to_local_view_are_added_to_state_manager(bool toObservableCollection)
        => base.Entities_added_to_local_view_are_added_to_state_manager(toObservableCollection);

    public override void Entities_and_owned_children_added_to_local_view_are_added_to_state_manager(bool toObservableCollection)
        => base.Entities_and_owned_children_added_to_local_view_are_added_to_state_manager(toObservableCollection);

    public override void Entities_removed_from_the_local_view_are_marked_deleted_in_the_state_manager(bool toObservableCollection)
        => base.Entities_removed_from_the_local_view_are_marked_deleted_in_the_state_manager(toObservableCollection);

    public override void Adding_entity_to_local_view_that_is_already_in_the_state_manager_and_not_Deleted_is_noop()
        => base.Adding_entity_to_local_view_that_is_already_in_the_state_manager_and_not_Deleted_is_noop();

    public override void Adding_entity_to_local_view_that_is_Deleted_in_the_state_manager_makes_entity_Added(bool toObservableCollection)
        => base.Adding_entity_to_local_view_that_is_Deleted_in_the_state_manager_makes_entity_Added(toObservableCollection);

    public override void Adding_entity_to_state_manager_of_different_type_than_local_keyless_type_has_no_effect_on_local_view(bool toObservableCollection)
        => base.Adding_entity_to_state_manager_of_different_type_than_local_keyless_type_has_no_effect_on_local_view(toObservableCollection);

    public override void Adding_entity_to_state_manager_of_subtype_still_shows_up_in_local_view(bool toObservableCollection)
        => base.Adding_entity_to_state_manager_of_subtype_still_shows_up_in_local_view(toObservableCollection);

    public override void DbSet_Local_is_cached_on_the_set()
        => base.DbSet_Local_is_cached_on_the_set();

    public override void DbSet_Local_calls_DetectChanges()
        => base.DbSet_Local_calls_DetectChanges();

    public override void Load_executes_query_on_keyless_entity_type()
        => base.Load_executes_query_on_keyless_entity_type();

    public override void DbSet_Local_ToBindingList_contains_Unchanged_Modified_and_Added_entities_but_not_Deleted_entities()
        => base.DbSet_Local_ToBindingList_contains_Unchanged_Modified_and_Added_entities_but_not_Deleted_entities();

    public override void Adding_entity_to_context_is_reflected_in_local_binding_list()
        => base.Adding_entity_to_context_is_reflected_in_local_binding_list();

    public override void Entities_materialized_into_context_are_reflected_in_local_binding_list()
        => base.Entities_materialized_into_context_are_reflected_in_local_binding_list();

    public override void Entities_detached_from_context_are_removed_from_local_binding_list()
        => base.Entities_detached_from_context_are_removed_from_local_binding_list();

    public override void Entities_deleted_from_context_are_removed_from_local_binding_list()
        => base.Entities_deleted_from_context_are_removed_from_local_binding_list();

    public override void Entities_added_to_local_binding_list_are_added_to_state_manager()
        => base.Entities_added_to_local_binding_list_are_added_to_state_manager();

    public override void Entities_removed_from_the_local_binding_list_are_marked_deleted_in_the_state_manager()
        => base.Entities_removed_from_the_local_binding_list_are_marked_deleted_in_the_state_manager();

    public override void Adding_entity_to_local_binding_list_that_is_Deleted_in_the_state_manager_makes_entity_Added()
        => base.Adding_entity_to_local_binding_list_that_is_Deleted_in_the_state_manager_makes_entity_Added();

    public override void Adding_entity_to_state_manager_of_different_type_than_local_keyless_type_has_no_effect_on_local_binding_list()
        => base.Adding_entity_to_state_manager_of_different_type_than_local_keyless_type_has_no_effect_on_local_binding_list();

    public override void Adding_entity_to_state_manager_of_subtype_still_shows_up_in_local_binding_list()
        => base.Adding_entity_to_state_manager_of_subtype_still_shows_up_in_local_binding_list();

    public override void Sets_of_subtypes_can_still_be_sorted()
        => base.Sets_of_subtypes_can_still_be_sorted();

    public override void Sets_containing_instances_of_subtypes_can_still_be_sorted()
        => base.Sets_containing_instances_of_subtypes_can_still_be_sorted();

    public override void DbSet_Local_ToBindingList_is_cached_on_the_set()
        => base.DbSet_Local_ToBindingList_is_cached_on_the_set();

    public override void Entity_added_to_context_is_added_to_navigation_property_binding_list()
        => base.Entity_added_to_context_is_added_to_navigation_property_binding_list();

    public override void Entity_added_to_navigation_property_binding_list_is_added_to_context_after_DetectChanges()
        => base.Entity_added_to_navigation_property_binding_list_is_added_to_context_after_DetectChanges();

    public override void Entity_removed_from_navigation_property_binding_list_is_removed_from_nav_property_but_not_marked_Deleted(CascadeTiming deleteOrphansTiming)
        => base.Entity_removed_from_navigation_property_binding_list_is_removed_from_nav_property_but_not_marked_Deleted(deleteOrphansTiming);

#if (EF10 || EF11)
    public override void Remove_detached_entity_from_LocalView(bool toObservableCollection)
        => base.Remove_detached_entity_from_LocalView(toObservableCollection);
#endif

    public class DataBindingMongoFixture : F1FixtureBase<byte[]>
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("DataBinding");

        public TestServer TestServer { get; private set; }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await base.InitializeAsync();
        }

#if !EF8
        // The provider does not run UseSeeding/UseAsyncSeeding delegates from EnsureCreated (EF-X215), so seed explicitly.
        protected override async Task SeedAsync(F1Context context)
        {
            if (await ShouldSeedAsync(context))
            {
                F1Context.AddSeedData(context);
                await context.SaveChangesAsync();
            }
        }
#endif

        public override TestHelpers TestHelpers
            => MongoTestHelpers.Instance;

        protected override void BuildModelExternal(ModelBuilder modelBuilder)
        {
            base.BuildModelExternal(modelBuilder);
            modelBuilder.FixExternalModel(dropOwnedConcurrencyTokens: true); // EF-X201, EF-X214
            modelBuilder.RelaxUniqueForeignKeyIndexes(); // EF-X207
        }
    }
}
