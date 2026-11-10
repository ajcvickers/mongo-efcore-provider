# Task 3 report: Unit-test `IsTransactionsUnsupported`

## What changed and why

The brief targeted `MongoShapedQueryCompilingExpressionVisitor.cs`, but per the task's Correction 1, the
predicate now lives in `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` (the bulk
execution code was extracted into Storage since the plan was written). All work was redirected there.

1. **`src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`**
   - Changed `IsTransactionsUnsupported(Exception exception)` from `private static` to `internal static`
     (signature unchanged) so the new unit tests can reach it. `InternalsVisibleTo` for
     `MongoDB.EntityFrameworkCore.UnitTests` was already declared in the csproj (per Correction 2, verified,
     not touched).
   - Updated the preceding comment to describe the *narrowed* behaviour (three failure shapes, matched
     conservatively so other `IllegalOperation` errors raised inside a transaction propagate untouched),
     matching the brief's Step 1 text verbatim, adapted only in file location.
   - Narrowed the predicate body (Step 4): removed the `MongoCommandException { Code: 20 }` arm entirely.
     `Code: 20` is `IllegalOperation` generically and is raised for many unrelated in-transaction failures
     (e.g. an unsupported aggregation stage), not just "this deployment doesn't support transactions". The
     transaction-number failure that arm was meant to catch is itself a `MongoException` whose message
     already matches the first arm ("Transaction numbers are only allowed..."), so nothing was lost. Final
     body:
     ```csharp
     internal static bool IsTransactionsUnsupported(Exception exception)
         => (exception is MongoException
             && exception.Message.Contains("Transaction numbers are only allowed", StringComparison.Ordinal))
            || (exception is NotSupportedException
                && exception.Message.Contains("does not support transactions", StringComparison.Ordinal));
     ```

2. **Created `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkTransactionErrorClassificationTests.cs`**
   (`#if !EF8`, `using MongoDB.EntityFrameworkCore.Storage;` per Correction 1). Four `[Fact]`s on a
   `public static class`, matching the sibling style in
   `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs`:
   - `Classifies_NotSupportedException_from_provider_transaction_startup` — plain `NotSupportedException`.
   - `Classifies_transaction_numbers_message_on_a_MongoException` — plain `MongoException` (no need for a
     real `MongoCommandException`; the narrowed predicate only inspects type + message for this arm).
   - `Does_not_classify_an_unrelated_IllegalOperation` — the one that must fail pre-fix; needs a real
     `MongoCommandException` with `Code == 20` (see construction details below).
   - `Does_not_classify_an_unrelated_exception` — plain `InvalidOperationException`.

   No redundant `using System;`/`using System.Linq;`/`using Xunit;` — `tests/Directory.Build.props` sets
   `<ImplicitUsings>enable</ImplicitUsings>`, and the Unit Tests project's `Usings.cs` globals `Xunit` (plus
   `MongoDB.EntityFrameworkCore.UnitTests.TestUtilities`) at the assembly level.

3. **`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`** — replaced the
   `Skip` reason on `ExecuteDelete_two_phase_on_standalone_throws_actionable_error` with the brief's Step 6
   text verbatim, so it points at the new unit tests instead of claiming coverage that didn't exist:
   ```csharp
   [Fact(Skip = "Requires a standalone (non-replica-set) MongoDB to exercise the end-to-end path; CI/local use "
       + "a replica set (atlas-local). The error classification itself is unit-tested server-free in "
       + "UnitTests/Query/BulkTransactionErrorClassificationTests.cs. EF-107")]
   ```

## Observed pre-fix failure (the point of this task)

Before narrowing the predicate (i.e. with the old body still containing
`exception is MongoCommandException { Code: 20 }`), running:

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~BulkTransactionErrorClassificationTests"
```

produced:

```
[xUnit.net 00:00:00.15]     MongoDB.EntityFrameworkCore.UnitTests.Query.BulkTransactionErrorClassificationTests.Does_not_classify_an_unrelated_IllegalOperation [FAIL]
  Failed MongoDB.EntityFrameworkCore.UnitTests.Query.BulkTransactionErrorClassificationTests.Does_not_classify_an_unrelated_IllegalOperation [4 ms]
  Error Message:
   Assert.False() Failure
Expected: False
Actual:   True
  Stack Trace:
     at MongoDB.EntityFrameworkCore.UnitTests.Query.BulkTransactionErrorClassificationTests.Does_not_classify_an_unrelated_IllegalOperation() in .../BulkTransactionErrorClassificationTests.cs:line 68

