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
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Tracking;

public class PropertyValuesMongoTest(PropertyValuesMongoTest.PropertyValuesMongoFixture fixture)
    : PropertyValuesTestBase<PropertyValuesMongoTest.PropertyValuesMongoFixture>(fixture)
{
#if EF11
    private readonly object? _ef11Skips = Ef11TestSkips.SkipIfListed("xUnit v3 (EF11) cannot hide this non-virtual upstream test; it fails here for the reasons in EF-X101 (see the New-method override on EF8 to EF10)", "Reload_when_entity_deleted_in_store_can_happen_for_any_state", "Store_values_really_are_store_values_not_current_or_original_values", "Store_values_really_are_store_values_not_current_or_original_values_async", "Values_can_be_reloaded_from_database_for_entity_in_any_state", "Reloading_optional_complex_property_with_null_does_not_throw");
#endif

    public class PropertyValuesMongoFixture : PropertyValuesFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("PropertyValues");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // EF-X102: the provider owns, by convention, any type it reaches through a navigation that is not yet in the model, which breaks the
            // upstream relationships; here every such type is an independent entity type.
            configurationBuilder.DoNotOwnTypes((_, _) => true);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
            modelBuilder.RelaxUniqueForeignKeyIndexes();
        }
    }

#if (EF10 || EF11)
    public override Task Complex_collection_current_values_can_be_accessed_as_a_property_dictionary()
        => base.Complex_collection_current_values_can_be_accessed_as_a_property_dictionary();
#endif

#if (EF10 || EF11)
    public override Task Complex_collection_original_values_can_be_accessed_as_a_property_dictionary()
        => base.Complex_collection_original_values_can_be_accessed_as_a_property_dictionary();
#endif

#if EF8 || EF9
    public override Task Complex_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => base.Complex_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty();
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Complex_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Complex_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty());
#endif

#if EF8 || EF9
    public override Task Complex_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => base.Complex_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty();
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Complex_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Complex_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty());
#endif

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Complex_store_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Complex_store_values_can_be_accessed_as_a_property_dictionary_using_IProperty());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Complex_store_values_can_be_accessed_asynchronously_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Complex_store_values_can_be_accessed_asynchronously_as_a_property_dictionary_using_IProperty());

#if (EF10 || EF11)
    public override void Current_values_can_be_cloned()
        => base.Current_values_can_be_cloned();
#endif

    public override Task Current_values_can_be_copied_from_a_non_generic_property_dictionary_into_an_object()
        => base.Current_values_can_be_copied_from_a_non_generic_property_dictionary_into_an_object();

    public override Task Current_values_can_be_copied_into_a_cloned_dictionary()
        => base.Current_values_can_be_copied_into_a_cloned_dictionary();

    public override Task Current_values_can_be_copied_into_a_non_generic_cloned_dictionary()
        => base.Current_values_can_be_copied_into_a_non_generic_cloned_dictionary();

    public override Task Current_values_can_be_copied_into_an_object()
        => base.Current_values_can_be_copied_into_an_object();

#if (EF10 || EF11)
    public override void Current_values_can_be_copied_to_object_using_ToObject()
        => base.Current_values_can_be_copied_to_object_using_ToObject();
