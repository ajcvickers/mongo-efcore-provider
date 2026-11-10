# Final fix wave — native conditional/date-part branch

Branch: `EF-322-Native-LINQ-rebased` (confirmed before starting work).

## Finding 1 (Critical) — MongoFieldPrefixRewriter missing arms

**Fix:** Added arms for `MongoConditionalExpression`, `MongoDatePartExpression`,
`MongoDateTimeOffsetLocalExpression`, and `MongoElementRefExpression` to
`MongoFieldPrefixRewriter.Rewrite`'s switch (`src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoFieldPrefixRewriter.cs`),
placed next to the existing `MongoConvertExpression` arm, exactly as specified. Added a comment above the
group explaining why the arms exist (this is the one node-kind switch whose default THROWS instead of
declining, so any new `TryTranslateValue`-producible node kind must be added here too).

**Regression test:** Added two tests to
`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeSelectManyTests.cs`:

- `Explicit_result_selector_form_computed_date_part_leaf_goes_native` — `SelectMany(o => o.Items, (o, i) => new { X = i.Occurred.Year + 1 })`, run under both `Native` and `NativeOnly`.
- `Explicit_result_selector_form_computed_conditional_leaf_goes_native` — `SelectMany(o => o.Items, (o, i) => new { X = (i.Flag ? 1m : 2m) + 0m })`, run under both `Native` and `NativeOnly`.

Note on the conditional test shape: `NativeSelectManyBinder.IsArithmeticComputedLeaf` only admits a leaf whose
TOP node is arithmetic (`+ - * / %`); a bare top-level ternary in a SelectMany result-selector declines there
(unrelated to this bug, unchanged before/after the fix). So the ternary is nested inside a `+ 0m` wrapper to
reach the actual `MongoFieldPrefixRewriter` code path Finding 1 is about (scope-1/inner-element leaf requiring
prefixing).

Added `Item.Occurred` (`DateTime`) and `Item.Flag` (`bool`) fields to the test fixture's `Item` class, with
seed values on the existing Alice/Carol items, to support these two new tests without disturbing any other
test in the file (additive fields; other tests never select them).

**Test evidence:**

