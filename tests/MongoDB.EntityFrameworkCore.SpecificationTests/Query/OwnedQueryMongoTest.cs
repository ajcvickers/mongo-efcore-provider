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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class OwnedQueryMongoTest : OwnedQueryTestBase<OwnedQueryMongoTest.OwnedQueryMongoFixture>
{
    public OwnedQueryMongoTest(OwnedQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Can_query_owner_with_different_owned_types_having_same_property_name_in_hierarchy(bool async)
    {
        await base.Can_query_owner_with_different_owned_types_having_same_property_name_in_hierarchy(async);
    }

    public override async Task Query_with_owned_entity_equality_operator(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Query_with_owned_entity_equality_operator(async), "could not be translated");
    }

    public override async Task Query_with_owned_entity_equality_method(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Query_with_owned_entity_equality_method(async), "could not be translated");
    }

    public override async Task Query_with_owned_entity_equality_object_method(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Query_with_owned_entity_equality_object_method(async), "could not be translated");
    }

    public override async Task Query_for_base_type_loads_all_owned_navs(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Query_for_base_type_loads_all_owned_navs(async), "derivedType");
    }

    public override async Task No_ignored_include_warning_when_implicit_load(bool async)
    {
        await base.No_ignored_include_warning_when_implicit_load(async);
    }

    public override async Task Query_for_branch_type_loads_all_owned_navs(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Query_for_branch_type_loads_all_owned_navs(async), "derivedType");
    }

    public override async Task Query_for_branch_type_loads_all_owned_navs_tracking(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Query_for_branch_type_loads_all_owned_navs_tracking(async), "derivedType");
    }

    public override async Task Query_for_leaf_type_loads_all_owned_navs(bool async)
    {
        // Fails: Owned navigations on a derived type fail to materialize: discriminator mismatch EF-X519
        await SpecFails.ThrowsAsync(() => base.Query_for_leaf_type_loads_all_owned_navs(async), "No discriminators matched");
    }

    public override async Task Query_when_subquery(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Query_when_subquery(async), "derivedType");
    }

#if EF9 || EF10
    public override async Task Project_owned_reference_navigation_which_owns_additional(bool async)
    {
        await base.Project_owned_reference_navigation_which_owns_additional(async);
    }
#endif

#if EF9 || EF10
    public override async Task Project_owned_reference_navigation_which_does_not_own_additional(bool async)
    {
        await base.Project_owned_reference_navigation_which_does_not_own_additional(async);
    }
#endif

    public override async Task Navigation_rewrite_on_owned_reference_projecting_scalar(bool async)
    {
        await base.Navigation_rewrite_on_owned_reference_projecting_scalar(async);
    }

    public override async Task Navigation_rewrite_on_owned_reference_projecting_entity(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_projecting_entity(async), "derivedType");
    }

    public override async Task Navigation_rewrite_on_owned_collection(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_collection(async), "derivedType");
    }

    public override async Task Navigation_rewrite_on_owned_collection_with_composition(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_collection_with_composition(async), "could not be translated");
    }

    public override async Task Navigation_rewrite_on_owned_collection_with_composition_complex(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_collection_with_composition_complex(async), "could not be translated");
    }

    public override async Task SelectMany_on_owned_collection(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.SelectMany_on_owned_collection(async), "could not be translated");
    }

#if EF9 || EF10
    public override async Task SelectMany_with_result_selector(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_result_selector(async), "could not be translated");
    }
#endif

    public override async Task Set_throws_for_owned_type(bool async)
    {
        await base.Set_throws_for_owned_type(async);
    }

#if EF10
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity(bool async)
    {
        await base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity(async);
    }
#endif

#if EF10
    public override async Task Filter_owned_entity_chained_with_regular_entity_followed_by_projecting_owned_collection(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Filter_owned_entity_chained_with_regular_entity_followed_by_projecting_owned_collection(async), "derivedType");
    }
#endif

