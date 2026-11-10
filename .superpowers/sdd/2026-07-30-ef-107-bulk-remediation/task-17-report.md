# Task 17 Report: Stream the async phase-1 cursor

## Status: DONE (includes fix round after review round 1 — see "Fix round" section below)

## What I implemented

Changed `MaterializeIdsAsync` in `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`
(lines ~348-388) to stream the phase-1 cursor rather than materialising every matched document via
`ToListAsync`. No other method was touched; no `PhaseOneResult` type was created; the signature is
unchanged.

Before: `IAsyncCursorSourceExtensions.ToListAsync((IAsyncCursorSource<BsonDocument>)documents, cancellationToken)`
built a `List<BsonDocument>` holding every matched document, then a second loop copied `_id` into
`List<BsonValue>`.

After:
```csharp
IAsyncCursor<BsonDocument> cursor;
try
{
    cursor = await ((IAsyncCursorSource<BsonDocument>)documents)
        .ToCursorAsync(cancellationToken).ConfigureAwait(false);
}
catch (ExpressionNotSupportedException exception)
{
    throw wrapTranslationFailure(exception);
}

using (cursor)
{
    var ids = new List<BsonValue>();
    while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
    {
        foreach (var document in cursor.Current)
        {
            ids.Add(document["_id"]);
        }
    }

    return ids;
}
```

Peak memory is now one cursor batch plus the ids list, matching the sync `MaterializeIds` (which wraps
`documents.GetEnumerator()` in the same try/catch shape, then a `using (enumerator)` block for the
per-document loop).

## Where the try ended up, and why

The `try` wraps only the `ToCursorAsync` call — nothing else. This was verified directly against the
pinned driver source (`/Users/arthur.vickers/code/driver2`), not assumed:

