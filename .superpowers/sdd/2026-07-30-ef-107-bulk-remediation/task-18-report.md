# Task 18 report — Bound the `$in` list

**Status: DONE**
**Commit: `78a64fb` — perf: batch the bulk _id $in filter to stay inside the BSON command limit**

## What I implemented

All in `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` (Correction 1 — nothing was
touched in Query, and the Query↔Storage bulk seam is unchanged):

- `private const int DefaultMaxIdsPerBulkCommand = 50_000;` plus `private static int MaxIdsPerBulkCommand`
  (the applied ceiling; a field only so the test seam can shrink it).
- `internal static IEnumerable<List<BsonValue>> BatchIds(List<BsonValue> ids, int batchSize)` — consecutive
  slices, in order, of at most `batchSize`. `batchSize` is a required parameter (not defaulted from the const)
  so the arithmetic is unit-testable server-free at a small size. Empty input yields no batches. The
  single-batch case yields the caller's own list rather than a `GetRange` copy, so the ceiling costs nothing
  on the common path (the batch is only ever read into a filter, never mutated).
- `internal static IDisposable OverrideMaxIdsPerBulkCommand(int)` + a private `MaxIdsPerBulkCommandOverride`
  scope class (the test seam — see below).
- `DeleteByIds`, `DeleteByIdsAsync`, `UpdateByIds`, `UpdateByIdsAsync` now loop over `BatchIds(ids,
  MaxIdsPerBulkCommand)`, building a fresh `Builders<BsonDocument>.Filter.In("_id", batch)` per batch and
  accumulating into `long`. Signatures are unchanged (Correction 2). Deletes accumulate `DeletedCount`; updates
  accumulate both `MatchedCount` and `ModifiedCount` and pass both to `ExecutedBulkUpdate`. Both return
  `checked((int)total)`.

Per the corrections, I did **not** add `CombineBulkCommand` (Correction 3): the loop still calls
`RenderBulkCommand(updateLogger, filter, update, targetIdQuery)` and still passes it as a `() => …` thunk, so
Task 16's single redaction gate and Task 5's laziness are both untouched. There was no `PhaseOneResult` to
consume (Correction 4), and the single-command executors are byte-for-byte unchanged.

Comment claims are limited to what is true: the 16 MB per-command BSON limit and large-`$in` query-planning
cost, atomicity preserved because every batch runs on the one session phase 1 already opened, and an explicit
statement that batching buys **no** relief from `transactionLifetimeLimitSeconds` (more round trips inside one
transaction can only make that limit easier to reach).

## The test seam I chose, and why it is safe

`OverrideMaxIdsPerBulkCommand(int)` returns an `IDisposable` scope that restores the previous value on
`Dispose`, used as `using (MongoBulkOperationExecutor.OverrideMaxIdsPerBulkCommand(2)) { … }`.

- **Internal only.** `internal static` on an already-`internal` class, reachable from the test assemblies
  purely through the existing `InternalsVisibleTo`. Per `AGENTS.md`, internal changes are explicitly not
  public-API breaks. No public knob, no options plumbing, nothing threaded through `MongoBulkPlan`.
- **Restores reliably.** The scope's `Dispose` writes the captured previous value back, so a failing or
  throwing test still restores it — unlike a bare assignment or a `finally` a future test author could forget.
- **A plain static is acceptable here** because both test assemblies set
  `[assembly: CollectionBehavior(DisableTestParallelization = true)]`, so no two tests run concurrently to
  observe each other's override. This is stated in the code comment, not just here.
- **Guarded.** A non-positive batch size would make `BatchIds` loop forever, so the scope's constructor throws
  `ArgumentOutOfRangeException` instead.

## Multi-batch integration test — evidence

Two theories (sync + async each, so all four `*ByIds` loops are covered):

- `ExecuteDeleteTests.ExecuteDelete_two_phase_splits_phase_2_across_batches_inside_one_transaction`
- `ExecuteUpdateTests.ExecuteUpdate_two_phase_splits_phase_2_across_batches_inside_one_transaction`

