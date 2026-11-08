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

public class ValueConvertersEndToEndMongoTest(ValueConvertersEndToEndMongoTest.ValueConvertersEndToEndMongoFixture fixture)
    : ValueConvertersEndToEndTestBase<ValueConvertersEndToEndMongoTest.ValueConvertersEndToEndMongoFixture>(fixture)
{
    // <generated>
#if (EF10 || EF11) || EF9
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override Task Can_insert_and_read_back_with_conversions(int[] valueOrder)
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Can_insert_and_read_back_with_conversions(valueOrder), "Unsupported ValueConverter for 'Nullable<Int32>' encountered. Null con");
#elif EF8
    // Fails: Value converter built from inline conversion expressions on a nullable property is not supported EF-X300
    public override void Can_insert_and_read_back_with_conversions(int[] valueOrder)
        => SpecFails.Sync<global::System.NotSupportedException>(() => base.Can_insert_and_read_back_with_conversions(valueOrder), "Unsupported ValueConverter for 'Nullable<Int32>' encountered. Null con");
#endif
    // </generated>

    public class ValueConvertersEndToEndMongoFixture : ValueConvertersEndToEndFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("ValueConvertersEndToEnd");

        public TestServer TestServer { get; private set; }

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await base.InitializeAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;
    }
}
