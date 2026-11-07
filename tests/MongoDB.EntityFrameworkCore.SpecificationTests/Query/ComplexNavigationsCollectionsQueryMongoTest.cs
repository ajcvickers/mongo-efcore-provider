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

public class ComplexNavigationsCollectionsQueryMongoTest : ComplexNavigationsCollectionsQueryTestBase<ComplexNavigationsCollectionsQueryMongoTest.ComplexNavigationsCollectionsQueryMongoFixture>
{
    public ComplexNavigationsCollectionsQueryMongoTest(ComplexNavigationsCollectionsQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Select_nav_prop_collection_one_to_many_required(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_nav_prop_collection_one_to_many_required(async), "could not be translated");
    }

    public override async Task Complex_SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_with_other_query_operators_composed_on_top(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_with_other_query_operators_composed_on_top(async), "could not be translated");
    }

    public override async Task Project_collection_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation(async), "could not be translated");
    }

    public override async Task Project_collection_navigation_nested(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation_nested(async), "could not be translated");
    }

    public override async Task Project_collection_navigation_nested_with_take(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation_nested_with_take(async), "could not be translated");
    }

    public override async Task Project_collection_navigation_using_ef_property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation_using_ef_property(async), "could not be translated");
    }

    public override async Task Project_collection_navigation_nested_anonymous(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation_nested_anonymous(async), "could not be translated");
    }

    public override async Task Project_collection_navigation_composed(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation_composed(async), "could not be translated");
    }

    public override async Task Project_collection_and_root_entity(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_and_root_entity(async), "could not be translated");
    }

    public override async Task Project_collection_and_include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_and_include(async), "could not be translated");
    }

    public override async Task Project_navigation_and_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_navigation_and_collection(async), "could not be translated");
    }

    public override async Task Include_inside_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_inside_subquery(async), "could not be translated");
    }

    public override async Task Null_check_in_anonymous_type_projection_should_not_be_removed(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Null_check_in_anonymous_type_projection_should_not_be_removed(async), "could not be translated");
    }

    public override async Task Null_check_in_Dto_projection_should_not_be_removed(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Null_check_in_Dto_projection_should_not_be_removed(async), "could not be translated");
    }

    public override async Task SelectMany_navigation_property_followed_by_select_collection_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property_followed_by_select_collection_navigation(async), "could not be translated");
    }

    public override async Task Multiple_SelectMany_navigation_property_followed_by_select_collection_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_navigation_property_followed_by_select_collection_navigation(async), "could not be translated");
    }

    public override async Task SelectMany_navigation_property_with_include_and_followed_by_select_collection_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property_with_include_and_followed_by_select_collection_navigation(async), "could not be translated");
    }

    public override async Task Lift_projection_mapping_when_pushing_down_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Lift_projection_mapping_when_pushing_down_subquery(async), "could not be translated");
    }

    public override async Task Select_subquery_single_nested_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_single_nested_subquery(async), "could not be translated");
    }

    public override async Task Select_subquery_single_nested_subquery2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_single_nested_subquery2(async), "could not be translated");
    }

    public override async Task Queryable_in_subquery_works_when_final_projection_is_List(bool async)
    {
        await base.Queryable_in_subquery_works_when_final_projection_is_List(async);
    }

    public override async Task Complex_query_with_let_collection_projection_FirstOrDefault_with_ToList_on_inner_and_outer(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_query_with_let_collection_projection_FirstOrDefault_with_ToList_on_inner_and_outer(async), "could not be translated");
    }

    public override async Task Complex_query_with_let_collection_projection_FirstOrDefault(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_query_with_let_collection_projection_FirstOrDefault(async), "could not be translated");
    }

    public override async Task SelectMany_DefaultIfEmpty_multiple_times_with_joins_projecting_a_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_DefaultIfEmpty_multiple_times_with_joins_projecting_a_collection(async), "could not be translated");
    }

    public override async Task Take_Select_collection_Take(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Take_Select_collection_Take(async), "could not be translated");
    }

    public override async Task Skip_Take_Select_collection_Skip_Take(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Skip_Take_Select_collection_Skip_Take(async), "could not be translated");
    }

    public override async Task Multi_level_include_one_to_many_optional_and_one_to_many_optional_produces_valid_sql(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Multi_level_include_one_to_many_optional_and_one_to_many_optional_produces_valid_sql(async), "Failure");
    }

    public override async Task Multi_level_include_correct_PK_is_chosen_as_the_join_predicate_for_queries_that_join_same_table_multiple_times(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Multi_level_include_correct_PK_is_chosen_as_the_join_predicate_for_queries_that_join_same_table_multiple_times(async), "Failure");
    }

