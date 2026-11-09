/* Copyright 2025-present MongoDB Inc.
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

#if !EF8 && !EF9
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Associations.OwnedNavigations;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Associations;

public class OwnedNavigationsStructuralEqualityMongoTest(OwnedNavigationsMongoFixture fixture)
    : OwnedNavigationsStructuralEqualityTestBase<OwnedNavigationsMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Equality between owned associates: driver ExpressionNotSupportedException EF-X865
    public override Task Two_associates()
        => SpecFails.ThrowsAsync(() => base.Two_associates());

    // Fails: Equality between owned associates: driver ExpressionNotSupportedException EF-X865
    public override Task Two_nested_associates()
        => SpecFails.ThrowsAsync(() => base.Two_nested_associates());

    // Fails: Equality between owned associates: driver ExpressionNotSupportedException EF-X865
    public override Task Not_equals()
        => SpecFails.ThrowsAsync(() => base.Not_equals());

    public override Task Associate_with_inline_null()
        => base.Associate_with_inline_null();

    public override Task Associate_with_parameter_null()
        => base.Associate_with_parameter_null();

    public override Task Nested_associate_with_inline_null()
        => base.Nested_associate_with_inline_null();

    // Fails: Owned associate comparison with inline/parameter value does not throw as expected EF-X866
    public override Task Nested_associate_with_inline()
        => SpecFails.ThrowsAsync(() => base.Nested_associate_with_inline());

    // Fails: Owned associate comparison with inline/parameter value does not throw as expected EF-X866
    public override Task Nested_associate_with_parameter()
        => SpecFails.ThrowsAsync(() => base.Nested_associate_with_parameter());

    public override Task Two_nested_collections()
        => base.Two_nested_collections();

    // Fails: Different exception type than the relational providers throw (NullReference / ExpressionNotSupported / NotSupported) EF-X853
    public override Task Nested_collection_with_inline()
        => SpecFails.ThrowsAsync(() => base.Nested_collection_with_inline());

    // Fails: Different exception type than the relational providers throw (NullReference / ExpressionNotSupported / NotSupported) EF-X853
    public override Task Nested_collection_with_parameter()
        => SpecFails.ThrowsAsync(() => base.Nested_collection_with_parameter());

    public override Task Contains_with_inline()
        => base.Contains_with_inline();

    public override Task Contains_with_parameter()
        => base.Contains_with_parameter();

    public override Task Contains_with_operators_composed_on_the_collection()
        => base.Contains_with_operators_composed_on_the_collection();

    public override Task Contains_with_nested_and_composed_operators()
        => base.Contains_with_nested_and_composed_operators();
}
#endif
