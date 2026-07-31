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

using System;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// A Join/GroupJoin/LeftJoin whose INNER sequence pages itself (Skip/Take) is declined by the provider, because
/// the MongoDB driver's LINQ provider mistranslates it — CSHARP-6017 — by folding the uncorrelated inner's
/// $sort/$skip/$limit into the CORRELATED $lookup sub-pipeline, where they run per-outer-row over a key-matched
/// subset of at most one document. The fallback therefore returns silently WRONG rows, so the shape must
/// hard-decline rather than fall back.
/// TODO(CSHARP-6017): delete this whole file when the driver is fixed — see the removal checklist in
/// docs/superpowers/specs/2026-07-31-groupby-join-uncorrelated-inner-decline-design.md §2.6. The tripwire test
/// at the bottom is what announces the fix.
/// </summary>
[XUnitCollection("QueryTests")]
public class NativeJoinPagedInnerDeclineTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    // The correct answer for PagedInnerJoin below: Regions ordered by Country are FR, UK, US; Take(2) keeps
    // FR and UK; the orders in those two countries are FR/300, UK/50, UK/25.
    private static readonly string[] CorrectRows = ["FR:EU", "UK:EU", "UK:EU"];

    // What the CSHARP-6017 fold returns instead: the $sort/$limit run inside the per-order $lookup, where every
    // order's single key match survives, so all five orders join.
    private const int FoldedWrongRowCount = 5;

    private static string[] PagedInnerJoin(PagedJoinDbContext db)
        => db.Orders
            .Join(db.Regions.OrderBy(r => r.Country).Take(2),
                o => o.Country, r => r.Country, (o, r) => new { o.Country, r.Continent })
            .AsEnumerable()
            .Select(x => x.Country + ":" + x.Continent)
            .OrderBy(s => s)
            .ToArray();

    [Fact]
    public void Join_with_paged_outer_still_runs_and_is_correct()
    {
        // CONTROL for an over-broad predicate that looks at the OUTER side too. Paging on the outer is emitted
        // at pipeline TOP LEVEL, before the $lookup, and is correct — it must keep working.
        using var db = CreateContext(MongoQueryMode.Native, nameof(Join_with_paged_outer_still_runs_and_is_correct));

        var rows = db.Orders.OrderBy(o => o.Amount).Take(2)
            .Join(db.Regions, o => o.Country, r => r.Country, (o, r) => new { o.Country, r.Continent })
            .AsEnumerable()
            .Select(x => x.Country + ":" + x.Continent)
            .OrderBy(s => s)
            .ToArray();

        // The two cheapest orders are UK/25 and UK/50.
        Assert.Equal(["UK:EU", "UK:EU"], rows);
    }

    [Fact]
    public void Join_with_reshaped_unpaged_inner_still_runs_and_is_correct()
    {
        // CONTROL for an over-broad predicate keyed on "the inner is a reshaping subquery". Driver 3.10 folds an
        // unpaged inner's $sort (or nothing at all) into the $lookup sub-pipeline, which is BENIGN: order within
        // a single-document key match is a no-op. Measured correct; must not be declined.
        using var db = CreateContext(MongoQueryMode.Native,
            nameof(Join_with_reshaped_unpaged_inner_still_runs_and_is_correct));

        var rows = db.Orders
            .Join(db.Regions.OrderBy(r => r.Country).Select(r => new { r.Country, r.Continent }),
                o => o.Country, r => r.Country, (o, r) => new { o.Country, r.Continent })
            .AsEnumerable()
            .Select(x => x.Country + ":" + x.Continent)
            .OrderBy(s => s)
            .ToArray();

        Assert.Equal(["FR:EU", "UK:EU", "UK:EU", "US:NA", "US:NA"], rows);
    }

    [Fact]
    public void Join_with_paged_inner_still_runs_under_driver_linq()
    {
        // Explicit DriverLinq is the user's documented opt-in to the previous path, wrong-data caveat included —
        // exactly as for the GroupBy+Join decline. It must never throw NativeTranslationNotSupportedException.
        using var db = CreateContext(MongoQueryMode.DriverLinq,
            nameof(Join_with_paged_inner_still_runs_under_driver_linq));

        var ex = Record.Exception(() => PagedInnerJoin(db));

        Assert.IsNotType<NativeTranslationNotSupportedException>(ex);
    }

    [Fact]
    public void Driver_still_folds_a_paged_join_inner_into_the_lookup_subpipeline_CSHARP_6017()
    {
        // EXPIRY TRIPWIRE, not a desired behavior. It pins the CSHARP-6017 driver defect that the provider guard
        // exists for, using the only mode that still reaches the driver. The CORRECT answer is CorrectRows (3
        // rows); the driver returns 5 because it folds $sort/$limit into the correlated $lookup sub-pipeline.
        //
        // WHEN THIS TEST FAILS, THE DRIVER HAS BEEN FIXED. Follow the removal checklist in
        // docs/superpowers/specs/2026-07-31-groupby-join-uncorrelated-inner-decline-design.md §2.6:
        // delete this file, the HasPagingAnywhere block in TranslateJoinCore, MongoSelectDefinition's
        // MarkPagedJoinInnerFallbackUnsafe/IsPagedJoinInnerFallbackUnsafe/HasPagingAnywhere, collapse
        // IsFallbackWrongData back to IsGroupByFallbackUnsafe, and revert the spec-suite retargets. Do NOT
        // delete PropagateFallbackWrongDataFrom — it fixes an unrelated EF-344 nesting hole.
        using var db = CreateContext(MongoQueryMode.DriverLinq,
            nameof(Driver_still_folds_a_paged_join_inner_into_the_lookup_subpipeline_CSHARP_6017));

        var rows = PagedInnerJoin(db);

        Assert.Equal(FoldedWrongRowCount, rows.Length);
        Assert.NotEqual(CorrectRows, rows);
    }

    [Fact]
    public void Join_with_paged_inner_declines_under_native()
    {
        using var db = CreateContext(MongoQueryMode.Native, nameof(Join_with_paged_inner_declines_under_native));

        Assert.Throws<NativeTranslationNotSupportedException>(() => PagedInnerJoin(db));
    }

    [Fact]
    public void Join_with_paged_inner_declines_under_native_only()
    {
        using var db = CreateContext(MongoQueryMode.NativeOnly,
            nameof(Join_with_paged_inner_declines_under_native_only));

        Assert.Throws<NativeTranslationNotSupportedException>(() => PagedInnerJoin(db));
    }

    [Fact]
    public void Join_with_paged_inner_never_returns_the_wrong_rows_under_native()
    {
        // MUTATION PIN for the data, deliberately NOT phrased as "it throws" — that is the job of
        // Join_with_paged_inner_declines_under_native, and a wrong-rows assertion placed AFTER a decline
        // assertion in the same method is unreachable exactly when the guard is deleted. Here the data
        // comparison is the branch that RUNS under mutation: delete the guard and the query executes, returns
        // the folded 5 rows, and Assert.Equal fails. Only two outcomes are acceptable — a clean decline, or the
        // correct rows (which is also what makes this test survive the eventual driver fix).
        using var db = CreateContext(MongoQueryMode.Native,
            nameof(Join_with_paged_inner_never_returns_the_wrong_rows_under_native));

        string[]? rows = null;
        var ex = Record.Exception(() => rows = PagedInnerJoin(db));

        if (ex is null)
        {
            Assert.Equal(CorrectRows, rows);
        }
        else
        {
            Assert.IsType<NativeTranslationNotSupportedException>(ex);
        }
    }

    [Fact]
    public void GroupJoin_with_paged_inner_declines_under_native()
    {
        // The GroupJoin / flattened-left-join spelling routes through the same TranslateJoinCore on every EF
        // version (on EF8/EF9 a LeftJoin is written this way), so it must decline identically.
        using var db = CreateContext(MongoQueryMode.Native, nameof(GroupJoin_with_paged_inner_declines_under_native));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            (from o in db.Orders
             join r in db.Regions.OrderBy(x => x.Country).Take(2) on o.Country equals r.Country into rs
             from r in rs
             select new { o.Country, r.Continent }).ToArray());
    }

    private PagedJoinDbContext CreateContext(MongoQueryMode mode, string name)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ordersName = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "O" + suffix;
        var regionsName = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "R" + suffix;

        database.MongoDatabase.GetCollection<Order>(ordersName).InsertMany(
        [
            new() { Id = ObjectId.GenerateNewId(), Country = "US", Year = 2020, Amount = 100 },
            new() { Id = ObjectId.GenerateNewId(), Country = "US", Year = 2021, Amount = 200 },
            new() { Id = ObjectId.GenerateNewId(), Country = "UK", Year = 2020, Amount = 50 },
            new() { Id = ObjectId.GenerateNewId(), Country = "UK", Year = 2020, Amount = 25 },
            new() { Id = ObjectId.GenerateNewId(), Country = "FR", Year = 2021, Amount = 300 },
        ]);
        database.MongoDatabase.GetCollection<Region>(regionsName).InsertMany(
        [
            new() { Id = ObjectId.GenerateNewId(), Country = "US", Continent = "NA" },
            new() { Id = ObjectId.GenerateNewId(), Country = "UK", Continent = "EU" },
            new() { Id = ObjectId.GenerateNewId(), Country = "FR", Continent = "EU" },
        ]);

        return new PagedJoinDbContext(database, ordersName, regionsName, mode);
    }

    private class Order
    {
        public ObjectId Id { get; set; }
        public string Country { get; set; } = "";
        public int Year { get; set; }
        public decimal Amount { get; set; }
    }

    private class Region
    {
        public ObjectId Id { get; set; }
        public string Country { get; set; } = "";
        public string Continent { get; set; } = "";
    }

    private class PagedJoinDbContext : DbContext
    {
        private readonly string _ordersCollection;
        private readonly string _regionsCollection;

        public PagedJoinDbContext(
            TemporaryDatabaseFixture database, string ordersCollection, string regionsCollection, MongoQueryMode mode)
            : base(new DbContextOptionsBuilder<PagedJoinDbContext>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName,
                    b => b.UseQueryMode(mode))
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .Options)
        {
            _ordersCollection = ordersCollection;
            _regionsCollection = regionsCollection;
        }

        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Region> Regions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Order>().ToCollection(_ordersCollection);
            modelBuilder.Entity<Region>().ToCollection(_regionsCollection);
        }

        private sealed class IgnoreCacheKeyFactory : IModelCacheKeyFactory
        {
            private static int _count;
            public object Create(DbContext context, bool designTime) => Interlocked.Increment(ref _count);
        }
    }
}
