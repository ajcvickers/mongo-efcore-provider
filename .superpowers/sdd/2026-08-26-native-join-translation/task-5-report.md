# Task 5 report: Lower a bound join scope into $lookup/$unwind stages

## What was implemented

Implemented ONLY the second (final, broadened) code snippet from the brief's Step 2 — the arm
gated on `query.IsJoinQuery` (not the first draft gated on `query.Select.JoinScope is { }
joinScope && joinScope.InnerPrefix == lookup.As`, which the brief itself identifies as
insufficient for the already-flattened-projection case).

New arm inserted into `MongoSelectLowerer.AppendLookupStages`, immediately before the existing
`lookup.Navigation is { IsCollection: true } && lookup.ForceUnwind` (reference-collection
SelectMany-flatten) arm:

```csharp
else if (lookup.Navigation is { IsCollection: false } && lookup.ForceUnwind && query.IsJoinQuery)
{
    var isLeftOuter = query.Select.JoinScope is { } joinScope && joinScope.InnerPrefix == lookup.As
        ? joinScope.IsLeftOuter
        : query.Joins.First(j => j.Alias == lookup.As).IsLeftOuter;
    stages.Add(new MongoLookupStage(lookup));
    stages.Add(new MongoUnwindStage(lookup, preserveNullAndEmptyArrays: isLeftOuter));
}
```

It discriminates from the existing reference-collection-flatten arm on `IsCollection` (`false` vs
`true`) — both are `ForceUnwind`-gated, so ordering between the two doesn't affect correctness,
but the join arm is placed first per the brief's stated preference (keeps both `ForceUnwind` arms
visually adjacent).

## Discrepancies vs. the brief's assumed signatures

None. Verified before editing:
- `MongoQueryExpression.Joins` is `IReadOnlyList<JoinInfo> Joins => _joins;`
  (`Expressions/MongoQueryExpression.Lookup.cs:169`).
- `JoinInfo` (`Expressions/JoinInfo.cs`) has `Alias { get; set; }` and
  `IsLeftOuter { get; }` exactly as assumed.
- `MongoJoinScope` (`Expressions/MongoJoinScope.cs`) has `InnerPrefix { get; }` and
  `IsLeftOuter { get; }` exactly as assumed.
- `MongoSelectDefinition.JoinScope` is `internal MongoJoinScope? JoinScope { get; set; }`
  (`Expressions/MongoSelectDefinition.cs:497`).
- `LookupExpression` has `Navigation`, `ForceUnwind`, `As` exactly as assumed.
- `System.Linq` was already `using`'d in `MongoSelectLowerer.cs`, so `query.Joins.First(...)`
  needed no new import.

The `AppendLookupStages` method body (lines ~342-395) matched the brief's quoted "current code"
verbatim — no drift since the plan was written.

## Smoke test failure mode: before and after

**Before** (Step 1, confirming Task 4's state):
```
NativeTranslationNotSupportedException : Query projects a non-entity result and
MongoQueryMode.NativeOnly forbids the driver-LINQ fallback.
  at MongoShapedQueryCompilingExpressionVisitor.ThrowIfNativeOnlyForbidsFallback
  at MongoShapedQueryCompilingExpressionVisitor.VisitProjectedQuery (line 332)
```

Note this is NOT the message the brief's Step 1 predicted verbatim ("Native pipeline does not
support this join shape (only single-level reference includes)." from `AppendLookupStages`'s own
join-coverage guard) — the query is failing even earlier, in `VisitProjectedQuery`'s
native/fallback gate, before `AppendLookupStages` (or the lowerer at all) is ever reached. This
matches the task context note: nothing yet marks the projected join `Select` as native-representable
end-to-end (`MongoJoinBinder` only registers the `$lookup`/`JoinInfo`/`JoinScope` state; it does not
touch `MongoSelectDefinition.Route`), so `Route` stays `Fallback` and the gate declines before
lowering runs.

**After** (Step 3, same test, same filter, rebuilt with the fix):
```
NativeTranslationNotSupportedException : Query projects a non-entity result and
MongoQueryMode.NativeOnly forbids the driver-LINQ fallback.
```
Identical failure — same message, same stack, same site (`VisitProjectedQuery` line 332). This is
expected: the fix in `AppendLookupStages` cannot be exercised because the query never reaches the
lowerer at all under the current gate. Confirmed by inspecting `MongoJoinBinder.cs`: there is no
call in it (or anywhere else search turned up) that flips the projection `Route` away from
`Fallback` for a join query, so `ClassifyNativeDisposition`/`VisitProjectedQuery`'s route check
always declines a joined projected `Select` today, independent of this task's change. Closing that
route-classification seam is explicitly Task 6's job per the brief.

