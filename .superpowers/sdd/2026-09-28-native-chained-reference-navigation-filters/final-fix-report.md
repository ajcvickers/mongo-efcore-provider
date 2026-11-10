# Final whole-branch review: fix report

Commit: `c45345ea EF-322: decline paging in PostJoinOps ahead of a later row-changing join` (on top of 5f4ac271). Not pushed.

## Critical 1: wrong rows when paging sits in PostJoinOps ahead of a later join

**Change**
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs:2001-2011`
  (`TranslateJoinCore`, right after `RebindInnerShaperToOuterQuery`, where the new `JoinInfo`'s Navigation/Lookup
  and IsLeftOuter are settled): `if (Select.HasNonCommutingPostJoinOp && !joinInfo.IsRowCountPreserving)
  Select.MarkNotNativelyRepresentable();`. This covers every consuming Select arm: the chained root leaf, the wrapped
  projection, bare value, and Include. It fails closed because a join with no resolved Lookup is not row-count-preserving.
  It doesn't mutate anything else, and it only declines.
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/JoinInfo.cs:72-79`: new `IsRowCountPreserving`
  (`IsLeftOuter && Lookup is { Navigation.IsCollection: false }`). This is the existing notion, now shared.
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQueryExpression.Lookup.cs:222`: `AreAllJoinsRowCountPreserving`
  now delegates to `JoinInfo.IsRowCountPreserving`. Behavior is identical.
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs:245-254`: new `HasNonCommutingPostJoinOp`
  (PostJoinOps contains a `MongoSkipOp`, `MongoLimitOp` or `MongoDistinctOp`). `MongoSelectOp` subtypes are Match, Sort,
  Skip, Limit and Distinct. Match and Sort commute with a later join.
- The comment at `MongoQueryableMethodTranslatingExpressionVisitor.cs:451-456` is corrected. Paging in PostJoinOps is
  correct only if no row-changing join follows it, and TranslateJoinCore declines when one does. I added the sentence
  that the guard also conservatively declines paging written after both joins while it is still in PipelineOps.
- No existing gate was loosened.

## Important 2: pins

- New `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeChainedJoinPagingTests.cs` (no BOM). It uses
  Owners/Ords/Lines with collection navigations. Seed: Ann has order 10 (Total 5) with lines A, A, B. Bob has order 20
  (Total 7) with line A. Each test uses `NativeModeAssert.DeclinesCleanly`: NativeOnly throws, and Native equals
  DriverLinq. Hand-computed rows are also asserted.
  - `Paging_between_joins_after_a_first_level_filter_pages_before_the_second_join`: expects [Ann, Ann, Ann].
  - `..._then_a_second_level_filter` (the `.Where(x => x.l.Sku == "A")` variant): expects [Ann, Ann].
  - `..._with_a_wrapped_projection` (`(e, l) => new { e.o.Name, l.Sku }`): expects [Ann:A, Ann:A, Ann:B].
  - `Skip_between_joins_...`: expects [Bob].
  - Control `Paging_after_both_joins_following_a_first_level_filter_goes_native` (`NativeAndParity`): expects [Ann, Ann].
- `NativeChainedReferenceNavigationFilterTests.Paging_after_a_first_hop_filter_then_a_second_hop_filter`: this is the
  positive row-count-preserving case. A Take in PostJoinOps is followed by a left-outer reference-nav join. It stays
  native (NativeOnly equals DriverLinq, rows [2]) before and after the fix.
- `JoinScopeWhereSlotPopulationTests` changes:
  - New `Paging_in_post_join_ops_ahead_of_a_row_multiplying_second_join_declines`: asserts HasUnsupportedOperator,
    Route Fallback, and !HasConfirmedJoinLookup.
  - New `..._with_wrapped_projection_declines`.
  - New `Paging_after_second_level_where_over_chained_join_with_no_later_join_goes_native`: asserts the last PostJoinOp
    is a MongoLimitOp and Route is WholeEntity.
  - Tightened `Root_entity_leaf_over_chained_join_with_paging_between_joins_declines` with
    `Assert.False(HasConfirmedJoinLookup)`.

**RED on HEAD (EF10)**
- Unit: 2 failed, both new decline pins with `Assert.True() Failure` on HasUnsupportedOperator. The other 18 passed,
  including the new "goes native" pin and the tightened test.
- Functional: all 4 decline tests failed with "Assert.Throws() Failure: No exception was thrown" under NativeOnly. I
  temporarily reordered NativeModeAssert (then reverted it) to show Native's wrong rows:
  - pages_before: expected [Ann, Ann, Ann], Native gave [Ann].
  - then_second_level_filter: expected [Ann, Ann], Native gave [Ann].
  - wrapped: expected [Ann:A, Ann:A, Ann:B], Native gave [Ann:A].
  - Skip: expected [Bob], Native gave [Ann, Ann, Bob].
  - DriverLinq matched the hand-computed rows in every case.

**GREEN (EF10)**
- Build succeeded.
- UnitTests: 1420/1420 passed.
- NativeChainedJoinPagingTests + NativeChainedReferenceNavigationFilterTests: 17/17 passed.
- FunctionalTests `FullyQualifiedName~Join` (plus both classes): 257 passed, 1 skipped (pre-existing), 0 failed.

## Minor 4: stale comments
- `NativeSlotPopulator.cs:452-454`: now cites `NativeJoinScopeProjectionBinder.TryTranslateScopeNullCheckConditional`
  (left-outer level plus a ForceUnwind lookup).
- `NativeJoinScopeTranslator.cs:231-232`: callers must check the level is left-outer and that its join has a
  ForceUnwind lookup (or a non-collection navigation).

BOMs were verified after editing. MQTEV, NativeSlotPopulator and MongoSelectDefinition have a BOM (efbbbf).
NativeJoinScopeTranslator, JoinInfo, MongoQueryExpression.Lookup and the test files have none.

## Spec sweeps (EF10, whole SpecificationTests project)
- Default mode after the fix: 4768 passed, 14 skipped, 0 failed.
- `MONGODB_EF_NATIVE_ONLY=1`: 178 failed before (HEAD, run from a detached worktree) and 178 failed after.
  **The failing-test sets are identical** (`diff` of the sorted names is empty), so nothing is newly declined.
- NorthwindNavigationsQueryMongoTest under NativeOnly: 147/147 passed, including the six targets.

## EF8
- `dotnet build -c "Debug EF8"` succeeded.
- UnitTests: 1427/1427 passed.
- New functional tests plus NativeChainedReferenceNavigationFilterTests: 17/17 passed.
- NorthwindNavigationsQueryMongoTest under NativeOnly: 145/147. The only failure is the known pre-existing
  `Collection_select_nav_prop_predicate` (sync and async).

## Concerns
- Out of scope and still open: paging in **PipelineOps** written between two joins. For example
  `Join(a).Take(1).Join(b)` with no Inner-side Where, which is the chain-paging gap documented at the bare-value arm
  and in IsSingleEligibleNativeJoinScope. The existing arm-level guards still handle it; this join-recording gate only
  inspects PostJoinOps, per the ruling. The same gate could be extended to PipelineOps paging recorded after a join,
  but that would overlap the existing arm guards and wasn't asked for.
- EF9 was not run. The brief required only EF10 and EF8.
