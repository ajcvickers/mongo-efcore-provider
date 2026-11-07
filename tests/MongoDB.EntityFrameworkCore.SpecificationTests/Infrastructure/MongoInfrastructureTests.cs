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

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Infrastructure;

public class LoggingMongoTest : LoggingTestBase
{
    protected override DbContextOptionsBuilder CreateOptionsBuilder(IServiceCollection services)
        => new DbContextOptionsBuilder()
            .UseMongoDB("mongodb://localhost:27017", "LoggingMongoTest")
            .UseInternalServiceProvider(services.AddEntityFrameworkMongoDB().BuildServiceProvider(validateScopes: true));

    protected override TestLogger CreateTestLogger()
        => new TestLogger<MongoLoggingDefinitions>();

    protected override string DefaultOptions
        => "DatabaseName=LoggingMongoTest ";

    protected override string ProviderName
        => typeof(MongoOptionsExtension).Assembly.GetName().Name!;

    protected override string ProviderVersion
        => typeof(MongoOptionsExtension).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;
}

public class ModelBuilding101MongoTest : ModelBuilding101TestBase
{
    protected override DbContextOptionsBuilder ConfigureContext(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMongoDB("mongodb://localhost:27017", "ModelBuilding101MongoTest");

    // Fails: Join-class foreign key discovery is ambiguous EF-X401
    public override void ManyToManyWithNavsToJoinClassTest()
        => Assert.Throws<InvalidOperationException>(() => base.ManyToManyWithNavsToJoinClassTest());

    // Fails: Join-class foreign key discovery is ambiguous EF-X401
    public override void ManyToManyWithNavsToAndFromJoinClassTest()
        => Assert.Throws<InvalidOperationException>(() => base.ManyToManyWithNavsToAndFromJoinClassTest());

    // Fails: Join-class foreign key discovery is ambiguous EF-X401
    public override void ManyToManyWithPayloadAndNavsToJoinClassTest()
        => Assert.Throws<InvalidOperationException>(() => base.ManyToManyWithPayloadAndNavsToJoinClassTest());
}

public class MongoServiceCollectionExtensionsTest : EntityFrameworkServiceCollectionExtensionsTestBase
{
    public MongoServiceCollectionExtensionsTest()
        : base(MongoTestHelpers.Instance)
    {
    }
}

public class SeedingMongoTest : SeedingTestBase
{
    protected override TestStore TestStore
        => MongoTestStore.Create("SeedingTest", TestServer.GetOrInitializeTestServerAsync(MongoCondition.None).GetAwaiter().GetResult());

    protected override SeedingContext CreateContextWithEmptyDatabase(string testId)
    {
        var context = new SeedingMongoContext(testId, TestServer.GetOrInitializeTestServerAsync(MongoCondition.None).GetAwaiter().GetResult());
        return context;
    }

    private class SeedingMongoContext(string testId, TestServer testServer) : SeedingContext(testId)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseMongoDB(testServer.Client, TestDatabaseNamer.GetUniqueDatabaseName("Seeding" + TestId));
    }
}

public class SharedTypeQueryMongoTest
#if EF10
    (NonSharedFixture fixture) : SharedTypeQueryTestBase(fixture)
#else
    : SharedTypeQueryTestBase
#endif
{
    private ITestStoreFactory? _testStoreFactory;

    protected override ITestStoreFactory TestStoreFactory
        => _testStoreFactory!;

    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("SharedTypeQuery");

    // Fails: Subqueries across DbSets in a query filter are not supported EF-X001
    public override async Task Can_use_shared_type_entity_type_in_query_filter(bool async)
        => Assert.Contains(
            "Unsupported cross-DbSet query between",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Can_use_shared_type_entity_type_in_query_filter(async))).Message);

    public override async Task InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);

        await base.InitializeAsync();
    }
}
