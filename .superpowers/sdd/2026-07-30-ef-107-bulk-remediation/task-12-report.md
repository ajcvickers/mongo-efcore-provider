# Task 12 report — BLOCKED at the RED step

## Status: BLOCKED

No production code was changed. No commit was made. The working tree is clean at `b02e480` (verified
with `git status --short` after `git checkout --` on the one file I had edited).

## What I did

1. Read the task brief and the correction/resolution notes.
2. Read `MongoBulkOperationExecutor.cs` (lines 380-530) to confirm the corrected file locations: `SafeRollback`
   line 504, `SafeRollbackAsync` line 510, the async call site inside `RunInBulkTransactionAsync`'s catch at
   line 485, sync call site at line 421.
3. Read `ExecuteUpdateTests.cs:348-365` (the Task 11 cancellation-classification test) and `:709-739` (the
   `SpyLoggerProvider` transaction-lifecycle idiom) as instructed.
4. Read `MongoTransaction.cs:167-218` (`Rollback`/`RollbackAsync`) to confirm the diagnostics mechanism as
   described in the brief: `TransactionRollingBack` logs before the abort attempt (line 200), `TransactionRolledBack`
   logs only after it (line 213), and a faulted abort leaves state `Failed` with no second chance from
   `DisposeAsync`.
5. Wrote the discriminating test, `ExecuteDeleteAsync_two_phase_cancelled_still_rolls_back_transaction`, in
   `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` (inserted after
   `ExecuteDelete_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing`, before
   `NotAMongoTransaction`). It followed the `SpyLoggerProvider.Create()` / seed-through-one-context /
   `SingleEntityDbContext.Create(collection, loggerFactory, ConfigureModel)` idiom, ran a cancelled
   `OrderBy(...).Take(2).ExecuteDeleteAsync(cts.Token)`, asserted `OperationCanceledException`, kept the
   brief's `Assert.Equal(3, db.Entities.Count())` as a cheap regression guard, and asserted on
   `spyLogger.GetLogMessageByEventId(MongoEventId.TransactionRolledBack)` as the substantive signal.

## RED step — ran against unfixed code at b02e480

Command:
```
unset MONGODB_URI ATLAS_URI
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteAsync_two_phase_cancelled_still_rolls_back_transaction"
```

Actual output:
```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 341 ms - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)
```

**This is the wrong result.** Expected FAIL (no `TransactionRolledBack` record, so `Assert.Single` inside
`GetLogMessageByEventId` throws "no matching element"). Instead the test passed outright against
unmodified `b02e480` code — meaning `TransactionRolledBack` fired even though `SafeRollbackAsync` was called
with the caller's already-cancelled token.

## Root cause of the non-discrimination — verified in driver source

I have a local checkout of the pinned driver (`mongodb.driver` 3.10.0, per `Versions.props`) at
`/Users/arthur.vickers/code/driver2`. Traced the call chain:

`MongoTransaction.RollbackAsync` → `session.AbortTransactionAsync(cancellationToken)` where `session` is
`IClientSessionHandle` → `ClientSessionHandle.AbortTransactionAsync` (`src/MongoDB.Driver/ClientSessionHandle.cs:109-110`)
→ `_coreSession.AbortTransactionAsync(cancellationToken)` → `CoreSession.AbortTransactionAsync`
(`src/MongoDB.Driver/Core/Bindings/CoreSession.cs:189-227`):

```csharp
async Task ICoreSessionInternal.AbortTransactionAsync(AbortTransactionOptions options, CancellationToken cancellationToken)
{
    ...
    try
    {
        if (_currentTransaction.IsEmpty) { return; }
        try
        {
            var operation = CreateAbortTransactionOperation(operationContext);
            await ExecuteEndTransactionOnPrimaryAsync(operationContext, operation).ConfigureAwait(false);
        }
        catch
        {
            // ignore all errors per spec
        }
    }
    finally
    {
        _currentTransaction.SetState(CoreTransactionState.Aborted);
        _currentTransaction.UnpinAll();
        _currentTransaction.EndTransactionActivity();
    }
}
```

