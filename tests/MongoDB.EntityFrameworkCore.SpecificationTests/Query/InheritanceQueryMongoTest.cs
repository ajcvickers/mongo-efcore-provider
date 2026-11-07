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

public class InheritanceQueryMongoTest : InheritanceQueryTestBase<InheritanceQueryMongoTest.InheritanceQueryMongoFixture>
{
    public InheritanceQueryMongoTest(InheritanceQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Can_query_when_shared_column(bool async)
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Can_query_when_shared_column(async), "Sequence contains");
    }

    public override async Task Can_query_all_types_when_shared_column(bool async)
    {
        await base.Can_query_all_types_when_shared_column(async);
    }

    public override async Task Can_use_of_type_animal(bool async)
    {
        await base.Can_use_of_type_animal(async);
    }

    public override async Task Can_use_is_kiwi(bool async)
    {
        await base.Can_use_is_kiwi(async);
    }

    public override async Task Can_use_is_kiwi_with_cast(bool async)
    {
        await base.Can_use_is_kiwi_with_cast(async);
    }

    public override async Task Can_use_backwards_is_animal(bool async)
    {
        // Fails: Hierarchy materialization fails: discriminator value does not match the requested type EF-X519
        await SpecFails.ThrowsAsync(() => base.Can_use_backwards_is_animal(async), "No discriminators matched");
    }

    public override async Task Can_use_is_kiwi_with_other_predicate(bool async)
    {
        await base.Can_use_is_kiwi_with_other_predicate(async);
    }

    public override async Task Can_use_is_kiwi_in_projection(bool async)
    {
        await base.Can_use_is_kiwi_in_projection(async);
    }

    public override async Task Can_use_of_type_bird(bool async)
    {
        await base.Can_use_of_type_bird(async);
    }

    public override async Task Can_use_of_type_bird_predicate(bool async)
    {
        await base.Can_use_of_type_bird_predicate(async);
    }

    public override async Task Can_use_of_type_bird_with_projection(bool async)
    {
        await base.Can_use_of_type_bird_with_projection(async);
    }

    public override async Task Can_use_of_type_bird_first(bool async)
    {
        await base.Can_use_of_type_bird_first(async);
    }

    public override async Task Can_use_of_type_kiwi(bool async)
    {
        await base.Can_use_of_type_kiwi(async);
    }

    public override async Task Can_use_backwards_of_type_animal(bool async)
    {
        // Fails: Hierarchy materialization fails: discriminator value does not match the requested type EF-X519
        await SpecFails.ThrowsAsync(() => base.Can_use_backwards_of_type_animal(async), "No discriminators matched");
    }

    public override async Task Can_use_of_type_rose(bool async)
    {
        await base.Can_use_of_type_rose(async);
    }

    public override async Task Can_query_all_animals(bool async)
    {
        await base.Can_query_all_animals(async);
    }

    public override async Task Can_query_all_animal_views(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Can_query_all_animal_views(async), "Failure");
    }

    public override async Task Can_query_all_plants(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Can_query_all_plants(async), "Document element is missing for required");
    }

    public override async Task Filter_on_property_inside_complex_type_on_derived_type(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Filter_on_property_inside_complex_type_on_derived_type(async), "Expression not supported");
    }

    public override async Task Can_filter_all_animals(bool async)
    {
        await base.Can_filter_all_animals(async);
    }

    public override async Task Can_query_all_birds(bool async)
    {
        await base.Can_query_all_birds(async);
    }

    public override async Task Can_query_just_kiwis(bool async)
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Can_query_just_kiwis(async), "Sequence contains");
    }

    public override async Task Can_query_just_roses(bool async)
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Can_query_just_roses(async), "Sequence contains");
    }

    public override async Task Can_include_animals(bool async)
    {
        await base.Can_include_animals(async);
    }

    public override async Task Can_include_prey(bool async)
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Can_include_prey(async), "Sequence contains");
    }

    public override async Task Can_use_of_type_kiwi_where_south_on_derived_property(bool async)
    {
        await base.Can_use_of_type_kiwi_where_south_on_derived_property(async);
    }

    public override async Task Can_use_of_type_kiwi_where_north_on_derived_property(bool async)
    {
        await base.Can_use_of_type_kiwi_where_north_on_derived_property(async);
    }

    public override async Task Discriminator_used_when_projection_over_derived_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Discriminator_used_when_projection_over_derived_type(async), "Failure");
    }

    public override async Task Discriminator_used_when_projection_over_derived_type2(bool async)
    {
        await base.Discriminator_used_when_projection_over_derived_type2(async);
    }

    public override async Task Discriminator_with_cast_in_shadow_property(bool async)
    {
        await base.Discriminator_with_cast_in_shadow_property(async);
    }

    public override async Task Discriminator_used_when_projection_over_of_type(bool async)
    {
        await base.Discriminator_used_when_projection_over_of_type(async);
    }

