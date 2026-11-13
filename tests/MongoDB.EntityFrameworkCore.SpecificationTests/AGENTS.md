---
area: Spec conformance & test infrastructure
scope: ["tests/MongoDB.EntityFrameworkCore.SpecificationTests/**", "tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/**", "tests/MongoDB.EntityFrameworkCore.FunctionalTests/Usings.cs"]
reviewer-agent: spec-conformance-reviewer
adjacent-areas: [all areas — every test mirrors src/ structure]
---

# Spec conformance & test infra — AGENTS.md

EF Core's provider-conformance suite plus shared functional-test infrastructure (`FunctionalTests/Utilities/`:
`TestServer`, `TemporaryDatabaseFixture(Base)`, `TestDatabaseNamer`, `DatabaseCleaner`, `NativeModeAssert`;
`TestMqlLoggerFactory` is in `SpecificationTests/Utilities/`). Per-area test folders belong to the matching `src/`
area. Connection and encryption env vars: see root `AGENTS.md`.

## Conventions

- **Test infrastructure is duplicated**: `SpecificationTests` no longer references `FunctionalTests`; each project
  carries its own copy of the server bootstrap and database namer (`TestServer`, `TestContainersTestServer`,
  `ExternalDatabaseTestServer`, `TestDatabaseNamer`, …) — keep the copies in sync when changing one.
- **Specification tests** inherit upstream `*TestBase<TFixture>`/`*FixtureBase<TModelCustomizer>` (package
  `Microsoft.EntityFrameworkCore.Specification.Tests`, matched to `Versions.props`); fixtures are
  `*MongoFixture<T>`, tests `*MongoTest`. Override the test method and assert MQL with `AssertMql(...)`; don't
  re-implement the upstream body. Permanently unsupported → `Skip` with a reason, never a silent return.
- **Known-failing spec tests** carry `// Fails:` (durable gap with a ticket). `// Failed:` is a legacy transitional
  marker; don't add new ones.
- **EF-version `#if`s**: upstream renamed `IModelCustomizer` → `ITestModelCustomizer` and `Seed()` → `SeedAsync()`
  between EF8 and EF9+. CI runs all three versions; use `/test-all` locally.
