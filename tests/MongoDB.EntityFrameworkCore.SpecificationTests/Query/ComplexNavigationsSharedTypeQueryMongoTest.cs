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

public class ComplexNavigationsSharedTypeQueryMongoTest : ComplexNavigationsSharedTypeQueryTestBase<ComplexNavigationsSharedTypeQueryMongoTest.ComplexNavigationsSharedTypeQueryMongoFixture>
{
    public ComplexNavigationsSharedTypeQueryMongoTest(ComplexNavigationsSharedTypeQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if EF11
    // Fails: new in the EF11 upstream package; asserts any failure until triaged. EF-X605
    public override Task Join_on_anonymous_type_with_multiple_properties(bool async)
        => SpecFails.ThrowsAsync(() => base.Join_on_anonymous_type_with_multiple_properties(async));

    public override Task Join_on_anonymous_type_with_single_property(bool async)
        => SpecFails.ThrowsAsync(() => base.Join_on_anonymous_type_with_single_property(async));
#endif

    public override async Task Join_navigation_self_ref(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_navigation_self_ref(async));
    }

#if (EF10 || EF11)
#if !EF11
    public override async Task Join_condition_optimizations_applied_correctly_when_anonymous_type_with_multiple_properties(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Join_condition_optimizations_applied_correctly_when_anonymous_type_with_multiple_properties(async), "Unsupported cross-DbSet query");
    }
#endif
#endif

#if (EF10 || EF11)
#if !EF11
    public override async Task Join_condition_optimizations_applied_correctly_when_anonymous_type_with_single_property(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Join_condition_optimizations_applied_correctly_when_anonymous_type_with_single_property(async), "Unsupported cross-DbSet query");
    }
#endif
#endif

    public override async Task Multiple_SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_joined_together(bool async)
    {
        await base.Multiple_SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_joined_together(async);
    }

    public override async Task SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany(bool async)
    {
        await base.SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany(async);
    }

    public override async Task SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany2(bool async)
    {
        await base.SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany2(async);
    }

    public override async Task SelectMany_with_nested_navigations_and_additional_joins_outside_of_SelectMany(bool async)
    {
        await base.SelectMany_with_nested_navigations_and_additional_joins_outside_of_SelectMany(async);
    }

    public override async Task Include8(bool async)
    {
        await base.Include8(async);
    }

    public override async Task Include9(bool async)
    {
        await base.Include9(async);
    }

    public override async Task Join_with_navigations_in_the_result_selector2(bool async)
    {
        await base.Join_with_navigations_in_the_result_selector2(async);
    }

    public override async Task Member_pushdown_chain_3_levels_deep(bool async)
    {
        await base.Member_pushdown_chain_3_levels_deep(async);
    }

    public override async Task Member_pushdown_chain_3_levels_deep_entity(bool async)
    {
        await base.Member_pushdown_chain_3_levels_deep_entity(async);
    }

    public override async Task Member_pushdown_with_collection_navigation_in_the_middle(bool async)
    {
        await base.Member_pushdown_with_collection_navigation_in_the_middle(async);
    }

    public override async Task Project_shadow_properties1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Project_shadow_properties1(async), "Failure");
    }

    public override async Task Project_shadow_properties2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Project_shadow_properties2(async), "Failure");
    }

    public override async Task Project_shadow_properties3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Project_shadow_properties3(async), "Failure");
    }

    public override async Task Project_shadow_properties4(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Project_shadow_properties4(async), "Failure");
    }

    public override async Task Project_shadow_properties9(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Project_shadow_properties9(async), "Failure");
    }

