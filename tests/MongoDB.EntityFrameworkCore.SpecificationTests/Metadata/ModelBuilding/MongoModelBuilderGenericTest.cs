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

        public override void Can_set_complex_property_annotation()
        {
            base.Can_set_complex_property_annotation();
        }

        public override void Access_mode_can_be_overridden_at_entity_and_property_levels()
        {
            base.Access_mode_can_be_overridden_at_entity_and_property_levels();
        }

        public override void Primitive_collections_can_have_field_set()
        {
            base.Primitive_collections_can_have_field_set();
        }

        public override void Can_call_PrimitiveCollection_on_a_field()
        {
            base.Can_call_PrimitiveCollection_on_a_field();
        }

#if EF10
        public override void Can_call_Property_on_a_field()
        {
            base.Can_call_Property_on_a_field();
        }

        public override void Can_ignore_a_field()
        {
            base.Can_ignore_a_field();
        }

        public override void IEnumerable_properties_with_value_converter_set_are_not_discovered_as_complex_properties()
        {
            base.IEnumerable_properties_with_value_converter_set_are_not_discovered_as_complex_properties();
        }

        public override void Properties_can_be_ignored()
        {
            base.Properties_can_be_ignored();
        }

        public override void Properties_can_have_field_set()
        {
            base.Properties_can_have_field_set();
        }

        public override void Properties_can_have_value_converter_configured_by_type()
        {
            base.Properties_can_have_value_converter_configured_by_type();
        }

        public override void Value_converter_type_is_checked()
        {
            base.Value_converter_type_is_checked();
        }
#else
        public override void Can_add_shadow_primitive_collections_when_they_have_been_ignored()
        {
            base.Can_add_shadow_primitive_collections_when_they_have_been_ignored();
        }

        public override void Can_set_custom_value_generator_for_primitive_collections()
        {
            base.Can_set_custom_value_generator_for_primitive_collections();
        }

        public override void Can_set_max_length_for_primitive_collections()
        {
            base.Can_set_max_length_for_primitive_collections();
        }

        public override void Can_set_primitive_collection_annotation_when_no_clr_property()
        {
            base.Can_set_primitive_collection_annotation_when_no_clr_property();
        }

        public override void Can_set_sentinel_for_primitive_collections()
        {
            base.Can_set_sentinel_for_primitive_collections();
        }

        public override void Can_set_unicode_for_primitive_collections()
        {
            base.Can_set_unicode_for_primitive_collections();
        }

        public override void Primitive_collections_are_required_by_default_only_if_CLR_type_is_nullable()
        {
            base.Primitive_collections_are_required_by_default_only_if_CLR_type_is_nullable();
        }

        public override void Primitive_collections_can_be_made_concurrency_tokens()
        {
            base.Primitive_collections_can_be_made_concurrency_tokens();
        }

        public override void Primitive_collections_can_be_made_optional()
        {
            base.Primitive_collections_can_be_made_optional();
        }

        public override void Primitive_collections_can_be_made_required()
        {
            base.Primitive_collections_can_be_made_required();
        }

        public override void Primitive_collections_can_be_set_to_generate_values_on_Add()
        {
            base.Primitive_collections_can_be_set_to_generate_values_on_Add();
        }

        public override void Primitive_collections_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties()
        {
            base.Primitive_collections_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties();
        }

        public override void Properties_can_be_made_concurrency_tokens()
        {
            base.Properties_can_be_made_concurrency_tokens();
        }

        public override void Properties_can_have_access_mode_set()
        {
            base.Properties_can_have_access_mode_set();
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
            base.Can_set_complex_property_annotation();
        }

        public override void Properties_can_be_ignored()
        {
            base.Properties_can_be_ignored();
        }

        public override void Properties_can_have_field_set()
        {
            base.Properties_can_have_field_set();
        }

        public override void Value_converter_type_is_checked()
        {
            base.Value_converter_type_is_checked();
        }
    }
#endif
}

#endif
