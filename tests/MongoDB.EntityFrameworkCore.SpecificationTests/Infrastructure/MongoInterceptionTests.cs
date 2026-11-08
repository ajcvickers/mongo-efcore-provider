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
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Infrastructure;

public class SaveChangesInterceptionMongoTest(SaveChangesInterceptionMongoTest.InterceptionMongoFixture fixture)
    : SaveChangesInterceptionTestBase(fixture), IClassFixture<SaveChangesInterceptionMongoTest.InterceptionMongoFixture>
{
    // Fails: DbUpdateConcurrencyException is thrown without invoking ISaveChangesInterceptor.ThrowingConcurrencyException EF-X400
    public override async Task Intercept_SaveChanges_failed(bool async, bool inject, bool noAcceptChanges, bool concurrencyError)
    {
        if (concurrencyError)
        {
            await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(
                () => base.Intercept_SaveChanges_failed(async, inject, noAcceptChanges, concurrencyError));
        }
        else
        {
            await base.Intercept_SaveChanges_failed(async, inject, noAcceptChanges, concurrencyError);
        }
    }

    // Fails: DbUpdateConcurrencyException is thrown without invoking ISaveChangesInterceptor.ThrowingConcurrencyException EF-X400
    public override Task Intercept_to_suppress_concurrency_exception(bool async, bool inject, bool noAcceptChanges)
        => Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(
            () => base.Intercept_to_suppress_concurrency_exception(async, inject, noAcceptChanges));

    public class InterceptionMongoFixture : InterceptionFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("SaveChangesInterception");

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override bool ShouldSubscribeToDiagnosticListener
            => false;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);
            await base.InitializeAsync();
        }

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<IInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkMongoDB(), injectedInterceptors);
    }
}

public class QueryExpressionInterceptionMongoTest(QueryExpressionInterceptionMongoTest.InterceptionMongoFixture fixture)
    : QueryExpressionInterceptionTestBase(fixture), IClassFixture<QueryExpressionInterceptionMongoTest.InterceptionMongoFixture>
{
    public class InterceptionMongoFixture : InterceptionFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("QueryExpressionInterception");

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override bool ShouldSubscribeToDiagnosticListener
            => false;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);
            await base.InitializeAsync();
        }

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<IInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkMongoDB(), injectedInterceptors);
    }
}

#if EF8
public class SingletonInterceptorsMongoTest(SingletonInterceptorsMongoTest.SingletonInterceptorsMongoFixture fixture)
    : SingletonInterceptorsTestBase<SingletonInterceptorsMongoTest.MongoLibraryContext>(fixture),
        IClassFixture<SingletonInterceptorsMongoTest.SingletonInterceptorsMongoFixture>
{
    public override LibraryContext CreateContext(IEnumerable<ISingletonInterceptor> interceptors, bool inject)
        => new MongoLibraryContext(Fixture.CreateOptions(interceptors, inject));

    public class MongoLibraryContext(DbContextOptions options) : LibraryContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestEntity30244>(b => b.OwnsMany(e => e.Settings));
        }
    }

    public class SingletonInterceptorsMongoFixture : SingletonInterceptorsFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("SingletonInterceptors");

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);
            await base.InitializeAsync();
        }

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<ISingletonInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkMongoDB(), injectedInterceptors);
    }
}

public class MaterializationInterceptionMongoTest(MaterializationInterceptionMongoTest.MaterializationInterceptionMongoFixture fixture)
    : MaterializationInterceptionTestBase<MaterializationInterceptionMongoTest.MongoLibraryContext>(fixture),
        IClassFixture<MaterializationInterceptionMongoTest.MaterializationInterceptionMongoFixture>
{
    public override LibraryContext CreateContext(IEnumerable<ISingletonInterceptor> interceptors, bool inject)
        => new MongoLibraryContext(Fixture.CreateOptions(interceptors, inject));

    public class MongoLibraryContext(DbContextOptions options) : LibraryContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestEntity30244>(b => b.OwnsMany(e => e.Settings));
        }
    }

    public class MaterializationInterceptionMongoFixture : SingletonInterceptorsFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("MaterializationInterception");

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);
            await base.InitializeAsync();
        }

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<ISingletonInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkMongoDB(), injectedInterceptors);
    }

    // Fails: Projecting an owned collection navigation directly is not translated EF-X402
    public override async Task Intercept_query_materialization_with_owned_types_projecting_collection(bool async)
        => Assert.Contains(
            "could not be translated",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Intercept_query_materialization_with_owned_types_projecting_collection(async))).Message);
}
#else
public class SingletonInterceptorsMongoTest
#if EF10 || EF11
    (NonSharedFixture fixture) : SingletonInterceptorsTestBase<SingletonInterceptorsMongoTest.MongoLibraryContext>(fixture)
