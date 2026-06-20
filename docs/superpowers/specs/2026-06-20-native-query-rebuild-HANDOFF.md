# HANDOFF: ground-up native LINQ query provider — rebuild

**Date:** 2026-06-20
**Spike branch:** `spike/low-level-provider` (off `main`)
**Audience:** a fresh engineer/agent starting the real (production) build. You do not need the spike's chat history — this doc + the linked artifacts are the complete starting point.

---

## The decision

Build a **ground-up native LINQ query provider** for the MongoDB EF Core provider: the provider translates EF queries to MongoDB aggregation pipelines (MQL) itself and materializes results with a forward-only streaming reader, using the C# driver **only for low-level access** (connection/protocol/auth/sessions, BSON, cursors, transactions, CSFLE, raw `Aggregate`/`BulkWrite`/command execution). The driver's **LINQ provider is no longer the translation engine.**

This was decided after a multi-stage spike (A→E + Includes) on `spike/low-level-provider` proved feasibility and quantified the win. The spike's deliverable is the **proof + the test/benchmark rig**, not code to preserve wholesale — see "Keep vs rebuild" below.

## Why (the two arguments)

**1. Performance / allocation** (headline numbers, validated against actual `origin/main` within ~1.4%; full table in `results/2026-06-20-headline-three-config.md`):

| Shape (N=10k) | Driver-only (no EF) | Current EF provider (main) | Native (spike) |
|---|---|---|---|
| Where→ToList | 5.8 ms / 1.6 MB | 15.1 ms / 22.7 MB | 8.3 ms / 9.6 MB |
| Whole-entity ToList (no-track) | 8.3 ms / 3.1 MB | 33.4 ms / 45.3 MB | 15.6 ms / 19.1 MB |
| Whole-entity ToList (tracked) | — | 43.0 ms / 51.4 MB | 25.4 ms / 25.2 MB |
| Reference Include→ToList | 38.8 ms / 7.9 MB | 138.3 ms / 52.0 MB | 114.7 ms / 22.3 MB |

- **Native vs current provider:** ~**51–58% less allocation** on every shape; ~**45–53% faster** on allocation-heavy reads. Closes ~60–70% of the time/alloc gap to the raw driver on materialization-heavy shapes.
- **Native vs driver-only:** still ~4–6× allocation above the raw-driver floor — that residual is EF's model/shaper/tracking/Include machinery, the headroom for future work.

**2. Conformance ceiling (the stronger argument).** The current provider delegates translation to the driver's LINQ-v3 provider, which (a) lacks operators (no `LeftJoin` translator, etc.) and (b) was **not built to EF Core semantics** — so a class of EF Core spec tests fail or are `// Fails`-flagged *because of the driver's LINQ provider*, and are painful/impossible to fix without changing the driver. A native translator written **to the EF Core spec tests** removes that ceiling: it lowers the achievable-conformance limit from `min(MongoDB-can-express, driver-LINQ-supports-AND-matches-EF-semantics)` down to just **`MongoDB-can-express`** — the only limit worth having.

**Important conformance reality:** "the suite passes" in spike validation means the **currently-enabled subset** of EF Core spec tests (those overridden and not `// Fails`-flagged). It is NOT full spec conformance — many spec tests are unimplemented/not-overridden, and many are `// Fails`. **The real success metric for the rebuild is EF Core spec conformance**, not the spike's "native coverage of enabled tests" (~64% spec / ~82% functional in force mode — see below; that's a progress proxy, not the goal).

## Architecture verdict: what to rebuild vs keep

