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
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Mapping;

public class CustomConvertersMongoTest(CustomConvertersMongoTest.CustomConvertersMongoFixture fixture)
    : CustomConvertersTestBase<CustomConvertersMongoTest.CustomConvertersMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
#if EF10 || EF9
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Can_compare_enum_to_constant()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Can_compare_enum_to_constant(), "Assert.Single() Failure: The collection was empty");
#elif EF8
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override void Can_compare_enum_to_constant()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Can_compare_enum_to_constant(), "Assert.Single() Failure: The collection was empty");
#endif

#if EF10 || EF9
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Can_compare_enum_to_parameter()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Can_compare_enum_to_parameter(), "Assert.Single() Failure: The collection was empty");
#elif EF8
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override void Can_compare_enum_to_parameter()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Can_compare_enum_to_parameter(), "Assert.Single() Failure: The collection was empty");
#endif

    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Can_filter_projection_with_captured_enum_variable(bool async)
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Can_filter_projection_with_captured_enum_variable(async), "Assert.Single() Failure: The collection was empty");

    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Can_filter_projection_with_inline_enum_variable(bool async)
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Can_filter_projection_with_inline_enum_variable(async), "Assert.Single() Failure: The collection was empty");

#if EF10 || EF9
    public override Task Can_insert_and_query_struct_to_string_converter_for_pk()
        => base.Can_insert_and_query_struct_to_string_converter_for_pk();
#elif EF8
    public override void Can_insert_and_query_struct_to_string_converter_for_pk()
        => base.Can_insert_and_query_struct_to_string_converter_for_pk();
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_all_non_nullable_data_types()
        => base.Can_insert_and_read_back_all_non_nullable_data_types();
#elif EF8
    public override void Can_insert_and_read_back_all_non_nullable_data_types()
        => base.Can_insert_and_read_back_all_non_nullable_data_types();
#endif

#if EF10 || EF9
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override Task Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_non_null()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_non_null(), "Unsupported ValueConverter for 'Nullable<Enum16>' encountered. Null co");
#elif EF8
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override void Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_non_null()
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_non_null(), "Unsupported ValueConverter for 'Nullable<Enum16>' encountered. Null co");
#endif

#if EF10 || EF9
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override Task Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_null()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_null(), "Unsupported ValueConverter for 'Nullable<Enum16>' encountered. Null co");
#elif EF8
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override void Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_null()
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_null(), "Unsupported ValueConverter for 'Nullable<Enum16>' encountered. Null co");
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_non_nullable_backed_data_types()
        => base.Can_insert_and_read_back_non_nullable_backed_data_types();
#elif EF8
    public override void Can_insert_and_read_back_non_nullable_backed_data_types()
        => base.Can_insert_and_read_back_non_nullable_backed_data_types();
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_nullable_backed_data_types()
        => base.Can_insert_and_read_back_nullable_backed_data_types();
#elif EF8
    public override void Can_insert_and_read_back_nullable_backed_data_types()
        => base.Can_insert_and_read_back_nullable_backed_data_types();
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_object_backed_data_types()
        => base.Can_insert_and_read_back_object_backed_data_types();
#elif EF8
    public override void Can_insert_and_read_back_object_backed_data_types()
        => base.Can_insert_and_read_back_object_backed_data_types();
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_with_binary_key()
        => base.Can_insert_and_read_back_with_binary_key();
#elif EF8
    public override void Can_insert_and_read_back_with_binary_key()
        => base.Can_insert_and_read_back_with_binary_key();
#endif

#if EF10 || EF9
    // Fails: Value-converter behavior differs from the provider-agnostic expectation (not triaged) EF-X315
    public override Task Can_insert_and_read_back_with_case_insensitive_string_key()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Can_insert_and_read_back_with_case_insensitive_string_key(), "Assert.Same() Failure: Values are not the same instance");
#elif EF8
    // Fails: Value-converter behavior differs from the provider-agnostic expectation (not triaged) EF-X315
    public override void Can_insert_and_read_back_with_case_insensitive_string_key()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Can_insert_and_read_back_with_case_insensitive_string_key(), "Assert.Same() Failure: Values are not the same instance");
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_with_null_binary_foreign_key()
        => base.Can_insert_and_read_back_with_null_binary_foreign_key();
#elif EF8
    public override void Can_insert_and_read_back_with_null_binary_foreign_key()
        => base.Can_insert_and_read_back_with_null_binary_foreign_key();
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_with_null_string_foreign_key()
        => base.Can_insert_and_read_back_with_null_string_foreign_key();
