# Task 7 Report: Close out `Optional_datetime_reading_null_from_database` + full three-EF regression sweep

## Branch check

`git branch --show-current` → `EF-322-Native-LINQ-rebased` at the start and re-confirmed before/after every command below. No unexpected branch switch occurred at any point during this task.

## Step 1: override still delegates to base

```
grep -n "Optional_datetime_reading_null_from_database" -A 1 tests/MongoDB.EntityFrameworkCore.SpecificationTests/Mapping/BuiltInDataTypesMongoTest.cs
```

Confirmed both overrides (the `#if !EF8` `Task`-returning one at line 55-56 and the EF8 `void` one at line 76-77) are pure `=> base.Optional_datetime_reading_null_from_database();` delegations. No change needed, as expected.

## Step 2/3: `BuiltInDataTypesMongo` filter under `NativeOnly`, all three EF versions

Commands run (in order), each preceded/followed by a branch check that stayed on `EF-322-Native-LINQ-rebased`:

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
MONGODB_EF_NATIVE_ONLY=1 dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~BuiltInDataTypesMongo"
  → Failed: 1, Passed: 28, Skipped: 1, Total: 30
  → The single failure is `Can_read_back_bool_mapped_as_int_through_navigation` (NativeTranslationNotSupportedException, non-entity projection under NativeOnly) — this is the deliberately-untouched, already-declared-unsupported navigation gap called out in the brief, not a regression.
  → Re-ran with `--logger "console;verbosity=detailed"` and grepped for the target test specifically: `Passed MongoDB.EntityFrameworkCore.SpecificationTests.Mapping.BuiltInDataTypesMongoTest.Optional_datetime_reading_null_from_database [7 ms]` — CONFIRMED PASSING under NativeOnly on EF10.

dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
MONGODB_EF_NATIVE_ONLY=1 dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" --no-build --filter "FullyQualifiedName~BuiltInDataTypesMongo"
  → Passed: 29, Skipped: 1, Failed: 0, Total: 30 — fully green as predicted (EF8 never had the navigation gap).

dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
MONGODB_EF_NATIVE_ONLY=1 dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~BuiltInDataTypesMongo"
  → Passed: 29, Skipped: 1, Failed: 0, Total: 30 — fully green as predicted.
```

`Optional_datetime_reading_null_from_database` is confirmed passing under `NativeOnly` on all three EF versions (it is part of the 28/29 passed counts above on each; explicitly verified by name on EF10, and it is not one of the 6 unrelated failures found in Step 4 below on any EF version, since the full-suite spec failures are all in `NorthwindSelectQueryMongoTest`/`NorthwindMiscellaneousQueryMongoTest`, not `BuiltInDataTypesMongoTest`).

## Step 4: full three-EF-version regression suite

Commands run (build then `dotnet test ... --no-build`, no filter, default `MongoQueryMode.Native`, no `MONGODB_EF_NATIVE_ONLY`):

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build
  UnitTests:            Passed: 988,  Failed: 0, Skipped: 0,  Total: 988
  SpecificationTests:   Passed: 4593, Failed: 6, Skipped: 14, Total: 4613  ← 6 failures, see below
  FunctionalTests:      Passed: 2964, Failed: 0, Skipped: 52, Total: 3016

dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build
  UnitTests:            Passed: 995,  Failed: 0, Skipped: 0,  Total: 995
  SpecificationTests:   Passed: 5021, Failed: 6, Skipped: 11, Total: 5038  ← same 6 failures
  FunctionalTests:      Passed: 2894, Failed: 0, Skipped: 52, Total: 2946

dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" --no-build
  UnitTests:            Passed: 995,  Failed: 0, Skipped: 0,  Total: 995
  SpecificationTests:   Passed: 4708, Failed: 6, Skipped: 11, Total: 4725  ← same 6 failures
  FunctionalTests:      Passed: 2846, Failed: 0, Skipped: 51, Total: 2897
```

Branch re-confirmed `EF-322-Native-LINQ-rebased` before and after each build/test invocation.

### The 6 failures (identical set, identical root cause, on all three EF versions)

