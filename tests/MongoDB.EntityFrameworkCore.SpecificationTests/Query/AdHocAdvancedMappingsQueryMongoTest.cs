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

public class AdHocAdvancedMappingsQueryMongoTest : AdHocAdvancedMappingsQueryTestBase
{
#if EF10 || EF11
    public AdHocAdvancedMappingsQueryMongoTest(NonSharedFixture fixture)
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

#if EF9 || EF10
    public override async Task Setting_IsUnicode_generates_unicode_literal_in_SQL()
    {
        await base.Setting_IsUnicode_generates_unicode_literal_in_SQL();
    }
#endif

#if EF9 || EF10
    public override async Task Projecting_correlated_collection_along_with_non_mapped_property()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Projecting_correlated_collection_along_with_non_mapped_property(), "cannot be tracked because another instance");
    }
#endif

#if EF9 || EF10
    public override async Task Projection_failing_with_EnumToStringConverter()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Projection_failing_with_EnumToStringConverter(), "cannot be tracked because another instance");
    }
#endif

#if EF10
    public override async Task Expression_tree_constructed_via_interface_works()
    {
        await base.Expression_tree_constructed_via_interface_works();
    }
#endif

#if EF9 || EF10
    public override async Task Double_convert_interface_created_expression_tree()
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Double_convert_interface_created_expression_tree(), "Unsupported cross-DbSet query");
    }
#endif

#if EF9 || EF10
    public override async Task Casts_are_removed_from_expression_tree_when_redundant()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Casts_are_removed_from_expression_tree_when_redundant(), "cannot be tracked because another instance");
    }
#endif

#if EF9 || EF10
    public override async Task Can_query_hierarchy_with_non_nullable_property_on_derived()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Can_query_hierarchy_with_non_nullable_property_on_derived(), "cannot be tracked because another instance");
    }
#endif

#if EF9 || EF10
    public override async Task Query_generates_correct_datetime2_parameter_definition(int? fractionalSeconds, string postfix)
    {
        await base.Query_generates_correct_datetime2_parameter_definition(fractionalSeconds, postfix);
    }
#endif

#if EF9 || EF10
    public override async Task Query_generates_correct_datetimeoffset_parameter_definition(int? fractionalSeconds, string postfix)
    {
        await base.Query_generates_correct_datetimeoffset_parameter_definition(fractionalSeconds, postfix);
    }
#endif

#if EF9 || EF10
    public override async Task Query_generates_correct_timespan_parameter_definition(int? fractionalSeconds, string postfix)
    {
        await base.Query_generates_correct_timespan_parameter_definition(fractionalSeconds, postfix);
    }
#endif

#if EF9 || EF10
    public override async Task Hierarchy_query_with_abstract_type_sibling(bool async)
    {
        await base.Hierarchy_query_with_abstract_type_sibling(async);
    }
#endif

#if EF9 || EF10
    public override async Task Projecting_property_with_converter_with_closure(bool async)
    {
        await base.Projecting_property_with_converter_with_closure(async);
    }
#endif

#if EF9 || EF10
    public override async Task Projecting_expression_with_converter_with_closure(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_expression_with_converter_with_closure(async), "could not be translated");
    }
#endif

#if EF9 || EF10
    public override async Task Projecting_property_with_converter_without_closure(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_property_with_converter_without_closure(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Expression_tree_constructed_via_interface_works()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Expression_tree_constructed_via_interface_works(), "could not be translated");
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
    protected override string NonSharedStoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocAdv");
#else
    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocAdv");
#endif

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);

        await base.InitializeAsync();
    }
}
