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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Loading;

public class LazyLoadProxyMongoTest(LazyLoadProxyMongoTest.LazyLoadProxyMongoFixture fixture)
    : LazyLoadProxyTestBase<LazyLoadProxyMongoTest.LazyLoadProxyMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Can_use_proxies_from_multiple_threads_when_navigations_already_loaded(bool noTracking)
        => SpecFails.TranslationFailed(() => base.Can_use_proxies_from_multiple_threads_when_navigations_already_loaded(noTracking));

    public override void Detected_principal_reference_navigation_changes_are_detected_and_marked_loaded()
        => base.Detected_principal_reference_navigation_changes_are_detected_and_marked_loaded();

    public override void Detected_dependent_reference_navigation_changes_are_detected_and_marked_loaded()
        => base.Detected_dependent_reference_navigation_changes_are_detected_and_marked_loaded();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Lazy_load_one_to_one_reference_with_recursive_property(EntityState state)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_with_recursive_property(state), "configured as an owned entity type");

    public override void Attached_references_to_principal_are_marked_as_loaded(EntityState state, bool lazy)
        => base.Attached_references_to_principal_are_marked_as_loaded(state, lazy);

    public override void Attached_references_to_dependents_are_marked_as_loaded(EntityState state, bool lazy)
        => base.Attached_references_to_dependents_are_marked_as_loaded(state, lazy);

    public override void Attached_collections_are_not_marked_as_loaded(EntityState state, bool lazy)
        => base.Attached_collections_are_not_marked_as_loaded(state, lazy);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_collection(EntityState state, bool useAttach, bool useDetach)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_collection(state, useAttach, useDetach), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_many_to_one_reference_to_principal(EntityState state, bool useAttach, bool useDetach)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_many_to_one_reference_to_principal(state, useAttach, useDetach), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_reference_to_principal(EntityState state, bool useAttach, bool useDetach)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_reference_to_principal(state, useAttach, useDetach), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_reference_to_dependent(EntityState state, bool useAttach, bool useDetach)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_reference_to_dependent(state, useAttach, useDetach), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_principal(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_PK_to_PK_reference_to_principal(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_dependent(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_PK_to_PK_reference_to_dependent(state), string.Empty);

    public override void Eager_load_one_to_one_non_virtual_reference_to_owned_type()
        => base.Eager_load_one_to_one_non_virtual_reference_to_owned_type();

    public override void Eager_load_one_to_one_virtual_reference_to_owned_type()
        => base.Eager_load_one_to_one_virtual_reference_to_owned_type();

    public override void Eager_load_one_to_many_non_virtual_collection_of_owned_types()
        => base.Eager_load_one_to_many_non_virtual_collection_of_owned_types();

    public override void Eager_load_one_to_many_virtual_collection_of_owned_types()
        => base.Eager_load_one_to_many_virtual_collection_of_owned_types();

    public override void Eager_load_one_to_many_non_virtual_collection_of_owned_types_with_explicit_lazy_load()
        => base.Eager_load_one_to_many_non_virtual_collection_of_owned_types_with_explicit_lazy_load();

    public override void Eager_load_one_to_many_virtual_collection_of_owned_types_with_explicit_lazy_load()
        => base.Eager_load_one_to_many_virtual_collection_of_owned_types_with_explicit_lazy_load();

    public override void Setting_reference_to_owned_type_to_null_is_allowed_on_non_virtual_navigation()
        => base.Setting_reference_to_owned_type_to_null_is_allowed_on_non_virtual_navigation();

    public override void Setting_reference_to_owned_type_to_null_is_allowed_on_virtual_navigation()
        => base.Setting_reference_to_owned_type_to_null_is_allowed_on_virtual_navigation();

    public override void Non_virtual_one_to_one_reference_to_principal_is_not_lazy_loaded()
        => base.Non_virtual_one_to_one_reference_to_principal_is_not_lazy_loaded();

    public override void Non_virtual_one_to_many_reference_to_principal_is_not_lazy_loaded()
        => base.Non_virtual_one_to_many_reference_to_principal_is_not_lazy_loaded();

    public override void Non_virtual_reference_to_dependent_is_not_lazy_loaded()
        => base.Non_virtual_reference_to_dependent_is_not_lazy_loaded();

    public override void Non_virtual_collection_is_not_lazy_loaded()
        => base.Non_virtual_collection_is_not_lazy_loaded();

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK(EntityState state)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK(state);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK(EntityState state)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK(state);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_many_to_one_reference_to_principal_changed_non_found_FK(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_many_to_one_reference_to_principal_changed_non_found_FK(state), string.Empty);

    // Fails: Reading a document with nested owned types reports a required element missing (tracked states only) EF-X212
    public override void Lazy_load_many_to_one_reference_to_principal_changed_found_FK(EntityState state)
    {
        if (state == EntityState.Detached)
        {
            base.Lazy_load_many_to_one_reference_to_principal_changed_found_FK(state);
        }
        else
        {
            // Fails: Reading a document with nested owned types reports a different value than expected EF-X212
            SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_many_to_one_reference_to_principal_changed_found_FK(state));
        }
    }

    public override void Lazy_load_collection_not_found(EntityState state)
        => base.Lazy_load_collection_not_found(state);

    public override void Lazy_load_many_to_one_reference_to_principal_not_found(EntityState state)
        => base.Lazy_load_many_to_one_reference_to_principal_not_found(state);

    public override void Lazy_load_one_to_one_reference_to_principal_not_found(EntityState state)
        => base.Lazy_load_one_to_one_reference_to_principal_not_found(state);

    public override void Lazy_load_one_to_one_reference_to_dependent_not_found(EntityState state)
        => base.Lazy_load_one_to_one_reference_to_dependent_not_found(state);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_collection_already_loaded(EntityState state, CascadeTiming cascadeDeleteTiming)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_collection_already_loaded(state, cascadeDeleteTiming), string.Empty);

