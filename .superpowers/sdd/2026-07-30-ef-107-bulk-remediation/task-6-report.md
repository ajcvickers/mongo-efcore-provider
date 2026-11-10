# Task 6 Report — Resolve the update logger once via `MongoQueryContext`

## Correction applied

The brief's Step 3 target file (`src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`)
no longer contains `GetUpdateLogger`. Bulk execution was extracted into Storage; the method (and all eight call
sites) now live in `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`. Steps 1 and 2 were
applied exactly as specified; Step 3's replacement body/comment were applied to that file instead.

## Files changed

### `src/MongoDB.EntityFrameworkCore/Query/MongoQueryContext.cs`
- Added `using Microsoft.EntityFrameworkCore;` (for `DbLoggerCategory`) and
  `using Microsoft.EntityFrameworkCore.Diagnostics;` (for `IDiagnosticsLogger<T>`).
- Constructor now takes a third parameter, `IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger`, and assigns
  it to a new `public virtual IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger { get; }` property.
- This is a public API change (public type, public constructor) — intentional per the brief. No compatibility
  overload was added, per the correction's explicit instruction: the constructor itself changed. The only caller
  in the assembly is `MongoQueryContextFactory`, which was updated in lockstep.

### `src/MongoDB.EntityFrameworkCore/Query/Factories/MongoQueryContextFactory.cs`
- Added the same two `using`s.
- Constructor now also takes `IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger`, stored in a new public
  `UpdateLogger` property (following the exact existing style: `Dependencies`/`Client` are stored as
  constructor-injected properties, and `Create()` forwards them to `new MongoQueryContext(...)`).
- `Create()` now passes `UpdateLogger` through as the third constructor argument.

**Why this matches the factory's existing pattern:** `MongoQueryContextFactory` is itself constructed by EF Core's
internal service provider (registered via `.TryAdd<IQueryContextFactory, MongoQueryContextFactory>()` in
`MongoServiceCollectionExtensions.AddEntityFrameworkMongoDB()`). It already receives `QueryContextDependencies` and
`IMongoClientWrapper` as plain constructor parameters, resolved by DI — there is no hand-rolled service-locator call
anywhere in this factory. `IDiagnosticsLogger<DbLoggerCategory.Update>` is registered by EF Core's core services as
a scoped service and is exactly the same shape of dependency `MongoDatabaseWrapper` already receives via
constructor injection (see `Storage/MongoDatabaseWrapper.cs`, which takes both
`IDiagnosticsLogger<DbLoggerCategory.Update>` and `IDiagnosticsLogger<DbLoggerCategory.Database.Transaction>` as
plain constructor parameters). Adding the same parameter to `MongoQueryContextFactory`'s constructor is consistent
with both call sites — no new registration or hand-authored resolution logic was needed.

### `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`
- Added `using MongoDB.EntityFrameworkCore.Query;` (needed for the `MongoQueryContext` cast).
- Replaced `GetUpdateLogger`'s body: was
  `queryContext.Context.GetService<IDiagnosticsLogger<DbLoggerCategory.Update>>()`, now
  `((MongoQueryContext)queryContext).UpdateLogger`, with the comment updated to the brief's wording (explains the
  logger is now supplied by `MongoQueryContextFactory`, matching how the read path gets its query logger, rather
  than being resolved from the context's service provider per execution).
- Did **not** remove `using Microsoft.EntityFrameworkCore.Infrastructure;` — `GetBulkCollectionAndSession` (same
  file) still calls `queryContext.Context.GetService<IMongoClientWrapper>()`, which needs that using. Confirmed via
  grep that this is the only other `GetService` call in the file, and it is untouched (out of scope: it resolves
  `IMongoClientWrapper`, not the diagnostics logger).
- No other line in the eight call sites of `GetUpdateLogger` was touched — same call shape (`var updateLogger =
  GetUpdateLogger(queryContext);`), same logging calls, same event-data construction, same `RenderBulkCommand`
  redaction gate (`ShouldLogSensitiveData()`), all untouched.