#endif

    public override Task Current_values_can_be_read_and_set_for_an_object_in_the_Added_state()
        => base.Current_values_can_be_read_and_set_for_an_object_in_the_Added_state();

    public override Task Current_values_can_be_read_and_set_for_an_object_in_the_Modified_state()
        => base.Current_values_can_be_read_and_set_for_an_object_in_the_Modified_state();

    public override Task Current_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state()
        => base.Current_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state();

    public override Task Current_values_can_be_read_or_set_for_a_Detached_object()
        => base.Current_values_can_be_read_or_set_for_a_Detached_object();

    public override Task Current_values_can_be_read_or_set_for_an_object_in_the_Deleted_state()
        => base.Current_values_can_be_read_or_set_for_an_object_in_the_Deleted_state();

    public override void Current_values_can_be_set_from_DTO_object_missing_key_using_non_generic_dictionary()
        => base.Current_values_can_be_set_from_DTO_object_missing_key_using_non_generic_dictionary();

    public override void Current_values_can_be_set_from_DTO_object_using_non_generic_dictionary()
        => base.Current_values_can_be_set_from_DTO_object_using_non_generic_dictionary();

    public override void Current_values_can_be_set_from_an_object_using_generic_dictionary()
        => base.Current_values_can_be_set_from_an_object_using_generic_dictionary();

    public override void Current_values_can_be_set_from_an_object_using_non_generic_dictionary()
        => base.Current_values_can_be_set_from_an_object_using_non_generic_dictionary();

    public override void Current_values_can_be_set_from_dictionary()
        => base.Current_values_can_be_set_from_dictionary();

    public override void Current_values_can_be_set_from_dictionary_some_missing()
        => base.Current_values_can_be_set_from_dictionary_some_missing();

    public override void Current_values_can_be_set_from_dictionary_typed_int()
        => base.Current_values_can_be_set_from_dictionary_typed_int();

    public override void Current_values_can_be_set_from_dictionary_typed_string()
        => base.Current_values_can_be_set_from_dictionary_typed_string();

    public override void Current_values_can_be_set_from_one_generic_dictionary_to_another_generic_dictionary()
        => base.Current_values_can_be_set_from_one_generic_dictionary_to_another_generic_dictionary();

    public override void Current_values_can_be_set_from_one_non_generic_dictionary_to_another_generic_dictionary()
        => base.Current_values_can_be_set_from_one_non_generic_dictionary_to_another_generic_dictionary();

    public override Task Current_values_for_derived_object_can_be_copied_into_an_object()
        => base.Current_values_for_derived_object_can_be_copied_into_an_object();

    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Current_values_for_join_entity_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Current_values_for_join_entity_can_be_copied_into_an_object());

    public override Task GetDatabaseValuesAsync_for_derived_entity_not_in_the_store_returns_null()
        => base.GetDatabaseValuesAsync_for_derived_entity_not_in_the_store_returns_null();

    public override Task GetDatabaseValuesAsync_for_entity_not_in_the_store_returns_null()
        => base.GetDatabaseValuesAsync_for_entity_not_in_the_store_returns_null();

    public override Task GetDatabaseValuesAsync_for_the_wrong_type_in_the_store_returns_null()
        => base.GetDatabaseValuesAsync_for_the_wrong_type_in_the_store_returns_null();

    public override Task GetDatabaseValues_for_derived_entity_not_in_the_store_returns_null()
        => base.GetDatabaseValues_for_derived_entity_not_in_the_store_returns_null();

    public override Task GetDatabaseValues_for_entity_not_in_the_store_returns_null()
        => base.GetDatabaseValues_for_entity_not_in_the_store_returns_null();

    public override Task GetDatabaseValues_for_the_wrong_type_in_the_store_returns_null()
        => base.GetDatabaseValues_for_the_wrong_type_in_the_store_returns_null();

    public override Task NonGeneric_GetDatabaseValuesAsync_for_derived_entity_not_in_the_store_returns_null()
        => base.NonGeneric_GetDatabaseValuesAsync_for_derived_entity_not_in_the_store_returns_null();

    public override Task NonGeneric_GetDatabaseValuesAsync_for_entity_not_in_the_store_returns_null()
        => base.NonGeneric_GetDatabaseValuesAsync_for_entity_not_in_the_store_returns_null();

    public override Task NonGeneric_GetDatabaseValuesAsync_for_the_wrong_type_in_the_store_throws()
        => base.NonGeneric_GetDatabaseValuesAsync_for_the_wrong_type_in_the_store_throws();

    public override Task NonGeneric_GetDatabaseValues_for_derived_entity_not_in_the_store_returns_null()
        => base.NonGeneric_GetDatabaseValues_for_derived_entity_not_in_the_store_returns_null();

    public override Task NonGeneric_GetDatabaseValues_for_entity_not_in_the_store_returns_null()
        => base.NonGeneric_GetDatabaseValues_for_entity_not_in_the_store_returns_null();

    public override Task NonGeneric_GetDatabaseValues_for_the_wrong_type_in_the_store_throws()
        => base.NonGeneric_GetDatabaseValues_for_the_wrong_type_in_the_store_throws();

    public override void Non_nullable_property_in_cloned_dictionary_cannot_be_set_to_null()
        => base.Non_nullable_property_in_cloned_dictionary_cannot_be_set_to_null();

    public override void Non_nullable_property_in_current_values_results_in_conceptual_null(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Non_nullable_property_in_current_values_results_in_conceptual_null(deleteOrphansTiming);

    public override void Non_nullable_property_in_original_values_cannot_be_set_to_null_in_property_dictionary()
        => base.Non_nullable_property_in_original_values_cannot_be_set_to_null_in_property_dictionary();

    public override void Non_nullable_shadow_property_in_current_values_results_in_conceptual_null(global::Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming deleteOrphansTiming)
        => base.Non_nullable_shadow_property_in_current_values_results_in_conceptual_null(deleteOrphansTiming);

    public override void Non_nullable_shadow_property_in_original_values_cannot_be_set_to_null_in_property_dictionary()
        => base.Non_nullable_shadow_property_in_original_values_cannot_be_set_to_null_in_property_dictionary();

#if (EF10 || EF11)
    public override void Nullable_complex_property_with_null_value_returns_null_when_using_ToObject()
        => base.Nullable_complex_property_with_null_value_returns_null_when_using_ToObject();
#endif

#if (EF10 || EF11)
    public override void Original_values_can_be_cloned()
        => base.Original_values_can_be_cloned();
#endif

    public override Task Original_values_can_be_copied_into_a_cloned_dictionary()
        => base.Original_values_can_be_copied_into_a_cloned_dictionary();

    public override Task Original_values_can_be_copied_into_a_non_generic_cloned_dictionary()
        => base.Original_values_can_be_copied_into_a_non_generic_cloned_dictionary();

    public override Task Original_values_can_be_copied_into_an_object()
        => base.Original_values_can_be_copied_into_an_object();

    public override Task Original_values_can_be_copied_non_generic_property_dictionary_into_an_object()
        => base.Original_values_can_be_copied_non_generic_property_dictionary_into_an_object();

#if (EF10 || EF11)
    public override void Original_values_can_be_copied_to_object_using_ToObject()
        => base.Original_values_can_be_copied_to_object_using_ToObject();
#endif

    public override Task Original_values_can_be_read_and_set_for_an_object_in_the_Deleted_state()
        => base.Original_values_can_be_read_and_set_for_an_object_in_the_Deleted_state();

    public override Task Original_values_can_be_read_and_set_for_an_object_in_the_Modified_state()
        => base.Original_values_can_be_read_and_set_for_an_object_in_the_Modified_state();

    public override Task Original_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state()
        => base.Original_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state();

    public override Task Original_values_can_be_read_or_set_for_a_Detached_object()
        => base.Original_values_can_be_read_or_set_for_a_Detached_object();

    public override Task Original_values_can_be_read_or_set_for_an_object_in_the_Added_state()
        => base.Original_values_can_be_read_or_set_for_an_object_in_the_Added_state();

    public override void Original_values_can_be_set_from_DTO_object_missing_key_using_non_generic_dictionary()
        => base.Original_values_can_be_set_from_DTO_object_missing_key_using_non_generic_dictionary();

    public override void Original_values_can_be_set_from_DTO_object_using_non_generic_dictionary()
        => base.Original_values_can_be_set_from_DTO_object_using_non_generic_dictionary();

    public override void Original_values_can_be_set_from_an_object_using_generic_dictionary()
        => base.Original_values_can_be_set_from_an_object_using_generic_dictionary();

    public override void Original_values_can_be_set_from_an_object_using_non_generic_dictionary()
        => base.Original_values_can_be_set_from_an_object_using_non_generic_dictionary();

    public override void Original_values_can_be_set_from_dictionary()
        => base.Original_values_can_be_set_from_dictionary();

    public override void Original_values_can_be_set_from_dictionary_some_missing()
        => base.Original_values_can_be_set_from_dictionary_some_missing();

    public override void Original_values_can_be_set_from_dictionary_typed_int()
        => base.Original_values_can_be_set_from_dictionary_typed_int();

    public override void Original_values_can_be_set_from_dictionary_typed_string()
        => base.Original_values_can_be_set_from_dictionary_typed_string();

    public override void Original_values_can_be_set_from_one_generic_dictionary_to_another_generic_dictionary()
        => base.Original_values_can_be_set_from_one_generic_dictionary_to_another_generic_dictionary();

    public override void Original_values_can_be_set_from_one_non_generic_dictionary_to_another_generic_dictionary()
        => base.Original_values_can_be_set_from_one_non_generic_dictionary_to_another_generic_dictionary();

    public override Task Original_values_for_derived_object_can_be_copied_into_an_object()
        => base.Original_values_for_derived_object_can_be_copied_into_an_object();

    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Original_values_for_join_entity_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Original_values_for_join_entity_can_be_copied_into_an_object());

    public override void Primary_key_in_current_values_cannot_be_changed_by_setting_values_from_another_dictionary()
        => base.Primary_key_in_current_values_cannot_be_changed_by_setting_values_from_another_dictionary();

    public override void Primary_key_in_current_values_cannot_be_changed_by_setting_values_from_object()
        => base.Primary_key_in_current_values_cannot_be_changed_by_setting_values_from_object();

    public override void Primary_key_in_current_values_cannot_be_changed_in_property_dictionary()
        => base.Primary_key_in_current_values_cannot_be_changed_in_property_dictionary();

    public override void Primary_key_in_original_values_cannot_be_changed_by_setting_values_from_another_dictionary()
        => base.Primary_key_in_original_values_cannot_be_changed_by_setting_values_from_another_dictionary();

    public override void Primary_key_in_original_values_cannot_be_changed_by_setting_values_from_object()
        => base.Primary_key_in_original_values_cannot_be_changed_by_setting_values_from_object();

    public override void Primary_key_in_original_values_cannot_be_changed_in_property_dictionary()
        => base.Primary_key_in_original_values_cannot_be_changed_in_property_dictionary();

