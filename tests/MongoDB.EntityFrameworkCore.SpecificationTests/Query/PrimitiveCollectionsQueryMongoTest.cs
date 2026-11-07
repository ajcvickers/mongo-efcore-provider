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

#if EF10
    public override async Task Inline_collection_of_ints_Contains()
    {
        await base.Inline_collection_of_ints_Contains();
    }
#endif

#if EF10
    public override async Task Inline_collection_of_nullable_ints_Contains()
    {
        await base.Inline_collection_of_nullable_ints_Contains();
    }
#endif

#if EF10
    public override async Task Inline_collection_of_nullable_ints_Contains_null()
    {
        await base.Inline_collection_of_nullable_ints_Contains_null();
    }
#endif

#if EF10
    public override async Task Inline_collection_Count_with_zero_values()
    {
        await base.Inline_collection_Count_with_zero_values();
    }
#endif

#if EF10
    public override async Task Inline_collection_Count_with_one_value()
    {
        await base.Inline_collection_Count_with_one_value();
    }
#endif

#if EF10
    public override async Task Inline_collection_Count_with_two_values()
    {
        await base.Inline_collection_Count_with_two_values();
    }
#endif

#if EF10
    public override async Task Inline_collection_Count_with_three_values()
    {
        await base.Inline_collection_Count_with_three_values();
    }
#endif

#if EF10
    public override async Task Inline_collection_Contains_with_zero_values()
    {
        await base.Inline_collection_Contains_with_zero_values();
    }
#endif

#if EF10
    public override async Task Inline_collection_Contains_with_one_value()
    {
        await base.Inline_collection_Contains_with_one_value();
    }
#endif

#if EF10
    public override async Task Inline_collection_Contains_with_two_values()
    {
        await base.Inline_collection_Contains_with_two_values();
    }
#endif

#if EF10
    public override async Task Inline_collection_Contains_with_three_values()
    {
        await base.Inline_collection_Contains_with_three_values();
    }
#endif

#if EF10
    public override async Task Inline_collection_Contains_with_all_parameters()
    {
        await base.Inline_collection_Contains_with_all_parameters();
    }
#endif

#if EF10
    public override async Task Inline_collection_Contains_with_constant_and_parameter()
    {
        await base.Inline_collection_Contains_with_constant_and_parameter();
    }
#endif

#if EF10
    public override async Task Inline_collection_Contains_with_mixed_value_types()
    {
        await base.Inline_collection_Contains_with_mixed_value_types();
    }
#endif

#if EF10
    public override async Task Inline_collection_List_Contains_with_mixed_value_types()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Contains_with_mixed_value_types(), "List");
    }
#endif

#if EF10
    public override async Task Inline_collection_Contains_as_Any_with_predicate()
    {
        await base.Inline_collection_Contains_as_Any_with_predicate();
    }
#endif

#if EF10
    public override async Task Inline_collection_negated_Contains_as_All()
    {
        await base.Inline_collection_negated_Contains_as_All();
    }
#endif

#if EF10
    public override async Task Inline_collection_Min_with_two_values()
    {
        await base.Inline_collection_Min_with_two_values();
    }
#endif

#if EF10
    public override async Task Inline_collection_List_Min_with_two_values()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Min_with_two_values(), "List");
    }
#endif

#if EF10
    public override async Task Inline_collection_Max_with_two_values()
    {
        await base.Inline_collection_Max_with_two_values();
    }
#endif

#if EF10
    public override async Task Inline_collection_List_Max_with_two_values()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Max_with_two_values(), "List");
    }
#endif

#if EF10
    public override async Task Inline_collection_Min_with_three_values()
    {
        await base.Inline_collection_Min_with_three_values();
    }
#endif

#if EF10
    public override async Task Inline_collection_List_Min_with_three_values()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Min_with_three_values(), "List");
    }
#endif

#if EF10
    public override async Task Inline_collection_Max_with_three_values()
    {
        await base.Inline_collection_Max_with_three_values();
    }
#endif

#if EF10
    public override async Task Inline_collection_List_Max_with_three_values()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_Max_with_three_values(), "List");
    }
#endif

#if EF10
    public override async Task Inline_collection_of_nullable_value_type_Min()
    {
        await base.Inline_collection_of_nullable_value_type_Min();
    }
#endif

#if EF10
    public override async Task Inline_collection_of_nullable_value_type_Max()
    {
        await base.Inline_collection_of_nullable_value_type_Max();
    }
#endif

#if EF10
    public override async Task Inline_collection_of_nullable_value_type_with_null_Min()
    {
        await base.Inline_collection_of_nullable_value_type_with_null_Min();
    }
#endif

#if EF10
    public override async Task Inline_collection_of_nullable_value_type_with_null_Max()
    {
        await base.Inline_collection_of_nullable_value_type_with_null_Max();
    }
