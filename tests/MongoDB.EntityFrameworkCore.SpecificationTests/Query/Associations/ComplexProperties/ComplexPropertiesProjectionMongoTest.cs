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
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Select_root(queryTrackingBehavior))).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_scalar_property_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_scalar_property_on_required_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_property_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_property_on_optional_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_value_type_property_on_null_associate_throws(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: throws the driver ExpressionNotSupportedException, not EF InvalidOperationException EF-168
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => base.Select_value_type_property_on_null_associate_throws(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_nullable_value_type_property_on_null_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_nullable_value_type_property_on_null_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_optional_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_required_nested_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_required_nested_on_required_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_optional_nested_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_optional_nested_on_required_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_required_nested_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_required_nested_on_optional_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_optional_nested_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_optional_nested_on_optional_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_required_associate_via_optional_navigation(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property data not round-tripped EF-168
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => base.Select_required_associate_via_optional_navigation(queryTrackingBehavior));

        AssertMql(
"""
RootReferencingEntity.?
""");
    }

    public override async Task Select_unmapped_associate_scalar_property(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_unmapped_associate_scalar_property(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_untranslatable_method_on_associate_scalar_property(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_untranslatable_method_on_associate_scalar_property(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_associate_collection(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_associate_collection(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_nested_collection_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_nested_collection_on_required_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_nested_collection_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_nested_collection_on_optional_associate(queryTrackingBehavior));

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task SelectMany_associate_collection(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.SelectMany_associate_collection(queryTrackingBehavior));

        AssertMql();
    }

    public override async Task SelectMany_nested_collection_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.SelectMany_nested_collection_on_required_associate(queryTrackingBehavior));

        AssertMql();
    }

    public override async Task SelectMany_nested_collection_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.SelectMany_nested_collection_on_optional_associate(queryTrackingBehavior));

        AssertMql();
    }

    public override async Task Select_root_duplicated(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Select_root_duplicated(queryTrackingBehavior))).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Select_subquery_required_related_FirstOrDefault(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_subquery_required_related_FirstOrDefault(queryTrackingBehavior));

        AssertMql();
    }

    public override async Task Select_subquery_optional_related_FirstOrDefault(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_subquery_optional_related_FirstOrDefault(queryTrackingBehavior));

        AssertMql();
    }

    public override async Task Select_subquery_FirstOrDefault_complex_collection(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_subquery_FirstOrDefault_complex_collection(queryTrackingBehavior));

        AssertMql();
    }

    public override async Task Select_root_with_value_types(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Select_root_with_value_types(queryTrackingBehavior))).Message);

        AssertMql(
"""
ValueRootEntity.?
""");
    }

    public override async Task Select_non_nullable_value_type(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_non_nullable_value_type(queryTrackingBehavior));

        AssertMql(
"""
ValueRootEntity.?
""");
    }

    public override async Task Select_nullable_value_type(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_nullable_value_type(queryTrackingBehavior));

        AssertMql(
"""
ValueRootEntity.?
""");
    }

    public override async Task Select_nullable_value_type_with_Value(QueryTrackingBehavior queryTrackingBehavior)
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_nullable_value_type_with_Value(queryTrackingBehavior));

        AssertMql(
"""
ValueRootEntity.?
""");
    }

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    protected new static Task AssertTranslationFailed(Func<Task> query)
        => MongoSpecTestHelpers.AssertNativeTranslationFailedAsync(query);
}

#endif
