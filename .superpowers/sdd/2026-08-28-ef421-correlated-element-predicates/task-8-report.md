# Task 8 Report: Retire the superseded decline test; regression pass; multi-EF-version check

## Stale decline test #1 — `NativeOwnedCollectionAllTests.cs`

Retired `All_with_a_correlated_element_predicate_declines_and_falls_back_to_correct_rows` exactly per
the brief's Step 1/2: renamed to `All_with_a_correlated_element_predicate_now_goes_native_since_EF421`,
swapped `AssertDeclinesCleanly` for `AssertNativeOnlyMatches`, updated the doc comment to explain the
EF-421 upgrade (two-scope translator + `$allElementsTrue`) while preserving the original reasoning for
why the seed (`Post.Title` colliding with `Blog.Title`) is discriminating. Same seed, same expected rows
(`["match"]`).

## Stale decline test #2 — `NativeOwnedCollectionPredicateTests.cs` (the `Any` sibling)

Confirmed present as flagged by Tasks 5/6/7: `Correlated_element_predicate_declines_and_falls_back_to_correct_rows`
(around line 512). Applied the same transformation:
- Renamed to `Correlated_element_predicate_now_goes_native_since_EF421`.
- This file's helper convention differs slightly from `NativeOwnedCollectionAllTests.cs`: it has both
  `AssertDeclinesCleanly` (NativeOnly throws + Native==DriverLinq parity) and `AssertNativeAndParity`
  (NativeOnly==DriverLinq parity, no throw expected). Swapped the former for the latter — a stronger
  check than `AssertNativeOnlyMatches` since the well-formed seed lets DriverLinq run as an independent
  oracle.
- Updated the doc comment to explain the EF-421 upgrade while preserving the original "GUARD
  REACHABILITY" reasoning (seed gives different answers under owner-scoped vs. element-scoped
  resolution) and the owner-scoped/element-scoped worked example.
- Updated both conjuncts of the test (bare correlated predicate, and the "mixed" one-element-only +
  one-correlated-conjunct form) — both now use `AssertNativeAndParity`.
- Renamed the two other in-file comment references to the old test name (lines 71 and 123, in
  `Post.Title`'s and the seed-builder's own doc comments) via `sed` so they still point at a real test
  name.
- The `AssertDeclinesCleanly` helper itself is still used elsewhere in the file
  (`Field_to_field_element_predicate_declines_cleanly`), so it was left in place, not removed.

## Step 3 — `NativeOwnedCollectionFilteredCountTests.cs`

Ran `grep -n "correlat"` and `grep -n "declines\|AssertDeclinesCleanly"` over the file. Confirmed this
file was ALREADY fully re-baselined for the `Where`-predicate correlated shape during Task 7:
`Correlated_element_predicate_now_goes_native_via_two_scope_translator_and_returns_correct_rows`
(around line 380-424) already asserts NativeOnly succeeds and returns correct (empty) rows, with a doc
comment explicitly referencing EF-421 and noting its "updated home" is `NativeOwnedCollectionCorrelatedTests`.
The remaining `declines`-tagged tests in the file (`Element_predicate_outside_the_renderable_set_declines`,
`Primitive_element_collection_filtered_count_declines`, `Filtered_count_nested_inside_a_quantifier_declines_...`,
the `Not`-over-value-converted-bool test, the array-leaf-sibling test) are all unrelated declines (regex,
primitive collection, quantifier nesting, value-converter, projection-mutation-safety) — none is the
correlated-predicate shape this task targets. **No change made to this file** — nothing left to do here,
as anticipated by the task context.

## Full regression pass (EF10)

- **FunctionalTests** (`dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build`):
  2993 passed, 0 failed, 52 skipped (all pre-existing encryption-test skips — `CRYPT_SHARED_LIB_PATH` unset).
  Ran with `MONGODB_URI`/`ATLAS_URI` unset, Docker-backed testcontainer.
- **UnitTests** (`dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build`):
  1011 passed, 0 failed, 0 skipped.
- Also ran a targeted pass first (`--filter "FullyQualifiedName~NativeOwnedCollectionAllTests|...Predicate...|...FilteredCount..."`):
  106/106 passed, confirming both renamed tests pass individually before the full-suite run.

## Multi-EF-version check (`/test-all`)

Pre-flight: net10.0 SDK present, Docker available, `MONGODB_URI`/`ATLAS_URI` both unset →
`SHARED_DB = false` → ran EF8/EF9/EF10 build+test in parallel via three background sub-agents, each
against the whole solution (`UnitTests` + `FunctionalTests` + `SpecificationTests`).