#elif EF8
    public override void Can_insert_and_read_back_with_null_string_foreign_key()
        => base.Can_insert_and_read_back_with_null_string_foreign_key();
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_with_string_key()
        => base.Can_insert_and_read_back_with_string_key();
#elif EF8
    public override void Can_insert_and_read_back_with_string_key()
        => base.Can_insert_and_read_back_with_string_key();
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_with_string_list()
        => base.Can_insert_and_read_back_with_string_list();
#elif EF8
    public override void Can_insert_and_read_back_with_string_list()
        => base.Can_insert_and_read_back_with_string_list();
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_with_max_length_set()
        => base.Can_insert_and_read_with_max_length_set();
#elif EF8
    public override void Can_insert_and_read_with_max_length_set()
        => base.Can_insert_and_read_with_max_length_set();
#endif

#if EF10 || EF9
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Can_insert_query_multiline_string()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Can_insert_query_multiline_string(), "Assert.Single() Failure: The collection was empty");
#elif EF8
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override void Can_insert_query_multiline_string()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Can_insert_query_multiline_string(), "Assert.Single() Failure: The collection was empty");
#endif

#if EF10 || EF9
    public override Task Can_perform_query_with_ansi_strings_test()
        => base.Can_perform_query_with_ansi_strings_test();
#elif EF8
    public override void Can_perform_query_with_ansi_strings_test()
        => base.Can_perform_query_with_ansi_strings_test();
#endif

#if EF10 || EF9
    public override Task Can_perform_query_with_max_length()
        => base.Can_perform_query_with_max_length();
#elif EF8
    public override void Can_perform_query_with_max_length()
        => base.Can_perform_query_with_max_length();
#endif

#if EF10 || EF9
    public override Task Can_query_and_update_with_conversion_for_custom_struct()
        => base.Can_query_and_update_with_conversion_for_custom_struct();
#elif EF8
    public override void Can_query_and_update_with_conversion_for_custom_struct()
        => base.Can_query_and_update_with_conversion_for_custom_struct();
#endif

#if EF10 || EF9
    public override Task Can_query_and_update_with_conversion_for_custom_type()
        => base.Can_query_and_update_with_conversion_for_custom_type();
#elif EF8
    public override void Can_query_and_update_with_conversion_for_custom_type()
        => base.Can_query_and_update_with_conversion_for_custom_type();
#endif

#if EF10 || EF9
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override Task Can_query_and_update_with_nullable_converter_on_primary_key()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_query_and_update_with_nullable_converter_on_primary_key(), "Unsupported ValueConverter for 'Nullable<Int32>' encountered. Null con");
#elif EF8
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override void Can_query_and_update_with_nullable_converter_on_primary_key()
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_query_and_update_with_nullable_converter_on_primary_key(), "Unsupported ValueConverter for 'Nullable<Int32>' encountered. Null con");
#endif

#if EF10 || EF9
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override Task Can_query_and_update_with_nullable_converter_on_unique_index()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_query_and_update_with_nullable_converter_on_unique_index(), "Unsupported ValueConverter for 'Nullable<SocialSecurityNumber>' encoun");
#elif EF8
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override void Can_query_and_update_with_nullable_converter_on_unique_index()
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_query_and_update_with_nullable_converter_on_unique_index(), "Unsupported ValueConverter for 'Nullable<SocialSecurityNumber>' encoun");
#endif

    public override Task Can_query_custom_type_not_mapped_by_default_equality(bool async)
        => base.Can_query_custom_type_not_mapped_by_default_equality(async);

#if EF10 || EF9
    // Fails: ValueConverterSerializer lacks IHasRepresentationSerializer for queries over converted properties EF-X302
    public override Task Can_query_using_any_data_type()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_query_using_any_data_type(), "Serializer class ValueConverterSerializer`2 does not implement IHasRep");
#elif EF8
    // Fails: ValueConverterSerializer lacks IHasRepresentationSerializer for queries over converted properties EF-X302
    public override void Can_query_using_any_data_type()
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_query_using_any_data_type(), "Serializer class ValueConverterSerializer`2 does not implement IHasRep");
#endif

#if EF10 || EF9
    public override Task Can_query_using_any_data_type_nullable_shadow()
        => base.Can_query_using_any_data_type_nullable_shadow();
#elif EF8
    public override void Can_query_using_any_data_type_nullable_shadow()
        => base.Can_query_using_any_data_type_nullable_shadow();
#endif

