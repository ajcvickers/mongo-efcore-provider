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
using Microsoft.EntityFrameworkCore.Query.Associations.Navigations;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Associations;

public class NavigationsStructuralEqualityMongoTest(NavigationsMongoFixture fixture)
    : NavigationsStructuralEqualityTestBase<NavigationsMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Two_associates()
        => SpecFails.ThrowsAsync(() => base.Two_associates());

    // Fails: WRONG RESULTS: associate/collection equality or Distinct returns the wrong number of rows EF-X867
    public override Task Two_nested_associates()
        => SpecFails.ThrowsAsync(() => base.Two_nested_associates());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Not_equals()
        => SpecFails.ThrowsAsync(() => base.Not_equals());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Associate_with_inline_null()
        => SpecFails.ThrowsAsync(() => base.Associate_with_inline_null());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Associate_with_parameter_null()
        => SpecFails.ThrowsAsync(() => base.Associate_with_parameter_null());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Nested_associate_with_inline_null()
        => SpecFails.ThrowsAsync(() => base.Nested_associate_with_inline_null());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Nested_associate_with_inline()
        => SpecFails.ThrowsAsync(() => base.Nested_associate_with_inline());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Nested_associate_with_parameter()
        => SpecFails.ThrowsAsync(() => base.Nested_associate_with_parameter());

    // Fails: WRONG RESULTS: associate/collection equality or Distinct returns the wrong number of rows EF-X867
    public override Task Two_nested_collections()
        => SpecFails.ThrowsAsync(() => base.Two_nested_collections());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Nested_collection_with_inline()
        => SpecFails.ThrowsAsync(() => base.Nested_collection_with_inline());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Nested_collection_with_parameter()
        => SpecFails.ThrowsAsync(() => base.Nested_collection_with_parameter());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Contains_with_inline()
        => SpecFails.ThrowsAsync(() => base.Contains_with_inline());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Contains_with_parameter()
        => SpecFails.ThrowsAsync(() => base.Contains_with_parameter());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Contains_with_operators_composed_on_the_collection()
        => SpecFails.ThrowsAsync(() => base.Contains_with_operators_composed_on_the_collection());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Contains_with_nested_and_composed_operators()
        => SpecFails.ThrowsAsync(() => base.Contains_with_nested_and_composed_operators());
}
#endif