#if EF9 || (EF10 || EF11)
    public override async Task Null_check_removal_applied_recursively_complex(bool async)
    {
        await base.Null_check_removal_applied_recursively_complex(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Entity_equality_empty(bool async)
    {
        // Fails: Entity equality issue EF-202
        await SpecFails.ThrowsAsync(() => base.Entity_equality_empty(async), "Entity to entity comparison is not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_when_sentinel_ef_property(bool async)
    {
        await base.Key_equality_when_sentinel_ef_property(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_required(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_required(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_required2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_required2(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_nested(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_nested(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_nested2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_nested2(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_and_member_expression1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression1(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_and_member_expression2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression2(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_and_member_expression3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression3(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_navigation_converted_to_FK(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_navigation_converted_to_FK(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_two_conditions_on_same_navigation(bool async)
    {
        // Fails: Entity equality issue EF-202
        await SpecFails.ThrowsAsync(() => base.Key_equality_two_conditions_on_same_navigation(async), "Entity to entity comparison is not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_two_conditions_on_same_navigation2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_two_conditions_on_same_navigation2(async), "Failure");
    }
#endif

    public override async Task Multi_level_include_with_short_circuiting(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multi_level_include_with_short_circuiting(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Join_navigation_key_access_optional(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Join_navigation_key_access_optional(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_key_access_required(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Join_navigation_key_access_required(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigation_key_access_optional_comparison(bool async)
    {
        await base.Navigation_key_access_optional_comparison(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Simple_level1_include(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_include(async), "Failure");
    }
#endif

    public override async Task Simple_level1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1(async), "Failure");
    }

#if (EF10 || EF11)
    public override async Task Simple_level1_level2_include(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_level2_include(async), "Failure");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Simple_level1_level2_GroupBy_Count(bool async)
    {
        await base.Simple_level1_level2_GroupBy_Count(async);
    }
#else
    public override async Task Simple_level1_level2_GroupBy_Count(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Simple_level1_level2_GroupBy_Count(async), "could not be translated");
    }
#endif

    public override async Task Simple_level1_level2_GroupBy_Having_Count(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Simple_level1_level2_GroupBy_Having_Count(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Simple_level1_level2_level3_include(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_level2_level3_include(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigation_key_access_required_comparison(bool async)
    {
        await base.Navigation_key_access_required_comparison(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigation_inside_method_call_translated_to_join(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Navigation_inside_method_call_translated_to_join(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigation_inside_method_call_translated_to_join2(bool async)
    {
        await base.Navigation_inside_method_call_translated_to_join2(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_method_call_translated_to_join(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_method_call_translated_to_join(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_property_method_translated_to_join(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_property_method_translated_to_join(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Method_call_on_optional_navigation_translates_to_null_conditional_properly_for_arguments(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Method_call_on_optional_navigation_translates_to_null_conditional_properly_for_arguments(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_method_call_translated_to_join_keeps_original_nullability(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_method_call_translated_to_join_keeps_original_nullability(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability_also_for_arguments(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability_also_for_arguments(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Join_navigation_in_outer_selector_translated_to_extra_join(async), "ArgumentException");
    }
#endif

    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join_nested(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_navigation_in_outer_selector_translated_to_extra_join_nested(async), "could not be translated");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join_nested2(bool async)
    {
        await base.Join_navigation_in_outer_selector_translated_to_extra_join_nested2(async);
    }
#else
    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join_nested2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_navigation_in_outer_selector_translated_to_extra_join_nested2(async), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_in_inner_selector(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Join_navigation_in_inner_selector(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigations_in_inner_selector_translated_without_collision(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Join_navigations_in_inner_selector_translated_without_collision(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_non_key_join(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Join_navigation_non_key_join(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_with_orderby_on_inner_sequence_navigation_non_key_join(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Join_with_orderby_on_inner_sequence_navigation_non_key_join(async), "ArgumentException");
    }
#endif

    public override async Task Join_navigation_nested(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_navigation_nested(async), "could not be translated");
    }

    public override async Task Join_navigation_nested2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_navigation_nested2(async), "could not be translated");
    }

    public override async Task Join_navigation_deeply_nested_non_key_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_navigation_deeply_nested_non_key_join(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Join_navigation_deeply_nested_required(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Join_navigation_deeply_nested_required(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include_reference_and_project_into_anonymous_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include_reference_and_project_into_anonymous_type(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_nav_prop_reference_optional1(bool async)
    {
        await base.Select_nav_prop_reference_optional1(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_nav_prop_reference_optional1_via_DefaultIfEmpty(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Select_nav_prop_reference_optional1_via_DefaultIfEmpty(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_nav_prop_reference_optional2(bool async)
    {
        await base.Select_nav_prop_reference_optional2(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_nav_prop_reference_optional2_via_DefaultIfEmpty(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Select_nav_prop_reference_optional2_via_DefaultIfEmpty(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_nav_prop_reference_optional3(bool async)
    {
        await base.Select_nav_prop_reference_optional3(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_nav_prop_reference_optional1(bool async)
    {
        await base.Where_nav_prop_reference_optional1(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_nav_prop_reference_optional1_via_DefaultIfEmpty(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Where_nav_prop_reference_optional1_via_DefaultIfEmpty(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_nav_prop_reference_optional2(bool async)
    {
        await base.Where_nav_prop_reference_optional2(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_nav_prop_reference_optional2_via_DefaultIfEmpty(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Where_nav_prop_reference_optional2_via_DefaultIfEmpty(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_multiple_nav_prop_reference_optional(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Select_multiple_nav_prop_reference_optional(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_member_compared_to_value(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Where_multiple_nav_prop_reference_optional_member_compared_to_value(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_member_compared_to_null(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Where_multiple_nav_prop_reference_optional_member_compared_to_null(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null1(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null2(bool async)
    {
        await base.Where_multiple_nav_prop_reference_optional_compared_to_null2(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null3(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null4(bool async)
    {
        await base.Where_multiple_nav_prop_reference_optional_compared_to_null4(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null5(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null5(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_multiple_nav_prop_reference_required(bool async)
    {
        await base.Select_multiple_nav_prop_reference_required(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_multiple_nav_prop_reference_required2(bool async)
    {
        await base.Select_multiple_nav_prop_reference_required2(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_multiple_nav_prop_optional_required(bool async)
    {
        await base.Select_multiple_nav_prop_optional_required(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_optional_required(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Where_multiple_nav_prop_optional_required(async), "Failure");
    }
#endif

    public override async Task SelectMany_navigation_comparison1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_comparison1(async), "could not be translated");
    }

    public override async Task SelectMany_navigation_comparison2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_comparison2(async), "could not be translated");
    }

    public override async Task SelectMany_navigation_comparison3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_comparison3(async), "could not be translated");
    }

    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_complex_predicate_with_with_nav_prop_and_OrElse1(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse2(bool async)
    {
        await base.Where_complex_predicate_with_with_nav_prop_and_OrElse2(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Where_complex_predicate_with_with_nav_prop_and_OrElse3(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse4(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.TranslationFailedAsync(() => base.Where_complex_predicate_with_with_nav_prop_and_OrElse4(async));
    }
#endif

    public override async Task Complex_navigations_with_predicate_projected_into_anonymous_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_navigations_with_predicate_projected_into_anonymous_type(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Complex_navigations_with_predicate_projected_into_anonymous_type2(bool async)
    {
        await base.Complex_navigations_with_predicate_projected_into_anonymous_type2(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_projected_into_DTO(bool async)
    {
        await base.Optional_navigation_projected_into_DTO(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task OrderBy_nav_prop_reference_optional(bool async)
    {
        await base.OrderBy_nav_prop_reference_optional(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task OrderBy_nav_prop_reference_optional_via_DefaultIfEmpty(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.OrderBy_nav_prop_reference_optional_via_DefaultIfEmpty(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Sum(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Sum(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Min(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Result_operator_nav_prop_reference_optional_Min(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Max(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Max(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Average(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Average_with_identity_selector(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average_with_identity_selector(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Average_without_selector(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average_without_selector(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_via_DefaultIfEmpty(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Result_operator_nav_prop_reference_optional_via_DefaultIfEmpty(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include_with_optional_navigation(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include_with_optional_navigation(async), "Failure");
    }
#endif

    public override async Task Join_flattening_bug_4539(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_flattening_bug_4539(async), "could not be translated");
    }

    public override async Task Query_source_materialization_bug_4547(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Query_source_materialization_bug_4547(async), "could not be translated");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task SelectMany_navigation_property(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.SelectMany_navigation_property(async));
    }
#else
    public override async Task SelectMany_navigation_property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task SelectMany_navigation_property_and_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.SelectMany_navigation_property_and_projection(async));
    }
#else
    public override async Task SelectMany_navigation_property_and_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property_and_projection(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task SelectMany_navigation_property_and_filter_before(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.SelectMany_navigation_property_and_filter_before(async));
    }
#else
    public override async Task SelectMany_navigation_property_and_filter_before(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property_and_filter_before(async), "could not be translated");
    }
#endif

    public override async Task SelectMany_navigation_property_and_filter_after(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property_and_filter_after(async), "could not be translated");
    }

    public override async Task SelectMany_nested_navigation_property_required(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_nested_navigation_property_required(async), "could not be translated");
    }

    public override async Task SelectMany_nested_navigation_property_optional_and_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_nested_navigation_property_optional_and_projection(async), "could not be translated");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Multiple_SelectMany_calls(bool async)
    {
        await base.Multiple_SelectMany_calls(async);
    }
#else
    public override async Task Multiple_SelectMany_calls(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_calls(async), "could not be translated");
    }
#endif

    public override async Task SelectMany_navigation_property_with_another_navigation_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property_with_another_navigation_in_subquery(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Where_navigation_property_to_collection(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_navigation_property_to_collection(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_navigation_property_to_collection2(bool async)
    {
        // Fails: Join key selector that is a sub-query: serializer cannot be created for MongoQuery EF-X534
        await SpecFails.ThrowsAsync(() => base.Where_navigation_property_to_collection2(async), "BsonSerializationException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_navigation_property_to_collection_of_original_entity_type(bool async)
    {
        // Fails: Join key selector that is a sub-query: serializer cannot be created for MongoQuery EF-X534
        await SpecFails.ThrowsAsync(() => base.Where_navigation_property_to_collection_of_original_entity_type(async), "BsonSerializationException");
    }
#endif

    public override async Task Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level(async), "Unsupported cross-DbSet query");
    }

#if (EF10 || EF11)
    public override async Task Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level_join(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level_join(async), "Unsupported cross-DbSet query");
    }
#endif

    public override async Task Correlated_nested_subquery_doesnt_project_unnecessary_columns_in_top_level(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Correlated_nested_subquery_doesnt_project_unnecessary_columns_in_top_level(async), "Unsupported cross-DbSet query");
    }

    public override async Task Correlated_nested_two_levels_up_subquery_doesnt_project_unnecessary_columns_in_top_level(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Correlated_nested_two_levels_up_subquery_doesnt_project_unnecessary_columns_in_top_level(async), "Unsupported cross-DbSet query");
    }

    public override async Task SelectMany_where_with_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_where_with_subquery(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access1(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access2(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access3(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Order_by_key_of_navigation_similar_to_projected_gets_optimized_into_FK_access(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Order_by_key_of_navigation_similar_to_projected_gets_optimized_into_FK_access(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(bool async)
    {
        await base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Order_by_key_of_anonymous_type_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(bool async)
    {
        await base.Order_by_key_of_anonymous_type_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_take_optional_navigation(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Optional_navigation_take_optional_navigation(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Projection_select_correct_table_from_subquery_when_materialization_is_not_required(bool async)
    {
        await base.Projection_select_correct_table_from_subquery_when_materialization_is_not_required(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Projection_select_correct_table_with_anonymous_projection_in_subquery(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.TranslationFailedAsync(() => base.Projection_select_correct_table_with_anonymous_projection_in_subquery(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Projection_select_correct_table_in_subquery_when_materialization_is_not_required_in_multiple_joins(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.TranslationFailedAsync(() => base.Projection_select_correct_table_in_subquery_when_materialization_is_not_required_in_multiple_joins(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_predicate_on_optional_reference_navigation(bool async)
    {
        await base.Where_predicate_on_optional_reference_navigation(async);
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task SelectMany_with_string_based_Include1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.SelectMany_with_string_based_Include1(async));
    }
#else
    public override async Task SelectMany_with_string_based_Include1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_string_based_Include1(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task SelectMany_with_EF_Property_Include1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.SelectMany_with_EF_Property_Include1(async));
    }
#else
    public override async Task SelectMany_with_EF_Property_Include1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_EF_Property_Include1(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task SelectMany_with_string_based_Include2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.SelectMany_with_string_based_Include2(async));
    }
#else
    public override async Task SelectMany_with_string_based_Include2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_string_based_Include2(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Multiple_SelectMany_with_string_based_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Multiple_SelectMany_with_string_based_Include(async));
    }
#else
    public override async Task Multiple_SelectMany_with_string_based_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_with_string_based_Include(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Multiple_SelectMany_with_EF_Property_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Multiple_SelectMany_with_EF_Property_Include(async));
    }
#else
    public override async Task Multiple_SelectMany_with_EF_Property_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_with_EF_Property_Include(async), "could not be translated");
    }
#endif

    public override async Task Multiple_required_navigations_with_Include(bool async)
    {
        await base.Multiple_required_navigations_with_Include(async);
    }

    public override async Task Multiple_required_navigation_using_multiple_selects_with_Include(bool async)
    {
        await base.Multiple_required_navigation_using_multiple_selects_with_Include(async);
    }

    public override async Task Multiple_required_navigation_with_string_based_Include(bool async)
    {
        await base.Multiple_required_navigation_with_string_based_Include(async);
    }

    public override async Task Multiple_required_navigation_with_EF_Property_Include(bool async)
    {
        await base.Multiple_required_navigation_with_EF_Property_Include(async);
    }

    public override async Task Multiple_required_navigation_using_multiple_selects_with_string_based_Include(bool async)
    {
        await base.Multiple_required_navigation_using_multiple_selects_with_string_based_Include(async);
    }

    public override async Task Multiple_required_navigation_using_multiple_selects_with_EF_Property_Include(bool async)
    {
        await base.Multiple_required_navigation_using_multiple_selects_with_EF_Property_Include(async);
    }

    public override async Task Optional_navigation_with_Include(bool async)
    {
        await base.Optional_navigation_with_Include(async);
    }

    public override async Task SelectMany_with_navigation_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_navigation_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_navigation_filter_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_navigation_filter_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigation_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigation_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigation_filter_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigation_filter_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_required_navigation_filter_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_required_navigation_filter_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany3(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany4(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany4(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_followed_by_Select_required_navigation_using_same_navs(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_followed_by_Select_required_navigation_using_same_navs(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_followed_by_Select_required_navigation_using_different_navs(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_followed_by_Select_required_navigation_using_different_navs(async), "could not be translated");
    }

    public override async Task Multiple_SelectMany_with_navigation_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_with_navigation_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_navigation_filter_paging_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_navigation_filter_paging_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Select_join_subquery_containing_filter_and_distinct(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.Select_join_subquery_containing_filter_and_distinct(async), "ArgumentNullException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_join_with_key_selector_being_a_subquery(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.Select_join_with_key_selector_being_a_subquery(async), "ArgumentNullException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Contains_with_subquery_optional_navigation_and_constant_item(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Contains_with_subquery_optional_navigation_and_constant_item(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Contains_with_subquery_optional_navigation_scalar_distinct_and_constant_item(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Contains_with_subquery_optional_navigation_scalar_distinct_and_constant_item(async), "Unsupported cross-DbSet query");
    }
#endif

    public override async Task Complex_query_with_optional_navigations_and_client_side_evaluation(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Complex_query_with_optional_navigations_and_client_side_evaluation(async), "Unsupported cross-DbSet query");
    }

    public override async Task Required_navigation_on_a_subquery_with_First_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Required_navigation_on_a_subquery_with_First_in_projection(async), "could not be translated");
    }

    public override async Task Required_navigation_on_a_subquery_with_complex_projection_and_First(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Required_navigation_on_a_subquery_with_complex_projection_and_First(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Required_navigation_on_a_subquery_with_First_in_predicate(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Required_navigation_on_a_subquery_with_First_in_predicate(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Manually_created_left_join_propagates_nullability_to_navigations(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Manually_created_left_join_propagates_nullability_to_navigations(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_propagates_nullability_to_manually_created_left_join1(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_propagates_nullability_to_manually_created_left_join1(async), "ArgumentException");
    }
#endif

    public override async Task Optional_navigation_propagates_nullability_to_manually_created_left_join2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_propagates_nullability_to_manually_created_left_join2(async), "could not be translated");
    }

    public override async Task Null_reference_protection_complex(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Null_reference_protection_complex(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Null_reference_protection_complex_materialization(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.Null_reference_protection_complex_materialization(async), "ArgumentNullException");
    }
#endif

    public override async Task Null_reference_protection_complex_client_eval(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Null_reference_protection_complex_client_eval(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened2(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened2(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened3(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened3(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer_with_client_method(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.ThrowsAsync(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer_with_client_method(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_inner(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_inner(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_with_orderby_on_inner_sequence_projecting_inner(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_with_orderby_on_inner_sequence_projecting_inner(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_left_side_being_a_subquery(bool async)
    {
        await base.GroupJoin_on_left_side_being_a_subquery(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_right_side_being_a_subquery(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_on_right_side_being_a_subquery(async), "ArgumentException");
    }
#endif

    public override async Task GroupJoin_in_subquery_with_client_result_operator(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_in_subquery_with_client_result_operator(async), "Unsupported cross-DbSet query");
    }

    public override async Task GroupJoin_in_subquery_with_client_projection(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_in_subquery_with_client_projection(async), "Unsupported cross-DbSet query");
    }

    public override async Task GroupJoin_in_subquery_with_client_projection_nested1(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_in_subquery_with_client_projection_nested1(async), "Unsupported cross-DbSet query");
    }

    public override async Task GroupJoin_in_subquery_with_client_projection_nested2(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_in_subquery_with_client_projection_nested2(async), "Unsupported cross-DbSet query");
    }

#if (EF10 || EF11)
    public override async Task GroupJoin_client_method_on_outer(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_client_method_on_outer(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_client_method_in_OrderBy(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_client_method_in_OrderBy(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_without_DefaultIfEmpty(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_without_DefaultIfEmpty(async), "Unsupported cross-DbSet query");
    }
#endif

    public override async Task GroupJoin_with_subquery_on_inner(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupJoin_with_subquery_on_inner(async), "could not be translated");
    }

    public override async Task GroupJoin_with_subquery_on_inner_and_no_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupJoin_with_subquery_on_inner_and_no_DefaultIfEmpty(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Optional_navigation_in_subquery_with_unrelated_projection(bool async)
    {
        await base.Optional_navigation_in_subquery_with_unrelated_projection(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Explicit_GroupJoin_in_subquery_with_unrelated_projection(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection2(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Explicit_GroupJoin_in_subquery_with_unrelated_projection2(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection3(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Explicit_GroupJoin_in_subquery_with_unrelated_projection3(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection4(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Explicit_GroupJoin_in_subquery_with_unrelated_projection4(async), "ArgumentException");
    }
#endif

    public override async Task Explicit_GroupJoin_in_subquery_with_scalar_result_operator(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Explicit_GroupJoin_in_subquery_with_scalar_result_operator(async), "Unsupported cross-DbSet query");
    }

    public override async Task Explicit_GroupJoin_in_subquery_with_multiple_result_operator_distinct_count_materializes_main_clause(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Explicit_GroupJoin_in_subquery_with_multiple_result_operator_distinct_count_materializes_main_clause(async), "Unsupported cross-DbSet query");
    }

#if (EF10 || EF11)
    public override async Task Where_on_multilevel_reference_in_subquery_with_outer_projection(bool async)
    {
        await base.Where_on_multilevel_reference_in_subquery_with_outer_projection(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Nested_group_join_with_take(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Nested_group_join_with_take(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigation_with_same_navigation_compared_to_null(bool async)
    {
        await base.Navigation_with_same_navigation_compared_to_null(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Multi_level_navigation_compared_to_null(bool async)
    {
        await base.Multi_level_navigation_compared_to_null(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Multi_level_navigation_with_same_navigation_compared_to_null(bool async)
    {
        await base.Multi_level_navigation_with_same_navigation_compared_to_null(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigations_compared_to_each_other1(bool async)
    {
        await base.Navigations_compared_to_each_other1(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigations_compared_to_each_other2(bool async)
    {
        await base.Navigations_compared_to_each_other2(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigations_compared_to_each_other3(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Navigations_compared_to_each_other3(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigations_compared_to_each_other4(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Navigations_compared_to_each_other4(async), "Unsupported cross-DbSet query");
    }
#endif

    public override async Task Navigations_compared_to_each_other5(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Navigations_compared_to_each_other5(async), "could not be translated");
    }

    public override async Task Level4_Include(bool async)
    {
        await base.Level4_Include(async);
    }

#if (EF10 || EF11)
    public override async Task Comparing_collection_navigation_on_optional_reference_to_null(bool async)
    {
        await base.Comparing_collection_navigation_on_optional_reference_to_null(async);
    }
#endif

    public override async Task Select_subquery_with_client_eval_and_navigation1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_with_client_eval_and_navigation1(async), "could not be translated");
    }

    public override async Task Select_subquery_with_client_eval_and_navigation2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_with_client_eval_and_navigation2(async), "could not be translated");
    }

    public override async Task Select_subquery_with_client_eval_and_multi_level_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_with_client_eval_and_multi_level_navigation(async), "could not be translated");
    }

    public override async Task Member_doesnt_get_pushed_down_into_subquery_with_result_operator(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Member_doesnt_get_pushed_down_into_subquery_with_result_operator(async), "could not be translated");
    }

    public override async Task Subquery_with_Distinct_Skip_FirstOrDefault_without_OrderBy(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_with_Distinct_Skip_FirstOrDefault_without_OrderBy(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Project_collection_navigation_count(bool async)
    {
        // Fails: Server error executing the generated pipeline EF-X528
        await SpecFails.TranslationFailedAsync(() => base.Project_collection_navigation_count(async));
    }
#endif

    public override async Task Select_optional_navigation_property_string_concat(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_optional_navigation_property_string_concat(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Entries_for_detached_entities_are_removed(bool async)
    {
        await base.Entries_for_detached_entities_are_removed(async);
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Include_reference_with_groupby_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Include_reference_with_groupby_in_subquery(async));
    }
#else
    public override async Task Include_reference_with_groupby_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_with_groupby_in_subquery(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Multi_include_with_groupby_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Multi_include_with_groupby_in_subquery(async));
    }
#else
    public override async Task Multi_include_with_groupby_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multi_include_with_groupby_in_subquery(async), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_same_type(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_same_type(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_different_type(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_different_type(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(async);
    }
#endif

    public override async Task String_include_multiple_derived_collection_navigation_with_same_name_and_same_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.String_include_multiple_derived_collection_navigation_with_same_name_and_same_type(async), "Failure");
    }

    public override async Task String_include_multiple_derived_collection_navigation_with_same_name_and_different_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.String_include_multiple_derived_collection_navigation_with_same_name_and_different_type(async), "Failure");
    }

    public override async Task String_include_multiple_derived_collection_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.String_include_multiple_derived_collection_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(async), "Failure");
    }

#if (EF10 || EF11)
    public override async Task String_include_multiple_derived_navigations_complex(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.String_include_multiple_derived_navigations_complex(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Nav_rewrite_doesnt_apply_null_protection_for_function_arguments(bool async)
    {
        await base.Nav_rewrite_doesnt_apply_null_protection_for_function_arguments(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Accessing_optional_property_inside_result_operator_subquery(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Accessing_optional_property_inside_result_operator_subquery(async), "Failure");
    }
#endif

    public override async Task SelectMany_subquery_with_custom_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_subquery_with_custom_projection(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Include1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include1(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include2(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include3(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include4(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include4(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include5(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include5(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include6(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include6(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include7(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include7(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include10(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include10(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include11(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include11(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include12(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include12(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include13(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include13(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include14(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include14(async), "Failure");
    }
#endif

    public override async Task Include17(bool async)
    {
        await base.Include17(async);
    }

#if (EF10 || EF11)
    public override async Task Include18_1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include18_1(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_1_1(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.Include18_1_1(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_2(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.Include18_2(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_3(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.Include18_3(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_3_1(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.Include18_3_1(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_3_2(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.Include18_3_2(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_3_3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include18_3_3(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_4(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Include18_4(async), "Document element is missing for required");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18(bool async)
    {
        await base.Include18(async);
    }
#endif

    public override async Task Include19(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include19(async), "could not be translated");
    }

    public override async Task Include_with_all_method_include_gets_ignored(bool async)
    {
        await base.Include_with_all_method_include_gets_ignored(async);
    }

#if (EF10 || EF11)
    public override async Task Join_with_navigations_in_the_result_selector1(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.Join_with_navigations_in_the_result_selector1(async), "ArgumentNullException");
    }
#endif

    public override async Task Member_pushdown_with_multiple_collections(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Member_pushdown_with_multiple_collections(async), "could not be translated");
    }

    public override async Task Include_multiple_collections_on_same_level(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include_multiple_collections_on_same_level(async), "Failure");
    }

#if (EF10 || EF11)
    public override async Task Null_check_removal_applied_recursively(bool async)
    {
        await base.Null_check_removal_applied_recursively(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Null_check_different_structure_does_not_remove_null_checks(bool async)
    {
        await base.Null_check_different_structure_does_not_remove_null_checks(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Union_over_entities_with_different_nullability(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.Union_over_entities_with_different_nullability(async), "ArgumentNullException");
    }
#endif

    public override async Task Including_reference_navigation_and_projecting_collection_navigation_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Including_reference_navigation_and_projecting_collection_navigation_2(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task OrderBy_collection_count_ThenBy_reference_navigation(bool async)
    {
        // Fails: Join key selector that is a sub-query: serializer cannot be created for MongoQuery EF-X534
        await SpecFails.ThrowsAsync(() => base.OrderBy_collection_count_ThenBy_reference_navigation(async), "BsonSerializationException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Null_conditional_is_not_applied_explicitly_for_optional_navigation(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Null_conditional_is_not_applied_explicitly_for_optional_navigation(async), "Failure");
    }
#endif

    public override async Task Sum_with_selector_cast_using_as(bool async)
    {
        await base.Sum_with_selector_cast_using_as(async);
    }

    public override async Task Sum_with_filter_with_include_selector_cast_using_as(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Sum_with_filter_with_include_selector_cast_using_as(async), "Unsupported cross-DbSet query");
    }

#if (EF10 || EF11)
    public override async Task Select_with_joined_where_clause_cast_using_as(bool async)
    {
        await base.Select_with_joined_where_clause_cast_using_as(async);
    }
#endif

    public override async Task SelectMany_with_outside_reference_to_joined_table_correctly_translated_to_apply(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_outside_reference_to_joined_table_correctly_translated_to_apply(async), "could not be translated");
    }

    public override async Task Nested_SelectMany_correlated_with_join_table_correctly_translated_to_apply(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Nested_SelectMany_correlated_with_join_table_correctly_translated_to_apply(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Contains_over_optional_navigation_with_null_constant(bool async)
    {
        await base.Contains_over_optional_navigation_with_null_constant(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Contains_over_optional_navigation_with_null_parameter(bool async)
    {
        await base.Contains_over_optional_navigation_with_null_parameter(async);
    }
#endif

    public override async Task Contains_over_optional_navigation_with_null_column(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Contains_over_optional_navigation_with_null_column(async), "could not be translated");
    }

    public override async Task Contains_over_optional_navigation_with_null_entity_reference(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Contains_over_optional_navigation_with_null_entity_reference(async), "could not be translated");
    }

    public override async Task Element_selector_with_coalesce_repeated_in_aggregate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Element_selector_with_coalesce_repeated_in_aggregate(async), "could not be translated");
    }

    public override async Task Nested_object_constructed_from_group_key_properties(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Nested_object_constructed_from_group_key_properties(async), "could not be translated");
    }

    public override async Task GroupBy_aggregate_where_required_relationship(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_aggregate_where_required_relationship(async), "could not be translated");
    }

    public override async Task GroupBy_aggregate_where_required_relationship_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_aggregate_where_required_relationship_2(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Member_over_null_check_ternary_and_nested_dto_type(bool async)
    {
        await base.Member_over_null_check_ternary_and_nested_dto_type(async);
    }
#endif

#if (EF10 || EF11)
    public override async Task Member_over_null_check_ternary_and_nested_anonymous_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Member_over_null_check_ternary_and_nested_anonymous_type(async), "Failure");
    }
#endif

    public override async Task Let_let_contains_from_outer_let(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Let_let_contains_from_outer_let(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Multiple_conditionals_in_projection(bool async)
    {
        await base.Multiple_conditionals_in_projection(async);
    }
#endif

    public override async Task Composite_key_join_on_groupby_aggregate_projecting_only_grouping_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Composite_key_join_on_groupby_aggregate_projecting_only_grouping_key(async), "could not be translated");
    }

#if EF9 || (EF10 || EF11)
    public override async Task Composite_key_join_on_groupby_aggregate_projecting_only_grouping_key2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Composite_key_join_on_groupby_aggregate_projecting_only_grouping_key2(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Multiple_joins_groupby_predicate(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentNullException>(() => base.Multiple_joins_groupby_predicate(async));
    }
#else
    public override async Task Multiple_joins_groupby_predicate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_joins_groupby_predicate(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Collection_FirstOrDefault_property_accesses_in_projection(bool async)
    {
        await base.Collection_FirstOrDefault_property_accesses_in_projection(async);
    }
#else
    public override async Task Collection_FirstOrDefault_property_accesses_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_FirstOrDefault_property_accesses_in_projection(async), "could not be translated");
    }
#endif

    public override async Task Collection_FirstOrDefault_entity_reference_accesses_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_FirstOrDefault_entity_reference_accesses_in_projection(async), "could not be translated");
    }

    public override async Task Collection_FirstOrDefault_entity_collection_accesses_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_FirstOrDefault_entity_collection_accesses_in_projection(async), "could not be translated");
    }

    public override async Task Multiple_collection_FirstOrDefault_followed_by_member_access_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_collection_FirstOrDefault_followed_by_member_access_in_projection(async), "could not be translated");
    }

    public override async Task Projecting_columns_with_same_name_from_different_entities_making_sure_aliasing_works_after_Distinct(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_columns_with_same_name_from_different_entities_making_sure_aliasing_works_after_Distinct(async), "could not be translated");
    }

    public override async Task Complex_query_with_let_collection_SelectMany(bool async)
    {
        await base.Complex_query_with_let_collection_SelectMany(async);
    }

    public override async Task SelectMany_without_collection_selector_returning_queryable(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_without_collection_selector_returning_queryable(async), "could not be translated");
    }

    public override async Task Select_projecting_queryable_followed_by_SelectMany(bool async)
    {
        await base.Select_projecting_queryable_followed_by_SelectMany(async);
    }

    public override async Task Join_with_result_selector_returning_queryable_throws_validation_error(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_with_result_selector_returning_queryable_throws_validation_error(async), "could not be translated");
    }

    public override async Task Select_projecting_queryable_followed_by_Join(bool async)
    {
        await base.Select_projecting_queryable_followed_by_Join(async);
    }

    public override async Task Select_projecting_queryable_in_anonymous_projection_followed_by_Join(bool async)
    {
        await base.Select_projecting_queryable_in_anonymous_projection_followed_by_Join(async);
    }

    public override async Task Project_shadow_properties5(bool async)
    {
        await base.Project_shadow_properties5(async);
    }

    public override async Task Project_shadow_properties6(bool async)
    {
        await base.Project_shadow_properties6(async);
    }

    public override async Task Project_shadow_properties7(bool async)
    {
        await base.Project_shadow_properties7(async);
    }

    public override async Task Project_shadow_properties8(bool async)
    {
        await base.Project_shadow_properties8(async);
    }

    public override async Task Project_shadow_properties10(bool async)
    {
        await base.Project_shadow_properties10(async);
    }

    public override async Task Prune_does_not_throw_null_ref(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Prune_does_not_throw_null_ref(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_with_predicate_using_closure(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure_nested(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_with_predicate_using_closure_nested(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested_same_param(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested_same_param(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure_nested_same_param(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_with_predicate_using_closure_nested_same_param(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Multiple_optional_navs_should_not_deadlock(bool async)
    {
        await base.Multiple_optional_navs_should_not_deadlock(async);
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Correlated_projection_with_first(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_projection_with_first(async), "could not be translated");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Max_in_multi_level_nested_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Max_in_multi_level_nested_subquery(async), "could not be translated");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Multiple_select_many_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_select_many_in_projection(async), "could not be translated");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Single_select_many_in_projection_with_take(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Single_select_many_in_projection_with_take(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Join_condition_optimizations_applied_correctly_when_anonymous_type_with_multiple_properties(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Join_condition_optimizations_applied_correctly_when_anonymous_type_with_multiple_properties(async), "Failure");
    }
#endif

#if EF8 || EF9
    public override async Task Join_condition_optimizations_applied_correctly_when_anonymous_type_with_single_property(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Join_condition_optimizations_applied_correctly_when_anonymous_type_with_single_property(async), "Failure");
    }
#endif

#if EF8 || EF9
    public override async Task Entity_equality_empty(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Entity_equality_empty(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_when_sentinel_ef_property(bool async)
    {
        await base.Key_equality_when_sentinel_ef_property(async);
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_required(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_required(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_required2(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_required2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_nested(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_nested(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_nested2(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_nested2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_and_member_expression1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_and_member_expression2(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_and_member_expression3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_navigation_converted_to_FK(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_navigation_converted_to_FK(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_two_conditions_on_same_navigation(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Key_equality_two_conditions_on_same_navigation(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_two_conditions_on_same_navigation2(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_two_conditions_on_same_navigation2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_key_access_optional(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigation_key_access_optional(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_key_access_required(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigation_key_access_required(async));
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_key_access_optional_comparison(bool async)
    {
        await base.Navigation_key_access_optional_comparison(async);
    }
#endif

#if EF8 || EF9
    public override async Task Simple_level1_include(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_include(async));
    }
#endif

#if EF8 || EF9
    public override async Task Simple_level1_level2_include(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_level2_include(async));
    }
#endif

#if EF8 || EF9
    public override async Task Simple_level1_level2_level3_include(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_level2_level3_include(async));
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_key_access_required_comparison(bool async)
    {
        await base.Navigation_key_access_required_comparison(async);
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_inside_method_call_translated_to_join(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Navigation_inside_method_call_translated_to_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_inside_method_call_translated_to_join2(bool async)
    {
        await base.Navigation_inside_method_call_translated_to_join2(async);
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_method_call_translated_to_join(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_method_call_translated_to_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_property_method_translated_to_join(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_property_method_translated_to_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Method_call_on_optional_navigation_translates_to_null_conditional_properly_for_arguments(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Method_call_on_optional_navigation_translates_to_null_conditional_properly_for_arguments(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_method_call_translated_to_join_keeps_original_nullability(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_method_call_translated_to_join_keeps_original_nullability(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability_also_for_arguments(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability_also_for_arguments(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Join_navigation_in_outer_selector_translated_to_extra_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_in_inner_selector(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Join_navigation_in_inner_selector(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigations_in_inner_selector_translated_without_collision(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Join_navigations_in_inner_selector_translated_without_collision(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_non_key_join(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Join_navigation_non_key_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_with_orderby_on_inner_sequence_navigation_non_key_join(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Join_with_orderby_on_inner_sequence_navigation_non_key_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_deeply_nested_required(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigation_deeply_nested_required(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_and_project_into_anonymous_type(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include_reference_and_project_into_anonymous_type(async));
    }
#endif

#if EF8 || EF9
    public override async Task Select_nav_prop_reference_optional1(bool async)
    {
        await base.Select_nav_prop_reference_optional1(async);
    }
#endif

#if EF8 || EF9
    public override async Task Select_nav_prop_reference_optional1_via_DefaultIfEmpty(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Select_nav_prop_reference_optional1_via_DefaultIfEmpty(async));
    }
#endif

#if EF8 || EF9
    public override async Task Select_nav_prop_reference_optional2(bool async)
    {
        await base.Select_nav_prop_reference_optional2(async);
    }
#endif

#if EF8 || EF9
    public override async Task Select_nav_prop_reference_optional2_via_DefaultIfEmpty(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Select_nav_prop_reference_optional2_via_DefaultIfEmpty(async));
    }
#endif

#if EF8 || EF9
    public override async Task Select_nav_prop_reference_optional3(bool async)
    {
        await base.Select_nav_prop_reference_optional3(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_nav_prop_reference_optional1(bool async)
    {
        await base.Where_nav_prop_reference_optional1(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_nav_prop_reference_optional1_via_DefaultIfEmpty(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Where_nav_prop_reference_optional1_via_DefaultIfEmpty(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_nav_prop_reference_optional2(bool async)
    {
        await base.Where_nav_prop_reference_optional2(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_nav_prop_reference_optional2_via_DefaultIfEmpty(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Where_nav_prop_reference_optional2_via_DefaultIfEmpty(async));
    }
#endif

#if EF8 || EF9
    public override async Task Select_multiple_nav_prop_reference_optional(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Select_multiple_nav_prop_reference_optional(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_member_compared_to_value(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Where_multiple_nav_prop_reference_optional_member_compared_to_value(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_member_compared_to_null(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Where_multiple_nav_prop_reference_optional_member_compared_to_null(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null2(bool async)
    {
        await base.Where_multiple_nav_prop_reference_optional_compared_to_null2(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null4(bool async)
    {
        await base.Where_multiple_nav_prop_reference_optional_compared_to_null4(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null5(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null5(async));
    }
#endif

#if EF8 || EF9
    public override async Task Select_multiple_nav_prop_reference_required(bool async)
    {
        await base.Select_multiple_nav_prop_reference_required(async);
    }
#endif

#if EF8 || EF9
    public override async Task Select_multiple_nav_prop_reference_required2(bool async)
    {
        await base.Select_multiple_nav_prop_reference_required2(async);
    }
#endif

#if EF8 || EF9
    public override async Task Select_multiple_nav_prop_optional_required(bool async)
    {
        await base.Select_multiple_nav_prop_optional_required(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_optional_required(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Where_multiple_nav_prop_optional_required(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse2(bool async)
    {
        await base.Where_complex_predicate_with_with_nav_prop_and_OrElse2(async);
    }
#endif

#if EF8 || EF9
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Where_complex_predicate_with_with_nav_prop_and_OrElse3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse4(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_complex_predicate_with_with_nav_prop_and_OrElse4(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Complex_navigations_with_predicate_projected_into_anonymous_type2(bool async)
    {
        await base.Complex_navigations_with_predicate_projected_into_anonymous_type2(async);
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_projected_into_DTO(bool async)
    {
        await base.Optional_navigation_projected_into_DTO(async);
    }
#endif

#if EF8 || EF9
    public override async Task OrderBy_nav_prop_reference_optional(bool async)
    {
        await base.OrderBy_nav_prop_reference_optional(async);
    }
#endif

#if EF8 || EF9
    public override async Task OrderBy_nav_prop_reference_optional_via_DefaultIfEmpty(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.OrderBy_nav_prop_reference_optional_via_DefaultIfEmpty(async));
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Sum(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Sum(async);
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Min(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Result_operator_nav_prop_reference_optional_Min(async));
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Max(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Max(async);
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Average(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average(async);
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Average_with_identity_selector(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average_with_identity_selector(async);
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Average_without_selector(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average_without_selector(async);
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_via_DefaultIfEmpty(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Result_operator_nav_prop_reference_optional_via_DefaultIfEmpty(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include_with_optional_navigation(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include_with_optional_navigation(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_navigation_property_to_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Where_navigation_property_to_collection(async));
    }
#endif

#if EF8 || EF9
#if EF8
    public override async Task Where_navigation_property_to_collection2(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::MongoDB.Bson.BsonSerializationException>(() => base.Where_navigation_property_to_collection2(async));
    }
#else
    public override async Task Where_navigation_property_to_collection2(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::MongoDB.Bson.BsonSerializationException>(() => base.Where_navigation_property_to_collection2(async));
    }
#endif
#endif

#if EF8 || EF9
#if EF8
    public override async Task Where_navigation_property_to_collection_of_original_entity_type(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::MongoDB.Bson.BsonSerializationException>(() => base.Where_navigation_property_to_collection_of_original_entity_type(async));
    }
#else
    public override async Task Where_navigation_property_to_collection_of_original_entity_type(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::MongoDB.Bson.BsonSerializationException>(() => base.Where_navigation_property_to_collection_of_original_entity_type(async));
    }
#endif
#endif

#if EF8 || EF9
    public override async Task Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access2(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Order_by_key_of_navigation_similar_to_projected_gets_optimized_into_FK_access(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Order_by_key_of_navigation_similar_to_projected_gets_optimized_into_FK_access(async));
    }
#endif

#if EF8 || EF9
    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(bool async)
    {
        await base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(async);
    }
#endif

#if EF8 || EF9
    public override async Task Order_by_key_of_anonymous_type_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(bool async)
    {
        await base.Order_by_key_of_anonymous_type_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(async);
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_take_optional_navigation(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Optional_navigation_take_optional_navigation(async));
    }
#endif

#if EF8 || EF9
    public override async Task Projection_select_correct_table_from_subquery_when_materialization_is_not_required(bool async)
    {
        await base.Projection_select_correct_table_from_subquery_when_materialization_is_not_required(async);
    }
#endif

#if EF8 || EF9
    public override async Task Projection_select_correct_table_with_anonymous_projection_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projection_select_correct_table_with_anonymous_projection_in_subquery(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Projection_select_correct_table_in_subquery_when_materialization_is_not_required_in_multiple_joins(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projection_select_correct_table_in_subquery_when_materialization_is_not_required_in_multiple_joins(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Where_predicate_on_optional_reference_navigation(bool async)
    {
        await base.Where_predicate_on_optional_reference_navigation(async);
    }
#endif

#if EF8 || EF9
    public override async Task Select_join_subquery_containing_filter_and_distinct(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentNullException>(() => base.Select_join_subquery_containing_filter_and_distinct(async));
    }
#endif

#if EF8 || EF9
    public override async Task Select_join_with_key_selector_being_a_subquery(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentNullException>(() => base.Select_join_with_key_selector_being_a_subquery(async));
    }
#endif

#if EF8 || EF9
    public override async Task Contains_with_subquery_optional_navigation_and_constant_item(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Contains_with_subquery_optional_navigation_and_constant_item(async));
    }
#endif

#if EF8 || EF9
    public override async Task Contains_with_subquery_optional_navigation_scalar_distinct_and_constant_item(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Contains_with_subquery_optional_navigation_scalar_distinct_and_constant_item(async));
    }
#endif

#if EF8 || EF9
    public override async Task Required_navigation_on_a_subquery_with_First_in_predicate(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Required_navigation_on_a_subquery_with_First_in_predicate(async));
    }
#endif

#if EF8 || EF9
    public override async Task Manually_created_left_join_propagates_nullability_to_navigations(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Manually_created_left_join_propagates_nullability_to_navigations(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_propagates_nullability_to_manually_created_left_join1(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Optional_navigation_propagates_nullability_to_manually_created_left_join1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Null_reference_protection_complex_materialization(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentNullException>(() => base.Null_reference_protection_complex_materialization(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened2(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened2(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened3(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened3(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer_with_client_method(bool async)
    {
        // Fails: an unsupported query throws ArgumentException where the upstream test expects InvalidOperationException; the exception type of an unsupported query is not contract EF-X705
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer_with_client_method(async), "typeof(System.ArgumentException)");
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_inner(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_inner(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_with_orderby_on_inner_sequence_projecting_inner(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_with_orderby_on_inner_sequence_projecting_inner(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_left_side_being_a_subquery(bool async)
    {
        await base.GroupJoin_on_left_side_being_a_subquery(async);
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_right_side_being_a_subquery(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_on_right_side_being_a_subquery(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_client_method_on_outer(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.GroupJoin_client_method_on_outer(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_client_method_in_OrderBy(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_client_method_in_OrderBy(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_without_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.GroupJoin_without_DefaultIfEmpty(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_in_subquery_with_unrelated_projection(bool async)
    {
        await base.Optional_navigation_in_subquery_with_unrelated_projection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Explicit_GroupJoin_in_subquery_with_unrelated_projection(async));
    }
#endif

#if EF8 || EF9
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection2(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Explicit_GroupJoin_in_subquery_with_unrelated_projection2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection3(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Explicit_GroupJoin_in_subquery_with_unrelated_projection3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection4(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Explicit_GroupJoin_in_subquery_with_unrelated_projection4(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_on_multilevel_reference_in_subquery_with_outer_projection(bool async)
    {
        await base.Where_on_multilevel_reference_in_subquery_with_outer_projection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Nested_group_join_with_take(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Nested_group_join_with_take(async));
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_with_same_navigation_compared_to_null(bool async)
    {
        await base.Navigation_with_same_navigation_compared_to_null(async);
    }
#endif

#if EF8 || EF9
    public override async Task Multi_level_navigation_compared_to_null(bool async)
    {
        await base.Multi_level_navigation_compared_to_null(async);
    }
#endif

#if EF8 || EF9
    public override async Task Multi_level_navigation_with_same_navigation_compared_to_null(bool async)
    {
        await base.Multi_level_navigation_with_same_navigation_compared_to_null(async);
    }
#endif

#if EF8 || EF9
    public override async Task Navigations_compared_to_each_other1(bool async)
    {
        await base.Navigations_compared_to_each_other1(async);
    }
#endif

#if EF8 || EF9
    public override async Task Navigations_compared_to_each_other2(bool async)
    {
        await base.Navigations_compared_to_each_other2(async);
    }
#endif

#if EF8 || EF9
    public override async Task Navigations_compared_to_each_other3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Navigations_compared_to_each_other3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Navigations_compared_to_each_other4(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Navigations_compared_to_each_other4(async));
    }
#endif

#if EF8 || EF9
    public override async Task Comparing_collection_navigation_on_optional_reference_to_null(bool async)
    {
        await base.Comparing_collection_navigation_on_optional_reference_to_null(async);
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_navigation_count(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Project_collection_navigation_count(async));
    }
#endif

#if EF8 || EF9
    public override async Task Entries_for_detached_entities_are_removed(bool async)
    {
        await base.Entries_for_detached_entities_are_removed(async);
    }
#endif

#if EF8 || EF9
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_same_type(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_same_type(async);
    }
#endif

#if EF8 || EF9
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_different_type(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_different_type(async);
    }
#endif

#if EF8 || EF9
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(async);
    }
#endif

#if EF8 || EF9
    public override async Task String_include_multiple_derived_navigations_complex(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.String_include_multiple_derived_navigations_complex(async));
    }
#endif

#if EF8 || EF9
    public override async Task Nav_rewrite_doesnt_apply_null_protection_for_function_arguments(bool async)
    {
        await base.Nav_rewrite_doesnt_apply_null_protection_for_function_arguments(async);
    }
#endif

#if EF8 || EF9
    public override async Task Accessing_optional_property_inside_result_operator_subquery(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Accessing_optional_property_inside_result_operator_subquery(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include2(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include4(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include4(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include5(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include5(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include6(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include6(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include7(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include7(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include10(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include10(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include11(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include11(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include12(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include12(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include13(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include13(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include14(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include14(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include18_1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include18_1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include18_1_1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include18_1_1(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include18_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include18_2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include18_3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include18_3(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include18_3_1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include18_3_1(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include18_3_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include18_3_2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include18_3_3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include18_3_3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include18_4(bool async)
    {
        // Fails: InvalidOperationException from query execution or materialization EF-X604
        await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Include18_4(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include18(bool async)
    {
        await base.Include18(async);
    }
#endif

#if EF8 || EF9
    public override async Task Join_with_navigations_in_the_result_selector1(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentNullException>(() => base.Join_with_navigations_in_the_result_selector1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Null_check_removal_applied_recursively(bool async)
    {
        await base.Null_check_removal_applied_recursively(async);
    }
#endif

#if EF8 || EF9
    public override async Task Null_check_different_structure_does_not_remove_null_checks(bool async)
    {
        await base.Null_check_different_structure_does_not_remove_null_checks(async);
    }
#endif

#if EF8 || EF9
    public override async Task Union_over_entities_with_different_nullability(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentNullException>(() => base.Union_over_entities_with_different_nullability(async));
    }
#endif

#if EF8 || EF9
#if EF8
    public override async Task OrderBy_collection_count_ThenBy_reference_navigation(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::MongoDB.Bson.BsonSerializationException>(() => base.OrderBy_collection_count_ThenBy_reference_navigation(async));
    }
#else
    public override async Task OrderBy_collection_count_ThenBy_reference_navigation(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::MongoDB.Bson.BsonSerializationException>(() => base.OrderBy_collection_count_ThenBy_reference_navigation(async));
    }
#endif
#endif

#if EF8 || EF9
    public override async Task Null_conditional_is_not_applied_explicitly_for_optional_navigation(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Null_conditional_is_not_applied_explicitly_for_optional_navigation(async));
    }
#endif

#if EF8 || EF9
    public override async Task Select_with_joined_where_clause_cast_using_as(bool async)
    {
        await base.Select_with_joined_where_clause_cast_using_as(async);
    }
#endif

#if EF8 || EF9
    public override async Task Contains_over_optional_navigation_with_null_constant(bool async)
    {
        await base.Contains_over_optional_navigation_with_null_constant(async);
    }
#endif

#if EF8 || EF9
    public override async Task Contains_over_optional_navigation_with_null_parameter(bool async)
    {
        await base.Contains_over_optional_navigation_with_null_parameter(async);
    }
#endif

#if EF8 || EF9
    public override async Task Member_over_null_check_ternary_and_nested_dto_type(bool async)
    {
        await base.Member_over_null_check_ternary_and_nested_dto_type(async);
    }
#endif

#if EF8 || EF9
    public override async Task Member_over_null_check_ternary_and_nested_anonymous_type(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Member_over_null_check_ternary_and_nested_anonymous_type(async));
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_conditionals_in_projection(bool async)
    {
        await base.Multiple_conditionals_in_projection(async);
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.GroupJoin_SelectMany_with_predicate_using_closure(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure_nested(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.GroupJoin_SelectMany_with_predicate_using_closure_nested(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested_same_param(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested_same_param(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure_nested_same_param(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.GroupJoin_SelectMany_with_predicate_using_closure_nested_same_param(async));
    }
#endif

#if EF9
    public override async Task Multiple_optional_navs_should_not_deadlock(bool async)
    {
        await base.Multiple_optional_navs_should_not_deadlock(async);
    }
#endif

#if EF8
    public override async Task Distinct_skip_without_orderby(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Distinct_skip_without_orderby(async), "could not be translated");
    }
#endif

#if EF8
    public override async Task Distinct_take_without_orderby(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Distinct_take_without_orderby(async), "could not be translated");
    }
#endif

    public class ComplexNavigationsSharedTypeQueryMongoFixture : ComplexNavigationsSharedTypeQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("CNavShared");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
        }

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }
    }
}