Shape: seed 7 documents, override the ceiling to 2 → 4 batches (2+2+2+1). The bulk op runs through a dedicated
`MongoClient` with `CommandCaptureSpy` attached, so command counts are observed on the wire rather than
inferred. `GetLogMessageByEventId` is deliberately **not** used (Correction 6); a new plural accessor
`SpyLoggerProvider.GetLogMessagesByEventId` was added alongside it, leaving the existing method's
`Assert.Single` behaviour untouched.

Observed (all four cases passing):

| Claim | Delete test | Update test |
|---|---|---|
| total affected | `deleted == 7` | `updated == 7`, and 7 documents now `"archived"` |
| commands on the wire | `delete` × **4** | `update` × **4** |
| one transaction | `commitTransaction` × **1** | `commitTransaction` × **1** |
| diagnostics | 4 × `ExecutingBulkDelete` (3 × `Targets=2`, 1 × `Targets=1`), 1 × `ExecutedBulkDelete` with `Deleted=7` | 4 × `ExecutingBulkUpdate`, 1 × `ExecutedBulkUpdate` with `Matched=7, Modified=7` |

### Proof of falsifiability — three injected bugs

Each bug was injected into the shipped source, rebuilt (`Debug EF10`), and the tests re-run; the source was
restored from a pristine copy afterwards (verified: no `INJECTED` markers remain, BOM intact, and the final
committed diff was re-reviewed).

1. **Batching removed** — `BatchIds`'s loop body replaced with `yield return ids;` (the pre-task behaviour):

   ```
   Failed ExecuteDeleteTests.ExecuteDelete_two_phase_splits_phase_2_across_batches_inside_one_transaction(async: False)
      Assert.Equal() Failure: Values differ
      Expected: 4
      Actual:   1
   ```
   All four cases failed the same way (delete and update, sync and async). This is the assertion that makes the
   test impossible to pass on unbatched code.

2. **Accumulation broken** — `deleted += result.DeletedCount` → `deleted = …`, and `matched += …` → `matched = …`
   (in all four methods):

   ```
   Failed ExecuteDeleteTests.ExecuteDelete_two_phase_splits_phase_2_across_batches_inside_one_transaction(async: False)
      Assert.Equal() Failure: Values differ
      Expected: 7
      Actual:   1
   ```
   All four cases failed (the reported total collapses to the last batch's 1).

3. **Atomicity broken** — a per-batch commit modelled faithfully by adding
   `if (session != null) { session.CommitTransaction(); session.StartTransaction(); }` inside `DeleteByIds`'s
   loop (i.e. the plausible alternative design where each batch is its own transaction):

   ```
   Failed ExecuteDeleteTests.ExecuteDelete_two_phase_splits_phase_2_across_batches_inside_one_transaction(async: False)
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   4
      at …/ExecuteDeleteTests.cs:line 532
   ```
   The async case passed, as expected — the injection was sync-only, which also confirms the assertion is
   discriminating rather than incidentally failing.

## Unit test

`tests/MongoDB.EntityFrameworkCore.UnitTests/Storage/BulkIdBatchingTests.cs` (Storage, not Query — Correction 5;
`public static class` with `[Fact]`/`[Theory] public static` copied from its neighbour
`BulkTransactionErrorClassificationTests.cs`). `[Theory]` over `(0,0) (1,1) (2,1) (3,1) (4,2) (6,2) (7,3)` at
`batchSize: 3`, asserting batch count, that every batch is in `[1, 3]` (no empty batch — which would mean a
command that can only match nothing — and none over the ceiling), and, load-bearing,
`Assert.Equal(ids, batches.SelectMany(b => b))` — a sum-of-counts check alone would pass for an off-by-one that
dropped one id while duplicating another.

## Diagnostics semantics decision

**I kept the plan's choice: `Executing` fires once per batch (with that batch's `Targets`), `Executed` fires
once with the accumulated totals.** So N `Executing` pair with 1 `Executed`, and no single event reports the
intended total up front. The reasoning is documented in a comment above `DeleteByIds`:

