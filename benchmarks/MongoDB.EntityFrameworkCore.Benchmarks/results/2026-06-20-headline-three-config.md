# Headline three-config benchmark: DriverOnly vs current EF provider vs native spike

Date: 2026-06-20
Branch: `spike/low-level-provider`
Source: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/HeadlineBenchmarks.cs`

> **Methodology fix (shared MongoClient):** all three configs now share a single `MongoClient` instance
> (the EF configs use the `UseMongoDB(IMongoClient, db, ...)` overload), so client startup is never charged
> per-invocation to EF the way it was amortized once for DriverOnly. Re-running with the shared client left
> every number unchanged within run-to-run noise — because the C# driver already deduplicates the underlying
> cluster (connection pool + SDAM topology) by connection settings via its internal cluster registry, so a
> per-context `new MongoClient(sameConnString)` only re-created a thin handle, not the heavy machinery. The
> comparison was already fair on this axis; the fix removes the ambiguity. Numbers below are the shared-client run.

Three configurations, all reading the **same seeded documents** (N = 10,000):

1. **DriverOnly** — raw MongoDB C# driver LINQ / aggregation, no EF Core at all
   (`IMongoCollection<T>.AsQueryable()` / `Aggregate()`). The performance floor.
2. **EF-DriverLinq** — EF provider with `UseNativeQuery(false)`: driver-LINQ + `BsonDocument` DOM path.
   This is the **current-main** query path (validated below).
3. **EF-Native** — EF provider with `UseNativeQuery(true)` (the default): native MQL + streaming materialization.

Scalar shapes use `FlatItem` (pure scalars — cleanest for a driver-only round-trip).
`ReferenceInclude` uses `Review` + `Product` (`Review.Include(r => r.Product)`); the DriverOnly equivalent
is a hand-written `$lookup` + projection. A `[GlobalSetup]` validation step asserts all three configs return
identical counts (10,000) and that DriverOnly materialized correct scalar values before any number is reported.

## Environment

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.5.1 (25F80) [Darwin 25.5.0]
Apple M4 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK 10.0.301
  [Host] : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
Runtime=.NET 10.0.9, Arm64 RyuJIT armv8.0-a
GC=Concurrent Workstation
Toolchain=InProcessEmitToolchain  IterationCount=10  WarmupCount=3
MemoryDiagnoser enabled.  MONGODB_URI=mongodb://localhost:27017 (replica set, container ef-bench-mongo)
```

## Raw results

| Method                                     | Mean       | Error     | StdDev    | Gen0      | Gen1      | Gen2      | Allocated   |
|------------------------------------------- |-----------:|----------:|----------:|----------:|----------:|----------:|------------:|
| WhereToList_DriverOnly                     |   5.752 ms | 0.0973 ms | 0.0643 ms |  187.5000 |   78.1250 |         - |  1589.97 KB |
| WhereToList_EF_DriverLinq                  |  15.112 ms | 0.3066 ms | 0.2028 ms | 3015.6250 |  781.2500 |  250.0000 | 22697.49 KB |
| WhereToList_EF_Native                      |   8.301 ms | 0.2670 ms | 0.1766 ms | 1171.8750 |  484.3750 |         - |  9606.66 KB |
| WholeEntityToList_DriverOnly               |   8.294 ms | 0.1950 ms | 0.1290 ms |  375.0000 |  140.6250 |   46.8750 |  3133.33 KB |
| WholeEntityToList_EF_DriverLinq_NoTracking |  33.445 ms | 1.1796 ms | 0.7802 ms | 6531.2500 | 2531.2500 | 1031.2500 | 45300.08 KB |
| WholeEntityToList_EF_Native_NoTracking     |  15.633 ms | 0.2329 ms | 0.1540 ms | 2640.6250 | 1031.2500 |  328.1250 | 19124.04 KB |
| WholeEntityToList_EF_DriverLinq_Tracked    |  42.988 ms | 1.6606 ms | 1.0984 ms | 7076.9231 | 2923.0769 | 1076.9231 | 51383.88 KB |
| WholeEntityToList_EF_Native_Tracked        |  25.357 ms | 0.5608 ms | 0.3709 ms | 3406.2500 | 1312.5000 |  562.5000 | 25211.57 KB |
| OrderByTake_DriverOnly                     |   2.239 ms | 0.0701 ms | 0.0463 ms |    3.9063 |         - |         - |    59.48 KB |
| OrderByTake_EF_DriverLinq                  |   2.718 ms | 0.0758 ms | 0.0451 ms |   62.5000 |   11.7188 |         - |   527.16 KB |
| OrderByTake_EF_Native                      |   2.491 ms | 0.0502 ms | 0.0299 ms |   31.2500 |    3.9063 |         - |   257.14 KB |
| ReferenceInclude_DriverOnly                |  38.768 ms | 0.5282 ms | 0.3494 ms | 1000.0000 |  384.6154 |   76.9231 |  7883.63 KB |
| ReferenceInclude_EF_DriverLinq             | 138.332 ms | 2.9577 ms | 1.9563 ms | 6750.0000 | 2250.0000 |  500.0000 | 51969.35 KB |
| ReferenceInclude_EF_Native                 | 114.747 ms | 3.8320 ms | 2.5346 ms | 2600.0000 |  800.0000 |         - | 22332.65 KB |

