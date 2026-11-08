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

#if !EF8
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ModelBuilding;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.ModelBuilding;

public class MongoModelBuilderFixture : ModelBuilderTest.ModelBuilderFixtureBase
{
    public override TestHelpers TestHelpers
        => MongoTestHelpers.Instance;
}

public abstract class MongoModelBuilderTestBase : ModelBuilderTest
{
    public abstract class MongoNonRelationship(MongoModelBuilderFixture fixture)
        : NonRelationshipTestBase(fixture), IClassFixture<MongoModelBuilderFixture>
    {
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Access_mode_can_be_overridden_at_entity_and_primitive_collection_levels()
            => Assert.Throws<System.InvalidOperationException>(() => base.Access_mode_can_be_overridden_at_entity_and_primitive_collection_levels());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Access_mode_can_be_overridden_at_entity_and_property_levels()
            => Assert.Throws<System.InvalidOperationException>(() => base.Access_mode_can_be_overridden_at_entity_and_property_levels());
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Can_add_contained_indexes()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Can_add_contained_indexes());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_add_multiple_indexes()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_add_multiple_indexes());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_add_shared_type_entity_type()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_add_shared_type_entity_type());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_call_PrimitiveCollection_on_an_entity_with_fields()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_call_PrimitiveCollection_on_an_entity_with_fields());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_set_alternate_key_for_primitive_collection_on_an_entity_with_fields()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_set_alternate_key_for_primitive_collection_on_an_entity_with_fields());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_set_composite_key_for_primitive_collection_on_an_entity_with_fields()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_set_composite_key_for_primitive_collection_on_an_entity_with_fields());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_set_custom_value_generator_for_primitive_collections()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_set_custom_value_generator_for_primitive_collections());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_set_element_type_annotation()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_set_element_type_annotation());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_set_max_length_for_primitive_collections()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_set_max_length_for_primitive_collections());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_set_primitive_collection_annotation()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_set_primitive_collection_annotation());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_set_primitive_collection_annotation_by_type()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_set_primitive_collection_annotation_by_type());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_set_primitive_collection_annotation_when_no_clr_property()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_set_primitive_collection_annotation_when_no_clr_property());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_set_sentinel_for_primitive_collections()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_set_sentinel_for_primitive_collections());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_set_unicode_for_primitive_collections()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_set_unicode_for_primitive_collections());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_upgrade_candidate_key_to_primary_key()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_upgrade_candidate_key_to_primary_key());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_are_nullable_by_default_if_the_type_is_nullable()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_are_nullable_by_default_if_the_type_is_nullable());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_can_be_made_required()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_can_be_made_required());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_can_have_custom_type_value_converter_type_set()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_can_have_custom_type_value_converter_type_set());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_can_have_max_length()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_can_have_max_length());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_can_have_non_generic_value_converter_set()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_can_have_non_generic_value_converter_set());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_can_have_precision_and_scale()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_can_have_precision_and_scale());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_can_have_provider_type_set()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_can_have_provider_type_set());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_can_have_unicode_set()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_can_have_unicode_set());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_have_default_precision_and_scale()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_have_default_precision_and_scale());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_have_default_unicode()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_have_default_unicode());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Element_types_have_no_max_length_by_default()
            => Assert.Throws<System.InvalidOperationException>(() => base.Element_types_have_no_max_length_by_default());
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Entity_key_on_shadow_property_is_discovered_by_convention()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Entity_key_on_shadow_property_is_discovered_by_convention());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Primitive_collections_are_required_by_default_only_if_CLR_type_is_nullable()
            => Assert.Throws<System.InvalidOperationException>(() => base.Primitive_collections_are_required_by_default_only_if_CLR_type_is_nullable());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Primitive_collections_can_be_made_concurrency_tokens()
            => Assert.Throws<System.InvalidOperationException>(() => base.Primitive_collections_can_be_made_concurrency_tokens());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Primitive_collections_can_be_made_optional()
            => Assert.Throws<System.InvalidOperationException>(() => base.Primitive_collections_can_be_made_optional());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Primitive_collections_can_be_made_required()
            => Assert.Throws<System.InvalidOperationException>(() => base.Primitive_collections_can_be_made_required());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Primitive_collections_can_be_set_to_generate_values_on_Add()
            => Assert.Throws<System.InvalidOperationException>(() => base.Primitive_collections_can_be_set_to_generate_values_on_Add());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Primitive_collections_can_have_access_mode_set()
            => Assert.Throws<System.InvalidOperationException>(() => base.Primitive_collections_can_have_access_mode_set());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Primitive_collections_can_have_field_set()
            => Assert.Throws<System.InvalidOperationException>(() => base.Primitive_collections_can_have_field_set());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Primitive_collections_can_have_value_converter_set()
            => Assert.Throws<System.InvalidOperationException>(() => base.Primitive_collections_can_have_value_converter_set());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Primitive_collections_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties()
            => Assert.Throws<System.InvalidOperationException>(() => base.Primitive_collections_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties());
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Properties_can_be_set_to_generate_values_on_Add()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Properties_can_be_set_to_generate_values_on_Add());
        // Fails: Multiple row-version properties are rejected EF-X410
        public override void Properties_can_set_row_version()
            => Assert.Throws<System.NotSupportedException>(() => base.Properties_can_set_row_version());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Value_converter_type_on_primitive_collection_is_checked()
            => Assert.Throws<System.InvalidOperationException>(() => base.Value_converter_type_on_primitive_collection_is_checked());
    }

    public abstract class MongoInheritance(MongoModelBuilderFixture fixture)
        : InheritanceTestBase(fixture), IClassFixture<MongoModelBuilderFixture>
    {
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Base_type_can_be_discovered_after_creating_foreign_keys_on_derived()
            => Assert.Throws<System.NullReferenceException>(() => base.Base_type_can_be_discovered_after_creating_foreign_keys_on_derived());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Index_removed_when_covered_by_an_inherited_foreign_key()
            => Assert.Throws<System.NullReferenceException>(() => base.Index_removed_when_covered_by_an_inherited_foreign_key());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Relationships_are_discovered_on_the_base_entity_type()
            => Assert.Throws<System.NullReferenceException>(() => base.Relationships_are_discovered_on_the_base_entity_type());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Relationships_on_derived_types_are_discovered_first_if_base_is_one_sided()
            => Assert.Throws<System.NullReferenceException>(() => base.Relationships_on_derived_types_are_discovered_first_if_base_is_one_sided());
    }

    public abstract class MongoOneToMany(MongoModelBuilderFixture fixture)
        : OneToManyTestBase(fixture), IClassFixture<MongoModelBuilderFixture>
    {
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Can_exclude_navigation_with_foreign_key_attribute_on_principal_type_from_explicit_configuration()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Can_exclude_navigation_with_foreign_key_attribute_on_principal_type_from_explicit_configuration());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_FK_by_convention_specified_with_explicit_principal_key()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_FK_by_convention_specified_with_explicit_principal_key());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_FK_by_convention_specified_with_explicit_principal_key_in_any_order()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_FK_by_convention_specified_with_explicit_principal_key_in_any_order());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_have_FK_semi_specified_with_explicit_PK()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_have_FK_semi_specified_with_explicit_PK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_both_convention_properties_specified()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_both_convention_properties_specified());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_both_convention_properties_specified_in_any_order()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_both_convention_properties_specified_in_any_order());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_have_principal_key_by_convention_replaced_with_primary_key()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_have_principal_key_by_convention_replaced_with_primary_key());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_principal_key_by_convention_specified_with_explicit_PK()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_principal_key_by_convention_specified_with_explicit_PK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_principal_key_by_convention_specified_with_explicit_PK_in_any_order()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_principal_key_by_convention_specified_with_explicit_PK_in_any_order());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_specify_requiredness_after_OnDelete()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_specify_requiredness_after_OnDelete());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_use_alternate_composite_key()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_use_alternate_composite_key());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_use_alternate_composite_key_in_any_order()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_use_alternate_composite_key_in_any_order());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_use_explicitly_specified_PK()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_use_explicitly_specified_PK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_use_non_PK_principal()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_use_non_PK_principal());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_creates_FK_specified()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_creates_FK_specified());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_creates_composite_FK_specified()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_creates_composite_FK_specified());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_creates_shadow_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_creates_shadow_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_matches_shadow_FK_property_by_convention()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_matches_shadow_FK_property_by_convention());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_overrides_existing_FK_when_uniqueness_does_not_match()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_overrides_existing_FK_when_uniqueness_does_not_match());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_uses_existing_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_uses_existing_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_uses_existing_FK_not_found_by_convention()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_uses_existing_FK_not_found_by_convention());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_uses_existing_composite_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_uses_existing_composite_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_uses_specified_FK_even_if_found_by_convention()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_uses_specified_FK_even_if_found_by_convention());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_overlapping_foreign_keys_with_different_nullability()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_overlapping_foreign_keys_with_different_nullability());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_relationship_with_both_navigations()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_relationship_with_both_navigations());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_relationship_with_no_navigations_and_specified_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_relationship_with_no_navigations_and_specified_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_relationship_with_no_navigations_and_specified_composite_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_relationship_with_no_navigations_and_specified_composite_FK());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Creates_shadow_FK_property_with_shadow_PK()
            => Assert.Throws<System.InvalidOperationException>(() => base.Creates_shadow_FK_property_with_shadow_PK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_shadow_FK_with_navigation_to_dependent()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_shadow_FK_with_navigation_to_dependent());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_shadow_FK_with_navigation_to_principal()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_shadow_FK_with_navigation_to_principal());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_shadow_FK_with_no_navigation()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_shadow_FK_with_no_navigation());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_specified_FK_with_navigation_to_dependent()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_specified_FK_with_navigation_to_dependent());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_specified_FK_with_navigation_to_principal()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_specified_FK_with_navigation_to_principal());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_specified_composite_FK_with_navigation_to_dependent()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_specified_composite_FK_with_navigation_to_dependent());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_specified_composite_FK_with_navigation_to_principal()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_specified_composite_FK_with_navigation_to_principal());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Finds_existing_navigation_to_dependent_and_uses_associated_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Finds_existing_navigation_to_dependent_and_uses_associated_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Finds_existing_navigation_to_principal_and_uses_associated_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Finds_existing_navigation_to_principal_and_uses_associated_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Finds_existing_navigations_and_uses_associated_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Finds_existing_navigations_and_uses_associated_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Finds_existing_navigations_and_uses_associated_FK_with_fields()
            => Assert.Throws<System.NullReferenceException>(() => base.Finds_existing_navigations_and_uses_associated_FK_with_fields());
        // Fails: Expected EF validation or convention exception is not thrown EF-X408
        public override void Navigation_to_shared_type_is_not_discovered_by_convention()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Navigation_to_shared_type_is_not_discovered_by_convention());
    }

    public abstract class MongoManyToOne(MongoModelBuilderFixture fixture)
        : ManyToOneTestBase(fixture), IClassFixture<MongoModelBuilderFixture>
    {
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_FK_by_convention_specified_with_explicit_principal_key()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_FK_by_convention_specified_with_explicit_principal_key());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_FK_by_convention_specified_with_explicit_principal_key_in_any_order()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_FK_by_convention_specified_with_explicit_principal_key_in_any_order());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_both_convention_properties_specified()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_both_convention_properties_specified());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_both_convention_properties_specified_in_any_order()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_both_convention_properties_specified_in_any_order());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_principal_key_by_convention_specified_with_explicit_PK()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_principal_key_by_convention_specified_with_explicit_PK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_principal_key_by_convention_specified_with_explicit_PK_in_any_order()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_principal_key_by_convention_specified_with_explicit_PK_in_any_order());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_use_alternate_composite_key()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_use_alternate_composite_key());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_use_alternate_composite_key_in_any_order()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_use_alternate_composite_key_in_any_order());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_use_explicitly_specified_PK()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_use_explicitly_specified_PK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_use_non_PK_principal()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_use_non_PK_principal());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_creates_FK_specified()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_creates_FK_specified());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_creates_composite_FK_specified()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_creates_composite_FK_specified());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_creates_new_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_creates_new_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_creates_shadow_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_creates_shadow_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_does_not_use_existing_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_does_not_use_existing_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_finds_existing_composite_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_finds_existing_composite_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_matches_shadow_FK_by_convention()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_matches_shadow_FK_by_convention());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_overrides_existing_FK_if_uniqueness_does_not_match()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_overrides_existing_FK_if_uniqueness_does_not_match());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_uses_specified_FK_even_if_found_by_convention()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_uses_specified_FK_even_if_found_by_convention());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_with_existing_FK_not_found_by_convention()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_with_existing_FK_not_found_by_convention());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_relationship_with_no_navigations_and_specified_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_relationship_with_no_navigations_and_specified_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_relationship_with_no_navigations_and_specified_composite_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_relationship_with_no_navigations_and_specified_composite_FK());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Creates_shadow_FK_property_with_shadow_PK()
            => Assert.Throws<System.InvalidOperationException>(() => base.Creates_shadow_FK_property_with_shadow_PK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_shadow_FK_with_navigation_to_dependent()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_shadow_FK_with_navigation_to_dependent());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_shadow_FK_with_navigation_to_principal()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_shadow_FK_with_navigation_to_principal());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_shadow_FK_with_no_navigations_with()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_shadow_FK_with_no_navigations_with());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_specified_FK_with_navigation_to_dependent()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_specified_FK_with_navigation_to_dependent());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_specified_FK_with_navigation_to_principal()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_specified_FK_with_navigation_to_principal());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_specified_composite_FK_with_navigation_to_dependent()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_specified_composite_FK_with_navigation_to_dependent());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_specified_composite_FK_with_navigation_to_principal()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_specified_composite_FK_with_navigation_to_principal());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Finds_existing_navigation_to_dependent_and_uses_associated_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Finds_existing_navigation_to_dependent_and_uses_associated_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Finds_existing_navigation_to_principal_and_uses_associated_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Finds_existing_navigation_to_principal_and_uses_associated_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Finds_existing_navigations_and_uses_associated_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Finds_existing_navigations_and_uses_associated_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Finds_existing_navigations_and_uses_associated_FK_with_fields()
            => Assert.Throws<System.NullReferenceException>(() => base.Finds_existing_navigations_and_uses_associated_FK_with_fields());
    }

    public abstract class MongoOneToOne(MongoModelBuilderFixture fixture)
        : OneToOneTestBase(fixture), IClassFixture<MongoModelBuilderFixture>
    {
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_create_relationship_if_navigations_have_same_name()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_create_relationship_if_navigations_have_same_name());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_both_keys_specified_explicitly()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_both_keys_specified_explicitly());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Can_have_both_keys_specified_explicitly_in_any_order()
            => Assert.Throws<System.NullReferenceException>(() => base.Can_have_both_keys_specified_explicitly_in_any_order());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_specify_shadow_fk_before_configuring_principal_PK()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_specify_shadow_fk_before_configuring_principal_PK());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_specify_shadow_fk_before_reconfiguring_principal_PK()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_specify_shadow_fk_before_reconfiguring_principal_PK());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_specify_shadow_identifying_fk()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_specify_shadow_identifying_fk());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Creates_both_navigations_and_new_FK_over_PK_by_convention()
            => Assert.Throws<System.InvalidOperationException>(() => base.Creates_both_navigations_and_new_FK_over_PK_by_convention());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_uses_existing_FK_not_found_by_convention()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_uses_existing_FK_not_found_by_convention());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_both_navigations_and_uses_existing_composite_FK()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_both_navigations_and_uses_existing_composite_FK());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_composite_FK_when_specified_on_principal_with_no_navigations()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_composite_FK_when_specified_on_principal_with_no_navigations());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_principal_key_when_specified_on_principal_with_no_navigations()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_principal_key_when_specified_on_principal_with_no_navigations());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_relationship_with_specified_FK_with_no_navigations()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_relationship_with_specified_FK_with_no_navigations());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_self_referencing_FK_by_convention()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_self_referencing_FK_by_convention());
        // Fails: Types reachable only by navigation become owned, so the relationship model differs EF-X403
        public override void Creates_self_referencing_FK_by_convention_inverted()
            => Assert.Throws<System.NullReferenceException>(() => base.Creates_self_referencing_FK_by_convention_inverted());
        // Fails: Expected EF validation or convention exception is not thrown EF-X408
        public override void Navigation_to_shared_type_is_not_discovered_by_convention()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Navigation_to_shared_type_is_not_discovered_by_convention());
    }

    public abstract class MongoManyToMany(MongoModelBuilderFixture fixture)
        : ManyToManyTestBase(fixture), IClassFixture<MongoModelBuilderFixture>
    {
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_use_implicit_shared_type_as_join_entity()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_use_implicit_shared_type_as_join_entity());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_use_implicit_shared_type_with_default_name_as_join_entity()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_use_implicit_shared_type_with_default_name_as_join_entity());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_use_implicit_shared_type_with_implicit_relationships_as_join_entity()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_use_implicit_shared_type_with_implicit_relationships_as_join_entity());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void Can_use_shared_type_as_join_entity()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_use_shared_type_as_join_entity());
        // Fails: Many-to-many navigation relationship cannot be determined EF-X409
        public override void ForeignKeyAttribute_configures_the_properties()
            => Assert.Throws<System.InvalidOperationException>(() => base.ForeignKeyAttribute_configures_the_properties());
        // Fails: Many-to-many navigation relationship cannot be determined EF-X409
        public override void Join_type_is_automatically_configured_by_convention()
            => Assert.Throws<System.InvalidOperationException>(() => base.Join_type_is_automatically_configured_by_convention());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void UsingEntity_with_shared_type_passed_when_marked_as_shared_type()
            => Assert.Throws<System.InvalidOperationException>(() => base.UsingEntity_with_shared_type_passed_when_marked_as_shared_type());
        // Fails: Primary key must map to _id (composite/shadow/field keys rejected) EF-X405
        public override void UsingEntity_with_shared_type_passes_when_configured_as_shared()
            => Assert.Throws<System.InvalidOperationException>(() => base.UsingEntity_with_shared_type_passes_when_configured_as_shared());
    }

    public abstract class MongoOwnedTypes(MongoModelBuilderFixture fixture)
        : OwnedTypesTestBase(fixture), IClassFixture<MongoModelBuilderFixture>
    {
        // Fails: Ambiguous foreign key between owned/join types EF-X401
        public override void Can_configure_another_relationship_to_owner()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_another_relationship_to_owner());
        // Fails: Owned type used from multiple navigations with different element names is rejected EF-X407
        public override void Can_configure_multiple_ownerships()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_multiple_ownerships());
        // Fails: Owned type used from multiple navigations with different element names is rejected EF-X407
        public override void Can_configure_one_to_one_relationship_from_an_owned_type()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_one_to_one_relationship_from_an_owned_type());
        // Fails: Owned type used from multiple navigations with different element names is rejected EF-X407
        public override void Can_configure_owned_entity_and_property_of_same_type()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_owned_entity_and_property_of_same_type());
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Can_configure_owned_type()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_owned_type());
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Can_configure_owned_type_collection()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_owned_type_collection());
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Can_configure_owned_type_collection_from_an_owned_type()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Can_configure_owned_type_collection_from_an_owned_type());
        // Fails: Owned type used from multiple navigations with different element names is rejected EF-X407
        public override void Can_configure_owned_type_collection_with_one_call()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_owned_type_collection_with_one_call());
        // Fails: Owned type used from multiple navigations with different element names is rejected EF-X407
        public override void Can_configure_owned_type_collection_with_one_call_afterwards()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_owned_type_collection_with_one_call_afterwards());
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Can_configure_owned_type_collection_without_explicit_key()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Can_configure_owned_type_collection_without_explicit_key());
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Can_configure_owned_type_collection_without_explicit_key_or_candidate()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Can_configure_owned_type_collection_without_explicit_key_or_candidate());
        // Fails: Seed data for owned types fails validation EF-X406
        public override void Can_configure_owned_type_from_an_owned_type_collection(global::Microsoft.EntityFrameworkCore.ModelBuilding.ModelBuilderTest.OwnedTypesTestBase.HasDataOverload hasDataOverload)
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_owned_type_from_an_owned_type_collection(hasDataOverload));
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Can_configure_owned_type_inverse()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Can_configure_owned_type_inverse());
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Can_configure_ownership_foreign_key()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Can_configure_ownership_foreign_key());
        // Fails: Owned type used from multiple navigations with different element names is rejected EF-X407
        public override void Can_configure_property_and_owned_entity_of_same_type()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_property_and_owned_entity_of_same_type());
        // Fails: Model metadata differs from baseline (owned types, value generation) EF-X411
        public override void Can_configure_relationship_with_PK_ValueConverter()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Can_configure_relationship_with_PK_ValueConverter());
        // Fails: Collection-typed properties (List/array/ObservableCollection) are not mapped EF-X404
        public override void Can_configure_relationship_with_PK_ValueConverter_shadow_FK()
            => Assert.Throws<System.InvalidOperationException>(() => base.Can_configure_relationship_with_PK_ValueConverter_shadow_FK());
        // Fails: Expected EF validation or convention exception is not thrown EF-X408
        public override void Configuring_base_type_as_owned_throws()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Configuring_base_type_as_owned_throws());
        // Fails: Expected EF validation or convention exception is not thrown EF-X408
        public override void Deriving_from_owned_type_throws()
            => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => base.Deriving_from_owned_type_throws());
    }
}

