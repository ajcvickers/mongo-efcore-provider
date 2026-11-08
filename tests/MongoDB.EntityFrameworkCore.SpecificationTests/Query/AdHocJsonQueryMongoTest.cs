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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

#if EF9 || EF10 // On EF11 the AdHocJsonQueryTestBase tests are part of JsonQueryTestBase<> (see JsonQueryMongoTest).

public class AdHocJsonQueryMongoTest : AdHocJsonQueryTestBase
{
#if EF10 || EF11
    public AdHocJsonQueryMongoTest(NonSharedFixture fixture)
        : base(fixture)
    {
        // The class-level fixture would reuse the first test's (already seeded) test store for every test;
        // give each test its own uniquely named database instead.
#if EF11
        NonSharedFixture = null;
#else
        Fixture = null;
#endif
    }

#endif
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if EF10 || EF11
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

    public override Task Contains_on_nested_collection_with_init_only_navigation()
        => base.Contains_on_nested_collection_with_init_only_navigation();

    public override Task Project_json_with_no_properties()
        => base.Project_json_with_no_properties();

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Query_with_nested_json_collection_mapped_to_private_field_via_IReadOnlyList()
        => SpecFails.ThrowsAsync(() => base.Query_with_nested_json_collection_mapped_to_private_field_via_IReadOnlyList());

    public override Task Project_entity_with_json_null_values()
        => base.Project_entity_with_json_null_values();

    public override Task Try_project_collection_but_JSON_is_entity()
        => base.Try_project_collection_but_JSON_is_entity();

    public override Task Try_project_reference_but_JSON_is_collection()
        => base.Try_project_reference_but_JSON_is_collection();

    public override Task Project_json_array_of_primitives_on_reference()
        => base.Project_json_array_of_primitives_on_reference();

    public override Task Project_json_array_of_primitives_on_collection()
        => base.Project_json_array_of_primitives_on_collection();

    public override Task Project_element_of_json_array_of_primitives()
        => base.Project_element_of_json_array_of_primitives();

    public override Task Predicate_based_on_element_of_json_array_of_primitives1()
        => base.Predicate_based_on_element_of_json_array_of_primitives1();

    public override Task Predicate_based_on_element_of_json_array_of_primitives2()
        => base.Predicate_based_on_element_of_json_array_of_primitives2();

    public override Task Predicate_based_on_element_of_json_array_of_primitives3()
        => base.Predicate_based_on_element_of_json_array_of_primitives3();

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

