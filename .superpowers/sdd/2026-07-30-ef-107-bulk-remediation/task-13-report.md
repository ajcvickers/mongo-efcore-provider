# Task 13 Report: Only require two-phase when paging is present

## Summary

Implemented exactly what the brief specifies, with the four corrections applied. `ClassifyBulkSource`
(`MongoQueryableMethodTranslatingExpressionVisitor.cs`) now only routes a bulk delete/update to the two-phase
path when `Skip`/`Take` is present; `Where`/`OrderBy`/`OrderByDescending`/`ThenBy`/`ThenByDescending`/`Distinct`
stay on the atomic single-command path. `BuildFilter` (`MongoShapedQueryCompilingExpressionVisitor.cs`) was
updated to step over the ordering/`Distinct` operators instead of stopping at the first non-`Where`, so it keeps
collecting `Where` predicates on both sides of an `OrderBy`/`Distinct` in the now-widened single-command chain.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` — `ClassifyBulkSource` (brief Step 3, verbatim body/doc as specified).
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` — `BuildFilter` (brief Step 4, verbatim body as specified; brief's Files list never named this file — see Correction 1 in the task brief, confirmed by locating the method at `:462` in the working tree).
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` — new tests (Steps 1 and 5, with Correction 2's fixed predicate and Correction 3's parameter ordering), plus two stale comment updates and one call-site fix (see below).

Commit: `c8c7d2d` — "perf: keep ordering-only and Distinct bulk operations on the atomic single-command path".

## TDD evidence

### Step 2 — RED

Command:
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDelete_with_OrderBy_and_no_paging|FullyQualifiedName~ExecuteDelete_with_Distinct_and_no_paging|FullyQualifiedName~ExecuteDelete_with_OrderBy_Take_still"
```

Actual output (against tests added, pre-implementation):
```
[xUnit.net 00:00:10.36]     ExecuteDeleteTests.ExecuteDelete_with_OrderBy_and_no_paging_works_under_AutoTransactionBehavior_Never [FAIL]
[xUnit.net 00:00:10.40]     ExecuteDeleteTests.ExecuteDelete_with_Distinct_and_no_paging_uses_the_single_command_path [FAIL]
[xUnit.net 00:00:10.42]     ExecuteDeleteTests.ExecuteDelete_with_OrderBy_and_no_paging_uses_the_single_command_path [FAIL]

  Failed ...ExecuteDelete_with_OrderBy_and_no_paging_works_under_AutoTransactionBehavior_Never
   System.InvalidOperationException : This bulk delete or update uses ordering, paging, or 'Distinct', which the
   MongoDB provider executes as a two-phase operation requiring a transaction. The context's AutoTransactionBehavior
   is 'Never', so open an explicit transaction (Database.BeginTransaction) around the call.

  Failed ...ExecuteDelete_with_Distinct_and_no_paging_uses_the_single_command_path
   Assert.Contains() Failure: Sub-string not found
   String:    "Executing Bulk Delete (24.3881 ms) Collec"···
   Not found: "Strategy=SingleCommand"

  Failed ...ExecuteDelete_with_OrderBy_and_no_paging_uses_the_single_command_path
   Assert.Contains() Failure: Sub-string not found
   String:    "Executing Bulk Delete (1.1009 ms) Collect"···
   Not found: "Strategy=SingleCommand"

Failed!  - Failed:     3, Passed:     1, Skipped:     0, Total:     4
```

Exactly the three `no_paging` tests failed for the expected reasons (`Strategy=TwoPhase` logged instead of
`SingleCommand`; the `AutoTransactionBehavior.Never` case throws because the old classifier still demanded a
transaction), and `ExecuteDelete_with_OrderBy_Take_still_uses_the_two_phase_path` passed — matching the brief's
Step 2 expectation exactly.

(Note: getting to this RED state first required fixing a pre-existing positional caller —
`ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected` called
`CreateSeededContext("...")` positionally to override the `[CallerMemberName]` collection name. Adding
`loggerFactory` before `collectionName` (Correction 3) made that a compile error; fixed by naming the argument
`collectionName: "..."` at that call site — the only such caller in the file.)

### Step 2 — GREEN

Same command, after implementing Steps 3 and 4:
```
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 379 ms
```

### Step 5 — discrimination check (Correction 2)

Ran the new interleaved-`Where` test on its own first:
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~ExecuteDelete_with_Where_either_side_of_OrderBy"
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 318 ms
```

Then temporarily broke `BuildFilter`'s walk so it stops at `OrderBy` instead of stepping over it (inserted
`if (method.Name == nameof(Queryable.OrderBy)) break; // TEMP discrimination probe` right after
`expression = call.Arguments[0];`).