#endif

#if EF10
    public override async Task Inline_collection_with_single_parameter_element_Contains()
    {
        await base.Inline_collection_with_single_parameter_element_Contains();
    }
#endif

#if EF10
    public override async Task Inline_collection_with_single_parameter_element_Count()
    {
        await base.Inline_collection_with_single_parameter_element_Count();
    }
#endif

#if EF10
    public override async Task Inline_collection_Contains_with_EF_Parameter()
    {
        await base.Inline_collection_Contains_with_EF_Parameter();
    }
#endif

#if EF10
    public override async Task Inline_collection_Contains_with_IEnumerable_EF_Parameter()
    {
        await base.Inline_collection_Contains_with_IEnumerable_EF_Parameter();
    }
#endif

#if EF10
    public override async Task Inline_collection_Count_with_column_predicate_with_EF_Parameter()
    {
        await base.Inline_collection_Count_with_column_predicate_with_EF_Parameter();
    }
#endif

#if EF10
    public override async Task Parameter_collection_Count()
    {
        await base.Parameter_collection_Count();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_ints_Contains_int()
    {
        await base.Parameter_collection_of_ints_Contains_int();
    }
#endif

#if EF10
    public override async Task Parameter_collection_HashSet_of_ints_Contains_int()
    {
        await base.Parameter_collection_HashSet_of_ints_Contains_int();
    }
#endif

#if EF10
    public override async Task Parameter_collection_ImmutableArray_of_ints_Contains_int()
    {
        await base.Parameter_collection_ImmutableArray_of_ints_Contains_int();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_ints_Contains_nullable_int()
    {
        await base.Parameter_collection_of_ints_Contains_nullable_int();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_nullable_ints_Contains_int()
    {
        await base.Parameter_collection_of_nullable_ints_Contains_int();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_nullable_ints_Contains_nullable_int()
    {
        await base.Parameter_collection_of_nullable_ints_Contains_nullable_int();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_nullable_ints_Contains_nullable_int_with_EF_Parameter()
    {
        await base.Parameter_collection_of_nullable_ints_Contains_nullable_int_with_EF_Parameter();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_structs_Contains_struct()
    {
        await base.Parameter_collection_of_structs_Contains_struct();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_structs_Contains_nullable_struct()
    {
        // Fails: NullReferenceException for Contains of a null item EF-X503
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_of_structs_Contains_nullable_struct(), "NullReferenceException");
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_structs_Contains_nullable_struct_with_nullable_comparer()
    {
        // Fails: Contains over collection of structs with value converter not translated or serializer mismatch EF-X504
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_of_structs_Contains_nullable_struct_with_nullable_comparer(), "WrappedId");
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_nullable_structs_Contains_struct()
    {
        await base.Parameter_collection_of_nullable_structs_Contains_struct();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_nullable_structs_Contains_nullable_struct()
    {
        // Fails: Contains over collection of structs with value converter not translated or serializer mismatch EF-X504
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_of_nullable_structs_Contains_nullable_struct(), "WrappedId");
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_nullable_structs_Contains_nullable_struct_with_nullable_comparer()
    {
        // Fails: Contains over collection of structs with value converter not translated or serializer mismatch EF-X504
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_of_nullable_structs_Contains_nullable_struct_with_nullable_comparer(), "WrappedId");
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_strings_Contains_string()
    {
        await base.Parameter_collection_of_strings_Contains_string();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_strings_Contains_nullable_string()
    {
        await base.Parameter_collection_of_strings_Contains_nullable_string();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_nullable_strings_Contains_string()
    {
        await base.Parameter_collection_of_nullable_strings_Contains_string();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_nullable_strings_Contains_nullable_string()
    {
        await base.Parameter_collection_of_nullable_strings_Contains_nullable_string();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_DateTimes_Contains()
    {
        await base.Parameter_collection_of_DateTimes_Contains();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_bools_Contains()
    {
        await base.Parameter_collection_of_bools_Contains();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_enums_Contains()
    {
        await base.Parameter_collection_of_enums_Contains();
    }
#endif

#if EF10
    public override async Task Parameter_collection_null_Contains()
    {
        // Fails: NullReferenceException for Contains of a null item EF-X503
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_null_Contains(), "NullReferenceException");
    }
#endif

#if EF10
    public override async Task Parameter_collection_empty_Contains()
    {
        await base.Parameter_collection_empty_Contains();
    }
#endif

#if EF10
    public override async Task Parameter_collection_empty_Join()
    {
        // Fails: Join over primitive collection not translated EF-X017
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_empty_Join(), "Join");
    }
#endif

#if EF10
    public override async Task Parameter_collection_Contains_with_EF_Constant()
    {
        await base.Parameter_collection_Contains_with_EF_Constant();
    }
#endif

#if EF10
    public override async Task Parameter_collection_Where_with_EF_Constant_Where_Any()
    {
        await base.Parameter_collection_Where_with_EF_Constant_Where_Any();
    }
#endif

#if EF10
    public override async Task Parameter_collection_Count_with_column_predicate_with_EF_Constant()
    {
        await base.Parameter_collection_Count_with_column_predicate_with_EF_Constant();
    }
#endif

#if EF10
    public override async Task Parameter_collection_Count_with_huge_number_of_values()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values();
    }
#endif

#if EF10
    public override async Task Parameter_collection_Count_with_huge_number_of_values_over_5_operations()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values_over_5_operations();
    }
#endif

#if EF10
    public override async Task Parameter_collection_Count_with_huge_number_of_values_over_5_operations_same_parameter()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values_over_5_operations_same_parameter();
    }
#endif

#if EF10
    public override async Task Parameter_collection_Count_with_huge_number_of_values_over_2_operations_same_parameter_different_type_mapping()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values_over_2_operations_same_parameter_different_type_mapping();
    }
#endif

#if EF10
    public override async Task Parameter_collection_Count_with_huge_number_of_values_over_5_operations_forced_constants()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values_over_5_operations_forced_constants();
    }
#endif

#if EF10
    public override async Task Parameter_collection_Count_with_huge_number_of_values_over_5_operations_mixed_parameters_constants()
    {
        await base.Parameter_collection_Count_with_huge_number_of_values_over_5_operations_mixed_parameters_constants();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_same_parameter()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_same_parameter();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_2_operations_same_parameter_different_type_mapping()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_2_operations_same_parameter_different_type_mapping();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_forced_constants()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_forced_constants();
    }
#endif

#if EF10
    public override async Task Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_mixed_parameters_constants()
    {
        await base.Parameter_collection_of_ints_Contains_int_with_huge_number_of_values_over_5_operations_mixed_parameters_constants();
    }
#endif

#if EF10
    public override async Task Column_collection_of_ints_Contains()
    {
        await base.Column_collection_of_ints_Contains();
    }
#endif

#if EF10
    public override async Task Column_collection_of_nullable_ints_Contains()
    {
        await base.Column_collection_of_nullable_ints_Contains();
    }
#endif

#if EF10
    public override async Task Column_collection_of_nullable_ints_Contains_null()
    {
        await base.Column_collection_of_nullable_ints_Contains_null();
    }
#endif

#if EF10
    public override async Task Column_collection_of_strings_contains_null()
    {
        await base.Column_collection_of_strings_contains_null();
    }
#endif

#if EF10
    public override async Task Column_collection_of_nullable_strings_contains_null()
    {
        await base.Column_collection_of_nullable_strings_contains_null();
    }
#endif

#if EF10
    public override async Task Column_collection_of_bools_Contains()
    {
        await base.Column_collection_of_bools_Contains();
    }
#endif

#if EF10
    public override async Task Contains_on_Enumerable()
    {
        await base.Contains_on_Enumerable();
    }
#endif

#if EF10
    public override async Task Contains_on_MemoryExtensions()
    {
        await base.Contains_on_MemoryExtensions();
    }
#endif

#if EF10
    public override async Task Contains_with_MemoryExtensions_with_null_comparer()
    {
        await base.Contains_with_MemoryExtensions_with_null_comparer();
    }
#endif

#if EF10
    public override async Task Column_collection_Count_method()
    {
        await base.Column_collection_Count_method();
    }
#endif

#if EF10
    public override async Task Column_collection_Length()
    {
        await base.Column_collection_Length();
    }
#endif

#if EF10
    public override async Task Column_collection_Count_with_predicate()
    {
        await base.Column_collection_Count_with_predicate();
    }
#endif

#if EF10
    public override async Task Column_collection_Where_Count()
    {
        await base.Column_collection_Where_Count();
    }
#endif

#if EF10
    public override async Task Column_collection_index_int()
    {
        await base.Column_collection_index_int();
    }
#endif

#if EF10
    public override async Task Column_collection_index_string()
    {
        await base.Column_collection_index_string();
    }
#endif

#if EF10
    public override async Task Column_collection_index_datetime()
    {
        await base.Column_collection_index_datetime();
    }
#endif

#if EF10
    public override async Task Column_collection_index_beyond_end()
    {
        await base.Column_collection_index_beyond_end();
    }
#endif

#if EF10
    public override async Task Nullable_reference_column_collection_index_equals_nullable_column()
    {
        // Fails: Wrong result: collection index compared with nullable column EF-X505
        await SpecFails.ThrowsAsync(() => base.Nullable_reference_column_collection_index_equals_nullable_column(), "Values differ");
    }
#endif

#if EF10
    public override async Task Non_nullable_reference_column_collection_index_equals_nullable_column()
    {
        await base.Non_nullable_reference_column_collection_index_equals_nullable_column();
    }
#endif

#if EF10
    public override async Task Inline_collection_index_Column()
    {
        await base.Inline_collection_index_Column();
    }
#endif

#if EF10
    public override async Task Inline_collection_index_Column_with_EF_Constant()
    {
        await base.Inline_collection_index_Column_with_EF_Constant();
    }
#endif

#if EF10
    public override async Task Inline_collection_value_index_Column()
    {
        await base.Inline_collection_value_index_Column();
    }
#endif

#if EF10
    public override async Task Inline_collection_List_value_index_Column()
    {
        // Fails: Inline List<T> collection (Min/Max/Contains/index) not translated or serialized EF-X501
        await SpecFails.ThrowsAsync(() => base.Inline_collection_List_value_index_Column(), "List");
    }
#endif

#if EF10
    public override async Task Parameter_collection_index_Column_equal_Column()
    {
        await base.Parameter_collection_index_Column_equal_Column();
    }
#endif

#if EF10
    public override async Task Parameter_collection_index_Column_equal_constant()
    {
        await base.Parameter_collection_index_Column_equal_constant();
    }
#endif

#if EF10
    public override async Task Column_collection_ElementAt()
    {
        await base.Column_collection_ElementAt();
    }
#endif

#if EF10
    public override async Task Column_collection_First()
    {
        await base.Column_collection_First();
    }
#endif

#if EF10
    public override async Task Column_collection_FirstOrDefault()
    {
        await base.Column_collection_FirstOrDefault();
    }
#endif

#if EF10
    public override async Task Column_collection_Single()
    {
        // Fails: Single/SingleOrDefault over a primitive collection not translated EF-X502
        await SpecFails.ThrowsAsync(() => base.Column_collection_Single(), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Column_collection_SingleOrDefault()
    {
        // Fails: Single/SingleOrDefault over a primitive collection not translated EF-X502
        await SpecFails.ThrowsAsync(() => base.Column_collection_SingleOrDefault(), "Expression not supported");
    }
#endif

#if EF10
    public override async Task Column_collection_Skip()
    {
        await base.Column_collection_Skip();
    }
#endif

#if EF10
    public override async Task Column_collection_Take()
    {
        await base.Column_collection_Take();
    }
#endif

#if EF10
    public override async Task Column_collection_Skip_Take()
    {
        await base.Column_collection_Skip_Take();
    }
#endif

#if EF10
    public override async Task Column_collection_Where_Skip()
    {
        await base.Column_collection_Where_Skip();
    }
#endif

#if EF10
    public override async Task Column_collection_Where_Take()
    {
        await base.Column_collection_Where_Take();
    }
#endif

#if EF10
    public override async Task Column_collection_Where_Skip_Take()
    {
        await base.Column_collection_Where_Skip_Take();
    }
#endif

#if EF10
    public override async Task Column_collection_Contains_over_subquery()
    {
        await base.Column_collection_Contains_over_subquery();
    }
#endif

#if EF10
    public override async Task Column_collection_OrderByDescending_ElementAt()
    {
        await base.Column_collection_OrderByDescending_ElementAt();
    }
#endif

#if EF10
    public override async Task Column_collection_Where_ElementAt()
    {
        await base.Column_collection_Where_ElementAt();
    }
#endif

#if EF10
    public override async Task Column_collection_Any()
    {
        await base.Column_collection_Any();
    }
#endif

#if EF10
    public override async Task Column_collection_Distinct()
    {
        await base.Column_collection_Distinct();
    }
#endif

#if EF10
    public override async Task Column_collection_SelectMany()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Column_collection_SelectMany(), "could not be translated");
    }
#endif

#if EF10
    public override async Task Column_collection_SelectMany_with_filter()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Column_collection_SelectMany_with_filter(), "could not be translated");
    }
#endif

#if EF10
    public override async Task Column_collection_SelectMany_with_Select_to_anonymous_type()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Column_collection_SelectMany_with_Select_to_anonymous_type(), "could not be translated");
    }
#endif

#if EF10
    public override async Task Column_collection_projection_from_top_level()
    {
        await base.Column_collection_projection_from_top_level();
    }
#endif

#if EF10
    public override async Task Column_collection_Join_parameter_collection()
    {
        // Fails: Join over primitive collection not translated EF-X017
        await SpecFails.ThrowsAsync(() => base.Column_collection_Join_parameter_collection(), "Join");
    }
#endif

#if EF10
    public override async Task Inline_collection_Join_ordered_column_collection()
    {
        // Fails: Join over primitive collection not translated EF-X017
        await SpecFails.ThrowsAsync(() => base.Inline_collection_Join_ordered_column_collection(), "Join");
    }
#endif

#if EF10
    public override async Task Parameter_collection_Concat_column_collection()
    {
        await base.Parameter_collection_Concat_column_collection();
    }
#endif

#if EF10
    public override async Task Parameter_collection_with_type_inference_for_JsonScalarExpression()
    {
        await base.Parameter_collection_with_type_inference_for_JsonScalarExpression();
    }
#endif

#if EF10
    public override async Task Column_collection_Union_parameter_collection()
    {
        await base.Column_collection_Union_parameter_collection();
    }
#endif

#if EF10
    public override async Task Column_collection_Intersect_inline_collection()
    {
        await base.Column_collection_Intersect_inline_collection();
    }
#endif

#if EF10
    public override async Task Inline_collection_Except_column_collection()
    {
        await base.Inline_collection_Except_column_collection();
    }
#endif

#if EF10
    public override async Task Column_collection_Where_Union()
    {
        await base.Column_collection_Where_Union();
    }
#endif

#if EF10
    public override async Task Column_collection_equality_parameter_collection()
    {
        await base.Column_collection_equality_parameter_collection();
    }
#endif

#if EF10
    public override async Task Column_collection_Concat_parameter_collection_equality_inline_collection()
    {
        await base.Column_collection_Concat_parameter_collection_equality_inline_collection();
    }
#endif

#if EF10
    public override async Task Column_collection_equality_inline_collection()
    {
        await base.Column_collection_equality_inline_collection();
    }
#endif

#if EF10
    public override async Task Column_collection_equality_inline_collection_with_parameters()
    {
        await base.Column_collection_equality_inline_collection_with_parameters();
    }
#endif

#if EF10
    public override async Task Column_collection_Where_equality_inline_collection()
    {
        await base.Column_collection_Where_equality_inline_collection();
    }
#endif

#if EF10
    public override async Task Parameter_collection_in_subquery_Count_as_compiled_query()
    {
        await base.Parameter_collection_in_subquery_Count_as_compiled_query();
    }
#endif

#if EF10
    public override async Task Parameter_collection_in_subquery_Union_column_collection_as_compiled_query()
    {
        await base.Parameter_collection_in_subquery_Union_column_collection_as_compiled_query();
    }
#endif

#if EF10
    public override async Task Parameter_collection_in_subquery_Union_column_collection()
    {
        await base.Parameter_collection_in_subquery_Union_column_collection();
    }
#endif

#if EF10
    public override async Task Parameter_collection_in_subquery_Union_column_collection_nested()
    {
        await base.Parameter_collection_in_subquery_Union_column_collection_nested();
    }
#endif

    public override void Parameter_collection_in_subquery_and_Convert_as_compiled_query()
    {
        base.Parameter_collection_in_subquery_and_Convert_as_compiled_query();
    }

#if EF10
    public override async Task Parameter_collection_in_subquery_Union_another_parameter_collection_as_compiled_query()
    {
        await base.Parameter_collection_in_subquery_Union_another_parameter_collection_as_compiled_query();
    }
#endif

#if EF10
    public override async Task Column_collection_in_subquery_Union_parameter_collection()
    {
        await base.Column_collection_in_subquery_Union_parameter_collection();
    }
#endif

#if EF10
    public override async Task Project_collection_of_ints_simple()
    {
        await base.Project_collection_of_ints_simple();
    }
#endif

#if EF10
    public override async Task Project_collection_of_ints_ordered()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_collection_of_ints_ordered(), "could not be translated");
    }
#endif

#if EF10
    public override async Task Project_collection_of_datetimes_filtered()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_collection_of_datetimes_filtered(), "could not be translated");
    }
