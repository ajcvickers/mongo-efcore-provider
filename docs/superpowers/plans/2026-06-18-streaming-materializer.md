# Streaming (DOM-free) Materializer (Sub-project C′) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** On the native query path, for streaming-eligible entities (simple key; scalars + primitive arrays + single owned reference sub-documents), materialize each row via a single forward `IBsonReader` pass into typed locals — no `BsonDocument` DOM, no boxing — reusing EF's construction/tracking, with everything else falling back to the existing DOM path.

**Architecture:** A BSON analog of EF's `JsonEntityMaterializerRewriter` (`dotnet/efcore` `RelationalShapedQueryCompilingExpressionVisitor.ShaperProcessingExpressionVisitor.cs`). It rewrites EF's injected materializer block: declare one local per property/navigation, run a forward `ReadName`→`if/else`→typed-read loop filling the locals (Skip unknown), then let EF's normal construction/snapshot/tracking run with each `ValueBufferTryReadValue(property)` replaced by the matching local. `IBsonReader` is a heap object, so — unlike EF's `Utf8JsonReaderManager` — it is threaded directly (no ref-struct capture/recreate). Eligible queries stream `RawBsonDocument` rows and open a `BsonBinaryReader` over the raw bytes; ineligible queries use the unchanged DOM shaper.

**Tech Stack:** C#, EF Core 8/9/10 (build `Debug EF10`), MongoDB.Driver 3.9.0. Reference: EF source methods `JsonEntityMaterializerRewriter.Rewrite`, `GenerateJsonPropertyReadLoop`, `CreateReadJsonPropertyValueExpression`, `MaterializeJsonEntityCollection`.

---

## Conventions
- Build: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`. The `#if` branches (`EF8 || EF9` use `InjectEntityMaterializers`; `EF10` uses `InjectStructuralTypeMaterializers`) mean the injected-materializer tree differs by version — build/validate at least EF10 and EF8.
- A replica-set MongoDB runs at `mongodb://localhost:27017`; tests/benchmark use `MONGODB_URI=mongodb://localhost:27017`.
- Switch: `MONGODB_EF_NATIVE_QUERY` (`auto`/`force`/`off`) already gates the native path; streaming is a further gate within it.
- Preserve BOMs; `<Nullable>enable</Nullable>`.

---

## Task 1: Diagnostic — capture the injected-materializer block + row-type plumbing

The rewriter must transform the *real* tree EF emits. Capture it for `Customer`.

**Files:**
- Temporarily modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Read-only: same file's `ExecuteShapedQuery` + `Query/QueryingEnumerable.cs` (understand the row-type generics)
- Create: `docs/superpowers/specs/2026-06-18-injected-materializer-shape.md`

- [ ] **Step 1: Instrument `CompileShapedQuery`**

In `CompileShapedQuery`, immediately after the `InjectStructuralTypeMaterializers`/`InjectEntityMaterializers` line (i.e. on the post-injection `shaperBody`, before `createBindingRemover(...).Visit(...)`), add:
```csharp
System.Console.Error.WriteLine("MATERIALIZER-BLOCK >>>\n" + shaperBody.ToString() + "\n<<<");
```

