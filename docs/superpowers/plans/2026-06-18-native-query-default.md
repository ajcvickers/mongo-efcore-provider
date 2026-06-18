# Productionize the Native-Query Gate + Harden (Sub-project E) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the env-var spike gate with a public per-context option — `UseNativeQuery(bool)`, default on, opt-out to driver-LINQ+DOM — and apply the hardening that makes default-on safe (graceful fallback on any failure, deterministic row disposal, tightened eligibility).

**Architecture:** Add `UseNativeQuery` to `MongoOptionsExtension` (immutable `With*` + `ExtensionInfo`), surfaced via `MongoDbContextOptionsBuilder.UseNativeQuery(bool)`. The factory reads the option from `ContextOptions` into `MongoQueryCompilationContext.UseNativeQuery`; the shaped-query visitor computes a per-query effective mode = `NativeQuery.EffectiveMode(option)` (option-off ⇒ Off; else the `MONGODB_EF_NATIVE_QUERY` env var as a test-only `force`/`off` override; else Auto). Hardening broadens the fallback catch, disposes undelivered rows, and tightens collection-of-collection eligibility.

**Tech Stack:** C#, EF Core 8/9/10 (build `Debug EF10`; validate EF8). MongoDB replica set at `mongodb://localhost:27017`.

---

## Conventions
- Build `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`. New public API must compile on EF8/9/10.
- Tests/benchmark use `MONGODB_URI=mongodb://localhost:27017`. Preserve BOMs. `<Nullable>enable</Nullable>`.
- Gate sites consulting the static today: `MongoShapedQueryCompilingExpressionVisitor.cs` lines ~192 (streaming gate), ~213 (catch), ~310 (native-pipeline gate), ~332 (catch).

---

## Task 1: `UseNativeQuery` on `MongoOptionsExtension`

**Files:** Modify `src/MongoDB.EntityFrameworkCore/Infrastructure/MongoOptionsExtension.cs`

- [ ] **Step 1: Add the property + immutable setter** (mirror the existing `QueryableEncryptionSchemaMode` property + `WithQueryableEncryptionSchemaMode` pattern, ~lines 212–224). Default **true**; copy it in the copy-ctor (`MongoOptionsExtension(MongoOptionsExtension copyFrom)`):
```csharp
    /// <summary>Whether the provider translates queries to native MQL with a streaming materializer (default true). When false, the driver LINQ provider + BsonDocument materialization is used.</summary>
    public bool UseNativeQuery { get; private set; } = true;

    /// <summary>Returns a copy of this extension with <see cref="UseNativeQuery"/> set.</summary>
    public virtual MongoOptionsExtension WithUseNativeQuery(bool useNativeQuery)
    {
        var clone = new MongoOptionsExtension(this);
        clone.UseNativeQuery = useNativeQuery;
        return clone;
    }
```
In the copy constructor, add `UseNativeQuery = copyFrom.UseNativeQuery;` alongside the other copied fields.

- [ ] **Step 2: Wire into `ExtensionInfo`** — in the nested `ExtensionInfo`:
  - `GetServiceProviderHashCode()` (~line 284): include `UseNativeQuery` in the hash combine (e.g. `HashCode.Combine(existing..., extension.UseNativeQuery)` — match the existing style; if it currently hashes a few fields, add this one).
  - `ShouldUseSameServiceProvider(...)` (~line 291): add `&& Extension.UseNativeQuery == otherInfo.Extension.UseNativeQuery` to the comparison (use the same `Extension`/`otherInfo` accessor the existing checks use).
  - `PopulateDebugInfo(...)` (~line 300): add `debugInfo["Mongo:UseNativeQuery"] = (Extension.UseNativeQuery).ToString();` (match the key style of existing entries).
  - `CreateLogFragment()` (~line 319): append `UseNativeQuery` to the fragment when false (or always — match existing style; e.g. `if (!Extension.UseNativeQuery) builder.Append("UseNativeQuery=false ");`).
  (Use the actual accessor names in the file — the nested class may expose the extension via a typed property; read it and match.)

- [ ] **Step 3: Build + commit**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git add src/ && git commit -m "Native query default: UseNativeQuery on MongoOptionsExtension"
```
Expected: clean.

---

## Task 2: `UseNativeQuery(bool)` on `MongoDbContextOptionsBuilder`

**Files:** Modify `src/MongoDB.EntityFrameworkCore/Infrastructure/MongoDbContextOptionsBuilder.cs`

- [ ] **Step 1: Add the public method** — follow the existing pattern in that file (the other builder methods call `((IMongoDbContextOptionsBuilderInfrastructure)this).AddOrUpdateExtension(extension.With...)` or similar — read the file to match exactly how it mutates `MongoOptionsExtension`). Add:
```csharp
    /// <summary>
    /// Configures whether the provider translates queries to native MQL with a streaming materializer
    /// (the default) or uses the MongoDB driver's LINQ provider with BsonDocument materialization.
    /// </summary>
    /// <param name="useNativeQuery"><see langword="true"/> (default) to use native MQL + streaming; <see langword="false"/> to use the driver LINQ provider.</param>
    /// <returns>The same builder instance so multiple calls can be chained.</returns>
    public virtual MongoDbContextOptionsBuilder UseNativeQuery(bool useNativeQuery = true)
    {
        // match how sibling methods obtain + replace the extension on the underlying options builder
        <set the MongoOptionsExtension via WithUseNativeQuery(useNativeQuery), as the other methods do>
        return this;
    }
