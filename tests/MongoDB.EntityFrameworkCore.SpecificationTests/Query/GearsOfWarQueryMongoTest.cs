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

public class GearsOfWarQueryMongoTest : GearsOfWarQueryTestBase<GearsOfWarQueryMongoTest.GearsOfWarQueryMongoFixture>
{
    public GearsOfWarQueryMongoTest(GearsOfWarQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Entity_equality_empty(bool async)
    {
        await base.Entity_equality_empty(async);
    }

#if EF10
    public override async Task Include_multiple_one_to_one_and_one_to_many(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_multiple_one_to_one_and_one_to_many(async), "Expression not supported");
    }
#endif

    public override async Task ToString_string_property_projection(bool async)
    {
        await base.ToString_string_property_projection(async);
    }

    public override async Task ToString_boolean_property_non_nullable(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.ToString_boolean_property_non_nullable(async), "Failure");
    }

    public override async Task ToString_boolean_property_nullable(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.ToString_boolean_property_nullable(async), "Failure");
    }

#if EF9 || EF10
    public override async Task ToString_boolean_computed_nullable(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.ToString_boolean_computed_nullable(async), "Failure");
    }
#endif

#if EF9 || EF10
    public override async Task ToString_enum_property_projection(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.ToString_enum_property_projection(async), "Failure");
    }
#endif

#if EF9 || EF10
    public override async Task ToString_nullable_enum_property_projection(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.ToString_nullable_enum_property_projection(async), "Failure");
    }
#endif

#if EF9 || EF10
    public override async Task ToString_enum_contains(bool async)
    {
        await base.ToString_enum_contains(async);
    }
#endif

#if EF9 || EF10
    public override async Task ToString_nullable_enum_contains(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.ToString_nullable_enum_contains(async), "Failure");
    }
#endif

    public override async Task Include_multiple_one_to_one_and_one_to_many_self_reference(bool async)
    {
        await base.Include_multiple_one_to_one_and_one_to_many_self_reference(async);
    }

#if EF10
    public override async Task Include_multiple_one_to_one_optional_and_one_to_one_required(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_multiple_one_to_one_optional_and_one_to_one_required(async), "Failure");
    }
#endif

    public override async Task Include_multiple_one_to_one_and_one_to_one_and_one_to_many(bool async)
    {
        await base.Include_multiple_one_to_one_and_one_to_one_and_one_to_many(async);
    }

    public override async Task Include_multiple_circular(bool async)
    {
        await base.Include_multiple_circular(async);
    }

    public override async Task Include_multiple_circular_with_filter(bool async)
    {
        await base.Include_multiple_circular_with_filter(async);
    }

    public override async Task Include_using_alternate_key(bool async)
    {
        await base.Include_using_alternate_key(async);
    }

    public override async Task Include_multiple_include_then_include(bool async)
    {
        await base.Include_multiple_include_then_include(async);
    }

    public override async Task Include_navigation_on_derived_type(bool async)
    {
        await base.Include_navigation_on_derived_type(async);
    }

    public override async Task String_based_Include_navigation_on_derived_type(bool async)
    {
        await base.String_based_Include_navigation_on_derived_type(async);
    }

    public override async Task EF_Property_based_Include_navigation_on_derived_type(bool async)
    {
        await base.EF_Property_based_Include_navigation_on_derived_type(async);
    }

#if EF10
    public override async Task Select_Where_Navigation_Included(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Included(async), "Expression not supported");
    }
#endif

    public override async Task Include_with_join_reference1(bool async)
    {
        await base.Include_with_join_reference1(async);
    }

    public override async Task Include_with_join_reference2(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_with_join_reference2(async), "Expression not supported");
    }

    public override async Task Include_with_join_collection1(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_with_join_collection1(async), "Expression not supported");
    }

    public override async Task Include_with_join_collection2(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_with_join_collection2(async), "Expression not supported");
    }

#if EF10
    public override async Task Include_where_list_contains_navigation(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_where_list_contains_navigation(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Include_where_list_contains_navigation2(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_where_list_contains_navigation2(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Navigation_accessed_twice_outside_and_inside_subquery(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Navigation_accessed_twice_outside_and_inside_subquery(async), "Expression not supported");
    }
#endif

    public override async Task Include_with_join_multi_level(bool async)
    {
        await base.Include_with_join_multi_level(async);
    }

    public override async Task Include_with_join_and_inheritance1(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_with_join_and_inheritance1(async), "Expression not supported");
    }

    public override async Task Include_with_join_and_inheritance_with_orderby_before_and_after_include(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_with_join_and_inheritance_with_orderby_before_and_after_include(async), "Expression not supported");
    }

    public override async Task Include_with_join_and_inheritance2(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_with_join_and_inheritance2(async), "Expression not supported");
    }

    public override async Task Include_with_join_and_inheritance3(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_with_join_and_inheritance3(async), "Expression not supported");
    }

#if EF10
    public override async Task Include_with_nested_navigation_in_order_by(bool async)
    {
        await base.Include_with_nested_navigation_in_order_by(async);
    }
#endif

    public override async Task Where_count_subquery_without_collision(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_count_subquery_without_collision(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_any_subquery_without_collision(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_any_subquery_without_collision(async), "Unsupported cross-DbSet query");
    }

    public override async Task Select_inverted_boolean(bool async)
    {
        await base.Select_inverted_boolean(async);
    }

#if EF9 || EF10
    public override async Task Select_inverted_nullable_boolean(bool async)
    {
        // Fails: Server error executing the generated pipeline EF-X528
        await SpecFails.ThrowsAsync(() => base.Select_inverted_nullable_boolean(async), "MongoCommandException");
    }
#endif

    public override async Task Select_comparison_with_null(bool async)
    {
        await base.Select_comparison_with_null(async);
    }

    public override async Task Select_null_parameter(bool async)
    {
        await base.Select_null_parameter(async);
    }

    public override async Task Select_ternary_operation_with_boolean(bool async)
    {
        await base.Select_ternary_operation_with_boolean(async);
    }

    public override async Task Select_ternary_operation_with_inverted_boolean(bool async)
    {
        await base.Select_ternary_operation_with_inverted_boolean(async);
    }

    public override async Task Select_ternary_operation_with_has_value_not_null(bool async)
    {
        await base.Select_ternary_operation_with_has_value_not_null(async);
    }

    public override async Task Select_ternary_operation_multiple_conditions(bool async)
    {
        await base.Select_ternary_operation_multiple_conditions(async);
    }

    public override async Task Select_ternary_operation_multiple_conditions_2(bool async)
    {
        await base.Select_ternary_operation_multiple_conditions_2(async);
    }

    public override async Task Select_multiple_conditions(bool async)
    {
        await base.Select_multiple_conditions(async);
    }

    public override async Task Select_nested_ternary_operations(bool async)
    {
        await base.Select_nested_ternary_operations(async);
    }

    public override async Task Null_propagation_optimization1(bool async)
    {
        await base.Null_propagation_optimization1(async);
    }

    public override async Task Null_propagation_optimization2(bool async)
    {
        await base.Null_propagation_optimization2(async);
    }

    public override async Task Null_propagation_optimization3(bool async)
    {
        await base.Null_propagation_optimization3(async);
    }

    public override async Task Null_propagation_optimization4(bool async)
    {
        await base.Null_propagation_optimization4(async);
    }

    public override async Task Null_propagation_optimization5(bool async)
    {
        await base.Null_propagation_optimization5(async);
    }

    public override async Task Null_propagation_optimization6(bool async)
    {
        await base.Null_propagation_optimization6(async);
    }

    public override async Task Select_null_propagation_optimization7(bool async)
    {
        await base.Select_null_propagation_optimization7(async);
    }

    public override async Task Select_null_propagation_optimization8(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_optimization8(async), "Failure");
    }

    public override async Task Select_null_propagation_optimization9(bool async)
    {
        await base.Select_null_propagation_optimization9(async);
    }

    public override async Task Select_null_propagation_negative1(bool async)
    {
        await base.Select_null_propagation_negative1(async);
    }

    public override async Task Select_null_propagation_negative2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_negative2(async), "could not be translated");
    }

#if EF10
    public override async Task Select_null_propagation_negative3(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_negative3(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Select_null_propagation_negative4(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_negative4(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Select_null_propagation_negative5(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_negative5(async), "Expression not supported");
    }
#endif

    public override async Task Select_null_propagation_negative6(bool async)
    {
        await base.Select_null_propagation_negative6(async);
    }

    public override async Task Select_null_propagation_negative7(bool async)
    {
        await base.Select_null_propagation_negative7(async);
    }

#if EF10
    public override async Task Select_null_propagation_negative8(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_negative8(async), "Failure");
    }
#endif

    public override async Task Select_null_propagation_negative9(bool async)
    {
        await base.Select_null_propagation_negative9(async);
    }

#if EF10
    public override async Task Select_null_propagation_works_for_navigations_with_composite_keys(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_works_for_navigations_with_composite_keys(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Select_null_propagation_works_for_multiple_navigations_with_composite_keys(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_works_for_multiple_navigations_with_composite_keys(async), "Failure");
    }
#endif

    public override async Task Select_conditional_with_anonymous_type_and_null_constant(bool async)
    {
        await base.Select_conditional_with_anonymous_type_and_null_constant(async);
    }

    public override async Task Select_conditional_with_anonymous_types(bool async)
    {
        await base.Select_conditional_with_anonymous_types(async);
    }

    public override async Task Where_conditional_equality_1(bool async)
    {
        await base.Where_conditional_equality_1(async);
    }

    public override async Task Where_conditional_equality_2(bool async)
    {
        await base.Where_conditional_equality_2(async);
    }

    public override async Task Where_conditional_equality_3(bool async)
    {
        await base.Where_conditional_equality_3(async);
    }

    public override async Task Select_coalesce_with_anonymous_types(bool async)
    {
        await base.Select_coalesce_with_anonymous_types(async);
    }

    public override async Task Where_coalesce_with_anonymous_types(bool async)
    {
        await base.Where_coalesce_with_anonymous_types(async);
    }

    public override async Task Where_compare_anonymous_types(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_compare_anonymous_types(async), "could not be translated");
    }

    public override async Task Where_member_access_on_anonymous_type(bool async)
    {
        await base.Where_member_access_on_anonymous_type(async);
    }

    public override async Task Where_compare_anonymous_types_with_uncorrelated_members(bool async)
    {
        await base.Where_compare_anonymous_types_with_uncorrelated_members(async);
    }

#if EF10
    public override async Task Select_Where_Navigation(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation(async), "Expression not supported");
    }
#endif

    public override async Task Select_Where_Navigation_Scalar_Equals_Navigation_Scalar(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Scalar_Equals_Navigation_Scalar(async), "could not be translated");
    }

    public override async Task Select_Where_Navigation_Scalar_Equals_Navigation_Scalar_Projected(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Scalar_Equals_Navigation_Scalar_Projected(async), "could not be translated");
    }

#if EF10
    public override async Task Optional_Navigation_Null_Coalesce_To_Clr_Type(bool async)
    {
        // Fails: Average/aggregate over a navigation without selector throws 'Operation is not valid due to the current state of the object' EF-X541
        await SpecFails.ThrowsAsync(() => base.Optional_Navigation_Null_Coalesce_To_Clr_Type(async), "Operation is not valid");
    }
