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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Metadata;

public class DataAnnotationMongoTest(DataAnnotationMongoTest.DataAnnotationMongoFixture fixture)
    : DataAnnotationTestBase<DataAnnotationMongoTest.DataAnnotationMongoFixture>(fixture)
{
    protected override TestHelpers TestHelpers => MongoTestHelpers.Instance;

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    public override void Annotation_in_derived_class_when_base_class_processed_after_derived_class()
        => base.Annotation_in_derived_class_when_base_class_processed_after_derived_class();

    public override void Attribute_set_shadow_FK_name_is_preserved_with_HasPrincipalKey()
        => base.Attribute_set_shadow_FK_name_is_preserved_with_HasPrincipalKey();

#if (EF10 || EF11) || EF9
    // Fails (some data rows): Single/First over empty result in test body (not triaged) EF-X310
    public override Task ConcurrencyCheckAttribute_throws_if_value_in_database_changed()
        => SpecFails.AsyncSomeRows<global::System.InvalidOperationException>(() => base.ConcurrencyCheckAttribute_throws_if_value_in_database_changed(), "Sequence contains no elements");
#elif EF8
    // Fails (some data rows): Single/First over empty result in test body (not triaged) EF-X310
    public override void ConcurrencyCheckAttribute_throws_if_value_in_database_changed()
        => SpecFails.SyncSomeRows<global::System.InvalidOperationException>(() => base.ConcurrencyCheckAttribute_throws_if_value_in_database_changed(), "Sequence contains no elements");
#endif

#if (EF10 || EF11) || EF9
    public override Task DatabaseGeneratedAttribute_autogenerates_values_when_set_to_identity()
        => base.DatabaseGeneratedAttribute_autogenerates_values_when_set_to_identity();
#elif EF8
    public override void DatabaseGeneratedAttribute_autogenerates_values_when_set_to_identity()
        => base.DatabaseGeneratedAttribute_autogenerates_values_when_set_to_identity();
#endif

    public override global::Microsoft.EntityFrameworkCore.Metadata.IModel DatabaseGeneratedOption_Identity_does_not_throw_on_noninteger_properties()
        => base.DatabaseGeneratedOption_Identity_does_not_throw_on_noninteger_properties();

    public override global::Microsoft.EntityFrameworkCore.Metadata.IModel DatabaseGeneratedOption_configures_the_property_correctly()
        => base.DatabaseGeneratedOption_configures_the_property_correctly();

    // Fails: Model validation requires key property to be mapped to _id EF-X307
    public override global::Microsoft.EntityFrameworkCore.ModelBuilder Default_length_for_key_string_column()
    {
        SpecFails.Sync<global::System.InvalidOperationException>(() => base.Default_length_for_key_string_column(), "The entity type 'Login1' primary key property 'UserName' must be mappe");
        return default!;
    }

    // Fails: Model validation requires key property to be mapped to _id EF-X307
    public override void Duplicate_column_order_is_ignored()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Duplicate_column_order_is_ignored(), "The entity type 'Entity_10558' primary key property 'Key1' must be map");

    public override void Explicit_configuration_on_derived_type_or_base_type_is_last_one_wins()
        => base.Explicit_configuration_on_derived_type_or_base_type_is_last_one_wins();

    public override void Explicit_configuration_on_derived_type_overrides_annotation_on_mapped_base_type()
        => base.Explicit_configuration_on_derived_type_overrides_annotation_on_mapped_base_type();

    public override void Explicit_configuration_on_derived_type_overrides_annotation_on_unmapped_base_type()
        => base.Explicit_configuration_on_derived_type_overrides_annotation_on_unmapped_base_type();

    // Fails: [Column(TypeName)] is reported as an error by default EF-X312
    public override global::Microsoft.EntityFrameworkCore.Metadata.IModel Field_annotations_are_enabled()
    {
        SpecFails.Sync<global::System.InvalidOperationException>(() => base.Field_annotations_are_enabled(), "An error was generated for warning 'Microsoft.EntityFrameworkCore.Mode");
        return default!;
    }

    public override void Fluent_API_relationship_throws_for_Keyless_attribute()
        => base.Fluent_API_relationship_throws_for_Keyless_attribute();

    public override void ForeignKeyAttribute_configures_relationships_when_inverse_on_derived()
        => base.ForeignKeyAttribute_configures_relationships_when_inverse_on_derived();

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void ForeignKeyAttribute_configures_two_self_referencing_relationships()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.ForeignKeyAttribute_configures_two_self_referencing_relationships(), "Object reference not set to an instance of an object.");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void ForeignKeyAttribute_creates_two_relationships_if_applied_on_navigation_and_property_on_different_sides_and_values_do_not_match()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.ForeignKeyAttribute_creates_two_relationships_if_applied_on_navigation_and_property_on_different_sides_and_values_do_not_match(), "The navigation 'AuthorDetails' cannot be changed, because the foreign ");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void ForeignKeyAttribute_creates_two_relationships_if_applied_on_navigations_on_both_sides_and_values_do_not_match()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.ForeignKeyAttribute_creates_two_relationships_if_applied_on_navigations_on_both_sides_and_values_do_not_match(), "The navigation 'Author' cannot be changed, because the foreign key bet");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void ForeignKeyAttribute_creates_two_relationships_if_applied_on_property_on_both_side()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.ForeignKeyAttribute_creates_two_relationships_if_applied_on_property_on_both_side(), "The navigation 'PostDetails' cannot be changed, because the foreign ke");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void ForeignKeyAttribute_throws_if_applied_on_both_navigations_connected_by_inverse_property_but_values_do_not_match()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.ForeignKeyAttribute_throws_if_applied_on_both_navigations_connected_by_inverse_property_but_values_do_not_match(), "Assert.Equal() Failure: Strings differ");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void ForeignKeyAttribute_throws_if_applied_on_property_on_both_side_but_navigations_are_connected_by_inverse_property()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.ForeignKeyAttribute_throws_if_applied_on_property_on_both_side_but_navigations_are_connected_by_inverse_property(), "Assert.Equal() Failure: Strings differ");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void ForeignKeyAttribute_throws_if_applied_on_two_relationships_targetting_the_same_property()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.ForeignKeyAttribute_throws_if_applied_on_two_relationships_targetting_the_same_property(), "Assert.Throws() Failure: No exception was thrown");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void ForeignKey_to_ForeignKey()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.ForeignKey_to_ForeignKey(), "The navigation 'Profile' cannot be changed, because the foreign key be");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void ForeignKey_to_ForeignKey_same_name()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.ForeignKey_to_ForeignKey_same_name(), "The navigation 'Login' cannot be changed, because the foreign key betw");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void ForeignKey_to_ForeignKey_same_name_one_shadow()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.ForeignKey_to_ForeignKey_same_name_one_shadow(), "The navigation 'Profile' cannot be changed, because the foreign key be");

    public override void ForeignKey_to_nothing()
        => base.ForeignKey_to_nothing();

    public override void InversePropertyAttribute_from_ignored_base_can_be_ignored_to_remove_ambiguity()
        => base.InversePropertyAttribute_from_ignored_base_can_be_ignored_to_remove_ambiguity();

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void InversePropertyAttribute_from_ignored_base_causes_ambiguity()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.InversePropertyAttribute_from_ignored_base_causes_ambiguity(), "Object reference not set to an instance of an object.");

    public override void InversePropertyAttribute_is_noop_in_unambiguous_models()
        => base.InversePropertyAttribute_is_noop_in_unambiguous_models();

    public override void InversePropertyAttribute_pointing_to_same_nav_on_base_causes_ambiguity()
        => base.InversePropertyAttribute_pointing_to_same_nav_on_base_causes_ambiguity();

    public override void InversePropertyAttribute_pointing_to_same_nav_on_base_with_one_ignored()
        => base.InversePropertyAttribute_pointing_to_same_nav_on_base_with_one_ignored();

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void InversePropertyAttribute_pointing_to_same_skip_nav_on_base_causes_ambiguity()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.InversePropertyAttribute_pointing_to_same_skip_nav_on_base_causes_ambiguity(), "Assert.Equal() Failure: Strings differ");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void InversePropertyAttribute_removes_ambiguity()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.InversePropertyAttribute_removes_ambiguity(), "Object reference not set to an instance of an object.");

    public override void InversePropertyAttribute_removes_ambiguity_from_the_ambiguous_end()
        => base.InversePropertyAttribute_removes_ambiguity_from_the_ambiguous_end();

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void InversePropertyAttribute_removes_ambiguity_when_combined_with_other_attributes()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.InversePropertyAttribute_removes_ambiguity_when_combined_with_other_attributes(), "Assert.Equal() Failure: Strings differ");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void InversePropertyAttribute_removes_ambiguity_with_base_type()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.InversePropertyAttribute_removes_ambiguity_with_base_type(), "Object reference not set to an instance of an object.");

    public override void InversePropertyAttribute_removes_ambiguity_with_base_type_bidirectional()
        => base.InversePropertyAttribute_removes_ambiguity_with_base_type_bidirectional();

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void InversePropertyAttribute_removes_ambiguity_with_base_type_ignored()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.InversePropertyAttribute_removes_ambiguity_with_base_type_ignored(), "Object reference not set to an instance of an object.");

    // Fails: [Column(TypeName)] is reported as an error by default EF-X312
    public override void InverseProperty_with_case_sensitive_clr_property()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.InverseProperty_with_case_sensitive_clr_property(), "An error was generated for warning 'Microsoft.EntityFrameworkCore.Mode");

