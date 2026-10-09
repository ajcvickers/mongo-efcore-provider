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

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Filter_on_property_inside_complex_type_after_subquery(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_property_inside_complex_type_after_subquery(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Filter_on_property_inside_nested_complex_type_after_subquery(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_property_inside_nested_complex_type_after_subquery(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Filter_on_required_property_inside_required_complex_type_on_optional_navigation(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_required_property_inside_required_complex_type_on_optional_navigation(async));

        AssertMql(
"""
CustomerGroup.
""");
    }

    public override async Task Filter_on_required_property_inside_required_complex_type_on_required_navigation(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_required_property_inside_required_complex_type_on_required_navigation(async));

        AssertMql(
"""
CustomerGroup.
""");
    }

#if !EF10
    public override async Task Project_complex_type_via_optional_navigation(bool async)
    {
        // Fails: Complex Types: query throws on this shape EF-168
        await Assert.ThrowsAsync<System.NullReferenceException>(() => base.Project_complex_type_via_optional_navigation(async));

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

#if EF10
    public override async Task Project_complex_type_via_optional_navigation(bool async)
    {
        // Fails: Complex Types: data not round-tripped EF-168
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => base.Project_complex_type_via_optional_navigation(async));

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

#if !EF10
    public override async Task Project_complex_type_via_required_navigation(bool async)
    {
        // Fails: Complex Types: query throws on this shape EF-168
        await Assert.ThrowsAsync<System.NullReferenceException>(() => base.Project_complex_type_via_required_navigation(async));

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.RequiredCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "Outer" : "$_outer", "Inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : "$Inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

#if EF10
    public override async Task Project_complex_type_via_required_navigation(bool async)
    {
        // Fails: Complex Types: data not round-tripped EF-168
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => base.Project_complex_type_via_required_navigation(async));

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.RequiredCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "Outer" : "$_outer", "Inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : "$Inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

    public override async Task Load_complex_type_after_subquery_on_entity_type(bool async)
    {
        // Fails: Complex Types: properties not materialized from BSON EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Load_complex_type_after_subquery_on_entity_type(async))).Message);

        AssertMql(
"""
Customer.{ "$sort" : { "_id" : 1 } }, { "$skip" : 1 }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }
""");
    }

    public override async Task Select_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Select_nested_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_nested_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Select_single_property_on_nested_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_single_property_on_nested_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Select_complex_type_Where(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_complex_type_Where(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Select_complex_type_Distinct(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_complex_type_Distinct(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Complex_type_equals_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Complex_type_equals_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Complex_type_equals_constant(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Complex_type_equals_constant(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Complex_type_equals_parameter(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Complex_type_equals_parameter(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Subquery_over_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Subquery_over_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Contains_over_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Contains_over_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Concat_entity_type_containing_complex_property(bool async)
    {
        // Fails: Complex Types: properties not materialized from BSON EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Concat_entity_type_containing_complex_property(async))).Message);

        AssertMql(
"""
Customer.{ "$match" : { "_id" : 1 } }, { "$unionWith" : { "coll" : "Customer", "pipeline" : [{ "$match" : { "_id" : 2 } }] } }
""");
    }

    public override async Task Union_entity_type_containing_complex_property(bool async)
    {
        // Fails: Complex Types: properties not materialized from BSON EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Union_entity_type_containing_complex_property(async))).Message);

        AssertMql(
"""
Customer.{ "$match" : { "_id" : 1 } }, { "$unionWith" : { "coll" : "Customer", "pipeline" : [{ "$match" : { "_id" : 2 } }] } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }
""");
    }

    public override async Task Concat_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Concat_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Union_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Union_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Concat_property_in_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Concat_property_in_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Union_property_in_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Union_property_in_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Concat_two_different_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Concat_two_different_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Union_two_different_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Union_two_different_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }

    public override async Task Filter_on_property_inside_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_property_inside_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Filter_on_property_inside_nested_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_property_inside_nested_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Filter_on_property_inside_struct_complex_type_after_subquery(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_property_inside_struct_complex_type_after_subquery(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Filter_on_property_inside_nested_struct_complex_type_after_subquery(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_property_inside_nested_struct_complex_type_after_subquery(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Filter_on_required_property_inside_required_struct_complex_type_on_optional_navigation(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_required_property_inside_required_struct_complex_type_on_optional_navigation(async));

        AssertMql(
"""
ValuedCustomerGroup.
""");
    }

    public override async Task Filter_on_required_property_inside_required_struct_complex_type_on_required_navigation(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_required_property_inside_required_struct_complex_type_on_required_navigation(async));

        AssertMql(
"""
ValuedCustomerGroup.
""");
    }

#if !EF10
    public override async Task Project_struct_complex_type_via_optional_navigation(bool async)
    {
        // Fails: Complex Types: query throws on this shape EF-168
        await Assert.ThrowsAsync<System.NullReferenceException>(() => base.Project_struct_complex_type_via_optional_navigation(async));

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

#if EF10
    public override async Task Project_struct_complex_type_via_optional_navigation(bool async)
    {
        // Fails: Complex Types: data not round-tripped EF-168
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => base.Project_struct_complex_type_via_optional_navigation(async));

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

#if EF10
    public override async Task Project_nullable_struct_complex_type_via_optional_navigation(bool async)
    {
        // Fails: Complex Types: data not round-tripped EF-168
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => base.Project_nullable_struct_complex_type_via_optional_navigation(async));

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }
""");
    }
#endif

    public override async Task Project_struct_complex_type_via_required_navigation(bool async)
    {
        // Fails: Complex Types: data not round-tripped EF-168
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => base.Project_struct_complex_type_via_required_navigation(async));

        AssertMql(
"""
ValuedCustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ValuedCustomer", "localField" : "_outer.RequiredCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "Outer" : "$_outer", "Inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : "$Inner.ShippingAddress", "_id" : 0 } }
""");
    }

    public override async Task Load_struct_complex_type_after_subquery_on_entity_type(bool async)
    {
        // Fails: Complex Types: properties not materialized from BSON EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Load_struct_complex_type_after_subquery_on_entity_type(async))).Message);

        AssertMql(
"""
ValuedCustomer.{ "$sort" : { "_id" : 1 } }, { "$skip" : 1 }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }
""");
    }

    public override async Task Select_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Select_nested_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_nested_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Select_single_property_on_nested_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_single_property_on_nested_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Select_struct_complex_type_Where(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_struct_complex_type_Where(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Select_struct_complex_type_Distinct(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Select_struct_complex_type_Distinct(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Struct_complex_type_equals_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Struct_complex_type_equals_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Struct_complex_type_equals_constant(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Struct_complex_type_equals_constant(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Struct_complex_type_equals_parameter(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Struct_complex_type_equals_parameter(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Subquery_over_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Subquery_over_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Contains_over_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Contains_over_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Concat_entity_type_containing_struct_complex_property(bool async)
    {
        // Fails: Complex Types: properties not materialized from BSON EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Concat_entity_type_containing_struct_complex_property(async))).Message);

        AssertMql(
"""
ValuedCustomer.{ "$match" : { "_id" : 1 } }, { "$unionWith" : { "coll" : "ValuedCustomer", "pipeline" : [{ "$match" : { "_id" : 2 } }] } }
""");
    }

    public override async Task Union_entity_type_containing_struct_complex_property(bool async)
    {
        // Fails: Complex Types: properties not materialized from BSON EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.Union_entity_type_containing_struct_complex_property(async))).Message);

        AssertMql(
"""
ValuedCustomer.{ "$match" : { "_id" : 1 } }, { "$unionWith" : { "coll" : "ValuedCustomer", "pipeline" : [{ "$match" : { "_id" : 2 } }] } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }
""");
    }

    public override async Task Concat_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Concat_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Union_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Union_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Concat_property_in_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Concat_property_in_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Union_property_in_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Union_property_in_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Concat_two_different_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Concat_two_different_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Union_two_different_struct_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Union_two_different_struct_complex_type(async));

        AssertMql(
"""
ValuedCustomer.
""");
    }

    public override async Task Project_same_nested_complex_type_twice_with_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Project_same_nested_complex_type_twice_with_pushdown(async));

AssertMql();
    }

    public override async Task Project_same_entity_with_nested_complex_type_twice_with_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Project_same_entity_with_nested_complex_type_twice_with_pushdown(async));

AssertMql();
    }

    public override async Task Project_same_nested_complex_type_twice_with_double_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Project_same_nested_complex_type_twice_with_double_pushdown(async));

AssertMql();
    }

    public override async Task Project_same_entity_with_nested_complex_type_twice_with_double_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Project_same_entity_with_nested_complex_type_twice_with_double_pushdown(async));

AssertMql();
    }

    public override async Task Project_same_struct_nested_complex_type_twice_with_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Project_same_struct_nested_complex_type_twice_with_pushdown(async));

AssertMql();
    }

    public override async Task Project_same_entity_with_struct_nested_complex_type_twice_with_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Project_same_entity_with_struct_nested_complex_type_twice_with_pushdown(async));

AssertMql();
    }

    public override async Task Project_same_struct_nested_complex_type_twice_with_double_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Project_same_struct_nested_complex_type_twice_with_double_pushdown(async));

AssertMql();
    }

    public override async Task Project_same_entity_with_struct_nested_complex_type_twice_with_double_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Project_same_entity_with_struct_nested_complex_type_twice_with_double_pushdown(async));

AssertMql();
    }

    public override async Task Union_of_same_entity_with_nested_complex_type_projected_twice_with_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Union_of_same_entity_with_nested_complex_type_projected_twice_with_pushdown(async));

AssertMql();
    }

    public override async Task Union_of_same_entity_with_nested_complex_type_projected_twice_with_double_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Union_of_same_entity_with_nested_complex_type_projected_twice_with_double_pushdown(async));

AssertMql();
    }

    public override async Task Union_of_same_nested_complex_type_projected_twice_with_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Union_of_same_nested_complex_type_projected_twice_with_pushdown(async));

AssertMql();
    }

    public override async Task Union_of_same_nested_complex_type_projected_twice_with_double_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Union_of_same_nested_complex_type_projected_twice_with_double_pushdown(async));

AssertMql();
    }

    public override async Task Same_entity_with_complex_type_projected_twice_with_pushdown_as_part_of_another_projection(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Same_entity_with_complex_type_projected_twice_with_pushdown_as_part_of_another_projection(async));

AssertMql();
    }

    [ConditionalTheory(Skip = "issue #31376"), MemberData(nameof(IsAsyncData))]
    public override async Task Same_complex_type_projected_twice_with_pushdown_as_part_of_another_projection(bool async)
    {
        await base.Same_complex_type_projected_twice_with_pushdown_as_part_of_another_projection(async);
    }

#if !EF8
    public override async Task GroupBy_over_property_in_nested_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.GroupBy_over_property_in_nested_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }
#endif

#if !EF8
    public override async Task GroupBy_over_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.GroupBy_over_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }
#endif

#if !EF8
    public override async Task GroupBy_over_nested_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.GroupBy_over_nested_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }
#endif

#if !EF8
    public override async Task Entity_with_complex_type_with_group_by_and_first(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Entity_with_complex_type_with_group_by_and_first(async));

AssertMql();
    }
#endif

    public override async Task Projecting_property_of_complex_type_using_left_join_with_pushdown(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Projecting_property_of_complex_type_using_left_join_with_pushdown(async));

        AssertMql(
"""
CustomerGroup.
""");
    }

    public override async Task Projecting_complex_from_optional_navigation_using_conditional(bool async)
    {
        // Fails: Complex Types: data not round-tripped EF-168
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => base.Projecting_complex_from_optional_navigation_using_conditional(async));

        AssertMql(
"""
CustomerGroup.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "Customer", "localField" : "_outer.OptionalCustomerId", "foreignField" : "_id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$sort" : { "_v._inner.ShippingAddress.ZipCode" : 1 } }, { "$limit" : 20 }, { "$project" : { "_v" : "$_v._inner.ShippingAddress", "_id" : 0 } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$project" : { "_v" : "$_v.ZipCode", "_id" : 0 } }
""");
    }

    public override async Task Project_entity_with_complex_type_pushdown_and_then_left_join(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Project_entity_with_complex_type_pushdown_and_then_left_join(async));

        AssertMql(
"""
Customer.
""");
    }

#if !EF10
    public override async Task Filter_on_property_inside_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_property_inside_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }
#endif

#if !EF10
    public override async Task Filter_on_property_inside_nested_complex_type(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Filter_on_property_inside_nested_complex_type(async));

        AssertMql(
"""
Customer.
""");
    }
#endif

#if !EF10
    public override async Task Complex_type_equals_null(bool async)
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Complex_type_equals_null(async));

        AssertMql(
"""
Customer.
""");
    }
#endif


    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    protected new static Task AssertTranslationFailed(Func<Task> query)
        => MongoSpecTestHelpers.AssertNativeTranslationFailedAsync(query);
}