#endif

    public override async Task Where_subquery_boolean(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_boolean(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_boolean_with_pushdown(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_boolean_with_pushdown(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_distinct_firstordefault_boolean(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_distinct_firstordefault_boolean(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_distinct_firstordefault_boolean_with_pushdown(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_distinct_firstordefault_boolean_with_pushdown(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_distinct_first_boolean(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_distinct_first_boolean(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_distinct_singleordefault_boolean1(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_distinct_singleordefault_boolean1(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_distinct_singleordefault_boolean2(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_distinct_singleordefault_boolean2(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_distinct_singleordefault_boolean_with_pushdown(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_distinct_singleordefault_boolean_with_pushdown(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_distinct_lastordefault_boolean(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_distinct_lastordefault_boolean(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_distinct_last_boolean(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_distinct_last_boolean(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_distinct_orderby_firstordefault_boolean(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_distinct_orderby_firstordefault_boolean(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_distinct_orderby_firstordefault_boolean_with_pushdown(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_distinct_orderby_firstordefault_boolean_with_pushdown(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_union_firstordefault_boolean(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_union_firstordefault_boolean(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_join_firstordefault_boolean(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_join_firstordefault_boolean(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_left_join_firstordefault_boolean(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_left_join_firstordefault_boolean(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_concat_firstordefault_boolean(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_concat_firstordefault_boolean(async), "Unsupported cross-DbSet query");
    }

    public override async Task Concat_with_count(bool async)
    {
        // Fails: NullReferenceException during query execution/materialization EF-X529
        await SpecFails.ThrowsAsync(() => base.Concat_with_count(async), "NullReferenceException");
    }

    public override async Task Concat_scalars_with_count(bool async)
    {
        await base.Concat_scalars_with_count(async);
    }

    public override async Task Concat_anonymous_with_count(bool async)
    {
        await base.Concat_anonymous_with_count(async);
    }

    public override async Task Concat_with_scalar_projection(bool async)
    {
        // Fails: NullReferenceException during query execution/materialization EF-X529
        await SpecFails.ThrowsAsync(() => base.Concat_with_scalar_projection(async), "NullReferenceException");
    }

    public override async Task Select_navigation_with_concat_and_count(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_navigation_with_concat_and_count(async), "could not be translated");
    }

    public override async Task Concat_with_collection_navigations(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Concat_with_collection_navigations(async), "could not be translated");
    }

    public override async Task Union_with_collection_navigations(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Union_with_collection_navigations(async), "could not be translated");
    }

    public override async Task Select_subquery_distinct_firstordefault(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_distinct_firstordefault(async), "could not be translated");
    }

#if EF10
    public override async Task Select_Where_Navigation_Client(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Client(async));
    }
#endif

#if EF10
    public override async Task Select_Where_Navigation_Null(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Null(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Select_Where_Navigation_Null_Reverse(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Null_Reverse(async), "Expression not supported");
    }
#endif

    public override async Task Select_Where_Navigation_Equals_Navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Equals_Navigation(async), "could not be translated");
    }

#if EF10
    public override async Task Conditional_Navigation_With_Trivial_Member_Access(bool async)
    {
        await base.Conditional_Navigation_With_Trivial_Member_Access(async);
    }
#endif

#if EF10
    public override async Task Conditional_Navigation_With_Member_Access_On_Same_Type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Conditional_Navigation_With_Member_Access_On_Same_Type(async), "Failure");
    }
#endif

#if EF10
    public override async Task Conditional_Navigation_With_Member_Access_On_Related_Types(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Conditional_Navigation_With_Member_Access_On_Related_Types(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Singleton_Navigation_With_Member_Access(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Singleton_Navigation_With_Member_Access(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Select_Singleton_Navigation_With_Member_Access(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_Singleton_Navigation_With_Member_Access(async), "Expression not supported");
    }
#endif

    public override async Task GroupJoin_Composite_Key(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GroupJoin_Composite_Key(async), "Expression not supported");
    }

#if EF10
    public override async Task Join_navigation_translated_to_subquery_composite_key(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_navigation_translated_to_subquery_composite_key(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Join_with_order_by_on_inner_sequence_navigation_translated_to_subquery_composite_key(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_with_order_by_on_inner_sequence_navigation_translated_to_subquery_composite_key(async), "Expression not supported");
    }
#endif

    public override async Task Join_with_order_by_without_skip_or_take(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_with_order_by_without_skip_or_take(async), "Expression not supported");
    }

    public override async Task Join_with_order_by_without_skip_or_take_nested(bool async)
    {
        await base.Join_with_order_by_without_skip_or_take_nested(async);
    }

#if EF10
    public override async Task Collection_with_inheritance_and_join_include_joined(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Collection_with_inheritance_and_join_include_joined(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Collection_with_inheritance_and_join_include_source(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Collection_with_inheritance_and_join_include_source(async), "Expression not supported");
    }
#endif

    public override async Task Non_unicode_string_literal_is_used_for_non_unicode_column(bool async)
    {
        await base.Non_unicode_string_literal_is_used_for_non_unicode_column(async);
    }

    public override async Task Non_unicode_string_literal_is_used_for_non_unicode_column_right(bool async)
    {
        await base.Non_unicode_string_literal_is_used_for_non_unicode_column_right(async);
    }

    public override async Task Non_unicode_parameter_is_used_for_non_unicode_column(bool async)
    {
        await base.Non_unicode_parameter_is_used_for_non_unicode_column(async);
    }

    public override async Task Non_unicode_string_literals_in_contains_is_used_for_non_unicode_column(bool async)
    {
        await base.Non_unicode_string_literals_in_contains_is_used_for_non_unicode_column(async);
    }

    public override async Task Non_unicode_string_literals_is_used_for_non_unicode_column_with_subquery(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Non_unicode_string_literals_is_used_for_non_unicode_column_with_subquery(async), "Unsupported cross-DbSet query");
    }

    public override async Task Non_unicode_string_literals_is_used_for_non_unicode_column_in_subquery(bool async)
    {
        await base.Non_unicode_string_literals_is_used_for_non_unicode_column_in_subquery(async);
    }

    public override async Task Non_unicode_string_literals_is_used_for_non_unicode_column_with_contains(bool async)
    {
        await base.Non_unicode_string_literals_is_used_for_non_unicode_column_with_contains(async);
    }

    public override async Task Unicode_string_literals_is_used_for_non_unicode_column_with_concat(bool async)
    {
        await base.Unicode_string_literals_is_used_for_non_unicode_column_with_concat(async);
    }

#if EF10
    public override void Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result1()
    {
        base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result1();
    }
#endif

#if EF10
    public override void Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result2()
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        SpecFails.Throws(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result2(), "Failure");
    }
#endif

#if EF10
    public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result3(async), "Failure");
    }
#endif

#if EF10
    public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result4(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result4(async), "Failure");
    }
#endif

#if EF10
    public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_inheritance_and_coalesce_result(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_inheritance_and_coalesce_result(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_conditional_result(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_conditional_result(async), "Failure");
    }
#endif

#if EF10
    public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_complex_projection_result(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_complex_projection_result(async), "Failure");
    }
#endif

#if EF10
    public override async Task Coalesce_operator_in_predicate(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Coalesce_operator_in_predicate(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Coalesce_operator_in_predicate_with_other_conditions(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Coalesce_operator_in_predicate_with_other_conditions(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Coalesce_operator_in_projection_with_other_conditions(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Coalesce_operator_in_projection_with_other_conditions(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_predicate(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_predicate(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_predicate2(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_predicate2(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_predicate_negated(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_predicate_negated(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_predicate_negated_complex1(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_predicate_negated_complex1(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_predicate_negated_complex2(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_predicate_negated_complex2(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_conditional_expression(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_conditional_expression(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_binary_expression(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_binary_expression(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_binary_and_expression(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_binary_and_expression(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_projection(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_projection(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_projection_into_anonymous_type(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_projection_into_anonymous_type(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_DTOs(bool async)
    {
        // Fails: Average/aggregate over a navigation without selector throws 'Operation is not valid due to the current state of the object' EF-X541
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_DTOs(async), "Operation is not valid");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_list_initializers(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_list_initializers(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_array_initializers(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_array_initializers(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_orderby(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_orderby(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_all(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_all(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_negated_predicate(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_negated_predicate(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Optional_navigation_type_compensation_works_with_contains(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_contains(async), "Expression not supported");
    }
#endif

    public override async Task Optional_navigation_type_compensation_works_with_skip(bool async)
    {
        await base.Optional_navigation_type_compensation_works_with_skip(async);
    }

    public override async Task Optional_navigation_type_compensation_works_with_take(bool async)
    {
        await base.Optional_navigation_type_compensation_works_with_take(async);
    }

    public override async Task Select_correlated_filtered_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_correlated_filtered_collection(async), "could not be translated");
    }

    public override async Task Select_correlated_filtered_collection_with_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_correlated_filtered_collection_with_composite_key(async), "could not be translated");
    }

    public override async Task Select_correlated_filtered_collection_returning_queryable_throws(bool async)
    {
        await base.Select_correlated_filtered_collection_returning_queryable_throws(async);
    }

    public override async Task Select_correlated_filtered_collection_works_with_caching(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_correlated_filtered_collection_works_with_caching(async), "could not be translated");
    }

    public override async Task Join_predicate_value_equals_condition(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_predicate_value_equals_condition(async), "Expression not supported");
    }

    public override async Task Join_predicate_value(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_predicate_value(async), "Expression not supported");
    }

    public override async Task Join_predicate_condition_equals_condition(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_predicate_condition_equals_condition(async), "Expression not supported");
    }

#if EF10
    public override async Task Left_join_predicate_value_equals_condition(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Left_join_predicate_value_equals_condition(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Left_join_predicate_value(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Left_join_predicate_value(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Left_join_predicate_condition_equals_condition(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Left_join_predicate_condition_equals_condition(async), "Expression not supported");
    }
#endif

    public override async Task Orderby_added_for_client_side_GroupJoin_composite_dependent_to_principal_LOJ_when_incomplete_key_is_used(bool async)
    {
        await base.Orderby_added_for_client_side_GroupJoin_composite_dependent_to_principal_LOJ_when_incomplete_key_is_used(async);
    }

#if EF10
    public override async Task Complex_predicate_with_AndAlso_and_nullable_bool_property(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Complex_predicate_with_AndAlso_and_nullable_bool_property(async), "Failure");
    }
#endif

#if EF10
    public override async Task Distinct_with_optional_navigation_is_translated_to_sql(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Distinct_with_optional_navigation_is_translated_to_sql(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Sum_with_optional_navigation_is_translated_to_sql(bool async)
    {
        // Fails: Average/aggregate over a navigation without selector throws 'Operation is not valid due to the current state of the object' EF-X541
        await SpecFails.ThrowsAsync(() => base.Sum_with_optional_navigation_is_translated_to_sql(async), "Operation is not valid");
    }
#endif

#if EF10
    public override async Task Count_with_optional_navigation_is_translated_to_sql(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Count_with_optional_navigation_is_translated_to_sql(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task FirstOrDefault_with_manually_created_groupjoin_is_translated_to_sql(bool async)
    {
        await base.FirstOrDefault_with_manually_created_groupjoin_is_translated_to_sql(async);
    }
#endif

    public override async Task Any_with_optional_navigation_as_subquery_predicate_is_translated_to_sql(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Any_with_optional_navigation_as_subquery_predicate_is_translated_to_sql(async), "Unsupported cross-DbSet query");
    }

#if EF10
    public override async Task All_with_optional_navigation_is_translated_to_sql(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.All_with_optional_navigation_is_translated_to_sql(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Client_side_equality_with_parameter_works_with_optional_navigations(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Client_side_equality_with_parameter_works_with_optional_navigations(async), "Expression not supported");
    }
#endif

    public override async Task Contains_with_local_nullable_guid_list_closure(bool async)
    {
        await base.Contains_with_local_nullable_guid_list_closure(async);
    }

    public override async Task Unnecessary_include_doesnt_get_added_complex_when_projecting_EF_Property(bool async)
    {
        await base.Unnecessary_include_doesnt_get_added_complex_when_projecting_EF_Property(async);
    }

    public override async Task Multiple_order_bys_are_properly_lifted_from_subquery_created_by_include(bool async)
    {
        await base.Multiple_order_bys_are_properly_lifted_from_subquery_created_by_include(async);
    }

    public override async Task Order_by_is_properly_lifted_from_subquery_with_same_order_by_in_the_outer_query(bool async)
    {
        await base.Order_by_is_properly_lifted_from_subquery_with_same_order_by_in_the_outer_query(async);
    }

#if EF10
    public override async Task Where_is_properly_lifted_from_subquery_created_by_include(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_is_properly_lifted_from_subquery_created_by_include(async), "Expression not supported");
    }
#endif

    public override async Task Subquery_is_lifted_from_main_from_clause_of_SelectMany(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_is_lifted_from_main_from_clause_of_SelectMany(async), "could not be translated");
    }

    public override async Task Subquery_containing_SelectMany_projecting_main_from_clause_gets_lifted(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_containing_SelectMany_projecting_main_from_clause_gets_lifted(async), "could not be translated");
    }

    public override async Task Subquery_containing_join_projecting_main_from_clause_gets_lifted(bool async)
    {
        await base.Subquery_containing_join_projecting_main_from_clause_gets_lifted(async);
    }

#if EF10
    public override async Task Subquery_containing_left_join_projecting_main_from_clause_gets_lifted(bool async)
    {
        await base.Subquery_containing_left_join_projecting_main_from_clause_gets_lifted(async);
    }
#endif

    public override async Task Subquery_containing_join_gets_lifted_clashing_names(bool async)
    {
        await base.Subquery_containing_join_gets_lifted_clashing_names(async);
    }

    public override async Task Subquery_created_by_include_gets_lifted_nested(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Subquery_created_by_include_gets_lifted_nested(async), "Unsupported cross-DbSet query");
    }

    public override async Task Subquery_is_lifted_from_additional_from_clause(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_is_lifted_from_additional_from_clause(async), "could not be translated");
    }

    public override async Task Subquery_with_result_operator_is_not_lifted(bool async)
    {
        await base.Subquery_with_result_operator_is_not_lifted(async);
    }

    public override async Task Skip_with_orderby_followed_by_orderBy_is_pushed_down(bool async)
    {
        await base.Skip_with_orderby_followed_by_orderBy_is_pushed_down(async);
    }

    public override async Task Take_without_orderby_followed_by_orderBy_is_pushed_down1(bool async)
    {
        await base.Take_without_orderby_followed_by_orderBy_is_pushed_down1(async);
    }

    public override async Task Take_without_orderby_followed_by_orderBy_is_pushed_down2(bool async)
    {
        await base.Take_without_orderby_followed_by_orderBy_is_pushed_down2(async);
    }

    public override async Task Take_without_orderby_followed_by_orderBy_is_pushed_down3(bool async)
    {
        await base.Take_without_orderby_followed_by_orderBy_is_pushed_down3(async);
    }

    public override async Task Select_length_of_string_property(bool async)
    {
        await base.Select_length_of_string_property(async);
    }

    public override async Task Client_method_on_collection_navigation_in_predicate(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Client_method_on_collection_navigation_in_predicate(async), "Unsupported cross-DbSet query");
    }

    public override async Task Client_method_on_collection_navigation_in_predicate_accessed_by_ef_property(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Client_method_on_collection_navigation_in_predicate_accessed_by_ef_property(async), "Unsupported cross-DbSet query");
    }

    public override async Task Client_method_on_collection_navigation_in_order_by(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Client_method_on_collection_navigation_in_order_by(async), "Unsupported cross-DbSet query");
    }

    public override async Task Client_method_on_collection_navigation_in_additional_from_clause(bool async)
    {
        await base.Client_method_on_collection_navigation_in_additional_from_clause(async);
    }

    public override async Task Client_method_on_collection_navigation_in_outer_join_key(bool async)
    {
        await base.Client_method_on_collection_navigation_in_outer_join_key(async);
    }

    public override async Task Member_access_on_derived_entity_using_cast(bool async)
    {
        await base.Member_access_on_derived_entity_using_cast(async);
    }

    public override async Task Member_access_on_derived_materialized_entity_using_cast(bool async)
    {
        await base.Member_access_on_derived_materialized_entity_using_cast(async);
    }

    public override async Task Member_access_on_derived_entity_using_cast_and_let(bool async)
    {
        await base.Member_access_on_derived_entity_using_cast_and_let(async);
    }

    public override async Task Property_access_on_derived_entity_using_cast(bool async)
    {
        await base.Property_access_on_derived_entity_using_cast(async);
    }

#if EF10
    public override async Task Navigation_access_on_derived_entity_using_cast(bool async)
    {
        await base.Navigation_access_on_derived_entity_using_cast(async);
    }
#endif

#if EF10
    public override async Task Navigation_access_on_derived_materialized_entity_using_cast(bool async)
    {
        await base.Navigation_access_on_derived_materialized_entity_using_cast(async);
    }
#endif

#if EF10
    public override async Task Navigation_access_via_EFProperty_on_derived_entity_using_cast(bool async)
    {
        await base.Navigation_access_via_EFProperty_on_derived_entity_using_cast(async);
    }
#endif

#if EF10
    public override async Task Navigation_access_fk_on_derived_entity_using_cast(bool async)
    {
        await base.Navigation_access_fk_on_derived_entity_using_cast(async);
    }
#endif

    public override async Task Collection_navigation_access_on_derived_entity_using_cast(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_navigation_access_on_derived_entity_using_cast(async), "could not be translated");
    }

    public override async Task Collection_navigation_access_on_derived_entity_using_cast_in_SelectMany(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_navigation_access_on_derived_entity_using_cast_in_SelectMany(async), "could not be translated");
    }

#if EF10
    public override async Task Include_on_derived_entity_using_OfType(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_on_derived_entity_using_OfType(async), "Failure");
    }
#endif

#if EF10
    public override async Task Include_on_derived_entity_with_cast(bool async)
    {
        await base.Include_on_derived_entity_with_cast(async);
    }
#endif

    public override async Task Distinct_on_subquery_doesnt_get_lifted(bool async)
    {
        await base.Distinct_on_subquery_doesnt_get_lifted(async);
    }

    public override async Task Cast_result_operator_on_subquery_is_properly_lifted_to_a_convert(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Cast_result_operator_on_subquery_is_properly_lifted_to_a_convert(async), "Expression not supported");
    }

    public override async Task Comparing_two_collection_navigations_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Comparing_two_collection_navigations_composite_key(async), "could not be translated");
    }

    public override async Task Comparing_two_collection_navigations_inheritance(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Comparing_two_collection_navigations_inheritance(async), "could not be translated");
    }

    public override async Task Comparing_entities_using_Equals_inheritance(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Comparing_entities_using_Equals_inheritance(async), "could not be translated");
    }

#if EF10
    public override async Task Contains_on_nullable_array_produces_correct_sql(bool async)
    {
        await base.Contains_on_nullable_array_produces_correct_sql(async);
    }
#endif

#if EF10
    public override async Task Optional_navigation_with_collection_composite_key(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_with_collection_composite_key(async), "Expression not supported");
    }
#endif

    public override async Task Select_null_conditional_with_inheritance(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.Select_null_conditional_with_inheritance(async), "ArgumentNullException");
    }

    public override async Task Select_null_conditional_with_inheritance_negative(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.Select_null_conditional_with_inheritance_negative(async), "ArgumentNullException");
    }

    public override async Task Project_collection_navigation_with_inheritance1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation_with_inheritance1(async), "could not be translated");
    }

    public override async Task Project_collection_navigation_with_inheritance2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation_with_inheritance2(async), "could not be translated");
    }

    public override async Task Project_collection_navigation_with_inheritance3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation_with_inheritance3(async), "could not be translated");
    }

#if EF10
    public override async Task Include_reference_on_derived_type_using_string(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_string(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Include_reference_on_derived_type_using_EF_Property(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_EF_Property(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Include_reference_on_derived_type_using_string_nested1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_string_nested1(async), "Failure");
    }
#endif

#if EF10
    public override async Task Include_reference_on_derived_type_using_string_nested2(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_string_nested2(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Include_reference_on_derived_type_using_lambda(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_lambda(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Include_reference_on_derived_type_using_lambda_with_soft_cast(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_lambda_with_soft_cast(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Include_reference_on_derived_type_using_lambda_with_tracking(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_lambda_with_tracking(async), "Expression not supported");
    }
#endif

    public override async Task Include_collection_on_derived_type_using_string(bool async)
    {
        await base.Include_collection_on_derived_type_using_string(async);
    }

    public override async Task Include_collection_on_derived_type_using_EF_Property(bool async)
    {
        await base.Include_collection_on_derived_type_using_EF_Property(async);
    }

    public override async Task Include_collection_on_derived_type_using_lambda(bool async)
    {
        await base.Include_collection_on_derived_type_using_lambda(async);
    }

    public override async Task Include_collection_on_derived_type_using_lambda_with_soft_cast(bool async)
    {
        await base.Include_collection_on_derived_type_using_lambda_with_soft_cast(async);
    }

#if EF10
    public override async Task Include_base_navigation_on_derived_entity(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_base_navigation_on_derived_entity(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task ThenInclude_collection_on_derived_after_base_reference(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.ThenInclude_collection_on_derived_after_base_reference(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task ThenInclude_collection_on_derived_after_derived_reference(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.ThenInclude_collection_on_derived_after_derived_reference(async), "Expression not supported");
    }
#endif

    public override async Task ThenInclude_collection_on_derived_after_derived_collection(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.ThenInclude_collection_on_derived_after_derived_collection(async), "Failure");
    }

    public override async Task ThenInclude_reference_on_derived_after_derived_collection(bool async)
    {
        await base.ThenInclude_reference_on_derived_after_derived_collection(async);
    }

#if EF10
    public override async Task Multiple_derived_included_on_one_method(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Multiple_derived_included_on_one_method(async), "Expression not supported");
    }
#endif

    public override async Task Include_on_derived_multi_level(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include_on_derived_multi_level(async), "Failure");
    }

    public override async Task Include_collection_and_invalid_navigation_using_string_throws(bool async)
    {
        await base.Include_collection_and_invalid_navigation_using_string_throws(async);
    }

#if EF10
    public override async Task Projecting_nullable_bool_in_conditional_works(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_nullable_bool_in_conditional_works(async), "Expression not supported");
    }
#endif

    public override async Task Correlated_collections_naked_navigation_with_ToList(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_naked_navigation_with_ToList(async), "could not be translated");
    }

    public override async Task Correlated_collections_naked_navigation_with_ToList_followed_by_projecting_count(bool async)
    {
        await base.Correlated_collections_naked_navigation_with_ToList_followed_by_projecting_count(async);
    }

    public override async Task Correlated_collections_naked_navigation_with_ToArray(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_naked_navigation_with_ToArray(async), "could not be translated");
    }

    public override async Task Correlated_collections_basic_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_basic_projection(async), "could not be translated");
    }

    public override async Task Correlated_collections_basic_projection_explicit_to_list(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_basic_projection_explicit_to_list(async), "could not be translated");
    }

    public override async Task Correlated_collections_basic_projection_explicit_to_array(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_basic_projection_explicit_to_array(async), "could not be translated");
    }

    public override async Task Correlated_collections_basic_projection_ordered(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_basic_projection_ordered(async), "could not be translated");
    }

    public override async Task Correlated_collections_basic_projection_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_basic_projection_composite_key(async), "could not be translated");
    }

    public override async Task Correlated_collections_basic_projecting_single_property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_basic_projecting_single_property(async), "could not be translated");
    }

    public override async Task Correlated_collections_basic_projecting_constant(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_basic_projecting_constant(async), "could not be translated");
    }

    public override async Task Correlated_collections_basic_projecting_constant_bool(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_basic_projecting_constant_bool(async), "could not be translated");
    }

    public override async Task Correlated_collections_projection_of_collection_thru_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_projection_of_collection_thru_navigation(async), "could not be translated");
    }

    public override async Task Correlated_collections_project_anonymous_collection_result(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_project_anonymous_collection_result(async), "could not be translated");
    }

    public override async Task Correlated_collections_nested(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_nested(async), "could not be translated");
    }

    public override async Task Correlated_collections_nested_mixed_streaming_with_buffer1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_nested_mixed_streaming_with_buffer1(async), "could not be translated");
    }

    public override async Task Correlated_collections_nested_mixed_streaming_with_buffer2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_nested_mixed_streaming_with_buffer2(async), "could not be translated");
    }

    public override async Task Correlated_collections_nested_with_custom_ordering(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_nested_with_custom_ordering(async), "could not be translated");
    }

    public override async Task Correlated_collections_same_collection_projected_multiple_times(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_same_collection_projected_multiple_times(async), "could not be translated");
    }

    public override async Task Correlated_collections_similar_collection_projected_multiple_times(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_similar_collection_projected_multiple_times(async), "could not be translated");
    }

    public override async Task Correlated_collections_different_collections_projected(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_different_collections_projected(async), "could not be translated");
    }

#if EF10
    public override async Task Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys(async), "Expression not supported");
    }
#endif

    public override async Task Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery(async), "could not be translated");
    }

    public override async Task Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery_duplicated_orderings(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery_duplicated_orderings(async), "could not be translated");
    }

    public override async Task Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery_complex_orderings(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery_complex_orderings(async), "could not be translated");
    }

    public override async Task Correlated_collections_multiple_nested_complex_collections(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_multiple_nested_complex_collections(async), "could not be translated");
    }

    public override async Task Correlated_collections_inner_subquery_selector_references_outer_qsre(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_inner_subquery_selector_references_outer_qsre(async), "could not be translated");
    }

    public override async Task Correlated_collections_inner_subquery_predicate_references_outer_qsre(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_inner_subquery_predicate_references_outer_qsre(async), "could not be translated");
    }

    public override async Task Correlated_collections_nested_inner_subquery_references_outer_qsre_one_level_up(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_nested_inner_subquery_references_outer_qsre_one_level_up(async), "could not be translated");
    }

    public override async Task Correlated_collections_nested_inner_subquery_references_outer_qsre_two_levels_up(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_nested_inner_subquery_references_outer_qsre_two_levels_up(async), "could not be translated");
    }

    public override async Task Correlated_collections_on_select_many(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_on_select_many(async), "could not be translated");
    }

    public override async Task Correlated_collections_with_Skip(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_with_Skip(async), "could not be translated");
    }

    public override async Task Correlated_collections_with_Take(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_with_Take(async), "could not be translated");
    }

    public override async Task Correlated_collections_with_Distinct(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_with_Distinct(async), "could not be translated");
    }

    public override async Task Correlated_collections_with_FirstOrDefault(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_with_FirstOrDefault(async), "could not be translated");
    }

    public override async Task Correlated_collections_on_left_join_with_predicate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_on_left_join_with_predicate(async), "could not be translated");
    }

