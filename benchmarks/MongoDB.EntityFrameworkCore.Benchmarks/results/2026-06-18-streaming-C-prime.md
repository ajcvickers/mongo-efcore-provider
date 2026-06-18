# Streaming materializer (sub-project C') — results

> **Final review verdict: SOLID-FOR-SPIKE** (query-reviewer probed owned present/absent, nullable, enum,
> Guid, value-converter, nested owned-owning-owned, tracking, and fallback under forced streaming — all
> correct). Non-blocking follow-ups for productionization:
> 1. **Broaden the rewriter's fallback catch.** `CompileShapedQuery`/`TranslateQuery` catch only
>    `NativeTranslationNotSupportedException`; an unexpected EF-version tree-shape change could throw another
>    type and hard-crash with no DOM fallback. For production, catch any exception in `auto` mode → fall back
>    (force still surfaces it). Deliberately NOT done for the spike: the narrow catch surfaces real rewriter
>    bugs during development.
> 2. **Deterministic RawBsonDocument disposal on abandoned/early-broken enumeration.** Rows are disposed
>    after shaping; on early `foreach` break / mid-stream exception, undelivered fetched rows rely on
>    finalization. GC-pressure/native-buffer risk, not correctness. Dispose undelivered rows in
>    `Enumerator.Dispose`/`DisposeAsync`.
> 3. Nits: document `BsonRowReader.Open`'s row-lifetime contract; comment the intentional nullable pre-empt
>    in `BuildTypedRead`; prefer reference-equality over name-match in `FindChildPlan`.

**Run on:** 2026-06-18, BenchmarkDotNet v0.15.8, Apple M4 Max (1 CPU, 14 logical / 14 physical cores) / macOS Tahoe 26.5.1 (25F80) [Darwin 25.5.0] / .NET SDK 10.0.301 / .NET 10.0.9 (10.0.926.27113), Arm64 RyuJIT armv8.0-a (InProcessEmitToolchain, IterationCount=10, WarmupCount=3). MongoDB: replica set at `mongodb://localhost:27017`.
**Change:** native-path eligible entities materialized by a forward-only `IBsonReader` streaming rewriter (typed locals, no `BsonDocument` DOM, no per-field `BsonValue` boxing); EF construction/tracking reused verbatim.

## Regression check (auto mode, per-assembly)

Per-assembly Query filter, each run **separately** (a combined solution run produces only the known
shared-DB `dropDatabase`/`being dropped` cross-assembly pollution — not code failures):

| Assembly | Passed | Failed | Skipped |
|---|---:|---:|---:|
| UnitTests | 8 | 0 | 0 |
| FunctionalTests | 544 | 0 | 44 |
| SpecificationTests | 4345 | 0 | 18 |
| **Total** | **4897** | **0** | **62** |

**4897 passed / 0 failed / 62 skipped — matches pre-C' exactly (0 failures).**

### Regressions found and fixed (all in `src/`)

The initial run surfaced 152 (FunctionalTests) Query failures from C', all instances of one runtime guard:
`InvalidOperationException : "Streaming materialization was selected but the query did not translate to a
native streaming pipeline."` Three distinct root causes, plus one materialization-semantics gap:

1. **Cardinality reducers selected streaming but never built a native pipeline**
   (`Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`). The compile-time streaming decision
   keyed only on `NativeQuery.Mode` + `StreamingEligibility`, but the run-time native-pipeline gate in
   `TranslateQuery` additionally requires `ResultCardinality.Enumerable` and zero pending lookups. A `First`/
   `Single`/etc. (or a pending-lookup query) over an eligible entity therefore committed to the RawBsonDocument
   streaming shaper at compile time, then fell through to the driver-LINQ path at run time (BsonDocuments) and
   tripped the guard. Fix: mirror that gate in the compile-time decision (`ResultCardinality == Enumerable &&
   GetPendingLookups().Count == 0`). This alone fixed ~145 of the failures (the bulk of `ValueConverterTests`).

2. **Predicates the native pipeline can't translate fell through at run time, mismatching the row type**
   (same file). Even within the gate, a `Where` predicate the native translator rejects (dictionary
   `ContainsKey`, list `.Contains`/`.Count`, `Mql.IsMissing`/`Exists`) throws
   `NativeTranslationNotSupportedException` *inside* `TranslateQuery` at run time and falls back to driver-LINQ
   — a decision the compile-time shaper choice cannot predict. Fix: when streaming is selected, compile **both**
   shapers (the RawBsonDocument streaming shaper and the BsonDocument DOM shaper) and have
   `ExecuteStreamingShapedQuery` dispatch at run time on `executableQuery.Streaming` — streaming shaper for the
   native pipeline, DOM shaper for the driver-LINQ fallback. Both `QueryingEnumerable<,>` instances are wrapped
   in a small `DispatchingQueryingEnumerable<TResult>` implementing both `IEnumerable<TResult>` and
   `IAsyncEnumerable<TResult>` so EF's sync and async query executors both get a usable static return type.
   (Removed the now-impossible guard throw; fixed the `WhereDictionaryTests`/`WhereTests`/`MqlMethodTests`
   group and the `async: True` vector-index throw tests, whose negative assertions previously saw the wrong
   exception type.)

