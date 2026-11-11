# HANDOFF — EF-361b: revive the bulk ExecuteUpdate/ExecuteDelete remediation on `release`

Written 2026-11-10 (provider2 clone). Nothing is ported yet; this branch is `origin/release` (`c8fdd423`) plus these docs.

## What this is
- Jira **EF-361** "Improve quality of bulk updates code" (Backlog): ~50 review findings against EF-107's bulk
  `ExecuteUpdate`/`ExecuteDelete` (#311). Task-by-task plan: `docs/superpowers/plans/2026-07-30-ef-107-bulk-remediation.md`
  (26 tasks, 7 phases; copied here from the old branch at `207d47ce`; written against base `58e05a0e`, pre-refactor shapes).
- The original work was done 2026-07-30..08-01 on local branch `EF-361` in provider2 (54 commits, tip of real work
  `207d47ce`; the branch tip `de136dc6` is an unrelated stray commit, ignore it). It was never pushed, squashed or PR'd.
  Full-suite green on EF8/9/10 at the time, final review "ready to merge: with fixes", fixes applied.
- `origin/release` (fork `ajcvickers/mongo-efcore-provider`) is 143 commits ahead of the old base, with the native query
  pipeline, EF-479 streaming, EF-168 complex types, EF-478 `$literal` wrapping.

## Decision: re-port, do NOT rebase
A trial merge conflicts in 12 files, and more importantly the code moved: bulk plan building left the shaped-query
compiler for `Query/Visitors/MongoBulkPlanBuilder.cs`; the translator's bulk half is
`MongoQueryableMethodTranslatingExpressionVisitor.NonQuery.cs`; executor is `Storage/MongoBulkOperationExecutor.cs`.
Port by phase using the old task briefs/reports as the spec. None of the 20 old test files compile as-is
(they use dead APIs); see the report.

## Applicability result (full table: `findings/2026-11-ef361-revival/applicability-report.md`)
17 tasks still open unchanged (A), 9 exist in changed form (C); no task fully fixed. Sub-items already fixed on release:
Task 1 EF8 ExecuteUpdate case, Task 20 compliance suites, Task 25 EF-X016 dup id, Task 26 dropped `nonQuery` param,
parts of Task 7 (complex `$set` paths) and Task 22 (`ExecuteUpdateLiteralSetterTests`).
None of the EF-361 fixes (guards, narrow catches, gated command logging, `_id` projection, streaming, `$in` batching,
caching) are on release.

## Order of work
1. **Phase 1 first: Tasks 4, 5, 6** (single EventDefinition per bulk EventId + MatchedCount; redaction-gated command
   logging, elapsed spanning both phases; logger via `MongoQueryContext`). Hard gate: ~half the ported tests assert
   `Strategy=`/`Command=` or payload fields that don't exist yet. Then Tasks 1-3 (test groundwork / narrowing).
2. Phase 2-3 guards + fail-closed (Tasks 7-12).
3. Phase 4 classification (13-15): widen `MongoBulkPlanBuilder.BuildFilter`'s Where-only walk IN STEP with
   `ClassifyBulkSource`, or a classification miss deletes wrong documents. Never put OfType/Cast in the neutral set; carry
   the Task-15 `OfType<TDerived>()` + self-referencing-setter guard.
4. Phase 5 performance (16-19). Task 19: port the FIXED cache design from the ledger, not the plan's original.
5. Phase 6 coverage (20-24). Task 24 must be the rewritten clean-rejection suite: Queryable Encryption forbids
   multi-document writes, so the plan's success-path tests can never pass; also needs `AutoInclude(false)` (EF9 auto-include
   defect) and typed `MongoException` catches. Task 22's "owned target throws" test is obsolete (release supports them).
6. Phase 7 docs (25-26): README.md:99 still says "single atomic deleteMany/updateMany" (per-document atomicity only).

## DECIDED (2026-11-10, owner): duplicate setters stay last-wins
Task 9 wanted duplicate setters rejected, but `release` documents last-wins on purpose (`NonQuery.cs` `ThrowIfSetterPathsOverlap`,
"the same path set twice is not an overlap"). Owner ruling: **keep last-wins** and drop the duplicate-setter rejection from Task 9. The rest of
Task 9 (`FindProperty(MemberInfo)`, declaring-typed `SetProperty` match) is still to port. Phase 2 is unblocked.

