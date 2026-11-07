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

public class ConvertToProviderTypesMongoTest(ConvertToProviderTypesMongoTest.ConvertToProviderTypesMongoFixture fixture)
    : ConvertToProviderTypesTestBase<ConvertToProviderTypesMongoTest.ConvertToProviderTypesMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
#if EF10 || EF9
    public override Task Can_compare_enum_to_constant()
        => base.Can_compare_enum_to_constant();
#elif EF8
    public override void Can_compare_enum_to_constant()
        => base.Can_compare_enum_to_constant();
#endif

#if EF10 || EF9
    public override Task Can_compare_enum_to_parameter()
        => base.Can_compare_enum_to_parameter();
#elif EF8
    public override void Can_compare_enum_to_parameter()
        => base.Can_compare_enum_to_parameter();
#endif

    public override Task Can_filter_projection_with_captured_enum_variable(bool async)
        => base.Can_filter_projection_with_captured_enum_variable(async);

    public override Task Can_filter_projection_with_inline_enum_variable(bool async)
        => base.Can_filter_projection_with_inline_enum_variable(async);

#if EF10 || EF9
    public override Task Can_insert_and_read_back_all_non_nullable_data_types()
        => base.Can_insert_and_read_back_all_non_nullable_data_types();
#elif EF8
    public override void Can_insert_and_read_back_all_non_nullable_data_types()
        => base.Can_insert_and_read_back_all_non_nullable_data_types();
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_non_null()
        => base.Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_non_null();
#elif EF8
    public override void Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_non_null()
        => base.Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_non_null();
#endif

#if EF10 || EF9
    public override Task Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_null()
        => base.Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_null();
#elif EF8
    public override void Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_null()
        => base.Can_insert_and_read_back_all_nullable_data_types_with_values_set_to_null();
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
    public override Task Can_insert_and_read_with_max_length_set()
        => base.Can_insert_and_read_with_max_length_set();
#elif EF8
    public override void Can_insert_and_read_with_max_length_set()
        => base.Can_insert_and_read_with_max_length_set();
#endif

#if EF10 || EF9
    public override Task Can_insert_query_multiline_string()
        => base.Can_insert_query_multiline_string();
#elif EF8
    public override void Can_insert_query_multiline_string()
        => base.Can_insert_query_multiline_string();
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
    // Fails: ValueConverterSerializer lacks IHasRepresentationSerializer for queries over converted properties EF-X302
    public override Task Can_query_using_any_data_type()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_query_using_any_data_type(), "Serializer class ValueConverterSerializer`2 does not implement IHasRep");
#elif EF8
    // Fails: ValueConverterSerializer lacks IHasRepresentationSerializer for queries over converted properties EF-X302
    public override void Can_query_using_any_data_type()
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_query_using_any_data_type(), "Serializer class ValueConverterSerializer`2 does not implement IHasRep");
#endif

#if EF10 || EF9
    // Fails: Query over converted property fails binding ValueConverterSerializer EF-X303
    public override Task Can_query_using_any_data_type_nullable_shadow()
        => SpecFails.Async<global::System.ArgumentException>(() => base.Can_query_using_any_data_type_nullable_shadow(), "Expression of type 'MongoDB.EntityFrameworkCore.Serializers.ValueConve");
#elif EF8
    // Fails: Query over converted property fails binding ValueConverterSerializer EF-X303
    public override void Can_query_using_any_data_type_nullable_shadow()
        => SpecFails.Sync<global::System.ArgumentException>(() => base.Can_query_using_any_data_type_nullable_shadow(), "Expression of type 'MongoDB.EntityFrameworkCore.Serializers.ValueConve");
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
    // Fails: Query over converted property fails binding ValueConverterSerializer EF-X303
    public override Task Can_query_using_any_nullable_data_type()
        => SpecFails.Async<global::System.ArgumentException>(() => base.Can_query_using_any_nullable_data_type(), "Expression of type 'MongoDB.EntityFrameworkCore.Serializers.ValueConve");
#elif EF8
    // Fails: Query over converted property fails binding ValueConverterSerializer EF-X303
    public override void Can_query_using_any_nullable_data_type()
        => SpecFails.Sync<global::System.ArgumentException>(() => base.Can_query_using_any_nullable_data_type(), "Expression of type 'MongoDB.EntityFrameworkCore.Serializers.ValueConve");
#endif

#if EF10 || EF9
    // Fails: Query comparing a converted property to a parameter/literal fails casting ValueConverterSerializer EF-X301
    public override Task Can_query_using_any_nullable_data_type_as_literal()
        => SpecFails.Async<global::System.InvalidCastException>(() => base.Can_query_using_any_nullable_data_type_as_literal(), "Unable to cast object of type 'MongoDB.EntityFrameworkCore.Serializers");
#elif EF8
    // Fails: Query comparing a converted property to a parameter/literal fails casting ValueConverterSerializer EF-X301
    public override void Can_query_using_any_nullable_data_type_as_literal()
        => SpecFails.Sync<global::System.InvalidCastException>(() => base.Can_query_using_any_nullable_data_type_as_literal(), "Unable to cast object of type 'MongoDB.EntityFrameworkCore.Serializers");
#endif

#if EF10 || EF9
    // Fails: Query comparing a converted property to a parameter/literal fails casting ValueConverterSerializer EF-X301
    public override Task Can_query_with_null_parameters_using_any_nullable_data_type()
        => SpecFails.Async<global::System.InvalidCastException>(() => base.Can_query_with_null_parameters_using_any_nullable_data_type(), "Unable to cast object of type 'MongoDB.EntityFrameworkCore.Serializers");
#elif EF8
    // Fails: Query comparing a converted property to a parameter/literal fails casting ValueConverterSerializer EF-X301
    public override void Can_query_with_null_parameters_using_any_nullable_data_type()
        => SpecFails.Sync<global::System.InvalidCastException>(() => base.Can_query_with_null_parameters_using_any_nullable_data_type(), "Unable to cast object of type 'MongoDB.EntityFrameworkCore.Serializers");
#endif

#if EF10
    public override Task Can_read_back_bool_mapped_as_int_through_navigation()
        => base.Can_read_back_bool_mapped_as_int_through_navigation();
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

    public override void Equals_method_over_enum_works()
        => base.Equals_method_over_enum_works();

    public override void Object_equals_method_over_enum_works()
        => base.Object_equals_method_over_enum_works();

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
    // </generated>

    public class ConvertToProviderTypesMongoFixture : ConvertToProviderTypesFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("ConvertToProviderTypes");

        public TestServer TestServer { get; private set; }

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await base.InitializeAsync();
        }

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder.ConfigureWarnings(w => w.Ignore(MongoEventId.ColumnAttributeWithTypeUsed)));

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
