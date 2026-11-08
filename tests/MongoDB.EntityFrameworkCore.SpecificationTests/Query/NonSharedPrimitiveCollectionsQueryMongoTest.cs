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

#if !EF11

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class NonSharedPrimitiveCollectionsQueryMongoTest : NonSharedPrimitiveCollectionsQueryTestBase
{
#if EF10 || EF11
    public NonSharedPrimitiveCollectionsQueryMongoTest(NonSharedFixture fixture)
        : base(fixture)
    {
        // The class-level fixture would reuse the first test's (already seeded) test store for every test;
        // give each test its own uniquely named database instead.
        Fixture = null;
    }

#endif
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Array_of_string()
    {
        await base.Array_of_string();
    }

    public override async Task Array_of_int()
    {
        await base.Array_of_int();
    }

    public override async Task Array_of_long()
    {
        await base.Array_of_long();
    }

    public override async Task Array_of_short()
    {
        await base.Array_of_short();
    }

    public override async Task Array_of_byte()
    {
        // Fails: byte[] element collection: ByteArraySerializer is not an IBsonArraySerializer EF-X510
        await SpecFails.ThrowsAsync(() => base.Array_of_byte(), "must implement IBsonArraySerializer");
    }

    public override async Task Array_of_double()
    {
        await base.Array_of_double();
    }

    public override async Task Array_of_float()
    {
        await base.Array_of_float();
    }

    public override async Task Array_of_decimal()
    {
        await base.Array_of_decimal();
    }

    public override async Task Array_of_DateTime()
    {
        await base.Array_of_DateTime();
    }

    public override async Task Array_of_DateTime_with_milliseconds()
    {
        await base.Array_of_DateTime_with_milliseconds();
    }

    public override async Task Array_of_DateTime_with_microseconds()
    {
        // Fails: Microsecond DateTime values are truncated to milliseconds so distinct elements collapse/duplicate EF-X508
        await SpecFails.ThrowsAsync(() => base.Array_of_DateTime_with_microseconds(), "Sequence contains more than one element");
    }

    public override async Task Array_of_DateOnly()
    {
        await base.Array_of_DateOnly();
    }

    public override async Task Array_of_TimeOnly()
    {
        await base.Array_of_TimeOnly();
    }

    public override async Task Array_of_TimeOnly_with_milliseconds()
    {
        await base.Array_of_TimeOnly_with_milliseconds();
    }

    public override async Task Array_of_TimeOnly_with_microseconds()
    {
        await base.Array_of_TimeOnly_with_microseconds();
    }

    public override async Task Array_of_DateTimeOffset()
    {
        await base.Array_of_DateTimeOffset();
    }

    public override async Task Array_of_bool()
    {
        await base.Array_of_bool();
    }

    public override async Task Array_of_Guid()
    {
        await base.Array_of_Guid();
    }

    public override async Task Array_of_byte_array()
    {
        await base.Array_of_byte_array();
    }

    public override async Task Array_of_enum()
    {
        await base.Array_of_enum();
    }

    public override async Task Multidimensional_array_is_not_supported()
    {
        await base.Multidimensional_array_is_not_supported();
    }

    public override async Task Column_with_custom_converter()
    {
        // Fails: Query over a primitive collection with a custom converter returns no rows instead of failing as the base expects EF-X509
        await SpecFails.ThrowsAsync(() => base.Column_with_custom_converter(), "Sub-string not found");
    }

    public override async Task Parameter_with_inferred_value_converter()
    {
        await base.Parameter_with_inferred_value_converter();
    }

    public override async Task Constant_with_inferred_value_converter()
    {
        await base.Constant_with_inferred_value_converter();
    }

    public override async Task Inline_collection_in_query_filter()
    {
        await base.Inline_collection_in_query_filter();
    }

#if EF9 || (EF10 || EF11)
    public override async Task Project_collection_from_entity_type_with_owned()
    {
        await base.Project_collection_from_entity_type_with_owned();
    }
#endif

#if (EF10 || EF11)
    public override async Task Subquery_over_primitive_collection_on_inheritance_derived_type()
    {
        await base.Subquery_over_primitive_collection_on_inheritance_derived_type();
    }
#endif

#if EF8
    public override async Task Array_of_array_is_not_supported()
    {
        // Fails: EF8 only: array of arrays does not throw the expected InvalidOperationException EF-X511
        await SpecFails.ThrowsAsync(() => base.Array_of_array_is_not_supported(), "No exception was thrown");
    }
#endif

    private ITestStoreFactory? _testStoreFactory;

    protected override ITestStoreFactory TestStoreFactory
        => _testStoreFactory!;

    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("NSPrimColl");

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

        await base.InitializeAsync();
    }
}

#endif
