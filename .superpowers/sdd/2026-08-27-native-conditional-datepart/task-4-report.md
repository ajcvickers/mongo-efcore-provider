# Task 4 report — Translation: date-part member chains (MongoExpressionTranslator)

## What was implemented

In `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs`:

1. `DatePartsByMemberName` — a `private static readonly Dictionary<string, MongoDatePart>` lookup table, added
   immediately before `WideningNumericConversions` (same static-lookup-table section), mapping `DateTime`
   member names (`Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`, `Millisecond`, `DayOfWeek`, `DayOfYear`,
   `Date`) to their `MongoDatePart` enum value. `TimeOfDay` is deliberately absent.

2. `TryTranslateDateTimeMember(Expression node, out MongoExpression? result)` — a new private instance method,
   added right after `TryTranslateValue`'s closing brace. Resolves a `DateTime`/`DateTimeOffset` member-access
   chain:
   - Declines anything that isn't a `MemberExpression` with a receiver, or whose (nullable-unwrapped) receiver
     type isn't `DateTime`/`DateTimeOffset`.
   - Resolves the receiver either via `TryResolveMember` (a plain stored field — including through a `.Value`
     hop, which `TryResolveMember` already peels generically) or recursively via
     `TryTranslateDateTimeMember` itself (composing a prior date-member hop, e.g. `.DateTime` before `.Date`).
   - For a `DateTimeOffset` receiver: `.UtcDateTime` → `MongoElementRefExpression("<field>.DateTime", ...)`
     (raw UTC subfield, no offset math); `.DateTime`/`.LocalDateTime` → `MongoDateTimeOffsetLocalExpression`
     wrapping the field; any other date-part member → `MongoDatePartExpression` wrapping a
     `MongoDateTimeOffsetLocalExpression` of the field.
   - For a plain `DateTime` receiver: direct `MongoDatePartExpression` over the resolved receiver expression,
     with no offset reconstruction.

3. New call site in `TranslateOperand`, inserted immediately after Task 3's `ConditionalExpression` block and
   before the existing `TryResolveMember` check:
   ```csharp
   if (TryTranslateDateTimeMember(node, out var dateTimeMember))
       return dateTimeMember;
   ```

In `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoConditionalAndDateTimeTranslatorTests.cs`:
appended the 7 test cases specified in the brief verbatim (plain `DateTime.Year`, `DateTimeOffset.Year` local
reconstruction, the 3-hop `OccurredOffset!.Value.DateTime.Date` chain, `UtcDateTime`, bare `.DateTime`,
`TimeOfDay` decline, and a conditional branch containing a date part) — no changes to the existing `Row`
fixture, `BuildValueBody` helper, or Task 3's 4 tests.

The brief's code matched the actual current state of the file exactly — no deviations were needed. In
particular `TryResolveMember`'s signature
(`private bool TryResolveMember(Expression node, [NotNullWhen(true)] out IProperty? property, [NotNullWhen(true)] out string? fieldPath)`)
matched what the brief assumed, and the insertion point (right after Task 3's `ConditionalExpression` case,
before `if (TryResolveMember(node, ...))` at what was line 904) was exactly as described.

## TDD evidence

