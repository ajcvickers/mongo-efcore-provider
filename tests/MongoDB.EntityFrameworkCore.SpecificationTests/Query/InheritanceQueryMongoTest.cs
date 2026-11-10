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
#if EF11
using Microsoft.EntityFrameworkCore.Query.Inheritance;
#endif
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

#if EF11
    public override async Task Primitive_collection_on_subtype(bool async)
    {
        await base.Primitive_collection_on_subtype(async);

        AssertMql(
            """
Drinks.{ "$match" : { "Ints" : { "$ne" : null, "$not" : { "$size" : 0 } } } }
""");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
#if EF11
    public override async Task Can_query_when_shared_column(bool async)
    {
        await base.Can_query_when_shared_column(async);

        AssertMql(
            """
Drinks.{ "$match" : { "_t" : 1 } }, { "$limit" : 2 }
""",
            //
            """
Drinks.{ "$match" : { "_t" : 2 } }, { "$limit" : 2 }
""",
            //
            """
Drinks.{ "$match" : { "_t" : 3 } }, { "$limit" : 2 }
""");
    }
#else
    public override async Task Can_query_when_shared_column(bool async)
    {
        await base.Can_query_when_shared_column(async);

        AssertMql(
            """
Drinks.{ "$match" : { "_t" : 1 } }, { "$limit" : 2 }
""",
            //
            """
Drinks.{ "$match" : { "_t" : 2 } }, { "$limit" : 2 }
""",
            //
            """
Drinks.{ "$match" : { "_t" : 3 } }, { "$limit" : 2 }
""");
    }
#endif
#else
    public override async Task Can_query_when_shared_column(bool async)
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Can_query_when_shared_column(async), "Sequence contains");
    }
#endif

#if EF11
    public override async Task Can_query_all_types_when_shared_column(bool async)
    {
        await base.Can_query_all_types_when_shared_column(async);

        AssertMql(
            """
Drinks.
""");
    }
#else
    public override async Task Can_query_all_types_when_shared_column(bool async)
    {
        await base.Can_query_all_types_when_shared_column(async);

        AssertMql(
            """
Drinks.
""");
    }
#endif

    public override async Task Can_use_of_type_animal(bool async)
    {
        await base.Can_use_of_type_animal(async);

        AssertMql(
            """
Animals.{ "$sort" : { "Species" : 1 } }
""");
    }

    public override async Task Can_use_is_kiwi(bool async)
    {
        await base.Can_use_is_kiwi(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }
""");
    }

    public override async Task Can_use_is_kiwi_with_cast(bool async)
    {
        await base.Can_use_is_kiwi_with_cast(async);

        // Review finding: the non-entity projection runs on the driver-LINQ fallback; NativeOnly refuses it ("Query projects a non-entity result").
        AssertMql(
            """
Animals.{ "$project" : { "Value" : { "$cond" : { "if" : { "$eq" : ["$_t", "Kiwi"] }, "then" : "$FoundOn", "else" : 0 } }, "_id" : 0 } }
""");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Can_use_backwards_is_animal(bool async)
    {
        await base.Can_use_backwards_is_animal(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }
""");
    }
#else
    public override async Task Can_use_backwards_is_animal(bool async)
    {
        // Fails: Hierarchy materialization fails: discriminator value does not match the requested type EF-X519
        await SpecFails.ThrowsAsync(() => base.Can_use_backwards_is_animal(async), "No discriminators matched");
    }
#endif

    public override async Task Can_use_is_kiwi_with_other_predicate(bool async)
    {
        await base.Can_use_is_kiwi_with_other_predicate(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi", "CountryId" : 1 } }
""");
    }

    public override async Task Can_use_is_kiwi_in_projection(bool async)
    {
        await base.Can_use_is_kiwi_in_projection(async);

        // Review finding: the projection runs on the driver-LINQ fallback (the whole document is fetched, no $project); NativeOnly refuses it ("Query projects a non-entity result").
        AssertMql(
            """
Animals.
""");
    }

    public override async Task Can_use_of_type_bird(bool async)
    {
        await base.Can_use_of_type_bird(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$sort" : { "Species" : 1 } }
""");
    }

    public override async Task Can_use_of_type_bird_predicate(bool async)
    {
        await base.Can_use_of_type_bird_predicate(async);

        AssertMql(
            """
Animals.{ "$match" : { "CountryId" : 1, "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$sort" : { "Species" : 1 } }
""");
    }

    public override async Task Can_use_of_type_bird_with_projection(bool async)
    {
        await base.Can_use_of_type_bird_with_projection(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$project" : { "EagleId" : "$EagleId", "_id" : 0 } }
""");
    }

    public override async Task Can_use_of_type_bird_first(bool async)
    {
        await base.Can_use_of_type_bird_first(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$sort" : { "Species" : 1 } }, { "$limit" : 1 }
""");
    }

    public override async Task Can_use_of_type_kiwi(bool async)
    {
        await base.Can_use_of_type_kiwi(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }
""");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Can_use_backwards_of_type_animal(bool async)
    {
        await base.Can_use_backwards_of_type_animal(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }
""");
    }