#if EF10
    public override async Task Multiple_complex_includes(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Multiple_complex_includes(async), "Document element is missing for required");
    }
#endif

#if EF10
    public override async Task Multiple_complex_includes_EF_Property(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Multiple_complex_includes_EF_Property(async), "Document element is missing for required");
    }
#endif

#if EF10
    public override async Task Multiple_complex_includes_self_ref(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Multiple_complex_includes_self_ref(async), "Failure");
    }
#endif

#if EF10
    public override async Task Multiple_complex_includes_self_ref_EF_Property(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Multiple_complex_includes_self_ref_EF_Property(async), "Failure");
    }
#endif

#if EF10
    public override async Task Include_reference_and_collection_order_by(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Include_reference_and_collection_order_by(async), "Document element is missing for required");
    }
#endif

#if EF10
    public override async Task Include_reference_ThenInclude_collection_order_by(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Include_reference_ThenInclude_collection_order_by(async), "Document element is missing for required");
    }
#endif

    public override async Task Include_collection_then_reference(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_then_reference(async), "Failure");
    }

    public override async Task Include_collection_with_conditional_order_by(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_conditional_order_by(async), "Failure");
    }

#if EF10
    public override async Task Multiple_complex_include_select(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Multiple_complex_include_select(async), "Document element is missing for required");
    }
#endif

#if EF10
    public override async Task Include_nested_with_optional_navigation(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Include_nested_with_optional_navigation(async), "Document element is missing for required");
    }
#endif

#if EF10
    public override async Task Complex_multi_include_with_order_by_and_paging(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Complex_multi_include_with_order_by_and_paging(async), "Failure");
    }
#endif

#if EF10
    public override async Task Complex_multi_include_with_order_by_and_paging_joins_on_correct_key(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Complex_multi_include_with_order_by_and_paging_joins_on_correct_key(async), "Failure");
    }
#endif

#if EF10
    public override async Task Complex_multi_include_with_order_by_and_paging_joins_on_correct_key2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Complex_multi_include_with_order_by_and_paging_joins_on_correct_key2(async), "Failure");
    }
#endif

    public override async Task Multiple_include_with_multiple_optional_navigations(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_include_with_multiple_optional_navigations(async), "could not be translated");
    }

    public override async Task SelectMany_with_Include1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_Include1(async), "could not be translated");
    }

    public override async Task Orderby_SelectMany_with_Include1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Orderby_SelectMany_with_Include1(async), "could not be translated");
    }

    public override async Task SelectMany_with_Include2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_Include2(async), "could not be translated");
    }

    public override async Task SelectMany_with_Include_ThenInclude(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_Include_ThenInclude(async), "could not be translated");
    }

    public override async Task Multiple_SelectMany_with_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_with_Include(async), "could not be translated");
    }

    public override async Task Required_navigation_with_Include(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Required_navigation_with_Include(async), "Failure");
    }

#if EF10
    public override async Task Required_navigation_with_Include_ThenInclude(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Required_navigation_with_Include_ThenInclude(async), "Failure");
    }
#endif

#if EF10
    public override async Task Optional_navigation_with_Include_ThenInclude(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_with_Include_ThenInclude(async), "Document element is missing for required");
    }
#endif

#if EF10
    public override async Task Multiple_optional_navigation_with_Include(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Multiple_optional_navigation_with_Include(async), "Failure");
    }
#endif

#if EF10
    public override async Task Multiple_optional_navigation_with_string_based_Include(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Multiple_optional_navigation_with_string_based_Include(async), "Failure");
    }
#endif

#if EF10
    public override async Task Optional_navigation_with_order_by_and_Include(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_with_order_by_and_Include(async), "Document element is missing for required");
    }
#endif

#if EF10
    public override async Task Optional_navigation_with_Include_and_order(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_with_Include_and_order(async), "Document element is missing for required");
    }