The sub-agents themselves hit their own 2-minute tool-call timeout mid-run and reported prematurely
("still waiting") — the underlying `dotnet test` OS processes kept running independently after the
sub-agent's foreground `Bash` call timed out. I monitored the actual log files directly (in the
scratchpad dir) until each produced all three `Test Run Successful` blocks, rather than trusting the
sub-agents' own premature reports.

| Version | Build | UnitTests | SpecificationTests | FunctionalTests | Total Passed | Failed | Skipped |
|---------|-------|-----------|---------------------|------------------|---------------|--------|---------|
| EF8     | OK    | 1018/1018 | 4714 passed, 11 skip (4725 total) | 2875 passed, 51 skip (2926 total) | 8607 | 0 | 62 |
| EF9     | OK    | 1018/1018 | 5027 passed, 11 skip (5038 total) | 2923 passed, 52 skip (2975 total) | 8968 | 0 | 63 |
| EF10    | OK    | 1011/1011 | 4599 passed, 14 skip (4613 total) | 2993 passed, 52 skip (3045 total) | 8603 | 0 | 66 |

**Zero failures on all three EF versions.** Total test counts differ slightly across EF versions
(e.g. EF9's SpecificationTests runs more tests than EF8/EF10's) — this is a pre-existing property of
EF Core's own version-specific Specification test bases, unrelated to this change (no `#if` was added
anywhere in the EF-421 plan). Skip counts are consistent with the documented encryption-test gate
(`CRYPT_SHARED_LIB_PATH` unset) plus a couple of pre-existing per-version skips. No version-specific
divergence in pass/fail outcome — exactly as expected.

## `Query/AGENTS.md` update

Added a new bullet immediately after the existing "Owned navigations" bullet (Step 8), documenting:
- The full shape now native: correlated `Count(pred)`/`Any`/`All` on owned collections.
- `MongoExpressionTranslator.SelfParam` — set by `NativeSlotPopulator` at every root-translator
  construction site — as the mechanism a quantifier/`Count(pred)` arm checks the element lambda's free
  parameter against via `ReferenceEquals`, building a two-scope child translator on a match instead of
  declining.
- `Count(pred)` renders the outer-scoped member inside the existing `$filter` via the new
  `MongoOuterFieldExpression` node (a `MongoFieldExpression` sibling that always resolves at document
  root even inside a `$filter`/`$map` scope).
- `Any`/`All` render via the new `$anyElementTrue`/`$allElementsTrue`-over-`$map` dialect
  (`MongoQuantifierExpression`), since `$elemMatch` cannot reference the enclosing document at all —
  and this is aggregation-expression-only, no query-dialect form, for the same reason as
  `MongoOuterFieldExpression`.
- `MongoExpressionNegator`'s De Morgan exception for `MongoQuantifierExpression`
  (`!Any(pred) ≡ All(!pred)`, `!All(pred) ≡ Any(!pred)`), narrow and deliberate against its own
  query-dialect gate, since the two quantifier renderings are exact duals.
- What's still out of scope: correlation nested two or more scopes deep.

Deliberately did NOT duplicate the existing "Scope must be resolved by parameter identity, never by
member name" durable invariant (line 38) — that bullet already covers the `ReferenceEquals`-against-
`SelfParam` rule generically across all scope-resolution sites (SelectMany, correlated-beyond-FK,
quantifiers), so the new bullet references it implicitly rather than restating it.

## Self-review

- Both stale decline tests retired and renamed consistently (including all in-file comment references
  to the old test names).
- Verified via targeted filter run (106/106) before the full-suite run, and via the full EF10
  FunctionalTests/UnitTests suites (0 failures) and all three EF versions via `/test-all` (0 failures on
  each).
- `git diff --stat` confirms exactly the three files touched by this task
  (`Query/AGENTS.md`, `NativeOwnedCollectionAllTests.cs`, `NativeOwnedCollectionPredicateTests.cs`) —
  `NativeOwnedCollectionFilteredCountTests.cs` correctly left untouched since Task 7 had already resolved
  its correlated-decline test.
- No `#if` was introduced anywhere in this task, consistent with the whole plan's global constraint.
- One process note (not a defect in the deliverable): the `/test-all` sub-agents' own tool-call timeout
  required me to fall back to monitoring the raw log files directly rather than trusting the sub-agents'
  self-reported (premature) status. Final counts were independently re-verified by grepping each log's
  `Test Run Successful`/`Total tests:`/`Passed:`/`Skipped:` lines myself.

## Issues or concerns

None. All decline tests retired as specified, full regression is 100% green at every EF version, and the
docs update accurately reflects the shipped scope of EF-421 as found in the current codebase
(`MongoOuterFieldExpression.cs`, `MongoQuantifierExpression.cs`, `MongoExpressionTranslator.SelfParam`,
`NativeSlotPopulator.cs`, `MongoExpressionNegator.cs`).
