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
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Mapping;

public class BuiltInDataTypesMongoTest(BuiltInDataTypesMongoTest.BuiltInDataTypesMongoFixture fixture)
    : BuiltInDataTypesTestBase<BuiltInDataTypesMongoTest.BuiltInDataTypesMongoFixture>(fixture)
{
    // Fails: Enum casting issue EF-215
    public override async Task Can_filter_projection_with_captured_enum_variable(bool async)
        => Assert.Contains(
            "Unexpected target type: Microsoft.EntityFrameworkCore.BuiltInDataTypesTestBase`1+EmailTemplateTypeDto[",
            (await Assert.ThrowsAsync<Exception>(async () => await base.Can_filter_projection_with_captured_enum_variable(async))).Message);

    // Fails: Enum casting issue EF-215
    public override async Task Can_filter_projection_with_inline_enum_variable(bool async)
        => Assert.Contains(
            "Unexpected target type: Microsoft.EntityFrameworkCore.BuiltInDataTypesTestBase`1+EmailTemplateTypeDto[",
            (await Assert.ThrowsAsync<Exception>(async () => await base.Can_filter_projection_with_inline_enum_variable(async))).Message);

    #if EF9
    // Fails: Include issue EF-117
    public override async Task Can_insert_and_read_back_with_string_key()
        => Assert.Contains(
            "Including navigation 'Navigation' is not supported as the navigation is not embedded in same resource.",
            (await Assert.ThrowsAsync<InvalidOperationException>(async () => await base.Can_insert_and_read_back_with_string_key()))
            .Message);

    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_read_back_bool_mapped_as_int_through_navigation()
        => AssertTranslationFailed(() => base.Can_read_back_bool_mapped_as_int_through_navigation());

    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_read_back_mapped_enum_from_collection_first_or_default()
        => AssertTranslationFailed(() => base.Can_read_back_mapped_enum_from_collection_first_or_default());

    // Fails: Call ToString on DateTimeOffset EF-217
    public override async Task Object_to_string_conversion()
        => Assert.Contains(
            "Unsupported conversion from object to string in $convert with no onError value.",
            (await Assert.ThrowsAsync<MongoCommandException>(async () => await base.Object_to_string_conversion())).Message);

    // Fails: Projecting DateTimeOffset members EF-218
    public override async Task Optional_datetime_reading_null_from_database()
        => Assert.Contains(
            "Serializer for System.DateTimeOffset does not represent members as fields.",
            (await Assert.ThrowsAsync<NotSupportedException>(async () => await base.Optional_datetime_reading_null_from_database()))
            .Message);
    #else
    // Fails: Include issue EF-117
    public override void Can_insert_and_read_back_with_string_key()
        => Assert.Contains(
            "Including navigation 'Navigation' is not supported as the navigation is not embedded in same resource.",
            Assert.Throws<InvalidOperationException>(() => base.Can_insert_and_read_back_with_string_key()).Message);

    // Fails: Cross-document navigation access issue EF-216
    public override void Can_read_back_bool_mapped_as_int_through_navigation()
        => AssertTranslationFailed(() => base.Can_read_back_bool_mapped_as_int_through_navigation());

    // Fails: Cross-document navigation access issue EF-216
    public override void Can_read_back_mapped_enum_from_collection_first_or_default()
        => AssertTranslationFailed(() => base.Can_read_back_mapped_enum_from_collection_first_or_default());

    // Fails: Call ToString on DateTimeOffset EF-217
    public override void Object_to_string_conversion()
        => Assert.Contains(
            "Unsupported conversion from object to string in $convert with no onError value.",
            Assert.Throws<MongoCommandException>(() => base.Object_to_string_conversion()).Message);

    // Fails: Projecting DateTimeOffset members EF-218
    public override void Optional_datetime_reading_null_from_database()
        => Assert.Contains(
            "Serializer for System.DateTimeOffset does not represent members as fields.",
            Assert.Throws<NotSupportedException>(() => base.Optional_datetime_reading_null_from_database()).Message);
    #endif

    private static void AssertTranslationFailed(Action query)
        => Assert.Contains(
            CoreStrings.TranslationFailed("")[48..],
            Assert.Throws<InvalidOperationException>(query).Message);

    private static async Task AssertTranslationFailed(Func<Task> query)
        => Assert.Contains(
            CoreStrings.TranslationFailed("")[48..],
            (await Assert.ThrowsAsync<InvalidOperationException>(query)).Message);

    public class BuiltInDataTypesMongoFixture : BuiltInDataTypesFixtureBase
    {
        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder.ConfigureWarnings(w => w.Ignore(MongoEventId.ColumnAttributeWithTypeUsed)));

        protected override ITestStoreFactory TestStoreFactory
            => MongoTestStoreFactory.Instance;

        public override bool StrictEquality
            => true;

        public override int IntegerPrecision
            => 53;

        public override bool SupportsAnsi
            => false;

        public override bool SupportsUnicodeToAnsiConversion
            => false;

        public override bool SupportsLargeStringComparisons
            => true;

        public override bool SupportsBinaryKeys
            => false;

        public override bool SupportsDecimalComparisons
            => true;

        public override DateTime DefaultDateTime
            => new();

        public override bool PreservesDateTimeKind
            => false;
    }
}