#if EF8 || EF9
    public override Task Properties_for_cloned_dictionary_returns_properties()
        => base.Properties_for_cloned_dictionary_returns_properties();
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Properties_for_cloned_dictionary_returns_properties()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Properties_for_cloned_dictionary_returns_properties());
#endif

#if EF8 || EF9
    public override Task Properties_for_current_values_returns_properties()
        => base.Properties_for_current_values_returns_properties();
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Properties_for_current_values_returns_properties()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Properties_for_current_values_returns_properties());
#endif

#if EF8 || EF9
    public override Task Properties_for_original_values_returns_properties()
        => base.Properties_for_original_values_returns_properties();
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Properties_for_original_values_returns_properties()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Properties_for_original_values_returns_properties());
#endif

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Properties_for_store_values_returns_properties()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Properties_for_store_values_returns_properties());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Properties_for_store_values_returns_properties_asynchronously()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Properties_for_store_values_returns_properties_asynchronously());

    public override void Property_in_cloned_dictionary_cannot_be_set_to_instance_of_wrong_type()
        => base.Property_in_cloned_dictionary_cannot_be_set_to_instance_of_wrong_type();

    public override void Property_in_current_values_cannot_be_set_to_instance_of_wrong_type()
        => base.Property_in_current_values_cannot_be_set_to_instance_of_wrong_type();

    public override void Property_in_original_values_cannot_be_set_to_instance_of_wrong_type()
        => base.Property_in_original_values_cannot_be_set_to_instance_of_wrong_type();