#endif

#if EF10
    public override async Task Project_collection_of_nullable_ints_with_paging()
    {
        await base.Project_collection_of_nullable_ints_with_paging();
    }
#endif

#if EF10
    public override async Task Project_collection_of_nullable_ints_with_paging2()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_collection_of_nullable_ints_with_paging2(), "could not be translated");
    }
#endif

#if EF10
    public override async Task Project_collection_of_nullable_ints_with_paging3()
    {
        await base.Project_collection_of_nullable_ints_with_paging3();
    }
#endif

#if EF10
    public override async Task Project_collection_of_ints_with_distinct()
    {
        await base.Project_collection_of_ints_with_distinct();
    }
#endif

#if EF10
    public override async Task Project_collection_of_nullable_ints_with_distinct()
    {
        await base.Project_collection_of_nullable_ints_with_distinct();
    }
#endif

#if EF10
    public override async Task Project_collection_of_ints_with_ToList_and_FirstOrDefault()
    {
        await base.Project_collection_of_ints_with_ToList_and_FirstOrDefault();
    }
#endif

#if EF10
    public override async Task Project_empty_collection_of_nullables_and_collection_only_containing_nulls()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_empty_collection_of_nullables_and_collection_only_containing_nulls(), "could not be translated");
    }
#endif

