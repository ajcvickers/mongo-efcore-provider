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

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class ComplexTypeQueryMongoTest : ComplexTypeQueryTestBase<ComplexTypeQueryMongoFixture>
{
    public ComplexTypeQueryMongoTest(ComplexTypeQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

#if EF11
    // EF Core 11 moved the AdHoc complex type tests into this base class and added alternate-key and index tests.
    // None of them passes against the provider yet (see #22).

    public override async Task Can_query_by_alternate_key_on_complex_type_property()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Can_query_by_alternate_key_on_complex_type_property());
    }

    public override async Task Projecting_complex_property_does_not_auto_include_owned_types()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Projecting_complex_property_does_not_auto_include_owned_types());
    }

    public override async Task Can_query_by_complex_type_property_with_index()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Can_query_by_complex_type_property_with_index());
    }

    public override async Task Complex_type_equals_parameter_with_nested_types_with_property_of_same_name()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Complex_type_equals_parameter_with_nested_types_with_property_of_same_name());
    }

    public override async Task Can_delete_entity_with_index_on_complex_type_property()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Can_delete_entity_with_index_on_complex_type_property());
    }

    public override async Task Non_optional_complex_type_with_all_nullable_properties_via_left_join()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Non_optional_complex_type_with_all_nullable_properties_via_left_join());
    }

    public override async Task Nullable_complex_type_with_discriminator_and_shadow_property()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Nullable_complex_type_with_discriminator_and_shadow_property());
    }

    public override async Task Optional_complex_type_with_discriminator()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Optional_complex_type_with_discriminator());
    }

    public override async Task Can_update_entity_with_index_on_complex_type_property()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Can_update_entity_with_index_on_complex_type_property());
    }

    public override async Task Nullable_complex_type_with_discriminator_update_non_null_entity_roundtrip()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Nullable_complex_type_with_discriminator_update_non_null_entity_roundtrip());
    }

    public override async Task Non_optional_complex_type_with_all_nullable_properties()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Non_optional_complex_type_with_all_nullable_properties());
    }

    public override async Task Nested_nullable_complex_type_with_discriminator_null_to_non_null_roundtrip()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Nested_nullable_complex_type_with_discriminator_null_to_non_null_roundtrip());
    }

    public override async Task Nullable_complex_type_with_discriminator_set_to_null()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Nullable_complex_type_with_discriminator_set_to_null());
    }

    public override async Task Can_save_batch_swapping_alternate_key_values_on_complex_type_property()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Can_save_batch_swapping_alternate_key_values_on_complex_type_property());
    }

    public override async Task Nullable_complex_type_with_discriminator_null_to_non_null_roundtrip()
        => await base.Nullable_complex_type_with_discriminator_null_to_non_null_roundtrip();

    public override async Task Nullable_complex_type_with_discriminator_non_null_to_null_roundtrip()
        => await base.Nullable_complex_type_with_discriminator_non_null_to_null_roundtrip();

    public override async Task Nullable_complex_type_with_discriminator_set_to_different_value()
        => await base.Nullable_complex_type_with_discriminator_set_to_different_value();

    public override async Task Update_entity_with_nullable_complex_type_and_discriminator_does_not_throw()
        => await base.Update_entity_with_nullable_complex_type_and_discriminator_does_not_throw();
