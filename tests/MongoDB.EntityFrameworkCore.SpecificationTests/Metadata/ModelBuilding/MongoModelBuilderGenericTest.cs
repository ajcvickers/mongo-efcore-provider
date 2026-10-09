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
            // Fails: Complex Types: model debug view differs EF-168
            Assert.Contains(
                "Element type: string",
                Assert.Throws<EqualException>(() => base.Can_set_complex_property_annotation()).Message);
        }

        public override void Access_mode_can_be_overridden_at_entity_and_property_levels()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Access_mode_can_be_overridden_at_entity_and_property_levels()).Message);
        }

        public override void Primitive_collections_can_have_field_set()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Primitive_collections_can_have_field_set()).Message);
        }

        public override void Can_call_PrimitiveCollection_on_a_field()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Can_call_PrimitiveCollection_on_a_field()).Message);
        }

#if EF10
        public override void Can_call_Property_on_a_field()
        {
            // Fails: Complex Types: model building unsupported EF-168
            Assert.Contains(
                "is used in multiple navigations",
                Assert.Throws<InvalidOperationException>(() => base.Can_call_Property_on_a_field()).Message);
        }

        public override void Can_ignore_a_field()
        {
            // Fails: Complex Types: model building unsupported EF-168
            Assert.Contains(
                "is used in multiple navigations",
                Assert.Throws<InvalidOperationException>(() => base.Can_ignore_a_field()).Message);
        }

        public override void IEnumerable_properties_with_value_converter_set_are_not_discovered_as_complex_properties()
        {
            // Fails: Complex Types: model building unsupported EF-168
            Assert.Contains(
                "is used in multiple navigations",
                Assert.Throws<InvalidOperationException>(() => base.IEnumerable_properties_with_value_converter_set_are_not_discovered_as_complex_properties()).Message);
        }

        public override void Properties_can_be_ignored()
        {
            // Fails: Complex Types: model building unsupported EF-168
            Assert.Contains(
                "is used in multiple navigations",
                Assert.Throws<InvalidOperationException>(() => base.Properties_can_be_ignored()).Message);
        }

        public override void Properties_can_have_field_set()
        {
            // Fails: Complex Types: model building unsupported EF-168
            Assert.Contains(
                "is used in multiple navigations",
                Assert.Throws<InvalidOperationException>(() => base.Properties_can_have_field_set()).Message);
        }

        public override void Properties_can_have_value_converter_configured_by_type()
        {
            // Fails: Complex Types: model building unsupported EF-168
            Assert.Contains(
                "is used in multiple navigations",
                Assert.Throws<InvalidOperationException>(() => base.Properties_can_have_value_converter_configured_by_type()).Message);
        }

        public override void Value_converter_type_is_checked()
        {
            // Fails: Complex Types: model building unsupported EF-168
            Assert.Contains(
                "is used in multiple navigations",
                Assert.Throws<InvalidOperationException>(() => base.Value_converter_type_is_checked()).Message);
        }
#else
        public override void Can_add_shadow_primitive_collections_when_they_have_been_ignored()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Can_add_shadow_primitive_collections_when_they_have_been_ignored()).Message);
        }

        public override void Can_set_custom_value_generator_for_primitive_collections()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Can_set_custom_value_generator_for_primitive_collections()).Message);
        }

        public override void Can_set_max_length_for_primitive_collections()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Can_set_max_length_for_primitive_collections()).Message);
        }

        public override void Can_set_primitive_collection_annotation_when_no_clr_property()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Can_set_primitive_collection_annotation_when_no_clr_property()).Message);
        }

        public override void Can_set_sentinel_for_primitive_collections()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Can_set_sentinel_for_primitive_collections()).Message);
        }

        public override void Can_set_unicode_for_primitive_collections()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Can_set_unicode_for_primitive_collections()).Message);
        }

        public override void Primitive_collections_are_required_by_default_only_if_CLR_type_is_nullable()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Primitive_collections_are_required_by_default_only_if_CLR_type_is_nullable()).Message);
        }

        public override void Primitive_collections_can_be_made_concurrency_tokens()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Primitive_collections_can_be_made_concurrency_tokens()).Message);
        }

        public override void Primitive_collections_can_be_made_optional()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Primitive_collections_can_be_made_optional()).Message);
        }

        public override void Primitive_collections_can_be_made_required()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Primitive_collections_can_be_made_required()).Message);
        }

        public override void Primitive_collections_can_be_set_to_generate_values_on_Add()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Primitive_collections_can_be_set_to_generate_values_on_Add()).Message);
        }

        public override void Primitive_collections_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Primitive_collections_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties()).Message);
        }

        public override void Properties_can_be_made_concurrency_tokens()
        {
            // Fails: Complex Types: property model building differs EF-168
            Assert.Contains(
                "Expected: 6",
                Assert.Throws<EqualException>(() => base.Properties_can_be_made_concurrency_tokens()).Message);
        }

        public override void Properties_can_have_access_mode_set()
        {
            // Fails: Complex Types: primitive collection property not supported EF-168
            Assert.Contains(
                "could not be mapped because the database provider does not support this type",
                Assert.Throws<InvalidOperationException>(() => base.Properties_can_have_access_mode_set()).Message);
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
            // Fails: Complex Types: model debug view differs EF-168
            Assert.Contains(
                "Element type: string",
                Assert.Throws<EqualException>(() => base.Can_set_complex_property_annotation()).Message);
        }

        public override void Properties_can_be_ignored()
        {
            // Fails: Complex Types: model building unsupported EF-168
            Assert.Contains(
                "is used in multiple navigations",
                Assert.Throws<InvalidOperationException>(() => base.Properties_can_be_ignored()).Message);
        }

        public override void Properties_can_have_field_set()
        {
            // Fails: Complex Types: model building unsupported EF-168
            Assert.Contains(
                "is used in multiple navigations",
                Assert.Throws<InvalidOperationException>(() => base.Properties_can_have_field_set()).Message);
        }

        public override void Value_converter_type_is_checked()
        {
            // Fails: Complex Types: model building unsupported EF-168
            Assert.Contains(
                "is used in multiple navigations",
                Assert.Throws<InvalidOperationException>(() => base.Value_converter_type_is_checked()).Message);
        }
    }
#endif
}

#endif