#else
    : SingletonInterceptorsTestBase<SingletonInterceptorsMongoTest.MongoLibraryContext>
#endif
{
    private ITestStoreFactory? _testStoreFactory;

#if EF11
    protected override ITestStoreFactory NonSharedTestStoreFactory
#else
    protected override ITestStoreFactory TestStoreFactory
#endif
        => _testStoreFactory!;

#if EF11
    protected override string NonSharedStoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("SingletonInterceptors");
#else
    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("SingletonInterceptors");
#endif

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);
        await base.InitializeAsync();
    }

    protected override IServiceCollection InjectInterceptors(
        IServiceCollection serviceCollection,
        IEnumerable<ISingletonInterceptor> injectedInterceptors)
        => base.InjectInterceptors(serviceCollection.AddEntityFrameworkMongoDB(), injectedInterceptors);

    public class MongoLibraryContext(DbContextOptions options) : LibraryContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestEntity30244>(b => b.OwnsMany(e => e.Settings));
        }
    }
}

public class MaterializationInterceptionMongoTest
#if EF10 || EF11
    (NonSharedFixture fixture) : MaterializationInterceptionTestBase<MaterializationInterceptionMongoTest.MongoLibraryContext>(fixture)
#else
    : MaterializationInterceptionTestBase<MaterializationInterceptionMongoTest.MongoLibraryContext>
#endif
{
    private ITestStoreFactory? _testStoreFactory;

#if EF11
    protected override ITestStoreFactory NonSharedTestStoreFactory
#else
    protected override ITestStoreFactory TestStoreFactory
#endif
        => _testStoreFactory!;

#if EF11
    protected override string NonSharedStoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("MaterializationInterception");
#else
    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("MaterializationInterception");
#endif

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);
        await base.InitializeAsync();
    }

    protected override IServiceCollection InjectInterceptors(
        IServiceCollection serviceCollection,
        IEnumerable<ISingletonInterceptor> injectedInterceptors)
        => base.InjectInterceptors(serviceCollection.AddEntityFrameworkMongoDB(), injectedInterceptors);

    // Fails: Projecting an owned collection navigation directly is not translated EF-X402
    public override async Task Intercept_query_materialization_with_owned_types_projecting_collection(bool async, bool usePooling)
        => Assert.Contains(
            "could not be translated",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Intercept_query_materialization_with_owned_types_projecting_collection(async, usePooling))).Message);

    public class MongoLibraryContext(DbContextOptions options) : LibraryContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestEntity30244>(b => b.OwnsMany(e => e.Settings));
        }
    }
}
#endif

public class ConcurrencyDetectorEnabledMongoTest(ConcurrencyDetectorEnabledMongoTest.ConcurrencyDetectorMongoFixture fixture)
    : ConcurrencyDetectorEnabledTestBase<ConcurrencyDetectorEnabledMongoTest.ConcurrencyDetectorMongoFixture>(fixture)
{
    public class ConcurrencyDetectorMongoFixture : ConcurrencyDetectorFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("ConcurrencyDetectorEnabled");

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);
            await base.InitializeAsync();
        }
    }
}

public class ConcurrencyDetectorDisabledMongoTest(ConcurrencyDetectorDisabledMongoTest.ConcurrencyDetectorMongoFixture fixture)
    : ConcurrencyDetectorDisabledTestBase<ConcurrencyDetectorDisabledMongoTest.ConcurrencyDetectorMongoFixture>(fixture)
{
    public class ConcurrencyDetectorMongoFixture : ConcurrencyDetectorFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("ConcurrencyDetectorDisabled");

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);
            await base.InitializeAsync();
        }

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder).EnableThreadSafetyChecks(false);
    }
}
