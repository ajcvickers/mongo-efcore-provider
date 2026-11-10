# SDD ledger — plan: docs/superpowers/plans/2026-08-26-native-join-translation.md

## Pre-flight conflict scan (2026-08-26)

Worktree isolation offered, declined by user — executing directly in
/Users/arthur.vickers/code/provider2 on branch EF-322-Native-LINQ-rebased.
Plan doc and spec doc already committed (f92725c spec, c62e64e plan).

Task-pair file/interface overlap table:

| Pair | Shared file/interface | Produces (earlier) | Consumes (later) | Finding |
|---|---|---|---|---|
| T1 -> T3/T6 | MongoTransparentScopeResolver | T1: TryResolveScopeDepth/ScopeRerootingVisitor | T3 "Consumes" line cites it | T6 Step 4 explicitly declines to force-fit this resolver onto Join's flat two-sibling shape and specifies a separate direct helper instead. Plan's own self-review already flags this as deliberate design refinement, not a defect. No ruling needed — noted, proceed as written. |
| T2 -> T3/T5/T6 | MongoSelectDefinition.JoinScope | T2: property + HasTerminalOperator | T3 sets it, T5/T6 read it | Consistent, no conflict. |
| T3 -> T4 | MongoJoinBinder.TryBindJoin signature | T3 defines | T4 calls | Signature matches exactly across both tasks. Consistent. |
| T3 -> T6 | MongoJoinBinder.cs body | T3 implements first cut | T6 REVISES the same method body (member-classification loop -> inline translate-and-register) | By design (incremental). GAP: T6 Step 5 only re-runs NativeJoinTests + functional Query suite; it does NOT list re-running T3's own unit tests (MongoJoinBinderTests) despite rewriting the method those tests cover. RULING below. |
| T5 Step 2 | AppendLookupStages new arm | — | — | Step 2 presents TWO candidate code snippets in sequence (first JoinScope-gated draft, then a broadened IsJoinQuery-gated "Fix:" version) as a worked-through narrative. Only the SECOND (final) snippet is meant to be implemented. RULING below to disambiguate for the implementer. |
| T2 Step1 vs Step6 | test file name | Files section says `MongoSelectDefinitionTests.cs` (add cases or find sibling); Step 1 code header also says that; Step 6 git add references `MongoSelectDefinitionJoinScopeTests.cs` | — | Minor naming inconsistency. RULING below: use the Step-6/class-matching name. |
| T7/T8 | NativeJoinTests.cs | T4 creates, T6/T7/T8 append | — | All additive appends to the same file, sequential tasks, no structural conflict. |
| EF-411 (concurrent agent) | MongoSelectDefinition.cs | T2 adds JoinScope + extends HasTerminalOperator | — | T2 already carries its own coordination note (rebase/diff-check before committing). Re-verified as still the only shared-file risk in this plan; nothing else in T1/T3/T4/T5 touches files EF-411 is expected to touch (NativeGroupByBinder/NativeCardinalityBinder/set-op routing). |

Every task's own internal text-vs-text consistency was also checked (tests specified vs. code specified, files created vs. later touched) — no other contradictions found beyond the three above.

**Rulings (pre-flight):**

- Ruling: Task 6's dispatch will explicitly instruct the implementer to also re-run `MongoJoinBinderTests` (Task 3's unit tests) after revising `MongoJoinBinder.cs`, in addition to the plan's own listed regression commands — cost if wrong: a silent regression in Task 3's classification unit tests that the plan's own step list would otherwise miss.
- Ruling: Task 5's dispatch will point the implementer at the SECOND (broadened, `query.IsJoinQuery`-gated) code snippet in Step 2 as the one to actually implement, not the first draft — cost if wrong: implementer ships the incomplete first draft, which the plan's own text already shows fails for the flattened-only smoke test.
- Ruling: Task 2's new unit test file is named `MongoSelectDefinitionJoinScopeTests.cs` (new file, matching the test class name and Task 6's own `git add` line), not appended into a `MongoSelectDefinitionTests.cs` — cost if wrong: trivial, a rename before commit.

