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
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Can_use_proxies_from_multiple_threads_when_navigations_already_loaded(noTracking)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Detected_principal_reference_navigation_changes_are_detected_and_marked_loaded()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Detected_principal_reference_navigation_changes_are_detected_and_marked_loaded()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Detected_dependent_reference_navigation_changes_are_detected_and_marked_loaded()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Detected_dependent_reference_navigation_changes_are_detected_and_marked_loaded()).Message);

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Lazy_load_one_to_one_reference_with_recursive_property(EntityState state)
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_with_recursive_property(state)).Message);

    public override void Attached_references_to_principal_are_marked_as_loaded(EntityState state, bool lazy)
        => base.Attached_references_to_principal_are_marked_as_loaded(state, lazy);

    public override void Attached_references_to_dependents_are_marked_as_loaded(EntityState state, bool lazy)
        => base.Attached_references_to_dependents_are_marked_as_loaded(state, lazy);

    public override void Attached_collections_are_not_marked_as_loaded(EntityState state, bool lazy)
        => base.Attached_collections_are_not_marked_as_loaded(state, lazy);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_collection(EntityState state, bool useAttach, bool useDetach)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_collection(state, useAttach, useDetach)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_many_to_one_reference_to_principal(EntityState state, bool useAttach, bool useDetach)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_many_to_one_reference_to_principal(state, useAttach, useDetach)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_reference_to_principal(EntityState state, bool useAttach, bool useDetach)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_principal(state, useAttach, useDetach)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_reference_to_dependent(EntityState state, bool useAttach, bool useDetach)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_dependent(state, useAttach, useDetach)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_principal(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_PK_to_PK_reference_to_principal(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_dependent(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_PK_to_PK_reference_to_dependent(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Eager_load_one_to_one_non_virtual_reference_to_owned_type()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Eager_load_one_to_one_non_virtual_reference_to_owned_type()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Eager_load_one_to_one_virtual_reference_to_owned_type()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Eager_load_one_to_one_virtual_reference_to_owned_type()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Eager_load_one_to_many_non_virtual_collection_of_owned_types()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Eager_load_one_to_many_non_virtual_collection_of_owned_types()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Eager_load_one_to_many_virtual_collection_of_owned_types()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Eager_load_one_to_many_virtual_collection_of_owned_types()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Eager_load_one_to_many_non_virtual_collection_of_owned_types_with_explicit_lazy_load()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Eager_load_one_to_many_non_virtual_collection_of_owned_types_with_explicit_lazy_load()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Eager_load_one_to_many_virtual_collection_of_owned_types_with_explicit_lazy_load()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Eager_load_one_to_many_virtual_collection_of_owned_types_with_explicit_lazy_load()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Setting_reference_to_owned_type_to_null_is_allowed_on_non_virtual_navigation()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Setting_reference_to_owned_type_to_null_is_allowed_on_non_virtual_navigation()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Setting_reference_to_owned_type_to_null_is_allowed_on_virtual_navigation()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Setting_reference_to_owned_type_to_null_is_allowed_on_virtual_navigation()).Message);

#if !EF10
    // Fails: Cross-collection Include/join not translated on EF8/EF9 EF-X020
    public override void Non_virtual_one_to_one_reference_to_principal_is_not_lazy_loaded()
        => Assert.Contains("could not be translated", Assert.Throws<global::System.InvalidOperationException>(() => base.Non_virtual_one_to_one_reference_to_principal_is_not_lazy_loaded()).Message);
#else
    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Non_virtual_one_to_one_reference_to_principal_is_not_lazy_loaded()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Non_virtual_one_to_one_reference_to_principal_is_not_lazy_loaded()).Message);
#endif

#if !EF10
    // Fails: Cross-collection Include/join not translated on EF8/EF9 EF-X020
    public override void Non_virtual_one_to_many_reference_to_principal_is_not_lazy_loaded()
        => Assert.Contains("could not be translated", Assert.Throws<global::System.InvalidOperationException>(() => base.Non_virtual_one_to_many_reference_to_principal_is_not_lazy_loaded()).Message);
#else
    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Non_virtual_one_to_many_reference_to_principal_is_not_lazy_loaded()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Non_virtual_one_to_many_reference_to_principal_is_not_lazy_loaded()).Message);
