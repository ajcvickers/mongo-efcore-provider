# SDD ledger — plan: docs/superpowers/plans/2026-08-28-ef421-correlated-element-predicates.md

Spec: docs/superpowers/specs/2026-08-28-ef421-correlated-element-predicates-design.md
Working in place on branch EF-322-Native-LINQ-rebased (no worktree — user chose "work in place").
Baseline verified: `dotnet build -c "Debug EF10"` clean; UnitTests 998/998 passing; SpecTests 7/7 passing (pre-EF-421 baseline).

## Pre-flight conflict scan

| Pair / Task | Shares | Produces → Consumes | Finding |
|---|---|---|---|
| Task1 → Task3/5 | `ReferencesEnclosingScope` 2 call sites in MongoExpressionTranslator.cs | Task1 patches both sites to `out _` (build-green placeholder) | Task3 Step7 rewrites Count(pred) site to real branch; Task5 Step4 rewrites quantifier site to real branch. Each only touches its own site — no overlap. Clean sequencing. |
| Task2 → Task3 | `MongoOuterFieldExpression` | Task2 produces node+rendering; Task3 constructs it at TranslateComparison/bare-bool/TranslateOperand sites | Task2 must land before Task3 — plan order already enforces this. Clean. |
| Task4 → Task5 | `MongoQuantifierExpression`, `MongoQuantifierKind` (widened internal in Task4) | Task4 produces; Task5 constructs at quantifier arm | Task4 before Task5 in plan order. Clean. |
| Task3 vs Task6 | `TryResolveOwnedFieldPath` signature | Task3 Step4 adds `out bool isOuter` (mechanical, always false); Task6 Step3 relativizes the body (same signature) | Task6's replace-block reproduces the Task3-mechanical signature verbatim then changes the body — applies cleanly against Task3's output. No collision. |
| Task3/5 vs Task6 | `TryResolveOwnedCollectionPath` call sites (quantifier arm, Count(pred) arm) | Task5/Task3 insert correlation branches *before* the existing `TryResolveOwnedCollectionPath(...)` guard line; Task6 Step6b/6c replace that guard line itself | Verified: Task3's replacement region starts AFTER the guard line (inside the `if` block); Task5's replacement region starts AFTER the guard line too. Guard line is untouched by 3 and 5, so Task6's literal-match replacement in Step 6b/6c applies cleanly post-Task5. No conflict. |
| Task7 | NativeSlotPopulator.cs, NativeProjectionBinder.cs, NativeCardinalityBinder.cs, NativeVectorSearchBinder.cs | New files, untouched by Tasks 1-6 | No conflict. |
| Task8 | NativeOwnedCollectionAllTests.cs, NativeOwnedCollectionFilteredCountTests.cs, NativeOwnedCollectionPredicateTests.cs, Query/AGENTS.md | First touch of these test files; NativeOwnedCollectionCorrelatedTests.cs extended serially by Tasks 3/5/6/7 | Serial appends to the same new test file across tasks — each task assumes prior task's additions are present, matching plan's own task order. Consistent. |
| Global Constraints vs task bodies | "decline outright when correlation doesn't match SelfParam" | Enforced via `if (SelfParam is null || !ReferenceEquals(found, SelfParam)) return null;` at both Task3/Task5 sites | Consistent with constraint. |
| Task3 Step5 prose | Says "seven sites", lists 5a-5i (9 labeled items, 2 share one heading = effectively 8 distinct call sites) | N/A | Cosmetic miscount in prose only, not in code diffs. **Ruling: non-blocking, no ledger action needed beyond this note.** |

**Ruling: scan clean.** No blocking conflicts found. Proceeding to Task 1.

## Task log

### Task 1

