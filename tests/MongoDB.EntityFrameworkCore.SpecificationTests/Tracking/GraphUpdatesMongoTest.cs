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
    : GraphUpdatesTestBase<GraphUpdatesMongoTest.GraphUpdatesMongoFixture>(fixture), IAsyncLifetime
{
    // The tests write to the shared database and there is no rollback, so which test sees which rows depends on the order they run in.
    public async LifetimeTask InitializeAsync()
        => await Fixture.ReseedAsync();

    public LifetimeTask DisposeAsync()
        => LifetimeTask.CompletedTask;

#if EF11
    private readonly object? _ef11Skips = Ef11TestSkips.SkipIfListed("xUnit v3 (EF11) cannot hide this non-virtual upstream test; it fails here for the reasons in EF-X101 (see the New-method override on EF8 to EF10)", "Can_update_many_to_many_and_reference_with_composite_key", "ClientSetDefault_with_sentinel_value_sets_FK_to_sentinel_on_delete");
#endif

    public class GraphUpdatesMongoFixture : GraphUpdatesFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("GraphUpdates");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ShadowForeignKeyPropertyCreated));

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // EF-X102: the provider owns, by convention, any type it reaches through a navigation that is not yet in the model, which breaks the
            // upstream relationships; here every such type is an independent entity type.
            configurationBuilder.DoNotOwnTypes((_, _) => true);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
            modelBuilder.RelaxUniqueForeignKeyIndexes();
        }
    }

    public override Task Alternate_key_over_foreign_key_doesnt_bypass_delete_behavior(bool async)
        => base.Alternate_key_over_foreign_key_doesnt_bypass_delete_behavior(async);

#if (EF10 || EF11) || EF9
    public override Task Avoid_nulling_shared_FK_property_when_deleting()
        => base.Avoid_nulling_shared_FK_property_when_deleting();
#elif EF8
    public override void Avoid_nulling_shared_FK_property_when_deleting()
        => base.Avoid_nulling_shared_FK_property_when_deleting();
#endif

#if (EF10 || EF11) || EF9
    public override Task Avoid_nulling_shared_FK_property_when_nulling_navigation(bool nullPrincipal)
        => base.Avoid_nulling_shared_FK_property_when_nulling_navigation(nullPrincipal);
#elif EF8
    public override void Avoid_nulling_shared_FK_property_when_nulling_navigation(bool nullPrincipal)
        => base.Avoid_nulling_shared_FK_property_when_nulling_navigation(nullPrincipal);
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_add_multiple_dependents_when_multiple_possible_principal_sides()
        => base.Can_add_multiple_dependents_when_multiple_possible_principal_sides();
#elif EF8
    public override void Can_add_multiple_dependents_when_multiple_possible_principal_sides()
        => base.Can_add_multiple_dependents_when_multiple_possible_principal_sides();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_add_valid_first_dependent_when_multiple_possible_principal_sides()
        => base.Can_add_valid_first_dependent_when_multiple_possible_principal_sides();
#elif EF8
    public override void Can_add_valid_first_dependent_when_multiple_possible_principal_sides()
        => base.Can_add_valid_first_dependent_when_multiple_possible_principal_sides();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_add_valid_second_dependent_when_multiple_possible_principal_sides()
        => base.Can_add_valid_second_dependent_when_multiple_possible_principal_sides();
#elif EF8
    public override void Can_add_valid_second_dependent_when_multiple_possible_principal_sides()
        => base.Can_add_valid_second_dependent_when_multiple_possible_principal_sides();
#endif

    [ConditionalTheory(Skip = "The upstream model for this test (the Cruiser entity types with a default-value foreign key) is registered only by the relational fixtures, so it does not exist in this fixture and the test cannot run. EF-X118")]
    [InlineData(false)]
    [InlineData(true)]
    public override Task Can_insert_when_FK_has_default_value(bool async)
        => base.Can_insert_when_FK_has_default_value(async);

    [ConditionalTheory(Skip = "The upstream model for this test (the Cruiser entity types with a default-value foreign key) is registered only by the relational fixtures, so it does not exist in this fixture and the test cannot run. EF-X118")]
    [InlineData(false)]
    [InlineData(true)]
    public override Task Can_insert_when_FK_has_sentinel_value(bool async)
        => base.Can_insert_when_FK_has_sentinel_value(async);