#endif

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Non_virtual_reference_to_dependent_is_not_lazy_loaded()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Non_virtual_reference_to_dependent_is_not_lazy_loaded()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Non_virtual_collection_is_not_lazy_loaded()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Non_virtual_collection_is_not_lazy_loaded()).Message);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK(EntityState state)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK(state);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK(EntityState state)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK(state);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_many_to_one_reference_to_principal_changed_non_found_FK(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_many_to_one_reference_to_principal_changed_non_found_FK(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing (tracked states only) EF-X212
    public override void Lazy_load_many_to_one_reference_to_principal_changed_found_FK(EntityState state)
    {
        if (state == EntityState.Detached)
        {
            base.Lazy_load_many_to_one_reference_to_principal_changed_found_FK(state);
        }
        else
        {
            Assert.Contains(
                "Document element is missing for required non-nullable property",
                Assert.Throws<InvalidOperationException>(() => base.Lazy_load_many_to_one_reference_to_principal_changed_found_FK(state)).Message);
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

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_collection_already_loaded(EntityState state, CascadeTiming cascadeDeleteTiming)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_collection_already_loaded(state, cascadeDeleteTiming)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_collection_already_partially_loaded(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_collection_already_partially_loaded(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_collection_already_partially_loaded_no_tracking(QueryTrackingBehavior queryTrackingBehavior)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_collection_already_partially_loaded_no_tracking(queryTrackingBehavior)).Message);

#if !EF10
    // Fails: Cross-collection Include/join not translated on EF8/EF9 EF-X020
    public override void Lazy_load_many_to_one_reference_to_principal_already_loaded(EntityState state, CascadeTiming cascadeDeleteTiming)
        => Assert.Contains("could not be translated", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_many_to_one_reference_to_principal_already_loaded(state, cascadeDeleteTiming)).Message);
#else
    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_many_to_one_reference_to_principal_already_loaded(EntityState state, CascadeTiming cascadeDeleteTiming)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_many_to_one_reference_to_principal_already_loaded(state, cascadeDeleteTiming)).Message);
#endif

#if !EF10
    // Fails: Cross-collection Include/join not translated on EF8/EF9 EF-X020
    public override void Lazy_load_one_to_one_reference_to_principal_already_loaded(EntityState state)
        => Assert.Contains("could not be translated", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_principal_already_loaded(state)).Message);
#else
    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_reference_to_principal_already_loaded(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_principal_already_loaded(state)).Message);
#endif

#if !EF10
    // Fails: Cross-collection Include/join not translated on EF8/EF9 EF-X020
    public override void Lazy_load_one_to_one_reference_to_dependent_already_loaded(EntityState state, CascadeTiming cascadeDeleteTiming)
        => Assert.Contains("could not be translated", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_dependent_already_loaded(state, cascadeDeleteTiming)).Message);
#else
    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_reference_to_dependent_already_loaded(EntityState state, CascadeTiming cascadeDeleteTiming)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_dependent_already_loaded(state, cascadeDeleteTiming)).Message);
#endif

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(state)).Message);

#if !EF10
    // Fails: Cross-collection Include/join not translated on EF8/EF9 EF-X020
    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(EntityState state)
        => Assert.Contains("could not be translated", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(state)).Message);
#else
    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(state)).Message);