#if EF10
    public override async Task Correlated_collections_on_RightJoin_with_predicate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_on_RightJoin_with_predicate(async), "could not be translated");
    }
#endif

    public override async Task Correlated_collections_on_left_join_with_null_value(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_on_left_join_with_null_value(async), "could not be translated");
    }

    public override async Task Correlated_collections_left_join_with_self_reference(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_left_join_with_self_reference(async), "could not be translated");
    }

    public override async Task Correlated_collections_deeply_nested_left_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_deeply_nested_left_join(async), "could not be translated");
    }

    public override async Task Correlated_collections_from_left_join_with_additional_elements_projected_of_that_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_from_left_join_with_additional_elements_projected_of_that_join(async), "could not be translated");
    }

    public override async Task Correlated_collections_complex_scenario1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_complex_scenario1(async), "could not be translated");
    }

    public override async Task Correlated_collections_complex_scenario2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_complex_scenario2(async), "could not be translated");
    }

    public override async Task Correlated_collections_with_funky_orderby_complex_scenario1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_with_funky_orderby_complex_scenario1(async), "could not be translated");
    }

    public override async Task Correlated_collections_with_funky_orderby_complex_scenario2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collections_with_funky_orderby_complex_scenario2(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_top_level_FirstOrDefault(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_top_level_FirstOrDefault(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_top_level_Count(bool async)
    {
        await base.Correlated_collection_with_top_level_Count(async);
    }

    public override async Task Correlated_collection_with_top_level_Last_with_orderby_on_outer(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_top_level_Last_with_orderby_on_outer(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_top_level_Last_with_order_by_on_inner(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_top_level_Last_with_order_by_on_inner(async), "could not be translated");
    }

    public override async Task Null_semantics_on_nullable_bool_from_inner_join_subquery_is_fully_applied(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Null_semantics_on_nullable_bool_from_inner_join_subquery_is_fully_applied(async), "Expression not supported");
    }

#if EF10
    public override async Task Null_semantics_on_nullable_bool_from_left_join_subquery_is_fully_applied(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Null_semantics_on_nullable_bool_from_left_join_subquery_is_fully_applied(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Include_on_derived_type_with_order_by_and_paging(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_on_derived_type_with_order_by_and_paging(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Select_required_navigation_on_derived_type(bool async)
    {
        await base.Select_required_navigation_on_derived_type(async);
    }
#endif

    public override async Task Select_required_navigation_on_the_same_type_with_cast(bool async)
    {
        await base.Select_required_navigation_on_the_same_type_with_cast(async);
    }

#if EF10
    public override async Task Where_required_navigation_on_derived_type(bool async)
    {
        await base.Where_required_navigation_on_derived_type(async);
    }
#endif

    public override async Task Outer_parameter_in_join_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Outer_parameter_in_join_key(async), "could not be translated");
    }

    public override async Task Outer_parameter_in_join_key_inner_and_outer(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Outer_parameter_in_join_key_inner_and_outer(async), "could not be translated");
    }

    public override async Task Outer_parameter_in_group_join_with_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Outer_parameter_in_group_join_with_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task Include_with_concat(bool async)
    {
        await base.Include_with_concat(async);
    }

#if EF10
    public override async Task Negated_bool_ternary_inside_anonymous_type_in_projection(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Negated_bool_ternary_inside_anonymous_type_in_projection(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Order_by_entity_qsre(bool async)
    {
        await base.Order_by_entity_qsre(async);
    }
#endif

    public override async Task Order_by_entity_qsre_with_inheritance(bool async)
    {
        await base.Order_by_entity_qsre_with_inheritance(async);
    }

#if EF10
    public override async Task Order_by_entity_qsre_composite_key(bool async)
    {
        await base.Order_by_entity_qsre_composite_key(async);
    }
#endif

#if EF10
    public override async Task Order_by_entity_qsre_with_other_orderbys(bool async)
    {
        await base.Order_by_entity_qsre_with_other_orderbys(async);
    }
#endif

    public override async Task Join_on_entity_qsre_keys(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_on_entity_qsre_keys(async), "Expression not supported");
    }

    public override async Task Join_on_entity_qsre_keys_composite_key(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_on_entity_qsre_keys_composite_key(async), "Expression not supported");
    }

    public override async Task Join_on_entity_qsre_keys_inheritance(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_on_entity_qsre_keys_inheritance(async), "Expression not supported");
    }

    public override async Task Join_on_entity_qsre_keys_outer_key_is_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_on_entity_qsre_keys_outer_key_is_navigation(async), "could not be translated");
    }

#if EF10
    public override async Task Join_on_entity_qsre_keys_inner_key_is_navigation(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_on_entity_qsre_keys_inner_key_is_navigation(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Join_on_entity_qsre_keys_inner_key_is_navigation_composite_key(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_on_entity_qsre_keys_inner_key_is_navigation_composite_key(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Join_on_entity_qsre_keys_inner_key_is_nested_navigation(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Join_on_entity_qsre_keys_inner_key_is_nested_navigation(async), "Unsupported cross-DbSet query");
    }
#endif

#if EF10
    public override async Task GroupJoin_on_entity_qsre_keys_inner_key_is_nested_navigation(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_on_entity_qsre_keys_inner_key_is_nested_navigation(async), "ArgumentException");
    }
#endif

    public override async Task Join_with_complex_key_selector(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_with_complex_key_selector(async), "Expression not supported");
    }

    public override async Task Streaming_correlated_collection_issue_11403_returning_ordered_enumerable_throws(bool async)
    {
        await base.Streaming_correlated_collection_issue_11403_returning_ordered_enumerable_throws(async);
    }

    public override async Task Streaming_correlated_collection_issue_11403(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Streaming_correlated_collection_issue_11403(async), "could not be translated");
    }

    public override async Task Project_one_value_type_from_empty_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_one_value_type_from_empty_collection(async), "could not be translated");
    }

    public override async Task Project_one_value_type_converted_to_nullable_from_empty_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_one_value_type_converted_to_nullable_from_empty_collection(async), "could not be translated");
    }

    public override async Task Project_one_value_type_with_client_projection_from_empty_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_one_value_type_with_client_projection_from_empty_collection(async), "could not be translated");
    }

    public override async Task Filter_on_subquery_projecting_one_value_type_from_empty_collection(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Filter_on_subquery_projecting_one_value_type_from_empty_collection(async), "Unsupported cross-DbSet query");
    }

    public override async Task Select_subquery_projecting_single_constant_int(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_projecting_single_constant_int(async), "could not be translated");
    }

    public override async Task Select_subquery_projecting_single_constant_string(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_projecting_single_constant_string(async), "could not be translated");
    }

    public override async Task Select_subquery_projecting_single_constant_bool(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_projecting_single_constant_bool(async), "could not be translated");
    }

    public override async Task Select_subquery_projecting_single_constant_inside_anonymous(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_projecting_single_constant_inside_anonymous(async), "could not be translated");
    }

    public override async Task Select_subquery_projecting_multiple_constants_inside_anonymous(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_projecting_multiple_constants_inside_anonymous(async), "could not be translated");
    }

    public override async Task Include_with_order_by_constant(bool async)
    {
        await base.Include_with_order_by_constant(async);
    }

    public override async Task Correlated_collection_order_by_constant(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_order_by_constant(async), "could not be translated");
    }

    public override async Task Select_subquery_projecting_single_constant_null_of_non_mapped_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_projecting_single_constant_null_of_non_mapped_type(async), "could not be translated");
    }

    public override async Task Select_subquery_projecting_single_constant_of_non_mapped_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_projecting_single_constant_of_non_mapped_type(async), "could not be translated");
    }

    public override async Task Correlated_collection_order_by_constant_null_of_non_mapped_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_order_by_constant_null_of_non_mapped_type(async), "could not be translated");
    }

    public override async Task Include_collection_OrderBy_aggregate(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Include_collection_OrderBy_aggregate(async), "Unsupported cross-DbSet query");
    }

    public override async Task Include_collection_with_complex_OrderBy2(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_complex_OrderBy2(async), "Unsupported cross-DbSet query");
    }

    public override async Task Include_collection_with_complex_OrderBy3(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_complex_OrderBy3(async), "Unsupported cross-DbSet query");
    }

    public override async Task Correlated_collection_with_complex_OrderBy(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_complex_OrderBy(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_very_complex_order_by(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_very_complex_order_by(async), "could not be translated");
    }

    public override async Task Cast_to_derived_type_causes_client_eval(bool async)
    {
        // Fails: Property converted to nullable/other type in a projection: no coercion operator defined (int to string) EF-X543
        await SpecFails.ThrowsAsync(() => base.Cast_to_derived_type_causes_client_eval(async));
    }

    public override async Task Cast_to_derived_type_after_OfType_works(bool async)
    {
        await base.Cast_to_derived_type_after_OfType_works(async);
    }

    public override async Task Select_subquery_boolean(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_boolean(async), "could not be translated");
    }

    public override async Task Select_subquery_boolean_with_pushdown(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_boolean_with_pushdown(async), "could not be translated");
    }

    public override async Task Select_subquery_int_with_inside_cast_and_coalesce(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_int_with_inside_cast_and_coalesce(async), "could not be translated");
    }

    public override async Task Select_subquery_int_with_outside_cast_and_coalesce(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_int_with_outside_cast_and_coalesce(async), "could not be translated");
    }

    public override async Task Select_subquery_int_with_pushdown_and_coalesce(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_int_with_pushdown_and_coalesce(async), "could not be translated");
    }

    public override async Task Select_subquery_int_with_pushdown_and_coalesce2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_int_with_pushdown_and_coalesce2(async), "could not be translated");
    }

    public override async Task Select_subquery_boolean_empty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_boolean_empty(async), "could not be translated");
    }

    public override async Task Select_subquery_boolean_empty_with_pushdown(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_boolean_empty_with_pushdown(async), "could not be translated");
    }

    public override async Task Select_subquery_distinct_singleordefault_boolean1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_distinct_singleordefault_boolean1(async), "could not be translated");
    }

    public override async Task Select_subquery_distinct_singleordefault_boolean2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_distinct_singleordefault_boolean2(async), "could not be translated");
    }

    public override async Task Select_subquery_distinct_singleordefault_boolean_with_pushdown(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_distinct_singleordefault_boolean_with_pushdown(async), "could not be translated");
    }

    public override async Task Select_subquery_distinct_singleordefault_boolean_empty1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_distinct_singleordefault_boolean_empty1(async), "could not be translated");
    }

    public override async Task Select_subquery_distinct_singleordefault_boolean_empty2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_distinct_singleordefault_boolean_empty2(async), "could not be translated");
    }

    public override async Task Select_subquery_distinct_singleordefault_boolean_empty_with_pushdown(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_distinct_singleordefault_boolean_empty_with_pushdown(async), "could not be translated");
    }

    public override async Task Cast_subquery_to_base_type_using_typed_ToList(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Cast_subquery_to_base_type_using_typed_ToList(async), "could not be translated");
    }

    public override async Task Cast_ordered_subquery_to_base_type_using_typed_ToArray(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Cast_ordered_subquery_to_base_type_using_typed_ToArray(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_complex_order_by_funcletized_to_constant_bool(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_complex_order_by_funcletized_to_constant_bool(async), "could not be translated");
    }

#if EF10
    public override async Task Double_order_by_on_nullable_bool_coming_from_optional_navigation(bool async)
    {
        await base.Double_order_by_on_nullable_bool_coming_from_optional_navigation(async);
    }
#endif

#if EF10
    public override async Task Double_order_by_on_Like(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Double_order_by_on_Like(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Double_order_by_on_is_null(bool async)
    {
        await base.Double_order_by_on_is_null(async);
    }
#endif

    public override async Task Double_order_by_on_string_compare(bool async)
    {
        await base.Double_order_by_on_string_compare(async);
    }

    public override async Task Double_order_by_binary_expression(bool async)
    {
        await base.Double_order_by_binary_expression(async);
    }

#if EF10
    public override async Task String_compare_with_null_conditional_argument(bool async)
    {
        await base.String_compare_with_null_conditional_argument(async);
    }
#endif

#if EF10
    public override async Task String_compare_with_null_conditional_argument2(bool async)
    {
        await base.String_compare_with_null_conditional_argument2(async);
    }
#endif

#if EF10
    public override async Task String_concat_with_null_conditional_argument(bool async)
    {
        await base.String_concat_with_null_conditional_argument(async);
    }
#endif

#if EF10
    public override async Task String_concat_with_null_conditional_argument2(bool async)
    {
        await base.String_concat_with_null_conditional_argument2(async);
    }
#endif

    public override async Task String_concat_nullable_expressions_are_coalesced(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.String_concat_nullable_expressions_are_coalesced(async), "Expression not supported");
    }

    public override async Task String_concat_on_various_types(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_concat_on_various_types(async), "could not be translated");
    }

    public override async Task GroupBy_Property_Include_Select_Average(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_Property_Include_Select_Average(async), "could not be translated");
    }

    public override async Task GroupBy_Property_Include_Select_Sum(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_Property_Include_Select_Sum(async), "could not be translated");
    }

    public override async Task GroupBy_Property_Include_Select_Count(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_Property_Include_Select_Count(async), "could not be translated");
    }

    public override async Task GroupBy_Property_Include_Select_LongCount(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_Property_Include_Select_LongCount(async), "could not be translated");
    }

    public override async Task GroupBy_Property_Include_Select_Max(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_Property_Include_Select_Max(async), "could not be translated");
    }

    public override async Task GroupBy_Property_Include_Select_Min(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_Property_Include_Select_Min(async), "could not be translated");
    }

    public override async Task GroupBy_Property_Include_Aggregate_with_anonymous_selector(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_Property_Include_Aggregate_with_anonymous_selector(async), "could not be translated");
    }

    public override async Task Group_by_with_include_with_entity_in_result_selector(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Group_by_with_include_with_entity_in_result_selector(async), "could not be translated");
    }

    public override async Task Include_with_group_by_and_FirstOrDefault_gets_properly_applied(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_with_group_by_and_FirstOrDefault_gets_properly_applied(async), "could not be translated");
    }

    public override async Task Include_collection_with_Cast_to_base(bool async)
    {
        await base.Include_collection_with_Cast_to_base(async);
    }

