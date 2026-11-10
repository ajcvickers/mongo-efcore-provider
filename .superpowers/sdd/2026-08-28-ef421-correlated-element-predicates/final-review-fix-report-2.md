# EF-421 final-review fix wave, round 2

Commit: `651468d` — "EF-421: confine bare-bool-arm MongoOuterFieldExpression to element scope, fixing a SelectMany truthiness regression"

## Issue 1 (CRITICAL, silent wrong-data) — bare-bool-arm truthiness gap

`MongoExpressionTranslator.TranslateNode`'s bare-boolean-member default arm (`src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs`, around line 789) constructed `MongoOuterFieldExpression` unconditionally whenever `boolIsOuter` was true, with no `_innerPrefix is null` gate — unlike the two `TranslateComparison` branches the previous fix wave had already gated. This meant a value-converted/non-default-`BsonRepresentation` bare outer bool reached through the PRE-EXISTING `NativeSelectManyBinder` correlated-inner-filter translator (`_innerPrefix` non-null) routed through `MongoOuterFieldExpression`, which is not query-dialect-renderable, so it fell to `{$expr: "$Field"}` — raw truthiness on the stored value.

**Fix:** gated the construction the same way as `TranslateComparison`:
```csharp
return boolIsOuter && _innerPrefix is null
    ? new MongoOuterFieldExpression(boolProp, boolPath!)
    : new MongoFieldExpression(boolProp, boolPath!);
```

## Issue 2 — `Not`-arm nullable-bool decline widening

`TranslateNode`'s `Not` case (line ~587) checked only `operand is MongoFieldExpression fieldExpr && fieldExpr.Property.IsNullable`, missing the `MongoOuterFieldExpression` sibling. Widened using the existing `TryGetBareFieldProperty` helper (added by the previous wave) so both node kinds are covered identically.

