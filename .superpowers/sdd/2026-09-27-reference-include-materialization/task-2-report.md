# Task 2 report: Shape 2 (Inner-leaf staging/materialization symmetry)

## Step 1: Test added

Added `Reference_included_Inner_leaf_of_a_join_scope_never_returns_silently_wrong_data` to
`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`, immediately after
`Whole_entity_leaf_that_is_also_reference_included_declines_rather_than_silently_returning_null`, exactly as
specified in the task brief (verbatim test body).

## Step 2: Pre-fix run (CURRENT/unmodified code)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Reference_included_Inner_leaf_of_a_join_scope_never_returns_silently_wrong_data"
```

Result: **FAILED**.

```
Assert.All() Failure: 3 out of 3 items in the collection did not pass.
[0]: Item:  Tuple ("Alice", null)
     Error: Object reference not set to an instance of an object.
[1]: Item:  Tuple ("Alice", null)
     Error: Object reference not set to an instance of an object.
[2]: Item:  Tuple ("Bob", null)
     Error: Object reference not set to an instance of an object.
```

The query ran to completion (no exception) and returned rows with `Order.Owner == null` for orders that have
a real, matching Owner in the seed data — i.e. **silently wrong data**, exactly the failure mode the design doc
flagged as possible. Shape 2 is real and reachable: the staging side (`NativeJoinScopeProjectionBinder`)
stages the Inner leaf as native despite the reference Include, while the materialization side's own guard
(`BindResultMember`, from `0b0a7a6d`) declines to populate the Include — the two halves disagree exactly as
suspected, and the disagreement resolves in favor of the silently-wrong outcome, not a clean decline.

**Conclusion: proceed to Step 3 (guard fix required).**

## Step 3: Guard fix applied

`src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs` (around
line 231), mirroring `BindResultMember`'s `allCollectionIncludes` guard from `0b0a7a6d`:

```csharp
var unwrappedLeafBody = leafBody;
var allCollectionIncludesInLeaf = true;
while (unwrappedLeafBody is IncludeExpression include)
{
    if (include.Navigation is not { IsCollection: true })
    {
        allCollectionIncludesInLeaf = false;
    }

    unwrappedLeafBody = include.EntityExpression;
}