#if EF10 || EF9
    // Fails: ValueConverterSerializer lacks IHasRepresentationSerializer for queries over converted properties EF-X302
    public override Task Can_query_using_any_data_type_shadow()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_query_using_any_data_type_shadow(), "Serializer class ValueConverterSerializer`2 does not implement IHasRep");
#elif EF8
    // Fails: ValueConverterSerializer lacks IHasRepresentationSerializer for queries over converted properties EF-X302
    public override void Can_query_using_any_data_type_shadow()
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_query_using_any_data_type_shadow(), "Serializer class ValueConverterSerializer`2 does not implement IHasRep");
#endif

#if EF10 || EF9
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override Task Can_query_using_any_nullable_data_type()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_query_using_any_nullable_data_type(), "Unsupported ValueConverter for 'Nullable<Enum16>' encountered. Null co");
#elif EF8
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override void Can_query_using_any_nullable_data_type()
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_query_using_any_nullable_data_type(), "Unsupported ValueConverter for 'Nullable<Enum16>' encountered. Null co");
#endif

#if EF10 || EF9
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override Task Can_query_using_any_nullable_data_type_as_literal()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_query_using_any_nullable_data_type_as_literal(), "Unsupported ValueConverter for 'Nullable<Enum16>' encountered. Null co");
#elif EF8
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override void Can_query_using_any_nullable_data_type_as_literal()
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_query_using_any_nullable_data_type_as_literal(), "Unsupported ValueConverter for 'Nullable<Enum16>' encountered. Null co");
#endif

#if EF10 || EF9
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override Task Can_query_with_null_parameters_using_any_nullable_data_type()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_query_with_null_parameters_using_any_nullable_data_type(), "Unsupported ValueConverter for 'Nullable<Enum16>' encountered. Null co");
#elif EF8
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override void Can_query_with_null_parameters_using_any_nullable_data_type()
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_query_with_null_parameters_using_any_nullable_data_type(), "Unsupported ValueConverter for 'Nullable<Enum16>' encountered. Null co");
#endif

#if EF10
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Can_read_back_bool_mapped_as_int_through_navigation()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Can_read_back_bool_mapped_as_int_through_navigation(), "Assert.Single() Failure: The collection was empty");
#elif EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_read_back_bool_mapped_as_int_through_navigation()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_back_bool_mapped_as_int_through_navigation(), "The LINQ expression 'DbSet<Animal>()");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Can_read_back_bool_mapped_as_int_through_navigation()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_back_bool_mapped_as_int_through_navigation(), "The LINQ expression 'DbSet<Animal>()");
#endif

#if EF10 || EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_read_back_mapped_enum_from_collection_first_or_default()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_back_mapped_enum_from_collection_first_or_default(), "The LINQ expression 'DbSet<AnimalIdentification>()' could not be trans");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Can_read_back_mapped_enum_from_collection_first_or_default()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_back_mapped_enum_from_collection_first_or_default(), "The LINQ expression 'DbSet<AnimalIdentification>()' could not be trans");
#endif

    // Fails: Value-converter behavior differs from the provider-agnostic expectation (not triaged) EF-X315
    public override void Collection_enum_as_string_Contains()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Collection_enum_as_string_Contains(), "Assert.Contains() Failure: Sub-string not found");

    // Fails: Value-converter behavior differs from the provider-agnostic expectation (not triaged) EF-X315
    public override void Collection_property_as_scalar_Any()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Collection_property_as_scalar_Any(), "Assert.Throws() Failure: No exception was thrown");

    // Fails: Driver LINQ translation rejects expression over converted property, indexer or subquery EF-X314
    public override void Collection_property_as_scalar_Count_member()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Collection_property_as_scalar_Count_member(), "Assert.Throws() Failure: Exception type was not an exact match");

    // Fails: Value-converter behavior differs from the provider-agnostic expectation (not triaged) EF-X315
    public override void Composition_over_collection_of_complex_mapped_as_scalar()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Composition_over_collection_of_complex_mapped_as_scalar(), "Assert.Equal() Failure: Strings differ");

#if EF10 || EF9
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Field_on_derived_type_retrieved_via_cast_applies_value_converter()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Field_on_derived_type_retrieved_via_cast_applies_value_converter(), "Assert.Single() Failure: The collection was empty");
#elif EF8
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override void Field_on_derived_type_retrieved_via_cast_applies_value_converter()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Field_on_derived_type_retrieved_via_cast_applies_value_converter(), "Assert.Single() Failure: The collection was empty");
#endif

    // Fails: GroupBy issue EF-149
    public override void GroupBy_converted_enum()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.GroupBy_converted_enum(), "The LINQ expression 'DbSet<Entity>()");