#if EF10
    public override async Task Project_multiple_owned_navigations(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Project_multiple_owned_navigations(async), "derivedType");
    }
#endif

    public override async Task Project_multiple_owned_navigations_with_expansion_on_owned_collections(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Project_multiple_owned_navigations_with_expansion_on_owned_collections(async), "could not be translated");
    }

#if EF10
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_filter(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_filter(async), "derivedType");
    }
#endif

#if EF10
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_property(bool async)
    {
        await base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_property(async);
    }
#endif

    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_collection(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_collection(async), "could not be translated");
    }

    public override async Task SelectMany_on_owned_reference_followed_by_regular_entity_and_collection(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.SelectMany_on_owned_reference_followed_by_regular_entity_and_collection(async), "could not be translated");
    }

    public override async Task SelectMany_on_owned_reference_with_entity_in_between_ending_in_owned_collection(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.SelectMany_on_owned_reference_with_entity_in_between_ending_in_owned_collection(async), "could not be translated");
    }

#if EF10
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_collection_count(bool async)
    {
        // Fails: Server aggregate error for owned-collection element access EF-X516
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_collection_count(async), "Command aggregate failed");
    }
#endif

#if EF10
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference(bool async)
    {
        // Fails: Wrong results for owned-collection operators (ElementAt/Union/...) EF-X515
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference(async), "Values differ");
    }
#endif

#if EF10
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference_and_scalar(bool async)
    {
        await base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference_and_scalar(async);
    }
#endif

#if EF10
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference_in_predicate_and_projection(bool async)
    {
        // Fails: Wrong results for owned-collection operators (ElementAt/Union/...) EF-X515
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference_in_predicate_and_projection(async), "Values differ");
    }
#endif

    public override async Task Query_with_OfType_eagerly_loads_correct_owned_navigations(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Query_with_OfType_eagerly_loads_correct_owned_navigations(async), "derivedType");
    }

#if EF10
    public override async Task Query_loads_reference_nav_automatically_in_projection(bool async)
    {
        await base.Query_loads_reference_nav_automatically_in_projection(async);
    }
#endif

    public override async Task Throw_for_owned_entities_without_owner_in_tracking_query(bool async)
    {
        await base.Throw_for_owned_entities_without_owner_in_tracking_query(async);
    }

    public override async Task Owned_entity_without_owner_does_not_throw_for_identity_resolution(bool async, bool useAsTracking)
    {
        await base.Owned_entity_without_owner_does_not_throw_for_identity_resolution(async, useAsTracking);
    }

#if EF9 || EF10
    public override async Task Preserve_includes_when_applying_skip_take_after_anonymous_type_select(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Preserve_includes_when_applying_skip_take_after_anonymous_type_select(async), "could not be translated");
    }
