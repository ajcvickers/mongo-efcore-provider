# Task 1 report: Investigate Shape 3's actual code path

## Steps performed

1. **Environment check.** `docker info` succeeded immediately (Docker already running); no wait needed.
2. **Build EF10 debug.** `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"` — 0 errors, 12 warnings
   (all pre-existing NU1903/CS1574 noise, unrelated to this work).
3. **Baseline reproduction (unmodified tree).**
   - `Include_reference_when_entity_in_projection`: fails with `System.FormatException : Element '_id'
     does not match any field or property of class ...Order` (a LOUD failure, not silent wrong data —
     see correction below).
   - `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join`: fails with
     `Assert.Equal() Failure: Expected: 2, Actual: 0` (silent wrong data — empty collection).
   - **Correction to the plan's premise:** the first test does NOT fail with silent null/wrong data in
     this tree. Commit `0b0a7a6d` (already on this branch) deliberately restored this exact
     `FormatException` as the intended behavior for this unsupported shape — its own commit message
     confirms "`Include_reference_when_entity_in_projection` now fails with `FormatException` again".
     This doesn't block the investigation; it just means Task 1's job for this test is confirming which
     arm produces that (expected) exception, not explaining a mystery silent-data bug.
4. **Added five temporary `Console.WriteLine` probes** at: `TranslateSelect`'s two recognizer/confirm
   arms (pure-reference and mixed), `TryGetReferenceIncludeChain`, `TryConfirmReferenceIncludeChain`,
   `IsSingleEligibleNativeJoinScope`, `BindResultMember` (right after the unwrap loop), and
   `NativeJoinScopeProjectionBinder.TryBindProjection` (entry).
5. **Rebuilt and re-ran the two target tests with probes active** (`--logger "console;verbosity=detailed"`
   was required — plain `dotnet test` output does not surface `Console.WriteLine` from the SUT; without
   that flag zero probe lines showed up in step 5's first attempt). Full probe output captured below.
6. **Ran the two already-passing bare reference-Include tests** with the same probes — both PASSED,
   and their probe output shows a completely different, non-overlapping code path.
7. **Ran the Shape 4 control repro.** Rather than adding a new throwaway `[Fact]`, found that
   `NativeJoinTests.Whole_entity_leaf_that_is_also_reference_included_declines_rather_than_silently_returning_null`
   (added by commit `0b0a7a6d`) already IS the exact repro
   (`db.Orders.Include(o => o.Owner).Select(o => new { o, o.OwnerId })` under `MongoQueryMode.NativeOnly`,
   `JoinTestDbContext`/`SeedOwnersAndOrders()`), so ran that existing test with probes instead — avoids
   any new untracked scratch file to track/delete.
8. **Reverted all probes.** `git diff --stat` before revert showed exactly the two visitor files
   changed (no scratch fact file existed, since step 7 reused an existing test). `git checkout --` both
   files, rebuilt (0 errors), and confirmed `git diff` against both files is byte-empty
   (`git diff -- <file1> <file2>` produced zero output, exit code 0).
9. **Wrote the findings doc**: `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md`.
10. **Committed the findings doc only.** `git status --short` after commit shows only the pre-existing
    untracked plan/spec/handoff docs (unrelated to this task, present since before this session started)
    — no tracked file besides the findings doc was touched.

## Literal probe output captured

### `Include_reference_when_entity_in_projection` (both async:True/False identical)
```
[SHAPE3-PROBE] TryGetReferenceIncludeChain: recognized=False
[SHAPE3-PROBE] IsSingleEligibleNativeJoinScope: JoinScope.Levels.Count=1, Joins.Count=1
[SHAPE3-PROBE] NativeJoinScopeProjectionBinder.TryBindProjection: CALLED
[SHAPE3-PROBE] BindResultMember: alias=o, includeWrappers.Count=1, allCollectionIncludes=False
[SHAPE3-PROBE] BindResultMember: alias=CustomerID, includeWrappers.Count=0, allCollectionIncludes=True
```

### `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join` (both async variants identical)
```
[SHAPE3-PROBE] TryGetReferenceIncludeChain: recognized=False
[SHAPE3-PROBE] IsSingleEligibleNativeJoinScope: JoinScope.Levels.Count=1, Joins.Count=1
[SHAPE3-PROBE] NativeJoinScopeProjectionBinder.TryBindProjection: CALLED
[SHAPE3-PROBE] BindResultMember: alias=cust, includeWrappers.Count=0, allCollectionIncludes=True
[SHAPE3-PROBE] BindResultMember: alias=order, includeWrappers.Count=1, allCollectionIncludes=True
```

### Two already-passing bare reference-Include tests (both PASSED)
```
[SHAPE3-PROBE] TryGetReferenceIncludeChain: recognized=True, referenceLevels.Count=1
[SHAPE3-PROBE] TranslateSelect: TryGetReferenceIncludeChain recognized, chain.Count=1
[SHAPE3-PROBE] TryConfirmReferenceIncludeChain: chain.Count=1, transitiveLevels.Count=0
[SHAPE3-PROBE] TranslateSelect: TryConfirmReferenceIncludeChain CONFIRMED (pure reference chain arm)
(repeated identically for the second test)
```
No `IsSingleEligibleNativeJoinScope`/`TryBindProjection`/`BindResultMember` lines appeared at all for
either test.

### Shape 4 control repro (`Whole_entity_leaf_that_is_also_reference_included_declines_rather_than_silently_returning_null`, PASSED)
```
[SHAPE3-PROBE] TryGetReferenceIncludeChain: recognized=False
[SHAPE3-PROBE] IsSingleEligibleNativeJoinScope: JoinScope.Levels.Count=1, Joins.Count=1
[SHAPE3-PROBE] NativeJoinScopeProjectionBinder.TryBindProjection: CALLED
[SHAPE3-PROBE] BindResultMember: alias=o, includeWrappers.Count=1, allCollectionIncludes=False
[SHAPE3-PROBE] BindResultMember: alias=OwnerId, includeWrappers.Count=0, allCollectionIncludes=True
```
Identical shape to `Include_reference_when_entity_in_projection`'s probe output — same arms, same
`allCollectionIncludes=False` decline for the reference-Include leaf.

## Analysis / conclusions (full detail in the findings doc)

- Both target tests reach `IsSingleEligibleNativeJoinScope` →
  `NativeJoinScopeProjectionBinder.TryBindProjection` → `BindResultMember` — **Hypothesis A** for both.
  Neither ever reaches `TryGetReferenceIncludeChain`/`TryConfirmReferenceIncludeChain`'s separate arm
  (that recognizer returns `false` immediately in both cases, because EF Core's nav-expansion already
  turned the Include-in-projection into an internal join-scope construct before `TranslateSelect` sees
  it).
- The two target tests DO differ downstream, as the brief anticipated: test 1's reference-Include leaf
  (`o`, `allCollectionIncludes=False`) declines `BindResultMember`'s fast path and falls through to
  `BindSelectManyMember`, producing the `FormatException`. Test 2's collection-Include leaf (`order`,
  `allCollectionIncludes=True`) takes the fast path and returns cleanly (no exception) — its silent
  empty-collection bug lives outside the arms this task instrumented (likely `$lookup`
  localField/foreignField resolution on the right side of a LeftJoin-shaped scope).