#if EF10 || EF9
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Id_object_as_entity_key()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Id_object_as_entity_key(), "Assert.Single() Failure: The collection was empty");
#elif EF8
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override void Id_object_as_entity_key()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Id_object_as_entity_key(), "Assert.Single() Failure: The collection was empty");
#endif

    // Fails: Driver LINQ translation rejects expression over converted property, indexer or subquery EF-X314
    public override void Infer_type_mapping_from_in_subquery_to_item()
        => SpecFails.Sync<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Infer_type_mapping_from_in_subquery_to_item(), "Expression not supported: ");

#if EF10 || EF9
    public override Task Object_to_string_conversion()
        => base.Object_to_string_conversion();
#elif EF8
    public override void Object_to_string_conversion()
        => base.Object_to_string_conversion();
#endif

#if EF10 || EF9
    public override Task Optional_datetime_reading_null_from_database()
        => base.Optional_datetime_reading_null_from_database();
#elif EF8
    public override void Optional_datetime_reading_null_from_database()
        => base.Optional_datetime_reading_null_from_database();
#endif

    // Fails: Value-converter behavior differs from the provider-agnostic expectation (not triaged) EF-X315
    public override void Optional_owned_with_converter_reading_non_nullable_column()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Optional_owned_with_converter_reading_non_nullable_column(), "Assert.Throws() Failure: No exception was thrown");

#if EF10 || EF9
    // Fails: Value-converter behavior differs from the provider-agnostic expectation (not triaged) EF-X315
    public override Task Select_bool_with_value_conversion_is_used()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Select_bool_with_value_conversion_is_used(), "Assert.Equal() Failure: Values differ");
#elif EF8
    // Fails: Value-converter behavior differs from the provider-agnostic expectation (not triaged) EF-X315
    public override void Select_bool_with_value_conversion_is_used()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Select_bool_with_value_conversion_is_used(), "Assert.Equal() Failure: Values differ");
#endif

#if EF10 || EF9
    // Fails: Value-converter behavior differs from the provider-agnostic expectation (not triaged) EF-X315
    public override Task Select_conditional_bool_with_value_conversion_is_used()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Select_conditional_bool_with_value_conversion_is_used(), "Assert.Equal() Failure: Values differ");
#elif EF8
    // Fails: Value-converter behavior differs from the provider-agnostic expectation (not triaged) EF-X315
    public override void Select_conditional_bool_with_value_conversion_is_used()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Select_conditional_bool_with_value_conversion_is_used(), "Assert.Equal() Failure: Values differ");
#endif

#if EF10 || EF9
    // Fails: Driver LINQ translation rejects expression over converted property, indexer or subquery EF-X314
    public override Task Value_conversion_is_appropriately_used_for_join_condition()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Value_conversion_is_appropriately_used_for_join_condition(), "Expression not supported: b => new ");
#elif EF8
    // Fails: Driver LINQ translation rejects expression over converted property, indexer or subquery EF-X314
    public override void Value_conversion_is_appropriately_used_for_join_condition()
        => SpecFails.Sync<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Value_conversion_is_appropriately_used_for_join_condition(), "Expression not supported: b => new ");
#endif

#if EF10
    // Fails: Driver LINQ translation rejects expression over converted property, indexer or subquery EF-X314
    public override Task Value_conversion_is_appropriately_used_for_left_join_condition()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Value_conversion_is_appropriately_used_for_left_join_condition(), "Expression not supported: b => new ");
#elif EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Value_conversion_is_appropriately_used_for_left_join_condition()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Value_conversion_is_appropriately_used_for_left_join_condition(), "The LINQ expression 'DbSet<Blog>()");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Value_conversion_is_appropriately_used_for_left_join_condition()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Value_conversion_is_appropriately_used_for_left_join_condition(), "The LINQ expression 'DbSet<Blog>()");
#endif

    // Fails: Query over converted property fails binding ValueConverterSerializer EF-X303
    public override void Value_conversion_on_enum_collection_contains()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Value_conversion_on_enum_collection_contains(), "MongoDB.EntityFrameworkCore.Serializers.ValueConverterSerializer`2[[Sy");

    // Fails: Driver LINQ translation rejects expression over converted property, indexer or subquery EF-X314
    public override void Value_conversion_with_property_named_value()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Value_conversion_with_property_named_value(), "Assert.Throws() Failure: Exception type was not an exact match");

