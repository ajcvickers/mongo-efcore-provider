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

public class OwnedEntityQueryMongoTest : OwnedEntityQueryTestBase
{
#if EF10 || EF11
    public OwnedEntityQueryMongoTest(NonSharedFixture fixture)
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

#if EF9 || (EF10 || EF11)
    public override async Task Include_collection_for_entity_with_owned_type_works()
    {
        // Fails: Owned collection items with a generated int key all get key 0 (no value generation), so tracking fails EF-X522
        await SpecFails.ThrowsAsync(() => base.Include_collection_for_entity_with_owned_type_works(), "cannot be tracked because another instance");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Multilevel_owned_entities_determine_correct_nullability()
    {
        await base.Multilevel_owned_entities_determine_correct_nullability();
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Correlated_subquery_with_owned_navigation_being_compared_to_null_works()
    {
        // Fails: Owned collection items with a generated int key all get key 0 (no value generation), so tracking fails EF-X522
        await SpecFails.ThrowsAsync(() => base.Correlated_subquery_with_owned_navigation_being_compared_to_null_works(), "cannot be tracked because another instance");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Owned_entity_multiple_level_in_aggregate()
    {
        // Fails: Deeply nested owned types: model validation rejects the relationship EF-X523
        await SpecFails.ThrowsAsync(() => base.Owned_entity_multiple_level_in_aggregate(), "The relationship from");
    }
#endif

    public override async Task Projecting_correlated_collection_property_for_owned_entity(bool async)
    {
        // Fails: Owned collection items with a generated int key all get key 0 (no value generation), so tracking fails EF-X522
        await SpecFails.ThrowsAsync(() => base.Projecting_correlated_collection_property_for_owned_entity(async), "cannot be tracked because another instance");
    }

#if EF9 || (EF10 || EF11)
    public override async Task Accessing_scalar_property_in_derived_type_projection_does_not_load_owned_navigations()
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Accessing_scalar_property_in_derived_type_projection_does_not_load_owned_navigations(), "could not be translated");
    }
#endif

    public override async Task Multiple_single_result_in_projection_containing_owned_types(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Multiple_single_result_in_projection_containing_owned_types(async), "could not be translated");
    }

#if EF9 || (EF10 || EF11)
    public override async Task Can_auto_include_navigation_from_model()
    {
        // Fails: Owned collection items with a generated int key all get key 0 (no value generation), so tracking fails EF-X522
        await SpecFails.ThrowsAsync(() => base.Can_auto_include_navigation_from_model(), "cannot be tracked because another instance");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Nested_owned_required_dependents_are_materialized()
    {
        await base.Nested_owned_required_dependents_are_materialized();
    }
#endif

    public override async Task OwnsMany_correlated_projection(bool async)
    {
        // Fails: Projecting an owned entity without its owner in a tracking query EF-X517
        await SpecFails.ThrowsAsync(() => base.OwnsMany_correlated_projection(async), "owned entity without a corresponding owner");
    }

    public override async Task Projecting_owned_collection_and_aggregate(bool async)
    {
        // Fails: Owned-collection operators (SelectMany/GroupBy/equality/...) not translated EF-X513
        await SpecFails.ThrowsAsync(() => base.Projecting_owned_collection_and_aggregate(async), "could not be translated");
    }

    private ITestStoreFactory? _testStoreFactory;

#if EF11
    protected override ITestStoreFactory NonSharedTestStoreFactory
#else
    protected override ITestStoreFactory TestStoreFactory
#endif
        => _testStoreFactory!;

#if EF11
    protected override string NonSharedStoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("OwnedEntityQuery");
#else
    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("OwnedEntityQuery");
#endif

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

        await base.InitializeAsync();
    }
}
