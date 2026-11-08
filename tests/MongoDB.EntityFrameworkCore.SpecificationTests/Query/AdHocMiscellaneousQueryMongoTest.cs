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

public class AdHocMiscellaneousQueryMongoTest : AdHocMiscellaneousQueryTestBase
{
#if EF10 || EF11
    public AdHocMiscellaneousQueryMongoTest(NonSharedFixture fixture)
        : base(fixture)
    {
        // The class-level fixture would reuse the first test's (already seeded) test store for every test;
        // give each test its own uniquely named database instead.
#if EF11
        NonSharedFixture = null;
#else
        Fixture = null;
#endif
    }

#endif
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task First_FirstOrDefault_ix_async()
    {
        await base.First_FirstOrDefault_ix_async();
    }

#if EF10
    public override async Task LeftJoin_with_missing_key_values_on_both_sides(bool async)
    {
        await base.LeftJoin_with_missing_key_values_on_both_sides(async);
    }
#endif

    public override async Task Shadow_property_with_inheritance()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Shadow_property_with_inheritance(), "cannot be tracked because another instance");
    }

    public override async Task Inlined_dbcontext_is_not_leaking()
    {
        await base.Inlined_dbcontext_is_not_leaking();
    }

    public override async Task Discriminator_type_is_handled_correctly()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Discriminator_type_is_handled_correctly(), "cannot be tracked because another instance");
    }

    public override async Task New_instances_in_projection_are_not_shared_across_results()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.New_instances_in_projection_are_not_shared_across_results(), "cannot be tracked because another instance");
    }

    public override async Task Enum_has_flag_applies_explicit_cast_for_constant()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Enum_has_flag_applies_explicit_cast_for_constant(), "cannot be tracked because another instance");
    }

    public override async Task Enum_has_flag_does_not_apply_explicit_cast_for_non_constant()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Enum_has_flag_does_not_apply_explicit_cast_for_non_constant(), "cannot be tracked because another instance");
    }

    public override async Task Variable_from_closure_is_parametrized()
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Variable_from_closure_is_parametrized(), "Failure");
    }

    public override async Task Relational_command_cache_creates_new_entry_when_parameter_nullability_changes()
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Relational_command_cache_creates_new_entry_when_parameter_nullability_changes(), "Failure");
    }

    public override async Task Query_cache_entries_are_evicted_as_necessary()
    {
        await base.Query_cache_entries_are_evicted_as_necessary();
    }

    public override async Task Explicitly_compiled_query_does_not_add_cache_entry()
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Explicitly_compiled_query_does_not_add_cache_entry(), "Failure");
    }

    public override async Task Conditional_expression_with_conditions_does_not_collapse_if_nullable_bool()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Conditional_expression_with_conditions_does_not_collapse_if_nullable_bool(), "cannot be tracked because another instance");
    }

    public override async Task QueryBuffer_requirement_is_computed_when_querying_base_type_while_derived_type_has_shadow_prop()
    {
        await base.QueryBuffer_requirement_is_computed_when_querying_base_type_while_derived_type_has_shadow_prop();
    }

    public override async Task Average_with_cast()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Average_with_cast(), "cannot be tracked because another instance");
    }

    public override async Task Parameterless_ctor_on_inner_DTO_gets_called_for_every_row()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Parameterless_ctor_on_inner_DTO_gets_called_for_every_row(), "cannot be tracked because another instance");
    }

#if EF9 || EF10
    public override async Task Union_and_insert_works_correctly_together()
    {
        // Fails: InvalidOperationException from query execution or materialization EF-X604
        await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Union_and_insert_works_correctly_together());
    }
#else
    public override async Task Union_and_insert_works_correctly_together()
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Union_and_insert_works_correctly_together(), "Unsupported cross-DbSet query");
    }