## Confirmation: no `GetService` call for the update logger survives

```
grep -rn "GetService<IDiagnosticsLogger" src/   →  no matches
```

The only remaining `GetService<...>()` call anywhere near this code is the pre-existing, unrelated
`queryContext.Context.GetService<IMongoClientWrapper>()` in `GetBulkCollectionAndSession`.

## EF8 build verification

`MongoQueryContext` and `MongoQueryContextFactory` are **not** guarded by `#if !EF8` — they serve the read path on
every configuration, so the new constructor parameter and `UpdateLogger` property compile and are supplied on EF8
too, even though `MongoBulkOperationExecutor` (the only consumer) is `#if !EF8`-guarded and doesn't exist on that
configuration. This is expected and matches the brief's Step 4 expectation. Verified: `Debug EF8` builds with
**0 warnings, 0 errors** — no unused-parameter/unused-field warnings were introduced (the property is a genuine
public API member, not a discarded local, so there's nothing for the compiler to flag).

## Test commands and results

All three configurations, `MONGODB_URI` and `ATLAS_URI` left unset (TestContainers boots
`mongodb/mongodb-atlas-local` automatically).

**Builds (all three configurations):**
```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    → Build succeeded. 0 Warning(s), 0 Error(s)
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    → Build succeeded. 0 Warning(s), 0 Error(s)
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded. 0 Warning(s), 0 Error(s)
```

**EF10 — bulk suites + logging + full Query suite (read path):**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~LoggingTests|FullyQualifiedName~Query"
```
Result:
- `MongoDB.EntityFrameworkCore.UnitTests.dll` — Passed: 18, Failed: 0, Skipped: 0
- `MongoDB.EntityFrameworkCore.FunctionalTests.dll` — Passed: 625, Failed: 0, Skipped: 45
- `MongoDB.EntityFrameworkCore.SpecificationTests.dll` — Passed: 4508, Failed: 0, Skipped: 27

(`ExecuteDeleteTests`/`ExecuteUpdateTests`/`BulkOperationsCommandTests`/`LoggingTests` are included in the
`FunctionalTests` count above; `~Query` pulled in the Query functional and specification suites, including the
Northwind conformance suite, as the read-path suite for this change.)

**EF9 — same filter (bulk code exists under `#if !EF9` is false, i.e. bulk is active on EF9):**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~LoggingTests|FullyQualifiedName~Query"
```
Result:
- `MongoDB.EntityFrameworkCore.UnitTests.dll` — Passed: 18, Failed: 0, Skipped: 0
- `MongoDB.EntityFrameworkCore.FunctionalTests.dll` — Passed: 604, Failed: 0, Skipped: 45
- `MongoDB.EntityFrameworkCore.SpecificationTests.dll` — Passed: 4937, Failed: 0, Skipped: 24

**EF8 — read-path Query suite only (bulk suites don't exist / are excluded under `#if !EF8`):**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" --no-build --filter "FullyQualifiedName~Query"
```
Result:
- `MongoDB.EntityFrameworkCore.UnitTests.dll` — Passed: 14, Failed: 0, Skipped: 0
- `MongoDB.EntityFrameworkCore.FunctionalTests.dll` — Passed: 532, Failed: 0, Skipped: 44
- `MongoDB.EntityFrameworkCore.SpecificationTests.dll` — Passed: 4624, Failed: 0, Skipped: 24

All three configurations: 0 failures across build and every filtered test run.

## Deliberately not changed

- No logging call, message, event-data construction, or the `RenderBulkCommand` redaction gate
  (`ShouldLogSensitiveData()`) in `MongoBulkOperationExecutor.cs` — Tasks 4/5's shape was left untouched.
- `using Microsoft.EntityFrameworkCore.Infrastructure;` in `MongoBulkOperationExecutor.cs` was kept — it's still
  needed by the unrelated `GetService<IMongoClientWrapper>()` call in `GetBulkCollectionAndSession`.
- No `MongoEventId` changes.
- No changes to the plan document (`.superpowers/sdd/2026-07-30-ef-107-bulk-remediation/*.md`) other than this
  report file.
- Did not add a compatibility overload to `MongoQueryContext`'s constructor — the brief and the task instructions
  explicitly call for changing the constructor itself, accepting the public API break.

## Commit

One commit, message exactly as specified by the brief's final step:
`refactor: supply the bulk update logger via MongoQueryContext instead of service location`

---

## Fix round 1 — document the public API breaks

**Gap:** the dispatch told me changing `MongoQueryContext`'s constructor was acceptable because the type was
unreleased. That was incorrect. Per `AGENTS.md`, breaks are measured against the latest released version of the
assembly, not against `main`. Both `MongoQueryContext` and `MongoQueryContextFactory`'s two-parameter public
constructors shipped in released versions, so adding a required third parameter to each is a genuine public API
break that the repo's minor-version-break policy permits but requires logging in `BREAKING-CHANGES.md`.

### Release tags verified

Ran `git fetch --tags` then diffed the prior constructor signatures directly against the working tree:

```
git show v10.0.2:src/MongoDB.EntityFrameworkCore/Query/MongoQueryContext.cs
git show v10.0.2:src/MongoDB.EntityFrameworkCore/Query/Factories/MongoQueryContextFactory.cs
git show v9.1.2:src/MongoDB.EntityFrameworkCore/Query/MongoQueryContext.cs
git show v9.1.2:src/MongoDB.EntityFrameworkCore/Query/Factories/MongoQueryContextFactory.cs
```

Confirmed at both tags:
- `public MongoQueryContext(QueryContextDependencies dependencies, IMongoClientWrapper mongoClient)`
- `public MongoQueryContextFactory(QueryContextDependencies dependencies, IMongoClientWrapper mongoClient)`

Both are two-parameter, matching the coordinator's report exactly — confirming the break is real and needs logging.

### Entry added

Added a new `###` subsection, `MongoQueryContext` and `MongoQueryContextFactory` constructors gained a parameter,
under the existing `## Breaking changes in 8.5.0 / 9.2.0 / 10.1.0` heading in `BREAKING-CHANGES.md`, placed directly
after the existing entry in that section (the cross-collection navigation entry) and before the next
`## Breaking changes in 8.4.0 / 9.1.0 / 10.0.0` heading. Followed the file's established structure exactly:
`#### Old behavior` / `#### New behavior` / `#### Why` / `#### Mitigations`, matching the heading depth and
subsection set used by the section's existing entry.

Content: both constructors' old two-parameter shape; the new required `IDiagnosticsLogger<DbLoggerCategory.Update>
updateLogger` parameter and the new `UpdateLogger` property; why (bulk `ExecuteUpdate`/`ExecuteDelete` bypasses
`SaveChanges` and needs the `Update`-category logger, now supplied via constructor injection instead of resolving
it from the `DbContext`'s service provider on the execution path, matching how the read path already supplies its
query logger via `QueryContextDependencies`); who is affected (nobody constructing a `DbContext` normally — both
types are built by the provider's own DI registration, and the only in-repo call site of the `MongoQueryContext`
constructor is `MongoQueryContextFactory.Create()`; the break bites only direct construction or subclassing, e.g.
test doubles or a custom provider extension); and mitigation (resolve the logger from the service provider, or
accept it via your own constructor and pass it through).

### Build verification (documentation-only change, no code touched)

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    → Build succeeded. 0 Warning(s), 0 Error(s)
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    → Build succeeded. 0 Warning(s), 0 Error(s)
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded. 0 Warning(s), 0 Error(s)
```

No test suites were re-run for this round per the coordinator's instruction (documentation only, no code changes).

### Commit

`BREAKING-CHANGES.md` only. No production files touched in this round.
