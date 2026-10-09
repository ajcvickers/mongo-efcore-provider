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

public class ComplexPropertiesMiscellaneousMongoTest : ComplexPropertiesMiscellaneousTestBase<ComplexPropertiesMongoFixture>
{
    public ComplexPropertiesMiscellaneousMongoTest(ComplexPropertiesMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Where_on_associate_scalar_property()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Where_on_associate_scalar_property());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Where_on_optional_associate_scalar_property()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Where_on_optional_associate_scalar_property());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Where_on_nested_associate_scalar_property()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Where_on_nested_associate_scalar_property());

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Where_property_on_non_nullable_value_type()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Where_property_on_non_nullable_value_type());

        AssertMql(
"""
ValueRootEntity.?
""");
    }

    public override async Task Where_property_on_nullable_value_type_Value()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Where_property_on_nullable_value_type_Value());

        AssertMql(
"""
ValueRootEntity.?
""");
    }

    public override async Task Where_HasValue_on_nullable_value_type()
    {
        // Fails: Complex Types: property translation not supported EF-168
        await AssertTranslationFailed(() => base.Where_HasValue_on_nullable_value_type());

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
