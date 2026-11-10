# Task 1 Report: Restore the deleted foreign-queryable-extension regression test

## What changed and why

1. **Created** `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/InvalidQueryTranslationTests.cs`
   exactly as specified in `task-1-brief.md` Step 1 (Apache licence header, explanatory comment,
   `[XUnitCollection("QueryTests")]`, `IClassFixture<TemporaryDatabaseFixture>` primary-constructor
   style, `SimpleEntity` nested type, `FakeQueryableExtensions` internal static helper with the
   `FakeExecuteDelete` extension method). This restores the only regression test of the
   `AllowedQueryableExtensions` gate in `MongoQueryableMethodTranslatingExpressionVisitor.VisitMethodCall`
   — a gate that a prior PR left untested when it renamed the old test file, wrapped it in `#if EF8`,
   and dropped the helper. The new file is deliberately **not** `#if`'d so the gate is pinned on every
   EF version, since the EF9+ bulk-operator early-return sits directly above it in `VisitMethodCall`.

   The helper method is named `FakeExecuteDelete` (not `ExecuteDelete`) because on EF9/EF10 that name
   would collide with EF Core's real `EntityFrameworkQueryableExtensions.ExecuteDelete`, and C# overload
   resolution would likely bind to the real one — defeating the purpose of the test (it would exercise
   the bulk path instead of the foreign-extension gate).

2. **Modified** `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsUnsupportedOnEf8Tests.cs`
   to add `ExecuteUpdate_throws_translation_failure`, mirroring the existing
   `ExecuteDelete_throws_translation_failure` test, per Step 3 of the brief. This closes a gap where the
   EF8 "clean failure" boundary was pinned for `ExecuteDelete` but not for `ExecuteUpdate`. This addition
   sits inside the file's existing `#if EF8` guard, consistent with the rest of the file (EF8 doesn't
   implement the bulk path at all, so both operators should fail identically there).

No production code was touched. No other files were modified.

## Assertions adapted

None. Both assertions in `Foreign_queryable_extension_throws_translation_failure`
(`Assert.Contains("could not be translated", ex.Message)` and
`Assert.Contains("FakeExecuteDelete", ex.Message)`) passed as written, verbatim from the brief, on all
three EF configurations. No fallback/relaxation permitted by Step 2 was needed.

## Test commands run and results

### Step 2 — new `InvalidQueryTranslationTests` on all three configurations

```
$ unset MONGODB_URI ATLAS_URI
$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~InvalidQueryTranslationTests"
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 179 ms - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --filter "FullyQualifiedName~InvalidQueryTranslationTests"
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 175 ms - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" --filter "FullyQualifiedName~InvalidQueryTranslationTests"
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 176 ms - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)
```

All three: PASS, 1/1, both assertions intact. (The other three test assemblies in the solution — UnitTests
and SpecificationTests — reported "No test matches the given testcase filter", as expected since the new
test lives only in FunctionalTests.)

Note on speed: each run completed in well under a second of test execution time and did not incur a
multi-minute container pull, because a `mongodb/mongodb-atlas-local` TestContainers container from a
prior session in this environment was already warm/cached; `MONGODB_URI`/`ATLAS_URI` were left unset for
every run per the brief's instruction, so TestContainers auto-provisioning was exercised each time, it
just didn't need to pull the image fresh.

### Step 4 — EF8 `BulkOperationsUnsupportedOnEf8Tests` suite (2 tests)

```
$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" --filter "FullyQualifiedName~BulkOperationsUnsupportedOnEf8Tests"
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: 197 ms - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)
```

PASS, 2/2 as expected (`ExecuteDelete_throws_translation_failure` and the newly added
`ExecuteUpdate_throws_translation_failure`).

### Additional verification (not required by the brief, done for safety)

Full solution build for EF9 and EF10 (to confirm the new EF8-only `ExecuteUpdate_throws_translation_failure`
test, gated by the file's pre-existing `#if EF8`, does not affect compilation on other EF versions, and
that nothing else broke):

```
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
Build succeeded. 0 Warning(s), 0 Error(s)

$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
Build succeeded. 0 Warning(s), 0 Error(s)
```

## Things noticed but deliberately not changed

- `BulkOperationsUnsupportedOnEf8Tests.cs` and `InvalidQueryTranslationTests.cs` have no BOM (verified via
  `xxd` on the existing sibling file before creating the new one) — plain UTF-8 was used for the new file,
  consistent with the existing convention. No BOM needed preserving.
- The brief's fallback language for Step 2 (relaxing/removing the `FakeExecuteDelete` name assertion) was
  not exercised since the assertion passed as-is on every configuration; left as brief-specified.
- Did not touch `BulkOperationsUnsupportedOnEf8Tests.cs`'s surrounding comment block, licence header, or
  `#if EF8` guard placement — only inserted the new `[Fact]` method after the existing one, matching
  existing formatting/indentation exactly.
- Did not modify `.superpowers/sdd/2026-07-30-ef-107-bulk-remediation/progress.md` or any other plan
  document, per the "do not touch anything else" instruction.

## Commit

Single commit created with message exactly as specified in the brief's Step 5:

```
test: restore foreign-queryable-extension regression test, add EF8 ExecuteUpdate case
```
