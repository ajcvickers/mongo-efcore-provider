# DOM-free Materialization via RawBsonDocument (Sub-project C) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the native query path return lazy byte-backed `RawBsonDocument` rows (instead of fully-parsed `BsonDocument` DOMs) — no shaper changes — to cut materialization allocations, and measure the win against the A baseline.

**Architecture:** Two edits. (1) `MongoClientWrapper.Execute` native branch fetches/aggregates `RawBsonDocument`; since `RawBsonDocument : BsonDocument` and the shaper does pure by-name access, rows flow into the unchanged `Func<QueryContext, BsonDocument, TResult>` shaper. (2) `QueryingEnumerable` disposes each row after shaping (the native rows are `IDisposable`; values are already copied out). The driver-LINQ fallback path is untouched.

**Tech Stack:** C#, EF Core 10 (build `Debug EF10`; benchmark `Release EF10`), MongoDB.Driver 3.9.0.

---

## Conventions
- Build provider: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`.
- A replica-set MongoDB runs at `mongodb://localhost:27017` (container `ef-bench-mongo`). Use `MONGODB_URI=mongodb://localhost:27017` for tests/benchmark.
- Preserve BOMs. `<Nullable>enable</Nullable>`.

---

## Task 1: Swap native rows to RawBsonDocument + dispose after shaping

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Storage/MongoClientWrapper.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/QueryingEnumerable.cs`

- [ ] **Step 1: Native branch → `RawBsonDocument` in `MongoClientWrapper.Execute<T>`**

Replace this block:
```csharp
            log = () => _commandLogger.ExecutedMqlQuery(executableQuery);
            var collection = Database.GetCollection<BsonDocument>(executableQuery.CollectionNamespace.CollectionName);
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = stages.ToArray();
            var cursor = executableQuery.Session is { } session
                ? collection.Aggregate(session, pipeline)
                : collection.Aggregate(pipeline);
            return (IEnumerable<T>)cursor.ToEnumerable();
```
with:
```csharp
            log = () => _commandLogger.ExecutedMqlQuery(executableQuery);
            // Return lazy, byte-backed RawBsonDocument rows instead of a fully-parsed BsonDocument DOM.
            // RawBsonDocument : BsonDocument, and the shaper does pure by-name access, so rows flow into
            // the existing Func<QueryContext, BsonDocument, T> shaper unchanged (T is BsonDocument here).
            var collection = Database.GetCollection<RawBsonDocument>(executableQuery.CollectionNamespace.CollectionName);
            PipelineDefinition<RawBsonDocument, RawBsonDocument> pipeline = stages.ToArray();
            var cursor = executableQuery.Session is { } session
                ? collection.Aggregate(session, pipeline)
                : collection.Aggregate(pipeline);
            return (IEnumerable<T>)cursor.ToEnumerable();
```
(`RawBsonDocument` is in `MongoDB.Bson` — already imported via the existing `using MongoDB.Bson;`. The `stages.ToArray()` is `BsonDocument[]`; the implicit conversion to `PipelineDefinition<RawBsonDocument,RawBsonDocument>` from a `BsonDocument[]` is the same operator used for the `BsonDocument` version. The `(IEnumerable<T>)` cast is safe: `T` is `BsonDocument` at runtime and `RawBsonDocument` derives from it.)

- [ ] **Step 2: Dispose each row after shaping in `QueryingEnumerable.MoveNextHelper`**

Both the sync `MoveNext` and async `MoveNextAsync` route through `MoveNextHelper`, so this single site covers both. Replace this line:
```csharp
                Current = _enumerator.Current is null ? default! : _shaper(_queryContext, _enumerator.Current);
```
with:
```csharp
                var row = _enumerator.Current;
                Current = row is null ? default! : _shaper(_queryContext, row);

                // Native-path rows are IDisposable RawBsonDocuments backed by a byte buffer. The shaper has
                // copied out every value it needs, so release the row immediately to avoid retaining buffers.
                // Driver-LINQ rows are plain BsonDocuments (not IDisposable), so this is a no-op there.
                if (row is IDisposable disposableRow)
                {
                    disposableRow.Dispose();
                }
```
(`IDisposable` is in `System` — confirm `using System;` is present in the file; add it if missing.)

- [ ] **Step 3: Build**
```bash
cd /Users/arthur.vickers/code/provider2
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
Expected: clean (0 errors). If `PipelineDefinition<RawBsonDocument,RawBsonDocument> = BsonDocument[]` fails to compile, use `PipelineDefinition<RawBsonDocument, RawBsonDocument>.Create(stages)` instead and note it.

