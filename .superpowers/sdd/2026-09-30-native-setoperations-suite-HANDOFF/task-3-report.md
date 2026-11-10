# Task 3 (J) report: a join-scope source1 in a projected set operation

**Status:** DONE_WITH_CONCERNS (minor; see Concerns)
**Commit:** `a0c1fb71 EF-322: native projected set operation over a join-scope source1` on `EF-322c-setops` (not pushed)

## Implemented

- `MongoQueryableMethodTranslatingExpressionVisitor.IsPlainProjectedSelect`: when `allowPreCombineLookups` is true
  (source1 only), a join query is admitted if `IsPreCombineJoinScope(mongo)`. The lookup allow-list becomes
  `InjectAfterRoot || ReferenceEquals(lookup, some Joins[i].Lookup)`. When the flag is false (source2), the
  check is unchanged: `!IsJoinQuery && Lookups.Count == 0`.
- New `IsPreCombineJoinScope` = `Select.HasConfirmedJoinLookup && Select.JoinScope is { Levels.Count: 1 }`.
  - `HasConfirmedJoinLookup` is set on a projected select (no grouping, no reducer) only by TranslateSelect's
    join-scope arms. Each of those arms is gated by `IsSingleEligibleNativeJoinScope`.
  - So the new predicate reuses that decision instead of re-running it. Re-running it could mutate state:
    `DeferPipelineOpsPastConfirmedJoin`.
  - The gate and the rewrite share this one predicate. There is no rewrite at the gate, so nothing mutates
    before a decline.
- `MongoSelectLowerer`, OperandsProjected branch: after `AppendLookupStages`, it now emits `PostJoinOps`, then
  `PostLookupPagingOps`, then source1's `$group`/`$project`. That is the same order as the non-set-op path.
- Updated the comments that said these lists can't co-occur with a set op, in `MongoSelectLowerer` and
  `MongoSelectDefinition._postJoinOps`.
- Negate was not touched.

## Tests (NativeGroupByOverJoinTests.cs, extended)

Every positive test uses `NativeAndParity`, which compares NativeOnly with explicit DriverLinq, and also checks a
hand-computed oracle. The seed is Owners/Orders, where Owner is required and Reviewer is optional.

| Test | Expected |
|---|---|
| Union_of_a_join_scope_operand_filtered_on_a_matching_inner_value_goes_native (Owner.Name == "Alice") | [1,10,20,30] |
| Concat_of_a_join_scope_operand_filtered_on_a_matching_inner_value_keeps_duplicates | [1,10,20,20,30] |
| Union_of_a_join_scope_operand_filtered_on_an_unmatched_inner_value_goes_native ("Nobody") | [1,20,30] |
| Concat_of_a_paged_join_scope_operand_pages_the_filtered_rows (inner Where, OrderBy, Skip(1), Take(1)) | [1,20,20,30] |
| Concat_of_an_optional_navigation_join_scope_operand_filtered_on_a_matching_inner_value (left join, like the spec) | [1,20,30,30] |
| Concat_of_a_join_scope_operand_with_paging_deferred_past_the_lookup (exercises PostLookupPagingOps; seed without the dangling owner) | [Alice,Alice,North,North,South] |
| Union_with_a_two_level_join_scope_first_operand_declines_cleanly | DeclinesCleanly |
| Union_with_a_join_scope_second_operand_declines | see below |

The source2-join test is not a `DeclinesCleanly` test. That shape has no driver-LINQ oracle: the fallback throws
the pre-existing "Unsupported cross-DbSet query" `InvalidOperationException`. The test therefore pins three things:
- NativeOnly throws `NativeTranslationNotSupportedException`.
- Native throws that same cross-DbSet `InvalidOperationException`.
- DriverLinq throws it too.

## RED / GREEN (EF10)