#endif

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_many_to_one_reference_to_principal_alternate_key(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_many_to_one_reference_to_principal_alternate_key(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_reference_to_principal_alternate_key(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_principal_alternate_key(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_reference_to_dependent_alternate_key(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_dependent_alternate_key(state)).Message);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_alternate_key(EntityState state)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_alternate_key(state);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_alternate_key(EntityState state)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_alternate_key(state);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_collection_shadow_fk(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_collection_shadow_fk(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_many_to_one_reference_to_principal_shadow_fk(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_many_to_one_reference_to_principal_shadow_fk(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_reference_to_principal_shadow_fk(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_principal_shadow_fk(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_reference_to_dependent_shadow_fk(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_dependent_shadow_fk(state)).Message);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_shadow_fk(state);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_shadow_fk(state);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_collection_composite_key(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_collection_composite_key(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_many_to_one_reference_to_principal_composite_key(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_many_to_one_reference_to_principal_composite_key(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_reference_to_principal_composite_key(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_principal_composite_key(state)).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_one_to_one_reference_to_dependent_composite_key(EntityState state)
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_one_to_one_reference_to_dependent_composite_key(state)).Message);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_composite_key(EntityState state)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_composite_key(state);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_composite_key(EntityState state)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_composite_key(state);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_collection_for_detached_is_no_op()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_collection_for_detached_is_no_op()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_reference_to_principal_for_detached_is_no_op()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_reference_to_principal_for_detached_is_no_op()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_reference_to_dependent_for_detached_is_no_op()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_reference_to_dependent_for_detached_is_no_op()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_collection_for_no_tracking_does_not_throw_if_populated()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_collection_for_no_tracking_does_not_throw_if_populated()).Message);

#if !EF10
    // Fails: Cross-collection Include/join not translated on EF8/EF9 EF-X020
    public override void Lazy_load_reference_to_principal_for_no_tracking_does_not_throw_if_populated()
        => Assert.Contains("could not be translated", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_reference_to_principal_for_no_tracking_does_not_throw_if_populated()).Message);
#else
    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_reference_to_principal_for_no_tracking_does_not_throw_if_populated()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_reference_to_principal_for_no_tracking_does_not_throw_if_populated()).Message);
#endif

#if !EF10
    // Fails: Cross-collection Include/join not translated on EF8/EF9 EF-X020
    public override void Lazy_load_reference_to_dependent_for_no_tracking_does_not_throw_if_populated()
        => Assert.Contains("could not be translated", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_reference_to_dependent_for_no_tracking_does_not_throw_if_populated()).Message);
#else
    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_load_reference_to_dependent_for_no_tracking_does_not_throw_if_populated()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_load_reference_to_dependent_for_no_tracking_does_not_throw_if_populated()).Message);
#endif

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override async Task Load_collection(EntityState state, bool async)
        => Assert.Contains("Document element is missing for required non-nullable property", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Load_collection(state, async))).Message);

#if EF8
    [ConditionalFact(Skip = "The EF8 test base calls Xunit.Assert.Equal(string, string, bool, bool, bool), missing from xunit 2.9 (MissingMethodException)")]
    public override void Can_serialize_proxies_to_JSON()
        => base.Can_serialize_proxies_to_JSON();
#else
    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Can_serialize_proxies_to_JSON()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Can_serialize_proxies_to_JSON()).Message);
#endif

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_loading_finds_correct_entity_type_with_already_loaded_owned_types()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_loading_finds_correct_entity_type_with_already_loaded_owned_types()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_loading_finds_correct_entity_type_with_multiple_queries()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_loading_finds_correct_entity_type_with_multiple_queries()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_loading_finds_correct_entity_type_with_opaque_predicate_and_multiple_queries()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_loading_finds_correct_entity_type_with_opaque_predicate_and_multiple_queries()).Message);

    public override void Lazy_loading_finds_correct_entity_type_with_multiple_queries_using_Count()
        => base.Lazy_loading_finds_correct_entity_type_with_multiple_queries_using_Count();

#if !EF11
#if !EF10
#if !EF10
    // Fails: Lazy-load/Single query over a type hierarchy returns more than one element EF-X206
    public override void Lazy_loading_shares_service__property_on_derived_types()
        => Assert.Contains("Sequence contains more than one element", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_loading_shares_service__property_on_derived_types()).Message);
#else
    public override void Lazy_loading_shares_service__property_on_derived_types()
        => base.Lazy_loading_shares_service__property_on_derived_types();
#endif
#endif
#endif

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_loading_handles_shadow_nullable_GUID_FK_in_TPH_model()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_loading_handles_shadow_nullable_GUID_FK_in_TPH_model()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Lazy_loading_finds_correct_entity_type_with_alternate_model()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_loading_finds_correct_entity_type_with_alternate_model()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override void Top_level_projection_track_entities_before_passing_to_client_method()
        => Assert.Contains("Document element is missing for required non-nullable property", Assert.Throws<global::System.InvalidOperationException>(() => base.Top_level_projection_track_entities_before_passing_to_client_method()).Message);

    // Fails: Reading a document with nested owned types reports a required element missing EF-X212
    public override async Task Entity_equality_with_proxy_parameter(bool async)
        => Assert.Contains("Document element is missing for required non-nullable property", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Entity_equality_with_proxy_parameter(async))).Message);

#if EF10
    // Fails: Lazy-load/Single query over a type hierarchy returns more than one element EF-X206
    public override void Lazy_loading_shares_service_property_on_derived_types()
        => Assert.Contains("Sequence contains more than one element", Assert.Throws<global::System.InvalidOperationException>(() => base.Lazy_loading_shares_service_property_on_derived_types()).Message);
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
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

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