#if !EF11
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task Reload_when_entity_deleted_in_store_can_happen_for_any_state(global::Microsoft.EntityFrameworkCore.EntityState state, bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif

    public override Task Scalar_current_values_can_be_accessed_as_a_non_generic_property_dictionary()
        => base.Scalar_current_values_can_be_accessed_as_a_non_generic_property_dictionary();

    public override Task Scalar_current_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty()
        => base.Scalar_current_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty();

    public override Task Scalar_current_values_can_be_accessed_as_a_property_dictionary()
        => base.Scalar_current_values_can_be_accessed_as_a_property_dictionary();

    public override Task Scalar_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => base.Scalar_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty();

    public override void Scalar_current_values_can_be_set_using_a_non_generic_property_dictionary()
        => base.Scalar_current_values_can_be_set_using_a_non_generic_property_dictionary();

    public override void Scalar_current_values_can_be_set_using_a_property_dictionary()
        => base.Scalar_current_values_can_be_set_using_a_property_dictionary();

    public override void Scalar_current_values_can_be_set_using_a_property_dictionary_with_IProperty()
        => base.Scalar_current_values_can_be_set_using_a_property_dictionary_with_IProperty();

    public override Task Scalar_current_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary()
        => base.Scalar_current_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary();

    public override Task Scalar_current_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary()
        => base.Scalar_current_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary();

    public override Task Scalar_original_values_can_be_accessed_as_a_non_generic_property_dictionary()
        => base.Scalar_original_values_can_be_accessed_as_a_non_generic_property_dictionary();

    public override Task Scalar_original_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty()
        => base.Scalar_original_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty();

    public override Task Scalar_original_values_can_be_accessed_as_a_property_dictionary()
        => base.Scalar_original_values_can_be_accessed_as_a_property_dictionary();

    public override Task Scalar_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => base.Scalar_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty();

    public override void Scalar_original_values_can_be_set_using_a_non_generic_property_dictionary()
        => base.Scalar_original_values_can_be_set_using_a_non_generic_property_dictionary();

    public override void Scalar_original_values_can_be_set_using_a_property_dictionary()
        => base.Scalar_original_values_can_be_set_using_a_property_dictionary();

    public override void Scalar_original_values_can_be_set_using_a_property_dictionary_with_IProperty()
        => base.Scalar_original_values_can_be_set_using_a_property_dictionary_with_IProperty();

    public override Task Scalar_original_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary()
        => base.Scalar_original_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary();

    public override Task Scalar_original_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary()
        => base.Scalar_original_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary();

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Scalar_store_values_can_be_accessed_as_a_non_generic_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Scalar_store_values_can_be_accessed_as_a_non_generic_property_dictionary());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Scalar_store_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Scalar_store_values_can_be_accessed_as_a_non_generic_property_dictionary_using_IProperty());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Scalar_store_values_can_be_accessed_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Scalar_store_values_can_be_accessed_as_a_property_dictionary());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Scalar_store_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Scalar_store_values_can_be_accessed_as_a_property_dictionary_using_IProperty());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Scalar_store_values_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Scalar_store_values_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Scalar_store_values_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Scalar_store_values_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary_using_IProperty());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Scalar_store_values_can_be_accessed_asynchronously_as_a_property_dictionary()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Scalar_store_values_can_be_accessed_asynchronously_as_a_property_dictionary());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Scalar_store_values_can_be_accessed_asynchronously_as_a_property_dictionary_using_IProperty()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Scalar_store_values_can_be_accessed_asynchronously_as_a_property_dictionary_using_IProperty());

    public override Task Scalar_store_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary()
        => base.Scalar_store_values_of_a_derived_object_can_be_accessed_as_a_non_generic_property_dictionary();

    public override Task Scalar_store_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary()
        => base.Scalar_store_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary();

    public override Task Scalar_store_values_of_a_derived_object_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary()
        => base.Scalar_store_values_of_a_derived_object_can_be_accessed_asynchronously_as_a_non_generic_property_dictionary();

    public override Task Scalar_store_values_of_a_derived_object_can_be_accessed_asynchronously_as_a_property_dictionary()
        => base.Scalar_store_values_of_a_derived_object_can_be_accessed_asynchronously_as_a_property_dictionary();

