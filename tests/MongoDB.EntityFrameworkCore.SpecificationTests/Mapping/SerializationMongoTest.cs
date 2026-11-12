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
    public override void Can_round_trip_through_JSON(bool useNewtonsoft, bool ignoreLoops, bool writeIndented)
        => base.Can_round_trip_through_JSON(useNewtonsoft, ignoreLoops, writeIndented);
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

            await base.InitializeAsync();
        }

        public override TestHelpers TestHelpers
            => MongoTestHelpers.Instance;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        // From EF9 the F1 data is added through UseSeeding / UseAsyncSeeding, which the provider's EnsureCreated does not run, so seed it here.
#if !EF8
        protected override async Task SeedAsync(Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel.F1Context context)
        {
            Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel.F1Context.AddSeedData(context);
            await context.SaveChangesAsync();
        }
#endif

        // The F1 model is built outside the normal pipeline (UseModel), so OnModelCreating is not part of it.
        protected override void BuildModelExternal(ModelBuilder modelBuilder)
        {
            base.BuildModelExternal(modelBuilder);

            // EF-X201: single-property keys must be mapped to _id. EF-X214: concurrency tokens are supported only on the document root.
            modelBuilder.FixExternalModel(dropOwnedConcurrencyTokens: true);

            // EF-X207: a unique index on a nullable foreign key treats every null as a duplicate.
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
            modelBuilder.RelaxUniqueForeignKeyIndexes();
        }
    }
}
