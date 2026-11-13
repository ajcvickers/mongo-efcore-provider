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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Tracking;

public class ProxyGraphUpdatesMongoTest(ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture fixture)
    : ProxyGraphUpdatesTestBase<ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>(fixture)
{
    // EF isolates each test by beginning a transaction on one context and never committing it. A second context the test creates joins
    // that transaction through this hook (#77, #87); it only participates, so disposing it does not end the transaction. Nothing is
    // reseeded: the rolled-back transaction leaves the fixture's seed data and the model's indexes as they were.
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction);

#if EF11
    private readonly object? _ef11Skips = Ef11TestSkips.SkipIfListed("xUnit v3 (EF11) cannot hide this non-virtual upstream test; it fails here for the reasons in EF-X102 (see the New-method override on EF8 to EF10)", "Can_attach_full_optional_graph_of_duplicates", "Can_attach_full_required_AK_graph_of_duplicates", "Can_attach_full_required_composite_graph_of_duplicates", "Can_attach_full_required_graph_of_duplicates", "Can_attach_full_required_non_PK_AK_graph_of_duplicates", "Can_attach_full_required_non_PK_graph_of_duplicates", "Can_attach_full_required_one_to_many_graph_of_duplicates");
#endif

    protected override bool DoesLazyLoading => true;

    protected override bool DoesChangeTracking => true;

    public class ProxyGraphUpdatesMongoFixture : ProxyGraphUpdatesFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("ProxyGraphUpdates");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
            => base.AddServices(serviceCollection).AddEntityFrameworkProxies();

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder)
                .UseLazyLoadingProxies()
                .UseChangeTrackingProxies()
#if EF11
                // EF11 reports this model validation warning before the relationship errors the other versions hit first.
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ShadowForeignKeyPropertyCreated))
#endif
                ;

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // EF-X102: the provider owns, by convention, any type it reaches through a navigation that is not yet in the model.
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
    public override Task Can_attach_full_optional_AK_graph_of_duplicates()
        => base.Can_attach_full_optional_AK_graph_of_duplicates();
#elif EF8
#if !EF11
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_optional_AK_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
#if EF9 || EF10
    public override Task Can_attach_full_optional_graph_of_duplicates()
        => base.Can_attach_full_optional_graph_of_duplicates();
#else
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_optional_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_optional_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif
#elif EF8
#if !EF11
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_optional_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
#if EF9 || EF10
    public override Task Can_attach_full_required_AK_graph_of_duplicates()
        => base.Can_attach_full_required_AK_graph_of_duplicates();
#else
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_AK_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_AK_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif
#elif EF8
#if !EF11
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_AK_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
#if EF9 || EF10
    public override Task Can_attach_full_required_composite_graph_of_duplicates()
        => base.Can_attach_full_required_composite_graph_of_duplicates();
#else
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_composite_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_composite_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif
#elif EF8
#if !EF11
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_composite_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
#if EF9 || EF10
    public override Task Can_attach_full_required_graph_of_duplicates()
        => base.Can_attach_full_required_graph_of_duplicates();
#else
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif
#elif EF8
#if !EF11
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
#if EF9 || EF10
    public override Task Can_attach_full_required_non_PK_AK_graph_of_duplicates()
        => base.Can_attach_full_required_non_PK_AK_graph_of_duplicates();
#else
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_non_PK_AK_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_non_PK_AK_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif
#elif EF8
#if !EF11
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_non_PK_AK_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
#if EF9 || EF10
    public override Task Can_attach_full_required_non_PK_graph_of_duplicates()
        => base.Can_attach_full_required_non_PK_graph_of_duplicates();
#else
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_non_PK_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_non_PK_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif
#elif EF8
#if !EF11
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_non_PK_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
#if EF9 || EF10
    public override Task Can_attach_full_required_one_to_many_graph_of_duplicates()
        => base.Can_attach_full_required_one_to_many_graph_of_duplicates();
