# Complex type support — design

Branch `EF-168a` (from `EF-322c`). Status: draft for owner review.

## Goal

Complex types work end to end in the MongoDB EF Core provider: model building, insert/update/delete, change
tracking, and querying. A complex property maps to an embedded subdocument under its element name; a complex
collection maps to an array of subdocuments. Every new query shape runs on the **native** pipeline.

In scope, per EF version (the exact matrix is confirmed by the Phase 0 probe, see Risks):

- Complex properties of class and struct type, nested to any depth, with `HasElementName` / `[BsonElement]` /
  `[BsonIgnore]` / `[BsonRequired]` / `[Column]` honored.
- Optional (nullable) complex properties and complex collections, where the EF version supports them (EF10).
- Reading (whole-type and leaf projection, predicates, ordering, grouping, aggregates, set operations,
  `Include`-adjacent shapes where the complex type sits on an entity), and writing.
- `ExecuteUpdate` / `ExecuteDelete` involving complex properties, **on the existing driver-LINQ bridge**
  (owner decision: bulk operations stay on the bridge; retiring it is separate work).

Measured per-version matrix (Phase 0 probe, `ComplexTypeFeatureMatrixTests`, provider convention set; Debug EF8/EF9/EF10):

| Feature | EF8 | EF9 | EF10 |
|---|---|---|---|
| Class complex property | builds | builds | builds |
| Struct complex property | builds | builds | builds |
| Nested complex property | builds | builds | builds |
| Optional (nullable reference) complex property | rejected (`InvalidOperationException`: optional not supported, call `IsRequired()`) | rejected (same) | builds |
| Complex collection (`ComplexCollection`) | API absent (does not compile) | API absent (does not compile) | builds |

Out of scope: complex types as keys or foreign-key targets (EF does not allow them), JSON-column mapping
concepts that do not exist for MongoDB, and any change to owned-type behavior.

**Owner-approved exceptions to "no owned-type behavior change"** (ruling R6, Task 9):

- (a) A bare projection of a dotted stored field (`Select(b => b.Home.City)`, multi-hop owned, composite-key component)
  now goes native under the driver's own bare alias `_v`, replacing the EF-362 decline: the decline existed only because
  a dotted alias can't round-trip, which `_v` avoids, and the shared read path is what complex leaves need.
- (b) A projected `Distinct` over a required reference-typed scalar (`string`, `byte[]`, primitive collection) whose
  element is MISSING reads `null`, as main did, instead of throwing: the same `$group` missing-marker fix that optional complex parents need. It changes the MQL baselines of
  `NorthwindSetOperationsQueryMongoTest.Union_on_distinct`, `Intersect_on_distinct` and `Except_on_distinct`
  (`CompanyName__isMissing`).

## Constraints

- No new driver-LINQ query paths. `MongoQueryMode.NativeOnly` is the oracle for every query test.
- Clear separation of concerns (see Architecture); no duplicated element-name or nullability logic.
- No git worktrees; commit on `EF-168a`. Commit subjects start `EF-168a: `.
- Tests are derived from this design and the code, not ported from upstream suites. The owner has asked that
  specific upstream complex-type test classes not be read or ported; tests are written independently and
  compared against upstream after the implementation.
- Multi-version: the code must build and pass on EF8, EF9, EF10 with `#if EF8 || EF9` / `#if !EF8` gating.

## Current state (verified in `src/`)

