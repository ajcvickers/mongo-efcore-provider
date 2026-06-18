# Native reference Include (Sub-project: Includes, slice 1) — design

**Date:** 2026-06-18
**Status:** Approved; pending implementation plan
**Branch:** `spike/low-level-provider` (off `main`)
**Program:** extends the low-level-provider migration to cross-collection `Include`. First slice:
single-level **reference** Include. Follows E (native query is the default behind `UseNativeQuery`).

## Goal

Translate and materialize a single-level **reference** `Include` (e.g. `order.Include(o => o.Customer)`,
where `Customer` lives in a separate collection) entirely on the native path — emitting the
`$lookup` + `$unwind` aggregation stages and materializing the joined entity with the streaming
reader — instead of falling back to the driver-LINQ + DOM path. Keep the throw→fallback contract.

## Background (existing machinery — reused)

- **`LookupExpression`** (`Query/Expressions/LookupExpression.cs`) is a structured IR per needed
  `$lookup`: `Navigation`, `From` (target collection), `LocalField`, `ForeignField`, `As`
  (`"_lookup_" + nav.Name`), `IsReference`, `ShouldUnwind`, optional `PipelineStages` (filtered).
  Added via `AddLookup`, read via `GetPendingLookups()` (dependency-ordered). For a reference the FK
  is on the dependent: `LocalField` = FK, `ForeignField` = principal PK, `ShouldUnwind` = true.
- **The driver-LINQ path** already builds the exact raw `$lookup` BSON (`{from, localField,
  foreignField, as}`) + `$unwind` (`{path: "$"+as, preserveNullAndEmptyArrays: true}`) in
  `MongoEFToLinqTranslatingExpressionVisitor.LeftJoin.cs` (`EmitLookupStages`).
- **The native path rejects lookups** at two gates in `MongoShapedQueryCompilingExpressionVisitor`
  (`GetPendingLookups().Count == 0`, ~lines 195 and 316) and the streaming rewriter throws for any
  non-owned navigation.
- The joined entity, after `$lookup`(+`$unwind` for a reference), is in the `_lookup_<Nav>` field — a
  single sub-document (reference) or BSON `Null` (no match). The streaming materializer's owned-
  reference path (C′) already reads a single sub-document with a `present` flag.

## 1. Native pipeline: emit the lookup stages

Replace the `GetPendingLookups().Count == 0` fallback gates with lookup-stage emission: for each
**reference** pending lookup on the query, append to the native pipeline (after the base
`$match`/`$sort`/`$skip`/`$limit`):
- `{ "$lookup": { "from": From, "localField": LocalField, "foreignField": ForeignField, "as": As } }`
- `{ "$unwind": { "path": "$" + As, "preserveNullAndEmptyArrays": true } }`

Reuse the BSON the driver path already constructs from `LookupExpression` (extract a shared builder
if clean, else replicate the ~6-line simple form). If any pending lookup is a **collection** lookup,
a **filtered** lookup (has `PipelineStages` / the `{from,let,pipeline,as}` form), or there is a
transitive/nested lookup (a lookup whose `LocalField` references another lookup's `As`), **fall back**
(deferred). Single-level reference lookups only (one or more independent ones are fine).

## 2. Eligibility

`StreamingEligibility` currently rejects non-owned navigations. Change: allow a **non-owned
reference** navigation when its target type is recursively streaming-eligible (so the root entity can
stream, and if the navigation is included it materializes via the lookup). Non-owned **collection**
navigations, and reference navigations to non-eligible targets, remain ineligible → fall back via the
rewriter throw.

## 3. Streaming materializer: materialize the joined entity

Extend `MongoStreamingEntityMaterializerRewriter` to handle an `IncludeExpression` whose navigation
is a non-owned **reference**, modeled on the owned-reference path with two differences:
- **Source field** is the lookup alias `_lookup_<Nav>` (= `LookupExpression.GetLookupAlias(navigation)`),
  read post-`$unwind` as a single sub-document; BSON `Null` ⇒ `present = false` (left-outer, no match).
  (Owned references read an embedded element by the owned containing-element name; here it's the
  lookup alias.)
- The joined entity is **non-owned**: EF's navigation-arm construction block reads the joined entity's
  **own primary key from the joined sub-document** and performs its own `TryGetEntry`/`StartTracking`.
  The rewriter rewrites its `ValueBufferTryReadValue`→locals as usual, with **no owner-key resolution**
  (the joined entity's key is a real field in the joined doc — simpler than owned). The included entity
  is tracked as its own entry.
- **Fixup:** reuse `SpliceReferenceInclude` (EF's `IncludeReference`). The diagnostic (plan task 1)
  confirms whether the non-owned reference fixup matches the owned-reference splice or needs a small
  variant.

A non-owned **collection** navigation in the `IncludeExpression` tree, or a filtered/nested include,
throws `NativeTranslationNotSupportedException` → fallback.

## Validation + measurement

- **No regressions, default:** the full Query suite at **0 failures** in `auto`. The Northwind
  Specification suite exercises reference Includes heavily (currently via fallback) — they must now
  pass **natively**. Any failure is a real bug in the lookup emission / joined-entity materialization /
  fixup — fix it (or tighten eligibility to fall back for the specific shape).
- **No regressions, opt-out:** the suite (or a subset) passes with `UseNativeQuery(false)` and with
  `MONGODB_EF_NATIVE_QUERY=off` (driver-LINQ path unchanged).
- **Benchmark:** add a benchmark entity with a reference Include (e.g. `Order` with a `Customer`
  reference in a separate collection) + a `ToList` shape that `Include`s it; measure streaming
  (default) vs `MONGODB_EF_NATIVE_QUERY=off`.
- **Diagnostic first:** capture the real cross-collection reference-`Include` materializer tree (the
  `_lookup_<Nav>` source access and the navigation-arm construction block) so the rewriter is built
  against reality.
- Builds EF8/EF9/EF10.

## Components / files

- `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` (and/or `MongoPipelineTranslator` /
  the native branch) — emit reference-lookup `$lookup`/`$unwind` stages; gate to single-level
  reference lookups (else fall back).
- `Query/NativeTranslation/StreamingEligibility.cs` — allow non-owned reference navigations with
  eligible targets.
- `Query/NativeTranslation/MongoStreamingEntityMaterializerRewriter.cs` — materialize a lookup-backed
  non-owned reference include (source = `_lookup_<Nav>`; no owner-key resolution; reference fixup).
- `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/` — reference-Include entity + shape + results.
- A diagnostic findings doc for the cross-collection `IncludeExpression` tree.

## Out of scope (fall back; later slices)

- Collection Includes (`$lookup` into an array — D's array machinery + relationship fixup).
- Filtered Includes (`{from, let, pipeline, as}` form with `$match`/`$sort`/`$skip`/`$take`).
- `ThenInclude` / nested / transitive lookups (dependency-ordered chains).
- The `ForceUnwind` / explicit-`Join` cardinality-multiplying case.