#else
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Can_attach_full_required_one_to_many_graph_of_duplicates()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_attach_full_required_one_to_many_graph_of_duplicates(), "Unable to determine the relationship represented by navigation 'RecordCar.Owner' of type '");
#endif
#elif EF8
#if !EF11
    // Fails: Relationship discovery between root entity types fails at model build EF-X102 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X102")]
    public new void Can_attach_full_required_one_to_many_graph_of_duplicates()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
#if EF9 || EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Can_use_record_proxies_with_base_types_to_load_collection()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Can_use_record_proxies_with_base_types_to_load_collection());
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Can_use_record_proxies_with_base_types_to_load_collection()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Can_use_record_proxies_with_base_types_to_load_collection());
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Can_use_record_proxies_with_base_types_to_load_collection()
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Can_use_record_proxies_with_base_types_to_load_collection());
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_use_record_proxies_with_base_types_to_load_reference()
        => base.Can_use_record_proxies_with_base_types_to_load_reference();
#elif EF8
    public override void Can_use_record_proxies_with_base_types_to_load_reference()
        => base.Can_use_record_proxies_with_base_types_to_load_reference();
#endif

#if (EF10 || EF11) || EF9
    public override Task No_fixup_to_Deleted_entities()
        => base.No_fixup_to_Deleted_entities();
#elif EF8
    public override void No_fixup_to_Deleted_entities()
        => base.No_fixup_to_Deleted_entities();
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_many_to_one_dependents_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_many_to_one_dependents_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_many_to_one_dependents_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.TrueException>(() => base.Optional_many_to_one_dependents_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Optional_many_to_one_dependents_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.TrueException>(() => base.Optional_many_to_one_dependents_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_many_to_one_dependents_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_many_to_one_dependents_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
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
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.TrueException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.TrueException>(() => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_one_to_one_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_one_to_one_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_one_to_one_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_one_to_one_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_one_to_one_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_one_to_one_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_one_to_one_relationships_are_one_to_one()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.ThrowsException>(() => base.Optional_one_to_one_relationships_are_one_to_one());
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Optional_one_to_one_relationships_are_one_to_one()
        => SpecFails.Throws<global::Xunit.Sdk.ThrowsException>(() => base.Optional_one_to_one_relationships_are_one_to_one());
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Optional_one_to_one_with_AK_relationships_are_one_to_one()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.ThrowsException>(() => base.Optional_one_to_one_with_AK_relationships_are_one_to_one());
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Optional_one_to_one_with_AK_relationships_are_one_to_one()
        => SpecFails.Throws<global::Xunit.Sdk.ThrowsException>(() => base.Optional_one_to_one_with_AK_relationships_are_one_to_one());
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_one_to_one_with_alternate_key_are_orphaned(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_with_alternate_key_are_orphaned(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_one_to_one_with_alternate_key_are_orphaned_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Optional_one_to_one_with_alternate_key_are_orphaned_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent)
        => base.Reparent_one_to_many_overlapping(changeMechanism, useExistingParent);
#elif EF8
    public override void Reparent_one_to_many_overlapping(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent)
        => base.Reparent_one_to_many_overlapping(changeMechanism, useExistingParent);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_optional_one_to_one(changeMechanism, useExistingRoot);
#elif EF8
    public override void Reparent_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_optional_one_to_one(changeMechanism, useExistingRoot);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_optional_one_to_one_with_alternate_key(changeMechanism, useExistingRoot);
#elif EF8
    public override void Reparent_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_optional_one_to_one_with_alternate_key(changeMechanism, useExistingRoot);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_required_non_PK_one_to_one(changeMechanism, useExistingRoot);
#elif EF8
    public override void Reparent_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_required_non_PK_one_to_one(changeMechanism, useExistingRoot);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_required_non_PK_one_to_one_with_alternate_key(changeMechanism, useExistingRoot);
#elif EF8
    public override void Reparent_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_required_non_PK_one_to_one_with_alternate_key(changeMechanism, useExistingRoot);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_required_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_required_one_to_one(changeMechanism, useExistingRoot);
#elif EF8
    public override void Reparent_required_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_required_one_to_one(changeMechanism, useExistingRoot);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_required_one_to_one_with_alternate_key(changeMechanism, useExistingRoot);
#elif EF8
    public override void Reparent_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingRoot)
        => base.Reparent_required_one_to_one_with_alternate_key(changeMechanism, useExistingRoot);
#endif

#if (EF10 || EF11) || EF9
    public override Task Reparent_to_different_one_to_many(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent)
        => base.Reparent_to_different_one_to_many(changeMechanism, useExistingParent);
#elif EF8
    public override void Reparent_to_different_one_to_many(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingParent)
        => base.Reparent_to_different_one_to_many(changeMechanism, useExistingParent);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
#if EF9 || EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_many_to_one_dependents_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
#if EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#elif EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.Throws<global::Xunit.Sdk.EmptyException>(() => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming));
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Required_non_PK_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), string.Empty);
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Required_non_PK_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), string.Empty);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), string.Empty);
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EmptyException>(() => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), string.Empty);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_one_to_one_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_one_to_one_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_one_to_one_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_one_to_one_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Required_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EmptyException>(() => base.Required_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), string.Empty);
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Required_one_to_one_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EmptyException>(() => base.Required_one_to_one_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), string.Empty);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_one_to_one_relationships_are_one_to_one()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.ThrowsException>(() => base.Required_one_to_one_relationships_are_one_to_one());
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_one_to_one_relationships_are_one_to_one()
        => SpecFails.Throws<global::Xunit.Sdk.ThrowsException>(() => base.Required_one_to_one_relationships_are_one_to_one());
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Required_one_to_one_with_AK_relationships_are_one_to_one()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.ThrowsException>(() => base.Required_one_to_one_with_AK_relationships_are_one_to_one());
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Required_one_to_one_with_AK_relationships_are_one_to_one()
        => SpecFails.Throws<global::Xunit.Sdk.ThrowsException>(() => base.Required_one_to_one_with_AK_relationships_are_one_to_one());
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_one_to_one_with_alternate_key_are_cascade_deleted(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EmptyException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), string.Empty);
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EmptyException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming), string.Empty);
#endif

