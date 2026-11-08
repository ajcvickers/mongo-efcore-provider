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

public class NavigationsIncludeMongoTest(NavigationsMongoFixture fixture)
    : NavigationsIncludeTestBase<NavigationsMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Include_required(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_required(async));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Include_optional(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_optional(async));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Include_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection(async));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Include_required_optional_and_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_required_optional_and_collection(async));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Include_nested(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_nested(async));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Include_nested_optional(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_nested_optional(async));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Include_nested_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_nested_collection(async));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Include_nested_collection_on_optional(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_nested_collection_on_optional(async));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Include_nested_collection_on_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_nested_collection_on_collection(async));
}
#endif
