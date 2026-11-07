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

#nullable enable

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ModelBuilding;
using Xunit.Sdk;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Metadata.ModelBuilding;

public class MongoModelBuilderGenericTest : MongoModelBuilderTest
{
    public class MongoGenericComplexType(MongoModelBuilderFixture fixture) : MongoComplexType(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new GenericTestModelBuilder(Fixture, configure);

#if EF9 || EF11
        public override void Properties_can_set_row_version()
        {
            // Fails: Concurrency token inside a complex type is refused by model validation #34
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Properties_can_set_row_version());
        }
#endif

#if EF11
        public override void Chained_primitive_collection_lambda_configures_nested_collection()
        {
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Chained_primitive_collection_lambda_configures_nested_collection());
        }
#endif

        public override void Can_set_complex_property_annotation()
        {
#if EF9 || EF10 || EF11
            // Fails: Complex property annotation message differs #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_set_complex_property_annotation());
#else
            base.Can_set_complex_property_annotation();
        
#endif
        }

        public override void Access_mode_can_be_overridden_at_entity_and_property_levels()
        {
#if EF9 || EF10 || EF11
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Access_mode_can_be_overridden_at_entity_and_property_levels());
#else
            base.Access_mode_can_be_overridden_at_entity_and_property_levels();
        
#endif
        }

        public override void Primitive_collections_can_have_field_set()
        {
#if EF9 || EF10 || EF11
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Primitive_collections_can_have_field_set());
#else
            base.Primitive_collections_can_have_field_set();
        
#endif
        }

        public override void Can_call_PrimitiveCollection_on_a_field()
        {
#if EF9 || EF10 || EF11
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_call_PrimitiveCollection_on_a_field());
#else
            base.Can_call_PrimitiveCollection_on_a_field();
        
#endif
        }

#if EF10 || EF11
        public override void Can_call_Property_on_a_field()
        {
#if EF10 || EF11
            // Fails: Model builder fixture reuses one owned type under several navigations #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_call_Property_on_a_field());
#else
            base.Can_call_Property_on_a_field();
        
#endif
        }

        public override void Can_ignore_a_field()
        {
#if EF10 || EF11
            // Fails: Model builder fixture reuses one owned type under several navigations #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_ignore_a_field());
#else
            base.Can_ignore_a_field();
        
#endif
        }

        public override void IEnumerable_properties_with_value_converter_set_are_not_discovered_as_complex_properties()
        {
#if EF10 || EF11
            // Fails: Model builder fixture reuses one owned type under several navigations #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.IEnumerable_properties_with_value_converter_set_are_not_discovered_as_complex_properties());
#else
            base.IEnumerable_properties_with_value_converter_set_are_not_discovered_as_complex_properties();
        
#endif
        }

        public override void Properties_can_be_ignored()
        {
#if EF10 || EF11
            // Fails: Model builder fixture reuses one owned type under several navigations #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Properties_can_be_ignored());
#else
            base.Properties_can_be_ignored();
        
#endif
        }

        public override void Properties_can_have_field_set()
        {
#if EF10 || EF11
            // Fails: Model builder fixture reuses one owned type under several navigations #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Properties_can_have_field_set());
#else
            base.Properties_can_have_field_set();
        
#endif
        }

        public override void Properties_can_have_value_converter_configured_by_type()
        {
#if EF10 || EF11
            // Fails: Model builder fixture reuses one owned type under several navigations #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Properties_can_have_value_converter_configured_by_type());
#else
            base.Properties_can_have_value_converter_configured_by_type();
        
#endif
        }

        public override void Value_converter_type_is_checked()
        {
#if EF10 || EF11
            // Fails: Model builder fixture reuses one owned type under several navigations #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Value_converter_type_is_checked());
#else
            base.Value_converter_type_is_checked();
        
#endif
        }
#else
        public override void Can_add_shadow_primitive_collections_when_they_have_been_ignored()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_add_shadow_primitive_collections_when_they_have_been_ignored());
#else
            base.Can_add_shadow_primitive_collections_when_they_have_been_ignored();
        