#endif

    public override async Task SelectMany_with_order_by_and_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_order_by_and_Include(async), "could not be translated");
    }

    public override async Task SelectMany_with_Include_and_order_by(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_Include_and_order_by(async), "could not be translated");
    }

    public override async Task SelectMany_with_navigation_and_Distinct(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_navigation_and_Distinct(async), "could not be translated");
    }

    public override async Task SelectMany_with_navigation_and_Distinct_projecting_columns_including_join_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_navigation_and_Distinct_projecting_columns_including_join_key(async), "could not be translated");
    }

    public override async Task Include_collection_with_multiple_orderbys_member(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_multiple_orderbys_member(async), "Failure");
    }

    public override async Task Include_collection_with_multiple_orderbys_property(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_multiple_orderbys_property(async), "Failure");
    }

    public override async Task Include_collection_with_multiple_orderbys_methodcall(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_multiple_orderbys_methodcall(async), "Failure");
    }

    public override async Task Include_collection_with_multiple_orderbys_complex(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_multiple_orderbys_complex(async), "Failure");
    }

    public override async Task Include_collection_with_multiple_orderbys_complex_repeated(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_multiple_orderbys_complex_repeated(async), "Failure");
    }

    public override async Task Include_collection_with_multiple_orderbys_complex_repeated_checked(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_multiple_orderbys_complex_repeated_checked(async), "Failure");
    }

    public override async Task Include_collection_with_groupby_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_groupby_in_subquery(async), "could not be translated");
    }

    public override async Task Include_collection_with_groupby_in_subquery_and_filter_before_groupby(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_groupby_in_subquery_and_filter_before_groupby(async), "could not be translated");
    }

    public override async Task Include_collection_with_groupby_in_subquery_and_filter_after_groupby(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_groupby_in_subquery_and_filter_after_groupby(async), "could not be translated");
    }

#if EF10
    public override async Task Include_reference_collection_order_by_reference_navigation(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Include_reference_collection_order_by_reference_navigation(async), "Document element is missing for required");
    }
#endif

    public override async Task Include_after_Select(bool async)
    {
        await base.Include_after_Select(async);
    }

    public override async Task Include_after_SelectMany_and_reference_navigation(bool async)
    {
        await base.Include_after_SelectMany_and_reference_navigation(async);
    }

    public override async Task Include_after_SelectMany(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_after_SelectMany(async), "could not be translated");
    }

    public override async Task Include_after_multiple_SelectMany_and_reference_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_after_multiple_SelectMany_and_reference_navigation(async), "could not be translated");
    }

    public override async Task Include_after_SelectMany_and_multiple_reference_navigations(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_after_SelectMany_and_multiple_reference_navigations(async), "could not be translated");
    }

    public override async Task Include_collection(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection(async), "Failure");
    }

    public override async Task Include_collection_followed_by_include_reference(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_followed_by_include_reference(async), "Failure");
    }

#if EF10
    public override async Task Include_reference_followed_by_include_collection(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Include_reference_followed_by_include_collection(async), "Document element is missing for required");
    }
