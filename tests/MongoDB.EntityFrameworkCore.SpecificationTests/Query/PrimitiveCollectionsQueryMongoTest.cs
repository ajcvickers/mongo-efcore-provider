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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class PrimitiveCollectionsQueryMongoTest : PrimitiveCollectionsQueryTestBase<PrimitiveCollectionsQueryMongoTest.PrimitiveCollectionsQueryMongoFixture>
{
    public PrimitiveCollectionsQueryMongoTest(PrimitiveCollectionsQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if EF11
    // Fails: new in the EF11 upstream package; asserts any failure until triaged. EF-X605
    public override Task Inline_collection_in_query_filter()
        => SpecFails.ThrowsAsync(() => base.Inline_collection_in_query_filter());

    public override Task Inline_collection_SelectMany_with_unreferenced_collection_value()
        => SpecFails.ThrowsAsync(() => base.Inline_collection_SelectMany_with_unreferenced_collection_value());

    public override Task Parameter_collection_FrozenSet_of_ints_Contains_int()
        => base.Parameter_collection_FrozenSet_of_ints_Contains_int();

    public override Task Parameter_collection_IReadOnlySet_of_ints_Contains_int()
        => base.Parameter_collection_IReadOnlySet_of_ints_Contains_int();

    public override Task Parameter_collection_ReadOnlyCollectionWithContains_of_ints_Contains_int()
        => base.Parameter_collection_ReadOnlyCollectionWithContains_of_ints_Contains_int();

    public override Task Parameter_collection_Count_with_huge_number_of_values_over_2_operations_same_parameter_different_property()
        => base.Parameter_collection_Count_with_huge_number_of_values_over_2_operations_same_parameter_different_property();

    public override Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_2_operations_same_parameter_different_property()
        => base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_2_operations_same_parameter_different_property();

    public override Task Static_readonly_collection_List_of_ints_Contains_int()
        => base.Static_readonly_collection_List_of_ints_Contains_int();

    public override Task Static_readonly_collection_FrozenSet_of_ints_Contains_int()
        => base.Static_readonly_collection_FrozenSet_of_ints_Contains_int();

    public override Task Static_readonly_collection_ImmutableArray_of_ints_Contains_int()
        => SpecFails.ThrowsAsync(() => base.Static_readonly_collection_ImmutableArray_of_ints_Contains_int());

    public override Task Column_collection_of_strings_Contains()
        => base.Column_collection_of_strings_Contains();

    public override Task Column_collection_of_strings_Contains_null()
        => base.Column_collection_of_strings_Contains_null();

    public override Task Column_with_custom_converter()
        => SpecFails.ThrowsAsync(() => base.Column_with_custom_converter());

    public override Task Parameter_with_inferred_value_converter()
        => SpecFails.ThrowsAsync(() => base.Parameter_with_inferred_value_converter());

    public override Task Constant_with_inferred_value_converter()
        => SpecFails.ThrowsAsync(() => base.Constant_with_inferred_value_converter());

    public override Task Multidimensional_array_is_not_supported()
        => base.Multidimensional_array_is_not_supported();

    public override Task Min_on_MemoryExtensions()
        => base.Min_on_MemoryExtensions();

    public override Task Max_on_MemoryExtensions()
        => base.Max_on_MemoryExtensions();

    public override Task Compiled_query_with_uncorrelated_parameter_collection_expression()
        => base.Compiled_query_with_uncorrelated_parameter_collection_expression();

    public override Task Project_collection_from_entity_type_with_owned()
        => SpecFails.ThrowsAsync(() => base.Project_collection_from_entity_type_with_owned());

    public override Task Subquery_over_primitive_collection_on_inheritance_derived_type()
        => SpecFails.ThrowsAsync(() => base.Subquery_over_primitive_collection_on_inheritance_derived_type());
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_of_ints_Contains()
    {
        await base.Inline_collection_of_ints_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_of_nullable_ints_Contains()
    {
        await base.Inline_collection_of_nullable_ints_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$in" : [10, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_of_nullable_ints_Contains_null()
    {
        await base.Inline_collection_of_nullable_ints_Contains_null();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$in" : [null, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Count_with_zero_values()
    {
        await base.Inline_collection_Count_with_zero_values();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Count_with_one_value()
    {
        await base.Inline_collection_Count_with_one_value();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Count_with_two_values()
    {
        await base.Inline_collection_Count_with_two_values();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2, 999], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Count_with_three_values()
    {
        await base.Inline_collection_Count_with_three_values();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2, 999, 1000], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Contains_with_zero_values()
    {
        await base.Inline_collection_Contains_with_zero_values();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Contains_with_one_value()
    {
        await base.Inline_collection_Contains_with_one_value();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Contains_with_two_values()
    {
        await base.Inline_collection_Contains_with_two_values();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Contains_with_three_values()
    {
        await base.Inline_collection_Contains_with_three_values();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999, 1000] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Contains_with_all_parameters()
    {
        await base.Inline_collection_Contains_with_all_parameters();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Contains_with_constant_and_parameter()
    {
        await base.Inline_collection_Contains_with_constant_and_parameter();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Contains_with_mixed_value_types()
    {
        await base.Inline_collection_Contains_with_mixed_value_types();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : ["$Int", [999, 11, "$_id", { "$add" : ["$_id", "$Int"] }]] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_List_Contains_with_mixed_value_types()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Contains_with_mixed_value_types(), "List");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Contains_as_Any_with_predicate()
    {
        await base.Inline_collection_Contains_as_Any_with_predicate();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_negated_Contains_as_All()
    {
        await base.Inline_collection_negated_Contains_as_All();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$nin" : [2, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Min_with_two_values()
    {
        await base.Inline_collection_Min_with_two_values();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$min" : [[30, "$Int"]] }, 30] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_List_Min_with_two_values()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Min_with_two_values(), "List");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Max_with_two_values()
    {
        await base.Inline_collection_Max_with_two_values();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$max" : [[30, "$Int"]] }, 30] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_List_Max_with_two_values()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Max_with_two_values(), "List");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Min_with_three_values()
    {
        await base.Inline_collection_Min_with_three_values();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$min" : [[30, "$Int", 25]] }, 25] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_List_Min_with_three_values()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Min_with_three_values(), "List");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Max_with_three_values()
    {
        await base.Inline_collection_Max_with_three_values();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$max" : [[30, "$Int", 35]] }, 35] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_List_Max_with_three_values()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Max_with_three_values(), "List");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_of_nullable_value_type_Min()
    {
        await base.Inline_collection_of_nullable_value_type_Min();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$min" : [[30, "$Int", 25]] }, 25] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_of_nullable_value_type_Max()
    {
        await base.Inline_collection_of_nullable_value_type_Max();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$max" : [[30, "$Int", 35]] }, 35] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_of_nullable_value_type_with_null_Min()
    {
        await base.Inline_collection_of_nullable_value_type_with_null_Min();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$min" : [[30, "$NullableInt", null]] }, 30] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_of_nullable_value_type_with_null_Max()
    {
        await base.Inline_collection_of_nullable_value_type_with_null_Max();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$max" : [[30, "$NullableInt", null]] }, 30] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_with_single_parameter_element_Contains()
    {
        await base.Inline_collection_with_single_parameter_element_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_with_single_parameter_element_Count()
    {
        await base.Inline_collection_with_single_parameter_element_Count();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Contains_with_EF_Parameter()
    {
        await base.Inline_collection_Contains_with_EF_Parameter();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999, 1000] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Contains_with_IEnumerable_EF_Parameter()
    {
        await base.Inline_collection_Contains_with_IEnumerable_EF_Parameter();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableString" : { "$in" : ["10", "a", "aa"] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Count_with_column_predicate_with_EF_Parameter()
    {
        await base.Inline_collection_Count_with_column_predicate_with_EF_Parameter();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2, 999, 1000], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_Count()
    {
        await base.Parameter_collection_Count();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2, 999], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_ints_Contains_int()
    {
        await base.Parameter_collection_of_ints_Contains_int();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$nin" : [10, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_HashSet_of_ints_Contains_int()
    {
        await base.Parameter_collection_HashSet_of_ints_Contains_int();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$nin" : [10, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
#if EF11
    public override async Task Parameter_collection_ImmutableArray_of_ints_Contains_int()
    {
    // Fails: Unexpected exception on EF11 (not yet triaged) EF-X605
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_ImmutableArray_of_ints_Contains_int());
    }
#else
    public override async Task Parameter_collection_ImmutableArray_of_ints_Contains_int()
    {
        await base.Parameter_collection_ImmutableArray_of_ints_Contains_int();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$nin" : [10, 999] } } }
""");
    }
#endif
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_ints_Contains_nullable_int()
    {
        await base.Parameter_collection_of_ints_Contains_nullable_int();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$in" : [10, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$nin" : [10, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_nullable_ints_Contains_int()
    {
        await base.Parameter_collection_of_nullable_ints_Contains_int();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$nin" : [10, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_nullable_ints_Contains_nullable_int()
    {
        await base.Parameter_collection_of_nullable_ints_Contains_nullable_int();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$in" : [null, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$nin" : [null, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_nullable_ints_Contains_nullable_int_with_EF_Parameter()
    {
        await base.Parameter_collection_of_nullable_ints_Contains_nullable_int_with_EF_Parameter();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$in" : [null, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_structs_Contains_struct()
    {
        await base.Parameter_collection_of_structs_Contains_struct();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "WrappedId" : { "$in" : [22, 33] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "WrappedId" : { "$nin" : [11, 44] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_structs_Contains_nullable_struct()
    {
        await base.Parameter_collection_of_structs_Contains_nullable_struct();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableWrappedId" : { "$in" : [22, 33] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableWrappedId" : { "$nin" : [11, 44] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_structs_Contains_nullable_struct_with_nullable_comparer()
    {
        // Fails: Contains over collection of structs with value converter not translated or serializer mismatch EF-X504
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_of_structs_Contains_nullable_struct_with_nullable_comparer(), "WrappedId");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_nullable_structs_Contains_struct()
    {
        await base.Parameter_collection_of_nullable_structs_Contains_struct();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "WrappedId" : { "$in" : [null, 22] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "WrappedId" : { "$nin" : [11, 44] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_nullable_structs_Contains_nullable_struct()
    {
        await base.Parameter_collection_of_nullable_structs_Contains_nullable_struct();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableWrappedId" : { "$in" : [null, 22] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableWrappedId" : { "$nin" : [11, 44] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_nullable_structs_Contains_nullable_struct_with_nullable_comparer()
    {
        // Fails: Contains over collection of structs with value converter not translated or serializer mismatch EF-X504
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_of_nullable_structs_Contains_nullable_struct_with_nullable_comparer(), "WrappedId");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_strings_Contains_string()
    {
        await base.Parameter_collection_of_strings_Contains_string();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "String" : { "$in" : ["10", "999"] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "String" : { "$nin" : ["10", "999"] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_strings_Contains_nullable_string()
    {
        await base.Parameter_collection_of_strings_Contains_nullable_string();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableString" : { "$in" : ["10", "999"] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableString" : { "$nin" : ["10", "999"] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_nullable_strings_Contains_string()
    {
        await base.Parameter_collection_of_nullable_strings_Contains_string();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "String" : { "$in" : ["10", null] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "String" : { "$nin" : ["10", null] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_nullable_strings_Contains_nullable_string()
    {
        await base.Parameter_collection_of_nullable_strings_Contains_nullable_string();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableString" : { "$in" : ["999", null] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableString" : { "$nin" : ["999", null] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_DateTimes_Contains()
    {
        await base.Parameter_collection_of_DateTimes_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "DateTime" : { "$in" : [{ "$date" : "2020-01-10T12:30:00Z" }, { "$date" : "9999-01-01T00:00:00Z" }] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_bools_Contains()
    {
        await base.Parameter_collection_of_bools_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Bool" : { "$in" : [true] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_enums_Contains()
    {
        await base.Parameter_collection_of_enums_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Enum" : { "$in" : [0, 3] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_null_Contains()
    {
        await base.Parameter_collection_null_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_empty_Contains()
    {
        await base.Parameter_collection_empty_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_empty_Join()
    {
        // Fails: Join over primitive collection not translated EF-X017
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_empty_Join(), "Join");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_Contains_with_EF_Constant()
    {
        await base.Parameter_collection_Contains_with_EF_Constant();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999, 1000] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_Where_with_EF_Constant_Where_Any()
    {
        await base.Parameter_collection_Where_with_EF_Constant_Where_Any();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$gt" : [{ "$size" : { "$filter" : { "input" : [2, 999, 1000], "as" : "x", "cond" : { "$gt" : ["$$x", 0] } } } }, 0] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_Count_with_column_predicate_with_EF_Constant()
    {
        await base.Parameter_collection_Count_with_column_predicate_with_EF_Constant();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2, 999, 1000], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_Count_with_huge_number_of_values()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_Count_with_huge_number_of_values_over_5_operations()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values_over_5_operations();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_Count_with_huge_number_of_values_over_5_operations_same_parameter()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values_over_5_operations_same_parameter();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
#if !EF11
    public override async Task Parameter_collection_Count_with_huge_number_of_values_over_2_operations_same_parameter_different_type_mapping()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values_over_2_operations_same_parameter_different_type_mapping();

        AssertMql();
    }
#endif
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_Count_with_huge_number_of_values_over_5_operations_forced_constants()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values_over_5_operations_forced_constants();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_Count_with_huge_number_of_values_over_5_operations_mixed_parameters_constants()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values_over_5_operations_mixed_parameters_constants();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_same_parameter()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_same_parameter();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
#if !EF11
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_2_operations_same_parameter_different_type_mapping()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_2_operations_same_parameter_different_type_mapping();

        AssertMql();
    }
#endif
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_forced_constants()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_forced_constants();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_mixed_parameters_constants()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_mixed_parameters_constants();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_of_ints_Contains()
    {
        await base.Column_collection_of_ints_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints" : 10 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_of_nullable_ints_Contains()
    {
        await base.Column_collection_of_nullable_ints_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInts" : 10 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_of_nullable_ints_Contains_null()
    {
        await base.Column_collection_of_nullable_ints_Contains_null();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInts" : null } }
""");
    }
#endif

#if (EF10 || EF11)
#if !EF11
    public override async Task Column_collection_of_strings_contains_null()
    {
        await base.Column_collection_of_strings_contains_null();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Strings" : null } }
""");
    }
#endif
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_of_nullable_strings_contains_null()
    {
        await base.Column_collection_of_nullable_strings_contains_null();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableStrings" : null } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_of_bools_Contains()
    {
        await base.Column_collection_of_bools_Contains();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Bools" : true } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Contains_on_Enumerable()
    {
        await base.Contains_on_Enumerable();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Contains_on_MemoryExtensions()
    {
        await base.Contains_on_MemoryExtensions();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Contains_with_MemoryExtensions_with_null_comparer()
    {
        await base.Contains_with_MemoryExtensions_with_null_comparer();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Count_method()
    {
        await base.Column_collection_Count_method();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.1" : { "$exists" : true }, "Ints.2" : { "$exists" : false } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Length()
    {
        await base.Column_collection_Length();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.1" : { "$exists" : true }, "Ints.2" : { "$exists" : false } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Count_with_predicate()
    {
        await base.Column_collection_Count_with_predicate();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : "$Ints", "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", 1] }, "then" : 1, "else" : 0 } } } } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Where_Count()
    {
        await base.Column_collection_Where_Count();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_index_int()
    {
        await base.Column_collection_index_int();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.1" : 10 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_index_string()
    {
        await base.Column_collection_index_string();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Strings.1" : "10" } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_index_datetime()
    {
        await base.Column_collection_index_datetime();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "DateTimes.1" : { "$date" : "2020-01-10T12:30:00Z" } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_index_beyond_end()
    {
        await base.Column_collection_index_beyond_end();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.999" : 10 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Nullable_reference_column_collection_index_equals_nullable_column()
    {
        // Fails: Wrong result: collection index compared with nullable column EF-X505
        await SpecFails.ThrowsAsync(() => base.Nullable_reference_column_collection_index_equals_nullable_column(), "Values differ");
    }
#endif

#if (EF10 || EF11)
    public override async Task Non_nullable_reference_column_collection_index_equals_nullable_column()
    {
        await base.Non_nullable_reference_column_collection_index_equals_nullable_column();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$and" : [{ "Strings" : { "$ne" : null } }, { "Strings" : { "$not" : { "$size" : 0 } } }, { "$expr" : { "$eq" : [{ "$arrayElemAt" : ["$Strings", 1] }, "$NullableString"] } }] } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_index_Column()
    {
        await base.Inline_collection_index_Column();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [[1, 2, 3], "$Int"] }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_index_Column_with_EF_Constant()
    {
        await base.Inline_collection_index_Column_with_EF_Constant();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [[1, 2, 3], "$Int"] }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_value_index_Column()
    {
        await base.Inline_collection_value_index_Column();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [[1, "$Int", 3], "$Int"] }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_List_value_index_Column()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_value_index_Column(), "List");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_index_Column_equal_Column()
    {
        await base.Parameter_collection_index_Column_equal_Column();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [[0, 2, 3], "$Int"] }, "$Int"] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_index_Column_equal_constant()
    {
        await base.Parameter_collection_index_Column_equal_constant();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [[1, 2, 3], "$Int"] }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_ElementAt()
    {
        await base.Column_collection_ElementAt();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.1" : 10 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_First()
    {
        await base.Column_collection_First();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.0" : 1 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_FirstOrDefault()
    {
        await base.Column_collection_FirstOrDefault();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$cond" : { "if" : { "$eq" : [{ "$size" : "$Ints" }, 0] }, "then" : 0, "else" : { "$arrayElemAt" : ["$Ints", 0] } } }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Single()
    {
        // Fails: Single/SingleOrDefault over a primitive collection not translated EF-X502
        await SpecFails.ThrowsAsync(() => base.Column_collection_Single(), "Expression not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_SingleOrDefault()
    {
        // Fails: Single/SingleOrDefault over a primitive collection not translated EF-X502
        await SpecFails.ThrowsAsync(() => base.Column_collection_SingleOrDefault(), "Expression not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Skip()
    {
        await base.Column_collection_Skip();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$slice" : ["$Ints", 1, 2147483647] } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Take()
    {
        await base.Column_collection_Take();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : [11, { "$slice" : ["$Ints", 2] }] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Skip_Take()
    {
        await base.Column_collection_Skip_Take();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : [11, { "$slice" : ["$Ints", 1, 2] }] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Where_Skip()
    {
        await base.Column_collection_Where_Skip();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$slice" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } }, 1, 2147483647] } }, 3] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Where_Take()
    {
        await base.Column_collection_Where_Take();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$slice" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } }, 2] } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Where_Skip_Take()
    {
        await base.Column_collection_Where_Skip_Take();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$slice" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } }, 1, 2] } }, 1] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Contains_over_subquery()
    {
        await base.Column_collection_Contains_over_subquery();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : [11, { "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } }] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_OrderByDescending_ElementAt()
    {
        await base.Column_collection_OrderByDescending_ElementAt();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [{ "$sortArray" : { "input" : "$Ints", "sortBy" : -1 } }, 0] }, 111] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Where_ElementAt()
    {
        await base.Column_collection_Where_ElementAt();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } }, 0] }, 11] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Any()
    {
        await base.Column_collection_Any();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.0" : { "$exists" : true } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Distinct()
    {
        await base.Column_collection_Distinct();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : "$Ints" } }, 3] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_SelectMany()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Column_collection_SelectMany(), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_SelectMany_with_filter()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Column_collection_SelectMany_with_filter(), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_SelectMany_with_Select_to_anonymous_type()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Column_collection_SelectMany_with_Select_to_anonymous_type(), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_projection_from_top_level()
    {
        await base.Column_collection_projection_from_top_level();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "Ints" : "$Ints", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Join_parameter_collection()
    {
        // Fails: Join over primitive collection not translated EF-X017
        await SpecFails.ThrowsAsync(() => base.Column_collection_Join_parameter_collection(), "Join");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Join_ordered_column_collection()
    {
        // Fails: Join over primitive collection not translated EF-X017
        await SpecFails.ThrowsAsync(() => base.Inline_collection_Join_ordered_column_collection(), "Join");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_Concat_column_collection()
    {
        await base.Parameter_collection_Concat_column_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$concatArrays" : [[11, 111], "$Ints"] } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_with_type_inference_for_JsonScalarExpression()
    {
        await base.Parameter_collection_with_type_inference_for_JsonScalarExpression();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$project" : { "_v" : { "$cond" : { "if" : { "$ne" : ["$_id", 0] }, "then" : { "$arrayElemAt" : [["one", "two"], { "$mod" : ["$Int", 2] }] }, "else" : "foo" } }, "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Union_parameter_collection()
    {
        await base.Column_collection_Union_parameter_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : ["$Ints", [11, 111]] } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Intersect_inline_collection()
    {
        await base.Column_collection_Intersect_inline_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setIntersection" : ["$Ints", [11, 111]] } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Inline_collection_Except_column_collection()
    {
        await base.Inline_collection_Except_column_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : { "$setDifference" : [[11, 111], "$Ints"] }, "as" : "i", "in" : { "$cond" : { "if" : { "$eq" : [{ "$mod" : ["$$i", 2] }, 1] }, "then" : 1, "else" : 0 } } } } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Where_Union()
    {
        await base.Column_collection_Where_Union();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 100] } } }, [50]] } }, 2] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_equality_parameter_collection()
    {
        await base.Column_collection_equality_parameter_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints" : [1, 10] } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Concat_parameter_collection_equality_inline_collection()
    {
        await base.Column_collection_Concat_parameter_collection_equality_inline_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$concatArrays" : ["$Ints", [1, 10]] }, [1, 11, 111, 1, 10]] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_equality_inline_collection()
    {
        await base.Column_collection_equality_inline_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints" : [1, 10] } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_equality_inline_collection_with_parameters()
    {
        await base.Column_collection_equality_inline_collection_with_parameters();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints" : [1, 10] } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_Where_equality_inline_collection()
    {
        await base.Column_collection_Where_equality_inline_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$ne" : ["$$i", 11] } } }, [1, 111]] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_in_subquery_Count_as_compiled_query()
    {
        await base.Parameter_collection_in_subquery_Count_as_compiled_query();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [111], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }, { "$count" : "_v" }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_in_subquery_Union_column_collection_as_compiled_query()
    {
        await base.Parameter_collection_in_subquery_Union_column_collection_as_compiled_query();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : [[111], "$Ints"] } }, 3] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_in_subquery_Union_column_collection()
    {
        await base.Parameter_collection_in_subquery_Union_column_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : [[111], "$Ints"] } }, 3] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Parameter_collection_in_subquery_Union_column_collection_nested()
    {
        await base.Parameter_collection_in_subquery_Union_column_collection_nested();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : [[111], { "$slice" : [{ "$sortArray" : { "input" : { "$setUnion" : { "$slice" : [{ "$sortArray" : { "input" : "$Ints", "sortBy" : 1 } }, 1, 2147483647] } }, "sortBy" : -1 } }, 20] }] } }, 3] } } }
""");
    }
#endif

    public override void Parameter_collection_in_subquery_and_Convert_as_compiled_query()
    {
        base.Parameter_collection_in_subquery_and_Convert_as_compiled_query();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "String" : "foo" } }
""");
    }

#if (EF10 || EF11)
    public override async Task Parameter_collection_in_subquery_Union_another_parameter_collection_as_compiled_query()
    {
        await base.Parameter_collection_in_subquery_Union_another_parameter_collection_as_compiled_query();

        AssertMql(
            """
PrimitiveCollectionsEntity.
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Column_collection_in_subquery_Union_parameter_collection()
    {
        await base.Column_collection_in_subquery_Union_parameter_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : [{ "$slice" : ["$Ints", 1, 2147483647] }, [10, 111]] } }, 3] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_collection_of_ints_simple()
    {
        await base.Project_collection_of_ints_simple();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "Ints" : "$Ints", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_collection_of_ints_ordered()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_collection_of_ints_ordered(), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_collection_of_datetimes_filtered()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_collection_of_datetimes_filtered(), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_collection_of_nullable_ints_with_paging()
    {
        await base.Project_collection_of_nullable_ints_with_paging();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "_v" : { "$slice" : ["$NullableInts", 20] }, "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_collection_of_nullable_ints_with_paging2()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_collection_of_nullable_ints_with_paging2(), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_collection_of_nullable_ints_with_paging3()
    {
        await base.Project_collection_of_nullable_ints_with_paging3();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "_v" : { "$slice" : ["$NullableInts", 2, 2147483647] }, "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_collection_of_ints_with_distinct()
    {
        await base.Project_collection_of_ints_with_distinct();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "_v" : { "$setUnion" : "$Ints" }, "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_collection_of_nullable_ints_with_distinct()
    {
        await base.Project_collection_of_nullable_ints_with_distinct();

        AssertMql();
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_collection_of_ints_with_ToList_and_FirstOrDefault()
    {
        await base.Project_collection_of_ints_with_ToList_and_FirstOrDefault();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "_v" : "$Ints", "_id" : 0 } }, { "$limit" : 1 }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_empty_collection_of_nullables_and_collection_only_containing_nulls()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_empty_collection_of_nullables_and_collection_only_containing_nulls(), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_multiple_collections()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_multiple_collections(), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_primitive_collections_element()
    {
        await base.Project_primitive_collections_element();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$lt" : 4 } } }, { "$sort" : { "_id" : 1 } }, { "$project" : { "Indexer" : { "$arrayElemAt" : ["$Ints", 0] }, "EnumerableElementAt" : { "$arrayElemAt" : ["$DateTimes", 0] }, "QueryableElementAt" : { "$arrayElemAt" : ["$Strings", 1] }, "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_inline_collection()
    {
        await base.Project_inline_collection();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$project" : { "_ctorArg0" : "$String", "_ctorArg1" : { "$literal" : "foo" }, "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_inline_collection_with_Union()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_inline_collection_with_Union(), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Project_inline_collection_with_Concat()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_inline_collection_with_Concat(), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Nested_contains_with_Lists_and_no_inferred_type_mapping()
    {
        await base.Nested_contains_with_Lists_and_no_inferred_type_mapping();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : [{ "$cond" : { "if" : { "$in" : ["$Int", { "$literal" : [1, 2, 3] }] }, "then" : { "$literal" : "one" }, "else" : { "$literal" : "two" } } }, { "$literal" : ["one", "two", "three"] }] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Nested_contains_with_arrays_and_no_inferred_type_mapping()
    {
        await base.Nested_contains_with_arrays_and_no_inferred_type_mapping();

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : [{ "$cond" : { "if" : { "$in" : ["$Int", { "$literal" : [1, 2, 3] }] }, "then" : { "$literal" : "one" }, "else" : { "$literal" : "two" } } }, { "$literal" : ["one", "two", "three"] }] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Values_of_enum_casted_to_underlying_value()
    {
        // Fails: Server error: input to $map must be an array (enum collection cast) EF-X506
        await SpecFails.ThrowsAsync(() => base.Values_of_enum_casted_to_underlying_value(), "input to $map must be an array");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_of_ints_Contains(bool async)
    {
        await base.Inline_collection_of_ints_Contains(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_of_nullable_ints_Contains(bool async)
    {
        await base.Inline_collection_of_nullable_ints_Contains(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$in" : [10, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_of_nullable_ints_Contains_null(bool async)
    {
        await base.Inline_collection_of_nullable_ints_Contains_null(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$in" : [null, 999] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_Count_with_zero_values(bool async)
    {
        await base.Inline_collection_Count_with_zero_values(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Count_with_one_value(bool async)
    {
        await base.Inline_collection_Count_with_one_value(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Count_with_two_values(bool async)
    {
        await base.Inline_collection_Count_with_two_values(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2, 999], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Count_with_three_values(bool async)
    {
        await base.Inline_collection_Count_with_three_values(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2, 999, 1000], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 2] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_zero_values(bool async)
    {
        await base.Inline_collection_Contains_with_zero_values(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_one_value(bool async)
    {
        await base.Inline_collection_Contains_with_one_value(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_two_values(bool async)
    {
        await base.Inline_collection_Contains_with_two_values(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_three_values(bool async)
    {
        await base.Inline_collection_Contains_with_three_values(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999, 1000] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_all_parameters(bool async)
    {
        await base.Inline_collection_Contains_with_all_parameters(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_constant_and_parameter(bool async)
    {
        await base.Inline_collection_Contains_with_constant_and_parameter(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_mixed_value_types(bool async)
    {
        await base.Inline_collection_Contains_with_mixed_value_types(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : ["$Int", [999, 11, "$_id", { "$add" : ["$_id", "$Int"] }]] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_List_Contains_with_mixed_value_types(bool async)
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Contains_with_mixed_value_types(async), "List");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_as_Any_with_predicate(bool async)
    {
        await base.Inline_collection_Contains_as_Any_with_predicate(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_negated_Contains_as_All(bool async)
    {
        await base.Inline_collection_negated_Contains_as_All(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$nin" : [2, 999] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_Min_with_two_values(bool async)
    {
        await base.Inline_collection_Min_with_two_values(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$min" : [[30, "$Int"]] }, 30] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_List_Min_with_two_values(bool async)
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Min_with_two_values(async), "List");
    }
#endif

#if EF9
    public override async Task Inline_collection_Max_with_two_values(bool async)
    {
        await base.Inline_collection_Max_with_two_values(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$max" : [[30, "$Int"]] }, 30] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_List_Max_with_two_values(bool async)
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Max_with_two_values(async), "List");
    }
#endif

#if EF9
    public override async Task Inline_collection_Min_with_three_values(bool async)
    {
        await base.Inline_collection_Min_with_three_values(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$min" : [[30, "$Int", 25]] }, 25] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_List_Min_with_three_values(bool async)
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Min_with_three_values(async), "List");
    }
#endif

#if EF9
    public override async Task Inline_collection_Max_with_three_values(bool async)
    {
        await base.Inline_collection_Max_with_three_values(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$max" : [[30, "$Int", 35]] }, 35] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_List_Max_with_three_values(bool async)
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Max_with_three_values(async), "List");
    }
#endif

#if EF9
    public override async Task Inline_collection_of_nullable_value_type_Min(bool async)
    {
        await base.Inline_collection_of_nullable_value_type_Min(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$min" : [[30, "$Int", 25]] }, 25] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_of_nullable_value_type_Max(bool async)
    {
        await base.Inline_collection_of_nullable_value_type_Max(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$max" : [[30, "$Int", 35]] }, 35] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_of_nullable_value_type_with_null_Min(bool async)
    {
        await base.Inline_collection_of_nullable_value_type_with_null_Min(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$min" : [[30, "$NullableInt", null]] }, 30] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_of_nullable_value_type_with_null_Max(bool async)
    {
        await base.Inline_collection_of_nullable_value_type_with_null_Max(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$max" : [[30, "$NullableInt", null]] }, 30] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_with_single_parameter_element_Contains(bool async)
    {
        await base.Inline_collection_with_single_parameter_element_Contains(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_with_single_parameter_element_Count(bool async)
    {
        await base.Inline_collection_with_single_parameter_element_Count(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_Contains_with_EF_Parameter(bool async)
    {
        await base.Inline_collection_Contains_with_EF_Parameter(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999, 1000] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_Count_with_column_predicate_with_EF_Parameter(bool async)
    {
        await base.Inline_collection_Count_with_column_predicate_with_EF_Parameter(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2, 999, 1000], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 2] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_Count(bool async)
    {
        await base.Parameter_collection_Count(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2, 999], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_ints_Contains_int(bool async)
    {
        await base.Parameter_collection_of_ints_Contains_int(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$nin" : [10, 999] } } }
""");
    }
#endif

#if EF9
    public override async Task Parameter_collection_HashSet_of_ints_Contains_int(bool async)
    {
        await base.Parameter_collection_HashSet_of_ints_Contains_int(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$nin" : [10, 999] } } }
""");
    }
#endif

#if EF9
    public override async Task Parameter_collection_ImmutableArray_of_ints_Contains_int(bool async)
    {
        await base.Parameter_collection_ImmutableArray_of_ints_Contains_int(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$nin" : [10, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_ints_Contains_nullable_int(bool async)
    {
        await base.Parameter_collection_of_ints_Contains_nullable_int(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$in" : [10, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$nin" : [10, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_nullable_ints_Contains_int(bool async)
    {
        await base.Parameter_collection_of_nullable_ints_Contains_int(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$nin" : [10, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_nullable_ints_Contains_nullable_int(bool async)
    {
        await base.Parameter_collection_of_nullable_ints_Contains_nullable_int(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$in" : [null, 999] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInt" : { "$nin" : [null, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_strings_Contains_string(bool async)
    {
        await base.Parameter_collection_of_strings_Contains_string(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "String" : { "$in" : ["10", "999"] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "String" : { "$nin" : ["10", "999"] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_strings_Contains_nullable_string(bool async)
    {
        await base.Parameter_collection_of_strings_Contains_nullable_string(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableString" : { "$in" : ["10", "999"] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableString" : { "$nin" : ["10", "999"] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_nullable_strings_Contains_string(bool async)
    {
        await base.Parameter_collection_of_nullable_strings_Contains_string(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "String" : { "$in" : ["10", null] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "String" : { "$nin" : ["10", null] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_nullable_strings_Contains_nullable_string(bool async)
    {
        await base.Parameter_collection_of_nullable_strings_Contains_nullable_string(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableString" : { "$in" : ["999", null] } } }
""",
            //
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableString" : { "$nin" : ["999", null] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_DateTimes_Contains(bool async)
    {
        await base.Parameter_collection_of_DateTimes_Contains(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "DateTime" : { "$in" : [{ "$date" : "2020-01-10T12:30:00Z" }, { "$date" : "9999-01-01T00:00:00Z" }] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_bools_Contains(bool async)
    {
        await base.Parameter_collection_of_bools_Contains(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Bool" : { "$in" : [true] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_enums_Contains(bool async)
    {
        await base.Parameter_collection_of_enums_Contains(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Enum" : { "$in" : [0, 3] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_null_Contains(bool async)
    {
        await base.Parameter_collection_null_Contains(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [] } } }
""");
    }
#endif

#if EF9
    public override async Task Parameter_collection_Contains_with_EF_Constant(bool async)
    {
        await base.Parameter_collection_Contains_with_EF_Constant(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999, 1000] } } }
""");
    }
#endif

#if EF9
    public override async Task Parameter_collection_Where_with_EF_Constant_Where_Any(bool async)
    {
        await base.Parameter_collection_Where_with_EF_Constant_Where_Any(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$gt" : [{ "$size" : { "$filter" : { "input" : [2, 999, 1000], "as" : "x", "cond" : { "$gt" : ["$$x", 0] } } } }, 0] } } }
""");
    }
#endif

#if EF9
    public override async Task Parameter_collection_Count_with_column_predicate_with_EF_Constant(bool async)
    {
        await base.Parameter_collection_Count_with_column_predicate_with_EF_Constant(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [2, 999, 1000], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 2] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_ints_Contains(bool async)
    {
        await base.Column_collection_of_ints_Contains(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints" : 10 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_nullable_ints_Contains(bool async)
    {
        await base.Column_collection_of_nullable_ints_Contains(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInts" : 10 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_nullable_ints_Contains_null(bool async)
    {
        await base.Column_collection_of_nullable_ints_Contains_null(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableInts" : null } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_strings_contains_null(bool async)
    {
        await base.Column_collection_of_strings_contains_null(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Strings" : null } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_nullable_strings_contains_null(bool async)
    {
        await base.Column_collection_of_nullable_strings_contains_null(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "NullableStrings" : null } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_bools_Contains(bool async)
    {
        await base.Column_collection_of_bools_Contains(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Bools" : true } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Contains_on_Enumerable(bool async)
    {
        await base.Contains_on_Enumerable(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Contains_on_MemoryExtensions(bool async)
    {
        await base.Contains_on_MemoryExtensions(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Int" : { "$in" : [10, 999] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Count_method(bool async)
    {
        await base.Column_collection_Count_method(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.1" : { "$exists" : true }, "Ints.2" : { "$exists" : false } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Length(bool async)
    {
        await base.Column_collection_Length(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.1" : { "$exists" : true }, "Ints.2" : { "$exists" : false } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Count_with_predicate(bool async)
    {
        await base.Column_collection_Count_with_predicate(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : "$Ints", "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", 1] }, "then" : 1, "else" : 0 } } } } }, 2] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Where_Count(bool async)
    {
        await base.Column_collection_Where_Count(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } } }, 2] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_index_int(bool async)
    {
        await base.Column_collection_index_int(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.1" : 10 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_index_string(bool async)
    {
        await base.Column_collection_index_string(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Strings.1" : "10" } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_index_datetime(bool async)
    {
        await base.Column_collection_index_datetime(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "DateTimes.1" : { "$date" : "2020-01-10T12:30:00Z" } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_index_beyond_end(bool async)
    {
        await base.Column_collection_index_beyond_end(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.999" : 10 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Nullable_reference_column_collection_index_equals_nullable_column(bool async)
    {
        // Fails: Wrong result: collection index compared with nullable column EF-X505
        await SpecFails.ThrowsAsync(() => base.Nullable_reference_column_collection_index_equals_nullable_column(async), "Values differ");
    }
#endif

#if EF8 || EF9
    public override async Task Non_nullable_reference_column_collection_index_equals_nullable_column(bool async)
    {
        await base.Non_nullable_reference_column_collection_index_equals_nullable_column(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$and" : [{ "Strings" : { "$ne" : null } }, { "Strings" : { "$not" : { "$size" : 0 } } }, { "$expr" : { "$eq" : [{ "$arrayElemAt" : ["$Strings", 1] }, "$NullableString"] } }] } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_index_Column(bool async)
    {
        await base.Inline_collection_index_Column(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [[1, 2, 3], "$Int"] }, 1] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_value_index_Column(bool async)
    {
        await base.Inline_collection_value_index_Column(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [[1, "$Int", 3], "$Int"] }, 1] } } }
""");
    }
#endif

#if EF9
    public override async Task Inline_collection_List_value_index_Column(bool async)
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_value_index_Column(async), "List");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_index_Column_equal_Column(bool async)
    {
        await base.Parameter_collection_index_Column_equal_Column(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [[0, 2, 3], "$Int"] }, "$Int"] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_index_Column_equal_constant(bool async)
    {
        await base.Parameter_collection_index_Column_equal_constant(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [[1, 2, 3], "$Int"] }, 1] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_ElementAt(bool async)
    {
        await base.Column_collection_ElementAt(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.1" : 10 } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_First(bool async)
    {
        await base.Column_collection_First(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.0" : 1 } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_FirstOrDefault(bool async)
    {
        await base.Column_collection_FirstOrDefault(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$cond" : { "if" : { "$eq" : [{ "$size" : "$Ints" }, 0] }, "then" : 0, "else" : { "$arrayElemAt" : ["$Ints", 0] } } }, 1] } } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_Single(bool async)
    {
        // Fails: Single/SingleOrDefault over a primitive collection not translated EF-X502
        await SpecFails.ThrowsAsync(() => base.Column_collection_Single(async), "Expression not supported");
    }
#endif

#if EF9
    public override async Task Column_collection_SingleOrDefault(bool async)
    {
        // Fails: Single/SingleOrDefault over a primitive collection not translated EF-X502
        await SpecFails.ThrowsAsync(() => base.Column_collection_SingleOrDefault(async), "Expression not supported");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Skip(bool async)
    {
        await base.Column_collection_Skip(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$slice" : ["$Ints", 1, 2147483647] } }, 2] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Take(bool async)
    {
        await base.Column_collection_Take(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : [11, { "$slice" : ["$Ints", 2] }] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Skip_Take(bool async)
    {
        await base.Column_collection_Skip_Take(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : [11, { "$slice" : ["$Ints", 1, 2] }] } } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_Where_Skip(bool async)
    {
        await base.Column_collection_Where_Skip(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$slice" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } }, 1, 2147483647] } }, 3] } } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_Where_Take(bool async)
    {
        await base.Column_collection_Where_Take(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$slice" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } }, 2] } }, 2] } } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_Where_Skip_Take(bool async)
    {
        await base.Column_collection_Where_Skip_Take(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$slice" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } }, 1, 2] } }, 1] } } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_Contains_over_subquery(bool async)
    {
        await base.Column_collection_Contains_over_subquery(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : [11, { "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } }] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_OrderByDescending_ElementAt(bool async)
    {
        await base.Column_collection_OrderByDescending_ElementAt(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [{ "$sortArray" : { "input" : "$Ints", "sortBy" : -1 } }, 0] }, 111] } } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_Where_ElementAt(bool async)
    {
        await base.Column_collection_Where_ElementAt(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 1] } } }, 0] }, 11] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Any(bool async)
    {
        await base.Column_collection_Any(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints.0" : { "$exists" : true } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Distinct(bool async)
    {
        await base.Column_collection_Distinct(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : "$Ints" } }, 3] } } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_SelectMany(bool async)
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Column_collection_SelectMany(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Column_collection_SelectMany_with_filter(bool async)
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Column_collection_SelectMany_with_filter(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Column_collection_SelectMany_with_Select_to_anonymous_type(bool async)
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Column_collection_SelectMany_with_Select_to_anonymous_type(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_projection_from_top_level(bool async)
    {
        await base.Column_collection_projection_from_top_level(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "Ints" : "$Ints", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Join_parameter_collection(bool async)
    {
        // Fails: Join over primitive collection not translated EF-X017
        await SpecFails.ThrowsAsync(() => base.Column_collection_Join_parameter_collection(async), "Join");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Join_ordered_column_collection(bool async)
    {
        // Fails: Join over primitive collection not translated EF-X017
        await SpecFails.ThrowsAsync(() => base.Inline_collection_Join_ordered_column_collection(async), "Join");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_Concat_column_collection(bool async)
    {
        await base.Parameter_collection_Concat_column_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$concatArrays" : [[11, 111], "$Ints"] } }, 2] } } }
""");
    }
#endif

#if EF9
    public override async Task Parameter_collection_with_type_inference_for_JsonScalarExpression(bool async)
    {
        await base.Parameter_collection_with_type_inference_for_JsonScalarExpression(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$project" : { "_v" : { "$cond" : { "if" : { "$ne" : ["$_id", 0] }, "then" : { "$arrayElemAt" : [["one", "two"], { "$mod" : ["$Int", 2] }] }, "else" : "foo" } }, "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Union_parameter_collection(bool async)
    {
        await base.Column_collection_Union_parameter_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : ["$Ints", [11, 111]] } }, 2] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Intersect_inline_collection(bool async)
    {
        await base.Column_collection_Intersect_inline_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setIntersection" : ["$Ints", [11, 111]] } }, 2] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Except_column_collection(bool async)
    {
        await base.Inline_collection_Except_column_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : { "$setDifference" : [[11, 111], "$Ints"] }, "as" : "i", "in" : { "$cond" : { "if" : { "$eq" : [{ "$mod" : ["$$i", 2] }, 1] }, "then" : 1, "else" : 0 } } } } }, 2] } } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_Where_Union(bool async)
    {
        await base.Column_collection_Where_Union(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$gt" : ["$$i", 100] } } }, [50]] } }, 2] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_equality_parameter_collection(bool async)
    {
        await base.Column_collection_equality_parameter_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints" : [1, 10] } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Concat_parameter_collection_equality_inline_collection(bool async)
    {
        await base.Column_collection_Concat_parameter_collection_equality_inline_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$concatArrays" : ["$Ints", [1, 10]] }, [1, 11, 111, 1, 10]] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_equality_inline_collection(bool async)
    {
        await base.Column_collection_equality_inline_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints" : [1, 10] } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_equality_inline_collection_with_parameters(bool async)
    {
        await base.Column_collection_equality_inline_collection_with_parameters(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "Ints" : [1, 10] } }
""");
    }
#endif

#if EF9
    public override async Task Column_collection_Where_equality_inline_collection(bool async)
    {
        await base.Column_collection_Where_equality_inline_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$filter" : { "input" : "$Ints", "as" : "i", "cond" : { "$ne" : ["$$i", 11] } } }, [1, 111]] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_in_subquery_Count_as_compiled_query(bool async)
    {
        await base.Parameter_collection_in_subquery_Count_as_compiled_query(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : [111], "as" : "i", "in" : { "$cond" : { "if" : { "$gt" : ["$$i", "$_id"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }, { "$count" : "_v" }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_in_subquery_Union_column_collection_as_compiled_query(bool async)
    {
        await base.Parameter_collection_in_subquery_Union_column_collection_as_compiled_query(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : [[111], "$Ints"] } }, 3] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_in_subquery_Union_column_collection(bool async)
    {
        await base.Parameter_collection_in_subquery_Union_column_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : [[111], "$Ints"] } }, 3] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_in_subquery_Union_column_collection_nested(bool async)
    {
        await base.Parameter_collection_in_subquery_Union_column_collection_nested(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : [[111], { "$slice" : [{ "$sortArray" : { "input" : { "$setUnion" : { "$slice" : [{ "$sortArray" : { "input" : "$Ints", "sortBy" : 1 } }, 1, 2147483647] } }, "sortBy" : -1 } }, 20] }] } }, 3] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_in_subquery_Union_another_parameter_collection_as_compiled_query(bool async)
    {
        await base.Parameter_collection_in_subquery_Union_another_parameter_collection_as_compiled_query(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.
""");
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_in_subquery_Union_parameter_collection(bool async)
    {
        await base.Column_collection_in_subquery_Union_parameter_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$eq" : [{ "$size" : { "$setUnion" : [{ "$slice" : ["$Ints", 1, 2147483647] }, [10, 111]] } }, 3] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_ints_simple(bool async)
    {
        await base.Project_collection_of_ints_simple(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "Ints" : "$Ints", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_ints_ordered(bool async)
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_collection_of_ints_ordered(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_datetimes_filtered(bool async)
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_collection_of_datetimes_filtered(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_nullable_ints_with_paging(bool async)
    {
        await base.Project_collection_of_nullable_ints_with_paging(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "_v" : { "$slice" : ["$NullableInts", 20] }, "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_nullable_ints_with_paging2(bool async)
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_collection_of_nullable_ints_with_paging2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_nullable_ints_with_paging3(bool async)
    {
        await base.Project_collection_of_nullable_ints_with_paging3(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "_v" : { "$slice" : ["$NullableInts", 2, 2147483647] }, "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_ints_with_distinct(bool async)
    {
        await base.Project_collection_of_ints_with_distinct(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "_v" : { "$setUnion" : "$Ints" }, "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_nullable_ints_with_distinct(bool async)
    {
        await base.Project_collection_of_nullable_ints_with_distinct(async);

        AssertMql();
    }
#endif

#if EF9
    public override async Task Project_collection_of_ints_with_ToList_and_FirstOrDefault(bool async)
    {
        await base.Project_collection_of_ints_with_ToList_and_FirstOrDefault(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "_v" : "$Ints", "_id" : 0 } }, { "$limit" : 1 }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Project_empty_collection_of_nullables_and_collection_only_containing_nulls(bool async)
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_empty_collection_of_nullables_and_collection_only_containing_nulls(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Project_multiple_collections(bool async)
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_multiple_collections(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Project_primitive_collections_element(bool async)
    {
        await base.Project_primitive_collections_element(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$lt" : 4 } } }, { "$sort" : { "_id" : 1 } }, { "$project" : { "Indexer" : { "$arrayElemAt" : ["$Ints", 0] }, "EnumerableElementAt" : { "$arrayElemAt" : ["$DateTimes", 0] }, "QueryableElementAt" : { "$arrayElemAt" : ["$Strings", 1] }, "_id" : 0 } }
""");
    }
#endif

#if EF9
    public override async Task Project_inline_collection(bool async)
    {
        await base.Project_inline_collection(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$project" : { "_ctorArg0" : "$String", "_ctorArg1" : { "$literal" : "foo" }, "_id" : 0 } }
""");
    }
#endif

#if EF9
    public override async Task Project_inline_collection_with_Union(bool async)
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_inline_collection_with_Union(async), "could not be translated");
    }
#endif

#if EF9
    public override async Task Project_inline_collection_with_Concat(bool async)
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_inline_collection_with_Concat(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Nested_contains_with_Lists_and_no_inferred_type_mapping(bool async)
    {
        await base.Nested_contains_with_Lists_and_no_inferred_type_mapping(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : [{ "$cond" : { "if" : { "$in" : ["$Int", { "$literal" : [1, 2, 3] }] }, "then" : { "$literal" : "one" }, "else" : { "$literal" : "two" } } }, { "$literal" : ["one", "two", "three"] }] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Nested_contains_with_arrays_and_no_inferred_type_mapping(bool async)
    {
        await base.Nested_contains_with_arrays_and_no_inferred_type_mapping(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "$expr" : { "$in" : [{ "$cond" : { "if" : { "$in" : ["$Int", { "$literal" : [1, 2, 3] }] }, "then" : { "$literal" : "one" }, "else" : { "$literal" : "two" } } }, { "$literal" : ["one", "two", "three"] }] } } }
""");
    }
#endif

#if EF8
    public override async Task Inline_collection_Count_with_zero_values(bool async)
    {
        // Fails: EF8 only: inline empty-collection Count query returns no rows EF-X507
        await SpecFails.ThrowsAsync(() => base.Inline_collection_Count_with_zero_values(async), "Query returned no results");
    }
#endif

#if EF8
    public override async Task Inline_collection_Contains_with_EF_Constant(bool async)
    {
        await base.Inline_collection_Contains_with_EF_Constant(async);

        AssertMql(
            """
PrimitiveCollectionsEntity.{ "$match" : { "_id" : { "$in" : [2, 999, 1000] } } }
""");
    }
#endif

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    public class PrimitiveCollectionsQueryMongoFixture : PrimitiveCollectionsQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("PrimitiveCollectionsQuery");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // Blocker: the provider cannot serialize a ValueConverter over a nullable value type
            // ("Unsupported ValueConverter for 'Nullable<WrappedId>'"), which fails seeding for every test.
#if (EF10 || EF11)
            modelBuilder.Entity<PrimitiveCollectionsEntity>().Ignore(e => e.NullableWrappedIdWithNullableComparer);
#endif
        }

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.MappedPropertyIgnoredWarning));

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }
    }
}
