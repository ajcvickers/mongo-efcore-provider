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

public class QueryFilterFuncletizationMongoTest : QueryFilterFuncletizationTestBase<QueryFilterFuncletizationMongoTest.QueryFilterFuncletizationMongoFixture>
{
    public QueryFilterFuncletizationMongoTest(QueryFilterFuncletizationMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override void DbContext_property_parameter_does_not_clash_with_closure_parameter_name()
    {
        base.DbContext_property_parameter_does_not_clash_with_closure_parameter_name();
    }

    public override void DbContext_field_is_parameterized()
    {
        base.DbContext_field_is_parameterized();
    }

    public override void DbContext_property_is_parameterized()
    {
        base.DbContext_property_is_parameterized();
    }

    public override void DbContext_method_call_is_parameterized()
    {
        base.DbContext_method_call_is_parameterized();
    }

    public override void DbContext_list_is_parameterized()
    {
        // Fails: NullReferenceException for a query filter using a DbContext list parameter EF-X524
        SpecFails.Throws(() => base.DbContext_list_is_parameterized(), "NullReferenceException");
    }

    public override void DbContext_property_chain_is_parameterized()
    {
        base.DbContext_property_chain_is_parameterized();
    }

    public override void DbContext_property_method_call_is_parameterized()
    {
        base.DbContext_property_method_call_is_parameterized();
    }

    public override void DbContext_method_call_chain_is_parameterized()
    {
        base.DbContext_method_call_chain_is_parameterized();
    }

    public override void DbContext_complex_expression_is_parameterized()
    {
        base.DbContext_complex_expression_is_parameterized();
    }

    public override void DbContext_property_based_filter_does_not_short_circuit()
    {
        base.DbContext_property_based_filter_does_not_short_circuit();
    }

    public override void EntityTypeConfiguration_DbContext_field_is_parameterized()
    {
        base.EntityTypeConfiguration_DbContext_field_is_parameterized();
    }

    public override void EntityTypeConfiguration_DbContext_property_is_parameterized()
    {
        base.EntityTypeConfiguration_DbContext_property_is_parameterized();
    }

    public override void EntityTypeConfiguration_DbContext_method_call_is_parameterized()
    {
        base.EntityTypeConfiguration_DbContext_method_call_is_parameterized();
    }

    public override void EntityTypeConfiguration_DbContext_property_chain_is_parameterized()
    {
        base.EntityTypeConfiguration_DbContext_property_chain_is_parameterized();
    }

    public override void Local_method_DbContext_field_is_parameterized()
    {
        base.Local_method_DbContext_field_is_parameterized();
    }

    public override void Local_static_method_DbContext_property_is_parameterized()
    {
        base.Local_static_method_DbContext_property_is_parameterized();
    }

    public override void Remote_method_DbContext_property_method_call_is_parameterized()
    {
        base.Remote_method_DbContext_property_method_call_is_parameterized();
    }

    public override void Extension_method_DbContext_field_is_parameterized()
    {
        base.Extension_method_DbContext_field_is_parameterized();
    }

    public override void Extension_method_DbContext_property_chain_is_parameterized()
    {
        base.Extension_method_DbContext_property_chain_is_parameterized();
    }

    public override void Using_DbSet_in_filter_works()
    {
        // Fails: Cross-document navigation access issue EF-216
        SpecFails.Throws(() => base.Using_DbSet_in_filter_works(), "Unsupported cross-DbSet query");
    }

    public override void Using_Context_set_method_in_filter_works()
    {
        // Fails: Cross-document navigation access issue EF-216
        SpecFails.Throws(() => base.Using_Context_set_method_in_filter_works(), "Unsupported cross-DbSet query");
    }

    public override void Static_member_from_dbContext_is_inlined()
    {
        base.Static_member_from_dbContext_is_inlined();
    }

    public override void Static_member_from_non_dbContext_is_inlined()
    {
        base.Static_member_from_non_dbContext_is_inlined();
    }

    public override void Local_variable_from_OnModelCreating_is_inlined()
    {
        base.Local_variable_from_OnModelCreating_is_inlined();
    }

    public override void Local_variable_from_OnModelCreating_can_throw_exception()
    {
        base.Local_variable_from_OnModelCreating_can_throw_exception();
    }

    public override void Method_parameter_is_inlined()
    {
        base.Method_parameter_is_inlined();
    }

    public override void Using_multiple_context_in_filter_parametrize_only_current_context()
    {
        base.Using_multiple_context_in_filter_parametrize_only_current_context();
    }

#if EF10
    public override void Using_multiple_entities_with_filters_reuses_parameters()
    {
        // Fails: Multiple query roots issue EF-220
        SpecFails.Throws(() => base.Using_multiple_entities_with_filters_reuses_parameters(), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override void Using_multiple_entities_with_filters_reuses_parameters()
    {
        // Fails: EF8/EF9: filtered query with multiple filtered entities returns duplicate rows EF-X525
        SpecFails.Throws(() => base.Using_multiple_entities_with_filters_reuses_parameters(), "Assert.Single() Failure");
    }
#endif

    public class QueryFilterFuncletizationMongoFixture : QueryFilterFuncletizationFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("QFFunc");

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