## What was tested

1. `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"` — 0 errors (pre-existing NU1903
   advisory + one pre-existing CS1574 XML-doc warning in `MongoJoinScope.cs`, both unrelated to
   this change).
2. `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"` — 0 errors, same pre-existing warnings.
3. `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"` — 0 errors, same pre-existing warnings.
4. `dotnet test .../FunctionalTests --filter "FullyQualifiedName~NativeJoinTests"` — before and
   after the edit: 1 failed (same exception, same site) as documented above.
5. `dotnet test .../FunctionalTests --filter "FullyQualifiedName~NativeSelectManyTests"` — 132
   passed, 0 failed (confirms the reference-collection-flatten arm placed right after the new arm
   still fires correctly and wasn't shadowed by it).
6. `dotnet test .../FunctionalTests --filter "FullyQualifiedName~Query"` (full functional Query
   regression suite, includes reference-Include and set-op $lookup tests) — 1766 passed, 1 failed
   (the known/expected `NativeJoinTests` failure above), 45 skipped (encryption tests gated on
   `CRYPT_SHARED_LIB_PATH`, unrelated).
7. `dotnet test .../UnitTests --filter "FullyQualifiedName~Query"` — 677 passed, 0 failed.

No regressions in any suite.

## Files changed

- `/Users/arthur.vickers/code/provider2/src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs`
  — added the new `IsJoinQuery`-gated arm in `AppendLookupStages` (14 lines added, no lines
  removed/modified elsewhere).

## Self-review

- Confirmed only the SECOND (broadened, `IsJoinQuery`-gated) snippet was shipped — grepped the
  committed diff; no `joinScope.InnerPrefix == lookup.As` top-level guard exists (that check only
  appears nested inside the ternary, exactly as the final snippet specifies).
- New arm sits between the `IsNativeCollectionLookup` arm and the pre-existing
  `IsCollection: true && ForceUnwind` arm — correct position per the brief.
- Confirmed the new arm's `IsCollection: false` guard cannot double-fire with the existing
  `IsCollection: true` arm (mutually exclusive on `Navigation.IsCollection`), and cannot shadow the
  `IsStreamableReference`/`IsNativeCollectionLookup` arms above it (those are checked first in the
  `if`/`else if` chain and are unrelated predicates).
- `query.Joins.First(j => j.Alias == lookup.As)` will throw `InvalidOperationException` if no
  matching `JoinInfo` exists — by construction this can't happen for any lookup that reaches this
  arm (a join-query lookup's `As` alias is always set from the same `AddJoin`/`JoinInfo.Alias`
  registration path — `MongoJoinBinder` populates both together), but flagging that this arm
  assumes the invariant rather than defensively re-checking it, matching the brief's own snippet
  exactly (no additional defensiveness was added, intentionally, to stay faithful to "implement
  only the second snippet").

## Issues or concerns

- None beyond the already-flagged, expected gap: the smoke test remains red with an unchanged
  failure mode because the query never reaches `AppendLookupStages` under the current gate — this
  is Task 6's scope, not a defect in this task's edit. The fix itself is inert until Task 6 lands,
  which cannot be verified by direct execution in this task; correctness was instead verified by
  (a) matching all real signatures against the brief's assumptions exactly, (b) confirming the new
  arm doesn't regress any existing `$lookup`-producing suite, and (c) tracing why the gate still
  declines (no `Route` mutation anywhere in `MongoJoinBinder`).

---

## Fix round 1: targeted unit test + arm-ordering bug it caught

The coordinator relayed a task-reviewer finding: the original report framed verification as
necessarily limited to a signature audit, but a targeted unit test in
`MongoSelectLowererTests.cs` could exercise `AppendLookupStages`'s new arm directly (no `Route`/
Task 6 involvement needed), by combining the existing `TestReferenceSelect`/Tests-16-17 pattern
with `MongoJoinBinderTests.BuildFixture`'s `AddJoin` trick for making `query.IsJoinQuery` true.
Asked to add such a test exercising both `IsLeftOuter`-resolution branches (`JoinScope` set vs.
falling back to `query.Joins`) in both directions (`true`/`false`), and to add a code comment
near the `query.Joins.First(j => j.Alias == lookup.As)` call documenting the alias-match invariant
(previously only in the report, not the code).

### What the new test found: the new arm was dead code

Writing the test surfaced a real defect that the original signature-only verification missed
entirely. Two things:

1. **The coordinator's message's own claim about `AddJoin`** ("`outer.AddJoin(...)` sets the
   internal `_innerCollections` count, which is exactly what `IsJoinQuery` reads") **does not
   match the real code.** `MongoQueryExpression.AddJoin` (`Expressions/MongoQueryExpression.Lookup.cs:178-183`)
   only appends to `_joins`; `IsJoinQuery` reads `_innerCollections.Count > 0`
   (`MongoQueryExpression.Lookup.cs:188`), which is populated by the separate `AddInnerCollection`
   method. The real translator (`MongoQueryableMethodTranslatingExpressionVisitor.cs:1675,1680`)
   calls **both** `AddInnerCollection` and `AddJoin` together for every join. The first test draft,
   following the coordinator's message literally, called only `AddJoin` — so `query.IsJoinQuery`
   was `false` in the test, and the new arm's guard never matched at all. Fixed by adding
   `query.AddInnerCollection(navigation.TargetEntityType)` alongside `AddJoin` in all four new
   tests, mirroring the real call site.

2. **A genuine arm-ordering bug in the shipped code (found even after fixing #1).** With
   `IsJoinQuery` correctly `true`, two of the four new tests still failed: `PreserveNullAndEmptyArrays`
   always came back `true` (the `IsStreamableReference` arm's behavior) regardless of the new
   arm's computed `isLeftOuter`. Root cause: `LookupExpression.IsReference` is
   `Navigation is not { IsCollection: true }`, which is `true` for **any** non-collection
   navigation (or a navigation-less lookup) **regardless of `ForceUnwind`**. Round 1's new arm was
   inserted *after* the pre-existing `IsStreamableReference` check in the `if`/`else if` chain, so
   for every non-collection-navigation join lookup, `IsStreamableReference` matched **first** and
   won — the new arm, guarded on `IsCollection: false && ForceUnwind && IsJoinQuery`, could never
   be reached: `IsStreamableReference`'s guard is a strict superset of it for `IsCollection: false`.
   This was invisible to round 1's build/regression/report-only verification because
   `MongoJoinBinder.TryBindJoin` (Task 3, already landed) independently sets
   `LookupExpression.PreserveNullAndEmptyArrays = joinInfo.IsLeftOuter` at *registration* time
   (`MongoJoinBinder.cs:128-130`) — so on the real end-to-end path (once Task 6 lands),
   `IsStreamableReference` winning and reading `lookup.PreserveNullAndEmptyArrays` would coincidentally
   still produce the *correct* result for the whole-side-capture case, masking the fact that the
   new arm added in round 1 was unreachable dead code. The unit test's `LookupExpression`s were
   built directly (bypassing `MongoJoinBinder`'s registration-time assignment, exactly as the
   coordinator's brief specified — `new LookupExpression(navigation, forceUnwind: true)` with no
   `PreserveNullAndEmptyArrays` override), which is what exposed the shadowing immediately.

   **Fix:** moved the new arm to be the **first** check in `AppendLookupStages`'s `if`/`else if`
   chain, ahead of `IsStreamableReference`. This is safe: the new arm's guard
   (`ForceUnwind && IsJoinQuery && IsCollection: false`) only matches lookups `MongoJoinBinder`
   registers (a real `Include`'s reference lookup never sets both `ForceUnwind` and `IsJoinQuery`
   simultaneously — `IsJoinQuery` is exclusively driven by `AddInnerCollection`/`AddJoin`, called
   only from the join-translation path), so no existing `IsStreamableReference`/`IsNativeCollectionLookup`/
   collection-flatten case is affected. Added a comment at the new arm's declaration explaining
   why it must precede `IsStreamableReference`, and confirmed with the passing test suite (below)
   that this reordering doesn't change any existing behavior. This finding also means the new arm is NOT
   redundant with Task 3's `MongoJoinBinder` pre-baked `PreserveNullAndEmptyArrays` — reordering it
   ahead of `IsStreamableReference` makes the lowerer itself the authoritative source of `IsLeftOuter`
   for a join lookup (derived from `JoinScope`/`JoinInfo` at lowering time), rather than silently
   relying on a value baked in at binding time that happened to agree.

### Also addressed: code comment on the `.First()` invariant

Added, at the `query.Joins.First(j => j.Alias == lookup.As)` call site:
```csharp
// .First() relies on the invariant that every join-query lookup has a matching JoinInfo by
// alias: MongoJoinBinder always sets joinInfo.Alias from the same LookupExpression.As it
// registers the lookup under, so this can't miss for a lookup that reaches this arm.
```

### New tests added

`tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoSelectLowererTests.cs`:
- New fixture `TestJoinSelect()` / `JoinOuter`/`JoinInner` — a genuine to-one (non-collection)
  reference navigation, distinct from `TestReferenceSelect`'s collection navigation, needed to
  hit the `IsCollection: false` half of the new arm's guard rather than the pre-existing
  `IsCollection: true` arm.
- `Join_lookup_with_a_recorded_JoinScope_takes_IsLeftOuter_from_the_scope_left_outer` — `JoinScope`
  set to `true`, `JoinInfo` set to `false` (deliberately disagreeing) → asserts the emitted
  `MongoUnwindStage.PreserveNullAndEmptyArrays` is `true` (scope wins).
- `Join_lookup_with_a_left_outer_JoinScope_but_an_inner_JoinInfo_still_takes_the_scope` — the
  complement: `JoinScope` `false`, `JoinInfo` `true` → asserts `false` (scope still wins).
- `Join_lookup_with_no_recorded_JoinScope_falls_back_to_the_matching_JoinInfos_IsLeftOuter_true` —
  no `JoinScope` (asserted `Null` as a precondition), `JoinInfo.IsLeftOuter = true` → asserts `true`.
- `Join_lookup_with_no_recorded_JoinScope_falls_back_to_the_matching_JoinInfos_IsLeftOuter_false` —
  same, `JoinInfo.IsLeftOuter = false` → asserts `false`.

All four assert the emitted stage sequence is exactly `MongoLookupStage` (same `Lookup` instance)
followed by `MongoUnwindStage` (same `Lookup` instance, expected `PreserveNullAndEmptyArrays`).

### Commands run and results

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   # 0 errors (pre-existing NU1903/CS1574 warnings only)
dotnet test tests/.../UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoSelectLowererTests"
  # first run (AddJoin only, arm not yet reordered): Failed 2, Passed 40, Total 42
  # after adding AddInnerCollection to all 4 new tests, STILL Failed 2, Passed 40, Total 42
  #   (proved the arm-ordering bug independently of the AddJoin/AddInnerCollection gap)
  # after reordering the new arm ahead of IsStreamableReference: Passed 42, Failed 0, Total 42
dotnet test tests/.../UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query"
  # Passed 681, Failed 0, Total 681
dotnet test tests/.../FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query"
  # Passed 1766, Failed 1, Skipped 45, Total 1812 — the 1 failure is the same, unchanged
  # NativeJoinTests.Flattened_scalar_join_goes_native_with_correct_results_and_mql
  # (NativeTranslationNotSupportedException: "Query projects a non-entity result and
  #  MongoQueryMode.NativeOnly forbids the driver-LINQ fallback", from VisitProjectedQuery —
  #  Task 6's known remaining gap, unaffected by this fix round)
dotnet test tests/.../FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeSelectManyTests"
  # Passed 132, Failed 0, Total 132 (confirms the reordering didn't disturb the reference-collection
  # SelectMany-flatten arm or the pre-existing IsStreamableReference/Include paths)
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    # 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    # 0 errors
```

### Files changed (this round)

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs` — moved the
  EF-392 join arm to be the first check in `AppendLookupStages`'s dispatch chain (ahead of
  `IsStreamableReference`); added a comment explaining why the ordering matters and a comment on
  the `.First()` alias-match invariant. No change to the arm's actual `IsLeftOuter`-resolution logic.
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoSelectLowererTests.cs` —
  added the `TestJoinSelect` fixture and four new tests (above).

### Self-review (round 2)

- Confirmed the reordering is minimally scoped: only the position of the existing new arm moved;
  its body (the `IsLeftOuter` ternary and stage emission) is byte-identical to round 1.
- Confirmed via the passing `NativeSelectManyTests` (132/132) and the full functional `Query`
  suite (1766/1812, only the known Task-6 gap failing) that no existing `IsStreamableReference`,
  `IsNativeCollectionLookup`, or collection-flatten behavior changed.
- The dead-code finding is a good illustration of why the task-reviewer's requested targeted test
  mattered: a "does it build, does the regression suite stay green" check cannot catch an arm that
  is syntactically present and semantically correct in isolation but never actually reached — only
  a test that exercises the arm's own decision (not just the pipeline's aggregate behavior, which
  a different arm happened to reproduce by coincidence) surfaces it.
- Remaining known gap, unchanged from round 1: the smoke test (`NativeJoinTests`) still can't reach
  this code at all end-to-end (declines earlier, at `Route`), so real-world reachability of the
  now-correctly-ordered arm is still verified only via the direct unit test, not an integration
  test — that end-to-end wiring is Task 6's job.

