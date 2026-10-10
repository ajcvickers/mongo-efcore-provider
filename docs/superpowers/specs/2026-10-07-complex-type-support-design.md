# Complex type support — design

Branch `EF-168a` (from `EF-322c`). Status: implemented (Tasks 1-16); user docs in `docs/complex-types.md`, breaking-change
entry in `BREAKING-CHANGES.md`. The sections below the Decisions summary are the original design, kept as written.

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

- (c) (ruling R9, Task 10) In a join, the query root's own shaper is recognised by its projection binding, not its CLR type, so a
  derived OUTER root (`People.OfType<Employee>()` with a self-reference to `Employee`) reads its own members from the root
  document: the mixed projection `new { e, e.Name, R = e.Referrer!.Name }` read `e.Name` off the joined referrer at 040cecdf
  (`Dev|Boss|Boss`, observed) and now answers `Dev|Dev|Boss`. A wrong-rows fix needed for derived complex values. Also a
  correction of RELEASED behaviour: published 10.0.4 answers the same `Dev|Boss|Boss` (measured on MongoDB 8, final fix
  wave); 8.4.4 and 9.1.4 throw (the reference navigation is untranslatable there).
- (d) (ruling R13, Task 11) Join and sort keys read through an owned/complex HOP are resolved structurally, never by the
  leaf's simple name, and the mixed reader's `_outer` redirect covers hop sources. Non-complex cases corrected (silent wrong
  rows before): an owned-hop join key beside a same-named root property (now declines; fallback correct); an inner-side hop
  key named like the navigation's principal key (`a => a.Tag.Id`, kept the `_id` navigation `$lookup`); the bridge's
  whole-entity `LeftJoin` on an owned-hop key; a filtered-Include `OrderBy` through an owned hop (sorted by the root
  property); an owned hop leaf beside a whole joined entity (read null). Same bug class as the complex shapes; one fix.
  The filtered-Include `OrderBy` (`GetSortField`) and bridge `LeftJoin` key (`TryGetKeyFieldPath`) cases also correct
  RELEASED code: both resolve by simple name at v8.4.4, v9.1.4 and v10.0.4 (verified in the tagged source).
- (e) (rulings R18/R22, Task 13) At the ROOT (entity scope), a relational comparison over a null-propagating date-add,
  conditional or coalesce whose operand may be null now gets the same null guard as every other relational comparison
  (`MayBeNull` is structural for those three nodes), so rows whose operand is null no longer match `<`/`<=`; the direction
  matches the existing relational-guard policy (driver-LINQ/main order null below every value and include them). Measured
  at b4ddcf95 vs HEAD and pinned per mode in `NativeRootNullPropagatingOperandGuardTests`; the concrete queries whose
  native rows changed: `x.Date.AddDays(x.Amount!.Value) < c` ([f, m, n, v] → [v]);
  `x.Date.AddDays(x.Flag ? 1 : x.Amount!.Value) < c` and `(x.Flag ? x.Date : x.NullableDate!.Value).AddDays(1) < c`
  ([f, h, m, n, v] → [h, m, n, v]); `(x.Flag ? 0 : x.Score!.Value) < 5` and `(x.Flag ? x.Rank : x.Score!.Value) < 5`
  ([f, h, m, n, v] → [h, m, n, v]); `(x.A ?? x.Label!.Length) < 5` ([f, m, n, v] → [v]);
  `(x.A ?? (x.Flag ? 0 : x.B!.Value)) < 5` ([f, h, m, n, v] → [h, m, n, v]). Already-guarded shapes (a nullable-typed
  date-add/coalesce, a conditional whose nullable branch gives it its type) and every `>` are unchanged.
- (f) (Task 9 round 2) An owned-hop `EF.Property` projection (`new { C = EF.Property<string>(b.Home, "City") }`) read NULL
  rows; it now reads the stored value. Unreleased native path; low risk.
- (g) (Task 10 round 4) `OfType<T>()` + `SelectMany` projecting a derived outer member threw `ArgumentException` in every
  mode; it now declines cleanly (fallback). Unreleased native path; low risk.

## Decisions

