# EF-421 final-review fix wave

Commit: `ece85c7` — "EF-421: fix bare-outer-bool truthiness gap, confine MongoOuterFieldExpression to element scope, correct stale docs"

## Finding 1 (CRITICAL — silent wrong-data risk)

Added a shared helper `MongoExpressionTranslator.TryGetBareFieldProperty(MongoExpression, out IProperty?)` that
matches both `MongoFieldExpression` and `MongoOuterFieldExpression`, and used it at the three named sites plus a
fourth that also checked `MongoFieldExpression` only (`CheckLogicalOperandSerialization`):

- `MongoAggregationExpressionRenderer.CanRenderLogicalOperand`
- `MongoAggregationExpressionRenderer.CanRender`'s `Not` arm
- `MongoAggregationExpressionRenderer.RenderUnary`'s render-time throw
- `MongoAggregationExpressionRenderer.CheckLogicalOperandSerialization` (a fourth, previously unnamed site with
  the identical `MongoFieldExpression`-only bug, used by `RenderBinary`'s `&&`/`||` guard)

**A fifth gap was found during tracing and also fixed**, because the four sites above do not cover it: a
correlated quantifier/`Count(pred)` whose element predicate is a **bare** field with no `&&`/`||`/`Not` wrapper
at all (e.g. the reviewer's own repro, `Where(b => b.Posts.Any(p => b.Flag))`) reaches
`MongoAggregationExpressionRenderer.CanRender`'s own **top, unconditional** arm
(`MongoFieldExpression or MongoElementRefExpression or MongoOuterFieldExpression => true`) or the raw `Render`
dispatch for a bare field — neither of which was ever gated for truthiness, for either sibling node kind, before
or after this plan. Fixed by:
- a new render-time guard `MongoAggregationExpressionRenderer.CheckBooleanRootSerialization`, called from both
  `RenderQuantifier`'s `$map`/`in` construction and `RenderFilteredSize`'s `$filter`/`cond` construction (mirrors
  the existing `RenderUnary`/`CheckLogicalOperandSerialization` render-time-throw pattern);
- a translate-time decline in `MongoExpressionTranslator.TranslateNode`'s quantifier arm, so this specific shape
  declines cleanly (matching the branch's existing disposition) rather than reaching the render-time throw.

## Finding 2 (IMPORTANT — regression on shipped SelectMany behavior)

In `MongoExpressionTranslator.TranslateComparison`'s two `isOuter` branches, `MongoOuterFieldExpression`
construction is now confined to `leftIsOuter && _innerPrefix is null` / `rightIsOuter && _innerPrefix is null` —
i.e. only the NEW element-scope two-scope translators (`Count(pred)`/quantifier, both always constructed with
`innerPrefix: null`), never the pre-existing `SelectMany` two-scope translator (always constructed with a real
non-null `_lookup_<Nav>`-style prefix, confirmed by reading every construction call site). Falls back to a plain
`MongoFieldExpression` at document root otherwise — byte-identical to pre-plan `SelectMany` emission.

## Finding 3 (stale doc)

`MongoQuantifierExpression.cs`'s remarks corrected: it no longer claims the node "sidesteps
`MongoExpressionNegator` entirely" — it now describes the negator's actual `MongoQuantifierExpression` case
(De Morgan complement: flip `Any`/`All`, negate `ElementPredicate`) and points to the negator's own remarks.

## Finding 4 (factually wrong doc)

`MongoExpressionTranslator.SelfParam`'s doc corrected: `NativeSlotPopulator.PopulateNativeSlots` handles exactly
one `MethodCallExpression`/one operator per call and constructs one translator per call — it does **not** reuse
one instance across several slot operators. The real reason `SelfParam` is settable: the translator is
constructed generically at the top of the method, before the `if`/`else if` dispatch determines which of the
seven slot operators this call is for (and therefore before that operator's own lambda parameter is known); only
the one arm that runs sets `SelfParam` to its own parameter immediately before translating. Left the property
settable — verified this is the only call site and changing to constructor-only would require restructuring
`PopulateNativeSlots` itself, a bigger change than the reviewer asked for.

## Finding 5 (test coverage gap)

Added `NativeOwnedCollectionCorrelatedTests.Correlated_Any_and_All_over_a_missing_or_null_Posts_array_do_not_throw_and_match_the_oracle`,
seeding one document with `Posts` entirely absent and one with `Posts: null`, asserting a correlated `Any`
returns empty and a correlated `All` returns both rows (both under `NativeOnly`, i.e. actually exercising
`RenderQuantifier`'s `$ifNull` wrapper) — proving the mandatory `$ifNull` doesn't just exist in the rendered BSON
but actually prevents the hard server error and returns the LINQ-correct answer.

## Tests added

`NativeOwnedCollectionCorrelatedTests.cs` (+6 tests):
- `Correlated_Any_over_a_bare_value_converted_outer_bool_declines_instead_of_answering_wrong`
- `Correlated_All_over_a_bare_value_converted_outer_bool_declines_instead_of_answering_wrong`
- `Correlated_Any_over_an_AndAlso_wrapped_value_converted_outer_bool_declines_instead_of_answering_wrong`
- `Correlated_filtered_Count_predicate_over_a_bare_value_converted_outer_bool_declines_instead_of_answering_wrong`
- `Correlated_Any_and_All_over_a_missing_or_null_Posts_array_do_not_throw_and_match_the_oracle`

`NativeSelectManyTests.cs` (+1 test, +1 nullable property on `Owner`):
- `Correlated_relational_operator_over_a_nullable_outer_property_still_excludes_null_rows`

**Verified all 5 hazard-repro tests actually fail without the fix** (temporarily `git stash`ed just the two
`src/` files, reran the targeted filter — all 5 failed with either "no exception thrown" or a doubled/wrong
result set; the missing/null-array test passed regardless, as expected since it's unrelated to Findings 1/2),
then restored the fix and reran to confirm green.

## Test run output (with the fix in place, EF10)

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
Build succeeded. 0 Error(s)

dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build
Passed!  - Failed: 0, Passed: 2999, Skipped: 52, Total: 3051, Duration: 1m 18s

dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build
Passed!  - Failed: 0, Passed: 1011, Skipped: 0, Total: 1011, Duration: 917ms

dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests -c "Debug EF10" --no-build
Passed!  - Failed: 0, Passed: 4599, Skipped: 14, Total: 4613, Duration: 43s
```

Zero regressions; skip counts match pre-existing env-gated (encryption/Atlas) skips.

## Concerns

- Finding 1's fix went beyond the three (really four) sites literally named in the brief: tracing the exact
  named repro (`Where(b => b.Posts.Any(p => b.Flag))`) showed those four sites alone do not close it, because a
  **bare** (unwrapped) element predicate reaches `CanRender`'s own unconditional top arm / the raw `Render`
  dispatch, neither of which any existing guard covered — for `MongoFieldExpression` either, not just the new
  `MongoOuterFieldExpression` sibling. This is flagged here for visibility even though the fix is a straight
  application of the same established pattern (render-time throw + translate-time decline, mirroring
  `RenderUnary`/`CheckLogicalOperandSerialization`), not a new mechanism.
- Finding 4: left `SelfParam` settable rather than constructor-only. Confirmed via grep this is the only call
  site that sets it post-construction, but did not restructure `NativeSlotPopulator.PopulateNativeSlots` to
  construct a translator per-arm, since that's a bigger structural change than the brief asked for.