#if (EF10 || EF11) || EF9
    // Fails: [Column(TypeName)] is reported as an error by default EF-X312
    public override void InverseProperty_with_potentially_ambigous_derived_types()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.InverseProperty_with_potentially_ambigous_derived_types(), "An error was generated for warning 'Microsoft.EntityFrameworkCore.Mode");
#endif

    public override void Inverse_and_self_ref_ForeignKey()
        => base.Inverse_and_self_ref_ForeignKey();

    // Fails: Model validation requires key property to be mapped to _id EF-X307
    public override global::Microsoft.EntityFrameworkCore.Metadata.IModel Key_and_MaxLength_64_produce_nvarchar_64()
    {
        SpecFails.Sync<global::System.InvalidOperationException>(() => base.Key_and_MaxLength_64_produce_nvarchar_64(), "The entity type 'ColumnKeyAnnotationClass2' primary key property 'Pers");
        return default!;
    }

    // Fails: [Column(TypeName)] is reported as an error by default EF-X312
    public override global::Microsoft.EntityFrameworkCore.Metadata.IModel Key_and_column_work_together()
    {
        SpecFails.Sync<global::System.InvalidOperationException>(() => base.Key_and_column_work_together(), "An error was generated for warning 'Microsoft.EntityFrameworkCore.Mode");
        return default!;
    }

    public override void Key_fluent_api_and_keyless_attribute_do_not_cause_warning()
        => base.Key_fluent_api_and_keyless_attribute_do_not_cause_warning();

    // Fails: Model validation requires key property to be mapped to _id EF-X307
    public override void Key_from_base_type_is_recognized()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Key_from_base_type_is_recognized(), "The entity type 'OKeyBase' primary key property 'OrderLineNo' must be ");

    // Fails: Model validation requires key property to be mapped to _id EF-X307
    public override void Key_from_base_type_is_recognized_if_base_discovered_first()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Key_from_base_type_is_recognized_if_base_discovered_first(), "The entity type 'OKeyBase' primary key property 'OrderLineNo' must be ");

    public override void Key_from_base_type_is_recognized_if_discovered_through_relationship()
        => base.Key_from_base_type_is_recognized_if_discovered_through_relationship();

    public override void Key_on_nav_prop_is_ignored()
        => base.Key_on_nav_prop_is_ignored();

    // Fails: Model validation requires key property to be mapped to _id EF-X307
    public override global::Microsoft.EntityFrameworkCore.ModelBuilder Key_property_is_not_used_for_FK_when_set_by_annotation()
    {
        SpecFails.Sync<global::System.InvalidOperationException>(() => base.Key_property_is_not_used_for_FK_when_set_by_annotation(), "The entity type 'Child' primary key property 'IdRow' must be mapped to");
        return default!;
    }

    public override global::Microsoft.EntityFrameworkCore.ModelBuilder Key_specified_on_multiple_properties_can_be_overridden()
        => base.Key_specified_on_multiple_properties_can_be_overridden();

    public override void Keyless_and_key_attributes_which_conflict_cause_warning()
        => base.Keyless_and_key_attributes_which_conflict_cause_warning();

    public override void Keyless_fluent_api_and_key_attribute_do_not_cause_warning()
        => base.Keyless_fluent_api_and_key_attribute_do_not_cause_warning();

