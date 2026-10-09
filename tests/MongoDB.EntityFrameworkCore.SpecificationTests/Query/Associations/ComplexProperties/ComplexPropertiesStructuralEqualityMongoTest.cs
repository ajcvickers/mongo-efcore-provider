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

public class ComplexPropertiesStructuralEqualityMongoTest : ComplexPropertiesStructuralEqualityTestBase<ComplexPropertiesMongoFixture>
{
    public ComplexPropertiesStructuralEqualityMongoTest(ComplexPropertiesMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Two_associates()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Two_associates());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Two_nested_associates()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Two_nested_associates());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Not_equals()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Not_equals());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Associate_with_inline_null()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Associate_with_inline_null());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Associate_with_parameter_null()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Associate_with_parameter_null());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Nested_associate_with_inline_null()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Nested_associate_with_inline_null());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Nested_associate_with_inline()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Nested_associate_with_inline());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Nested_associate_with_parameter()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Nested_associate_with_parameter());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Two_nested_collections()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Two_nested_collections());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Nested_collection_with_inline()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Nested_collection_with_inline());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Nested_collection_with_parameter()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Nested_collection_with_parameter());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Contains_with_inline()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Contains_with_inline());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Contains_with_parameter()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Contains_with_parameter());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Contains_with_operators_composed_on_the_collection()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Contains_with_operators_composed_on_the_collection());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Contains_with_nested_and_composed_operators()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Contains_with_nested_and_composed_operators());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Nullable_value_type_with_null()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Nullable_value_type_with_null());

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
