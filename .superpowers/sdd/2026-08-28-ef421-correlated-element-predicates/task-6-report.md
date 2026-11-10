# Task 6 Report: Relativize `TryResolveOwnedFieldPath`/`TryResolveOwnedCollectionPath`

## What was implemented

Followed the brief exactly:

1. **`TryResolveOwnedFieldPath`** (`MongoExpressionTranslator.Members.cs`): replaced the unconditional
   `_outerParam is not null || _innerPrefix is not null → return false` guard with:
   - Decline only when `_innerPrefix is not null` (SelectMany-unwind two-scope mode stays out of scope,
     unchanged).
   - Otherwise walk the hop chain to the root `ParameterExpression`, compute
     `isOuter = _outerParam is not null && ReferenceEquals(rootParam, _outerParam)` — root-identity check,
     never by name, mirroring `TryResolveMember`'s existing `isOuter` logic.
   - Decline if in two-scope mode (`_outerParam is not null`) but rooted on neither known parameter.
   - Seed `scopeType` from `_outerEntityType!` when `isOuter`, else `_entityType` (was always `_entityType`).
   - The hop-walking loop and leaf construction below are untouched — they already operate on the local
     `scopeType`/`segments` variables.

2. **`TryResolveOwnedCollectionPath`** (same file): identical relativization pattern, plus the new
   `out bool isOuter` parameter added to the signature.

3. **Three callers in `MongoExpressionTranslator.cs`** updated to the new 4-arg signature:
   - `TryTranslateOwnedCollectionArray` (~line 285): `&& !arrayIsOuter` guard (defensive — this call site is
     always root-scoped in practice, per the existing remark above it).
   - Quantifier arm (~line 611): added `if (sourceIsOuter) return null;` — the array itself being reached
     through the outer scope is a separate, still out-of-scope shape.
   - `Count(pred)` arm (~line 1109): added `&& !countSourceIsOuter` conjunct, same rationale.

4. **Doc-comment upkeep** (not explicitly itemized by the brief's diff blocks, but needed to stay accurate
   per the "no scope creep" instruction's spirit — these are the XML `<remarks>` paragraphs that literally
   said "two-scope mode ... is still declined", which would now be a stale/misleading doc after the code
   changed): updated the two `<remarks>` paragraphs above `TryResolveOwnedFieldPath` and
   `TryResolveOwnedCollectionPath`, and the inline comments around the `Count(pred)` call site in
   `MongoExpressionTranslator.cs`, to describe the new root-identity-based behavior instead of an
   unconditional decline. No code-behavior changes beyond what the brief specified — comments only.

5. **Test**: appended `RefBlog`/`RefOwner`/`RefPost`/`RefBlogModel` and
   `Correlated_Any_through_an_outer_owned_single_reference_hop_goes_native` to
   `NativeOwnedCollectionCorrelatedTests.cs`, verbatim per the brief.

## TDD evidence

**RED** (test added, src changes stashed to restore old behavior):

```
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Correlated_Any_through_an_outer_owned_single_reference_hop_goes_native"
```
```
Failed MongoDB.EntityFrameworkCore.FunctionalTests.Query.NativeOwnedCollectionCorrelatedTests.
  Correlated_Any_through_an_outer_owned_single_reference_hop_goes_native [304 ms]
  Error Message:
   MongoDB.EntityFrameworkCore.Query.NativeTranslation.NativeTranslationNotSupportedException :
   Query is not natively representable and MongoQueryMode.NativeOnly forbids the driver-LINQ fallback.
Failed!  - Failed: 1, Passed: 0, Skipped: 0, Total: 1
```
Matches the brief's predicted RED exactly.

**GREEN** (src changes restored):

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded, 0 Errors
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Correlated_Any_through_an_outer_owned_single_reference_hop_goes_native"
```
```
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 402 ms
```

**Regression suite** (Step 9):

```
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NativeOwnedCollection|FullyQualifiedName~NativeSelectMany"
```
```
Failed!  - Failed: 2, Passed: 354, Skipped: 0, Total: 356
```
The 2 failures are:
- `NativeOwnedCollectionAllTests.All_with_a_correlated_element_predicate_declines_and_falls_back_to_correct_rows`
  — the ONE known exception the brief calls out explicitly (resolved in Task 8).
- `NativeOwnedCollectionPredicateTests.Correlated_element_predicate_declines_and_falls_back_to_correct_rows`
  — NOT named in the brief, so I verified independently: re-ran this exact filter with the `src/` changes
  stashed (old, pre-Task-6 code, new test file still present) and confirmed **both** failures reproduce
  identically on the pre-Task-6 code. Both are pre-existing regressions introduced by Task 5's two-scope
  `Any`/`All` translator landing (an owner-referencing-only element predicate, `p => b.Title == "match"`,
  now resolves natively via the two-scope path instead of declining — an existing gap Task 6 does not touch
  or worsen). Confirmed NOT a Task-6 regression; both are the same "correlated Any/All owner-only predicate
  now goes native when it used to decline" class of test, only one of the two was mentioned in this task's
  brief.
- `NativeSelectMany*` suite: unchanged pass/fail set (confirmed no NativeSelectMany failures appear in the
  above run — `TryResolveMember`'s shared two-scope branch was not touched by this task).

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.Members.cs`
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionCorrelatedTests.cs`

Commit: `0940a67 EF-421: relativize TryResolveOwnedFieldPath/TryResolveOwnedCollectionPath for the outer scope`

## Self-review findings

- Confirmed all three callers of `TryResolveOwnedCollectionPath` updated (grep-verified no other call
  sites in `src/`).
- Confirmed `TryResolveMember`'s delegation to `TryResolveOwnedFieldPath` (line 111) already threads
  `isOuter` through unchanged — no further change needed (Step 4 of the brief, already done in Task 3).
- Did NOT touch `MongoFieldPrefixRewriter`, per the plan's explicit out-of-scope note.
- Did NOT touch `Query/AGENTS.md`'s capability-summary prose — that's a bigger doc-sync exercise better
  left to the plan's final task/review pass, not itemized in this task's brief. I did update the two
  `<remarks>` XML doc paragraphs in `MongoExpressionTranslator.Members.cs` and the inline comments in
  `MongoExpressionTranslator.cs` around the `Count(pred)` arm that would otherwise have gone stale/wrong
  (they asserted "two-scope mode is always declined" — no longer true) — this is doc-only, no behavior
  change, and directly serves the "no stale/misleading comments" discipline this codebase's history
  clearly cares about (see commit `3def6ee` in the recent log).
- Verified via git-stash RED/GREEN round-trip (not just eyeballing) that the implementation genuinely
  causes the test transition, and that the regression suite's failures are pre-existing, not newly
  introduced.

## Issues or concerns

None blocking. One flag for whoever picks up Task 8 (or a later review pass): the brief's Step 9 names
only ONE known-failing test (`NativeOwnedCollectionAllTests.All_with_a_correlated_element_predicate_...`)
but a second, structurally identical test in `NativeOwnedCollectionPredicateTests.cs`
(`Correlated_element_predicate_declines_and_falls_back_to_correct_rows`) also now fails, for the same
underlying reason (Task 5's two-scope `Any` translator natively resolving an owner-only, non-`p`-referencing
element predicate that used to decline). Worth confirming Task 8's fix addresses both, or that the plan's
scope for Task 8 already covers this second test too.