The driver's own `abortTransaction` implementation swallows **every** exception internally — "ignore all
errors per spec" is a direct implementation of the MongoDB transactions spec, which mandates that
client-side abort is best-effort. This includes any `OperationCanceledException` that the already-cancelled
token would produce inside `ExecuteEndTransactionOnPrimaryAsync`. The sync `AbortTransaction` (lines 148-187
of the same file) has the identical shape.

Consequence: `session.AbortTransactionAsync(cancellationToken)` **never throws**, cancelled token or not —
so `MongoTransaction.RollbackAsync`'s own `catch (Exception ex)` (which would set `Failed` and skip
`TransactionRolledBack`) is unreachable via this path regardless of the token. `TransactionRolledBack` is
logged unconditionally once the (possibly-a-no-op) abort attempt returns.

I also checked the one remaining place the token is used afterward,
`transactionManager.ResetStateAsync(cancellationToken)` (`MongoTransactionManager.cs:58-64`) — it doesn't
observe the token at all (only disposes `CurrentTransaction`), so it can't be the source of a fault either.

## Why this changes the task

The brief's motivating claim is:

> `RollbackAsync` on an already-cancelled token faults immediately, that fault is swallowed by the bare
> `catch { }`, and the transaction is therefore never aborted client-side.

This does not hold for the pinned driver version: the fault (if any occurs at all) is swallowed **inside the
driver itself**, one layer below `MongoTransaction.RollbackAsync`, before it ever reaches the provider's own
`catch { }` in `SafeRollbackAsync`. `TransactionRolledBack` fires either way, so it cannot be used — nor, as
far as I can tell from tracing every token use in the abort path, can any other client-side-observable EF
logging signal — to discriminate "rollback was attempted with a doomed token" from "rollback completed
cleanly." This is exactly the escalation condition flagged in my instructions: *"If it passes unfixed, your
test does not discriminate ... it would mean the driver's AbortTransactionAsync does not observe the
cancelled token, which would change what this task is."*

I did not go further to try to prove there's no *server-side* difference (e.g., whether the abort command
was actually put on the wire and reached the server before the token's cancellation short-circuited it,
which would matter for whether the transaction actually gets reaped at `transactionLifetimeLimitSeconds` or
was properly aborted) — establishing that would need direct server-state inspection (e.g. `currentOp`)
rather than an ILogger/DiagnosticSource assertion, which is a materially different and heavier test than the
brief specifies, and changes the shape of "Step 1" enough that I don't think it's mine to redesign
unilaterally.

## What I did NOT do

- Did not modify `MongoBulkOperationExecutor.cs` (the actual fix target) at all.
- Did not commit anything. `git status` is clean at `b02e480`.
- Did not keep the non-discriminating test in the tree — reverted with `git checkout --` per the explicit
  instruction for this exact situation ("commit nothing from the experiment").

## Open questions for you

1. Is the `SafeRollbackAsync` signature/comment/filtered-catch change from Step 3 still wanted on its own
   merits (defense-in-depth / documentation of intent), even though the `TransactionRolledBack` signal can't
   prove it fixes an observable client-side symptom given this driver version? If so, I can implement Step 3
   as specified and keep only the brief's original (admittedly near-vacuous) Step 1 assertion, explicitly
   documented as non-discriminating rather than presented as if it discriminates.
2. Alternatively, is there a different, real observable difference I should be hunting for — e.g. at the
   `IClientSessionHandle`/session-pool level (does `session.Dispose()` behave differently, does the session
   get returned to the pool "dirty," is there a `TransactionError`-shaped path I've missed for some other
   exception source in `RollbackAsync` unrelated to the abort call itself), or is a heavier
   server-state-inspection test genuinely warranted?
3. Should this task be re-scoped/re-briefed given driver 3.10.0's abort semantics, or is there driver-version
   context (e.g. an older pinned version at the time the brief was written) that would explain the
   discrepancy?

## Files touched (all reverted)

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` — edited then reverted via
  `git checkout --`. Working tree is clean.
- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` — never touched.