#if (EF10 || EF11) || EF9
    // Fails: Required/MaxLength/StringLength are not enforced by the store EF-X309
    public override Task MaxLengthAttribute_throws_while_inserting_value_longer_than_max_length()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.MaxLengthAttribute_throws_while_inserting_value_longer_than_max_length(), "Assert.Throws() Failure: No exception was thrown");
#elif EF8
    // Fails: Required/MaxLength/StringLength are not enforced by the store EF-X309
    public override void MaxLengthAttribute_throws_while_inserting_value_longer_than_max_length()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.MaxLengthAttribute_throws_while_inserting_value_longer_than_max_length(), "Assert.Throws() Failure: No exception was thrown");
#endif

    public override void MaxLength_with_length_takes_precedence_over_StringLength()
        => base.MaxLength_with_length_takes_precedence_over_StringLength();

    public override void Multiple_self_ref_ForeignKey_and_Inverse()
        => base.Multiple_self_ref_ForeignKey_and_Inverse();

    public override void Multiple_self_ref_ForeignKeys_on_navigations()
        => base.Multiple_self_ref_ForeignKeys_on_navigations();

    public override void Multiple_self_ref_ForeignKeys_on_properties()
        => base.Multiple_self_ref_ForeignKeys_on_properties();

    // Fails: [Column(TypeName)] is reported as an error by default EF-X312
    public override global::Microsoft.EntityFrameworkCore.Metadata.IModel Non_public_annotations_are_enabled()
    {
        SpecFails.Sync<global::System.InvalidOperationException>(() => base.Non_public_annotations_are_enabled(), "An error was generated for warning 'Microsoft.EntityFrameworkCore.Mode");
        return default!;
    }

    public override void NotMappedAttribute_ignores_entityType()
        => base.NotMappedAttribute_ignores_entityType();

    public override void NotMappedAttribute_ignores_explicit_interface_implementation_property()
        => base.NotMappedAttribute_ignores_explicit_interface_implementation_property();

    public override void NotMappedAttribute_ignores_navigation()
        => base.NotMappedAttribute_ignores_navigation();

    public override void NotMappedAttribute_ignores_property()
        => base.NotMappedAttribute_ignores_property();

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void NotMappedAttribute_removes_ambiguity_in_relationship_building()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.NotMappedAttribute_removes_ambiguity_in_relationship_building(), "Assert.Contains() Failure: Item not found in collection");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void NotMappedAttribute_removes_ambiguity_in_relationship_building_with_base()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.NotMappedAttribute_removes_ambiguity_in_relationship_building_with_base(), "Object reference not set to an instance of an object.");

    public override void NotMapped_on_abstract_base_class_property_ignores_it()
        => base.NotMapped_on_abstract_base_class_property_ignores_it();

