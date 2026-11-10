# SDD ledger — plan: /Users/arthur.vickers/code/mongo-efcore-provider/docs/superpowers/plans/2026-09-30-native-setoperations-suite-HANDOFF.md
Clone: ~/code/provider2, branch EF-322c-setops from EF-322c @ 8c5f00e1. No spec doc (provisional rulings). Gate: stop after every task (repo AGENTS.md).

## Pre-flight conflict scan
| Pair / task | Produces vs consumes | Finding |
|---|---|---|
| T1 x T2 (MQTEV TryTranslateSetOperation, IsBareScalarOperand, test class) | T1 produces IsBareScalarOperand; T2 CanRebindConstantLeafToDocument consumes it | Consistent. T2 gate edit and rewrite sit in the same admitted branch as T1's re-alias; order: rebind source1 shaper, re-alias source2 |
| T2 x T3 (MQTEV gate, IsPlainProjectedSelect) | T3 loosens !IsJoinQuery; T2 loosens HasShaperUnsafeConstantLeaf | Independent predicates; no conflict |
| T1 x T4 / T3 x T4 | T4 rebases onto Select plan Task 1 (external) | Baseline file conflicts -> regenerate. T4 blocked if Select T1 not landed -> stop and report |
| T1 own text | Tests vs code | DeclinesCleanly control not buildable with anon types; plan gives predicate-unit-test fallback. OK |
| T2 own text | Tests vs code | Allow-list excludes DateTime/Guid/enum so those DeclinesCleanly tests agree |
| T3 own text | Files | Modifies lowerer + MQTEV; tests in existing classes. Agrees |
| T4 own text | Steps | Step 1 may stop the plan (external dependency) |
Scan clean; no rulings needed.
Task 1: implemented 8c5f00e1..0deedefd; review: spec OK, 1 Important (default-serialization guard only partly mutation-proven: source1-side conjunct + grouped branch untested)
Task 1: minor (deferred): no positive test for Distinct operand re-alias (source2 IsPlainDistinctSelect)
Task 1: minor (deferred): no server-side trailing op (OrderBy/Take) over re-aliased combined result in functional tests
Task 1: minor (deferred): unverified by reviewer that bare o.PlainDate.AddDays(1) doesn't bind natively (implementer claim)
Task 1: fix round 1/5 (1 addressed pending re-review; commit amended 0deedefd -> 4ce4a0c4)
Task 1: Ruling: keep the grouped-accumulator default-serialization check as-is (over-conservative but safe: declines to fallback, never wrong data) — simplifying risks a wrong-data path and the owner can trim later — cost if wrong: a few grouped-over-converted shapes stay on driver-LINQ
Task 1: minor (deferred): key-part check in ReadsOnlyDefaultSerializedFields grouping branch is unreachable (dead code per implementer mutation)
Task 1: minor (deferred): grouped-accumulator decline test comment should say decline is deliberately conservative, not required for correctness
Task 1: minor (deferred): EF8/EF9 tests not re-run in fix round (build only)
Task 1: complete (commits 8c5f00e..4ce4a0c, review clean after fix round 1)
Task 2: implemented 4ce4a0c..0280a5b2; review: spec OK, approved. Important (pre-existing, outside diff, reasoned not run): Select(x=>8).Distinct().Union(...) passes HasShaperUnsafeConstantLeaf and nothing rebinds -> possible wrong data
Task 2: Ruling: not a fix-loop item (pre-existing, outside Task 2 diff); schedule a NativeAndParity probe for Distinct-wrapped constant source1 at the START of Task 4 verification and fix or decline if it fails — cost if wrong: possible wrong rows on an unfixed shape until then
Task 2: minor (deferred): Guid decline test passes upstream (comment it); client-evaluated decline test comment overstates; IsRoundTripSafeConstantLeafType uses shaper type not leaf type (double(long) widening >2^53 throws); HasShaperUnsafeConstantLeaf comment needs cross-ref; TryProbeBareValueRenders conjunct defence-in-depth, unkillable
Task 2: complete (commits 4ce4a0c..0280a5b, review clean)
Task 3: implemented 0280a5b..a0c1fb7; review: 1 Important (gate widening also admits Intersect/Except over join-scope source1, untested, no driver oracle)
Task 3: minor (deferred): duplicated lookup/PostJoinOps emission in lowerer (:88-94 vs :65-75); no test of OrderBy/Take over combined result with join-scope source1; HasConfirmedJoinLookup conjunct unkillable (comment in code); source2-join test pins existing cross-DbSet error (comment as known limitation); deferred-paging test can't show placement
Task 3: fix round 1/5 dispatched (Intersect/Except hand-oracle tests + two comments)
Task 3: fix round 1/5 (1 addressed, 0 open; commits a0c1fb7->16c9c9f amended)
Task 3: complete (commits 0280a5b..16c9c9f, review clean after fix round 1)
Task 4: Negate landed on EF-322c as 5498305b (tip 1f915fb8); backup/EF-322c-setops-pre-rebase @16c9c9fe made; Task 4 dispatched
Task 4: complete (rebased onto EF-322c 1f915fb8; commits 27d7c861 1b67cf82 37a49bbc 43837300 38770d19 6038952f; NativeOnly SetOps 0 failures EF8/9/10; full suites 0 failures)
Task 4: Ruling: Distinct-wrapped constant probe DID return wrong data ([8,8]); fixed in 43837300 along with a Task-2 regression (GroupBy(N).Select(g=>8) source1 crashed in default mode)
Task 4: Ruling: docs/native-query-status-EF-322.md absent on branch (and main clone) so not created; supported-shape list lives in task-4-report.md; Query/AGENTS.md invariant added
Final whole-branch review dispatched (fable) over 1f915fb8..6038952f
Final: fix wave b019be51 re-review clean (3/3 addressed); running EF10 full-suite confirmation