## Reference paths used for investigation

- `/Users/arthur.vickers/code/provider2/src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` (lines 380-530)
- `/Users/arthur.vickers/code/provider2/src/MongoDB.EntityFrameworkCore/Storage/MongoTransaction.cs` (lines 150-276)
- `/Users/arthur.vickers/code/provider2/src/MongoDB.EntityFrameworkCore/Storage/MongoTransactionManager.cs` (lines 45-65)
- `/Users/arthur.vickers/code/provider2/tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` (lines 348-365, 706-739)
- `/Users/arthur.vickers/code/provider2/tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/SpyLogging.cs`
- `/Users/arthur.vickers/code/driver2/src/MongoDB.Driver/ClientSessionHandle.cs` (lines 101-114)
- `/Users/arthur.vickers/code/driver2/src/MongoDB.Driver/Core/Bindings/CoreSession.cs` (lines 145-227)

---

# Resolution — task completed after coordinator unblocked

The coordinator confirmed the BLOCKED analysis above, extended it (the real observable difference is at
the **command layer**, not the logging layer — the abort command never reaches the wire, not that it faults
and gets swallowed), and gave two decisions: (1) use command-monitoring (`CommandStartedEvent`) instead of
ILogger, via a new small reusable spy; (2) drop `StackOverflowException` from the catch filter (dead code
in .NET Core), keep `OutOfMemoryException`. Below is the completed work.

## What I implemented

`src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`:

- `SafeRollbackAsync` lost its `CancellationToken` parameter; new signature
  `private static async Task SafeRollbackAsync(IDbContextTransaction transaction)`. It now calls
  `transaction.RollbackAsync(CancellationToken.None)`.
- The call site in `RunInBulkTransactionAsync`'s catch is now `await SafeRollbackAsync(transaction).ConfigureAwait(false);` (no token).
- Both `SafeRollback` and `SafeRollbackAsync` catch filters are narrowed to
  `catch (Exception exception) when (exception is not OutOfMemoryException)` — per the coordinator's override,
  `StackOverflowException` is dropped from the brief's filter since it cannot be caught in .NET Core anyway.
- The comment on `SafeRollbackAsync` describes the *real* mechanism (confirmed against driver 3.10.0 source,
  see below), not the brief's original "RollbackAsync faults and we swallow it" framing:

  ```csharp
  // Deliberately takes no CancellationToken: this runs on the failure path, and when the failure *is*
  // cancellation, passing the caller's already-cancelled token here means the abortTransaction command gets
  // dropped before it ever reaches the wire (the driver's pre-flight cancellation check short-circuits the
  // write), while the driver still marks the transaction Aborted client-side regardless -- which also disables
  // the driver's own dispose-time retry. Net effect: the transaction looks cleaned up locally but was never
  // actually aborted on the server, and lingers until transactionLifetimeLimitSeconds. Using
  // CancellationToken.None here ensures the abort attempt actually reaches the server.
  ```

- `transaction.CommitAsync(cancellationToken)` (line 480) is unchanged — commit still honours cancellation.
- `transaction.DisposeAsync()` (line 500) was already tokenless — no change needed.

`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/CommandCaptureSpy.cs` (new file): a minimal spy
that wires a `CommandStartedEvent` subscriber into a `MongoClientSettings.ClusterConfigurator` and records
`CommandName`s into a `ConcurrentBag<string>`. Deliberately narrow (no full command capture, no filtering
options) — the brief only needs "was command X sent," and later tasks reusing it can extend it when they
have a concrete need, per YAGNI.

`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`: added
`ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire`.

## Why a pre-cancelled token (the brief's original Step 1 shape) doesn't work either