- RED (fix reverted via `git stash` on just `MongoFieldPrefixRewriter.cs`): both new tests failed with
  `NativeTranslationNotSupportedException: Cannot prefix-rewrite MongoExpression node 'MongoDatePartExpression'.`
  and `'MongoConditionalExpression'.` respectively — reproducing exactly the regression described in the
  finding, in every mode (the loop over `Native`/`NativeOnly` failed on the first iteration in both cases,
  confirming it's a translation-time throw, not merely a `NativeOnly`-only decline).
- GREEN (fix restored): `dotnet test ... --filter "FullyQualifiedName~NativeSelectManyTests"` → **134/134 passed**
  (includes the 2 new tests plus all 132 pre-existing ones — no regressions).

No surprises downstream — the fix worked as specified; no further gate/renderer changes were needed.

## Finding 2 (Important) — stale EF-449 ticket reference

Removed the `(EF-449: native ternary/date-part projection leaves)` parenthetical from the "Gate 1c" comment
in `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeProjectionBinder.cs` (~line 1284), keeping
the descriptive text. Verified no other `EF-449` reference exists anywhere in the repo (`grep -rn "EF-449"`
returned only this one, now-fixed, hit before the change).

## Finding 3 (Important) — Query/AGENTS.md stale/missing capability documentation

- Removed "date parts" from the `MongoExpressionTranslator` bullet's "computed long tail... remains
  unsupported" list (kept "string transforms" and "`Math.*`").
- Added a new capability bullet ("Conditional/ternary and DateTime/DateTimeOffset date parts...") to the
  "What's native today" section, describing `$cond` for ternaries and the native date-part/member-access
  surface (`.Value`/`.DateTime`/`.LocalDateTime`/`.UtcDateTime`/`.Date`/`.Year`/.../`.DayOfYear`, explicitly
  excluding `.TimeOfDay`), in the same terse style as neighboring bullets, citing the relevant node types.

## Finding 4 (Minor sweep)

1. **Tautological assertion** — `MongoConditionalAndDateTimeTranslatorTests.Conditional_with_null_check_condition_translates`
   now asserts `Assert.Null(Assert.IsType<MongoConstantExpression>(conditional.IfTrue).Value)` directly (the
   `IfTrue` branch is the null-constant one in that test's selector), replacing the always-`Assert.Null(null)`
   ternary.
2. **`MongoConditionalExpression.Type`** — now `ifTrue is MongoConstantExpression { Value: null } ? ifFalse.Type : ifTrue.Type`,
   with an XML remarks addition explaining why (a null constant's own `.Type` falls back to `object`, and
   `MongoSelectLowerer` reads `KeySelector.Type` for a computed sort key, which a conditional can now be).
   Added `MongoConditionalExpression_type_prefers_IfFalse_type_when_IfTrue_is_a_null_constant` to
   `MongoConditionalAndDatePartExpressionTests.cs` pinning `Type == typeof(DateTime)` for that shape.
3. **Wording nit** — reworded the "neither/either" comment (which described 2 alternatives but covers 3 node
   types) to "none of these has a query-dialect form, so admitting any of them here would..." in
   `MongoQueryLanguageRenderer.cs`.
4. **Missing `.UtcDateTime` wrapped-leaf coverage** — added
   `UtcDateTime_wrapped_projection_leaf_is_admitted` to `NativeProjectionBinderConditionalAndDateTimeTests.cs`,
   asserting `new { UtcDT = r.OccurredOffset!.Value.UtcDateTime }` is admitted and its projection expression is
   a `MongoElementRefExpression` (confirmed via `MongoExpressionTranslator`: `.UtcDateTime` addresses the
   stored subdocument's own `.DateTime` sub-field directly via `MongoElementRefExpression`, not the `$dateAdd`
   reconstruction `MongoDateTimeOffsetLocalExpression` performs for the local-time variants).
5. **Null/missing-date empirical coverage** — **attempted, found not constructible from valid LINQ, no test
   added.** A bare (non-conditional-guarded) date-part extraction over a nullable `DateTimeOffset?`/`DateTime?`
   requires `.Value` to compile in C#, and `.Value` throws client-side on a null nullable *before* the
   expression tree is ever built/translated — there is no way to get a genuinely-null value to reach the
   native date-part translator/renderer via valid EF LINQ syntax. The only server-reachable "null" scenario
   (a null-guarded conditional) is already covered by
   `Conditional_null_check_over_DateTimeOffset_matches_across_all_three_modes` in
   `NativeConditionalAndDateTimeTests.cs`. No artificial test was added.
6. **`MongoDateTimeOffsetLocalExpression` doc caveat** — added a `<para>` to the XML remarks stating (a)
   `.LocalDateTime` is treated identically to `.DateTime` (both use the stored `Offset`, not the executing
   machine's time zone), and (b) the reconstructed `DateTime` is millisecond-truncated relative to full tick
   precision — both deliberate, mirroring the pre-existing driver-bridge behavior.

All Finding-4 changes are in a single commit (see below).

## Full-suite regression results

- `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"` → **Build succeeded, 0 errors** (only pre-existing
  NU1903/CS8603 warnings, unrelated to this change).
- `dotnet test ... --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"` →
  **UnitTests: 993/993 passed. SpecificationTests: 7/7 passed** (no functional tests matched this filter, as
  expected — they're a separate assembly namespace).
- `dotnet test ... --filter "FullyQualifiedName~NativeSelectManyTests|FullyQualifiedName~NativeConditionalAndDateTimeTests"` →
  **144/144 passed** (FunctionalTests, real MongoDB via atlas-local container).

No regressions found anywhere. Only EF10 configuration was run (per the task's explicit test commands); EF8/EF9
were not separately verified in this pass — flagging this as the one thing not covered end-to-end.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoFieldPrefixRewriter.cs` (Finding 1)
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeProjectionBinder.cs` (Finding 2)
- `src/MongoDB.EntityFrameworkCore/Query/AGENTS.md` (Finding 3)
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoConditionalExpression.cs` (Finding 4.2)
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoQueryLanguageRenderer.cs` (Finding 4.3)
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoDateTimeOffsetLocalExpression.cs` (Finding 4.6)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeSelectManyTests.cs` (Finding 1 regression tests)
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoConditionalAndDateTimeTranslatorTests.cs` (Finding 4.1)
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoConditionalAndDatePartExpressionTests.cs` (Finding 4.2 test)
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeProjectionBinderConditionalAndDateTimeTests.cs` (Finding 4.4)

## Self-review

- Grepped the full diff for `EF-[0-9]+`: only pre-existing (unrelated, unmodified) context lines matched;
  the one `EF-449` reference that existed in the diff's *removed* line is gone from the added lines.
- Grepped for "EF-449" repo-wide: zero hits after the fix.
- No JIRA ticket numbers introduced anywhere in new code, comments, or (intended) commit messages.
- Verified `MongoElementRefExpression`'s constructor/property names (`Path`, `Type`) match what the Finding-1
  fix snippet assumed — they do.
- Verified the Finding-1 fix doesn't just mask the symptom: confirmed via RED/GREEN that reverting only the
  fix reproduces the exact throw, and restoring it fixes both new tests plus all 132 pre-existing
  `NativeSelectManyTests` with no collateral failures.
- Considered whether the conditional regression test's nested-in-arithmetic shape is a faithful reproduction
  of the finding's intent — yes: a bare top-level ternary in this binder's result-selector was already
  out-of-scope (declines at `IsArithmeticComputedLeaf`) both before and after this fix, so testing it wouldn't
  exercise `MongoFieldPrefixRewriter` at all; nesting it inside `+ 0m` is the minimal change needed to reach
  the actual code path the finding is about, and is exactly analogous to the existing
  `Explicit_result_selector_form_computed_arithmetic_leaf_goes_native` test's shape.

## Issues / concerns

- Finding 4 item 5: confirmed not constructible from valid LINQ (see above) — no test added, as instructed.
- Only EF10 build/test configuration was exercised (matching the task's given commands exactly); EF8/EF9 were
  not independently verified.