#if (EF10 || EF11)
    public override void SetValues_throws_for_complex_collection_with_non_dictionary_item()
        => base.SetValues_throws_for_complex_collection_with_non_dictionary_item();
#endif

#if (EF10 || EF11)
    public override void SetValues_throws_for_complex_collection_with_non_list_value()
        => base.SetValues_throws_for_complex_collection_with_non_list_value();
#endif

#if (EF10 || EF11)
    public override void SetValues_throws_for_complex_property_with_non_dictionary_value()
        => base.SetValues_throws_for_complex_property_with_non_dictionary_value();
#endif

#if (EF10 || EF11)
    public override void SetValues_throws_for_nested_complex_collection_with_non_dictionary_item()
        => base.SetValues_throws_for_nested_complex_collection_with_non_dictionary_item();
#endif

#if (EF10 || EF11)
    public override void SetValues_throws_for_nested_complex_collection_with_non_list_value()
        => base.SetValues_throws_for_nested_complex_collection_with_non_list_value();
#endif

#if (EF10 || EF11)
    public override void Setting_complex_collection_current_values_from_DTO_with_complex_metadata_access_works()
        => base.Setting_complex_collection_current_values_from_DTO_with_complex_metadata_access_works();
#endif

#if (EF10 || EF11)
    public override void Setting_complex_collection_current_values_from_dictionary_with_nulls_works()
        => base.Setting_complex_collection_current_values_from_dictionary_with_nulls_works();