3. **Required owned references silently yielded null instead of throwing**
   (`Query/NativeTranslation/MongoStreamingEntityMaterializerRewriter.cs`, `.../StreamingEligibility.cs`).
   `OwnedEntity_throws_when_missing_required_owned_entity` expects an `InvalidOperationException` when a
   *required* owned sub-document is absent (EF's DOM path enforces this via `BsonBinding.GetBsonDocument`'s
   required-field guard). The streaming rewriter tracked presence only as a nullable navigation
   (`!present ? null : block`). Fix: in `RewriteOwnedNavigation`, when `navigation.ForeignKey.IsRequiredDependent`
   the `!present` branch throws the same `"Field '<name>' required but not present in BsonDocument for a
   '<owner>'."` exception, rather than tightening eligibility to fall back. Reproducing the throw (rather than
   falling back) keeps required-owned entities — including the benchmark's `Customer.Address`, which **is**
   `IsRequiredDependent` — on the streaming path, which is where the measured win comes from.

No test was weakened, skipped, or disabled. Streaming was not disabled wholesale.

## Benchmark: C' vs A baseline (and B, C)

Baselines: `results/2026-06-16-baseline.md` (A, driver-LINQ + DOM), `2026-06-17-native-B.md` (native MQL,
DOM materialization), `2026-06-17-dom-free-C.md` (RawBsonDocument under the random-access shaper — reverted,
+70–82% alloc). C' numbers are the stable second of two runs (allocations were deterministic to <0.02% across
runs: e.g. Where 28797.38 → 28798.07 KB). Customer is streaming-eligible (single ObjectId key, scalars + one
owned `Address` reference), so all four whole-entity shapes stream; `Projection_ToList` has a real trailing
user `Select` and still falls back to driver-LINQ.

| Shape | A Mean | C' Mean | Δ Mean | A Alloc | C' Alloc | Δ Alloc % | C' path |
|---|---:|---:|---:|---:|---:|---:|:--|
| Where_ToList      |  55.978 ms |  32.259 ms | −42.4% |  73.46 MB |  28.12 MB | **−61.7%** | streaming |
| Projection_ToList |  13.805 ms |  14.745 ms |  +6.8% |   8.27 MB |   8.27 MB |   **0.0%** | fallback |
| OrderBy_Take      |   3.492 ms |   3.613 ms |  +3.5% |   1.55 MB |   0.63 MB | **−59.4%** | streaming |
| Tracked_ToList    | 105.559 ms |  61.128 ms | −42.1% | 147.04 MB |  56.37 MB | **−61.7%** | streaming |
| NoTracking_ToList |  70.214 ms |  35.581 ms | −49.3% | 125.83 MB |  40.06 MB | **−68.2%** | streaming |

(C' Alloc in MB = reported KB / 1024: Where 28798.07 KB, Projection 8470.72 KB, OrderBy 645.01 KB,
Tracked 57726.62 KB, NoTracking 41017.32 KB.)

## Reading & recommendation

- **The streaming win is real, large, and lands exactly where C' predicted.** Every shape that streams drops
  allocations **−59% to −68%** versus the A baseline — squarely in the spike's ~65% pure-materialization range,
  and the exact inverse of sub-project C's +70–82% regression (C kept random-access by-name access on
  RawBsonDocument; C' reads each field once, forward-only, into typed locals with zero per-field `BsonValue`
  churn and no element dictionary). The control shape (`Projection_ToList`, fallback) is byte-identical to
  baseline (8.27 MB) — isolating the entire change to the streaming path.
- **NoTracking shows the biggest allocation win (−68.2%)**, as expected: with less EF change-tracking /
  identity / snapshot overhead to dominate, removing the DOM is a larger share of the total. The two tracked
  paths (`Where_ToList`, `Tracked_ToList`) still drop −61.7%, well above the "~15–30% tracking-capped"
  conservative expectation — EF's per-entity tracking cost is real but the DOM + per-field `BsonValue` boxing
  it sat on top of was a larger chunk than feared.
- **Mean time also improves materially on the materialization-bound shapes** (Where −42%, Tracked −42%,
  NoTracking −49%) — unlike B (translation-only, flat times) and C (slower from re-materialization). The two
  small/cheap shapes (`Projection_ToList` fallback, `OrderBy_Take` which returns only 10 rows) move within
  run-to-run InProcess variance (±a few %); their absolute times are dominated by fixed query overhead, not
  materialization, so the allocation win there (−59% on OrderBy_Take) is the more meaningful signal.
- **Recommendation:**
  - **Make streaming the default for the shapes it covers (already effectively the case in `auto`), and keep
    the DOM path as the fallback** — do not retire it. The dual-shaper dispatch (fix 2) is exactly the
    mechanism that lets streaming be aggressive on eligibility while staying correct: any query whose predicate
    or pipeline the native translator can't build silently and correctly falls back to DOM at run time. That
    safety net is cheap (one extra compiled shaper per eligible query) and is what makes "stream by default"
    tenable.
  - **The rewriter's complexity is justified by this win.** A −60%+ allocation cut on the provider's hottest
    path (entity enumeration) for flat + single-owned-reference entities is the headline result of the whole
    low-level-provider effort, and these are the most common real-world shapes.
  - **Extend eligibility next, in order of payoff:** owned **collections** (very common, currently the biggest
    gap — needs the forward reader to handle a sub-array loop), then **includes / cross-collection
    navigations**, then **TPH discriminator hierarchies** (cheap: read the discriminator field first, pick the
    concrete plan). **Projections** are lower priority — they already push down to driver-LINQ efficiently and
    don't carry the DOM cost. Each extension should reuse the same "rewriter throws → fall back" safety contract
    so it can ship incrementally without risk.
