# Task 1 Report: Generalize NativeSelectManyBinder's scope-rerooting walker

## Summary

Successfully extracted the private `TryResolveScopeDepth` method and `ScopeRerootingVisitor` class from `NativeSelectManyBinder.cs` into a new public-to-the-assembly `MongoTransparentScopeResolver` utility, generalized to accept a caller-supplied `hopNames` list instead of hardcoded "Outer"/"Inner" literals. This is a pure behavior-preserving refactor that enables future Join binders to reuse the identical scope-resolution logic.

## What Was Implemented

1. **New file:** `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoTransparentScopeResolver.cs`
   - Static `TryResolveScopeDepth` method that generalizes the scope-depth resolution to accept arbitrary hop names via `IReadOnlyList<string> hopNames` parameter
   - Inner sealed class `ScopeRerootingVisitor` that walks expression trees and rewrites scope-rooted member accesses onto synthetic scope parameters
   - Both types internal (assembly-scoped visibility)

2. **Refactored:** `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSelectManyBinder.cs`
   - Removed old private `TryResolveScopeDepth` method (lines 892-921)
   - Removed old private `ScopeRerootingVisitor` class (lines 860-878)
   - Updated all four call sites to use `MongoTransparentScopeResolver`:
     - Line ~634: `TryBindTransparentIdentifierProjection` member loop
     - Line ~778: `TryTranslateCrossScopeComputedLeaf` leaf classification
     - Line ~832: `TryTranslateScopedSubtree` subtree re-rooting
     - Line ~868: Internal `ScopeRerootingVisitor.VisitMember` now calls the shared resolver
   - Updated XML documentation references to point to new location
   - All call sites now pass `hopNames: ["Outer", "Inner"]` explicitly, matching the original hardcoded behavior

3. **Test file:** `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoTransparentScopeResolverTests.cs`
   - Three test cases covering:
     - Root scope resolution for pure "Outer" chain matching source count
     - Inner scope resolution for trailing "Inner" hop
     - Generalized hop-names support (tested with identical "Outer"/"Inner" but caller-supplied)

## Testing Results

### Unit Tests
- **New tests (MongoTransparentScopeResolverTests):** 3 tests, all PASS ✓
- **Regression tests (NativeSelectManyBinderTests):** 53 tests, all PASS ✓

### Functional Tests
- **NativeSelectManyTests:** 132 tests, all PASS ✓

### Build Results
- **EF8 configuration:** Build succeeded ✓
- **EF9 configuration:** Build succeeded ✓
- **EF10 configuration:** Build succeeded ✓
- **Warnings:** Only SSH.NET package vulnerability warning (unrelated to changes)

## Files Changed

1. `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoTransparentScopeResolver.cs` — **CREATED** (109 lines)
2. `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSelectManyBinder.cs` — **MODIFIED** (-74 lines, +20 lines = -54 net)
3. `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoTransparentScopeResolverTests.cs` — **CREATED** (72 lines)

Total: 181 insertions, 74 deletions

## Self-Review Findings

✓ **Completeness:** All four call sites updated; old private members fully deleted (not just superseded)

✓ **Code quality:**
   - Matches existing codebase patterns (internal sealed class, ExpressionVisitor inheritance)
   - Preserves exact behavior: SelectMany still passes `["Outer", "Inner"]` as hopNames
   - Clear parameter naming and comprehensive XML documentation

✓ **Discipline:** No scope creep; pure extraction with zero behavior change, ready for Join binder to consume

✓ **Testing:** Tests assert expected behavior; new test validates arbitrary hop-names support; regression tests confirm identical MQL output

✓ **Build & Warnings:** Clean builds across all EF versions; XML documentation references updated correctly

## Commit

```
ded5240 EF-392: extract MongoTransparentScopeResolver from NativeSelectManyBinder for reuse by Join
```

## Notes

The refactor is transparent to callers within `NativeSelectManyBinder.cs` — behavior is 100% identical. The `hopNames` parameter is now explicit at all call sites, making it clear that the resolution algorithm generalizes beyond the hardcoded "Outer"/"Inner" pair. This sets up subsequent Join-translation tasks to reuse the resolver with custom scope-name lists appropriate to Join's result selector shape.

---

## Review Findings & Fixes

**Important Finding:** The original `Supports_arbitrary_hop_names_for_a_join_scope` test was asserting its purpose without verifying it — it passed `hopNames: ["Outer", "Inner"]` identical to the other two tests, not demonstrating genuine data-driven hop names. The brief's own suggested version (`hopNames: ["Outer"]`, `sourceCount: 1`) would crash with `IndexOutOfRangeException` against the implementation (accessing `hopNames[1]` unconditionally). 

**Fix Applied:** Replaced the test with a new fixture type `JoinResult` carrying genuinely different member names ("Left"/"Right"), and the updated test now calls the resolver with `hopNames: ["Left", "Right"]` to prove hop-names are data-driven, not implicitly hardcoded. The test asserts both root and inner scope resolution under the custom hop-name set.

**Minor Findings:**

1. **Stale comment in NativeSelectManyBinder.cs (line 774):** Plain `//` comment referenced `ScopeRerootingVisitor.CrossScope` without the full qualification. **Fixed:** Updated to `MongoTransparentScopeResolver.ScopeRerootingVisitor.CrossScope` for consistency with the XML doc comments updated in the initial commit.

2. **Over-stated contract in MongoTransparentScopeResolver.cs:** The `hopNames` parameter documentation claimed "arbitrary ordered list of hop member names," overstating the actual requirement. **Fixed:** Tightened the XML doc to explicitly state that exactly two elements are required (index 0 = outer/root-ward hop, index 1 = inner/leaf-ward hop).

### Test Results After Fixes

All tests pass with the corrected hop-names test:

```bash
# New unit test (MongoTransparentScopeResolverTests)
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoTransparentScopeResolverTests"
# Result: 3 PASS
```

```bash
# Regression tests (NativeSelectManyBinderTests)
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeSelectManyBinderTests"
# Result: 53 PASS
```

```bash
# Functional tests (NativeSelectManyTests)
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeSelectManyTests"
# Result: 132 PASS
```

### Files Changed in Fix Commit

1. `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoTransparentScopeResolver.cs` — **MODIFIED:** Tightened XML doc for `TryResolveScopeDepth` to explicitly require exactly 2 hop names
2. `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSelectManyBinder.cs` — **MODIFIED:** Qualified stale `//` comment to new location
3. `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoTransparentScopeResolverTests.cs` — **MODIFIED:** Replaced `Supports_arbitrary_hop_names_for_a_join_scope` test with real demonstration using `JoinResult` fixture with "Left"/"Right" hops