**Rebuild (the Query *translation* subsystem) — ground-up, on a proper AST:**
- The foundational compromise is that `MongoQueryExpression` is **not a real query AST**. `Where`/`OrderBy`/`Skip`/`Take` are NOT structured nodes — the `MongoQueryableMethodTranslatingExpressionVisitor` returns `null` for them and they survive as a raw LINQ `MethodCallExpression` chain on `CapturedExpression`, *to be forwarded to the driver*. Build instead a canonical Mongo query AST (a `SelectExpression`-analog: structured `Predicate`, `Orderings`, `Projection`, `Offset`/`Limit`, and `$lookup`/stages as **first-class nodes**), populated by the `QueryableMethodTranslatingExpressionVisitor` and consumed by a dedicated pipeline (MQL) generator. This is the proven relational-EF shape; the provider deliberately skipped it to delegate.
- **Delete the delegation scar tissue** (these get removed, not refactored):
  - `Query/Visitors/MongoEFToLinqTranslatingExpressionVisitor.cs` (+`.LeftJoin.cs`, ~1,800 LOC) — exists only to rewrite the EF tree into a *driver-LINQ* tree (`Mql.Field`, `.As<serializer>`, `AppendStage`).
  - The `LeftJoin → Join + LeftJoinResult` rewrite, and the two-join-shape reconciliation (`_outer`/`_inner` driver shape vs `_lookup_<Nav>` native shape; `UsesDriverJoinFields`; `GetStreamingReferenceLookups()` synthesizing lookups *from* the driver's join decision in `MongoQueryExpression.Lookup.cs`).
  - The shallow `MongoQueryExpression` / `CapturedExpression` raw-chain IR itself.
- The dual-shaper `DispatchingQueryingEnumerable` + `nativeEligible` plumbing is migration scaffolding — keep the dual-path discipline *during* the rebuild (driver-LINQ fallback stays until native reaches parity), retire at parity.

**Keep (carry forward, do not rewrite):**
- **The streaming materializer** — `Query/NativeTranslation/MongoStreamingEntityMaterializerRewriter.cs` + `BsonRowReader` + the `StreamingEligibility` predicate. It is delegation-independent and EF-idiomatic: it reuses EF's `StructuralTypeShaperExpression` / structural-type-materializer injection / `ValueBufferTryReadValue` interception, swapping only the value source to a forward `IBsonReader`. It is modeled on EF's relational JSON-column streaming materializer (`JsonEntityMaterializerRewriter` in dotnet/efcore `RelationalShapedQueryCompilingExpressionVisitor.ShaperProcessingExpressionVisitor.cs`); `IBsonReader` is a heap object so it needs **no** `Utf8JsonReaderManager` ref-struct machinery. This is the source of the allocation win.
- **Storage / update / transactions / serialization / CSFLE / metadata / value-generation** — entirely independent of the LINQ-delegation question.
- **The public option** `MongoDbContextOptionsBuilder.UseNativeQuery(bool)` (default on) + `MongoOptionsExtension.UseNativeQuery` — the gate, correctly part of service-provider identity (so native/non-native don't collide in EF's compiled-query cache). (Open design Q: bool vs an enum `UseQueryMode(Native|DriverLinq)`.)
- **The entire test + benchmark rig** — the EF spec-conformance harness, `AssertMql`, the `MONGODB_EF_NATIVE_QUERY` force-mode coverage instrument, the benchmark project + baselines. This is the most valuable asset and is path-agnostic.

## What the spike proved (de-risked unknowns — you start knowing these)

- DOM-free forward-`IBsonReader` materialization works, is correct against the enabled spec suite, and yields the allocation win. Reusing EF's materializer/tracking via `ValueBufferTryReadValue` interception is the right seam.
- Native pipeline execution: build the pipeline as raw `BsonDocument[]` and run `IMongoCollection<BsonDocument|RawBsonDocument>.Aggregate(session, pipeline)` → `IAsyncCursor` → streaming shaper. (`MongoClientWrapper.Execute`.) `RawBsonDocument` as a *random-access* row was a measured DEAD END (+68–82% alloc — it re-scans per field); it only pays off *behind* a forward reader. (See `results/2026-06-17-dom-free-C.md`.)
- Predicate/sort/paging → `$match`/`$sort`/`$skip`/`$limit`, and reference Include → `$lookup`/`$unwind`, are all expressible as raw stages the provider builds itself (no driver join operator needed). (`results/2026-06-18-*`.)
- EF query-parameter shapes differ by version (`QueryParameterExpression`/`Parameters` on EF10 vs `ParameterExpression`/`ParameterValues` on EF8/9) — guard with `#if`.

## Class-(c) intrinsic limits — reconnaissance (CONFIRM against spec tests; not exhaustive)

These are *candidate* EF-Core semantics MongoDB may not be able to express — i.e. tests that would be `// Fails` on ANY architecture, native or delegating. Treat as a hypothesis list to validate/triage early in the rebuild (distinguish true class-c from class-b "expressible with effort"):
- **Null / three-valued logic:** MongoDB `$eq:null` matches missing fields; comparison operators type-bracket (only compare within BSON type, with a fixed cross-type order). EF/C# null-comparison and `!=` semantics diverge. Some expressible with explicit `$type`/`$exists` guards (class-b, awkward); some genuinely divergent (class-c). The spike already falls back on nullable-equality for this reason.
- **Cross-type comparison & sort order:** mixed-type fields sort by BSON type order, not .NET semantics.
- **String collation / case / culture:** `$regex` covers `StartsWith`/`Contains`; `StringComparison`/culture/case-insensitive semantics don't map cleanly to MongoDB collation (per-op/index-scoped).
- **DateTime/DateTimeOffset/TimeSpan:** UTC storage, `DateTimeKind`, offset preservation — known divergence areas.
- **Decimal/numeric:** Decimal128 vs .NET decimal mostly OK; mixed int/double/decimal arithmetic + overflow semantics differ.
- **Translate-or-throw contract:** some spec tests assert a *specific exception* for untranslatable queries — the native provider must match EF's translate-or-throw behavior (right exception type), a behavior-conformance concern distinct from can't-express.
- **Unspecified result order:** results without `$sort` are unordered; some tests assume/assert order.

## Current native coverage (force-mode measure, EF10, 2026-06-20) — a progress proxy, NOT the goal

Run the suite with `MONGODB_EF_NATIVE_QUERY=force` (throws instead of falling back; failures = "still needs driver LINQ"): ~**64% SpecificationTests Query / ~82% FunctionalTests Query** run fully native. Biggest remaining buckets (what the rebuild must cover), largest first:
1. **Predicate breadth** — string methods/`Contains`/`IN`/subqueries/computed/date/member-cast/nullable-eq (the long tail; the spike's `MongoPredicateTranslator` only does `==,!=,<,<=,>,>=,&&,||`,bool).
2. **Includes beyond single-level reference** — collection / nested / filtered.
3. **Two whole-path cutouts hardwired to driver-LINQ** regardless of operator support: **scalar cardinality** (`Count`/`First`/`Any`/aggregates → `ExecuteScalar`) and **scalar/anonymous projection push-down** (`ExecuteProjectedQuery`, `nativeMode:Off`). The new AST should make these native (`$count`/`$limit`/`$group`, `$project`).
4. Discrete operators: `GroupBy`→`$group`, `SelectMany`, set ops, `Distinct`, `OfType`/type-tests, `VectorSearch`, non-canonical `Skip`/`Take`.

## How to run things

- **Mongo:** a replica set is required (the provider's `SaveChanges` uses transactions). `docker run -d --name ef-bench-mongo -p 27017:27017 mongo:8 --replSet rs0` then `rs.initiate({_id:'rs0',members:[{_id:0,host:'localhost:27017'}]})`. Tests/benchmarks read `MONGODB_URI`.
- **Build the provider:** `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"` (also validate `Debug EF8`). The provider uses build *configurations* `Debug|Release EF8|EF9|EF10`, not plain `Debug/Release`.
- **Tests (per-assembly — a combined solution run causes spurious shared-DB cross-assembly failures):** `MONGODB_URI=... dotnet test tests/<assembly>/*.csproj -c "Debug EF10" --filter "FullyQualifiedName~Query"`.
- **Force-mode coverage instrument:** prefix any test run with `MONGODB_EF_NATIVE_QUERY=force` to make non-native queries throw — the failure set + messages are the "what still isn't native" report.
- **Benchmarks:** `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/` — build/run `-c "Release EF10"`, InProcess toolchain (the default BenchmarkDotNet toolchain breaks on the config-conditional csproj). `dotnet run -c "Release EF10"` runs the headline three-config set; `-- --query` runs the per-shape set; `-- --smoke` does a fast correctness check.

## Artifacts (all on `spike/low-level-provider`)

- Program design: `docs/superpowers/specs/2026-06-16-low-level-provider-migration-design.md`.
- Per-sub-project specs + plans: `docs/superpowers/specs|plans/2026-06-1{6,7,8}-*` (benchmark baseline A; native MQL B; DOM-free C/C′; owned collections D; native-query default + harden E; native reference Include).
- Results/measurements: `benchmarks/.../results/2026-06-1{6,7,8}-*.md` and `2026-06-20-headline-three-config.md`.
- Spike Query code (reference for the rebuild — keep the materializer, study but replace the translation): `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/` + the gate edits in `MongoShapedQueryCompilingExpressionVisitor.cs`.

## Where to start the rebuild

1. Design the canonical Mongo query AST (`SelectExpression`-analog) + the pipeline generator that renders it to `BsonDocument[]`. This is the new foundation.
2. Port the streaming materializer onto it (it's already separable).
3. Keep the driver-LINQ path alive as the gated fallback during the rebuild (same dual-path / `force`-mode discipline that kept the spike at zero regressions); drive EF **spec conformance** up, retire the fallback at parity.
4. Triage the class-(c) list early so the conformance target is realistic.

Also pre-merge cleanup carried over: `src/MongoDB.EntityFrameworkCore/Query/AGENTS.md` is now **stale** (it states the provider "sits on top of the driver's LINQ v3 provider... does not generate aggregation BSON itself" — the native path contradicts this); update it to describe the rebuilt architecture.
