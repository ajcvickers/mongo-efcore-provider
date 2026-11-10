# SDD ledger — plan: docs/superpowers/plans/2026-09-25-native-join-scope-include-materialization.md

Pre-flight scan:

| Pair / Task | Shares | Produces (earlier) | Consumes (later) | Finding |
|---|---|---|---|---|
| Task 1 -> Task 2 | src/.../MongoProjectionBindingExpressionVisitor.cs, src/.../MongoQueryableMethodTranslatingExpressionVisitor.cs | `VisitIncludeExpression(MongoQueryExpression, IncludeExpression)`, new `BindResultMember`/`BuildSelectManyResultShaper`/`BuildSelectManyWrappedShaper` signatures (visitor param threaded through) | Task 2 only touches these files IF the Inner/LeftJoin case needs its own fix; Task 1 must be committed first | Clean — strictly sequential, Task 2 starts from Task 1's committed HEAD, no parallel edit risk. |
| Task 1 -> Task 3 | tests/.../NativeJoinTests.cs | Task 1 commits the file's current uncommitted state (the two acceptance tests + the fix making them pass) | Task 3 appends one new test to the same file, after Task 1's commit | Clean — sequential; Task 3's brief must diff against Task 1's already-committed file, not the pre-Task-1 working tree. |
| Task 2 -> Task 3 | none beyond the shared fix already covered above | Task 2's fix (if any) is unconditional over scope depth per plan text | Task 3's test exercises the fix at depth 2 | Clean — plan explicitly designed Task 3 to depend on whichever fix shape Tasks 1-2 land on, calling that out in its own Step 2 wording already. |
| Task 4 | tests/.../SpecificationTests/Query/Northwind*IncludeQueryMongoTest.cs (4 files) | Tasks 1-3 collectively (a fix that survives NativeOnly with correct data) | n/a | Clean — disjoint files from Tasks 1-3. |
| Task 5 | none (verification only) | Tasks 1-4 | n/a | Clean. |

Self-consistency, per task:
- Task 1: Steps 2-4's code changes (bridge method + unwrap/rewrap/thread-through) match Step 5-9's test
  evidence and commit list. Consistent.
- Task 2: pure verification-or-fix-if-needed; no internal contradiction.
- Task 3: new test's seed data (`SeedOwnersOrdersAndLines`) and assertions were independently checked against
  the plan author's own arithmetic (Alice: 2 orders/3 lines total, Bob: 1 order/1 line) before the plan was
  written — consistent.
- Task 4/5: verification-only, no internal contradiction.

No plan-mandated defect patterns found (no test asserting nothing, no mandated verbatim duplication).

Scan clean overall. No rulings needed before dispatch. Proceeding to Task 1.