#endif

    public override async Task Repeated_parameters_in_generated_query_sql()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Repeated_parameters_in_generated_query_sql(), "cannot be tracked because another instance");
    }

    public override async Task Operators_combine_nullability_of_entity_shapers()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Operators_combine_nullability_of_entity_shapers(), "cannot be tracked because another instance");
    }

    public override async Task Comparing_enum_casted_to_byte_with_int_parameter(bool async)
    {
        await base.Comparing_enum_casted_to_byte_with_int_parameter(async);
    }

    public override async Task Comparing_enum_casted_to_byte_with_int_constant(bool async)
    {
        await base.Comparing_enum_casted_to_byte_with_int_constant(async);
    }

    public override async Task Comparing_byte_column_to_enum_in_vb_creating_double_cast(bool async)
    {
        await base.Comparing_byte_column_to_enum_in_vb_creating_double_cast(async);
    }

    public override async Task Null_check_removal_in_ternary_maintain_appropriate_cast(bool async)
    {
        await base.Null_check_removal_in_ternary_maintain_appropriate_cast(async);
    }

    public override async Task SaveChangesAsync_accepts_changes_with_ConfigureAwait_true()
    {
        await base.SaveChangesAsync_accepts_changes_with_ConfigureAwait_true();
    }

    public override async Task Bool_discriminator_column_works(bool async)
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Bool_discriminator_column_works(async), "cannot be tracked because another instance");
    }

    public override async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Unwrap_convert_node_over_projection_when_translating_contains_over_subquery(async), "could not be translated");
    }

    public override async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_2(async), "could not be translated");
    }

    public override async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_3(async), "could not be translated");
    }

    public override async Task GroupBy_aggregate_on_right_side_of_join(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupBy_aggregate_on_right_side_of_join(async), "ArgumentException");
    }

    public override async Task Enum_with_value_converter_matching_take_value(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Enum_with_value_converter_matching_take_value(async), "could not be translated");
    }

    public override async Task GroupBy_Aggregate_over_navigations_repeated(bool async)
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.GroupBy_Aggregate_over_navigations_repeated(async), "cannot be tracked because another instance");
    }

    public override async Task Aggregate_over_subquery_in_group_by_projection(bool async)
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Aggregate_over_subquery_in_group_by_projection(async), "cannot be tracked because another instance");
    }

#if EF9 || EF10
    public override async Task Aggregate_over_subquery_in_group_by_projection_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Aggregate_over_subquery_in_group_by_projection_2(async));
    }
#else
    public override async Task Aggregate_over_subquery_in_group_by_projection_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Aggregate_over_subquery_in_group_by_projection_2(async), "could not be translated");
    }
#endif

#if EF9 || EF10
    public override async Task Group_by_aggregate_in_subquery_projection_after_group_by(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Group_by_aggregate_in_subquery_projection_after_group_by(async));
    }
#else
    public override async Task Group_by_aggregate_in_subquery_projection_after_group_by(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Group_by_aggregate_in_subquery_projection_after_group_by(async), "could not be translated");
    }
#endif

    public override async Task Subquery_first_member_compared_to_null(bool async)
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Subquery_first_member_compared_to_null(async), "cannot be tracked because another instance");
    }

    public override async Task SelectMany_where_Select(bool async)
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.SelectMany_where_Select(async), "cannot be tracked because another instance");
    }

    public override async Task Flattened_GroupJoin_on_interface_generic(bool async)
    {
        // Fails: Many-to-many join entity type is configured as owned and cannot be queried as a DbSet EF-X539
        await SpecFails.ThrowsAsync(() => base.Flattened_GroupJoin_on_interface_generic(async), "configured as an owned");
    }

#if EF9
    public override async Task Pushdown_does_not_add_grouping_key_to_projection_when_distinct_is_applied(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Pushdown_does_not_add_grouping_key_to_projection_when_distinct_is_applied(async));
    }
#elif EF10
    public override async Task Pushdown_does_not_add_grouping_key_to_projection_when_distinct_is_applied(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Pushdown_does_not_add_grouping_key_to_projection_when_distinct_is_applied(async));
    }
#else
    public override async Task Pushdown_does_not_add_grouping_key_to_projection_when_distinct_is_applied(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Pushdown_does_not_add_grouping_key_to_projection_when_distinct_is_applied(async), "could not be translated");
    }
#endif

#if EF10
    public override async Task Filter_on_nested_DTO_with_interface_gets_simplified_correctly(bool async)
    {
        // Fails: Filter on a nested DTO via an interface: serializer value type mismatch EF-X546
        await SpecFails.ThrowsAsync(() => base.Filter_on_nested_DTO_with_interface_gets_simplified_correctly(async), "Value type of serializer");
    }
#endif

#if EF9
    public override async Task Left_join_with_missing_key_values_on_both_sides(bool async)
    {
        await base.Left_join_with_missing_key_values_on_both_sides(async);
    }
#endif

#if EF9
    public override async Task Filter_on_nested_DTO_with_interface_gets_simplified_correctly(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Filter_on_nested_DTO_with_interface_gets_simplified_correctly(async));
    }
#endif

    private ITestStoreFactory? _testStoreFactory;

#if EF11
    protected override ITestStoreFactory NonSharedTestStoreFactory
#else
    protected override ITestStoreFactory TestStoreFactory
#endif
        => _testStoreFactory!;

#if EF11
    protected override string NonSharedStoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocMisc");
#else
    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocMisc");
#endif

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);

        await base.InitializeAsync();
    }
}

#endif
