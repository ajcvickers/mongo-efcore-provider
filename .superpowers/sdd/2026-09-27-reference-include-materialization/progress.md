# SDD ledger — plan: docs/superpowers/plans/2026-09-27-reference-include-materialization.md

Spec: docs/superpowers/specs/2026-09-25-reference-include-materialization-design.md
Execution: subagent-driven-development. Working in place on branch EF-322-Native-LINQ-rebased
(user declined a separate worktree — this branch is already dedicated to EF-322 and has been
committed to directly all session).

## Pre-flight conflict scan

| Pair | Shares | A produces / B consumes | Finding |
|---|---|---|---|
| Task 1 ↔ Task 2 | `MongoQueryableMethodTranslatingExpressionVisitor.cs`, `NativeJoinScopeProjectionBinder.cs` (Task 1 touches both temporarily; Task 2 permanently edits the latter) | Task 1's edits are explicitly reverted (Step 8) before its only commit (findings doc only) | Clean — no overlapping working-tree state once Task 1 finishes; Task 2 starts from a tree identical to pre-Task-1. |
| Task 1 ↔ Task 3 | Findings doc | Task 1 produces `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md`; Task 3 consumes it to pick branch 3A/3B | Clean — plan order (1 then 2 then 3) guarantees the doc exists before Task 3 dispatch. |
| Task 2 ↔ Task 3 | `NativeJoinScopeProjectionBinder.cs` (Task 2) vs. `MongoProjectionBindingExpressionVisitor.cs` + `MongoQueryableMethodTranslatingExpressionVisitor.cs` (Task 3, both branches) | Different files (3A) / different region of the shared visitor file, `TryConfirmReferenceIncludeChain` vs. `BindResultMember`/staging unwrap (3B) | Clean — no line-level overlap expected; sequential dispatch order enforces linear history either way. |
| Task 3 (3A) self-consistency | Plan text vs. own tests | Step 4 promotes an existing decline-tolerant test to a strict pass; Step 5 mandates 4-sibling-class spec baseline regen | Consistent — the plan's own Global Constraint ("acceptance criteria ... across all 4 Northwind sibling classes") matches Step 5 exactly. |
| Task 3 (3B) self-consistency | Plan text | Steps 1-3 defer the exact fix to what Step 1-2's trace finds, per design doc's own mandate that this cannot be pre-specified | Consistent with spec's Task 1 investigation-first mandate — not a placeholder, an intentional investigation-gated branch. |
| Task 4 | No new files | Runs full suite + conditional `/test-all` + doc cross-reference | Clean — purely verification/cleanup, no code. |

Scan is clean. No rulings needed before dispatching Task 1.

## Task log

Task 1: fix round 1/5 (1 addressed, 0 open — mixed-chain-arm claim now backed by quoted evidence; commits 285a9011..84d90a54)
Task 1: complete (commits 0b0a7a6d..84d90a54, review clean after 1 fix round)

Task 2: implementer found Shape 2 reachable and silently wrong pre-fix; applied the symmetric guard to
NativeJoinScopeProjectionBinder.cs. Full-suite run surfaced a pre-existing UNIT test
(NativeJoinScopeProjectionBinderTests.Binds_a_duplicated_whole_inner_entity_leaf_that_is_also_include_wrapped)
that pinned the OLD buggy behavior (asserted this shape stages natively) — not anticipated by the plan's file
list (plan named only NativeJoinScopeProjectionBinder.cs + NativeJoinTests.cs).
Ruling: update that unit test's 3 assertions to expect a clean decline (Empty Projection/Lookups,
NativeRoute.Fallback), mirroring the file's own Declines_when_a_sibling_leaf_reached_through_the_whole_entity_reference_is_untranslatable
pattern, and commit it together with the guard fix and the new functional test in one commit — this is a
correction of a test's expected outcome to match now-correct behavior, not new scope. Cost if wrong: a stale
unit test would need a follow-up fix; low risk, easily caught by the task reviewer.

Task 2: minor (deferred): renamed unit test's comment overstates parallel to Declines_a_chain_scalar_leaf_that_spans_two_scopes
(different mechanism — join+Include registration vs. multi-join flattening — though the asserted count of 2 is correct).
Task 2: complete (commits 84d90a54..91a8528a, review clean, Shape 2 confirmed reachable/silently-wrong pre-fix, guard applied)

