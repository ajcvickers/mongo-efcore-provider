### Task 7: Reducer after a row-count-preserving `$lookup`

Closes gap **E** (2 tests).

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQueryExpression.Lookup.cs` (new helper)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeCardinalityBinder.cs` (`TryBindReducer`, the `HasConfirmedJoinLookup` guard ~line 67)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` (the two "every join 1:1-safe" loops in `IsSingleEligibleNativeJoinScope`, ~lines 1136–1180: a pure refactor onto the helper)
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeReducerAfterReferenceLookupTests.cs`
- Modify: `tests/.../FunctionalTests/Query/NativeJoinTests.cs` (append one required-navigation guard test)
- Modify (baselines): `NorthwindAggregateOperatorsQueryMongoTest.cs`

**Interfaces:**
- Produces: `internal bool MongoQueryExpression.AreAllJoinsRowCountPreserving()`.

Why it is safe: a left-outer join over a reference (non-collection) navigation lowers to `$lookup` + `$unwind(preserveNullAndEmptyArrays: true)`, which neither drops nor multiplies rows. A reducer's `$limit` placed before it is therefore equivalent to one placed after it. That is the same 1:1 argument `IsSingleEligibleNativeJoinScope` already relies on for paging. If a `Where` or `OrderBy` reached the joined side first (`JoinInnerAccessConfirmed`), `ActiveOps` already routes the `$limit` into `PostJoinOps`, after the lookup. And `PostLookupPagingOps` deferral only happens for joins that are *not* 1:1-safe, so the two cannot combine here.

- [ ] **Step 1: Pure refactor first — add the helper and use it in `IsSingleEligibleNativeJoinScope`.** In `MongoQueryExpression.Lookup.cs`:

```csharp
    /// <summary>
    /// Whether every recorded join lowers to a row-count-preserving <c>$lookup</c>/<c>$unwind</c>: a left-outer join
    /// over a reference (non-collection) navigation, whose <c>$unwind</c> (<c>preserveNullAndEmptyArrays: true</c>)
    /// neither drops nor multiplies rows — so a <c>$skip</c>/<c>$limit</c> placed before the join is equivalent to the
    /// same stage placed after it.
    /// </summary>
    internal bool AreAllJoinsRowCountPreserving()
    {
        foreach (var join in Joins)
        {
            if (join.Lookup is not { } lookup || !(join.IsLeftOuter && lookup.Navigation is { IsCollection: false }))
                return false;
        }

        return true;
    }
```

Replace the reducer-branch loop in `IsSingleEligibleNativeJoinScope` with `if (!mongoQueryExpression.AreAllJoinsRowCountPreserving()) return false;`. Replace the paging-branch loop plus the `everyJoinPreLookupSafe` local with `var everyJoinPreLookupSafe = mongoQueryExpression.AreAllJoinsRowCountPreserving();`. Keep all comments.

Build, then run `dotnet test $FUNC -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoin"` and the spec Query sweep (normal mode). They must be identical to before. Commit on its own: `git commit -m "EF-322: extract MongoQueryExpression.AreAllJoinsRowCountPreserving (refactor)"`.

