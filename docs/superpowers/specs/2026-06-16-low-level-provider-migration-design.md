# Migrating the provider to low-level driver access — program design

**Date:** 2026-06-16
**Status:** Approved (program roadmap + sub-project A design); pending implementation plan for sub-project A
**Branch:** `spike/low-level-provider` (off `main`)

## Goal

Move the MongoDB EF Core provider off the C# driver's LINQ pipeline so that EF owns LINQ→MQL
translation and object materialization, and the driver is used only for low-level access
(connection/protocol/auth/sessions, BSON, cursors, CSFLE, raw command/aggregate execution). This
is the production realization of the analysis in the conversation and of the DOM-free
materialization spike (`spike/dom-free-materialization`), which measured a reader-based shaper at
~2.3× faster / ~65% fewer allocations than the current `BsonDocument`-DOM path.

## Why this is a program, not one project

The Query area is ~8,300 lines across ~40 files. The pieces that must change are the largest:
the EF→driver-LINQ bridge (`MongoEFToLinqTranslatingExpressionVisitor` + `.LeftJoin`, ~1,780
lines), the projection-binding / DOM shaper (`MongoProjectionBinding*`, ~2,550 lines), and the
method translator (~560 lines). Rewriting all of it at once would break the entire query pipeline
with no working baseline. So the work is decomposed into sequential, individually plan-sized
sub-projects, each with its own spec → plan → implementation cycle.

## Key strategic decisions

- **Dual-path + per-query fallback.** The native MQL/reader path is added behind a switch. When
  native translation hits an unsupported node, the query falls back to the existing driver-LINQ
  path. The full existing test suite keeps passing throughout; tests are flipped onto the native
  path to surface gaps. The driver-LINQ path is deleted only at the final step, once at parity.
- **Existing tests are the validation.** No new correctness test suite; the provider's existing
  Functional/Specification tests validate the native path (run with native forced on).
- **First native slice is minimal:** single collection — `Where`, simple scalar/entity projection,
  `OrderBy`/`ThenBy`, `Skip`/`Take`. Everything else falls back.

## Program roadmap (sequential)

- **A. In-repo benchmark harness + current-path baseline** *(this spec; foundational, first)* —
  measurement infrastructure and committed baseline numbers. No provider code changes.
- **B. Native MQL generation + raw execution** — translate `MongoQueryExpression` directly to an
  aggregation-pipeline `BsonDocument[]`, execute via `IMongoCollection.Aggregate(rawPipeline)`,
  behind the switch with fallback. First slice only.
- **C. Reader-based (DOM-free) materializer** — replace the `BsonDocument`-DOM shaper with a
  forward-only `BsonBinaryReader` shaper for the native slice (validated by the spike).
- **D. Operator-coverage expansion** — iterate B+C across the operator surface (filter, projection,
  ordering, paging, joins/Includes, grouping, set/aggregate ops) until parity with the existing
  path.
- **E. Remove the driver-LINQ path** — delete the bridge; provider uses only low-level driver
  access.

Each of B–E is planned and implemented separately, against the benchmark baseline from A.

---

# Sub-project A design: benchmark harness + baseline

## Scope

Stand up a benchmark project in the repo that measures the **current** provider end-to-end on the
query shapes the migration will touch, and capture committed baseline numbers. A changes **no**
provider code — it only observes.

## Build setup (handles the EF-config matrix)

The provider declares only `Debug/Release EF8|EF9|EF10` configurations (no plain `Release`), and
EF8/EF9 build `net8.0` while EF10 builds `net10.0`. A project referencing the provider must pin a
specific EF configuration or it will not compile.

- New project `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/`, **not** added to
  `MongoDB.EFCoreProvider.sln` (keeps the EF-config matrix and `/test-all` undisturbed).
- Empty `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/Directory.Build.props` (`<Project/>`) to
  stop any future repo-root build props from leaking in.
- Target framework `net10.0` (must match the provider's EF10 TFM — a `net10.0` app cannot reference
  a `net8.0`-built assembly).
- Reference the provider with a pinned configuration so it always builds the same way regardless of
  the benchmark's own config:
  ```xml
  <ProjectReference Include="..\..\src\MongoDB.EntityFrameworkCore\MongoDB.EntityFrameworkCore.csproj"
                    SetConfiguration="Configuration=Release EF10" />
  ```
- EF package version from `Versions.props` (`$(EF10Version)` = 10.0.8), pulled in by explicitly
  importing `Versions.props` (no repo-root `Directory.Build.props` exists to import it
  automatically).
- `BenchmarkDotNet` 0.15.8.

**EF version pinned to EF10 / net10.0:** it is the current major the repo leads with, the dev
machine has the .NET 10 SDK, and net10.0 is the more representative runtime for an
allocation/materialization perf baseline. The baseline only needs to be *consistent*, not
multi-version.

## Model + dataset

- `BenchmarkDbContext` with one entity `Customer`: ~10 scalar properties (mix of int, long, string,
  ObjectId, DateTime, decimal, double, bool, Guid, enum) + an owned `Address` (a few scalars).
  Deliberately close to the DOM-free spike's entity so materialization cost is comparable.
- `[GlobalSetup]`: connect to `MONGODB_URI` (default `mongodb://localhost:27017`); create a
  uniquely-named database; `EnsureCreated`; seed N = 10,000 `Customer`s deterministically;
  `SaveChanges`.
- `[GlobalCleanup]`: drop the database.
- Requires a running MongoDB (documented). No testcontainers dependency — keeps the benchmark
  project light.

## Benchmarks

`[MemoryDiagnoser]`; each benchmark opens a fresh `DbContext` per invocation (so EF identity-map
caching doesn't skew results). A capped job (3 warmup / 10 iterations) keeps a live-DB run
reasonable; allocations are the clean, server-independent signal and mean time is directional.
Shapes map directly onto the first native slice (B):

- `Where_ToList` — `Customers.Where(c => c.Active).ToList()`
- `Projection_ToList` — `Customers.Select(c => new { c.Name, c.Count }).ToList()`
- `OrderBy_Take` — `Customers.OrderBy(c => c.Count).Take(100).ToList()`
- `Tracked_ToList` — `Customers.ToList()` (full tracked entities)
- `NoTracking_ToList` — `Customers.AsNoTracking().ToList()` (full entities)

## Baseline capture

Run on the current provider; save the BenchmarkDotNet summary table verbatim to
`benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-16-baseline.md` with the
environment header. This is the reference B–E compare against. The `BenchmarkDotNet.Artifacts/`
output directory is left untracked (not committed).

## Explicitly out of scope (YAGNI for A)

- Materialization-only micro-benchmarks wired to the real serializers.
- Include/join and grouping benchmarks.
- Multi-EF-version benchmark runs.
- Any provider code change.

These arrive with the later slices that need them.

## Risks / notes

- A live MongoDB is required to run the benchmarks; the baseline numbers are machine- and
  server-dependent. Allocations are the portable signal; absolute times are only comparable on the
  same machine/server.
- `SetConfiguration` on the `ProjectReference` is the mechanism that pins the provider to
  `Release EF10`; if a future EF-config rename happens, this string must track it.
