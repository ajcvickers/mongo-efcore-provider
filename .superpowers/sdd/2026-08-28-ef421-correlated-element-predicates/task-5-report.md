# Task 5 Report: Correlated `Any`/`All` go native

## What was implemented

In `MongoExpressionTranslator.cs`'s quantifier arm (`TryMatchQuantifierMethod` branch of `TranslateNode`), the
correlation check that previously declined unconditionally now:

1. Calls `ReferencesEnclosingScope(elementLambda.Body, elementLambda.Parameters[0], out var quantifierFreeParam)`.
2. If a free parameter was found and it is `ReferenceEquals` to this translator's own `SelfParam`, builds a
   two-scope child translator (`outerParam: SelfParam, outerEntityType: _entityType, innerPrefix: null` — the
   brief's `innerPrefix: ""` corrected to `null` per the task instructions, matching Task 3's identical
   `$filter`/`$map`-variable-relative rationale), translates the element predicate with it, gates on
   `MongoAggregationExpressionRenderer.CanRender`, and returns a `MongoQuantifierExpression` wrapping a
   `MongoElementRefExpression(arrayPath, quantifierSource.Type)` and the correlated predicate.
3. Any other free parameter (a correlation reaching past the immediate enclosing scope, or `SelfParam is null`
   because this translator is itself already a two-scope child) still declines exactly as before.
4. The uncorrelated path (element-scoped translator → `$elemMatch`, with `All`'s negation-via-complement) is
   completely unchanged — untouched code below the new branch, confirmed via `git diff`.

Also added, per the brief's Step 2, to `NativeOwnedCollectionCorrelatedTests.cs`:
- `Correlated_All_goes_native`
- `Correlated_Any_and_All_are_correct_against_an_in_memory_oracle` (differential oracle test covering
  empty/no-match/all-match/one-mismatch states for `Any`, `All`, and `Count(pred) > 0`)

## TDD evidence

**RED** (Step 1 — `Correlated_Any_goes_native`, pre-existing from Task 3):
```
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~Correlated_Any_goes_native"
```
Result: FAIL — `NativeTranslationNotSupportedException`, as expected.

**RED** (Step 3 — whole class after adding the two new tests):
```
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollectionCorrelatedTests"
```
Result: `Correlated_filtered_Count_predicate_goes_native` PASSED (Task 3's work); `Correlated_Any_goes_native`,
`Correlated_All_goes_native`, `Correlated_Any_and_All_are_correct_against_an_in_memory_oracle` FAILED — 3
Failed, 1 Passed, Total 4.

**GREEN** (Step 6, after implementation, Step 5 build succeeded first):
```
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollectionCorrelatedTests"
```
Result: `Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4`

**Regression suite** (Step 7):
```
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollection"
```
Result: `Failed: 2, Passed: 217, Total: 219`. The two failures are:
- `NativeOwnedCollectionAllTests.All_with_a_correlated_element_predicate_declines_and_falls_back_to_correct_rows`
  — explicitly predicted by the brief as EXPECTED (resolved in Task 8). Confirmed.
- `NativeOwnedCollectionPredicateTests.Correlated_element_predicate_declines_and_falls_back_to_correct_rows`
  — NOT explicitly named in the brief, but the identical mechanism: an `AssertDeclinesCleanly`-style test for
  a correlated `Any` predicate that now legitimately goes native instead of declining. Verified this is not a
  correctness regression: temporarily patched a throwaway local copy of the test (never committed — restored
  byte-for-byte afterward, confirmed via `git diff` showing no changes to this file) to bypass the
  `Assert.Throws` line and print the `NativeOnly` result; the full test (including the "mixed form" second
  assertion) then passed, meaning `NativeOnly == DriverLinq == expected rows` for both the `b.Title == "match"`
  and mixed `b.Title == "match" && p.Rank > 3` predicates. Only the stale "must decline" expectation fails, not
  the query result. Left as-is per the task's Task-8 deferral instruction, since it is the same family/root
  cause as the explicitly-called-out `NativeOwnedCollectionAllTests` failure.

Final full build: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"` — Build succeeded, 0 errors.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs` — quantifier arm's
  correlation branch (lines ~626–655 pre-edit range).
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionCorrelatedTests.cs` — two new
  tests appended per the brief.

Commit: `b3761e9` "EF-421: correlated Any/All quantifiers go native via $anyElementTrue/$allElementsTrue"

## Self-review findings

- Confirmed via `git diff` that the uncorrelated `$elemMatch` path (element-scoped translator, `All`'s
  negation-via-complement, `IsQueryDialectRenderable` gate) is byte-for-byte unchanged — the new branch is
  purely additive above it.
- Applied the required `innerPrefix: null` correction (not the brief's literal `innerPrefix: ""`), consistent
  with Task 3's identical `$filter`/`$map`-`as`-variable-relative reasoning documented at the Count-predicate
  call site a few hundred lines above.
- Found one additional pre-existing test (`NativeOwnedCollectionPredicateTests.Correlated_element_predicate_declines_and_falls_back_to_correct_rows`)
  in the same "expected fallout" family as the brief's named one. Verified it is a stale-expectation failure,
  not a wrong-rows regression, and left it for Task 8 per the task's explicit instruction not to fix the
  known regression in this task.

## Issues or concerns

None blocking. One informational note carried forward for Task 8: it should also retire/rewrite
`NativeOwnedCollectionPredicateTests.Correlated_element_predicate_declines_and_falls_back_to_correct_rows`
(not just the `NativeOwnedCollectionAllTests` one named in the brief), since both assert a decline that no
longer happens after this task.

---

## Doc Comment Fix (post-review finding)

**Finding fixed:** `MongoExpressionTranslator.MethodCalls.cs:217-219` had an inverted doc comment claiming
"The quantifier (`Any`/`All`) call site does not build a two-scope translator and uses only the boolean return
(`out _`), so this multi-parameter distinction does not apply to it." This was load-bearing documentation that
predicted a hazard (multi-free-parameter silently mis-scoping) while describing the opposite of what Task 5
actually implemented.

**Fix applied:**
- Updated `ReferencesEnclosingScope()` doc summary (lines 206–220) to accurately state:
  - Both `Count(pred)` (Task 3) and quantifier `Any`/`All` (Task 5) call sites use `ReferencesEnclosingScope`
    and build two-scope child translators when correlation matches `SelfParam`.
  - Both rely on the multi-parameter distinction (`FoundParameter null` on two-or-more distinct free parameters)
    for correctness.
  - The distinction prevents silently mis-scoping a second free parameter's members by name against the element
    scope.

**Build:** `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"` — Build succeeded, 0 errors.

**Test run:**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollection"
```
Result: `Failed: 2, Passed: 217, Total: 219` — identical to Task 5's regression suite result; the two known
failures (`All_with_a_correlated_element_predicate_declines_and_falls_back_to_correct_rows` and
`Correlated_element_predicate_declines_and_falls_back_to_correct_rows`) are pre-existing Task 8 work items,
unaffected by the doc-only change.

**Commit:** `c4601c1` "EF-421: update stale doc comment on ReferencesEnclosingScope's multi-parameter guard"