Task 1: BLOCKED report received after Steps 1-4 (build clean, uncommitted). Implementer correctly identified
that the brief's own Step 6 fix (rename `wrappedLeafJoin.Lookup.As` + sync `Levels[...].InnerPrefix`) cannot
work: `MongoJoinScopeLevel.InnerPrefix` (`Query/Expressions/MongoJoinScope.cs:43-51`) is `{ get; }`-only, set
once in the constructor, and is consulted only ONCE, at translation time inside `TryBindProjection`/
`TryTranslateSingleScope` — both of which bake the alias into an IMMUTABLE `MongoFieldExpression.ElementName`/
`MongoElementRefExpression.Path` before the wrapped-leaf-join arm's own `if` body (where Step 6's rename would
run) is even reached. Renaming the join's alias post-hoc cannot reach back and fix those already-baked
strings — exactly the desync Step 6 itself warned about, with no mechanism preventing it. Confirmed via a
temporary diagnostic (added, inspected, removed — clean diff) that the failure IS the predicted single-lookup
alias collision (`AddLookup` dedups by `.As`; the join's `ForceUnwind: true` bare lookup silently wins over the
Include's own attempted registration since neither carries a pipeline), just manifesting as an
`InvalidOperationException` ("Document element '_lookup_Orders' is Document when BsonArray is required")
rather than literally either of Step 5's outcome (b)/(c) wordings.

Task 1: Ruling: Step 6 as written in the plan is WRONG and is superseded by the following corrected fix,
which stays entirely within Task 1's already-scoped files
(`MongoProjectionBindingExpressionVisitor.cs`/`.Lookup.cs`) and does NOT touch `MongoJoinScope.cs` or
`NativeJoinScopeProjectionBinder`/`NativeJoinScopeTranslator` at all — because the join's own alias and
everything already staged against it (any Inner-leaf scalar/whole-entity read in the SAME projection, e.g.
`r.Total`) must stay completely untouched; only the INCLUDE's own, separate lookup registration needs a fresh,
non-colliding alias when it detects the collision:

1. In `MongoProjectionBindingExpressionVisitor.cs`'s `VisitExtension`'s `case IncludeExpression` block
   (currently ~line 546-610): after constructing `lookup` (`var lookup = new LookupExpression(includableNavigation);`,
   ~line 546) and after the existing `ExtractNestedIncludePipeline(...)` call, but BEFORE
   `_queryExpression.AddLookup(lookup)` (~line 609), insert:
   ```csharp
   // EF-322 Phase 2 Group B (root cause A2): the Include's target navigation can coincide with a join's own
   // already-registered lookup at the SAME alias (e.g. Owners.Include(o => o.Orders).Join(db.Orders,
   // o => o.Id, r => r.OwnerId, ...) — the join's key resolves to the exact same Owner.Orders navigation the
   // Include targets). AddLookup dedupes by alias and, when neither side carries a pipeline, silently keeps
   // whichever was registered FIRST — the join's, since a join registers its lookup at
   // TranslateJoinCore/the wrapped-leaf-join arm's own commit, before this Include registration ever runs.
   // The join's own lookup is $unwind-ed (ForceUnwind: true — single document per row, needed for the
   // join's own row multiplication); this Include needs the BARE array. The two are structurally
   // incompatible under one $lookup regardless of pipeline state, so give THIS lookup a distinct alias
   // instead of colliding — the join's own alias, and everything already staged against it elsewhere in the
   // SAME projection, is untouched.
   var existingIncompatibleLookup = _queryExpression.GetPendingLookups()
       .FirstOrDefault(l => l.As == lookup.As && l.ShouldUnwind);
   if (existingIncompatibleLookup != null)
   {
       lookup.As = $"{lookup.As}_include";
   }
   ```
   Then change the following two lines from:
   ```csharp
                       _queryExpression.AddLookup(lookup);
                       return RewriteCollectionIncludeForLookup(includeExpression, includableNavigation);
   ```
   to:
   ```csharp
                       _queryExpression.AddLookup(lookup);
                       return RewriteCollectionIncludeForLookup(includeExpression, includableNavigation, lookup.As);
   ```
2. In `MongoProjectionBindingExpressionVisitor.Lookup.cs`, change `RewriteCollectionIncludeForLookup`'s
   signature (currently ~line 520) from
   `private Expression RewriteCollectionIncludeForLookup(IncludeExpression includeExpression, INavigation navigation)`
   to add a third parameter, `string lookupAlias`, and DELETE the method's own internal
   `var lookupAlias = LookupExpression.GetLookupAlias(navigation);` line (~line 551) — use the passed-in
   parameter instead everywhere that local used to be read (just the `ObjectArrayProjectionExpression`
   construction immediately below it). This is the ONLY call site of this method
   (confirmed via `grep -rn "RewriteCollectionIncludeForLookup(" src/MongoDB.EntityFrameworkCore/Query/Visitors/*.cs`
   — exactly one definition, one call), so the signature change is safe and total.
3. `LookupExpression.As` already has a public setter (`Query/Expressions/LookupExpression.cs:150`,
   `public string As { get; set; }`) and `LookupExpression.ShouldUnwind`/`GetPendingLookups()` are already
   public/accessible (confirmed) — no new API surface needed anywhere else.
4. Known, deliberately NOT fixed by this ruling: the OTHER, unrelated `var lookupAlias =
   LookupExpression.GetLookupAlias(navigation);` call in this same file (~line 200, inside the
   projected-collection-navigation path, `TryBindProjectedCollectionNavigation`'s helper — a DIFFERENT feature,
   `select new { ..., o.Orders.ToList() }`, not an `Include`) has the same theoretical collision risk but is
   unreachable by either of this plan's two acceptance tests. Out of scope for this ticket; noted here so a
   future finding against it is recognized as pre-existing, not a regression from this fix.

Cost if wrong: the rename could theoretically collide with some OTHER already-pending lookup also named
`"{alias}_include"` — vanishingly unlikely (would require a second, independently-suffixed collision on the
exact same string) and, if it somehow happened, `AddLookup`'s own dedup-by-alias would silently merge them
exactly as it does today for the ORIGINAL collision, i.e. no worse than the bug this fix removes. Re-dispatching
the same implementer (round 1 of resuming after BLOCKED, not a review-loop round) with this corrected Step 6.

Task 1: second BLOCKED report after applying the above verbatim. New failure, ONE LAYER DEEPER:
`MongoSelectLowerer.AppendLookupStages` (`Query/NativeTranslation/MongoSelectLowerer.cs:559-628`) recognizes a
plain collection-Include lookup via an OR-chain — `lookup.IsNativeCollectionLookup` (requires `As ==
GetLookupAlias(navigation)` EXACTLY, `LookupExpression.cs:232-243`) `|| <pipelined-nav disjunct>` `||
lookup.IsTransitiveCollectionLookup` (requires `As` to END with `"." + plainAlias`, a DOTTED nesting shape,
`LookupExpression.cs:249-262`) — falling to a hard `NativeTranslationNotSupportedException` throw
(`MongoSelectLowerer.cs:618-628`) if none match. My renamed alias (`"{plain}_include"`) matches NEITHER
recognized shape (not exact, not dotted), so it now hits the throw. This lookup used to be
indistinguishable from an ordinary collection-Include lookup before the rename; the rename fixed the
alias-collision but broke this SEPARATE, downstream recognition step — a third layer of the same underlying
coupling (join and Include sharing one alias namespace with no room for a legitimate third spelling).

Task 1: Ruling: rather than widening `IsNativeCollectionLookup` itself (touched by 2 OTHER call sites in
`NativeProjectionBinder.cs:2318,2455` for the UNRELATED projected-collection-navigation-count/list-leaf
feature — loosening its alias-equality check risks changing behavior there too, for no reason those call
sites need), add a NEW, narrowly-scoped, dedicated flag that only this fix's own rename ever sets, and widen
ONLY the lowerer's OR-chain with an independent disjunct guarded by it. Confirmed safe by reading the
render-side: `IsNativeCollectionLookup`'s branch and `IsTransitiveCollectionLookup`'s branch both render via
the IDENTICAL `stages.Add(new MongoLookupStage(lookup));` (no `$unwind`) — the disjunct choice only affects
RECOGNITION, never RENDERING — so a new disjunct with the same rendering is trivially correct once it fires.

1. In `Query/Expressions/LookupExpression.cs`, add a new property (near `ForceUnwind`/
   `PreserveNullAndEmptyArrays`): `public bool RenamedToAvoidJoinCollision { get; set; }` — documented as set
   only by `MongoProjectionBindingExpressionVisitor`'s collision-rename (root cause A2), letting the lowerer
   recognize this lookup as a plain collection-Include lookup despite its non-default alias.
2. In `MongoProjectionBindingExpressionVisitor.cs`'s collision-rename block (added in the prior ruling above),
   change:
   ```csharp
   if (existingIncompatibleLookup != null)
   {
       lookup.As = $"{lookup.As}_include";
   }
   ```
   to:
   ```csharp
   if (existingIncompatibleLookup != null)
   {
       lookup.As = $"{lookup.As}_include";
       lookup.RenamedToAvoidJoinCollision = true;
   }
   ```
3. In `Query/NativeTranslation/MongoSelectLowerer.cs`'s `AppendLookupStages` OR-chain (currently ~line
   559-564), add the new disjunct:
   ```csharp
   else if (lookup.IsNativeCollectionLookup
            || lookup.RenamedToAvoidJoinCollision
            || (lookup.Navigation is { IsCollection: true } pipelinedNav
                && lookup.PipelineKind is LookupPipelineKind.NestedInclude or LookupPipelineKind.FilteredInclude
                && !lookup.ForceUnwind
                && lookup.As == LookupExpression.GetLookupAlias(pipelinedNav))
            || lookup.IsTransitiveCollectionLookup)
   ```
   (insert `|| lookup.RenamedToAvoidJoinCollision` as the second disjunct; everything else in this `if` body
   is unchanged — it already renders via `stages.Add(new MongoLookupStage(lookup));` with no `$unwind`,
   exactly what this lookup needs).

