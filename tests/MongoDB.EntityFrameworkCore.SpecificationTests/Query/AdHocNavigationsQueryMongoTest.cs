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

public class AdHocNavigationsQueryMongoTest : AdHocNavigationsQueryTestBase
{
#if EF10 || EF11
    public AdHocNavigationsQueryMongoTest(NonSharedFixture fixture)
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

#if EF11
    // Fails: new in the EF11 upstream package; asserts any failure until triaged. EF-X605
    public override Task Filtered_collection_through_optional_navigation_does_not_match_on_null_keys(bool async)
        => SpecFails.ThrowsAsync(() => base.Filtered_collection_through_optional_navigation_does_not_match_on_null_keys(async));
#endif

    public override async Task ThenInclude_with_interface_navigations()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.ThenInclude_with_interface_navigations(), "cannot be tracked because another instance");
    }

    public override async Task Customer_collections_materialize_properly()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Customer_collections_materialize_properly(), "cannot be tracked because another instance");
    }

    public override async Task Reference_include_on_derived_type_with_sibling_works()
    {
        // Fails: Model validation: cannot determine the relationship represented by a navigation EF-X545
        await SpecFails.ThrowsAsync(() => base.Reference_include_on_derived_type_with_sibling_works(), "Unable to determine the relationship");
    }

    public override async Task Include_collection_optional_reference_collection()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Include_collection_optional_reference_collection(), "cannot be tracked because another instance");
    }

    public override async Task Include_with_order_by_on_interface_key()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Include_with_order_by_on_interface_key(), "cannot be tracked because another instance");
    }

    public override async Task Collection_without_setter_materialized_correctly()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Collection_without_setter_materialized_correctly(), "cannot be tracked because another instance");
    }

    public override async Task Include_collection_works_when_defined_on_intermediate_type()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Include_collection_works_when_defined_on_intermediate_type(), "cannot be tracked because another instance");
    }

    public override async Task Let_multiple_references_with_reference_to_outer()
    {
        // Fails: Model validation: cannot determine the relationship/owner represented by a navigation EF-X545
        await SpecFails.ThrowsAsync(() => base.Let_multiple_references_with_reference_to_outer(), "InvalidOperationException");
    }

    public override async Task Include_collection_with_OfType_base()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Include_collection_with_OfType_base(), "cannot be tracked because another instance");
    }

    public override async Task Correlated_collection_correctly_associates_entities_with_byte_array_keys()
    {
        // Fails: Single-property primary key with a name other than Id must be mapped to '_id' (test model does not) EF-X547
        await SpecFails.ThrowsAsync(() => base.Correlated_collection_correctly_associates_entities_with_byte_array_keys(), "must be mapped to element");
    }

    public override async Task Can_ignore_invalid_include_path_error()
    {
        await base.Can_ignore_invalid_include_path_error();
    }

    public override async Task SelectMany_and_collection_in_projection_in_FirstOrDefault()
    {
        // Fails: Model validation rejects the relationship (owned/nested owned or ambiguous FK) EF-X523
        await SpecFails.ThrowsAsync(() => base.SelectMany_and_collection_in_projection_in_FirstOrDefault(), "The relationship from");
    }

    public override async Task Using_explicit_interface_implementation_as_navigation_works()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Using_explicit_interface_implementation_as_navigation_works(), "could not be translated");
    }

    public override async Task Cycles_in_auto_include()
    {
        // Fails: Model validation: cannot determine the relationship/owner represented by a navigation EF-X545
        await SpecFails.ThrowsAsync(() => base.Cycles_in_auto_include(), "InvalidOperationException");
    }

    public override async Task Walking_back_include_tree_is_not_allowed_1()
    {
        // Fails: Model validation: an owned entity type used in multiple navigations is rejected EF-X544
        await SpecFails.ThrowsAsync(() => base.Walking_back_include_tree_is_not_allowed_1(), "is used in multiple navigations");
    }

    public override async Task Walking_back_include_tree_is_not_allowed_2()
    {
        // Fails: Model validation: an owned entity type used in multiple navigations is rejected EF-X544
        await SpecFails.ThrowsAsync(() => base.Walking_back_include_tree_is_not_allowed_2(), "is used in multiple navigations");
    }

    public override async Task Walking_back_include_tree_is_not_allowed_3()
    {
        // Fails: Model validation: an owned entity type used in multiple navigations is rejected EF-X544
        await SpecFails.ThrowsAsync(() => base.Walking_back_include_tree_is_not_allowed_3(), "is used in multiple navigations");
    }

    public override async Task Walking_back_include_tree_is_not_allowed_4()
    {
        // Fails: Model validation: an owned entity type used in multiple navigations is rejected EF-X544
        await SpecFails.ThrowsAsync(() => base.Walking_back_include_tree_is_not_allowed_4(), "is used in multiple navigations");
    }

    public override async Task Projection_with_multiple_includes_and_subquery_with_set_operation()
    {
        // Fails: Model validation: cannot determine the relationship represented by a navigation EF-X545
        await SpecFails.ThrowsAsync(() => base.Projection_with_multiple_includes_and_subquery_with_set_operation(), "Unable to determine the relationship");
    }

    public override async Task Count_member_over_IReadOnlyCollection_works(bool async)
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.Count_member_over_IReadOnlyCollection_works(async), "cannot be tracked because another instance");
    }

    private ITestStoreFactory? _testStoreFactory;

#if EF11
    protected override ITestStoreFactory NonSharedTestStoreFactory
#else
    protected override ITestStoreFactory TestStoreFactory
#endif
        => _testStoreFactory!;

#if EF11
    protected override string NonSharedStoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocNav");
#else
    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocNav");
#endif

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);

        await base.InitializeAsync();
    }
}

#endif
