### Task 10 report: Require a real `MongoTransaction` when joining an ambient transaction

## Location correction applied

The brief names `MongoShapedQueryCompilingExpressionVisitor.cs`, but bulk execution now lives in
`src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` (per the task instructions'
correction). All production changes were made there instead. The read-side `as MongoTransaction` cast
in `TranslateQuery` (`Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`) was left untouched,
as instructed — `RequireBulkSession` throws before `body()` runs, so phase 1 never executes under a bad
ambient transaction.

## What changed and why

`RunInBulkTransaction` (line ~309) and `RunInBulkTransactionAsync` (line ~352) in
`MongoBulkOperationExecutor.cs` both had an "ambient transaction" fast path that trusted
`database.CurrentTransaction != null` and ran the two-phase body directly. The session used by both
phases (`GetBulkCollectionAndSession`, line 437) is obtained via `(database.CurrentTransaction as
MongoTransaction)?.Session`, which silently yields `null` for any `IDbContextTransaction`
implementation that isn't `MongoTransaction`. In that case both phases ran with `session == null` —
outside any transaction — while the orchestration code believed it was inside one. Since two-phase
correctness for authorization/tenancy-style predicates depends entirely on phase 1 and phase 2 sharing
one transaction (phase 2 only filters on `_id`, never re-evaluating the user's predicate), this was a
silent loss of the only safety net.

Added a new private static helper, `RequireBulkSession(DatabaseFacade database)`, placed immediately
above `RunInBulkTransaction`, and call it from both fast paths (in `RunInBulkTransaction` and
`RunInBulkTransactionAsync`) before invoking `body()`. It performs the same `as MongoTransaction` cast
and returns the session on success; on failure (null cast or null `Session`) it throws
`InvalidOperationException` instead of allowing the operation to proceed with no session.

## Exact guard message

```
This bulk delete or update requires a MongoDB transaction, but the ambient transaction on this
DbContext is not a MongoDB transaction with an active session. Open the transaction with
Database.BeginTransaction() on a MongoDB-backed context.
```

## New test

`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`:
`ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected`.

It seeds a context, casts `db.Database.GetService<IDbContextTransactionManager>()` to
`MongoTransactionManager` (reachable via `InternalsVisibleTo` on
`src/MongoDB.EntityFrameworkCore/MongoDB.EntityFrameworkCore.csproj:60`, same pattern as
`NorthwindBulkUpdatesMongoFixture.UseTransaction`), sets `manager.CurrentTransaction` to a stub
`NotAMongoTransaction`, then asserts a two-phase `ExecuteDelete()` (`OrderBy(...).Take(1)`) throws
`InvalidOperationException` containing "transaction". After the throw, it clears
`manager.CurrentTransaction = null` before asserting `db.Entities.Count() == 3` (unchanged) — per the
task instructions' guidance to not depend on the read path's own null-session behavior for the
post-throw assertion, even though that would also happen to work.

### Observed failure before the fix (quoted)

```
MongoDB.EntityFrameworkCore.FunctionalTests.Query.ExecuteDeleteTests.ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected [FAIL]
  Failed MongoDB.EntityFrameworkCore.FunctionalTests.Query.ExecuteDeleteTests.ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected [300 ms]
  Error Message:
   Assert.Throws() Failure: No exception was thrown
Expected: typeof(System.InvalidOperationException)
```

This confirms the failure was exactly the predicted bug — no exception, meaning the delete silently ran
to completion with `session == null` (one document was in fact deleted before the fix was applied) —
not an incidental compile or setup error.

## `IDbContextTransaction` members the stub had to implement

The brief's suggested `NotAMongoTransaction` stub only implemented `TransactionId`, `Commit`,
`Rollback`, `CommitAsync`, `RollbackAsync`, `Dispose`, `DisposeAsync`. Per the task instructions, I
checked the actual interface via reflection against the three pinned package versions
(`Versions.props`: EF8=8.0.27, EF9=9.0.16, EF10=10.0.8) rather than assuming the brief's list was
complete. All three versions expose the identical, larger surface:

- `Guid TransactionId { get; }`
- `bool SupportsSavepoints { get; }`
- `void Commit()`
- `void Rollback()`
- `Task CommitAsync(CancellationToken cancellationToken = default)`
- `Task RollbackAsync(CancellationToken cancellationToken = default)`
- `void CreateSavepoint(string name)`
- `Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default)`
- `void ReleaseSavepoint(string name)`
- `Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)`
- `void RollbackToSavepoint(string name)`
- `Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)`
- `void Dispose()` (from `IDisposable`)
- `ValueTask DisposeAsync()` (from `IAsyncDisposable`)

The final `NotAMongoTransaction` stub implements all of the above (savepoint methods are no-ops,
`SupportsSavepoints => false`), so it compiles cleanly and identically across EF8/EF9/EF10.

## Test commands and results per configuration

Builds (all clean, 0 warnings / 0 errors):

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    → Build succeeded
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    → Build succeeded
```

(EF8 build-only: the new test and all bulk code are inside `#if !EF8`, so no bulk tests run under EF8 —
this matches the existing convention, confirmed by grep of the `#if !EF8` guard at the top of both the
test file and `MongoBulkOperationExecutor.cs`.)

