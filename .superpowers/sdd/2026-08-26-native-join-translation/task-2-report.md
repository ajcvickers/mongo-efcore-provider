# Task 2 Report: Add a join-scope marker to MongoSelectDefinition

## Summary

Completed Task 2 of the 8-task plan for native MongoDB aggregation-pipeline translation of LINQ Join/LeftJoin operators. Added plumbing for tracking join scopes on `MongoSelectDefinition` without implementing the logic that populates them (that's Task 3+).

## Implementation

### 1. Created `MongoJoinScope.cs`
- **Location:** `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs`
- **Type:** Internal sealed record class
- **Properties:**
  - `OuterMemberName`: Result-selector member name capturing the whole outer entity (nullable)
  - `InnerMemberName`: Result-selector member name capturing the whole inner entity (nullable)
  - `OuterEntityType`: The outer side's entity type
  - `InnerEntityType`: The inner side's entity type
  - `InnerPrefix`: The `$lookup` alias prefix (e.g., `_lookup_Orders`)
  - `IsLeftOuter`: Boolean flag for left-outer vs. inner join
- **Documentation:** Comprehensive XML comments explaining the scope-tracking purpose for later operator binding

### 2. Extended `MongoSelectDefinition.cs`
- **Modified `HasTerminalOperator` property (line 394):**
  - Before: `IsGroupBy || IsDistinct || IsSetOp || Grouping != null || UnwindSources.Count > 0`
  - After: `IsGroupBy || IsDistinct || IsSetOp || Grouping != null || UnwindSources.Count > 0 || JoinScope != null`
  - Effect: A query with a native join scope now correctly reports itself as having a terminal operator

- **Added `JoinScope` property (after line 492):**
  - Type: `internal MongoJoinScope? JoinScope { get; set; }`
  - Placement: Immediately after `IsSingleReferenceUnwindTerminalOnly`, before `_isGroupByFallbackUnsafe` field
  - Documentation: XML comments explaining when it's set and what it tracks

### 3. Created Unit Test
- **Location:** `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoSelectDefinitionJoinScopeTests.cs`
- **Test Class:** `MongoSelectDefinitionJoinScopeTests`
- **Single Test:** `HasTerminalOperator_is_true_once_a_join_scope_is_set`
  - Sets up a `MongoSelectDefinition` with `HasTerminalOperator == false`
  - Creates two entity types via a real EF Core model
  - Sets `JoinScope` to a new `MongoJoinScope` instance
  - Asserts `HasTerminalOperator` becomes `true`
  - Test infrastructure: Uses `SingleEntityDbContext.Create<T>()` to create real entity types without mocking

## Testing

### Test Results
- **Unit test status:** ✓ PASS
  - Test runs under EF10 configuration
  - Execution time: ~5ms
  - Assertion: `HasTerminalOperator` transitions from false to true when `JoinScope` is set

- **Build verification:**
  - ✓ `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"` — succeeded
  - ✓ `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"` — succeeded
  - ✓ `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"` — succeeded
  - No warnings-as-errors violations
  - Total build warnings: 2 (both unrelated SSH.NET package vulnerability notices)

## Files Changed

1. **Created:**
   - `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs` (56 lines)
   - `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoSelectDefinitionJoinScopeTests.cs` (53 lines)

2. **Modified:**
   - `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs` (2 edits, 8 lines added):
     - Line 394: Extended `HasTerminalOperator` property
     - Lines 493-497: Added `JoinScope` property and XML documentation

## Self-Review Findings

### Code Quality
✓ File structure matches existing codebase patterns:
  - Apache 2.0 license header on all new files
  - Correct namespace hierarchies
  - XML documentation follows project conventions

✓ Implementation correctness:
  - `MongoJoinScope` is a sealed record matching the brief specification exactly
  - Property accessors are init-only (via constructor record syntax)
  - `HasTerminalOperator` logic now includes `JoinScope != null` check

✓ Test implementation:
  - Uses real EF Core entity types (not mocks) via `SingleEntityDbContext`
  - Follows existing test patterns from the same directory
  - Clear, focused test with minimal setup
  - Single assertion on the key behavior change

✓ No scope creep:
  - No code that *populates* `JoinScope` (reserved for Task 3)
  - No changes to any join-related translators or binders
  - Pure additive plumbing: new file, one new property, one extended condition

### Discipline Checks
✓ The `HasTerminalOperator` extension is minimal and correct:
  - Exactly mirrors the pattern of existing terminal-operator checks (`Grouping != null`, `UnwindSources.Count > 0`)
  - Maintains the conjunctive semantics (all conditions with `||`)
  - No unintended side effects on other terminal-operator logic

✓ The `JoinScope` property placement:
  - Logically grouped with other scope-related properties (`UnwindSources`, `UnwindSource`)
  - Placed before internal implementation fields to maintain logical ordering

## Concerns or Issues

**No blockers or concerns.**

Minor non-issue: The XML comment in `MongoJoinScope.cs` references `MongoTransparentScopeResolver` which exists but is in a different namespace (`MongoDB.EntityFrameworkCore.Query.NativeTranslation`). The reference is semantic/documentation only and doesn't affect functionality. IntelliSense correctly resolves it.

## Verification

✓ Concurrent changes check: No unexpected edits to `MongoSelectDefinition.cs` since task start
✓ All three EF versions build cleanly
✓ Test is RED before the implementation, GREEN after
✓ Exact commit message per brief
✓ Exact file list per brief
✓ No population of `JoinScope` (that's Task 3)

---

**Commit SHA:** `3f21dd9`  
**Status:** Ready for Task 3 (which will populate `JoinScope` in join translators)