#endif

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Filter_on_property_inside_complex_type_after_subquery(bool async)
    {
        await base.Filter_on_property_inside_complex_type_after_subquery(async);

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Filter_on_property_inside_nested_complex_type_after_subquery(bool async)
    {
        await base.Filter_on_property_inside_nested_complex_type_after_subquery(async);

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Filter_on_required_property_inside_required_complex_type_on_optional_navigation(bool async)
    {
        await base.Filter_on_required_property_inside_required_complex_type_on_optional_navigation(async);

        AssertMql(
"""
CustomerGroup.
""");
    }

    public override async Task Filter_on_required_property_inside_required_complex_type_on_required_navigation(bool async)
    {
        await base.Filter_on_required_property_inside_required_complex_type_on_required_navigation(async);

        AssertMql(
"""
CustomerGroup.
""");
    }

#if !EF10 && !EF11
    public override async Task Project_complex_type_via_optional_navigation(bool async)
    {
#if EF8 || EF9
        // Fails: Projection through an optional navigation reads wrong data on EF8/EF9 #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_complex_type_via_optional_navigation(async));
#else
        await base.Project_complex_type_via_optional_navigation(async);

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }
""");
    
#endif
    }
#endif

#if EF10 || EF11
    public override async Task Project_complex_type_via_optional_navigation(bool async)
    {
        await base.Project_complex_type_via_optional_navigation(async);

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

#if !EF10 && !EF11
    public override async Task Project_complex_type_via_required_navigation(bool async)
    {
        await base.Project_complex_type_via_required_navigation(async);

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.RequiredCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "Outer" : "$_outer", "Inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : "$Inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

#if EF10 || EF11
    public override async Task Project_complex_type_via_required_navigation(bool async)
    {
        await base.Project_complex_type_via_required_navigation(async);

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.RequiredCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "Outer" : "$_outer", "Inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : "$Inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

    public override async Task Load_complex_type_after_subquery_on_entity_type(bool async)
    {
        await base.Load_complex_type_after_subquery_on_entity_type(async);

        AssertMql(
"""
Customer.{ "$sort" : { "_id" : 1 } }, { "$skip" : 1 }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }
""");
    }

    public override async Task Select_complex_type(bool async)
    {
        await base.Select_complex_type(async);

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Select_nested_complex_type(bool async)
    {
        await base.Select_nested_complex_type(async);

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Select_single_property_on_nested_complex_type(bool async)
    {
        await base.Select_single_property_on_nested_complex_type(async);

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Select_complex_type_Where(bool async)
    {
        await base.Select_complex_type_Where(async);

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Select_complex_type_Distinct(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Select_complex_type_Distinct(async));
    }

    public override async Task Complex_type_equals_complex_type(bool async)
    {
#if EF9 || EF10 || EF11
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Complex_type_equals_complex_type(async));
#else
        await base.Complex_type_equals_complex_type(async);

        AssertMql(
"""
Customer.
""");
    
#endif
    }

    public override async Task Complex_type_equals_constant(bool async)
    {
        // Fails: Whole-value equality over a type with collection leaves is refused #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Complex_type_equals_constant(async));
    }

    public override async Task Complex_type_equals_parameter(bool async)
    {
#if EF9 || EF10 || EF11
        // Fails: Whole-value equality over a type with collection leaves is refused #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Complex_type_equals_parameter(async));
#else
        await base.Complex_type_equals_parameter(async);

        AssertMql(
"""
Customer.
""");
    
#endif
    }

    public override async Task Subquery_over_complex_type(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Subquery_over_complex_type(async));
    }

    public override async Task Contains_over_complex_type(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Contains_over_complex_type(async));
    }

    public override async Task Concat_entity_type_containing_complex_property(bool async)
    {
        await base.Concat_entity_type_containing_complex_property(async);

        AssertMql(
"""
Customer.{ "$match" : { "_id" : 1 } }, { "$unionWith" : { "coll" : "Customer", "pipeline" : [{ "$match" : { "_id" : 2 } }] } }
""");
    }

    public override async Task Union_entity_type_containing_complex_property(bool async)
    {
        await base.Union_entity_type_containing_complex_property(async);

        AssertMql(
"""
Customer.{ "$match" : { "_id" : 1 } }, { "$unionWith" : { "coll" : "Customer", "pipeline" : [{ "$match" : { "_id" : 2 } }] } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }
""");
    }

    public override async Task Concat_complex_type(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Concat_complex_type(async));
    }

    public override async Task Union_complex_type(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Union_complex_type(async));
    }

    public override async Task Concat_property_in_complex_type(bool async)
    {
        await base.Concat_property_in_complex_type(async);

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Union_property_in_complex_type(bool async)
    {
        await base.Union_property_in_complex_type(async);

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Concat_two_different_complex_type(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Concat_two_different_complex_type(async));
    }

    public override async Task Union_two_different_complex_type(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Union_two_different_complex_type(async));
    }

    public override async Task Filter_on_property_inside_struct_complex_type(bool async)
    {
        await base.Filter_on_property_inside_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Filter_on_property_inside_nested_struct_complex_type(bool async)
    {
        await base.Filter_on_property_inside_nested_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Filter_on_property_inside_struct_complex_type_after_subquery(bool async)
    {
        await base.Filter_on_property_inside_struct_complex_type_after_subquery(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Filter_on_property_inside_nested_struct_complex_type_after_subquery(bool async)
    {
        await base.Filter_on_property_inside_nested_struct_complex_type_after_subquery(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Filter_on_required_property_inside_required_struct_complex_type_on_optional_navigation(bool async)
    {
        await base.Filter_on_required_property_inside_required_struct_complex_type_on_optional_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.
""");
    }

    public override async Task Filter_on_required_property_inside_required_struct_complex_type_on_required_navigation(bool async)
    {
        await base.Filter_on_required_property_inside_required_struct_complex_type_on_required_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.
""");
    }

#if !EF10 && !EF11
    public override async Task Project_struct_complex_type_via_optional_navigation(bool async)
    {
#if EF8 || EF9
        // Fails: Projection through an optional navigation reads wrong data on EF8/EF9 #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_struct_complex_type_via_optional_navigation(async));
#else
        await base.Project_struct_complex_type_via_optional_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }
""");
    