    public override Task Project_proxies_entity_with_json()
        => base.Project_proxies_entity_with_json();

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Not_ICollection_basic_projection()
        => SpecFails.ThrowsAsync(() => base.Not_ICollection_basic_projection());

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_duplicated_navigations(bool async)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_duplicated_navigations(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_duplicated_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_duplicated_scalars(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_empty_navigations(bool async)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_empty_navigations(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_empty_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_empty_scalars(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_null_navigations(bool async)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_null_navigations(async));

    // Fails: Ad hoc JSON data for MongoDB not ported (the base seeds raw relational JSON column values), so the query finds no or incomplete data EF-X900
    public override Task Bad_json_properties_null_scalars(bool async)
        => SpecFails.ThrowsAsync(() => base.Bad_json_properties_null_scalars(async));
#endif

    public override async Task Project_root_with_missing_scalars(bool async)
    {
        // Fails: Missing property values issue EF-164
        Assert.Contains(
            "Document element is missing for required non-nullable property 'Number'",
            (await Assert.ThrowsAsync<InvalidOperationException>(() =>
                base.Project_root_with_missing_scalars(async)))
            .Message);

        AssertMql(
            """
            Entities.{ "$match" : { "_id" : { "$lt" : 4 } } }
            """);
    }

    public override async Task Project_top_level_json_entity_with_missing_scalars(bool async)
    {
        // Fails: No support for nested JSON EF-X008
        Assert.Contains(
            "Document element is missing for required",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Project_top_level_json_entity_with_missing_scalars(async)))
            .Message);

        AssertMql(
            """
            Entities.{ "$match" : { "_id" : { "$lt" : 4 } } }, { "$project" : { "Id" : "$_id", "OptionalReference" : "$OptionalReference", "RequiredReference" : "$RequiredReference", "Collection" : "$Collection", "_id" : "$_id" } }
            """);
    }

    public override async Task Project_nested_json_entity_with_missing_scalars(bool async)
    {
        // Fails: No support for nested JSON EF-X008. Declined cleanly by the assignability guard in
        // MongoProjectionBindingExpressionVisitor.VisitMethodCall.
        Assert.Contains(
            CoreStrings.TranslationFailed("")[48..],
            (await Assert.ThrowsAsync<InvalidOperationException>(() =>
                base.Project_nested_json_entity_with_missing_scalars(async)))
            .Message);

        AssertMql();
    }

    public override async Task Project_top_level_entity_with_null_value_required_scalars(bool async)
    {
        await base.Project_top_level_entity_with_null_value_required_scalars(async);

        AssertMql(
            """
            Entities.{ "$match" : { "_id" : 4 } }, { "$project" : { "Id" : "$_id", "RequiredReference" : "$RequiredReference", "_id" : "$_id" } }
            """);
    }

    public override async Task Project_root_entity_with_missing_required_navigation(bool async)
    {
        // Fails: Missing property values issue EF-164
        // The document lacks two required references (the root's, and one inside an element of its owned collection). Which is reported first
        // depends on whether the collection's elements are built before the root's own references are checked, which is not part of the contract.
        var message = (await Assert.ThrowsAsync<InvalidOperationException>(() =>
            base.Project_root_entity_with_missing_required_navigation(async))).Message;
        Assert.Matches("Field '(Nested)?RequiredReference' required but not present", message);

        AssertMql(
            """
            Entities.{ "$match" : { "_id" : 5 } }
            """);
    }

    public override async Task Project_missing_required_navigation(bool async)
    {
        // Fails: Entity id=5 has no RequiredReference field EF-X005
        // A bare owned-reference leaf is not a native projection (main threw here too): NativeOnly refuses it.
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            await MongoSpecTestHelpers.AssertNativeTranslationFailedAsync(() => base.Project_missing_required_navigation(async));
            AssertMql();
            return;
        }

        Assert.Contains(
            "Field 'RequiredReference' required but not present",
            (await Assert.ThrowsAsync<InvalidOperationException>(() =>
                base.Project_missing_required_navigation(async)))
            .Message);

        AssertMql(
            """
            Entities.{ "$match" : { "_id" : 5 } }
            """);
    }