#if EF10
    public override async Task Project_multiple_collections()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_multiple_collections(), "could not be translated");
    }
#endif

#if EF10
    public override async Task Project_primitive_collections_element()
    {
        await base.Project_primitive_collections_element();
    }
#endif

#if EF10
    public override async Task Project_inline_collection()
    {
        await base.Project_inline_collection();
    }
#endif

#if EF10
    public override async Task Project_inline_collection_with_Union()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_inline_collection_with_Union(), "could not be translated");
    }
#endif

#if EF10
    public override async Task Project_inline_collection_with_Concat()
    {
        // Fails: Primitive-collection projection/SelectMany/Union/Concat/ordering not translated EF-X500
        await SpecFails.ThrowsAsync(() => base.Project_inline_collection_with_Concat(), "could not be translated");
    }
#endif

#if EF10
    public override async Task Nested_contains_with_Lists_and_no_inferred_type_mapping()
    {
        await base.Nested_contains_with_Lists_and_no_inferred_type_mapping();
    }
#endif

#if EF10
    public override async Task Nested_contains_with_arrays_and_no_inferred_type_mapping()
    {
        await base.Nested_contains_with_arrays_and_no_inferred_type_mapping();
    }
#endif

#if EF10
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
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_of_nullable_ints_Contains(bool async)
    {
        await base.Inline_collection_of_nullable_ints_Contains(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_of_nullable_ints_Contains_null(bool async)
    {
        await base.Inline_collection_of_nullable_ints_Contains_null(async);
    }
#endif

#if EF9
    public override async Task Inline_collection_Count_with_zero_values(bool async)
    {
        await base.Inline_collection_Count_with_zero_values(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Count_with_one_value(bool async)
    {
        await base.Inline_collection_Count_with_one_value(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Count_with_two_values(bool async)
    {
        await base.Inline_collection_Count_with_two_values(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Count_with_three_values(bool async)
    {
        await base.Inline_collection_Count_with_three_values(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_zero_values(bool async)
    {
        await base.Inline_collection_Contains_with_zero_values(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_one_value(bool async)
    {
        await base.Inline_collection_Contains_with_one_value(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_two_values(bool async)
    {
        await base.Inline_collection_Contains_with_two_values(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_three_values(bool async)
    {
        await base.Inline_collection_Contains_with_three_values(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_all_parameters(bool async)
    {
        await base.Inline_collection_Contains_with_all_parameters(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_constant_and_parameter(bool async)
    {
        await base.Inline_collection_Contains_with_constant_and_parameter(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Contains_with_mixed_value_types(bool async)
    {
        await base.Inline_collection_Contains_with_mixed_value_types(async);
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
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_negated_Contains_as_All(bool async)
    {
        await base.Inline_collection_negated_Contains_as_All(async);
    }
#endif

#if EF9
    public override async Task Inline_collection_Min_with_two_values(bool async)
    {
        await base.Inline_collection_Min_with_two_values(async);
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
    }
#endif

#if EF9
    public override async Task Inline_collection_of_nullable_value_type_Max(bool async)
    {
        await base.Inline_collection_of_nullable_value_type_Max(async);
    }
#endif

#if EF9
    public override async Task Inline_collection_of_nullable_value_type_with_null_Min(bool async)
    {
        await base.Inline_collection_of_nullable_value_type_with_null_Min(async);
    }
#endif

#if EF9
    public override async Task Inline_collection_of_nullable_value_type_with_null_Max(bool async)
    {
        await base.Inline_collection_of_nullable_value_type_with_null_Max(async);
    }
#endif

#if EF9
    public override async Task Inline_collection_with_single_parameter_element_Contains(bool async)
    {
        await base.Inline_collection_with_single_parameter_element_Contains(async);
    }
#endif

#if EF9
    public override async Task Inline_collection_with_single_parameter_element_Count(bool async)
    {
        await base.Inline_collection_with_single_parameter_element_Count(async);
    }
#endif

#if EF9
    public override async Task Inline_collection_Contains_with_EF_Parameter(bool async)
    {
        await base.Inline_collection_Contains_with_EF_Parameter(async);
    }
#endif

#if EF9
    public override async Task Inline_collection_Count_with_column_predicate_with_EF_Parameter(bool async)
    {
        await base.Inline_collection_Count_with_column_predicate_with_EF_Parameter(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_Count(bool async)
    {
        await base.Parameter_collection_Count(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_ints_Contains_int(bool async)
    {
        await base.Parameter_collection_of_ints_Contains_int(async);
    }
#endif

#if EF9
    public override async Task Parameter_collection_HashSet_of_ints_Contains_int(bool async)
    {
        await base.Parameter_collection_HashSet_of_ints_Contains_int(async);
    }
#endif

#if EF9
    public override async Task Parameter_collection_ImmutableArray_of_ints_Contains_int(bool async)
    {
        await base.Parameter_collection_ImmutableArray_of_ints_Contains_int(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_ints_Contains_nullable_int(bool async)
    {
        await base.Parameter_collection_of_ints_Contains_nullable_int(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_nullable_ints_Contains_int(bool async)
    {
        await base.Parameter_collection_of_nullable_ints_Contains_int(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_nullable_ints_Contains_nullable_int(bool async)
    {
        await base.Parameter_collection_of_nullable_ints_Contains_nullable_int(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_strings_Contains_string(bool async)
    {
        await base.Parameter_collection_of_strings_Contains_string(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_strings_Contains_nullable_string(bool async)
    {
        await base.Parameter_collection_of_strings_Contains_nullable_string(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_nullable_strings_Contains_string(bool async)
    {
        await base.Parameter_collection_of_nullable_strings_Contains_string(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_nullable_strings_Contains_nullable_string(bool async)
    {
        await base.Parameter_collection_of_nullable_strings_Contains_nullable_string(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_DateTimes_Contains(bool async)
    {
        await base.Parameter_collection_of_DateTimes_Contains(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_bools_Contains(bool async)
    {
        await base.Parameter_collection_of_bools_Contains(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_of_enums_Contains(bool async)
    {
        await base.Parameter_collection_of_enums_Contains(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_null_Contains(bool async)
    {
        // Fails: NullReferenceException for Contains of a null item EF-X503
        await SpecFails.ThrowsAsync(() => base.Parameter_collection_null_Contains(async), "NullReferenceException");
    }
#endif

#if EF9
    public override async Task Parameter_collection_Contains_with_EF_Constant(bool async)
    {
        await base.Parameter_collection_Contains_with_EF_Constant(async);
    }
#endif

#if EF9
    public override async Task Parameter_collection_Where_with_EF_Constant_Where_Any(bool async)
    {
        await base.Parameter_collection_Where_with_EF_Constant_Where_Any(async);
    }
#endif

#if EF9
    public override async Task Parameter_collection_Count_with_column_predicate_with_EF_Constant(bool async)
    {
        await base.Parameter_collection_Count_with_column_predicate_with_EF_Constant(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_ints_Contains(bool async)
    {
        await base.Column_collection_of_ints_Contains(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_nullable_ints_Contains(bool async)
    {
        await base.Column_collection_of_nullable_ints_Contains(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_nullable_ints_Contains_null(bool async)
    {
        await base.Column_collection_of_nullable_ints_Contains_null(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_strings_contains_null(bool async)
    {
        await base.Column_collection_of_strings_contains_null(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_nullable_strings_contains_null(bool async)
    {
        await base.Column_collection_of_nullable_strings_contains_null(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_of_bools_Contains(bool async)
    {
        await base.Column_collection_of_bools_Contains(async);
    }
#endif

#if EF8 || EF9
    public override async Task Contains_on_Enumerable(bool async)
    {
        await base.Contains_on_Enumerable(async);
    }
#endif

#if EF8 || EF9
    public override async Task Contains_on_MemoryExtensions(bool async)
    {
        await base.Contains_on_MemoryExtensions(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Count_method(bool async)
    {
        await base.Column_collection_Count_method(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Length(bool async)
    {
        await base.Column_collection_Length(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Count_with_predicate(bool async)
    {
        await base.Column_collection_Count_with_predicate(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Where_Count(bool async)
    {
        await base.Column_collection_Where_Count(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_index_int(bool async)
    {
        await base.Column_collection_index_int(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_index_string(bool async)
    {
        await base.Column_collection_index_string(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_index_datetime(bool async)
    {
        await base.Column_collection_index_datetime(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_index_beyond_end(bool async)
    {
        await base.Column_collection_index_beyond_end(async);
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
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_index_Column(bool async)
    {
        await base.Inline_collection_index_Column(async);
    }
#endif

#if EF9
    public override async Task Inline_collection_value_index_Column(bool async)
    {
        await base.Inline_collection_value_index_Column(async);
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
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_index_Column_equal_constant(bool async)
    {
        await base.Parameter_collection_index_Column_equal_constant(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_ElementAt(bool async)
    {
        await base.Column_collection_ElementAt(async);
    }
#endif

#if EF9
    public override async Task Column_collection_First(bool async)
    {
        await base.Column_collection_First(async);
    }
#endif

#if EF9
    public override async Task Column_collection_FirstOrDefault(bool async)
    {
        await base.Column_collection_FirstOrDefault(async);
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
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Take(bool async)
    {
        await base.Column_collection_Take(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Skip_Take(bool async)
    {
        await base.Column_collection_Skip_Take(async);
    }
#endif

#if EF9
    public override async Task Column_collection_Where_Skip(bool async)
    {
        await base.Column_collection_Where_Skip(async);
    }
#endif

#if EF9
    public override async Task Column_collection_Where_Take(bool async)
    {
        await base.Column_collection_Where_Take(async);
    }
#endif

#if EF9
    public override async Task Column_collection_Where_Skip_Take(bool async)
    {
        await base.Column_collection_Where_Skip_Take(async);
    }
#endif

#if EF9
    public override async Task Column_collection_Contains_over_subquery(bool async)
    {
        await base.Column_collection_Contains_over_subquery(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_OrderByDescending_ElementAt(bool async)
    {
        await base.Column_collection_OrderByDescending_ElementAt(async);
    }
#endif

#if EF9
    public override async Task Column_collection_Where_ElementAt(bool async)
    {
        await base.Column_collection_Where_ElementAt(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Any(bool async)
    {
        await base.Column_collection_Any(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Distinct(bool async)
    {
        await base.Column_collection_Distinct(async);
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
    }
#endif

#if EF9
    public override async Task Parameter_collection_with_type_inference_for_JsonScalarExpression(bool async)
    {
        await base.Parameter_collection_with_type_inference_for_JsonScalarExpression(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Union_parameter_collection(bool async)
    {
        await base.Column_collection_Union_parameter_collection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Intersect_inline_collection(bool async)
    {
        await base.Column_collection_Intersect_inline_collection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Inline_collection_Except_column_collection(bool async)
    {
        await base.Inline_collection_Except_column_collection(async);
    }
#endif

#if EF9
    public override async Task Column_collection_Where_Union(bool async)
    {
        await base.Column_collection_Where_Union(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_equality_parameter_collection(bool async)
    {
        await base.Column_collection_equality_parameter_collection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_Concat_parameter_collection_equality_inline_collection(bool async)
    {
        await base.Column_collection_Concat_parameter_collection_equality_inline_collection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_equality_inline_collection(bool async)
    {
        await base.Column_collection_equality_inline_collection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_equality_inline_collection_with_parameters(bool async)
    {
        await base.Column_collection_equality_inline_collection_with_parameters(async);
    }
#endif

#if EF9
    public override async Task Column_collection_Where_equality_inline_collection(bool async)
    {
        await base.Column_collection_Where_equality_inline_collection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_in_subquery_Count_as_compiled_query(bool async)
    {
        await base.Parameter_collection_in_subquery_Count_as_compiled_query(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_in_subquery_Union_column_collection_as_compiled_query(bool async)
    {
        await base.Parameter_collection_in_subquery_Union_column_collection_as_compiled_query(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_in_subquery_Union_column_collection(bool async)
    {
        await base.Parameter_collection_in_subquery_Union_column_collection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_in_subquery_Union_column_collection_nested(bool async)
    {
        await base.Parameter_collection_in_subquery_Union_column_collection_nested(async);
    }
#endif

#if EF8 || EF9
    public override async Task Parameter_collection_in_subquery_Union_another_parameter_collection_as_compiled_query(bool async)
    {
        await base.Parameter_collection_in_subquery_Union_another_parameter_collection_as_compiled_query(async);
    }
#endif

#if EF8 || EF9
    public override async Task Column_collection_in_subquery_Union_parameter_collection(bool async)
    {
        await base.Column_collection_in_subquery_Union_parameter_collection(async);
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_ints_simple(bool async)
    {
        await base.Project_collection_of_ints_simple(async);
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
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_ints_with_distinct(bool async)
    {
        await base.Project_collection_of_ints_with_distinct(async);
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_of_nullable_ints_with_distinct(bool async)
    {
        await base.Project_collection_of_nullable_ints_with_distinct(async);
    }
#endif

#if EF9
    public override async Task Project_collection_of_ints_with_ToList_and_FirstOrDefault(bool async)
    {
        await base.Project_collection_of_ints_with_ToList_and_FirstOrDefault(async);
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
    }
#endif

#if EF9
    public override async Task Project_inline_collection(bool async)
    {
        await base.Project_inline_collection(async);
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
    }
#endif

#if EF8 || EF9
    public override async Task Nested_contains_with_arrays_and_no_inferred_type_mapping(bool async)
    {
        await base.Nested_contains_with_arrays_and_no_inferred_type_mapping(async);
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
    }
#endif

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
#if EF10
            modelBuilder.Entity<PrimitiveCollectionsEntity>().Ignore(e => e.NullableWrappedIdWithNullableComparer);
#endif
        }

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.MappedPropertyIgnoredWarning));

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server);

            await base.InitializeAsync();
        }
    }
}