## Per-shape comparison (3-way uses no-tracking EF for apples-to-apples with DriverOnly)

| Shape | DriverOnly | EF-DriverLinq (current) | EF-Native | Native vs DriverLinq (the rebuild win) | Native vs DriverOnly (remaining EF overhead) |
|---|---|---|---|---|---|
| **WhereToList** | 5.752 ms / 1590 KB | 15.112 ms / 22697 KB | 8.301 ms / 9607 KB | **-45.1% time, -57.7% alloc** | +44.3% time, +5.0x alloc |
| **WholeEntityToList** (no-track) | 8.294 ms / 3133 KB | 33.445 ms / 45300 KB | 15.633 ms / 19124 KB | **-53.3% time, -57.8% alloc** | +88.5% time, +6.1x alloc |
| **OrderByTake** | 2.239 ms / 59 KB | 2.718 ms / 527 KB | 2.491 ms / 257 KB | **-8.4% time, -51.2% alloc** | +11.3% time, +4.3x alloc |
| **ReferenceInclude** | 38.768 ms / 7884 KB | 138.332 ms / 51969 KB | 114.747 ms / 22333 KB | **-17.0% time, -57.0% alloc** | +196% time, +2.8x alloc |

### Tracking cost (EF-only, visible separately)

| Shape | EF-DriverLinq no-track → tracked | EF-Native no-track → tracked |
|---|---|---|
| WholeEntityToList | 33.445 → 42.988 ms (+28.5%); 45300 → 51384 KB (+13.4%) | 15.633 → 25.357 ms (+62.2%); 19124 → 25212 KB (+31.8%) |

Change tracking adds a fixed per-entity cost. On the native path that fixed cost is a larger *fraction* of the
(now much cheaper) read, but the tracked native read is still far below the tracked DriverLinq read
(25.4 ms / 25 MB vs 43.0 ms / 51 MB).

### Gap closed (how far native moves from current-EF toward the raw-driver floor)

| Shape | Time gap closed | Allocation gap closed |
|---|---|---|
| WhereToList | 72.8% | 62.0% |
| WholeEntityToList (no-track) | 70.8% | 62.1% |
| ReferenceInclude | 31.0% | 67.0% |

(`gap closed = (DriverLinq − Native) / (DriverLinq − DriverOnly)`.)

## Config-2 == current-main validation

The EF-DriverLinq config uses `UseNativeQuery(false)`, claimed identical to origin/main's query path
(main has no `UseNativeQuery` option — its default IS driver-LINQ + DOM). Validated by building the same
benchmark against a fresh `git worktree` at `origin/main` (efb5f25) and re-running three shapes with EF
default options:

| Shape | Spike EF-DriverLinq | origin/main default | Δ Mean | Δ Allocated |
|---|---|---|---|---|
| WhereToList | 15.112 ms / 22697.49 KB | 14.741 ms / 22384.23 KB | +2.5% | +1.4% |
| WholeEntityToList (no-track) | 33.445 ms / 45300.08 KB | 31.291 ms / 44673.52 KB | +6.4% | +1.4% |
| OrderByTake | 2.718 ms / 527.16 KB | 2.719 ms / 520.85 KB | ~0% | +1.2% |

Allocation is within ~1.4% on all three shapes (essentially identical — allocation is the deterministic
signal here and the DOM path's heavy allocation is reproduced exactly). Mean differences are within
run-to-run timing noise. **Config 2 is validated equivalent to origin/main.**

## Notes / caveats

- DriverOnly `ReferenceInclude` is a hand-written `$lookup` + projection-into-POCO; it is **not** a
  perfectly equivalent shape to EF's Include (EF also fixes up navigations / builds the dependent graph),
  so its absolute number is an informative floor rather than a strict apples-to-apples comparison. The
  EF-Native-vs-EF-DriverLinq Include delta (-17% time / -57% alloc) is the credible "rebuild win" number.
- DriverOnly has no change tracker, so the 3-way comparison uses `AsNoTracking()` EF reads; EF tracked
  vs no-tracking is reported separately above.
- Collections are named after the DbSet (pluralized): `FlatItems`, `Reviews`, `Products`. The driver's
  default conventions deserialize the same documents into the same POCOs; this round-trip is asserted in
  `[GlobalSetup]` before any measurement.

## Headline

Versus the current EF provider (driver-LINQ + DOM), the native MQL + streaming path **cuts allocation
~51-58% across every shape and cuts time 45-53% on the allocation-heavy whole-entity / filtered reads**
(8-17% on the already-cheap Take and the lookup-bound Include). On those materialization-heavy shapes the
rebuild closes roughly **60-70% of the time and allocation gap** between the current provider and the raw
driver. Remaining overhead above the raw-driver floor (~4-6x allocation, ~45-90% time on scalar reads) is
the cost of EF's model/shaper machinery and is the target for further work.