#endif
        }

        public override void Can_set_custom_value_generator_for_primitive_collections()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_set_custom_value_generator_for_primitive_collections());
#else
            base.Can_set_custom_value_generator_for_primitive_collections();
        
#endif
        }

        public override void Can_set_max_length_for_primitive_collections()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_set_max_length_for_primitive_collections());
#else
            base.Can_set_max_length_for_primitive_collections();
        
#endif
        }

        public override void Can_set_primitive_collection_annotation_when_no_clr_property()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_set_primitive_collection_annotation_when_no_clr_property());
#else
            base.Can_set_primitive_collection_annotation_when_no_clr_property();
        
#endif
        }

        public override void Can_set_sentinel_for_primitive_collections()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_set_sentinel_for_primitive_collections());
#else
            base.Can_set_sentinel_for_primitive_collections();
        
#endif
        }

        public override void Can_set_unicode_for_primitive_collections()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_set_unicode_for_primitive_collections());
#else
            base.Can_set_unicode_for_primitive_collections();
        
#endif
        }

        public override void Primitive_collections_are_required_by_default_only_if_CLR_type_is_nullable()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Primitive_collections_are_required_by_default_only_if_CLR_type_is_nullable());
#else
            base.Primitive_collections_are_required_by_default_only_if_CLR_type_is_nullable();
        
#endif
        }

        public override void Primitive_collections_can_be_made_concurrency_tokens()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Primitive_collections_can_be_made_concurrency_tokens());
#else
            base.Primitive_collections_can_be_made_concurrency_tokens();
        
#endif
        }

        public override void Primitive_collections_can_be_made_optional()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Primitive_collections_can_be_made_optional());
#else
            base.Primitive_collections_can_be_made_optional();
        
#endif
        }

        public override void Primitive_collections_can_be_made_required()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Primitive_collections_can_be_made_required());
#else
            base.Primitive_collections_can_be_made_required();
        
#endif
        }

        public override void Primitive_collections_can_be_set_to_generate_values_on_Add()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Primitive_collections_can_be_set_to_generate_values_on_Add());
#else
            base.Primitive_collections_can_be_set_to_generate_values_on_Add();
        
#endif
        }

        public override void Primitive_collections_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Primitive_collections_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties());
#else
            base.Primitive_collections_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties();
        
#endif
        }

        public override void Properties_can_be_made_concurrency_tokens()
        {
#if EF9
            // Fails: Concurrency token inside a complex type is refused by model validation #34
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Properties_can_be_made_concurrency_tokens());
#else
            base.Properties_can_be_made_concurrency_tokens();
        
#endif
        }

        public override void Properties_can_have_access_mode_set()
        {
#if EF9
            // Fails: Primitive collection property type not mappable by the provider #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Properties_can_have_access_mode_set());
#else
            base.Properties_can_have_access_mode_set();
        
#endif
        }
#endif
    }

#if EF10
    public class MongoGenericComplexCollection(MongoModelBuilderFixture fixture) : MongoComplexCollection(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder>? configure = null)
            => new GenericTestModelBuilder(Fixture, configure);

        public override void Can_set_complex_property_annotation()
        {
#if EF10 || EF11
            // Fails: Complex property annotation message differs #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Can_set_complex_property_annotation());
#else
            base.Can_set_complex_property_annotation();
        
#endif
        }

        public override void Properties_can_be_ignored()
        {
#if EF10 || EF11
            // Fails: Model builder fixture reuses one owned type under several navigations #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Properties_can_be_ignored());
#else
            base.Properties_can_be_ignored();
        
#endif
        }

        public override void Properties_can_have_field_set()
        {
#if EF10 || EF11
            // Fails: Model builder fixture reuses one owned type under several navigations #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Properties_can_have_field_set());
#else
            base.Properties_can_have_field_set();
        
#endif
        }

        public override void Value_converter_type_is_checked()
        {
#if EF10 || EF11
            // Fails: Model builder fixture reuses one owned type under several navigations #35
            global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGap(() => base.Value_converter_type_is_checked());
#else
            base.Value_converter_type_is_checked();
        
#endif
        }
    }
#endif
}

#endif