- Each `Executing` describes exactly one command that is about to be sent, and its `Command` value renders
  *that* command's filter. An `Executing` fired once before the loop with the run total would have nothing
  truthful to render as `Command` — there is no single filter at that point — so it would have to either omit
  the command or present one batch's filter as if it were the whole operation. Per-batch is therefore the only
  shape in which `Executing.Command` stays accurate, which matters because `Command` is the value
  `BulkOperationsCommandTests` pins.
- `Executed` reports the outcome of the operation as a whole, which is exactly the total.
- Consumers see extra `Executing` events **only** above `MaxIdsPerBulkCommand` ids; below that the event stream
  is identical to before. Each batch's `Command` repeats phase 1's pipeline, which is the accurate report —
  phase 1 ran once and its ids feed every batch. Both points are in the comment.

I do not think firing once with the total is better, for the `Command`-truthfulness reason above, so nothing
was changed unilaterally.

No existing test needed relaxing: every pre-existing fixture is far below 50,000 ids, so they all still see a
single batch and the `Assert.Single`/`.Single(…)` assertions in `BulkOperationsCommandTests`,
`ExecuteDeleteTests` and `ExecuteUpdateTests` continue to hold unchanged.

## TDD evidence

**RED** (`dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`), before any implementation:

```
tests/…/Query/ExecuteDeleteTests.cs(520,43): error CS0117: 'MongoBulkOperationExecutor' does not contain a definition for 'OverrideMaxIdsPerBulkCommand'
tests/…/Query/ExecuteUpdateTests.cs(781,43): error CS0117: 'MongoBulkOperationExecutor' does not contain a definition for 'OverrideMaxIdsPerBulkCommand'
tests/…/Storage/BulkIdBatchingTests.cs(45,50): error CS0117: 'MongoBulkOperationExecutor' does not contain a definition for 'BatchIds'
Build FAILED.
```

That is a compile failure, not an assertion failure — the expected RED for tests of an API that does not exist
yet, as the brief anticipated.

**GREEN** after implementing:

```
$ dotnet test … -c "Debug EF10" --no-build --filter "FullyQualifiedName~BulkIdBatchingTests"
Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7 (UnitTests)

$ dotnet test …FunctionalTests… -c "Debug EF10" --no-build --filter "FullyQualifiedName~splits_phase_2_across_batches"
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4
```

## What I tested, and results

All runs with `MONGODB_URI` and `ATLAS_URI` unset (TestContainers atlas-local).

| Suite | Config | Result |
|---|---|---|
| Builds — `Debug EF8`, `Debug EF9`, `Debug EF10` | all three | Build succeeded, 0 errors, 0 CS warnings |
| `ExecuteDeleteTests` + `ExecuteUpdateTests` + `BulkOperationsCommandTests` + `BulkOperationsInheritanceTests` (+ `BulkOperationsUnsupportedOnEf8Tests`) | EF10 | 91 passed, 1 skipped (the pre-existing standalone-deployment skip), 0 failed |
| same | EF9 | 92 passed, 1 skipped, 0 failed |
| `BulkIdBatchingTests` | EF10 / EF9 | 7 passed / 7 passed |
| `NorthwindBulkUpdatesMongoTest` | **EF10: 181/181** | 0 failed — matches the stated baseline |
| `NorthwindBulkUpdatesMongoTest` | **EF9: 169/169** | 0 failed — matches the stated baseline |
| Full `UnitTests` assembly | EF10 | 297 passed, 0 failed |
| Full `FunctionalTests` assembly | EF10 | 1802 passed, 52 skipped (pre-existing: encryption without `CRYPT_SHARED_LIB_PATH`, etc.), 0 failed |

Test output is pristine — no stray console writes; the only `[SKIP]` lines are pre-existing skips.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Storage/BulkIdBatchingTests.cs` (new)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/SpyLogging.cs`

