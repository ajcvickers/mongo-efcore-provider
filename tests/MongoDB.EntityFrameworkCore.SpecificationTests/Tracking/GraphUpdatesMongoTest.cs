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

public class GraphUpdatesMongoTest(GraphUpdatesMongoTest.GraphUpdatesMongoFixture fixture)
    : GraphUpdatesTestBase<GraphUpdatesMongoTest.GraphUpdatesMongoFixture>(fixture)
{
    public class GraphUpdatesMongoFixture : GraphUpdatesFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("GraphUpdates");

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

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ShadowForeignKeyPropertyCreated));

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: false);
        }
    }

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Alternate_key_over_foreign_key_doesnt_bypass_delete_behavior(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Alternate_key_over_foreign_key_doesnt_bypass_delete_behavior(async), "The instance of entity type 'Optional");

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Avoid_nulling_shared_FK_property_when_deleting()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Avoid_nulling_shared_FK_property_when_deleting(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Avoid_nulling_shared_FK_property_when_deleting()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Avoid_nulling_shared_FK_property_when_deleting(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Avoid_nulling_shared_FK_property_when_nulling_navigation(bool nullPrincipal)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Avoid_nulling_shared_FK_property_when_nulling_navigation(nullPrincipal), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Avoid_nulling_shared_FK_property_when_nulling_navigation(bool nullPrincipal)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Avoid_nulling_shared_FK_property_when_nulling_navigation(nullPrincipal), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_add_multiple_dependents_when_multiple_possible_principal_sides()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_multiple_dependents_when_multiple_possible_principal_sides(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Can_add_multiple_dependents_when_multiple_possible_principal_sides()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_multiple_dependents_when_multiple_possible_principal_sides(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_add_valid_first_dependent_when_multiple_possible_principal_sides()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_valid_first_dependent_when_multiple_possible_principal_sides(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Can_add_valid_first_dependent_when_multiple_possible_principal_sides()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_valid_first_dependent_when_multiple_possible_principal_sides(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_add_valid_second_dependent_when_multiple_possible_principal_sides()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_valid_second_dependent_when_multiple_possible_principal_sides(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Can_add_valid_second_dependent_when_multiple_possible_principal_sides()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_valid_second_dependent_when_multiple_possible_principal_sides(), "The instance of entity type 'Optional");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_insert_when_FK_has_default_value(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_when_FK_has_default_value(async), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_insert_when_FK_has_sentinel_value(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_when_FK_has_sentinel_value(async), "The instance of entity type 'Optional");

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_insert_when_bool_PK_in_composite_key_has_sentinel_value(bool async, bool initialValue)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_when_bool_PK_in_composite_key_has_sentinel_value(async, initialValue), "The instance of entity type 'Optional");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_insert_when_composite_FK_has_default_value_for_one_part(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_when_composite_FK_has_default_value_for_one_part(async), "The instance of entity type 'Optional");

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_insert_when_int_PK_in_composite_key_has_sentinel_value(bool async, int initialValue)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_when_int_PK_in_composite_key_has_sentinel_value(async, initialValue), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_insert_when_nullable_bool_PK_in_composite_key_has_sentinel_value(bool async, bool? initialValue)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_when_nullable_bool_PK_in_composite_key_has_sentinel_value(async, initialValue), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Changes_to_Added_relationships_are_picked_up(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Changes_to_Added_relationships_are_picked_up(changeMechanism), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Changes_to_Added_relationships_are_picked_up(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Changes_to_Added_relationships_are_picked_up(changeMechanism), "The instance of entity type 'Optional");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Clearing_CLR_key_owned_collection(bool async, bool useUpdate, bool addNew)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Clearing_CLR_key_owned_collection(async, useUpdate, addNew), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Clearing_shadow_key_owned_collection_throws(bool async, bool useUpdate, bool addNew)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Clearing_shadow_key_owned_collection_throws(async, useUpdate, addNew), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Delete_principal_with_CLR_key_owned_collection(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Delete_principal_with_CLR_key_owned_collection(async), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Delete_principal_with_shadow_key_owned_collection_throws(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Delete_principal_with_shadow_key_owned_collection_throws(async), "The instance of entity type 'Optional");

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Detaching_dependent_entity_will_not_remove_references_to_it()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Detaching_dependent_entity_will_not_remove_references_to_it(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Detaching_dependent_entity_will_not_remove_references_to_it()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Detaching_dependent_entity_will_not_remove_references_to_it(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Detaching_principal_entity_will_remove_references_to_it()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Detaching_principal_entity_will_remove_references_to_it(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Detaching_principal_entity_will_remove_references_to_it()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Detaching_principal_entity_will_remove_references_to_it(), "The instance of entity type 'Optional");
#endif

#if EF10
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Discriminator_values_are_not_marked_as_unknown(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Discriminator_values_are_not_marked_as_unknown(async), "The instance of entity type 'Optional");
#elif EF9 || EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task Discriminator_values_are_not_marked_as_unknown(bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Mark_explicitly_set_dependent_appropriately_with_any_inheritance_and_stable_generator(bool async, bool useAdd)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Mark_explicitly_set_dependent_appropriately_with_any_inheritance_and_stable_generator(async, useAdd), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Mark_explicitly_set_stable_dependent_appropriately(bool async, bool useAdd)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Mark_explicitly_set_stable_dependent_appropriately(async, useAdd), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Mark_explicitly_set_stable_dependent_appropriately_when_deep_in_graph(bool async, bool useAdd)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Mark_explicitly_set_stable_dependent_appropriately_when_deep_in_graph(async, useAdd), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Mark_modified_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Mark_modified_one_to_many_overlapping(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Mark_modified_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Mark_modified_one_to_many_overlapping(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Mutating_discriminator_value_can_be_configured_to_allow_mutation()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Mutating_discriminator_value_can_be_configured_to_allow_mutation(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Mutating_discriminator_value_can_be_configured_to_allow_mutation()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Mutating_discriminator_value_can_be_configured_to_allow_mutation(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Mutating_discriminator_value_throws_by_convention()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Mutating_discriminator_value_throws_by_convention(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Mutating_discriminator_value_throws_by_convention()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Mutating_discriminator_value_throws_by_convention(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task New_FK_is_not_cleared_on_old_dependent_delete(bool loadNewParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.New_FK_is_not_cleared_on_old_dependent_delete(loadNewParent, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void New_FK_is_not_cleared_on_old_dependent_delete(bool loadNewParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.New_FK_is_not_cleared_on_old_dependent_delete(loadNewParent, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task No_fixup_to_Deleted_entities(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.No_fixup_to_Deleted_entities(deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void No_fixup_to_Deleted_entities(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.No_fixup_to_Deleted_entities(deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Notification_entities_can_have_indexes()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Notification_entities_can_have_indexes(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Notification_entities_can_have_indexes()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Notification_entities_can_have_indexes(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_many_to_one_dependent_leaves_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependent_leaves_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_many_to_one_dependent_leaves_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependent_leaves_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_many_to_one_dependents_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_many_to_one_dependents_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_many_to_one_dependents_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_many_to_one_dependents_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_many_to_one_dependents_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_many_to_one_dependents_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_many_to_one_dependents_are_orphaned_with_Added_graph(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_with_Added_graph(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_many_to_one_dependents_are_orphaned_with_Added_graph(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_with_Added_graph(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_one_to_one_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_one_to_one_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_one_to_one_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_one_to_one_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_one_to_one_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_one_to_one_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_one_to_one_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_relationships_are_one_to_one(deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_one_to_one_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_relationships_are_one_to_one(deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_one_to_one_with_AK_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_AK_relationships_are_one_to_one(deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_one_to_one_with_AK_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_AK_relationships_are_one_to_one(deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_one_to_one_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_one_to_one_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Re_childing_parent_to_new_child_with_delete(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Re_childing_parent_to_new_child_with_delete(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Re_childing_parent_to_new_child_with_delete(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Re_childing_parent_to_new_child_with_delete(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reparent_dependent_one_to_many(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_dependent_one_to_many(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Reparent_dependent_one_to_many(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_dependent_one_to_many(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reparent_dependent_one_to_many_ak(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_dependent_one_to_many_ak(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Reparent_dependent_one_to_many_ak(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_dependent_one_to_many_ak(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reparent_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_one_to_many_overlapping(changeMechanism, useExistingParent, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Reparent_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_one_to_many_overlapping(changeMechanism, useExistingParent, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reparent_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_optional_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Reparent_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_optional_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reparent_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_optional_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Reparent_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_optional_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reparent_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_required_non_PK_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Reparent_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_required_non_PK_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reparent_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_required_non_PK_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Reparent_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_required_non_PK_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reparent_required_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_required_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Reparent_required_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_required_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reparent_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_required_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Reparent_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_required_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reparent_to_different_one_to_many(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_to_different_one_to_many(changeMechanism, useExistingParent, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Reparent_to_different_one_to_many(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_to_different_one_to_many(changeMechanism, useExistingParent, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_many_to_one_dependent_leaves_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependent_leaves_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_many_to_one_dependent_leaves_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependent_leaves_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_many_to_one_dependents_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_many_to_one_dependents_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_many_to_one_dependents_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_many_to_one_dependents_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_many_to_one_dependents_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_many_to_one_dependents_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_non_PK_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_non_PK_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_non_PK_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_non_PK_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_non_PK_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_non_PK_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_non_PK_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_non_PK_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_relationships_are_one_to_one(deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_relationships_are_one_to_one(deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_with_AK_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_AK_relationships_are_one_to_one(deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_with_AK_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_AK_relationships_are_one_to_one(deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Reset_unknown_original_value_when_current_value_is_set(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reset_unknown_original_value_when_current_value_is_set(async), "The instance of entity type 'Optional");

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Resetting_a_deleted_reference_fixes_up_again()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Resetting_a_deleted_reference_fixes_up_again(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Resetting_a_deleted_reference_fixes_up_again()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Resetting_a_deleted_reference_fixes_up_again(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_changed_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_changed_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_changed_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_changed_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_changed_optional_one_to_one_with_alternate_key_in_store()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one_with_alternate_key_in_store(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_changed_optional_one_to_one_with_alternate_key_in_store()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one_with_alternate_key_in_store(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_changed_owned_one_to_many()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_changed_owned_one_to_many(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_changed_owned_one_to_one()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_changed_owned_one_to_one(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_optional_many_to_one_dependents(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_optional_many_to_one_dependents(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_optional_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_optional_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_removed_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_removed_optional_many_to_one_dependents(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_removed_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_removed_optional_many_to_one_dependents(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_removed_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_removed_optional_many_to_one_dependents_with_alternate_key(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_removed_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_removed_optional_many_to_one_dependents_with_alternate_key(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_removed_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_removed_required_many_to_one_dependents(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_removed_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_removed_required_many_to_one_dependents(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_removed_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_removed_required_many_to_one_dependents_with_alternate_key(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_removed_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_removed_required_many_to_one_dependents_with_alternate_key(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_many_to_one_dependents(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_many_to_one_dependents(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_required_non_PK_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_non_PK_one_to_one_changed_by_reference(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_required_non_PK_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_non_PK_one_to_one_changed_by_reference(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_required_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_one_to_one_changed_by_reference(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_required_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_one_to_one_changed_by_reference(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Save_required_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Save_required_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Saving_multiple_modified_entities_with_the_same_key_does_not_overflow(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Saving_multiple_modified_entities_with_the_same_key_does_not_overflow(async), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Saving_unknown_key_value_marks_it_as_unmodified(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Saving_unknown_key_value_marks_it_as_unmodified(async), "The instance of entity type 'Optional");

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Sever_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_optional_one_to_one(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Sever_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_optional_one_to_one(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Sever_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_optional_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Sever_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_optional_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Sever_relationship_that_will_later_be_deleted(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_relationship_that_will_later_be_deleted(async), "The instance of entity type 'Optional");

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Sever_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_required_non_PK_one_to_one(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Sever_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_required_non_PK_one_to_one(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Sever_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_required_non_PK_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Sever_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_required_non_PK_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Sever_required_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_required_one_to_one(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Sever_required_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_required_one_to_one(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Sever_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_required_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Sever_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_required_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming), "The instance of entity type 'Optional");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Shadow_skip_navigation_in_base_class_is_handled(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Shadow_skip_navigation_in_base_class_is_handled(async), "The instance of entity type 'Optional");

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref(), "The instance of entity type 'Optional");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref(), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Throws_for_single_property_bool_key_with_default_value_generation(bool async, bool initialValue)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Throws_for_single_property_bool_key_with_default_value_generation(async, initialValue), "The instance of entity type 'Optional");
#endif

#if EF10 || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Throws_for_single_property_nullable_bool_key_with_default_value_generation(bool async, bool? initialValue)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Throws_for_single_property_nullable_bool_key_with_default_value_generation(async, initialValue), "The instance of entity type 'Optional");
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Update_principal_with_CLR_key_owned_collection(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_principal_with_CLR_key_owned_collection(async), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Update_principal_with_non_generated_shadow_key_owned_collection_throws(bool async, bool delete)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_principal_with_non_generated_shadow_key_owned_collection_throws(async, delete), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Update_principal_with_shadow_key_owned_collection_throws(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_principal_with_shadow_key_owned_collection_throws(async), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Update_root_by_collection_replacement_of_deleted_first_level(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_root_by_collection_replacement_of_deleted_first_level(async), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Update_root_by_collection_replacement_of_deleted_second_level(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_root_by_collection_replacement_of_deleted_second_level(async), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Update_root_by_collection_replacement_of_deleted_third_level(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_root_by_collection_replacement_of_deleted_third_level(async), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Update_root_by_collection_replacement_of_inserted_first_level(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_root_by_collection_replacement_of_inserted_first_level(async), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Update_root_by_collection_replacement_of_inserted_first_level_level(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_root_by_collection_replacement_of_inserted_first_level_level(async), "The instance of entity type 'Optional");

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Update_root_by_collection_replacement_of_inserted_second_level(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_root_by_collection_replacement_of_inserted_second_level(async), "The instance of entity type 'Optional");
}