```
Replace the `<...>` line with the exact mechanism the sibling methods use (e.g. obtaining the `IMongoDbContextOptionsBuilderInfrastructure`/`OptionsBuilder` and calling `((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(GetOrCreateExtension().WithUseNativeQuery(useNativeQuery))`). Read the file's existing methods and mirror them precisely.

- [ ] **Step 2: Build + commit**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
git add src/ && git commit -m "Native query default: UseNativeQuery(bool) public option"
```
Expected: both clean.

---

## Task 3: Per-query effective mode from the option

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/MongoQueryCompilationContext.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Factories/MongoQueryCompilationContextFactory.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeQueryMode.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`

- [ ] **Step 1: Carry the option on the compilation context**

In `MongoQueryCompilationContext`, add `public bool UseNativeQuery { get; }` set from a constructor parameter. In `MongoQueryCompilationContextFactory.Create(...)`, read the option from the context options and pass it: `var useNativeQuery = dependencies.ContextOptions.FindExtension<MongoOptionsExtension>()?.UseNativeQuery ?? true;` (the factory's `QueryCompilationContextDependencies dependencies` exposes `ContextOptions`; verify the member name). Pass `useNativeQuery` into the `MongoQueryCompilationContext` constructor. Add `using MongoDB.EntityFrameworkCore.Infrastructure;` as needed.

- [ ] **Step 2: Effective-mode helper**

In `NativeQueryMode.cs`, keep the static env-var parse but add:
```csharp
    /// <summary>The env-var override, read once (test-only). Null if unset.</summary>
    private static readonly NativeQueryMode? EnvOverride = ParseEnv(Environment.GetEnvironmentVariable("MONGODB_EF_NATIVE_QUERY"));

    private static NativeQueryMode? ParseEnv(string? value)
        => value?.Trim().ToLowerInvariant() switch
        {
            "force" => NativeQueryMode.Force,
            "off" => NativeQueryMode.Off,
            "auto" => NativeQueryMode.Auto,
            _ => null
        };

    /// <summary>Effective mode for a query: the per-context option, overridden by the test-only env var.</summary>
    public static NativeQueryMode EffectiveMode(bool optionEnabled)
        => !optionEnabled ? NativeQueryMode.Off : (EnvOverride ?? NativeQueryMode.Auto);
```
(Keep the existing `Mode` static if other code still uses it, or remove it once all call sites move to `EffectiveMode`. Prefer removing `Mode` and routing everything through `EffectiveMode`.)

- [ ] **Step 3: Consult the per-query mode at the gate sites**

In `MongoShapedQueryCompilingExpressionVisitor`, where the four sites use `NativeQuery.Mode`, compute once per query `var nativeMode = NativeQuery.EffectiveMode(((MongoQueryCompilationContext)QueryCompilationContext).UseNativeQuery);` and use `nativeMode` in:
- the streaming gate (`nativeMode != NativeQueryMode.Off && StreamingEligibility...`),
- the catch `when (nativeMode != NativeQueryMode.Force)`,
- the native-pipeline gate (`nativeMode != NativeQueryMode.Off && ...`),
- the second catch `when (nativeMode != NativeQueryMode.Force)`.
(The `QueryCompilationContext` base property is accessible; cast to `MongoQueryCompilationContext`. If a gate site is in a `static` helper like `TranslateQuery`, thread the `bool useNativeQuery`/`nativeMode` in as a parameter from the caller that has the context.)

- [ ] **Step 4: Build + smoke (default still streams; option off uses DOM)**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10" -- --smoke
```
Expected: `SMOKE OK ...`, `FLAT OK ...`, `BASKET OK: baskets=100, items=300` (default = native on, unchanged). The benchmark `BenchmarkDbContext` doesn't set the option, so it defaults true → streams.

- [ ] **Step 5: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add src/ && git commit -m "Native query default: per-query effective mode from UseNativeQuery option (env var = test override)"
```

---

## Task 4: Hardening

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/QueryingEnumerable.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/StreamingEligibility.cs`

- [ ] **Step 1: Broaden the fallback catch**

At the two streaming/native catch sites, in non-`Force` mode catch any exception (not just `NativeTranslationNotSupportedException`) and fall back. Change each:
```csharp
            catch (NativeTranslationNotSupportedException) when (nativeMode != NativeQueryMode.Force)
```
to:
```csharp
            catch (Exception) when (nativeMode != NativeQueryMode.Force)
```
(In `Force` mode the exception still propagates — tests surface real bugs. In `Auto` (default), any unexpected rewrite/translation failure degrades to the driver-LINQ+DOM path. Do NOT swallow exceptions from the actual driver-LINQ fallback execution — only the native translation/rewrite attempt is inside the try.)

- [ ] **Step 2: Dispose undelivered rows in `QueryingEnumerable`**

The per-row dispose-after-shape handles delivered rows. Add disposal of an undelivered fetched row (e.g. when enumeration is abandoned mid-stream) in the enumerator's `Dispose()` and `DisposeAsync()`: track the current row reference and, in Dispose/DisposeAsync, if the last fetched row is `IDisposable` and was not yet disposed, dispose it before disposing the underlying enumerator. Concretely: keep the row in a field as it's read; null it after disposing it post-shape; in `Dispose`/`DisposeAsync` dispose it if non-null. (Match the existing `Dispose`/`DisposeAsync` at the bottom of the file; keep `_enumerator?.Dispose()`/async equivalents.)

- [ ] **Step 3: Tighten collection-of-collection eligibility**

In `StreamingEligibility.IsEligible`, when validating an owned **collection** navigation, reject it if the element type itself owns a collection (the rewriter falls back for that today). I.e. for a collection navigation, require the target type to be eligible AND to have no owned-collection navigations of its own. Add a check: if `navigation.IsCollection` and the target type has any navigation that `IsCollection`, return false. (Single owned references nested inside a collection element remain allowed.)

- [ ] **Step 4: Build (EF10 + EF8) + smoke**
```bash
cd /Users/arthur.vickers/code/provider2
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=force dotnet run -c "Release EF10" -- --smoke
```
Expected: builds clean; force-mode smoke still `SMOKE OK ... / FLAT OK ... / BASKET OK: baskets=100, items=300` (Basket is one collection level → still eligible/streams under the tightened rule).

- [ ] **Step 5: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add src/ && git commit -m "Native query default: harden (broaden fallback catch, dispose undelivered rows, tighten collection-of-collection eligibility)"
```

---

## Task 5: Validate (default + opt-out) + functional test + benchmark

**Files:**
- Create: a functional test under `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/` (e.g. `UseNativeQueryOptionTests.cs`)

- [ ] **Step 1: Functional test for the opt-out**

Add a test that builds two contexts against a seeded collection — one default (`UseNativeQuery` unset → true) and one with `.UseMongoDB(conn, db, o => o.UseNativeQuery(false))` — runs the same simple query (e.g. `Where` + whole-entity `ToList`) on both, and asserts identical, correct results. Follow the existing FunctionalTests/Query fixture + `UseMongoDB` patterns (see `ConnectionTests.cs` for how options are built; use `TestServer`/`TestDatabaseNamer`). Optionally assert the path via the command logger if a `TestMqlLoggerFactory`-style hook is available (native logs the pipeline `BsonDocument[]`; driver-LINQ logs differently) — if asserting the path is brittle, assert correctness + that the option is honored (no exception, right rows).

- [ ] **Step 2: Run it + the regression suites (default), per assembly**
```bash
cd /Users/arthur.vickers/code/provider2
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
MONGODB_URI=mongodb://localhost:27017 dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/*.csproj -c "Debug EF10" --filter "FullyQualifiedName~UseNativeQueryOption" 2>&1 | tail -8
MONGODB_URI=mongodb://localhost:27017 dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/*.csproj          -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -6
MONGODB_URI=mongodb://localhost:27017 dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/*.csproj    -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -6
MONGODB_URI=mongodb://localhost:27017 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/*.csproj -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -6
```
(Timeout 600000 each.) Expected: the new test passes; suites match pre-E (UnitTests 8/0, FunctionalTests 544+1/0/44, SpecificationTests 4345/0/18) — **0 failures**. Any regression is a bug in the gate plumbing or hardening — fix it (do not weaken tests).

- [ ] **Step 3: Opt-out regression sanity** — confirm the driver-LINQ+DOM path still works as the escape hatch by running a representative Query subset with the env override forcing the old path:
```bash
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=off dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/*.csproj -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -6
```
Expected: **0 failures** (this is the pre-migration path; it must still pass). Record the count.

- [ ] **Step 4: Benchmark sanity** — default config still streams:
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10" 2>&1 | tail -20
```
Confirm the streamed shapes' allocations still match C′/D (no regression from the plumbing). Capture the table.

- [ ] **Step 5: Record + commit**

Create `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-18-native-query-default-E.md` with: the default + opt-out (`off`) regression counts, the new test result, and the benchmark table (vs C′/D — should be unchanged). Then:
```bash
cd /Users/arthur.vickers/code/provider2
git add -A && git commit -m "Native query default: validate E (default + opt-out regressions, option test, benchmark sanity)"
```

---

## Notes for the executor
- **New public API** (`UseNativeQuery`) is an addition — keep it minimal and documented; the public-api / api-stability reviewers care about the signature, default, and `MongoOptionsExtension` annotation/ExtensionInfo hygiene.
- **The env var stays a test-only override** layered on the option (`force`/`off`); the option is the product control. Effective mode lives in `NativeQuery.EffectiveMode`.
- **Broadened catch only wraps the native translation/rewrite attempt** — never the driver-LINQ fallback execution itself (that must surface real errors).
- **Never weaken/skip a test.** A regression in default mode = a plumbing/hardening bug; the opt-out path must also stay green.
- Leave `ef-bench-mongo` running.