BOMs preserved (`MongoBulkOperationExecutor.cs` and `SpyLogging.cs` verified as still `EF BB BF`; the new unit
test file has no BOM, matching its neighbour `BulkTransactionErrorClassificationTests.cs`).

## Self-review findings

- **Does each new test fail if the behaviour regresses?** Yes — demonstrated with three separate injected bugs
  above, each caught by a different assertion. The unit test's `Assert.Equal(ids, batches.SelectMany(…))` also
  catches slice-offset errors that a count check would miss.
- **Laziness thunk intact?** Yes — `() => RenderBulkCommand(…)` at all four call sites;
  `Executing_events_do_not_render_the_command_when_nobody_is_listening` still passes.
- **Single redaction gate untouched?** Yes — `RenderBulkCommand` is not modified at all, and no `"?"` sentinel
  comparison was introduced anywhere. All eight `Assert.Equal("?", closed.Command)` assertions still pass.
- **Are all comments true?** Reviewed line by line. Atomicity is claimed only on the basis of the shared
  session (and is now pinned by the `commitTransaction` assertion rather than asserted in prose); the
  `transactionLifetimeLimitSeconds` sentence explicitly says batching does *not* help there; the "16 MB /
  ~500k ObjectIds" figure and the query-planning cost are the only ceilings claimed. I fixed one comment
  inaccuracy found during review (an overwriting loop reports the *last* batch's 1, not 2).
- **Empty-id case:** `BatchIds([])` yields no batches, so no command is sent; the two-phase callers still
  short-circuit before phase 2, so this is unreachable in practice, and the unit test covers `(0, 0)`.
- **`MongoEventId`:** untouched (no new events).
- **Public API:** unchanged — the only new members are `internal`.
- One stylistic note for a reviewer: `MaxIdsPerBulkCommand` is a mutable `private static` field with
  PascalCase, matching the file's/codebase's PascalCase static-field habit; the comment above it says
  explicitly that it is mutable only for the test seam and is always `DefaultMaxIdsPerBulkCommand` outside a
  test.

## Concerns

None blocking. Two things a reviewer may want to weigh in on:

1. **The diagnostics asymmetry is observable to `DiagnosticSource` consumers** above 50,000 ids (N `Executing`,
   1 `Executed`). I believe this is right for the `Command`-truthfulness reason above, and it is documented in
   the code, but it is a genuine behaviour change for a very large bulk operation and worth a second opinion.
2. **`BatchIds` returns the caller's list in the single-batch case** rather than a copy, to avoid an O(n) copy
   on the common path. It is safe today (batches are only read) and commented as such, but it is an implicit
   contract a future caller could violate by mutating a batch.

---

# Task 18 — review fix report (round 1)

**Commit: `812ad18` — docs: document the per-batch bulk TargetCount semantics and tighten the batching tests**

All seven items addressed (Important 1 + Minors 2-7). Minors 8 and 9 left untouched as deferred.

## Important 1 — the four public XML docs now describe per-batch `TargetCount`

Amended all four sites the review named. Each now says `TargetCount` is the number of target ids in the `$in`
filter of the command **this event describes**, notes that a two-phase operation exceeding the per-command
ceiling is sent as several commands each raising its own event with its own share, states that N `Executing`
events pair with one `Executed` (and that it is the `Executed` event's counts that report the operation as a
whole), and notes that all such commands run inside the one transaction phase one opened.

- `Diagnostics/MongoBulkDeleteEventData.cs` — rewrote the `TargetCount` `<summary>` and added a `<remarks>`
  block (cross-references `DeleteCount`).
- `Diagnostics/MongoBulkUpdateEventData.cs` — same shape (cross-references `MatchedCount`/`ModifyCount`).
- `Diagnostics/MongoLoggerUpdateExtensions.cs` — the `targetCount` `<param>` on `ExecutingBulkDelete` and on
  `ExecutingBulkUpdate`, each pointing at `MongoEventId.ExecutedBulkDelete`/`ExecutedBulkUpdate` as the event
  carrying the total.

The private comment in `MongoBulkOperationExecutor.cs` is unchanged and still carries the *rationale*; the
public docs now carry the *contract*, so a `DiagnosticSource` consumer reading only the shipped NuGet XML learns
the semantics. No signature, event ID, or behaviour changed — documentation only.

## Minor 2 — the update test now distinguishes `Matched` from `Modified`

`ExecuteUpdateTests.cs`. Two of the seven seeded documents now start as `"archived"`, so they match the `$in`
filter and are written by the `$set` but are not modified. Expected outcome is `Matched=7, Modified=5`
(`expectedModified = seedCount - alreadyArchivedCount`).

The two pre-archived documents are deliberately placed at `Quantity` 0 and 3 — i.e. in *different* batches
(batches are `[0,1] [2,3] [4,5] [6]`) — and the final single-document batch is one that genuinely changes, so a
loop reporting the last batch's `Modified` (1) or `Matched` (1) cannot coincide with either expected total. The
comment at the top of the test was rewritten: it no longer claims only "last-batch value" coverage, and now
names the cross-assignment bug and explains why the counters are seeded to differ.

**Evidence — the actual message.** With the assertion temporarily replaced by `Assert.Fail(executed)` to dump
the literal rendered event:

```
Executed Bulk Update (56.5394 ms) Collection='…ExecuteUpdate_two_phase_splits_phase_2_across_batches_inside_one_transactionFalse', Matched=7, Modified=5
Executed Bulk Update (10.3106 ms) Collection='…ExecuteUpdate_two_phase_splits_phase_2_across_batches_inside_one_transactionTrue',  Matched=7, Modified=5
```

Both sync and async report `Matched=7, Modified=5` — the counters are genuinely separated, not asserted equal.

**Evidence — the bug the reviewer named is now caught.** Injected `modified += result.MatchedCount` (the
cross-assignment) into both `UpdateByIds` and `UpdateByIdsAsync`, rebuilt, re-ran:

```
Failed …ExecuteUpdate_two_phase_splits_phase_2_across_batches_inside_one_transaction(async: False)
   Assert.Contains() Failure: Sub-string not found
   String:    "Executed Bulk Update (55.5979 ms) Collect"···
   Not found: "Modified=5"
Failed …(async: True) — same
Failed!  - Failed: 2, Passed: 0
```

Both cases fail. Under the previous seeding (all 7 documents changing, both totals 7) this bug passed silently —
so this is a real coverage gain, not a restatement. Source restored from a pristine copy afterwards; verified no
`INJECTED` markers remain anywhere in `src/` or `tests/`, and the BOM is intact.

## Minor 3 — the `commitTransaction` assertion is no longer readable as stronger than it is

`ExecuteDeleteTests.cs`, added to the claims comment:

> Claim 3 is narrower than it may read: it proves no *extra* commit happened, i.e. the batches were not each
> wrapped in their own transaction. It does not prove every batch carried the session, because a batch sent
> without it (`collection.DeleteMany(filter)` rather than `DeleteMany(session, filter)`) would still produce
> these same 4 deletes and 1 commit. Pinning that would need the spy to capture command *documents* and inspect
> `lsid`/`txnNumber`, which `CommandCaptureSpy.CommandNames` deliberately does not expose; the session-passing
> itself is instead visible by inspection in the four `*ByIds` loops.

No spy change was made, per the instruction not to build document capture.

## Minor 4 — wire counts are captured before the verification read

Both tests now snapshot into locals immediately after the `using` block that scopes the override, before any
verification read runs on the spy client:

```csharp
var deleteCommandCount = commandSpy.CommandNames.Count(name => name == "delete");
var commitCount = commandSpy.CommandNames.Count(name => name == "commitTransaction");
```

The delete test carries the explanatory comment (reads only produce `aggregate` today, but counting afterwards
would couple these assertions to future changes in how reads are issued or in read transactionality, and the
failure would look like a batching bug); the update test cross-references it. Assertion order is otherwise
unchanged, so the counts asserted are exactly the bulk operation's own traffic.

## Minor 5 — single-batch fast path dropped

`BatchIds` is now just the loop:

```csharp
internal static IEnumerable<List<BsonValue>> BatchIds(List<BsonValue> ids, int batchSize)
{
    for (var start = 0; start < ids.Count; start += batchSize)
    {
        yield return ids.GetRange(start, Math.Min(batchSize, ids.Count - start));
    }
}
```

The `ids.Count <= batchSize` branch and its three-line ownership-contract comment are gone; the loop already
handles both that case and `ids.Count == 0`. Every batch is now uniformly a fresh `GetRange` copy — one
ownership rule instead of two — and the unit test's `Assert.Equal(ids, batches.SelectMany(…))` is no longer
tautological for the single-batch rows. All 7 unit-test rows still pass. I agree with the trade: the saved
reference copy was unmeasurable against a round trip that serializes the same values.

## Minor 6 — mutable static renamed

`MaxIdsPerBulkCommand` → `_maxIdsPerBulkCommand` (the .NET/EF convention for mutable static state), at all 8 use
sites plus the 3 comment references. `DefaultMaxIdsPerBulkCommand` (the actual `const`) keeps PascalCase, so the
naming now distinguishes the constant from the mutable field at a glance. Its comment now opens "Mutable state,
not a constant". Verified by grep that no PascalCase `MaxIdsPerBulkCommand` field reference remains (only
`DefaultMaxIdsPerBulkCommand`, `OverrideMaxIdsPerBulkCommand`, `MaxIdsPerBulkCommandOverride`, and the
`maxIdsPerBulkCommand` parameter).

