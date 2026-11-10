# Task 5 report: Projection-binder wiring — DONE

## Status: DONE

## Summary

Implemented the brief's two Step-3 production edits, PLUS a coordinator-ruled fix for a plan gap
discovered mid-implementation (a second, undocumented node-kind gate that also had to be widened for the
bare-projection-leaf form to work). All three new tests pass; full unit-suite regression run is clean.

## What was implemented

1. **Test file** (brief's two tests, plus one coordinator-directed regression test):
   `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeProjectionBinderConditionalAndDateTimeTests.cs`
   - `Conditional_wrapped_projection_leaf_is_admitted` (brief-specified)
   - `Conditional_bare_projection_leaf_is_admitted` (brief-specified)
   - `Bare_conditional_over_an_owned_collection_count_is_declined_by_the_subtree_check` (added per
     coordinator ruling — proves the new Tier-2 arm's `IsArrayFreeComputedSubtree` guard actually runs)

2. **`NativeProjectionBinder.cs`** — three changes:
   - Step-3-specified: widened the generic computed-leaf gate in `TryTranslateLeaf` (~line 478) to admit
     `MongoConditionalExpression`, `MongoDatePartExpression`, `MongoDateTimeOffsetLocalExpression`,
     `MongoElementRefExpression`.
   - Coordinator-ruled: added recursive arms to `IsArrayFreeComputedSubtree` (~line 1415) for
     `MongoConditionalExpression` (recurses into `Test`/`IfTrue`/`IfFalse`), `MongoDatePartExpression`
     (recurses into `Operand`), and `MongoDateTimeOffsetLocalExpression` (recurses into `Operand`).
   - Coordinator-ruled: added a new "gate 1c" case to `TryDeriveSyntheticAlias`'s switch (~line 1261),
     admitting `MongoConditionalExpression or MongoDatePartExpression or MongoDateTimeOffsetLocalExpression`
     as a bare-leaf top node, gated by `IsArrayFreeComputedSubtree(leaf)` — mirroring the pre-existing
     arithmetic/cast arm (gate 1b) exactly.

3. **`MongoProjectionBindingExpressionVisitor.cs`** — the one visitor change the brief specified: added a
   `ConditionalExpression` case to `Visit`'s switch (inserted after the cast case, before the
   `MethodCallExpression` case), gated on `Route == NativeRoute.Projection`, registering the whole
   `ConditionalExpression` as one projection-mapping entry.

## Verified (not just trusted): the date-member-chain visitor claim

Read the current `Visit` switch before editing and confirmed the pre-existing unconditional
`case MemberExpression memberExpression:` (line 155) sits BEFORE the arithmetic (line 176) and cast (line
205) cases, with no `when` guard, registering the whole `MemberExpression` and returning immediately. A
date-member chain (`r.OccurredOffset.Value.DateTime.Date`) is a `MemberExpression` at the top, so it's
caught there. No case was added for it — confirmed correct by the passing
`Conditional_wrapped_projection_leaf_is_admitted` test itself, whose `DT` assignment IS exactly this shape
nested inside the ternary's `IfFalse` branch, and which resolves to the right `MongoConditionalExpression`
projection without any visitor change beyond the one `ConditionalExpression` case.

## The plan gap (found, escalated, then resolved per coordinator ruling)

After implementing the brief's Step 3 exactly as written, the wrapped test passed but the bare test still
failed. Traced it to `NativeProjectionBinder.TryPopulateNativeProjection`'s bare-body branch: after
`TryTranslateLeaf` succeeds (via the Step-3 gate), the binder must ALSO derive an alias for the bare leaf via
`TryDeriveDocumentPathAlias` (declines — a conditional/date-part has no document path) or
`TryDeriveSyntheticAlias` (a SEPARATE node-kind allow-list the brief never mentioned, which didn't list any
of the three new kinds). This is a real plan gap, not something I should have guessed past — I stopped,
wrote up the finding, and reported NEEDS_CONTEXT.

**Coordinator ruling (final):** extend both `IsArrayFreeComputedSubtree` and `TryDeriveSyntheticAlias`
together, mirroring the existing arm 1b pattern exactly, plus add a regression test proving a bare
conditional wrapping a nested owned-collection `Count` (e.g. `x.Flag ? x.Items.Count : 0`) still declines
(not admitted unconditionally by node kind alone). Implemented exactly as directed — see code above.

## TDD evidence

**RED** (before either production edit) —
`dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeProjectionBinderConditionalAndDateTimeTests"`
— both of the brief's tests failed (`Assert.True() Failure — Expected: True, Actual: False`).

**PARTIAL GREEN** (after Step-3 edit only) — same command: `Conditional_wrapped_projection_leaf_is_admitted`
passed; `Conditional_bare_projection_leaf_is_admitted` still failed — this is what surfaced the plan gap.

**FULL GREEN** (after the coordinator-ruled Tier-2 fix + new regression test) — same command:

```
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 61 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```

All three tests pass: both brief-specified tests, plus the new
`Bare_conditional_over_an_owned_collection_count_is_declined_by_the_subtree_check` regression (asserting
`Assert.False(...)`, `Assert.Empty(mongoQ.Select.Projection)`, `Assert.False(mongoQ.Select.IsBareProjection)`
— confirming the safety net actually declines this shape rather than silently admitting it).

## Full-suite regression run

`dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"`

```
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7, Duration: 64 ms - MongoDB.EntityFrameworkCore.SpecificationTests.dll (net10.0)
Passed!  - Failed:     0, Passed:   988, Skipped:     0, Total:   988, Duration: 902 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```

Zero regressions (988/988 unit tests, plus 7 matched spec tests, all green).

## Self-review

- **Completeness:** both brief-specified files touched; brief's one new visitor case added, no more, no
  less. The additional `NativeProjectionBinder.cs` edits (Tier-2 gate + subtree recursion) were explicitly
  directed by the coordinator's ruling, not self-invented scope.
- **Quality:** new code matches the file's existing style — switch-expression arms for
  `IsArrayFreeComputedSubtree`, a `case ... when` arm for `TryDeriveSyntheticAlias` mirroring gate 1b's
  shape exactly, comments in the same voice/density as the surrounding code, referencing the specific
  hazard (nested `MongoSizeExpression`/`MongoFilteredSizeExpression` under an un-stripped driver-fallback
  `$size`) the way gate 1b's own comments do.
- **Discipline:** no case added for the date-member chain in the visitor — verified by testing (the wrapped
  test's `DT` assignment IS a date-member chain nested inside the conditional, and it resolves correctly
  with no visitor change beyond the `ConditionalExpression` case).
- **Testing:** all three tests in the new file pass (wrapped, bare, and the coordinator-directed
  decline-regression); full unit suite clean.

## Files changed (committed)

- `/Users/arthur.vickers/code/provider2/src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeProjectionBinder.cs`
- `/Users/arthur.vickers/code/provider2/src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs`
- `/Users/arthur.vickers/code/provider2/tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeProjectionBinderConditionalAndDateTimeTests.cs`

## Commit

`80f4654` — "Wire conditional and date-part projection leaves into NativeProjectionBinder and the shaper visitor"

(Note: `.superpowers/sdd/...` is gitignored in this repo, so this report file itself is not part of the
commit — only the three source/test files above are.)

## Issues / concerns

None outstanding. The plan gap found mid-task was resolved per an explicit, final coordinator ruling before
completion.
