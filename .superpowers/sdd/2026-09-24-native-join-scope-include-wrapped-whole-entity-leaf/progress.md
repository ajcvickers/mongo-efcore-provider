# SDD ledger — plan: docs/superpowers/plans/2026-09-24-native-join-scope-include-wrapped-whole-entity-leaf.md

Pre-flight: Task 2 consumes Task 1's fix (unconditional over scope.Levels.Count) and the pre-existing
TranslateThreeSourceJoinQuery harness — no interface mismatch, Task 2 is a proof test only. Task 3 consumes
Task 1's fix via the real compiled pipeline (functional, not unit) — no interface mismatch. Task 4 consumes
Tasks 1-3 collectively — no interface mismatch. No shared interfaces beyond "the fix exists and works",
already covered per-task by each task's own Expected: lines.

Task 1: Ruling: dropped the plan's `.ThenInclude(x => x.Owner)` from the Outer whole-entity-leaf test —
`Order.Owner` is the auto-fixed-up inverse of `Owner.Orders`, and EF Core's own NavigationExpandingExpressionVisitor
throws `NavigationBaseIncludeIgnored` for a ThenInclude targeting a nav it already fixed up walking back up the
tree. Unrelated to the fix under test; a single-level Include already exercises the unwrap loop (a `while`, so
depth-agnostic by construction), and Task 2 provides the multi-level-shape proof instead. Cost if wrong: none —
Task 2's chain test independently proves depth generalization.
Task 1: Ruling: `Binds_a_duplicated_whole_inner_entity_leaf_that_is_also_include_wrapped` asserted
`Assert.Single(mongoQ.Lookups)`; changed to `Assert.Equal(2, mongoQ.Lookups.Count)` — the test's own Include
target (`Order.Owner`) is deliberately UNRELATED to the join's own key, so it correctly registers its own
separate `$lookup` in addition to the join's, exactly as the design doc predicts. My own test assertion was
wrong, not the production code. Cost if wrong: none — verified against actual staged `Lookups` contents in the
failure output (`_lookup_Orders` + `_lookup_Owner`, both real, both expected).
Task 1: complete (commits cc6c6c4e..d1107a77, tests: dotnet test UnitTests.csproj -c "Debug EF10" --no-build → 1284/1284 pass)

Task 2: Ruling: plan named Including the chain's LAST level (`l.Order`) for the chain-depth proof test.
MEASURED: that shape makes `scope.Levels.Count` come back 3, not 2 — `ChainOrderLine.Order`'s target type
(ChainOrder) coincidentally matches Levels[0]'s own InnerEntityType, and TranslateJoinCore's candidate-join
detection folds the Include's own reference nav in as a THIRD chain level — the exact pre-existing "RESIDUAL
GAP" (coincidental CLR-type match) NativeJoinScopeTranslator's own remarks already document as a separate,
pre-existing hazard, not something this plan owns or needs to characterize further. Re-rooted the test's
Include onto the ROOT leaf (`owners.Include(o => o.Orders)`) instead — identical to Task 1's already-proven
Outer-Include case, just composed ahead of a genuine 2-level chain, with no opportunity to interact with the
second join. Cost if wrong: none — this still exercises the unwrap loop inside a real Levels.Count > 1 scope,
which is this task's actual goal; renamed the test from "...at_the_last_level..." to "...root..." to match.
Task 2: complete (commits d1107a77..6e0446a4, tests: dotnet test UnitTests.csproj -c "Debug EF10" --no-build → 1285/1285 pass)

Task 3: Ruling: STOPPED — do not proceed with Task 3/4 under this plan without a new design. Wrote the two
functional tests exactly as specified in the brief (both promoted spikes) and ran them for real (Docker/Mongo
available). BOTH fail, but not with NativeTranslationNotSupportedException — they fail with wrong/crashing
materialization, proving the design doc's "Why the materialization side needs no change" section is WRONG for
this shape family. Root-caused via MQL dump + reflection: `MongoQueryableMethodTranslatingExpressionVisitor
.BindResultMember` (the wrappedLeafJoin fold's per-member binder) pattern-matches
`foldedExpression is StructuralTypeShaperExpression` directly. For an Include-wrapped leaf, `foldedExpression`
is `IncludeExpression { EntityExpression: <shaper> }`, so the match fails and the code falls through to
`BindSelectManyMember`, registering the RAW (still Include-wrapped, unfolded) leaf under the member's OWN
alias in `mongoQueryExpression.Projection` — a field the native `$project` never emits (Outer leaf emits under
`$$ROOT`; Inner leaf emits under its fixed `InnerPrefix`, never the member's alias). Outer case: reading `$$ROOT`
as a scalar of the entity's CLR type throws `FormatException` (extra elements from the join's own lookup field).
Inner case: reading a field that was never emitted returns null silently (no exception) — matches the observed
`Whole_inner_entity_leaf...` mismatch (Alice's Total came back null despite a real match).
Attempted fix: unwrap the IncludeExpression chain off `foldedExpression` before the `StructuralTypeShaperExpression`
check (mirroring Task 1's unwrap), rebind, then re-wrap the SAME IncludeExpression chain around the rebound
shaper so `MongoProjectionBindingExpressionVisitor`'s IncludeExpression case still finds it. This compiled, but
broke WORSE: `System.ArgumentException: must be reducible node` at `LambdaCompiler.Compile`, meaning some
Extension-typed node is left un-reduced downstream — most likely because the join-scope wrapped-leaf arm never
does the Include-specific bookkeeping (AddLookup / MarkReferenceIncludeConfirmed-equivalent for the Include's
OWN nav) that the OTHER, bare-selector Include-over-join-scope arm
(`TryGetCollectionIncludeOverJoinScope`) does — so `MongoProjectionBindingExpressionVisitor`'s IncludeExpression
processing (which registers the Include's own `$lookup` "unconditionally, later") runs against missing/wrong
state for this arm specifically. Confirmed via a control test (`Orders.Include(o => o.Owner).Select(o => new
{ o, o.OwnerId })`, no join at all) that wrapped-Include-with-`new{}` ALSO has a pre-existing, SEPARATE,
non-crashing data gap in this codebase (the included nav came back empty) — so this is not solely a join-scope
problem, and a correct fix is a multi-file undertaking (NativeJoinScopeProjectionBinder's Include bookkeeping,
BindResultMember's unwrap, and possibly MongoProjectionBindingExpressionVisitor's IncludeExpression case), not
a one-line patch. REVERTED the attempted production fix (git checkout on
MongoQueryableMethodTranslatingExpressionVisitor.cs) rather than ship a half-fix or paper over it. Left the two
Task-3 test methods in NativeJoinTests.cs (RED, as literally specified in the brief) since they are correct
per the plan and per the design doc's own spikes — they should stay red until the follow-up work lands, not be
weakened or deleted. Cost if wrong: none — reverting a broken change and leaving accurate failing tests is the
safe choice; the alternative (a half-working rewrap) would have shipped a WORSE regression (a hard crash) than
today's silent-fallback-decline behavior.

STOPPING THIS PLAN HERE. Recommend: file a new ticket/design ("EF-322 Phase 2 Group B root cause A2 —
materialization side of Include-wrapped join-scope whole-entity leaf") scoped to fix `BindResultMember`'s
unwrap AND the Include lookup-registration bookkeeping the wrapped-leaf arm is missing, verified against both
of Task 3's tests plus the pre-existing wrapped-reference-Include-no-join data gap found as a control. Task 4
(spec baseline regeneration) is blocked on that follow-up — do NOT attempt it against the current broken
materialization path.
