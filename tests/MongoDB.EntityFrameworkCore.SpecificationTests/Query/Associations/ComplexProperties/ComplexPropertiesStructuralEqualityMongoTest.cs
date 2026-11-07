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
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Two_associates());
#else
        await base.Two_associates();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Two_nested_associates()
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Two_nested_associates());
#else
        await base.Two_nested_associates();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Not_equals()
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Not_equals());
#else
        await base.Not_equals();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Associate_with_inline_null()
    {
        await base.Associate_with_inline_null();

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Associate_with_parameter_null()
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Associate_with_parameter_null());
            return;
        }

        await base.Associate_with_parameter_null();

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Nested_associate_with_inline_null()
    {
        await base.Nested_associate_with_inline_null();

        AssertMql(
"""
RootEntity.?
""");
    }

    public override async Task Nested_associate_with_inline()
    {
#if EF10
        // Fails: Whole-value equality over a type with collection leaves is refused #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Nested_associate_with_inline());
#else
        await base.Nested_associate_with_inline();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Nested_associate_with_parameter()
    {
#if EF10
        // Fails: Whole-value equality over a type with collection leaves is refused #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Nested_associate_with_parameter());
#else
        await base.Nested_associate_with_parameter();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Two_nested_collections()
    {
#if EF10
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Two_nested_collections());
#else
        await base.Two_nested_collections();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Nested_collection_with_inline()
    {
#if EF10
        // Fails: Whole-value equality over a type with collection leaves is refused #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Nested_collection_with_inline());
#else
        await base.Nested_collection_with_inline();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Nested_collection_with_parameter()
    {
#if EF10
        // Fails: Whole-value equality over a type with collection leaves is refused #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Nested_collection_with_parameter());
#else
        await base.Nested_collection_with_parameter();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Contains_with_inline()
    {
#if EF10
        // Fails: Whole-value equality over a type with collection leaves is refused #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Contains_with_inline());
#else
        await base.Contains_with_inline();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Contains_with_parameter()
    {
#if EF10
        // Fails: Whole-value equality over a type with collection leaves is refused #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Contains_with_parameter());
#else
        await base.Contains_with_parameter();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Contains_with_operators_composed_on_the_collection()
    {
#if EF10
        // Fails: Not supported yet #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Contains_with_operators_composed_on_the_collection());
#else
        await base.Contains_with_operators_composed_on_the_collection();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Contains_with_nested_and_composed_operators()
    {
#if EF10
        // Fails: Not supported yet #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Contains_with_nested_and_composed_operators());
#else
        await base.Contains_with_nested_and_composed_operators();

        AssertMql(
"""
RootEntity.?
""");
    
#endif
    }

    public override async Task Nullable_value_type_with_null()
    {
        await base.Nullable_value_type_with_null();

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
