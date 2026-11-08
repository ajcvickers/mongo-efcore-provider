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

public class OwnedNavigationsCollectionMongoTest(OwnedNavigationsMongoFixture fixture)
    : OwnedNavigationsCollectionTestBase<OwnedNavigationsMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override Task Count()
        => base.Count();

    public override Task Where()
        => base.Where();

    public override Task OrderBy_ElementAt()
        => base.OrderBy_ElementAt();

    public override Task Distinct()
        => base.Distinct();

    // Fails: Distinct/Select over an owned collection (EF.Property<List<T>>.AsQueryable()) cannot be translated EF-X858
    public override Task Distinct_projected(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Distinct_projected(queryTrackingBehavior));

    public override Task Distinct_over_projected_nested_collection()
        => base.Distinct_over_projected_nested_collection();

    // Fails: WRONG RESULTS: associate/collection equality or Distinct returns the wrong number of rows EF-X867
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

    // Fails: GroupBy over an owned collection: driver ExpressionNotSupportedException EF-X859
    public override Task GroupBy()
        => SpecFails.ThrowsAsync(() => base.GroupBy());

    // Fails: Nested aggregate over an owned collection inside a Select cannot be translated EF-X860
    public override Task Select_within_Select_within_Select_with_aggregates()
        => SpecFails.ThrowsAsync(() => base.Select_within_Select_within_Select_with_aggregates());
}
#endif