- **RED:** all 6 positive tests failed with `NativeTranslationNotSupportedException` ("Query projects a
  non-entity result..."). The two decline tests passed.
- **GREEN:** 8/8 pass. The three set-op test classes pass in full (261/261).

## Mutation matrix (EF10)

Suite run for each mutation: NativeGroupByOverJoinTests, NativeSetOperationProjectionTests and NativeSetOpsTests
(261 tests).

| Mutation | Result |
|---|---|
| Lowerer drops the `PostJoinOps` emission | 5 fail, all with **extra rows**. For example, matching-city Union gives [1,5,10,20,30] instead of [1,10,20,30], and paged Concat gives [1,5,10,20,20,...]. The spec Concat_with_one_side_being_GroupBy_aggregate under NativeOnly also fails (480 rows vs 53). |
| Lowerer drops the `PostLookupPagingOps` emission | ..._paging_deferred_past_the_lookup fails: [Alice,Alice,Bob,Cara,...] |
| Drop the `Levels.Count == 1` conjunct | Union_with_a_two_level_join_scope_first_operand_declines_cleanly fails (it goes native) |
| `IsPreCombineJoinScope => false` | all 6 positives fail |
| Drop the join-lookup disjunct from the allow-list | all 6 positives fail |
| Pass `allowPreCombineLookups: true` for source2 | Union_with_a_join_scope_second_operand_declines fails (it goes native) |
| Drop the `HasConfirmedJoinLookup` conjunct | **nothing fails** |

The `HasConfirmedJoinLookup` conjunct is unreachable defence in depth. I added a probe that logged every
unconfirmed join query reaching the predicate, with no change to behaviour. It never fired across the full EF10
functional and spec suites. That fits the design: an unconfirmed candidate join already has
`Route == Fallback`, so `IsPlainProjectedSelect` stops at its Route check first.

## Spec tests (NorthwindSetOperationsQueryMongoTest, MONGODB_EF_NATIVE_ONLY=1)

- **EF10:** 22 failing before (measured with the source reverted to HEAD), 20 after.
  - Flipped: Concat_with_one_side_being_GroupBy_aggregate (both async variants).
  - The remaining 20 are all the unary bucket that Task 4 owns.
- **EF8 / EF9:** 20 failing after, and they are the same unary tests.

**Baseline regenerated:** Concat_with_one_side_being_GroupBy_aggregate only, with a tight filter and
EF_TEST_REWRITE_BASELINES=1, then rebuilt and rerun green. The native MQL differs from the old baseline in two ways:
- There is no `$set`/`$ifNull` after the `$unwind`.
- The grouped operand uses a `$max` accumulator instead of `$push` + `$max` over `_elements`.

## Other runs

- **Step 4, default mode:** NorthwindSetOperations, NorthwindJoin and NorthwindGroupBy together gave 821 passed,
  0 failed on EF10. EF8 gave 803/0 and EF9 811/0. There was no diff: no other baselines changed.
- **Full EF10 suites, final code:** functional 4188 passed / 0 failed, spec 4768/0, unit 1565/0.
- **EF8 and EF9:** both built with 0 errors. The three functional set-op classes passed 261/261 on each, the spec
  set-op class under NativeOnly had the 20 unary failures noted above, and the default-mode spec classes were green.
  I did not run the full EF8/EF9 suites.

## Files changed

- src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs
- src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs
- src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs (comment only)
- tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeGroupByOverJoinTests.cs
- tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindSetOperationsQueryMongoTest.cs (baseline)

BOMs were preserved. The untracked plan doc and nuget.config were not touched.

## Concerns

1. **The brief's premise about the spec test is partly wrong.** It said the "Seatte" typo lets a dropped `$match`
   pass. It doesn't: dropping the `$match` returns every order, so the spec also fails (480 vs 53 rows). The
   matching-city tests remain the tighter proof.
2. **The source2-join test is weaker than asked.** It can't be `DeclinesCleanly` because driver-LINQ has no
   oracle for that shape. It pins the decline plus the identical fallback error instead.
3. **The HasConfirmedJoinLookup conjunct is not mutation-proven.** Evidence that it is unreachable is above.
4. **The single-level restriction was not tested for correctness.** With the `Levels.Count == 1` conjunct removed,
   the two-level chain goes native. I did not check whether its data would then be correct; the restriction is
   kept as the brief asked.
5. **Probe run for the unused conjunct.** A second full EF10 run was needed for the reachability probe. It is not
   in the code; the final full run was on the committed code.

Scratch logs: /private/tmp/claude-502/-Users-arthur-vickers-code-provider2/1e74fee7-dd23-4ba4-ae57-0d16411d74fb/scratchpad/task3/

---

## Fix round 1: Intersect/Except over a join-scope source1

**Finding:** the widened gate doesn't look at the set-op kind. Intersect/Except over a join-scope source1 used to
fail in every mode; they now go native and lower to MongoSetDifferenceStage over source1's joined, filtered,
projected rows. The original report didn't mention this.

**Result:** the new tests show correct data, so I did not restrict the gate to Union/Concat.

**Commit:** amended, not a fixup. Old SHA a0c1fb71, new SHA **16c9c9fe**, same subject. The commit message now
includes the Intersect/Except mutation results.

### Changes

- **Two new NativeOnly hand-oracle tests** in NativeGroupByOverJoinTests. These shapes have no driver-LINQ
  oracle, so the tests don't use parity. The filter is `Owner.Name == "Alice"`.
  - Intersect_of_a_join_scope_operand_filtered_on_a_matching_inner_value_goes_native: expects [20]
  - Except_of_a_join_scope_operand_filtered_on_a_matching_inner_value_goes_native: expects [10]
- **Oracles, checked against CreateSeed:**
  - Alice owns o1 (Total 10) and o2 (Total 20), so source1 = {10, 20}.
  - Max Total by Region: North = max(10, 20, 5) = 20, South = 30, East = 1. So the grouped operand = {1, 20, 30}.
  - Intersect = {20} and Except = {10}.
  - With the $match dropped, source1 is every row the required join keeps: {10, 20, 30, 5} (o5 dangles and is
    dropped). That gives [20, 30] and [5, 10].
- **MQTEV comment at IsPreCombineJoinScope:**
  - The HasConfirmedJoinLookup conjunct is defence in depth and can't be killed by mutation, with the reason.
  - The predicate serves every set-op kind, including Intersect/Except.
- **Comment on Union_with_a_join_scope_second_operand_declines:** the pinned cross-DbSet error is a known existing
  limitation of the driver-LINQ fallback, not intended behaviour.

### Commands and output

- **Harness:** `python3 mutate.py <M> "FullyQualifiedName~NativeGroupByOverJoinTests|FullyQualifiedName~NativeSetOperationProjectionTests|FullyQualifiedName~NativeSetOpsTests"`
  on Debug EF10, with MONGODB_URI and ATLAS_URI unset.

| Mutation | Result |
|---|---|
| NONE (final code) | 263/263 passed |
| M1: drop the lowerer's PostJoinOps emission | 7 fail, including both new tests: Intersect gave [20, 30] instead of [20], Except gave [5, 10] instead of [10] |
| M5: `IsPreCombineJoinScope => false` (the pre-change gate) | 8 fail; both new tests throw InvalidOperationException (the pre-change hard failure) |

- **SetOps spec on EF10:**
  - NativeOnly: 20 failed / 163 passed, and all 20 are unary tests.
  - Default mode: 183/183 passed.
- **EF8 and EF9:** both built with 0 errors, and NativeGroupByOverJoinTests passed 90/90 on each.
- **Logs:** scratchpad/task3/fix1/