Proceeding to dispatch Task 1.

## Task 1: Generalize NativeSelectManyBinder's scope-rerooting walker

Task 1: fix round 1/5 (3 addressed, 0 open; commits ded5240..59a4892)
Task 1: complete (commits c62e64e..59a4892, review clean)

## Task 2: Add join-scope marker to MongoSelectDefinition

Task 2: minor (deferred): MongoJoinScope.cs XML doc <see cref="MongoTransparentScopeResolver"/> is unqualified across namespaces (Query.Expressions -> Query.NativeTranslation) — cosmetic, no WarningsAsErrors in this repo, not a build blocker.
Task 2: complete (commits 59a4892..3f21dd9, review clean)

## Task 3: MongoJoinBinder — eligibility, native $lookup registration, scope classification

Ruling: JoinInfo.Lookup (pre-existing, EF-372/EF-377 multi-hop-flattening property) is not set by TryBindJoin — matches the brief exactly, and this plan's chunk explicitly excludes chained/multi-hop joins, so leaving it unset is correct, not a gap. Carried as a note into Task 4's dispatch to double-check nothing downstream expects it. Cost if wrong: Task 4 wiring would surface a null-Lookup issue immediately, cheap to catch.
Task 3: fix round 1/5 (1 addressed, 0 open; commits dd80159..386bbad)
Task 3: complete (commits 3f21dd9..386bbad, review clean)

## Task 4: Wire MongoJoinBinder into TranslateJoinCore

