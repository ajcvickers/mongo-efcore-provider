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
#if EF8 || EF9
using Microsoft.EntityFrameworkCore.TestModels.ComplexTypeModel;
#endif
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

    // These ran as "known gaps" while the non-shared test store was never cleaned between tests (their results depended on the
    // documents earlier tests left behind); with an isolated store they pass.
    public override async Task Projecting_complex_property_does_not_auto_include_owned_types()
    {
        await base.Projecting_complex_property_does_not_auto_include_owned_types();

        AssertMql(
            """
EntityType.{ "$project" : { "Complex" : "$Complex", "_id" : 0 } }
""");
    }

    public override async Task Complex_type_equals_parameter_with_nested_types_with_property_of_same_name()
    {
        await base.Complex_type_equals_parameter_with_nested_types_with_property_of_same_name();

        AssertMql(
            """
EntityType.{ "$match" : { "$or" : [{ "$and" : [{ "$expr" : false }, { "ComplexContainer" : null }] }, { "$and" : [{ "$expr" : true }, { "ComplexContainer.Id" : 1 }, { "$or" : [{ "$and" : [{ "$expr" : false }, { "ComplexContainer.Containee1" : null }] }, { "$and" : [{ "$expr" : true }, { "ComplexContainer.Containee1.Id" : 2 }] }] }, { "$or" : [{ "$and" : [{ "$expr" : false }, { "ComplexContainer.Containee2" : null }] }, { "$and" : [{ "$expr" : true }, { "ComplexContainer.Containee2.Id" : 3 }] }] }] }] } }, { "$limit" : 2 }
""");
    }

    public override async Task Non_optional_complex_type_with_all_nullable_properties_via_left_join()
    {
        await base.Non_optional_complex_type_with_all_nullable_properties_via_left_join();

        AssertMql(
            """
Parent.{ "$limit" : 2 }, { "$lookup" : { "from" : "Child", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_Children" } }
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_and_shadow_property()
    {
        await base.Nullable_complex_type_with_discriminator_and_shadow_property();

        AssertMql(
            """
EntityType.
""");
    }

    public override async Task Optional_complex_type_with_discriminator()
    {
        await base.Optional_complex_type_with_discriminator();

        AssertMql(
            """
EntityType.{ "$match" : { "$or" : [{ "AllOptionalsComplexType" : null }, { "AllOptionalsComplexType.Discriminator" : null }] } }, { "$limit" : 2 }
""",
            //
            """
EntityType.
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_update_non_null_entity_roundtrip()
    {
        await base.Nullable_complex_type_with_discriminator_update_non_null_entity_roundtrip();

        AssertMql(
            """
EntityType.{ "$limit" : 2 }
""",
            //
            """
EntityType.{ "$limit" : 2 }
""");
    }

    public override async Task Non_optional_complex_type_with_all_nullable_properties()
    {
        await base.Non_optional_complex_type_with_all_nullable_properties();

        AssertMql(
            """
EntityType.{ "$limit" : 2 }
""");
    }

    public override async Task Nested_nullable_complex_type_with_discriminator_null_to_non_null_roundtrip()
    {
        await base.Nested_nullable_complex_type_with_discriminator_null_to_non_null_roundtrip();

        AssertMql(
            """
EntityType.{ "$limit" : 2 }
""",
            //
            """
EntityType.{ "$limit" : 2 }
""");
    }

    public override async Task Can_query_by_alternate_key_on_complex_type_property()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Can_query_by_alternate_key_on_complex_type_property());
    }

    public override async Task Can_query_by_complex_type_property_with_index()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Can_query_by_complex_type_property_with_index());
    }

    public override async Task Can_delete_entity_with_index_on_complex_type_property()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Can_delete_entity_with_index_on_complex_type_property());
    }

    public override async Task Can_update_entity_with_index_on_complex_type_property()
    {
        // Fails: EF11 complex type conformance not implemented yet #22
        await MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Can_update_entity_with_index_on_complex_type_property());
    }

    public override async Task Nullable_complex_type_with_discriminator_set_to_null()
    {
        await base.Nullable_complex_type_with_discriminator_set_to_null();

        // The query embeds a Guid key generated per run, so Guid values are normalized before comparing.
        AssertMqlIgnoringGuids(
            """
EntityType.{ "$match" : { "_id" : { "$binary" : { "base64" : "fejqUgYURjy2/6Hzdm188w==", "subType" : "04" } } } }, { "$limit" : 2 }
""");
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
Customer.{ "$sort" : { "_id" : 1 } }, { "$skip" : 1 }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$match" : { "ShippingAddress.ZipCode" : 7728 } }
""");
    }

    public override async Task Filter_on_property_inside_nested_complex_type_after_subquery(bool async)
    {
        await base.Filter_on_property_inside_nested_complex_type_after_subquery(async);

        AssertMql(
"""
Customer.{ "$sort" : { "_id" : 1 } }, { "$skip" : 1 }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$match" : { "ShippingAddress.Country.Code" : "DE" } }
""");
    }

    public override async Task Filter_on_required_property_inside_required_complex_type_on_optional_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Filter_on_required_property_inside_required_complex_type_on_optional_navigation(async));
            return;
        }

        await base.Filter_on_required_property_inside_required_complex_type_on_optional_navigation(async);

        AssertMql(
"""
CustomerGroup.{ "$lookup" : { "from" : "Customer", "localField" : "OptionalCustomerId", "foreignField" : "_id", "as" : "_lookup_OptionalCustomer" } }, { "$unwind" : { "path" : "$_lookup_OptionalCustomer", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OptionalCustomer" : { "$ifNull" : ["$_lookup_OptionalCustomer", null] } } }, { "$match" : { "_lookup_OptionalCustomer.ShippingAddress.ZipCode" : { "$ne" : 7728 } } }, { "$lookup" : { "from" : "Customer", "localField" : "RequiredCustomerId", "foreignField" : "_id", "as" : "_lookup_RequiredCustomer" } }, { "$unwind" : { "path" : "$_lookup_RequiredCustomer", "preserveNullAndEmptyArrays" : false } }
""");
    }

    public override async Task Filter_on_required_property_inside_required_complex_type_on_required_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Filter_on_required_property_inside_required_complex_type_on_required_navigation(async));
            return;
        }

        await base.Filter_on_required_property_inside_required_complex_type_on_required_navigation(async);

        AssertMql(
"""
CustomerGroup.{ "$lookup" : { "from" : "Customer", "localField" : "RequiredCustomerId", "foreignField" : "_id", "as" : "_lookup_RequiredCustomer" } }, { "$unwind" : { "path" : "$_lookup_RequiredCustomer", "preserveNullAndEmptyArrays" : false } }, { "$match" : { "_lookup_RequiredCustomer.ShippingAddress.ZipCode" : { "$ne" : 7728 } } }, { "$lookup" : { "from" : "Customer", "localField" : "OptionalCustomerId", "foreignField" : "_id", "as" : "_lookup_OptionalCustomer" } }, { "$unwind" : { "path" : "$_lookup_OptionalCustomer", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OptionalCustomer" : { "$ifNull" : ["$_lookup_OptionalCustomer", null] } } }
""");
    }

#if !EF10 && !EF11
    public override async Task Project_complex_type_via_optional_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Project_complex_type_via_optional_navigation(async));
            return;
        }

        // The fixture's Address sorter dereferences null, and this query yields a null row for a group without an optional customer.
        await AssertQuery(
            async,
            ss => from cg in ss.Set<CustomerGroup>() select cg.OptionalCustomer!.ShippingAddress,
            e => e?.ZipCode!,
            assertOrder: false);

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }
#endif

#if EF10 || EF11
    public override async Task Project_complex_type_via_optional_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Project_complex_type_via_optional_navigation(async));
            return;
        }

        await base.Project_complex_type_via_optional_navigation(async);

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }
#endif

#if !EF10 && !EF11
    public override async Task Project_complex_type_via_required_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Project_complex_type_via_required_navigation(async));
            return;
        }

        await base.Project_complex_type_via_required_navigation(async);

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.RequiredCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }
#endif

#if EF10 || EF11
    public override async Task Project_complex_type_via_required_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Project_complex_type_via_required_navigation(async));
            return;
        }

        await base.Project_complex_type_via_required_navigation(async);

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.RequiredCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
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
Customer.{ "$project" : { "ShippingAddress" : "$ShippingAddress", "_id" : 0 } }
""");
    }

    public override async Task Select_nested_complex_type(bool async)
    {
        await base.Select_nested_complex_type(async);

        AssertMql(
"""
Customer.{ "$project" : { "_v" : "$ShippingAddress.Country", "_id" : 0 } }
""");
    }

    public override async Task Select_single_property_on_nested_complex_type(bool async)
    {
        await base.Select_single_property_on_nested_complex_type(async);

        AssertMql(
"""
Customer.{ "$project" : { "_v" : "$ShippingAddress.Country.FullName", "_id" : 0 } }
""");
    }

    public override async Task Select_complex_type_Where(bool async)
    {
        await base.Select_complex_type_Where(async);

        AssertMql(
"""
Customer.{ "$match" : { "ShippingAddress.ZipCode" : 7728 } }, { "$project" : { "ShippingAddress" : "$ShippingAddress", "_id" : 0 } }
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
Customer.{ "$match" : { "$and" : [{ "$expr" : { "$eq" : [{ "$ifNull" : ["$ShippingAddress.AddressLine1", null] }, { "$ifNull" : ["$BillingAddress.AddressLine1", null] }] } }, { "$expr" : { "$eq" : [{ "$ifNull" : ["$ShippingAddress.AddressLine2", null] }, { "$ifNull" : ["$BillingAddress.AddressLine2", null] }] } }, { "$expr" : { "$eq" : [{ "$ifNull" : ["$ShippingAddress.ZipCode", null] }, { "$ifNull" : ["$BillingAddress.ZipCode", null] }] } }, { "$expr" : { "$eq" : [{ "$ifNull" : ["$ShippingAddress.Country.Code", null] }, { "$ifNull" : ["$BillingAddress.Country.Code", null] }] } }, { "$expr" : { "$eq" : [{ "$ifNull" : ["$ShippingAddress.Country.FullName", null] }, { "$ifNull" : ["$BillingAddress.Country.FullName", null] }] } }] } }
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
Customer.{ "$match" : { "$or" : [{ "$and" : [{ "$expr" : false }, { "ShippingAddress" : null }] }, { "$and" : [{ "$expr" : true }, { "ShippingAddress.AddressLine1" : "804 S. Lakeshore Road", "ShippingAddress.AddressLine2" : null }, { "ShippingAddress.ZipCode" : 38654 }, { "$or" : [{ "$and" : [{ "$expr" : false }, { "ShippingAddress.Country" : null }] }, { "$and" : [{ "$expr" : true }, { "ShippingAddress.Country.Code" : "US", "ShippingAddress.Country.FullName" : "United States" }] }] }] }] } }
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
Customer.{ "$project" : { "_v" : "$ShippingAddress.AddressLine1", "_id" : 0 } }, { "$unionWith" : { "coll" : "Customer", "pipeline" : [{ "$project" : { "_v" : "$BillingAddress.AddressLine1", "_id" : 0 } }] } }
""");
    }

    public override async Task Union_property_in_complex_type(bool async)
    {
        await base.Union_property_in_complex_type(async);

        AssertMql(
"""
Customer.{ "$project" : { "_v" : "$ShippingAddress.AddressLine1", "_id" : 0 } }, { "$unionWith" : { "coll" : "Customer", "pipeline" : [{ "$project" : { "_v" : "$BillingAddress.AddressLine1", "_id" : 0 } }] } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }
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
ValuedCustomer.{ "$match" : { "ShippingAddress.ZipCode" : 7728 } }
""");
    }

    public override async Task Filter_on_property_inside_nested_struct_complex_type(bool async)
    {
        await base.Filter_on_property_inside_nested_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.{ "$match" : { "ShippingAddress.Country.Code" : "DE" } }
""");
    }

    public override async Task Filter_on_property_inside_struct_complex_type_after_subquery(bool async)
    {
        await base.Filter_on_property_inside_struct_complex_type_after_subquery(async);

        AssertMql(
"""
ValuedCustomer.{ "$sort" : { "_id" : 1 } }, { "$skip" : 1 }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$match" : { "ShippingAddress.ZipCode" : 7728 } }
""");
    }

    public override async Task Filter_on_property_inside_nested_struct_complex_type_after_subquery(bool async)
    {
        await base.Filter_on_property_inside_nested_struct_complex_type_after_subquery(async);

        AssertMql(
"""
ValuedCustomer.{ "$sort" : { "_id" : 1 } }, { "$skip" : 1 }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$match" : { "ShippingAddress.Country.Code" : "DE" } }
""");
    }

    public override async Task Filter_on_required_property_inside_required_struct_complex_type_on_optional_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Filter_on_required_property_inside_required_struct_complex_type_on_optional_navigation(async));
            return;
        }

        await base.Filter_on_required_property_inside_required_struct_complex_type_on_optional_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.{ "$lookup" : { "from" : "ValuedCustomer", "localField" : "OptionalCustomerId", "foreignField" : "_id", "as" : "_lookup_OptionalCustomer" } }, { "$unwind" : { "path" : "$_lookup_OptionalCustomer", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OptionalCustomer" : { "$ifNull" : ["$_lookup_OptionalCustomer", null] } } }, { "$match" : { "_lookup_OptionalCustomer.ShippingAddress.ZipCode" : { "$ne" : 7728 } } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "RequiredCustomerId", "foreignField" : "_id", "as" : "_lookup_RequiredCustomer" } }, { "$unwind" : { "path" : "$_lookup_RequiredCustomer", "preserveNullAndEmptyArrays" : false } }
""");
    }

    public override async Task Filter_on_required_property_inside_required_struct_complex_type_on_required_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Filter_on_required_property_inside_required_struct_complex_type_on_required_navigation(async));
            return;
        }

        await base.Filter_on_required_property_inside_required_struct_complex_type_on_required_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.{ "$lookup" : { "from" : "ValuedCustomer", "localField" : "RequiredCustomerId", "foreignField" : "_id", "as" : "_lookup_RequiredCustomer" } }, { "$unwind" : { "path" : "$_lookup_RequiredCustomer", "preserveNullAndEmptyArrays" : false } }, { "$match" : { "_lookup_RequiredCustomer.ShippingAddress.ZipCode" : { "$ne" : 7728 } } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "OptionalCustomerId", "foreignField" : "_id", "as" : "_lookup_OptionalCustomer" } }, { "$unwind" : { "path" : "$_lookup_OptionalCustomer", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OptionalCustomer" : { "$ifNull" : ["$_lookup_OptionalCustomer", null] } } }
""");
    }

#if !EF10 && !EF11
    public override async Task Project_struct_complex_type_via_optional_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Project_struct_complex_type_via_optional_navigation(async));
            return;
        }

        // The expected (in-memory) query of the upstream test dereferences the null optional customer and throws before any result is
        // compared, because a struct projection cannot be null. Project a nullable struct so that the null row is represented.
        await AssertQuery(
            async,
            ss => from cg in ss.Set<ValuedCustomerGroup>()
                  select cg.OptionalCustomer == null ? (AddressStruct?)null : cg.OptionalCustomer.ShippingAddress,
            e => e?.ZipCode!,
            assertOrder: false);

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }
#endif

#if EF10 || EF11
    public override async Task Project_struct_complex_type_via_optional_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Project_struct_complex_type_via_optional_navigation(async));
            return;
        }

        await base.Project_struct_complex_type_via_optional_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }
#endif

#if EF10 || EF11
    public override async Task Project_nullable_struct_complex_type_via_optional_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Project_nullable_struct_complex_type_via_optional_navigation(async));
            return;
        }

        await base.Project_nullable_struct_complex_type_via_optional_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }
#endif

    public override async Task Project_struct_complex_type_via_required_navigation(bool async)
    {
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            // NativeOnly: the shape is not natively representable, so the driver-LINQ fallback it needs is refused.
            await AssertTranslationFailed(() => base.Project_struct_complex_type_via_required_navigation(async));
            return;
        }

        await base.Project_struct_complex_type_via_required_navigation(async);

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.RequiredCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
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
ValuedCustomer.{ "$project" : { "ShippingAddress" : "$ShippingAddress", "_id" : 0 } }
""");
    }

    public override async Task Select_nested_struct_complex_type(bool async)
    {
        await base.Select_nested_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.{ "$project" : { "_v" : "$ShippingAddress.Country", "_id" : 0 } }
""");
    }

    public override async Task Select_single_property_on_nested_struct_complex_type(bool async)
    {
        await base.Select_single_property_on_nested_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.{ "$project" : { "_v" : "$ShippingAddress.Country.FullName", "_id" : 0 } }
""");
    }

    public override async Task Select_struct_complex_type_Where(bool async)
    {
        await base.Select_struct_complex_type_Where(async);

        AssertMql(
"""
ValuedCustomer.{ "$match" : { "ShippingAddress.ZipCode" : 7728 } }, { "$project" : { "ShippingAddress" : "$ShippingAddress", "_id" : 0 } }
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
ValuedCustomer.{ "$match" : { "$and" : [{ "$expr" : { "$eq" : [{ "$ifNull" : ["$ShippingAddress.AddressLine1", null] }, { "$ifNull" : ["$BillingAddress.AddressLine1", null] }] } }, { "$expr" : { "$eq" : [{ "$ifNull" : ["$ShippingAddress.AddressLine2", null] }, { "$ifNull" : ["$BillingAddress.AddressLine2", null] }] } }, { "$expr" : { "$eq" : [{ "$ifNull" : ["$ShippingAddress.ZipCode", null] }, { "$ifNull" : ["$BillingAddress.ZipCode", null] }] } }, { "$expr" : { "$eq" : [{ "$ifNull" : ["$ShippingAddress.Country.Code", null] }, { "$ifNull" : ["$BillingAddress.Country.Code", null] }] } }, { "$expr" : { "$eq" : [{ "$ifNull" : ["$ShippingAddress.Country.FullName", null] }, { "$ifNull" : ["$BillingAddress.Country.FullName", null] }] } }] } }
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
ValuedCustomer.{ "$match" : { "$and" : [{ "ShippingAddress.AddressLine1" : "804 S. Lakeshore Road", "ShippingAddress.AddressLine2" : null }, { "ShippingAddress.ZipCode" : 38654 }, { "ShippingAddress.Country.Code" : "US", "ShippingAddress.Country.FullName" : "United States" }] } }
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
ValuedCustomer.{ "$project" : { "_v" : "$ShippingAddress.AddressLine1", "_id" : 0 } }, { "$unionWith" : { "coll" : "ValuedCustomer", "pipeline" : [{ "$project" : { "_v" : "$BillingAddress.AddressLine1", "_id" : 0 } }] } }
""");
    }

    public override async Task Union_property_in_struct_complex_type(bool async)
    {
        await base.Union_property_in_struct_complex_type(async);

        AssertMql(
"""
ValuedCustomer.{ "$project" : { "_v" : "$ShippingAddress.AddressLine1", "_id" : 0 } }, { "$unionWith" : { "coll" : "ValuedCustomer", "pipeline" : [{ "$project" : { "_v" : "$BillingAddress.AddressLine1", "_id" : 0 } }] } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }
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

        AssertMql();
    }

#if !EF8
    public override async Task GroupBy_over_property_in_nested_complex_type(bool async)
    {
        await base.GroupBy_over_property_in_nested_complex_type(async);

        AssertMql(
"""
Customer.{ "$group" : { "_id" : "$ShippingAddress.Country.Code", "Count" : { "$sum" : 1 } } }, { "$project" : { "Code" : "$_id", "Count" : "$Count", "_id" : 0 } }
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

AssertMql(
"""
Customer.{ "$group" : { "_id" : "$_id", "_v" : { "$first" : "$$ROOT" } } }, { "$replaceRoot" : { "newRoot" : "$_v" } }
""");
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
Customer.{ "$match" : { "ShippingAddress.ZipCode" : 7728 } }
""");
    }
#endif

#if !EF10 && !EF11
    public override async Task Filter_on_property_inside_nested_complex_type(bool async)
    {
        await base.Filter_on_property_inside_nested_complex_type(async);

        AssertMql(
"""
Customer.{ "$match" : { "ShippingAddress.Country.Code" : "DE" } }
""");
    }
#endif

#if !EF10 && !EF11
    public override async Task Complex_type_equals_null(bool async)
    {
        // EF8/EF9 complex types are required, so `ComplexProperty == null` matches nothing on either side.
        await AssertQuery(
            async,
            ss => from c in ss.Set<Customer>() where c.ShippingAddress == null select c,
            assertEmpty: true);

        AssertMql(
"""
Customer.{ "$match" : { "ShippingAddress" : null } }
""");
    }
#endif


    private void AssertMqlIgnoringGuids(params string[] expected)
    {
        static string Normalize(string mql)
            => System.Text.RegularExpressions.Regex.Replace(
                mql, """\{ "base64" : "[^"]*", "subType" : "04" \}""", """{ "base64" : <guid>, "subType" : "04" }""");

        var actual = Fixture.TestMqlLoggerFactory.MqlStatements.Select(Normalize).ToArray();
        Fixture.TestMqlLoggerFactory.Clear();
        Assert.Equal(expected.Select(Normalize).ToArray(), actual);
    }

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    protected new static Task AssertTranslationFailed(Func<Task> query)
        => MongoSpecTestHelpers.AssertNativeTranslationFailedAsync(query);
}
