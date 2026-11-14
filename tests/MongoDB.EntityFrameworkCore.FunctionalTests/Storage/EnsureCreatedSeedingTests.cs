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

#if !EF8 // UseSeeding / UseAsyncSeeding were added in EF9.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Storage;

// EnsureCreated[Async] runs the delegates set with UseSeeding / UseAsyncSeeding, as relational providers do (#66).
public class EnsureCreatedSeedingTests
{
    private class Seeded
    {
        public ObjectId _id { get; set; }
        public string name { get; set; } = null!;
    }

    private static readonly ObjectId ModelDataId = ObjectId.Parse("64b7f0c2a1b2c3d4e5f60718");

    private static async Task<(SingleEntityDbContext<Seeded> Db, IMongoCollection<Seeded> Collection)> CreateContextAsync(
        Action<DbContextOptionsBuilder> configure,
        Action<ModelBuilder>? modelBuilderAction = null)
    {
        // A fresh database for each test: whether it existed decides the argument the delegate receives.
        var database = await TemporaryDatabaseFixture.CreateInitializedAsync();
        var collection = database.MongoDatabase.GetCollection<Seeded>("seeded");
        return (SingleEntityDbContext.Create(collection, modelBuilderAction, optionsBuilderAction: configure), collection);
    }

    private static void SeedOnce(DbContext context, string name)
    {
        var db = (SingleEntityDbContext<Seeded>)context;
        if (!db.Entities.Any())
        {
            db.Entities.Add(new Seeded { name = name });
            db.SaveChanges();
        }
    }