**Correction (fix round, Minor 6):** my original description of what this drops was wrong. The captured chain
is built outside-in from the fluent call order: the outermost node is the *last* `Where` written
(`Where(o => o.Quantity > 15)`), wrapping `OrderBy`, wrapping the *first* `Where` (`o.Status == "open"`). The
walk visits outer-to-inner, so it processes `Where(Quantity > 15)` first (collecting it into `combinedBody`),
then reaches the `OrderBy` node — where the probe's `break` exits the `while` loop entirely, immediately after
advancing `expression` to the inner `Where(Status == "open")` node but *before* that node is ever matched
against the switch. So the predicate silently dropped is the **inner** `Where(o.Status == "open")`, not the
trailing one. The resulting filter is just `Quantity > 15`, which matches both `{open, 20}` and `{closed, 30}` —
hence the observed `Actual: 2` (both survive `Quantity > 15` once `Status == "open"` no longer narrows anything).
The evidence itself is unchanged and still stands as the correct discrimination proof; only the prose describing
*which* predicate was dropped was inaccurate.

```
[xUnit.net 00:00:09.21]     ExecuteDelete_with_Where_either_side_of_OrderBy_applies_both_predicates [FAIL]
  Assert.Equal() Failure: Values differ
  Expected: 1
  Actual:   2
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 266 ms
```

This confirms the test genuinely discriminates: with the inner `Where(Status == "open")` silently dropped, the
delete count regresses from 1 to 2 (only the trailing `Quantity > 15` predicate applied, matching both the
`{open,20}` and `{closed,30}` documents). The probe was then removed and the fix restored; re-ran the same
filter to confirm GREEN again:
```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 318 ms
```

## Full verification (Step 6, plus corrections)

```
dotnet test ... -c "Debug EF10" --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
  -> Passed! Failed: 0, Passed: 74, Skipped: 1, Total: 75  (the 1 skip is the pre-existing
     ExecuteDelete_two_phase_on_standalone_throws_actionable_error, unrelated to this task)

dotnet test ... -c "Debug EF9" --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
  -> Passed! Failed: 0, Passed: 62, Skipped: 1, Total: 63

dotnet test ... -c "Debug EF10" --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
  -> Passed! Failed: 0, Passed: 181, Skipped: 0, Total: 181   (matches baseline: 181/181)

dotnet test ... -c "Debug EF9" --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
  -> Passed! Failed: 0, Passed: 169, Skipped: 0, Total: 169   (matches baseline: 169/169; this run was
     omitted from the brief's Step 6 but required per task instructions)
```

`dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"` and `-c "Debug EF9"` both succeed with 0
warnings/errors (bulk code is `#if !EF8` and doesn't compile for EF8; `ExecuteDeleteTests.cs` stays inside its
existing `#if !EF8` guard).

