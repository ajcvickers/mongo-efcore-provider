# Task 4 Report: `MongoQuantifierExpression` node type + rendering

## Summary

Implemented `MongoQuantifierExpression` node type and its rendering as `$anyElementTrue`/`$allElementsTrue` over a `$map`. This node represents correlated quantifiers (`Any`/`All`) over owned collections where the element predicate references the enclosing entity. The implementation follows the established pattern for aggregation-expression-only nodes (no query-dialect form).

## Implementation Details

### Files Created
- **`src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQuantifierExpression.cs`** — New expression node type
  - Constructor: `MongoQuantifierExpression(MongoElementRefExpression arrayPath, MongoExpression elementPredicate, MongoExpressionTranslator.MongoQuantifierKind kind)`
  - Properties: `ArrayPath`, `ElementPredicate`, `Kind`
  - Type: always `bool`
  - Renders as `{ $anyElementTrue: { $map: … } }` or `{ $allElementsTrue: { $map: … } }`

### Files Modified

1. **`src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.MethodCalls.cs`**
   - Changed `MongoQuantifierKind` from `private enum` to `internal enum` to make it accessible from the `Expressions` namespace

2. **`src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs`**
   - Added `Render` switch arm for `MongoQuantifierExpression` (line 88)
   - Added `CanRender` switch arm for `MongoQuantifierExpression` (line 155)
   - Added `RenderQuantifier` method implementing the core rendering logic:
     - Uses nesting-variable convention for nested quantifiers (same pattern as `RenderFilteredSize`)
     - Wraps array path in `$ifNull` (mandatory for server safety)
     - Renders to `$anyElementTrue` or `$allElementsTrue` based on `Kind`

3. **`src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoQueryLanguageRenderer.cs`**
   - Added `MongoQuantifierExpression => false` to `IsQueryDialectRenderable` (aggregation-expression-only, no query-dialect form)

4. **`tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs`**
   - Added 3 test cases:
     - `MongoQuantifierExpression_Any_renders_as_anyElementTrue_over_map` — verifies `Any` rendering
     - `MongoQuantifierExpression_All_renders_as_allElementsTrue_over_map` — verifies `All` rendering
     - `MongoQuantifierExpression_can_render` — verifies `CanRender` acceptance

## Test Results

### RED (before implementation)
Tests failed at compile time due to missing `MongoQuantifierExpression` type.

### GREEN (after implementation)
```
Passed!  - Failed:     0, Passed:  3, Skipped:     0, Total:     3
```

All 3 new tests pass.

### Full Unit Test Suite
```
Passed!  - Failed:     0, Passed:  1005, Skipped:     0, Total:  1005
```

No regressions detected. All existing tests pass.

## Self-Review

### Completeness ✓
- Node type fully implemented with proper XML documentation
- All required rendering arms added (Render, CanRender, IsQueryDialectRenderable)
- Tests verify core functionality
- No construction of the node yet (per scope — Task 5 will construct it)

### Quality ✓
- Follows established patterns:
  - BOM preserved
  - `internal sealed class` for expression node
  - XML doc comments in established style
  - Plain xUnit `Assert.*` in tests (no FluentAssertions)
  - Nesting variable convention matches `RenderFilteredSize`
  - Remarks document the exact semantics and safety invariants

### Code Organization ✓
- Node in `Query/Expressions/` (correct namespace)
- Rendering methods added near similar methods (`RenderFilteredSize`, etc.)
- Consistent with other aggregation-expression-only nodes (`MongoConditionalExpression`, `MongoDatePartExpression`)

### Discipline ✓
- No scope creep — node type + rendering only
- No construction logic (that's Task 5)
- No query-dialect form (correct — correlated quantifiers need `$map` which is aggregation-only)

## Issues/Concerns

**None.** All requirements met, tests passing, no regressions.

## Key Design Points

1. **Aggregation-expression-only**: Unlike `MongoElemMatchExpression` which has a query-dialect form, `MongoQuantifierExpression` has NO query-dialect analogue. The `$map` construct used to reference the enclosing document requires aggregation expressions.

2. **No negation needed for `All`**: Unlike the uncorrelated `All` path (which uses a negated `$elemMatch` over the complement), the correlated path uses `$allElementsTrue` directly — no negation, no negator invocation.

3. **Nesting safety**: Variable names are derived from the enclosing context (`"e"`, `"ee"`, `"eee"`, etc.) to ensure nested quantifiers/filters never collide.

4. **Null/missing array safety**: `$ifNull` wrapping is mandatory — `$map` over missing/null arrays is a hard server error. Empty array `[]` yields the correct LINQ semantics (`false` for `Any`, `true` for `All`).

## Commit

SHA: `9651bc7`  
Message: `EF-421: add MongoQuantifierExpression node and $anyElementTrue/$allElementsTrue rendering`

Changed files:
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQuantifierExpression.cs` (new)
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.MethodCalls.cs` (1 line: `private` → `internal`)
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs` (32 lines added)
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoQueryLanguageRenderer.cs` (1 line added)
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs` (52 lines added)