#if EF9 || EF10
    public override async Task Can_insert_update_delete()
    {
        await base.Can_insert_update_delete();
    }
#endif

    public override async Task Union_siblings_with_duplicate_property_in_subquery(bool async)
    {
        await base.Union_siblings_with_duplicate_property_in_subquery(async);
    }

    public override async Task OfType_Union_subquery(bool async)
    {
        await base.OfType_Union_subquery(async);
    }

    public override async Task OfType_Union_OfType(bool async)
    {
        await base.OfType_Union_OfType(async);
    }

    public override async Task Subquery_OfType(bool async)
    {
        await base.Subquery_OfType(async);
    }

    public override async Task Union_entity_equality(bool async)
    {
        await base.Union_entity_equality(async);
    }

#if EF9 || EF10
    public override async Task Setting_foreign_key_to_a_different_type_throws()
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Setting_foreign_key_to_a_different_type_throws(), "Sequence contains");
    }
#endif

    public override async Task Byte_enum_value_constant_used_in_projection(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Byte_enum_value_constant_used_in_projection(async), "Failure");
    }

#if EF9 || EF10
    public override async Task Member_access_on_intermediate_type_works()
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Member_access_on_intermediate_type_works(), "Failure");
    }
#endif

    public override async Task Is_operator_on_result_of_FirstOrDefault(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Is_operator_on_result_of_FirstOrDefault(async), "Expression not supported");
    }

    public override async Task Selecting_only_base_properties_on_base_type(bool async)
    {
        await base.Selecting_only_base_properties_on_base_type(async);
    }

    public override async Task Selecting_only_base_properties_on_derived_type(bool async)
    {
        await base.Selecting_only_base_properties_on_derived_type(async);
    }

    public override async Task Using_is_operator_on_multiple_type_with_no_result(bool async)
    {
        await base.Using_is_operator_on_multiple_type_with_no_result(async);
    }

    public override async Task Using_is_operator_with_of_type_on_multiple_type_with_no_result(bool async)
    {
        await base.Using_is_operator_with_of_type_on_multiple_type_with_no_result(async);
    }

    public override async Task Using_OfType_on_multiple_type_with_no_result(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Using_OfType_on_multiple_type_with_no_result(async), "Failure");
    }

    public override async Task GetType_in_hierarchy_in_abstract_base_type(bool async)
    {
        await base.GetType_in_hierarchy_in_abstract_base_type(async);
    }

    public override async Task GetType_in_hierarchy_in_intermediate_type(bool async)
    {
        await base.GetType_in_hierarchy_in_intermediate_type(async);
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling(async);
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling2(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling2(async);
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling2_reverse(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling2_reverse(async);
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling2_not_equal(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling2_not_equal(async);
    }

#if EF8
    public override void Can_insert_update_delete()
    {
        base.Can_insert_update_delete();
    }
#endif

#if EF8
    public override void Setting_foreign_key_to_a_different_type_throws()
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        SpecFails.Throws(() => base.Setting_foreign_key_to_a_different_type_throws(), "Sequence contains");
    }
#endif

#if EF8
    public override void Member_access_on_intermediate_type_works()
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        SpecFails.Throws(() => base.Member_access_on_intermediate_type_works(), "Failure");
    }
#endif

    public class InheritanceQueryMongoFixture : InheritanceQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("Inherit");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
        }

        public override async Task InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server);

            await base.InitializeAsync();
        }
    }
}