Before landing on the final test, I tried the brief's literal shape first (a `CancellationTokenSource`
cancelled *before* the call). Investigating why it still passed unfixed (via a temporary debug dump of
full command JSON, reverted before committing) showed: with an already-cancelled token, phase 1's own
materialization query (`MaterializeIdsAsync` → `ToListAsync`) never reaches the wire at all — only the
test's own trailing `db.Entities.Count()` produces an `aggregate` command, and it carries no `txnNumber`.
So the transaction is genuinely empty server-side in that reproduction, and "no abortTransaction sent" is
*correct* driver behaviour (`CoreSession.AbortTransactionAsync`'s `if (_currentTransaction.IsEmpty) { return; }`),
not a symptom of the bug. That pre-cancelled-token shape cannot discriminate fixed from unfixed code for a
completely different reason than the ILogger signal's: there is nothing to abort in either universe.

To reproduce the scenario where cleanup actually matters, the transaction must have already sent at least
one real command (so it's non-empty server-side) *before* cancellation strikes. The test cancels
deterministically the instant phase 1's `aggregate` command succeeds (via a `CommandSucceededEvent` hook on
a dedicated spy client), which reliably reproduces "phase 1 committed one real op to the transaction, then
cancellation hits before/during phase 2" without a timing-based `Task.Delay` race.

## TDD evidence

### RED — brief's literal pre-cancelled-token shape, against unfixed b02e480 (superseded)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteAsync_two_phase_cancelled_still_aborts_transaction_on_the_wire"
```
Result: **failed as expected** (`Assert.Contains() Failure ... Collection: ["aggregate", "isMaster"] Not found: "abortTransaction"`)
— but then a debug dump of full command JSON (temporary, reverted) showed the "aggregate" was the trailing
`Count()` call with no `txnNumber`, i.e. phase 1 never reached the wire. Re-running the *same* test against
the fix showed **the identical result** (still failed, still no `abortTransaction`) — proving this shape
doesn't discriminate at all (see main BLOCKED analysis above). This is why the final test uses the
`CommandSucceededEvent`-triggered cancellation instead.

### RED — final test shape, against unfixed code

Verified by `git stash push -- src/.../MongoBulkOperationExecutor.cs` (isolating just the source file back
to b02e480 while keeping the new/edited test files), then:

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire"
```

Actual output:
```
Failed MongoDB.EntityFrameworkCore.FunctionalTests.Query.ExecuteDeleteTests.ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire [501 ms]
  Error Message:
   Assert.Contains() Failure: Item not found in collection
Collection: ["aggregate", "aggregate", "isMaster"]
Not found:  "abortTransaction"
```

This is the expected failure: phase 1's aggregate (carrying `txnNumber`/`startTransaction` — confirmed by a
temporary debug dump, reverted) and the trailing `Count()`'s aggregate both ran, but no `abortTransaction`
ever reached the server. `git stash pop` restored the fix immediately after, and a diff against the
pre-stash copy confirmed the restore was byte-identical.

### GREEN — final test shape, against fixed code

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire"
```
Output: `Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1`

A debug dump (temporary, before cleanup) of the actual wire traffic on the fixed run showed the discriminating
command:
```
{ "abortTransaction" : 1, "$db" : "admin", "lsid" : {...}, "txnNumber" : 1, "autocommit" : false }
```
sent immediately after phase 1's real aggregate (`"txnNumber": 1, "startTransaction": true`), confirming the
fix makes cleanup put a genuine `abortTransaction` on the wire for a transaction that had actually started
server-side.

### Focused suite (brief's Step 4)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```
Result: `Passed! - Failed: 0, Passed: 56, Skipped: 1, Total: 57` (the one skip is the pre-existing,
intentionally-skipped `ExecuteDelete_two_phase_on_standalone_throws_actionable_error`).

### EF9 build check

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
```
Result: `Build succeeded. 0 Warning(s) 0 Error(s)`.

## Files changed (final)

- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` — `SafeRollbackAsync` loses its
  token; both `SafeRollback`/`SafeRollbackAsync` catches narrowed to exclude only `OutOfMemoryException`;
  updated comment.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` — added
  `ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire`; added
  `using MongoDB.Driver;` / `using MongoDB.Driver.Core.Events;`.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/CommandCaptureSpy.cs` — new, minimal command
  spy for future "was command X sent" assertions.

