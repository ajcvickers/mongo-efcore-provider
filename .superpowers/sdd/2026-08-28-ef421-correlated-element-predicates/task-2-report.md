# Task 2 Report: MongoOuterFieldExpression Node Type + Rendering

## Summary

Successfully implemented Task 2 of the EF-421 plan: added a new dialect-agnostic expression node `MongoOuterFieldExpression` that represents a field reference always resolving at document root, even when rendered inside `$filter`/`$map` scope. This node is a deliberate sibling to `MongoFieldExpression` rather than a flag, following the codebase's established pattern for nodes needing different handling at multiple sites (per `MongoFilteredSizeExpression` remarks).

## Implementation

### 1. Node Type
- **File:** Created `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoOuterFieldExpression.cs`
- **Design:** Internal sealed class inheriting from `MongoExpression` with two properties:
  - `Property: IProperty` — EF Core property metadata
  - `ElementName: string` — Document element name relative to outer document root
- **Type Contract:** `Type => Property.ClrType`
- **Documentation:** Comprehensive XML doc comments explaining the sibling vs. flag rationale and the two-scope translator context

### 2. Rendering Support

#### MongoAggregationExpressionRenderer.cs
- **Render arm (line 59):** Added pattern-match case that renders via `FieldRef(outer.ElementName, elementVariable: null)`, deliberately passing `null` for `elementVariable` to force document-root rendering regardless of scope
- **CanRender arm (line 121):** Updated pattern to include `MongoOuterFieldExpression` alongside `MongoFieldExpression` and `MongoElementRefExpression`

#### MongoQueryLanguageRenderer.cs
- **IsQueryDialectRenderable arm (line 483):** Added explicit return `false` with comment explaining aggregation-expression-only nature and cross-referencing established pattern from `MongoConditionalExpression`/`MongoDatePartExpression`

#### MongoExpressionTranslator.cs
- **AllFieldsDefaultSerialized arm (line 365):** Added correctness check mirroring the `MongoFieldExpression` arm, delegating to `NativeGroupByBinder.HasDefaultKeySerialization` over the outer property

### 3. Test Suite
- **File:** Added 3 new test methods to `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs`
- **Tests:**
  1. `MongoOuterFieldExpression_renders_at_document_root_with_no_element_variable()` — baseline case, renders as `"$Status"`
  2. `MongoOuterFieldExpression_renders_at_document_root_even_inside_a_filter_scope()` — **core test** demonstrating the key semantic: even with `elementVariable: "e"` in scope, still renders as `"$Status"`, NOT `"$$e.Status"`
  3. `MongoOuterFieldExpression_can_render()` — affirms `CanRender` returns `true`

## TDD Evidence

### RED Step
**Command:** 
```bash
dotnet build tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10"
```
**Output (excerpt):**
```
error CS0246: The type or namespace name 'MongoOuterFieldExpression' could not be found
  ... (3 error instances for lines 483, 496, 507)
```
**Evidence:** Tests fail to compile; type does not exist.

### GREEN Step
**Command after implementation:**
```bash
dotnet build tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10"
```
**Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**Test execution:**
```bash
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoOuterFieldExpression"
```
**Output:**
```
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 13 ms
```

**Full unit test suite:**
```bash
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build
```
**Output:**
```
Passed!  - Failed:     0, Passed:  1001, Skipped:     0, Total:  1001, Duration: 879 ms
```

## Files Changed

1. **Created:** `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoOuterFieldExpression.cs` (71 lines)
   - New node type with full XML documentation
   
2. **Modified:** `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs`
   - Added Render case with comment explaining document-root-forcing semantics
   - Updated CanRender pattern guard to include `MongoOuterFieldExpression`
   
3. **Modified:** `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoQueryLanguageRenderer.cs`
   - Added explicit `IsQueryDialectRenderable` arm returning `false` with explanatory comment
   
4. **Modified:** `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs`
   - Added `AllFieldsDefaultSerialized` arm mirroring `MongoFieldExpression` pattern
   
5. **Modified:** `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs`
   - Added 3 unit tests (40 lines of test code)

## Self-Review Findings

### Code Quality ✓
- **Consistency:** Follows established patterns (`MongoFieldExpression`/`MongoElementRefExpression` pairing, sealed internal class, XML doc style)
- **Naming:** Clear semantics (`MongoOuterFieldExpression`, `ElementName`)
- **Comments:** Comprehensive remarks explaining two-scope context and sibling-vs-flag rationale
- **Test pattern:** Matches existing renderer test file structure with `GetProperty<T>()` helper and `PlaceholderTable`

### Correctness ✓
- **Render semantics:** Correctly forces document-root rendering by explicitly passing `elementVariable: null` to `FieldRef`
- **Scope isolation:** CanRender/IsQueryDialectRenderable arms properly aligned (aggregation-expression-only, no query-dialect form)
- **Serialization guard:** `AllFieldsDefaultSerialized` arm uses identical logic to `MongoFieldExpression` via the shared `NativeGroupByBinder.HasDefaultKeySerialization` utility
- **Test coverage:** All three key cases tested:
  - No element variable in scope (baseline)
  - Element variable in scope (semantic core test)
  - CanRender classification

### Discipline ✓
- **Scope containment:** No scope creep; all four rendering arms added exactly where the brief specified
- **No breaking changes:** Internal node type, pre-construction (Task 3 builds upon this)
- **Multi-version compat:** No EF8/EF9/EF10 conditionals needed (generic enough for all versions)

### Testing ✓
- **Coverage:** 3/3 new tests green; 1001/1001 unit tests green (no regressions)
- **Real behavior proven:** Second test specifically validates the key semantic difference (scope-agnostic rendering) vs. `MongoFieldExpression`'s scope-aware rendering

## Commit

```
4dd7491 EF-421: add MongoOuterFieldExpression node and rendering
```

Full commit message captures the purpose, four rendering arms, and test count.

## Notes

- **Not yet used:** This node is constructed nowhere in this task (Task 1 added the `SelfParam` plumbing; Task 3 adds the translator logic that *constructs* this node). Task 2 exists purely to add the node type and prove its rendering works in isolation, hence the unit-test-focused scope.
- **Scope accuracy:** Test file choice was verified—`MongoAggregationExpressionRendererTests.cs` already existed, so new tests were added inline per the brief's Step 1 instruction.
- **No integration tests needed:** This is an internal expression node with no external effect until constructed by Task 3; the unit tests isolate its rendering behavior completely.

## Status

✅ **COMPLETE**

All TDD steps verified, all tests passing, no regressions, commit created.
