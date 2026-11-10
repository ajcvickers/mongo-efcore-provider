# Task 4 Report: Spec suite — flip the six tests, rebaseline, sweep all EF versions

## Commit

`5f4ac271` — `EF-322: native multi-hop reference-navigation filters in NorthwindNavigationsQueryMongoTest; rebaseline MQL`

Files changed:
- `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindNavigationsQueryMongoTest.cs` (6 baselines)
- `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindMiscellaneousQueryMongoTest.cs` (1 baseline, discovered in Step 4 sweep)

Both files kept their original BOM status (`NorthwindNavigationsQueryMongoTest.cs` — no BOM; `NorthwindMiscellaneousQueryMongoTest.cs` — has BOM, preserved).

## Step 1: NativeOnly, target class, EF10

```
MONGODB_EF_NATIVE_ONLY=1 dotnet test .../SpecificationTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindNavigationsQueryMongoTest"
```
Result: `Failed: 12, Passed: 135, Total: 147`. All 12 failures were the six target tests × async(True/False), all `AssertMql` string mismatches — no `NativeTranslationNotSupportedException` anywhere (confirmed via grep). `Select_Where_Navigation_Deep` did **not** throw; it went native cleanly. Proceeded to Step 3.

## Step 2: Default mode, target class, EF10

Same command without the env var: identical `Failed: 12, Passed: 135` — same six tests, same AssertMql mismatches, no data/behavior failures.

## Step 3: Rebaseline

Ran `EF_TEST_REWRITE_BASELINES=1` per method for all six, reviewed `git diff` after each, then rebuilt and reran Steps 1–2 → both green (`147/147` in both modes).

Baseline excerpts (new vs. old pipeline stage order):

- **`Select_Where_Navigation_Deep`** — now: two `$lookup`/`$unwind` pairs (Orders, then Customers), then `$match` on `_lookup_Customer.City`, then `$sort`/`$limit`. Previously an extra `$set` (`$ifNull`) sat between the second `$unwind` and the `$match`; that `$set` is gone and `$match` directly follows both joins, with `$sort`/`$limit` after the `$match` as expected.
- **`Select_Where_Navigation_Null_Deep`** — now: `$lookup`/`$unwind` (Manager), `$lookup`/`$unwind` (Manager_1 — second hop), then `$match : { "_lookup_Manager_1" : null }`. **Confirmed**: it tests `_lookup_Manager_1` (the second-level alias), not `_lookup_Manager`. The intermediate `$set { "$ifNull" }` stages present in the old baseline are gone.
- **`Navigation_inside_contains_nested`** and **`Navigation_from_join_clause_inside_contains`** — same shape: two `$lookup`/`$unwind` pairs (Orders, Customers) then a single `$match` (dropped the intervening `$set`).
- **`Include_with_multiple_optional_navigations`** — two `$lookup`/`$unwind` pairs then `$match`, `$set` removed.
- **`Multiple_include_with_multiple_optional_navigations`** — now all four `$lookup`/`$unwind` stages (Customer join-scope, then Product) run before the single `$match`, instead of `$match` sitting between the two Include joins. Correct: the filter now waits for both join scopes to close before matching.

## Step 4: Whole spec suite sweep, EF10

- `dotnet build -c "Debug EF10"` — succeeded.
- Default mode, full suite: initially `Failed: 2, Passed: 4766, Skipped: 14` — one new AssertMql-only regression: `NorthwindMiscellaneousQueryMongoTest.Where_join_orderby_join_select` (async True/False). Rebaselined it (tight filter, reviewed diff): old MQL sorted *after* the first `$lookup`/`$unwind`; new MQL sorts by `_id` via `$set __sort0`/`$sort`/`$unset` **before** any join, then does the joins. This is the expected effect of joins now being recorded via the same join-scope machinery — sort hoisted earlier since it applies to the still-unjoined root. Rebuilt; full suite now **`Failed: 0, Passed: 4768, Skipped: 14, Total: 4782`**.
- NativeOnly, full suite: `Failed: 178, Passed: 4590, Skipped: 14, Total: 4782`.
  - Compared against a `git worktree` build of pre-Task-1 commit `fdd36a39` (same command): `Failed: 192, Passed: 4576, Skipped: 14, Total: 4782`.
  - Diffed the two full lists of failing test names (name+async, order/timestamp-normalized): **14 tests fixed, 0 regressions** (`comm -23`/`comm -13` on sorted, deduplicated name lists). The 14 fixed = the six target tests × 2 (async True/False) + `NorthwindMiscellaneousQueryMongoTest.Where_join_orderby_join_select` × 2 (a bonus fix from the same join-scope-confirmation change). No test that passed before now fails.
  - Worktree cleaned up with `git worktree remove`.

## Step 5: Three-EF-version sweep

Ran build + full solution tests per version (UnitTests, FunctionalTests, SpecificationTests), each `-c "Debug <v>"`, in the foreground (splitting FunctionalTests to background for EF8/EF9 since each run took several minutes).

| EF version | UnitTests | FunctionalTests | SpecificationTests |
|---|---|---|---|
| EF8  | 1424 passed, 0 failed | 3413 passed, 51 skipped, 0 failed | 4714 passed, 11 skipped, 0 failed |
| EF9  | 1424 passed, 0 failed | 3461 passed, 52 skipped, 0 failed | 5027 passed, 11 skipped, 0 failed |
| EF10 | 1417 passed, 0 failed | 3517 passed, 52 skipped, 0 failed | 4768 passed, 14 skipped, 0 failed |

All green across all three versions and all three projects. (EF8/EF9/EF10 have different spec totals — expected; upstream test-base counts and skip lists vary by EF version, unrelated to this change.)

### NativeOnly target-class run, EF8 and EF9

```
MONGODB_EF_NATIVE_ONLY=1 dotnet test .../SpecificationTests.csproj -c "Debug <v>" --no-build --filter "FullyQualifiedName~NorthwindNavigationsQueryMongoTest"
```

- **EF9**: `Failed: 0, Passed: 147, Total: 147`. All six target tests go native on EF9 (the `LeftJoin`-shim join-recording path handles them fine).
- **EF8**: `Failed: 2, Passed: 145, Total: 147`. The two failures are `Collection_select_nav_prop_predicate(async: True/False)` — **not** one of the six target tests. Error: `NativeTranslationNotSupportedException: Query projects a non-entity result and MongoQueryMode.NativeOnly forbids the driver-LINQ fallback.` Verified via the same `fdd36a39` worktree that this exact test already failed identically on EF8 NativeOnly *before* Task 1–3 — it is a pre-existing, unrelated gap (non-entity scalar projection guard), not a regression and not one of the six in scope. All six target tests pass cleanly (go native) on EF8 too.

No `#if`s were added; no production code was touched in this task.

## Concerns

- None from this task's scope. The `Where_join_orderby_join_select` baseline shift (sort hoisted before the join) is a pure MQL-shape change with unchanged semantics — expected side effect of the join-scope confirmation logic from Tasks 1–3, not something introduced here.
- `Collection_select_nav_prop_predicate` EF8-NativeOnly failure is pre-existing and out of scope; flagging per the brief's instruction rather than touching it.
- All builds/tests were run against fresh TestContainers-backed MongoDB (both `MONGODB_URI`/`ATLAS_URI` left unset), per the binding constraints.