#if (EF10 || EF11) || EF9
    public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#elif EF8
    public override void Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_starting_detached(cascadeDeleteTiming, deleteOrphansTiming);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EmptyException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), string.Empty);
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming cascadeDeleteTiming, global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EmptyException>(() => base.Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming), string.Empty);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_changed_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_changed_optional_one_to_one(changeMechanism, useExistingEntities);
#elif EF8
    public override void Save_changed_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_changed_optional_one_to_one(changeMechanism, useExistingEntities);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_changed_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_changed_optional_one_to_one_with_alternate_key(changeMechanism, useExistingEntities);
#elif EF8
    public override void Save_changed_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_changed_optional_one_to_one_with_alternate_key(changeMechanism, useExistingEntities);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_changed_optional_one_to_one_with_alternate_key_in_store()
        => base.Save_changed_optional_one_to_one_with_alternate_key_in_store();
#elif EF8
    public override void Save_changed_optional_one_to_one_with_alternate_key_in_store()
        => base.Save_changed_optional_one_to_one_with_alternate_key_in_store();
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_optional_many_to_one_dependents(changeMechanism, useExistingEntities);
#elif EF8
    public override void Save_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_optional_many_to_one_dependents(changeMechanism, useExistingEntities);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_optional_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities);
#elif EF8
    public override void Save_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_optional_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_removed_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Save_removed_optional_many_to_one_dependents(changeMechanism);
#elif EF8
    public override void Save_removed_optional_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Save_removed_optional_many_to_one_dependents(changeMechanism);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_removed_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Save_removed_optional_many_to_one_dependents_with_alternate_key(changeMechanism);
#elif EF8
    public override void Save_removed_optional_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Save_removed_optional_many_to_one_dependents_with_alternate_key(changeMechanism);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_removed_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Save_removed_required_many_to_one_dependents(changeMechanism);
#elif EF8
    public override void Save_removed_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Save_removed_required_many_to_one_dependents(changeMechanism);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_removed_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Save_removed_required_many_to_one_dependents_with_alternate_key(changeMechanism);