- **Isolation**: `TemporaryDatabaseFixtureBase.InitializeAsync()` takes a unique name from
  `TestDatabaseNamer.GetUniqueDatabaseName()` (assembly- and EF-version-specific prefix such as
  `EF10-FunctionalTest-` / `EF10-SpecTest-`; startup cleanup only drops its own assembly's prefix); collection
  names come from `[CallerMemberName]`. Never use fixed names. `DisposeAsync` is a no-op; stale `Test*` databases are removed only by manually running
  `DatabaseCleaner.CleanDatabase`.
- **Never make the Specification tests parallel** (`SpecificationTests/Usings.cs`, xUnit v2, disables it; tests share
  global MongoDB state). `FunctionalTests` (xUnit v3) does not disable it and relies on unique per-fixture
  databases. On MongoDB 5.0/6.0 CI runs the test projects, and their collections, one at a time
  (`evergreen/run-tests.sh`), because overlapping writes cause `Unable to read from a snapshot due to pending
  collection catalog changes` (code 246); don't run the projects concurrently against such a server. Heavy fixtures
  use `[CollectionDefinition]` + `[XUnitCollection]`; encryption and compatibility tests get their own collections.
- The container server is cached per test process (random port). `ATLAS_URI="Disabled"` skips Atlas tests.
- `MONGODB_EF_QUERY_MODE=Native|NativeOnly|DriverLinq` (case-insensitive) sets the query mode for every spec and
  functional context (`TestQueryMode`, the single resolver); `NativeOnly` makes any fallback throw.
  `MONGODB_EF_NATIVE_ONLY=1` is an alias for `NativeOnly`. An unrecognised value, or the alias combined with a
  different `MONGODB_EF_QUERY_MODE`, throws at startup. MQL shape alone does not prove a query went native.
- MQL assertions are field-order-sensitive; a new translator branch often needs baselines updated across many tests.
- After a `Mongo:*` annotation change, regenerate design-time compiled-model output under
  `FunctionalTests/Design/Generated/EF{8,9,10}/`. `Encryption/` is gated on `CRYPT_SHARED_LIB_PATH` (tests skip silently, with
  a run warning, when unset; enable via `eval "$(tests/tools/fetch-crypt-shared.sh)"`, see `CONTRIBUTING.md`); `Compatibility/`
  round-trips stored data across provider versions.

## Ported suites that assert today's failures

Most `*MongoTest` classes were ported cheaply (tracking issue #35; inventory `docs/failing-spec-tests.md`): a test that passes calls
`base`, and every test that does not yet pass is overridden to **assert the current failure**, so the suite stays green and any change
in behavior (including the test starting to pass) is detected.

- **Overrides are inline**, in the test class, each preceded by `// Fails: <short reason> <ticket>`. Add a row for a new failure mode
  to the inventory. Do not move overrides into generated partial classes.
- **A test that passed before and fails now is a regression**: tag it `// Fails: REGRESSION vs <baseline>: ... <ticket>` and list it in
  the "Regressions" section of the inventory. A test that failed before and fails differently is not.
- **Assert the failure with `Utilities/SpecFails`**: `Throws`/`ThrowsAsync` (any exception, optional message fragment),
  `Throws<T>`/`ThrowsAsync<T>` (exact type; with a fragment it also checks the message; fails if the test unexpectedly passes),
  `Sync<T>`/`Async<T>` (unwrap `TargetInvocationException`/`AggregateException`), `SyncSomeRows<T>`/`AsyncSomeRows<T>` for theories
  where only some rows fail or fail only in some environments, and `TranslationFailed`/`TranslationFailedAsync` (the translation fails
  in whichever form the active query mode reports it).
  `Capture<T>`/`CaptureAsync<T>` return the exception when the override must inspect it. Never use raw `Assert.Throws*` in a `// Fails:`
  method: `python tests/tools/count-spec-fails.py --check` rejects it.
- **Model adaptation lives in `Utilities/MongoSpecModelHelpers`**: `MapSingleKeysToId(modelBuilder, generateIntegerKeys, temporaryIntegerKeys)` (single-property
  keys must map to `_id`; `true` also uses the client-side `TestIntegerValueGenerator<T>`, because the provider generates no integer
  keys; the optional `temporaryIntegerKeys` predicate picks the entity types whose keys use `TestTemporaryIntegerValueGenerator<T>`,
  which EF Core treats as temporary like a relational provider's generated values, e.g. `Gumball` in `StoreGeneratedMongoTest`), `RelaxNullableUniqueIndexes`, `RelaxUniqueForeignKeyIndexes`, `FixExternalModel` and the `MapSinglePropertyKeysToElementId`
  convention. Every workaround is a gap with a ticket: remove it when the provider no longer needs it. `Utilities/InitFailureGuard`
  lets a fixture whose model, seed or store creation fails still be constructed and rethrows the failure from the first `CreateContext()`,
  so it can be asserted per test.
- **Upstream tests that are `public` but not `virtual` cannot be overridden.** Hide one with a `new` method that carries the ticket id: it
  either asserts the failure (`CompositeKeyEndToEndMongoTest`) or is marked `Skip` when the failure cannot be asserted
  (`PropertyValuesMongoTest`, `DataBindingMongoTest`).
- **On EF11 (xUnit v3) a `new` hiding method is rejected** ("has overloads, which is not supported"). The hides are wrapped in `#if !EF11`,
  and an inherited test that fails as base is skipped at run time by `Ef11TestSkips.SkipIfListed(...)` from a field initializer in the test
  class (EF-X711). Upstream tests that exist only on some versions are wrapped the same way.
- **Pin the default query mode for a ported suite.** CI runs the specification tests again with `MONGODB_EF_QUERY_MODE=NativeOnly`. Ported
  suites are baselined in the default mode only, so build their store with `new MongoTestStoreFactory(server, pinDefaultQueryMode: true)`
  (or a fixture that does). Without the pin, every test whose fallback is refused fails in that CI job.
- **Time zone dependent assertions.** CI runs in UTC; a developer machine often does not. A `DateTime` of kind Unspecified is treated as local
  time (EF-X006), so some known failures only appear outside UTC. `SpecFails.Either`/`EitherAsync` require a pass in a UTC environment and accept the
  known failure elsewhere (see EF-X720). To reproduce CI on Linux/WSL, run with `TZ=UTC`.
- **A ported fixture's database is shared, and a suite that writes to it must either roll back or reseed.** The relational providers roll back: EF begins a transaction on one context and never commits it, and the test class's `UseTransaction(DatabaseFacade, IDbContextTransaction)` hook enlists the other contexts. Override the hook with `facade.UseTransaction(transaction)` (`GraphUpdatesMongoTest`, `StoreGeneratedMongoTest`, `FieldMappingMongoTest`, ...) and the suite needs no reseed, keeps the model's unique indexes (a reseed drops them, which changes what some tests do, see EF-X606) and runs far faster. A failed write aborts the whole MongoDB transaction, so this does not suit a test that carries on after an expected `SaveChanges` exception (none of the ported tracking suites needed to reseed). If a test writes (or a test that relational providers expect to throw succeeds here), later tests
  see its rows, in an order dependent way (EF-X700 was this). Reset the data afterwards (see `InheritanceQueryMongoFixture.ResetDatabaseAsync`) and, before
  reporting wrong results as a provider bug, run the test on its own.
- **EF11 follows the EF10 arms.** A condition that mentions `EF10` is written `(EF10 || EF11)` unless EF11 behaves differently; the
  harvest-and-rebaseline pass splits the arms where it does.
- **Remove a base from `MongoComplianceTest.IgnoredTestBases` when you port it.** A base that stays ignored for a concrete reason carries
  a comment naming the ticket.
- **Do not put dates or times in MQL baselines.** `Today`/`Now`/`UtcNow` translations embed the current date or time, so those tests do
  not assert MQL.
- **Database names are limited to 63 characters.** `TestDatabaseNamer.GetUniqueDatabaseName` truncates the caller's part to fit and never
  truncates the unique counter, so a long fixture name cannot fail late in a long run.
- **EF10 `NonShared*TestBase` classes** cache the first test's seeded store in a shared fixture. The ported classes no longer work
  around this with `Fixture = null`; don't add per-test fixture resets to them.
- **Counts:** `python tests/tools/count-spec-fails.py` regenerates the figures in `docs/failing-spec-tests.md`.

## Regenerating MQL baselines

```bash
EF_TEST_REWRITE_BASELINES=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~<Class>.<Method>"
```

When an `AssertMql` fails with the var set, `TestMqlLoggerFactory.AssertBaseline` rewrites that override in place
from captured MQL. **The test still reports failed** (the signal a rewrite happened); rebuild and rerun without the
var to confirm green.

- `AssertMql` runs after `await base.Test(...)`, so data/behavior failures never reach the rewriter — but it will
  record MQL for a test whose only remaining failure is the baseline, including a partial pipeline before an
  expected throw.
- To baseline a pass-through test, add an empty `AssertMql();` after the `base` call and run the rewrite; the rewriter parses the file with the
  current build's `EF8`..`EF11` symbol, so run it once per EF version to fill each `#if` arm (EF11: the spec exe). Repeat build+run until no
  empty `AssertMql();` remains in an arm the build compiles. Skipped tests never run, so leave them without `AssertMql`.
- MQL that embeds a client-generated integer key (`TestIntegerValueGenerator`, process-wide counter) is not stable; normalize it before
  comparing (see `AssertMqlIgnoringGeneratedKeys` in `FiltersInheritanceQueryMongoTest`).
- Ported suites are pinned to the default query mode, so `NativeOnly` does not reveal fallbacks. To find them, temporarily set
  `pinDefaultQueryMode: false`, run with `MONGODB_EF_QUERY_MODE=NativeOnly`, note the failing tests in a comment, then restore the pin.
- Always scope with a tight `--filter`, then `git diff` and rebuild before trusting it. The rewriter (`Utilities/QueryBaselineRewriter`,
  tested by `QueryBaselineRewriterTests`, #64) changes only the argument list of the `AssertMql(...)` call that *starts* on the line the
  stack trace names, and otherwise leaves the file untouched and prints `Baseline NOT rewritten (...)` in the test output. It refuses when
  the source was modified after the test assembly was built (**rebuild after every edit, and after each rewrite**), when something other
  than the rewriter changed the file during the run (do not run several EF versions with the variable set against one checkout at the
  same time: the first writer wins), or when the new text would not parse (for example MQL containing `"""`). It keeps the layout of the
  baseline it replaces (opening `"""` indented or in column 0), so regenerating an unchanged baseline is a no-op. It still truncates the
  console baseline at 9 statements.
- **If the rewriter ever damages a file, misplaces a baseline, or refuses for a reason that looks wrong, add a comment to issue #64** (the
  issue is deliberately left open): the test, the line, `git diff` of the damage and the `Baseline NOT rewritten` text. Then restore the file with
  `git checkout`. #64 stays open until a period without reports, after which the fix is ported to dotnet/efcore's `TestSqlLoggerFactory`.

## Test folder mirror

Test folders mirror `src/` (`UnitTests/`, `FunctionalTests/`, `SpecificationTests/`). Exceptions: `Storage/` functional tests also use `Update/`; `Serializers/` and `ChangeTracking/` use `Mapping/`/`Serialization/`; `Extensions/` and `Infrastructure/` use `Design/`; `ValueGeneration/` convention coverage is in `SpecificationTests/Metadata/Conventions/`.

## Native/driver differential runner

`tests/tools/native-parity-diff.sh <EF8|EF9|EF10> <outdir> [--gate]` runs the SpecificationTests and FunctionalTests projects twice (4 parallel `dotnet test` processes, each with its own Atlas-local container): once with `MONGODB_EF_QUERY_MODE=DriverLinq`, once with `NativeOnly`; then `native-parity-diff.py` compares the TRX files. It needs a prior `Debug <ver>` build (`--no-build`) and unsets `MONGODB_URI`/`ATLAS_URI`/`MONGODB_EF_NATIVE_ONLY`.

- On Windows run it from **Git Bash**, not PowerShell: there `bash` resolves to WSL, which can't see the Windows `dotnet`/Docker. Python is the first of `python3`, `python`, `py` (override with `PYTHON=<cmd>`).
- Exits nonzero on a missing TRX, a test missing from one mode, or an unexpected outcome; `--gate` (or `NATIVE_PARITY_FAIL_ON_REGRESS=1`) also fails on any regress entry.
- Outputs `<outdir>/<ver>-<project>-regress.txt` (passes under DriverLinq, fails under NativeOnly: the native parity gaps) and `-improve.txt` (the reverse), with the first 400 characters of each failure message.
- It exports `MONGODB_EF_SKIP_MQL_ASSERTIONS=1` (`AssertBaseline` returns early; MQL legitimately differs between paths) and `MONGODB_EF_DIFFERENTIAL=1` (`IsNativeOnly` is false in both runs and `AssertRefusal*` accepts either exception type). Tests whose `IsNativeOnly` branches diverge in outcome are therefore NOT masked and show as regress/improve entries.