#endif

    public override async Task Include_collection_followed_by_projecting_the_included_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_collection_followed_by_projecting_the_included_collection(async), "could not be translated");
    }

    public override async Task Include_and_ThenInclude_collections_followed_by_projecting_the_first_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_and_ThenInclude_collections_followed_by_projecting_the_first_collection(async), "could not be translated");
    }

    public override async Task Include_collection_and_another_navigation_chain_followed_by_projecting_the_first_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_collection_and_another_navigation_chain_followed_by_projecting_the_first_collection(async), "could not be translated");
    }

    public override async Task Include_collection_ThenInclude_two_references(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_ThenInclude_two_references(async), "Failure");
    }

    public override async Task Include_collection_followed_by_complex_includes_and_projecting_the_included_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_collection_followed_by_complex_includes_and_projecting_the_included_collection(async), "could not be translated");
    }

    public override async Task Include_collection_multiple(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_collection_multiple(async), "Failure");
    }

    public override async Task Include_ThenInclude_ThenInclude_followed_by_two_nested_selects(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_ThenInclude_ThenInclude_followed_by_two_nested_selects(async), "could not be translated");
    }

    public override async Task Include_collection_ThenInclude_reference_followed_by_projection_into_anonmous_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_collection_ThenInclude_reference_followed_by_projection_into_anonmous_type(async), "could not be translated");
    }

    public override async Task Include_collection_multiple_with_filter(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Include_collection_multiple_with_filter(async), "Unsupported cross-DbSet query");
    }

    public override async Task Include_collection_multiple_with_filter_EF_Property(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Include_collection_multiple_with_filter_EF_Property(async), "Unsupported cross-DbSet query");
    }

    public override async Task Including_reference_navigation_and_projecting_collection_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Including_reference_navigation_and_projecting_collection_navigation(async), "could not be translated");
    }

    public override async Task LeftJoin_with_Any_on_outer_source_and_projecting_collection_from_inner(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.LeftJoin_with_Any_on_outer_source_and_projecting_collection_from_inner(async), "could not be translated");
    }

    public override async Task Filtered_include_basic_Where(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_basic_Where(async), "could not be translated");
    }

    public override async Task Filtered_include_basic_Where_EF_Property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_basic_Where_EF_Property(async), "could not be translated");
    }

    public override async Task Filtered_include_OrderBy(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_OrderBy(async), "Failure");
    }

    public override async Task Filtered_include_OrderBy_EF_Property(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_OrderBy_EF_Property(async), "Failure");
    }

    public override async Task Filtered_ThenInclude_OrderBy(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_ThenInclude_OrderBy(async), "Failure");
    }

    public override async Task Filtered_include_ThenInclude_OrderBy(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_ThenInclude_OrderBy(async), "Failure");
    }

    public override async Task Filtered_include_basic_OrderBy_Take(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_basic_OrderBy_Take(async), "Failure");
    }

    public override async Task Filtered_include_basic_OrderBy_Skip(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_basic_OrderBy_Skip(async), "Failure");
    }

    public override async Task Filtered_include_basic_OrderBy_Skip_Take(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_basic_OrderBy_Skip_Take(async), "Failure");
    }

    public override async Task Filtered_include_basic_OrderBy_Skip_Take_EF_Property(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_basic_OrderBy_Skip_Take_EF_Property(async), "Failure");
    }

    public override async Task Filtered_include_Skip_without_OrderBy(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_Skip_without_OrderBy(async), "Failure");
    }

    public override async Task Filtered_include_Take_without_OrderBy(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_Take_without_OrderBy(async), "Failure");
    }

    public override async Task Filtered_include_on_ThenInclude(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_on_ThenInclude(async), "could not be translated");
    }

    public override async Task Filtered_include_on_ThenInclude_EF_Property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_on_ThenInclude_EF_Property(async), "could not be translated");
    }

    public override async Task Filtered_include_after_reference_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_after_reference_navigation(async), "could not be translated");
    }

    public override async Task Filtered_include_after_different_filtered_include_same_level(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_after_different_filtered_include_same_level(async), "could not be translated");
    }

    public override async Task Filtered_include_after_different_filtered_include_different_level(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_after_different_filtered_include_different_level(async), "could not be translated");
    }

    public override async Task Filtered_include_different_filter_set_on_same_navigation_twice(bool async)
    {
        await base.Filtered_include_different_filter_set_on_same_navigation_twice(async);
    }

    public override async Task Filtered_include_different_filter_set_on_same_navigation_twice_multi_level(bool async)
    {
        await base.Filtered_include_different_filter_set_on_same_navigation_twice_multi_level(async);
    }

    public override async Task Filtered_include_same_filter_set_on_same_navigation_twice(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_same_filter_set_on_same_navigation_twice(async), "could not be translated");
    }

    public override async Task Filtered_include_same_filter_set_on_same_navigation_twice_followed_by_ThenIncludes(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_same_filter_set_on_same_navigation_twice_followed_by_ThenIncludes(async), "Failure");
    }

    public override async Task Filtered_include_multiple_multi_level_includes_with_first_level_using_filter_include_on_one_of_the_chains_only(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_multiple_multi_level_includes_with_first_level_using_filter_include_on_one_of_the_chains_only(async), "Failure");
    }

    public override async Task Filtered_include_and_non_filtered_include_on_same_navigation1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_and_non_filtered_include_on_same_navigation1(async), "could not be translated");
    }

    public override async Task Filtered_include_and_non_filtered_include_on_same_navigation2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_and_non_filtered_include_on_same_navigation2(async), "could not be translated");
    }

    public override async Task Filtered_include_and_non_filtered_include_followed_by_then_include_on_same_navigation(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_and_non_filtered_include_followed_by_then_include_on_same_navigation(async), "Failure");
    }

    public override async Task Filtered_include_complex_three_level_with_middle_having_filter1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_complex_three_level_with_middle_having_filter1(async), "Failure");
    }

    public override async Task Filtered_include_complex_three_level_with_middle_having_filter2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_complex_three_level_with_middle_having_filter2(async), "Failure");
    }

