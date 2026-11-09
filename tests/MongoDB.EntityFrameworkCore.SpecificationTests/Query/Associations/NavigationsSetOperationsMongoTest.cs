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

public class NavigationsSetOperationsMongoTest(NavigationsMongoFixture fixture)
    : NavigationsSetOperationsTestBase<NavigationsMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Over_associate_collections()
        => SpecFails.ThrowsAsync(() => base.Over_associate_collections());

    // Fails: Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) EF-X852
    public override Task Over_associate_collection_projected(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Over_associate_collection_projected(queryTrackingBehavior));

    // Fails: Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) EF-X852
    public override Task Over_assocate_collection_Select_nested_with_aggregates_projected(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Over_assocate_collection_Select_nested_with_aggregates_projected(queryTrackingBehavior));

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Over_nested_associate_collection()
        => SpecFails.ThrowsAsync(() => base.Over_nested_associate_collection());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Over_different_collection_properties()
        => SpecFails.ThrowsAsync(() => base.Over_different_collection_properties());
}
#endif
