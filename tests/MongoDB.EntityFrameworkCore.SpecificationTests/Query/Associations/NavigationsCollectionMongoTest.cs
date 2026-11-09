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

public class NavigationsCollectionMongoTest(NavigationsMongoFixture fixture)
    : NavigationsCollectionTestBase<NavigationsMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Count()
        => SpecFails.ThrowsAsync(() => base.Count());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Where()
        => SpecFails.ThrowsAsync(() => base.Where());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task OrderBy_ElementAt()
        => SpecFails.ThrowsAsync(() => base.OrderBy_ElementAt());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Distinct()
        => SpecFails.ThrowsAsync(() => base.Distinct());

    // Fails: Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) EF-X852
    public override Task Distinct_projected(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Distinct_projected(queryTrackingBehavior));

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Distinct_over_projected_nested_collection()
        => SpecFails.ThrowsAsync(() => base.Distinct_over_projected_nested_collection());

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task Distinct_over_projected_filtered_nested_collection()
        => SpecFails.ThrowsAsync(() => base.Distinct_over_projected_filtered_nested_collection());

    public override Task Index_constant()
        => base.Index_constant();

    public override Task Index_parameter()
        => base.Index_parameter();

    public override Task Index_column()
        => base.Index_column();

    public override Task Index_on_nested_collection()
        => base.Index_on_nested_collection();

    public override Task Index_out_of_bounds()
        => base.Index_out_of_bounds();

    // Fails: Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join EF-X851
    public override Task GroupBy()
        => SpecFails.ThrowsAsync(() => base.GroupBy());

    // Fails: Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) EF-X852
    public override Task Select_within_Select_within_Select_with_aggregates()
        => SpecFails.ThrowsAsync(() => base.Select_within_Select_within_Select_with_aggregates());
}
#endif