- [ ] **Step 2: Failing functional tests.** Create `NativeReducerAfterReferenceLookupTests.cs`, modeled on `NativeJoinTests`' context (header/BOM from that file). The navigation is **optional**, a nullable FK, so EF emits a left-outer join:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// First/FirstOrDefault/Single composed after a projection through an OPTIONAL reference navigation
/// (<c>OrderBy(..).Select(p =&gt; p.Customer!.City).First()</c> — EF's Northwind <c>OfType_Select</c>). The join is a
/// left-outer reference <c>$lookup</c>, which preserves the row count, so the reducer's <c>$limit</c> is safe to emit
/// before it.
/// </summary>
[XUnitCollection("QueryTests")]
public class NativeReducerAfterReferenceLookupTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Customer
    {
        public ObjectId Id { get; set; }
        public string City { get; set; } = "";
    }

    public class Purchase
    {
        public ObjectId Id { get; set; }
        public int Seq { get; set; }
        public ObjectId? CustomerId { get; set; }
        public Customer? Customer { get; set; }
    }

    private sealed class Ctx(TemporaryDatabaseFixture database, string customers, string purchases, MongoQueryMode mode)
        : DbContext(BuildOptions(database, mode))
    {
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Purchase> Purchases { get; set; } = null!;

        private static DbContextOptions BuildOptions(TemporaryDatabaseFixture database, MongoQueryMode mode)
        {
            var builder = new DbContextOptionsBuilder<Ctx>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
            return builder.Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().ToCollection(customers);
            modelBuilder.Entity<Purchase>(b =>
            {
                b.ToCollection(purchases);
                b.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).IsRequired(false);
            });
        }

        private sealed class IgnoreCacheKeyFactory : IModelCacheKeyFactory
        {
            private static int _count;
            public object Create(DbContext context, bool designTime) => Interlocked.Increment(ref _count);
        }
    }

    /// <summary>Seq 1 has a NULL FK, Seq 2 a DANGLING FK, Seq 3 a matched FK ("London").</summary>
    private Func<MongoQueryMode, Ctx> Seed(string name)
    {
        var suffix = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        var london = new Customer { Id = ObjectId.GenerateNewId(), City = "London" };
        database.MongoDatabase.GetCollection<Customer>("C" + suffix).InsertOne(london);
        database.MongoDatabase.GetCollection<Purchase>("P" + suffix).InsertMany(
        [
            new Purchase { Id = ObjectId.GenerateNewId(), Seq = 1, CustomerId = null },
            new Purchase { Id = ObjectId.GenerateNewId(), Seq = 2, CustomerId = ObjectId.GenerateNewId() },
            new Purchase { Id = ObjectId.GenerateNewId(), Seq = 3, CustomerId = london.Id },
        ]);
        return mode => new Ctx(database, "C" + suffix, "P" + suffix, mode);
    }

    // Review Focus 4: the first row has a null FK — First() must return its (null) City, not skip it.
    [Fact]
    public void First_over_optional_reference_nav_projection_goes_native_and_keeps_null_fk_row()
    {
        var create = Seed(nameof(First_over_optional_reference_nav_projection_goes_native_and_keeps_null_fk_row));
        var result = NativeModeAssert.NativeAndParity(mode =>
        {
            using var db = create(mode);
            return [db.Purchases.OrderBy(p => p.Seq).Select(p => p.Customer!.City).First()];
        });
        Assert.Equal([null], result);
    }

    [Fact]
    public void FirstOrDefault_after_skip_over_dangling_fk_row_goes_native()
    {
        var create = Seed(nameof(FirstOrDefault_after_skip_over_dangling_fk_row_goes_native));
        var result = NativeModeAssert.NativeAndParity(mode =>
        {
            using var db = create(mode);
            return [db.Purchases.OrderBy(p => p.Seq).Skip(1).Select(p => p.Customer!.City).FirstOrDefault()];
        });
        Assert.Equal([null], result);
    }

    [Fact]
    public void Single_over_matched_row_goes_native()
    {
        var create = Seed(nameof(Single_over_matched_row_goes_native));
        var result = NativeModeAssert.NativeAndParity(mode =>
        {
            using var db = create(mode);
            return [db.Purchases.Where(p => p.Seq == 3).Select(p => p.Customer!.City).Single()];
        });
        Assert.Equal(["London"], result);
    }

    [Fact]
    public void Last_over_optional_reference_nav_projection_goes_native()
    {
        var create = Seed(nameof(Last_over_optional_reference_nav_projection_goes_native));
        var result = NativeModeAssert.NativeAndParity(mode =>
        {
            using var db = create(mode);
            return [db.Purchases.OrderBy(p => p.Seq).Select(p => p.Customer!.City).Last()];
        });
        Assert.Equal(["London"], result);
    }
}
```

And append this guard to `NativeJoinTests`. `Order.Owner` is a **required** navigation (non-nullable FK ⇒ inner join ⇒ not row-count-preserving):

```csharp
    [Fact]
    public void First_over_required_reference_nav_projection_with_dangling_first_row_is_correct_in_every_mode()
    {
        // Order.OwnerId is non-nullable, so EF inner-joins: the dangling first order is DROPPED by the join and
        // First() must answer with the matched order's owner. A $limit emitted before this (non-row-count-preserving)
        // $lookup would keep only the dangling order and return nothing — the reducer must decline (or be correct).
        var seed = SeedDanglingOrderFirstThenMatched();
        foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            using var db = CreateContext(seed, mode, nameof(First_over_required_reference_nav_projection_with_dangling_first_row_is_correct_in_every_mode) + mode);
            Assert.Equal("Alice", db.Orders.OrderBy(o => o.Region).Select(o => o.Owner!.Name).First());
        }
    }
```

Before writing that test, check `SeedDanglingOrderFirstThenMatched` (~line 2395). It must put the dangling order first under `OrderBy(o => o.Region)` and name the matched owner `"Alice"`. If it doesn't, adjust the ordering key or the expected name to match that helper; don't change the helper.

- [ ] **Step 3: Run → the four new `NativeReducerAfterReferenceLookupTests` fail** with `NativeTranslationNotSupportedException`. The `NativeJoinTests` guard passes already.

- [ ] **Step 4: Implement.** In `TryBindReducer` replace

```csharp
        if (select.HasConfirmedJoinLookup)
            return false;
```

with

```csharp
        // ...unless every confirmed join is row-count-preserving (a left-outer REFERENCE-navigation $lookup, e.g.
        // `OrderBy(o => o.OrderID).Select(o => o.Customer.City).First()` — EF's OfType_Select): its $unwind
        // (preserveNullAndEmptyArrays: true) neither drops nor multiplies rows, so a $limit before it is exactly
        // equivalent to one after it — the same 1:1 argument IsSingleEligibleNativeJoinScope already relies on for
        // paging. (If a Where/OrderBy reached the join's Inner side first, ActiveOps already routes the $limit into
        // PostJoinOps, after the lookup, anyway.)
        if (select.HasConfirmedJoinLookup && !mongoQ.AreAllJoinsRowCountPreserving())
            return false;
```

Update the preceding comment's "(EF-392)" paragraph so it says the decline applies to joins that are **not** row-count-preserving.

- [ ] **Step 5: Run → pass.** Also run the whole `NativeJoinTests` class. `First_after_a_confirmed_join_declines_cleanly_under_NativeOnly` (a collection-nav join) must stay green.

- [ ] **Step 6: Spec tests** `OfType_Select OfType_Select_OfType_Select`: native-only. They may already match the existing baselines (the native MQL can be identical to the driver's). Regenerate only if `AssertMql` fails. Both modes green.

- [ ] **Step 7: Regression sweep** (−4). Also run `NorthwindMiscellaneousQueryMongoTest.Projection_take_projection`, `.Projection_skip_projection` and `.Projection_skip_take_projection` in both modes: they depend on the refactored predicate.

- [ ] **Step 8: Commit** — `git commit -m "EF-322: native reducer after a row-count-preserving reference lookup"`

---