Final summary (the rulings are recorded in the SDD ledger; the ones that changed or completed the design):

- **R1** The complex serializer is for member lookup only: whole-value `Serialize` throws (never match by example).
- **R5** Any member change rewrites the whole top-level complex property (`$set`): last-writer-wins per complex property;
  per-member `$set` is a deferred optimization.
- **R7/R8** A whole complex value is refused (clear `NotSupportedException`, every mode) as the operand of value-reading
  operators (Distinct, set operations, Contains, Cast, Join, selector-less aggregates, operators after paging) and in
  GroupBy (key, key part, grouped elements); R10 accepts two residual GroupBy shapes that fail like their scalar analogues.
- **R14-R16** Complex equality: a presence guard whenever the compared path has ANY optional ancestor; stored pairs need a
  bijection of mapped members; element-scope equality agrees with the element's null check.
- **R17-R22** Complex collections (EF10): a null element reads every member as null (R17); `MayBeNull` is structural for
  date-add/conditional/coalesce (R18; captured non-nullable operands are not "may be null", so root MQL over captured values
  is unchanged); a non-nullable bool member of a null element reads false (R19); default mode serves correct rows or refuses
  (R20) through a best-effort structural net with documented limits (R21); the root-visible R18 change is accepted (R22,
  exception (e)).
- **R23** Explicit `DriverLinq` stays an opt-out of the bulk complex-collection refusal (documented as a data-loss warning).
- **R24** EF8/EF9: complex projections from an entity that also has an owned navigation fall back (whole values fail
  loudly); accepted as a documented limitation.
- Task 16: encryption annotations and concurrency tokens inside complex types are rejected by the model validator (both
  were silently ignored); bulk setters whose stored paths overlap are refused at translation.

