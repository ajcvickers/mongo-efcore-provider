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

public class FunkyDataQueryMongoTest : FunkyDataQueryTestBase<FunkyDataQueryMongoTest.FunkyDataQueryMongoFixture>
{
    public FunkyDataQueryMongoTest(FunkyDataQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task String_contains_on_argument_with_wildcard_constant(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.String_contains_on_argument_with_wildcard_constant(async), "ArgumentNullException");
    }

    public override async Task String_contains_on_argument_with_wildcard_parameter(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.String_contains_on_argument_with_wildcard_parameter(async), "ArgumentNullException");
    }

    public override async Task String_contains_on_argument_with_wildcard_column(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_contains_on_argument_with_wildcard_column(async), "could not be translated");
    }

    public override async Task String_contains_on_argument_with_wildcard_column_negated(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_contains_on_argument_with_wildcard_column_negated(async), "could not be translated");
    }

    public override async Task String_starts_with_on_argument_with_wildcard_constant(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.String_starts_with_on_argument_with_wildcard_constant(async), "ArgumentNullException");
    }

    public override async Task String_starts_with_on_argument_with_wildcard_parameter(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.String_starts_with_on_argument_with_wildcard_parameter(async), "ArgumentNullException");
    }

    public override async Task String_starts_with_on_argument_with_bracket(bool async)
    {
        // Fails: Server error executing the generated pipeline EF-X528
        await SpecFails.ThrowsAsync(() => base.String_starts_with_on_argument_with_bracket(async), "MongoCommandException");
    }

    public override async Task String_starts_with_on_argument_with_wildcard_column(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_starts_with_on_argument_with_wildcard_column(async), "could not be translated");
    }

    public override async Task String_starts_with_on_argument_with_wildcard_column_negated(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_starts_with_on_argument_with_wildcard_column_negated(async), "could not be translated");
    }

    public override async Task String_ends_with_on_argument_with_wildcard_constant(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.String_ends_with_on_argument_with_wildcard_constant(async), "ArgumentNullException");
    }

    public override async Task String_ends_with_on_argument_with_wildcard_parameter(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.String_ends_with_on_argument_with_wildcard_parameter(async), "ArgumentNullException");
    }

    public override async Task String_ends_with_on_argument_with_wildcard_column(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_ends_with_on_argument_with_wildcard_column(async), "could not be translated");
    }

    public override async Task String_ends_with_on_argument_with_wildcard_column_negated(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_ends_with_on_argument_with_wildcard_column_negated(async), "could not be translated");
    }

    public override async Task String_ends_with_inside_conditional(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_ends_with_inside_conditional(async), "could not be translated");
    }

    public override async Task String_ends_with_inside_conditional_negated(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_ends_with_inside_conditional_negated(async), "could not be translated");
    }

    public override async Task String_ends_with_equals_nullable_column(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_ends_with_equals_nullable_column(async), "could not be translated");
    }

    public override async Task String_ends_with_not_equals_nullable_column(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.String_ends_with_not_equals_nullable_column(async), "could not be translated");
    }

    public override async Task String_FirstOrDefault_and_LastOrDefault(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.String_FirstOrDefault_and_LastOrDefault(async), "ArgumentNullException");
    }

    public override async Task String_Contains_and_StartsWith_with_same_parameter(bool async)
    {
        await base.String_Contains_and_StartsWith_with_same_parameter(async);
    }

    public class FunkyDataQueryMongoFixture : FunkyDataQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("FunkyData");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server);

            await base.InitializeAsync();
        }
    }
}