Task 3: dispatched (Branch 3A, per Task 1's findings doc recommendation). Carried forward the findings doc's
caveat that the two target spec tests may be two SEPARATE bugs sharing one recognizer arm (test 1: hard
exception; test 2 mixed left-join: silently-empty data, suspected $lookup field-mismatch, unrelated to
BindResultMember). Instructed implementer to investigate test 2 independently if 3A's fix doesn't also fix
it, and to fix it as a separate commit if narrowly scoped, or escalate if not. Model: opus (architecture-
level change to shared shaper/lookup-registration machinery). BASE=91a8528a.

Task 3: confirmed genuinely two separate bugs. Commit 382dccbb (Branch 3A, reusing the join's own existing
$lookup rather than registering a new one — cheaper than the brief's suggested mechanism, verified correct
by reviewer) fixes test 1. Commit 89c3446e (nests a collection Include's own $lookup inside its declaring
join's pipeline instead of emitting at the document root) fixes test 2 — has its own in-memory-oracle
regression test. Review: Important finding — the new ThenInclude test compares DriverLinq against itself
(tautological — this shape falls back to DriverLinq in Native mode) plus change-tracker fixup masks a broken
collection-nav tail; doesn't actually prove "correct data" as claimed. Fix round 1 dispatched: rebuild the
ThenInclude test against a real in-memory seed oracle + AsNoTracking.

Task 3: minor (deferred, for final review): (1) IncludedTargetAlsoProjected test has the same fixup-masking
risk — AsNoTracking would help; (2) BindResultMember header comment line overlong/not rewrapped; (3) no test
for two distinct reference Includes on one leaf; (4) no test for an optional/unmatched reference Include row
through the new arm (LeftJoin preserveNull case); (5) let/$expr $lookup form for inner-side collection
Includes may deserve a follow-up index-use check (correctness confirmed fine, performance unverified).
Out-of-scope, pre-existing, needs its own ticket: Orders.LeftJoin(Owners.Include(w => w.Orders)) with a
dangling FK throws "Document element is missing for required non-nullable property 'Id'" in Native AND
DriverLinq — confirmed not caused/worsened by this diff (falls through the unchanged reference-join
intermediate/dotted-`as` path, untouched by either commit). Loud failure, not silent, so acceptable to leave
out of scope per this plan's own "loud beats silent" bar — but should be ticketed separately.

Task 3: fix round 1/5 (1 addressed, 0 open — ThenInclude test now uses a real in-memory seed oracle for both
continuations, AsNoTracking where possible / a filtered-Where trick where EF Core rejects no-tracking due to
an Include cycle, mutation-verified; commits 89c3446e..1f7ac9a6)
Task 3: minor (deferred): the fix round's comment-rewrap attempt on BindResultMember's header (~line 3388)
produced a line still overlong (173 chars) — cosmetic only, needs tidying in a future pass.
Task 3: complete (commits 91a8528a..1f7ac9a6, review clean after 1 fix round; both target spec tests pass
with correct data across all 4 sibling classes, default + NativeOnly)

Task 4: full regression sweep across all 3 EF versions (guard-check found version-conditional files touched).
EF10: 0 failures (1285 unit, 4599 spec, 3284 functional). EF8/EF9: 0 new failures, only the known 10
pre-existing NorthwindMiscellaneousQueryMongoTest failures (unrelated, present before this plan's work).
Added a status note to the design doc pointing at this plan + the findings doc (this also committed the
design doc for the first time — it was untracked all session, hence the larger-than-expected diff size).
Review clean, no findings.
Task 4: complete (commits 1f7ac9a6..69ec26b0, review clean)

All 4 tasks complete. Proceeding to final whole-branch review.

Final whole-branch review (0b0a7a6d..69ec26b0, opus, read-only, spot-verified test claims by re-running the
two target spec tests on EF9/EF10): Ready to merge = Yes. No Critical/Important findings. Verified the
dangling-FK/reference-join phantom-subdocument bug is genuinely pre-existing and unaffected by this plan
(structural trace, not a live re-run at base). Minor findings triaged:
- Fixing now (trivial, reviewer-recommended before merge): overlong comment line at
  MongoQueryableMethodTranslatingExpressionVisitor.cs:3388; stale test name
  Reference_included_whole_entity_leaf_goes_native_and_matches_driver_linq (now compares against an oracle,
  not driver-linq); stale Shape-4 prose in the design doc contradicting the findings doc's own conclusion.
- Parked for a follow-up ticket (new test coverage / out-of-scope, not defects): optional/unmatched
  reference-Include-through-the-new-arm test; two-reference-Includes-on-one-leaf test; owned/embedded
  reference on a join-scope whole-entity leaf test; nested ThenInclude under an inner-side collection Include
  test; let/$expr $lookup performance follow-up; the pre-existing dangling-FK dotted-`as` phantom-subdocument
  bug (own ticket, reviewer notes 89c3446e's nesting technique would likely fix it too); EF8/EF9 NativeOnly
  guard granularity on the new collection-Include-nesting test (currently blanket #if !EF8 && !EF9, could be
  narrowed to just the GroupJoin-shim-affected shape).
Dispatched ONE fix wave for the three trivial items.

Ruling: scope the final review to THIS PLAN's own work (0b0a7a6d..69ec26b0), not `git merge-base main HEAD`
(1d7ec56a) — that merge-base predates 21 prior commits on this branch from earlier, already-completed and
already-reviewed sessions (per docs/superpowers/HANDOFF.md: "final whole-branch review clean" for that
earlier work). Re-reviewing already-shipped, already-reviewed work is out of scope for this plan's final
gate and would waste a large review pass on unrelated code. Cost if wrong: an already-reviewed commit outside
this plan's scope goes unexamined by this specific review pass — low risk, since it already passed its own
review cycle in a prior session.
