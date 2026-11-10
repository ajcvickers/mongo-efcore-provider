# Task 21 report — async two-phase coverage

## What was converted

All 16 brief-named tests were found in the current tree and converted from `[Fact]` to
`[Theory][InlineData(false)][InlineData(true)]` with a `bool async` parameter, following the mechanical
shape (`async ? await ...Async() : ...()`), exactly as in the worked example.

`ExecuteDeleteTests.cs` (9):
- `ExecuteDelete_with_OrderBy_Take_deletes_lowest_quantities`
- `ExecuteDelete_with_Where_and_Skip_Take_scopes_to_window`
- `ExecuteDelete_two_phase_empty_target_returns_zero`
- `ExecuteDelete_two_phase_rolls_back_inside_user_transaction`
- `ExecuteDelete_two_phase_commits_inside_user_transaction`
- `ExecuteDelete_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing`
- `ExecuteDelete_with_Distinct_deletes_matching`
- `ExecuteDelete_two_phase_with_composite_key_deletes_targeted_rows`
- `ExecuteDelete_with_two_Where_clauses_deletes_matching_both_and_returns_count`

`ExecuteUpdateTests.cs` (7):
- `ExecuteUpdate_with_OrderBy_Take_updates_only_targeted_rows`
- `ExecuteUpdate_two_phase_self_referencing_setter`
- `ExecuteUpdate_two_phase_rolls_back_inside_user_transaction`
- `ExecuteUpdate_two_phase_empty_target_returns_zero`
- `ExecuteUpdate_two_phase_returns_matched_count_even_when_values_already_match`
- `ExecuteUpdate_with_Where_and_Skip_Take_scopes_to_window`
- `ExecuteUpdate_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing`

**None of the 16 brief-named tests were missing** — Tasks 12-20 didn't rename or restructure any of them,
contrary to the brief's caveat that some might have moved. All 16 were still plain `[Fact]`s in the current
tree.

## Unlisted two-phase tests considered but NOT converted

Two pairs of existing `[Fact]`s exercise the two-phase path but were deliberately left alone:

- `ExecuteDelete_with_OrderBy_Take_still_uses_the_two_phase_path` and
  `ExecuteDelete_with_bare_Skip_and_no_Take_still_uses_the_two_phase_path` (`ExecuteDeleteTests.cs`) —
  these assert strategy *classification* (the `Strategy=TwoPhase` log line), which is decided during query
  translation before phase 1 ever executes. That decision path is identical for sync and async, so an async
  case would add no coverage of the thing Task 17 actually changed (cursor draining). Converting them would
  be pure churn.
- `ExecuteUpdate_two_phase_with_an_invalid_setter_fails_before_starting_a_transaction`
  (`ExecuteUpdateTests.cs`) — this pins that setter validation happens before `RunInBulkTransaction`'s
  guard, i.e. it throws before phase 1 (and before any `await`) is ever reached. Same reasoning: nothing
  async-specific to exercise.

No other two-phase-shaped test (i.e. using `OrderBy`+`Take`/`Skip` and reaching the two-phase code path) was
found outside the brief's list and these three. Everything else in both files is either already a
sync/async theory (Task 17's cancellation/multi-batch drain tests, Task 18's batching tests, the two
existing diagnostics theories, the translation-failure and foreign-ambient-transaction theories), is
single-command, or is unrelated to bulk two-phase behavior at all.

## Correction 1 — collision was real, not masked

Verified empirically with a throwaway probe test (`ZZZCollectionCollisionProbeTests`, added, run, then
deleted — not part of the final diff): called `TemporaryDatabaseFixtureBase.CreateCollection<T>(name)` twice
with the same literal name, inserting a document after the first call.