## Progress log
- **Phase 1** (2026-11-10): Task 4 (`34ad08fa`: one `EventDefinition` per bulk `EventId`, `MatchedCount`, `Strategy` on the event payloads),
  Task 5 (redaction-gated `Command`, lazy `Func<string>` renderer, stopwatch spanning both phases) and Task 6 (`UpdateLogger` on `MongoQueryContext`,
  with a `BREAKING-CHANGES.md` entry for the two public constructor changes; latest releases are v10.0.5 / v9.1.5 / v8.4.5) are ported.
  Not yet ported from Task 5/23: payload-level assertions (`Command`, `LogSensitiveData`, `Elapsed > 0` for two-phase) need `DiagnosticSourceSpy`,
  which does not exist on `release`; Task 5's tests assert the rendered log message instead. Add the payload assertions with Task 23.
- **Tasks 1-3** (test groundwork and narrowing) and **Phase 2, Tasks 7-12** are ported (commits `d9c5de04`..`ed4bfdc6`, plus `be0d8d0d`).
  Decisions and findings worth knowing:
  - Task 7 ships two guards (primary key, discriminator); concurrency-token setters stay permitted (positive test). Task 7's `GetSetElementKey` was
    not ported: the primary-key guard makes its composite-key branch unreachable. Task 9 ships only the EF9 `SetProperty` declaring-type check
    (duplicate setters stay last-wins; `FindProperty(MemberInfo)` not adopted, believed to be a thin wrapper over the name lookup, unverified).
  - Task 8's motivating shape (a self-referencing navigation in a setter value) is already rejected by the translator on `release`
    ("could not be translated"), so its guard is defence in depth; the functional test pins that the shape fails loudly and writes nothing.
  - **Trap found in Task 11:** the phase-1 query binds the transaction's session when it is BUILT (`TranslateQuery` reads `CurrentTransaction`),
    so building it before `BeginTransaction` makes phase 1 read outside the transaction. Counts and results cannot show it. Only the update
    document is built before the transaction; `ExecuteDelete_two_phase_reads_its_target_ids_inside_the_transaction` checks the command's `txnNumber`.
  - Task 12's test cancels from a `CommandSucceededEvent` hook AFTER phase 1 (a pre-cancelled token starts no transaction), using the extended
    `CommandCapture`. Verified red with the old rollback token.
  - EF11 drops the "with details" translation-failure message; the reason is in the inner exception, so tests read the whole chain.
  - Full `/test-all --native` on `ed4bfdc6`: EF8/EF9/EF10 clean in both modes; EF11 only the two #63 spec tests, plus three ExecuteUpdate message
    assertions fixed in `be0d8d0d`.
  - Not ported: Task 11's try-split of `BeginTransaction` (pointless after Task 3's narrowing) and its `BuildFilter` loud-failure change (already on `release`).
  - Next: Phase 4 classification (Tasks 13-15), widening `ClassifyBulkSource` in step with `BuildFilter`'s Where-only walk.
- **Phase 4, Tasks 13-15** (classification) are ported (`858322be`, `8fc88544`, `c4a5fc5a`). Decisions and findings:
  - One predicate set, `MongoNonQueryExpression.IsWhere/IsAffectedSetNeutral/IsPaging/IsOfType`, is consulted by BOTH `ClassifyBulkSource` and
    `MongoBulkPlanBuilder.BuildFilter` and matches canonical `QueryableMethods` definitions (Task 14). `BuildFilter` steps over the neutral
    operators and still THROWS on anything else, so a classification miss cannot silently widen a delete. OfType/Cast/Skip/Take must never be
    added to the neutral set (a unit test asserts it).
  - Ordering and `Distinct` alone are single-command (Task 13). Only `Skip`/`Take` and `OfType<TDerived>` are two-phase.
  - `OfType<TDerived>` is two-phase so phase 1 reuses the read path's discriminator filter; a test pins from the command stream that phase 1
    carries `_t` and phase 2 acts only on those ids. `OfType<Root>` stays single-command. A derived `DbSet` works (EF narrows it to OfType).
  - **`Cast` stays rejected** (as on the old branch): it only reshapes the shaper and the driver cannot translate it. Reversible, nothing depends on it.
  - A self-referencing setter over an `OfType` source is refused with a clear message (the lambda parameter is typed as the derived type but the
    value is rendered against the root).
  - Task 14 changes no reachable behaviour (EF rejects the other overloads, and rewrites `Order()` to `OrderBy(x => x)`, before provider code).
  - The two-phase error messages no longer enumerate their triggers (they had gone stale three times).
  - Six `Inheritance*BulkUpdatesMongoTest` wrappers (EF-X016) now pass and call `base`. **For Task 25:** `docs/failing-spec-tests.md` still counts
    them and says `OfType` is not translated for bulk; also re-check `MongoComplianceTest.IgnoredTestBases` (Task 20) and the README atomicity wording.
