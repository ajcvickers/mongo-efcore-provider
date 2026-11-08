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

#if !EF8

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class JsonQueryMongoTest : JsonQueryTestBase<JsonQueryMongoTest.JsonQueryMongoFixture>
{
    public JsonQueryMongoTest(JsonQueryMongoFixture fixture)
        : base(fixture)
    {
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if EF9
    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owner_entity(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owner_entity(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owner_entity_NoTracking(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owner_entity_NoTracking(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owner_entity_duplicated(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owner_entity_duplicated(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owner_entity_duplicated_NoTracking(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owner_entity_duplicated_NoTracking(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owner_entity_twice(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owner_entity_twice(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owner_entity_twice_NoTracking(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owner_entity_twice_NoTracking(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_reference_root(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_root(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_reference_duplicated(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_duplicated(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_reference_duplicated2(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_duplicated2(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_collection_root(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_collection_root(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_reference_branch(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_branch(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_collection_branch(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_collection_branch(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_with_deduplication(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_with_deduplication(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_with_deduplication_reverse_order(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_with_deduplication_reverse_order(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Left_join_json_entities(bool async)
        => SpecFails.ThrowsAsync(() => base.Left_join_json_entities(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Project_json_entity_FirstOrDefault_subquery(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_FirstOrDefault_subquery(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Project_json_entity_FirstOrDefault_subquery_deduplication(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_FirstOrDefault_subquery_deduplication(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Project_json_entity_FirstOrDefault_subquery_deduplication_and_outer_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_FirstOrDefault_subquery_deduplication_and_outer_reference(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Project_json_entity_FirstOrDefault_subquery_deduplication_outer_reference_and_pruning(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_FirstOrDefault_subquery_deduplication_outer_reference_and_pruning(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_SelectMany(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_SelectMany(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_nested_collection_SelectMany(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_nested_collection_SelectMany(async));
#endif
#if EF11
    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Entity_including_collection_with_json_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Entity_including_collection_with_json_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Entity_including_collection_with_json_and_separate_json_projection_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Entity_including_collection_with_json_and_separate_json_projection_AsNoTrackingWithIdentityResolution(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_root_with_missing_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_root_with_missing_scalars(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_top_level_json_entity_with_missing_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_top_level_json_entity_with_missing_scalars(async));

    // Fails: No support for nested JSON EF-X008
    public override Task Project_nested_json_entity_with_missing_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_nested_json_entity_with_missing_scalars(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_top_level_entity_with_null_value_required_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_top_level_entity_with_null_value_required_scalars(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_root_entity_with_missing_required_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_root_entity_with_missing_required_navigation(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_missing_required_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_missing_required_navigation(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_root_entity_with_null_required_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_root_entity_with_null_required_navigation(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_null_required_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_null_required_navigation(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900 (depends on the time zone: fails in UTC) EF-X720
    public override Task Project_missing_required_scalar(bool async)
        => SpecFails.FailsInUtcAsync(() => base.Project_missing_required_scalar(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_null_required_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_null_required_scalar(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Optional_json_properties_materialized_as_null_when_the_element_in_json_is_not_present()
        => SpecFails.ThrowsAsync(() => base.Optional_json_properties_materialized_as_null_when_the_element_in_json_is_not_present());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Can_project_nullable_json_property_when_the_element_in_json_is_not_present()
        => SpecFails.ThrowsAsync(() => base.Can_project_nullable_json_property_when_the_element_in_json_is_not_present());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Accessing_missing_navigation_works()
        => SpecFails.ThrowsAsync(() => base.Accessing_missing_navigation_works());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Missing_navigation_works_with_deduplication(bool async)
        => SpecFails.ThrowsAsync(() => base.Missing_navigation_works_with_deduplication(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Contains_on_nested_collection_with_init_only_navigation()
        => SpecFails.ThrowsAsync(() => base.Contains_on_nested_collection_with_init_only_navigation());

    public override Task Project_json_with_no_properties()
        => base.Project_json_with_no_properties();

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Query_with_nested_json_collection_mapped_to_private_field_via_IReadOnlyList()
        => SpecFails.ThrowsAsync(() => base.Query_with_nested_json_collection_mapped_to_private_field_via_IReadOnlyList());

    public override Task Project_entity_with_json_null_values()
        => base.Project_entity_with_json_null_values();

    public override Task Try_project_collection_but_JSON_is_entity()
        => base.Try_project_collection_but_JSON_is_entity();

    public override Task Try_project_reference_but_JSON_is_collection()
        => base.Try_project_reference_but_JSON_is_collection();

    // Fails: depends on the time zone: passes in UTC EF-X720
    public override Task Project_json_array_of_primitives_on_reference()
        => SpecFails.EitherAsync(() => base.Project_json_array_of_primitives_on_reference());

    public override Task Project_json_array_of_primitives_on_collection()
        => base.Project_json_array_of_primitives_on_collection();

    public override Task Project_element_of_json_array_of_primitives()
        => base.Project_element_of_json_array_of_primitives();

    // Fails: depends on the time zone: passes in UTC EF-X720
    public override Task Predicate_based_on_element_of_json_array_of_primitives1()
        => SpecFails.EitherAsync(() => base.Predicate_based_on_element_of_json_array_of_primitives1());

    // Fails: depends on the time zone: passes in UTC EF-X720
    public override Task Predicate_based_on_element_of_json_array_of_primitives2()
        => SpecFails.EitherAsync(() => base.Predicate_based_on_element_of_json_array_of_primitives2());

    // Fails: depends on the time zone: passes in UTC EF-X720
    public override Task Predicate_based_on_element_of_json_array_of_primitives3()
        => SpecFails.EitherAsync(() => base.Predicate_based_on_element_of_json_array_of_primitives3());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Junk_in_json_basic_tracking()
        => SpecFails.ThrowsAsync(() => base.Junk_in_json_basic_tracking());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Junk_in_json_basic_no_tracking()
        => SpecFails.ThrowsAsync(() => base.Junk_in_json_basic_no_tracking());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Tricky_buffering_basic()
        => SpecFails.ThrowsAsync(() => base.Tricky_buffering_basic());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Shadow_properties_basic_tracking()
        => SpecFails.ThrowsAsync(() => base.Shadow_properties_basic_tracking());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Shadow_properties_basic_no_tracking()
        => SpecFails.ThrowsAsync(() => base.Shadow_properties_basic_no_tracking());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_shadow_properties_from_json_entity()
        => SpecFails.ThrowsAsync(() => base.Project_shadow_properties_from_json_entity());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_proxies_entity_with_json()
        => SpecFails.ThrowsAsync(() => base.Project_proxies_entity_with_json());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_proxies_entity_with_json_with_primitive_collection()
        => SpecFails.ThrowsAsync(() => base.Project_proxies_entity_with_json_with_primitive_collection());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Not_ICollection_basic_projection()
        => SpecFails.ThrowsAsync(() => base.Not_ICollection_basic_projection());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_duplicated_navigations(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_duplicated_navigations(noTracking));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_duplicated_scalars(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_duplicated_scalars(noTracking));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_empty_navigations(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_empty_navigations(noTracking));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_empty_scalars(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_empty_scalars(noTracking));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_null_navigations(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_null_navigations(noTracking));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_null_scalars(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_null_scalars(noTracking));

#endif

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owner_entity_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owner_entity_NoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owner_entity_duplicated_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owner_entity_duplicated_NoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owner_entity_twice_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owner_entity_twice_NoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_reference_root_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_root_NoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_reference_duplicated_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_duplicated_NoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_reference_duplicated2_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_duplicated2_NoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_collection_root_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_collection_root_NoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_reference_branch_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_branch_NoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_collection_branch_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_collection_branch_NoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_reference_leaf(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_leaf(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_owned_collection_leaf(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_collection_leaf(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_scalar(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_scalar_length(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_scalar_length(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Basic_json_projection_enum_inside_json_entity(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_enum_inside_json_entity(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_enum_with_custom_conversion(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_enum_with_custom_conversion(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_property_in_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_property_in_predicate(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_subquery_property_pushdown_length(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_subquery_property_pushdown_length(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_subquery_reference_pushdown_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_subquery_reference_pushdown_reference(async));

    public override Task Json_subquery_reference_pushdown_reference_anonymous_projection(bool async)
        => base.Json_subquery_reference_pushdown_reference_anonymous_projection(async);

    public override Task Json_subquery_reference_pushdown_reference_pushdown_anonymous_projection(bool async)
        => base.Json_subquery_reference_pushdown_reference_pushdown_anonymous_projection(async);

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_subquery_reference_pushdown_reference_pushdown_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_subquery_reference_pushdown_reference_pushdown_reference(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_subquery_reference_pushdown_reference_pushdown_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_subquery_reference_pushdown_reference_pushdown_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_subquery_reference_pushdown_property(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_subquery_reference_pushdown_property(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Custom_naming_projection_owner_entity(bool async)
        => SpecFails.ThrowsAsync(() => base.Custom_naming_projection_owner_entity(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Custom_naming_projection_owned_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Custom_naming_projection_owned_reference(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Custom_naming_projection_owned_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Custom_naming_projection_owned_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Custom_naming_projection_owned_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Custom_naming_projection_owned_scalar(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Custom_naming_projection_everything(bool async)
        => SpecFails.ThrowsAsync(() => base.Custom_naming_projection_everything(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Project_entity_with_single_owned(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_entity_with_single_owned(async));

#if EF10 || EF11
    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task LeftJoin_json_entities(bool async)
        => SpecFails.ThrowsAsync(() => base.LeftJoin_json_entities(async));
#endif

#if EF10 || EF11
    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task RightJoin_json_entities(bool async)
        => SpecFails.ThrowsAsync(() => base.RightJoin_json_entities(async));
#endif

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Left_join_json_entities_complex_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Left_join_json_entities_complex_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Left_join_json_entities_json_being_inner(bool async)
        => SpecFails.ThrowsAsync(() => base.Left_join_json_entities_json_being_inner(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Left_join_json_entities_complex_projection_json_being_inner(bool async)
        => SpecFails.ThrowsAsync(() => base.Left_join_json_entities_complex_projection_json_being_inner(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Project_json_entity_FirstOrDefault_subquery_with_binding_on_top(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_FirstOrDefault_subquery_with_binding_on_top(async));

    public override Task Project_json_entity_FirstOrDefault_subquery_with_entity_comparison_on_top(bool async)
        => base.Project_json_entity_FirstOrDefault_subquery_with_entity_comparison_on_top(async);

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_entity_with_inheritance_basic_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_entity_with_inheritance_basic_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_entity_with_inheritance_project_derived(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_entity_with_inheritance_project_derived(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_entity_with_inheritance_project_navigations(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_entity_with_inheritance_project_navigations(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_entity_with_inheritance_project_navigations_on_derived(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_entity_with_inheritance_project_navigations_on_derived(async));

    public override Task Json_entity_backtracking(bool async)
        => base.Json_entity_backtracking(async);

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_basic(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_basic(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_ElementAt_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAt_in_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_ElementAtOrDefault_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAtOrDefault_in_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_project_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_project_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_ElementAt_project_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAt_project_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_ElementAtOrDefault_project_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAtOrDefault_project_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_using_parameter(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_parameter(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_using_column(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_column(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_using_untranslatable_client_method(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_untranslatable_client_method(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_using_untranslatable_client_method2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_untranslatable_client_method2(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_outside_bounds(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_outside_bounds(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_outside_bounds2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_outside_bounds2(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_outside_bounds_with_property_access(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_outside_bounds_with_property_access(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_nested(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_nested(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_nested_project_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_nested_project_scalar(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_nested_project_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_nested_project_reference(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_nested_project_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_nested_project_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_nested_project_collection_anonymous_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_nested_project_collection_anonymous_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_predicate_using_constant(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_predicate_using_constant(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_predicate_using_variable(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_predicate_using_variable(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_predicate_using_column(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_predicate_using_column(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_predicate_using_complex_expression1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_predicate_using_complex_expression1(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_predicate_using_complex_expression2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_predicate_using_complex_expression2(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_ElementAt_in_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAt_in_predicate(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_predicate_nested_mix(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_predicate_nested_mix(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_ElementAt_and_pushdown(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAt_and_pushdown(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_Any_with_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Any_with_predicate(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_Where_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Where_ElementAt(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_Skip(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Skip(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_OrderByDescending_Skip_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_OrderByDescending_Skip_ElementAt(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_Distinct_Count_with_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Distinct_Count_with_predicate(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_within_collection_Count(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_within_collection_Count(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_in_projection_with_composition_count(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_in_projection_with_composition_count(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_in_projection_with_anonymous_projection_of_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_in_projection_with_anonymous_projection_of_scalars(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_in_projection_with_composition_where_and_anonymous_projection_of_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_in_projection_with_composition_where_and_anonymous_projection_of_scalars(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_in_projection_with_composition_where_and_anonymous_projection_of_primitive_arrays(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_in_projection_with_composition_where_and_anonymous_projection_of_primitive_arrays(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_filter_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_filter_in_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_nested_collection_filter_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_nested_collection_filter_in_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_nested_collection_anonymous_projection_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_nested_collection_anonymous_projection_in_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_skip_take_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_skip_take_in_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_skip_take_in_projection_project_into_anonymous_type(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_skip_take_in_projection_project_into_anonymous_type(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_skip_take_in_projection_with_json_reference_access_as_final_operation(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_skip_take_in_projection_with_json_reference_access_as_final_operation(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_distinct_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_distinct_in_projection(async));

    public override Task Json_collection_anonymous_projection_distinct_in_projection(bool async)
        => base.Json_collection_anonymous_projection_distinct_in_projection(async);

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_leaf_filter_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_leaf_filter_in_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_multiple_collection_projections(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_multiple_collection_projections(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_branch_collection_distinct_and_other_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_branch_collection_distinct_and_other_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_leaf_collection_distinct_and_other_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_leaf_collection_distinct_and_other_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_of_primitives_SelectMany(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_of_primitives_SelectMany(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_of_primitives_index_used_in_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_of_primitives_index_used_in_predicate(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_of_primitives_index_used_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_of_primitives_index_used_in_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_of_primitives_index_used_in_orderby(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_of_primitives_index_used_in_orderby(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_of_primitives_contains_in_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_of_primitives_contains_in_predicate(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_with_parameter_Select_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_with_parameter_Select_ElementAt(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_with_expression_Select_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_with_expression_Select_ElementAt(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_Select_entity_collection_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Select_entity_collection_ElementAt(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_Select_entity_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Select_entity_ElementAt(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_Select_entity_in_anonymous_object_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Select_entity_in_anonymous_object_ElementAt(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_Select_entity_with_initializer_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Select_entity_with_initializer_ElementAt(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_deduplication_with_collection_indexer_in_original(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_deduplication_with_collection_indexer_in_original(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_deduplication_with_collection_indexer_in_target(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_deduplication_with_collection_indexer_in_target(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_deduplication_with_collection_in_original_and_collection_indexer_in_target(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_deduplication_with_collection_in_original_and_collection_indexer_in_target(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_using_constant_when_owner_is_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_constant_when_owner_is_present(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_using_constant_when_owner_is_not_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_constant_when_owner_is_not_present(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_using_parameter_when_owner_is_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_parameter_when_owner_is_present(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_using_parameter_when_owner_is_not_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_parameter_when_owner_is_not_present(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_after_collection_index_in_projection_using_constant_when_owner_is_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_after_collection_index_in_projection_using_constant_when_owner_is_present(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_after_collection_index_in_projection_using_constant_when_owner_is_not_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_after_collection_index_in_projection_using_constant_when_owner_is_not_present(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_after_collection_index_in_projection_using_parameter_when_owner_is_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_after_collection_index_in_projection_using_parameter_when_owner_is_present(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_after_collection_index_in_projection_using_parameter_when_owner_is_not_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_after_collection_index_in_projection_using_parameter_when_owner_is_not_present(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_when_owner_is_present_misc1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_present_misc1(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_when_owner_is_not_present_misc1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_not_present_misc1(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_when_owner_is_present_misc2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_present_misc2(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_when_owner_is_not_present_misc2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_not_present_misc2(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_when_owner_is_present_multiple(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_present_multiple(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_collection_index_in_projection_when_owner_is_not_present_multiple(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_not_present_multiple(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_scalar_required_null_semantics(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_scalar_required_null_semantics(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_scalar_optional_null_semantics(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_scalar_optional_null_semantics(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Group_by_on_json_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Group_by_on_json_scalar(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Group_by_on_json_scalar_using_collection_indexer(bool async)
        => SpecFails.ThrowsAsync(() => base.Group_by_on_json_scalar_using_collection_indexer(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Group_by_First_on_json_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Group_by_First_on_json_scalar(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Group_by_FirstOrDefault_on_json_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Group_by_FirstOrDefault_on_json_scalar(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Group_by_Skip_Take_on_json_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Group_by_Skip_Take_on_json_scalar(async));

    public override Task Group_by_json_scalar_Orderby_json_scalar_FirstOrDefault(bool async)
        => base.Group_by_json_scalar_Orderby_json_scalar_FirstOrDefault(async);

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Group_by_json_scalar_Skip_First_project_json_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Group_by_json_scalar_Skip_First_project_json_scalar(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_include_on_json_entity(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_include_on_json_entity(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_include_on_entity_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_include_on_entity_reference(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_include_on_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_include_on_entity_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Entity_including_collection_with_json(bool async)
        => SpecFails.ThrowsAsync(() => base.Entity_including_collection_with_json(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_include_on_entity_collection_and_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_include_on_entity_collection_and_reference(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_projection_of_json_reference_leaf_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_json_reference_leaf_and_entity_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_projection_of_json_reference_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_json_reference_and_entity_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_projection_of_multiple_json_references_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_multiple_json_references_and_entity_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_projection_of_json_collection_leaf_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_json_collection_leaf_and_entity_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_projection_of_json_collection_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_json_collection_and_entity_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_projection_of_json_collection_element_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_json_collection_element_and_entity_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_with_projection_of_mix_of_json_collections_json_references_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_mix_of_json_collections_json_references_and_entity_collection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_all_types_entity_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_all_types_entity_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_all_types_projection_from_owned_entity_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_all_types_projection_from_owned_entity_reference(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_all_types_projection_individual_properties(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_all_types_projection_individual_properties(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_boolean_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_boolean_predicate(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_boolean_predicate_negated(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_boolean_predicate_negated(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_boolean_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_boolean_projection(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_boolean_projection_negated(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_boolean_projection_negated(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_default_string(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_default_string(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_max_length_string(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_max_length_string(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_string_condition(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_string_condition(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_byte(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_byte(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_byte_array(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_byte_array(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_character(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_character(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_datetime(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_datetime(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_datetimeoffset(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_datetimeoffset(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_decimal(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_decimal(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_double(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_double(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_guid(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_guid(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_int16(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_int16(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_int32(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_int32(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_int64(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_int64(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_signedbyte(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_signedbyte(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_single(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_single(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_timespan(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_timespan(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_dateonly(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_dateonly(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_timeonly(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_timeonly(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_unisgnedint16(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_unisgnedint16(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_unsignedint32(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_unsignedint32(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_unsignedint64(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_unsignedint64(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_enum(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_enum(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_enumwithintconverter(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_enumwithintconverter(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_nullableenum1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableenum1(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_nullableenum2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableenum2(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_nullableenumwithconverterthathandlesnulls1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableenumwithconverterthathandlesnulls1(async));

    public override Task Json_predicate_on_nullableenumwithconverterthathandlesnulls2(bool async)
        => base.Json_predicate_on_nullableenumwithconverterthathandlesnulls2(async);

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_nullableenumwithconverter1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableenumwithconverter1(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_nullableenumwithconverter2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableenumwithconverter2(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_nullableint321(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableint321(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_nullableint322(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableint322(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_bool_converted_to_int_zero_one(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_bool_converted_to_int_zero_one(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_bool_converted_to_int_zero_one_with_explicit_comparison(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_bool_converted_to_int_zero_one_with_explicit_comparison(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_bool_converted_to_string_True_False(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_bool_converted_to_string_True_False(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_bool_converted_to_string_True_False_with_explicit_comparison(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_bool_converted_to_string_True_False_with_explicit_comparison(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_bool_converted_to_string_Y_N(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_bool_converted_to_string_Y_N(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_bool_converted_to_string_Y_N_with_explicit_comparison(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_bool_converted_to_string_Y_N_with_explicit_comparison(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_int_zero_one_converted_to_bool(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_int_zero_one_converted_to_bool(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_string_True_False_converted_to_bool(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_string_True_False_converted_to_bool(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_predicate_on_string_Y_N_converted_to_bool(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_string_Y_N_converted_to_bool(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_collection_element_and_reference_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_collection_element_and_reference_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_nothing_interesting_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_nothing_interesting_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_owner_entity_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_owner_entity_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_nested_collection_anonymous_projection_of_primitives_in_projection_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_nested_collection_anonymous_projection_of_primitives_in_projection_NoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_second_element_through_collection_element_constant_projected_after_owner_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_second_element_through_collection_element_constant_projected_after_owner_nested_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_reference_collection_and_collection_element_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_reference_collection_and_collection_element_nested_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_second_element_through_collection_element_parameter_correctly_projected_after_owner_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_second_element_through_collection_element_parameter_correctly_projected_after_owner_nested_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_only_second_element_through_collection_element_constant_projected_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_only_second_element_through_collection_element_constant_projected_nested_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_only_second_element_through_collection_element_parameter_projected_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_only_second_element_through_collection_element_parameter_projected_nested_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_second_element_through_collection_element_constant_different_values_projected_before_owner_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_second_element_through_collection_element_constant_different_values_projected_before_owner_nested_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_nested_collection_and_element_correct_order_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_nested_collection_and_element_correct_order_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_nested_collection_element_using_parameter_and_the_owner_in_correct_order_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_nested_collection_element_using_parameter_and_the_owner_in_correct_order_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_second_element_projected_before_owner_as_well_as_root_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_second_element_projected_before_owner_as_well_as_root_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Json_projection_second_element_projected_before_owner_nested_as_well_as_root_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_second_element_projected_before_owner_nested_as_well_as_root_AsNoTrackingWithIdentityResolution(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Project_json_reference_in_tracking_query_fails(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_reference_in_tracking_query_fails(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Project_json_collection_in_tracking_query_fails(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_collection_in_tracking_query_fails(async));

    // Fails: Seeding the JSON model throws OverflowException (uint values above Int32.MaxValue written through the Int32 representation) EF-X548
    public override Task Project_json_entity_in_tracking_query_fails_even_when_owner_is_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_in_tracking_query_fails_even_when_owner_is_present(async));

#if EF11
    // The seeds of the ad hoc JSON tests (merged into this base on EF11) insert raw relational JSON column values, which has no MongoDB
    // equivalent: they are not implemented, and the fixture failure is what every test asserts anyway.
    protected override Task Seed30028(DbContext ctx)
        => Task.CompletedTask;

    protected override Task Seed33046(DbContext ctx)
        => Task.CompletedTask;

    protected override Task SeedBadJsonProperties(ContextBadJsonProperties ctx)
        => Task.CompletedTask;

    protected override Task SeedJunkInJson(DbContext ctx)
        => Task.CompletedTask;

    protected override Task SeedNotICollection(DbContext ctx)
        => Task.CompletedTask;

    protected override Task SeedShadowProperties(DbContext ctx)
        => Task.CompletedTask;

    protected override Task SeedTrickyBuffering(DbContext ctx)
        => Task.CompletedTask;

#endif
    public class JsonQueryMongoFixture : JsonQueryFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("JsonQuery");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        public override Microsoft.EntityFrameworkCore.TestModels.JsonQuery.JsonQueryContext CreateContext()
        {
            _initFailure.ThrowIfFailed();
            return base.CreateContext();
        }
    }
}

#endif