Result: **the second call neither throws nor drops-and-recreates.** It silently returns a handle to the
already-existing collection, with the first call's document still present (`Document count after second
call: 1`). So without the discriminator, converting `CreateSeededContext()`-based tests to theories would
have made the second (`async: true`) case run against the *sync* case's leftover data — silent corruption,
exactly as the brief warned, not a loud failure. **The discriminator is load-bearing, not belt-and-braces.**

Fix: threaded an optional `bool? async = null` parameter through `CreateSeededContext` (in both files) and
`CreateSeededCompositeKeyContext` (in `ExecuteDeleteTests.cs`), placed *before* the `[CallerMemberName]`
parameter so existing non-theory callers (which never pass it) are unaffected. When non-null, it's forwarded
to `CreateCollection<T>(name, async)` — the existing `params object?[] values` overload already used
elsewhere in the suite (e.g. `ExecuteUpdateTests.cs:608`) — which appends `_{async}` to the collection name,
giving the two theory cases distinct collections (`..._False` / `..._True`).

## Correction 3 — transaction decision

For the two rollback tests (`ExecuteDelete_two_phase_rolls_back_inside_user_transaction`,
`ExecuteUpdate_two_phase_rolls_back_inside_user_transaction`) and the one commit test
(`ExecuteDelete_two_phase_commits_inside_user_transaction`), **I made the async case genuinely async**:
`BeginTransactionAsync()` / `RollbackAsync()` / `CommitAsync()` instead of the sync forms, branching on
`async` with an `if/else` (assertions identical in both branches, unchanged from the original body).
Rationale: the sync-transaction-calls option would have left these three tests only proving the delete/update
call itself is awaitable, not that it composes correctly with an async ambient transaction — which is
exactly the kind of interaction a real caller would hit. There is no update twin of the commit test in the
brief's list (and none exists in the file), so only delete got the extra commit coverage; update only got
rollback, matching the brief.

## Correction 4 — sync-throw pinning

For both `AutoTransactionBehavior.Never` tests, the async branch uses
`await Assert.ThrowsAsync<InvalidOperationException>(() => ...ExecuteDeleteAsync())` /
`...ExecuteUpdateAsync())`, with a comment explaining that `RunInBulkTransaction`'s guard runs synchronously
before phase 1 (and before any `await`), so a later refactor that adds an earlier `await` would silently
change this test's failure characteristics if the precise `ThrowsAsync` pinning weren't there.

## Test counts (list-tests, filter `ExecuteDeleteTests|ExecuteUpdateTests`)

| EF version | Before | After | Delta |
|---|---|---|---|
| EF10 | 71 | 87 | +16 |
| EF9  | 72 | 88 | +16 |

(EF9 has one extra test throughout — `ExecuteUpdate_with_no_set_property_throws`, gated `#if EF9` — unrelated
to this change.) The +16 delta exactly matches 16 Facts → 16 Theories × 2 cases each.

## Test run results

- EF10, `ExecuteDeleteTests|ExecuteUpdateTests`: **86 passed, 1 skipped, 0 failed, total 87** (run twice,
  identical both times).
- EF9, same filter: **87 passed, 1 skipped, 0 failed, total 88** (run twice, identical both times).
- The 1 skip in both is the pre-existing `ExecuteDelete_two_phase_on_standalone_throws_actionable_error`
  (`[Fact(Skip = ...)]`, unrelated to this task).
- **No flakiness observed** across the two repeated runs on each EF version, despite the doubled container
  work (each converted theory now creates two collections instead of one).
- No async-only failure occurred anywhere — every converted async case passed on the first try, so there is
  **no evidence of a sync/async production divergence**. No `src/` files were touched.

Northwind regression check (`NorthwindBulkUpdatesMongoTest`):
- EF10: **181/181** passed — matches baseline.
- EF9: **169/169** passed — matches baseline.

Build: all three configurations (`Debug EF8`, `Debug EF9`, `Debug EF10`) build clean, 0 warnings, 0 errors,
both before committing.

## Files changed

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

(The verification probe file, `Query/ZZZCollectionCollisionProbeTests.cs`, was created, run, and deleted —
it never made it into a commit.)

## Self-review

- **Every converted async branch genuinely calls the `*Async` method** — verified by re-reading the final
  diff for all 16 conversions; every `async ? await ...Async(...) : ...(...)` ternary (or `if (async) { ...
  await ... }` block for the three transaction tests) resolves to a real `ExecuteDeleteAsync`/
  `ExecuteUpdateAsync`/`BeginTransactionAsync`/`RollbackAsync`/`CommitAsync` call, not a no-op wrapper.
- **Isolation confirmed, not assumed**: the probe test (Correction 1 section above) proved the underlying
  collision mechanism, and the two full test-suite runs on each EF version passing cleanly (with `async:
  async` threaded into every converted `CreateSeededContext`/`CreateSeededCompositeKeyContext` call)
  confirms the fix holds for the real tests, not just the isolated probe.
- **Assertions preserved exactly** — confirmed by diffing each converted body; only the seeding call, the
  operation call, and (for the three transaction tests) the transaction API calls changed. No assertion
  text, expected value, or count changed.
- **Test output pristine** — no new warnings, no console noise beyond the pre-existing single skip message,
  both builds and both EF-version test runs completed with clean summaries.

## Concerns

None. All corrections were verified rather than assumed, all conversions preserved assertions, no
production code was touched, and repeated runs showed no flakiness.
