# Streaming owned collections (Sub-project D) — design

**Date:** 2026-06-18
**Status:** Approved; pending implementation plan
**Branch:** `spike/low-level-provider` (off `main`)
**Program:** sub-project D of the low-level-provider migration. Extends C′ (the forward-only
streaming materializer), which covers simple-key entities with scalars + single owned *reference*
sub-documents.

## Goal

Extend the streaming materializer to **owned collections** — an owned navigation stored as a BSON
array of owned sub-documents (`List<OwnedThing>`) — so entities with owned collections materialize
via the forward `IBsonReader` pass instead of falling back to the `BsonDocument` DOM. Keep the
throw→fallback contract; reuse EF's per-element construction/tracking.

## Background (how owned collections materialize today)

In the DOM path (`MongoProjectionBindingRemovingExpressionVisitor`, `CollectionShaperExpression`
case): the parent's `BsonArray` is fetched by element name (`BsonBinding.CreateGetBsonArray(parentDoc,
name)`), iterated with `EnumerableMethods.SelectWithOrdinal` (0-based ordinal), each element is a
`BsonDocument` materialized by a nested `StructuralTypeShaperExpression`, and the results feed
`PopulateCollection<TEntity,TCollection>(IClrCollectionAccessor, IEnumerable<TEntity>)`. Owned
collection elements carry a synthetic `IsOwnedTypeOrdinalKey` property whose value is loop-index + 1
(supplied via `_ordinalMappings`). The C′ streaming rewriter currently rejects collection
navigations at `BuildPlan` and `StreamingEligibility` (`navigation.IsCollection`). EF's relational
JSON materializer does the streaming-array analog in `MaterializeJsonEntityCollection`
(StartArray → per-element materialize → add → EndArray, ordinal from a counter).

## Mechanism

Extend `MongoStreamingEntityMaterializerRewriter` so a collection navigation routes (instead of
throwing) to an array-loop, mirroring the single-owned-reference path but as a loop:

- In the parent fill loop, the collection's element name dispatches to:
  `reader.ReadStartArray()`; `int counter = 0`; `while (reader.ReadBsonType() != BsonType.EndOfDocument)`
  (end-of-array sentinel) — per element: `reader.ReadStartDocument()`, fill the element's locals (the
  existing sub-fill-loop over the element type), `reader.ReadEndDocument()`, run EF's per-element
  construction (rewritten: `ValueBufferTryReadValue`→element locals, `MaterializationContext`→
  `ValueBuffer.Empty`), add the constructed element to an accumulating `List<TElement>`, `counter++`;
  then `reader.ReadEndArray()`.
- **Per-iteration construction.** Unlike a reference (fill-once, construct-once), a collection
  re-fills the element locals and runs the element construction **each iteration**, accumulating into
  the list.
- **Synthetic ordinal key.** The element's `IsOwnedTypeOrdinalKey` property read resolves to
  `counter + 1` — the same property→local substitution mechanism C′ uses for the owner-principal key,
  but pointing at the loop counter.
- **Owner FK.** The element's shadow FK to the owner resolves to the owner's key local (C′'s
  owned-key resolution, reused).
- **Populate.** Assign the materialized `List<TElement>` to the parent navigation via the
  navigation's `IClrCollectionAccessor` (the `PopulateCollection` helper), wired through EF's
  collection-Include fixup — the rewriter keeps EF's `IncludeExpression` structure, as it already
  does for references.

## Recursion

Full recursion: a collection element type may itself contain owned references and/or owned
collections, handled by the same logic recursively. `StreamingEligibility` allows an owned
collection navigation when its element type is recursively eligible. The rewriter and eligibility
stay aligned: if a nested shape proves too intricate to generate correctly, the rewriter throws
`NativeTranslationNotSupportedException` and eligibility is tightened to match — falling back is
always safe (never a wrong result).

## Eligibility change

`StreamingEligibility.IsEligible`: replace the blanket `navigation.IsCollection` rejection with —
allow a collection navigation when `navigation.TargetEntityType.IsOwned()` **and** the element type
is recursively `IsEligible`. Single owned references remain allowed as today. Non-owned / skip
navigations and TPH hierarchies still make a type ineligible.

## Validation + measurement

- **Regression gate:** the full Query suite in `auto` mode stays at **0 failures** (4897 pre-D).
  Entities with owned collections across the Specification/Functional suites now stream; ineligible
  shapes fall back.
- **Benchmark:** the existing benchmark entities have no owned collection. Add a benchmark entity
  `Basket` with an owned `List<BasketItem>` (scalar items) + `NoTracking_ToList` / `Tracked_ToList`
  shapes. Measure **streaming (default `auto`) vs DOM (`MONGODB_EF_NATIVE_QUERY=off`)** to isolate the
  owned-collection materialization win (a fresh entity has no A-baseline, so on/off is the control).
- **Smoke:** extend `--smoke` with a `Basket`-with-items check (e.g. count + an item field) that
  passes in `force` mode (Basket streams end-to-end, items materialized via the array loop).

## Components / files

- `Query/NativeTranslation/MongoStreamingEntityMaterializerRewriter.cs` — route collection
  navigations to the array-loop materialization (counter, per-element construct, ordinal-key
  substitution, populate); recurse.
- `Query/NativeTranslation/StreamingEligibility.cs` — allow recursively-eligible owned collections.
- `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/` — `Basket`/`BasketItem` entity (`OwnsMany`),
  `DbSet`, seeding, smoke check, and benchmark shapes.
- `benchmarks/.../results/2026-06-18-streaming-owned-collections-D.md` — results.

## Out of scope (later)

- Cross-collection Includes / `$lookup` (a query-translation axis, not just materialization).
- TPH / discriminator hierarchies.
- The C′ productionization follow-ups (broaden fallback catch; deterministic row disposal) — tracked
  in the C′ results doc.
