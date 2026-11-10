# SDD ledger — plan: docs/superpowers/plans/2026-09-28-native-chained-reference-navigation-filters.md

Start HEAD: fdd36a39 (branch EF-322-Native-LINQ-rebased). No separate spec doc: the plan's Background section is the
authority, so rulings are provisional.

Ruling: work in place on EF-322-Native-LINQ-rebased, no new worktree — it's a feature branch (not main), and prior
plans on this branch ran the same way — if wrong, the commits are easy to cherry-pick onto a separate branch.

## Pre-flight scan

| Rows | Produces → consumes / checked | Finding |
|---|---|---|
| T1 ↔ T2 (NativeJoinScopeTranslator.cs, JoinScopeWhereSlotPopulationTests.cs) | T1 adds TryTranslateSingleScopePredicate + Where arm; T2 adds TryResolveBareScopeLeaf; T2's first unit test composes T1's Where arm | consistent; distinct names, no overlap |
| T1 ↔ T3 | T3 end-to-end needs T1's Where arm | consistent |
| T2 ↔ T3 | T3 needs T2's root-leaf arm; T3's helpers originally projected `Select(o => o.OrderNo)` server-side = a chained scalar leaf, which no task makes native | DEFECT — every NativeAndParity test would decline for a reason unrelated to the arms under test |
| T3 ↔ T4 | T4 only runs suites | consistent |
| T1 self | Step 2 expects the two decline pins to pass pre-change: cross-scope body and inner-join null check both reach the catch-all today | consistent. The inner-join null check is matched structurally by TryMatchScopeNullCheck, then the guard returns false, so it still declines after the change |
| T2 self | tests: bare root leaf goes native; x.l leaf stays Fallback (IsTransparentIdentifierMemberAccessSelector skips the MARK branch, so candidates stay unconfirmed); Take between joins → HasPaging && HasPagingRecordedAfterAJoin | consistent |
| T3 self | Ordering_and_paging expected [5,4], but RunOrders sorts ascending client-side | DEFECT — should be [4,5] |
| T4 self | rebaseline only on AssertMql-only failures | consistent |