**No outcome assertion changed anywhere.** I specifically checked
`ExecuteDelete_with_OrderBy_and_no_Take_deletes_everything` and `ExecuteDelete_with_Distinct_deletes_matching`
(the brief's flagged "bare OrderBy/Distinct" shapes) — neither asserts strategy or transaction behavior, both
only assert delete counts, and both still pass unmodified in that respect. I only updated their inline comments
(see below), which described the old two-phase mechanism and had gone stale; I left the assertions untouched.

I did not find any test asserting *strategy* or *transaction* behavior for a bare-OrderBy/Distinct (no Skip/Take)
shape that would legitimately need to change — the four such assertions (`Strategy=SingleCommand` for OrderBy and
Distinct without paging, and the `AutoTransactionBehavior.Never` no-throw case) are the ones added by this task
itself in Step 1, not pre-existing ones.

## Corrections applied

1. **Two-file span.** Verified `ClassifyBulkSource` at `MongoQueryableMethodTranslatingExpressionVisitor.cs:408`
   and `BuildFilter` at `MongoShapedQueryCompilingExpressionVisitor.cs:462` (not in the brief's Files list) before
   editing either. Both stayed in Query; nothing moved to Storage.
2. **Vacuous Step 5 test.** Used `.Where(o => o.Quantity > 15)` (not `> 1`) as the trailing predicate so only
   `{open, 20}` survives both predicates — 1 deleted, 2 remaining, matching the brief's asserted counts while
   actually discriminating (proven above).
3. **`CreateSeededContext` parameter ordering.** Added `ILoggerFactory? loggerFactory = null` *before*
   `[CallerMemberName] string? collectionName = null`, forwarding to `SingleEntityDbContext.Create(collection,
   ConfigureModel)` when null and to `SingleEntityDbContext.Create(collection, (LoggerFactory)loggerFactory,
   ConfigureModel)` otherwise. The cast is required because the only logger-accepting overload of
   `SingleEntityDbContext.Create` takes a concrete `Microsoft.Extensions.Logging.LoggerFactory`, not
   `ILoggerFactory`; `SpyLoggerProvider.Create()` (the only real caller) returns a concrete `LoggerFactory`, so the
   cast always succeeds in practice. Found and fixed one existing positional caller that broke under the new
   parameter order (see RED section above) by naming its argument.
4. **Stale invariant comment.** Rewrote the comment above `BuildFilter`'s while-loop to describe the new
   invariant (Where + ordering/Distinct admitted; Skip/Take excluded) instead of the old "only Where" claim.

Regarding the brief's/correction's mention of "keep the `Debug.Assert` on the strategy": I searched the current
`MongoShapedQueryCompilingExpressionVisitor.cs` (and the whole `src/` tree) for any `Debug.Assert` referencing
`BuildFilter` or `nonQuery.Strategy` and found none — it does not exist in this file at eb28d0d. I did not add
one, since the brief's shown Step 4 replacement code doesn't include one either and I didn't want to add
speculative code not called for by the brief (YAGNI). Flagging this as a minor discrepancy between the
correction note and the actual tree, not a functional gap: `CreateBulkPlan` already structurally guarantees
`BuildFilter` is only wired up when `Strategy == SingleCommand` (`BuildFilter = isTwoPhase ? null : ...`), so
there is no invariant left unguarded.

## Additional changes beyond the brief (small, in scope)

- Updated two stale inline comments that described the pre-fix two-phase mechanism for bare `OrderBy`/`Distinct`
  shapes (`ExecuteDelete_with_OrderBy_and_no_Take_deletes_everything` and `ExecuteDelete_with_Distinct_deletes_matching`).
  Assertions in both are untouched.

## Self-review

- **Completeness vs. brief:** all of Steps 1–7 done, including the two corrections' extra work (call-site fix,
  comment updates).
- **Naming:** new test names read as intended (`..._uses_the_single_command_path`, `..._still_uses_the_two_phase_path`).
- **YAGNI:** did not add the speculative `Debug.Assert` discussed above; did not touch `SingleEntityDbContext.cs`
  (cast at the call site was simpler and kept the change surface to the three brief-scoped files).
- **Do tests verify real behavior / could each fail:** yes — demonstrated for the interleaved-`Where` test via the
  discrimination check; the other three new tests were shown failing pre-fix in the RED run above, for the exact
  reasons expected.
- **Test output pristine:** no skips added; the one pre-existing skip (`ExecuteDelete_two_phase_on_standalone_throws_actionable_error`) is unrelated and untouched.
- **Comments accurate post-change:** updated the `ClassifyBulkSource` doc comment (per brief), the `BuildFilter`
  invariant comment (per Correction 4), and the two stale test comments noted above.

## Concerns

None that block. The one open item is the Debug.Assert discrepancy noted above (Correction 1's associated
correction text references something not present in the current tree) — flagging for visibility, not blocking,
since the invariant it would have guarded is already structurally enforced elsewhere in `CreateBulkPlan`.

---

## Fix round 1 (review response)

Review verdict on the first pass: spec-compliant against all four amendments, quality Approved, both Northwind
baselines matched. One Important, three Minors (of which Minors 1/2/4 were not flagged against this task —
only Important 1, Minor 3, Minor 5, and Minor 6 applied here). All four addressed below.

### Important 1 — three user-facing messages still blamed ordering/Distinct for the two-phase/transaction requirement

Only `Skip`/`Take` reach the two-phase path now, but three messages still said "ordering, paging, or 'Distinct'":

- `Storage/MongoBulkOperationExecutor.cs:539` (`NoAutoTransactionError`) and `:545` (`StandaloneTransactionError`):
  changed `"This bulk delete or update uses ordering, paging, or 'Distinct', which the MongoDB provider executes..."`
  to `"This bulk delete or update uses paging ('Skip'/'Take'), which the MongoDB provider executes..."`.
- `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs:433` (`EnsureBulkKeyOrThrow`): changed
  `"the entity must have a primary key to use ordering, paging, or Distinct in a bulk delete or update."` to
  `"the entity must have a primary key to use paging ('Skip'/'Take') in a bulk delete or update."`.

Swept for other stale prose per the review's instruction (grepped `src/` and `tests/` for
`ordering, paging`, `paging, or`, `Distinct` near `transaction`/`two-phase`/`OrderBy` near `transaction`, and
`requires a transaction` / `requiring a transaction`). Found one more, in a test comment (not a message, but
still incorrect prose about the same mechanism, so fixed under the same umbrella):

