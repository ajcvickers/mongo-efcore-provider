# Task 3 Implementation Report: `TryResolveMember` reports `isOuter`; correlated `Count(pred)` goes native

## Summary

Implemented the task exactly as specified in the brief for every listed call site, plus two
necessary fixes discovered during implementation that the brief's exact snippets did not
account for (both documented below with full reasoning). All steps in the brief (1–11) were
executed in order; the new FunctionalTest passes, the deliberately-deferred `Any` test in the
same file still fails as documented, and the full FunctionalTests/UnitTests/SpecificationTests
suites are green with zero unexpected regressions.

## Implementation, call site by call site

### `MongoExpressionTranslator.Members.cs`

- **`TryResolveMember`** (Step 3): added `out bool isOuter`, initialized `false`, and the existing
  local `var isOuter = ...` computation became the out-parameter assignment (unchanged logic).
- **`TryResolveOwnedFieldPath`** (Step 4): mechanical signature change only — accepts and always
  sets `out bool isOuter = false`; its own two-scope decline is untouched (Task 6's scope).

### `MongoExpressionTranslator.cs` — the 7 (+2 mechanical) call sites (Step 5)

- **5a `TryTranslateDateTimeMember`**: declines (`return false`) when the receiver resolves `isOuter`.
- **5b array-`Contains` receiver**: added `&& !arrayIsOuter` to the pattern-match guard — declines cleanly.
- **5c collection-membership item**: declines when the item resolves `isOuter`.
- **5d regex receiver**: declines when the receiver resolves `isOuter`.
- **5e `HasValue`**: declines when the nullable receiver resolves `isOuter`.
- **5f bare boolean member default**: **builds** `MongoOuterFieldExpression` when `boolIsOuter` (the
  brief's one exception among the "declining" sites — a bare correlated bool costs nothing extra).
- **5g/5h `TranslateComparison`'s two branches**: both now build `MongoOuterFieldExpression` for the
  member side when `isOuter`, wrapped through a local `MongoExpression leftField`/`rightField`
  variable feeding the existing `MongoBinaryExpression` construction — no change to the branch's
  operator-mapping/value-translation logic.
- **5i `TranslateOperand`'s own call site**: builds `MongoOuterFieldExpression` on `operandIsOuter` —
  this is the arithmetic/field-to-field operand path, so it needed the real branch, not a decline
  (matches the brief's explicit callout).
- **Step 6 `TryTranslateField`** (sort-key path, mechanical): declines whenever `isOuter` is true (a
  sort key is always built by the root translator in practice, so this is a defense-in-depth guard,
  not a live path today).
- **Step 7 `Count(pred)` arm**: replaced the unconditional `ReferencesEnclosingScope(...) => return null`
  guard with the real branch — a free parameter that `ReferenceEquals`-matches `SelfParam` now builds
  a two-scope child translator (`outerParam: SelfParam, outerEntityType: _entityType`) instead of
  declining; anything else (no `SelfParam`, or a different free parameter) still declines exactly as
  before.

## Necessary fixes beyond the brief's literal text

The brief's exact code, applied literally, does not make its own required test pass. Both issues
were root-caused, fixed minimally, and verified — I did not treat "the brief said so" as
authorization to skip verifying the result was correct.

### 1. `NativeSlotPopulator` never set `SelfParam` (missing wiring, not in the brief's file list)

`MongoExpressionTranslator.SelfParam`'s own doc comment (written in Task 1) says: *"SETTABLE...
because `NativeSlotPopulator` reuses one translator instance across several slot operators... that
call site sets this field for the duration of each arm."* This was aspirational — Task 1's brief
explicitly deferred it, and no later task's brief filled it in. Without it, `SelfParam` is always
`null` for the `Where` predicate's translator, so the new `Count(pred)` correlation branch could
never fire (`SelfParam is null || !ReferenceEquals(...)` always takes the decline path) — the RED
test stayed RED after implementing every literal edit in Steps 3–7.

**Fix:** added `translator.SelfParam = predicate.Parameters[0];` (and the equivalent for the
`OrderBy`/`ThenBy` key-selector parameter, for consistency with the documented design and because a
computed sort key can itself reach the `Count(pred)` branch) in `NativeSlotPopulator.PopulateNativeSlots`.
This is exactly what Task 1's own documentation already committed to; it was simply never wired up.

### 2. The two-scope constructor's `innerPrefix: ""` (brief's exact snippet) produces invalid MQL

The brief's Step 7 code passes `innerPrefix: ""` to the two-scope constructor. Unlike
`NativeSelectManyBinder`'s existing two-scope usage — where `innerPrefix` is a real document-root
dotted prefix for a `$lookup`/unwind scope (e.g. `"_lookup_Tags"`) — the `Count(pred)` element scope
is rendered via `$filter`'s own `as`-variable addressing (`MongoAggregationExpressionRenderer`
threads an `elementVariable` and renders `MongoFieldExpression` as `"$$" + elementVariable + "." +
path"`). An empty-string prefix is not "no prefix": `TryResolveMember`'s
`if (!isOuter && _innerPrefix is not null) fieldPath = _innerPrefix + "." + fieldPath;` treats `""`
as a real (empty) prefix, producing a leading-dot path segment (`"$$e..Title"`), which MongoDB
rejects: `Command aggregate failed: ... FieldPath field names may not be empty strings.` — confirmed
live (this was the RED failure after fixing issue #1 above).

**Fix:** widened the two-scope constructor's `innerPrefix` parameter to `string?` and pass `null`
(not `""`) from the `Count(pred)` call site, with a doc comment on the constructor explaining why
`""` and `null` are NOT interchangeable here. `null` correctly leaves the field path unprefixed,
matching what the `$filter` renderer's `elementVariable` threading already expects.

## Fallout: pre-existing tests updated (not new regressions)

Both fixes below are **assertion updates matching an intended, documented consequence** of Step
5g/5h's changes — not accidental breakage. Confirmed via the brief's own note on `TranslateComparison`
("a comparison whose left side is now a `MongoOuterFieldExpression` is correctly no longer
'query-native'... No change needed [to the renderer]") — this note only makes sense if the type
change was expected to reach every existing two-scope call site, including `NativeSelectManyBinder`'s,
not just the new `Count(pred)` one.

1. **`tests/.../FunctionalTests/Query/NativeOwnedCollectionFilteredCountTests.cs`** —
   `Correlated_element_predicate_declines_and_falls_back_to_correct_rows` asserted the *old*
   "decline" behavior for the exact shape `b.Posts.Count(p => p.Title == b.Title) > 0` that this
   task changes. Renamed to `..._now_goes_native_via_two_scope_translator_and_returns_correct_rows`
   and updated its `NativeOnly` assertion from `Assert.Throws<NativeTranslationNotSupportedException>`
   to `Assert.Empty(...)` — the query now succeeds natively and, crucially, still returns the
   CORRECT (empty) result, which is the property this test exists to pin (a by-name misresolution
   would return the row instead). The `Native`-mode `Assert.Empty` assertion (checked first,
   independently load-bearing per the original test's own comment) is unchanged.
2. **`tests/.../UnitTests/Query/NativeTranslation/MongoExpressionTranslatorTests.cs`** (4 tests) and
   **`NativeSelectManyBinderTests.cs`** (4 tests) — each asserted `Assert.IsType<MongoFieldExpression>`
   on the OUTER side of an existing two-scope field-to-field comparison built by
   `NativeSelectManyBinder`. Updated to `Assert.IsType<MongoOuterFieldExpression>`. Verified this is a
   type-only change with **zero MQL difference**: these comparisons were already routed through the
   field-to-field/`$expr` branch (`TranslateOperand`), and `MongoOuterFieldExpression` renders
   identically to a document-root `MongoFieldExpression` (`FieldRef(ElementName, elementVariable: null)`
   in both cases, since `elementVariable` was already `null` at this rendering depth).

## TDD evidence

**RED** (before any src change):
```
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NativeOwnedCollectionCorrelatedTests.Correlated_filtered_Count_predicate_goes_native"
```
```
Failed ... Correlated_filtered_Count_predicate_goes_native [240 ms]
MongoDB.EntityFrameworkCore.Query.NativeTranslation.NativeTranslationNotSupportedException :
Query is not natively representable and MongoQueryMode.NativeOnly forbids the driver-LINQ fallback.
```

**Intermediate RED** (after Steps 3–7 literally, before fix #1 — SelfParam never wired):
same exception, same test still fails — confirmed the literal brief text alone was insufficient.

**Intermediate RED** (after fix #1, before fix #2 — `innerPrefix: ""`):
```
MongoDB.Driver.MongoCommandException : Command aggregate failed: Consider using $getField or
$setField for a field path with '.' or '$'. :: caused by :: FieldPath field names may not be empty strings..
```

**GREEN** (after fix #2):
```
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NativeOwnedCollectionCorrelatedTests.Correlated_filtered_Count_predicate_goes_native"
```
```
Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 280 ms
```

**Full-suite verification:**
```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"                → 0 errors
dotnet test tests/.../FunctionalTests --filter "~NativeOwnedCollection" → 216 passed, 1 failed
                                                                           (Correlated_Any_goes_native —
                                                                            expected, Task 5's scope)
dotnet test tests/.../FunctionalTests --filter "~Query"                → 1820 passed, 1 failed (same),
                                                                           45 skipped (pre-existing,
                                                                           encryption/vector gates)
dotnet test tests/.../UnitTests                                        → 1001 passed, 0 failed
dotnet test tests/.../SpecificationTests --filter "~SelectMany"        → 178 passed, 0 failed
dotnet test tests/.../SpecificationTests (full)                        → 4599 passed, 0 failed,
                                                                           14 skipped (pre-existing)
```

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs`
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.Members.cs`
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs` (not in the
  brief's file list — necessary wiring fix, see above)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionCorrelatedTests.cs` (new)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionFilteredCountTests.cs`
  (pre-existing test updated to match new intended behavior)
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoExpressionTranslatorTests.cs`
  (4 assertions updated: outer field now types as `MongoOuterFieldExpression`)
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeSelectManyBinderTests.cs`
  (4 assertions updated, same reason)

Commit: `96a220f` — "EF-421: correlated filtered Count(pred) goes native via a two-scope translator"

## Self-review findings

- **Completeness**: all 9 `TryResolveMember` call sites in `MongoExpressionTranslator.cs` (7 decline/build
  arms + `TryTranslateField` + the `Count(pred)` arm) updated per the brief's exact disposition table;
  `TryResolveOwnedFieldPath`'s signature updated mechanically only (no relativization — confirmed Task 6
  is untouched: its own two-scope decline guard is unchanged).
- **No scope creep**: did not touch `TryResolveOwnedCollectionPath` (unrelated to this task's signature
  change — it doesn't call `TryResolveMember`), did not attempt Any/All's real correlation wiring (Task 5),
  did not relativize any owned-path resolver (Task 6).
- **Judgment calls made, both documented above and both necessary for the brief's own required test to
  pass**: (1) wiring `SelfParam` in `NativeSlotPopulator` (file not in the brief's list, but required —
  without it the feature is inert); (2) changing `innerPrefix` to nullable and passing `null` instead of
  `""` (the brief's literal snippet was a bug — `""` is silently wrong, not merely stylistically
  different, since it emits invalid MQL that the server rejects). Both are minimal, targeted, and
  verified against the full test suite with zero unexpected regressions.
- **Testing**: the new FunctionalTests file runs against a real MongoDB (`mongodb-atlas-local` via
  Testcontainers, `MONGODB_URI`/`ATLAS_URI` both unset, Docker confirmed available) — not skipped.
  `Correlated_filtered_Count_predicate_goes_native` is GREEN; `Correlated_Any_goes_native` is RED exactly
  as the brief predicts (fixed in Task 5). Full FunctionalTests/UnitTests/SpecificationTests suites all
  green apart from that one expected, documented exception.

## Concerns for the reviewer

1. The two necessary fixes above mean this task's diff is not a pure application of the brief's literal
   text — flagging this explicitly per the task instructions ("if anything is ambiguous... ASK"). I judged
   both to be unambiguous bugs/gaps (one a missing wire-up the brief's own prior-task documentation already
   committed to, one a literal empty-string/null confusion that produces a server-rejected MQL string) rather
   than open design questions, and fixed them minimally with full-suite verification rather than stopping to
   ask, per the auto-mode guidance to make the reasonable call and flag it. Happy to revert either if the
   plan author intended something different.
2. `NativeSlotPopulator`'s `SelfParam` wiring was added for `OrderBy`/`ThenBy` as well as `Where`, even
   though only `Where`'s shape is exercised by this task's test. This was a deliberate consistency choice
   (matches the documented general design and costs nothing today, since `TryTranslateField` itself declines
   on `isOuter` per Step 6), but flagging it as slightly beyond the letter of "just make the Count(pred) test
   pass" in case the plan intended `Where`-only wiring for now.

## Review fix round (2026-08-28)

The task reviewer approved the task overall but raised two Important findings. Both are now fixed.

### Finding 1: multi-free-parameter loophole (production code fix)

**Root cause.** `FreeParameterVisitor.FoundParameter` (`MongoExpressionTranslator.MethodCalls.cs`)
recorded only the FIRST free `ParameterExpression` it encountered (`FoundParameter ??= node`). The
`Count(pred)` two-scope arm added by this task (`MongoExpressionTranslator.cs`, the `TranslateOperand`
filtered-count branch) accepts the correlation whenever `ReferenceEquals(countFreeParam, SelfParam)`. If
a predicate body ever contained TWO distinct free parameters — one matching `SelfParam`, one not — the
first-found heuristic could report the matching one, pass the identity check, and build a two-scope child
translator; that child would then resolve the SECOND free parameter's members by NAME against the element
entity type (neither the outer param nor bound), which is exactly the silent wrong-rows hazard this
codebase's own invariant ("scope must be resolved by parameter identity, never by member name") forbids.
No currently-reachable EF query-authoring path produces this shape (a captured local surfaces as a
closure-field `MemberExpression`, not an extra free `ParameterExpression`), so this was a defense-in-depth
gap rather than an observed bug — but the reviewer correctly flagged the mechanism as unsound for the new
caller.

**Fix (option (b) from the reviewer's suggestions).** `FreeParameterVisitor` now tracks every DISTINCT
(by reference identity) free parameter in a private list, and `FoundParameter` is a computed property
that returns the sole entry when exactly one was found, and `null` when zero or two-or-more were found.
Because `null` can never `ReferenceEquals` a real `ParameterExpression`, the `Count(pred)` call site's
existing `ReferenceEquals(countFreeParam, SelfParam)` check already declines the multi-free-parameter
shape correctly — I added an explicit `countFreeParam is null` guard alongside it for readability/defense
in depth (belt-and-braces; not load-bearing beyond the `ReferenceEquals` check itself, but makes the
"exactly one, and it matches" contract explicit at the call site rather than implicit in the property).
Updated both `FreeParameterVisitor.FoundParameter`'s XML doc comment (removed the now-inaccurate "a body
with two distinct free parameters is already declined by the caller regardless of which one is reported"
claim) and `ReferencesEnclosingScope`'s summary to describe the new contract accurately, including a note
that the quantifier (`Any`/`All`) call site is unaffected since it only ever uses the boolean return
(`out _`).

**Files:** `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.MethodCalls.cs`,
`src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs`.

**Test added (unit, not functional):** since no real EF LINQ query can currently construct a Count(pred)
body with two distinct free `ParameterExpression`s (captured locals become closure-field member accesses,
not extra parameters), the regression pin is a hand-built expression tree exercised directly against
`MongoExpressionTranslator.TryTranslate` (mirroring the existing hand-built-expression pattern used by
`Query_parameter_becomes_MongoParameterExpression_not_constant` in the same file) —
`MongoExpressionTranslatorTests.Correlated_Count_element_predicate_with_two_distinct_free_parameters_declines`
in `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoExpressionTranslatorTests.cs`.
It builds `b.Posts.Count(p => p.Title == b.Title && p.Rank == x) > 0` by hand (`b`/`p`/`x` all explicit
`ParameterExpression`s, `x` free and unrelated), sets `translator.SelfParam = bParam` (mirroring
`NativeSlotPopulator`'s real wiring), and asserts `TryTranslate` returns `false`/`null` — i.e. the
correlation still declines outright rather than building a two-scope translator.

### Finding 2: untested SelectMany outer-vs-value rendering shape (test-only)

No production code change — the reviewer confirmed the new `MongoOuterFieldExpression`-routed rendering
(`{$expr: {$eq: [...]}}` instead of the pre-existing query-dialect `{Name: "x"}`) for this shape is
correct, just previously unexercised by any test. Added two FunctionalTests to
`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeSelectManyTests.cs`, adjacent to the
existing `Owned_correlated_beyond_outer_*` family:

- `Owned_correlated_beyond_outer_against_a_plain_value_goes_native_with_correct_rows` — outer member vs. a
  captured-local parameter (`o.Items.Where(i => o.Name == ownerName)`), asserting correct, non-vacuous rows
  under `MongoQueryMode.NativeOnly` against an in-memory LINQ oracle (no driver-LINQ oracle exists for any
  correlated owned `SelectMany` shape, per this file's pre-existing convention).
- `Owned_correlated_beyond_outer_against_a_plain_constant_goes_native_with_correct_rows` — same shape with
  a literal constant instead of a parameter, covering both `MongoConstantExpression` and
  `MongoParameterExpression` right-hand forms of the `MongoOuterFieldExpression` comparison.

### Verification

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
  → Build succeeded, 0 errors.

dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NativeOwnedCollection|FullyQualifiedName~NativeSelectMany"
  → Failed: 1, Passed: 352, Skipped: 0, Total: 353.
    The one failure is NativeOwnedCollectionCorrelatedTests.Correlated_Any_goes_native, a PRE-EXISTING,
    already-documented failure (this task's own report, "Testing" section above, and the test's own
    in-repo comment both state it is expected to fail until Task 5 gives the Any/All quantifier arm its
    own two-scope branch). Confirmed by stashing this fix round's changes and re-running against the
    unmodified Task-3 commit (96a220f) in isolation: identical single failure, same stack trace — so this
    fix round introduces zero regressions.

dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build
  → Passed: 1002, Failed: 0, Skipped: 0, Total: 1002.
```

Committed as `7b86cd0` — "EF-421: close multi-free-parameter correlation gap; add SelectMany outer-vs-value
coverage" (new commit, not an amend, per instructions).