Targeted new test, before the fix, `Debug EF10`:

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~ExecuteDelete_two_phase_with_a_foreign_ambient_transaction"
→ FAILED (see quoted failure above)
```

After the fix, `Debug EF10`:

```
MONGODB_URI= ATLAS_URI= dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
→ Passed! - Failed: 0, Passed: 63, Skipped: 1, Total: 64

MONGODB_URI= ATLAS_URI= dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
→ Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181
```

`Debug EF9`:

```
MONGODB_URI= ATLAS_URI= dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
→ Passed! - Failed: 0, Passed: 64, Skipped: 1, Total: 65

MONGODB_URI= ATLAS_URI= dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
→ Passed! - Failed: 0, Passed: 169, Skipped: 0, Total: 169
```

The single `[Fact(Skip = ...)]` in both EF9/EF10 runs of `ExecuteDeleteTests` is the pre-existing,
intentionally-skipped `ExecuteDelete_two_phase_on_standalone_throws_actionable_error` (requires a
standalone deployment; unrelated to this change). The Northwind Bulk Updates suite ran its full
conformance set on both EF9 (169 cases) and EF10 (181 cases) with zero failures, confirming
`RequireBulkSession` accepts the real `MongoTransaction` the fixture's `UseTransaction` enlists via
`MongoTransactionManager.CurrentTransaction`, for every case (~181 on EF10).

MongoDB URI/Atlas URI were left unset for all runs; TestContainers booted `mongodb/mongodb-atlas-local`
as expected (no external server used).

## Anything noticed but deliberately not changed

- The second `as MongoTransaction` cast on the read side (`TranslateQuery` in
  `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`) has the identical shape and would
  independently yield a null session for a non-`MongoTransaction` ambient transaction. Per this task's
  scope, it is not guarded, because `RequireBulkSession` throwing before `body()` in both
  `RunInBulkTransaction`/`RunInBulkTransactionAsync` means phase 1 (which is what invokes
  `TranslateQuery`) is never reached with a bad ambient transaction for bulk operations. This cast is
  outside the scope of Task 10 and was left untouched.
- `RunInBulkTransaction`'s overall shape (the try/catch/finally around `BeginTransaction`, the
  translation-then-transaction ordering) was left exactly as-is beyond the two-line insertion — Task 11
  is described as restructuring this method (hoisting `BeginTransaction` into its own mapped try,
  moving translation out of the transaction body), and pre-emptively touching more than the ambient
  fast path would work against that.
- The unrelated 1-test difference in `ExecuteDeleteTests|ExecuteUpdateTests|BulkOperationsCommandTests`
  counts between EF9 (65 total) and EF10 (64 total) is pre-existing EF-version-conditional test
  structure in `ExecuteUpdateTests.cs`, unrelated to this change; not investigated further as out of
  scope.
- No other production code, tests, or plan documents were touched. `GetSetElementKey`,
  `RebindSetterValueParameters`, the two setter guards, the redaction gate (`RenderBulkCommand`), and
  surrounding formatting in `MongoBulkOperationExecutor.cs` are untouched — confirmed via `git diff`
  showing only the two guard-call insertions and the new `RequireBulkSession` method.

## Commit

One commit, message exactly:

```
fix: require a usable MongoDB session when joining an ambient transaction for bulk ops
```

---

## Fix round 1

Review approved the original change (spec met, no Critical/Important defects) and raised two Minor items.
Both addressed below.

### 1. Cover the async guard

The original test only drove `ExecuteDelete()` → `RunInBulkTransaction`. There was no coverage of
`ExecuteDeleteAsync()` → `RunInBulkTransactionAsync` with a foreign ambient transaction, so the async
fast path's `RequireBulkSession` call was unverified by a negative test (only the Northwind conformance
suite's *positive* case exercised the async path).

Converted `ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected`
from `[Fact]` to `[Theory]` with `[InlineData(false)]` / `[InlineData(true)]`, following the sync/async
`async` bool parameter pattern already used elsewhere in the file (e.g.
`ExecuteDelete_logs_ExecutedBulkDelete_event_with_count`). The async branch uses
`await Assert.ThrowsAsync<InvalidOperationException>(() => query.ExecuteDeleteAsync())`; the sync branch
is unchanged. The "data unchanged" assertion (`Assert.Equal(3, db.Entities.Count())`, after clearing the
foreign `CurrentTransaction`) runs identically for both cases.

One fix was needed beyond the brief's shape: `CreateSeededContext()`'s default collection name comes from
`[CallerMemberName]`, which is identical for both `[InlineData]` invocations of the same `[Theory]` method.
Without a discriminator, both cases seeded into the *same* collection, and the async case (running after
the sync case) saw 6 documents instead of 3 (3 leftover from the first invocation + 3 freshly seeded), which
surfaced as a spurious `Assert.Equal(3, ...)` failure with `Actual: 6` — not a guard problem, purely a test
setup collision. Fixed by passing an explicit collection name that includes `async` into `CreateSeededContext`:

```csharp
using var db = CreateSeededContext(
    $"{nameof(ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected)}{async}");