#endif

    public override async Task Unmapped_property_projection_loads_owned_navigations(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Unmapped_property_projection_loads_owned_navigations(async), "Expression not supported");
    }

    public override async Task Client_method_skip_loads_owned_navigations(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Client_method_skip_loads_owned_navigations(async), "derivedType");
    }

    public override async Task Client_method_take_loads_owned_navigations(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Client_method_take_loads_owned_navigations(async), "derivedType");
    }

    public override async Task Client_method_skip_take_loads_owned_navigations(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Client_method_skip_take_loads_owned_navigations(async), "derivedType");
    }

    public override async Task Client_method_skip_loads_owned_navigations_variation_2(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Client_method_skip_loads_owned_navigations_variation_2(async), "Expression not supported");
    }

    public override async Task Client_method_take_loads_owned_navigations_variation_2(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Client_method_take_loads_owned_navigations_variation_2(async), "Expression not supported");
    }

    public override async Task Client_method_skip_take_loads_owned_navigations_variation_2(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Client_method_skip_take_loads_owned_navigations_variation_2(async), "Expression not supported");
    }

    public override async Task Where_owned_collection_navigation_ToList_Count(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Where_owned_collection_navigation_ToList_Count(async), "could not be translated");
    }

    public override async Task Where_collection_navigation_ToArray_Count(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Where_collection_navigation_ToArray_Count(async), "could not be translated");
    }

    public override async Task Where_collection_navigation_AsEnumerable_Count(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Where_collection_navigation_AsEnumerable_Count(async), "could not be translated");
    }

    public override async Task Where_collection_navigation_ToList_Count_member(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Where_collection_navigation_ToList_Count_member(async), "could not be translated");
    }

    public override async Task Where_collection_navigation_ToArray_Length_member(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Where_collection_navigation_ToArray_Length_member(async), "could not be translated");
    }

    public override async Task Can_query_on_indexer_properties(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Can_query_on_indexer_properties(async), "Expression not supported");
    }

    public override async Task Can_query_on_owned_indexer_properties(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Can_query_on_owned_indexer_properties(async), "Expression not supported");
    }

    public override async Task Can_query_on_indexer_property_when_property_name_from_closure(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Can_query_on_indexer_property_when_property_name_from_closure(async), "Expression not supported");
    }

    public override async Task Can_project_indexer_properties(bool async)
    {
        await base.Can_project_indexer_properties(async);
    }

    public override async Task Can_project_owned_indexer_properties(bool async)
    {
        await base.Can_project_owned_indexer_properties(async);
    }

    public override async Task Can_project_indexer_properties_converted(bool async)
    {
        await base.Can_project_indexer_properties_converted(async);
    }

    public override async Task Can_project_owned_indexer_properties_converted(bool async)
    {
        await base.Can_project_owned_indexer_properties_converted(async);
    }

    public override async Task Can_OrderBy_indexer_properties(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Can_OrderBy_indexer_properties(async), "Expression not supported");
    }

    public override async Task Can_OrderBy_indexer_properties_converted(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Can_OrderBy_indexer_properties_converted(async), "Expression not supported");
    }

    public override async Task Can_OrderBy_owned_indexer_properties(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Can_OrderBy_owned_indexer_properties(async), "Expression not supported");
    }

#if EF9 || EF10
    public override async Task Can_OrderBy_owned_indexer_properties_converted(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Can_OrderBy_owned_indexer_properties_converted(async), "Expression not supported");
    }
