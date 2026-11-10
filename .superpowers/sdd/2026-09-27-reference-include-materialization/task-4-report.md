# Task 4 report — Whole-branch regression sweep and cleanup

## Step 1: Full EF10 suite

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   -> Build succeeded, 0 errors
dotnet test  MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build
```

| Assembly | Passed | Failed | Skipped | Total |
|---|---|---|---|---|
| UnitTests | 1285 | 0 | 0 | 1285 |
| SpecificationTests | 4599 | 0 | 14 | 4613 |
| FunctionalTests | 3284 | 0 | 52 | 3336 |

**All green. 0 failures across 9,234 tests.**

## Step 2: Version-conditional guard check → ran all three EF versions

Per the brief's check (`git diff 0b0a7a6d..HEAD --name-only | xargs grep -l "#if EF8\|#if EF9\|#if !EF8"`),
several files touched by this plan's commits (0b0a7a6d..HEAD) contain those guards elsewhere in the file:
`NativeJoinScopeProjectionBinder.cs`, `MongoProjectionBindingExpressionVisitor.cs`,
`MongoQueryableMethodTranslatingExpressionVisitor.cs`, `NativeJoinTests.cs`,
`NorthwindEFPropertyIncludeQueryMongoTest.cs`, `NorthwindIncludeNoTrackingQueryMongoTest.cs`,
`NorthwindIncludeQueryMongoTest.cs`. This triggers the check, so per the deviation instructions in my task
brief I ran the three EF version build+test commands directly (rather than invoking `/test-all`).

### EF8

Build succeeded, 0 errors.

| Assembly | Passed | Failed | Skipped | Total |
|---|---|---|---|---|
| UnitTests | 1292 | 0 | 0 | 1292 |
| SpecificationTests | 4704 | **10** | 11 | 4725 |
| FunctionalTests | 3180 | 0 | 51 | 3231 |

10 failures, all `NorthwindMiscellaneousQueryMongoTest` (5 test methods × async:True/False):
- `OrderBy_object_type_server_evals`
- `Projection_skip_take_projection`
- `Projection_take_projection`
- `Projection_skip_projection`
- `Contains_over_concatenated_columns_both_fixed_length`

This is exactly the known, pre-existing, unrelated `NorthwindMiscellaneousQueryMongoTest` failure set called
out in the task brief (confirmed present by Task 3's implementer even with this branch's changes stashed).
No new failures.

### EF9

Build succeeded, 0 errors.

| Assembly | Passed | Failed | Skipped | Total |
|---|---|---|---|---|
| UnitTests | 1292 | 0 | 0 | 1292 |
| SpecificationTests | 5017 | **10** | 11 | 5038 |
| FunctionalTests | 3228 | 0 | 52 | 3280 |

Same 10 `NorthwindMiscellaneousQueryMongoTest` failures, identical test names to EF8. No new failures.

### EF10

Already run and green in Step 1 (re-run not repeated a second time since Step 1 already gave a clean,
just-built EF10 result immediately before this check).

**Conclusion: no new failures introduced by this branch on any of EF8/EF9/EF10. The only failures anywhere are
the pre-existing, known, unrelated `NorthwindMiscellaneousQueryMongoTest` set on EF8/EF9 (absent on EF10).**

## Step 3: Bare reference-Include tests

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Optional_reference_Include_goes_native_with_a_left_outer_unwind|FullyQualifiedName~Required_reference_Include_goes_native_with_an_inner_unwind"
```

Result: `Passed! - Failed: 0, Passed: 2, Skipped: 0, Total: 2` — both tests (in
`NativeReferenceIncludeTests.cs`) still pass.

## Step 4: Doc cross-reference

Added a "Status: IMPLEMENTED" note near the top of
`docs/superpowers/specs/2026-09-25-reference-include-materialization-design.md`, pointing to:
- `docs/superpowers/plans/2026-09-27-reference-include-materialization.md` (the executed plan)
- `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md` (Task 1's investigation findings —
  Hypothesis A confirmed)

Both paths verified to exist before committing.

## Step 5: Commit

`69ec26b0` — "EF-322: mark reference-Include materialization design doc as implemented"

(Shows as 227 insertions / new file because the spec doc itself was untracked in git status before this
commit — it was created earlier in this session's work but never previously committed. This is its first
commit into the branch.)

## Files changed by this task

- `docs/superpowers/specs/2026-09-25-reference-include-materialization-design.md` (doc edit + first commit)

## Issues / concerns

None. All EF10 tests pass cleanly; EF8/EF9 show only the pre-existing, already-documented
`NorthwindMiscellaneousQueryMongoTest` failures, unrelated to this plan's changes. No functional code was
touched in this task, as expected.