```

No production code (`MongoBulkOperationExecutor.cs`) changed for this item — the guard's logic and message
are untouched.

### 2. Correct the report's unverified claim

The original report stated, of the pre-fix failing test: "one document was in fact deleted before the fix
was applied" — presented as an observation, but the quoted `Assert.Throws` failure output doesn't actually
show a document count (the sync-only test at the time short-circuited on "No exception was thrown" before
reaching `Assert.Equal(3, ...)`), so this was an inference from the code path, not a demonstrated fact.

Rather than reword it as a hedged inference, I established it directly, per the coordinator's preferred
resolution:

1. Temporarily commented out both `RequireBulkSession(database);` call sites in
   `MongoBulkOperationExecutor.cs` (`RunInBulkTransaction` and `RunInBulkTransactionAsync`), restoring the
   original silently-runs-unprotected behavior.
2. Temporarily rewrote the new theory test's body (in-memory only, never committed) to capture the actual
   delete count and remaining count via intentionally-wrong expected values (`Assert.Equal(-999, ...)`),
   so the real values would surface in the xUnit failure message.
3. Built and ran `--filter "FullyQualifiedName~ExecuteDelete_two_phase_with_a_foreign_ambient_transaction"`
   under `Debug EF10`. Both cases failed with the same shape:

   ```
   ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected(async: True) [FAIL]
     Error Message:
      Assert.Equal() Failure: Values differ
   Expected: -999
   Actual:   1

   ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected(async: False) [FAIL]
     Error Message:
      Assert.Equal() Failure: Values differ
   Expected: -999
   Actual:   1
   ```

   This confirms: with the guard removed, both the sync and async two-phase paths silently delete exactly
   one document (matching the query's `OrderBy(...).Take(1)`), with no exception raised in either case —
   i.e., the operation truly executes to completion unprotected, for both entry points.
4. Restored `MongoBulkOperationExecutor.cs` via `git checkout --` (it was already committed at the correct
   guarded state, so this reverted cleanly to zero diff — confirmed via `git diff --stat`) and restored the
   test file to its correct final Theory form (throw-based assertions, no `-999` probes). Nothing from the
   experiment was committed; `git diff --stat` before the fix-round commit shows only the intended
   Theory-conversion diff to `ExecuteDeleteTests.cs`.

The report's earlier sentence should be read as superseded by this measured result: under the pre-fix
(guard-less) code, both `ExecuteDelete()` and `ExecuteDeleteAsync()` against a foreign ambient transaction
delete exactly 1 document silently, confirmed by direct measurement rather than inferred from the code path.

### Re-run results (fix round 1)

Builds:

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded, 0 Warnings, 0 Errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    → Build succeeded, 0 Warnings, 0 Errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    → Build succeeded, 0 Warnings, 0 Errors
```

New theory, both cases, `Debug EF10`:

```
MONGODB_URI= ATLAS_URI= dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~ExecuteDelete_two_phase_with_a_foreign_ambient_transaction"
→ Passed! - Failed: 0, Passed: 2, Skipped: 0, Total: 2
```

`ExecuteDeleteTests`/`ExecuteUpdateTests`/`BulkOperationsCommandTests`, `Debug EF10`:

```
MONGODB_URI= ATLAS_URI= dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
→ Passed! - Failed: 0, Passed: 64, Skipped: 1, Total: 65
```

(64 passed vs. the original round's 63 — exactly the one extra `[InlineData]` case added by the Theory
conversion. The one skip is the pre-existing, intentionally-skipped standalone-deployment test.)

`NorthwindBulkUpdatesMongoTest`, `Debug EF10`:

```
MONGODB_URI= ATLAS_URI= dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
→ Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181
```

New theory, single `Debug EF9` run (per coordinator's instruction — one EF9 confirmation is enough):

```
MONGODB_URI= ATLAS_URI= dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~ExecuteDelete_two_phase_with_a_foreign_ambient_transaction"
→ Passed! - Failed: 0, Passed: 2, Skipped: 0, Total: 2
```

All environment variables (`MONGODB_URI`, `ATLAS_URI`) left unset throughout, as before.

### Scope confirmation

- Guard logic and message in `MongoBulkOperationExecutor.cs` unchanged — `git diff` against the prior
  commit for that file is empty.
- `RunInBulkTransaction`'s broader structure untouched (left for Task 11).
- Read-side `TranslateQuery` cast in `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
  untouched.
- Only file with a real diff for this fix round: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`.
