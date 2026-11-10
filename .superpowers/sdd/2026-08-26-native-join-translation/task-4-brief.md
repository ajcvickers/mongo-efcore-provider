### Task 4: Wire `MongoJoinBinder` into `TranslateJoinCore`

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs:1630-1704` (`TranslateJoinCore`)
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs` (new — first smoke test only; the full suite is Task 7)

**Interfaces:**
- Consumes: `MongoJoinBinder.TryBindJoin` (Task 3).
- Produces: `TranslateJoinCore` now attempts native binding for every eligible join before falling through to its existing (unchanged) driver-LINQ-capable shaper substitution — native binding failure is invisible to the rest of the method, which proceeds exactly as before.

- [ ] **Step 1: Write a failing functional smoke test for the simplest native shape**

```csharp
// tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs
/* Copyright 2023-present MongoDB Inc.
 * ... (standard header, copy from NativeSelectManyTests.cs)
 */

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

public class NativeJoinTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    // Follow the exact fixture/entity/CreateContext(WithLogging) helper pattern already established in
    // NativeSelectManyTests.cs in this same directory for constructing `db`, seeding, and reading back
    // MQL via spyLogger — do not invent a different harness.

    [Fact]
    public void Flattened_scalar_join_goes_native_with_correct_results_and_mql()
    {
        var seed = SeedOwnersAndOrders(); // one-time helper: two related entity sets with an FK
        using var db = CreateContextWithLogging(seed, MongoQueryMode.NativeOnly,
            nameof(Flattened_scalar_join_goes_native_with_correct_results_and_mql), out var spyLogger);

        var result = db.Owners
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .AsEnumerable()
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        // Succeeding under NativeOnly is itself the "went native" signal (Query/AGENTS.md).
        Assert.Equal(expected, result);

        var message = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        Assert.Contains("\"$lookup\"", message);
        Assert.Contains("\"$unwind\"", message);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"`
Expected: FAIL — throws `NativeTranslationNotSupportedException` under `NativeOnly` (today's "joins are not natively representable" decline).

- [ ] **Step 3: Wire the binder call into `TranslateJoinCore`**

In `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`, immediately after the existing `RebindInnerShaperToOuterQuery` call (around line 1694-1695) and before the `newResultSelector`/`return` lines:

```csharp
        var reboundInnerShaper = RebindInnerShaperToOuterQuery(
            inner.ShaperExpression, innerQueryExpression, outerQueryExpression, outerKeySelector, innerKeySelector, joinInfo);

        // NEW: attempt native binding for a genuine two-sided join (Include-shaped joins are confirmed
        // separately, later, by TryConfirmReferenceInclude — this call is a no-op for that shape since
        // TryBindJoin's result-selector classification only recognizes a NewExpression/MemberInit body,
        // never an IncludeExpression). A binding failure here is not a decline: the query still falls back
        // to the unchanged driver-LINQ bridge below via the ordinary Route == Fallback path, exactly as
        // before this task landed.
        MongoJoinBinder.TryBindJoin(outerQueryExpression, innerQueryExpression, resultSelector, joinInfo);

        var newResultSelector = ReplacingExpressionVisitor.Replace(
            resultSelector.Parameters[0], outer.ShaperExpression,
            ReplacingExpressionVisitor.Replace(
                resultSelector.Parameters[1], reboundInnerShaper!,
                resultSelector.Body));

        return outer.UpdateShaperExpression(newResultSelector);
```

Add the `using MongoDB.EntityFrameworkCore.Query.NativeTranslation;` directive at the top of the file if not already present (check first — `MongoQueryableMethodTranslatingExpressionVisitor.cs` already references other `NativeTranslation` types like `NativeSelectManyBinder`, so this using is very likely already there).

- [ ] **Step 4: Run the test — expect it to still fail, for a DIFFERENT reason**

Run the same command as Step 2.
Expected: `TryBindJoin` now returns `true` and registers the `$lookup`, but nothing downstream yet knows the select is natively representable for a join shape — `MongoSelectLowerer.AppendLookupStages`'s existing guard (`query.IsJoinQuery && lookups.Count < query.InnerCollections.Count`) still throws, because that guard doesn't yet know about this new lookup. This confirms Task 3's binder ran correctly; Task 5 closes the loop.

- [ ] **Step 5: Commit this step even though the smoke test doesn't fully pass yet — Tasks 5 and 6 complete the wiring**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs
git commit -m "EF-392: wire MongoJoinBinder into TranslateJoinCore"
```

(The build must pass even though the new functional test is still red — the sanity/build gate is about compilation and no regressions in *other* tests, not this in-progress feature's own not-yet-complete test. Re-run the untouched suites — `NativeSelectManyTests`, `NativeCastTests` or similar — as a spot-check that nothing else broke.)

---

