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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

// EF Core 11 has no AdHocComplexTypeQueryTestBase in the Specification.Tests package yet (see #22).
#if !EF11
public class AdHocComplexTypeQueryMongoTest : AdHocComplexTypeQueryTestBase
{
#if EF10
    public AdHocComplexTypeQueryMongoTest(NonSharedFixture fixture)
        : base(fixture)
    {
    }
#endif

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Complex_type_equals_parameter_with_nested_types_with_property_of_same_name()
    {
        await base.Complex_type_equals_parameter_with_nested_types_with_property_of_same_name();

        AssertMql(
"""
EntityType.{ "$match" : { "$or" : [{ "$and" : [{ "$expr" : false }, { "ComplexContainer" : null }] }, { "$and" : [{ "$expr" : true }, { "ComplexContainer.Id" : 1 }, { "$or" : [{ "$and" : [{ "$expr" : false }, { "ComplexContainer.Containee1" : null }] }, { "$and" : [{ "$expr" : true }, { "ComplexContainer.Containee1.Id" : 2 }] }] }, { "$or" : [{ "$and" : [{ "$expr" : false }, { "ComplexContainer.Containee2" : null }] }, { "$and" : [{ "$expr" : true }, { "ComplexContainer.Containee2.Id" : 3 }] }] }] }] } }, { "$limit" : 2 }
""");
    }

#if EF10
    public override async Task Projecting_complex_property_does_not_auto_include_owned_types()
    {
        await base.Projecting_complex_property_does_not_auto_include_owned_types();

        AssertMql(
"""
EntityType.{ "$project" : { "Complex" : "$Complex", "_id" : 0 } }
""");
    }

    public override async Task Optional_complex_type_with_discriminator()
    {
        await base.Optional_complex_type_with_discriminator();

        AssertMql(
"""
EntityType.{ "$match" : { "$or" : [{ "AllOptionalsComplexType" : null }, { "AllOptionalsComplexType.Discriminator" : null }] } }, { "$limit" : 2 }
""",
            //
"""
EntityType.
""");
    }

    public override async Task Complex_type_equality_with_non_default_type_mapping()
    {
#if EF10
        // Fails: Not supported yet #35
        await global::MongoDB.EntityFrameworkCore.SpecificationTests.Query.MongoSpecTestHelpers.AssertKnownGapAsync(() => base.Complex_type_equality_with_non_default_type_mapping());
#else
        await base.Complex_type_equality_with_non_default_type_mapping();

        AssertMql();
    
#endif
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
        await base.Nullable_complex_type_with_discriminator_and_shadow_property();

        AssertMql(
"""
EntityType.
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_null_to_non_null_roundtrip()
    {
        await base.Nullable_complex_type_with_discriminator_null_to_non_null_roundtrip();

        AssertMql(
"""
EntityType.{ "$limit" : 2 }
""",
"""
EntityType.{ "$limit" : 2 }
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_non_null_to_null_roundtrip()
    {
        await base.Nullable_complex_type_with_discriminator_non_null_to_null_roundtrip();

        AssertMql(
"""
EntityType.{ "$limit" : 2 }
""",
"""
EntityType.{ "$limit" : 2 }
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_update_non_null_entity_roundtrip()
    {
        await base.Nullable_complex_type_with_discriminator_update_non_null_entity_roundtrip();

        AssertMql(
"""
EntityType.{ "$limit" : 2 }
""",
"""
EntityType.{ "$limit" : 2 }
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_set_to_different_value()
    {
        await base.Nullable_complex_type_with_discriminator_set_to_different_value();

        // The query embeds a Guid key generated per run, so Guid values are normalized before comparing.
        AssertMqlIgnoringGuids(
            """
EntityType.{ "$match" : { "_id" : { "$binary" : { "base64" : "+5NAsgL6SdGhPRHD2HGA3w==", "subType" : "04" } } } }, { "$limit" : 2 }
""");
    }

    public override async Task Nullable_complex_type_with_discriminator_set_to_null()
    {
        await base.Nullable_complex_type_with_discriminator_set_to_null();

        // The query embeds a Guid key generated per run, so Guid values are normalized before comparing.
        AssertMqlIgnoringGuids(
            """
EntityType.{ "$match" : { "_id" : { "$binary" : { "base64" : "cmMgCGFVS6aOJOJfovJw8g==", "subType" : "04" } } } }, { "$limit" : 2 }
""");
    }

    public override async Task Nested_nullable_complex_type_with_discriminator_null_to_non_null_roundtrip()
    {
        await base.Nested_nullable_complex_type_with_discriminator_null_to_non_null_roundtrip();

        AssertMql(
"""
EntityType.{ "$limit" : 2 }
""",
"""
EntityType.{ "$limit" : 2 }
""");
    }
#endif

    private TestMqlLoggerFactory TestMqlLoggerFactory
        => (TestMqlLoggerFactory)ListLoggerFactory;

    private void AssertMqlIgnoringGuids(params string[] expected)
    {
        static string Normalize(string mql)
            => System.Text.RegularExpressions.Regex.Replace(
                mql, """\{ "base64" : "[^"]*", "subType" : "04" \}""", """{ "base64" : <guid>, "subType" : "04" }""");

        var actual = TestMqlLoggerFactory.MqlStatements.Select(Normalize).ToArray();
        TestMqlLoggerFactory.Clear();
        Assert.Equal(expected.Select(Normalize).ToArray(), actual);
    }

    private void AssertMql(params string[] expected)
        => TestMqlLoggerFactory.AssertBaseline(expected);

#if EF10
    // The seed saves three EntityType rows with an int key, which the provider does not generate (EF-X205); without a client-side
    // generator all three get Id 0 and the seed fails with an identity conflict before any query runs.
    protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
        => base.AddServices(serviceCollection)
            .Replace(ServiceDescriptor.Singleton<IModelCustomizer, IntegerKeyModelCustomizer>());

    private sealed class IntegerKeyModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
        }
    }

#endif
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
#endif