#if EF10
    public override async Task Include_with_client_method_and_member_access_still_applies_includes(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_with_client_method_and_member_access_still_applies_includes(async), "Expression not supported");
    }
#endif

    public override async Task Include_with_projection_of_unmapped_property_still_gets_applied(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Include_with_projection_of_unmapped_property_still_gets_applied(async), "Expression not supported");
    }

    public override async Task Multiple_includes_with_client_method_around_entity_and_also_projecting_included_collection()
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Multiple_includes_with_client_method_around_entity_and_also_projecting_included_collection(), "Expression not supported");
    }

    public override async Task OrderBy_same_expression_containing_IsNull_correctly_deduplicates_the_ordering(bool async)
    {
        await base.OrderBy_same_expression_containing_IsNull_correctly_deduplicates_the_ordering(async);
    }

    public override async Task GetValueOrDefault_in_projection(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GetValueOrDefault_in_projection(async), "Expression not supported");
    }

    public override async Task GetValueOrDefault_in_filter(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GetValueOrDefault_in_filter(async), "Expression not supported");
    }

    public override async Task GetValueOrDefault_in_filter_non_nullable_column(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GetValueOrDefault_in_filter_non_nullable_column(async), "Expression not supported");
    }

    public override async Task GetValueOrDefault_on_DateTimeOffset(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GetValueOrDefault_on_DateTimeOffset(async), "Expression not supported");
    }

    public override async Task GetValueOrDefault_in_order_by(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GetValueOrDefault_in_order_by(async), "Expression not supported");
    }

    public override async Task GetValueOrDefault_with_argument(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GetValueOrDefault_with_argument(async), "Expression not supported");
    }

    public override async Task GetValueOrDefault_with_argument_complex(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GetValueOrDefault_with_argument_complex(async), "Expression not supported");
    }

    public override async Task Filter_with_complex_predicate_containing_subquery(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Filter_with_complex_predicate_containing_subquery(async), "Unsupported cross-DbSet query");
    }

    public override async Task Query_with_complex_let_containing_ordering_and_filter_projecting_firstOrDefault_element_of_let(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Query_with_complex_let_containing_ordering_and_filter_projecting_firstOrDefault_element_of_let(async), "could not be translated");
    }

#if EF10
    public override async Task Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation_complex(bool async)
    {
        // Fails: Server error executing the generated pipeline EF-X528
        await SpecFails.ThrowsAsync(() => base.Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation_complex(async), "MongoCommandException");
    }
#endif

#if EF10
    public override async Task OfTypeNav1(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.OfTypeNav1(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task OfTypeNav2(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.OfTypeNav2(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task OfTypeNav3(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.OfTypeNav3(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Nav_rewrite_Distinct_with_convert()
    {
        // Fails: Set operation/GroupBy result builds an invalid materialization expression (QueryingEnumerable not assignable) EF-X532
        await SpecFails.ThrowsAsync(() => base.Nav_rewrite_Distinct_with_convert());
    }
#endif

#if EF10
    public override async Task Nav_rewrite_Distinct_with_convert_anonymous()
    {
        // Fails: Set operation/GroupBy result builds an invalid materialization expression (QueryingEnumerable not assignable) EF-X532
        await SpecFails.ThrowsAsync(() => base.Nav_rewrite_Distinct_with_convert_anonymous());
    }
#endif

#if EF10
    public override async Task Nav_rewrite_with_convert1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Nav_rewrite_with_convert1(async), "Failure");
    }
#endif

#if EF10
    public override async Task Nav_rewrite_with_convert2(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Nav_rewrite_with_convert2(async), "Document element is missing for required");
    }
#endif

#if EF10
    public override async Task Nav_rewrite_with_convert3(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Nav_rewrite_with_convert3(async), "Document element is missing for required");
    }
#endif

    public override async Task Where_contains_on_navigation_with_composite_keys(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_contains_on_navigation_with_composite_keys(async), "Unsupported cross-DbSet query");
    }

    public override async Task Project_derivied_entity_with_convert_to_parent(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Project_derivied_entity_with_convert_to_parent(async), "Document element is missing for required");
    }

    public override async Task Include_with_complex_order_by(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Include_with_complex_order_by(async), "Unsupported cross-DbSet query");
    }

    public override async Task Anonymous_projection_take_followed_by_projecting_single_element_from_collection_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Anonymous_projection_take_followed_by_projecting_single_element_from_collection_navigation(async), "could not be translated");
    }

    public override async Task Bool_projection_from_subquery_treated_appropriately_in_where(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Bool_projection_from_subquery_treated_appropriately_in_where(async), "Unsupported cross-DbSet query");
    }

    public override async Task DateTimeOffset_Contains_Less_than_Greater_than(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.DateTimeOffset_Contains_Less_than_Greater_than(async), "Expression not supported");
    }

    public override async Task DateTimeOffsetNow_minus_timespan(bool async)
    {
        await base.DateTimeOffsetNow_minus_timespan(async);
    }