- The two already-passing bare reference-Include tests share **zero** arms with the failing tests —
  they go exclusively through the pure `TryGetReferenceIncludeChain`/`TryConfirmReferenceIncludeChain`
  arm and never touch the join-scope machinery. A Task 3 fix to the join-scope arms is structurally
  isolated from these two tests (though Task 3 should still re-run them as a regression check per the
  brief, since `TryWalkIncludeChain` is a shared helper).
- Shape 4's control repro hits the SAME arms as target test 1 (not merely an analogous path) —
  confirms EF's nav-expansion inserts an internal join-scope construct even for this simpler,
  no-explicit-`.Join()` shape.
- **Recommendation:** Task 3 should execute **Branch 3A** (fix the join-scope side —
  `BindResultMember`/`NativeJoinScopeProjectionBinder`, teaching it to materialize a reference-Include
  leaf instead of declining, mirroring the existing collection-Include rebind path with a reference
  lookup instead), not Branch 3B (the separate `TryGetReferenceIncludeChain` arm, proven unreachable for
  both target tests). Caveat: the two target tests fail via two different downstream mechanisms sharing
  one recognizer path — a Branch-3A fix aimed at the reference-Include decline may fix test 1 without
  touching test 2's `$lookup`-mechanics root cause; treat as two bugs, not one.

## Files changed

Exactly one: `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md` (new, committed as
`285a9011`). `git status --short` after the commit shows no other tracked-file changes; `git diff`
against both visitor files is empty.

## Self-review findings

- Confirmed via `git diff` (empty output, exit 0) that both visitor files show zero diff after revert —
  not just "I remember reverting them."
- Findings doc quotes literal captured probe output (copy-pasted from the actual `dotnet test` runs, not
  paraphrased).
- Explicitly distinguished the two target tests' downstream mechanisms (exception vs. silent-empty),
  not just their shared recognizer arm.
- Explicitly checked and reported: the two passing tests share NO arms with the failing ones.
- One-paragraph recommendation (Branch 3A, with the two-separate-bugs caveat) is stated plainly in the
  findings doc's final section for Task 3 to act on directly.
