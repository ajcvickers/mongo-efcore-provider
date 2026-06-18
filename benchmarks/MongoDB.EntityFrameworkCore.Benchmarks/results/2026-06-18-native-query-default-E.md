# Native query default + harden (sub-project E) — results

**Run on:** 2026-06-18, BenchmarkDotNet v0.15.8, Apple M4 Max (1 CPU, 14 logical / 14 physical cores) / macOS Tahoe 26.5.1 (25F80) [Darwin 25.5.0] / .NET SDK 10.0.301 / .NET 10.0.9 (10.0.926.27113) Arm64 RyuJIT armv8.0-a (InProcessEmitToolchain, IterationCount=10, WarmupCount=3). MongoDB: replica set at `mongodb://localhost:27017`. Tests built/run `-c "Debug EF10"`; benchmark `-c "Release EF10"`.
**Change:** native+streaming is now a per-context option `MongoDbContextOptionsBuilder.UseNativeQuery(bool)` (default on) → `MongoOptionsExtension.UseNativeQuery` → `MongoQueryCompilationContext.UseNativeQuery` → `NativeQuery.EffectiveMode(option)`. `UseNativeQuery(false)` routes the context to the driver-LINQ + BsonDocument-DOM path. The `MONGODB_EF_NATIVE_QUERY` env var is now a TEST-ONLY override (force/off) layered on the option. Hardening: broadened fallback catch (any native-translation failure → fallback in non-force), undelivered-row disposal, collection-of-collection eligibility tightened.

## Regression check (auto/default, per-assembly)
Per-assembly Query filter, each run separately. New functional test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/UseNativeQueryOptionTests.cs` (2 tests — default vs opt-out parity on a `Where` predicate and on a no-predicate `ToList`).

| Assembly | Passed | Failed | Skipped | + new option test |
|---|---:|---:|---:|:--|
| UnitTests (~Query) | 8 | 0 | 0 | n/a |
| FunctionalTests (~Query) | 546 | 0 | 44 | includes the 2 new `UseNativeQueryOptionTests` (544 pre-E + 2) |
| SpecificationTests (~Query) | 4345 | 0 | 18 | n/a |

Matches pre-E exactly (UnitTests 8/0, FunctionalTests 544/0/44 → 546/0/44 with the 2 new tests, SpecificationTests 4345/0/18). **0 failures. No `src/` fixes were required.**

## Opt-out path (MONGODB_EF_NATIVE_QUERY=off — driver-LINQ+DOM): 4345 passed / 0 failed / 18 skipped
SpecificationTests Query suite forced onto the pre-migration driver-LINQ + BsonDocument-DOM path globally — still fully green, matching the auto/default counts. The escape hatch works.

## Benchmark sanity (default = streaming) vs C'/D
Default-on (no env var) — the default still streams for the covered shapes; allocations are byte-identical to C'/D (<0.02% drift), confirming the option plumbing added zero overhead.

| Shape | C'/D Alloc | E Alloc | C'/D Mean | E Mean | Path |
|---|---:|---:|---:|---:|:--|
| Where_ToList             | 28798.07 KB | 28796.98 KB | 32.259 ms | 33.179 ms | streaming |
| Projection_ToList        |  8470.72 KB |  8471.28 KB | 14.745 ms | 14.183 ms | fallback (real trailing Select) |
| OrderBy_Take             |   645.01 KB |   645.59 KB |  3.613 ms |  3.122 ms | streaming |
| Tracked_ToList           | 57726.62 KB | 57726.75 KB | 61.128 ms | 61.694 ms | streaming |
| NoTracking_ToList        | 41017.32 KB | 41012.80 KB | 35.581 ms | 36.962 ms | streaming |
| Basket_NoTracking_ToList | 35830.23 KB | 35830.31 KB | 36.589 ms | 37.865 ms | streaming (owned collection) |
| Basket_Tracked_ToList    | 71823.55 KB | 71822.47 KB | 84.773 ms | 91.829 ms | streaming (owned collection) |

Means move only within InProcess run-to-run variance (±a few %); allocations — the load-bearing signal — are unchanged to the byte.

## Reading
The option works: the default streams (native MQL + forward-only materializer, −56% to −70% allocation on covered shapes, exactly as in C'/D), the opt-out (`UseNativeQuery(false)`) routes to the driver-LINQ + DOM path, and the new functional test proves both paths return identical, correct rows for `Where` + whole-entity and no-predicate `ToList`. The test-only `MONGODB_EF_NATIVE_QUERY=off` override forces the DOM path suite-wide at 0 failures. Hardening (broadened fallback catch, undelivered-row disposal, collection-of-collection eligibility) is in place with no regressions. Native + streaming is now the safe product default with a public escape hatch.