#endif
    }
#endif

#if EF10 || EF11
    public override async Task Project_struct_complex_type_via_optional_navigation(bool async)
    {
        await base.Project_struct_complex_type_via_optional_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

#if EF10 || EF11
    public override async Task Project_nullable_struct_complex_type_via_optional_navigation(bool async)
    {
#if EF10 || EF11
        // Fails: Not supported yet #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_nullable_struct_complex_type_via_optional_navigation(async));
#else
        await base.Project_nullable_struct_complex_type_via_optional_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }
""");
    
#endif
    }
#endif

    public override async Task Project_struct_complex_type_via_required_navigation(bool async)
    {
        await base.Project_struct_complex_type_via_required_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.RequiredCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "Outer" : "$_outer", "Inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : "$Inner.ShippingAddress", "_id" : 0 } }
""");
    }

    public override async Task Load_struct_complex_type_after_subquery_on_entity_type(bool async)
    {
        await base.Load_struct_complex_type_after_subquery_on_entity_type(async);

        AssertMql(
"""
ValuedCustomer.{ "$sort" : { "_id" : 1 } }, { "$skip" : 1 }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }
""");
    }

    public override async Task Select_struct_complex_type(bool async)
    {
        await base.Select_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Select_nested_struct_complex_type(bool async)
    {
        await base.Select_nested_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Select_single_property_on_nested_struct_complex_type(bool async)
    {
        await base.Select_single_property_on_nested_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Select_struct_complex_type_Where(bool async)
    {
        await base.Select_struct_complex_type_Where(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Select_struct_complex_type_Distinct(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Select_struct_complex_type_Distinct(async));
    }

    public override async Task Struct_complex_type_equals_struct_complex_type(bool async)
    {
        await base.Struct_complex_type_equals_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Struct_complex_type_equals_constant(bool async)
    {
        // Fails: Whole-value equality over a type with collection leaves is refused #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Struct_complex_type_equals_constant(async));
    }

    public override async Task Struct_complex_type_equals_parameter(bool async)
    {
        await base.Struct_complex_type_equals_parameter(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Subquery_over_struct_complex_type(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Subquery_over_struct_complex_type(async));
    }

    public override async Task Contains_over_struct_complex_type(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Contains_over_struct_complex_type(async));
    }

    public override async Task Concat_entity_type_containing_struct_complex_property(bool async)
    {
        await base.Concat_entity_type_containing_struct_complex_property(async);

        AssertMql(
"""
ValuedCustomer.{ "$match" : { "_id" : 1 } }, { "$unionWith" : { "coll" : "ValuedCustomer", "pipeline" : [{ "$match" : { "_id" : 2 } }] } }
""");
    }

    public override async Task Union_entity_type_containing_struct_complex_property(bool async)
    {
        await base.Union_entity_type_containing_struct_complex_property(async);

        AssertMql(
"""
ValuedCustomer.{ "$match" : { "_id" : 1 } }, { "$unionWith" : { "coll" : "ValuedCustomer", "pipeline" : [{ "$match" : { "_id" : 2 } }] } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }
""");
    }

    public override async Task Concat_struct_complex_type(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Concat_struct_complex_type(async));
    }

    public override async Task Union_struct_complex_type(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Union_struct_complex_type(async));
    }

    public override async Task Concat_property_in_struct_complex_type(bool async)
    {
        await base.Concat_property_in_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Union_property_in_struct_complex_type(bool async)
    {
        await base.Union_property_in_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Concat_two_different_struct_complex_type(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Concat_two_different_struct_complex_type(async));
    }

    public override async Task Union_two_different_struct_complex_type(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Union_two_different_struct_complex_type(async));
    }

    public override async Task Project_same_nested_complex_type_twice_with_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_same_nested_complex_type_twice_with_pushdown(async));
    }

    public override async Task Project_same_entity_with_nested_complex_type_twice_with_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_same_entity_with_nested_complex_type_twice_with_pushdown(async));
    }

    public override async Task Project_same_nested_complex_type_twice_with_double_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_same_nested_complex_type_twice_with_double_pushdown(async));
    }

    public override async Task Project_same_entity_with_nested_complex_type_twice_with_double_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_same_entity_with_nested_complex_type_twice_with_double_pushdown(async));
    }

    public override async Task Project_same_struct_nested_complex_type_twice_with_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_same_struct_nested_complex_type_twice_with_pushdown(async));
    }

    public override async Task Project_same_entity_with_struct_nested_complex_type_twice_with_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_same_entity_with_struct_nested_complex_type_twice_with_pushdown(async));
    }

    public override async Task Project_same_struct_nested_complex_type_twice_with_double_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_same_struct_nested_complex_type_twice_with_double_pushdown(async));
    }

    public override async Task Project_same_entity_with_struct_nested_complex_type_twice_with_double_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_same_entity_with_struct_nested_complex_type_twice_with_double_pushdown(async));
    }

    public override async Task Union_of_same_entity_with_nested_complex_type_projected_twice_with_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Union_of_same_entity_with_nested_complex_type_projected_twice_with_pushdown(async));
    }

    public override async Task Union_of_same_entity_with_nested_complex_type_projected_twice_with_double_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Union_of_same_entity_with_nested_complex_type_projected_twice_with_double_pushdown(async));
    }

    public override async Task Union_of_same_nested_complex_type_projected_twice_with_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Union_of_same_nested_complex_type_projected_twice_with_pushdown(async));
    }

    public override async Task Union_of_same_nested_complex_type_projected_twice_with_double_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Union_of_same_nested_complex_type_projected_twice_with_double_pushdown(async));
    }

    public override async Task Same_entity_with_complex_type_projected_twice_with_pushdown_as_part_of_another_projection(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Same_entity_with_complex_type_projected_twice_with_pushdown_as_part_of_another_projection(async));
    }

    [ConditionalTheory(Skip = "issue #31376"), MemberData(nameof(IsAsyncData))]
    public override async Task Same_complex_type_projected_twice_with_pushdown_as_part_of_another_projection(bool async)
    {
        await base.Same_complex_type_projected_twice_with_pushdown_as_part_of_another_projection(async);
    }

