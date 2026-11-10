# Task 6 report: Functional differential tests — real server, all three query modes

## What was implemented

Created `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeConditionalAndDateTimeTests.cs` following
the brief and the `NativeEnumCastProjectionTests.cs` pattern: `[XUnitCollection("QueryTests")]`,
`IClassFixture<TemporaryDatabaseFixture>`, `SingleEntityDbContext.Create`, and three test methods:

1. `Conditional_null_check_over_DateTimeOffset_matches_across_all_three_modes` — asserts `NativeOnly`, `Native`,
   `DriverLinq`, and in-memory LINQ all agree on `x.OccurredOffset == null ? (DateTime?)null : x.OccurredOffset.Value.DateTime.Date`.
2. `DateTimeOffset_date_part_matches_in_memory_LINQ_under_NativeOnly` (theory, 8 cases: Year/Month/Day/Hour/
   Minute/Second/DayOfWeek/DayOfYear) — asserts each native date-part projection matches in-memory LINQ.
3. `Plain_DateTime_Date_matches_in_memory_LINQ_under_NativeOnly` — asserts plain `DateTime.Date` under native.

Seed data (3 rows) covers a positive UTC offset (+02:00), a negative UTC offset (-05:00), and a null
`OccurredOffset` row, per the brief.

The added `using MongoDB.Bson;` / `using MongoDB.Driver;` (not present in the brief's snippet, since those
namespaces are not covered by this project's global usings — confirmed against `Usings.cs` and sibling test
files such as `DateTimeOffsetMemberProjectionTests.cs`) were necessary for the file to compile.

## Test run

Docker was available; `MONGODB_URI`/`ATLAS_URI` were left unset so `TestServer` auto-provisioned a
`mongodb/mongodb-atlas-local` TestContainers instance (confirmed running via `docker ps` during the run —
`mongodb/mongodb-atlas-local:latest Up ... 0.0.0.0:59799->27017/tcp`).

Command:
```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeConditionalAndDateTimeTests"
```

### First run (as originally written per the brief) — 8 failures

All 8 `[Theory]` cases of `DateTimeOffset_date_part_matches_in_memory_LINQ_under_NativeOnly` failed; the two
`[Fact]` tests passed. Every failure was the **same** exception, thrown at query-compile time (never reached
the server):

```
MongoDB.EntityFrameworkCore.Query.NativeTranslation.NativeTranslationNotSupportedException :
Query projects a non-entity result and MongoQueryMode.NativeOnly forbids the driver-LINQ fallback.
   at MongoDB.EntityFrameworkCore.Query.Visitors.MongoShapedQueryCompilingExpressionVisitor.ThrowIfNativeOnlyForbidsFallback(...)
   at MongoDB.EntityFrameworkCore.Query.Visitors.MongoShapedQueryCompilingExpressionVisitor.VisitProjectedQuery(...)
   ...
```

Failed: 8, Passed: 2, Skipped: 0, Total: 10.

**Root-cause diagnosis:** the brief's theory test's projections are written as
`Select(x => (object)x.OccurredOffset!.Value.Year)` — a boxing `Convert` to `object` INSIDE the LINQ expression
tree (needed at the C# level because the `switch` must return a common `object[]` across differently-typed
parts). That `Convert(..., typeof(object))` node reaches `MongoExpressionTranslator.TranslateOperand`, whose
`Convert`/`ConvertChecked` branch only emits a `$toX` aggregation operator when
`MongoConvertExpression.ToOperatorFor(toType)` is non-null — and that method only maps `int`/`long`/`double`/
`decimal`, never `object` (confirmed by reading `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs:956-985`
and `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoConvertExpression.cs:76-84`). So `TryTranslateValue`
declines for ANY leaf boxed to `object` inside the tree — a plain field boxed the same way would decline
identically; this has nothing to do with date parts, null handling, or `$dayOfWeek` numbering. It is a
pre-existing translator limitation (boxing-cast-to-`object` support was never built for a projection leaf,
scalar or computed), outside this task's scope (Tasks 1-5 only added conditional expressions and date-part
member access, not general object-boxing support).

This is **not** one of the two named empirical risks (null/missing-date server errors; `$dayOfWeek` numbering)
and it is **not a server-side error at all** — the exception is thrown by the compile-time gate before any
pipeline is built or sent to MongoDB. Per the brief's escalation rule ("if fixing a discovered issue would
require touching more than the renderer's date-operator rendering ... don't silently patch those, report what
you found instead"), no production code was touched for this. Instead the **test** was fixed: the boxing to
`object` was moved to AFTER materialization (`.ToArray().Cast<object>().ToArray()`), so each part's `Select`
projects its own natural CLR type (`int` or `DayOfWeek`) with no `Convert(..., typeof(object))` node ever
reaching the translator — this still exercises exactly what the task needs to measure (native date-part
translation vs. in-memory LINQ) without tripping the unrelated boxing gap. The in-memory (`expected`) side
still boxes inline in plain C#, which is unaffected since it never goes through the translator.

### Second run (after the test fix) — all pass

```
Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10, Duration: 438 ms - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)
```

All 10 tests (1 + 8 theory cases + 1) passed against the real, auto-provisioned MongoDB server.

## Findings on the two named empirical risks

1. **Null/missing-date server errors** — NOT observed. `Conditional_null_check_over_DateTimeOffset_matches_across_all_three_modes`
   passed cleanly across `NativeOnly`/`Native`/`DriverLinq`/in-memory LINQ, including the null-`OccurredOffset`
   row ("c"). No `$dateAdd`/date-extraction operator hard-errored on a null/missing input in this test's shapes;
   no `$ifNull` renderer guard was needed.
2. **`$dayOfWeek` numbering mismatch** — the `DayOfWeek` theory case passed against the real server, confirming
   the Task 2 renderer-side subtraction (converting MongoDB's `$dayOfWeek` 1-7/Sunday-first numbering to .NET's
   `DayOfWeek` 0-6/Sunday-first enum) holds empirically, not just at the unit/BSON-assertion level.

## Files changed

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeConditionalAndDateTimeTests.cs` (new file)

No production code (`src/`) was changed.

## Self-review

- **Completeness:** all three test methods from the brief are present; null-check conditional differential
  test, date-part-vs-in-memory-LINQ theory test (all 8 parts), and the plain-`DateTime.Date` test are all
  implemented and passing.
- **Quality:** seed data covers positive offset (+02:00, row "a"), negative offset (-05:00, row "b"), and a
  null `OccurredOffset` (row "c"), matching the brief's stated coverage intent.
- **Discipline:** no production code changes — the one issue found (`object`-boxing-cast gap) was diagnosed as
  a pre-existing, out-of-scope translator limitation and worked around in the test rather than patched in
  `src/`. No defensive `$ifNull` fix was added since risk #1 (null/missing-date server errors) did not
  materialize.
- **Testing:** all 10 tests ran against a real, auto-provisioned `mongodb-atlas-local` TestContainers server
  (verified via `docker ps` during the run) — not skipped, not mocked. Final result: 10/10 passed.

## Issues / concerns

- The brief's exact code (as given) does not compile/run cleanly against a real server as-is for the theory
  test — the boxing-to-`object` cast inside the LINQ `Select` trips a pre-existing, unrelated translator gap.
  This is a latent, real (if narrow) gap in the native projection binder — boxing any leaf to `object` inside
  an expression tree always declines under `NativeOnly` today — but it is out of this task's scope per the
  brief's own escalation boundary, so it was not fixed in production code. Worth flagging for a future ticket
  if boxed-`object` projections are expected to be supported.