Cost if wrong: the new flag/disjunct is additive and independently gated — it cannot change behavior for any
lookup that isn't the product of this fix's own rename (confirmed: only 4 total call sites of
`IsNativeCollectionLookup` exist codebase-wide, and this ruling touches none of them; only a NEW, separate
disjunct is added to the lowerer). Re-dispatching the same implementer with this addition on top of the
already-applied alias-rename fix.

Before re-dispatching a third time, checked for a FOURTH layer rather than assume this is the last one (two
prior "should be sufficient" rulings both proved incomplete): `MongoStreamingEntityMaterializerRewriter.cs:298`
ALSO independently recomputes `LookupExpression.GetLookupAlias(navigation)` for a collection Include, rather
than reading the actual registered alias off the `LookupExpression`/`ObjectArrayProjectionExpression` object
— which WOULD break the same way for a renamed lookup. Confirmed unreachable for both of this plan's
acceptance tests: `MongoShapedQueryCompilingExpressionVisitor`'s `Route == NativeRoute.Projection` branch
(the one both tests take, via the wrapped-leaf-join arm) passes `allowStreaming: false`, so the streaming
materializer never runs for this shape — only the DOM path (`MongoProjectionBindingRemovingExpressionVisitor`,
already confirmed to read via the correctly-threaded `Name`) does. Ruling: leave
`MongoStreamingEntityMaterializerRewriter.cs` untouched — this is a pre-existing, currently-unreachable latent
gap (would only bite a hypothetical future shape that combines streaming eligibility with this exact
alias-collision, which nothing in this plan produces), not something this fix needs to close. Cost if wrong:
zero for this plan's own acceptance criteria (confirmed unreachable); a future feature that makes THIS
specific collision reachable under streaming would need to apply the same rename-awareness there too — noted
here so that isn't a surprise later.

Task 1: BOTH acceptance tests (this task's Outer-leaf AND Task 2's Inner/LeftJoin leaf) PASS after the
3-part fix above. Full `NativeJoinTests.cs`: 53/53. Step 8 (full unit-test project) surfaced 6 regressions —
verified genuine (not pre-existing) by the implementer stashing all src changes and confirming a clean
baseline first:
- 4 crashes in `JoinScopeWhereSlotPopulationTests` (`Where_reading_outer_scope_after_join_populates_predicate_natively`,
  `OrderBy_reading_inner_scope_after_join_populates_sort_in_post_join_ops`,
  `Depth_one_join_through_owned_navigation_key_selector_is_natively_eligible`,
  `ThenBy_reading_inner_scope_after_outer_OrderBy_relocates_whole_sort_into_post_join_ops`) — all
  `InvalidOperationException: Stack empty` from `Stack<ProjectionMember>.Peek()` in `GetCurrentProjectionMember()`,
  reached via an ordinary (non-Include) owned-collection-navigation shape that happens to also route through
  `VisitIncludeExpression`/`VisitExtension`'s `IncludeExpression` case. Root cause: `VisitIncludeExpression`
  (added in Task 1 Step 2) calls `Visit` without ever pushing a `_projectionMembers` frame — `Translate` does
  (`_projectionMembers.Push(new ProjectionMember())`) before every `Visit` call; the bridge method didn't
  replicate that half of `Translate`'s setup (only skipped `ReplaceProjectionMapping`, correctly).