#else
    public override async Task Can_use_backwards_of_type_animal(bool async)
    {
        // Fails: Hierarchy materialization fails: discriminator value does not match the requested type EF-X519
        await SpecFails.ThrowsAsync(() => base.Can_use_backwards_of_type_animal(async), "No discriminators matched");
    }
#endif

    public override async Task Can_use_of_type_rose(bool async)
    {
        await base.Can_use_of_type_rose(async);

        AssertMql(
            """
Plants.{ "$match" : { "_t" : "Rose" } }
""");
    }

    public override async Task Can_query_all_animals(bool async)
    {
        await base.Can_query_all_animals(async);

        AssertMql(
            """
Animals.{ "$sort" : { "Species" : 1 } }
""");
    }

    public override async Task Can_query_all_animal_views(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Can_query_all_animal_views(async), "Failure");
    }

#if EF11
    public override async Task Can_query_all_plants(bool async)
    {
        await base.Can_query_all_plants(async);

        AssertMql(
            """
Plants.{ "$sort" : { "_id" : 1 } }
""");
    }
#else
    public override async Task Can_query_all_plants(bool async)
    {
        await base.Can_query_all_plants(async);

        AssertMql(
            """
Plants.{ "$sort" : { "_id" : 1 } }
""");
    }
#endif

#if !EF11
    public override async Task Filter_on_property_inside_complex_type_on_derived_type(bool async)
    {
        await base.Filter_on_property_inside_complex_type_on_derived_type(async);

        // Review finding: runs on the driver-LINQ fallback; NativeOnly refuses it ("Query is not natively representable").
        AssertMql(
            """
Plants.{ "$match" : { "_t" : "Daisy" } }, { "$match" : { "AdditionalInfo.LeafStructure.AreLeavesBig" : true } }
""");
    }
#endif

    public override async Task Can_filter_all_animals(bool async)
    {
        await base.Can_filter_all_animals(async);

        AssertMql(
            """
Animals.{ "$sort" : { "Species" : 1 } }, { "$match" : { "Name" : "Great spotted kiwi" } }
""");
    }

    public override async Task Can_query_all_birds(bool async)
    {
        await base.Can_query_all_birds(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$sort" : { "Species" : 1 } }
""");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Can_query_just_kiwis(bool async)
    {
        await base.Can_query_just_kiwis(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }, { "$limit" : 2 }
""");
    }
#else
    public override async Task Can_query_just_kiwis(bool async)
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Can_query_just_kiwis(async), "Sequence contains");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Can_query_just_roses(bool async)
    {
        await base.Can_query_just_roses(async);

        AssertMql(
            """
Plants.{ "$match" : { "_t" : "Rose" } }, { "$limit" : 2 }
""");
    }
#else
    public override async Task Can_query_just_roses(bool async)
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Can_query_just_roses(async), "Sequence contains");
    }
