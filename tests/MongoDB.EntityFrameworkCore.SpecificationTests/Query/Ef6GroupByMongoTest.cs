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

public class Ef6GroupByMongoTest : Ef6GroupByTestBase<Ef6GroupByMongoTest.Ef6GroupByMongoFixture>
{
    public Ef6GroupByMongoTest(Ef6GroupByMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task GroupBy_is_optimized_when_projecting_group_key(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_projecting_group_key(async), "could not be translated");
    }

    public override async Task GroupBy_is_optimized_when_projecting_group_count(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_projecting_group_count(async), "could not be translated");
    }

    public override async Task GroupBy_is_optimized_when_projecting_expression_containing_group_key(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_projecting_expression_containing_group_key(async), "could not be translated");
    }

    public override async Task GroupBy_is_optimized_when_projecting_aggregate_on_the_group(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_projecting_aggregate_on_the_group(async), "could not be translated");
    }

    public override async Task GroupBy_is_optimized_when_projecting_anonymous_type_containing_group_key_and_group_aggregate(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_projecting_anonymous_type_containing_group_key_and_group_aggregate(async), "could not be translated");
    }

    public override async Task GroupBy_is_optimized_when_projecting_anonymous_type_containing_group_key_and_multiple_group_aggregates(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_projecting_anonymous_type_containing_group_key_and_multiple_group_aggregates(async), "could not be translated");
    }

    public override async Task GroupBy_is_optimized_when_projecting_conditional_expression_containing_group_key(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_projecting_conditional_expression_containing_group_key(async), "could not be translated");
    }

#if EF9 || EF10
    public override async Task GroupBy_is_optimized_when_filtering_and_projecting_anonymous_type_with_group_key_and_function_aggregate(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_filtering_and_projecting_anonymous_type_with_group_key_and_function_aggregate(async), "could not be translated");
    }
#endif

    public override async Task GroupBy_is_optimized_when_projecting_function_aggregate_with_expression(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_projecting_function_aggregate_with_expression(async), "could not be translated");
    }

    public override async Task GroupBy_is_optimized_when_projecting_expression_with_multiple_function_aggregates(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_projecting_expression_with_multiple_function_aggregates(async), "could not be translated");
    }

    public override async Task GroupBy_is_optimized_when_grouping_by_row_and_projecting_column_of_the_key_row(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_grouping_by_row_and_projecting_column_of_the_key_row(async), "could not be translated");
    }

    public override async Task Grouping_by_all_columns_doesnt_produce_a_groupby_statement(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_doesnt_produce_a_groupby_statement(async), "could not be translated");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_1(bool async)
    {
        // Fails: GroupBy over all columns builds an invalid materialization expression (QueryingEnumerable not assignable) EF-X532
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_with_aggregate_function_works_1(async), "Expression of type");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_2(bool async)
    {
        // Fails: GroupBy over all columns builds an invalid materialization expression (QueryingEnumerable not assignable) EF-X532
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_with_aggregate_function_works_2(async), "Expression of type");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_3(bool async)
    {
        // Fails: GroupBy over all columns builds an invalid materialization expression (QueryingEnumerable not assignable) EF-X532
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_with_aggregate_function_works_3(async), "Expression of type");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_4(bool async)
    {
        // Fails: GroupBy over all columns builds an invalid materialization expression (QueryingEnumerable not assignable) EF-X532
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_with_aggregate_function_works_4(async), "Expression of type");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_5(bool async)
    {
        // Fails: GroupBy over all columns builds an invalid materialization expression (QueryingEnumerable not assignable) EF-X532
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_with_aggregate_function_works_5(async), "Expression of type");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_6(bool async)
    {
        // Fails: GroupBy over all columns builds an invalid materialization expression (QueryingEnumerable not assignable) EF-X532
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_with_aggregate_function_works_6(async), "Expression of type");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_7(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_with_aggregate_function_works_7(async), "could not be translated");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_8(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_with_aggregate_function_works_8(async), "could not be translated");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_9(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_with_aggregate_function_works_9(async), "could not be translated");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_10(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Grouping_by_all_columns_with_aggregate_function_works_10(async), "could not be translated");
    }

    public override async Task GroupBy_Simple_1_from_LINQ_101(bool async)
    {
        await base.GroupBy_Simple_1_from_LINQ_101(async);
    }

    public override async Task GroupBy_Simple_2_from_LINQ_101(bool async)
    {
        await base.GroupBy_Simple_2_from_LINQ_101(async);
    }

    public override async Task GroupBy_Simple_3_from_LINQ_101(bool async)
    {
        await base.GroupBy_Simple_3_from_LINQ_101(async);
    }

    public override async Task GroupBy_Nested_from_LINQ_101(bool async)
    {
        await base.GroupBy_Nested_from_LINQ_101(async);
    }

    public override async Task Any_Grouped_from_LINQ_101(bool async)
    {
        await base.Any_Grouped_from_LINQ_101(async);
    }

    public override async Task All_Grouped_from_LINQ_101(bool async)
    {
        await base.All_Grouped_from_LINQ_101(async);
    }

    public override async Task Count_Grouped_from_LINQ_101(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Count_Grouped_from_LINQ_101(async), "could not be translated");
    }

    public override async Task LongCount_Grouped_from_LINQ_101(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.LongCount_Grouped_from_LINQ_101(async), "could not be translated");
    }

    public override async Task Sum_Grouped_from_LINQ_101(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Sum_Grouped_from_LINQ_101(async), "could not be translated");
    }

    public override async Task Min_Grouped_from_LINQ_101(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Min_Grouped_from_LINQ_101(async), "could not be translated");
    }

    public override async Task Min_Elements_from_LINQ_101(bool async)
    {
        await base.Min_Elements_from_LINQ_101(async);
    }

    public override async Task Max_Grouped_from_LINQ_101(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Max_Grouped_from_LINQ_101(async), "could not be translated");
    }

    public override async Task Max_Elements_from_LINQ_101(bool async)
    {
        await base.Max_Elements_from_LINQ_101(async);
    }

    public override async Task Average_Grouped_from_LINQ_101(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Average_Grouped_from_LINQ_101(async), "could not be translated");
    }

    public override async Task Group_Join_from_LINQ_101(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Group_Join_from_LINQ_101(async), "could not be translated");
    }