#if EF10
    public override async Task Navigation_inside_interpolated_string_expanded(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Navigation_inside_interpolated_string_expanded(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Left_join_projection_using_coalesce_tracking(bool async)
    {
        await base.Left_join_projection_using_coalesce_tracking(async);
    }
#endif

#if EF10
    public override async Task Left_join_projection_using_conditional_tracking(bool async)
    {
        await base.Left_join_projection_using_conditional_tracking(async);
    }
#endif

    public override async Task Project_collection_navigation_nested_with_take_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation_nested_with_take_composite_key(async), "could not be translated");
    }

    public override async Task Project_collection_navigation_nested_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_collection_navigation_nested_composite_key(async), "could not be translated");
    }

    public override async Task Null_checks_in_correlated_predicate_are_correctly_translated(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Null_checks_in_correlated_predicate_are_correctly_translated(async), "could not be translated");
    }

    public override async Task SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector(async), "could not be translated");
    }

    public override async Task SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector_not_equal(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector_not_equal(async), "could not be translated");
    }

    public override async Task SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector_order_comparison(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector_order_comparison(async), "could not be translated");
    }

#if EF10
    public override async Task DefaultIfEmpty_top_level_over_column_with_nullable_value_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.DefaultIfEmpty_top_level_over_column_with_nullable_value_type(async), "could not be translated");
    }
#endif

#if EF10
    public override async Task DefaultIfEmpty_top_level_over_arbitrary_expression_with_nullable_value_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.DefaultIfEmpty_top_level_over_arbitrary_expression_with_nullable_value_type(async), "could not be translated");
    }
#endif

#if EF10
    public override async Task DefaultIfEmpty_top_level_over_arbitrary_expression_with_non_nullable_value_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.DefaultIfEmpty_top_level_over_arbitrary_expression_with_non_nullable_value_type(async), "could not be translated");
    }
#endif

    public override async Task Join_with_inner_being_a_subquery_projecting_single_property(bool async)
    {
        await base.Join_with_inner_being_a_subquery_projecting_single_property(async);
    }

    public override async Task Join_with_inner_being_a_subquery_projecting_anonymous_type_with_single_property(bool async)
    {
        await base.Join_with_inner_being_a_subquery_projecting_anonymous_type_with_single_property(async);
    }

#if EF10
    public override async Task Navigation_based_on_complex_expression1(bool async)
    {
        await base.Navigation_based_on_complex_expression1(async);
    }
#endif

#if EF10
    public override async Task Navigation_based_on_complex_expression2(bool async)
    {
        await base.Navigation_based_on_complex_expression2(async);
    }
#endif

#if EF10
    public override async Task Navigation_based_on_complex_expression3(bool async)
    {
        await base.Navigation_based_on_complex_expression3(async);
    }
#endif

#if EF9 || EF10
    public override async Task Navigation_based_on_complex_expression4(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression4(async));
    }
#endif

#if EF10
    public override async Task Navigation_based_on_complex_expression5(bool async)
    {
        // Fails: Average/aggregate over a navigation without selector throws 'Operation is not valid due to the current state of the object' EF-X541
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression5(async));
    }
#endif

#if EF10
    public override async Task Navigation_based_on_complex_expression6(bool async)
    {
        // Fails: Average/aggregate over a navigation without selector throws 'Operation is not valid due to the current state of the object' EF-X541
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression6(async));
    }
#endif

    public override async Task Select_as_operator(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Select_as_operator(async), "Document element is missing for required");
    }

    public override async Task Select_datetimeoffset_comparison_in_projection(bool async)
    {
        await base.Select_datetimeoffset_comparison_in_projection(async);
    }

    public override async Task OfType_in_subquery_works(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.OfType_in_subquery_works(async), "could not be translated");
    }

    public override async Task Nullable_bool_comparison_is_translated_to_server(bool async)
    {
        await base.Nullable_bool_comparison_is_translated_to_server(async);
    }

    public override async Task Accessing_reference_navigation_collection_composition_generates_single_query(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Accessing_reference_navigation_collection_composition_generates_single_query(async), "could not be translated");
    }

#if EF10
    public override async Task Reference_include_chain_loads_correctly_when_middle_is_null(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Reference_include_chain_loads_correctly_when_middle_is_null(async), "Failure");
    }
#endif

    public override async Task Accessing_property_of_optional_navigation_in_child_projection_works(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Accessing_property_of_optional_navigation_in_child_projection_works(async), "could not be translated");
    }

    public override async Task Collection_navigation_ofType_filter_works(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Collection_navigation_ofType_filter_works(async), "Unsupported cross-DbSet query");
    }

    public override async Task Include_after_select_with_cast_throws(bool async)
    {
        await base.Include_after_select_with_cast_throws(async);
    }

    public override async Task Include_after_select_with_entity_projection_throws(bool async)
    {
        await base.Include_after_select_with_entity_projection_throws(async);
    }

    public override async Task Include_after_select_anonymous_projection_throws(bool async)
    {
        await base.Include_after_select_anonymous_projection_throws(async);
    }

#if EF10
    public override async Task Include_after_Select_throws(bool async)
    {
        await base.Include_after_Select_throws(async);
    }
#endif

    public override async Task Include_after_SelectMany_throws(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_after_SelectMany_throws(async), "could not be translated");
    }

    public override async Task Query_reusing_parameter_doesnt_declare_duplicate_parameter(bool async)
    {
        await base.Query_reusing_parameter_doesnt_declare_duplicate_parameter(async);
    }

    public override async Task Query_reusing_parameter_with_inner_query_doesnt_declare_duplicate_parameter(bool async)
    {
        // Fails: Join key selector that is a sub-query: serializer cannot be created for MongoQuery EF-X534
        await SpecFails.ThrowsAsync(() => base.Query_reusing_parameter_with_inner_query_doesnt_declare_duplicate_parameter(async), "BsonSerializationException");
    }

    public override async Task Query_reusing_parameter_with_inner_query_expression_doesnt_declare_duplicate_parameter(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Query_reusing_parameter_with_inner_query_expression_doesnt_declare_duplicate_parameter(async), "Unsupported cross-DbSet query");
    }

    public override async Task Query_reusing_parameter_doesnt_declare_duplicate_parameter_complex(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.ThrowsAsync(() => base.Query_reusing_parameter_doesnt_declare_duplicate_parameter_complex(async), "composed between two cross-collection joins");
    }

    public override async Task Project_entity_and_collection_element(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_entity_and_collection_element(async), "could not be translated");
    }

    public override async Task Complex_GroupBy_after_set_operator(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_GroupBy_after_set_operator(async), "could not be translated");
    }

#if EF10
    public override async Task Complex_GroupBy_after_set_operator_using_result_selector(bool async)
    {
        // Fails: Set operation/GroupBy result builds an invalid materialization expression (QueryingEnumerable not assignable) EF-X532
        await SpecFails.ThrowsAsync(() => base.Complex_GroupBy_after_set_operator_using_result_selector(async), "Expression of type");
    }
#endif

    public override async Task Left_join_with_GroupBy_with_composite_group_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Left_join_with_GroupBy_with_composite_group_key(async), "could not be translated");
    }

    public override async Task GroupBy_with_boolean_grouping_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_with_boolean_grouping_key(async), "could not be translated");
    }

    public override async Task GroupBy_with_boolean_groupin_key_thru_navigation_access(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_with_boolean_groupin_key_thru_navigation_access(async), "could not be translated");
    }

    public override async Task Group_by_over_projection_with_multiple_properties_accessed_thru_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Group_by_over_projection_with_multiple_properties_accessed_thru_navigation(async), "could not be translated");
    }

    public override async Task Group_by_with_aggregate_max_on_entity_type(bool async)
    {
        await base.Group_by_with_aggregate_max_on_entity_type(async);
    }

    public override async Task Group_by_on_StartsWith_with_null_parameter_as_argument(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Group_by_on_StartsWith_with_null_parameter_as_argument(async), "could not be translated");
    }

    public override async Task Group_by_with_having_StartsWith_with_null_parameter_as_argument(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Group_by_with_having_StartsWith_with_null_parameter_as_argument(async), "could not be translated");
    }

    public override async Task Select_StartsWith_with_null_parameter_as_argument(bool async)
    {
        // Fails: Server error executing the generated pipeline EF-X528
        await SpecFails.ThrowsAsync(() => base.Select_StartsWith_with_null_parameter_as_argument(async), "MongoCommandException");
    }

    public override async Task Select_null_parameter_is_not_null(bool async)
    {
        await base.Select_null_parameter_is_not_null(async);
    }

    public override async Task Where_null_parameter_is_not_null(bool async)
    {
        await base.Where_null_parameter_is_not_null(async);
    }

    public override async Task OrderBy_StartsWith_with_null_parameter_as_argument(bool async)
    {
        // Fails: Server error executing the generated pipeline EF-X528
        await SpecFails.ThrowsAsync(() => base.OrderBy_StartsWith_with_null_parameter_as_argument(async), "MongoCommandException");
    }

    public override async Task OrderBy_Contains_empty_list(bool async)
    {
        await base.OrderBy_Contains_empty_list(async);
    }

    public override async Task Where_with_enum_flags_parameter(bool async)
    {
        await base.Where_with_enum_flags_parameter(async);
    }

#if EF10
    public override async Task FirstOrDefault_navigation_access_entity_equality_in_where_predicate_apply_peneding_selector(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.FirstOrDefault_navigation_access_entity_equality_in_where_predicate_apply_peneding_selector(async), "ArgumentException");
    }
#endif

    public override async Task Conditional_expression_with_test_being_simplified_to_constant_simple(bool isAsync)
    {
        await base.Conditional_expression_with_test_being_simplified_to_constant_simple(isAsync);
    }

    public override async Task Conditional_expression_with_test_being_simplified_to_constant_complex(bool isAsync)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Conditional_expression_with_test_being_simplified_to_constant_complex(isAsync), "Unsupported cross-DbSet query");
    }

    public override async Task Bitwise_operation_with_non_null_parameter_optimizes_null_checks(bool async)
    {
        await base.Bitwise_operation_with_non_null_parameter_optimizes_null_checks(async);
    }

    public override async Task Bitwise_operation_with_null_arguments(bool async)
    {
        await base.Bitwise_operation_with_null_arguments(async);
    }

    public override async Task Logical_operation_with_non_null_parameter_optimizes_null_checks(bool async)
    {
        await base.Logical_operation_with_non_null_parameter_optimizes_null_checks(async);
    }

    public override async Task Cast_OfType_works_correctly(bool async)
    {
        await base.Cast_OfType_works_correctly(async);
    }

    public override async Task Join_inner_source_custom_projection_followed_by_filter(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_inner_source_custom_projection_followed_by_filter(async), "Expression not supported");
    }

    public override async Task Byte_array_filter_by_length_literal_does_not_cast_on_varbinary_n(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Byte_array_filter_by_length_literal_does_not_cast_on_varbinary_n(async), "Failure");
    }

#if EF10
    public override async Task OrderBy_bool_coming_from_optional_navigation(bool async)
    {
        await base.OrderBy_bool_coming_from_optional_navigation(async);
    }
#endif

    public override async Task DateTimeOffset_Date_returns_datetime(bool async)
    {
        await base.DateTimeOffset_Date_returns_datetime(async);
    }

    public override async Task Conditional_with_conditions_evaluating_to_false_gets_optimized(bool async)
    {
        await base.Conditional_with_conditions_evaluating_to_false_gets_optimized(async);
    }

    public override async Task Conditional_with_conditions_evaluating_to_true_gets_optimized(bool async)
    {
        await base.Conditional_with_conditions_evaluating_to_true_gets_optimized(async);
    }

    public override async Task Projecting_required_string_column_compared_to_null_parameter(bool async)
    {
        await base.Projecting_required_string_column_compared_to_null_parameter(async);
    }

    public override async Task Group_by_nullable_property_HasValue_and_project_the_grouping_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Group_by_nullable_property_HasValue_and_project_the_grouping_key(async), "could not be translated");
    }

    public override async Task Group_by_nullable_property_and_project_the_grouping_key_HasValue(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Group_by_nullable_property_and_project_the_grouping_key_HasValue(async), "could not be translated");
    }

    public override async Task Checked_context_with_cast_does_not_fail(bool isAsync)
    {
        await base.Checked_context_with_cast_does_not_fail(isAsync);
    }

    public override async Task Checked_context_with_addition_does_not_fail(bool isAsync)
    {
        await base.Checked_context_with_addition_does_not_fail(isAsync);
    }

    public override async Task Checked_context_throws_on_client_evaluation(bool isAsync)
    {
        await base.Checked_context_throws_on_client_evaluation(isAsync);
    }

    public override async Task Contains_on_collection_of_byte_subquery(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Contains_on_collection_of_byte_subquery(async), "Expression not supported");
    }

    public override async Task Contains_on_collection_of_nullable_byte_subquery(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Contains_on_collection_of_nullable_byte_subquery(async), "Expression not supported");
    }

    public override async Task Contains_on_collection_of_nullable_byte_subquery_null_constant(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Contains_on_collection_of_nullable_byte_subquery_null_constant(async), "Expression not supported");
    }

    public override async Task Contains_on_collection_of_nullable_byte_subquery_null_parameter(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Contains_on_collection_of_nullable_byte_subquery_null_parameter(async), "Expression not supported");
    }

    public override async Task Subquery_projecting_non_nullable_scalar_contains_non_nullable_value_doesnt_need_null_expansion(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_projecting_non_nullable_scalar_contains_non_nullable_value_doesnt_need_null_expansion(async), "could not be translated");
    }

    public override async Task Subquery_projecting_non_nullable_scalar_contains_non_nullable_value_doesnt_need_null_expansion_negated(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_projecting_non_nullable_scalar_contains_non_nullable_value_doesnt_need_null_expansion_negated(async), "could not be translated");
    }

    public override async Task Subquery_projecting_nullable_scalar_contains_nullable_value_needs_null_expansion(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_projecting_nullable_scalar_contains_nullable_value_needs_null_expansion(async), "could not be translated");
    }

    public override async Task Subquery_projecting_nullable_scalar_contains_nullable_value_needs_null_expansion_negated(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_projecting_nullable_scalar_contains_nullable_value_needs_null_expansion_negated(async), "could not be translated");
    }

    public override async Task Enum_closure_typed_as_underlying_type_generates_correct_parameter_type(bool async)
    {
        await base.Enum_closure_typed_as_underlying_type_generates_correct_parameter_type(async);
    }

    public override async Task Enum_flags_closure_typed_as_underlying_type_generates_correct_parameter_type(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Enum_flags_closure_typed_as_underlying_type_generates_correct_parameter_type(async), "Expression not supported");
    }

    public override async Task Enum_flags_closure_typed_as_different_type_generates_correct_parameter_type(bool async)
    {
        // Fails: Enum flags bitwise/closure typed as a different type: ConvertIntegralTypeToEnumSerializer lacks IHasRepresentationSerializer EF-X542
        await SpecFails.ThrowsAsync(() => base.Enum_flags_closure_typed_as_different_type_generates_correct_parameter_type(async), "IHasRepresentationSerializer");
    }

    public override async Task Constant_enum_with_same_underlying_value_as_previously_parameterized_int(bool async)
    {
        await base.Constant_enum_with_same_underlying_value_as_previously_parameterized_int(async);
    }