#endif

    public override async Task Can_group_by_indexer_property(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Can_group_by_indexer_property(async), "could not be translated");
    }

    public override async Task Can_group_by_converted_indexer_property(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Can_group_by_converted_indexer_property(async), "could not be translated");
    }

    public override async Task Can_group_by_owned_indexer_property(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Can_group_by_owned_indexer_property(async), "could not be translated");
    }

    public override async Task Can_group_by_converted_owned_indexer_property(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Can_group_by_converted_owned_indexer_property(async), "could not be translated");
    }

    public override async Task Can_join_on_indexer_property_on_query(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Can_join_on_indexer_property_on_query(async), "Expression not supported");
    }

    public override async Task Projecting_indexer_property_ignores_include(bool async)
    {
        // Fails: Projecting an owned entity (indexer) without its owner in a tracking query EF-X517
        await SpecFails.ThrowsAsync(() => base.Projecting_indexer_property_ignores_include(async), "owned entity without a corresponding owner");
    }

    public override async Task Projecting_indexer_property_ignores_include_converted(bool async)
    {
        // Fails: Projecting an owned entity (indexer) without its owner in a tracking query EF-X517
        await SpecFails.ThrowsAsync(() => base.Projecting_indexer_property_ignores_include_converted(async), "owned entity without a corresponding owner");
    }

    public override async Task Indexer_property_is_pushdown_into_subquery(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Indexer_property_is_pushdown_into_subquery(async), "Expression not supported");
    }

    public override async Task Can_query_indexer_property_on_owned_collection(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Can_query_indexer_property_on_owned_collection(async), "Expression not supported");
    }

    public override async Task NoTracking_Include_with_cycles_throws(bool async)
    {
        await base.NoTracking_Include_with_cycles_throws(async);
    }

    public override async Task NoTracking_Include_with_cycles_does_not_throw_when_performing_identity_resolution(bool async, bool useAsTracking)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.NoTracking_Include_with_cycles_does_not_throw_when_performing_identity_resolution(async, useAsTracking), "could not be translated");
    }

    public override async Task Trying_to_access_non_existent_indexer_property_throws_meaningful_exception(bool async)
    {
        // Fails: Provider throws a different exception type than the base test expects EF-X518
        await SpecFails.ThrowsAsync(() => base.Trying_to_access_non_existent_indexer_property_throws_meaningful_exception(async), "Exception type was not an exact match");
    }

    public override async Task GroupBy_with_multiple_aggregates_on_owned_navigation_properties(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.GroupBy_with_multiple_aggregates_on_owned_navigation_properties(async), "could not be translated");
    }

    public override async Task Non_nullable_property_through_optional_navigation(bool async)
    {
        // Fails: Provider throws a different exception type than the base test expects EF-X518
        await SpecFails.ThrowsAsync(() => base.Non_nullable_property_through_optional_navigation(async), "Exception type was not an exact match");
    }

    public override async Task Ordering_by_identifying_projection(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Ordering_by_identifying_projection(async), "derivedType");
    }

    public override async Task Query_on_collection_entry_works_for_owned_collection(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Query_on_collection_entry_works_for_owned_collection(async), "derivedType");
    }

    public override async Task Projecting_collection_correlated_with_keyless_entity_after_navigation_works_using_parent_identifiers(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Projecting_collection_correlated_with_keyless_entity_after_navigation_works_using_parent_identifiers(async), "could not be translated");
    }

    public override async Task Filter_on_indexer_using_closure(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Filter_on_indexer_using_closure(async), "Expression not supported");
    }

    public override async Task Filter_on_indexer_using_function_argument(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Filter_on_indexer_using_function_argument(async), "Expression not supported");
    }

    public override async Task Simple_query_entity_with_owned_collection(bool async)
    {
        await base.Simple_query_entity_with_owned_collection(async);
    }

#if EF10
    public override async Task Left_join_on_entity_with_owned_navigations(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Left_join_on_entity_with_owned_navigations(async), "derivedType");
    }
#endif

#if EF10
    public override async Task Left_join_on_entity_with_owned_navigations_complex(bool async)
    {
        // Fails: Left join on entity with owned navigations throws ArgumentNullException (key) EF-X521
        await SpecFails.ThrowsAsync(() => base.Left_join_on_entity_with_owned_navigations_complex(async), "Parameter 'key'");
    }
#endif

    public override async Task GroupBy_aggregate_on_owned_navigation_in_aggregate_selector(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.GroupBy_aggregate_on_owned_navigation_in_aggregate_selector(async), "could not be translated");
    }

#if EF9 || EF10
    public override async Task Count_over_owned_collection(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Count_over_owned_collection(async), "derivedType");
    }
#endif

#if EF9 || EF10
    public override async Task Any_without_predicate_over_owned_collection(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Any_without_predicate_over_owned_collection(async), "derivedType");
    }
#endif

#if EF9 || EF10
    public override async Task Any_with_predicate_over_owned_collection(bool async)
    {
        // Fails: Wrong results for owned-collection operators (ElementAt/Union/...) EF-X515
        await SpecFails.ThrowsAsync(() => base.Any_with_predicate_over_owned_collection(async), "Values differ");
    }
#endif

#if EF9 || EF10
    public override async Task Contains_over_owned_collection(bool async)
    {
        // Fails: Owned collection Contains over indexer property builds an invalid expression EF-X520
        await SpecFails.ThrowsAsync(() => base.Contains_over_owned_collection(async), "Incorrect number of arguments");
    }
#endif

