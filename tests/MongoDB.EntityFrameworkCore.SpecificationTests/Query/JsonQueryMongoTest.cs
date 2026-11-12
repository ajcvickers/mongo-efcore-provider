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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class JsonQueryMongoTest : JsonQueryTestBase<JsonQueryMongoTest.JsonQueryMongoFixture>
{
    public JsonQueryMongoTest(JsonQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if EF9
    public override async Task Basic_json_projection_owner_entity(bool async)
    {
        await base.Basic_json_projection_owner_entity(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    public override async Task Basic_json_projection_owner_entity_NoTracking(bool async)
    {
        await base.Basic_json_projection_owner_entity_NoTracking(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    public override async Task Basic_json_projection_owner_entity_duplicated(bool async)
    {
        await base.Basic_json_projection_owner_entity_duplicated(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$project" : { "First" : "$$ROOT", "Second" : "$$ROOT", "_id" : 0 } }
""");
    }

    public override async Task Basic_json_projection_owner_entity_duplicated_NoTracking(bool async)
    {
        await base.Basic_json_projection_owner_entity_duplicated_NoTracking(async);

        AssertMql(
            """
JsonEntitiesSingleOwned.{ "$project" : { "First" : "$$ROOT", "Second" : "$$ROOT", "_id" : 0 } }
""");
    }

    public override async Task Basic_json_projection_owner_entity_twice(bool async)
    {
        await base.Basic_json_projection_owner_entity_twice(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$project" : { "First" : "$$ROOT", "Second" : "$$ROOT", "_id" : 0 } }
""");
    }

    public override async Task Basic_json_projection_owner_entity_twice_NoTracking(bool async)
    {
        await base.Basic_json_projection_owner_entity_twice_NoTracking(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$project" : { "First" : "$$ROOT", "Second" : "$$ROOT", "_id" : 0 } }
""");
    }

    public override async Task Basic_json_projection_owned_reference_root(bool async)
    {
        await base.Basic_json_projection_owned_reference_root(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    // Fails: NullReferenceException while materializing EF-X529
    public override Task Basic_json_projection_owned_reference_duplicated(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_duplicated(async));

    // Fails: NullReferenceException while materializing EF-X529
    public override Task Basic_json_projection_owned_reference_duplicated2(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_duplicated2(async));

    public override async Task Basic_json_projection_owned_collection_root(bool async)
    {
        await base.Basic_json_projection_owned_collection_root(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$project" : { "OwnedCollectionRoot" : "$OwnedCollectionRoot", "_id" : "$_id" } }
""");
    }

    public override async Task Basic_json_projection_owned_reference_branch(bool async)
    {
        await base.Basic_json_projection_owned_reference_branch(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    public override async Task Basic_json_projection_owned_collection_branch(bool async)
    {
        await base.Basic_json_projection_owned_collection_branch(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    // Fails: NullReferenceException while materializing EF-X529
    public override Task Json_projection_with_deduplication(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_with_deduplication(async));

    public override async Task Json_projection_with_deduplication_reverse_order(bool async)
    {
        await base.Json_projection_with_deduplication_reverse_order(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    public override async Task Left_join_json_entities(bool async)
    {
        await base.Left_join_json_entities(async);

        AssertMql(
            """
JsonEntitiesSingleOwned.{ "$lookup" : { "from" : "JsonEntitiesBasic", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_JsonEntityBasic" } }, { "$set" : { "_lookup_JsonEntityBasic" : { "$arrayElemAt" : ["$_lookup_JsonEntityBasic", 0] } } }, { "$project" : { "e1" : "$$ROOT", "_lookup_JsonEntityBasic" : "$_lookup_JsonEntityBasic", "_id" : 0 } }
""");
    }

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Project_json_entity_FirstOrDefault_subquery(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_FirstOrDefault_subquery(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Project_json_entity_FirstOrDefault_subquery_deduplication(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_FirstOrDefault_subquery_deduplication(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Project_json_entity_FirstOrDefault_subquery_deduplication_and_outer_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_FirstOrDefault_subquery_deduplication_and_outer_reference(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Project_json_entity_FirstOrDefault_subquery_deduplication_outer_reference_and_pruning(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_FirstOrDefault_subquery_deduplication_outer_reference_and_pruning(async), "could not be translated");

    // Fails: NotSupportedException while translating or materializing the query EF-X526
    public override Task Json_collection_SelectMany(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_SelectMany(async));

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_nested_collection_SelectMany(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_nested_collection_SelectMany(async), "could not be translated");
#endif
#if EF11
    // Fails: TargetInvocationException while translating or materializing the query EF-X526
    public override Task Entity_including_collection_with_json_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Entity_including_collection_with_json_AsNoTrackingWithIdentityResolution(async));

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Entity_including_collection_with_json_and_separate_json_projection_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Entity_including_collection_with_json_and_separate_json_projection_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_root_with_missing_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_root_with_missing_scalars(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_top_level_json_entity_with_missing_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_top_level_json_entity_with_missing_scalars(async));

    // Fails: No support for nested JSON EF-X008
    public override Task Project_nested_json_entity_with_missing_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_nested_json_entity_with_missing_scalars(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_top_level_entity_with_null_value_required_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_top_level_entity_with_null_value_required_scalars(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_root_entity_with_missing_required_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_root_entity_with_missing_required_navigation(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_missing_required_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_missing_required_navigation(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_root_entity_with_null_required_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_root_entity_with_null_required_navigation(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_null_required_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_null_required_navigation(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900 (depends on the time zone: fails in UTC) EF-X720
    public override Task Project_missing_required_scalar(bool async)
        => SpecFails.FailsInUtcAsync(() => base.Project_missing_required_scalar(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_null_required_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_null_required_scalar(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Optional_json_properties_materialized_as_null_when_the_element_in_json_is_not_present()
        => SpecFails.ThrowsAsync(() => base.Optional_json_properties_materialized_as_null_when_the_element_in_json_is_not_present());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Can_project_nullable_json_property_when_the_element_in_json_is_not_present()
        => SpecFails.ThrowsAsync(() => base.Can_project_nullable_json_property_when_the_element_in_json_is_not_present());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Accessing_missing_navigation_works()
        => SpecFails.ThrowsAsync(() => base.Accessing_missing_navigation_works());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Missing_navigation_works_with_deduplication(bool async)
        => SpecFails.ThrowsAsync(() => base.Missing_navigation_works_with_deduplication(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900 (depends on the time zone: passes in UTC) EF-X720
    public override Task Contains_on_nested_collection_with_init_only_navigation()
        => SpecFails.EitherAsync(() => base.Contains_on_nested_collection_with_init_only_navigation());

    public override async Task Project_json_with_no_properties()
    {
        await base.Project_json_with_no_properties();

        AssertMql(
            """
Entity.
""");
    }

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Query_with_nested_json_collection_mapped_to_private_field_via_IReadOnlyList()
        => SpecFails.ThrowsAsync(() => base.Query_with_nested_json_collection_mapped_to_private_field_via_IReadOnlyList());

    public override async Task Project_entity_with_json_null_values()
    {
        await base.Project_entity_with_json_null_values();

        AssertMql(
            """
Entities.
""");
    }

    public override async Task Try_project_collection_but_JSON_is_entity()
    {
        await base.Try_project_collection_but_JSON_is_entity();

        AssertMql(
            """
Junk.{ "$match" : { "_id" : 1 } }, { "$limit" : 1 }, { "$project" : { "Collection" : "$Collection", "_id" : "$_id" } }
""");
    }

    public override async Task Try_project_reference_but_JSON_is_collection()
    {
        await base.Try_project_reference_but_JSON_is_collection();

        AssertMql(
            """
Junk.{ "$match" : { "_id" : 2 } }, { "$limit" : 1 }
""");
    }

    // Fails: depends on the time zone: passes in UTC EF-X720
    public override Task Project_json_array_of_primitives_on_reference()
        => SpecFails.EitherAsync(() => base.Project_json_array_of_primitives_on_reference());

    public override async Task Project_json_array_of_primitives_on_collection()
    {
        await base.Project_json_array_of_primitives_on_collection();

        AssertMql();
    }

    public override async Task Project_element_of_json_array_of_primitives()
    {
        await base.Project_element_of_json_array_of_primitives();

        AssertMql(
            """
MyEntity.{ "$sort" : { "_id" : 1 } }, { "$project" : { "ArrayElement" : { "$arrayElemAt" : ["$Reference.IntArray", 0] }, "ListElement" : { "$arrayElemAt" : ["$Reference.ListOfString", 1] }, "_id" : 0 } }
""");
    }

    // Fails: depends on the time zone: passes in UTC EF-X720
    public override Task Predicate_based_on_element_of_json_array_of_primitives1()
        => SpecFails.EitherAsync(() => base.Predicate_based_on_element_of_json_array_of_primitives1());

    // Fails: depends on the time zone: passes in UTC EF-X720
    public override Task Predicate_based_on_element_of_json_array_of_primitives2()
        => SpecFails.EitherAsync(() => base.Predicate_based_on_element_of_json_array_of_primitives2());

    // Fails: depends on the time zone: passes in UTC EF-X720
    public override Task Predicate_based_on_element_of_json_array_of_primitives3()
        => SpecFails.EitherAsync(() => base.Predicate_based_on_element_of_json_array_of_primitives3());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Junk_in_json_basic_tracking()
        => SpecFails.ThrowsAsync(() => base.Junk_in_json_basic_tracking());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Junk_in_json_basic_no_tracking()
        => SpecFails.ThrowsAsync(() => base.Junk_in_json_basic_no_tracking());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Tricky_buffering_basic()
        => SpecFails.ThrowsAsync(() => base.Tricky_buffering_basic());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Shadow_properties_basic_tracking()
        => SpecFails.ThrowsAsync(() => base.Shadow_properties_basic_tracking());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Shadow_properties_basic_no_tracking()
        => SpecFails.ThrowsAsync(() => base.Shadow_properties_basic_no_tracking());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Project_shadow_properties_from_json_entity()
        => SpecFails.ThrowsAsync(() => base.Project_shadow_properties_from_json_entity());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900 (depends on the time zone: passes in UTC) EF-X720
    public override Task Project_proxies_entity_with_json()
        => SpecFails.EitherAsync(() => base.Project_proxies_entity_with_json());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900 (depends on the time zone: passes in UTC) EF-X720
    public override Task Project_proxies_entity_with_json_with_primitive_collection()
        => SpecFails.EitherAsync(() => base.Project_proxies_entity_with_json_with_primitive_collection());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Not_ICollection_basic_projection()
        => SpecFails.ThrowsAsync(() => base.Not_ICollection_basic_projection());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_duplicated_navigations(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_duplicated_navigations(noTracking));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_duplicated_scalars(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_duplicated_scalars(noTracking));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_empty_navigations(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_empty_navigations(noTracking));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_empty_scalars(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_empty_scalars(noTracking));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_null_navigations(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_null_navigations(noTracking));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_null_scalars(bool noTracking)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_null_scalars(noTracking));

#endif

    public override async Task Basic_json_projection_owner_entity_NoTrackingWithIdentityResolution(bool async)
    {
        await base.Basic_json_projection_owner_entity_NoTrackingWithIdentityResolution(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    public override async Task Basic_json_projection_owner_entity_duplicated_NoTrackingWithIdentityResolution(bool async)
    {
        await base.Basic_json_projection_owner_entity_duplicated_NoTrackingWithIdentityResolution(async);

        AssertMql(
            """
JsonEntitiesSingleOwned.{ "$project" : { "First" : "$$ROOT", "Second" : "$$ROOT", "_id" : 0 } }
""");
    }

    public override async Task Basic_json_projection_owner_entity_twice_NoTrackingWithIdentityResolution(bool async)
    {
        await base.Basic_json_projection_owner_entity_twice_NoTrackingWithIdentityResolution(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$project" : { "First" : "$$ROOT", "Second" : "$$ROOT", "_id" : 0 } }
""");
    }

    public override async Task Basic_json_projection_owned_reference_root_NoTrackingWithIdentityResolution(bool async)
    {
        await base.Basic_json_projection_owned_reference_root_NoTrackingWithIdentityResolution(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    // Fails: NullReferenceException while materializing EF-X529
    public override Task Basic_json_projection_owned_reference_duplicated_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_duplicated_NoTrackingWithIdentityResolution(async));

    // Fails: NullReferenceException while materializing EF-X529
    public override Task Basic_json_projection_owned_reference_duplicated2_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_reference_duplicated2_NoTrackingWithIdentityResolution(async));

    // Fails: KeyNotFoundException while translating or materializing the query EF-X526
    public override Task Basic_json_projection_owned_collection_root_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Basic_json_projection_owned_collection_root_NoTrackingWithIdentityResolution(async));

    public override async Task Basic_json_projection_owned_reference_branch_NoTrackingWithIdentityResolution(bool async)
    {
        await base.Basic_json_projection_owned_reference_branch_NoTrackingWithIdentityResolution(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    public override async Task Basic_json_projection_owned_collection_branch_NoTrackingWithIdentityResolution(bool async)
    {
        await base.Basic_json_projection_owned_collection_branch_NoTrackingWithIdentityResolution(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    public override async Task Basic_json_projection_owned_reference_leaf(bool async)
    {
        await base.Basic_json_projection_owned_reference_leaf(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    public override async Task Basic_json_projection_owned_collection_leaf(bool async)
    {
        await base.Basic_json_projection_owned_collection_leaf(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    public override async Task Basic_json_projection_scalar(bool async)
    {
        await base.Basic_json_projection_scalar(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$project" : { "_v" : "$OwnedReferenceRoot.Name", "_id" : 0 } }
""");
    }

    public override async Task Json_scalar_length(bool async)
    {
        await base.Json_scalar_length(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$gt" : [{ "$cond" : { "if" : { "$eq" : [{ "$ifNull" : ["$OwnedReferenceRoot.Name", null] }, null] }, "then" : null, "else" : { "$strLenCP" : "$OwnedReferenceRoot.Name" } } }, 2] } } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }
""");
    }

    public override async Task Basic_json_projection_enum_inside_json_entity(bool async)
    {
        await base.Basic_json_projection_enum_inside_json_entity(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$project" : { "Id" : "$_id", "Enum" : "$OwnedReferenceRoot.OwnedReferenceBranch.Enum", "_id" : 0 } }
""");
    }

    public override async Task Json_projection_enum_with_custom_conversion(bool async)
    {
        await base.Json_projection_enum_with_custom_conversion(async);

        AssertMql(
            """
JsonEntitiesCustomNaming.{ "$project" : { "Id" : "$_id", "Enum" : "$OwnedReferenceRoot.Enum", "_id" : 0 } }
""");
    }

    public override async Task Json_property_in_predicate(bool async)
    {
        await base.Json_property_in_predicate(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "OwnedReferenceRoot.OwnedReferenceBranch.Fraction" : { "$lt" : { "$numberDecimal" : "20.5" } } } }, { "$project" : { "_id" : "$_id" } }
""");
    }

    public override async Task Json_subquery_property_pushdown_length(bool async)
    {
        await base.Json_subquery_property_pushdown_length(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$sort" : { "_id" : 1 } }, { "$limit" : 3 }, { "$project" : { "_v" : "$OwnedReferenceRoot.OwnedReferenceBranch.OwnedReferenceLeaf.SomethingSomething", "_id" : 0 } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$project" : { "_v" : { "$strLenCP" : "$_v" }, "_id" : 0 } }
""");
    }

    // Fails: InvalidCastException while translating or materializing the query EF-X526
    public override Task Json_subquery_reference_pushdown_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_subquery_reference_pushdown_reference(async));

    public override async Task Json_subquery_reference_pushdown_reference_anonymous_projection(bool async)
    {
        await base.Json_subquery_reference_pushdown_reference_anonymous_projection(async);

        AssertMql();
    }

    public override async Task Json_subquery_reference_pushdown_reference_pushdown_anonymous_projection(bool async)
    {
        await base.Json_subquery_reference_pushdown_reference_pushdown_anonymous_projection(async);

        AssertMql();
    }

    // Fails: InvalidCastException while translating or materializing the query EF-X526
    public override Task Json_subquery_reference_pushdown_reference_pushdown_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_subquery_reference_pushdown_reference_pushdown_reference(async));

    // Fails: InvalidCastException while translating or materializing the query EF-X526
    public override Task Json_subquery_reference_pushdown_reference_pushdown_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_subquery_reference_pushdown_reference_pushdown_collection(async));

    public override async Task Json_subquery_reference_pushdown_property(bool async)
    {
        await base.Json_subquery_reference_pushdown_property(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$sort" : { "_id" : 1 } }, { "$limit" : 10 }, { "$project" : { "_v" : "$OwnedReferenceRoot.OwnedReferenceBranch.OwnedReferenceLeaf", "_id" : 0 } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$project" : { "_v" : "$_v.SomethingSomething", "_id" : 0 } }
""");
    }

    public override async Task Custom_naming_projection_owner_entity(bool async)
    {
        await base.Custom_naming_projection_owner_entity(async);

        AssertMql(
            """
JsonEntitiesCustomNaming.
""");
    }

    public override async Task Custom_naming_projection_owned_reference(bool async)
    {
        await base.Custom_naming_projection_owned_reference(async);

        AssertMql(
            """
JsonEntitiesCustomNaming.
""");
    }

    public override async Task Custom_naming_projection_owned_collection(bool async)
    {
        await base.Custom_naming_projection_owned_collection(async);

        AssertMql(
            """
JsonEntitiesCustomNaming.{ "$sort" : { "_id" : 1 } }, { "$project" : { "OwnedCollectionRoot" : "$OwnedCollectionRoot", "_id" : "$_id" } }
""");
    }

    public override async Task Custom_naming_projection_owned_scalar(bool async)
    {
        await base.Custom_naming_projection_owned_scalar(async);

        AssertMql(
            """
JsonEntitiesCustomNaming.{ "$project" : { "_v" : "$OwnedReferenceRoot.OwnedReferenceBranch.Fraction", "_id" : 0 } }
""");
    }

    // Fails: NullReferenceException while materializing EF-X529
    public override Task Custom_naming_projection_everything(bool async)
        => SpecFails.ThrowsAsync(() => base.Custom_naming_projection_everything(async));

    public override async Task Project_entity_with_single_owned(bool async)
    {
        await base.Project_entity_with_single_owned(async);

        AssertMql(
            """
JsonEntitiesSingleOwned.
""");
    }

#if EF10 || EF11
    public override async Task LeftJoin_json_entities(bool async)
    {
        await base.LeftJoin_json_entities(async);

        AssertMql(
            """
JsonEntitiesSingleOwned.{ "$lookup" : { "from" : "JsonEntitiesBasic", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_JsonEntityBasic" } }, { "$set" : { "_lookup_JsonEntityBasic" : { "$arrayElemAt" : ["$_lookup_JsonEntityBasic", 0] } } }, { "$project" : { "e1" : "$$ROOT", "_lookup_JsonEntityBasic" : "$_lookup_JsonEntityBasic", "_id" : 0 } }
""");
    }
#endif

#if EF10 || EF11
    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task RightJoin_json_entities(bool async)
        => SpecFails.ThrowsAsync(() => base.RightJoin_json_entities(async), "could not be translated");
#endif

    // Fails: NullReferenceException while materializing EF-X529
    public override Task Left_join_json_entities_complex_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Left_join_json_entities_complex_projection(async));

    public override async Task Left_join_json_entities_json_being_inner(bool async)
    {
        await base.Left_join_json_entities_json_being_inner(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$lookup" : { "from" : "JsonEntitiesSingleOwned", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_JsonEntitySingleOwned" } }, { "$set" : { "_lookup_JsonEntitySingleOwned" : { "$arrayElemAt" : ["$_lookup_JsonEntitySingleOwned", 0] } } }, { "$project" : { "e1" : "$$ROOT", "_lookup_JsonEntitySingleOwned" : "$_lookup_JsonEntitySingleOwned", "_id" : 0 } }
""");
    }

    // Fails: NullReferenceException while materializing EF-X529
    public override Task Left_join_json_entities_complex_projection_json_being_inner(bool async)
        => SpecFails.ThrowsAsync(() => base.Left_join_json_entities_complex_projection_json_being_inner(async));

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Project_json_entity_FirstOrDefault_subquery_with_binding_on_top(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_FirstOrDefault_subquery_with_binding_on_top(async), "could not be translated");

    public override async Task Project_json_entity_FirstOrDefault_subquery_with_entity_comparison_on_top(bool async)
    {
        await base.Project_json_entity_FirstOrDefault_subquery_with_entity_comparison_on_top(async);

        AssertMql();
    }

    public override async Task Json_entity_with_inheritance_basic_projection(bool async)
    {
        await base.Json_entity_with_inheritance_basic_projection(async);

        AssertMql(
            """
JsonEntitiesInheritance.
""");
    }

    public override async Task Json_entity_with_inheritance_project_derived(bool async)
    {
        await base.Json_entity_with_inheritance_project_derived(async);

        AssertMql(
            """
JsonEntitiesInheritance.{ "$match" : { "_t" : "JsonEntityInheritanceDerived" } }
""");
    }

    public override async Task Json_entity_with_inheritance_project_navigations(bool async)
    {
        await base.Json_entity_with_inheritance_project_navigations(async);

        AssertMql(
            """
JsonEntitiesInheritance.{ "$project" : { "Id" : "$_id", "ReferenceOnBase" : "$ReferenceOnBase", "CollectionOnBase" : "$CollectionOnBase", "_id" : "$_id" } }
""");
    }

    public override async Task Json_entity_with_inheritance_project_navigations_on_derived(bool async)
    {
        await base.Json_entity_with_inheritance_project_navigations_on_derived(async);

        AssertMql(
            """
JsonEntitiesInheritance.{ "$match" : { "_t" : "JsonEntityInheritanceDerived" } }
""");
    }

    public override async Task Json_entity_backtracking(bool async)
    {
        await base.Json_entity_backtracking(async);

        AssertMql();
    }

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_basic(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_basic(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_ElementAt_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAt_in_projection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_ElementAtOrDefault_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAtOrDefault_in_projection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_project_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_project_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_ElementAt_project_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAt_project_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_ElementAtOrDefault_project_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAtOrDefault_project_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_using_parameter(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_parameter(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_using_column(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_column(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_using_untranslatable_client_method(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_untranslatable_client_method(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_using_untranslatable_client_method2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_untranslatable_client_method2(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_outside_bounds(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_outside_bounds(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_outside_bounds2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_outside_bounds2(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_outside_bounds_with_property_access(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_outside_bounds_with_property_access(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_nested(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_nested(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_nested_project_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_nested_project_scalar(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_nested_project_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_nested_project_reference(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_nested_project_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_nested_project_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_nested_project_collection_anonymous_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_nested_project_collection_anonymous_projection(async), "could not be translated");

    public override async Task Json_collection_index_in_predicate_using_constant(bool async)
    {
        await base.Json_collection_index_in_predicate_using_constant(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$ne" : [{ "$arrayElemAt" : ["$OwnedCollectionRoot.Name", 0] }, "Foo"] } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }

    public override async Task Json_collection_index_in_predicate_using_variable(bool async)
    {
        await base.Json_collection_index_in_predicate_using_variable(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$ne" : [{ "$arrayElemAt" : ["$OwnedCollectionRoot.Name", 1] }, "Foo"] } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }

    public override async Task Json_collection_index_in_predicate_using_column(bool async)
    {
        await base.Json_collection_index_in_predicate_using_column(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : ["$OwnedCollectionRoot.Name", "$_id"] }, "e1_c2"] } } }
""");
    }

    public override async Task Json_collection_index_in_predicate_using_complex_expression1(bool async)
    {
        await base.Json_collection_index_in_predicate_using_complex_expression1(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : ["$OwnedCollectionRoot.Name", { "$cond" : { "if" : { "$eq" : ["$_id", 1] }, "then" : 0, "else" : 1 } }] }, "e1_c1"] } } }
""");
    }

    // Fails: expression not supported by the driver LINQ translator EF-X527
    public override Task Json_collection_index_in_predicate_using_complex_expression2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_predicate_using_complex_expression2(async));

    public override async Task Json_collection_ElementAt_in_predicate(bool async)
    {
        await base.Json_collection_ElementAt_in_predicate(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$ne" : [{ "$arrayElemAt" : ["$OwnedCollectionRoot.Name", 1] }, "Foo"] } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }

    public override async Task Json_collection_index_in_predicate_nested_mix(bool async)
    {
        await base.Json_collection_index_in_predicate_nested_mix(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [{ "$let" : { "vars" : { "this" : { "$let" : { "vars" : { "this" : { "$arrayElemAt" : [{ "$let" : { "vars" : { "this" : { "$arrayElemAt" : ["$OwnedCollectionRoot", 1] } }, "in" : "$$this.OwnedCollectionBranch" } }, 0] } }, "in" : "$$this.OwnedCollectionLeaf" } } }, "in" : "$$this.SomethingSomething" } }, { "$subtract" : ["$_id", 1] }] }, "e1_c2_c1_c1"] } } }
""");
    }

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_ElementAt_and_pushdown(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_ElementAt_and_pushdown(async), "could not be translated");

    public override async Task Json_collection_Any_with_predicate(bool async)
    {
        await base.Json_collection_Any_with_predicate(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "OwnedReferenceRoot.OwnedCollectionBranch" : { "$elemMatch" : { "OwnedReferenceLeaf.SomethingSomething" : "e1_r_c1_r" } } } }
""");
    }

    public override async Task Json_collection_Where_ElementAt(bool async)
    {
        await base.Json_collection_Where_ElementAt(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [{ "$map" : { "input" : { "$filter" : { "input" : "$OwnedReferenceRoot.OwnedCollectionBranch", "as" : "o", "cond" : { "$eq" : ["$$o.Enum", -3] } } }, "as" : "o", "in" : "$$o.OwnedReferenceLeaf.SomethingSomething" } }, 0] }, "e1_r_c2_r"] } } }
""");
    }

    public override async Task Json_collection_Skip(bool async)
    {
        await base.Json_collection_Skip(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [{ "$map" : { "input" : { "$slice" : ["$OwnedReferenceRoot.OwnedCollectionBranch", 1, 2147483647] }, "as" : "o", "in" : "$$o.OwnedReferenceLeaf.SomethingSomething" } }, 0] }, "e1_r_c2_r"] } } }
""");
    }

    public override async Task Json_collection_OrderByDescending_Skip_ElementAt(bool async)
    {
        await base.Json_collection_OrderByDescending_Skip_ElementAt(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$eq" : [{ "$arrayElemAt" : [{ "$map" : { "input" : { "$slice" : [{ "$sortArray" : { "input" : "$OwnedReferenceRoot.OwnedCollectionBranch", "sortBy" : { "Date" : -1 } } }, 1, 2147483647] }, "as" : "o", "in" : "$$o.OwnedReferenceLeaf.SomethingSomething" } }, 0] }, "e1_r_c1_r"] } } }
""");
    }

    public override async Task Json_collection_Distinct_Count_with_predicate(bool async)
    {
        await base.Json_collection_Distinct_Count_with_predicate(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$eq" : [{ "$sum" : { "$map" : { "input" : { "$setUnion" : "$OwnedReferenceRoot.OwnedCollectionBranch" }, "as" : "e", "in" : { "$cond" : { "if" : { "$eq" : ["$$e.OwnedReferenceLeaf.SomethingSomething", "e1_r_c2_r"] }, "then" : 1, "else" : 0 } } } } }, 1] } } }
""");
    }

    public override async Task Json_collection_within_collection_Count(bool async)
    {
        await base.Json_collection_within_collection_Count(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "OwnedCollectionRoot" : { "$elemMatch" : { "OwnedCollectionBranch.1" : { "$exists" : true }, "OwnedCollectionBranch.2" : { "$exists" : false } } } } }
""");
    }

    public override async Task Json_collection_in_projection_with_composition_count(bool async)
    {
        await base.Json_collection_in_projection_with_composition_count(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$sort" : { "_id" : 1 } }, { "$project" : { "_v" : { "$size" : { "$ifNull" : ["$OwnedCollectionRoot", []] } }, "_id" : 0 } }
""");
    }

    // Fails: tracking query projects an owned entity without its owner EF-X517
    public override Task Json_collection_in_projection_with_anonymous_projection_of_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_in_projection_with_anonymous_projection_of_scalars(async));

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_in_projection_with_composition_where_and_anonymous_projection_of_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_in_projection_with_composition_where_and_anonymous_projection_of_scalars(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_in_projection_with_composition_where_and_anonymous_projection_of_primitive_arrays(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_in_projection_with_composition_where_and_anonymous_projection_of_primitive_arrays(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_filter_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_filter_in_projection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_nested_collection_filter_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_nested_collection_filter_in_projection(async), "could not be translated");

    // Fails: InvalidOperationException while translating or materializing the query EF-X526
    public override Task Json_nested_collection_anonymous_projection_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_nested_collection_anonymous_projection_in_projection(async));

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_skip_take_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_skip_take_in_projection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_skip_take_in_projection_project_into_anonymous_type(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_skip_take_in_projection_project_into_anonymous_type(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_skip_take_in_projection_with_json_reference_access_as_final_operation(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_skip_take_in_projection_with_json_reference_access_as_final_operation(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_distinct_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_distinct_in_projection(async), "could not be translated");

    public override async Task Json_collection_anonymous_projection_distinct_in_projection(bool async)
    {
        await base.Json_collection_anonymous_projection_distinct_in_projection(async);

        AssertMql();
    }

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_leaf_filter_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_leaf_filter_in_projection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_multiple_collection_projections(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_multiple_collection_projections(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_branch_collection_distinct_and_other_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_branch_collection_distinct_and_other_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_leaf_collection_distinct_and_other_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_leaf_collection_distinct_and_other_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_of_primitives_SelectMany(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_of_primitives_SelectMany(async), "could not be translated");

    public override async Task Json_collection_of_primitives_index_used_in_predicate(bool async)
    {
        await base.Json_collection_of_primitives_index_used_in_predicate(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "OwnedReferenceRoot.Names.0" : "e1_r1" } }
""");
    }

    public override async Task Json_collection_of_primitives_index_used_in_projection(bool async)
    {
        await base.Json_collection_of_primitives_index_used_in_projection(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$sort" : { "_id" : 1 } }, { "$project" : { "_v" : { "$arrayElemAt" : ["$OwnedReferenceRoot.OwnedReferenceBranch.Enums", 0] }, "_id" : 0 } }
""");
    }

    public override async Task Json_collection_of_primitives_index_used_in_orderby(bool async)
    {
        await base.Json_collection_of_primitives_index_used_in_orderby(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$project" : { "_id" : 0, "_document" : "$$ROOT", "_key1" : { "$arrayElemAt" : ["$OwnedReferenceRoot.Numbers", 0] } } }, { "$sort" : { "_key1" : 1 } }, { "$replaceRoot" : { "newRoot" : "$_document" } }
""");
    }

    public override async Task Json_collection_of_primitives_contains_in_predicate(bool async)
    {
        await base.Json_collection_of_primitives_contains_in_predicate(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "OwnedReferenceRoot.Names" : "e1_r1" } }
""");
    }

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_with_parameter_Select_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_with_parameter_Select_ElementAt(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_with_expression_Select_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_with_expression_Select_ElementAt(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_Select_entity_collection_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Select_entity_collection_ElementAt(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_Select_entity_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Select_entity_ElementAt(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_Select_entity_in_anonymous_object_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Select_entity_in_anonymous_object_ElementAt(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_Select_entity_with_initializer_ElementAt(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_Select_entity_with_initializer_ElementAt(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_deduplication_with_collection_indexer_in_original(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_deduplication_with_collection_indexer_in_original(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_deduplication_with_collection_indexer_in_target(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_deduplication_with_collection_indexer_in_target(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_deduplication_with_collection_in_original_and_collection_indexer_in_target(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_deduplication_with_collection_in_original_and_collection_indexer_in_target(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_using_constant_when_owner_is_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_constant_when_owner_is_present(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_using_constant_when_owner_is_not_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_constant_when_owner_is_not_present(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_using_parameter_when_owner_is_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_parameter_when_owner_is_present(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_using_parameter_when_owner_is_not_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_using_parameter_when_owner_is_not_present(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_after_collection_index_in_projection_using_constant_when_owner_is_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_after_collection_index_in_projection_using_constant_when_owner_is_present(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_after_collection_index_in_projection_using_constant_when_owner_is_not_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_after_collection_index_in_projection_using_constant_when_owner_is_not_present(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_after_collection_index_in_projection_using_parameter_when_owner_is_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_after_collection_index_in_projection_using_parameter_when_owner_is_present(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_after_collection_index_in_projection_using_parameter_when_owner_is_not_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_after_collection_index_in_projection_using_parameter_when_owner_is_not_present(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_when_owner_is_present_misc1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_present_misc1(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_when_owner_is_not_present_misc1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_not_present_misc1(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_when_owner_is_present_misc2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_present_misc2(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_when_owner_is_not_present_misc2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_not_present_misc2(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_when_owner_is_present_multiple(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_present_multiple(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_collection_index_in_projection_when_owner_is_not_present_multiple(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_collection_index_in_projection_when_owner_is_not_present_multiple(async), "could not be translated");

    public override async Task Json_scalar_required_null_semantics(bool async)
    {
        await base.Json_scalar_required_null_semantics(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$ne" : ["$OwnedReferenceRoot.Number", { "$cond" : { "if" : { "$eq" : [{ "$ifNull" : ["$OwnedReferenceRoot.Name", null] }, null] }, "then" : null, "else" : { "$strLenCP" : "$OwnedReferenceRoot.Name" } } }] } } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }
""");
    }

    public override async Task Json_scalar_optional_null_semantics(bool async)
    {
        await base.Json_scalar_optional_null_semantics(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$match" : { "$expr" : { "$ne" : ["$OwnedReferenceRoot.Name", "$OwnedReferenceRoot.OwnedReferenceBranch.OwnedReferenceLeaf.SomethingSomething"] } } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }
""");
    }

    public override async Task Group_by_on_json_scalar(bool async)
    {
        await base.Group_by_on_json_scalar(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$group" : { "_id" : "$OwnedReferenceRoot.Name", "Count" : { "$sum" : 1 } } }, { "$project" : { "Key" : "$_id", "Count" : "$Count", "_id" : 0 } }
""");
    }

    public override async Task Group_by_on_json_scalar_using_collection_indexer(bool async)
    {
        await base.Group_by_on_json_scalar_using_collection_indexer(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$group" : { "_id" : { "$arrayElemAt" : ["$OwnedCollectionRoot.Name", 0] }, "__agg0" : { "$sum" : 1 } } }, { "$project" : { "Key" : "$_id", "Count" : "$__agg0", "_id" : 0 } }
""");
    }

    // Fails: InvalidOperationException while translating or materializing the query EF-X526
    public override Task Group_by_First_on_json_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Group_by_First_on_json_scalar(async));

    // Fails: InvalidOperationException while translating or materializing the query EF-X526
    public override Task Group_by_FirstOrDefault_on_json_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Group_by_FirstOrDefault_on_json_scalar(async));

    // Fails: InvalidOperationException while translating or materializing the query EF-X526
    public override Task Group_by_Skip_Take_on_json_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Group_by_Skip_Take_on_json_scalar(async));

    public override async Task Group_by_json_scalar_Orderby_json_scalar_FirstOrDefault(bool async)
    {
        await base.Group_by_json_scalar_Orderby_json_scalar_FirstOrDefault(async);

        AssertMql();
    }

    // Fails: InvalidOperationException while translating or materializing the query EF-X526
    public override Task Group_by_json_scalar_Skip_First_project_json_scalar(bool async)
        => SpecFails.ThrowsAsync(() => base.Group_by_json_scalar_Skip_First_project_json_scalar(async));

    public override async Task Json_with_include_on_json_entity(bool async)
    {
        await base.Json_with_include_on_json_entity(async);

        AssertMql(
            """
JsonEntitiesBasic.
""");
    }

    public override async Task Json_with_include_on_entity_reference(bool async)
    {
        await base.Json_with_include_on_entity_reference(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$lookup" : { "from" : "JsonEntitiesBasicForReference", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_EntityReference" } }, { "$unwind" : { "path" : "$_lookup_EntityReference", "preserveNullAndEmptyArrays" : true } }
""");
    }

    public override async Task Json_with_include_on_entity_collection(bool async)
    {
        await base.Json_with_include_on_entity_collection(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$lookup" : { "from" : "JsonEntitiesBasicForCollection", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_JsonEntityBasicForCollection_ParentId__id" } }, { "$lookup" : { "from" : "JsonEntitiesBasicForCollection", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_EntityCollection" } }
""");
    }

    // Fails: TargetInvocationException while translating or materializing the query EF-X526
    public override Task Entity_including_collection_with_json(bool async)
        => SpecFails.ThrowsAsync(() => base.Entity_including_collection_with_json(async));

    public override async Task Json_with_include_on_entity_collection_and_reference(bool async)
    {
        await base.Json_with_include_on_entity_collection_and_reference(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$lookup" : { "from" : "JsonEntitiesBasicForReference", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_EntityReference" } }, { "$unwind" : { "path" : "$_lookup_EntityReference", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "JsonEntitiesBasicForCollection", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_EntityCollection" } }
""");
    }

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_with_projection_of_json_reference_leaf_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_json_reference_leaf_and_entity_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_with_projection_of_json_reference_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_json_reference_and_entity_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_with_projection_of_multiple_json_references_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_multiple_json_references_and_entity_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_with_projection_of_json_collection_leaf_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_json_collection_leaf_and_entity_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_with_projection_of_json_collection_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_json_collection_and_entity_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_with_projection_of_json_collection_element_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_json_collection_element_and_entity_collection(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_with_projection_of_mix_of_json_collections_json_references_and_entity_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_with_projection_of_mix_of_json_collections_json_references_and_entity_collection(async), "could not be translated");

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_all_types_entity_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_all_types_entity_projection(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_all_types_projection_from_owned_entity_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_all_types_projection_from_owned_entity_reference(async));

    public override async Task Json_all_types_projection_individual_properties(bool async)
    {
        await base.Json_all_types_projection_individual_properties(async);

        AssertMql(
            """
JsonEntitiesAllTypes.{ "$project" : { "TestDefaultString" : "$Reference.TestDefaultString", "TestMaxLengthString" : "$Reference.TestMaxLengthString", "TestBoolean" : "$Reference.TestBoolean", "TestByte" : "$Reference.TestByte", "TestCharacter" : "$Reference.TestCharacter", "TestDateTime" : "$Reference.TestDateTime", "TestDateTimeOffset" : "$Reference.TestDateTimeOffset", "TestDecimal" : "$Reference.TestDecimal", "TestDouble" : "$Reference.TestDouble", "TestGuid" : "$Reference.TestGuid", "TestInt16" : "$Reference.TestInt16", "TestInt32" : "$Reference.TestInt32", "TestInt64" : "$Reference.TestInt64", "TestSignedByte" : "$Reference.TestSignedByte", "TestSingle" : "$Reference.TestSingle", "TestTimeSpan" : "$Reference.TestTimeSpan", "TestDateOnly" : "$Reference.TestDateOnly", "TestTimeOnly" : "$Reference.TestTimeOnly", "TestUnsignedInt16" : "$Reference.TestUnsignedInt16", "TestUnsignedInt32" : "$Reference.TestUnsignedInt32", "TestUnsignedInt64" : "$Reference.TestUnsignedInt64", "TestEnum" : "$Reference.TestEnum", "TestEnumWithIntConverter" : "$Reference.TestEnumWithIntConverter", "TestNullableEnum" : "$Reference.TestNullableEnum", "TestNullableEnumWithIntConverter" : "$Reference.TestNullableEnumWithIntConverter", "TestNullableEnumWithConverterThatHandlesNulls" : "$Reference.TestNullableEnumWithConverterThatHandlesNulls", "_id" : 0 } }
""");
    }

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_boolean_predicate(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_boolean_predicate(async));

    public override async Task Json_boolean_predicate_negated(bool async)
    {
        await base.Json_boolean_predicate_negated(async);

        AssertMql(
            """
JsonEntitiesAllTypes.{ "$match" : { "Reference.TestBoolean" : { "$ne" : true } } }
""");
    }

    public override async Task Json_boolean_projection(bool async)
    {
        await base.Json_boolean_projection(async);

        AssertMql(
            """
JsonEntitiesAllTypes.{ "$project" : { "_v" : "$Reference.TestBoolean", "_id" : 0 } }
""");
    }

    public override async Task Json_boolean_projection_negated(bool async)
    {
        await base.Json_boolean_projection_negated(async);

        AssertMql(
            """
JsonEntitiesAllTypes.{ "$project" : { "_v" : { "$not" : ["$Reference.TestBoolean"] }, "_id" : 0 } }
""");
    }

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_default_string(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_default_string(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_max_length_string(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_max_length_string(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_string_condition(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_string_condition(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_byte(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_byte(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_byte_array(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_byte_array(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_character(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_character(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_datetime(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_datetime(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_datetimeoffset(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_datetimeoffset(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_decimal(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_decimal(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_double(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_double(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_guid(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_guid(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_int16(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_int16(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_int32(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_int32(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_int64(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_int64(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_signedbyte(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_signedbyte(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_single(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_single(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_timespan(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_timespan(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_dateonly(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_dateonly(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_timeonly(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_timeonly(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_unisgnedint16(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_unisgnedint16(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_unsignedint32(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_unsignedint32(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_unsignedint64(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_unsignedint64(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_enum(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_enum(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_enumwithintconverter(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_enumwithintconverter(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_nullableenum1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableenum1(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_nullableenum2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableenum2(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_nullableenumwithconverterthathandlesnulls1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableenumwithconverterthathandlesnulls1(async));

    public override async Task Json_predicate_on_nullableenumwithconverterthathandlesnulls2(bool async)
    {
        await base.Json_predicate_on_nullableenumwithconverterthathandlesnulls2(async);

        AssertMql();
    }

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_nullableenumwithconverter1(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableenumwithconverter1(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_nullableenumwithconverter2(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableenumwithconverter2(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_nullableint321(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableint321(async));

    // Fails: uint values above Int32.MaxValue do not round-trip: the seed clamps them to Int32.MaxValue, so the stored data differs from the expected data EF-X548
    public override Task Json_predicate_on_nullableint322(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_predicate_on_nullableint322(async));

    public override async Task Json_predicate_on_bool_converted_to_int_zero_one(bool async)
    {
        await base.Json_predicate_on_bool_converted_to_int_zero_one(async);

        AssertMql(
            """
JsonEntitiesConverters.{ "$match" : { "Reference.BoolConvertedToIntZeroOne" : 1 } }
""");
    }

    public override async Task Json_predicate_on_bool_converted_to_int_zero_one_with_explicit_comparison(bool async)
    {
        await base.Json_predicate_on_bool_converted_to_int_zero_one_with_explicit_comparison(async);

        AssertMql(
            """
JsonEntitiesConverters.{ "$match" : { "Reference.BoolConvertedToIntZeroOne" : 0 } }
""");
    }

    public override async Task Json_predicate_on_bool_converted_to_string_True_False(bool async)
    {
        await base.Json_predicate_on_bool_converted_to_string_True_False(async);

        AssertMql(
            """
JsonEntitiesConverters.{ "$match" : { "Reference.BoolConvertedToStringTrueFalse" : "True" } }
""");
    }

    public override async Task Json_predicate_on_bool_converted_to_string_True_False_with_explicit_comparison(bool async)
    {
        await base.Json_predicate_on_bool_converted_to_string_True_False_with_explicit_comparison(async);

        AssertMql(
            """
JsonEntitiesConverters.{ "$match" : { "Reference.BoolConvertedToStringTrueFalse" : "True" } }
""");
    }

    public override async Task Json_predicate_on_bool_converted_to_string_Y_N(bool async)
    {
        await base.Json_predicate_on_bool_converted_to_string_Y_N(async);

        AssertMql(
            """
JsonEntitiesConverters.{ "$match" : { "Reference.BoolConvertedToStringYN" : "Y" } }
""");
    }

    public override async Task Json_predicate_on_bool_converted_to_string_Y_N_with_explicit_comparison(bool async)
    {
        await base.Json_predicate_on_bool_converted_to_string_Y_N_with_explicit_comparison(async);

        AssertMql(
            """
JsonEntitiesConverters.{ "$match" : { "Reference.BoolConvertedToStringYN" : "N" } }
""");
    }

    public override async Task Json_predicate_on_int_zero_one_converted_to_bool(bool async)
    {
        await base.Json_predicate_on_int_zero_one_converted_to_bool(async);

        AssertMql(
            """
JsonEntitiesConverters.{ "$match" : { "Reference.IntZeroOneConvertedToBool" : true } }
""");
    }

    public override async Task Json_predicate_on_string_True_False_converted_to_bool(bool async)
    {
        await base.Json_predicate_on_string_True_False_converted_to_bool(async);

        AssertMql(
            """
JsonEntitiesConverters.{ "$match" : { "Reference.StringTrueFalseConvertedToBool" : false } }
""");
    }

    public override async Task Json_predicate_on_string_Y_N_converted_to_bool(bool async)
    {
        await base.Json_predicate_on_string_Y_N_converted_to_bool(async);

        AssertMql(
            """
JsonEntitiesConverters.{ "$match" : { "Reference.StringYNConvertedToBool" : false } }
""");
    }

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_collection_element_and_reference_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_collection_element_and_reference_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    public override async Task Json_projection_nothing_interesting_AsNoTrackingWithIdentityResolution(bool async)
    {
        await base.Json_projection_nothing_interesting_AsNoTrackingWithIdentityResolution(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$project" : { "Id" : "$_id", "Name" : "$Name", "_id" : 0 } }
""");
    }

    public override async Task Json_projection_owner_entity_AsNoTrackingWithIdentityResolution(bool async)
    {
        await base.Json_projection_owner_entity_AsNoTrackingWithIdentityResolution(async);

        AssertMql(
            """
JsonEntitiesBasic.{ "$project" : { "Id" : "$_id", "x" : "$$ROOT", "_id" : 0 } }
""");
    }

    // Fails: InvalidOperationException while translating or materializing the query EF-X526
    public override Task Json_nested_collection_anonymous_projection_of_primitives_in_projection_NoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_nested_collection_anonymous_projection_of_primitives_in_projection_NoTrackingWithIdentityResolution(async));

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_second_element_through_collection_element_constant_projected_after_owner_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_second_element_through_collection_element_constant_projected_after_owner_nested_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_reference_collection_and_collection_element_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_reference_collection_and_collection_element_nested_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_second_element_through_collection_element_parameter_correctly_projected_after_owner_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_second_element_through_collection_element_parameter_correctly_projected_after_owner_nested_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_only_second_element_through_collection_element_constant_projected_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_only_second_element_through_collection_element_constant_projected_nested_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_only_second_element_through_collection_element_parameter_projected_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_only_second_element_through_collection_element_parameter_projected_nested_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_second_element_through_collection_element_constant_different_values_projected_before_owner_nested_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_second_element_through_collection_element_constant_different_values_projected_before_owner_nested_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_nested_collection_and_element_correct_order_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_nested_collection_and_element_correct_order_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_nested_collection_element_using_parameter_and_the_owner_in_correct_order_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_nested_collection_element_using_parameter_and_the_owner_in_correct_order_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_second_element_projected_before_owner_as_well_as_root_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_second_element_projected_before_owner_as_well_as_root_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: a JSON-owned collection that is indexed (ElementAt/indexer), projected or composed in a subquery is not translated EF-X549
    public override Task Json_projection_second_element_projected_before_owner_nested_as_well_as_root_AsNoTrackingWithIdentityResolution(bool async)
        => SpecFails.ThrowsAsync(() => base.Json_projection_second_element_projected_before_owner_nested_as_well_as_root_AsNoTrackingWithIdentityResolution(async), "could not be translated");

    // Fails: tracking query projects an owned entity without its owner EF-X517
    public override Task Project_json_reference_in_tracking_query_fails(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_reference_in_tracking_query_fails(async));

    // Fails: tracking query projects an owned entity without its owner EF-X517
    public override Task Project_json_collection_in_tracking_query_fails(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_collection_in_tracking_query_fails(async));

    // Fails: NullReferenceException while materializing EF-X529
    public override Task Project_json_entity_in_tracking_query_fails_even_when_owner_is_present(bool async)
        => SpecFails.ThrowsAsync(() => base.Project_json_entity_in_tracking_query_fails_even_when_owner_is_present(async));

#if EF11
    // The seeds of the ad hoc JSON tests (merged into this base on EF11) insert raw relational JSON column values, which has no MongoDB
    // equivalent: they are not implemented, and the fixture failure is what every test asserts anyway.
    protected override Task Seed30028(DbContext ctx)
        => Task.CompletedTask;

    protected override Task Seed33046(DbContext ctx)
        => Task.CompletedTask;

    protected override Task SeedBadJsonProperties(ContextBadJsonProperties ctx)
        => Task.CompletedTask;

    protected override Task SeedJunkInJson(DbContext ctx)
        => Task.CompletedTask;

    protected override Task SeedNotICollection(DbContext ctx)
        => Task.CompletedTask;

    protected override Task SeedShadowProperties(DbContext ctx)
        => Task.CompletedTask;

    protected override Task SeedTrickyBuffering(DbContext ctx)
        => Task.CompletedTask;

#endif
    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    public class JsonQueryMongoFixture : JsonQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("JsonQuery");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // EF-X300: an inline HasConversion on a nullable property fails at write time, so this one property is stored without its converter.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(e => e.FindProperty("TestNullableEnumWithConverterThatHandlesNulls") != null).ToList())
            {
                ((IMutableEntityType)entityType).FindProperty("TestNullableEnumWithConverterThatHandlesNulls")!.SetValueConverter((ValueConverter?)null);
            }
        }

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder).AddInterceptors(new ClampUIntInterceptor());

        // EF-X548: uint and ulong values above Int32.MaxValue / Int64.MaxValue overflow on write, so the seed clamps them to keep the rest of the suite running.
        private sealed class ClampUIntInterceptor : SaveChangesInterceptor
        {
            public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            {
                Clamp(eventData.Context!);
                return result;
            }

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                Clamp(eventData.Context!);
                return new(result);
            }

            private static void Clamp(DbContext context)
            {
                foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
                {
                    foreach (var property in entry.Properties)
                    {
                        // Test the declared type: array covariance makes an int[] or an enum[] match uint[] at run time.
                        if (property.CurrentValue is not System.Collections.IList { IsReadOnly: false } values)
                        {
                            continue;
                        }

                        var clrType = property.Metadata.ClrType;
                        var elementType = clrType.IsArray ? clrType.GetElementType() : clrType.GenericTypeArguments.FirstOrDefault();
                        elementType = elementType == null ? null : Nullable.GetUnderlyingType(elementType) ?? elementType;
                        if (elementType != typeof(uint) && elementType != typeof(ulong))
                        {
                            continue;
                        }

                        for (var i = 0; i < values.Count; i++)
                        {
                            values[i] = values[i] switch
                            {
                                uint u => Math.Min(u, (uint)int.MaxValue),
                                ulong ul => Math.Min(ul, (ulong)long.MaxValue),
                                _ => values[i]
                            };
                        }
                    }
                }
            }
        }

    }
}

#endif