- `ToCursorAsync` is declared as an **interface member** on `IAsyncCursorSource<TDocument>`
  (`src/MongoDB.Driver/Core/IAsyncCursorSource.cs:43`), not an extension method. There is no
  `IAsyncCursorSourceExtensions.ToCursorAsync` to qualify against — only `ToListAsync` (and other
  terminal helpers) are extension methods that internally call the interface member
  `source.ToCursorAsync(...)`. So the call is `((IAsyncCursorSource<BsonDocument>)documents).ToCursorAsync(cancellationToken)`,
  a direct interface-method invocation via the same cast style the old code used, not an
  `IAsyncCursorSourceExtensions.*` qualification (that class simply doesn't have this member).
- The concrete implementation, `MongoQuery<TDocument,TOutput>.ToCursorAsync`
  (`src/MongoDB.Driver/Linq/Linq3Implementation/MongoQuery.cs:97-102`), translates the expression tree
  synchronously (`ExpressionToExecutableQueryTranslator.Translate(...)`) and only then hands off to
  `_provider.ExecuteAsync(executableQuery, cancellationToken)`. `ToListAsync` itself is implemented as
  `ToCursorAsync` followed by `cursor.ToListAsync` (`IAsyncCursorSource.cs:373-379`), confirming
  `ToCursorAsync` is where translation happens, identically to before.
- `MoveNextAsync` only performs `getMore` I/O against an already-translated pipeline and cannot throw
  `ExpressionNotSupportedException`, so it sits outside the try, inside a `using (cursor)` block — the
  async mirror of the sync path's `using (enumerator)` shape.

The updated ~10-line comment block above the try explains this (translation happens inside
`ToCursorAsync`, confirmed via the driver source showing `ToListAsync` is `ToCursorAsync` + `cursor.ToListAsync`),
preserving the original comment's reasoning rather than deleting it.

## Correction 5 — phase-1 pipeline logging confirmed intact

Traced `LoggedStages` end-to-end in the driver source:
- `RenderPhaseOnePipeline` (`MongoBulkOperationExecutor.cs:645-648`) reads
  `(targetIdQuery.Provider as IMongoQueryProvider)?.LoggedStages`.
- `MongoQueryProvider.LoggedStages => _executedQuery?.LoggedStages` (`MongoQueryProvider.cs:108`).
- `MongoQueryProvider.ExecuteAsync<TResult>(ExecutableQuery<TDocument,TResult>, CancellationToken)`
  (`MongoQueryProvider.cs:154-157`) sets `_executedQuery = executableQuery` **before** calling
  `executableQuery.ExecuteAsync(...)`.
- `MongoQuery<TDocument,TOutput>.ToCursorAsync` and the `ToListAsync` extension's internal call to
  `ToCursorAsync` both route through this exact same `_provider.ExecuteAsync(executableQuery, cancellationToken)`
  call, with the same `executableQuery` built by the same `ExpressionToExecutableQueryTranslator.Translate` call.

So `ToCursorAsync` populates `LoggedStages` identically to `ToListAsync` — same code path, not just "should".

Ran `BulkOperationsCommandTests` on both EF10 and EF9 (15/15 passed each, see below). The two theory
cases that assert this directly:
- `TwoPhaseDelete_command_is_gated_by_sensitive_data_logging(async: True)` — asserts
  `Assert.Contains("$project", open.Command)` on the async path. Passed.
- `TwoPhaseUpdate_command_is_gated_by_sensitive_data_logging(async: True)` — same assertion. Passed.

Neither is JSON `null` — `Assert.Contains("$project", ...)` on a JSON-`null` `phase1Pipeline` value would
fail (the string would contain `"phase1Pipeline": null`, not `$project`), so passing confirms the actual
asserted value contains the rendered `{ "$project": { "_id": 1 } }` stage, not null. No stop-and-report
condition was hit.

## Correction 4 — capacity comment, verified before writing

Confirmed by reading the code (not assuming): the sync path (`MaterializeIds`, line 338) already uses
`new List<BsonValue>()` with **no** capacity hint — it always did, this wasn't new. The *async* path
previously could pass a capacity (`new List<BsonValue>(materialized.Count)`, old line 367) only because
`ToListAsync` handed it a count for free after full materialization. Streaming removes that count, so the
async path now also uses `new List<BsonValue>()`, with a comment framed as "streaming trades an available
capacity hint for bounded peak memory" — matching what's actually true rather than the brief's claim that
both paths always lacked a hint symmetrically.

## Tests run and results

Environment: `MONGODB_URI` and `ATLAS_URI` confirmed unset before running (TestContainers booted its own
`mongodb/mongodb-atlas-local` container per run).

1. **Builds** — all three configurations, clean:
   - `dotnet build -c "Debug EF8"` — Build succeeded, 0 warnings, 0 errors.
   - `dotnet build -c "Debug EF9"` — Build succeeded, 0 warnings, 0 errors.
   - `dotnet build -c "Debug EF10"` — Build succeeded, 0 warnings, 0 errors.

2. **EF10**, `ExecuteDeleteTests|ExecuteUpdateTests|BulkOperationsCommandTests`: 79 passed, 1 skipped
   (`ExecuteDelete_two_phase_on_standalone_throws_actionable_error`, a standalone-deployment-only test,
   expected skip on the replica-set-backed atlas-local container), 0 failed.

3. **EF9**, same filter: 80 passed, 1 skipped (same test), 0 failed.

4. **BulkOperationsCommandTests** individually on both EF10 and EF9: 15/15 passed each, including both
   async theory cases with the `$project`/`phase1Pipeline` assertions above.

5. **The three named Task 11/12 tests**, run explicitly on EF10:
   - `ExecuteUpdate_two_phase_wraps_a_translation_failure_thrown_during_phase_1_materialization(async: True)` — Passed.
   - `ExecuteUpdate_two_phase_wraps_a_translation_failure_thrown_during_phase_1_materialization(async: False)` — Passed.
   - `ExecuteUpdateAsync_two_phase_propagates_cancellation_unchanged_during_phase_1_materialization` — Passed.
   - `ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire` — Passed.
   Neither Task 11 test regressed.

6. **Regression baseline** — `NorthwindBulkUpdatesMongoTest`:
   - EF10: 181/181 passed (matches stated baseline).
   - EF9: 169/169 passed (matches stated baseline).

All test runs were clean (no unexpected warnings/errors in output beyond normal TestContainers startup
logging and the one expected skip).

## Which existing tests actually exercise the streaming code

The brief and the task both note the async path is thinly covered until Task 23. Being explicit about
which tests actually execute the new `ToCursorAsync`/`MoveNextAsync` loop (as opposed to merely building
against the changed signature, which nothing does since the signature didn't change):

- `ExecuteUpdate_two_phase_wraps_a_translation_failure_thrown_during_phase_1_materialization(async: True)`
  — drives the translation-failure path through `MaterializeIdsAsync`, hitting the `try`/`ToCursorAsync`/
  catch branch specifically (this is the reproducer for the narrow catch).
- `ExecuteUpdateAsync_two_phase_propagates_cancellation_unchanged_during_phase_1_materialization` — **correction
  to an earlier version of this report**: this test cancels the token *before* calling `ExecuteUpdateAsync`
  (`ExecuteUpdateTests.cs:356-357`, `await cts.CancelAsync()` precedes the call, with the test's own comment
  noting "An already-cancelled token makes this deterministic"). With an already-cancelled token, cancellation
  surfaces from inside `ToCursorAsync` — **inside** the try, not from the `MoveNextAsync` loop outside it. What
  it actually proves: an `OperationCanceledException` raised inside the try is not mislabelled by the narrow
  `catch (ExpressionNotSupportedException)` as a translation failure. It does not exercise the `MoveNextAsync`
  loop's cancellation handling at all.
- `ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire` — runs a
  real (non-empty-result) async two-phase delete through the full streaming loop (`ToCursorAsync` +
  repeated `MoveNextAsync` + `cursor.Current` iteration + `ids.Add`) before cancelling for phase 2.
- `TwoPhaseDelete_command_is_gated_by_sensitive_data_logging(async: True)` and
  `TwoPhaseUpdate_command_is_gated_by_sensitive_data_logging(async: True)` — each inserts real documents
  and runs a real async two-phase delete/update with a non-empty match set, so the streaming loop
  actually iterates a populated cursor and produces a non-empty id list; these also confirm
  `LoggedStages`/`phase1Pipeline` end-to-end.
- Every `[Theory(async: true)]`/`ExecuteUpdateAsync`/`ExecuteDeleteAsync` case throughout
  `ExecuteUpdateTests`/`ExecuteDeleteTests` that reaches the two-phase strategy (most two-phase-tagged
  tests are parameterized `[Theory]` over `async`) exercises this loop as a side effect of getting a
  correct end-to-end result, even though most don't assert anything about memory or iteration shape
  directly.

No test (prior to the fix round below) asserted batch-by-batch behavior, multi-batch iteration, or a mid-loop
`getMore` failure/cancellation directly — that finer-grained coverage was Task 23's stated scope, per the
brief.

**Honest limits, restated plainly (per review round 1):**
- None of the three Task 11/12 tests I ran (`ExecuteUpdate_two_phase_wraps_a_translation_failure_...`,
  `ExecuteUpdateAsync_two_phase_propagates_cancellation_unchanged_...`,
  `ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire`) would fail if
  this change were reverted back to `ToListAsync`. All three pin *behavioral equivalence* (same exception
  classification, same ids, same transaction-abort outcome) between the old and new code, not anything
  specific to streaming. I verified this is a real property of the tests, not a guess: the translation
  failure and the already-cancelled-token cancellation both occur inside `ToCursorAsync`/`ToListAsync`'s
  shared translation step, which is unchanged by this task, and the Task 12 abort test cancels only after
  phase 1's `aggregate` has already succeeded, so it never touches how ids are extracted from the cursor.
- Peak memory (the actual reason for this change) is not directly observable by any test here, and I did not
  attempt to fabricate one — an unfalsifiable memory-bound assertion would be worse than no assertion.
- What *is* newly falsifiable, and now has a test (see "Fix round" below): whether the hand-rolled
  `ToCursorAsync` + `MoveNextAsync` drain loop correctly drains every batch rather than truncating.
- No test (including the new one) exercises a failure or cancellation raised *during* cursor iteration (a
  mid-loop `getMore` that throws or is cancelled after the loop has already started, rather than before
  `ToCursorAsync` is ever called). That gap is explicitly Task 23's territory per the coordinator, and I did
  not build it here.

## Fix round (review round 1)

Two Important findings came back from review. Both were about evidence, not the code itself — the code was
independently re-verified and approved.

### Important 1 — added a falsifiable test for the new drain loop

The reviewer correctly identified that none of my original tests would fail if this change were reverted:
they all pin *behavioral equivalence*, not anything specific to the new streaming loop. The real, falsifiable
risk they pointed at: `MaterializeIdsAsync` now owns a hand-rolled `ToCursorAsync` + `MoveNextAsync` drain
loop that the driver's `ToListAsync` used to own internally. If that loop is wrong — stops after the first
batch, mishandles an empty first batch, etc. — ids get silently truncated and a bulk delete/update quietly
affects fewer documents than it should.

**Added test**: `ExecuteDeleteAsync_two_phase_streams_every_id_across_multiple_getMore_batches` in
`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` (after
`ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire`, in the same file
as the Task 12 test and using the same `CommandCaptureSpy` infrastructure already established there).

What it does:
- Seeds 250 tiny `Order` documents (checked first: neither `AggregateOptions.BatchSize` nor
  `FindOptions.BatchSize` have a driver-side default — both are `int?` left `null` — and `BuildTargetIdQuery`'s
  `collection.AsQueryable()` call never sets one, so there is no provider-level knob to force a small batch
  size deterministically; the only lever is seeding past the server's own first-batch cap).
- Runs `db.Entities.OrderBy(o => o.Quantity).Skip(1).ExecuteDeleteAsync()` — a bare `Skip` (no `Take`) keeps
  this on the two-phase path (same pattern as the existing
  `ExecuteDelete_with_bare_Skip_and_no_Take_still_uses_the_two_phase_path`) while still targeting effectively
  every seeded document (249 of 250), so a truncating drain loop loses count rather than merely changing which
  documents survive.
- Asserts `deleted == seedCount - 1` (249) and that exactly 1 document remains.
- Attaches a `CommandCaptureSpy` to a dedicated client and asserts `getMoreCount > 0` — i.e. it checks that
  phase 1's cursor actually spanned more than one batch for this seed size, rather than asserting a
  multi-batch claim the test never verifies.

**Batch evidence, measured directly** (not assumed): I temporarily changed the `getMoreCount > 0` assertion to
`getMoreCount > 100000` (guaranteed to fail) to force the assertion message to print, then ran the test:

```
DEBUGPROBE getMoreCount=1 allCommands=[aggregate, delete, getMore, aggregate, commitTransaction, isMaster]
```

Exactly **1 `getMore`** was observed for 250 seeded documents — confirming the server's first batch held fewer
than 250 documents (consistent with the well-known ~101-document/16MB first-batch cap) and a second batch was
fetched via `getMore` to complete the cursor. That means `MaterializeIdsAsync`'s `while (await
cursor.MoveNextAsync(...))` loop body ran across at least two distinct batches for this test — genuine
multi-batch coverage, not a single large batch dressed up as one. I then reverted the probe back to the
honest `getMoreCount > 0` assertion and re-ran to confirm it still passes cleanly.

**Falsifiability check**: I temporarily broke `MaterializeIdsAsync` in `MongoBulkOperationExecutor.cs` by
changing `while (await cursor.MoveNextAsync(...))` to `if (await cursor.MoveNextAsync(...))` — a synthetic
"only process the first batch" truncation bug — rebuilt, and re-ran the new test. It failed
(`Assert.Equal() Failure: Values differ`, on the `deleted == seedCount - 1` assertion), confirming the test
actually catches the class of bug it was written for. I then reverted the change (`git diff` confirmed the
file exactly matched the committed version afterward) and rebuilt to restore the correct implementation
before re-running the full verification suite.

### Important 2 — corrected the report's overstated claim

Fixed in the "Which existing tests actually exercise the streaming code" section above: the claim that
`ExecuteUpdateAsync_two_phase_propagates_cancellation_unchanged_during_phase_1_materialization` "exercises the
`MoveNextAsync` loop's cancellation-token plumbing outside the try" was wrong. The token is cancelled before
the call, so cancellation surfaces from inside `ToCursorAsync` (inside the try), proving only that a
same-shape cancellation isn't mislabelled by the narrow catch — not anything about the loop.

### Fix-round verification

- Build: all three configurations (`Debug EF8`, `Debug EF9`, `Debug EF10`) — clean, 0 warnings, 0 errors,
  after the final revert (matches the pre-probe committed state).
- `dotnet test -c "Debug EF10" --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"`
  → **80 passed, 1 skipped** (the same standalone-only skip as before), 0 failed. Includes the new test.
- `dotnet test -c "Debug EF9"` with the same filter → **81 passed, 1 skipped**, 0 failed. Includes the new
  test.
- `MONGODB_URI`/`ATLAS_URI` confirmed unset for every run in this fix round.
- Northwind was not re-run — no shared code outside `MaterializeIdsAsync` was touched (only the async
  drain-loop probe, which was fully reverted, and the new test file).

## Files changed

- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` — `MaterializeIdsAsync` only.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` — added
  `ExecuteDeleteAsync_two_phase_streams_every_id_across_multiple_getMore_batches` (fix round).

## Commits

- `ea0e01d` — "perf: stream the async bulk phase-one cursor instead of materialising every document"
- `f327a0a` — "test: pin that the async phase-1 drain loop does not truncate ids across batches" (fix round)

## Self-review

- **Narrow catch intact and still wraps only the translation-triggering call**: yes. `catch
  (ExpressionNotSupportedException exception) { throw wrapTranslationFailure(exception); }` wraps only
  `ToCursorAsync`; the `MoveNextAsync` loop is outside the try, inside `using (cursor)`. Verified against
  driver source that `ToCursorAsync` is where translation happens and `MoveNextAsync` only does `getMore`
  I/O.
- **Every await uses `ConfigureAwait(false)`**: yes — both the `ToCursorAsync` await and the
  `MoveNextAsync` await in the loop.
- **Cursor disposed on every path, including exceptions**: yes. If `ToCursorAsync` throws, no cursor was
  ever created (nothing to dispose). Once assigned, the `using (cursor)` block disposes it on normal
  return and on any exception (e.g. from `MoveNextAsync`) that propagates out of the block. This mirrors
  the driver's own extension methods (`AnyAsync`, `FirstAsync`, etc. in `IAsyncCursorSource.cs`), which
  use synchronous `using (var cursor = await source.ToCursorAsync(...))` rather than `await using` —
  `IAsyncCursor<T>` implements both `IDisposable` and `IAsyncDisposable`, and the driver's own idiom uses
  the synchronous form, so this file's usage stays consistent with driver convention.
- **Comments accurate**: rewrote the comment above the try to describe streaming (not `ToListAsync`) and
  to state precisely why `ToCursorAsync` (not the loop) is the translation-triggering call, with the
  driver-source justification. Rewrote the capacity comment per Correction 4's verified framing.
- **Test output pristine**: yes, no unexpected warnings, no flaky output; the one skip
  (`ExecuteDelete_two_phase_on_standalone_throws_actionable_error`) is an expected environment-dependent
  skip unrelated to this change (it needs a standalone, non-replica-set deployment).
- **No unused leftovers**: confirmed no remaining reference to `IAsyncCursorSourceExtensions` in the file
  after the edit.

## Concerns

None. All five corrections were independently verified against the pinned driver source and the current
tree rather than taken on trust, all named tests pass, both regression baselines matched exactly on the
initial pass, and all three build configurations are clean after the fix round. The two Important findings
from review round 1 (an unfalsifiable test claim, and an overstated report claim) are addressed above: a
genuinely falsifiable test now covers the new drain loop (verified to fail against a synthetic truncation
bug and to genuinely span multiple batches), and the report's inaccurate claim about which test exercises
the `MoveNextAsync` loop's cancellation path has been corrected and the remaining coverage gaps stated
plainly rather than glossed over.