#elif EF8
    public override void Save_removed_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Save_removed_required_many_to_one_dependents_with_alternate_key(changeMechanism);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_required_many_to_one_dependents(changeMechanism, useExistingEntities);
#elif EF8
    public override void Save_required_many_to_one_dependents(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_required_many_to_one_dependents(changeMechanism, useExistingEntities);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_required_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities);
#elif EF8
    public override void Save_required_many_to_one_dependents_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_required_many_to_one_dependents_with_alternate_key(changeMechanism, useExistingEntities);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_required_non_PK_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_required_non_PK_one_to_one_changed_by_reference(changeMechanism, useExistingEntities);
#elif EF8
    public override void Save_required_non_PK_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_required_non_PK_one_to_one_changed_by_reference(changeMechanism, useExistingEntities);
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities);
#elif EF8
    public override void Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_required_non_PK_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities);
#endif

#if (EF10 || EF11) || EF9
#if EF9
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Save_required_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Save_required_one_to_one_changed_by_reference(changeMechanism));
#elif EF10
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Save_required_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Save_required_one_to_one_changed_by_reference(changeMechanism));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Save_required_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Save_required_one_to_one_changed_by_reference(changeMechanism));
#endif
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Save_required_one_to_one_changed_by_reference(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => SpecFails.Throws<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Save_required_one_to_one_changed_by_reference(changeMechanism));
#endif

#if (EF10 || EF11) || EF9
    public override Task Save_required_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_required_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities);
#elif EF8
    public override void Save_required_one_to_one_changed_by_reference_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism, bool useExistingEntities)
        => base.Save_required_one_to_one_changed_by_reference_with_alternate_key(changeMechanism, useExistingEntities);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Save_two_entity_cycle_with_lazy_loading()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.ThrowsException>(() => base.Save_two_entity_cycle_with_lazy_loading());
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Save_two_entity_cycle_with_lazy_loading()
        => SpecFails.Throws<global::Xunit.Sdk.ThrowsException>(() => base.Save_two_entity_cycle_with_lazy_loading());
#endif

#if (EF10 || EF11) || EF9
    public override Task Sever_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_optional_one_to_one(changeMechanism);
#elif EF8
    public override void Sever_optional_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_optional_one_to_one(changeMechanism);
#endif

#if (EF10 || EF11) || EF9
    public override Task Sever_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_optional_one_to_one_with_alternate_key(changeMechanism);
#elif EF8
    public override void Sever_optional_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_optional_one_to_one_with_alternate_key(changeMechanism);
#endif

#if (EF10 || EF11) || EF9
    public override Task Sever_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_required_non_PK_one_to_one(changeMechanism);
#elif EF8
    public override void Sever_required_non_PK_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_required_non_PK_one_to_one(changeMechanism);
#endif

#if (EF10 || EF11) || EF9
    public override Task Sever_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_required_non_PK_one_to_one_with_alternate_key(changeMechanism);
#elif EF8
    public override void Sever_required_non_PK_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_required_non_PK_one_to_one_with_alternate_key(changeMechanism);
#endif

#if (EF10 || EF11) || EF9
    public override Task Sever_required_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_required_one_to_one(changeMechanism);
#elif EF8
    public override void Sever_required_one_to_one(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_required_one_to_one(changeMechanism);
#endif

#if (EF10 || EF11) || EF9
    public override Task Sever_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_required_one_to_one_with_alternate_key(changeMechanism);
#elif EF8
    public override void Sever_required_one_to_one_with_alternate_key(global::Microsoft.EntityFrameworkCore.ProxyGraphUpdatesTestBase<global::MongoDB.EntityFrameworkCore.SpecificationTests.Tracking.ProxyGraphUpdatesMongoTest.ProxyGraphUpdatesMongoFixture>.ChangeMechanism changeMechanism)
        => base.Sever_required_one_to_one_with_alternate_key(changeMechanism);
#endif

#if (EF10 || EF11) || EF9
    public override Task Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref()
        => base.Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref();
#elif EF8
    public override void Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref()
        => base.Sometimes_not_calling_DetectChanges_when_required_does_not_throw_for_null_ref();
#endif
}