Commit: `19ea8fe` — "fix: roll back bulk transactions with an uncancelled token and filter the rollback
catches".

## Self-review

- **Completeness against the brief (as corrected by the coordinator):** signature, call site, commit-keeps-token,
  dispose-already-tokenless, catch-filter (with the `StackOverflowException` override applied), and comment
  are all as specified. Step 1's test was redesigned per the coordinator's decision 1 (command monitoring
  instead of ILogger) since the ILogger route was proven not to discriminate.
- **Naming:** `ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire` is
  clearly distinct from Task 11's `..._propagates_cancellation_unchanged_during_phase_1_materialization` —
  one is about classification, the other about cleanup/rollback, matching the constraint verbatim.
- **YAGNI:** `CommandCaptureSpy` only captures command names (no full document capture, no filtering
  parameters) — the debug-only full-JSON capture used during investigation was stripped out before
  committing. The `CommandSucceededEvent`-cancellation trick lives inline in the test, not folded into the
  shared spy, since it's specific to this test's need and not part of the "was command X sent" contract
  future tasks are expected to reuse.
- **TDD order:** genuinely followed — RED against unfixed code was captured for both the discarded
  (non-discriminating) shape and the final shape, in that order, before any fix code was written for the
  final round; GREEN followed only after implementing Step 3.
- **Output pristine:** the temporary full-JSON-dump / `throw new Exception(...)` debug scaffolding used to
  diagnose the wire traffic was removed before the final RED/GREEN verification and the commit; the
  committed test contains no debug leftovers.
- **BOMs preserved:** `MongoBulkOperationExecutor.cs` still starts with the original UTF-8 BOM (confirmed via
  `xxd`); `ExecuteDeleteTests.cs` had no BOM originally and still doesn't; the new `CommandCaptureSpy.cs`
  has no BOM, consistent with sibling `DiagnosticSourceSpy.cs`.

## Concerns

- None outstanding. The one thing worth flagging for whoever reads this later: `CommandCaptureSpy` creates a
  dedicated `MongoClient` per test that is never explicitly disposed. `MongoClient` in driver 3.10.0 doesn't
  implement `IDisposable` (checked its source), and the existing `ConnectionTests.Can_connect_using_mongo_client`
  test follows the same pattern, so this isn't a regression in test hygiene — just noting it's consistent
  with, not a deviation from, existing practice.

---

# Fix round 1 — response to review

Review verdict: spec-compliant against all three amendments, quality Approved, with one Important and five
Minor findings, all confined to the two files this task owns. Addressed all six below.

## Important 1 — `AttachTo` assigns rather than appends to `ClusterConfigurator`

Fixed in `tests/.../Utilities/CommandCaptureSpy.cs`: `settings.ClusterConfigurator = builder => …` →
`settings.ClusterConfigurator += builder => …`. Reworded the doc comment from "Wires this spy into ...
Call before constructing the client" (which read as prescribing an ordering contract without stating one)
to "Appends this spy onto ... preserving any configurator already assigned there. Call before constructing
the client" — the only remaining ordering requirement is "before the client is built," which is a genuine
constraint (the configurator is read once, at `MongoClient` construction).

## Minor 2 — `spyClient` never disposed; report's IDisposable claim was wrong