#endif

    public override async Task Can_include_animals(bool async)
    {
        await base.Can_include_animals(async);

        AssertMql(
            """
Countries.{ "$sort" : { "Name" : 1 } }, { "$lookup" : { "from" : "Animals", "localField" : "_id", "foreignField" : "CountryId", "as" : "_lookup_Animals" } }
""");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Can_include_prey(bool async)
    {
        await base.Can_include_prey(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Eagle" } }, { "$limit" : 2 }, { "$lookup" : { "from" : "Animals", "let" : { "localField" : "$_id" }, "pipeline" : [{ "$match" : { "$expr" : { "$eq" : ["$EagleId", "$$localField"] } } }, { "$match" : { "_t" : { "$in" : ["Bird", "Eagle", "Kiwi"] } } }], "as" : "_lookup_Prey" } }
""");
    }
#else
    public override async Task Can_include_prey(bool async)
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Can_include_prey(async), "Sequence contains");
    }
#endif

    public override async Task Can_use_of_type_kiwi_where_south_on_derived_property(bool async)
    {
        await base.Can_use_of_type_kiwi_where_south_on_derived_property(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi", "FoundOn" : 1 } }
""");
    }

    public override async Task Can_use_of_type_kiwi_where_north_on_derived_property(bool async)
    {
        await base.Can_use_of_type_kiwi_where_north_on_derived_property(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi", "FoundOn" : 0 } }
""");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Discriminator_used_when_projection_over_derived_type(bool async)
    {
        await base.Discriminator_used_when_projection_over_derived_type(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }, { "$project" : { "FoundOn" : "$FoundOn", "_id" : 0 } }
""");
    }
#else
    public override async Task Discriminator_used_when_projection_over_derived_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Discriminator_used_when_projection_over_derived_type(async), "Failure");
    }
#endif

    public override async Task Discriminator_used_when_projection_over_derived_type2(bool async)
    {
        await base.Discriminator_used_when_projection_over_derived_type2(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$project" : { "IsFlightless" : "$IsFlightless", "Discriminator" : "$_t", "_id" : 0 } }
""");
    }

    public override async Task Discriminator_with_cast_in_shadow_property(bool async)
    {
        await base.Discriminator_with_cast_in_shadow_property(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }, { "$project" : { "Predator" : "$Name", "_id" : 0 } }
""");
    }

    public override async Task Discriminator_used_when_projection_over_of_type(bool async)
    {
        await base.Discriminator_used_when_projection_over_of_type(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }, { "$project" : { "FoundOn" : "$FoundOn", "_id" : 0 } }
""");
    }

#if EF9 || (EF10 || EF11)
    public override async Task Can_insert_update_delete()
    {
        try
        {
            await base.Can_insert_update_delete();

            AssertMql(
                """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] }, "Species" : "Aquila chrysaetos canadensis" } }, { "$limit" : 2 }
""",
                //
                """
Countries.{ "$match" : { "_id" : 1 } }, { "$limit" : 2 }
""",
                //
                """
Animals.{ "$match" : { "_t" : "Kiwi", "Species" : { "$regularExpression" : { "pattern" : "owenii$", "options" : "s" } } } }, { "$limit" : 2 }
""",
                //
                """
Animals.{ "$match" : { "_t" : "Kiwi", "Species" : { "$regularExpression" : { "pattern" : "owenii$", "options" : "s" } } } }, { "$limit" : 2 }
""",
                //
                """
Animals.{ "$match" : { "_t" : "Kiwi", "Species" : { "$regularExpression" : { "pattern" : "owenii$", "options" : "s" } } } }, { "$count" : "v" }
""");
        }
        finally
        {
            await Fixture.ResetDatabaseAsync();
        }
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

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$sort" : { "Species" : 1 } }, { "$limit" : 5 }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$match" : { "_t" : "Kiwi" } }
""");
    }

    public override async Task Union_entity_equality(bool async)
    {
        await base.Union_entity_equality(async);
    }