#if EF10
    public override async Task Cross_Join_with_Group_Join_from_LINQ_101(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Cross_Join_with_Group_Join_from_LINQ_101(async), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Left_Outer_Join_with_Group_Join_from_LINQ_101(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Left_Outer_Join_with_Group_Join_from_LINQ_101(async), "Expression not supported");
    }
#endif

    public override async Task Whats_new_2021_sample_1(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_1(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_2(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_2(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_3(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_3(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_4(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_4(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_5(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_5(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_6(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_6(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_7(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_7(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_8(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_8(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_9(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_9(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_10(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_10(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_11(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_11(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_12(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_12(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_13(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_13(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_14(bool async)
    {
        await base.Whats_new_2021_sample_14(async);
    }

    public override async Task Whats_new_2021_sample_15(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Whats_new_2021_sample_15(async), "could not be translated");
    }

    public override async Task Whats_new_2021_sample_16(bool async)
    {
        await base.Whats_new_2021_sample_16(async);
    }

#if EF8 || EF9
    public override async Task Cross_Join_with_Group_Join_from_LINQ_101(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Cross_Join_with_Group_Join_from_LINQ_101(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Left_Outer_Join_with_Group_Join_from_LINQ_101(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.Left_Outer_Join_with_Group_Join_from_LINQ_101(async), "could not be translated");
    }
#endif

#if EF8
    public override async Task GroupBy_is_optimized_when_filerting_and_projecting_anonymous_type_with_group_key_and_function_aggregate(bool async)
    {
        // Fails: GroupBy issue EF-149
        await SpecFails.ThrowsAsync(() => base.GroupBy_is_optimized_when_filerting_and_projecting_anonymous_type_with_group_key_and_function_aggregate(async), "could not be translated");
    }
#endif

    public class Ef6GroupByMongoFixture : Ef6GroupByFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("Ef6GroupBy");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
        }

        public override async Task InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server);

            await base.InitializeAsync();
        }
    }
}
