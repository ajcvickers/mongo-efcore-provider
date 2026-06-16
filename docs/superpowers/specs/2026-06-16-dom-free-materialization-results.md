# DOM-free materialization spike — results

**Run on:** 2026-06-16, Apple M4 Max (1 CPU, 14 logical / 14 physical cores), macOS Tahoe 26.5.1 (25F80) [Darwin 25.5.0], .NET SDK 10.0.301, runtime .NET 8.0.17 (8.0.17, 8.0.1725.26602), Arm64 RyuJIT armv8.0-a.
**Corpus:** N = 10,000 documents, entity = 12 scalars + 1 nested doc + 1 array.

## Summary table

| Method               | N     | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Gen0       | Gen1     | Allocated | Alloc Ratio |
|--------------------- |------ |---------:|---------:|---------:|---------:|------:|--------:|-----------:|---------:|----------:|------------:|
| Dom_BsonDocument     | 10000 | 37.06 ms | 0.719 ms | 1.098 ms | 36.51 ms |  1.00 |    0.04 | 14615.3846 | 153.8462 | 116.91 MB |        1.00 |
| Reader_RawBytes      | 10000 | 16.18 ms | 0.236 ms | 0.221 ms | 16.15 ms |  0.44 |    0.01 |  5156.2500 |        - |  41.27 MB |        0.35 |
| Driver_TypedClassMap | 10000 | 12.51 ms | 0.051 ms | 0.045 ms | 12.52 ms |  0.34 |    0.01 |  2140.6250 |        - |  17.16 MB |        0.15 |

## Reading

- Reader vs DOM — time: 0.44x (i.e. Reader is faster than DOM — roughly 2.3x faster, 16.18 ms vs 37.06 ms). Allocations: 116.91 MB -> 41.27 MB (a 64.7% reduction).
- Reader vs Driver typed class-map (ceiling): time — Reader 16.18 ms vs Driver 12.51 ms (Reader is ~29% slower than the ceiling); allocations — Reader 41.27 MB vs Driver 17.16 MB (Reader allocates ~2.4x the ceiling, so meaningful headroom remains: ~24 MB / op).

## Decision

**GO.** The reader-based shaper more than satisfies the decision rule: it cuts allocations by 64.7% (116.91 MB -> 41.27 MB) while also more than halving wall-clock time (0.44x the DOM baseline). Both dimensions move strongly in the right direction with no correctness regression (the GlobalSetup gate confirmed all three paths produce identical entities). The gap to the typed class-map ceiling (12.51 ms / 17.16 MB) shows there is still ~29% time and ~2.4x allocation headroom to chase later, but the candidate already clears the bar against today's DOM path. Next step is to design the order-tolerant reader-shaper codegen for the provider (random-access -> forward-dispatch, constructor binding, includes) — a separate spec.