- **Complex-value equality is member-wise and native** (Task 12; resolves the open question below). `==`, `!=`, `!(...)` and
  `.Equals(...)` between a whole single complex value and `null`, a constant/captured/inline-constructed instance, or another
  stored complex value of the same CLR type translate to a conjunction of leaf equalities, recursing into nested complex
  values; never a by-example match of the stored subdocument (element order and unmapped elements are irrelevant).
  Null semantics are C#'s: BSON null and MISSING read alike (`{p: null}` / `{p: {$ne: null}}` in the query dialect,
  `$ifNull`-normalized in `$expr` and element scopes); `{}` is present; a value that can be absent (it or ANY owned/complex
  ancestor is optional) equals an instance only when present, and two such values are equal when both are absent (null
  propagation, `null == null`); stored pairs must map the same members (else decline, so operand order can't matter); a captured
  comparand's null-ness and members are read per execution. `!=` is the exact De Morgan complement built with the equality.
  Declines (fallback refuses: the complex serializer, ruling R1, or the driver's "serialized differently"): complex
  collections, primitive-collection/`byte[]` leaves, shadow leaves against an instance (EF8/EF9), an inline construction
  leaving a member unbound or reading the row, two stored values whose leaves are not `StoredSerialization.StoredAlike`,
  out-of-scope chains (outer/element scope, after a projected `Distinct`), and an optional struct's `.Value.Leaf`.

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
- **Complex-to-complex equality**: decided, member-wise (see Decisions).
- **Partial update granularity** for complex properties depends on what `IUpdateEntry` exposes for complex
  leaves on each EF version; the writer falls back to whole-subdocument replacement where it does not.
- **`StructuralPath` blast radius:** replacing the entity-only walk touches many binders; it is introduced behind
  the existing predicate with owned-type behavior pinned by the existing suite before and after.

## Known limitations and follow-ups

Candidate Jira tickets (NOT filed; owner to decide), numbered as in the SDD ledger:

1. Explicit BSON null in a required `string`: native throws, driver-LINQ reads null (D-F10).
2. Same-type self-join projecting the whole outer entity and an inner-side hop leaf returns wrong rows on driver-LINQ (pinned).
3. Explicit join to an `OfType<T>()` set is unserved.
4. Owned `SelectMany` fallback fails loudly for a whole outer/element beside a member.
5. Reference-collection `SelectMany` has no fallback.
6. GroupJoin on non-FK keys answers FK counts (known main bug M35).
7. Joins/set operations over converted values compare stored forms.
8. The fallback misreads members in a join between two shared-type entities of one CLR type.
9. Driver-LINQ: projected complex equality always `false`.
10. Driver-LINQ: `c.Opt == null ? ...` treats a missing optional complex value as present.
11. Driver-LINQ misses a missing element inside an owned collection in `Any/Count(i => i.Pos == null)`.
12. Root comparisons on members of a null/missing `DateTimeOffset?` match those rows in every mode (pre-existing).
13. Driver-LINQ `$strLenCP` error on field-to-field string operators over null.
14. Driver-LINQ misses a missing complex value in `$map`.
15. Upstream EF: `Any(a => a == null)` over a complex collection throws `ArgumentException`.
16. Driver-LINQ element-scope missing-vs-null semantics (general).
17. `optionalCollection == null` on the Native fallback also returns rows whose array is `[null]`. RESOLVED by ruling R25
    (final fix wave): the default mode refuses a complex collection compared with null; explicit DriverLinq keeps the
    driver's rows (pinned).
18. Root `BsonRepresentation` self-referencing bulk setter writes the computed CLR type into a string-stored field.
19. Driver-LINQ `byte[].Length == 0` returns no rows.
20. Driver-LINQ `Distinct` over collection-typed properties returns a duplicate `<null>` row (explicit null vs missing).

(CSHARP-5296, driver-LINQ DateTimeOffset members, is an existing upstream driver ticket.)

Other follow-ups: R24 (peel the EF8/EF9 owned `IncludeExpression` in the hop walk); native joins/reference navigations
through complex or owned hops; native element-member projections, indexers and element aggregates over complex
collections; native constructor/record projections of complex values; native GroupBy/Distinct over whole complex values
(structural equality); per-member `$set` (R5); index-friendly `$elemMatch` for positive complex-collection `Any`; narrower
R20/bulk refusals (relational upper side, `== true`, non-nullable `Contains`) and making the R20 scanner call the
R17-R19 predicates instead of restating them; DatePart-over-DateAdd native translation (removes one R20 refusal).

Final-triage follow-ups (NOT filed in Jira; owner to decide):

1. R24: EF8/EF9 owned-navigation + complex projection goes to the fallback (whole complex value fails loudly); peel the
   owned `IncludeExpression` in the hop walk.
2. R21 limits of the R20 net (navigation/join roots, element-leaf `Select`, null tests not against a null constant, element
   reads inside nested lambdas): file a Jira before release.
3. Root comparisons over members of a null/missing `DateTimeOffset?` match those rows in every mode (Jira candidate 12).
4. R23: optionally log a warning when an explicit-DriverLinq bulk operation reads complex collection elements.
5. Split `ComplexElementNullGuardRefusal` (one Finder, two jobs by a `strict` flag) into a shared keying base plus query
   and bulk subclasses.
6. Split the large complex-type test files (Robustness, Materialization, ComplexCollectionNativeQuery) by theme.
7. Complex-collection positive `Any` renders in an aggregation element scope (`$anyElementTrue` over `$map`), losing index
   use; index-friendly `$elemMatch` where no null element can change the answer (performance only).
8. Fluent `HasElementName` overloads for complex properties and members are a public-API gap (only annotation spellings
   exist today: `HasPropertyAnnotation`/`HasAnnotation(MongoAnnotationNames.ElementName, ...)`,
   `Metadata.SetElementName` on a member; `HasElementName` is CS1929 on EF8/EF9/EF10).
9. DECISION BEFORE RELEASE (owner declined for now, 'Keep strict'): read a missing REQUIRED complex value whose members
   are all nullable as an instance (matches released behaviour; 10.0.4 returned a non-null instance with null members).
   Today it throws; BREAKING-CHANGES.md documents the change and the mitigations.
10. EF8/EF9 baselines: the published 8.4.4/9.1.4 probes match 10.0.4 for complex-property storage; EF8/EF9 run a subset of
    the complex-type tests (complex collections and optional complex properties are EF10-only), so their suite totals
    are lower by design.