#if (EF10 || EF11)
#if EF11
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void NotMapped_on_base_class_property_and_overridden_property_ignores_them()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.NotMapped_on_base_class_property_and_overridden_property_ignores_them());
#else
    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void NotMapped_on_base_class_property_and_overridden_property_ignores_them()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.NotMapped_on_base_class_property_and_overridden_property_ignores_them(), "The entity type 'AbstractBaseEntity2' cannot be instantiated because i");
#endif
#elif EF9 || EF8
    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void NotMapped_on_base_class_property_and_overridden_property_ignores_them()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.NotMapped_on_base_class_property_and_overridden_property_ignores_them(), "The corresponding CLR type for entity type 'AbstractBaseEntity2' canno");
#endif

#if (EF10 || EF11)
    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void NotMapped_on_base_class_property_discovered_through_navigation_ignores_it()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.NotMapped_on_base_class_property_discovered_through_navigation_ignores_it(), "The entity type 'AbstractBaseEntity3' cannot be instantiated because i");
#elif EF9 || EF8
    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void NotMapped_on_base_class_property_discovered_through_navigation_ignores_it()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.NotMapped_on_base_class_property_discovered_through_navigation_ignores_it(), "The corresponding CLR type for entity type 'AbstractBaseEntity3' canno");
#endif