**Note on test coverage for this specific issue:** I verified empirically (a throwaway scratch unit test, since deleted) that EF Core's model builder throws `InvalidOperationException` when you attempt `.Property(x => x.SomeBool).IsRequired(false)` on a CLR-non-nullable `bool` property ("cannot be marked as nullable/optional because the type ... is not a nullable type"). Since `IProperty.ClrType` for a genuinely-nullable-CLR (`bool?`) property already fails the *earlier* `ClrType != typeof(bool)` check in the bare-bool default arm (returning null before the `Not` case's operand-nullable check is even reached — confirmed by the pre-existing unit tests `Bare_nullable_bool_field_reports_not_translatable` / `Not_over_nullable_bool_reports_not_translatable`), the scenario this widened check exists to catch (`IsNullable == true` with `ClrType == typeof(bool)`) is not constructible through a real EF model today. The fix is still correct and mirrors the established pattern exactly (same as the previous wave's now-dead-code-symmetric inner-scope check), but I did not add a dedicated regression test for it — it would be untestable without reflection-based IProperty mocking, and the task gave explicit latitude to skip this given the bare-bool case was the priority.

## Issue 3 — `RenderUnary` fall-to-`$expr` truthiness gap

`MongoQueryLanguageRenderer.RenderUnary`'s fall-to-`$expr` branch (`unary.Operand is not MongoFieldExpression field` — handles a `Not` operand with no query-dialect form) called `MongoAggregationExpressionRenderer.CanRender(unary.Operand)` directly on the *operand*, not re-entering `CanRender`'s own `MongoUnaryExpression{Not}` arm (which already carries the `AllFieldsDefaultSerialized` guard). A bare `MongoOuterFieldExpression`/`MongoFieldExpression` operand instead hit `CanRender`'s unconditional top arm (`=> true`), so this call site needed to repeat the guard itself.

**Fix:**
```csharp
if (MongoAggregationExpressionRenderer.CanRender(unary.Operand)
    && (!MongoExpressionTranslator.TryGetBareFieldProperty(unary.Operand, out _)
        || MongoExpressionTranslator.AllFieldsDefaultSerialized(unary.Operand)))
```
mirroring `CanRender`'s own `Not` arm exactly.

## Doc-comment corrections (step 7)

- `src/MongoDB.EntityFrameworkCore/Query/AGENTS.md` — "four sites" → "SIX sites", naming all six (`CanRender`'s `Not` arm, `CanRenderLogicalOperand`, `MongoAggregationExpressionRenderer`'s own private `RenderUnary` (EF-413), `CheckLogicalOperandSerialization`, `CheckBooleanRootSerialization`, and the new `MongoQueryLanguageRenderer.RenderUnary` fall-to-`$expr` guard from issue 3 above).
- `MongoExpressionTranslator.AllFieldsDefaultSerialized`'s remarks — "FOUR separate guards" → "SIX separate guards", with the same breakdown (five in `MongoAggregationExpressionRenderer`, one in `MongoQueryLanguageRenderer`).

Note: the task brief said the count should go to FIVE (matching the state after round 1). Since round 2's issue-3 fix adds a genuinely new, sixth call site, I updated both doc comments to SIX rather than five, to stay accurate as of this commit.

## Tests added

1. **Functional** (`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeSelectManyTests.cs`):
   - New `Owner.ConvertedFlag` (`bool`) property.
   - `Owned_correlated_beyond_outer_bare_converted_bool_uses_property_serializer_not_raw_truthiness` — raw-`BsonDocument`-seeded Owner/Item pair (Alice: `ConvertedFlag="False"`, Carol: `ConvertedFlag="True"`), model configures an explicit `bool<->string` converter (`v => v ? "True" : "False"`, matching the raw seed exactly — the built-in parameterless `HasConversion<string>()` actually maps to `"0"`/`"1"`, not `"True"`/`"False"`, which I discovered via a failed first attempt). Runs `o.Items.Where(i => o.ConvertedFlag)` under `NativeOnly` and asserts only Carol's item is returned.
   - **Verified as a genuine regression pin**: reverted the issue-1 source fix, rebuilt, and confirmed this test fails with exactly the predicted bug shape (both Alice's and Carol's items returned — 2 rows instead of 1). Restored the fix and confirmed green again.

2. **Unit** (`tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoQueryLanguageRendererTests.cs`):
   - New `Customer.ConvertedActive` property + `GetConvertedActiveProperty()` helper (explicit `bool<->string` converter).
   - `Not_over_a_bare_outer_field_with_non_default_serialization_declines_instead_of_expr_truthiness` — asserts `NativeTranslationNotSupportedException` for `Not(MongoOuterFieldExpression(ConvertedActive))`.
   - `Not_over_a_bare_outer_field_with_default_serialization_still_renders_via_expr` — control test proving the new guard doesn't over-decline an ordinarily-serialized outer bool.
   - **Verified as a genuine regression pin** the same way: reverted the issue-3 source fix, confirmed the first test fails ("No exception was thrown"), restored and confirmed green.

## Build & test results

- `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"` — succeeded, 0 errors.
- `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build` — **1013 passed, 0 failed, 0 skipped** (1011 baseline + 2 new).
- `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build` — **3000 passed, 0 failed, 52 skipped** (2999 baseline + 1 new).

Both meet or exceed the required baselines with zero regressions.

## Concerns

- Issue 2's widened check has no dedicated regression test (see note above) — it is a correct, defensive parity fix mirroring the established pattern, but the exact scenario it guards against (`IsNullable == true` with a non-nullable CLR `bool` `ClrType`) could not be constructed through EF Core's real model-building API (confirmed by a throwaway test that reproduced EF Core's own `InvalidOperationException` for this attempt). If this is considered insufficient, the alternative would be a lower-level test against `TryGetBareFieldProperty`/the `Not` case using a hand-mocked `IProperty`, which I judged not worth the added complexity given the explicit priority guidance.
- I only ran the EF10 configuration (build + both test suites), per the explicit instructions in this task; did not run `/test-all` across EF8/EF9/EF10.