- `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindBulkUpdatesMongoTest.cs:36`: changed
  `"Ordering/paging/Distinct-scoped bulk operations are supported via the two-phase (_id-projection) execution
  path and call base."` to `"Paging (Skip/Take)-scoped bulk operations are supported via the two-phase
  (_id-projection) execution path; ordering-only and Distinct-only shapes (no paging) don't change the affected
  set and stay on the atomic single-command path instead. Both call base."`

Checked and confirmed **not** stale (left unchanged): `ThrowBulkSourceNotSupported`'s message
(`MongoQueryableMethodTranslatingExpressionVisitor.cs:493-496`) lists `OrderBy`/`.../Distinct` as operators that
*can* scope a bulk operation at all — that's still true and isn't a transaction/key claim. The
`ExecuteUpdateTests.cs:432-434` comment and the `MongoBulkPlan.cs`/`MongoBulkDeleteEventData.cs`/
`MongoBulkUpdateEventData.cs`/`MongoLoggerUpdateExtensions.cs` "two-phase" mentions describe the two-phase
mechanism itself (phase counts, strategy enum) without tying it to ordering/Distinct. Did not touch
`README.md` per the review's explicit instruction (Task 25 owns it).

Per the review's note, no test in `tests/` asserts these exact strings, so the message-text edits were
unguarded by any existing assertion (confirmed via the same grep, plus the full test run below).

### Minor 3 — `ILoggerFactory?` forced a downcast

Changed `CreateSeededContext`'s parameter from `ILoggerFactory? loggerFactory = null` to
`LoggerFactory? loggerFactory = null` (the concrete type), dropping the `(LoggerFactory)loggerFactory` cast —
now `SingleEntityDbContext.Create(collection, loggerFactory, ConfigureModel)` is called directly. This makes a
future type mismatch a compile error instead of a runtime `InvalidCastException`. Build succeeded immediately
(no other caller of `CreateSeededContext` passes a non-`LoggerFactory` `ILoggerFactory`), confirming the
narrower type was never actually needed.

### Minor 5 — two coverage gaps: bare `Skip` and `ThenBy`

Added exactly two tests, in the style of the existing ones:

```csharp
[Fact]
public void ExecuteDelete_with_bare_Skip_and_no_Take_still_uses_the_two_phase_path()
{
    var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
    using var db = CreateSeededContext(loggerFactory);

    // Skip alone (no Take) still changes the affected set, so it must stay two-phase even without Take.
    var deleted = db.Entities.OrderBy(o => o.Quantity).Skip(1).ExecuteDelete();

    Assert.Equal(2, deleted);
    Assert.Contains("Strategy=TwoPhase", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
}

[Fact]
public void ExecuteDelete_with_Where_around_OrderBy_ThenBy_uses_the_single_command_path()
{
    var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
    using var db = CreateSeededContext(loggerFactory);

    // Covers the ThenBy arm of both switches, and predicate collection across an OrderBy+ThenBy pair.
    // Seed data is {open,10}, {open,20}, {closed,30} -- only {open,20} satisfies both predicates.
    var deleted = db.Entities
        .Where(o => o.Status == "open")
        .OrderBy(o => o.Status)
        .ThenBy(o => o.Quantity)
        .Where(o => o.Quantity > 15)
        .ExecuteDelete();

    Assert.Equal(1, deleted);
    Assert.Equal(2, db.Entities.Count());
    Assert.Contains("Strategy=SingleCommand", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
}
```