#if (EF10 || EF11) || EF9
    public override Task Can_insert_when_bool_PK_in_composite_key_has_sentinel_value(bool async, bool initialValue)
        => base.Can_insert_when_bool_PK_in_composite_key_has_sentinel_value(async, initialValue);
#endif

    [ConditionalTheory(Skip = "The upstream model for this test (the Cruiser entity types with a default-value foreign key) is registered only by the relational fixtures, so it does not exist in this fixture and the test cannot run. EF-X118")]
    [InlineData(false)]
    [InlineData(true)]
    public override Task Can_insert_when_composite_FK_has_default_value_for_one_part(bool async)
        => base.Can_insert_when_composite_FK_has_default_value_for_one_part(async);

#if (EF10 || EF11) || EF9
    public override Task Can_insert_when_int_PK_in_composite_key_has_sentinel_value(bool async, int initialValue)
        => base.Can_insert_when_int_PK_in_composite_key_has_sentinel_value(async, initialValue);
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_insert_when_nullable_bool_PK_in_composite_key_has_sentinel_value(bool async, bool? initialValue)
        => base.Can_insert_when_nullable_bool_PK_in_composite_key_has_sentinel_value(async, initialValue);
#endif

#if (EF10 || EF11) || EF9
    public override Task Changes_to_Added_relationships_are_picked_up(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Changes_to_Added_relationships_are_picked_up(changeMechanism);
#elif EF8
    public override void Changes_to_Added_relationships_are_picked_up(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Changes_to_Added_relationships_are_picked_up(changeMechanism);
#endif

#if EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Clearing_CLR_key_owned_collection(bool async, bool useUpdate, bool addNew)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Clearing_CLR_key_owned_collection(async, useUpdate, addNew));
#elif EF9
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Clearing_CLR_key_owned_collection(bool async, bool useUpdate, bool addNew)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Clearing_CLR_key_owned_collection(async, useUpdate, addNew));
#elif EF10
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Clearing_CLR_key_owned_collection(bool async, bool useUpdate, bool addNew)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Clearing_CLR_key_owned_collection(async, useUpdate, addNew));
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Clearing_CLR_key_owned_collection(bool async, bool useUpdate, bool addNew)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Clearing_CLR_key_owned_collection(async, useUpdate, addNew));
#endif

    public override Task Clearing_shadow_key_owned_collection_throws(bool async, bool useUpdate, bool addNew)
        => base.Clearing_shadow_key_owned_collection_throws(async, useUpdate, addNew);

#if EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Delete_principal_with_CLR_key_owned_collection(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Delete_principal_with_CLR_key_owned_collection(async));
#elif EF9
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Delete_principal_with_CLR_key_owned_collection(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Delete_principal_with_CLR_key_owned_collection(async));
#elif EF10
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Delete_principal_with_CLR_key_owned_collection(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Delete_principal_with_CLR_key_owned_collection(async));
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Delete_principal_with_CLR_key_owned_collection(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Delete_principal_with_CLR_key_owned_collection(async));
#endif

    public override Task Delete_principal_with_shadow_key_owned_collection_throws(bool async)
        => base.Delete_principal_with_shadow_key_owned_collection_throws(async);

#if (EF10 || EF11) || EF9
    public override Task Detaching_dependent_entity_will_not_remove_references_to_it()
        => base.Detaching_dependent_entity_will_not_remove_references_to_it();
#elif EF8
    public override void Detaching_dependent_entity_will_not_remove_references_to_it()
        => base.Detaching_dependent_entity_will_not_remove_references_to_it();
#endif

#if (EF10 || EF11) || EF9
    public override Task Detaching_principal_entity_will_remove_references_to_it()
        => base.Detaching_principal_entity_will_remove_references_to_it();
#elif EF8
    public override void Detaching_principal_entity_will_remove_references_to_it()
        => base.Detaching_principal_entity_will_remove_references_to_it();
#endif