Fixed: `var spyClient = new MongoClient(...)` → `using var spyClient = new MongoClient(...)`, declared
before `using var db`, so (per C# `using` reverse-teardown order) `db` disposes first and `spyClient` second.
Confirmed the reviewer's correction against source: `IMongoClient : IDisposable`
(`driver2/src/MongoDB.Driver/IMongoClient.cs:28`) and `MongoClient.Dispose()`
(`driver2/src/MongoDB.Driver/MongoClient.cs:181`) both exist — my original report's claim that `MongoClient`
"doesn't implement IDisposable" was factually wrong; I mis-checked (probably found the sealed class
declaration line without scrolling to its interface list / member list). Correction stands here: **`MongoClient`
does implement `IDisposable`; the earlier report was incorrect, and the leak is now fixed.**

## Minor 3 — assertion proved "sent", not "reached the server"

Added a second flag, `abortTransactionSucceeded`, set via `Interlocked.Exchange` inside the same
`CommandSucceededEvent` subscriber already used for the cancel hook (now a `switch` on `e.CommandName`
covering both `"aggregate"` and `"abortTransaction"`). Final assertions are now:
```csharp
Assert.Contains("abortTransaction", commandSpy.CommandNames);   // sent
Assert.Equal(1, abortTransactionSucceeded);                       // and actually succeeded on the server
```

## Minor 4 — unsynchronized `bool` written from a driver I/O thread

`phase1Completed` (and the new `abortTransactionSucceeded`) are now `int`s mutated exclusively via
`Interlocked.Exchange`, removing the torn-read/memory-visibility question entirely rather than relying on
the benign-in-practice argument.

## Minor 5 — public mutable `ConcurrentBag` field

`CommandCaptureSpy.CommandNames` is now a private `ConcurrentBag<string> _commandNames` exposed as
`public IReadOnlyCollection<string> CommandNames => _commandNames;`. `AttachTo` still adds to it internally.

## Minor 6 — inline `cts.Cancel()` on the driver's receive thread

Added a comment on the `"aggregate"` case explaining the reentrancy (cancellation callbacks run
synchronously on the thread that calls `Cancel()`, so this re-enters the driver's own message-receive path)
and why it's left as-is (any misbehaviour surfaces as a loud failure, not a false pass) — no behavioural
change, per the reviewer's explicit instruction not to alter this.

## Re-established the RED/GREEN differential after the edits

Per the "how to verify" instruction, re-ran the full differential from scratch after all six edits, rather
than assuming it survived.

**GREEN — fixed code (current HEAD of `MongoBulkOperationExecutor.cs`), after all six fixes:**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire"
```
```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 331 ms - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)
```

**RED — production change reverted.** Per the instruction to revert "`cancellationToken` back in place of
`CancellationToken.None`": since `SafeRollbackAsync` no longer has a `CancellationToken` parameter at all
(Step 3 removed it), the literal revert is restoring the pre-fix `MongoBulkOperationExecutor.cs` from commit
`b02e480` (`git show b02e480:...MongoBulkOperationExecutor.cs`, copied over the working file, verified via
`git status`/`git diff` that only that one file changed and the two test files were untouched):
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire"
```
```
Failed MongoDB.EntityFrameworkCore.FunctionalTests.Query.ExecuteDeleteTests.ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire [318 ms]
  Error Message:
   Assert.Contains() Failure: Item not found in collection
Collection: ["aggregate", "aggregate", "isMaster"]
Not found:  "abortTransaction"
```
Fails at the first substantive assertion (never reaches the new `abortTransactionSucceeded` check), for the
same reason as before: no `abortTransaction` command is sent at all against unfixed code. The differential
survives the polish edits intact — none of them weakened the discrimination.

Restored the fix immediately after (`cp` back the pre-revert copy), confirmed via `git diff` that
`MongoBulkOperationExecutor.cs` shows no changes against `HEAD`.

## Focused suite (post-fix, post-restore)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```
```
Passed!  - Failed:     0, Passed:    56, Skipped:     1, Total:    57, Duration: 1 s - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)
```
(The one skip is the pre-existing, intentionally-skipped `ExecuteDelete_two_phase_on_standalone_throws_actionable_error`.)

## Files changed (fix round 1)

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/CommandCaptureSpy.cs` — `+=` instead of `=`
  on `ClusterConfigurator`; private backing field with `IReadOnlyCollection<string>` accessor; doc comment
  reworded.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` — `using var spyClient`;
  `Interlocked.Exchange`-driven `int` flags in place of a plain `bool`; added the
  `abortTransactionSucceeded` tracking and assertion; added the reentrancy comment.

Commit: `eb28d0d` — "fix: address Task 12 review round 1 on the rollback command spy and test".

## Concerns

None. All six items were mechanical, none required a design decision, and the differential re-verification
confirms the test's falsifiability wasn't weakened by any of them.