#if EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Lazy_load_collection_already_partially_loaded(EntityState state)
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_collection_already_partially_loaded(state));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_collection_already_partially_loaded(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_collection_already_partially_loaded(state), string.Empty);
#endif

    public override void Lazy_load_collection_already_partially_loaded_no_tracking(QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_already_partially_loaded_no_tracking(queryTrackingBehavior);

    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Lazy_load_many_to_one_reference_to_principal_already_loaded(EntityState state, CascadeTiming cascadeDeleteTiming)
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_many_to_one_reference_to_principal_already_loaded(state, cascadeDeleteTiming));

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_reference_to_principal_already_loaded(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_reference_to_principal_already_loaded(state), string.Empty);

    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Lazy_load_one_to_one_reference_to_dependent_already_loaded(EntityState state, CascadeTiming cascadeDeleteTiming)
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_reference_to_dependent_already_loaded(state, cascadeDeleteTiming));

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_many_to_one_reference_to_principal_alternate_key(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_many_to_one_reference_to_principal_alternate_key(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_reference_to_principal_alternate_key(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_reference_to_principal_alternate_key(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_reference_to_dependent_alternate_key(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_reference_to_dependent_alternate_key(state), string.Empty);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_alternate_key(EntityState state)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_alternate_key(state);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_alternate_key(EntityState state)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_alternate_key(state);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_collection_shadow_fk(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_collection_shadow_fk(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_many_to_one_reference_to_principal_shadow_fk(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_many_to_one_reference_to_principal_shadow_fk(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_reference_to_principal_shadow_fk(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_reference_to_principal_shadow_fk(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_reference_to_dependent_shadow_fk(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_reference_to_dependent_shadow_fk(state), string.Empty);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_shadow_fk(state);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_shadow_fk(state);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_collection_composite_key(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_collection_composite_key(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_many_to_one_reference_to_principal_composite_key(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_many_to_one_reference_to_principal_composite_key(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_reference_to_principal_composite_key(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_reference_to_principal_composite_key(state), string.Empty);

    // Fails: Other unexpected exception from query execution EF-X605
    public override void Lazy_load_one_to_one_reference_to_dependent_composite_key(EntityState state)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Lazy_load_one_to_one_reference_to_dependent_composite_key(state), string.Empty);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_composite_key(EntityState state)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_composite_key(state);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_composite_key(EntityState state)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_composite_key(state);

    public override void Lazy_load_collection_for_detached_is_no_op()
        => base.Lazy_load_collection_for_detached_is_no_op();

    public override void Lazy_load_reference_to_principal_for_detached_is_no_op()
        => base.Lazy_load_reference_to_principal_for_detached_is_no_op();

    public override void Lazy_load_reference_to_dependent_for_detached_is_no_op()
        => base.Lazy_load_reference_to_dependent_for_detached_is_no_op();

    public override void Lazy_load_collection_for_no_tracking_does_not_throw_if_populated()
        => base.Lazy_load_collection_for_no_tracking_does_not_throw_if_populated();

    public override void Lazy_load_reference_to_principal_for_no_tracking_does_not_throw_if_populated()
        => base.Lazy_load_reference_to_principal_for_no_tracking_does_not_throw_if_populated();

    public override void Lazy_load_reference_to_dependent_for_no_tracking_does_not_throw_if_populated()
        => base.Lazy_load_reference_to_dependent_for_no_tracking_does_not_throw_if_populated();

    public override async Task Load_collection(EntityState state, bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.AsyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Load_collection(state, async), string.Empty);
    }

#if EF8
    [ConditionalFact(Skip = "The EF8 test base calls Xunit.Assert.Equal(string, string, bool, bool, bool), missing from xunit 2.9 (MissingMethodException)")]
    public override void Can_serialize_proxies_to_JSON()
        => base.Can_serialize_proxies_to_JSON();
#else
#if EF9 || EF10
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Can_serialize_proxies_to_JSON()
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Can_serialize_proxies_to_JSON());
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Can_serialize_proxies_to_JSON()
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Can_serialize_proxies_to_JSON());
#endif
#endif

    public override void Lazy_loading_finds_correct_entity_type_with_already_loaded_owned_types()
        => base.Lazy_loading_finds_correct_entity_type_with_already_loaded_owned_types();

    public override void Lazy_loading_finds_correct_entity_type_with_multiple_queries()
        => base.Lazy_loading_finds_correct_entity_type_with_multiple_queries();

    public override void Lazy_loading_finds_correct_entity_type_with_opaque_predicate_and_multiple_queries()
        => base.Lazy_loading_finds_correct_entity_type_with_opaque_predicate_and_multiple_queries();

    public override void Lazy_loading_finds_correct_entity_type_with_multiple_queries_using_Count()
        => base.Lazy_loading_finds_correct_entity_type_with_multiple_queries_using_Count();

#if !EF11
#if !(EF10 || EF11)
#if !(EF10 || EF11)
    public override void Lazy_loading_shares_service__property_on_derived_types()
        => base.Lazy_loading_shares_service__property_on_derived_types();
#else
    public override void Lazy_loading_shares_service__property_on_derived_types()
        => base.Lazy_loading_shares_service__property_on_derived_types();
#endif
#endif
#endif

    // Fails: an Unspecified DateTime is treated as local time, so the value is shifted outside UTC (EF-X006) (depends on the time zone: passes in UTC) EF-X720
    public override void Lazy_loading_handles_shadow_nullable_GUID_FK_in_TPH_model()
        => SpecFails.Either(() => base.Lazy_loading_handles_shadow_nullable_GUID_FK_in_TPH_model());

    public override void Lazy_loading_finds_correct_entity_type_with_alternate_model()
        => base.Lazy_loading_finds_correct_entity_type_with_alternate_model();

    public override void Top_level_projection_track_entities_before_passing_to_client_method()
        => base.Top_level_projection_track_entities_before_passing_to_client_method();

    public override async Task Entity_equality_with_proxy_parameter(bool async)
    {
        await base.Entity_equality_with_proxy_parameter(async);
    }

#if (EF10 || EF11)
    public override void Lazy_loading_shares_service_property_on_derived_types()
        => base.Lazy_loading_shares_service_property_on_derived_types();
#endif

    public class LazyLoadProxyMongoFixture : LoadFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("LazyLoadProxyTest");

        public TestServer TestServer { get; private set; }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder).ConfigureWarnings(
                w => w.Ignore(CoreEventId.DuplicateDependentEntityTypeInstanceWarning)); // EF-X210: Nose instance shared by two owned navigations

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.MapSinglePropertyKeysToElementId(generateIntKeys: true); // EF-X201, EF-X205
        }
    }
}
