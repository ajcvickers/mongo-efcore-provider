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

public class NavigationsMiscellaneousMongoTest(NavigationsMongoFixture fixture)
    : NavigationsMiscellaneousTestBase<NavigationsMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Where_on_associate_scalar_property()
        => SpecFails.ThrowsAsync(() => base.Where_on_associate_scalar_property());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Where_on_optional_associate_scalar_property()
        => SpecFails.ThrowsAsync(() => base.Where_on_optional_associate_scalar_property());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Where_on_nested_associate_scalar_property()
        => SpecFails.ThrowsAsync(() => base.Where_on_nested_associate_scalar_property());
}
#endif