#endif

#if (EF10 || EF11)
    public override void Setting_complex_collection_current_values_from_dictionary_works()
        => base.Setting_complex_collection_current_values_from_dictionary_works();
#endif

#if (EF10 || EF11)
    public override void Setting_complex_collection_current_values_from_object_with_nulls_works()
        => base.Setting_complex_collection_current_values_from_object_with_nulls_works();
#endif

#if (EF10 || EF11)
    public override void Setting_complex_collection_original_values_from_dictionary_with_nulls_works()
        => base.Setting_complex_collection_original_values_from_dictionary_with_nulls_works();
#endif

#if (EF10 || EF11)
    public override void Setting_complex_collection_original_values_from_object_with_nulls_works()
        => base.Setting_complex_collection_original_values_from_object_with_nulls_works();
#endif

#if (EF10 || EF11)
    public override void Setting_complex_collection_values_from_DTO_with_nulls_works()
        => base.Setting_complex_collection_values_from_DTO_with_nulls_works();
#endif

#if (EF10 || EF11)
    public override void Setting_complex_collection_values_from_object_works()
        => base.Setting_complex_collection_values_from_object_works();
#endif

#if (EF10 || EF11)
    public override void Setting_current_values_from_cloned_values_sets_nullable_complex_property_to_null()
        => base.Setting_current_values_from_cloned_values_sets_nullable_complex_property_to_null();
#endif

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override void Setting_store_values_does_not_change_current_or_original_values()
        => SpecFails.Throws<global::System.FormatException>(() => base.Setting_store_values_does_not_change_current_or_original_values());

    public override void Shadow_property_in_current_values_cannot_be_set_to_instance_of_wrong_type()
        => base.Shadow_property_in_current_values_cannot_be_set_to_instance_of_wrong_type();

    public override void Shadow_property_in_original_values_cannot_be_set_to_instance_of_wrong_type()
        => base.Shadow_property_in_original_values_cannot_be_set_to_instance_of_wrong_type();

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_copied_asynchronously_into_a_non_generic_cloned_dictionary()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_copied_asynchronously_into_a_non_generic_cloned_dictionary());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_copied_asynchronously_non_generic_property_dictionary_into_an_object()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_copied_asynchronously_non_generic_property_dictionary_into_an_object());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_copied_into_a_cloned_dictionary()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_copied_into_a_cloned_dictionary());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_copied_into_a_cloned_dictionary_asynchronously()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_copied_into_a_cloned_dictionary_asynchronously());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_copied_into_a_non_generic_cloned_dictionary()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_copied_into_a_non_generic_cloned_dictionary());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_copied_into_an_object());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_copied_into_an_object_asynchronously()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_copied_into_an_object_asynchronously());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_copied_non_generic_property_dictionary_into_an_object()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_copied_non_generic_property_dictionary_into_an_object());

