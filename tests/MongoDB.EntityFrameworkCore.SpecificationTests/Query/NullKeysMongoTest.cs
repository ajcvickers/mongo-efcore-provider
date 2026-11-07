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

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class NullKeysMongoTest(NullKeysMongoTest.NullKeysMongoFixture fixture)
    : NullKeysTestBase<NullKeysMongoTest.NullKeysMongoFixture>(fixture)
{
    public class NullKeysMongoFixture : NullKeysFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("NullKeys");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override PoolableDbContext CreateContext()
        {
            _initFailure.ThrowIfFailed();
            return base.CreateContext();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: false);
        }
    }

    // Fails: Unique index build fails on null/missing key values EF-X106
    public override void Include_with_non_nullable_FKs_and_nullable_PK()
        => SpecFails.Throws<global::MongoDB.Driver.MongoCommandException>(() => base.Include_with_non_nullable_FKs_and_nullable_PK(), "Command createIndexes failed: Index build failed");

    // Fails: Unique index build fails on null/missing key values EF-X106
    public override void Include_with_null_FKs_and_nullable_PK()
        => SpecFails.Throws<global::MongoDB.Driver.MongoCommandException>(() => base.Include_with_null_FKs_and_nullable_PK(), "Command createIndexes failed: Index build failed");

    // Fails: Unique index build fails on null/missing key values EF-X106
    public override void Include_with_null_fKs_and_non_nullable_PK()
        => SpecFails.Throws<global::MongoDB.Driver.MongoCommandException>(() => base.Include_with_null_fKs_and_non_nullable_PK(), "Command createIndexes failed: Index build failed");

    // Fails: Unique index build fails on null/missing key values EF-X106
    public override void Include_with_null_fKs_and_nullable_PK()
        => SpecFails.Throws<global::MongoDB.Driver.MongoCommandException>(() => base.Include_with_null_fKs_and_nullable_PK(), "Command createIndexes failed: Index build failed");

    // Fails: Unique index build fails on null/missing key values EF-X106
    public override void One_to_one_self_ref_Include()
        => SpecFails.Throws<global::MongoDB.Driver.MongoCommandException>(() => base.One_to_one_self_ref_Include(), "Command createIndexes failed: Index build failed");
}
