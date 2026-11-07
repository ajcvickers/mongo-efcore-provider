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

public class AdHocQueryFiltersQueryMongoTest : AdHocQueryFiltersQueryTestBase
{
#if EF10 || EF11
    public AdHocQueryFiltersQueryMongoTest(NonSharedFixture fixture)
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

#if EF10
    public override async Task Named_query_filters()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Named_query_filters(), "cannot be tracked because another instance");
    }
#endif

#if EF10
    public override async Task Named_query_filters_ignore_some()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Named_query_filters_ignore_some(), "cannot be tracked because another instance");
    }
#endif

#if EF10
    public override async Task Named_query_filters_caching()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Named_query_filters_caching(), "cannot be tracked because another instance");
    }
#endif

#if EF10
    public override async Task Named_query_filters_ignore_all()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Named_query_filters_ignore_all(), "cannot be tracked because another instance");
    }
#endif

#if EF10
    public override async Task Named_query_filters_anonymous()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Named_query_filters_anonymous(), "cannot be tracked because another instance");
    }
#endif

#if EF10
    public override async Task Named_query_filters_anonymous_ignore()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Named_query_filters_anonymous_ignore(), "cannot be tracked because another instance");
    }
#endif

#if EF10
    public override async Task Named_query_filters_combined()
    {
        await base.Named_query_filters_combined();
    }
#endif

#if EF10
    public override async Task Named_query_filters_overwriting()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Named_query_filters_overwriting(), "cannot be tracked because another instance");
    }
#endif

#if EF10
    public override async Task Named_query_filters_removing()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Named_query_filters_removing(), "cannot be tracked because another instance");
    }
#endif

    public override async Task Query_filter_with_contains_evaluates_correctly()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Query_filter_with_contains_evaluates_correctly(), "cannot be tracked because another instance");
    }

    public override async Task MultiContext_query_filter_test()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.MultiContext_query_filter_test(), "cannot be tracked because another instance");
    }

    public override async Task Weak_entities_with_query_filter_subquery_flattening()
    {
        await base.Weak_entities_with_query_filter_subquery_flattening();
    }

#if EF10
    public override async Task Query_filter_with_pk_fk_optimization()
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Query_filter_with_pk_fk_optimization(), "Expression not supported");
    }
#endif

    public override async Task Self_reference_in_query_filter_works()
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Self_reference_in_query_filter_works(), "Expression not supported");
    }

    public override async Task Invoke_inside_query_filter_gets_correctly_evaluated_during_translation()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Invoke_inside_query_filter_gets_correctly_evaluated_during_translation(), "cannot be tracked because another instance");
    }

    public override async Task Query_filter_with_null_constant()
    {
        await base.Query_filter_with_null_constant();
    }

#if EF10
    public override async Task IsDeleted_query_filter_with_conversion_to_int_works(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.IsDeleted_query_filter_with_conversion_to_int_works(async), "Expression not supported");
    }
#endif

    public override async Task Group_by_multiple_aggregate_joining_different_tables(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Group_by_multiple_aggregate_joining_different_tables(async), "could not be translated");
    }

    public override async Task Group_by_multiple_aggregate_joining_different_tables_with_query_filter(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Group_by_multiple_aggregate_joining_different_tables_with_query_filter(async), "could not be translated");
    }

    public override async Task Query_filter_with_context_accessor_with_constant(bool async)
    {
        await base.Query_filter_with_context_accessor_with_constant(async);
    }

#if EF10
    public override async Task Query_filter_with_primary_constructor_parameter()
    {
        await base.Query_filter_with_primary_constructor_parameter();
    }
#endif

#if EF9
    public override async Task Query_filter_with_pk_fk_optimization()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Query_filter_with_pk_fk_optimization(), "could not be translated");
    }
#endif

#if EF9
    public override async Task GroupJoin_SelectMany_gets_flattened()
    {
        // Fails: Single-property primary key with a name other than Id must be mapped to '_id' (test model does not) EF-X547
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_gets_flattened(), "must be mapped to element");
    }
#endif

#if EF9
    public override async Task IsDeleted_query_filter_with_conversion_to_int_works(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.IsDeleted_query_filter_with_conversion_to_int_works(async), "could not be translated");
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
    protected override string NonSharedStoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocQF");
#else
    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocQF");
#endif

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);

        await base.InitializeAsync();
    }
}

#endif