if (allCollectionIncludesInLeaf && MongoTransparentScopeResolver.TryResolveScopeDepth(
        unwrappedLeafBody, rootParam, hopNames: ["Outer", "Inner"], sourceCount: scope.Levels.Count, out var scopeIndex))
{
```

A `false` `allCollectionIncludesInLeaf` now falls through past the recognition arm entirely, exactly as
`BindResultMember`'s own guard falls through to its pre-existing fallback — no `MongoProjection` is staged for
a leaf containing any reference Include.

## Step 4: Post-fix run of the new test

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Reference_included_Inner_leaf_of_a_join_scope_never_returns_silently_wrong_data"
```

Result: **PASSED** (Passed: 1, Failed: 0).

## Step 5/6: Full suite run — REGRESSION FOUND, work paused here

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NativeJoinTests|FullyQualifiedName~NativeReferenceIncludeTests|FullyQualifiedName~NativeJoinScopeProjectionBinderTests"
```

Result:
- `MongoDB.EntityFrameworkCore.FunctionalTests.dll`: **80/80 passed** (includes all `NativeJoinTests`, the two
  bare reference-Include tests `Whole_outer_entity_leaf_that_is_also_reference_included_goes_native` and
  `Whole_entity_leaf_that_is_also_reference_included_declines_rather_than_silently_returning_null`, and the new
  Shape 2 test).
- `MongoDB.EntityFrameworkCore.UnitTests.dll`: **26/27 passed, 1 FAILED**:

```
MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation.NativeJoinScopeProjectionBinderTests
  .Binds_a_duplicated_whole_inner_entity_leaf_that_is_also_include_wrapped [FAIL]
  Assert.Single() Failure: The collection was empty
    at .../NativeJoinScopeProjectionBinderTests.cs:line 325
```

### Why this happened

This existing unit test (`tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeProjectionBinderTests.cs:311-336`)
exercises **exactly Shape 2** — `owners.Join(orders.Include(r => r.Owner), ...)` staging a duplicated whole
Inner leaf (`new { a = r, b = r }`) where `r` carries a reference Include (`Order.Owner`, not a collection nav).
Before my fix, it asserted this leaf **does** stage as native (`Assert.Single(mongoQ.Select.Projection)`,
`NativeRoute.Projection`, 2 lookups). That assertion is precisely the staging-side half of the bug this task
closes — the test pinned the old (silently-wrong, per the new functional test) behavior as if it were the
happy path. Its own comment block describes the shape but never checked whether the reference Include actually
materializes correctly downstream (it couldn't — it's a `NativeJoinScopeProjectionBinderTests` unit test, no
materialization involved).

With the guard applied, this leaf now correctly declines (no `MongoProjection` staged, presumably
`NativeRoute.Fallback`), so the test's assertions (`Assert.Single(...)`, `Route == Projection`, `Lookups.Count == 2`)
fail as written.

### Why I stopped here rather than editing a third file

The task brief's "Code Organization" section states: *"This task touches at most two files: the test file and
(conditionally) `NativeJoinScopeProjectionBinder.cs`. If you find yourself needing to touch anything else, stop
and report DONE_WITH_CONCERNS explaining why."*

Fixing this regression requires editing a **third file**
(`tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeProjectionBinderTests.cs`)
to update `Binds_a_duplicated_whole_inner_entity_leaf_that_is_also_include_wrapped` so it asserts the new
(correct) decline behavior instead of the old native-staging behavior it wrongly pinned. I did not make that
edit — per the brief's explicit stop condition, I'm reporting back instead of unilaterally widening scope,
especially since the brief also references "the design doc's Review Focus item about not touching the
root-cause-A2 area without verification."

**My assessment (not yet acted on):** the fix appears correct and this unit test needs its assertions updated
to expect decline — i.e. change the expected outcome from `Assert.Single(mongoQ.Select.Projection)` /
`NativeRoute.Projection` / `Assert.Equal(2, mongoQ.Lookups.Count)` to something asserting
`Assert.Empty(mongoQ.Select.Projection)` / `NativeRoute.Fallback` / `Assert.Empty(mongoQ.Lookups)`, mirroring
`Declines_when_a_sibling_leaf_reached_through_the_whole_entity_reference_is_untranslatable`'s existing decline
assertions in the same file (lines 416-432). I have not made this change and am awaiting direction.

## Coordinator ruling: update the third file, then commit all three together

The coordinator ruled this is not scope creep — the pre-existing unit test pinned the exact bug being fixed
and must be corrected to match the now-fixed real behavior. Instructed to update its 3 assertions to expect a
clean decline, consider renaming it, re-run the full filter set, and commit all three files together.

### Unit test update

`tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeProjectionBinderTests.cs`:
renamed `Binds_a_duplicated_whole_inner_entity_leaf_that_is_also_include_wrapped` to
`Declines_a_duplicated_whole_inner_entity_leaf_that_is_also_include_wrapped` (the old name no longer describes
what it verifies). Rewrote its doc comment to describe the EF-322 Task 2 finding and the new expected
(decline) behavior instead of native staging with 2 lookups.

Assertions changed from:
```csharp
var projection = Assert.Single(mongoQ.Select.Projection);
Assert.Equal(innerPrefix, projection.Alias);
var innerLeaf = Assert.IsType<MongoElementRefExpression>(projection.Expression);
Assert.Equal(innerPrefix, innerLeaf.Path);
Assert.Equal(2, mongoQ.Lookups.Count);
Assert.False(mongoQ.Select.HasUnconfirmedCandidateJoin);
Assert.Equal(NativeRoute.Projection, mongoQ.Select.Route);
```
to:
```csharp
Assert.NotNull(mongoQ.Select.JoinScope);
Assert.Empty(mongoQ.Select.Projection);
Assert.Equal(2, mongoQ.Lookups.Count);
Assert.Equal(NativeRoute.Fallback, mongoQ.Select.Route);
```

**One deviation from the coordinator's suggested mirror**: the coordinator's suggested assertions included
`Assert.Empty(mongoQ.Lookups)`, mirrored from
`Declines_when_a_sibling_leaf_reached_through_the_whole_entity_reference_is_untranslatable` (no Include in that
shape). Running that first draft failed:
```
Assert.Empty() Failure: Collection was not empty
Collection: [LookupExpression { As = "_lookup_Orders", ... }, LookupExpression { As = "_lookup_Owner", ... }]
```
Both lookups (the join's own and the Include's own) are registered unconditionally by earlier join/Include
resolution, *before* this projection binder ever runs — independent of whether the trailing leaf itself
commits natively. This exact behavior is already documented and asserted elsewhere in the same file, on
`Declines_a_chain_scalar_leaf_that_spans_two_scopes` (line ~774-782, `Assert.Equal(2, mongoQ.Lookups.Count)`
alongside `NativeRoute.Fallback`, with a comment explaining why it's *not* `Assert.Empty`). Corrected the new
test to `Assert.Equal(2, mongoQ.Lookups.Count)` to match this pre-existing, measured behavior, and added a
comment cross-referencing that test.

### Re-run of the full filter set (post all three changes)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NativeJoinTests|FullyQualifiedName~NativeReferenceIncludeTests|FullyQualifiedName~NativeJoinScopeProjectionBinderTests"
```

Result:
```
MongoDB.EntityFrameworkCore.UnitTests.dll: Passed! - Failed: 0, Passed: 27, Skipped: 0, Total: 27
MongoDB.EntityFrameworkCore.FunctionalTests.dll: Passed! - Failed: 0, Passed: 80, Skipped: 0, Total: 80
```
0 failures across both assemblies.

### Commit

All three files committed together as `91a8528a`:

```
EF-322: guard NativeJoinScopeProjectionBinder's Inner-leaf unwrap against reference Includes

Design doc Shape 2 (parked, unverified at A2's final review): the staging side's
IncludeExpression unwrap had no IsCollection check, mirroring the gap
BindResultMember (the materialization side) already got a guard for in 0b0a7a6d.
Verified reachable and silently wrong (Order.Owner came back null with no
exception) via a new functional test; the same allCollectionIncludesInLeaf
guard is applied here, symmetric with 0b0a7a6d, so the Inner leaf now declines
cleanly instead. A pre-existing unit test
(Binds_a_duplicated_whole_inner_entity_leaf_that_is_also_include_wrapped,
renamed to Declines_...) pinned the old, now-incorrect native-staging outcome
for this exact shape; updated to assert the corrected decline behavior.
```

`git status --short` after commit shows only pre-existing untracked docs (unrelated `docs/superpowers/*`
files from earlier session work), nothing else outstanding.

## Files changed (all committed in 91a8528a)

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs` — new test (Step 1).
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs` — guard fix
  (Step 3).
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeProjectionBinderTests.cs`
  — updated (renamed + re-asserted) per coordinator ruling.

## Self-review

- Determined the guard fix was needed by actually running the test against unmodified code first (Step 2,
  confirmed silently-wrong null `Owner`, no exception) — yes.
- Guard mirrors `BindResultMember`'s `allCollectionIncludes` pattern exactly (same tracking variable spirit,
  same fall-through-to-decline semantics) — yes.
- Verified the two already-passing bare reference-Include functional tests still pass — yes (both in the
  80/80 FunctionalTests pass, both before and after the unit-test update).
- Found one pre-existing unit test that encoded the bug as expected behavior; per coordinator ruling, updated
  it rather than leaving it red — verified the corrected assertions against actual observed
  `mongoQ.Lookups`/`Route` values rather than assuming the sibling decline test's exact shape applied
  unchanged (the `Assert.Empty(mongoQ.Lookups)` mirror did not hold here; used the measured `Count == 2`
  instead, matching a different but analogous pre-existing precedent in the same file).
- Full filter-set run is 0 failures across UnitTests (27/27) and FunctionalTests (80/80) after all three files
  are in place.
