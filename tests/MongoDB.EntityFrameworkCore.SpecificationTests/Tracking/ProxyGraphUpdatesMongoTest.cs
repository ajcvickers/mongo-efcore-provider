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

public class ProxyGraphUpdatesMongoTest(ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture fixture)
    : ProxyGraphUpdatesTestBase<ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>(fixture)
{
    protected override bool DoesLazyLoading => true;

    protected override bool DoesChangeTracking => true;

    public class ProxyGraphUpdatesMongoFixture : ProxyGraphUpdatesFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("ProxyGraphUpdates");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override DbContext CreateContext()
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

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Avoid_nulling_shared_FK_property_when_deleting()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Avoid_nulling_shared_FK_property_when_deleting(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Avoid_nulling_shared_FK_property_when_deleting()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Avoid_nulling_shared_FK_property_when_deleting(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Avoid_nulling_shared_FK_property_when_nulling_navigation(bool nullPrincipal)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Avoid_nulling_shared_FK_property_when_nulling_navigation(nullPrincipal), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Avoid_nulling_shared_FK_property_when_nulling_navigation(bool nullPrincipal)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Avoid_nulling_shared_FK_property_when_nulling_navigation(nullPrincipal), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_optional_AK_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_optional_AK_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_optional_AK_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_optional_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_optional_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_optional_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_AK_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_AK_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_AK_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_composite_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_composite_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_composite_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_non_PK_AK_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_non_PK_AK_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_non_PK_AK_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_non_PK_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_non_PK_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_non_PK_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_one_to_many_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_one_to_many_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_one_to_many_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_use_record_proxies_with_base_types_to_load_collection()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_use_record_proxies_with_base_types_to_load_collection(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Can_use_record_proxies_with_base_types_to_load_collection()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_use_record_proxies_with_base_types_to_load_collection(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_use_record_proxies_with_base_types_to_load_reference()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_use_record_proxies_with_base_types_to_load_reference(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Can_use_record_proxies_with_base_types_to_load_reference()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_use_record_proxies_with_base_types_to_load_reference(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task No_fixup_to_Deleted_entities()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.No_fixup_to_Deleted_entities(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void No_fixup_to_Deleted_entities()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.No_fixup_to_Deleted_entities(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_many_to_one_dependents_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_many_to_one_dependents_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_many_to_one_dependents_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_many_to_one_dependents_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_many_to_one_dependents_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_many_to_one_dependents_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_many_to_one_dependents_are_orphaned_with_Added_graph(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_with_Added_graph(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_many_to_one_dependents_are_orphaned_with_Added_graph(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming? deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_are_orphaned_with_Added_graph(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_one_to_one_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_one_to_one_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_one_to_one_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_one_to_one_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_one_to_one_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_one_to_one_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_one_to_one_relationships_are_one_to_one()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_relationships_are_one_to_one(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_one_to_one_relationships_are_one_to_one()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_relationships_are_one_to_one(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_one_to_one_with_AK_relationships_are_one_to_one()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_AK_relationships_are_one_to_one(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_one_to_one_with_AK_relationships_are_one_to_one()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_AK_relationships_are_one_to_one(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_one_to_one_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_one_to_one_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Reparent_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_one_to_many_overlapping(changeMechanism, useExistingParent), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Reparent_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_one_to_many_overlapping(changeMechanism, useExistingParent), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Reparent_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_optional_one_to_one(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Reparent_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_optional_one_to_one(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Reparent_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_optional_one_to_one_with_alternate_key(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Reparent_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_optional_one_to_one_with_alternate_key(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Reparent_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_required_non_PK_one_to_one(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Reparent_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_required_non_PK_one_to_one(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Reparent_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_required_non_PK_one_to_one_with_alternate_key(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Reparent_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_required_non_PK_one_to_one_with_alternate_key(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Reparent_required_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_required_one_to_one(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Reparent_required_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_required_one_to_one(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Reparent_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_required_one_to_one_with_alternate_key(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Reparent_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_required_one_to_one_with_alternate_key(changeMechanism, useExistingRoot), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Reparent_to_different_one_to_many(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Reparent_to_different_one_to_many(changeMechanism, useExistingParent), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Reparent_to_different_one_to_many(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Reparent_to_different_one_to_many(changeMechanism, useExistingParent), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_many_to_one_dependents_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_many_to_one_dependents_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_many_to_one_dependents_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_many_to_one_dependents_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_many_to_one_dependents_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_many_to_one_dependents_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_non_PK_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_non_PK_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_non_PK_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_non_PK_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_non_PK_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_non_PK_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_one_to_one_relationships_are_one_to_one()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_relationships_are_one_to_one(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_one_to_one_relationships_are_one_to_one()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_relationships_are_one_to_one(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_one_to_one_with_AK_relationships_are_one_to_one()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_AK_relationships_are_one_to_one(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_one_to_one_with_AK_relationships_are_one_to_one()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_AK_relationships_are_one_to_one(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_changed_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_changed_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_changed_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one_with_alternate_key(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_changed_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one_with_alternate_key(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_changed_optional_one_to_one_with_alternate_key_in_store()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one_with_alternate_key_in_store(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_changed_optional_one_to_one_with_alternate_key_in_store()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_changed_optional_one_to_one_with_alternate_key_in_store(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_optional_many_to_one_dependents(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_optional_many_to_one_dependents(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_optional_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_optional_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_removed_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_removed_optional_many_to_one_dependents(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_removed_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_removed_optional_many_to_one_dependents(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_removed_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_removed_optional_many_to_one_dependents_with_alternate_key(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_removed_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_removed_optional_many_to_one_dependents_with_alternate_key(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_removed_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_removed_required_many_to_one_dependents(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_removed_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_removed_required_many_to_one_dependents(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_removed_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_removed_required_many_to_one_dependents_with_alternate_key(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_removed_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_removed_required_many_to_one_dependents_with_alternate_key(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_many_to_one_dependents(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_many_to_one_dependents(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_required_non_PK_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_non_PK_one_to_one_changed_by_reference(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_required_non_PK_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_non_PK_one_to_one_changed_by_reference(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_required_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_one_to_one_changed_by_reference(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_required_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_one_to_one_changed_by_reference(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_required_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_required_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_required_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_required_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Save_two_entity_cycle_with_lazy_loading()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Save_two_entity_cycle_with_lazy_loading(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Save_two_entity_cycle_with_lazy_loading()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Save_two_entity_cycle_with_lazy_loading(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Sever_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_optional_one_to_one(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Sever_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_optional_one_to_one(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Sever_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_optional_one_to_one_with_alternate_key(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Sever_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_optional_one_to_one_with_alternate_key(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Sever_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_required_non_PK_one_to_one(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Sever_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_required_non_PK_one_to_one(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Sever_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_required_non_PK_one_to_one_with_alternate_key(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Sever_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_required_non_PK_one_to_one_with_alternate_key(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Sever_required_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_required_one_to_one(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Sever_required_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_required_one_to_one(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Sever_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sever_required_one_to_one_with_alternate_key(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Sever_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sever_required_one_to_one_with_alternate_key(changeMechanism), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif
}