- [ ] **Step 4: Force-mode smoke (native path returns RawBsonDocument)**
```bash
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=force dotnet run -c "Release EF10" -- --smoke
```
Expected: `SMOKE OK: where=50, proj=100, ordered=10, tracked=100, noTrack=100, withCity=100`. This confirms the native path materializes correctly from `RawBsonDocument` — including the owned `Address` (`withCity=100`). If it throws (e.g. a cast or disposal issue) or a count is wrong, debug; do NOT weaken the smoke. A likely failure mode to check: a value read AFTER the row is disposed — but the shaper runs before disposal, so this should not occur; if it does, it means something retains the row, which must be fixed (not by skipping disposal silently — investigate what holds the reference).

- [ ] **Step 5: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add src/
git commit -m "DOM-free: native path returns RawBsonDocument rows + dispose after shaping"
```

---

## Task 2: Validate (no regressions) + benchmark + record

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-17-dom-free-C.md`

- [ ] **Step 1: Auto-mode regression check (the key gate)**
```bash
cd /Users/arthur.vickers/code/provider2
MONGODB_URI=mongodb://localhost:27017 dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query" 2>&1 | tail -30
```
(Bash timeout 600000.) Native shapes now flow `RawBsonDocument` through the real shaper, exercising owned entities, composite keys, and arrays. Expected: **0 failures** (same as pre-C: UnitTests 8/0, FunctionalTests 544/0/44, SpecificationTests 4345/0/18 when run per-assembly; a single combined run is fine as long as failures are 0). If ANY test fails:
- A cast failure / wrong value with `RawBsonDocument` is a real bug. Most likely culprit: nested navigation or a serializer that assumed a concrete mutable `BsonDocument`. Investigate and fix in `BsonBinding`/shaper, keeping `RawBsonDocument` rows. Do NOT revert to `BsonDocument` to get green without flagging it.
- Re-run until 0 failures.

- [ ] **Step 2: Re-run the benchmark**
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10"
```
(Bash timeout 600000.) Capture the results table (Mean, Allocated, Gen0/1/2 per shape). The 5 shapes that go native: `Where_ToList`, `Projection_ToList`, `OrderBy_Take`, `Tracked_ToList`, `NoTracking_ToList` (all whole-entity or client-shaped projections — all native after sub-project B's entity-Select fix).

- [ ] **Step 3: Record results + recommendation**

Read `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-16-baseline.md` (A baseline) and `results/2026-06-17-native-B.md` (B). Create `results/2026-06-17-dom-free-C.md`:
```markdown
# DOM-free materialization (sub-project C) — results

**Run on:** <date/CPU/OS/SDK from the BenchmarkDotNet header>
**Change:** native path returns RawBsonDocument (lazy byte-backed) instead of BsonDocument DOM; rows disposed after shaping.

## Regression check (auto mode)
<passed/failed/skipped counts> — <statement that it matches pre-C (0 failures)>.

## Benchmark: C vs A baseline (and B)

| Shape | A baseline Mean / Alloc | C Mean / Alloc | Δ Alloc vs A |
|---|---|---|---|
<one row per shape, with the actual numbers and computed % allocation delta>

## Reading & recommendation
- <How much allocation dropped per shape. Expect a real but partial reduction — RawBsonDocument
  avoids the eager BsonValue tree + element dictionary but still allocates a BsonValue per accessed
  field, and EF change-tracking/identity allocations are unchanged.>
- <Recommendation: is the remaining gap large enough to justify a future full streaming-reader
  shaper sub-project (zero per-field BsonValue allocation), or is RawBsonDocument's win sufficient?>
```
Fill every `<...>` from the actual run; compute the allocation deltas.

- [ ] **Step 4: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-17-dom-free-C.md
git commit -m "DOM-free: record sub-project C results vs baseline"
```

---

## Notes for the executor
- Only the two `src/` edits in Task 1; no shaper/`BsonBinding` rewrite (that's a possible future sub-project, informed by these numbers).
- The driver-LINQ fallback path must stay on `BsonDocument` — only the `NativePipeline` branch changes.
- `BenchmarkDotNet.Artifacts/` is gitignored; commit only the `results/*.md`.
- Leave the `ef-bench-mongo` container running.