    private static async Task SeedOnceAsync(DbContext context, string name, CancellationToken cancellationToken)
    {
        var db = (SingleEntityDbContext<Seeded>)context;
        if (!await db.Entities.AnyAsync(cancellationToken))
        {
            db.Entities.Add(new Seeded { name = name });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EnsureCreated_runs_the_seeding_delegate_every_time_and_says_whether_the_database_was_created(bool async)
    {
        var calls = new List<bool>();
        DbContext? seen = null;

        var (db, collection) = await CreateContextAsync(ob => ob
            .UseSeeding((context, created) =>
            {
                seen = context;
                calls.Add(created);
                SeedOnce(context, "seeded");
            })
            .UseAsyncSeeding(async (context, created, cancellationToken) =>
            {
                seen = context;
                calls.Add(created);
                await SeedOnceAsync(context, "seeded", cancellationToken);
            }));
        using var disposable = db;

        Assert.True(async ? await db.Database.EnsureCreatedAsync() : db.Database.EnsureCreated());
        Assert.Same(db, seen);
        Assert.Equal("seeded", Assert.Single(collection.AsQueryable().ToList()).name);

        // The delegate runs again when nothing was created, and is the one that keeps itself idempotent.
        Assert.False(async ? await db.Database.EnsureCreatedAsync() : db.Database.EnsureCreated());
        Assert.Equal([true, false], calls);
        Assert.Single(collection.AsQueryable().ToList());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EnsureCreated_runs_the_seeding_delegate_after_model_data_is_written(bool async)
    {
        var seenNames = new List<string>();

        var (db, collection) = await CreateContextAsync(
            ob => ob
                .UseSeeding((context, _) =>
                {
                    seenNames.AddRange(((SingleEntityDbContext<Seeded>)context).Entities.Select(e => e.name));
                    SeedOnce(context, "from delegate");
                })
                .UseAsyncSeeding(async (context, _, cancellationToken) =>
                {
                    seenNames.AddRange(await ((SingleEntityDbContext<Seeded>)context).Entities.Select(e => e.name).ToListAsync(cancellationToken));
                    await SeedOnceAsync(context, "from delegate", cancellationToken);
                }),
            mb => mb.Entity<Seeded>().HasData(new Seeded { _id = ModelDataId, name = "from model" }));
        using var disposable = db;

        Assert.True(async ? await db.Database.EnsureCreatedAsync() : db.Database.EnsureCreated());

        Assert.Equal(["from model"], seenNames);
        Assert.Equal(["from model"], collection.AsQueryable().Select(e => e.name).ToList());
    }

    [Fact]
    public async Task EnsureCreated_without_seeding_delegates_is_unchanged()
    {
        var (db, collection) = await CreateContextAsync(_ => { });
        using var disposable = db;

        Assert.True(db.Database.EnsureCreated());
        Assert.False(db.Database.EnsureCreated());
        Assert.Empty(collection.AsQueryable().ToList());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EnsureCreated_seeding_delegate_runs_outside_a_transaction_so_works_with_retry_on_failure(bool async)
    {
        // The delegate is not wrapped in a transaction: a retrying strategy rejects a transaction begun outside it, so a wrapper would
        // make the delegate's own SaveChanges throw. Check from inside the delegate that the retrying strategy is active and that
        // no transaction is open, and that SaveChanges then works.
        var retrying = new List<bool>();
        var transactions = new List<bool>();
        void Observe(DbContext context)
        {
            retrying.Add(context.Database.CreateExecutionStrategy().RetriesOnFailure);
            transactions.Add(context.Database.CurrentTransaction != null);
        }

        var (db, collection) = await CreateContextAsync(ob =>
        {
            new MongoDbContextOptionsBuilder(ob).EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(50));
            ob.UseSeeding((context, _) =>
                {
                    Observe(context);
                    SeedOnce(context, "retried");
                })
                .UseAsyncSeeding((context, _, cancellationToken) =>
                {
                    Observe(context);
                    return SeedOnceAsync(context, "retried", cancellationToken);
                });
        });
        using var disposable = db;

        Assert.True(async ? await db.Database.EnsureCreatedAsync() : db.Database.EnsureCreated());

        Assert.Equal([true], retrying);
        Assert.Equal([false], transactions);
        Assert.Equal("retried", Assert.Single(collection.AsQueryable().ToList()).name);
    }

    [Fact]
    public async Task A_creator_built_with_the_original_constructor_runs_no_seeding_delegates()
    {
        var seeded = false;
        var (db, collection) = await CreateContextAsync(ob => ob
            .UseSeeding((_, _) => seeded = true)
            .UseAsyncSeeding((_, _, _) =>
            {
                seeded = true;
                return Task.CompletedTask;
            }));
        using var disposable = db;

        var creator = new MongoDatabaseCreator(
            db.GetService<IMongoClientWrapper>(),
            db.GetService<IDesignTimeModel>(),
            db.GetService<IUpdateAdapterFactory>(),
            db.GetService<IDatabase>(),
            db.GetService<IDiagnosticsLogger<DbLoggerCategory.Database>>());

        Assert.True(creator.EnsureCreated());
        Assert.False(await creator.EnsureCreatedAsync());
        Assert.False(seeded);
        Assert.Empty(collection.AsQueryable().ToList());
    }

    [Fact]
    public async Task EnsureCreated_throws_when_only_the_async_seeding_delegate_is_configured()
    {
        var (db, collection) = await CreateContextAsync(ob => ob
            .UseAsyncSeeding((context, _, cancellationToken) => SeedOnceAsync(context, "never", cancellationToken)));
        using var disposable = db;

        Assert.Throws<InvalidOperationException>(() => db.Database.EnsureCreated());
        Assert.Empty(collection.AsQueryable().ToList());
    }

    [Fact]
    public async Task EnsureCreatedAsync_throws_when_only_the_sync_seeding_delegate_is_configured()
    {
        var (db, collection) = await CreateContextAsync(ob => ob.UseSeeding((context, _) => SeedOnce(context, "never")));
        using var disposable = db;

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.Database.EnsureCreatedAsync());
        Assert.Empty(collection.AsQueryable().ToList());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EnsureCreated_propagates_an_exception_from_the_seeding_delegate(bool async)
    {
        var (db, _) = await CreateContextAsync(ob => ob
            .UseSeeding((_, _) => throw new ArgumentException("seed failed"))
            .UseAsyncSeeding((_, _, _) => throw new ArgumentException("seed failed")));
        using var disposable = db;

        var exception = async
            ? await Assert.ThrowsAsync<ArgumentException>(() => db.Database.EnsureCreatedAsync())
            : Assert.Throws<ArgumentException>(() => db.Database.EnsureCreated());
        Assert.Equal("seed failed", exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EnsureCreated_with_options_runs_the_seeding_delegate(bool async)
    {
        var calls = 0;
        var (db, collection) = await CreateContextAsync(ob => ob
            .UseSeeding((context, _) =>
            {
                calls++;
                SeedOnce(context, "options");
            })
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                calls++;
                await SeedOnceAsync(context, "options", cancellationToken);
            }));
        using var disposable = db;

        var options = new MongoDatabaseCreationOptions(CreateMissingIndexes: false);
        Assert.True(async ? await db.Database.EnsureCreatedAsync(options) : db.Database.EnsureCreated(options));

        Assert.Equal(1, calls);
        Assert.Single(collection.AsQueryable().ToList());
    }
}

#endif