- [ ] **Step 2: Run the Customer smoke to capture the tree**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10" -- --smoke 2>&1 | sed -n '/MATERIALIZER-BLOCK >>>/,/<<</p'
```

- [ ] **Step 3: Record findings**

Create `docs/superpowers/specs/2026-06-18-injected-materializer-shape.md` capturing, for the tracked `Customer` read: the materializer block (trimmed), and notes on: (a) the exact form of each `ValueBufferTryReadValue` call (the method, and how the property is referenced — `Expression.Constant(IProperty)`); (b) how the constructor is invoked and where the `MaterializationContext`/`ValueBuffer.Empty` appears; (c) how the owned `Address` appears (nested `StructuralTypeShaperExpression` / a nested block bound to a sub-`BsonDocument`, the element name used); (d) how the `Tags` array appears (a single `ValueBufferTryReadValue` with an array serializer, or a collection shaper); (e) the snapshot/second-read of properties (for tracking). Also document the row-type generics: what `TSource`/`TResult` are in `ExecuteShapedQuery<TSource,TResult>` and what `QueryingEnumerable<,>`/`Execute<>` use as the row type (so Task 3 can switch the eligible path to `RawBsonDocument`).

- [ ] **Step 4: Revert instrumentation; rebuild**
```bash
# remove the Console.Error.WriteLine line
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git diff --stat src/   # expect: empty
```

- [ ] **Step 5: Commit the findings doc**
```bash
git add docs/superpowers/specs/2026-06-18-injected-materializer-shape.md
git commit -m "Streaming materializer: capture injected-materializer block shape (diagnostic)"
```

---

## Task 2: Streaming-eligibility predicate

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/StreamingEligibility.cs`

- [ ] **Step 1: Implement the predicate**

`StreamingEligibility.cs` (an entity-type is eligible if its shape is covered by the C′ slice; recursive for owned reference sub-types):
```csharp
/* Copyright 2023-present MongoDB Inc.  (full Apache header like siblings) */

using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>Decides whether an entity type can be materialized by the forward-only streaming reader.</summary>
internal static class StreamingEligibility
{
    /// <summary>
    /// Eligible: a simple single-property primary key; properties are scalars or primitive arrays;
    /// navigations are only single (reference) owned sub-documents whose target types are themselves
    /// eligible. No owned collections, no cross-collection / non-owned navigations, no TPH
    /// discriminator hierarchy.
    /// </summary>
    public static bool IsEligible(IEntityType entityType)
        => IsEligible(entityType, new System.Collections.Generic.HashSet<IEntityType>());

    private static bool IsEligible(IEntityType entityType, System.Collections.Generic.HashSet<IEntityType> visiting)
    {
        if (!visiting.Add(entityType))
            return true; // already validating this type (avoid cycles)

        // No discriminator hierarchy (single concrete type only).
        if (entityType.GetDirectlyDerivedTypes().Any() || entityType.BaseType != null)
            return false;

        // Simple single-property primary key.
        var pk = entityType.FindPrimaryKey();
        if (pk == null || pk.Properties.Count != 1)
            return false;

        foreach (var navigation in entityType.GetNavigations())
        {
            // Only owned, single (reference), to an eligible owned type.
            if (navigation.IsCollection
                || !navigation.TargetEntityType.IsOwned()
                || !IsEligible(navigation.TargetEntityType, visiting))
            {
                return false;
            }
        }

        // Skip-navigations / non-owned relationships make it ineligible.
        if (entityType.GetSkipNavigations().Any())
            return false;

        return true;
    }
}
```

- [ ] **Step 2: Build**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
Expected: clean. (If `GetNavigations`/`IsOwned`/`GetSkipNavigations`/`GetDirectlyDerivedTypes` signatures differ across EF versions, adjust with `#if`; these are stable EF metadata APIs but verify on EF8.)

- [ ] **Step 3: Commit**
```bash
git add src/ && git commit -m "Streaming materializer: entity-type eligibility predicate"
```

---

## Task 3: RawBsonDocument transport + reader plumbing for the eligible path

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Storage/MongoClientWrapper.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/MongoExecutableQuery.cs`
- Create: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/BsonRowReader.cs`

The eligible streaming shaper reads from an `IBsonReader` over a row's raw bytes. The cursor must therefore yield `RawBsonDocument` for the streaming path (NOT accessed by name — opened as a forward reader). Gate this with a flag on the executable query set by the translator (Task 6).

- [ ] **Step 1: Add a streaming flag to `MongoExecutableQuery`**

Add an init property:
```csharp
    /// <summary>When true, native rows are RawBsonDocument and materialized by the forward-only streaming reader.</summary>
    public bool Streaming { get; init; }
```

