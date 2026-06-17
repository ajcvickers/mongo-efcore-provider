# Native MQL generation (Sub-project B) — design

**Date:** 2026-06-17
**Status:** Approved; pending implementation plan
**Branch:** `spike/low-level-provider` (off `main`)
**Program:** sub-project B of the low-level-provider migration (see
`2026-06-16-low-level-provider-migration-design.md`). Follows A (benchmark harness + baseline);
precedes C (reader-based materialization).

## Goal

Translate the provider's captured EF query expression directly into a MongoDB aggregation pipeline
(`BsonDocument[]`) and execute it via `IMongoCollection.Aggregate`, bypassing the driver's LINQ
provider — for a minimal single-collection slice (filter / sort / paging), behind a per-query
fallback to the existing driver-LINQ path. The existing DOM-based shaper is reused unchanged;
reader-based materialization is sub-project C.

## Key finding that shapes this work

The `MongoQueryExpression` IR is **shallow**: `Where`/`OrderBy`/`Skip`/`Take` are not decomposed
into structured fields. `MongoQueryableMethodTranslatingExpressionVisitor` returns `null` for them,
and they survive as raw LINQ `MethodCallExpression`/`LambdaExpression` nodes in `CapturedExpression`.
The existing `MongoEFToLinqTranslatingExpressionVisitor` walks that captured chain and rewrites it
into driver-LINQ calls (`Mql.Field`, etc.) for the driver's LINQ-v3 provider to compile into stages.

Therefore "native MQL generation" means **building a (minimal) expression-tree → aggregation-pipeline
translator** that walks the same captured chain and emits `BsonDocument` stages directly. It parallels
a subset of the existing visitor but emits BSON instead of `Mql.Field` calls.

## Seam (confirmed by code reading)

- Branch point: `MongoShapedQueryCompilingExpressionVisitor.TranslateQuery<TSource>` — today it builds
  a `MongoEFToLinqTranslatingExpressionVisitor`, calls `translate(...)`, and wraps the result in a
  `MongoExecutableQuery(Query, Cardinality, Provider, CollectionNamespace, AdditionalState)`.
- Execution: `MongoClientWrapper.Execute<T>` calls `executableQuery.Provider.CreateQuery<T>(Query)`
  for enumerable queries; `ExecuteScalar<T>` calls `Provider.Execute<T>(Query)` for scalar.
- Shaper: the compiled shaper is `Func<QueryContext, BsonDocument, TResult>`
  (`MongoProjectionBindingRemovingExpressionVisitor` reads fields from a `BsonDocument` per row).
  A native pipeline returning `IAsyncCursor<BsonDocument>` feeds it **unchanged**.

## Scope (minimal slice)

Native translation handles, over a single collection, a captured chain composed of:
- `Where(pred)` → `$match`.
- `OrderBy` / `OrderByDescending` / `ThenBy` / `ThenByDescending` → ordered `$sort`.
- `Skip(n)` / `Take(n)` → `$skip` / `$limit`.
- A pure trailing `Select` (projection) → **ignored** in the pipeline; full documents are returned
  and the existing shaper projects + materializes client-side.
- Empty chain (plain `ToList`) → empty pipeline (returns all documents).

### Predicate translation (`Where` body → `$match`)
- Binary comparisons `==`,`!=`,`<`,`<=`,`>`,`>=` → `{field:value}` (equality) or
  `{field:{$ne|$lt|$lte|$gt|$gte:value}}`.
- `AndAlso` → `$and`; `OrElse` → `$or`; `Not` / negated boolean → negation.
- Bare boolean property `c.Active` → `{Active:true}`; `!c.Active` → `{Active:false}`.
- Property → BSON element name via `IReadOnlyProperty.GetElementName()`.
- Comparison constant → `BsonValue` via the property's serializer (correct BSON representation/type).
- **EF query parameters** (the common case — EF parameterizes literals) resolved from
  `QueryContext.ParameterValues` at execution time.

### Sort / paging
- Sort keys are entity properties → element names; ascending `1`, descending `-1`; `ThenBy` chains
  append in order (`$sort` document key order is significant).
- `Skip`/`Take` counts may be EF parameters → resolved from `QueryContext`.

## Fallback (anything outside the slice)

The translator throws an internal `NativeTranslationNotSupportedException` on: joins / pending
`$lookup`s, grouping, set ops, scalar cardinality, predicates/sorts over post-`Select` projected
values, unresolvable member/constant nodes, or an operator order that doesn't map cleanly. The
branch in `TranslateQuery` catches it and falls back to the existing driver-LINQ translation.

Native translation is attempted **only** for enumerable cardinality with no pending lookups;
scalar (`Count`/`First`/`Single`/`Any`) and join queries always use the driver path in B.

## The switch

Environment variable `MONGODB_EF_NATIVE_QUERY`, read once into a static `NativeQueryMode`:
- `auto` (default) — try native; on `NativeTranslationNotSupportedException`, silently fall back.
- `force` — native or throw (the exception propagates), so running the existing test suite surfaces
  exactly which queries the native path cannot yet handle.
- `off` — always use the driver-LINQ path.

Env var (not a `DbContextOptions` flag) to avoid touching public API surface for a spike.

## Components

New, under `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/`:
- `MongoPipelineTranslator` — walks the captured chain, returns `List<BsonDocument>` stages or throws.
- `MongoPredicateTranslator` — `Where` lambda body → `$match` `BsonDocument`.
- `NativeTranslationNotSupportedException` — internal sentinel.
- `NativeQueryMode` (enum) + a static reader of the env var.

Edits:
- `Query/MongoExecutableQuery.cs` — add `IReadOnlyList<BsonDocument>? NativePipeline` (null ⇒ driver path).
- `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` — `TranslateQuery` branch (try native, fall back; honor the mode).
- `Storage/MongoClientWrapper.cs` — `Execute<T>`: when `NativePipeline` is set, run
  `collection.Aggregate<BsonDocument>(session, pipeline)`; else existing path. Log the pipeline.

## Execution & logging

- Enumerable native: `IMongoCollection<BsonDocument>.Aggregate(session, pipeline)` → `IAsyncCursor<BsonDocument>`
  → existing shaper. Honor the current transaction's session as the existing path does.
- `ExecuteScalar` is untouched (scalar always falls back).
- The command logger logs the actual pipeline `BsonDocument[]` (real MQL in logs).

## Validation

1. **No regressions:** existing FunctionalTests + SpecificationTests pass in `auto` mode (fallback
   covers everything not yet native).
2. **Native coverage:** run the suite in `force` mode; iterate B until the targeted slice shapes pass
   natively, and record which shapes still fall back (expected: joins/grouping/scalar/projection-heavy).
3. **Perf:** re-run the A benchmark with native on (`auto`), compare to the committed baseline. B keeps
   the DOM shaper, so the large materialization win is C's; B's expected gains are removing driver-LINQ
   translation overhead and (for filtered/paged shapes) correct server-side `$match`/`$sort`/paging.

## Implementation risk retired first

The exact shape of `CapturedExpression` — how property access appears (`EF.Property(...)` calls vs
`MemberExpression`), how constants vs EF parameters appear — is the main unknown. The plan's first
task is a diagnostic that dumps the real captured tree for the benchmark query shapes, so the
predicate/sort translators are built against reality.

## Out of scope for B (later sub-projects)

- `$project` pushdown (server-side projection).
- Scalar cardinality (`Count`/`First`/`Single`/`Any`).
- Joins / `Include` / `$lookup`, grouping, set/aggregate operators.
- Reader-based (DOM-free) materialization — sub-project C.
