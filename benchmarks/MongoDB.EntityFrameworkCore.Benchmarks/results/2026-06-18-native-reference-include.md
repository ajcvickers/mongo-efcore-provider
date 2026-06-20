# Native reference Include — results

**Run on:** 2026-06-20 — Apple M4 Max (14 logical / 14 physical cores), macOS Tahoe 26.5.1 (Darwin 25.5.0), .NET SDK 10.0.301 / Runtime 10.0.9 Arm64 RyuJIT, BenchmarkDotNet v0.15.8 (InProcessEmitToolchain, 3 warmup / 10 iterations). MongoDB replica set at mongodb://localhost:27017.
**Change:** single-level reference Include → native $lookup+$unwind, streaming materialization of the joined non-owned entity.

## Regression check (auto, per-assembly)
| Assembly | Passed | Failed | Skipped |
|---|---|---|---|
| UnitTests (Query) | 8 | 0 | 0 |
| FunctionalTests (Query) | 546 | 0 | 44 |
| SpecificationTests (Query) | 4345 | 0 | 18 |

0 failures across all three assemblies — matches the pre-Include baseline exactly. No `src/` fixes were required. No `AssertMql` baseline churn: the Northwind reference-Include spec tests (e.g. `Order.Customer`) and all other Query specs passed unchanged, so their fixed expected-MQL baselines still hold. No fallback adjustments needed.

## Opt-out (MONGODB_EF_NATIVE_QUERY=off): 4345 passed / 0 failed / 18 skipped (SpecificationTests Query)

## Benchmark: Review.Include(Product) — streaming vs DOM
10,000 Reviews, 100 Products, ProductId round-robin. Reference Include in a separate collection.

| Shape | DOM Mean | Streaming Mean | Δ Mean | DOM Alloc | Streaming Alloc | Δ Alloc % |
|---|---|---|---|---|---|---|
| Review_Include_NoTracking | 136.385 ms | 114.915 ms | -21.470 ms (-15.7%) | 50.75 MB | 21.81 MB (22336.36 KB) | -57.0% |
| Review_Include_Tracked | 146.884 ms | 125.677 ms | -21.207 ms (-14.4%) | 45.77 MB | 26.41 MB (27045.70 KB) | -42.3% |

## Reading & recommendation
- Native `$lookup`+`$unwind` with streaming materialization of the joined non-owned entity is a clear win on both axes for single-level reference Include: ~14–16% faster and 42–57% less allocation versus the DOM/driver-LINQ path. The allocation reduction is the dominant signal (NoTracking drops from 50.75 MB to 21.81 MB), consistent with the streaming reader avoiding the intermediate BSON document object graph per row — the same pattern observed for the flat / owned-collection (Basket) entity shapes, now extended across a cross-collection join.
- The tracked vs no-tracking gap narrows on streaming (Δ alloc 42.3% tracked vs 57.0% no-tracking) because identity-resolution / change-tracker entries are unaffected by the materialization path and add a fixed overhead on top.
- Next slice: collection Include (`HasMany`), then filtered Include and `ThenInclude` (multi-level) — all currently fall back to driver-LINQ; bringing them onto the streaming path should yield comparable allocation wins on the larger fan-out shapes.