Failed!  - Failed:     1, Passed:     3, Skipped:     0, Total:     4, Duration: 19 ms
```

This is the intended assertion failure (`Assert.False` received `true` because the old `Code: 20` arm
matched), not a construction/compile error — the test built and ran, exercised the real predicate against a
real `MongoCommandException` with `Code == 20`, and got the wrong (old) answer. The other three tests passed
in this same run, confirming they were unaffected by the bug being demonstrated.

## Constructing the `MongoCommandException`

The brief's `CommandException` helper (using `MongoConnectionId`/`MongoServerId`) does not exist in the
driver version this repo pins (`CSharpDriverVersion` = `3.10.0` in `Versions.props`). I verified the actual
API by writing a small throwaway probe console app (in the scratchpad, referencing
`MongoDB.Driver` 3.10.0 via a `PackageReference`, not committed anywhere) and reflecting over
`typeof(MongoCommandException)`:

```
ctor: Void .ctor(MongoDB.Driver.Core.Connections.ConnectionId, System.String, MongoDB.Bson.BsonDocument)
ctor: Void .ctor(MongoDB.Driver.Core.Connections.ConnectionId, System.String, MongoDB.Bson.BsonDocument, MongoDB.Bson.BsonDocument)
ctor: Void .ctor(MongoDB.Driver.Core.Connections.ConnectionId, System.String, MongoDB.Bson.BsonDocument, MongoDB.Bson.BsonDocument, System.Exception)
ConnectionId ctor: Void .ctor(MongoDB.Driver.Core.Servers.ServerId)
ConnectionId ctor: Void .ctor(MongoDB.Driver.Core.Servers.ServerId, Int64)
ServerId ctor: Void .ctor(MongoDB.Driver.Core.Clusters.ClusterId, System.Net.EndPoint)
ClusterId ctor: Void .ctor()
ClusterId ctor: Void .ctor(Int32)
Code property: Int32 Code CanWrite=False
```

This confirms exactly the shape the task instructions anticipated:
`new MongoCommandException(connectionId, message, command, result)` with
`connectionId = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)))`, and
that `Code` is a read-only property (no settable backing field — it must come from the `result` document).

I then ran the constructed exception through the same probe to confirm `Code` really is populated from the
result document's `code` field rather than defaulting or coming from somewhere else:

```csharp
var result = new BsonDocument { { "ok", 0 }, { "code", 20 }, { "errmsg", "Stage not supported inside of a multi-document transaction: $unionWith" } };
var ex = new MongoCommandException(connectionId, "...", command, result);
// Output:
// Code = 20
// Message = Stage not supported inside of a multi-document transaction: $unionWith
// Is MongoException: True
```

This matched exactly, confirming `CommandException(code, message)` in the final test file builds a real
`MongoCommandException` whose `.Code` is genuinely `20` via the driver's own parsing of the `result`
document — not a mock or a hand-set field.

## Test commands and results

Build, all three configurations:
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"   # Build succeeded, 0 warnings, 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"   # Build succeeded, 0 warnings, 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"  # Build succeeded, 0 warnings, 0 errors
```
(EF8 build implicitly confirms the new test file's `#if !EF8` guard compiles out cleanly — no `Query` test
assembly failures under EF8.)

New unit tests, post-fix:
```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~BulkTransactionErrorClassificationTests"
# Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4, Duration: 13 ms
```

Full unit test project, post-fix (regression check):
```bash
cd tests/MongoDB.EntityFrameworkCore.UnitTests
dotnet test MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build
# Passed! - Failed: 0, Passed: 284, Skipped: 0, Total: 284, Duration: 197 ms
```

Bulk functional suites (MONGODB_URI and ATLAS_URI left unset; TestContainers used the already-pulled
`mongodb/mongodb-atlas-local` image):
```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
# ExecuteDelete_two_phase_on_standalone_throws_actionable_error [SKIP] (expected — unchanged skip, new reason)
# Passed! - Failed: 0, Passed: 42, Skipped: 1, Total: 43, Duration: 1 s (FunctionalTests.dll, net10.0)
```

No regressions in any suite.

## Files touched (single commit)

- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkTransactionErrorClassificationTests.cs` (new)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

Commit: `5e94714` — `fix: narrow transaction-unsupported classification and unit-test it server-free`
(exact message from the brief's Step 7, per the task instructions' explicit override of the repo's normal
"EF-1234: Description" commit convention for this task).

## Noticed but deliberately not changed

- `src/MongoDB.EntityFrameworkCore/MongoDB.EntityFrameworkCore.csproj:58` already has
  `<InternalsVisibleTo Include="MongoDB.EntityFrameworkCore.UnitTests" />` — confirmed per Correction 2,
  csproj left untouched.
- The brief's speculative `CommandException` helper (using `MongoConnectionId`/`MongoServerId`, which don't
  exist in driver 3.10.0 — those names belong to the legacy 2.x driver split between `MongoDB.Driver` and
  `MongoDB.Driver.Core`) was replaced with a helper using the real 3.10.0 types
  (`MongoDB.Driver.Core.Connections.ConnectionId`, `...Servers.ServerId`, `...Clusters.ClusterId`), per
  Correction 2/"Context the brief cannot know".
- Left the sibling `MongoNonQueryExpressionUnwrapTests.cs`'s explicit `using Xunit;` as precedent noted but
  did not copy it into the new file — the task instructions explicitly say not to add redundant usings for
  `System`/`System.Linq`/`Xunit` since `Usings.cs` and `ImplicitUsings` already cover them; I followed the
  explicit instruction over the (inconsistent) sibling-file precedent.
- Did not touch `MongoShapedQueryCompilingExpressionVisitor.cs` at all — grepped to confirm it no longer
  contains `IsTransactionsUnsupported` or any related predicate; the brief's original target location is
  stale, consistent with Correction 1.
- Did not modify anything else under `.superpowers/` besides this report file.
- Left `.claude`/other unrelated files untouched; `git status` before commit showed exactly the three
  intended files (two modified, one new), no stray changes swept in.

## Fix round 1 (review response)

Review approved the work overall, including independently confirming (against the driver source) that
dropping the bare `Code: 20` arm loses no real failure shape. Two findings addressed:

1. **Comment now describes shapes that don't match the code's two arms.** The prior comment still said
   "three shapes," listing "(2) a raw driver `MongoCommandException` (code 20 / IllegalOperation) whose
   message names the transaction-number restriction" as if it were a peer of "(3) a … message on any
   `MongoException`" — but the code has only two boolean arms, and (2)/(3) are the same arm; (2) was just
   describing a common concrete subtype of the exception the arm catches. Rewrote the comment in
   `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` (directly above
   `IsTransactionsUnsupported`) to describe the two arms the code actually has, and to mention the code-20
   `MongoCommandException` explicitly as *"just the concrete subtype the driver throws for this message;
   there is no separate check on the error code"* rather than as an independent shape. Kept the "match
   conservatively" rationale (now phrased as "on message content rather than on error code alone") so the
   reasoning for narrowness isn't lost.

2. **Test file misplaced relative to the project's own convention.** The predicate under test lives in
   `Storage/MongoBulkOperationExecutor`, but the test file sat in
   `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/`, while the UnitTests project already mirrors `src/`
   layout with a `Storage/` folder (`Storage/BsonBindingTests.cs`, `Storage/BsonTypeHelperTests.cs`). Moved
   the file with `git mv` to
   `tests/MongoDB.EntityFrameworkCore.UnitTests/Storage/BulkTransactionErrorClassificationTests.cs` and
   changed its namespace from `MongoDB.EntityFrameworkCore.UnitTests.Query` to
   `MongoDB.EntityFrameworkCore.UnitTests.Storage`. No other content in the file changed — same four
   `[Fact]`s, same `CommandException` helper, same `#if !EF8` guard.

Nothing else changed: the predicate body, the four test cases' bodies, the `ExecuteDeleteTests.cs`
skip-reason edit, and the round-1 commit message all stood up to review as-is.

### Covering tests re-run after the fix

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkTransactionErrorClassificationTests"
```
Output (trimmed to the relevant line):
```
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 15 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```
Output (trimmed):
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
Confirms the `#if !EF8` guard around the moved/renamed test file still compiles out cleanly under EF8, and
the four tests still pass at the new location/namespace under EF10. Functional suites were not re-run per
the coordinator's instruction (this round touches only a comment and a test file's location/namespace,
neither of which the functional suites exercise).

### Files touched in this round

- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` (comment only; predicate body
  unchanged)
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkTransactionErrorClassificationTests.cs` →
  `tests/MongoDB.EntityFrameworkCore.UnitTests/Storage/BulkTransactionErrorClassificationTests.cs` (`git mv`
  + namespace change only; test bodies unchanged)

Commit: see below.
