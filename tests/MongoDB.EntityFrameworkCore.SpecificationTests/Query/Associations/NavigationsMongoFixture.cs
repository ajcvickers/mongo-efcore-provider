/* Copyright 2025-present MongoDB Inc.
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
using Microsoft.EntityFrameworkCore.Query.Associations.Navigations;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Associations;

public class NavigationsMongoFixture : NavigationsFixtureBase
{
    private ITestStoreFactory? _testStoreFactory;

    protected override ITestStoreFactory TestStoreFactory
        => _testStoreFactory!;

    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("Navigations");

    public TestMqlLoggerFactory TestMqlLoggerFactory
        => (TestMqlLoggerFactory)ListLoggerFactory;

    protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
    {
        base.OnModelCreating(modelBuilder, context);

        // A MongoDB unique index treats null foreign keys as duplicates, so the one-to-one FK indexes cannot be unique (EF-X207).
        modelBuilder.RelaxUniqueForeignKeyIndexes();
    }

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);

        await base.InitializeAsync();
    }
}
#endif
