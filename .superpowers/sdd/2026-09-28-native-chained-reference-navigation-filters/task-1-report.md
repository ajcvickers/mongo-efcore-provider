# Task 1 report: Chained-scope `Where` over a non-root level

## Summary

Implemented exactly per the brief. Added:
- `NativeJoinScopeTranslator.TryTranslateSingleScopePredicate` (public), backed by a new private
  `TryTranslateSingleScopeCore`, which `TryTranslateSingleScope` now also delegates to (its public signature and
  XML doc unchanged).
- `NativeSlotPopulator.TryTranslateChainedScopeConjunction` (private), placed directly after
  `TryTranslateJoinScopeConjunction`.
- A new `Where` arm in `PopulateNativeSlots`, inserted directly after the existing chained root-only arm
  (`TryTranslateRootScopeOnly`) and before the reference-Include null-check arm.
- Five new tests appended to `JoinScopeWhereSlotPopulationTests`.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeTranslator.cs`
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs`
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/JoinScopeWhereSlotPopulationTests.cs`

## TDD evidence

### RED

Command:
```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~JoinScopeWhereSlotPopulationTests"
```
Result: build succeeded; 3 failures, 11 passed:
```
Failed MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation.JoinScopeWhereSlotPopulationTests.Where_conjunction_over_root_and_second_level_of_chained_join_populates_one_post_join_match
  Assert.False() Failure
  Expected: False
  Actual:   True
Failed MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation.JoinScopeWhereSlotPopulationTests.Where_reading_second_level_inner_scope_of_chained_join_populates_predicate_in_post_join_ops
  Assert.False() Failure
  Expected: False
  Actual:   True
Failed MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation.JoinScopeWhereSlotPopulationTests.Where_reading_first_level_inner_scope_of_chained_join_populates_predicate_in_post_join_ops
  Assert.False() Failure
  Expected: False
  Actual:   True

Failed!  - Failed: 3, Passed: 11, Skipped: 0, Total: 14
```
This matches the brief's expectation exactly: the three `…populates…` tests fail on `HasUnsupportedOperator`
being true (no chained-inner arm existed yet to translate them), and the two `…declines` tests already pass
(there was nothing to accidentally translate them with).

### GREEN

Command:
```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~JoinScope"
```
Result:
```
Passed!  - Failed: 0, Passed: 85, Skipped: 0, Total: 85, Duration: 256 ms
```
All `JoinScope*SlotPopulationTests` pass, including the pre-existing depth-1/root-only tests (no regression).

## Full-suite result

```
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build
```
```
Passed!  - Failed: 0, Passed: 1414, Skipped: 0, Total: 1414, Duration: 1 s
```

## Self-review

- **BOM preservation**: confirmed via `xxd` before and after — `NativeSlotPopulator.cs` still has the UTF-8 BOM
  (`ef bb bf`); `NativeJoinScopeTranslator.cs` has no BOM (contrary to the brief's claim it has one — checked
  with `xxd`, it starts `2f 2a` = `/*`, no BOM). Both preserved as-is by the Edit tool; the test file (no BOM)
  also preserved.
- **`[NotNullWhen(true)]`**: applied on `TryTranslateSingleScopePredicate`'s and
  `TryTranslateChainedScopeConjunction`'s `out` result parameters, matching neighboring methods.
- **No mutate-then-decline**: `TryTranslateChainedScopeConjunction` accumulates into a local `translated` list
  and only assigns the `out conjuncts` parameter after every gate (including the final `readsNonRootScope`
  check) passes. Any early `return false` leaves `conjuncts` at its initial `null`.
- **Scope resolution**: routed entirely through `TryTranslateSingleScopePredicate`/`TryMatchScopeNullCheck`, both
  of which resolve by parameter identity / hop-chain walk (`TryRerootToSingleScope`/`TryRerootToBareScope`), never
  by member name or CLR type.
- **Gates not loosened**: `HasUnconfirmedCandidateJoin`, `IsSingleEligibleNativeJoinScope`'s paging gates, and
  the post-confirmed-join guard (`HasConfirmedJoinLookup && IsSevenSlotOperator` check earlier in
  `PopulateNativeSlots`) are untouched.
- **Public API**: `TryTranslateSingleScope`'s signature and XML doc are unchanged; the new
  `TryTranslateSingleScopePredicate` is a net-new public method (internal-facing since `NativeJoinScopeTranslator`
  itself is `internal static`), consistent with existing sibling methods on that class.
- **Naming/idiom**: matches the brief's exact interface names, comment density, and the surrounding file's use of
  a `Stack<Expression>`-based flattening loop (mirrors `TryTranslateJoinScopeConjunction` closely, as intended —
  the two are structurally parallel by design, one for single-level scopes, one for chained scopes).
- **YAGNI**: no extra abstraction added beyond what the brief specifies; the new arm is inserted at exactly the
  specified point, no reordering of existing arms.
- Test output is pristine (no warnings beyond the pre-existing SSH.NET NU1903 advisories unrelated to this change).

## Concerns

None. Implementation matches the brief verbatim, all RED/GREEN transitions occurred exactly as predicted, and the
full unit-test suite (1414 tests) passes with no regressions.