All 6 are in `MongoDB.EntityFrameworkCore.SpecificationTests`, all are `AssertMql`/`TestMqlLoggerFactory.AssertBaseline` string-comparison failures (i.e. the query **results** are not being asserted as wrong — only the recorded expected MQL string literal in the test no longer matches what's now emitted), and all trace back to the native conditional/date-part translation this 8-task plan added (Tasks 1-6, most recent commits: `be586cc`, `80f4654`, `9c7fb82`, `f5cf964`, `9d807d6`, ... `c87d85e`):

1. `NorthwindSelectQueryMongoTest.Select_datetime_DayOfWeek_component(async: False)` and `(async: True)`
   - Expected: `{ "$subtract" : [{ "$dayO"...` / Actual: `{ "$toInt" : { "$subtract"...` — the native `DayOfWeek` translation now wraps in `$toInt`; the hardcoded baseline predates that.
2. `NorthwindSelectQueryMongoTest.Select_bool_constant(async: False)` and `(async: True)`
   - Expected: `{ "$eq" : ["$_id", "ALFKI"...` / Actual: `{ "$cond" : { "if" : { "$"...` — a boolean-constant projection is now natively translated through `$cond` (the new `MongoConditionalExpression` path) instead of the old `$eq` shape.
3. `NorthwindMiscellaneousQueryMongoTest.OrderBy_conditional_operator(async: False)` and `(async: True)`
   - Expected: `"Customers.{ "$project" : { "_id" : 0, "_d"...` / Actual: `"Customers.{ "$set" : { "__sort0" : { "$co"...` — an `OrderBy` over a conditional expression is now natively translated (using `$set` + `$cond` for the sort key) instead of falling back to driver-LINQ's `$project` shape.

Per `AGENTS.md`, "the exact aggregation-pipeline (MQL) emitted for a given query" is explicitly **not** a breaking change and is expected to shift when the native translator picks up new cases — but that's exactly why these three tests hardcode `AssertMql` baselines that assert the literal emitted pipeline: those literal strings are now stale artifacts of Tasks 1-6's own work landing new native translation for conditionals and `DayOfWeek`, and nobody updated the recorded baselines in `NorthwindSelectQueryMongoTest.cs` / `NorthwindMiscellaneousQueryMongoTest.cs` to match the new (correct, intended) native MQL shape.

This is **not** a result-correctness regression (no evidence any query now returns wrong data — every failure is purely a string-literal MQL-shape assertion), but it is a real, reproducible regression in the sense that these three spec tests fail identically and consistently across all three EF versions, on a clean build/test with no filter, no env var overrides. Per this task's explicit instruction not to guess at a fix for something Task 7 didn't introduce, I did not touch these test files — this needs to go back to whichever of Tasks 1-6 introduced native `$cond`/`DayOfWeek`/conditional-OrderBy translation (or a small follow-up task) to update the recorded `AssertMql` baselines in:
- `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindSelectQueryMongoTest.cs` (`Select_datetime_DayOfWeek_component` line ~1915/2107, `Select_bool_constant` line ~911/2107)
- `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindMiscellaneousQueryMongoTest.cs` (`OrderBy_conditional_operator` line ~2132/5525)

## Step 5: commit

No commit made — this task is verification-only per its own instructions, and since a real (non-Task-7) issue was found rather than fixed, there is nothing to commit here regardless.

## Update: baseline rewrite performed (per coordinator ruling)

The coordinator ruled the 6 failures above expected fallout from Tasks 1-6 (a changed-but-non-breaking MQL shape for a supported query, per `AGENTS.md`'s versioning rubric), not a regression — the fix is to regenerate the stale `AssertMql` baselines per the documented procedure in `tests/MongoDB.EntityFrameworkCore.SpecificationTests/AGENTS.md` ("Regenerating MQL baselines").

### Rewrite run (EF10 only — baselines are shared source across EF versions)

Branch re-confirmed `EF-322-Native-LINQ-rebased` before starting; working tree was clean.

```
EF_TEST_REWRITE_BASELINES=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindSelectQueryMongoTest.Select_datetime_DayOfWeek_component"
EF_TEST_REWRITE_BASELINES=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindSelectQueryMongoTest.Select_bool_constant"
EF_TEST_REWRITE_BASELINES=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindMiscellaneousQueryMongoTest.OrderBy_conditional_operator"
```

Each ran per-method (the `|`-combined filter syntax wasn't tried for the rewrite pass; each test reported FAILED as documented — the signal a rewrite happened). `git diff --stat` afterward showed only the two expected test files touched.

**Corruption found, per the documented caveat.** The auto-rewriter flattened the indented C# raw-string literals to flush-left (e.g. `            """` / `            Customers....` / `            """);` became `            """` / `Customers....` / `""");`), matching the "auto-rewriter can corrupt some files" warning. Content (the actual MQL text) was correct in every case — only indentation/formatting was wrong. I hand-fixed all three occurrences (`Select_bool_constant`, `Select_datetime_DayOfWeek_component` in `NorthwindSelectQueryMongoTest.cs`; `OrderBy_conditional_operator` in `NorthwindMiscellaneousQueryMongoTest.cs`) back to the file's normal 12-space-indented raw-string style, then re-diffed to confirm only the MQL-string content differed line-for-line from the pre-rewrite version — no other code touched.

Final diff (3 lines changed across 2 files):
- `Select_bool_constant`: `{ "$eq" : [...] }` → `{ "$cond" : { "if" : { "$eq" : [...] }, "then" : true, "else" : false } }`
- `Select_datetime_DayOfWeek_component`: `{ "$subtract" : [...] }` → `{ "$toInt" : { "$subtract" : [...] } }`
- `OrderBy_conditional_operator`: `$project`/`$replaceRoot` shape → `$set`/`$sort`/`$unset` shape (native sort-key materialization)

### Re-verification without the rewrite env var, all three EF versions

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test tests/.../SpecificationTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindSelectQueryMongoTest.Select_datetime_DayOfWeek_component|FullyQualifiedName~NorthwindSelectQueryMongoTest.Select_bool_constant|FullyQualifiedName~NorthwindMiscellaneousQueryMongoTest.OrderBy_conditional_operator"
  → Passed: 8, Failed: 0, Skipped: 0, Total: 8   (8 = 4 test methods × async True/False; the OR-filter also picked up the neighboring already-passing OrderBy_conditional_operator_where_condition_false)

dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9" && dotnet test ... -c "Debug EF9" --no-build --filter (same)
  → Passed: 8, Failed: 0, Skipped: 0, Total: 8   (no separate EF9 rewrite pass needed — same shared source file, already green after the EF10-driven fix)

dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8" && dotnet test ... -c "Debug EF8" --no-build --filter (same)
  → Passed: 8, Failed: 0, Skipped: 0, Total: 8   (same — no separate EF8 rewrite pass needed)
```

### Full three-EF-version regression suite, re-run after the fix

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build
  UnitTests:          Passed: 988,  Failed: 0, Skipped: 0,  Total: 988
  SpecificationTests: Passed: 4599, Failed: 0, Skipped: 14, Total: 4613   (was 4593/6/14 — now fully green)
  FunctionalTests:    Passed: 2964, Failed: 0, Skipped: 52, Total: 3016

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build
  UnitTests:          Passed: 995,  Failed: 0, Skipped: 0,  Total: 995
  SpecificationTests: Passed: 5027, Failed: 0, Skipped: 11, Total: 5038   (was 5021/6/11 — now fully green)
  FunctionalTests:    Passed: 2894, Failed: 0, Skipped: 52, Total: 2946

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" --no-build
  UnitTests:          Passed: 995,  Failed: 0, Skipped: 0,  Total: 995
  SpecificationTests: Passed: 4714, Failed: 0, Skipped: 11, Total: 4725   (was 4708/6/11 — now fully green)
  FunctionalTests:    Passed: 2846, Failed: 0, Skipped: 51, Total: 2897
```

Zero failures anywhere, across all three EF versions, all three test projects.

### Re-confirmed `MONGODB_EF_NATIVE_ONLY=1` BuiltInDataTypesMongo (unaffected by the rebaseline)

```
EF10: Failed: 1, Passed: 28, Skipped: 1, Total: 30   (still only the pre-known, deliberately-untouched Can_read_back_bool_mapped_as_int_through_navigation gap)
EF9:  Failed: 0, Passed: 29, Skipped: 1, Total: 30   (fully green)
EF8:  Failed: 0, Passed: 29, Skipped: 1, Total: 30   (fully green)
```

### Commit

Committed as `9e8e660` on `EF-322-Native-LINQ-rebased`:

```
Rebaseline MQL assertions for DayOfWeek/bool-constant/conditional-OrderBy now going native
```

Branch confirmed `EF-322-Native-LINQ-rebased` before and after the commit; working tree clean afterward (`git status --short` empty).

## Summary (final)

- `Optional_datetime_reading_null_from_database`: CONFIRMED PASSING under `NativeOnly` on EF8, EF9, and EF10 — the motivating test for this whole plan is closed out.
- `BuiltInDataTypesMongoTest` under `NativeOnly`: fully green on EF8 and EF9 (29 passed / 1 skipped / 0 failed each); green except for the one pre-known, deliberately-untouched `Can_read_back_bool_mapped_as_int_through_navigation` gap on EF10 (28 passed / 1 skipped / 1 failed) — exactly as predicted by the brief.
- Full three-EF-version suite: **NOW FULLY GREEN** on all three EF versions, all three test projects (Unit/Functional/Spec), zero failures anywhere, after regenerating and hand-fixing the 3 stale `AssertMql` baselines (`Select_bool_constant`, `Select_datetime_DayOfWeek_component`, `OrderBy_conditional_operator`) that had drifted from Tasks 1-6 correctly extending native translation. Change committed as `9e8e660`.