- **Phase 5, performance (Tasks 16-19)** is ported. Findings worth knowing:
  - Task 16: phase 1 appends `{ $project: { _id: 1 } }` last (via `MongoQueryable.AppendStage` in `MongoBulkPlanBuilder.BuildIdDocumentQuery`). The two-phase
    `Command` is now `{ phase1Pipeline, filter, update }`, rendered from the driver's `LoggedStages` inside `RenderBulkCommand`, behind the one redaction gate
    (so the closed-gate assertions cover it). `targetIdQuery` is a required argument (single-command sites pass `null`) so a new two-phase site cannot forget it.
    The projection saves wire bytes and client parsing; it is a covered index scan only if `_id` is in the index key or the collection is clustered on `_id`.
  - Task 17: `MaterializeIdsAsync` drains the cursor instead of `ToListAsync`; the narrow `ExpressionNotSupportedException` catch wraps only `ToCursorAsync`.
    A 250-document test asserts every `getMore` batch is drained. No test covers a failure DURING cursor iteration (Task 23).
  - Task 18: phase 2 is batched at 50,000 ids on the one transaction (`BatchIds`, internal `OverrideMaxIdsPerBulkCommand` test seam). `Executing` now fires once
    per batch, `Executed` once. Ids are still buffered in one list (not bounded).
  - Task 19: `Setter` is now a sealed class (a record field would join equality) with a `volatile` cached compiled delegate for the fallback, plus a cache of the
    generic aggregate renderer. What is cached is the delegate, never a value. A value lambda that ignores its parameter is evaluated once per execution, not
    per document (pre-existing; a follow-up ticket candidate).
  - Next: Phase 6 coverage (Tasks 20-24), then Phase 7 docs.
- Known unrelated failure on one checkout: two EF11 `ConferencePlannerMongoTest` SpecFails wrappers, tracked in issue #63.

## Recorded decisions from the old work (do not re-litigate)
- Concurrency-token setters are PERMITTED (2026-07-30, `832ad8bd`).
- TPH: guard `entityType.BaseType != null` in VisitNonQuery for a derived-DbSet bulk (verify with a test first; ticket the
  read-path gap). Old fix `2d7fb8ba`.
- Cast stays rejected; OfType supported via two-phase.
- Follow-up tickets to FILE (never recorded): "no error event on failure" (review-map row 20), coverage for the deleted
  skipped test `ExecuteDelete_two_phase_on_standalone_throws_actionable_error` (paired with `MapBulkTransactionFailure` extraction),
  TPH read-path discriminator gap.

## Where the old material is (NOT in this branch unless noted)
- Old branch `EF-361` and its SDD ledger/briefs/reports/review diffs exist only on the original machine, in provider2:
  `.superpowers/sdd/2026-07-30-ef-107-bulk-remediation/` (untracked, gitignored: `progress.md`, `task-N-brief|report.md`,
  `final-fix-wave-report.md`). They are the best spec for each port. Copy them across or push `EF-361` (e.g. as
  `EF-361-old`) if needed on another machine.
- Plan and applicability report: committed here.

## Conventions
Commit subjects start `EF-361: `. No worktrees. Tests with `MONGODB_URI`/`ATLAS_URI` unset; run `/test-all` (EF8/9/10) and a
full suite before calling any dispatch/allow-list change done. Don't force-push shared branches.
