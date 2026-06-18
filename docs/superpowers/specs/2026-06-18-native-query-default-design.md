# Productionize the native-query gate + harden (Sub-project E) — design

**Date:** 2026-06-18
**Status:** Approved; pending implementation plan
**Branch:** `spike/low-level-provider` (off `main`)
**Program:** sub-project E of the low-level-provider migration. Follows D (streaming owned
collections). Makes native MQL + streaming materialization a proper, safe-by-default behavior with a
public opt-out, replacing the env-var spike gate.

## Goal

Turn the env-var spike toggle (`MONGODB_EF_NATIVE_QUERY`, a process-wide `static`) into a per-context
public option — native + streaming **on by default**, with an explicit opt-out to the existing
driver-LINQ + `BsonDocument`-DOM path — and apply the hardening that makes default-on safe (graceful
fallback on any unexpected failure, deterministic row disposal, tightened eligibility). The DOM /
driver-LINQ path becomes a purely internal fallback (and an opt-out escape hatch), not a spike knob.

## Current state

Native + streaming is already the default *behavior* (`NativeQuery.Mode` defaults to `Auto`), but the
gate is a `static readonly` read of `MONGODB_EF_NATIVE_QUERY`: undocumented, `internal`, process-wide,
with no `DbContextOptions`/`UseMongoDB` configuration. Eligible entities stream; everything else falls
back transparently. The three gate sites: `CompileShapedQuery` (streaming-shaper gate), `TranslateQuery`
(native-pipeline gate), and the fallback `catch (NativeTranslationNotSupportedException) when (Mode != Force)`.

## 1. Public opt-out API

- **`MongoOptionsExtension`** (`Infrastructure/`): add a `bool UseNativeQuery` (default **true**) and a
  `WithUseNativeQuery(bool)` clone method, following the existing immutable-with-pattern. Reflect it in
  the extension's `ExtensionInfo`: include it in `LogFragment`, `GetServiceProviderHashCode`,
  `ShouldUseSameServiceProvider`, and `PopulateDebugInfo`, so EF treats the setting as part of service
  identity (consistent with the other options).
- **`MongoDbContextOptionsBuilder.UseNativeQuery(bool enabled = true)`** (`Extensions/`): the public,
  per-context control; `UseNativeQuery(false)` routes the context entirely through driver-LINQ + DOM.
  This is new public API surface — an **addition** (not a break); the `public-api-reviewer` /
  `api-stability-reviewer` run.

## 2. Gate plumbing

Replace the process-wide `static NativeQuery.Mode` consumption with a per-query effective mode read at
compile time from `QueryCompilationContext.ContextOptions.FindExtension<MongoOptionsExtension>()`:
- The **option** is the product control: `UseNativeQuery == false` ⇒ effective mode **Off** (driver-LINQ
  + DOM for that context).
- The **env var is kept as a test-only override** layered on top (preserves the suite's coverage
  measurement): when `UseNativeQuery == true`, effective mode = `MONGODB_EF_NATIVE_QUERY` if set
  (`force` ⇒ Force, `off` ⇒ Off), else `Auto`.
- All three gate sites consult this per-query effective mode rather than the static. `NativeQuery` (or
  a small successor) computes it from `(option, env-var)`.

## 3. Hardening (makes default-on safe)

- **Broaden the fallback catch.** In `CompileShapedQuery`/`TranslateQuery`, in non-`Force` mode, catch
  **any** exception from native translation / the streaming rewrite (not only
  `NativeTranslationNotSupportedException`) and fall back to driver-LINQ + DOM (preserve the original as
  inner exception for diagnostics). In `Force` mode, propagate (so tests still surface gaps). This is the
  key safety property for a default-on feature: an unforeseen injected-tree shape (incl. future EF-version
  divergence) degrades gracefully instead of throwing to the caller.
- **Deterministic `RawBsonDocument` disposal.** Dispose undelivered fetched rows in
  `QueryingEnumerable.Dispose` / `DisposeAsync` (today rows are disposed only after shaping; an early
  `foreach` break or mid-stream exception leaks the byte buffers to finalization).
- **Tighten collection-of-collection eligibility.** `StreamingEligibility` rejects a collection whose
  element type itself owns a collection (the rewriter can't stream that yet; today it's over-admitted and
  relies on the runtime throw → fallback). State intent in eligibility.

## 4. Validation

- **No regressions, default (native on):** full Query suite at **0 failures** (4897 pre-E).
- **No regressions, opt-out:** the suite (or a representative subset) also passes with
  `UseNativeQuery(false)` — confirming the driver-LINQ + DOM path (pre-migration behavior) still works as
  the escape hatch. A focused functional test sets `UseNativeQuery(false)` and asserts results are correct
  (and, via MQL logging or a marker, that it used the driver-LINQ/DOM path, not streaming).
- **Benchmark sanity:** default config still streams; the C′/D shapes hold their numbers.
- **Builds EF8/EF9/EF10.**

## Components / files

- `Infrastructure/MongoOptionsExtension.cs` — `UseNativeQuery` flag + `WithUseNativeQuery` + ExtensionInfo.
- `Extensions/MongoDbContextOptionsBuilder.cs` — `UseNativeQuery(bool enabled = true)`.
- `Query/NativeTranslation/NativeQueryMode.cs` (or successor) — per-query effective-mode from
  (option, env-var); stop relying solely on the static env read.
- `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` + `MongoStreamingEntityMaterializerRewriter`
  — broaden the fallback catch; consult the per-query mode.
- `Query/QueryingEnumerable.cs` — dispose undelivered rows on Dispose/DisposeAsync.
- `Query/NativeTranslation/StreamingEligibility.cs` — reject collection-of-collection.
- A new functional test for `UseNativeQuery(false)`.

## Out of scope

- Deleting the driver-LINQ bridge (it is the permanent fallback for un-translated query shapes).
- New operator / eligibility coverage (cross-collection Includes / `$lookup`, TPH).