| Layer | State |
|---|---|
| Metadata conventions | `BsonIgnore` and `BsonRequired` conventions already handle complex properties. `GetContainingElementName` exists only for `IEntityType`. |
| Element-name lookup | Duplicated read of `Mongo:ElementName` ?? CLR name: `MongoQueryableMethodTranslatingExpressionVisitor.GetComplexPropertyElementName` and `MongoSelectLowerer.GetComplexPropertyElementName`. |
| Write path | `MongoUpdate.WriteEntity` writes keys, scalar properties and owned navigations only. Complex properties are never written. Measured (EF8, EF9, EF10): `SaveChanges` of a `CustomerWithAddress` stores only `{ _id, Name }`; the complex value is silently not written, and a later `Find` throws `InvalidOperationException: Document element is missing for required non-nullable property 'City'`. |
| Serializers | `EntitySerializer.TryGetMemberSerializationInfo` resolves properties and navigations only; `BsonSerializerFactory` has no complex-type serializer. |
| Native query | Path resolution (`TryBeginOwnedHopWalk`, `TryWalkEmbeddedReferenceHops`) is typed on `IEntityType` and walks embedded *navigations*. `TranslateOfType` throws for complex types. |
| Materialization | Shapers and streaming rewriter materialize entity types; no complex-type step. |

## Architecture

The central idea: **one description of "where does this structural member live in the document", consumed by every
layer**, instead of each layer re-deriving it.

### 1. Metadata: `MongoStructuralMemberExtensions` (Extensions, internal; beside `MongoEntityTypeExtensions`)

Single source of truth for complex properties:

- `GetElementName(IReadOnlyComplexProperty)` (annotation, else CLR name) — replaces both duplicated helpers.
- `IsEmbeddedCollection`, `IsOptional` (EF10) and nullability facts, so no caller inspects EF flags directly.
- Convention work only where a convention needs a complex-property overload (naming convention for camel-case
  element names, `[BsonElement]`/`[Column]` attribute conventions). Annotation keys are not added or changed
  (changing `Mongo:` keys is a breaking change per AGENTS.md); complex properties reuse `Mongo:ElementName`.

### 2. Document layout: `StructuralPath` (Query/NativeTranslation)

A small resolver that walks a root-first list of member names across a **mixed chain** of owned navigations and
complex properties and returns `(segments, leaf, crossesCollection)`. It replaces `TryWalkEmbeddedReferenceHops`'s
entity-only walk and is the only place that knows both kinds of hop. Everything native (predicates, projections,
ordering, grouping, aggregates, set-op operands) asks it for a field path. Anything that crosses an array
without a quantifier declines to the existing quantifier path (`TryResolveOwnedCollectionPath` generalized to
accept complex collections).

### 3. Write path: `ComplexValueWriter` (Storage)

Writes a complex property's current value as a subdocument / array of subdocuments, recursing through nested
complex properties, using `BsonSerializerFactory` per leaf property. `MongoUpdate.WriteEntity` calls it next to
`WriteOwnedEntities`; it does not grow `WriteOwnedEntities`. Change detection: EF tracks complex properties as
part of the owning entry; the writer honors `IsModified` per complex *leaf* so partial updates stay partial where
the existing update pipeline supports that, and otherwise rewrites the whole subdocument.

### 4. Serializers (Serializers)

`BsonSerializerFactory` gains complex-type serializer creation (single and collection), cached per
`IReadOnlyComplexType`, composing driver serializers (no global registration). `EntitySerializer`
`TryGetMemberSerializationInfo` resolves complex properties to their element and sub-serializer, so driver-LINQ
(bulk bridge, fallback) and the native renderer agree on stored shape. Value converters, `BsonRepresentation`
and discriminator behavior on complex members use the existing paths unchanged.

### 5. Native read path (Query)

- **Binding/projection:** whole complex value, leaf of a complex path, and complex type as a member of an
  anonymous/DTO projection bind through `StructuralPath`; `NativeProjectionBinder` stays free of complex-type
  special cases beyond the resolver call.
- **Materialization:** a dedicated complex-type materialization step in the shaper (DOM) and the streaming
  rewriter (one-pass), reading nested documents/arrays; required members absent in the document follow the
  existing missing-element rules; optional complex properties materialize null for missing or null.
- **Predicates/ordering/grouping/aggregates:** property-leaf comparisons use the field path; `null` checks on an
  optional complex property use existing null-vs-missing rendering rules (`nullSafe` element refs);
  complex-type-to-complex-type equality is handled structurally (member-wise, in a defined order) or declines
  with `MarkNotNativelyRepresentable()` — decided during Phase 3 and recorded in Query `AGENTS.md`.