#if (EF10 || EF11)
#if EF11
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void NotMapped_on_base_class_property_ignores_it()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.NotMapped_on_base_class_property_ignores_it());
#else
    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void NotMapped_on_base_class_property_ignores_it()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.NotMapped_on_base_class_property_ignores_it(), "The entity type 'AbstractBaseEntity1' cannot be instantiated because i");
#endif
#elif EF9 || EF8
    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void NotMapped_on_base_class_property_ignores_it()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.NotMapped_on_base_class_property_ignores_it(), "The corresponding CLR type for entity type 'AbstractBaseEntity1' canno");
#endif

    public override void NotMapped_on_new_property_with_same_name_as_in_unmapped_base_class_ignores_it()
        => base.NotMapped_on_new_property_with_same_name_as_in_unmapped_base_class_ignores_it();

#if (EF10 || EF11)
#if EF11
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void NotMapped_on_overridden_property_is_ignored()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.NotMapped_on_overridden_property_is_ignored());
#else
    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void NotMapped_on_overridden_property_is_ignored()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.NotMapped_on_overridden_property_is_ignored(), "The entity type 'AbstractBaseEntity3' cannot be instantiated because i");
#endif
#elif EF9 || EF8
    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void NotMapped_on_overridden_property_is_ignored()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.NotMapped_on_overridden_property_is_ignored(), "The corresponding CLR type for entity type 'AbstractBaseEntity3' canno");
#endif

    public override void NotMapped_on_unmapped_base_class_property_and_overridden_property_ignores_it()
        => base.NotMapped_on_unmapped_base_class_property_and_overridden_property_ignores_it();

    public override void NotMapped_on_unmapped_base_class_property_ignores_it()
        => base.NotMapped_on_unmapped_base_class_property_ignores_it();

    public override void NotMapped_on_unmapped_derived_property_ignores_it()
        => base.NotMapped_on_unmapped_derived_property_ignores_it();

    public override void NotMapped_should_propagate_down_inheritance_hierarchy()
        => base.NotMapped_should_propagate_down_inheritance_hierarchy();

    public override void Nothing_to_ForeignKey()
        => base.Nothing_to_ForeignKey();

    public override void Nothing_to_Required_and_ForeignKey()
        => base.Nothing_to_Required_and_ForeignKey();

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void OwnedEntityTypeAttribute_configures_all_references_as_owned()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.OwnedEntityTypeAttribute_configures_all_references_as_owned(), "An error was generated for warning 'Microsoft.EntityFrameworkCore.Mode");

    public override void OwnedEntityTypeAttribute_configures_one_reference_as_owned()
        => base.OwnedEntityTypeAttribute_configures_one_reference_as_owned();

    public override void PrecisionAttribute_sets_precision_for_properties_and_fields()
        => base.PrecisionAttribute_sets_precision_for_properties_and_fields();

#if (EF10 || EF11) || EF9
    // Fails: Required/MaxLength/StringLength are not enforced by the store EF-X309
    public override Task RequiredAttribute_for_navigation_throws_while_inserting_null_value()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.RequiredAttribute_for_navigation_throws_while_inserting_null_value(), "Assert.Throws() Failure: No exception was thrown");
#elif EF8
    // Fails: Required/MaxLength/StringLength are not enforced by the store EF-X309
    public override void RequiredAttribute_for_navigation_throws_while_inserting_null_value()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.RequiredAttribute_for_navigation_throws_while_inserting_null_value(), "Assert.Throws() Failure: No exception was thrown");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Required/MaxLength/StringLength are not enforced by the store EF-X309
    public override Task RequiredAttribute_for_property_throws_while_inserting_null_value()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.RequiredAttribute_for_property_throws_while_inserting_null_value(), "Assert.Throws() Failure: No exception was thrown");
