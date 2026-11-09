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

public class SerializationMongoTest(SerializationMongoTest.SerializationMongoFixture fixture)
    : SerializationTestBase<SerializationMongoTest.SerializationMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    // Fails: Concurrency token on an owned entity is rejected; round trip differs (not triaged) EF-X319
    public override void Can_round_trip_through_JSON(bool useNewtonsoft, bool ignoreLoops, bool writeIndented)
        => SpecFails.Sync<global::Xunit.Sdk.XunitException>(() => base.Can_round_trip_through_JSON(useNewtonsoft, ignoreLoops, writeIndented), "Assert.Equal() Failure: Values differ");
    // </generated>

    public class SerializationMongoFixture : F1FixtureBase<byte[]>
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("Serialization");

        public TestServer TestServer { get; private set; }

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            try
            {
                await base.InitializeAsync();
            }
            catch (NotSupportedException)
            {
                // Fails: Model/seeding fails during fixture initialization EF-X319
                // The tests then run (and fail) against an uninitialized database.
            }
        }

        public override TestHelpers TestHelpers
            => MongoTestHelpers.Instance;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
        }
    }
}