public class MongoModelBuilderGenericTest : MongoModelBuilderTestBase
{
    public class MongoGenericNonRelationship(MongoModelBuilderFixture fixture) : MongoNonRelationship(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class MongoGenericInheritance(MongoModelBuilderFixture fixture) : MongoInheritance(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class MongoGenericOneToMany(MongoModelBuilderFixture fixture) : MongoOneToMany(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class MongoGenericManyToOne(MongoModelBuilderFixture fixture) : MongoManyToOne(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class MongoGenericOneToOne(MongoModelBuilderFixture fixture) : MongoOneToOne(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class MongoGenericManyToMany(MongoModelBuilderFixture fixture) : MongoManyToMany(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class MongoGenericOwnedTypes(MongoModelBuilderFixture fixture) : MongoOwnedTypes(fixture)
    {
#if EF11
        private readonly object? _ef11Skips = Ef11TestSkips.SkipIfListed("xUnit v3 (EF11) cannot hide this non-virtual upstream test; it fails here for the reasons in EF-X411 (see the New-method override on EF8 to EF10)", "Can_configure_relationship_with_PK_ValueConverter_after_FK");
#endif

        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new GenericTestModelBuilder(Fixture, configure);
    }
}

public class MongoModelBuilderNonGenericTest : MongoModelBuilderTestBase
{
    public class MongoNonGenericNonRelationship(MongoModelBuilderFixture fixture) : MongoNonRelationship(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new NonGenericTestModelBuilder(Fixture, configure);
    }

    public class MongoNonGenericInheritance(MongoModelBuilderFixture fixture) : MongoInheritance(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new NonGenericTestModelBuilder(Fixture, configure);
    }

    public class MongoNonGenericOneToMany(MongoModelBuilderFixture fixture) : MongoOneToMany(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new NonGenericTestModelBuilder(Fixture, configure);
    }

    public class MongoNonGenericManyToOne(MongoModelBuilderFixture fixture) : MongoManyToOne(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new NonGenericTestModelBuilder(Fixture, configure);
    }

    public class MongoNonGenericOneToOne(MongoModelBuilderFixture fixture) : MongoOneToOne(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new NonGenericTestModelBuilder(Fixture, configure);
    }

    public class MongoNonGenericManyToMany(MongoModelBuilderFixture fixture) : MongoManyToMany(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new NonGenericTestModelBuilder(Fixture, configure);
    }

    public class MongoNonGenericOwnedTypes(MongoModelBuilderFixture fixture) : MongoOwnedTypes(fixture)
    {
#if EF11
        private readonly object? _ef11Skips = Ef11TestSkips.SkipIfListed("xUnit v3 (EF11) cannot hide this non-virtual upstream test; it fails here for the reasons in EF-X411 (see the New-method override on EF8 to EF10)", "Can_configure_relationship_with_PK_ValueConverter_after_FK");
#endif

        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new NonGenericTestModelBuilder(Fixture, configure);
    }
}

#endif
