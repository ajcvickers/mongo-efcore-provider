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

public class OwnedNavigationsSetOperationsMongoTest(OwnedNavigationsMongoFixture fixture)
    : OwnedNavigationsSetOperationsTestBase<OwnedNavigationsMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override Task Over_associate_collections()
        => base.Over_associate_collections();

    // Fails: Distinct/Select over an owned collection (EF.Property<List<T>>.AsQueryable()) cannot be translated EF-X858
    public override Task Over_associate_collection_projected(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Over_associate_collection_projected(queryTrackingBehavior));

    // Fails: Distinct/Select over an owned collection (EF.Property<List<T>>.AsQueryable()) cannot be translated EF-X858
    public override Task Over_assocate_collection_Select_nested_with_aggregates_projected(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Over_assocate_collection_Select_nested_with_aggregates_projected(queryTrackingBehavior));

    public override Task Over_nested_associate_collection()
        => base.Over_nested_associate_collection();

    // Fails: Set operation over different owned collections fails in the server aggregate EF-X864
    public override Task Over_different_collection_properties()
        => SpecFails.ThrowsAsync(() => base.Over_different_collection_properties());
}
#endif
