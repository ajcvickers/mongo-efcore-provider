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