#if !EF8
    public override async Task GroupBy_over_property_in_nested_complex_type(bool async)
    {
        await base.GroupBy_over_property_in_nested_complex_type(async);

        AssertMql(
"""
Customer.
""");
    }
#endif

#if !EF8
    public override async Task GroupBy_over_complex_type(bool async)
    {
#if EF9 || EF10 || EF11
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.GroupBy_over_complex_type(async));
#else
        await base.GroupBy_over_complex_type(async);

        AssertMql(
"""
Customer.
""");
    
#endif
    }
#endif

#if !EF8
    public override async Task GroupBy_over_nested_complex_type(bool async)
    {
#if EF9 || EF10 || EF11
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.GroupBy_over_nested_complex_type(async));
#else
        await base.GroupBy_over_nested_complex_type(async);

        AssertMql(
"""
Customer.
""");
    
#endif
    }
#endif

#if !EF8
    public override async Task Entity_with_complex_type_with_group_by_and_first(bool async)
    {
        await base.Entity_with_complex_type_with_group_by_and_first(async);

AssertMql();
    }
#endif

    public override async Task Projecting_property_of_complex_type_using_left_join_with_pushdown(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Projecting_property_of_complex_type_using_left_join_with_pushdown(async));
    }

    public override async Task Projecting_complex_from_optional_navigation_using_conditional(bool async)
    {
        // Fails: Whole complex value as a set-operator or GroupBy operand is refused #38
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Projecting_complex_from_optional_navigation_using_conditional(async));
    }

    public override async Task Project_entity_with_complex_type_pushdown_and_then_left_join(bool async)
    {
        // Fails: Query shape (cross-collection subquery, left-join pushdown, SelectMany) not translated #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Project_entity_with_complex_type_pushdown_and_then_left_join(async));
    }

#if !EF10 && !EF11
    public override async Task Filter_on_property_inside_complex_type(bool async)
    {
        await base.Filter_on_property_inside_complex_type(async);

        AssertMql(
"""
Customer.
""");
    }
#endif

#if !EF10 && !EF11
    public override async Task Filter_on_property_inside_nested_complex_type(bool async)
    {
        await base.Filter_on_property_inside_nested_complex_type(async);

        AssertMql(
"""
Customer.
""");
    }
#endif

#if !EF10 && !EF11
    public override async Task Complex_type_equals_null(bool async)
    {
#if EF8 || EF9
        // Fails: Not supported yet #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Complex_type_equals_null(async));
#else
        await base.Complex_type_equals_null(async);

        AssertMql(
"""
Customer.
""");
    
#endif
    }
#endif


    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    protected new static Task AssertTranslationFailed(Func<Task> query)
        => MongoSpecTestHelpers.AssertNativeTranslationFailedAsync(query);
}