#if EF10
    public override async Task Enum_array_contains(bool async)
    {
        await base.Enum_array_contains(async);
    }
#endif

#if EF9 || EF10
    public override async Task Coalesce_with_non_root_evaluatable_Convert(bool async)
    {
        await base.Coalesce_with_non_root_evaluatable_Convert(async);
    }
#endif

    public override async Task Client_eval_followed_by_aggregate_operation(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Client_eval_followed_by_aggregate_operation(async), "Expression not supported");
    }

    public override async Task Trying_to_access_unmapped_property_throws_informative_error(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Trying_to_access_unmapped_property_throws_informative_error(async));
    }

    public override async Task Trying_to_access_unmapped_property_in_projection(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Trying_to_access_unmapped_property_in_projection(async), "Expression not supported");
    }

    public override async Task Trying_to_access_unmapped_property_inside_aggregate(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Trying_to_access_unmapped_property_inside_aggregate(async), "Unsupported cross-DbSet query");
    }

    public override async Task Trying_to_access_unmapped_property_inside_subquery(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Trying_to_access_unmapped_property_inside_subquery(async), "Unsupported cross-DbSet query");
    }

#if EF10
    public override async Task Trying_to_access_unmapped_property_inside_join_key_selector(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Trying_to_access_unmapped_property_inside_join_key_selector(async));
    }
#endif

    public override async Task Client_projection_with_nested_unmapped_property_bubbles_up_translation_failure_info(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Client_projection_with_nested_unmapped_property_bubbles_up_translation_failure_info(async), "could not be translated");
    }

    public override async Task Client_member_and_unsupported_string_Equals_in_the_same_query(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Client_member_and_unsupported_string_Equals_in_the_same_query(async), "Expression not supported");
    }

    public override async Task CompareTo_used_with_non_unicode_string_column_and_constant(bool async)
    {
        await base.CompareTo_used_with_non_unicode_string_column_and_constant(async);
    }

    public override async Task Coalesce_used_with_non_unicode_string_column_and_constant(bool async)
    {
        await base.Coalesce_used_with_non_unicode_string_column_and_constant(async);
    }

    public override async Task Groupby_anonymous_type_with_navigations_followed_up_by_anonymous_projection_and_orderby(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Groupby_anonymous_type_with_navigations_followed_up_by_anonymous_projection_and_orderby(async), "could not be translated");
    }

    public override async Task SelectMany_predicate_with_non_equality_comparison_converted_to_inner_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_predicate_with_non_equality_comparison_converted_to_inner_join(async), "could not be translated");
    }

    public override async Task SelectMany_predicate_with_non_equality_comparison_DefaultIfEmpty_converted_to_left_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_predicate_with_non_equality_comparison_DefaultIfEmpty_converted_to_left_join(async), "could not be translated");
    }

    public override async Task SelectMany_predicate_after_navigation_with_non_equality_comparison_DefaultIfEmpty_converted_to_left_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_predicate_after_navigation_with_non_equality_comparison_DefaultIfEmpty_converted_to_left_join(async), "could not be translated");
    }

    public override async Task SelectMany_without_result_selector_and_non_equality_comparison_converted_to_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_without_result_selector_and_non_equality_comparison_converted_to_join(async), "could not be translated");
    }

    public override async Task Filtered_collection_projection_with_order_comparison_predicate_converted_to_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_collection_projection_with_order_comparison_predicate_converted_to_join(async), "could not be translated");
    }

    public override async Task Filtered_collection_projection_with_order_comparison_predicate_converted_to_join2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_collection_projection_with_order_comparison_predicate_converted_to_join2(async), "could not be translated");
    }

    public override async Task Filtered_collection_projection_with_order_comparison_predicate_converted_to_join3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Filtered_collection_projection_with_order_comparison_predicate_converted_to_join3(async), "could not be translated");
    }

    public override async Task SelectMany_predicate_with_non_equality_comparison_with_Take_doesnt_convert_to_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_predicate_with_non_equality_comparison_with_Take_doesnt_convert_to_join(async), "could not be translated");
    }

    public override async Task FirstOrDefault_over_int_compared_to_zero(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.FirstOrDefault_over_int_compared_to_zero(async), "Unsupported cross-DbSet query");
    }

    public override async Task Correlated_collection_with_inner_collection_references_element_two_levels_up(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_inner_collection_references_element_two_levels_up(async), "could not be translated");
    }

    public override async Task Accessing_derived_property_using_hard_and_soft_cast(bool async)
    {
        await base.Accessing_derived_property_using_hard_and_soft_cast(async);
    }

    public override async Task Cast_to_derived_followed_by_include_and_FirstOrDefault(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Cast_to_derived_followed_by_include_and_FirstOrDefault(async), "could not be translated");
    }

    public override async Task Cast_to_derived_followed_by_multiple_includes(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Cast_to_derived_followed_by_multiple_includes(async), "could not be translated");
    }

    public override async Task Correlated_collection_take(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_take(async), "could not be translated");
    }

    public override async Task GroupBy_Select_sum(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_Select_sum(async), "could not be translated");
    }

    public override async Task Sum_with_no_data_nullable_double(bool async)
    {
        await base.Sum_with_no_data_nullable_double(async);
    }

    public override async Task FirstOrDefault_on_empty_collection_of_DateTime_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.FirstOrDefault_on_empty_collection_of_DateTime_in_subquery(async), "could not be translated");
    }

    public override async Task Project_shadow_properties(bool async)
    {
        await base.Project_shadow_properties(async);
    }

    public override async Task Composite_key_entity_equal(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Composite_key_entity_equal(async), "could not be translated");
    }

    public override async Task Composite_key_entity_not_equal(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Composite_key_entity_not_equal(async), "could not be translated");
    }

