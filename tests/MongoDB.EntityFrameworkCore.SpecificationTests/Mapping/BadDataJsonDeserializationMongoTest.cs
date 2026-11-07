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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Mapping;

public class BadDataJsonDeserializationMongoTest : BadDataJsonDeserializationTestBase
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMongoDB("mongodb://localhost:27017", "BadDataJsonDeserialization");

    // <generated>
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Throws_for_bad_collection_of_nullable_long_JSON_values(string json)
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Throws_for_bad_collection_of_nullable_long_JSON_values(json), "The 'IList<long?>' property 'NullableInt64CollectionType.Int64' could ");

    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Throws_for_bad_collection_of_sbyte_JSON_values(string json)
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Throws_for_bad_collection_of_sbyte_JSON_values(json), "The 'sbyte[]' property 'Int8CollectionType.Int8' could not be mapped b");

    // Fails (some data rows): Bad JSON value does not throw on materialization EF-X318
    public override void Throws_for_bad_nullable_long_JSON_values(string json)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.XunitException>(() => base.Throws_for_bad_nullable_long_JSON_values(json), "Assert.True() Failure");

    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Throws_for_bad_point_as_GeoJson(string json)
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Throws_for_bad_point_as_GeoJson(json), "The 'Point' property 'PointType.Point' could not be mapped because the");

    // Fails (some data rows): Bad JSON value does not throw on materialization EF-X318
    public override void Throws_for_bad_sbyte_JSON_values(string json)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.XunitException>(() => base.Throws_for_bad_sbyte_JSON_values(json), "Assert.True() Failure");
    // </generated>
}