#if EF9 || (EF10 || EF11)
    public override async Task Setting_foreign_key_to_a_different_type_throws()
    {
        // Fails: MongoDB has no foreign keys, so the SaveChanges the test expects to throw succeeds and persists a new Kiwi EF-X600
        try
        {
            await SpecFails.ThrowsAsync<global::Xunit.Sdk.ThrowsException>(() => base.Setting_foreign_key_to_a_different_type_throws());
        }
        finally
        {
            await Fixture.ResetDatabaseAsync();
        }
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Byte_enum_value_constant_used_in_projection(bool async)
    {
        await base.Byte_enum_value_constant_used_in_projection(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }, { "$project" : { "_v" : { "$cond" : { "if" : "$IsFlightless", "then" : { "$literal" : 0 }, "else" : { "$literal" : 1 } } }, "_id" : 0 } }
""");
    }
#else
    public override async Task Byte_enum_value_constant_used_in_projection(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Byte_enum_value_constant_used_in_projection(async), "Failure");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Member_access_on_intermediate_type_works()
    {
        await base.Member_access_on_intermediate_type_works();

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }, { "$sort" : { "Name" : 1 } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }
""");
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

        AssertMql(
            """
Animals.{ "$project" : { "Name" : "$Name", "_id" : 0 } }
""");
    }

    public override async Task Selecting_only_base_properties_on_derived_type(bool async)
    {
        await base.Selecting_only_base_properties_on_derived_type(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }
""");
    }

    public override async Task Using_is_operator_on_multiple_type_with_no_result(bool async)
    {
        await base.Using_is_operator_on_multiple_type_with_no_result(async);

        AssertMql(
            """
Animals.{ "$match" : { "$and" : [{ "_t" : "Kiwi" }, { "_t" : "Eagle" }] } }
""");
    }

    public override async Task Using_is_operator_with_of_type_on_multiple_type_with_no_result(bool async)
    {
        await base.Using_is_operator_with_of_type_on_multiple_type_with_no_result(async);

        AssertMql(
            """
Animals.{ "$match" : { "$and" : [{ "_t" : "Kiwi" }, { "_t" : "Eagle" }] } }
""");
    }

    public override async Task Using_OfType_on_multiple_type_with_no_result(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Using_OfType_on_multiple_type_with_no_result(async), "Failure");
    }

    public override async Task GetType_in_hierarchy_in_abstract_base_type(bool async)
    {
        await base.GetType_in_hierarchy_in_abstract_base_type(async);

        AssertMql(
            """
Animals.{ "$match" : { "_id" : { "$type" : -1 } } }
""");
    }

    public override async Task GetType_in_hierarchy_in_intermediate_type(bool async)
    {
        await base.GetType_in_hierarchy_in_intermediate_type(async);

        AssertMql(
            """
Animals.{ "$match" : { "_id" : { "$type" : -1 } } }
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Eagle" } }
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling2(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling2(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling2_reverse(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling2_reverse(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling2_not_equal(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling2_not_equal(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$ne" : "Kiwi" } } }
""");
    }

#if EF8
    public override void Can_insert_update_delete()
    {
        try
        {
            base.Can_insert_update_delete();

            AssertMql(
                """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] }, "Species" : "Aquila chrysaetos canadensis" } }, { "$limit" : 2 }
""",
                //
                """
Countries.{ "$match" : { "_id" : 1 } }, { "$limit" : 2 }
""",
                //
                """
Animals.{ "$match" : { "_t" : "Kiwi", "Species" : { "$regularExpression" : { "pattern" : "owenii$", "options" : "s" } } } }, { "$limit" : 2 }
""",
                //
                """
Animals.{ "$match" : { "_t" : "Kiwi", "Species" : { "$regularExpression" : { "pattern" : "owenii$", "options" : "s" } } } }, { "$limit" : 2 }
""",
                //
                """
Animals.{ "$match" : { "_t" : "Kiwi", "Species" : { "$regularExpression" : { "pattern" : "owenii$", "options" : "s" } } } }, { "$count" : "v" }
""");
        }
        finally
        {
            Fixture.ResetDatabaseAsync().GetAwaiter().GetResult();
        }
    }
#endif

#if EF8
    // Fails: MongoDB has no foreign keys, so the SaveChanges the test expects to throw succeeds and persists a new Kiwi EF-X600
    public override void Setting_foreign_key_to_a_different_type_throws()
    {
        try
        {
            SpecFails.Throws<global::Xunit.Sdk.ThrowsException>(() => base.Setting_foreign_key_to_a_different_type_throws());
        }
        finally
        {
            Fixture.ResetDatabaseAsync().GetAwaiter().GetResult();
        }
    }
#endif

#if EF8
    public override void Member_access_on_intermediate_type_works()
    {
        base.Member_access_on_intermediate_type_works();

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }, { "$sort" : { "Name" : 1 } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }
""");
    }
#endif

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

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

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        /// <summary>
        /// Drops and re-seeds the database. The fixture's database is shared by every test in the class and, unlike a relational
        /// provider, there is no transaction to roll back: a test whose SaveChanges succeeds (MongoDB has no foreign keys, so
        /// Setting_foreign_key_to_a_different_type_throws persists a new Kiwi) would otherwise change the row counts that later
        /// tests assert, and which tests fail would depend on the run order (and so on the OS).
        /// </summary>
        public async Task ResetDatabaseAsync()
        {
            ((MongoTestStore)TestStore).TestServer.Client.DropDatabase(StoreName);

            using var context = CreateContext();
            await context.Database.EnsureCreatedAsync();
            await SeedAsync(context);
        }
    }
}