Ruling: T3 helpers and the two explicit tests materialize whole entities and project client-side
(`.AsEnumerable().Select(...)`) instead of a server-side scalar Select — the scalar leaf over a chain is out of scope and
would mask the arms under test — if wrong, T3 under-tests the scalar-projection shape, which is still depth-1 only.
Plan text patched.
Ruling: T3 `Ordering_and_paging_after_a_second_hop_filter` expects [4,5] (top-2 of desc order, re-sorted ascending by
the helper) — plan arithmetic error — costs nothing if wrong; the test would fail loudly. Plan text patched.
Task 1: dispatched implementer (agent a920896120d21510c, sonnet), BASE fdd36a39
Task 1: implementer DONE (4b9bd104, unit 1414/1414); review dispatched (opus) on fdd36a39..4b9bd104. Note: NativeJoinScopeTranslator.cs has no BOM (plan claim wrong; unchanged).
Task 1: minor (deferred): comment at NativeSlotPopulator.cs:452 cites TryTranslateConditionalSortKey but the guard actually mirrors NativeJoinScopeProjectionBinder.TryTranslateScopeNullCheckConditional (IsLeftOuter + ForceUnwind); TryMatchScopeNullCheck doc (NativeJoinScopeTranslator.cs:228) still says callers must check non-collection
Task 1: minor (deferred): no unit test of the accepting null-check path (left-outer level-2 x.l == null → MongoLookupNullCheckExpression with InnerPrefix); Task 3 covers it against a real DB
Task 1: minor (deferred): ForceUnwind gate checks only the level being read, not the earlier levels (harmless: every JoinInfo.Lookup is ForceUnwind today)
Task 1: complete (commits fdd36a3..4b9bd10, review clean)
Task 2: dispatched implementer (agent a195344fc5bbe05a3, sonnet), BASE 4b9bd104
Task 2: implementer DONE (2ada0c71, unit 1417/1417)
Task 2: minor (deferred): paging-between-joins unit test asserts only Route == Fallback; tighten with Assert.False(HasConfirmedJoinLookup)
Task 2: minor (deferred): comment claims not pinned — Where(non-root).Take(n).Select(root) should go native; Take-before-any-join chain keeps existing handling; add unit pins
Task 2: minor (deferred): guard over-declines paging written after both joins (flags can't distinguish it from between-joins); conservative/safe, worth a comment sentence
Task 2: complete (commits 4b9bd10..2ada0c7, review clean)
Task 3: dispatched implementer (agent adb388c595226f4e2, sonnet), BASE 2ada0c71
Task 3: implementer BLOCKED — 10/11 pass; Comparison_spanning_two_hops_declines_cleanly got [1,4,5], expected [1]. Native == DriverLinq, and NativeOnly declines as intended.
Ruling: Comparison_spanning_two_hops_declines_cleanly expects [1,4,5] — EF's C# null semantics make null == null true for orders 4/5 (Customer missing ⇒ both sides null); the plan's [1] assumed SQL null semantics. The test's purpose (clean decline + trustworthy fallback) is unchanged — if wrong, a real null-semantics bug in the driver path goes unpinned, but both paths agree, so it would be a pre-existing driver behaviour, not this work.
Task 3: implementer DONE after ruling (cf1a8f4a, 11/11 functional)
Task 3: minor (deferred): RunOrders/RunEmployees near-duplicate helper shape (NativeChainedReferenceNavigationFilterTests.cs:168-203)
Task 3: complete (commits 2ada0c7..cf1a8f4, review clean)
Task 4: dispatched implementer (agent ae80e1bd256baf5f0, sonnet), BASE cf1a8f4a
Task 4: implementer DONE (5f4ac271; EF8/9/10 full suites green; NativeOnly 192→178, 0 regressions; bonus Where_join_orderby_join_select rebaselined)
Ruling: EF8 NativeOnly Collection_select_nav_prop_predicate failure (not in the plan's known-failure list) accepted as pre-existing — the implementer reproduced it on a worktree build of fdd36a39, and it's an EF-216 collection-projection gap unrelated to the chained arms — if wrong, an EF8-only regression slipped past this plan; the final review is asked to check.
Task 4: minor (deferred): native sort emits $set __sort0 / $sort / $unset for a plain _id key (Where_join_orderby_join_select) instead of { $sort: { _id: 1 } }; can't use the _id index. Pre-existing sort emitter, not this plan
Task 4: minor (deferred): Where_join_orderby_join_select doesn't assert order upstream, so only the MQL baseline guards sort placement
Task 4: complete (commits cf1a8f4..5f4ac27, review clean)
Ruling: the final review covers fdd36a39..HEAD (this plan's range), not merge-base with main — the branch carries weeks of prior EF-322 work already reviewed under other plans — if wrong, interactions with older branch code only get a named-risk look.
Final review: dispatched (agent a26b1e591eb29099e, opus) on fdd36a39..5f4ac271
Final review: With fixes — Critical 1: paging in PostJoinOps before a later row-multiplying join is emitted after every $lookup (wrong rows in Native mode; used to fall back); Important 2: no pin for it; Minor 3: owned root declines (safe); Minor 4: stale comments; Minor 5: $match after the Include-only lookup (performance).
Ruling: close Critical 1 at join-recording time for every arm — decline when the outer select's PostJoinOps already holds a Skip/Limit/Distinct op and the new join isn't provably row-count-preserving (fail closed if unknown) — it also closes the pre-existing wrapped-projection exposure — if wrong, some currently-native shapes decline to driver LINQ (safe, visible in the NativeOnly sweep count).
Ruling: Minor 3 (owned-root unwrap) and Minor 5 (Include $match placement) deferred as follow-ups — both are safe/perf-only — cost: those shapes stay on the fallback or run a slower pipeline.
Final fix: dispatched ONE fixer (opus), FIX_BASE 5f4ac271
Final fix: DONE_WITH_CONCERNS (c45345ea; EF10 spec default green, NativeOnly 178 unchanged, 147/147 target; EF8 ok; EF9 not run; PipelineOps between-joins paging still relies on per-arm guards)
Final fix re-review: all findings ADDRESSED, no new breakage (c45345ea)
Ruling: park the implementer's concern (paging kept in PipelineOps between two joins with no Inner-side Where) — out of scope for this fix, pre-existing, and guarded per arm; the new root-leaf arm declines it — if wrong, a future arm could re-expose the chain-paging gap unless it copies the guard.
Final: EF9 unit 1427/0, functional 3467/0 (52 skip), spec 5027/0 (11 skip), nav NativeOnly 147/147