#if EF10
    public override async Task Filtered_include_variable_used_inside_filter(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_variable_used_inside_filter(async), "could not be translated");
    }
#endif

#if EF10
    public override async Task Filtered_include_context_accessed_inside_filter(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_context_accessed_inside_filter(async), "could not be translated");
    }
#endif

    public override async Task Filtered_include_context_accessed_inside_filter_correlated(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_context_accessed_inside_filter_correlated(async), "Failure");
    }

    public override async Task Filtered_include_include_parameter_used_inside_filter_throws(bool async)
    {
        await base.Filtered_include_include_parameter_used_inside_filter_throws(async);
    }

    public override async Task Filtered_include_outer_parameter_used_inside_filter(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_include_outer_parameter_used_inside_filter(async), "could not be translated");
    }

    public override async Task Filtered_include_is_considered_loaded(bool async)
    {
        await base.Filtered_include_is_considered_loaded(async);
    }

    public override async Task Filtered_include_with_Distinct_throws(bool async)
    {
        await base.Filtered_include_with_Distinct_throws(async);
    }

    public override async Task Filtered_include_calling_methods_directly_on_parameter_throws(bool async)
    {
        await base.Filtered_include_calling_methods_directly_on_parameter_throws(async);
    }

    public override async Task Filtered_include_Take_with_another_Take_on_top_level(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_Take_with_another_Take_on_top_level(async), "Failure");
    }

    public override async Task Filtered_include_Skip_Take_with_another_Skip_Take_on_top_level(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_Skip_Take_with_another_Skip_Take_on_top_level(async), "Failure");
    }

    public override async Task Filtered_include_with_Take_without_order_by_followed_by_ThenInclude_and_FirstOrDefault_on_top_level(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_with_Take_without_order_by_followed_by_ThenInclude_and_FirstOrDefault_on_top_level(async), "Failure");
    }

    public override async Task Filtered_include_with_Take_without_order_by_followed_by_ThenInclude_and_unordered_Take_on_top_level(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_with_Take_without_order_by_followed_by_ThenInclude_and_unordered_Take_on_top_level(async), "Failure");
    }

    public override async Task Projecting_collection_with_FirstOrDefault(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_collection_with_FirstOrDefault(async), "could not be translated");
    }

    public override async Task Skip_on_grouping_element(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Skip_on_grouping_element(async), "could not be translated");
    }

    public override async Task Take_on_grouping_element(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Take_on_grouping_element(async), "could not be translated");
    }

    public override async Task Skip_Take_on_grouping_element(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Skip_Take_on_grouping_element(async), "could not be translated");
    }

    public override async Task Skip_Take_Distinct_on_grouping_element(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Skip_Take_Distinct_on_grouping_element(async), "could not be translated");
    }

    public override async Task Skip_Take_ToList_on_grouping_element(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Skip_Take_ToList_on_grouping_element(async), "could not be translated");
    }

    public override async Task Skip_Take_on_grouping_element_into_non_entity(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Skip_Take_on_grouping_element_into_non_entity(async), "could not be translated");
    }

    public override async Task Skip_Take_on_grouping_element_with_collection_include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Skip_Take_on_grouping_element_with_collection_include(async), "could not be translated");
    }

    public override async Task Skip_Take_on_grouping_element_with_reference_include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Skip_Take_on_grouping_element_with_reference_include(async), "could not be translated");
    }

    public override async Task Skip_Take_on_grouping_element_inside_collection_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Skip_Take_on_grouping_element_inside_collection_projection(async), "could not be translated");
    }

    public override async Task Collection_projection_over_GroupBy_over_parameter(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_projection_over_GroupBy_over_parameter(async), "could not be translated");
    }

    public override async Task SelectMany_over_conditional_null_source(bool async)
    {
        await base.SelectMany_over_conditional_null_source(async);
    }

    public override async Task SelectMany_over_conditional_empty_source(bool async)
    {
        await base.SelectMany_over_conditional_empty_source(async);
    }

    public override async Task Include_partially_added_before_Where_and_then_build_upon(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_partially_added_before_Where_and_then_build_upon(async), "could not be translated");
    }

    public override async Task Include_partially_added_before_Where_and_then_build_upon_with_filtered_include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_partially_added_before_Where_and_then_build_upon_with_filtered_include(async), "could not be translated");
    }

    public override async Task Take_on_correlated_collection_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Take_on_correlated_collection_in_projection(async), "could not be translated");
    }

    public override async Task FirstOrDefault_with_predicate_on_correlated_collection_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.FirstOrDefault_with_predicate_on_correlated_collection_in_projection(async), "could not be translated");
    }

    public override async Task SelectMany_with_predicate_and_DefaultIfEmpty_projecting_root_collection_element_and_another_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_predicate_and_DefaultIfEmpty_projecting_root_collection_element_and_another_collection(async), "could not be translated");
    }

    public override async Task Complex_query_issue_21665(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_query_issue_21665(async), "could not be translated");
    }

    public override async Task Projecting_collection_after_optional_reference_correlated_with_parent(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_collection_after_optional_reference_correlated_with_parent(async), "could not be translated");
    }

    public override async Task Projecting_collection_with_group_by_after_optional_reference_correlated_with_parent(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_collection_with_group_by_after_optional_reference_correlated_with_parent(async), "could not be translated");
    }

    public override async Task Final_GroupBy_property_entity_Include_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Final_GroupBy_property_entity_Include_collection(async), "could not be translated");
    }

    public override async Task Final_GroupBy_property_entity_Include_collection_nested(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Final_GroupBy_property_entity_Include_collection_nested(async), "could not be translated");
    }

    public override async Task Final_GroupBy_property_entity_Include_collection_reference(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Final_GroupBy_property_entity_Include_collection_reference(async), "could not be translated");
    }

    public override async Task Final_GroupBy_property_entity_Include_collection_multiple(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Final_GroupBy_property_entity_Include_collection_multiple(async), "could not be translated");
    }

    public override async Task Final_GroupBy_property_entity_Include_collection_reference_same_level(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Final_GroupBy_property_entity_Include_collection_reference_same_level(async), "could not be translated");
    }

    public override async Task Final_GroupBy_property_entity_Include_reference(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Final_GroupBy_property_entity_Include_reference(async), "could not be translated");
    }

    public override async Task Final_GroupBy_property_entity_Include_reference_multiple(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Final_GroupBy_property_entity_Include_reference_multiple(async), "could not be translated");
    }