Ruling: the plan's stated invocation-order assumption (TryConfirmReferenceInclude declines Include-sugar BEFORE TryBindJoin ever runs) was wrong — TryConfirmReferenceInclude actually runs LATER, from a subsequent TranslateSelect, not from TranslateJoinCore. This caused 12 real regressions (Include-lowering's own TransparentIdentifier-shaped joins were misclassified as genuine two-sided joins). Implementer fixed by adding a guard in MongoJoinBinder.cs (Task 3's file) declining on the TransparentIdentifier type-name prefix, modeled on the existing IsTransparentIdentifierSelector/IsTransparentIdentifierMemberAccessSelector convention already used for this exact distinction elsewhere in the codebase. Reviewer independently verified the regression claim, the fix's placement (before any state mutation), and that it's a faithful instance of established precedent, not a shortcut violation. Cost if wrong: would have reintroduced silent Include-shape corruption; reviewer's independent repro make this low-risk.
Task 4: minor (deferred): MongoJoinBinder.cs comment says precedent is "elsewhere in this file" but it's in MongoQueryableMethodTranslatingExpressionVisitor.cs — misleading phrasing, one-word fix.
Task 4: minor (deferred): NativeJoinTests.cs has an unused non-logging CreateContext helper alongside CreateContextWithLogging — harmless dead code.
Task 4: complete (commits 386bbad..7c1a5d2, review clean)

## Task 5: Lower a bound join scope into $lookup/$unwind stages

Task 5: fix round 1/5 (2 addressed, 0 open — plus a NEW dead-code bug surfaced and fixed during the fix round: the new arm as originally placed (right after IsStreamableReference, per the brief's literal instruction) was unreachable, because IsStreamableReference's guard strictly subsumes it for every non-collection ForceUnwind lookup. Fixed by moving the new arm to be first in the dispatch chain. Independently verified by re-reviewer via LookupExpression.IsReference/IsStreamableReference source, not just the report's claim; commits d922879..4acb9ba)
Task 5: minor (deferred, for final review): reordering the new arm to be first means ordinary single-level reference-Include lookups now ALSO route through the new join arm (not just genuine Joins), since query.IsJoinQuery is already true for Include by the time TryConfirmReferenceInclude registers its lookup (EF's nav-expansion already lowered the Include into an internal Join). This is correctness-preserving TODAY only because JoinInfo.IsLeftOuter (operator-derived) and the Include-confirm site's PreserveNullAndEmptyArrays (IsRequired-derived) are set from the same underlying signal by construction — but it's a latent coupling: a future change that let these two diverge for this shape would silently flip which one wins. Not a current defect (full functional Query suite, including reference-Include coverage, passes); flagged for the final whole-branch review to decide whether a regression test or an explicit comment pinning this coupling is warranted.
Task 5: complete (commits 7c1a5d2..4acb9ba, review clean after 1 fix round)

## Task 6: BLOCKED — plan premise invalidated (NEEDS_CONTEXT)

Task 6 implementer (opus) reports NEEDS_CONTEXT with a measured, load-bearing finding that invalidates a core assumption of this plan and retroactively affects Tasks 3-4's chosen mechanism:

- EF's own `NavigationExpandingExpressionVisitor` normalizes EVERY `Join`/`LeftJoin` call — genuine user-authored ones AND Include-lowering-generated ones — into the SAME `TransparentIdentifier(Outer, Inner)` result-selector shape (plus a trailing Select/Where) before QMTEV/TranslateJoinCore ever sees it. There is no "flattened vs. whole-side-capture" distinction visible at TranslateJoinCore's resultSelector — both smoke-test spellings compile to an identical tree.
- Consequence 1: Task 3's member-classification loop (whole-side-capture vs. already-flattened) is dead code for real joins — it never fires as designed.
- Consequence 2: Task 4's `TransparentIdentifier`-type-name decline guard (added to fix the 12 Include regressions) declines EVERY genuine join too, not just Include-lowering-generated ones, since both now share that exact shape. This is why the smoke test's failure mode never moved through Tasks 4-5 despite them being individually correct against their own briefs.
- Design point 1 (translator API) was resolved cleanly: `MongoExpressionTranslator.TryTranslateValue` is the correct single call for a two-scope projection leaf (subsumes TryTranslateField, carries the AllFieldsDefaultSerialized guard) — this part of Task 6 is genuinely done and reusable.
- Design point 2 (my dispatch instruction to avoid MongoTransparentScopeResolver) was based on the plan's now-falsified premise. The real lambdas ARE the fixed Outer/Inner two-hop shape MongoTransparentScopeResolver already handles at sourceCount:1 — Task 1's resolver should be REUSED here, not avoided. My dispatch instruction was wrong; noted for the record, not the implementer's fault.
- Additional structural issues found: (a) MongoSelectLowerer emits PipelineOps (Where/OrderBy/Skip/Take) BEFORE $lookup stages, so a post-join Where referencing a joined field would $match a not-yet-existing field — a silent zero-rows bug waiting to be hit once Where-after-Join is real; (b) NativeSlotPopulator's catch-all unconditionally forces Route to Fallback for any join query; (c) the shaper itself needs rebuilding, not just the projection; (d) Task 5's new lowering arm is gated on `IsCollection: false`, but this real shape resolves through a COLLECTION navigation, so Task 5's arm likely never fires for it either — worth re-verifying against Task 5's own (approved, tested) work, since this contradicts what Task 5's reviewer verified. This needs to be checked, not assumed.
- The proposed real fix requires a second confirm/rollback mechanism between MongoJoinBinder's tentative bind-time registration and TryConfirmReferenceInclude's later Select-time Include confirmation (whichever runs later "wins," or an explicit undo path) — a candidate/confirm handshake pattern the codebase's own AGENTS.md explicitly calls out as a "repeated silent-wrong-data trap." This is a genuine architectural decision with multiple valid approaches, not a mechanical fix.

Working tree: exploratory diff (builds clean, debug instrumentation removed, but incomplete/blocked) was stashed rather than committed or discarded — `git stash list`:
stash@{0}: On EF-322-Native-LINQ-rebased: EF-392 Task 6 exploratory diff (blocked — see task-6-report.md, NEEDS_CONTEXT)

Controller decision: this crosses the "plan so broken every path forward is a guess" bar for Task 6 as specified, AND retroactively puts Task 4's already-approved fix in question (it solves the wrong problem — declining ALL joins, not just Include's). Stopping the automated loop here to consult the user rather than unilaterally redesigning the Include/Join disambiguation mechanism, given the AGENTS.md-documented risk class involved. Not marking Task 6 complete or failed — parked pending user direction.

## Task 6 (REVISED): Document dormant scaffolding, verify graceful fallback

Task 6: minor (deferred): NativeJoinTests.cs's fallback-correctness test discards an unused spy logger from CreateContextWithLogging (inherited from the brief's own snippet, not an implementer error) — a plain CreateContext call would be simpler.
Task 6: complete (commits 4acb9ba..9a33d3d, review clean; original scope abandoned per EF-439/human ruling, see spec doc's "Blocker found during implementation")

## Task 7: Declining regression tests for explicit non-goals

Task 7: fix round 1/5 (2 addressed, 0 open; commits 105d2e2..aa697dd)
Task 7: complete (commits 9a33d3d..aa697dd, review clean)

## Task 8: Differential-correctness oracle fixture and full regression pass

Task 8: fix round 1/5 (1 addressed — evidence addendum, no code/test change, no new commit; commit unchanged at 2391931)
Task 8: complete (commits aa697dd..2391931, review clean; full 3-EF-config regression green, MONGODB_EF_NATIVE_ONLY=1 confirms zero new Join-related native passes per EF-439)

## All 8 tasks complete. Proceeding to final whole-branch review.

## Final whole-branch review (opus, range e59e3c9..2391931)

Verdict: With fixes. Confirmed real value in Task 1's extraction and the pivot's documentation discipline, but found the "dormant scaffolding" safety claim wasn't literally true: GroupJoin's user-authored result selector bypasses the TransparentIdentifier guard entirely (EF Core doesn't normalize GroupJoin the way it normalizes Join/LeftJoin), so MongoJoinBinder.TryBindJoin could still mutate state (register a lookup, flip UsesDriverJoinFields) on a live GroupJoin path — the same corruption class already measured dangerous for reference-Include elsewhere in this codebase. Plus: a lowerer comment that asserts the opposite of its own reachability (the new arm IS live for reference-Include, not just for the now-dormant join binder), a .First() that should degrade to .FirstOrDefault(), and an unordered oracle-theory assertion that only passes by coincidence. Dispatched one consolidated fix wave for all 4 Important findings + Minor sweep (cref qualification, comment wording, dead test helper, spec "superseded" markers, Query/AGENTS.md note, stray blank line) rather than one-fixer-per-finding.

## Final review fix wave + scoped re-review

Fix wave: fac1bae, fec1fd0, 042e869 — all 4 Important findings + 6-item Minor sweep addressed in one consolidated dispatch (not one-fixer-per-finding).
Re-review verdict: all findings addressed, no new Critical/Important breakage. Three new Minor observations surfaced by the re-review (deferred, no action needed): (1) new CS0162 unreachable-code warning in MongoJoinBinder.cs — non-fatal, repo has no TreatWarningsAsErrors anywhere (verified); (2) MongoJoinBinder's class <summary> is now stale relative to the unconditional-decline fix (the <remarks> below it is correct) — a future doc pass could tighten the summary too; (3) MongoSelectLowerer.cs's comment says isLeftOuter is "never lookup.PreserveNullAndEmptyArrays" while the FirstOrDefault-degrade path can technically read exactly that value — mildly self-contradictory as written, but the surrounding comment explains the degrade so a reader isn't misled.
Final review + fix wave: complete (commits 2391931..042e869, review clean).

## PLAN COMPLETE. All 8 tasks + final whole-branch review + one fix wave + scoped re-review are done, clean. Ready for finishing-a-development-branch.
