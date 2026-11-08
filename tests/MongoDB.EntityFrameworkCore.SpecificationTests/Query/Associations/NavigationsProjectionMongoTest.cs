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

public class NavigationsProjectionMongoTest(NavigationsMongoFixture fixture)
    : NavigationsProjectionTestBase<NavigationsMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Select_root(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_root(queryTrackingBehavior));

    public override Task Select_scalar_property_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
        => base.Select_scalar_property_on_required_associate(queryTrackingBehavior);

    public override Task Select_property_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
        => base.Select_property_on_optional_associate(queryTrackingBehavior);

    // Fails: Different exception type than the relational providers throw (NullReference / ExpressionNotSupported / NotSupported) EF-X853
    public override Task Select_value_type_property_on_null_associate_throws(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_value_type_property_on_null_associate_throws(queryTrackingBehavior));

    public override Task Select_nullable_value_type_property_on_null_associate(QueryTrackingBehavior queryTrackingBehavior)
        => base.Select_nullable_value_type_property_on_null_associate(queryTrackingBehavior);

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Select_associate(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_associate(queryTrackingBehavior));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Select_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_optional_associate(queryTrackingBehavior));

    public override Task Select_required_nested_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
        => base.Select_required_nested_on_required_associate(queryTrackingBehavior);

    public override Task Select_optional_nested_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
        => base.Select_optional_nested_on_required_associate(queryTrackingBehavior);

    public override Task Select_required_nested_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
        => base.Select_required_nested_on_optional_associate(queryTrackingBehavior);

    public override Task Select_optional_nested_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
        => base.Select_optional_nested_on_optional_associate(queryTrackingBehavior);

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Select_required_associate_via_optional_navigation(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_required_associate_via_optional_navigation(queryTrackingBehavior));

    // Fails: Unmapped associate property: driver ExpressionNotSupportedException instead of EF InvalidOperationException EF-X854
    public override Task Select_unmapped_associate_scalar_property(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_unmapped_associate_scalar_property(queryTrackingBehavior));

    // Fails: Untranslatable client method in projection: driver ExpressionNotSupportedException instead of client evaluation EF-X855
    public override Task Select_untranslatable_method_on_associate_scalar_property(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_untranslatable_method_on_associate_scalar_property(queryTrackingBehavior));

    // Fails: Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) EF-X852
    public override Task Select_associate_collection(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_associate_collection(queryTrackingBehavior));

    // Fails: Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) EF-X852
    public override Task Select_nested_collection_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_nested_collection_on_required_associate(queryTrackingBehavior));

    // Fails: Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) EF-X852
    public override Task Select_nested_collection_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_nested_collection_on_optional_associate(queryTrackingBehavior));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task SelectMany_associate_collection(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.SelectMany_associate_collection(queryTrackingBehavior));

    // Fails: SelectMany over a nested collection of an optional/required associate cannot be translated EF-X856
    public override Task SelectMany_nested_collection_on_required_associate(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.SelectMany_nested_collection_on_required_associate(queryTrackingBehavior));

    // Fails: SelectMany over a nested collection of an optional/required associate cannot be translated EF-X856
    public override Task SelectMany_nested_collection_on_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.SelectMany_nested_collection_on_optional_associate(queryTrackingBehavior));

    // Fails: Query result has null associate navigations (documents in the other collection are not loaded or fixed up) EF-X850
    public override Task Select_root_duplicated(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_root_duplicated(queryTrackingBehavior));

    // Fails: Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) EF-X852
    public override Task Select_subquery_required_related_FirstOrDefault(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_subquery_required_related_FirstOrDefault(queryTrackingBehavior));

    // Fails: Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) EF-X852
    public override Task Select_subquery_optional_related_FirstOrDefault(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_subquery_optional_related_FirstOrDefault(queryTrackingBehavior));

    // Fails: Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) EF-X852
    public override Task Select_subquery_FirstOrDefault_complex_collection(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_subquery_FirstOrDefault_complex_collection(queryTrackingBehavior));

#if EF11

    // Fails: EF11-only test: null associate navigation in the result is not populated EF-X857
    public override Task Select_associate_and_target_to_index_based_binding_via_closure(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_associate_and_target_to_index_based_binding_via_closure(queryTrackingBehavior));

    // Fails: EF11-only test: null associate navigation in the result is not populated EF-X857
    public override Task Select_required_associate_duplicated(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_required_associate_duplicated(queryTrackingBehavior));

    // Fails: EF11-only test: null associate navigation in the result is not populated EF-X857
    public override Task Select_required_associate_and_optional_associate(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_required_associate_and_optional_associate(queryTrackingBehavior));

    // Fails: EF11-only test: null associate navigation in the result is not populated EF-X857
    public override Task Select_optional_associate_and_ints(QueryTrackingBehavior queryTrackingBehavior)
        => SpecFails.ThrowsAsync(() => base.Select_optional_associate_and_ints(queryTrackingBehavior));
#endif
}
#endif
