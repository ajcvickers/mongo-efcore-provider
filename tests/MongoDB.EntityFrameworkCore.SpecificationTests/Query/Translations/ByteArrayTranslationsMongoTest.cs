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

#if !EF8 && !EF9

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Translations;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Translations;

public class ByteArrayTranslationsMongoTest : ByteArrayTranslationsTestBase<MongoBasicTypesQueryFixture>
{
    public ByteArrayTranslationsMongoTest(MongoBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    // Fails: Translated pipeline is rejected by the server EF-X323
    public override Task Contains_with_column()
        => SpecFails.Async<global::MongoDB.Driver.MongoCommandException>(() => base.Contains_with_column(), "Command aggregate failed: Executor error during aggregate command on n");

    // Fails: byte[] is not supported as an array in LINQ EF-X322
    public override Task Contains_with_constant()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Contains_with_constant(), "MongoDB.Bson.Serialization.Serializers.ByteArraySerializer must implem");

    // Fails: byte[] is not supported as an array in LINQ EF-X322
    public override Task Contains_with_parameter()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Contains_with_parameter(), "MongoDB.Bson.Serialization.Serializers.ByteArraySerializer must implem");

    // Fails: byte[] is not supported as an array in LINQ EF-X322
    public override Task First()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.First(), "MongoDB.Bson.Serialization.Serializers.ByteArraySerializer must implem");

    // Fails: byte[] is not supported as an array in LINQ EF-X322
    public override Task Index()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Index(), "MongoDB.Bson.Serialization.Serializers.ByteArraySerializer must implem");

    // Fails: byte[].Length query returns no rows EF-X324
    public override Task Length()
        => SpecFails.Async<global::Xunit.Sdk.XunitException>(() => base.Length(), "Assert.Equal() Failure: Values differ");

    // Fails: Translated pipeline is rejected by the server EF-X323
    public override Task SequenceEqual()
        => SpecFails.Async<global::MongoDB.Driver.MongoCommandException>(() => base.SequenceEqual(), "Command aggregate failed: Failed to optimize pipeline :: caused by :: ");
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