#if (EF10 || EF11)
    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_copied_to_object_using_ToObject()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_copied_to_object_using_ToObject());
#endif

#if (EF10 || EF11)
    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_copied_to_object_using_ToObject_asynchronously()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_copied_to_object_using_ToObject_asynchronously());
#endif

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Deleted_state()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Deleted_state());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Deleted_state_asynchronously()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Deleted_state_asynchronously());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Modified_state()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Modified_state());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Modified_state_asynchronously()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Modified_state_asynchronously());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state_asynchronously()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_read_and_set_for_an_object_in_the_Unchanged_state_asynchronously());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_read_or_set_for_a_Detached_object()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_read_or_set_for_a_Detached_object());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_read_or_set_for_a_Detached_object_asynchronously()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_read_or_set_for_a_Detached_object_asynchronously());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_read_or_set_for_an_object_in_the_Added_state()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_read_or_set_for_an_object_in_the_Added_state());

    // Fails: Serialization/format error while translating or materializing EF-X602
    public override Task Store_values_can_be_read_or_set_for_an_object_in_the_Added_state_asynchronously()
        => SpecFails.ThrowsAsync<global::System.FormatException>(() => base.Store_values_can_be_read_or_set_for_an_object_in_the_Added_state_asynchronously());

    public override Task Store_values_for_derived_object_can_be_copied_into_an_object()
        => base.Store_values_for_derived_object_can_be_copied_into_an_object();

    public override Task Store_values_for_derived_object_can_be_copied_into_an_object_asynchronously()
        => base.Store_values_for_derived_object_can_be_copied_into_an_object_asynchronously();

    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Store_values_for_join_entity_can_be_copied_into_an_object()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_for_join_entity_can_be_copied_into_an_object());

    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Store_values_for_join_entity_can_be_copied_into_an_object_asynchronously()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Store_values_for_join_entity_can_be_copied_into_an_object_asynchronously());

#if !EF11
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task Store_values_really_are_store_values_not_current_or_original_values()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif

#if !EF11
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task Store_values_really_are_store_values_not_current_or_original_values_async()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif

    public override void Using_bad_IProperty_instances_throws()
        => base.Using_bad_IProperty_instances_throws();

    public override void Using_bad_IProperty_instances_throws_derived()
        => base.Using_bad_IProperty_instances_throws_derived();

    public override void Using_bad_property_names_throws()
        => base.Using_bad_property_names_throws();

    public override void Using_bad_property_names_throws_derived()
        => base.Using_bad_property_names_throws_derived();

#if (EF10 || EF11)
    public override void Using_complex_property_value_not_list_throws()
        => base.Using_complex_property_value_not_list_throws();
#endif

#if (EF10 || EF11)
    public override void Using_non_collection_complex_property_throws()
        => base.Using_non_collection_complex_property_throws();
#endif

#if !EF11
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task Values_can_be_reloaded_from_database_for_entity_in_any_state(global::Microsoft.EntityFrameworkCore.EntityState state, bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif

#if (EF10 || EF11) || EF9
    // Fails: Argument exception while translating or materializing EF-X603
    public override Task Values_can_be_reloaded_from_database_for_entity_in_any_state_with_inheritance(global::Microsoft.EntityFrameworkCore.EntityState state, bool async)
        => SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Values_can_be_reloaded_from_database_for_entity_in_any_state_with_inheritance(state, async));
#endif

    public override void Values_in_cloned_dictionary_can_be_set_with_IProperty()
        => base.Values_in_cloned_dictionary_can_be_set_with_IProperty();
}