Task 1: review — Approved overall, 2 Important findings (both labeled plan-mandated by reviewer: the brief's own verbatim replacement text caused them).

Ruling: finding 2 ("SelfParam doc describes NativeSlotPopulator wiring that doesn't exist yet") — PARKED, no fix. The doc text is forward-referencing Task 7, which lands later in this same plan and will make the sentence literally true. This is expected sequencing in a multi-task plan, not a defect. Cost if wrong: negligible — a reader mid-sequence sees a doc comment describing near-future behavior, corrected within a few commits.

Ruling: finding 1 (duplicated/malformed XML doc block on ReferencesEnclosingScope — two <summary> blocks feeding one method, no separator) is real and cheap to fix — dispatching fix round 1 to squash into one doc block. Cost if wrong (left as-is): confusing generated docs/IntelliSense, no functional impact.

Task 1: fix round 1/5 (dispatched — merge duplicate XML doc block on ReferencesEnclosingScope)
Task 1: fix round 1/5 (1 addressed, 0 open; commits c5b3ebc..c27fe9e)
Task 1: complete (commits 1ed78b1..c27fe9e, 1 parked)

### Task 2
Task 2: minor (deferred): MongoOuterFieldExpression.cs missing the codebase's usual explanatory comment on `public new IProperty Property` (cosmetic, no functional impact)
Task 2: complete (commits c27fe9e..4dd7491, review clean)

### Task 3
Task 3 implementer reported DONE_WITH_CONCERNS with two deviations from the brief's literal text, both root-caused and fixed:
1. NativeSlotPopulator never wired SelfParam (Task 1's doc comment promised it, no task's brief actually did it) — implementer added it for Where/OrderBy/ThenBy in Task 3's commit, since without it the whole feature is inert and the brief's own required test cannot pass.
2. The brief's exact `innerPrefix: ""` snippet (Task 3 Step 7, and identically in Task 5 Step 4's planned quantifier snippet) produces invalid MQL — MongoDB rejects a leading-dot field path from an empty-string prefix concatenation. Implementer widened the two-scope constructor's `innerPrefix` to `string?` and passes `null` instead of `""`.

Ruling (finding conflicts with plan text — plan is wrong, spec is silent on this exact point but the two-scope translator's whole purpose requires correct MQL, so correctness wins): ACCEPT both fixes as committed. Cost if wrong: none identified — both are verified against the full test suite (FunctionalTests/UnitTests/SpecificationTests all green except the one task-boundary-expected failure).

Ruling: carry forward to Task 5 dispatch — the quantifier arm's two-scope constructor call must ALSO pass `innerPrefix: null`, not `""` (the plan's Step 4 snippet as written would reintroduce the identical invalid-MQL bug). Task 5's dispatch prompt must explicitly override the plan's literal `innerPrefix: ""` with `null`.

Ruling: carry forward to Task 7 dispatch — NativeSlotPopulator's Where/OrderBy/ThenBy SelfParam wiring is ALREADY DONE (landed in Task 3's commit 96a220f). Task 7's dispatch must exclude that portion of the brief (it will find the hunks already present) and focus only on NativeProjectionBinder/NativeCardinalityBinder/NativeVectorSearchBinder, then verify (not re-add) the NativeSlotPopulator wiring.