#elif EF8
    // Fails: Required/MaxLength/StringLength are not enforced by the store EF-X309
    public override void RequiredAttribute_for_property_throws_while_inserting_null_value()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.RequiredAttribute_for_property_throws_while_inserting_null_value(), "Assert.Throws() Failure: No exception was thrown");
#endif

    public override void Required_and_ForeignKey_to_ForeignKey_can_be_overridden()
        => base.Required_and_ForeignKey_to_ForeignKey_can_be_overridden();

    public override void Required_and_ForeignKey_to_Required()
        => base.Required_and_ForeignKey_to_Required();

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void Required_and_ForeignKey_to_Required_and_ForeignKey()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Required_and_ForeignKey_to_Required_and_ForeignKey(), "The navigation 'Profile' cannot be changed, because the foreign key be");

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void Required_and_ForeignKey_to_Required_and_ForeignKey_can_be_overridden()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Required_and_ForeignKey_to_Required_and_ForeignKey_can_be_overridden(), "The navigation 'Profile' cannot be changed, because the foreign key be");

    public override void Required_and_ForeignKey_to_nothing()
        => base.Required_and_ForeignKey_to_nothing();

    // Fails: Single/First over empty result in test body (not triaged) EF-X310
    public override void Required_to_Nothing()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Required_to_Nothing(), "Sequence contains no elements");

    public override void Required_to_Nothing_inverted()
        => base.Required_to_Nothing_inverted();

    public override void Required_to_Required_and_ForeignKey()
        => base.Required_to_Required_and_ForeignKey();

    // Fails: Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override void Shared_ForeignKey_to_different_principals()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Shared_ForeignKey_to_different_principals(), "The entity type 'Profile12' has been marked as owned and must be refer");

#if (EF10 || EF11) || EF9
    // Fails: Required/MaxLength/StringLength are not enforced by the store EF-X309
    public override Task StringLengthAttribute_throws_while_inserting_value_longer_than_max_length()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.StringLengthAttribute_throws_while_inserting_value_longer_than_max_length(), "Assert.Throws() Failure: No exception was thrown");
#elif EF8
    // Fails: Required/MaxLength/StringLength are not enforced by the store EF-X309
    public override void StringLengthAttribute_throws_while_inserting_value_longer_than_max_length()
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.StringLengthAttribute_throws_while_inserting_value_longer_than_max_length(), "Assert.Throws() Failure: No exception was thrown");
#endif

    public override void StringLength_with_value_takes_precedence_over_MaxLength()
        => base.StringLength_with_value_takes_precedence_over_MaxLength();

    public override global::Microsoft.EntityFrameworkCore.Metadata.IModel TableNameAttribute_affects_table_name_in_TPH()
        => base.TableNameAttribute_affects_table_name_in_TPH();

#if (EF10 || EF11) || EF9
    // Fails: Single/First over empty result in test body (not triaged) EF-X310
    public override Task TimestampAttribute_throws_if_value_in_database_changed()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.TimestampAttribute_throws_if_value_in_database_changed(), "Sequence contains no elements");
#elif EF8
    // Fails: Single/First over empty result in test body (not triaged) EF-X310
    public override void TimestampAttribute_throws_if_value_in_database_changed()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.TimestampAttribute_throws_if_value_in_database_changed(), "Sequence contains no elements");
#endif

    public override global::Microsoft.EntityFrameworkCore.Metadata.IModel Timestamp_takes_precedence_over_MaxLength()
        => base.Timestamp_takes_precedence_over_MaxLength();

    public override void UnicodeAttribute_sets_unicode_for_properties_and_fields()
        => base.UnicodeAttribute_sets_unicode_for_properties_and_fields();
    // </generated>

    public class DataAnnotationMongoFixture : DataAnnotationFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("DataAnnotation");

        public TestServer TestServer { get; private set; }

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await base.InitializeAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;
    }
}