#if EF9 || EF10
    public override async Task Project_collection_and_nested_conditional(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_and_nested_conditional(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_complex_includes(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_complex_includes(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_complex_includes_EF_Property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_complex_includes_EF_Property(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_complex_includes_self_ref(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_complex_includes_self_ref(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_complex_includes_self_ref_EF_Property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_complex_includes_self_ref_EF_Property(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_and_collection_order_by(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_and_collection_order_by(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_ThenInclude_collection_order_by(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_ThenInclude_collection_order_by(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_complex_include_select(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_complex_include_select(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_nested_with_optional_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_nested_with_optional_navigation(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Complex_multi_include_with_order_by_and_paging(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_multi_include_with_order_by_and_paging(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Complex_multi_include_with_order_by_and_paging_joins_on_correct_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_multi_include_with_order_by_and_paging_joins_on_correct_key(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Complex_multi_include_with_order_by_and_paging_joins_on_correct_key2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_multi_include_with_order_by_and_paging_joins_on_correct_key2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Required_navigation_with_Include_ThenInclude(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Required_navigation_with_Include_ThenInclude(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_with_Include_ThenInclude(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_with_Include_ThenInclude(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_optional_navigation_with_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_optional_navigation_with_Include(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_optional_navigation_with_string_based_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_optional_navigation_with_string_based_Include(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_with_order_by_and_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_with_order_by_and_Include(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_with_Include_and_order(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_with_Include_and_order(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_collection_order_by_reference_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_collection_order_by_reference_navigation(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_followed_by_include_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_followed_by_include_collection(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Filtered_include_variable_used_inside_filter(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_variable_used_inside_filter(async), "Failure");
    }
#endif

#if EF8 || EF9
    public override async Task Filtered_include_context_accessed_inside_filter(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Filtered_include_context_accessed_inside_filter(async), "Failure");
    }
#endif

    public class ComplexNavigationsCollectionsQueryMongoFixture : ComplexNavigationsQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("CNavColl");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
        }

        public override async Task InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server);

            await base.InitializeAsync();
        }
    }
}