#if EF10
    public override async Task Composite_key_entity_equal_null(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Composite_key_entity_equal_null(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Composite_key_entity_not_equal_null(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Composite_key_entity_not_equal_null(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_with_comparison(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_comparison(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_with_addition(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_addition(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_with_addition_and_final_projection(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_addition_and_final_projection(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_with_conditional(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_conditional(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_with_function_call(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_function_call(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_with_function_call2(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_function_call2(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_into_element_init(bool async)
    {
        // Fails: Property converted to nullable/other type in a projection: no coercion operator defined (int to string) EF-X543
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_into_element_init(async), "No coercion operator");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_into_member_assignment(bool async)
    {
        // Fails: Average/aggregate over a navigation without selector throws 'Operation is not valid due to the current state of the object' EF-X541
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_into_member_assignment(async), "Operation is not valid");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_into_new_array(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_into_new_array(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_into_unary(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_into_unary(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_into_member_access(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_into_member_access(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Projecting_property_converted_to_nullable_and_use_it_in_order_by(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_and_use_it_in_order_by(async), "Expression not supported");
    }
#endif

    public override async Task Correlated_collection_with_distinct_projecting_identifier_column(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_distinct_projecting_identifier_column(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_distinct_projecting_identifier_column_and_correlation_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_distinct_projecting_identifier_column_and_correlation_key(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_distinct_projecting_identifier_column_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_distinct_projecting_identifier_column_composite_key(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_distinct_not_projecting_identifier_column(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_distinct_not_projecting_identifier_column(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_distinct_not_projecting_identifier_column_also_projecting_complex_expressions(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_distinct_not_projecting_identifier_column_also_projecting_complex_expressions(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_groupby_not_projecting_identifier_column_but_only_grouping_key_in_final_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_groupby_not_projecting_identifier_column_but_only_grouping_key_in_final_projection(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_groupby_not_projecting_identifier_column_with_group_aggregate_in_final_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_groupby_not_projecting_identifier_column_with_group_aggregate_in_final_projection(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_groupby_not_projecting_identifier_column_with_group_aggregate_in_final_projection_multiple_grouping_keys(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_groupby_not_projecting_identifier_column_with_group_aggregate_in_final_projection_multiple_grouping_keys(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_groupby_with_complex_grouping_key_not_projecting_identifier_column_with_group_aggregate_in_final_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_groupby_with_complex_grouping_key_not_projecting_identifier_column_with_group_aggregate_in_final_projection(async), "could not be translated");
    }

    public override async Task Correlated_collection_via_SelectMany_with_Distinct_missing_indentifying_columns_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_via_SelectMany_with_Distinct_missing_indentifying_columns_in_projection(async), "could not be translated");
    }

    public override async Task Projecting_correlated_collection_followed_by_Distinct(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_correlated_collection_followed_by_Distinct(async), "could not be translated");
    }

    public override async Task Projecting_some_properties_as_well_as_correlated_collection_followed_by_Distinct(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_some_properties_as_well_as_correlated_collection_followed_by_Distinct(async), "could not be translated");
    }

    public override async Task Projecting_entity_as_well_as_correlated_collection_followed_by_Distinct(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_entity_as_well_as_correlated_collection_followed_by_Distinct(async), "could not be translated");
    }

    public override async Task Projecting_entity_as_well_as_complex_correlated_collection_followed_by_Distinct(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_entity_as_well_as_complex_correlated_collection_followed_by_Distinct(async), "could not be translated");
    }

    public override async Task Projecting_entity_as_well_as_correlated_collection_of_scalars_followed_by_Distinct(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_entity_as_well_as_correlated_collection_of_scalars_followed_by_Distinct(async), "could not be translated");
    }

    public override async Task Correlated_collection_with_distinct_3_levels(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_with_distinct_3_levels(async), "could not be translated");
    }

    public override async Task Correlated_collection_after_distinct_3_levels(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_after_distinct_3_levels(async), "could not be translated");
    }

    public override async Task Correlated_collection_after_distinct_3_levels_without_original_identifiers(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_after_distinct_3_levels_without_original_identifiers(async), "could not be translated");
    }

    public override async Task Basic_query_gears(bool async)
    {
        await base.Basic_query_gears(async);
    }

    public override async Task Include_on_entity_that_is_not_present_in_final_projection_but_uses_TypeIs_instead(bool async)
    {
        await base.Include_on_entity_that_is_not_present_in_final_projection_but_uses_TypeIs_instead(async);
    }

    public override async Task Comparison_with_value_converted_subclass(bool async)
    {
        await base.Comparison_with_value_converted_subclass(async);
    }

#if EF9 || EF10
    public override async Task Project_equality_with_value_converted_property(bool async)
    {
        await base.Project_equality_with_value_converted_property(async);
    }
#endif

    public override async Task Contains_on_readonly_enumerable(bool async)
    {
        await base.Contains_on_readonly_enumerable(async);
    }

#if EF10
    public override async Task Project_navigation_defined_on_base_from_entity_with_inheritance_using_soft_cast(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Project_navigation_defined_on_base_from_entity_with_inheritance_using_soft_cast(async), "is missing but required");
    }
#endif

#if EF10
    public override async Task Project_navigation_defined_on_derived_from_entity_with_inheritance_using_soft_cast(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Project_navigation_defined_on_derived_from_entity_with_inheritance_using_soft_cast(async), "is missing but required");
    }
#endif

    public override async Task Join_entity_with_itself_grouped_by_key_followed_by_include_skip_take(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_entity_with_itself_grouped_by_key_followed_by_include_skip_take(async), "could not be translated");
    }

    public override async Task Where_equals_method_on_nullable_with_object_overload(bool async)
    {
        await base.Where_equals_method_on_nullable_with_object_overload(async);
    }

    public override async Task Where_bool_column_and_Contains(bool async)
    {
        await base.Where_bool_column_and_Contains(async);
    }

    public override async Task Where_bool_column_or_Contains(bool async)
    {
        await base.Where_bool_column_or_Contains(async);
    }

    public override async Task Enum_matching_take_value_gets_different_type_mapping(bool async)
    {
        await base.Enum_matching_take_value_gets_different_type_mapping(async);
    }

    public override async Task Where_subquery_equality_to_null_with_composite_key(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_equality_to_null_with_composite_key(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_equality_to_null_with_composite_key_should_match_nulls(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_equality_to_null_with_composite_key_should_match_nulls(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_equality_to_null_without_composite_key(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_equality_to_null_without_composite_key(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_equality_to_null_without_composite_key_should_match_null(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_equality_to_null_without_composite_key_should_match_null(async), "Unsupported cross-DbSet query");
    }

    public override async Task ElementAt_basic_with_OrderBy(bool async)
    {
        // Fails: ElementAt/indexer call is rebuilt with the wrong number of arguments EF-X520
        await SpecFails.ThrowsAsync(() => base.ElementAt_basic_with_OrderBy(async), "Incorrect number of arguments");
    }

    public override async Task ElementAtOrDefault_basic_with_OrderBy(bool async)
    {
        // Fails: ElementAt/indexer call is rebuilt with the wrong number of arguments EF-X520
        await SpecFails.ThrowsAsync(() => base.ElementAtOrDefault_basic_with_OrderBy(async), "Incorrect number of arguments");
    }

    public override async Task ElementAtOrDefault_basic_with_OrderBy_parameter(bool async)
    {
        // Fails: ElementAt/indexer call is rebuilt with the wrong number of arguments EF-X520
        await SpecFails.ThrowsAsync(() => base.ElementAtOrDefault_basic_with_OrderBy_parameter(async), "Incorrect number of arguments");
    }

    public override async Task Where_subquery_with_ElementAtOrDefault_equality_to_null_with_composite_key(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_with_ElementAtOrDefault_equality_to_null_with_composite_key(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_subquery_with_ElementAt_using_column_as_index(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_subquery_with_ElementAt_using_column_as_index(async), "Unsupported cross-DbSet query");
    }

    public override async Task Using_indexer_on_byte_array_and_string_in_projection(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Using_indexer_on_byte_array_and_string_in_projection(async), "Expression not supported");
    }

    public override async Task Set_operator_with_navigation_in_projection_groupby_aggregate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Set_operator_with_navigation_in_projection_groupby_aggregate(async), "could not be translated");
    }

    public override async Task Nav_expansion_inside_Contains_argument(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Nav_expansion_inside_Contains_argument(async), "Unsupported cross-DbSet query");
    }

    public override async Task Nav_expansion_with_member_pushdown_inside_Contains_argument(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Nav_expansion_with_member_pushdown_inside_Contains_argument(async), "Unsupported cross-DbSet query");
    }

    public override async Task Subquery_inside_Take_argument(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_inside_Take_argument(async), "could not be translated");
    }

    public override async Task Nav_expansion_inside_Skip_correlated_to_source(bool async)
    {
        await base.Nav_expansion_inside_Skip_correlated_to_source(async);
    }

    public override async Task Nav_expansion_inside_Take_correlated_to_source(bool async)
    {
        await base.Nav_expansion_inside_Take_correlated_to_source(async);
    }

    public override async Task Nav_expansion_with_member_pushdown_inside_Take_correlated_to_source(bool async)
    {
        await base.Nav_expansion_with_member_pushdown_inside_Take_correlated_to_source(async);
    }

    public override async Task Nav_expansion_inside_ElementAt_correlated_to_source(bool async)
    {
        await base.Nav_expansion_inside_ElementAt_correlated_to_source(async);
    }

#if EF9 || EF10
    public override async Task Include_one_to_many_on_composite_key_then_orderby_key_properties(bool async)
    {
        await base.Include_one_to_many_on_composite_key_then_orderby_key_properties(async);
    }
#endif

#if EF10
    public override async Task Find_underlying_property_after_GroupJoin_DefaultIfEmpty(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Find_underlying_property_after_GroupJoin_DefaultIfEmpty(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Join_include_coalesce_simple(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Join_include_coalesce_simple(async), "Failure");
    }
#endif

#if EF10
    public override async Task Join_include_coalesce_nested(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Join_include_coalesce_nested(async), "Failure");
    }
#endif

#if EF10
    public override async Task Join_include_conditional(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Join_include_conditional(async), "Failure");
    }
#endif

#if EF10
    public override async Task Derived_reference_is_skipped_when_base_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Derived_reference_is_skipped_when_base_type(async), "Failure");
    }
#endif

#if EF9 || EF10
    public override async Task Nested_contains_with_enum(bool async)
    {
        // Fails: Join key selector that is a sub-query: serializer cannot be created for MongoQuery EF-X534
        await SpecFails.ThrowsAsync(() => base.Nested_contains_with_enum(async), "BsonSerializationException");
    }
#endif

#if EF9 || EF10
    public override async Task Non_string_concat_uses_appropriate_type_mapping(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Non_string_concat_uses_appropriate_type_mapping(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Include_multiple_one_to_one_and_one_to_many(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_multiple_one_to_one_and_one_to_many(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task ToString_guid_property_projection(bool async)
    {
        await base.ToString_guid_property_projection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Include_multiple_one_to_one_optional_and_one_to_one_required(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_multiple_one_to_one_optional_and_one_to_one_required(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_Where_Navigation_Included(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Included(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_where_list_contains_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_where_list_contains_navigation(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_where_list_contains_navigation2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_where_list_contains_navigation2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_accessed_twice_outside_and_inside_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_accessed_twice_outside_and_inside_subquery(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_with_nested_navigation_in_order_by(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_with_nested_navigation_in_order_by(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Where_enum(bool async)
    {
        await base.Where_enum(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_nullable_enum_with_constant(bool async)
    {
        await base.Where_nullable_enum_with_constant(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_nullable_enum_with_null_constant(bool async)
    {
        await base.Where_nullable_enum_with_null_constant(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_nullable_enum_with_non_nullable_parameter(bool async)
    {
        await base.Where_nullable_enum_with_non_nullable_parameter(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_nullable_enum_with_nullable_parameter(bool async)
    {
        await base.Where_nullable_enum_with_nullable_parameter(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_bitwise_and_enum(bool async)
    {
        await base.Where_bitwise_and_enum(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_bitwise_and_integral(bool async)
    {
        // Fails: Enum flags bitwise/closure typed as a different type: ConvertIntegralTypeToEnumSerializer lacks IHasRepresentationSerializer EF-X542
        await SpecFails.ThrowsAsync(() => base.Where_bitwise_and_integral(async), "IHasRepresentationSerializer");
    }
#endif

#if EF8 || EF9
    public override async Task Where_bitwise_and_nullable_enum_with_constant(bool async)
    {
        await base.Where_bitwise_and_nullable_enum_with_constant(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_bitwise_and_nullable_enum_with_null_constant(bool async)
    {
        await base.Where_bitwise_and_nullable_enum_with_null_constant(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_bitwise_and_nullable_enum_with_non_nullable_parameter(bool async)
    {
        await base.Where_bitwise_and_nullable_enum_with_non_nullable_parameter(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_bitwise_and_nullable_enum_with_nullable_parameter(bool async)
    {
        await base.Where_bitwise_and_nullable_enum_with_nullable_parameter(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_bitwise_or_enum(bool async)
    {
        await base.Where_bitwise_or_enum(async);
    }
#endif

#if EF8 || EF9
    public override async Task Bitwise_projects_values_in_select(bool async)
    {
        await base.Bitwise_projects_values_in_select(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_enum_has_flag(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_enum_has_flag(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_enum_has_flag_subquery(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_enum_has_flag_subquery(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_enum_has_flag_subquery_with_pushdown(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_enum_has_flag_subquery_with_pushdown(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_enum_has_flag_subquery_client_eval(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_enum_has_flag_subquery_client_eval(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_enum_has_flag_with_non_nullable_parameter(bool async)
    {
        await base.Where_enum_has_flag_with_non_nullable_parameter(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_has_flag_with_nullable_parameter(bool async)
    {
        await base.Where_has_flag_with_nullable_parameter(async);
    }
#endif

#if EF8 || EF9
    public override async Task Select_enum_has_flag(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_enum_has_flag(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Select_null_propagation_negative3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_negative3(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_null_propagation_negative4(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_negative4(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_null_propagation_negative5(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_negative5(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_null_propagation_negative8(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_negative8(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_null_propagation_works_for_navigations_with_composite_keys(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_works_for_navigations_with_composite_keys(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_null_propagation_works_for_multiple_navigations_with_composite_keys(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_null_propagation_works_for_multiple_navigations_with_composite_keys(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_Where_Navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_Navigation_Null_Coalesce_To_Clr_Type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_Navigation_Null_Coalesce_To_Clr_Type(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_Where_Navigation_Client(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Client(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_Where_Navigation_Null(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Null(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_Where_Navigation_Null_Reverse(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_Where_Navigation_Null_Reverse(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Singleton_Navigation_With_Member_Access(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Singleton_Navigation_With_Member_Access(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_Singleton_Navigation_With_Member_Access(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_Singleton_Navigation_With_Member_Access(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_translated_to_subquery_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_navigation_translated_to_subquery_composite_key(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Join_with_order_by_on_inner_sequence_navigation_translated_to_subquery_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_with_order_by_on_inner_sequence_navigation_translated_to_subquery_composite_key(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Collection_with_inheritance_and_join_include_joined(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_with_inheritance_and_join_include_joined(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Collection_with_inheritance_and_join_include_source(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_with_inheritance_and_join_include_source(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override void Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result1()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        SpecFails.Throws(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result1(), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override void Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result2()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        SpecFails.Throws(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result2(), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result3(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result4(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result4(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_inheritance_and_coalesce_result(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_inheritance_and_coalesce_result(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_conditional_result(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_conditional_result(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_complex_projection_result(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_complex_projection_result(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Coalesce_operator_in_predicate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Coalesce_operator_in_predicate(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Coalesce_operator_in_predicate_with_other_conditions(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Coalesce_operator_in_predicate_with_other_conditions(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Coalesce_operator_in_projection_with_other_conditions(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Coalesce_operator_in_projection_with_other_conditions(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_predicate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_predicate(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_predicate2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_predicate2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_predicate_negated(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_predicate_negated(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_predicate_negated_complex1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_predicate_negated_complex1(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_predicate_negated_complex2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_predicate_negated_complex2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_conditional_expression(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_conditional_expression(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_binary_expression(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_binary_expression(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_binary_and_expression(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_binary_and_expression(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_projection(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_projection_into_anonymous_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_projection_into_anonymous_type(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_DTOs(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_DTOs(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_list_initializers(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_list_initializers(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_array_initializers(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_array_initializers(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_orderby(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_orderby(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_all(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_all(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_negated_predicate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_negated_predicate(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_type_compensation_works_with_contains(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_type_compensation_works_with_contains(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Left_join_predicate_value_equals_condition(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Left_join_predicate_value_equals_condition(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Left_join_predicate_value(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Left_join_predicate_value(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Left_join_predicate_condition_equals_condition(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Left_join_predicate_condition_equals_condition(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_now(bool async)
    {
        await base.Where_datetimeoffset_now(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_utcnow(bool async)
    {
        await base.Where_datetimeoffset_utcnow(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_date_component(bool async)
    {
        await base.Where_datetimeoffset_date_component(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_year_component(bool async)
    {
        await base.Where_datetimeoffset_year_component(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_month_component(bool async)
    {
        await base.Where_datetimeoffset_month_component(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_dayofyear_component(bool async)
    {
        await base.Where_datetimeoffset_dayofyear_component(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_day_component(bool async)
    {
        await base.Where_datetimeoffset_day_component(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_hour_component(bool async)
    {
        await base.Where_datetimeoffset_hour_component(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_minute_component(bool async)
    {
        await base.Where_datetimeoffset_minute_component(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_second_component(bool async)
    {
        await base.Where_datetimeoffset_second_component(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_millisecond_component(bool async)
    {
        await base.Where_datetimeoffset_millisecond_component(async);
    }
#endif

#if EF8 || EF9
    public override async Task DateTimeOffset_DateAdd_AddYears(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.DateTimeOffset_DateAdd_AddYears(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task DateTimeOffset_DateAdd_AddMonths(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.DateTimeOffset_DateAdd_AddMonths(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task DateTimeOffset_DateAdd_AddDays(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.DateTimeOffset_DateAdd_AddDays(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task DateTimeOffset_DateAdd_AddHours(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.DateTimeOffset_DateAdd_AddHours(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task DateTimeOffset_DateAdd_AddMinutes(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.DateTimeOffset_DateAdd_AddMinutes(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task DateTimeOffset_DateAdd_AddSeconds(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.DateTimeOffset_DateAdd_AddSeconds(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task DateTimeOffset_DateAdd_AddMilliseconds(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.DateTimeOffset_DateAdd_AddMilliseconds(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_datetimeoffset_milliseconds_parameter_and_constant(bool async)
    {
        await base.Where_datetimeoffset_milliseconds_parameter_and_constant(async);
    }
#endif

#if EF8 || EF9
    public override async Task Complex_predicate_with_AndAlso_and_nullable_bool_property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_predicate_with_AndAlso_and_nullable_bool_property(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Distinct_with_optional_navigation_is_translated_to_sql(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Distinct_with_optional_navigation_is_translated_to_sql(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Sum_with_optional_navigation_is_translated_to_sql(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Sum_with_optional_navigation_is_translated_to_sql(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Count_with_optional_navigation_is_translated_to_sql(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Count_with_optional_navigation_is_translated_to_sql(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task FirstOrDefault_with_manually_created_groupjoin_is_translated_to_sql(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.FirstOrDefault_with_manually_created_groupjoin_is_translated_to_sql(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task All_with_optional_navigation_is_translated_to_sql(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.All_with_optional_navigation_is_translated_to_sql(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Client_side_equality_with_parameter_works_with_optional_navigations(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Client_side_equality_with_parameter_works_with_optional_navigations(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Where_is_properly_lifted_from_subquery_created_by_include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_is_properly_lifted_from_subquery_created_by_include(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Subquery_containing_left_join_projecting_main_from_clause_gets_lifted(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_containing_left_join_projecting_main_from_clause_gets_lifted(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_access_on_derived_entity_using_cast(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_access_on_derived_entity_using_cast(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_access_on_derived_materialized_entity_using_cast(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_access_on_derived_materialized_entity_using_cast(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_access_via_EFProperty_on_derived_entity_using_cast(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_access_via_EFProperty_on_derived_entity_using_cast(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_access_fk_on_derived_entity_using_cast(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_access_fk_on_derived_entity_using_cast(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_on_derived_entity_using_OfType(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_on_derived_entity_using_OfType(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_on_derived_entity_with_cast(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_on_derived_entity_with_cast(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Contains_on_nullable_array_produces_correct_sql(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Contains_on_nullable_array_produces_correct_sql(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_with_collection_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_with_collection_composite_key(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_on_derived_type_using_string(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_string(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_on_derived_type_using_EF_Property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_EF_Property(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_on_derived_type_using_string_nested1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_string_nested1(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_on_derived_type_using_string_nested2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_string_nested2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_on_derived_type_using_lambda(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_lambda(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_on_derived_type_using_lambda_with_soft_cast(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_lambda_with_soft_cast(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_on_derived_type_using_lambda_with_tracking(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_on_derived_type_using_lambda_with_tracking(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_base_navigation_on_derived_entity(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_base_navigation_on_derived_entity(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task ThenInclude_collection_on_derived_after_base_reference(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.ThenInclude_collection_on_derived_after_base_reference(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task ThenInclude_collection_on_derived_after_derived_reference(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.ThenInclude_collection_on_derived_after_derived_reference(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_derived_included_on_one_method(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_derived_included_on_one_method(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_nullable_bool_in_conditional_works(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_nullable_bool_in_conditional_works(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Null_semantics_on_nullable_bool_from_left_join_subquery_is_fully_applied(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Null_semantics_on_nullable_bool_from_left_join_subquery_is_fully_applied(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_on_derived_type_with_order_by_and_paging(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_on_derived_type_with_order_by_and_paging(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Select_required_navigation_on_derived_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_required_navigation_on_derived_type(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Where_required_navigation_on_derived_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_required_navigation_on_derived_type(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Negated_bool_ternary_inside_anonymous_type_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Negated_bool_ternary_inside_anonymous_type_in_projection(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Order_by_entity_qsre(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Order_by_entity_qsre(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Order_by_entity_qsre_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Order_by_entity_qsre_composite_key(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Order_by_entity_qsre_with_other_orderbys(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Order_by_entity_qsre_with_other_orderbys(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Join_on_entity_qsre_keys_inner_key_is_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_on_entity_qsre_keys_inner_key_is_navigation(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Join_on_entity_qsre_keys_inner_key_is_navigation_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_on_entity_qsre_keys_inner_key_is_navigation_composite_key(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Join_on_entity_qsre_keys_inner_key_is_nested_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_on_entity_qsre_keys_inner_key_is_nested_navigation(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_entity_qsre_keys_inner_key_is_nested_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupJoin_on_entity_qsre_keys_inner_key_is_nested_navigation(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Double_order_by_on_nullable_bool_coming_from_optional_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Double_order_by_on_nullable_bool_coming_from_optional_navigation(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Double_order_by_on_Like(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Double_order_by_on_Like(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Double_order_by_on_is_null(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Double_order_by_on_is_null(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task String_compare_with_null_conditional_argument(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_compare_with_null_conditional_argument(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task String_compare_with_null_conditional_argument2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_compare_with_null_conditional_argument2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task String_concat_with_null_conditional_argument(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_concat_with_null_conditional_argument(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task String_concat_with_null_conditional_argument2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_concat_with_null_conditional_argument2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Time_of_day_datetimeoffset(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Time_of_day_datetimeoffset(async), "Failure");
    }
#endif

#if EF8 || EF9
    public override async Task Include_with_client_method_and_member_access_still_applies_includes(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_with_client_method_and_member_access_still_applies_includes(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation_complex(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation_complex(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Filter_with_new_Guid(bool async)
    {
        await base.Filter_with_new_Guid(async);
    }
#endif

#if EF8 || EF9
    public override async Task OfTypeNav1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.OfTypeNav1(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task OfTypeNav2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.OfTypeNav2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task OfTypeNav3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.OfTypeNav3(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Nav_rewrite_Distinct_with_convert()
    {
        await base.Nav_rewrite_Distinct_with_convert();
    }
#endif

#if EF8 || EF9
    public override async Task Nav_rewrite_Distinct_with_convert_anonymous()
    {
        await base.Nav_rewrite_Distinct_with_convert_anonymous();
    }
#endif

#if EF8 || EF9
    public override async Task Nav_rewrite_with_convert1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Nav_rewrite_with_convert1(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Nav_rewrite_with_convert2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Nav_rewrite_with_convert2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Nav_rewrite_with_convert3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Nav_rewrite_with_convert3(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_inside_interpolated_string_expanded(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_inside_interpolated_string_expanded(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Left_join_projection_using_coalesce_tracking(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Left_join_projection_using_coalesce_tracking(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Left_join_projection_using_conditional_tracking(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Left_join_projection_using_conditional_tracking(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_based_on_complex_expression1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression1(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_based_on_complex_expression2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_based_on_complex_expression3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression3(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Navigation_based_on_complex_expression5(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression5(async));
    }
#endif

#if EF9
    public override async Task Navigation_based_on_complex_expression6(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression6(async));
    }
#endif

#if EF8 || EF9
    public override async Task Reference_include_chain_loads_correctly_when_middle_is_null(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Reference_include_chain_loads_correctly_when_middle_is_null(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include_after_Select_throws(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_after_Select_throws(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Complex_GroupBy_after_set_operator_using_result_selector(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_GroupBy_after_set_operator_using_result_selector(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task FirstOrDefault_navigation_access_entity_equality_in_where_predicate_apply_peneding_selector(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.FirstOrDefault_navigation_access_entity_equality_in_where_predicate_apply_peneding_selector(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Byte_array_contains_literal(bool async)
    {
        // Fails: byte[] element access / element collection: ByteArraySerializer is not an IBsonArraySerializer EF-X510
        await SpecFails.ThrowsAsync(() => base.Byte_array_contains_literal(async), "must implement IBsonArraySerializer");
    }
#endif

#if EF8 || EF9
    public override async Task Byte_array_contains_parameter(bool async)
    {
        // Fails: byte[] element access / element collection: ByteArraySerializer is not an IBsonArraySerializer EF-X510
        await SpecFails.ThrowsAsync(() => base.Byte_array_contains_parameter(async), "must implement IBsonArraySerializer");
    }
#endif

#if EF8 || EF9
    public override async Task Byte_array_filter_by_length_literal(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Byte_array_filter_by_length_literal(async), "Failure");
    }
#endif

#if EF8 || EF9
    public override async Task Byte_array_filter_by_length_parameter(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Byte_array_filter_by_length_parameter(async), "Failure");
    }
#endif

#if EF8 || EF9
    public override async Task OrderBy_bool_coming_from_optional_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.OrderBy_bool_coming_from_optional_navigation(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override void Byte_array_filter_by_length_parameter_compiled()
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        SpecFails.Throws(() => base.Byte_array_filter_by_length_parameter_compiled(), "Failure");
    }
#endif

#if EF8 || EF9
    public override async Task Byte_array_filter_by_SequenceEqual(bool async)
    {
        // Fails: Server error executing the generated pipeline EF-X528
        await SpecFails.ThrowsAsync(() => base.Byte_array_filter_by_SequenceEqual(async), "MongoCommandException");
    }
#endif

#if EF8 || EF9
    public override async Task TimeSpan_Hours(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.TimeSpan_Hours(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task TimeSpan_Minutes(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.TimeSpan_Minutes(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task TimeSpan_Seconds(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.TimeSpan_Seconds(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task TimeSpan_Milliseconds(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.TimeSpan_Milliseconds(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeSpan_Hours(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeSpan_Hours(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeSpan_Minutes(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeSpan_Minutes(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeSpan_Seconds(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeSpan_Seconds(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeSpan_Milliseconds(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeSpan_Milliseconds(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Contains_on_byte_array_property_using_byte_column(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Contains_on_byte_array_property_using_byte_column(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Enum_array_contains(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Enum_array_contains(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Trying_to_access_unmapped_property_inside_join_key_selector(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Trying_to_access_unmapped_property_inside_join_key_selector(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task First_on_byte_array(bool async)
    {
        // Fails: byte[] element access / element collection: ByteArraySerializer is not an IBsonArraySerializer EF-X510
        await SpecFails.ThrowsAsync(() => base.First_on_byte_array(async), "must implement IBsonArraySerializer");
    }
#endif

#if EF8 || EF9
    public override async Task Array_access_on_byte_array(bool async)
    {
        // Fails: byte[] element access / element collection: ByteArraySerializer is not an IBsonArraySerializer EF-X510
        await SpecFails.ThrowsAsync(() => base.Array_access_on_byte_array(async), "must implement IBsonArraySerializer");
    }
#endif

#if EF8 || EF9
    public override async Task Composite_key_entity_equal_null(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Composite_key_entity_equal_null(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Composite_key_entity_not_equal_null(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Composite_key_entity_not_equal_null(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_with_comparison(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_comparison(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_with_addition(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_addition(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_with_addition_and_final_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_addition_and_final_projection(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_with_conditional(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_conditional(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_with_function_call(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_function_call(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_with_function_call2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_with_function_call2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_into_element_init(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_into_element_init(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_into_member_assignment(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_into_member_assignment(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_into_new_array(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_into_new_array(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_into_unary(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_into_unary(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_into_member_access(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_into_member_access(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projecting_property_converted_to_nullable_and_use_it_in_order_by(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_converted_to_nullable_and_use_it_in_order_by(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Where_DateOnly_Year(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_DateOnly_Year(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_DateOnly_Month(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_DateOnly_Month(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_DateOnly_Day(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_DateOnly_Day(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_DateOnly_DayOfYear(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_DateOnly_DayOfYear(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_DateOnly_DayOfWeek(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_DateOnly_DayOfWeek(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_DateOnly_AddYears(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_DateOnly_AddYears(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_DateOnly_AddMonths(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_DateOnly_AddMonths(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_DateOnly_AddDays(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_DateOnly_AddDays(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeOnly_Hour(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_Hour(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeOnly_Minute(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_Minute(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeOnly_Second(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_Second(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeOnly_Millisecond(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_Millisecond(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeOnly_AddHours(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_AddHours(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeOnly_AddMinutes(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_AddMinutes(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeOnly_Add_TimeSpan(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_Add_TimeSpan(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeOnly_IsBetween(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_IsBetween(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Where_TimeOnly_subtract_TimeOnly(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_subtract_TimeOnly(async), "Expression not supported");
    }
#endif

#if EF9
    public override async Task Where_TimeOnly_FromDateTime_compared_to_property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_FromDateTime_compared_to_property(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Where_TimeOnly_FromDateTime_compared_to_parameter(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_FromDateTime_compared_to_parameter(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Where_TimeOnly_FromDateTime_compared_to_constant(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_FromDateTime_compared_to_constant(async), "Expression not supported");
    }
#endif

#if EF9
    public override async Task Where_TimeOnly_FromTimeSpan_compared_to_property(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_FromTimeSpan_compared_to_property(async), "Expression not supported");
    }
#endif

#if EF9
    public override async Task Where_TimeOnly_FromTimeSpan_compared_to_parameter(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_TimeOnly_FromTimeSpan_compared_to_parameter(async), "Expression not supported");
    }
#endif

#if EF9
    public override async Task Order_by_TimeOnly_FromTimeSpan(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Order_by_TimeOnly_FromTimeSpan(async), "Expression not supported");
    }
#endif

#if EF9
    public override async Task Where_DateOnly_FromDateTime_compared_to_property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_DateOnly_FromDateTime_compared_to_property(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Where_DateOnly_FromDateTime_compared_to_constant_and_parameter(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_DateOnly_FromDateTime_compared_to_constant_and_parameter(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Project_navigation_defined_on_base_from_entity_with_inheritance_using_soft_cast(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_navigation_defined_on_base_from_entity_with_inheritance_using_soft_cast(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Project_navigation_defined_on_derived_from_entity_with_inheritance_using_soft_cast(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Project_navigation_defined_on_derived_from_entity_with_inheritance_using_soft_cast(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task DateTimeOffset_to_unix_time_milliseconds(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.DateTimeOffset_to_unix_time_milliseconds(async), "ArgumentException");
    }
#endif

#if EF8 || EF9
    public override async Task DateTimeOffset_to_unix_time_seconds(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.DateTimeOffset_to_unix_time_seconds(async), "ArgumentException");
    }
#endif

#if EF9
    public override async Task Find_underlying_property_after_GroupJoin_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Find_underlying_property_after_GroupJoin_DefaultIfEmpty(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Join_include_coalesce_simple(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_include_coalesce_simple(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Join_include_coalesce_nested(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_include_coalesce_nested(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Join_include_conditional(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_include_conditional(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Derived_reference_is_skipped_when_base_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Derived_reference_is_skipped_when_base_type(async), "could not be translated");
    }
#endif

#if EF8
    public override async Task Coalesce_operator_in_predicate(bool async)
    {
        await base.Coalesce_operator_in_predicate(async);
    }
#endif

#if EF8
    public override async Task Coalesce_operator_in_predicate_with_other_conditions(bool async)
    {
        await base.Coalesce_operator_in_predicate_with_other_conditions(async);
    }
#endif

#if EF8
    public override async Task Coalesce_operator_in_projection_with_other_conditions(bool async)
    {
        await base.Coalesce_operator_in_projection_with_other_conditions(async);
    }
#endif

#if EF8
    public override async Task Enum_ToString_is_client_eval(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Enum_ToString_is_client_eval(async), "Failure");
    }
#endif

#if EF8
    public override async Task Navigation_based_on_complex_expression4(bool async)
    {
        // Fails: Result mismatch whose assertion failure surfaces as TypeLoadException (xunit.assert version skew), hiding the wrong-result cause EF-X540
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression4(async), "TypeLoadException");
    }
#endif

#if EF8
    public override async Task Navigation_based_on_complex_expression5(bool async)
    {
        // Fails: Result mismatch whose assertion failure surfaces as TypeLoadException (xunit.assert version skew), hiding the wrong-result cause EF-X540
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression5(async), "TypeLoadException");
    }
#endif

#if EF8
    public override async Task Navigation_based_on_complex_expression6(bool async)
    {
        // Fails: Result mismatch whose assertion failure surfaces as TypeLoadException (xunit.assert version skew), hiding the wrong-result cause EF-X540
        await SpecFails.ThrowsAsync(() => base.Navigation_based_on_complex_expression6(async), "TypeLoadException");
    }
#endif

    public class GearsOfWarQueryMongoFixture : GearsOfWarQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("Gears");

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
