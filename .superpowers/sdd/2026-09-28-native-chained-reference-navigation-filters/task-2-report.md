# Task 2 report: Bare root-entity leaf over a chained scope

## Summary

Implemented exactly as specified in the brief. A bare root-entity leaf over a chained (`Levels.Count > 1`) join
scope (`ti => ti.Outer.Outer`, nav-expansion's shape for a multi-hop reference-navigation filter) now confirms
the entire join chain and goes native, instead of falling to Fallback.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeTranslator.cs` — added public wrapper
  `TryResolveBareScopeLeaf(MongoJoinScope, ParameterExpression, Expression, out int)` directly above the
  existing private `TryRerootToBareScope`, delegating to it verbatim (no BOM — confirmed unchanged).
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` — added
  the new arm to `TranslateSelect`, inserted directly after the depth-1 bare-leaf arm (ends with
  `MarkJoinLookupConfirmed();`) and before the wrapped-projection arm. Also updated the depth-1 arm's trailing
  comment from "Possible follow-up: use NativeJoinScopeProjectionBinder.ConfirmEntireChain for chains." to
  "A bare root leaf over a chain is handled by the next arm." (UTF-8 BOM — confirmed unchanged).
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/JoinScopeWhereSlotPopulationTests.cs` —
  appended the three brief-specified tests: `Root_entity_leaf_after_second_level_where_over_chained_join_goes_native`,
  `Inner_entity_leaf_over_chained_join_is_not_confirmed`, `Root_entity_leaf_over_chained_join_with_paging_between_joins_declines`
  (no BOM — confirmed unchanged).

Diffs match the brief's exact code verbatim (verified via `git show` after commit).

## TDD evidence

### RED

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~JoinScopeWhereSlotPopulationTests"
```

Result: `Root_entity_leaf_after_second_level_where_over_chained_join_goes_native` FAILED —
`Assert.False() Failure / Expected: False / Actual: True` on the `HasUnsupportedOperator` assertion (line
`Assert.False(mongoQ.Select.HasUnsupportedOperator);`), matching the brief's expectation exactly ("FAILS
(`HasUnsupportedOperator` true, from TranslateSelect's projected-Select branch)"). The two decline-pin tests
(`Inner_entity_leaf_over_chained_join_is_not_confirmed`, `Root_entity_leaf_over_chained_join_with_paging_between_joins_declines`)
PASSED as expected — total: 1 failed, 16 passed, 17 total.

### GREEN

After implementing Steps 3–4:

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
→ Build succeeded, 0 errors.

```
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build
```
→ `Passed! - Failed: 0, Passed: 1417, Skipped: 0, Total: 1417, Duration: 1 s`

No `HasUnconfirmedCandidateJoin` failure encountered — went straight to full green, so the Step 5 stop-condition
(candidate/confirmation count mismatch) never triggered.

## BOM verification

`head -c3 <file> | xxd` before and after, on all three touched files:
- `NativeJoinScopeTranslator.cs`: `2f2a 20` (`/* `) — no BOM, unchanged.
- `MongoQueryableMethodTranslatingExpressionVisitor.cs`: `efbb bf` — UTF-8 BOM, unchanged.
- `JoinScopeWhereSlotPopulationTests.cs`: `2f2a 20` — no BOM, unchanged.

## Self-review

- New arm placed exactly between the depth-1 bare-leaf arm and the wrapped-projection arm, as the brief
  requires (`git show` confirms it lands immediately after the depth-1 arm's closing brace and before the
  wrapped-projection `else if`).
- No mutation before gate: the arm's body is a single statement (`ConfirmEntireChain`) reached only once every
  `&&` condition (bare-scope-leaf resolution, root index, paging guard, `IsSingleEligibleNativeJoinScope`) has
  passed — consistent with the "stage into locals, commit only after every gate passes" invariant (nothing to
  stage here since the arm makes no intermediate mutations).
- Paging guard (`!(HasPaging && HasPagingRecordedAfterAJoin)`) is checked before
  `IsSingleEligibleNativeJoinScope`, per the brief's explicit ordering requirement (that call can itself defer
  PipelineOps via `DeferPipelineOpsPastConfirmedJoin`).
- No `return` statement in the new arm, matching the depth-1 arm's pattern and the brief's note that the
  generic shaper fold at the end of `TranslateSelect` resolves `ti.Outer.Outer` against the join's shaper.
- `TryResolveBareScopeLeaf` is a pure one-line delegation to the existing `TryRerootToBareScope`, adding no new
  logic — matches the brief's "new public wrapper" framing exactly.
- Gates (`HasUnconfirmedCandidateJoin`, `IsSingleEligibleNativeJoinScope`'s paging gates, post-confirmed-join
  guard) were not touched or loosened.
- Full unit-test suite (1417 tests) green; no regressions.

## Concerns

None. Implementation matches the brief verbatim; no ambiguity encountered; no stop-condition triggered.