#if (EF10 || EF11)
#if EF10
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Discriminator_values_are_not_marked_as_unknown(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Discriminator_values_are_not_marked_as_unknown(async));
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Discriminator_values_are_not_marked_as_unknown(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Discriminator_values_are_not_marked_as_unknown(async));
#endif
#elif EF9 || EF8
#if !EF11
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task Discriminator_values_are_not_marked_as_unknown(bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
    public override Task Mark_explicitly_set_dependent_appropriately_with_any_inheritance_and_stable_generator(bool async, bool useAdd)
        => base.Mark_explicitly_set_dependent_appropriately_with_any_inheritance_and_stable_generator(async, useAdd);
#endif

#if (EF10 || EF11) || EF9
    public override Task Mark_explicitly_set_stable_dependent_appropriately(bool async, bool useAdd)
        => base.Mark_explicitly_set_stable_dependent_appropriately(async, useAdd);
#endif

#if (EF10 || EF11) || EF9
    public override Task Mark_explicitly_set_stable_dependent_appropriately_when_deep_in_graph(bool async, bool useAdd)
        => base.Mark_explicitly_set_stable_dependent_appropriately_when_deep_in_graph(async, useAdd);
#endif

#if (EF10 || EF11) || EF9
    public override Task Mark_modified_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Mark_modified_one_to_many_overlapping(changeMechanism, deleteOrphansTiming);
#elif EF8
    public override void Mark_modified_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Mark_modified_one_to_many_overlapping(changeMechanism, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Mutating_discriminator_value_can_be_configured_to_allow_mutation()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Mutating_discriminator_value_can_be_configured_to_allow_mutation());
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Mutating_discriminator_value_can_be_configured_to_allow_mutation()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Mutating_discriminator_value_can_be_configured_to_allow_mutation());
#endif

#if (EF10 || EF11) || EF9
    public override Task Mutating_discriminator_value_throws_by_convention()
        => base.Mutating_discriminator_value_throws_by_convention();
#elif EF8
    public override void Mutating_discriminator_value_throws_by_convention()
        => base.Mutating_discriminator_value_throws_by_convention();
#endif

