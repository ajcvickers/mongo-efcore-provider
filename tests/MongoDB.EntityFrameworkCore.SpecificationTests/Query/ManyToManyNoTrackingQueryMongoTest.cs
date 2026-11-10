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
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class ManyToManyNoTrackingQueryMongoTest : ManyToManyNoTrackingQueryTestBase<ManyToManyNoTrackingQueryMongoTest.ManyToManyNoTrackingQueryMongoFixture>
{
    public ManyToManyNoTrackingQueryMongoTest(ManyToManyNoTrackingQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if EF11
    // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
    public override Task SelectMany_with_collection_selector_having_subquery()
        => SpecFails.ThrowsAsync(() => base.SelectMany_with_collection_selector_having_subquery());

    // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
    public override Task Many_to_many_load_works_when_join_entity_has_custom_key(bool async)
        => SpecFails.ThrowsAsync(() => base.Many_to_many_load_works_when_join_entity_has_custom_key(async));

#endif

    public override async Task Include_skip_navigation_then_include_inverse_throws_in_no_tracking(bool async)
    {
        await base.Include_skip_navigation_then_include_inverse_throws_in_no_tracking(async);

        AssertMql();
    }

    public override Task Include_skip_navigation_then_include_inverse_works_for_tracking_query(bool async)
        => base.Include_skip_navigation_then_include_inverse_works_for_tracking_query(async);

    public override Task Include_skip_navigation_then_include_inverse_works_for_tracking_query_unidirectional(bool async)
        => base.Include_skip_navigation_then_include_inverse_works_for_tracking_query_unidirectional(async);

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_all(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_all(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_any_without_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_any_without_predicate(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_any_with_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_any_with_predicate(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_contains(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_contains(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_count_without_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_count_without_predicate(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_count_with_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_count_with_predicate(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_long_count_without_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_long_count_without_predicate(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_long_count_with_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_long_count_with_predicate(async), "Unsupported cross-DbSet");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_select_many_average(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_select_many_average(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_select_many_max(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_select_many_max(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_select_many_min(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_select_many_min(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_select_many_sum(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_select_many_sum(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_select_subquery_average(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_select_subquery_average(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_select_subquery_max(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_select_subquery_max(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_select_subquery_min(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_select_subquery_min(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_select_subquery_sum(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_select_subquery_sum(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_order_by_first_or_default(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_order_by_first_or_default(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_order_by_single_or_default(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_order_by_single_or_default(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_order_by_last_or_default(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_order_by_last_or_default(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_order_by_reverse_first_or_default(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_order_by_reverse_first_or_default(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_cast(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_cast(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_of_type(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_of_type(async), "could not be translated");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Join_with_skip_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Join_with_skip_navigation(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Left_join_with_skip_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Left_join_with_skip_navigation(async), "Unsupported cross-DbSet");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_where(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_where(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_order_by_skip(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_order_by_skip(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_order_by_take(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_order_by_take(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_order_by_skip_take(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_order_by_skip_take(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_of_type(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_of_type(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_cast(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_cast(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_skip_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_skip_navigation(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_skip_navigation_multiple(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_skip_navigation_multiple(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_skip_navigation_first_or_default(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_skip_navigation_first_or_default(async), "could not be translated");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Include_skip_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_skip_navigation(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Include_skip_navigation_then_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_skip_navigation_then_reference(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Include_skip_navigation_then_include_skip_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_skip_navigation_then_include_skip_navigation(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Include_skip_navigation_then_include_reference_and_skip_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_skip_navigation_then_include_reference_and_skip_navigation(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Include_skip_navigation_and_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_skip_navigation_and_reference(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_where(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_where(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by_skip(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by_skip(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by_take(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by_take(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by_take_EF_Property(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by_take_EF_Property(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by_skip_take(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by_skip_take(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_then_include_skip_navigation_where(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_then_include_skip_navigation_where(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_then_include_skip_navigation_order_by_skip_take(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_then_include_skip_navigation_order_by_skip_take(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_where_then_include_skip_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_where_then_include_skip_navigation(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_EF_Property(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_EF_Property(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_where_then_include_skip_navigation_order_by_skip_take(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_where_then_include_skip_navigation_order_by_skip_take(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filter_include_on_skip_navigation_combined(bool async)
        => SpecFails.ThrowsAsync(() => base.Filter_include_on_skip_navigation_combined(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filter_include_on_skip_navigation_combined_with_filtered_then_includes(bool async)
        => SpecFails.ThrowsAsync(() => base.Filter_include_on_skip_navigation_combined_with_filtered_then_includes(async), "Including navigation");

    public override async Task Throws_when_different_filtered_include(bool async)
    {
        await base.Throws_when_different_filtered_include(async);

        AssertMql();
    }

    public override async Task Throws_when_different_filtered_then_include(bool async)
    {
        await base.Throws_when_different_filtered_then_include(async);

        AssertMql();
    }

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_on_skip_navigation_then_filtered_include_on_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_on_skip_navigation_then_filtered_include_on_navigation(async), "Including navigation");

    // Fails: TargetInvocationException while translating or materializing the query EF-X526
    public override Task Filtered_include_on_navigation_then_filtered_include_on_skip_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_on_navigation_then_filtered_include_on_skip_navigation(async));

    public override Task Includes_accessed_via_different_path_are_merged(bool async)
        => base.Includes_accessed_via_different_path_are_merged(async);

    public override Task Filtered_includes_accessed_via_different_path_are_merged(bool async)
        => base.Filtered_includes_accessed_via_different_path_are_merged(async);

    public override Task Throws_when_different_filtered_then_include_via_different_paths(bool async)
        => base.Throws_when_different_filtered_then_include_via_different_paths(async);

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_where_non_equality(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_where_non_equality(async), "could not be translated");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Contains_on_skip_collection_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Contains_on_skip_collection_navigation(async), "Unsupported cross-DbSet");

    public override async Task GetType_in_hierarchy_in_base_type(bool async)
    {
        await base.GetType_in_hierarchy_in_base_type(async);

        AssertMql(
            """
EntityRoots.{ "$match" : { "_t" : "EntityRoot" } }
""");
    }

    public override async Task GetType_in_hierarchy_in_intermediate_type(bool async)
    {
        await base.GetType_in_hierarchy_in_intermediate_type(async);

        AssertMql(
            """
EntityRoots.{ "$match" : { "_t" : "EntityBranch" } }
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type(async);

        AssertMql(
            """
EntityRoots.{ "$match" : { "_t" : "EntityLeaf" } }
""");
    }

    public override async Task GetType_in_hierarchy_in_querying_base_type(bool async)
    {
        await base.GetType_in_hierarchy_in_querying_base_type(async);

        AssertMql(
            """
EntityRoots.{ "$match" : { "$and" : [{ "_t" : { "$in" : ["EntityBranch", "EntityLeaf"] } }, { "_t" : "EntityRoot" }] } }
""");
    }

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_all_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_all_unidirectional(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_any_with_predicate_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_any_with_predicate_unidirectional(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_contains_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_contains_unidirectional(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_count_without_predicate_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_count_without_predicate_unidirectional(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Skip_navigation_count_with_predicate_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_count_with_predicate_unidirectional(async), "Unsupported cross-DbSet");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_select_subquery_average_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_select_subquery_average_unidirectional(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_order_by_reverse_first_or_default_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_order_by_reverse_first_or_default_unidirectional(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Skip_navigation_of_type_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Skip_navigation_of_type_unidirectional(async), "could not be translated");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Join_with_skip_navigation_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Join_with_skip_navigation_unidirectional(async), "Unsupported cross-DbSet");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Left_join_with_skip_navigation_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Left_join_with_skip_navigation_unidirectional(async), "Unsupported cross-DbSet");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_unidirectional(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_where_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_where_unidirectional(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_order_by_take_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_order_by_take_unidirectional(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_order_by_skip_take_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_order_by_skip_take_unidirectional(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_cast_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_cast_unidirectional(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Select_skip_navigation_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_skip_navigation_unidirectional(async), "could not be translated");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Include_skip_navigation_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_skip_navigation_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Include_skip_navigation_then_reference_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_skip_navigation_then_reference_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Include_skip_navigation_then_include_skip_navigation_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_skip_navigation_then_include_skip_navigation_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Include_skip_navigation_then_include_reference_and_skip_navigation_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_skip_navigation_then_include_reference_and_skip_navigation_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Include_skip_navigation_and_reference_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_skip_navigation_and_reference_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_where_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_where_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by_skip_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by_skip_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by_take_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by_take_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_order_by_skip_take_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_order_by_skip_take_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filtered_include_skip_navigation_where_then_include_skip_navigation_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_include_skip_navigation_where_then_include_skip_navigation_unidirectional(async), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X208
    public override Task Filter_include_on_skip_navigation_combined_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Filter_include_on_skip_navigation_combined_unidirectional(async), "Including navigation");

    public override async Task Throws_when_different_filtered_include_unidirectional(bool async)
    {
        await base.Throws_when_different_filtered_include_unidirectional(async);

        AssertMql();
    }

    public override Task Includes_accessed_via_different_path_are_merged_unidirectional(bool async)
        => base.Includes_accessed_via_different_path_are_merged_unidirectional(async);

    // Fails: the query is not translated EF-X526
    public override Task Select_many_over_skip_navigation_where_non_equality_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Select_many_over_skip_navigation_where_non_equality_unidirectional(async), "could not be translated");

    // Fails: the query crosses collection boundaries EF-216
    public override Task Contains_on_skip_collection_navigation_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Contains_on_skip_collection_navigation_unidirectional(async), "Unsupported cross-DbSet");

    public override async Task GetType_in_hierarchy_in_base_type_unidirectional(bool async)
    {
        await base.GetType_in_hierarchy_in_base_type_unidirectional(async);

        AssertMql(
            """
UnidirectionalEntityRoots.{ "$match" : { "_t" : "UnidirectionalEntityRoot" } }
""");
    }

    public override async Task GetType_in_hierarchy_in_intermediate_type_unidirectional(bool async)
    {
        await base.GetType_in_hierarchy_in_intermediate_type_unidirectional(async);

        AssertMql(
            """
UnidirectionalEntityRoots.{ "$match" : { "_t" : "UnidirectionalEntityBranch" } }
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_unidirectional(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_unidirectional(async);

        AssertMql(
            """
UnidirectionalEntityRoots.{ "$match" : { "_t" : "UnidirectionalEntityLeaf" } }
""");
    }

    public override async Task GetType_in_hierarchy_in_querying_base_type_unidirectional(bool async)
    {
        await base.GetType_in_hierarchy_in_querying_base_type_unidirectional(async);

        AssertMql(
            """
UnidirectionalEntityRoots.{ "$match" : { "$and" : [{ "_t" : { "$in" : ["UnidirectionalEntityBranch", "UnidirectionalEntityLeaf"] } }, { "_t" : "UnidirectionalEntityRoot" }] } }
""");
    }

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    public class ManyToManyNoTrackingQueryMongoFixture : ManyToManyQueryFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("M2MNoTrack");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.DoNotOwnTypes((type, _) => type.Name.Contains("Join"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // EF-X207: a unique index on a nullable property or foreign key treats every null as a duplicate.
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
            modelBuilder.RelaxUniqueForeignKeyIndexes();
        }

        public override Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel.ManyToManyContext CreateContext()
        {
            _initFailure.ThrowIfFailed();
            return base.CreateContext();
        }
    }
}