- **Gate:** the gate calls the same predicate the binders use (Query invariant); unsupported shapes decline to
  `Fallback`, and under `NativeOnly` the tests assert there is no fallback for supported shapes.

### 6. Bulk (Query/Visitors, unchanged architecture)

`ExecuteUpdate` / `ExecuteDelete` over complex properties go through `MongoEFToLinqTranslatingExpressionVisitor`
and rely on §4 so the driver sees the right element paths. Tests live in the bulk area, not the native path.

### Dependency direction

`Metadata` ← `Serializers` ← `Storage` (write) and `Query` (read). `Query` and `Storage` do not depend on each
other for complex-type logic; both depend on the metadata extensions and serializer factory only.

## Phasing (each phase ends green on all three EF versions, with review)

0. **Probe + metadata.** Confirm the per-version EF feature matrix against the pinned packages; add
   `MongoStructuralMemberExtensions`; remove the duplicated helpers. Unit tests.
1. **Write + change tracking.** Insert/update/delete for class and struct, nested, optional, collections;
   round-trip tests against a real server; snapshot/modified-state tests.
2. **Serializers.** Complex-type serializers and member lookup; unit tests including `HasElementName`,
   converters, `BsonRepresentation`, nesting, struct.
3. **Native read path.** `StructuralPath`, materialization (DOM + streaming), projection, predicates, ordering,
   grouping, aggregates, set operations. `NativeOnly` tests for every supported shape; explicit `DriverLinq`
   pins where the native path could mask a broken fallback.
4. **Collections and optional (EF10).** Element predicates (`Any`/`All`/`Count`), indexing, null/missing
   semantics, optional-property null checks.
5. **Bulk.** `ExecuteUpdate`/`ExecuteDelete` on complex properties via the bridge.
6. **Hardening.** Full three-version suites (not per-class: allow-list/dispatch changes need the full suite),
   the area reviewers (`query-`, `storage-`, `metadata-`, `serialization-`, `public-api-`, `ef-conformance-`,
   `api-stability-`, `security-reviewer`), mutation checks that guard tests discriminate, Query/Storage/
   Metadata/Serializers `AGENTS.md` updates, `BREAKING-CHANGES.md` check against the latest release tag.

## Testing strategy

- **Unit (no DB):** metadata extensions, serializer creation/member lookup, writer output as BSON, `StructuralPath`
  resolution (including mixed owned/complex chains, collections, struct, shared types, element-name overrides).
- **Functional (real server, per-process containers, `MONGODB_URI`/`ATLAS_URI` unset):** round trips for every
  supported model shape; stored-document-shape assertions (read the raw BSON, don't trust round trips alone);
  null vs missing vs empty for optional/required members and collections; tracking and partial-update behavior.
- **Query:** each shape under default mode and under `NativeOnly`; shapes with a driver-LINQ equivalent also
  under explicit `DriverLinq` for parity. Compare against in-memory LINQ oracles where possible; classify
  failures positively (never "fails, so skip").
- **Negative/robustness:** unsupported shapes decline cleanly with the documented exception; BSON edge cases
  (extra elements, wrong element type, deep nesting, empty collections, very long chains).
- **Discrimination:** every guard test is shown to fail when its guard is removed (mutation), per project lesson.
- Count claims in docs are re-derived from tables, never restated.

## Risks and open questions

- **EF per-version feature matrix** (struct, optional, collections on EF8/9/10) is from memory of EF release notes
  and is verified in the Phase 0 probe; the matrix above is a plan input, not a fact.
- **Complex-to-complex equality** semantics (member-wise vs decline) need a ruling once the renderer cost is clear.
- **Partial update granularity** for complex properties depends on what `IUpdateEntry` exposes for complex
  leaves on each EF version; the writer falls back to whole-subdocument replacement where it does not.
- **`StructuralPath` blast radius:** replacing the entity-only walk touches many binders; it is introduced behind
  the existing predicate with owned-type behavior pinned by the existing suite before and after.
