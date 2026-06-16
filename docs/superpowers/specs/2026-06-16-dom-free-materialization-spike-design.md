# Spike: DOM-free materialization — design

**Date:** 2026-06-16
**Status:** Approved, pending implementation plan
**Type:** Throwaway spike (measurement only — no production code)

## Background

The MongoDB EF Core provider's query read path is currently:

```
wire bytes → driver builds a BsonDocument DOM → EF shaper walks it,
             (BsonElement/BsonValue allocations)   per property: BsonSerializationInfo.DeserializeValue(bsonValue)
```

Every result document is materialized twice — once into a `BsonValue` tree, then re-read
element-by-element into the entity (`Storage/BsonBinding.cs`). The DOM is intermediate garbage.
The DOM round-trip exists purely because the provider asks the driver for `BsonDocument` as the
cursor output type (`MongoShapedQueryCompilingExpressionVisitor` sets the cursor result to
`BsonDocument`; `EntitySerializer<T>.Deserialize` even throws `NotImplementedException` — the
serializer is only a translation oracle, never a materializer).

A reader-driven materializer would instead go **wire bytes → CLR directly** off an
`IBsonReader`, with no DOM and no `BsonValue` allocations — the way relational EF providers read
positionally from a `DbDataReader`, and the way the driver's own `BsonClassMapSerializer` works.

The driver fully supports this through public API: a `PipelineDefinition<TInput,TOutput>` carries
its own output `IBsonSerializer<TOutput>`, and the driver feeds that serializer a
`BsonBinaryReader` positioned over each document's raw bytes. No internal/unstable surface is
required. The open question is **how big the win actually is** — that decides whether the
order-tolerant reader-shaper codegen rewrite is worth building.

## Goal

Produce one credible measurement: **time and allocations to materialize a result set, DOM path vs
reader path, over identical documents, with correctness verified.** Allocations are the headline
metric — they are the core of the DOM cost story.

This is a spike. The code is throwaway; the output is a number and a go/no-go decision.

## Scope

In scope:
- Driver-level materialization isolation (no EF stack), as decided during brainstorming.
- A `byte[]` → entity comparison across three materialization strategies.
- An entity shape with nesting + an array, so the reader path exercises recursion.

Out of scope:
- Wiring any reader-shaper into EF's `QueryingEnumerable` / query pipeline.
- MQL generation.
- Live-cursor / round-trip / cursor-batch effects (a possible later variant, not this spike).
- Write-path (CLR → DOM → wire) symmetry.

## Key simplification: no live MongoDB

Materialization cost is identical whether the BSON bytes arrive off the wire or from memory — the
server is not part of the variable being isolated. The spike therefore generates its corpus
in-memory: create N deterministic entities, serialize each to `byte[]` via the driver
(byte-identical to what the server would store and return, including element names, types, and
insertion field order), and feed the same bytes to every path. This makes the spike deterministic,
free of Docker/network flakiness, and runnable anywhere.

## Project layout

- New standalone console project: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/`.
- Target framework `net8.0`.
- References: `MongoDB.Driver` (version from `Versions.props`) and `BenchmarkDotNet`. **No EF
  reference** — the point is to isolate driver-level materialization.
- **Deliberately excluded from `MongoDB.EFCoreProvider.sln`** so it does not perturb the
  `Debug/Release EF8|EF9|EF10` configuration matrix or the `/test-all` flow. Run directly with
  `dotnet run -c Release` from the project directory.

## Entity shape

`BenchmarkEntity`:
- ~12 mixed scalars: `int`, `long`, `string`, `ObjectId`, `DateTime`, `decimal`, `double`, `bool`,
  `Guid`, and an `enum` (plus a couple more scalars to round out the set).
- One owned nested document: `Address` (a few scalar fields).
- One primitive array: `int[]` or `string[]`.

Deterministic generation (fixed seed) so the corpus is reproducible and the correctness gate is
exact.

## The three paths (all `byte[]` → `BenchmarkEntity`)

1. **`Dom_BsonDocument` — baseline (mimics today's provider).**
   `BsonSerializer.Deserialize<BsonDocument>(bytes)` builds the DOM, then each field is read via a
   precomputed `BsonSerializationInfo.DeserializeValue(bsonValue)`, descending into the nested
   `Address` document and iterating the array — faithfully reproducing what `BsonBinding` does
   today.

2. **`Reader_RawBytes` — candidate.**
   `new BsonBinaryReader(bytes)` → a hand-written **order-tolerant** shaper:
   `ReadStartDocument()`; loop `ReadName()` → dispatch via a name→handler switch → read each scalar
   with the driver's primitive serializer; `ReadStartDocument()` recursion for `Address`;
   `ReadStartArray()` loop for the array; `SkipValue()` for any unmapped element. No DOM, no
   `BsonValue` allocations. Order-tolerant because BSON field order is not guaranteed to match the
   entity's property order.

3. **`Driver_TypedClassMap` — reference ceiling (optional, keep).**
   `BsonSerializer.Deserialize<BenchmarkEntity>(bytes)` using the driver's own auto-mapped
   `BsonClassMapSerializer` (already DOM-free). Shows how close the hand-shaper gets to the
   driver's optimized typed path — the practical floor on materialization cost.

## Measurement

- BenchmarkDotNet with `[MemoryDiagnoser]`.
- Each `[Benchmark]` method materializes the full corpus (N = 10,000 documents) per invocation, so
  results map directly to "materialize a 10k-row query result."
- Reported per path: mean time, allocated bytes/op, Gen0/1/2 collections.

## Correctness gate

In `[GlobalSetup]` (untimed): assert all three paths produce entities equal to the original
generated entities before any timing runs. A fast-but-wrong path would invalidate the spike, so
this gate is mandatory.

## Decision rule

- If `Reader_RawBytes` shows materially lower allocations and comparable-or-better time than
  `Dom_BsonDocument`, the order-tolerant reader-shaper codegen rewrite is justified.
- The gap between `Reader_RawBytes` and `Driver_TypedClassMap` indicates how much hand-tuning
  headroom remains beyond a straightforward reader-shaper.

## Risks / notes

- **Reader API details** (`BsonBinaryReader` construction over a `byte[]`/stream, the exact
  read-name/read-value/skip sequence) to be confirmed against driver 3.9.0 during implementation.
- **Fairness**: all three paths consume the identical `byte[]` corpus; only result type and
  materialization differ.
- The reader-shaper here is hand-written for one entity shape — it is a proof of cost, not the
  general codegen. The general codegen (random-access → forward-dispatch shaper restructuring,
  constructor binding, includes) is the follow-on work this spike informs.