Task 3: fix-in-flight assessment pending task review (dispatching now, with focus on verifying both fixes' correctness and the 9 updated pre-existing test assertions for masked regressions).

Task 3: review — Approved overall, 2 Important findings, 4 Minor (deferred below).
Task 3: minor (deferred): SelfParam doc comment factually wrong re: NativeSlotPopulator reuse (MongoExpressionTranslator.cs:100-106)
Task 3: minor (deferred): MongoFieldPrefixRewriter has no MongoOuterFieldExpression arm (unreachable today, but a latent hard-fail trap per that switch's own contract comment)
Task 3: minor (deferred): bare-outer-bool truthiness hazard for a value-converted bool (pre-existing gap, widened not introduced)
Task 3: minor (deferred): knowingly-red test + unused using statement in NativeOwnedCollectionCorrelatedTests.cs (brief-mandated red until Task 5; cleanup the stray using)

Dispatching fix round 1 for the 2 Important findings:
1. FreeParameterVisitor/ReferencesEnclosingScope must decline when the body has MORE THAN ONE distinct free parameter, not just report the first — closes a latent (not currently reachable, but spec-violating) silent-mis-scope gap at the Count(pred) two-scope branch.
2. Add a covering test proving the SelectMany correlated outer-vs-simple-value shape (query-dialect -> $expr routing change) still returns correct rows — not a defect, but an asserted-untested claim in the report.
Task 3: fix round 1/5 (2 addressed, 0 open; commits 96a220f..7b86cd0)
Task 3: complete (commits 4dd7491..7b86cd0, 4 parked)

### Task 4
Task 4: minor (deferred): test helper NameProperty() misnamed/redundant wrapper around existing GetProperty<T> (MongoAggregationExpressionRendererTests.cs:561)
Task 4: minor (deferred): helper-region comment banner style inconsistent with file convention (MongoAggregationExpressionRendererTests.cs:559)
Task 4: complete (commits 7b86cd0..9651bc7, review clean)

### Task 5
Task 5 implementer applied the innerPrefix:null correction as instructed. Found a second stale-decline test beyond the one the brief named: NativeOwnedCollectionPredicateTests.Correlated_element_predicate_declines_and_falls_back_to_correct_rows — verified (via throwaway reverted patch) it's a stale assertion not a wrong-rows bug. This matches Task 8 Step 4's own plan (already directs checking this exact file) — no ruling needed, just confirms Task 8's step is real and necessary, not speculative.

Task 5: review — Approved overall, 1 Important finding (stale doc comment now contradicts the code), 4 Minor (deferred below).
Task 5: minor (deferred): stale remark in Members.cs:307-312 attributes safety to the wrong mechanism (conclusion still true, reasoning outdated)
Task 5: minor (deferred): quantifier site missing the innerPrefix:null explanatory comment the Count(pred) site has
Task 5: minor (deferred): asymmetric decline condition phrasing vs Count(pred) site (behaviorally identical)
Task 5: minor (deferred): test coverage gap — missing/null-array $ifNull path, negated correlated Any, mixed predicate w/ Rank not covered by NativeOnly test
Task 5: confirmed — second stale-decline test (NativeOwnedCollectionPredicateTests) correctly deferred to Task 8, not fixed here (would be scope creep). Carrying forward: Task 8 dispatch must explicitly include this file even though Task 8's own brief only names NativeOwnedCollectionFilteredCountTests/NativeOwnedCollectionAllTests by number — its Step 4 already directs checking NativeOwnedCollectionPredicateTests.cs, so this is confirmed necessary, not speculative.
Dispatching fix round 1 for the 1 Important finding (stale doc comment, MongoExpressionTranslator.MethodCalls.cs:217-219).
Task 5: fix round 1/5 (1 addressed, 0 open; commits b3761e9..c4601c1)
Task 5: complete (commits 9651bc7..c4601c1, 4 parked)

### Task 6
Task 6 implementer independently re-confirmed the same second stale test (NativeOwnedCollectionPredicateTests.Correlated_element_predicate_declines_and_falls_back_to_correct_rows) flagged by Task 5's implementer — verified pre-existing (root cause: Task 5's Any/All native path), not a Task 6 regression. Two independent confirmations now on record; Task 8 dispatch will explicitly require fixing this file, not just the two named in its own brief text.
Task 6: minor (deferred): decline guard is stricter than stated (also declines an inner-rooted dotted chain in two-scope mode, fail-closed, plan-mandated, not a regression)
Task 6: minor (deferred): doc-comment edits beyond brief's literal diff blocks (verified comment-only, accurate)
Task 6: complete (commits c4601c1..0940a67, review clean)

### Task 7
Task 7 implementer correctly stopped (NEEDS_CONTEXT) rather than guess: 2 of the brief's own 3 Step-1 tests fail for reasons unrelated to SelfParam wiring (Steps 4-6 themselves verified correct and complete):
1. Correlated_Any_inside_a_projection_leaf_goes_native — TranslateOperand/TryTranslateLeaf never route a bare Any/All quantifier into VALUE position at all (only Contains/Not were widened this way, under EF-413, narrowly). Not correlation-specific; an uncorrelated bare Any projection leaf would fail identically. This is a materially larger, orthogonal feature never in EF-421's design doc.
2. Correlated_All_inside_a_scalar_aggregate_predicate_goes_native — MongoExpressionNegator has no case for MongoQuantifierExpression, so negating a root All(pred) whose pred contains a nested correlated Any (now natively MongoQuantifierExpression, per Task 5) fails and the whole scalar query throws under NativeOnly.

Ruling (finding conflicts with plan text — the plan's Task 7 brief wrongly assumed "the correlation-matching logic itself is already built" covers both test shapes; it does not):
- Finding 1: DESCOPE the test. Rewrite Correlated_Any_inside_a_projection_leaf_goes_native to project Count(pred) > 0 instead of a bare Any (Count(pred)-as-value already works via TranslateOperand's existing arm, proven by the sort-key test in the same file) — this still proves SelfParam reaches NativeProjectionBinder, without requiring new production capability outside this plan's scope. Bare-quantifier-as-projection-value remains an accepted, documented gap (not fixed by EF-421) — note this in Query/AGENTS.md's Task 8 doc update.
- Finding 2: FIX for real. Add a MongoQuantifierExpression case to MongoExpressionNegator: exact De Morgan complement (flip Any<->All Kind, recursively negate ElementPredicate via the existing TryNegate) — mirrors the existing MongoElemMatchExpression negation pattern already in that file. This is a foreseeable, in-scope consequence of Tasks 4/5's new node type, required by the plan's own Task 7 test, and small/well-defined enough to fix carefully now rather than deferring.
Cost if wrong: finding 1's descope leaves a real (but pre-existing, non-regressive) gap undocumented if I forget the AGENTS.md note; finding 2's negator fix is correctness-critical (silent wrong-data risk per this codebase's own negator invariant) and will get an opus-level review given the stakes.

Resuming the same Task 7 implementer with these two rulings.

Task 7 resolution grew beyond the two rulings' anticipated scope: the negator fix needed 3 coordinated pieces (outer-gate bypass for MongoQuantifierExpression, a second outer-field-aware comparison case in TryNegateCore, a Not-over-quantifier arm in MongoExpressionTranslator), discovered empirically. The Count(pred)-projection descope also needed a second correction (Count(pred) > 0 isn't an admitted leaf shape either — projected raw Count(pred), computed >0 client-side). Full-suite run then surfaced 2 MORE pre-existing tests pinning old decline behavior for correlated Count(pred) projection leaves, re-baselined following this file's own EF-365/EF-413 precedent.
Given the correctness stakes (negator exactness) and the scope growth, dispatching an opus-level review with explicit line-by-line scrutiny of all 3 negator pieces and the 2 newly re-baselined tests (not just the ones already covered by Task 3's review pattern).

Task 7: review — Approved, 2 Important findings (both about the negator's structural robustness, not live correctness), 4 Minor (deferred below). Reviewer actively hunted for a negation gap across all 3 coordinated pieces (elemMatch leak, truthiness hazard, empty/missing-array semantics, involution, decline propagation) and found none — Critical: none.
Task 7: minor (deferred): Not-over-quantifier arm narrows pre-existing coverage on decline (harmless graceful fallback, no test covers it, one-line fix suggested but not required)
Task 7: minor (deferred): MongoQuantifierExpression's own remarks stale re: "sidesteps the negator entirely"
Task 7: minor (deferred): ContainsShaperReference guard lost both its pinning tests (comment claims coverage without naming a covering test)
Task 7: minor (deferred): both re-baselined tests assert only zero counts (non-vacuous but nonzero-count arithmetic untested there specifically; covered elsewhere in the new correlated-tests file)

Dispatching fix round 1 for the 2 Important findings:
1. MongoExpressionNegator's class-level doc comment is now false (claims output is always query-dialect-renderable and never routes to $expr catch-all) — update to state the real invariant per the reviewer's suggested wording.
2. comparison3's safety is emergent (relies on IsQueryDialectRenderable staying exhaustive elsewhere) rather than structurally enforced — thread a bool context flag through TryNegateCore, set only by the quantifier's ElementPredicate recursion, gate comparison3 on it.
Task 7: fix round 1/5 (2 addressed, 0 open; commits b057ca1..1078377). Re-review additionally validated the guard removes no live behavior (comparison3 was genuinely unreachable before, structurally unreachable now) and confirmed MongoFilteredSizeExpression's sealed-sibling status was the exact latent hazard the fix closes.
Task 7: minor (deferred): new unit test's in-code comment overstates what it actually pins (harmless, test itself is fine)
Task 7: minor (deferred, out-of-scope observation): quantifier bypass's single-call-site safety argument is prose-enforced, not structural — inverse asymmetry of finding 2, not required to fix
Task 7: complete (commits 0940a67..1078377, 6 parked)
Task 8: minor (deferred): AllTests helper (AssertNativeOnlyMatches) has no driver-LINQ parity leg unlike the Any sibling's AssertNativeAndParity — pre-existing, justified choice (full-matrix seed's missing/null rows abort the driver's own translation), not a gap introduced by this task
Task 8: complete (commits 1078377..40bf94a, 1 parked)

## All 8 tasks complete. Proceeding to final whole-branch review.

## Final whole-branch review

Final reviewer (opus): "Ready to merge: With fixes". Findings:
- C1 (Critical): MongoOuterFieldExpression bypasses all 3 bare-field truthiness guards (CanRenderLogicalOperand, CanRender's Not arm, RenderUnary's throw) — a value-converted/non-default-represented bare outer bool in a correlated quantifier silently answers the wrong boolean. NEWLY REACHABLE (this shape declined before EF-421). This overturns Task 3's own "pre-existing gap, widened not introduced" parking ruling — the reviewer explicitly disagrees with that adjudication and is right: pre-EF-421 the shape declined outright.
- I1 (Important): MongoOuterFieldExpression construction in TranslateComparison applies unconditionally to EVERY two-scope translator, including the PRE-EXISTING NativeSelectManyBinder path — un-type-brackets a relational (</<=/>/>=) comparison on a nullable outer property there, since $expr doesn't type-bracket null/missing the way the query dialect does. Possible silent wrong-rows regression on ALREADY-SHIPPED SelectMany behavior, not just new EF-421 surface.
- I2 (Important): MongoQuantifierExpression.cs's own doc still claims "sidesteps MongoExpressionNegator entirely" — false since Task 7. Same stale-comment class the negator's own class doc was already fixed for in Task 7's fix round, but this sibling file was missed.
- Fix-now triage items: M1 (SelfParam doc factually wrong re: NativeSlotPopulator reuse — should be constructor-set, not settable-because-reused), M5 (no NativeOnly test for a correlated quantifier over a genuinely MISSING/null array, only empty-array is covered).
- Deferred (leave as-is, no action): M2 (MongoQuantifierKind namespace layering), M3 (MongoFieldPrefixRewriter arm, moot if I1 scoped correctly), M4 (missing comment cosmetic), plus all Task 4/5/6/7/8 minors the reviewer explicitly said to leave.

Plan-quality note from reviewer (recorded for completeness, not a code finding): spec §3 under-scoped MongoOuterFieldExpression to "all two-scope translators" without analyzing the SelectMany-path type-bracketing consequence (root cause of I1); spec's "three sites that must account for a new node" enumeration missed the three bare-field truthiness sites (root cause of C1). Both are "enumerated node-dispatch sites, missed value-semantics sites" — noted as a lesson for future specs in this codebase, no action needed on the spec doc itself now that the code is shipping correctly.

Dispatching ONE fix wave for: C1, I1, I2, M1, M5.

## Final fix-wave re-review — residual finding, breaker reached

Re-review verdict: Findings 1 (named repro), 3, 4, 5 addressed; the implementer-discovered 5th gap (bare-field-in-quantifier truthiness) verified real and genuinely closed. Finding 2 only HALF addressed:
- Fixed correctly: TranslateComparison's two branches (MongoExpressionTranslator.cs ~879, ~912) now gate MongoOuterFieldExpression construction on `_innerPrefix is null`, correctly restoring byte-identical SelectMany emission for FIELD-TO-VALUE/FIELD-TO-FIELD comparisons.
- NOT fixed: the bare-boolean-member arm (MongoExpressionTranslator.cs:789-791) was NOT given the same `_innerPrefix is null` gate — reopens the identical truthiness hazard (Finding 1's hazard class) on the PRE-EXISTING, already-shipped SelectMany path: `SelectMany(o => o.Items.Where(i => o.ConvertedFlag))` with a value-converted bool now silently answers wrong (both "True"/"False" string-converted values are truthy under $expr raw-truthiness) instead of correctly (pre-plan: MongoFieldExpression rendered {Flag: "True"} via the property's own serializer).
- Two adjacent Important divergences at the same root cause: (a) MongoExpressionTranslator.cs:587's nullable-outer-bool-under-Not conservative decline no longer catches MongoOuterFieldExpression; (b) MongoQueryLanguageRenderer.cs:180's Not-over-field routing doesn't consult the already-fixed CanRender Not-arm for an outer-field operand the way it should.
- Suggested fix (same pattern already applied twice in this exact commit): `return boolIsOuter && _innerPrefix is null ? new MongoOuterFieldExpression(...) : new MongoFieldExpression(...);` at the bare-bool arm, plus widening :587's nullable decline to cover MongoOuterFieldExpression too.
- Also flagged (out-of-scope, non-blocking): AGENTS.md and AllFieldsDefaultSerialized's own remarks still say "four guards" — now five (CheckBooleanRootSerialization added). Should be updated whenever the residual is fixed.

Per subagent-driven-development's explicit design: no second automated fix wave. Surfacing this to the user now rather than looping again.

Ruling: user explicitly authorized a second fix wave for the residual finding (overriding the skill's default "no second fix wave" — this is the human decision point the skill designed for, and the human chose to proceed). Dispatching fix wave 2 + scoped re-review.

Fix wave 2 (residual finding, user-authorized): all 3 issues addressed, re-review clean, no new breakage. Traced correctly: SelectMany's non-null-innerPrefix case now gets MongoFieldExpression (restored pre-plan behavior), Count(pred)/quantifier's null-innerPrefix case unaffected (still gets MongoOuterFieldExpression). Doc-count corrected to SIX guard sites (enumerated and verified), not five as originally suggested — implementer's own discovery during the fix, confirmed accurate by re-reviewer.
Minor (deferred, out-of-scope, non-blocking): a 4th unguarded boolIsOuter construction site in TryTranslateValue — verified benign today (value-context nodes render identically whether MongoFieldExpression or MongoOuterFieldExpression outside an element-variable scope; no current path puts a SelectMany two-scope translator's value inside an element scope).
Minor (deferred): one exception message slightly narrower in reality than stated (cosmetic).

## FINAL WHOLE-BRANCH REVIEW: CLEAN. All 8 tasks + both fix waves complete and reviewed. Ready to finish the branch.