#if EF9 || EF10
    public override async Task ElementAt_over_owned_collection(bool async)
    {
        // Fails: Wrong results for owned-collection operators (ElementAt/Union/...) EF-X515
        await SpecFails.ThrowsAsync(() => base.ElementAt_over_owned_collection(async), "Values differ");
    }
#endif

#if EF9 || EF10
    public override async Task ElementAtOrDefault_over_owned_collection(bool async)
    {
        // Fails: Server aggregate error for owned-collection element access EF-X516
        await SpecFails.ThrowsAsync(() => base.ElementAtOrDefault_over_owned_collection(async), "Command aggregate failed");
    }
#endif

#if EF9 || EF10
    public override async Task OrderBy_ElementAt_over_owned_collection(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.OrderBy_ElementAt_over_owned_collection(async), "Expression not supported");
    }
#endif

#if EF9 || EF10
    public override async Task Skip_Take_over_owned_collection(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Skip_Take_over_owned_collection(async), "derivedType");
    }
#endif

#if EF9 || EF10
    public override async Task FirstOrDefault_over_owned_collection(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.FirstOrDefault_over_owned_collection(async), "Expression not supported");
    }
#endif

#if EF9 || EF10
    public override async Task Distinct_over_owned_collection(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Distinct_over_owned_collection(async), "derivedType");
    }
#endif

#if EF9 || EF10
    public override async Task Union_over_owned_collection(bool async)
    {
        // Fails: Wrong results for owned-collection operators (ElementAt/Union/...) EF-X515
        await SpecFails.ThrowsAsync(() => base.Union_over_owned_collection(async), "Values differ");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Filter_owned_entity_chained_with_regular_entity_followed_by_projecting_owned_collection(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Filter_owned_entity_chained_with_regular_entity_followed_by_projecting_owned_collection(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Project_multiple_owned_navigations(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Project_multiple_owned_navigations(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_filter(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_filter(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_property(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_property(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_collection_count(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_collection_count(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference_and_scalar(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference_and_scalar(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference_in_predicate_and_projection(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Navigation_rewrite_on_owned_reference_followed_by_regular_entity_and_another_reference_in_predicate_and_projection(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Query_loads_reference_nav_automatically_in_projection(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Query_loads_reference_nav_automatically_in_projection(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Left_join_on_entity_with_owned_navigations(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Left_join_on_entity_with_owned_navigations(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Left_join_on_entity_with_owned_navigations_complex(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Left_join_on_entity_with_owned_navigations_complex(async), "could not be translated");
    }
#endif

#if EF8
    public override async Task Preserve_includes_when_applying_skip_take_after_anonymous_type_select(bool async)
    {
        // Fails: Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) EF-X512
        await SpecFails.ThrowsAsync(() => base.Preserve_includes_when_applying_skip_take_after_anonymous_type_select(async), "derivedType");
    }
#endif

#if EF8
    public override async Task Can_OrderBy_owened_indexer_properties_converted(bool async)
    {
        // Fails: Owned indexer properties / owned-collection element expressions not supported by driver LINQ EF-X514
        await SpecFails.ThrowsAsync(() => base.Can_OrderBy_owened_indexer_properties_converted(async), "Expression not supported");
    }
#endif

    public class OwnedQueryMongoFixture : OwnedQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("OwnedQuery");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // The base model's HasData supplies Order.Id and OrderDetail.OrderId/Id as the key parts. Without these
            // explicit keys the provider's owned-collection key convention adds shadow key parts (Id1, OrderId1)
            // that HasData cannot supply values for ("The seed entity ... no value was provided for ... 'Id1'").
            modelBuilder.Entity<OwnedPerson>().OwnsMany(p => p.Orders, ob =>
            {
                ob.HasKey("ClientId", "Id");
                ob.OwnsMany(o => o.Details, odb =>
                {
                    odb.WithOwner().HasForeignKey("OrderClientId", "OrderId");
                    odb.HasKey("OrderClientId", "OrderId", "Id");
                });
            });
        }

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server);

            await base.InitializeAsync();
        }
    }
}