- [ ] **Step 2: Reader-open helper**

`BsonRowReader.cs`:
```csharp
/* Copyright 2023-present MongoDB Inc.  (full Apache header) */

using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>Opens a forward-only <see cref="IBsonReader"/> over a RawBsonDocument's raw bytes (no DOM).</summary>
internal static class BsonRowReader
{
    public static BsonBinaryReader Open(RawBsonDocument row)
        => new(new ByteBufferStream(row.Slice, ownsBuffer: false));
}
```
(Verify `RawBsonDocument.Slice` and `ByteBufferStream(IByteBuffer, bool ownsBuffer)` signatures against the driver; if `Slice` isn't accessible, fall back to `new BsonBinaryReader(new MemoryStream(row.ToBson()))` and note the extra copy. The generated shaper from Task 4 will call `BsonRowReader.Open(row)`, `using` it.)

- [ ] **Step 3: Streaming branch in `MongoClientWrapper.Execute<T>`**

In the `NativePipeline` branch, when `executableQuery.Streaming` is true, fetch `RawBsonDocument` rows; otherwise keep `BsonDocument`:
```csharp
        if (executableQuery.NativePipeline is { } stages)
        {
            log = () => _commandLogger.ExecutedMqlQuery(executableQuery);
            if (executableQuery.Streaming)
            {
                var rawCollection = Database.GetCollection<RawBsonDocument>(executableQuery.CollectionNamespace.CollectionName);
                PipelineDefinition<RawBsonDocument, RawBsonDocument> rawPipeline = stages.ToArray();
                var rawCursor = executableQuery.Session is { } s
                    ? rawCollection.Aggregate(s, rawPipeline)
                    : rawCollection.Aggregate(rawPipeline);
                return (IEnumerable<T>)rawCursor.ToEnumerable();
            }

            var collection = Database.GetCollection<BsonDocument>(executableQuery.CollectionNamespace.CollectionName);
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = stages.ToArray();
            var cursor = executableQuery.Session is { } session
                ? collection.Aggregate(session, pipeline)
                : collection.Aggregate(pipeline);
            return (IEnumerable<T>)cursor.ToEnumerable();
        }
```
(`T` is `RawBsonDocument` at runtime for the streaming path; the generated shaper's row parameter type matches — see Task 6 plumbing.)

- [ ] **Step 4: Dispose rows after shaping in `QueryingEnumerable.MoveNextHelper`**

Re-add the C dispose-after-shape (it was reverted), since streaming rows (`RawBsonDocument`) are `IDisposable`. Replace the shaper line:
```csharp
                Current = _enumerator.Current is null ? default! : _shaper(_queryContext, _enumerator.Current);
```
with:
```csharp
                var row = _enumerator.Current;
                Current = row is null ? default! : _shaper(_queryContext, row);
                if (row is IDisposable disposableRow)
                {
                    disposableRow.Dispose();
                }
```
(No-op for `BsonDocument`; releases `RawBsonDocument` buffers. The generated streaming shaper disposes its own `BsonBinaryReader` internally via `using`.)

- [ ] **Step 5: Build + commit**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git add src/ && git commit -m "Streaming materializer: RawBsonDocument transport + row reader + dispose-after-shape"
```

---

## Task 4: Streaming rewriter — scalars only

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoStreamingEntityMaterializerRewriter.cs`

Build the rewriter against Task 1's captured tree, mirroring `JsonEntityMaterializerRewriter`. **Scalars only in this task** (defer arrays/owned to Task 5; if the materializer block contains an owned/array node, throw `NativeTranslationNotSupportedException` so the query falls back). Wiring into `CompileShapedQuery` happens in Task 6 — for now the class compiles and is unit-exercised via Task 6's smoke; build-only here.

- [ ] **Step 1: Implement the rewriter (scalars)**

The rewriter takes the post-injection materializer `BlockExpression`, the `IEntityType`, the `BsonSerializerFactory`, and the `RawBsonDocument` row parameter; it returns a block that:
1. opens an `IBsonReader` (`BsonRowReader.Open(row)`, `using`),
2. declares one typed local per scalar property (keyed by element name),
3. emits the forward loop: `reader.ReadStartDocument(); while (reader.ReadBsonType() != BsonType.EndOfDocument) { name = reader.ReadName(); if (name == elem1) local1 = ReadTyped(...); else if ... else reader.SkipValue(); } reader.ReadEndDocument();`,
4. runs the original materializer block with each `ValueBufferTryReadValue(property)` replaced by the property's local (reuse the existing replacement approach in `MongoProjectionBindingRemovingExpressionVisitor` — intercept `ExpressionExtensions.ValueBufferTryReadValueMethod`).

`MongoStreamingEntityMaterializerRewriter.cs` — provide the full class. Key pieces (the executor builds the exact expression tree against Task 1's findings; this is the required structure):
```csharp
/* Copyright 2023-present MongoDB Inc.  (full Apache header) */
// Mirrors dotnet/efcore JsonEntityMaterializerRewriter (relational JSON-column streaming materializer):
//   - one local per property; forward ReadName->dispatch loop fills locals; Skip unknown.
//   - rewrite ValueBufferTryReadValue(property) -> local; keep EF's construction/snapshot/tracking.
// IBsonReader is a heap object, so it is threaded directly (no Utf8JsonReaderManager ref-struct dance).
//
// SCOPE (this task): scalar properties only. Owned sub-documents / arrays -> throw
// NativeTranslationNotSupportedException (caller falls back to the DOM path).
```
Implementation notes for the executor (build against Task 1's tree; keep "throw on anything not handled" so ineligible shapes fall back):
- Element name per property: `property.GetElementName()`.
- Typed read per scalar: select the property's serializer (`BsonSerializerFactory.GetPropertySerializationInfo(property).Serializer` or the typed `IBsonSerializer<T>`), emit an unboxed `Deserialize` against a `BsonDeserializationContext.CreateRoot(reader)`. For a nullable property, branch on `reader.GetCurrentBsonType() == BsonType.Null` → `reader.ReadNull()` + default; else read.
- The `_id`/key property element name is `"_id"` (`GetElementName` returns it).
- Replace `ValueBufferTryReadValue` exactly as `MongoProjectionBindingRemovingExpressionVisitor.VisitMethodCall` does (find the `IProperty` from the call args), but return the local variable for that property instead of a `BsonBinding` read.
- If the block references an owned navigation or an array property (detected via the property/nav being a collection or owned reference), `throw new NativeTranslationNotSupportedException(...)` (handled in Task 5).

- [ ] **Step 2: Build**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
Expected: clean.

- [ ] **Step 3: Commit**
```bash
git add src/ && git commit -m "Streaming materializer: rewriter (scalars only)"
```

---

## Task 5: Extend rewriter to primitive arrays + owned reference sub-documents

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoStreamingEntityMaterializerRewriter.cs`

- [ ] **Step 1: Primitive array property**

For a primitive array property (e.g. `string[] Tags`): in the forward loop's dispatch for that element, `reader.ReadStartArray(); var list = new List<T>(); while (reader.ReadBsonType() != BsonType.EndOfDocument) { list.Add(readElementTyped(reader)); } reader.ReadEndArray();` then store `list.ToArray()` (or use the property's array serializer reading the array directly off the reader — preferred: `IBsonSerializer<T[]>.Deserialize(context)` reads the whole array forward in one call). Use the property's serializer (it already maps `string[]` via an array serializer) — emit `serializer.Deserialize(context)` positioned at the array; this reads it forward, no DOM.

- [ ] **Step 2: Owned reference sub-document (recursion)**

For a single owned reference navigation (e.g. `Address`): in the dispatch for its containing element name, `reader.ReadStartDocument();` recurse — a nested instance of the same forward-loop logic over the owned type's properties filling the owned type's locals, then construct the owned entity (the nested `StructuralTypeShaperExpression` block from EF, rewritten the same way) — then `reader.ReadEndDocument();`. The owned entity is wired into the parent via EF's existing fixup (the materializer block already contains the owned shaper / fixup; rewrite its `ValueBufferTryReadValue` calls to the owned locals, same as the root). Maintain strict bracket matching so the sub-read consumes exactly the sub-document.

(Mirror `JsonEntityMaterializerRewriter`'s nested handling — but with `IBsonReader` passed directly, no `CaptureState`/recreate. Build against Task 1's captured owned-`Address` sub-block.)

- [ ] **Step 3: Build (EF10 + EF8)**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```
Expected: both clean.

- [ ] **Step 4: Commit**
```bash
git add src/ && git commit -m "Streaming materializer: primitive arrays + owned reference sub-documents"
```

---

## Task 6: Wire eligibility gate + streaming shaper into compilation; validate end-to-end (force)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`

- [ ] **Step 1: Gate + route in `CompileShapedQuery` / `VisitShapedQuery`**

When `NativeQuery.Mode != Off`, the query is a whole-entity native read (not the projected path), and `StreamingEligibility.IsEligible(rootEntityType)`: build the streaming shaper instead of the DOM one. Concretely, in `CompileShapedQuery`, after `InjectStructuralType/EntityMaterializers`, branch:
```csharp
        var rowParameter = bsonDocParameter;
        var streaming = NativeQuery.Mode != NativeQueryMode.Off
            && StreamingEligibility.IsEligible(rootEntityType);
        if (streaming)
        {
            var rawRowParameter = Expression.Parameter(typeof(RawBsonDocument), "rawRow");
            try
            {
                shaperBody = new MongoStreamingEntityMaterializerRewriter(
                    rootEntityType, _bsonSerializerFactory, rawRowParameter).Rewrite((BlockExpression)shaperBody);
                rowParameter = rawRowParameter;
            }
            catch (NativeTranslationNotSupportedException) when (NativeQuery.Mode != NativeQueryMode.Force)
            {
                streaming = false;
                shaperBody = createBindingRemover(bsonDocParameter, trackingBehavior).Visit(/* original injected body */);
            }
        }
        else
        {
            shaperBody = createBindingRemover(bsonDocParameter, trackingBehavior).Visit(shaperBody);
        }
```
Then build the lambda with `rowParameter` (typed `RawBsonDocument` when streaming, else `BsonDocument`), set `Streaming = streaming` on the `MongoExecutableQuery` produced for this query (thread the flag through `ExecuteShapedQuery`/`TranslateQuery` to the executable query so `MongoClientWrapper.Execute` fetches `RawBsonDocument`), and use the right row type as `TSource`. (Task 1's plumbing notes tell you exactly which generic args / row type to change; the streaming path needs `TSource = RawBsonDocument`.)
NOTE: to "re-run binding removal on the original injected body" in the fallback catch, capture the post-injection `shaperBody` in a local BEFORE attempting the rewrite (the rewriter must not mutate it in place). Keep the existing non-streaming flow byte-for-byte for ineligible queries.

- [ ] **Step 2: Build**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```

- [ ] **Step 3: Force-mode smoke (Customer streams end-to-end)**
```bash
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=force dotnet run -c "Release EF10" -- --smoke
```
Expected: `SMOKE OK: where=50, proj=100, ordered=10, tracked=100, noTrack=100, withCity=100`. `tracked`/`noTrack`/`where`/`ordered` now materialize via the streaming reader (Customer is eligible); `withCity=100` confirms the owned `Address` recursion. If it throws `NativeTranslationNotSupportedException` in force mode, the rewriter didn't handle a real node — extend Task 4/5 to handle it (or, if out-of-slice, that's a fallback case — but in force mode for the eligible Customer it must stream). If a value is wrong (count off, missing Address), debug the rewriter against Task 1's tree; a temporary `Console.Error.WriteLine(shaperBody.ToString())` helps. Do NOT weaken the smoke.

- [ ] **Step 4: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add src/ && git commit -m "Streaming materializer: wire eligibility gate + streaming shaper with fallback"
```

---

## Task 7: Validate suite (no regressions) + benchmark + record

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-18-streaming-C-prime.md`

- [ ] **Step 1: Auto-mode regression check (key gate)**
```bash
cd /Users/arthur.vickers/code/provider2
MONGODB_URI=mongodb://localhost:27017 dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query" 2>&1 | tail -30
```
(Timeout 600000; run per-assembly if combined produces the known shared-DB pollution — pre-C′ baseline: UnitTests 8/0, FunctionalTests 544/0/44, SpecificationTests 4345/0/18.) Expected **0 failures**: eligible entities now stream through EF's tracking; ineligible fall back. Any failure on an eligible entity is a real streaming-materializer bug (wrong value, wrong tracking, mis-bracketed sub-read) — fix the rewriter (keep `RawBsonDocument`/streaming; do not silently revert). Re-run until 0.

- [ ] **Step 2: Benchmark vs baseline**
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10"
```
(Timeout 600000.) Capture the table. The eligible `Customer` shapes (`Where_ToList`, `OrderBy_Take`, `Tracked_ToList`, `NoTracking_ToList`, and the now-streamed projection materialization) should show reduced Mean/Allocated vs baseline.

- [ ] **Step 3: Record results + recommendation**

Read `results/2026-06-16-baseline.md`, `2026-06-17-native-B.md`, `2026-06-17-dom-free-C.md`. Create `results/2026-06-18-streaming-C-prime.md`:
```markdown
# Streaming materializer (sub-project C') — results

**Run on:** <date/CPU/OS/SDK from BenchmarkDotNet header>
**Change:** native-path eligible entities materialized by a forward-only IBsonReader streaming rewriter (typed locals, no DOM, no boxing); EF construction/tracking reused.

## Regression check (auto mode)
<counts> — <matches pre-C' (0 failures)?>

## Benchmark: C' vs A baseline (and B, C)
| Shape | A Mean | C' Mean | Δ Mean | A Alloc | C' Alloc | Δ Alloc % |
|---|---:|---:|---:|---:|---:|---:|
<rows with actual numbers + computed deltas>

## Reading & recommendation
- <Per-shape allocation/time delta. Compare to the spike's ~65% (pure materialization) and the
  ~15-30% EF-overhead-capped expectation for tracked reads.>
- <Recommendation: extend eligibility (owned collections, includes, projections, TPH)? make streaming
  the default and retire the DOM path for covered shapes? Is the win worth the rewriter's complexity?>
```
Fill every `<...>` from the actual runs.

- [ ] **Step 4: Commit**
```bash
git add benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-18-streaming-C-prime.md
git commit -m "Streaming materializer: record C' results vs baseline"
```

---

## Notes for the executor
- **Task 1 governs Tasks 4–6.** The rewriter must be built against the *actual* injected-materializer tree (EF-version-specific). Mirror `JsonEntityMaterializerRewriter`; keep "throw `NativeTranslationNotSupportedException` on any node not handled" so `auto` mode always falls back safely.
- **`force` mode is the development driver** (as in B): run the smoke / a focused functional test in `force` and handle real node shapes one message at a time.
- **Reuse EF's construction/tracking** — only the value-source (DOM-by-name → forward-loop-into-locals) changes. Do not reimplement entity construction, snapshot, identity, or fixup.
- **No ref-struct manager** — `IBsonReader` is threaded directly; just keep strict `ReadStartDocument`/`EndDocument`, `ReadStartArray`/`EndArray` bracketing so each (sub-)read consumes exactly its (sub-)document.
- **Never weaken a test.** An eligible-entity failure in `auto` is a real bug; fix the rewriter.
- Leave the `ef-bench-mongo` container running.