#if EF10 || EF9
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Where_bool_gets_converted_to_equality_when_value_conversion_is_used()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Where_bool_gets_converted_to_equality_when_value_conversion_is_used(), "Assert.Single() Failure: The collection was empty");
#elif EF8
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override void Where_bool_gets_converted_to_equality_when_value_conversion_is_used()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Where_bool_gets_converted_to_equality_when_value_conversion_is_used(), "Assert.Single() Failure: The collection was empty");
#endif

#if EF10 || EF9
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Where_bool_gets_converted_to_equality_when_value_conversion_is_used_using_EFProperty()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Where_bool_gets_converted_to_equality_when_value_conversion_is_used_using_EFProperty(), "Assert.Single() Failure: The collection was empty");
#elif EF8
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override void Where_bool_gets_converted_to_equality_when_value_conversion_is_used_using_EFProperty()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Where_bool_gets_converted_to_equality_when_value_conversion_is_used_using_EFProperty(), "Assert.Single() Failure: The collection was empty");
#endif

#if EF10 || EF9
    // Fails: Driver LINQ translation rejects expression over converted property, indexer or subquery EF-X314
    public override Task Where_bool_gets_converted_to_equality_when_value_conversion_is_used_using_indexer()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Where_bool_gets_converted_to_equality_when_value_conversion_is_used_using_indexer(), "Expression not supported: b.get_Item(\"IndexerVisible\").");
#elif EF8
    // Fails: Driver LINQ translation rejects expression over converted property, indexer or subquery EF-X314
    public override void Where_bool_gets_converted_to_equality_when_value_conversion_is_used_using_indexer()
        => SpecFails.Sync<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Where_bool_gets_converted_to_equality_when_value_conversion_is_used_using_indexer(), "Expression not supported: b.get_Item(\"IndexerVisible\").");
#endif

#if EF10 || EF9
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Where_bool_with_value_conversion_inside_comparison_doesnt_get_converted_twice()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Where_bool_with_value_conversion_inside_comparison_doesnt_get_converted_twice(), "Assert.Single() Failure: The collection was empty");
#elif EF8
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override void Where_bool_with_value_conversion_inside_comparison_doesnt_get_converted_twice()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Where_bool_with_value_conversion_inside_comparison_doesnt_get_converted_twice(), "Assert.Single() Failure: The collection was empty");
#endif

#if EF10 || EF9
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Where_conditional_bool_with_value_conversion_is_used()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Where_conditional_bool_with_value_conversion_is_used(), "Assert.Single() Failure: The collection was empty");
#elif EF8
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override void Where_conditional_bool_with_value_conversion_is_used()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Where_conditional_bool_with_value_conversion_is_used(), "Assert.Single() Failure: The collection was empty");
#endif

#if EF10 || EF9
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override Task Where_negated_bool_gets_converted_to_equality_when_value_conversion_is_used()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Where_negated_bool_gets_converted_to_equality_when_value_conversion_is_used(), "Assert.Single() Failure: The collection was empty");
#elif EF8
    // Fails: Fixture seeding fails (value converter on nullable property), so the test finds no data EF-X300
    public override void Where_negated_bool_gets_converted_to_equality_when_value_conversion_is_used()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Where_negated_bool_gets_converted_to_equality_when_value_conversion_is_used(), "Assert.Single() Failure: The collection was empty");
#endif
    // </generated>

    public class CustomConvertersMongoFixture : CustomConvertersFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("CustomConverters");

        public TestServer TestServer { get; private set; }

        public override async Task InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            try
            {
                await base.InitializeAsync();
            }
            catch (NotSupportedException)
            {
                // Fails: Seeding fails for value converters built from inline conversion expressions EF-X300
                // The tests then run (and fail) against an unseeded database.
            }
        }

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder.ConfigureWarnings(w => w.Ignore(MongoEventId.ColumnAttributeWithTypeUsed)));

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override bool StrictEquality
            => true;

        public override int IntegerPrecision
            => 53;

        public override bool SupportsAnsi
            => false;

        public override bool SupportsUnicodeToAnsiConversion
            => false;

        public override bool SupportsLargeStringComparisons
            => true;

        public override bool SupportsBinaryKeys
            => false;

        public override bool SupportsDecimalComparisons
            => true;

        public override DateTime DefaultDateTime
            => new();

        public override bool PreservesDateTimeKind
            => false;
    }
}