- No issues found requiring further fixes.

## Concerns

- None blocking. One thing to flag for Task 3: the plan's original premise (silent null/wrong data for
  `Include_reference_when_entity_in_projection`) doesn't match this tree's current behavior — it's a
  loud, intentional `FormatException` per commit `0b0a7a6d`. This doesn't change today's Hypothesis A/B
  conclusion, but Task 3 should not expect to be "fixing a silent null bug" for that test — it's fixing a
  hard decline into a correct materialization.

## Fix report (review follow-up)

**Finding addressed:** task review flagged that the findings doc asserted
`TryGetMixedReferenceAndCollectionIncludeChain` was "never entered"/"bypassed" for both target tests,
but this was inferred from the absence of other probe lines rather than shown via a quoted probe line
for that recognizer's own resolved outcome — exactly the "assumed away" failure mode the original brief
warned against, since `TranslateSelect` is an `if (TryGetReferenceIncludeChain...) ... else if
(TryGetMixedReferenceAndCollectionIncludeChain...) ...` chain (lines 417/427), so the mixed recognizer
IS evaluated whenever the pure one returns `false`.

**What was changed:**
1. Re-added one temporary probe at the top of `TryGetMixedReferenceAndCollectionIncludeChain`
   (`src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`,
   ~line 1575), printing `CALLED` on entry and `recognized=True`/`False` (with `referenceLevels.Count`
   when `True`) on exit.
2. Rebuilt (`Debug EF10`, 0 errors) and re-ran both target spec tests
   (`Include_reference_when_entity_in_projection`,
   `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join`) with
   `--logger "console;verbosity=detailed"`, isolated to just this one probe (the five probes from the
   original pass were not re-added — they'd already produced their quoted evidence and this follow-up
   only needed the missing data point).
3. Captured probe output — **identical for both target tests, all four async variants**:
   ```
   [SHAPE3-PROBE] TryGetMixedReferenceAndCollectionIncludeChain: CALLED
   [SHAPE3-PROBE] TryGetMixedReferenceAndCollectionIncludeChain: recognized=False
   ```
   This directly confirms the recognizer IS called (per the `else if` control flow) and returns `false`
   for both tests — evaluated-and-declined, not skipped. For `Include_reference_when_entity_in_projection`
   this is structural: the selector has only one reference-Include (`o.Customer`), no collection-Include
   sibling, so `TryWalkIncludeChain`'s `collectionLevel == null` gate declines it. For
   `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join` — despite its
   name suggesting a genuine mixed reference+collection shape — the recognizer still declines because it
   operates over `TranslateSelect`'s own `selector` parameter, and by the time `TranslateSelect` runs for
   this query, EF's nav-expansion has already rewritten the Include information into the join-scope's own
   `IncludeExpression`-wrapped leaves (visible in `BindResultMember`'s `alias=order,
   includeWrappers.Count=1` line from the original pass) rather than into an `IncludeExpression` chain
   hanging directly off this selector body for `TryWalkIncludeChain` to walk.
4. Reverted the temporary probe: `git checkout --
   src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`,
   rebuilt again (0 errors), and confirmed `git diff` against both visitor files
   (`MongoQueryableMethodTranslatingExpressionVisitor.cs` and `NativeJoinScopeProjectionBinder.cs`) is
   byte-empty (exit code 0, no output) before amending the findings doc.
5. Amended `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md`: replaced the
   "never entered"/"bypassed" language under both target tests' sections with the quoted probe output
   above plus the structural explanation of why the recognizer declines in each case, and added a note
   to the doc's intro paragraph flagging this second, follow-up probe pass. Grepped the amended doc for
   "never entered"/"bypassed" — zero remaining matches.
6. Committed as a **new commit** (`84d90a54`, "EF-322: back mixed-chain-arm claim with quoted probe
   evidence (review fix)") rather than amending `285a9011`, per this repo's git-safety convention of
   preferring new commits over amends. `git status --short` after the commit shows only the same
   pre-existing untracked plan/spec/handoff docs from before this task began — no other tracked file
   touched. Working tree ends with exactly the findings doc changed across the two commits combined.

**Self-review re-run:** the specific sub-claim ("does `TryGetMixedReferenceAndCollectionIncludeChain`
get evaluated, and with what outcome, for each target test") is now backed by a directly quoted probe
line for both tests, not inferred from the absence of other output. The Hypothesis A conclusion and the
Branch 3A recommendation are unchanged — this fix only strengthens the evidentiary basis for one
sub-claim within that conclusion, exactly as scoped by the review finding.

**Tree state confirmed clean:** `git diff` on both `.cs` files empty; `git status --short` shows only
the findings doc as a change (already committed) plus the same pre-existing untracked docs present since
session start.