**RED** — `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoConditionalAndDateTimeTranslatorTests"`
(run after appending tests only, before implementation):
```
Failed!  - Failed:     6, Passed:     5, Skipped:     0, Total:    11, Duration: 37 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```
The 6 failures were exactly the date-part tests expected to fail (`Plain_DateTime_Year_...`,
`DateTimeOffset_Year_...`, `DateTimeOffset_Value_DateTime_Date_...`, `DateTimeOffset_UtcDateTime_...`,
`DateTimeOffset_DateTime_alone_...`, `Conditional_branch_with_a_date_part_translates`). `TimeOfDay_declines`
passed already (5th passing test, alongside Task 3's 4), exactly as the brief predicted ("everything declines
today").

**GREEN** — same command, after implementation:
```
Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11, Duration: 37 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```

## Full-suite regression run

`dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"`:
```
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7, Duration: 68 ms - MongoDB.EntityFrameworkCore.SpecificationTests.dll (net10.0)
Passed!  - Failed:     0, Passed:   982, Skipped:     0, Total:   982, Duration: 956 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```
Zero regressions (982 unit tests + 7 spec tests, all green).

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs`
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoConditionalAndDateTimeTranslatorTests.cs`

## Self-review findings

- **Completeness**: all 7 new tests pass, all pre-existing tests (4 from Task 3, plus the full unit suite)
  still pass.
- **Multi-hop composition (trickiest part)**: verified explicitly via
  `DateTimeOffset_Value_DateTime_Date_three_hop_chain_composes` — `OccurredOffset!.Value.DateTime.Date`
  resolves as: `.Value` peeled generically inside `TryResolveMember` (confirmed by reading
  `MongoExpressionTranslator.Members.cs`, which documents "Peel Nullable<T>.Value" as its first step) →
  `.DateTime` recognized as a `DateTimeOffset` receiver, wraps the resolved field in
  `MongoDateTimeOffsetLocalExpression` → `.Date` recognized as a plain-`DateTime`-typed receiver (the CLR type
  of a `MongoDateTimeOffsetLocalExpression` is `DateTime`), resolved via the recursive
  `TryTranslateDateTimeMember(receiver, ...)` branch, then wrapped in `MongoDatePartExpression(Date)`. Test
  assertions confirm the exact resulting shape (`MongoDatePartExpression.Part == Date`, `.Operand` is
  `MongoDateTimeOffsetLocalExpression` whose `.Operand.ElementName == "OccurredOffset"`).
- **Discipline**: `TimeOfDay` is not implemented — absent from `DatePartsByMemberName`, and the
  `TimeOfDay_declines` test (asserting `TryTranslateValue` returns `false`) passes. Nothing else beyond the
  brief's scope was touched; the diff is additive-only within the two specified files.
- **Testing**: pristine output on both the task-filtered run and the full unit-test run; no flaky or skipped
  tests.

## Issues

None. The brief's code compiled and worked against the actual current file state without modification.

## Fix round: AllFieldsDefaultSerialized guard for MongoConditionalExpression / MongoDatePartExpression / MongoDateTimeOffsetLocalExpression

Review flagged an Important finding: `MongoExpressionTranslator.AllFieldsDefaultSerialized` had no arm for
`MongoConditionalExpression` (Task 3), `MongoDatePartExpression`, or `MongoDateTimeOffsetLocalExpression`
(both Task 4) — all three fell into the catch-all `_ => true`. This is a silent-wrong-data risk: a
`DateTime`/`DateTimeOffset` property with a non-default `[BsonRepresentation]` or a `ValueConverter` would
still be admitted into native `.Year`/`.Date`/etc. translation and rendered as a raw MQL date operator against
a field that isn't actually stored as a raw BSON date.

### What was changed

In `MongoExpressionTranslator.AllFieldsDefaultSerialized` (`src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs`),
added three new switch arms, inserted right after the existing `MongoUnaryExpression` arm and before the
`MongoInExpression`/`MongoSizeExpression`/`MongoArrayContainsExpression` explanatory comments:

```csharp
MongoConditionalExpression cond => AllFieldsDefaultSerialized(cond.Test)
    && AllFieldsDefaultSerialized(cond.IfTrue)
    && AllFieldsDefaultSerialized(cond.IfFalse),
MongoDatePartExpression datePart => AllFieldsDefaultSerialized(datePart.Operand),
MongoDateTimeOffsetLocalExpression local => AllFieldsDefaultSerialized(local.Operand),
```

`MongoElementRefExpression` was deliberately left alone — it carries no backing `IProperty` to check, matching
existing precedent for that node type elsewhere (GroupBy/Distinct flattening), per the review's explicit
instruction not to guard it.

### Regression tests added

Appended to `MongoConditionalAndDateTimeTranslatorTests.cs`:
- A new helper, `BuildValueBodyWithNonDefaultSerialization`, mirroring the existing
  `MongoExpressionTranslatorTests.BuildOrderPredicateBody` pattern — builds a `Row` model with a caller-supplied
  `Action<ModelBuilder>` so a property can be given a `HasConversion` value converter.
- `DatePart_over_a_value_converted_DateTime_declines` — `.Year` over a converted `Occurred` now declines.
- `DatePart_over_a_value_converted_DateTimeOffset_declines` — `.Year` over a converted `OccurredOffset` (the
  `MongoDateTimeOffsetLocalExpression`-wrapping path) now declines.
- `Conditional_branch_over_a_value_converted_field_declines` — bonus test per the brief's "if you can, do it
  too" ask: a conditional branch reaching a converted `Amount` field now declines the whole conditional.

**Proved these pin real behavior, not tautologies:** stashed the `MongoExpressionTranslator.cs` fix (keeping
the tests), rebuilt, and reran just the 3 new tests — all 3 failed (`Assert.False() Failure: Expected: False,
Actual: True`), confirming they exercised the actual bug before the fix. Then restored the fix
(`git stash pop`) and rebuilt clean.

### Test commands and output

RED (fix reverted, tests present):
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~DatePart_over_a_value_converted|FullyQualifiedName~Conditional_branch_over_a_value_converted"
Failed!  - Failed:     3, Passed:     0, Skipped:     0, Total:     3, Duration: 23 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```

GREEN (fix restored) — task-filtered:
```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoConditionalAndDateTimeTranslatorTests"
Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14, Duration: 38 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```
(11 from before the fix round + 3 new regression tests = 14.)

Full-suite regression:
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7, Duration: 65 ms - MongoDB.EntityFrameworkCore.SpecificationTests.dll (net10.0)
Passed!  - Failed:     0, Passed:   985, Skipped:     0, Total:   985, Duration: 879 ms - MongoDB.EntityFrameworkCore.UnitTests.dll (net10.0)
```
Zero regressions (985 unit tests, up from 982 by the 3 new tests; 7 spec tests unchanged).

### Commit

`9c7fb82` "Guard AllFieldsDefaultSerialized against MongoConditionalExpression/MongoDatePartExpression/MongoDateTimeOffsetLocalExpression"
— 2 files changed, 73 insertions(+), on top of the original Task 4 commit `f5cf964` (not amended, per
instructions).