The first pins bare `Skip` (no `Take`) still routes to `TwoPhase` in `ClassifyBulkSource`'s Skip/Take arm. The
second exercises `ThenBy` in both `ClassifyBulkSource` and `BuildFilter`'s switch arms, and — by putting a
`Where` on each side of the `OrderBy`/`ThenBy` pair — proves predicate collection continues correctly past a
`ThenBy` (matching the discrimination style of the earlier interleaved-`Where` test, plus an explicit
`Strategy=SingleCommand` assertion since this shape is otherwise easy to mistake for two-phase).

Command and output:
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDelete_with_bare_Skip|FullyQualifiedName~ExecuteDelete_with_Where_around_OrderBy_ThenBy"

Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 365 ms
```

### Minor 6 — corrected the RED-round probe description

Fixed the Step 5 discrimination-check narrative above: the probe drops the **inner** `Where(o.Status == "open")`
(the walk breaks out of the loop right after stepping past `OrderBy`, before that inner `Where` node is ever
reached), not the trailing `Where(o.Quantity > 15)` as originally written — the outer/trailing `Where` is
processed *first* since the chain is walked outside-in. The observed `Actual: 2` is unchanged and consistent
with the corrected description (both `{open,20}` and `{closed,30}` satisfy `Quantity > 15` once
`Status == "open"` no longer narrows anything).

### Fix-round verification

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   -> Build succeeded. 0 Warning(s) 0 Error(s)
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    -> Build succeeded. 0 Warning(s) 0 Error(s)
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    -> Build succeeded. 0 Warning(s) 0 Error(s)

dotnet test ... -c "Debug EF10" --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
  -> Passed! Failed: 0, Passed: 76, Skipped: 1, Total: 77
     (76 = previous 74 + the 2 new Minor-5 tests; the 1 skip is the pre-existing, unrelated
     ExecuteDelete_two_phase_on_standalone_throws_actionable_error)

dotnet test ... -c "Debug EF9" --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
  -> Passed! Failed: 0, Passed: 64, Skipped: 1, Total: 65   (64 = previous 62 + the 2 new tests)

dotnet test ... -c "Debug EF10" --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
  -> Passed! Failed: 0, Passed: 181, Skipped: 0, Total: 181   (baseline held: 181/181)

dotnet test ... -c "Debug EF9" --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
  -> Passed! Failed: 0, Passed: 169, Skipped: 0, Total: 169   (baseline held: 169/169)
```

Re-ran Northwind on both EF versions as instructed, since Important 1 touched `MongoBulkOperationExecutor.cs`
(Storage), which is shared by every bulk call site, not just the ones this task's own tests exercise. Both
baselines held.

### Files changed (fix round)

- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` — narrowed `NoAutoTransactionError`/`StandaloneTransactionError` message text to paging only.
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` — narrowed `EnsureBulkKeyOrThrow` message text to paging only.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` — `CreateSeededContext`'s parameter narrowed to `LoggerFactory?` (Minor 3); added the two Minor-5 tests.
- `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindBulkUpdatesMongoTest.cs` — corrected the stale "ordering/paging/Distinct... two-phase" class comment.

Commit: `9379382` — "fix: narrow bulk error messages to paging, add ThenBy/bare-Skip coverage".

### Fix-round self-review

- All four review items addressed directly; no scope creep beyond the sweep the review asked for.
- Re-checked the sweep didn't miss anything by re-running the same greps after the edits — clean.
- No outcome assertions changed; the two new tests are additive and the message-text changes are unguarded by any existing assertion (confirmed by grep and by the full green test run).
- `README.md` deliberately left untouched, per the review's explicit instruction.

## Concerns (after fix round)

None. Both Northwind baselines held after the Storage-layer message change; all three configurations build
clean; the sweep for other stale ordering/Distinct-vs-transaction prose came back clean beyond the one test
comment fixed.
