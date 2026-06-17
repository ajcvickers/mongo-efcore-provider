# DOM-free materialization via RawBsonDocument (Sub-project C) — design

**Date:** 2026-06-17
**Status:** Approved; pending implementation plan
**Branch:** `spike/low-level-provider` (off `main`)
**Program:** sub-project C of the low-level-provider migration (see
`2026-06-16-low-level-provider-migration-design.md`). Follows B (native MQL generation).

## Goal

Reduce materialization allocations on the native query path by returning lazy, byte-backed
`RawBsonDocument` rows instead of fully-parsed `BsonDocument` DOMs — with no shaper rewrite — and
measure the resulting allocation win against the A baseline. This realizes (partially) the
DOM-free-materialization idea the spike proved, at low risk.

## Key findings (from code reading)

- The shaper does **pure by-name random access** to the document (`BsonBinding.TryReadElementValue`
  → `document.TryGetValue(name, ...)`, and `ElementPath` navigation via `(BsonDocument)rawValue` +
  `TryGetValue` for composite keys / nested owned entities). No sequential/streaming reads.
- The compiled shaper's row parameter is typed `BsonDocument`
  (`Func<QueryContext, BsonDocument, TResult>`), and `RawBsonDocument : BsonDocument`. So passing a
  `RawBsonDocument` requires **no shaper changes**.
- No code constructs or mutates the row `BsonDocument`; all access is read-only.
- The driver's built-in `RawBsonDocumentSerializer` deserializes a result document as raw bytes,
  skipping the eager `BsonValue` tree and the element dictionary.

## Scope

- Change applies **only to the native path** (`MongoExecutableQuery.NativePipeline` set). The
  driver-LINQ fallback path stays on `BsonDocument`, unchanged.
- No change to `BsonBinding` or the shaper codegen. The full forward-only streaming-reader shaper
  (zero `BsonValue` allocation — the spike's larger win) is explicitly **out of scope** and remains
  a candidate future sub-project, to be decided by C's measured result.

## The change

In `MongoClientWrapper.Execute<T>`, native branch:
- `Database.GetCollection<RawBsonDocument>(executableQuery.CollectionNamespace.CollectionName)`.
- `PipelineDefinition<RawBsonDocument, RawBsonDocument> pipeline = stages.ToArray();`
- `collection.Aggregate(session, pipeline)` / `collection.Aggregate(pipeline)` →
  `IAsyncCursor<RawBsonDocument>`.
- `return (IEnumerable<T>)cursor.ToEnumerable();` — `T` is `BsonDocument` at runtime; `RawBsonDocument`
  derives from it, so the cast is safe and rows flow into the existing shaper unchanged.
- MQL logging unchanged (logs `NativePipeline`).

## Correctness — row lifetime

`RawBsonDocument` is `IDisposable` (wraps a byte buffer). `QueryingEnumerable` materializes one row
at a time (`Current = _shaper(queryContext, row)`), and the shaper extracts independent CLR /
`BsonValue` copies into the entity — so each row may be disposed immediately after the shaper call.

Add a "dispose the row after shaping if it is `IDisposable`" step to `QueryingEnumerable`'s **sync
and async** move-next paths. This is a safe no-op for the driver-LINQ path (a plain `BsonDocument`
is not `IDisposable`) and prevents byte-buffer retention/leaks on the native path. Values already
extracted by the shaper remain valid after the row is disposed.

## Risks (measured, not blocking)

- `RawBsonDocument.TryGetValue` scans the byte buffer per lookup, so an N-field whole-entity read
  performs ~N linear scans (O(N²) in field count). Fine for the ~12-field benchmark entity; noted
  for very wide documents. The benchmark measures the time impact.
- Nested owned entities and composite keys rely on `ElementPath` navigation that casts
  `(BsonDocument)rawValue`; nested elements of a `RawBsonDocument` come back as
  `RawBsonDocument` / `RawBsonArray` (both derive from `BsonDocument` / `BsonArray`), so the casts
  and by-name access work. Verified by the existing tests in validation.

## Validation

1. **No regressions:** the existing query suite in `auto` mode stays at **0 failures** (4897
   passed pre-C). Native shapes now flow `RawBsonDocument` through the real shaper, exercising owned
   entities, composite keys, and arrays.
2. **Native correctness in `force` mode:** the native shapes still return correct results
   (benchmark `--smoke` prints the expected `SMOKE OK: ...`).
3. **Perf:** re-run the A benchmark with native on; compare each native shape's Mean/Allocated to
   `results/2026-06-16-baseline.md` (and B's `results/2026-06-17-native-B.md`). Record the
   allocation delta and a recommendation on whether the full streaming-reader shaper is worth a
   future sub-project.

## Components / files

- `src/MongoDB.EntityFrameworkCore/Storage/MongoClientWrapper.cs` — native branch → `RawBsonDocument`.
- `src/MongoDB.EntityFrameworkCore/Query/QueryingEnumerable.cs` — dispose the row after shaping if
  `IDisposable` (sync + async paths).
- `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-17-dom-free-C.md` — results.

## Out of scope (later)

- The forward-only streaming-reader shaper (rewriting `BsonBinding` + shaper codegen from
  random-access to forward-dispatch) — the spike's full ~65% materialization win.
- Any change to the driver-LINQ fallback path.
- `$project` pushdown, scalar cardinality, joins/grouping (earlier sub-projects' deferred items).
