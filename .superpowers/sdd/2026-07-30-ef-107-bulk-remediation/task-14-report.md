# Task 14 Report: Match canonical `QueryableMethods` definitions and accept `Order`/`OrderDescending`

## Summary

Implemented per Corrections 1-3 as specified. Correction 4 (Order() reachability) resolved to the
**"Not reachable"** branch, empirically — and a parallel probe found the brief's *other* Step-1 test
(indexed `Where`) is **also** not reachable through the mechanism the brief assumed, for a different but
related reason. Both findings are detailed below since they diverge from the task's stated premises.

## What was implemented

1. **New shared source of truth** — `MongoNonQueryExpression.AffectedSetNeutralDefinitions`
   (`internal static readonly HashSet<MethodInfo>`) and `MongoNonQueryExpression.IsAffectedSetNeutral(MethodInfo)`
   in `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs`. Built by reflection over
   `typeof(Queryable)` exactly as the brief specified (arity-filtered by name for `OrderBy`/`OrderByDescending`/
   `ThenBy`/`ThenByDescending` at 2 params and `Order`/`OrderDescending` at 1 param, plus `QueryableMethods.Distinct`).
   Named for what it actually contains (it's not all "ordering" — `Distinct` is in it) per Correction 2.
   `OfType`/`Cast` deliberately excluded per Correction 2 (reserved for Task 15, they narrow the affected set).
2. **`ClassifyBulkSource`** (`MongoQueryableMethodTranslatingExpressionVisitor.cs`) rewritten to compare
   `method.GetGenericMethodDefinition()` against `QueryableMethods.Where`, `MongoNonQueryExpression.IsAffectedSetNeutral(...)`,
   and `QueryableMethods.Skip`/`QueryableMethods.Take`, in that order, replacing the old bare `Method.Name` switch.
3. **`BuildFilter`** (`MongoShapedQueryCompilingExpressionVisitor.cs`) — Correction 1's real bug fix — converted
   from a `switch (method.Name)` to the same three-way definition check via the *same* `MongoNonQueryExpression.IsAffectedSetNeutral`
   helper, so the two layers structurally cannot disagree. This closes the "indexed `Where` rebinds only
   `Parameters[0]`" hazard by construction (see reachability finding below for why it wasn't live today).
4. **`ThrowBulkSourceNotSupported`**'s user-facing message extended to mention `'Order'`/`'OrderDescending'`
   as scoping operators (these are genuinely usable by end users, even though the literal AST node is rewritten
   away before reaching the classifier — see below).
5. **XML doc on `ClassifyBulkSource`** updated to mention `Order`/`OrderDescending` and to point at
   `IsAffectedSetNeutral` as the source of truth, rather than re-enumerating the list inline.
6. **Unit test** `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/AffectedSetNeutralDefinitionsTests.cs` (new
   file) pins the reflection set's membership durably (Correction 3), since `AffectedSetNeutralDefinitions` is
   `internal` and `InternalsVisibleTo` already exposes internals to `MongoDB.EntityFrameworkCore.UnitTests` — no
   visibility was widened past `internal`.

## `Order()` reachability finding (Correction 4) — NOT REACHABLE

Probed by adding a temporary debug write of `expression?.Print()` at the top of `ClassifyBulkSource` and
running `db.Entities.Where(o => o.Status == "open").Order().ExecuteDelete()` against the **unmodified**
(pre-Task-14) classifier. The captured chain that actually reached `ClassifyBulkSource` was:

```
DbSet<Order>()
    .Where(o0 => o0.Status == "open")
    .OrderBy(o0 => EF.Property<ObjectId>(o0, "_id"))
```

**EF Core's own `NavigationExpandingExpressionVisitor` rewrites `Order()`/`OrderDescending()` on an
entity-typed source into `OrderBy(key)`/`OrderByDescending(key)` before either bulk visitor ever sees the
literal call** (a documented general EF Core behavior for .NET 7's keyless ordering: since `Comparer<T>.Default`
isn't translatable server-side, EF Core substitutes ordering by the primary key). Confirmed with old
(unmodified) code: the literal `Order` case in the switch was unreachable, and the query still worked (fell
through to the existing `OrderBy`-by-name case) — that's why `ExecuteDelete_with_Order_deletes_matching`, as
written in the brief, **passed even with zero code changes**, for the wrong reason.

**Branch taken: Not reachable.** Per Correction 4, I did not contort the model. I:
- Added `Order`/`OrderDescending` to the classifier and `BuildFilter` for completeness (via the shared set).
- Recorded the unreachability, and the reasoning, in the XML doc on `AffectedSetNeutralDefinitions` and as an
  inline comment in `ExecuteDeleteTests.cs` where the test would have gone.
- **Dropped** `ExecuteDelete_with_Order_deletes_matching` rather than ship a test that passes without exercising
  the new code.

## Second reachability finding (not anticipated by the brief): indexed `Where` is ALSO blocked upstream

Probed the same way: ran `db.Entities.Where((o, i) => i == 0).ExecuteDelete()` (and, for confirmation, the same
predicate on a plain read with `.ToList()` instead of `.ExecuteDelete()`) against the **unmodified** classifier.
Both throw, at the exact same stack frame, **before reaching any Mongo-provider code**:

```
System.InvalidOperationException: The LINQ expression 'DbSet<Order>()
    .Where((o, i) => i == 0)' could not be translated. ...
   at Microsoft.EntityFrameworkCore.Query.Internal.NavigationExpandingExpressionVisitor.VisitMethodCall(...)
   ...
   at MongoDB.EntityFrameworkCore.Query.MongoQueryTranslationPreprocessor.Process(...)
```

EF Core's `NavigationExpandingExpressionVisitor` rejects the indexed `Where` overload universally — for every
provider, for reads and bulk operations alike — because it can't be expanded through navigation rewriting. It
never reaches `ClassifyBulkSource` or `BuildFilter` at all. I also probed `OrderBy(keySelector, IComparer<T>)`
the same way (a second overload the brief names as a hazard) and got the identical rejection at the identical
stack frame.

This means `ExecuteDelete_with_indexed_Where_throws_the_canonical_failure`, exactly as specified in the brief,
**also passes with zero code changes** — it is valid regression coverage of end-to-end behavior, but it does
**not** exercise `ClassifyBulkSource`'s or `BuildFilter`'s definition-based matching, contradicting the
corrections' framing of it as "the substantive coverage for this task." I kept the test (it's harmless, correct,
and worth keeping as coverage of the observable behavior) but added a comment recording that the rejection
currently comes from EF Core's own pipeline, not from this task's code, and noted it here for your review as
instructed ("if anything ... contradicts what I have told you — ask/report").

**Net implication:** every overload-mismatch hazard the brief names (indexed `Where`, `OrderBy`/comparer,
and — per Task 13's own finding — `Distinct`/comparer) is already blocked by EF Core itself, for any provider,
before dispatch ever reaches this provider's code. The definition-based matching this task adds is genuine
defense-in-depth and brings the classifier in line with the codebase's established convention (`Query/AGENTS.md`'s
"Reference-equality on MethodInfo" pitfall, and the pattern every other dispatch in the file already follows) —
not a fix for a currently-exploitable bug. `Take(Range)` was not separately probed (out of scope: not part of
the neutral set either way), but I'd expect the same pattern.

## Enumerated resolved members of `AffectedSetNeutralDefinitions` (7, as expected)

Dumped via a temporary probe test (removed before commit):

```
IOrderedQueryable<TSource> OrderBy[TSource,TKey](IQueryable<TSource>, Expression<Func<TSource,TKey>>)
IOrderedQueryable<TSource> OrderByDescending[TSource,TKey](IQueryable<TSource>, Expression<Func<TSource,TKey>>)
IOrderedQueryable<TSource> ThenBy[TSource,TKey](IOrderedQueryable<TSource>, Expression<Func<TSource,TKey>>)
IOrderedQueryable<TSource> ThenByDescending[TSource,TKey](IOrderedQueryable<TSource>, Expression<Func<TSource,TKey>>)
IOrderedQueryable<T> Order[T](IQueryable<T>)
IOrderedQueryable<T> OrderDescending[T](IQueryable<T>)
IQueryable<TSource> Distinct[TSource](IQueryable<TSource>)
```

Confirmed resolving correctly across all three pinned frameworks by running the pinning unit test
(`AffectedSetNeutralDefinitionsTests.Contains_exactly_the_seven_expected_operator_definitions`) under
`Debug EF8`, `Debug EF9`, and `Debug EF10` — all pass (see GREEN evidence below). `Order`/`OrderDescending`
confirmed present in the BCL on both `net8.0` (EF8/EF9) and `net10.0` (EF10) targets, not merely assumed.

## TDD evidence

### Functional-level RED/GREEN: not producible (see reachability findings)

Both of the brief's Step-1 tests pass unmodified against the pre-Task-14 code (verified — see findings above),
so no functional test could show a genuine RED→GREEN cycle for the switch-to-definition-matching change itself;
every scenario that would distinguish name-matching from definition-matching is blocked upstream by EF Core
before it reaches this provider. I shifted the TDD cycle to the level where the distinction *is* observable:
the shared reflection-based set itself.

### Unit-level RED

Command (with the new `AffectedSetNeutralDefinitions`/`IsAffectedSetNeutral` members temporarily removed via
`git stash` of `MongoNonQueryExpression.cs`, before implementing `ClassifyBulkSource`/`BuildFilter` changes):

```
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" \
  --filter "FullyQualifiedName~AffectedSetNeutralDefinitionsTests"
```

Actual failing output:
```
error CS0117: 'MongoNonQueryExpression' does not contain a definition for 'AffectedSetNeutralDefinitions'
error CS0117: 'MongoNonQueryExpression' does not contain a definition for 'IsAffectedSetNeutral'  (x3)
Build FAILED.
```
Expected because the test references members that did not yet exist — a genuine compile-time RED.

### Unit-level GREEN

Restored the implementation (`git stash pop`) and reran:

```
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" \
  --filter "FullyQualifiedName~AffectedSetNeutralDefinitionsTests"
```
```
Passed MongoDB.EntityFrameworkCore.UnitTests.Query.AffectedSetNeutralDefinitionsTests.Does_not_contain_the_comparer_overload_of_OrderBy_or_Distinct
Passed MongoDB.EntityFrameworkCore.UnitTests.Query.AffectedSetNeutralDefinitionsTests.Does_not_contain_the_indexed_Where_overload
Passed MongoDB.EntityFrameworkCore.UnitTests.Query.AffectedSetNeutralDefinitionsTests.Contains_exactly_the_seven_expected_operator_definitions
Test Run Successful. Total tests: 3, Passed: 3
```
Repeated under `Debug EF8` and `Debug EF9` — all 3/3 pass on every configuration.

### Functional regression evidence (existing tests, all still GREEN after the switch rewrite)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```
`Passed! Failed: 0, Passed: 64, Skipped: 1 (standalone-only test, pre-existing), Total: 65`

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```
`Passed! Failed: 0, Passed: 65, Skipped: 1, Total: 66`

Northwind conformance (both baselines, per Global Constraints):
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
→ Passed! Failed: 0, Passed: 181, Skipped: 0, Total: 181   (matches baseline: 181/181)

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
→ Passed! Failed: 0, Passed: 169, Skipped: 0, Total: 169   (matches baseline: 169/169)
```
**No conformance test changed status** — nothing moved to `AssertTranslationFailed`, nothing newly failed.

Broader regression sanity (not strictly required, run for confidence given the shared-seam refactor):
- Full `UnitTests` project, all 3 configs: 286/286 (EF8), 298/298 (EF9), 291/291 (EF10), 0 failed.
- Full Query-area `SpecificationTests` + `FunctionalTests`, EF10: 4508/4535 passed (27 skipped, pre-existing gates), 629/674 passed (45 skipped, encryption gate), 0 failed in both.

### Build (Global Constraint: all 3 configurations)

`dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"` / `"Debug EF9"` / `"Debug EF10"` — all three: `Build succeeded, 0 Warning(s), 0 Error(s)`.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs` — new shared
  `AffectedSetNeutralDefinitions` set + `IsAffectedSetNeutral` helper, with full rationale in the XML doc
  (including the Order/OrderDescending unreachability note and the OfType/Cast exclusion note for Task 15).
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` —
  `ClassifyBulkSource` converted to definition-based matching via the shared helper; XML doc updated;
  `ThrowBulkSourceNotSupported`'s message extended to name `Order`/`OrderDescending`.
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` —
  `BuildFilter`'s walk converted to definition-based matching via the same shared helper (Correction 1's fix).
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` — added
  `ExecuteDelete_with_indexed_Where_throws_the_canonical_failure` (kept, with a comment recording what it
  actually verifies); did **not** add an `Order()` deletion test (comment explains why, in place of the test).
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/AffectedSetNeutralDefinitionsTests.cs` (new) — pins the
  reflection set's membership and definition-vs-name distinction directly.

## Self-review

- **Completeness**: both switches now consult one shared, accurately-named set; `Order`/`OrderDescending`
  handled in both; reflection resolution verified with durable (not temporary) evidence; Order() reachability
  settled empirically before writing tests, per Correction 4's explicit instruction.
- **Naming**: `IsAffectedSetNeutral`/`AffectedSetNeutralDefinitions` chosen (not `IsOrderingOperator`) — correct
  per Correction 2, since the set includes `Distinct`.
- **YAGNI**: did not add `OfType`/`Cast` (Task 15's job); did not probe/test `Take(Range)` separately since it's
  out of scope either way and the pattern is already well-established by the two overloads I did probe.
- **Could each new test actually fail?** The unit tests: yes, demonstrated via the stash/pop RED→GREEN cycle
  above. The functional indexed-`Where` test: yes, it fails if the exception type/message/count assertions are
  wrong, but (per the finding above) not for the reason originally assumed — it exercises EF Core's own
  pipeline, not this task's code. I judged keeping it (as honest regression coverage with a corrected comment)
  better than deleting it, since Arthur may want the option to move it once Task 15 or later work changes what's
  reachable.
- **Test output pristine**: no `Console.WriteLine`/stray diagnostics left in any test; all debug/probe code
  (`File.WriteAllText` probes, the `PROBE_*` throwaway tests) was removed before the final build/test pass.
- **Stale comments check**: `ClassifyBulkSource`'s XML doc, `ThrowBulkSourceNotSupported`'s message, and
  `BuildFilter`'s invariant comment were all updated to reflect `Order`/`OrderDescending` and the shared helper;
  I re-read each after editing to confirm no leftover reference to "the ordering/Distinct operators" by name
  that would now be inaccurate.

## Concerns

1. **The task's stated premise about reachability was wrong for both Step-1 hazards**, not just `Order()`. I
   want this specifically flagged rather than silently absorbed, since the corrections describe the indexed-`Where`
   test as "the substantive coverage for this task" — it is valid coverage, but of EF Core's own behavior, not
   of the code this task changed. If this matters for how the task is scored/reviewed, please let me know.
2. Given every named hazard is already blocked upstream by EF Core for any provider, this change is
   correctness/defense-in-depth and codebase-convention alignment, not a fix for an currently reachable bug —
   consistent with, and extending, Task 13's own finding that `Distinct(IEqualityComparer<T>)` was "only
   unreachable by luck." Flagging so the commit message and any changelog entry don't overstate user-visible impact.

---

## Fix report (review round 1: spec ❌ / quality Needs fixes)

The reviewer independently confirmed both of the concerns I raised (the false reachability premises for the
indexed-`Where` and `Order()` Step-1 tests) against an EF Core checkout. Two mechanism corrections and three
fixes below.

### Mechanism corrections accepted

- The `Order()`/`OrderDescending()` normalization is done by EF's
  **`QueryableMethodNormalizingExpressionVisitor.TryNormalizeOrderAndOrderDescending`** (v9.0.16:529-547), not
  `NavigationExpandingExpressionVisitor` as I'd written. It fires **unconditionally for all source types**
  (not just entity-typed ones) and produces `OrderBy(x => x)`; the `EF.Property<ObjectId>(o,"_id")` key selector
  I printed in the original report is a *separate, later* nav-expansion rewrite of that `OrderBy(x => x)` for
  the entity case — which is why my trace looked entity-specific when the underlying normalization isn't.
  (The indexed-`Where` attribution to `NavigationExpandingExpressionVisitor` was *not* flagged — that one is
  correct, confirmed by my own captured stack trace, which shows `NavigationExpandingExpressionVisitor.VisitMethodCall`
  as the actual throw site.)

### Important 1 — build the set from canonical `QueryableMethods` constants, not reflection; `#if !EF8`

Verified before fixing: `strings` on the three pinned `Microsoft.EntityFrameworkCore.dll` assemblies confirms
`OrderDescending` (a distinctive symbol only `QueryableMethods`/`Queryable`'s own metadata would contain) is
absent from 8.0.27 and present in both 9.0.16 and 10.0.8 — matching the reviewer's claim precisely.

Fixed `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs`:
- `AffectedSetNeutralDefinitions` is now built directly from `QueryableMethods.OrderBy`,
  `.OrderByDescending`, `.ThenBy`, `.ThenByDescending`, `.Order`, `.OrderDescending`, `.Distinct` — no
  reflection, no arity filtering.
- Wrapped the field and `IsAffectedSetNeutral` in `#if !EF8` (the whole type previously carried no `#if`s;
  now this member follows the standing "bulk operations code lives inside `#if !EF8`" convention).
- Corrected the XML doc to state the true fact (constants exist on EF9+; this is exactly why EF8 needs the
  guard) rather than the false claim that `QueryableMethods` lacks these members.
- Reworked `AffectedSetNeutralDefinitionsTests.cs` (`Contains_exactly_the_seven_canonical_operator_constants`)
  to assert `Assert.Equal` against a `HashSet<MethodInfo>` built from the same seven `QueryableMethods`
  constants — a genuine identity pin now, not a re-derivation of the technique under test. Wrapped the whole
  test file in `#if !EF8` (it references EF9-only members).

**Verified the EF8 break empirically**, the same way I verified Correction 4's `Order()` finding in round 1 —
temporarily removed the `#if !EF8` guard and rebuilt `Debug EF8`:
```
error CS0117: 'QueryableMethods' does not contain a definition for 'Order'
error CS0117: 'QueryableMethods' does not contain a definition for 'OrderDescending'
Build FAILED.
```
Then restored the guard and rebuilt clean (see Build evidence below). This is exactly the failure the reviewer
predicted, and exactly why Important 1 mattered: without the guard, EF8 would not build at all.

### Minor 2 — corrected the misattributed EF component in both comments

- `MongoNonQueryExpression.cs`: XML doc on `AffectedSetNeutralDefinitions` now reads "EF Core normalizes
  `Order()`/`OrderDescending()` into `OrderBy(x => x)`/`OrderByDescending(x => x)` during preprocessing
  (`QueryableMethodNormalizingExpressionVisitor.TryNormalizeOrderAndOrderDescending`, unconditionally, for
  every source type) ... so these two entries are unreachable unless that normalization is removed."
- `ExecuteDeleteTests.cs` (the "No `ExecuteDelete_with_Order` test" comment): reworded identically, replacing
  "on an entity-typed source is rewritten by EF Core's own `NavigationExpandingExpressionVisitor` into
  `OrderBy(e => EF.Property<TKey>(e, "_id"))`" with the corrected mechanism and scope.
- Confirmed no other reference to `NavigationExpandingExpressionVisitor` exists anywhere in `src/`/`tests/`
  except the (correct, unflagged) indexed-`Where` comment.

### Minor 3 — exposed as `IReadOnlySet<MethodInfo>`

`AffectedSetNeutralDefinitions` is now `internal static readonly IReadOnlySet<MethodInfo>` (field initializer
unchanged — still a `HashSet<MethodInfo>` object, just exposed through the narrower interface), so no caller
can `Add`/`Remove` through the public-facing (internal) surface.

### Minor 5 — dropped the near-tautological comparer-overload test

Dropped `Does_not_contain_the_comparer_overload_of_OrderBy_or_Distinct`. Reasoning: now that the production
set is a fixed list of seven named constants rather than an arity-filtered reflection query, there is no
"arity filter" left whose *shape* that test was guarding — a 3-arg `OrderBy` or 2-arg `Distinct` simply isn't
one of the seven constants, and `Contains_exactly_the_seven_canonical_operator_constants`'s `Assert.Equal`
already fails if the set ever contains anything other than exactly those seven. The dropped test is fully
subsumed by the identity assertion, not testing anything additional. Kept
`Does_not_contain_the_indexed_Where_overload` as-is (reviewer explicitly approved this one, and it still tests
something the identity assertion doesn't: that a real, differently-shaped `MethodInfo` correctly misses the set).

### Verification

Build, all three configurations (EF8 is the one that would have broken on Important 1's fix — confirmed
clean here, and confirmed it breaks correctly without the fix, above):
```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"   → Build succeeded. 0 Warning(s). 0 Error(s).
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"   → Build succeeded. 0 Warning(s). 0 Error(s).
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"  → Build succeeded. 0 Warning(s). 0 Error(s).
```

Unit tests, all three configurations:
```
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF8" --no-build
→ Passed! Failed: 0, Passed: 283, Skipped: 0, Total: 283   (AffectedSetNeutralDefinitionsTests correctly absent — #if !EF8)

dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF9" --no-build
→ Passed! Failed: 0, Passed: 297, Skipped: 0, Total: 297

dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build
→ Passed! Failed: 0, Passed: 290, Skipped: 0, Total: 290
```
Targeted re-run of the reworked pinning tests (2/2 now, was 3/3 — one dropped per Minor 5), EF9 and EF10:
```
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF9" --no-build --filter "FullyQualifiedName~AffectedSetNeutralDefinitionsTests"
→ Total tests: 2, Passed: 2
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~AffectedSetNeutralDefinitionsTests"
→ Total tests: 2, Passed: 2
```
And confirmed zero matches under EF8 (correctly excluded by the guard, not merely absent by accident):
```
dotnet test ... -c "Debug EF8" --no-build --filter "FullyQualifiedName~AffectedSetNeutralDefinitionsTests"
→ No test matches the given testcase filter ... (0 tests found, build succeeded)
```

ExecuteDelete/ExecuteUpdate suites, EF10 and EF9:
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
→ Passed! Failed: 0, Passed: 64, Skipped: 1 (pre-existing standalone-only skip), Total: 65

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
→ Passed! Failed: 0, Passed: 65, Skipped: 1, Total: 66
```
Identical counts to round 1 — no regression from the rework.

Both Northwind baselines, as required before re-dispatching review:
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
→ Passed! Failed: 0, Passed: 181, Skipped: 0, Total: 181   (baseline: 181/181, met)

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
→ Passed! Failed: 0, Passed: 169, Skipped: 0, Total: 169   (baseline: 169/169, met)
```

Broader Query-area regression re-run (EF10), for confidence given the touched shared type:
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query"
→ Passed! Failed: 0, Passed: 4508, Skipped: 27, Total: 4535
```
Identical to round 1 — no new failures, no new skips.

### Files changed (this round)

- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs` — Important 1, Minor 2, Minor 3.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` — Minor 2 (comment only).
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/AffectedSetNeutralDefinitionsTests.cs` — Important 1
  (identity assertion, `#if !EF8` guard), Minor 5 (dropped test).

Not touched: the "responsibility drift" note (operator table hosted on an expression-node class) — deferred
to final review per the coordinator's explicit instruction not to restructure this round.

### Commit

`2d8c40e` — "fix: build the affected-set-neutral operators from canonical QueryableMethods constants"