## Minor 7 — comment gap closed

Appended to the phase-1-pipeline clause:

> That does mean the pipeline is re-rendered once per batch when a listener is attached, but only trivially so —
> `RenderPhaseOnePipeline` just wraps the stages the driver already captured in `LoggedStages`, so the repeat
> costs one `BsonArray` allocation per batch and no re-translation.

## Verification

All with `MONGODB_URI` and `ATLAS_URI` unset.

| Suite | Config | Result |
|---|---|---|
| Builds — `Debug EF8`, `Debug EF9`, `Debug EF10` | all three | Build succeeded, 0 errors, 0 CS warnings |
| The 4 multi-batch theory cases | EF10 | 4 passed (individually listed: delete/update × sync/async) |
| `ExecuteDeleteTests` + `ExecuteUpdateTests` + `BulkOperationsCommandTests` + `BulkOperationsInheritanceTests` + `BulkOperationsUnsupportedOnEf8Tests` | EF10 | 91 passed, 1 pre-existing skip, 0 failed |
| same | EF9 | 92 passed, 1 skip, 0 failed |
| Full `UnitTests` assembly | EF10 / EF9 | 297 passed / 304 passed, 0 failed |
| **`NorthwindBulkUpdatesMongoTest`** | **EF10** | **181/181, 0 failed** — baseline held |
| **`NorthwindBulkUpdatesMongoTest`** | **EF9** | **169/169, 0 failed** — baseline held |
| Full `FunctionalTests` assembly | EF10 | 1802 passed, 52 pre-existing skips, 0 failed |

Commands used (representative):

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   # and EF9, EF8
env -u MONGODB_URI -u ATLAS_URI dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/… \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~splits_phase_2_across_batches"
env -u MONGODB_URI -u ATLAS_URI dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/… \
  -c "Debug EF9" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Output is pristine — the only `[SKIP]` lines are pre-existing skips.

## Files changed in this round

- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkDeleteEventData.cs`
- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkUpdateEventData.cs`
- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerUpdateExtensions.cs`
- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

BOMs preserved. `BulkIdBatchingTests.cs` and `SpyLogging.cs` needed no further change.

## Concerns after this round

The two concerns from the first report are resolved or closed: the diagnostics asymmetry is now documented in
public XML rather than only a private comment (Important 1), and the `BatchIds` ownership asymmetry is gone
(Minor 5). No new concerns.
