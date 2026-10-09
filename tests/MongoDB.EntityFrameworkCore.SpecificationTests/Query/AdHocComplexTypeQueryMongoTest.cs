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
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class AdHocComplexTypeQueryMongoTest : AdHocComplexTypeQueryTestBase
{
#if EF10
    public AdHocComplexTypeQueryMongoTest(NonSharedFixture fixture)
        : base(fixture)
        // Do not share the (never-cleaned, seed-once) store across tests: the base defines a different inline
        // context per test, so each test gets its own uniquely-named database, like every other Mongo spec test.
        => Fixture = null;
#endif

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Complex_type_equals_parameter_with_nested_types_with_property_of_same_name()
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Complex_type_equals_parameter_with_nested_types_with_property_of_same_name());

        AssertMql(
"""
EntityType.
""");
    }

#if EF10
    public override async Task Projecting_complex_property_does_not_auto_include_owned_types()
    {
        // Fails: Complex Types: query translation not supported EF-168
        await AssertTranslationFailed(() => base.Projecting_complex_property_does_not_auto_include_owned_types());

        AssertMql(
"""
EntityType.
""");
    }

    public override async Task Optional_complex_type_with_discriminator()
    {
        // Fails: Complex Types: entities with a discriminated optional complex type get a duplicate generated key EF-168
        await Assert.ThrowsAsync<InvalidOperationException>(() => base.Optional_complex_type_with_discriminator());

        AssertMql();
    }

    public override async Task Complex_type_equality_with_non_default_type_mapping()
    {
        // Fails: Complex Types: ColumnAttribute.TypeName throws as a model warning EF-168
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => base.Complex_type_equality_with_non_default_type_mapping());
        Assert.Contains("specifies a 'ColumnAttribute.TypeName' which is not supported by MongoDB", exception.Message);

        AssertMql();
    }

    public override async Task Non_optional_complex_type_with_all_nullable_properties()
    {
        await base.Non_optional_complex_type_with_all_nullable_properties();

        AssertMql(
"""
EntityType.{ "$limit" : 2 }
""");
    }

    public override async Task Non_optional_complex_type_with_all_nullable_properties_via_left_join()
    {
        await base.Non_optional_complex_type_with_all_nullable_properties_via_left_join();

        AssertMql(
"""
Parent.{ "$limit" : 2 }, { "$lookup" : { "from" : "Child", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_Children" } }
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_and_shadow_property()
    {
        // Fails: Complex Types: discriminator not persisted EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Nullable_complex_type_with_discriminator_and_shadow_property())).Message);

        AssertMql(
"""
EntityType.
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_null_to_non_null_roundtrip()
    {
        // Fails: Complex Types: discriminator not persisted EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Nullable_complex_type_with_discriminator_null_to_non_null_roundtrip())).Message);

        AssertMql(
"""
EntityType.{ "$limit" : 2 }
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_non_null_to_null_roundtrip()
    {
        // Fails: Complex Types: discriminator not persisted EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Nullable_complex_type_with_discriminator_non_null_to_null_roundtrip())).Message);

        AssertMql(
"""
EntityType.{ "$limit" : 2 }
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_update_non_null_entity_roundtrip()
    {
        // Fails: Complex Types: discriminator not persisted EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Nullable_complex_type_with_discriminator_update_non_null_entity_roundtrip())).Message);

        AssertMql(
"""
EntityType.{ "$limit" : 2 }
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_set_to_different_value()
    {
        // Fails: Complex Types: discriminator not persisted EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Nullable_complex_type_with_discriminator_set_to_different_value())).Message);

        // No AssertMql: the query filters on a random Guid generated inside the test.
    }

    public override async Task Nullable_complex_type_with_discriminator_set_to_null()
    {
        // Fails: Complex Types: discriminator not persisted EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Nullable_complex_type_with_discriminator_set_to_null())).Message);

        // No AssertMql: the query filters on a random Guid generated inside the test.
    }

    public override async Task Nested_nullable_complex_type_with_discriminator_null_to_non_null_roundtrip()
    {
        // Fails: Complex Types: discriminator not persisted EF-168
        Assert.Contains(
            "Document element is missing",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Nested_nullable_complex_type_with_discriminator_null_to_non_null_roundtrip())).Message);

        AssertMql(
"""
EntityType.{ "$limit" : 2 }
""");
    }
#endif

    private TestMqlLoggerFactory TestMqlLoggerFactory
        => (TestMqlLoggerFactory)ListLoggerFactory;

    private void AssertMql(params string[] expected)
        => TestMqlLoggerFactory.AssertBaseline(expected);

    protected static Task AssertTranslationFailed(Func<Task> query)
        => MongoSpecTestHelpers.AssertNativeTranslationFailedAsync(query);

    private ITestStoreFactory? _testStoreFactory;

    protected override ITestStoreFactory TestStoreFactory
        => _testStoreFactory!;

    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocComplexTypeQuery");

    public override async Task InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);

        await base.InitializeAsync();
    }
}