    public override async Task Project_root_entity_with_null_required_navigation(bool async)
    {
        // Fails: Projections issue EF-164
        Assert.Contains(
            "Field 'NestedRequiredReference' required but not present",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Project_root_entity_with_null_required_navigation(async)))
            .Message);

        AssertMql(
            """
            Entities.{ "$match" : { "_id" : 6 } }
            """);
    }

    public override async Task Project_null_required_navigation(bool async)
    {
        // Fails: NestedRequiredReference is null in BsonDocument for entity id=6 EF-X005
        // A bare owned-reference leaf is not a native projection (main threw here too): NativeOnly refuses it.
        if (MongoSpecTestHelpers.IsNativeOnly)
        {
            await MongoSpecTestHelpers.AssertNativeTranslationFailedAsync(() => base.Project_null_required_navigation(async));
            AssertMql();
            return;
        }

        Assert.Contains(
            "Field 'NestedRequiredReference' required but not present",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Project_null_required_navigation(async)))
            .Message);

        AssertMql(
            """
            Entities.{ "$match" : { "_id" : 6 } }
            """);
    }

    public override async Task Project_missing_required_scalar(bool async)
    {
        await base.Project_missing_required_scalar(async);

        AssertMql(
            """
            Entities.{ "$match" : { "_id" : 2 } }, { "$project" : { "Id" : "$_id", "Number" : "$RequiredReference.Number", "_id" : 0 } }
            """);
    }

    public override async Task Project_null_required_scalar(bool async)
    {
        await base.Project_null_required_scalar(async);

        AssertMql(
            """
            Entities.{ "$match" : { "_id" : 4 } }, { "$project" : { "Id" : "$_id", "Number" : "$RequiredReference.Number", "_id" : 0 } }
            """);
    }

    protected override void OnModelCreating21006(ModelBuilder modelBuilder)
    {
        base.OnModelCreating21006(modelBuilder);

        modelBuilder.Entity<Context21006.Entity>().ToCollection("Entities");
    }

    protected override async Task Seed21006(Context21006 context)
    {
        await base.Seed21006(context);

        var wrapper = (MongoClientWrapper)context.GetService<IMongoClientWrapper>();
        var collection = wrapper.GetCollection<BsonDocument>("Entities");

        var missingTopLevel = new BsonDocument
        {
            ["_id"] = 2,
            ["Name"] = "e2",
            ["Collection"] =
                new BsonArray
                {
                    new BsonDocument
                    {
                        ["Text"] = "e2 c1",
                        ["NestedCollection"] =
                            new BsonArray
                            {
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e2 c1 c1"
                                },
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e2 c1 c2"
                                }
                            },
                        ["NestedOptionalReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e2 c1 nor"
                            },
                        ["NestedRequiredReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e2 c1 nrr"
                            }
                    },
                    new BsonDocument
                    {
                        ["Text"] = "e2 c2",
                        ["NestedCollection"] =
                            new BsonArray
                            {
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e2 c2 c1"
                                },
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e2 c2 c2"
                                }
                            },
                        ["NestedOptionalReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e2 c2 nor"
                            },
                        ["NestedRequiredReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e2 c2 nrr"
                            }
                    }
                },
            ["OptionalReference"] = new BsonDocument
            {
                ["Text"] = "e2 or",
                ["NestedCollection"] =
                    new BsonArray
                    {
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e2 or c1"
                        },
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e2 or c2"
                        }
                    },
                ["NestedOptionalReference"] =
                    new BsonDocument
                    {
                        ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        ["Text"] = "e2 or nor"
                    },
                ["NestedRequiredReference"] =
                    new BsonDocument
                    {
                        ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        ["Text"] = "e2 or nrr"
                    }
            },
            ["RequiredReference"] = new BsonDocument
            {
                ["Text"] = "e2 rr",
                ["NestedCollection"] =
                    new BsonArray
                    {
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e2 rr c1"
                        },
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e2 rr c2"
                        }
                    },
                ["NestedOptionalReference"] =
                    new BsonDocument
                    {
                        ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        ["Text"] = "e2 rr nor"
                    },
                ["NestedRequiredReference"] = new BsonDocument
                {
                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ["Text"] = "e2 rr nrr"
                }
            }
        };

        var missingNested = new BsonDocument
        {
            ["_id"] = 3,
            ["Name"] = "e3",
            ["Collection"] =
                new BsonArray
                {
                    new BsonDocument
                    {
                        ["Number"] = 7.0,
                        ["Text"] = "e3 c1",
                        ["NestedCollection"] =
                            new BsonArray
                            {
                                new BsonDocument { ["Text"] = "e3 c1 c1" },
                                new BsonDocument { ["Text"] = "e3 c1 c2" }
                            },
                        ["NestedOptionalReference"] = new BsonDocument { ["Text"] = "e3 c1 nor" },
                        ["NestedRequiredReference"] = new BsonDocument { ["Text"] = "e3 c1 nrr" }
                    },
                    new BsonDocument
                    {
                        ["Number"] = 7.0,
                        ["Text"] = "e3 c2",
                        ["NestedCollection"] =
                            new BsonArray
                            {
                                new BsonDocument { ["Text"] = "e3 c2 c1" },
                                new BsonDocument { ["Text"] = "e3 c2 c2" }
                            },
                        ["NestedOptionalReference"] = new BsonDocument { ["Text"] = "e3 c2 nor" },
                        ["NestedRequiredReference"] = new BsonDocument { ["Text"] = "e3 c2 nrr" }
                    }
                },
            ["OptionalReference"] = new BsonDocument
            {
                ["Number"] = 7.0,
                ["Text"] = "e3 or",
                ["NestedCollection"] =
                    new BsonArray
                    {
                        new BsonDocument { ["Text"] = "e3 or c1" },
                        new BsonDocument { ["Text"] = "e3 or c2" }
                    },
                ["NestedOptionalReference"] = new BsonDocument { ["Text"] = "e3 or nor" },
                ["NestedRequiredReference"] = new BsonDocument { ["Text"] = "e3 or nrr" }
            },
            ["RequiredReference"] = new BsonDocument
            {
                ["Number"] = 7.0,
                ["Text"] = "e3 rr",
                ["NestedCollection"] =
                    new BsonArray
                    {
                        new BsonDocument { ["Text"] = "e3 rr c1" },
                        new BsonDocument { ["Text"] = "e3 rr c2" }
                    },
                ["NestedOptionalReference"] = new BsonDocument { ["Text"] = "e3 rr nor" },
                ["NestedRequiredReference"] = new BsonDocument { ["Text"] = "e3 rr nrr" }
            }
        };

        var nullTopLevel = new BsonDocument
        {
            ["_id"] = 4,
            ["Name"] = "e4",
            ["Collection"] =
                new BsonArray
                {
                    new BsonDocument
                    {
                        ["Number"] = BsonNull.Value,
                        ["Text"] = "e4 c1",
                        ["NestedCollection"] =
                            new BsonArray
                            {
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e4 c1 c1"
                                },
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e4 c1 c2"
                                }
                            },
                        ["NestedOptionalReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e4 c1 nor"
                            },
                        ["NestedRequiredReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e4 c1 nrr"
                            }
                    },
                    new BsonDocument
                    {
                        ["Number"] = BsonNull.Value,
                        ["Text"] = "e4 c2",
                        ["NestedCollection"] =
                            new BsonArray
                            {
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e4 c2 c1"
                                },
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e4 c2 c2"
                                }
                            },
                        ["NestedOptionalReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e4 c2 nor"
                            },
                        ["NestedRequiredReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e4 c2 nrr"
                            }
                    }
                },
            ["OptionalReference"] = new BsonDocument
            {
                ["Number"] = BsonNull.Value,
                ["Text"] = "e4 or",
                ["NestedCollection"] =
                    new BsonArray
                    {
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e4 or c1"
                        },
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e4 or c2"
                        }
                    },
                ["NestedOptionalReference"] =
                    new BsonDocument
                    {
                        ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        ["Text"] = "e4 or nor"
                    },
                ["NestedRequiredReference"] =
                    new BsonDocument
                    {
                        ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        ["Text"] = "e4 or nrr"
                    }
            },
            ["RequiredReference"] = new BsonDocument
            {
                ["Number"] = BsonNull.Value,
                ["Text"] = "e4 rr",
                ["NestedCollection"] =
                    new BsonArray
                    {
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e4 rr c1"
                        },
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e4 rr c2"
                        }
                    },
                ["NestedOptionalReference"] =
                    new BsonDocument
                    {
                        ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        ["Text"] = "e4 rr nor"
                    },
                ["NestedRequiredReference"] = new BsonDocument
                {
                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ["Text"] = "e4 rr nrr"
                }
            }
        };

        var missingRequiredNav = new BsonDocument
        {
            ["_id"] = 5,
            ["Name"] = "e5",
            ["Collection"] =
                new BsonArray
                {
                    new BsonDocument
                    {
                        ["Number"] = 7.0,
                        ["Text"] = "e5 c1",
                        ["NestedCollection"] =
                            new BsonArray
                            {
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e5 c1 c1"
                                },
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e5 c1 c2"
                                }
                            },
                        ["NestedOptionalReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e5 c1 nor"
                            }
                    },
                    new BsonDocument
                    {
                        ["Number"] = 7.0,
                        ["Text"] = "e5 c2",
                        ["NestedCollection"] =
                            new BsonArray
                            {
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e5 c2 c1"
                                },
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e5 c2 c2"
                                }
                            },
                        ["NestedOptionalReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e5 c2 nor"
                            }
                    }
                },
            ["OptionalReference"] = new BsonDocument
            {
                ["Number"] = 7.0,
                ["Text"] = "e5 or",
                ["NestedCollection"] =
                    new BsonArray
                    {
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e5 or c1"
                        },
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e5 or c2"
                        }
                    },
                ["NestedOptionalReference"] =
                    new BsonDocument
                    {
                        ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        ["Text"] = "e5 or nor"
                    }
            }
        };

        var nullRequiredNav = new BsonDocument
        {
            ["_id"] = 6,
            ["Name"] = "e6",
            ["Collection"] =
                new BsonArray
                {
                    new BsonDocument
                    {
                        ["Number"] = 7.0,
                        ["Text"] = "e6 c1",
                        ["NestedCollection"] =
                            new BsonArray
                            {
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e6 c1 c1"
                                },
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e6 c1 c2"
                                }
                            },
                        ["NestedOptionalReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e6 c1 nor"
                            },
                        ["NestedRequiredReference"] = BsonNull.Value
                    },
                    new BsonDocument
                    {
                        ["Number"] = 7.0,
                        ["Text"] = "e6 c2",
                        ["NestedCollection"] =
                            new BsonArray
                            {
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e6 c2 c1"
                                },
                                new BsonDocument
                                {
                                    ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    ["Text"] = "e6 c2 c2"
                                }
                            },
                        ["NestedOptionalReference"] =
                            new BsonDocument
                            {
                                ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                ["Text"] = "e6 c2 nor"
                            },
                        ["NestedRequiredReference"] = BsonNull.Value
                    }
                },
            ["OptionalReference"] = new BsonDocument
            {
                ["Number"] = 7.0,
                ["Text"] = "e6 or",
                ["NestedCollection"] =
                    new BsonArray
                    {
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e6 or c1"
                        },
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e6 or c2"
                        }
                    },
                ["NestedOptionalReference"] =
                    new BsonDocument
                    {
                        ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        ["Text"] = "e6 or nor"
                    },
                ["NestedRequiredReference"] = BsonNull.Value
            },
            ["RequiredReference"] = new BsonDocument
            {
                ["Number"] = 7.0,
                ["Text"] = "e6 rr",
                ["NestedCollection"] =
                    new BsonArray
                    {
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e6 rr c1"
                        },
                        new BsonDocument
                        {
                            ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ["Text"] = "e6 rr c2"
                        }
                    },
                ["NestedOptionalReference"] =
                    new BsonDocument
                    {
                        ["DoB"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        ["Text"] = "e6 rr nor"
                    },
                ["NestedRequiredReference"] = BsonNull.Value
            }
        };

        await collection.InsertManyAsync([
            missingTopLevel,
            missingNested,
            nullTopLevel,
            missingRequiredNav,
            nullRequiredNav]).ConfigureAwait(false);
    }

#if !EF9
    // The remaining seeds are abstract in the base: they are written for relational providers as raw JSON column values.
    // They are not implemented for MongoDB, so the tests that use them find no data (or no collection) and assert that.
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
    private TestMqlLoggerFactory TestMqlLoggerFactory
        => (TestMqlLoggerFactory)ListLoggerFactory;

    private void AssertMql(params string[] expected)
        => TestMqlLoggerFactory.AssertBaseline(expected);

    private ITestStoreFactory? _testStoreFactory;

    protected override ITestStoreFactory TestStoreFactory
        => _testStoreFactory!;

    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocJsonQuery");

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);

        await base.InitializeAsync();
    }
}

#endif
