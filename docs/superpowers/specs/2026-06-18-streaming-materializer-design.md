# Streaming (DOM-free) materializer (Sub-project C′) — design

**Date:** 2026-06-18
**Status:** Approved; pending implementation plan
**Branch:** `spike/low-level-provider` (off `main`)
**Program:** sub-project C′ of the low-level-provider migration. Follows C (the `RawBsonDocument`
transport-swap experiment, which regressed and was reverted — see
`results/2026-06-17-dom-free-C.md`).

## Goal

Replace the `BsonDocument`-DOM materialization on the native query path with a forward-only
`IBsonReader` streaming materializer that reads each row once into typed locals and constructs the
entity from them — eliminating both the DOM (dict + `BsonValue` tree) and the per-field boxing the
current path incurs (`DeserializeValue` → `object` → cast). Measured against the A baseline on the
existing benchmark shapes.

## Reference implementation (the blueprint)

EF Core already contains a streaming-reader materializer that solved exactly this problem when it
moved JSON-column materialization from a DOM to `System.Text.Json`'s `Utf8JsonReader`:
`src/EFCore.Relational/Query/RelationalShapedQueryCompilingExpressionVisitor.ShaperProcessingExpressionVisitor.cs`
(`JsonEntityMaterializerRewriter`, `GenerateJsonPropertyReadLoop`, `CreateReadJsonPropertyValueExpression`)
and `…ClientMethods.cs` (`MaterializeJsonEntityCollection`), over `src/EFCore/Storage/Json/`
(`Utf8JsonReaderManager`, `JsonReaderData`, `JsonValueReaderWriter<T>`). (The Cosmos streaming
materializer rewrite tracks the same pattern — dotnet/efcore #30604.) C′ is the BSON analog.

### Patterns adopted from it

- **Typed locals + forward name-dispatch loop + construct-after.** Declare one local per
  property/navigation. Run a single forward loop: `ReadBsonType`/`ReadName` → an `if/else` chain
  dispatches by element name → read the typed value into the matching local; unknown elements →
  `SkipValue()`. *After* the loop, run EF's normal construction/assignment block. EF still reads each
  property in its own order, and again for the change-tracking snapshot — but those reads resolve to
  the **already-filled local**, not the reader. This reconciles a forward-only reader with EF's
  out-of-order/repeated reads with **no random access, no offset index, no `ValueBuffer`, no boxing**.
- **Transform EF's existing materializer block; reuse everything above the value-read.** Like
  `JsonEntityMaterializerRewriter.Rewrite`, C′ rewrites the block EF produces, replacing each
  `Infrastructure.ExpressionExtensions.ValueBufferTryReadValue(property)` call with the matching
  local (the **same interception point** the current `MongoProjectionBindingRemovingExpressionVisitor`
  already uses). `StructuralTypeShaperExpression`, `MaterializationContext` (with `ValueBuffer.Empty`),
  constructor binding, shadow-state snapshot, identity resolution, change tracking, and include fixup
  remain EF's, untouched.
- **Typed reads:** the JSON path selects `property.GetJsonValueReaderWriter() ?? typeMapping.JsonValueReaderWriter`
  and calls `FromJsonTyped(ref manager)` (unboxed). C′'s analog: select the property's typed
  `IBsonSerializer<T>` from `BsonSerializerFactory` and invoke it on the reader, unboxed. Nullable
  properties wrap the read in a `BsonType.Null` check (consume the null, yield default/null).
- **Nested owned sub-document & primitive array:** recursive forward sub-reads with strict bracket
  matching — `ReadStartDocument`/`ReadEndDocument` around an owned sub-document, `ReadStartArray`/
  `ReadEndArray` around a primitive array (element loop). Array element ordinals come from the loop
  counter, not the document.
- **Out-of-order / missing / extra:** name dispatch is order-independent; a missing element leaves
  the local at its CLR default; an unmapped element is `SkipValue()`-d.

### Dropped (a BSON simplification)

EF's `Utf8JsonReaderManager` + `JsonReaderData` two-type machinery exists **only** because
`Utf8JsonReader` is a `ref struct` that can't cross delegate/iterator boundaries or be stored on the
heap, requiring a capture-state/recreate-from-buffer dance. **`IBsonReader` is an ordinary heap
interface** — it can be held in a local, captured in a closure, and passed straight into the shaper
delegate; the driver buffers and bookmarks internally. So C′ needs **no `BsonReaderManager`, no
capture/recreate, no buffer-refill loop** — just thread the `IBsonReader`. The discipline we keep is
strict bracket assertions so each (sub-)materializer consumes exactly its (sub-)document.

## Architecture / data flow

- **Eligibility gate (at translation):** the streaming materializer is used only when the query is
  on the native path (`NativePipeline`), is a whole-entity read (no projection), and the root entity
  is **streaming-eligible** (see Scope). Otherwise the existing DOM path is used unchanged
  (fallback). Reuses the `MONGODB_EF_NATIVE_QUERY` gating already in place.
- **Transport:** the streaming branch returns `RawBsonDocument` rows (byte-backed; *not* accessed by
  name — that was C's mistake). The generated shaper opens a forward `BsonBinaryReader` over the
  row's raw bytes (`RawBsonDocument.Slice` via a `ByteBufferStream`), reads once, materializes, and
  disposes the reader and the row (extending C's dispose-after-shape; values are already copied into
  the entity).
- **Materializer:** a new `MongoStreamingEntityMaterializerRewriter` (BSON analog of
  `JsonEntityMaterializerRewriter`) produces the locals + forward-loop block and rewrites
  `ValueBufferTryReadValue`→locals; EF's construction/tracking wraps it.

## Components / files

- New: `Query/NativeTranslation/MongoStreamingEntityMaterializerRewriter.cs` — the block rewriter
  (forward loop, locals, name dispatch, recursion, bracket asserts).
- New: a small typed-read helper (select `IBsonSerializer<T>` per property; emit the unboxed read +
  nullable null-check) and the element-name→local dispatch builder.
- Edit: `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` — eligibility gate; route
  eligible entity queries to the streaming rewriter + `RawBsonDocument` shaper, else the existing
  path.
- Edit: `Storage/MongoClientWrapper.cs` — streaming branch returns `RawBsonDocument` rows.
- Edit: `Query/QueryingEnumerable.cs` — already disposes `IDisposable` rows after shaping (from C,
  reverted; re-add scoped to this path) and the reader.
- `benchmarks/.../results/2026-06-18-streaming-C-prime.md` — results.

## Scope (first measurable slice)

**Streaming-eligible** (everything else falls back to the DOM path, unchanged):
- Native path, whole-entity read (no projection / anonymous type).
- Root entity with a **simple single-property key** (e.g. `ObjectId Id` at top-level `_id`).
- Properties limited to: scalars, primitive arrays (e.g. `string[]`), and **single owned reference
  sub-documents** (recursively, same constraints — e.g. `Address`).
- **No** owned collections, cross-collection includes/navigations, TPH/discriminator hierarchies,
  composite or owned-key-under-`_id` shapes. Those → fallback.

This covers the existing benchmark `Customer` (`ObjectId Id`, scalars, `Tags` array, owned `Address`)
so the headline `Tracked_ToList` (147 MB) and `NoTracking_ToList` (126 MB) shapes are measured
directly.

## Validation

1. **No regressions:** existing query suite in `auto` mode stays at **0 failures** (4897 pre-C′).
   Eligible entity reads now stream; everything else falls back. This exercises the streaming
   materializer against real owned entities, arrays, tracking, and no-tracking.
2. **Native correctness in `force` mode:** benchmark `--smoke` prints the expected `SMOKE OK: ...`
   (including `withCity=100` — owned `Address` via the recursive sub-read).
3. **Perf:** re-run the A benchmark; compare each eligible shape's Mean/Allocated to
   `results/2026-06-16-baseline.md` and B/C. Record the delta and whether to extend eligibility
   (owned collections, includes, projections) and/or make streaming the default.

## Honest expectation

EF's change-tracking/identity/snapshot allocations are untouched, so the end-to-end win is capped
(~15–30% on tracked reads, more on no-tracking) even though the materializer itself becomes
allocation-light. The measurement decides whether the win justifies extending the streaming
materializer's coverage.

## Out of scope (later)

- Owned collections, cross-collection includes, projections/anonymous types, TPH discriminator
  hierarchies, composite/owned keys — all fall back for now.
- Making streaming the default / removing the DOM path (depends on measured win + coverage).
- The `Utf8JsonReaderManager`/`JsonReaderData` ref-struct layer — not applicable to `IBsonReader`.
