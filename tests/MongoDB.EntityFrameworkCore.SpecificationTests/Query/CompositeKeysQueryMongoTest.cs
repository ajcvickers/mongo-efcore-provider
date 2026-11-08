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

public class CompositeKeysQueryMongoTest : CompositeKeysQueryTestBase<CompositeKeysQueryMongoTest.CompositeKeysQueryMongoFixture>
{
    public CompositeKeysQueryMongoTest(CompositeKeysQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Projecting_multiple_collections_same_level_top_level_ordering(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_multiple_collections_same_level_top_level_ordering(async), "could not be translated");
    }

    public override async Task Projecting_multiple_collections_same_level_top_level_ordering_using_entire_composite_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_multiple_collections_same_level_top_level_ordering_using_entire_composite_key(async), "could not be translated");
    }

    public override async Task Projecting_multiple_collections_with_ordering_same_level(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_multiple_collections_with_ordering_same_level(async), "could not be translated");
    }

    public override async Task Projecting_multiple_collections_with_ordering_same_level_top_level_ordering(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_multiple_collections_with_ordering_same_level_top_level_ordering(async), "could not be translated");
    }

    public override async Task Projecting_collections_multi_level(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_collections_multi_level(async), "could not be translated");
    }

    public override async Task Projecting_multiple_collections_on_multiple_levels_no_explicit_ordering(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_multiple_collections_on_multiple_levels_no_explicit_ordering(async), "could not be translated");
    }

    public override async Task Projecting_multiple_collections_on_multiple_levels_some_explicit_ordering(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_multiple_collections_on_multiple_levels_some_explicit_ordering(async), "could not be translated");
    }

    public class CompositeKeysQueryMongoFixture : CompositeKeysQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("CompKeys");

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
    }
}