#if (EF10 || EF11) || EF9
    public override Task New_FK_is_not_cleared_on_old_dependent_delete(bool loadNewParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.New_FK_is_not_cleared_on_old_dependent_delete(loadNewParent, deleteOrphansTiming);
#elif EF8
    public override void New_FK_is_not_cleared_on_old_dependent_delete(bool loadNewParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.New_FK_is_not_cleared_on_old_dependent_delete(loadNewParent, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task No_fixup_to_Deleted_entities(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.No_fixup_to_Deleted_entities(deleteOrphansTiming);
#elif EF8
    public override void No_fixup_to_Deleted_entities(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.No_fixup_to_Deleted_entities(deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Notification_entities_can_have_indexes()
        => base.Notification_entities_can_have_indexes();
#elif EF8
    public override void Notification_entities_can_have_indexes()
        => base.Notification_entities_can_have_indexes();
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_many_to_one_dependent_leaves_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependent_leaves_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_many_to_one_dependent_leaves_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependent_leaves_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_many_to_one_dependents_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_many_to_one_dependents_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_many_to_one_dependents_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.TrueException>(() => base.Optional_many_to_one_dependents_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Optional_many_to_one_dependents_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.TrueException>(() => base.Optional_many_to_one_dependents_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_many_to_one_dependents_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_many_to_one_dependents_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_many_to_one_dependents_are_orphaned_with_Added_graph(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_are_orphaned_with_Added_graph(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_many_to_one_dependents_are_orphaned_with_Added_graph(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_are_orphaned_with_Added_graph(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.TrueException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.TrueException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_one_to_one_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_one_to_one_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_one_to_one_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_one_to_one_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
#if EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_one_to_one_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.NullException>(() => base.Optional_one_to_one_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_one_to_one_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.NullException>(() => base.Optional_one_to_one_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_one_to_one_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.NullException>(() => base.Optional_one_to_one_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Optional_one_to_one_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.NullException>(() => base.Optional_one_to_one_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_one_to_one_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_one_to_one_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_one_to_one_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_one_to_one_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Optional_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_one_to_one_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.ThrowsException>(() => base.Optional_one_to_one_relationships_are_one_to_one(deleteOrphansTiming));
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Optional_one_to_one_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.ThrowsException>(() => base.Optional_one_to_one_relationships_are_one_to_one(deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_one_to_one_with_AK_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.ThrowsException>(() => base.Optional_one_to_one_with_AK_relationships_are_one_to_one(deleteOrphansTiming));
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Optional_one_to_one_with_AK_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.ThrowsException>(() => base.Optional_one_to_one_with_AK_relationships_are_one_to_one(deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Optional_one_to_one_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Optional_one_to_one_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Re_childing_parent_to_new_child_with_delete(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Re_childing_parent_to_new_child_with_delete(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Re_childing_parent_to_new_child_with_delete(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Re_childing_parent_to_new_child_with_delete(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_dependent_one_to_many(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_dependent_one_to_many(changeMechanism, deleteOrphansTiming);
#elif EF8
    public override void Reparent_dependent_one_to_many(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_dependent_one_to_many(changeMechanism, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_dependent_one_to_many_ak(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_dependent_one_to_many_ak(changeMechanism, deleteOrphansTiming);
#elif EF8
    public override void Reparent_dependent_one_to_many_ak(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_dependent_one_to_many_ak(changeMechanism, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_one_to_many_overlapping(changeMechanism, useExistingParent, deleteOrphansTiming);
#elif EF8
    public override void Reparent_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_one_to_many_overlapping(changeMechanism, useExistingParent, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_optional_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming);
#elif EF8
    public override void Reparent_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_optional_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Reparent_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Reparent_optional_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Reparent_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Reparent_optional_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_required_non_PK_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming);
#elif EF8
    public override void Reparent_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_required_non_PK_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_required_non_PK_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming);
#elif EF8
    public override void Reparent_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_required_non_PK_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_required_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_required_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming);
#elif EF8
    public override void Reparent_required_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_required_one_to_one(changeMechanism, useExistingRoot, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Reparent_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Reparent_required_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Reparent_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Reparent_required_one_to_one_with_alternate_key(changeMechanism, useExistingRoot, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_to_different_one_to_many(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_to_different_one_to_many(changeMechanism, useExistingParent, deleteOrphansTiming);
#elif EF8
    public override void Reparent_to_different_one_to_many(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Reparent_to_different_one_to_many(changeMechanism, useExistingParent, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependent_leaves_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependent_leaves_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependent_leaves_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependent_leaves_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
#if EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
#if EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
#if EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_non_PK_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_non_PK_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_non_PK_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_non_PK_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
#if EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
#if EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.EmptyException>(() => base.Required_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_one_to_one_leaf_can_be_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Required_one_to_one_leaf_can_be_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_one_to_one_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.ThrowsException>(() => base.Required_one_to_one_relationships_are_one_to_one(deleteOrphansTiming));
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_one_to_one_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.ThrowsException>(() => base.Required_one_to_one_relationships_are_one_to_one(deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_one_to_one_with_AK_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.ThrowsException>(() => base.Required_one_to_one_with_AK_relationships_are_one_to_one(deleteOrphansTiming));
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_one_to_one_with_AK_relationships_are_one_to_one(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.ThrowsException>(() => base.Required_one_to_one_with_AK_relationships_are_one_to_one(deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    // Fails: Argument exception while translating or materializing EF-X603
    public override Task Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.ArgumentNullException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Argument exception while translating or materializing EF-X603
    public override void Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.ArgumentNullException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming));
#endif

    public override Task Reset_unknown_original_value_when_current_value_is_set(bool async)
        => base.Reset_unknown_original_value_when_current_value_is_set(async);

#if (EF10 || EF11) || EF9
    public override Task Resetting_a_deleted_reference_fixes_up_again()
        => base.Resetting_a_deleted_reference_fixes_up_again();
#elif EF8
    public override void Resetting_a_deleted_reference_fixes_up_again()
        => base.Resetting_a_deleted_reference_fixes_up_again();
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_changed_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_changed_optional_one_to_one(changeMechanism, useExistingEntities, deleteOrphansTiming);
#elif EF8
    public override void Save_changed_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_changed_optional_one_to_one(changeMechanism, useExistingEntities, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Save_changed_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Save_changed_optional_one_to_one_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Save_changed_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Save_changed_optional_one_to_one_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Save_changed_optional_one_to_one_with_alternate_key_in_store()
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Save_changed_optional_one_to_one_with_alternate_key_in_store());
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Save_changed_optional_one_to_one_with_alternate_key_in_store()
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Save_changed_optional_one_to_one_with_alternate_key_in_store());
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_changed_owned_one_to_many()
        => base.Save_changed_owned_one_to_many();
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_changed_owned_one_to_one()
        => base.Save_changed_owned_one_to_one();
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_optional_many_to_one_dependents(changeMechanism, useExistingEntities, deleteOrphansTiming);
#elif EF8
    public override void Save_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_optional_many_to_one_dependents(changeMechanism, useExistingEntities, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Save_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Save_optional_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Save_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Save_optional_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_removed_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_removed_optional_many_to_one_dependents(changeMechanism, deleteOrphansTiming);
#elif EF8
    public override void Save_removed_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_removed_optional_many_to_one_dependents(changeMechanism, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Save_removed_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Save_removed_optional_many_to_one_dependents_with_alternate_key(changeMechanism, deleteOrphansTiming), string.Empty);
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Save_removed_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Save_removed_optional_many_to_one_dependents_with_alternate_key(changeMechanism, deleteOrphansTiming), string.Empty);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_removed_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_removed_required_many_to_one_dependents(changeMechanism, deleteOrphansTiming);
#elif EF8
    public override void Save_removed_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_removed_required_many_to_one_dependents(changeMechanism, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Save_removed_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Save_removed_required_many_to_one_dependents_with_alternate_key(changeMechanism, deleteOrphansTiming), string.Empty);
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Save_removed_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Save_removed_required_many_to_one_dependents_with_alternate_key(changeMechanism, deleteOrphansTiming), string.Empty);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_required_many_to_one_dependents(changeMechanism, useExistingEntities, deleteOrphansTiming);
#elif EF8
    public override void Save_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_required_many_to_one_dependents(changeMechanism, useExistingEntities, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Save_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Save_required_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Save_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Save_required_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_required_non_PK_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_required_non_PK_one_to_one_changed_by_reference(changeMechanism, useExistingEntities, deleteOrphansTiming);
#elif EF8
    public override void Save_required_non_PK_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_required_non_PK_one_to_one_changed_by_reference(changeMechanism, useExistingEntities, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming);
#elif EF8
    public override void Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_required_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_required_one_to_one_changed_by_reference(changeMechanism, deleteOrphansTiming);
#elif EF8
    public override void Save_required_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Save_required_one_to_one_changed_by_reference(changeMechanism, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Save_required_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => deleteOrphansTiming == global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming.Never
            ? base.Save_required_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming)
            : SpecFails.ThrowsAsync(() => base.Save_required_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Save_required_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
    {
        if (deleteOrphansTiming == global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming.Never)
        {
            base.Save_required_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming);
        }
        else
        {
            SpecFails.Throws(() => base.Save_required_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities, deleteOrphansTiming));
        }
    }
#endif

    public override Task Saving_multiple_modified_entities_with_the_same_key_does_not_overflow(bool async)
        => base.Saving_multiple_modified_entities_with_the_same_key_does_not_overflow(async);

    public override Task Saving_unknown_key_value_marks_it_as_unmodified(bool async)
        => base.Saving_unknown_key_value_marks_it_as_unmodified(async);

#if (EF10 || EF11) || EF9
    public override Task Sever_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Sever_optional_one_to_one(changeMechanism, deleteOrphansTiming);
#elif EF8
    public override void Sever_optional_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Sever_optional_one_to_one(changeMechanism, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Sever_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.NullReferenceException>(() => base.Sever_optional_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming));
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Sever_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.NullReferenceException>(() => base.Sever_optional_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming));
#endif

    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Sever_relationship_that_will_later_be_deleted(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_relationship_that_will_later_be_deleted(async));

#if (EF10 || EF11) || EF9
    public override Task Sever_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Sever_required_non_PK_one_to_one(changeMechanism, deleteOrphansTiming);
#elif EF8
    public override void Sever_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Sever_required_non_PK_one_to_one(changeMechanism, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Sever_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Sever_required_non_PK_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming);
#elif EF8
    public override void Sever_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Sever_required_non_PK_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Sever_required_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Sever_required_one_to_one(changeMechanism, deleteOrphansTiming);
#elif EF8
    public override void Sever_required_one_to_one(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => base.Sever_required_one_to_one(changeMechanism, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Sever_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Sever_required_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming), string.Empty);
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Sever_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.GraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.GraphUpdatesMongoTest.GraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Sever_required_one_to_one_with_alternate_key(changeMechanism, deleteOrphansTiming), string.Empty);
#endif

    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Shadow_skip_navigation_in_base_class_is_handled(bool async)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Shadow_skip_navigation_in_base_class_is_handled(async));

#if (EF10 || EF11) || EF9
    public override Task Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref()
        => base.Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref();
#elif EF8
    public override void Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref()
        => base.Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref();
#endif

#if (EF10 || EF11) || EF9
    public override Task Throws_for_single_property_bool_key_with_default_value_generation(bool async, bool initialValue)
        => base.Throws_for_single_property_bool_key_with_default_value_generation(async, initialValue);
#endif

#if (EF10 || EF11) || EF9
    public override Task Throws_for_single_property_nullable_bool_key_with_default_value_generation(bool async, bool? initialValue)
        => base.Throws_for_single_property_nullable_bool_key_with_default_value_generation(async, initialValue);
#endif

#if EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Update_principal_with_CLR_key_owned_collection(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_principal_with_CLR_key_owned_collection(async));
#elif EF9
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Update_principal_with_CLR_key_owned_collection(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_principal_with_CLR_key_owned_collection(async));
#elif EF10
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Update_principal_with_CLR_key_owned_collection(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_principal_with_CLR_key_owned_collection(async));
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Update_principal_with_CLR_key_owned_collection(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Update_principal_with_CLR_key_owned_collection(async));
#endif

#if EF8 || EF9 || EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Update_principal_with_non_generated_shadow_key_owned_collection_throws(bool async, bool delete)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Update_principal_with_non_generated_shadow_key_owned_collection_throws(async, delete));
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Update_principal_with_non_generated_shadow_key_owned_collection_throws(bool async, bool delete)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Update_principal_with_non_generated_shadow_key_owned_collection_throws(async, delete));
#endif

    public override Task Update_principal_with_shadow_key_owned_collection_throws(bool async)
        => base.Update_principal_with_shadow_key_owned_collection_throws(async);

    public override Task Update_root_by_collection_replacement_of_deleted_first_level(bool async)
        => base.Update_root_by_collection_replacement_of_deleted_first_level(async);

#if EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_deleted_second_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_deleted_second_level(async));
#elif EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_deleted_second_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_deleted_second_level(async));
#elif EF10
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_deleted_second_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_deleted_second_level(async));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_deleted_second_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_deleted_second_level(async));
#endif

#if EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_deleted_third_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_deleted_third_level(async));
#elif EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_deleted_third_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_deleted_third_level(async));
#elif EF10
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_deleted_third_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_deleted_third_level(async));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_deleted_third_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_deleted_third_level(async));
#endif

#if EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_first_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_first_level(async));
#elif EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_first_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_first_level(async));
#elif EF10
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_first_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_first_level(async));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_first_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_first_level(async));
#endif

#if EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_first_level_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_first_level_level(async));
#elif EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_first_level_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_first_level_level(async));
#elif EF10
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_first_level_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_first_level_level(async));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_first_level_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_first_level_level(async));
#endif

#if EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_second_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_second_level(async));
#elif EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_second_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_second_level(async));
#elif EF10
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_second_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_second_level(async));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Update_root_by_collection_replacement_of_inserted_second_level(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Update_root_by_collection_replacement_of_inserted_second_level(async));
#endif
}