- 2 stale assertions in `NativeJoinScopeProjectionBinderTests.cs` (`Binds_a_whole_outer_entity_leaf_that_is_also_include_wrapped`,
  `Binds_a_two_level_chain_root_whole_entity_leaf_that_is_also_include_wrapped`) — both asserted a lookup COUNT
  that was correct only because the alias collision USED to silently collapse two lookups into one; now that
  the collision is fixed, there are correctly 2 (resp. 3) lookups, exactly as the design doc always said there
  should be (a distinct lookup for the join and for the Include when they'd otherwise collide).

Task 1: Ruling: fix `VisitIncludeExpression` to also push/pop a `_projectionMembers` frame, mirroring
`Translate`'s OWN push (`_projectionMembers.Push(new ProjectionMember())`) but — unlike `Translate` — still
skipping `ReplaceProjectionMapping`/`_projectionMapping.Clear()`/`_translatedRootExpression` (those remain the
reason this bridge exists instead of just calling `Translate`; only the ProjectionMember-stack half of
`Translate`'s setup was missing). Confirmed `_translatedRootExpression` staying `null` throughout the bridge's
lifetime is safe: its only 2 reads (`MongoProjectionBindingExpressionVisitor.cs:1101`, an arithmetic-spine
reachability check) treat a `null` root as "not reachable" — a safe, conservative decline, not a crash — and
this bridge's input is always an `IncludeExpression`, never the bare arithmetic `MethodCallExpression` shape
that check exists for. Corrected `VisitIncludeExpression`:
```csharp
internal Expression VisitIncludeExpression(MongoQueryExpression queryExpression, IncludeExpression includeExpression)
{
    var previousQueryExpression = _queryExpression;
    _queryExpression = queryExpression;
    _projectionMembers.Push(new ProjectionMember());
    try
    {
        return Visit(includeExpression);
    }
    finally
    {
        _projectionMembers.Pop();
        _queryExpression = previousQueryExpression;
    }
}
```
Also authorizing the implementer to fix the 2 stale unit-test assertions (bump the expected lookup counts by
1, with a one-line comment noting the collision fix is why) — these are a direct, correct, in-scope
consequence of Task 1's own fix, not scope creep; leaving them red would be a false regression signal for
every future run of this plan's later tasks and for the final whole-branch review.
Cost if wrong: none identified — the push/pop is exactly mirrored from `Translate`'s own proven-safe pattern,
scoped to the SAME method, self-balancing (push before, pop in `finally`), and cannot leak state since
`_projectionMembers` is empty both before and after every `Translate`/`VisitIncludeExpression` call today (no
other code path leaves a frame on the stack).

Task 1: Review 1 (opus). Spec compliant. 1 Important finding, 4 Minor (deferred):
- Important: new lowerer disjunct `|| lookup.RenamedToAvoidJoinCollision` admits ANY renamed lookup regardless
  of `PipelineKind`/`ForceUnwind` — a TPH-derived collection Include target (LookupPipelineKind.FallbackOnly)
  that also collided would render natively instead of staying fallback-only, silently producing wrong rows.
  Not reachable by either acceptance test, but opens a documented fail-closed guard. Entering fix loop, round 1.
- Task 1: minor (deferred): unit test assertions check only lookup COUNT, not which lookup got which
  alias/flag — a future change renaming the wrong side would still pass. Suggest 2-3 more asserts.
- Task 1: minor (deferred): `Whole_outer_entity_leaf_that_is_also_reference_included_goes_native`'s name/comment
  says "reference Include"/"unrelated to the join's own key" but it's actually a COLLECTION Include on the
  EXACT navigation the join's key resolves to — the opposite of what the comment says. Test behavior is
  correct (constraint forbids rewriting it), only the name/comment are misleading. Cannot fix without
  violating the plan's "do not rewrite the acceptance tests" constraint as literally stated — parking for the
  final review to decide whether a comment-only edit (no behavior change) is within that constraint's spirit.
- Task 1: minor (deferred): `FirstOrDefault(...) != null` with unused variable could be `.Any(...)`.
- Task 1: minor (deferred): a THIRD independent alias-recomputer found — `EntityProjectionExpression
  .BindNavigation` (`EntityProjectionExpression.cs:91`) also calls `GetLookupAlias(navigation)` directly.
  Same latent-gap shape as the two already ledgered (`Lookup.cs:200`, `MongoStreamingEntityMaterializerRewriter
  .cs:298`) — confirmed not reachable by either acceptance test. Added to the running list of known,
  out-of-scope latent gaps for a future ticket that makes any of these three reachable.

Task 1: fix round 1/5 (1 addressed, 0 open; commits fd3ede09..3415e526)
Task 1: complete (commits 6e0446a4..3415e526, review clean)

Task 2: verification-only, zero commits produced (confirmed via `git log`/`git status` — HEAD still at
3415e526, no new commits). Inner-leaf test 1/1 pass in isolation; full NativeJoinTests.cs 53/53 pass. No diff
exists to dispatch a task reviewer against, per the plan's own Step 3 allowance ("if Step 1 passed with no
code changes, skip this step — there is nothing to commit"). Task 1's fix generalizes over scope index with
no additional work needed.
Task 2: complete (no commits — Task 1's fix already covers this case, verified)

Task 3: Review 1 (sonnet). Spec compliant, no findings. Task quality: Approved.
Task 3: complete (commits 3415e526..4d22a90d, review clean)

Task 4: BLOCKED report at Step 1. Both target Northwind tests fail with DATA-CORRECTNESS assertions (not
AssertMql mismatches) at HEAD 4d22a90d — expected "FOLKO" got null; expected count 2 got 0. Same result with
and without MONGODB_EF_NATIVE_ONLY, so this is the DEFAULT (native) code path, not a NativeOnly artifact.
The stale recorded baselines for both tests show an "_outer"/"_inner"-keyed pipeline shape
(NorthwindIncludeQueryMongoTest.cs:559,641) — this is the shape family the DRIVER-LINQ FALLBACK's own
translation uses (MongoEFToLinqTranslatingExpressionVisitor / the driver's own LINQ v3 provider), NOT the flat
"_lookup_<Nav>" shape NativeJoinScopeProjectionBinder/BindResultMember (root cause A/A2's own machinery)
produces. This means these two tests likely never routed through the join-scope wrapped-leaf arm Tasks 1-3
(this plan) or root cause A (the prior plan) actually fixed — they go through a DIFFERENT arm entirely
(almost certainly TryGetReferenceIncludeChain/TryConfirmReferenceIncludeChain or
TryGetMixedReferenceAndCollectionIncludeChain in MongoQueryableMethodTranslatingExpressionVisitor.TranslateSelect,
never analyzed by either design doc).

Verified via a temporary worktree at the pre-this-plan commit (6e0446a4, i.e. root cause A's own final state,
BEFORE any of Tasks 1-3's code): BOTH tests ALSO fail there — with a DIFFERENT failure signature
(`FormatException: Element '_id' does not match any field or property of class Order` for test 1;
`Assert.Equal() Failure: Expected: False Actual: True` inside `AssertIncludeObject` for test 2) than at
4d22a90d (null/0 data mismatches). CONCLUSION: both tests were ALREADY broken before this plan's work started
— this is a PRE-EXISTING bug, NOT a regression introduced by Tasks 1-3. The failure SIGNATURE changed (Tasks
1-3's shared infrastructure — `MongoProjectionBindingExpressionVisitor`'s IncludeExpression handling runs
during projection-binding compilation regardless of eventual native/driver-linq routing, so it's plausible our
rename/flag changes touched this arm's behavior too) but the underlying BROKENNESS did not — neither state
produces correct data. Worktree removed after verification (`git worktree remove /tmp/ef322-pre-fix-check
--force`); no changes made to the main checkout.

Task 4: Ruling: the plan's Task 4 rests on a premise inherited from the ORIGINAL root-cause-A design doc
(`docs/superpowers/specs/2026-09-24-native-join-scope-include-wrapped-whole-entity-leaf-design.md`) — that
these two exact Northwind tests fail identically to that doc's own hand-built spikes, and would therefore be
fixed by the SAME mechanism. That premise was NEVER verified by actually running these two Northwind tests
against the fix until Task 4 just did so for the first time in this plan's entire history (spanning both the
prior plan and this one) — and it's FALSE: the two Northwind tests exercise a structurally different
translation arm (driver-fallback "_outer"/"_inner" reference/mixed-Include-chain handling) that neither
plan's fix ever touched or was designed to touch. This is a genuine, confirmed PLAN DEFECT in the premise
itself, not an implementation gap Task 4's own scope (baseline regeneration) can close, and not something to
attempt a THIRD unplanned/undesigned production fix for mid-task. Do NOT attempt to fix the underlying bug
here — it needs its own investigation and design doc (a new "root cause C," fully separate from the
join-scope Include work), exactly as this plan itself was spun up as a follow-up to root cause A.

Task 4: Ruling: mark this task's original 2 target tests as UNACHIEVABLE under this plan's scope. Do NOT
regenerate their baselines (doing so now would either fail outright, per Task 4's own Step 1 guard, or —
worse — could silently record a baseline for genuinely wrong data if the AssertMql call were ever reached,
which is exactly the failure mode `EF_TEST_REWRITE_BASELINES` is documented to be blind to). Leave both tests
and their existing stale baselines completely untouched. Cost if wrong: none — no files were modified by this
task, confirmed via `git status`; the two tests remain exactly as broken as they were found, ready for a
future, properly-scoped investigation.

Task 4: complete (no commits — target tests are unachievable under this plan's scope; confirmed pre-existing
via worktree verification, not a regression; recommend a new follow-up ticket for the driver-fallback
reference/mixed-Include-chain bug, fully separate from this plan's join-scope work)

Task 5: BLOCKED — genuine NEW regression found on EF10's NativeOnly sweep: 60 failures (10 multi-level-Include
test methods x 2 async variants x 3 of 4 Northwind Include sibling classes) absent at the pre-this-plan
baseline (cc6c6c4e, confirmed via the implementer's own temporary worktree diff: 298 pre-existing failures at
baseline vs 358 at HEAD — 60 net new, 0 fixed). All 60 fail identically: `Assert.Equal() Failure: Expected: 5
Actual: 1` in `QueryAsserter.AssertIncludeCollection` — a collection Include materializes only 1 element
instead of 5.

Root-caused by the controller (not the implementer, who correctly stopped rather than guess): Task 1's own
fix changed `RewriteCollectionIncludeForLookup`'s signature to take the ACTUAL `lookup.As` (the value used
for `AddLookup`/`$lookup` bookkeeping) as its `lookupAlias` parameter, replacing the method's PREVIOUS
internal computation of the PLAIN `LookupExpression.GetLookupAlias(navigation)`. This was correct for THIS
plan's own collision case (root-level rename, "_include" suffix) — but WRONG for the pre-existing "flat
multi-lookup mode" transitive case (`MongoProjectionBindingExpressionVisitor.cs`, the `else` branch handling
chained cross-collection reference navigations, ~line 578-618): for a multi-level Include
(`Include(o => o.Customer).Include(o => o.OrderDetails)`-shaped chains), `lookup.As` gets PREFIXED with the
intermediate reference lookup's own alias (`lookup.As = $"{intermediateLookup.As}.{lookup.As}"`) purely for
`$lookup`/`AddLookup` bookkeeping — but `ObjectArrayProjectionExpression`'s `Name` (what
`RewriteCollectionIncludeForLookup` builds it with) is designed to stay PLAIN/unprefixed, because the
intermediate scoping is ALREADY encoded separately via `outerEntityProjection.ParentAccessExpression` (per
the design doc comment right above this code: "the shaper reads the collection array relative to the
intermediate's ParentAccessExpression (i.e. `_lookup_Nav._lookup_Collection`)" — the PARENT access expression
supplies the FIRST half of that dotted path, `Name` supplies only the SECOND, plain half). Confirmed the
SAME plain-not-prefixed expectation holds for the OTHER pre-existing prefixing branch too
(`UsesDriverJoinFields`, the `_outer.`/`_inner.` prefix) — `RewriteCollectionIncludeForLookup`'s ORIGINAL,
pre-Task-1 internal `GetLookupAlias(navigation)` call ignored BOTH kinds of `lookup.As` prefixing
unconditionally, which was correct; my Task 1 fix broke that invariant by feeding the method the fully
mutated `lookup.As` instead.

Task 5: Ruling: fix `MongoProjectionBindingExpressionVisitor.cs`'s `case IncludeExpression` block to capture
the PLAIN alias (`LookupExpression.GetLookupAlias(includableNavigation)`, equivalently `lookup.As` read
IMMEDIATELY after `new LookupExpression(includableNavigation)`, before ANY prefixing) into a separate local,
and base the "_include" collision-rename suffix (and the value passed to `RewriteCollectionIncludeForLookup`)
on THAT plain value — completely decoupled from whatever `lookup.As` ends up mutated to for
`AddLookup`/`$lookup`-registration purposes (driver-join-fields prefix, transitive intermediate prefix, or
neither). The collision check itself (`l.As == lookup.As && l.ShouldUnwind`) stays keyed on the ACTUAL,
possibly-prefixed `lookup.As` — correct, since that's the real value that would collide with another
registered lookup — only the VALUE PASSED TO THE READER changes. Sending this back to Task 1's original
implementer (same agent, since the bug is in code it wrote and best understands) rather than dispatching
fresh, since context is intact and this is a precise, well-understood, single-location fix — NOT grounds for
escalating model tier, this isn't a case of the implementer being stuck.

Task 1 (regression fix, commit 20bb0b9a): Review (opus). Spec compliant, task quality Approved. 4 Minor
findings (deferred — same-class-bug-risk check came back clean/no new instances, no test pins the
combined-collision+prefix case, comment carries incident history that belongs in git not code, `readAlias`
lacks its own explanatory comment). One ⚠️ item: reviewer could not independently confirm the 18 remaining
NativeOnly failures (in the 3 affected Northwind classes) are pre-existing/unrelated to the regression fix,
using only the implementer's summary. Controller resolved this directly against Task 5's own report file
(section listing the 10 regressed method names, lines 52-61 of task-5-report.md): none of the 10 regressed
names (`Include_multi_level_reference_and_collection_predicate`,
`Include_multi_level_reference_then_include_collection_predicate`,
`Include_multiple_references_and_collection_multi_level`,
`Include_multiple_references_and_collection_multi_level_reverse`,
`Include_multiple_references_then_include_collection_multi_level`,
`Include_multiple_references_then_include_collection_multi_level_reverse`,
`Include_references_and_collection_multi_level`, `Include_references_and_collection_multi_level_predicate`,
`Include_references_then_include_collection_multi_level`,
`Include_references_then_include_collection_multi_level_predicate`) match any of the 3 known-pre-existing
names (`Include_with_complex_projection_does_not_change_ordering_of_projection`,
`Include_reference_when_entity_in_projection`,
`Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join`) — 3 names x 2 async x
3 classes = 18, matching exactly. Confirmed independently, not just trusting the implementer's claim.
Task 1: fix round 2/5 (1 addressed — the multi-level regression — 0 open; commits 4d22a90d..20bb0b9a)
Task 1: complete (commits 6e0446a4..20bb0b9a, review clean, 1 regression found+fixed during Task 5's sweep)

Task 5: Retry results. Steps 1-2 (MONGODB_EF_NATIVE_ONLY=1, ~Query, EF10/EF9/EF8): byte-for-byte identical
failure-name sets vs the pre-plan baseline on all 3 versions (298/687/653 respectively) — zero regressions,
all 10 previously-regressed multi-level-Include names confirmed absent. Step 3 (/test-all): unit tests 100%
green on all 3 versions; SpecificationTests failures all within the known set (brief's original ~7 + Task 4's
2 pre-existing Northwind names across their sibling classes); FunctionalTests found ONE new-looking failure —
`NativeJoinTests.Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join` (this
plan's OWN Task 1 test) fails on EF8 and EF9 only (passes EF10), thrown by EF Core's OWN
`NavigationExpandingExpressionVisitor` before any Mongo-specific dispatch — NOT a Mongo translator bug.

Task 5: Ruling: this is a real, but ALREADY-TRACKED, pre-existing EF8/EF9 limitation, unrelated to this
plan's own fix. Confirmed by reading the untracked sibling plan
`docs/superpowers/plans/2026-09-24-ef8-ef9-leftjoin-shim-native-include.md` (recovered earlier this session,
written by a prior session before this plan existed): "EF Core lowers Include on an optional reference
navigation to its own private LeftJoin shim [under EF8/EF9]... the query is marked non-native before the
join-scope binder... ever runs" — a DIFFERENT, EF-Core-nav-expansion-level gap that has its OWN dedicated,
not-yet-executed implementation plan. This plan's Task 1 added a test combining an explicit `.LeftJoin(...)`
with `.Include(...)` on the joined-in side without checking EF8/EF9 compatibility first — an oversight in
THIS plan's own test, not a regression in the production fix (the fix itself is version-agnostic and
untouched by this finding; the failure occurs before the fix's own code ever runs, at EF Core's nav-expansion
stage). Gating this ONE test to EF10-only (matching this exact file's own established pattern — 6 other
`#if !EF8 && !EF9` blocks already exist in `NativeJoinTests.cs`) is the correct, minimal, honest fix: it does
not hide a production regression (there is none), it just stops asserting a shape EF8/EF9 cannot even
construct via nav-expansion for reasons entirely outside this plan's scope. The OTHER two acceptance tests
(`Whole_outer_entity_leaf_that_is_also_reference_included_goes_native`, using explicit `.Join` not `.LeftJoin`,
and Task 3's chain-depth test) are confirmed unaffected and run correctly on all 3 EF versions — this is not a
gap in coverage of the FIX, only of this one test's LeftJoin+Include shape on EF8/EF9. Cost if wrong: if the
sibling LeftJoin-shim plan is never executed, EF8/EF9 users combining an explicit LeftJoin with a
joined-in-side collection Include would remain on the (correct, if slower) driver-LINQ fallback — a known,
already-tracked, pre-existing limitation, not a regression from this work.

Task 1 (EF8/EF9 guard fix, commit cab78a07): Review (haiku, small test-only diff). Approved, no issues.
Task 1: fix round 3/5 (1 addressed — EF8/EF9 guard — 0 open; commits 20bb0b9a..cab78a07)
Task 1: complete (commits 6e0446a4..cab78a07, review clean)

Task 5: complete (no commits — verification only). Steps 1-2 clean on all 3 EF versions from the retry (zero
regressions, byte-for-byte identical failure sets vs pre-plan baseline). Step 3's one finding (the EF8/EF9
LeftJoin+Include test incompatibility) was resolved via Task 1's fix round 3 (cab78a07, reviewed clean) — a
targeted rerun of NativeJoinTests on all 3 EF versions post-fix confirms EF8 45/45, EF9 45/45, EF10 54/54, no
full third re-sweep needed since the fix is test-file-only and narrowly scoped (removes one test from EF8/EF9
compilation, no production code touched). Final state: zero regressions from this plan's work; 2 pre-existing,
out-of-scope Northwind bugs identified (Task 4, need a separate follow-up ticket); the design doc's own
non-join-scope wrapped-reference-Include gap remains unverified/presumed unchanged (out of scope); the
EF8/EF9 LeftJoin-shim-Include gap is real, pre-existing, and already tracked by a separate, not-yet-executed
sibling plan.

Final review (opus, whole-branch 6e0446a4..cab78a07): "With fixes." 1 Important finding CONFIRMED CORRECT
by the controller (overturning the controller's own Task 4 ruling) via independent verification:

**Important #1 — CONFIRMED, Task 4's ruling was wrong.** `BindResultMember`'s unwrap loop
(`while (unwrappedFoldedExpression is IncludeExpression includeToUnwrap) { ... }`) fires for ANY
`IncludeExpression`, reference or collection — it never checks `Navigation.IsCollection`. For a REFERENCE
Include, `VisitIncludeExpression` → `Visit` → `VisitExtension`'s `case IncludeExpression` → since
`!includableNavigation.IsCollection`, takes the OTHER branch (`_includedNavigations.Push(...);
base.VisitExtension(includeExpression); ...`), which has NO lookup-registration logic at all (that only
lives inside the `IsCollection` branch) — so nothing establishes where to read the reference nav's data from.

Controller fetched the ACTUAL EF Core spec-test source (`gh api repos/dotnet/efcore/contents/test/
EFCore.Specification.Tests/Query/NorthwindIncludeQueryTestBase.cs`) to verify, rather than trust the
reviewer's inference: `Include_reference_when_entity_in_projection` is literally
`ss.Set<Order>().Where(o => o.CustomerID!.StartsWith("F")).Include(o => o.Customer).Select(o => new { o,
o.CustomerID })` — a REFERENCE Include (`Order.Customer`) wrapped in a projection, with NO explicit
`.Join()`/`.LeftJoin()` call in the user's own LINQ. This is IDENTICAL (module field name) to the ORIGINAL
root-cause-A design doc's own "spike 1" example
(`Orders.Include(o => o.Customer).Select(o => new { o, o.CustomerID })`) — meaning root cause A's OWN
analysis, from the very first design doc, already established that EF Core's nav-expansion lowers a
reference-Include-in-projection into an INTERNAL join construct that OUR `IsSingleEligibleNativeJoinScope`/
`NativeJoinScopeProjectionBinder` machinery recognizes, DESPITE no explicit `.Join()` in the user's LINQ.
Task 4's dismissal ("different arm, the '_outer'/'_inner' MQL naming proves it") was based ONLY on surface-
level MQL field-naming convention (driver-join-fields mode uses "_outer"/"_inner"; our explicit-Join
functional tests use flat "_lookup_<Nav>" naming) — CONTROLLER NEVER ACTUALLY TRACED WHICH CODE ARM HANDLES
IT, just inferred from naming. The naming difference reflects WHICH JOIN REPRESENTATION was used
(driver-join-fields vs flat-multi-lookup), NOT which RECOGNIZER (`NativeJoinScopeProjectionBinder` vs some
other arm) processed it — both representations can reach the SAME wrapped-leaf-join arm.

This is a genuine regression in behavior class (loud FormatException -> silent null), even though the TEST
itself was already broken before AND after (Task 4's "unachievable, needs separate ticket" conclusion for
regenerating ITS baseline still stands) — the codebase's own documented design principle
(`MongoProjectionBindingExpressionVisitor.cs`'s `nativeRootShaper` comment: "a mismatch is a hard failure,
never silent wrong data") is violated by this diff for the reference-Include case, and this affects REAL
USERS writing `.Include(referenceNav).Select(o => new { o, ... })` over any join-scope-recognized shape, not
just this one spec test.

Final review: Ruling: narrow `BindResultMember`'s unwrap to fire ONLY when EVERY `IncludeExpression` in the
chain targets a COLLECTION navigation. If any wrapper in the chain targets a REFERENCE navigation, do NOT
unwrap at all — fall through entirely to the pre-existing `BindSelectManyMember` path, restoring EXACTLY the
pre-Task-1 (loud-failure) behavior for that shape. This correctly scopes this plan's fix to what it actually
tested and proved (collection-only Include chains on a join-scope whole-entity leaf) — fixing reference-
Include materialization within a join-scope leaf is a separate, unverified, harder problem (does the
reference nav need its own lookup-registration/collision logic mirroring the collection case? unknown) that
needs its OWN investigation, exactly like the design doc's other flagged latent gaps. Cost if wrong: reverts
to the SAFE, pre-existing behavior for reference Includes (loud failure) — worse than fixing it, but no worse
than before this plan existed, and never worse than silent wrong data.

Important #3 (VisitIncludeExpression doesn't restore `_projectionMapping`): also confirmed real defensive
gap, though it becomes UNREACHABLE by this diff specifically once Important #1's narrowing fix lands (the
reference-Include branch that writes to `_projectionMapping` via `base.VisitExtension` can no longer be
reached through this bridge at all). Fixing anyway as cheap, correct defensive hardening per the plan's own
Review Focus item ("must not leak state... on every return path").

Dispatching both fixes to Task 1's implementer (same agent, full context).

Final review fix wave: applied both fixes (narrow BindResultMember's unwrap to collection-only Include
chains; restore _projectionMapping in VisitIncludeExpression). Implementer verified the bug BEFORE fixing
(confirmed silent Owner=null, no throw) and AFTER (confirmed loud FormatException, matching pre-Task-1
behavior) — proper RED/GREEN proof, not just a claim. Full regression: NativeJoinTests 55/55, unit tests
1285/1285, 3 Northwind Include classes under NativeOnly: 697/18 (same 18 known-pre-existing failures,
confirmed by name against the real EF Core source, no new failures). Committed as 0b0a7a6d on top of
cab78a07. Commit chain: fd3ede09, 3415e526, 20bb0b9a, cab78a07, 0b0a7a6d.

Final review fix wave: scoped re-review (opus). Finding ADDRESSED, no new Critical/Important breakage.
Re-reviewer independently confirmed GREEN on all 3 EF versions (implementer had only reported EF10 for the
new test specifically — re-reviewer ran EF8/EF9 too: 46/46, 46/46, 55/55, filling a gap in the implementer's
own verification). 2 Minor findings + 3 out-of-scope observations:

Final: minor (deferred): the collection-only gate in BindResultMember is wider than strictly necessary — it
also routes an OWNED (embedded) reference Include wrapper to the fallback path, even though owned references
would have worked fine on the fixed path (VisitExtension's base.VisitExtension arm reads them from the
parent document already). No functional test uses owned types in a join scope, so this is unverified either
way; costs nothing today, narrowing later ("IsCollection || navigation.IsEmbedded()") is a safe future
tightening if someone needs it.
Final: minor (deferred): the new fallback-path comment/test claims "hard failure for that shape" which holds
for the Outer/root ($$ROOT) leaf position (the only one tested) but not necessarily for an Inner leaf, where
the SAME fallback mechanism is already documented (design doc) to return null silently rather than throw.

Final: Ruling (parked, unverified, out of scope for this plan): a reference Include on an INNER join-scope
leaf (e.g. `Owners.Join(Orders.Include(r => r.Owner), ..., (o, r) => new { o, r })`) may hit the SAME
silent-null failure mode this plan just fixed for the Outer/root position — but via a DIFFERENT code path
(`NativeJoinScopeProjectionBinder.cs`'s own unwrap, from the EARLIER root-cause-A plan, not this plan's own
commits) that the re-reviewer did NOT confirm is actually reachable (unverified whether EF lowers any
real-world shape into a join scope this recognizes for this exact combination). Not fixing this now: it's
unverified, untested by anything in this plan, and the correct fix (decline at `TryBindProjection` when any
wrapper targets a non-embedded reference) touches code from a DIFFERENT, already-merged plan — properly
scoped as a follow-up investigation (alongside or as part of the same "root cause A3" ticket Task 4's finding
already calls for), not a fix to make inline here at the very end of an already-long fix chain. Cost if
wrong: a real, currently-unverified gap remains — but no worse than it was before this entire plan started,
and explicitly surfaced here rather than silently dropped.

Final: Ruling: both Task-4-identified pre-existing Northwind bugs (`Include_reference_when_entity_in_projection`,
`Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join`) ALSO fail in DEFAULT
query mode, not just under `MONGODB_EF_NATIVE_ONLY=1` (re-reviewer confirmed: 8/8 fail in default mode on
EF10, including `NorthwindIncludeNoTrackingQueryMongoTest`, a 4th sibling class the implementer's own 3-class
filter didn't cover) — since native is the default mode on this branch, real users hit this, not just a
NativeOnly-gated CI signal. This raises the priority of Task 4's already-recommended separate follow-up
ticket but does not change this plan's own scope. Noting the 4th sibling class
(`NorthwindIncludeNoTrackingQueryMongoTest`) as also affected, for whoever picks up that follow-up.

Final review: clean (finding addressed, fix wave re-reviewed, residual items parked/deferred with rulings
above — none load-bearing for this plan's own tasks).
