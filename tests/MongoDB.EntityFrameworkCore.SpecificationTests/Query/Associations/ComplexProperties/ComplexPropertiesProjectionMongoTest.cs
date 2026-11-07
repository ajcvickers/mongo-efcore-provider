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

// The Associations/ComplexProperties test bases only exist in EF10.
#if EF10

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Associations.ComplexProperties;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Associations.ComplexProperties;

public class ComplexPropertiesProjectionMongoTest : ComplexPropertiesProjectionTestBase<ComplexPropertiesMongoFixture>
{
    public ComplexPropertiesProjectionMongoTest(ComplexPropertiesMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Select_root(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_root(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_scalar_property_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_scalar_property_on_required_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_property_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_property_on_optional_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_value_type_property_on_null_associate_throws(QueryTrackingBehavior queryTrackingBehavior)
    {
#if EF10
        // Fails: Throws NullReferenceException where InvalidOperationException is expected #41
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Select_value_type_property_on_null_associate_throws(queryTrackingBehavior), typeof(global::Xunit.Sdk.ThrowsException));
#else
        await base.Select_value_type_property_on_null_associate_throws(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Select_nullable_value_type_property_on_null_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_nullable_value_type_property_on_null_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_optional_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_required_nested_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_required_nested_on_required_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_optional_nested_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_optional_nested_on_required_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_required_nested_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_required_nested_on_optional_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_optional_nested_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_optional_nested_on_optional_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_required_associate_via_optional_navigation(QueryTrackingBehavior queryTrackingBehavior)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Select_required_associate_via_optional_navigation(queryTrackingBehavior));
            return;
        }

        await base.Select_required_associate_via_optional_navigation(queryTrackingBehavior);

        AssertMql(
"""
RootReferencingEntity.?
""");
    }

    public override async Task Select_unmapped_associate_scalar_property(QueryTrackingBehavior queryTrackingBehavior)
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Select_unmapped_associate_scalar_property(queryTrackingBehavior));
#else
        await base.Select_unmapped_associate_scalar_property(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Select_untranslatable_method_on_associate_scalar_property(QueryTrackingBehavior queryTrackingBehavior)
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Select_untranslatable_method_on_associate_scalar_property(queryTrackingBehavior));
#else
        await base.Select_untranslatable_method_on_associate_scalar_property(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Select_associate_collection(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_associate_collection(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_nested_collection_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_nested_collection_on_required_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_nested_collection_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_nested_collection_on_optional_associate(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task SelectMany_associate_collection(QueryTrackingBehavior queryTrackingBehavior)
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.SelectMany_associate_collection(queryTrackingBehavior));
#else
        await base.SelectMany_associate_collection(queryTrackingBehavior);

        AssertMql();
    
#endif
    }

    public override async Task SelectMany_nested_collection_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.SelectMany_nested_collection_on_required_associate(queryTrackingBehavior));
#else
        await base.SelectMany_nested_collection_on_required_associate(queryTrackingBehavior);

        AssertMql();
    
#endif
    }

    public override async Task SelectMany_nested_collection_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.SelectMany_nested_collection_on_optional_associate(queryTrackingBehavior));
#else
        await base.SelectMany_nested_collection_on_optional_associate(queryTrackingBehavior);

        AssertMql();
    
#endif
    }

    public override async Task Select_root_duplicated(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_root_duplicated(queryTrackingBehavior);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_subquery_required_related_FirstOrDefault(QueryTrackingBehavior queryTrackingBehavior)
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Select_subquery_required_related_FirstOrDefault(queryTrackingBehavior));
#else
        await base.Select_subquery_required_related_FirstOrDefault(queryTrackingBehavior);

        AssertMql();
    
#endif
    }

    public override async Task Select_subquery_optional_related_FirstOrDefault(QueryTrackingBehavior queryTrackingBehavior)
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Select_subquery_optional_related_FirstOrDefault(queryTrackingBehavior));
#else
        await base.Select_subquery_optional_related_FirstOrDefault(queryTrackingBehavior);

        AssertMql();
    
#endif
    }

    public override async Task Select_subquery_FirstOrDefault_complex_collection(QueryTrackingBehavior queryTrackingBehavior)
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Select_subquery_FirstOrDefault_complex_collection(queryTrackingBehavior));
#else
        await base.Select_subquery_FirstOrDefault_complex_collection(queryTrackingBehavior);

        AssertMql();
    
#endif
    }

    public override async Task Select_root_with_value_types(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_root_with_value_types(queryTrackingBehavior);

        AssertMql(
"""
ValueRootEntity.?
""");
    }

    public override async Task Select_non_nullable_value_type(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_non_nullable_value_type(queryTrackingBehavior);

        AssertMql(
"""
ValueRootEntity.?
""");
    }

    public override async Task Select_nullable_value_type(QueryTrackingBehavior queryTrackingBehavior)
    {
        await base.Select_nullable_value_type(queryTrackingBehavior);

        AssertMql(
"""
ValueRootEntity.?
""");
    }

    public override async Task Select_nullable_value_type_with_Value(QueryTrackingBehavior queryTrackingBehavior)
    {
#if EF10
        // Fails: Not supported yet #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Select_nullable_value_type_with_Value(queryTrackingBehavior));
#else
        await base.Select_nullable_value_type_with_Value(queryTrackingBehavior);

        AssertMql(
"""
ValueRootEntity.?
""");
    
#endif
    }

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    protected new static Task AssertTranslationFailed(Func<Task> query)
        => MongoSpecTestHelpers.AssertNativeTranslationFailedAsync(query);
}

#endif
