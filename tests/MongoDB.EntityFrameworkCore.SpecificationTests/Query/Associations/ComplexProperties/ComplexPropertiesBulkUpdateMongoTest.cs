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

public class ComplexPropertiesBulkUpdateMongoTest : ComplexPropertiesBulkUpdateTestBase<ComplexPropertiesMongoFixture>
{
    public ComplexPropertiesBulkUpdateMongoTest(ComplexPropertiesMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Delete_entity_with_associations()
    {
        await base.Delete_entity_with_associations();

        AssertMql();
    }

    public override async Task Delete_required_associate()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Delete_required_associate());

        AssertMql();
    }

    public override async Task Delete_optional_associate()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Delete_optional_associate());

        AssertMql();
    }

    public override async Task Update_property_inside_associate()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_property_inside_associate())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_property_inside_associate_with_special_chars()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Update_property_inside_associate_with_special_chars());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_property_inside_nested_associate()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_property_inside_nested_associate())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_property_on_projected_associate()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Update_property_on_projected_associate());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_property_on_projected_associate_with_OrderBy_Skip()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Update_property_on_projected_associate_with_OrderBy_Skip());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_associate_with_null_required_property()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_associate_with_null_required_property())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_associate_to_parameter()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_associate_to_parameter())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_nested_associate_to_parameter()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_nested_associate_to_parameter())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_associate_to_another_associate()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_associate_to_another_associate())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_nested_associate_to_another_nested_associate()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_nested_associate_to_another_nested_associate())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_associate_to_inline()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_associate_to_inline())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_associate_to_inline_with_lambda()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_associate_to_inline_with_lambda())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_nested_associate_to_inline_with_lambda()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_nested_associate_to_inline_with_lambda())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_associate_to_null()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_associate_to_null())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_associate_to_null_with_lambda()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_associate_to_null_with_lambda())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_associate_to_null_parameter()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_associate_to_null_parameter())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_required_nested_associate_to_null()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_required_nested_associate_to_null())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_collection_to_parameter()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_collection_to_parameter())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_nested_collection_to_parameter()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_nested_collection_to_parameter())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_nested_collection_to_inline_with_lambda()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_nested_collection_to_inline_with_lambda())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_collection_referencing_the_original_collection()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Update_collection_referencing_the_original_collection());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_nested_collection_to_another_nested_collection()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Update_nested_collection_to_another_nested_collection());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_inside_structural_collection()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Update_inside_structural_collection());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_primitive_collection_to_constant()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_primitive_collection_to_constant())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_primitive_collection_to_parameter()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_primitive_collection_to_parameter())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_primitive_collection_to_another_collection()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_primitive_collection_to_another_collection())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_inside_primitive_collection()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Update_inside_primitive_collection());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_multiple_properties_inside_same_associate()
    {
        // Fails: Complex Types: property elements not persisted / materialized EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Update_multiple_properties_inside_same_associate())).Message);

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_multiple_properties_inside_associates_and_on_entity_type()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Update_multiple_properties_inside_associates_and_on_entity_type());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Update_multiple_projected_associates_via_anonymous_type()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Update_multiple_projected_associates_via_anonymous_type());

        AssertMql(
"""
RootEntity.?
""");
    }

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    protected new static Task AssertTranslationFailed(Func<Task> query)
        => MongoSpecTestHelpers.AssertNativeTranslationFailedAsync(query);
}

#endif
