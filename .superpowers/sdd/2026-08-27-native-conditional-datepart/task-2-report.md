# Task 2 Report: Renderer arms (`MongoAggregationExpressionRenderer`)

## Fix (post-review)

**Finding (Important):** `DatePart_renders_as_the_matching_operator` originally worked around the CS0051
problem (`MongoDatePart` is `internal`, can't be a public `[Theory]` parameter type) by passing
`(int)MongoDatePart.Year` etc. through `[InlineData]` and casting back with `(MongoDatePart)partValue`. The
reviewer correctly flagged this: round-tripping through a raw `int` has no compile-time/attribute-level
connection to `MongoDatePart` at all, so an out-of-range int would silently reconstruct an invalid enum value
with no safety net. Task 1 had already hit and fixed the identical problem for the same enum by converting to
a `[MemberData]`-driven theory that boxes the actual enum value in `object[]` rows (see
`MongoConditionalAndDatePartExpressionTests.cs`'s `DatePartTestData()`), which is also the pre-existing idiom
in the sibling `MongoAggregationExpressionRendererTests.cs` for `MongoBinaryOperator` (also internal).

**Change:** Converted `DatePart_renders_as_the_matching_operator` in
`tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererDatePartTests.cs`
from `[InlineData((int)MongoDatePart.Year, "$year")]`-style rows to `[MemberData(nameof(DatePartOperatorTestData))]`,
where `DatePartOperatorTestData()` is a new `static IEnumerable<object[]>` yielding rows like
`[MongoDatePart.Year, "$year"]` — boxing the real enum constant, not its underlying int. The theory parameter
is now `object part`, cast back via `(MongoDatePart)part` inside the method body. Added
`using System.Collections.Generic;` for the `IEnumerable<object[]>` return type. Updated the explanatory
comment above the theory to reference the correct established idiom (`MongoConditionalAndDatePartExpressionTests.cs`'s
`DatePartTestData()`) instead of describing the now-removed int round-trip.

**Verification:**

`dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoAggregationExpressionRendererDatePartTests"`

```
Passed!  - Failed:     0, Passed:    13, Skipped:     0, Total:    13, Duration: 30 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```

Also re-ran the full unit-suite regression check:
`dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"`

```
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7, Duration: 66 ms - MongoDB.EntityFrameworkCore.SpecificationTests.dll (net10.0)
Passed!  - Failed:     0, Passed:   971, Skipped:     0, Total:   971, Duration: 906 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```

Zero regressions. Committed as a new commit `a666e40` ("Fix review finding: box MongoDatePart via
[MemberData] instead of round-tripping an int"), not amended onto the original commit `649157a`.

---


## What was implemented

Extended `MongoAggregationExpressionRenderer` (in
`src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs`) to render the
three Task-1 IR node types:

- **`Render` switch** (immediately after the existing `MongoConvertExpression` arm, before the catch-all):
  - `MongoConditionalExpression` → `{$cond: {if, then, else}}`.
  - `MongoDateTimeOffsetLocalExpression` → `{$dateAdd: {startDate: "<field>.DateTime", unit: "minute", amount: "<field>.Offset"}}`.
  - `MongoDatePartExpression` → dispatched to a new `RenderDatePart` helper.
- **`CanRender` switch** (same insertion point): `MongoConditionalExpression` (all three sub-expressions must
  render), `MongoDateTimeOffsetLocalExpression` (operand must render), `MongoDatePartExpression` (operand must
  render).
- **New `RenderDatePart` private helper** (placed just before `RenderSize`, after `FieldRef`), mapping each
  `MongoDatePart` to its MQL operator: `Year`→`$year`, `Month`→`$month`, `Day`→`$dayOfMonth`, `Hour`→`$hour`,
  `Minute`→`$minute`, `Second`→`$second`, `Millisecond`→`$millisecond`, `DayOfYear`→`$dayOfYear`,
  `DayOfWeek`→`{$subtract: [{$dayOfWeek: <date>}, 1]}` (correctness transform for .NET's 0-based `DayOfWeek`
  vs. MongoDB's 1-based `$dayOfWeek`), `Date`→`{$dateTrunc: {date, unit: "day"}}`. All 10 `MongoDatePart`
  members are handled (9 mapped operators + a defensive `_ => throw` for exhaustiveness).

New test file:
`tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererDatePartTests.cs`
— follows the brief's code exactly, with one adaptation (see Issues below).

## TDD evidence

**RED** — `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoAggregationExpressionRendererDatePartTests"`

Build succeeded (the switches' catch-all arms compile fine against unhandled node types), but all 13 tests
failed as expected:
```
Failed!  - Failed:    13, Passed:     0, Skipped:     0, Total:    13, Duration: 29 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```
Failures were `NativeTranslationNotSupportedException: MongoAggregationExpressionRenderer does not support
node type 'MongoDatePartExpression'/'MongoDateTimeOffsetLocalExpression'/'MongoConditionalExpression'` and
one `Assert.True() Failure` for the `CanRender` theory — exactly the expected RED shape.

**GREEN** — same command after implementation:
```
Passed!  - Failed:     0, Passed:    13, Skipped:     0, Total:    13, Duration: 27 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```

## Full-suite regression run

`dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"`

```
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7, Duration: 217 ms - MongoDB.EntityFrameworkCore.SpecificationTests.dll (net10.0)
Passed!  - Failed:     0, Passed:   971, Skipped:     0, Total:   971, Duration: 1 s - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```
Zero regressions, pristine output (only pre-existing NU1903 SSH.NET advisory warnings, unrelated to this
change).

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs` — new
  `Render`/`CanRender` arms + `RenderDatePart` helper.
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererDatePartTests.cs`
  — new test file.

## Self-review

- **Completeness:** Both `Render` and `CanRender` got all 3 new arms, inserted exactly where the brief
  specified (right after the `MongoConvertExpression` arm, before each switch's catch-all), without touching
  or reordering any existing arm. `RenderDatePart` handles all 10 `MongoDatePart` members (Year, Month, Day,
  Hour, Minute, Second, Millisecond, DayOfYear, DayOfWeek, Date).
- **Quality:** Matches the existing file's style (expression-bodied switch arms, private static helper next to
  `RenderSize`/`RenderFilteredSize`). The `$dayOfWeek` subtraction has an explanatory comment matching the
  brief's wording, explaining it is a mandatory correctness transform, not defensive.
- **Discipline:** Implemented exactly what the brief specified — no additional refactoring, no scope creep.
- **Testing:** Full unit suite passes (971/971 + 7/7 spec-unit), zero regressions, pristine output.

## Issues encountered

The brief's test code used `[InlineData(MongoDatePart.Year, "$year")]` etc. directly on a public `[Theory]`
method. `MongoDatePart` is an `internal` enum, and C# does not allow a `public` method to expose a
less-accessible type in its signature (`CS0051`) — this is a hard language rule that `InternalsVisibleTo` does
not bypass, since the test class itself must stay `public` for xUnit discovery. This is not new to this
brief: `MongoAggregationExpressionRendererTests.cs` (the pre-existing sibling test file) documents and works
around the identical issue for `MongoExpression`-typed `[Theory]` parameters via a `[MemberData]` +
`object`-boxing idiom.

I adapted the `DatePart_renders_as_the_matching_operator` theory to pass the enum value's underlying `int`
(`(int)MongoDatePart.Year`, etc.) and cast it back to `MongoDatePart` inside the test body, preserving the
brief's requested per-operator `[Theory]`/`[InlineData]` shape (rather than switching to the
`[MemberData]`-over-boxed-`object`s idiom, which would have been a bigger structural deviation for a
single-parameter case). Added a comment above the theory referencing the established idiom in
`MongoAggregationExpressionRendererTests.cs`. All other test code is verbatim from the brief.
