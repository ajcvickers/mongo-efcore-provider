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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

public class MongoTestStore : TestStore
{
    public TestServer TestServer { get; }

    private readonly bool _pinDefaultQueryMode;

    // EF's convention: a fixture's store is shared (initialized once per name); the store of a non-shared test is not, so it is
    // initialized, cleaned and seeded for every test (TestStore.InitializeAsync).
    public static MongoTestStore Create(string name, TestServer testServer, bool shared = false, bool pinDefaultQueryMode = false)
        => new(name, testServer, shared, pinDefaultQueryMode);

    private MongoTestStore(string name, TestServer testServer, bool shared, bool pinDefaultQueryMode)
        : base(name, shared)
    {
        TestServer = testServer;
        _pinDefaultQueryMode = pinDefaultQueryMode;
    }

    protected override DbContext CreateDefaultContext()
        => throw new NotSupportedException();

    // MONGODB_EF_QUERY_MODE=NativeOnly|DriverLinq (or the alias MONGODB_EF_NATIVE_ONLY=1) flips every spec context
    // to that MongoQueryMode; see TestQueryMode (the single resolver, shared with the functional suite).
    //
    // TODO(EF-417): this switch and the functional Native* parity tests use driver-LINQ as the oracle; retiring the
    // fallback needs a replacement oracle (in-memory LINQ or pinned result sets).
    //
    // A store created with pinDefaultQueryMode ignores that switch and always runs the default query mode (an explicit
    // UseQueryMode wins over the process default). The suites ported in #35 are baselined in the default mode only.
    public override DbContextOptionsBuilder AddProviderOptions(DbContextOptionsBuilder builder)
        => builder.UseMongoDB(TestServer.Client, Name,
            _pinDefaultQueryMode
                ? o => o.UseQueryMode(MongoQueryMode.Native)
                : TestQueryMode.Current != MongoQueryMode.Native
                    ? o => o.UseQueryMode(TestQueryMode.Current)
                    : null);

#if !EF8
    protected override async Task InitializeAsync(Func<DbContext> createContext, Func<DbContext, Task>? seed,
        Func<DbContext, Task>? clean)
    {
        await base.InitializeAsync(createContext, seed, clean).ConfigureAwait(false);

        using var context = createContext();
        var databaseCreator = context.GetService<IDatabaseCreator>();
        if (!await databaseCreator.EnsureCreatedAsync().ConfigureAwait(false)) // Create or update indexes
        {
            // Seed for tests even if we didn't create the database.
            await ((MongoDatabaseCreator)databaseCreator).SeedFromModelAsync().ConfigureAwait(false);
        }
    }
#else
    protected override void Initialize(Func<DbContext> createContext, Action<DbContext>? seed, Action<DbContext>? clean)
    {
        base.Initialize(createContext, seed, clean);

        using var context = createContext();
        var databaseCreator = context.GetService<IDatabaseCreator>();
        if (!databaseCreator.EnsureCreated()) // Create or update indexes
        {
            // Seed for tests even if we didn't create the database.
            ((MongoDatabaseCreator)databaseCreator).SeedFromModel();
        }
    }

    // EF resets the store before each seed; the non-shared tests of a class all use one database (<fixture>_NonShared) and rely
    // on this, so documents left by one test must not be seen by the next (order-dependent "more than one element" failures).
    public override void Clean(DbContext context)
        => TestServer.Client.DropDatabase(Name);
#endif

#if EF11
    public override Task CleanAsync(DbContext context, bool createTables = true)
#else
    public override Task CleanAsync(DbContext context)
#endif
        => TestServer.Client.DropDatabaseAsync(Name);
}
