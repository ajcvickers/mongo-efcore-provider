# Complex Type Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complex types (class/struct, nested, optional, collections where EF supports them) work end to end in the MongoDB EF Core provider: model, save, track, and query on the native pipeline.

**Architecture:** One shared description of where a complex member lives in the document (`MongoStructuralMemberExtensions` + `StructuralPath`) consumed by a new write path (`ComplexValueWriter`), new serializers (`BsonSerializerFactory`/`EntitySerializer`), and the native read path (binders, translator, materializers). Bulk operations stay on the driver-LINQ bridge.

**Tech Stack:** C#, EF Core 8/9/10 (build configurations `Debug EF8|EF9|EF10`), MongoDB C# driver 3.11.2, xUnit with plain `Assert.*`, TestContainers (`MONGODB_URI`/`ATLAS_URI` unset).

**Spec:** `docs/superpowers/specs/2026-10-07-complex-type-support-design.md`

## Global Constraints

- Branch `EF-168a` (from `EF-322c`); commit directly on it, **no worktrees**; every commit subject starts `EF-168a: `.
- All new queries use the native pipeline; `MongoQueryMode.NativeOnly` is the oracle; no new driver-LINQ query paths.
- Bulk `ExecuteUpdate`/`ExecuteDelete` stay on the driver-LINQ bridge.
- Do **not** read or port upstream test classes: `ComplexTypesTrackingTestBase<T>`, `ComplexTypeQueryTestBase<T>`, `AdHocComplexTypeQueryTestBase`, `ModelBuilderTest.ComplexTypeTestBase`, `ModelBuilderTest.ComplexCollectionTestBase`, `ComplexPropertiesProjectionTestBase`, `ComplexPropertiesStructuralEqualityTestBase`, `ComplexPropertiesMiscellaneousTestBase`, `ComplexPropertiesBulkUpdateTestBase`. Tests are derived from the spec and the code.
- Preserve file BOMs; `src/` is nullable-enabled; annotate new types. xUnit plain `Assert.*`; tests run serially.
- Version-conditional code uses `#if EF8 || EF9` / `#if !EF8`. Everything must build and pass on EF8, EF9, EF10.
- No `Mongo:` annotation key is added or changed; complex properties reuse `Mongo:ElementName`.
- Run tests with `MONGODB_URI` and `ATLAS_URI` unset. Give each subagent a unique scratch subdir (e.g. `/tmp/ef168a-<task>/`) for logs.
- Allow-list/dispatch changes are verified with the **full** suite (all three versions) before being called done.
- Counts in prose are re-derived from tables, never restated.

## Review Focus

1. Required complex property whose subdocument is **missing or null** in a stored document: materializes per existing missing-required-element rules, not an NRE or silent default struct. Test in Task 10.
2. Optional complex property **null vs missing vs empty `{}`** round-trips and filters (`== null`, `!= null`, leaf compare) distinguish them correctly. Tasks 6, 12.
3. Complex property **shadowing a scalar or navigation element name** (`HasElementName` collision, element `_t`/`_id`): model validation error, not silent overwrite. Task 3.
4. **Deep nesting + struct + same complex CLR type used at two places** (shared type, different element names): each path resolves independently. Task 9.
5. **Complex collection edge cases:** empty array, null array, element with unmapped extra fields, `Any`/`Count` on empty. Task 13.

## File Structure

| File | Responsibility |
|---|---|
| `src/…/Extensions/MongoStructuralMemberExtensions.cs` (new, internal) | Element name, collection/optional facts for `IReadOnlyComplexProperty`; nothing else |
| `src/…/Storage/ComplexValueWriter.cs` (new, internal) | Write complex values (single/collection/nested) as BSON from an `IUpdateEntry` |
| `src/…/Storage/MongoUpdate.cs` (modify) | Call the writer next to `WriteOwnedEntities` |
| `src/…/Serializers/BsonSerializerFactory.cs`, `EntitySerializer.cs` (modify) | Complex-type serializers; member lookup |
| `src/…/Query/NativeTranslation/StructuralPath.cs` (new, internal) | Resolve a member-name chain across owned navs and complex properties to a field path |
| `src/…/Query/NativeTranslation/*` binders/translator (modify) | Call `StructuralPath`; no complex special-casing elsewhere |
| `src/…/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`, `MongoStreamingEntityMaterializerRewriter.cs`, `MongoProjectionBindingRemovingExpressionVisitor.cs` (modify) | Complex materialization (DOM + one-pass) |
| `tests/…UnitTests/Extensions/MongoStructuralMemberExtensionsTests.cs` etc. | Unit tests per component |
| `tests/…FunctionalTests/ComplexTypes/*.cs` (new folder) | Round-trip, stored-shape, tracking, native-query, bulk tests; one file per concern |

Test entity model shared by the functional tests lives in `tests/…FunctionalTests/ComplexTypes/ComplexTypeModels.cs` (Task 1).

Commands used throughout (replace `EF10` with `EF8`/`EF9`):

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
env -u MONGODB_URI -u ATLAS_URI dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~<Class>"
```

---

## Phase 0 — Probe and metadata

### Task 1: Feature-matrix probe and shared test models

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/ComplexTypes/ComplexTypeModels.cs`
- Create: `tests/MongoDB.EntityFrameworkCore.UnitTests/Metadata/ComplexTypeFeatureMatrixTests.cs`
- Modify: `docs/superpowers/specs/2026-10-07-complex-type-support-design.md` (record the measured matrix)

**Interfaces:**
- Produces: model classes used by all later functional tests:
  `ComplexAddress { string Street; string City; GeoPoint Location }`, `GeoPoint` (struct `{ double Lat; double Lon }`), `CustomerWithAddress { ObjectId Id; string Name; ComplexAddress Address; }`, `CustomerWithOptionalAddress { …; ComplexAddress? Address }` (`#if !EF8 && !EF9` gated if the probe says optional needs EF10), `CustomerWithAddressList { …; List<ComplexAddress> Addresses }` (gated likewise).

- [ ] **Step 1: Write the probe test.** A unit test per feature that builds a model (`ModelBuilder` through `MongoConventionSetBuilder`, as the existing `BsonIgnoreComplexPropertyTests` do — copy its model-building helper) and asserts it builds: class property, struct property, nested, optional (`ComplexProperty(e => e.Address)` on a nullable reference), collection (`ComplexCollection`). Wrap each in its own `[Fact]`; guard unsupported-on-this-EF-version facts with `#if` and record which.
- [ ] **Step 2: Run on EF8, EF9, EF10; write down which facts pass/fail per version.**

  Run: `dotnet test … -c "Debug EF8" --filter "FullyQualifiedName~ComplexTypeFeatureMatrixTests"` (and EF9, EF10).
- [ ] **Step 3: Update the spec's scope bullet with the measured matrix** (table: feature × EF8/EF9/EF10, from the runs) and fix `#if` guards in the models/tests to match.
- [ ] **Step 4: Probe current runtime behavior** with a throwaway functional test (not committed): `SaveChanges` then `Find` for `CustomerWithAddress` today. Record in the spec's "Current state" row what actually happens (expected: complex value silently not written).
- [ ] **Step 5: Commit.**

```bash
git add tests docs && git commit -m "EF-168a: probe the per-version complex type feature matrix; shared test models"
```

### Task 2: `MongoStructuralMemberExtensions` + de-duplicate element-name helpers

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Extensions/MongoStructuralMemberExtensions.cs`
- Modify: `src/…/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` (delete `GetComplexPropertyElementName`, ~line 1271; update caller ~line 1261)
- Modify: `src/…/Query/NativeTranslation/MongoSelectLowerer.cs` (delete helper ~line 597; update caller ~line 591)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Extensions/MongoStructuralMemberExtensionsTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  internal static class MongoStructuralMemberExtensions
  {
      internal static string GetElementName(this IReadOnlyComplexProperty property);
      internal static bool IsEmbeddedCollection(this IReadOnlyComplexProperty property);
      internal static bool IsOptional(this IReadOnlyComplexProperty property); // #if !EF8 && !EF9 reads IsNullable; else false
  }
  ```

- [ ] **Step 1: Write failing tests**: default element name = property name; `HasElementName("addr")`-style annotation (`complexProperty.SetAnnotation(MongoAnnotationNames.ElementName, "addr")` via the model finalization the existing unit tests use) overrides; collection flag true for `ComplexCollection`; `IsOptional` false for required.
- [ ] **Step 2: Run, expect compile failure** (`GetElementName` for complex property missing).
- [ ] **Step 3: Implement** (body of the old helper: `(string?)property[MongoAnnotationNames.ElementName] ?? property.Name`), delete the two duplicated helpers, route both call sites through it.
- [ ] **Step 4: Run unit tests plus `MongoSelectLowererTests`** on EF10, EF9, EF8; all green.
- [ ] **Step 5: Commit** `EF-168a: share one complex-property element-name helper`.

### Task 3: Convention and validation coverage for complex properties

**Files:**
- Modify: `src/…/Metadata/Conventions/CamelCaseElementNameConvention.cs`, `Metadata/Conventions/BsonAttributes/*` (only where a complex-property overload is missing; check each convention that handles `IPropertyAddedConvention` for a sibling `IComplexPropertyAddedConvention`)
- Test: `tests/…UnitTests/Metadata/Conventions/ComplexPropertyConventionTests.cs`

**Interfaces:** Consumes `GetElementName(this IReadOnlyComplexProperty)` (Task 2).

- [ ] **Step 1: Write failing tests**: camel-case convention applies to complex property element name and to leaf properties inside complex types; `[BsonElement("addr")]` on a complex property sets its element name; `[BsonIgnore]`/`[BsonRequired]` already covered — add a regression assert each; two members of one type mapping to the same element name (Review Focus 3) fails model validation with a clear message (use the existing duplicate-element-name validation if present; if no validator covers complex properties, extend it in `Infrastructure`/model validator the same way).
- [ ] **Step 2: Run; expect the specific failures (not compile errors).**
- [ ] **Step 3: Implement** the minimal convention/validator additions, each as its own small class or method beside its siblings.
- [ ] **Step 4: Run unit suite on EF8/EF9/EF10.** Green.
- [ ] **Step 5: Commit** `EF-168a: conventions and validation for complex property element names`.

---

## Phase 1 — Write path and change tracking

### Task 4: `ComplexValueWriter` (BSON output)

**Files:**
- Create: `src/…/Storage/ComplexValueWriter.cs`
- Modify: `src/…/Storage/MongoUpdate.cs` (`WriteEntity`: call `ComplexValueWriter.WriteComplexProperties(writer, entry, propertyFilter)` after `WriteOwnedEntities`)
- Test: `tests/…FunctionalTests/ComplexTypes/ComplexTypeWriteTests.cs`

**Interfaces:**
- Produces: `internal static class ComplexValueWriter { internal static void WriteComplexProperties(IBsonWriter writer, IUpdateEntry entry, Func<IProperty,bool>? propertyFilter); }` — iterates `entry.EntityType.GetComplexProperties()`, writes `GetElementName()` then the value: `null` → `WriteNull`; collection → array of subdocuments; otherwise a subdocument whose leaves are written via `BsonSerializerFactory.GetPropertySerializationInfo(leaf)` and whose nested complex properties recurse. Leaf values come from `entry.GetCurrentValue(complexProperty)` then the member accessor (`leaf.GetGetter().GetClrValue(instance)`), not from separate entries (complex values have no entries).

- [ ] **Step 1: Write failing tests** (functional, real server; follow `OwnedEntityElementNameTests`' style): insert `CustomerWithAddress`; read the **raw** `BsonDocument`; assert `Address` is a subdocument with `Street`, `City`, nested `Location.Lat/Lon` (struct); assert **no** `_id` inside it. Add: `HasElementName` override; value converter on a complex leaf; `BsonRepresentation` leaf; an `ObjectId`/`Guid`/`DateTime` leaf serialized as the driver would.
- [ ] **Step 2: Run; expect failures — `Address` missing from stored doc.**
- [ ] **Step 3: Implement the writer** and the one-line `MongoUpdate` call. Keep `WriteOwnedEntities` untouched.
- [ ] **Step 4: Run `ComplexTypeWriteTests` on EF10/EF9/EF8.** Green.
- [ ] **Step 5: Commit** `EF-168a: write complex properties as embedded subdocuments`.

### Task 5: Update and delete behavior, change tracking

**Files:**
- Modify: `src/…/Storage/ComplexValueWriter.cs`, possibly `MongoUpdate.cs` (partial-update filter)
- Test: `tests/…FunctionalTests/ComplexTypes/ComplexTypeTrackingTests.cs`

**Interfaces:** Consumes `ComplexValueWriter.WriteComplexProperties` (Task 4).

- [ ] **Step 1: Write failing tests**: load a customer, mutate `Address.City`, `SaveChanges`, assert the raw stored doc has the new city and the **other** leaves/ other top-level fields unchanged; replace `Address` with a new instance; mutate a nested struct leaf; no-op save does not issue an update (use the `SpyLogging`/command interceptor pattern the existing update tests use); delete removes the document; `entry.Property(e => e.Address.City).IsModified` reflects the change; `ChangeTracker.DetectChanges` picks up a nested-leaf change on a **tracked** entity loaded by query.
- [ ] **Step 2: Run; record exactly which fail.**
- [ ] **Step 3: Implement.** Decision rule: if the update pipeline passes `entry.IsModified` for complex leaves, honor it per leaf; otherwise, when any leaf of a complex property is modified, rewrite that **whole** complex subdocument (never write a partial subdocument that would drop untouched leaves). Record which applies per EF version in a comment on `WriteComplexProperties`.
- [ ] **Step 4: Run tracking + write tests on all three versions.** Green.
- [ ] **Step 5: Commit** `EF-168a: update and track complex property changes`.

### Task 6: Optional, nested-optional and collection writes (per matrix)

**Files:**
- Modify: `src/…/Storage/ComplexValueWriter.cs`
- Test: `tests/…FunctionalTests/ComplexTypes/ComplexTypeOptionalAndCollectionWriteTests.cs`

- [ ] **Step 1: Write failing tests** (guarded by `#if` per the Task 1 matrix): optional set to `null` stores BSON `null` (decision: matches existing owned-reference behavior `WriteOwnedEntities` → `WriteNull`; assert and keep consistent); optional with value; collection of N elements stored as array of N subdocuments in list order; empty collection stored as `[]`; null collection; nested complex inside a collection element; modifying one element rewrites the whole array.
- [ ] **Step 2: Run; expect failures.** 
- [ ] **Step 3: Implement** the array/null branches.
- [ ] **Step 4: Run on all supported versions.** Green.
- [ ] **Step 5: Commit** `EF-168a: write optional and collection complex properties`.

---

## Phase 2 — Serializers

### Task 7: Complex serializers and member lookup

**Files:**
- Modify: `src/…/Serializers/BsonSerializerFactory.cs` (add `internal IBsonSerializer GetComplexPropertySerializer(IReadOnlyComplexProperty)` cached per `IReadOnlyComplexType`; collection variant via the existing `GetCollectionSerializer`)
- Modify: `src/…/Serializers/EntitySerializer.cs` (`TryGetMemberSerializationInfo`: after properties, check `_entityType.FindComplexProperty(memberName)`)
- Create: `src/…/Serializers/ComplexTypeSerializer.cs` (`IBsonSerializer<T>` + `IBsonDocumentSerializer` mirroring `EntitySerializer` but over `IReadOnlyComplexType`; member lookup for leaves and nested complex properties)
- Test: `tests/…UnitTests/Serializers/ComplexTypeSerializerTests.cs`

**Interfaces:**
- Produces: `TryGetMemberSerializationInfo("Address")` returns `BsonSerializationInfo(elementName: complexProperty.GetElementName(), serializer, nominalType)`; for a nested path the sub-serializer answers `"City"`.
- Consumes: `GetElementName` (Task 2), `GetPropertySerializationInfo` (existing).

- [ ] **Step 1: Write failing unit tests**: `TryGetMemberSerializationInfo` for complex property, leaf inside it, struct leaf, `HasElementName`, converter leaf (serializer is `ValueConverterSerializer`), nested complex chain; unknown member returns false; the serializer for a collection is an array serializer over the complex-type serializer.
- [ ] **Step 2: Run; expect failures.**
- [ ] **Step 3: Implement.** The complex serializer's `Serialize` throws `NotSupportedException` for whole-value comparison exactly as `EntitySerializer` does **unless** Task 12's equality ruling lands (revisit there); `Deserialize` is `NotImplementedException` like the entity serializer (materialization is not via serializers).
- [ ] **Step 4: Run unit + `…FunctionalTests/Serialization` filters on three versions.** Green.
- [ ] **Step 5: Commit** `EF-168a: complex type serializers and member lookup`.

---

## Phase 3 — Native read path

### Task 8: `StructuralPath` resolver

**Files:**
- Create: `src/…/Query/NativeTranslation/StructuralPath.cs`
- Modify: `src/…/Query/NativeTranslation/MongoExpressionTranslator.Members.cs` (`TryWalkEmbeddedReferenceHops`, `TryResolveOwnedReferenceNavigationPath`, `TryResolveOwnedPropertyPath` use it)
- Test: `tests/…UnitTests/Query/NativeTranslation/StructuralPathTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  internal readonly record struct StructuralPathResult(
      IReadOnlyList<string> Segments, IReadOnlyPropertyBase? Leaf, ITypeBase LeafOwner, bool CrossesCollection);
  internal static class StructuralPath
  {
      // names: root-first member names; scope: the starting entity type.
      internal static bool TryResolve(
          ITypeBase scope, IReadOnlyList<string> names, int hopCount, out StructuralPathResult result);
  }
  ```
  A hop resolves, in order, as an embedded navigation (existing semantics, single reference only) or a complex property (`FindComplexProperty`); `CrossesCollection` is set for collection hops and, when true, `TryResolve` returns false for the dotted-path callers (quantifier callers use the collection variant, Task 13).

- [ ] **Step 1: Write failing unit tests** on in-memory models: owned→owned (existing behavior preserved, assert same segments as today), complex→leaf, owned→complex→complex→leaf, struct hop, element-name override, composite-key leaf inside owned (existing `_id.` behavior preserved), unknown name returns false, collection hop returns false.
- [ ] **Step 2: Run; expect compile failure.**
- [ ] **Step 3: Implement `StructuralPath`; replace the three call sites.** Owned-type behavior must not change: run the whole `Native*Owned*` and `Ef362*` functional tests before and after (they are the pin).
- [ ] **Step 4: Run unit tests + `FullyQualifiedName~Owned|FullyQualifiedName~Ef362` functional filters on EF10; then full unit suites on all three.** Green.
- [ ] **Step 5: Commit** `EF-168a: StructuralPath resolves mixed owned/complex member chains`.

### Task 9: Native predicates, ordering and scalar projection over complex paths

**Files:**
- Modify: `MongoExpressionTranslator.Members.cs`, `NativeProjectionBinder.cs`, `NativeSlotPopulator.cs`, `MongoQueryableMethodTranslatingExpressionVisitor.cs` (translate-where/order/select entry points) — only to call `StructuralPath`
- Test: `tests/…FunctionalTests/ComplexTypes/ComplexTypeNativeQueryTests.cs`

**Interfaces:** Consumes `StructuralPath.TryResolve` (Task 8).

- [ ] **Step 1: Write failing tests** using `NativeModeAssert.NativeAndParity` (default-mode results equal the driver-LINQ oracle, and NativeOnly doesn't throw): seed N customers; `Where(c => c.Address.City == "X")`; nested `c.Address.Location.Lat > 1`; string ops (`StartsWith`) on a complex leaf; `OrderBy`/`ThenBy` on leaf; `Select(c => c.Address.City)`; `Select(c => new { c.Name, c.Address.Location.Lon })`; `Distinct` of a complex leaf; `GroupBy(c => c.Address.City)` with `Count`/`Sum`; `Min`/`Max`/`Average` over a leaf; same CLR complex type at two paths (`BillingAddress`, `ShippingAddress` with different element names — Review Focus 4). Assert emitted MQL uses dotted paths (use the MQL logging the existing `Native*Tests` use).
- [ ] **Step 2: Run; classify each failure positively** (decline vs wrong rows vs exception) in a scratch note.
- [ ] **Step 3: Implement** until green; each fix is made at the layer that owns it (resolver, binder, gate), never by special-casing complex types in unrelated binders. The gate must call the same predicate the binder uses.
- [ ] **Step 4: Run `ComplexTypeNativeQueryTests` on three versions, then the full `FunctionalTests` on EF10.** Green.
- [ ] **Step 5: Commit** `EF-168a: native predicates, ordering and projection over complex paths`.

### Task 10: Whole-complex-value projection and materialization (DOM + one-pass)

**Files:**
- Modify: `src/…/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` (`InjectStructuralTypeMaterializers` neighborhood), `MongoStreamingEntityMaterializerRewriter.cs`, `MongoProjectionBindingRemovingExpressionVisitor.cs`, `MongoProjectionBindingExpressionVisitor.cs`
- Create (if the logic exceeds ~80 lines): `src/…/Query/Visitors/ComplexTypeMaterializationBuilder.cs` — builds the expression that reads a nested document into a complex-type instance (class: construct + set; struct: construct + set on a local + assign) for a given `IReadOnlyComplexType`, used by both the DOM binding remover and the streaming rewriter so the logic exists once.
- Test: `tests/…FunctionalTests/ComplexTypes/ComplexTypeMaterializationTests.cs`

**Interfaces:**
- Produces: `internal static class ComplexTypeMaterializationBuilder { internal static Expression Build(IReadOnlyComplexProperty property, Expression bsonSource, …); }` (exact extra parameters follow whichever of the two callers needs them; both callers pass the same shape).

- [ ] **Step 1: Write failing tests** (default mode, `NativeOnly`, tracking and `AsNoTracking`, and both materializer paths via the existing `NativeMaterializerOnePassTests` switch): load entity with complex property; `Select(c => c.Address)` (whole complex value, **not tracked**); `Select(c => new { c.Name, c.Address })`; nested struct; `Select(c => c.Address.Location)`; **Review Focus 1**: stored doc with `Address` missing/null for a required complex property — assert the behavior the existing missing-required-scalar rules prescribe for entity members (read `NativeMissingRequiredScalarProjectionTests` and mirror its stated rule, documenting it); unmapped extra elements in the subdocument are ignored; wrong BSON type throws the same exception type as a wrong-typed scalar.
- [ ] **Step 2: Run; expect failures.**
- [ ] **Step 3: Implement** the builder and wire both paths; the one-pass path throws `NativeTranslationNotSupportedException` for shapes it cannot stream (existing contract), which falls to DOM.
- [ ] **Step 4: Run on three versions; then the **full** `FunctionalTests` + `SpecificationTests` on EF10** (shaper change: full-suite verification).** Green.
- [ ] **Step 5: Commit** `EF-168a: materialize complex properties natively (DOM and one-pass)`.

### Task 11: Complex types in set operations, Distinct, joins and Include-adjacent shapes

**Files:**
- Modify: whichever binder rejects (found by test failures; the gate-calls-the-predicate rule applies)
- Test: `tests/…FunctionalTests/ComplexTypes/ComplexTypeCompositionTests.cs`

- [ ] **Step 1: Write failing tests**: `Concat`/`Union`/`Intersect` of projections including complex leaves; `Distinct` on a complex leaf projection; a complex property on an entity that is the **inner** of a join and the outer; complex property on an entity that also has `Include`d reference/collection navigation; `FirstOrDefault`/`Single`/`Count`/`Any` with complex-leaf predicates; `Skip/Take` with complex-leaf ordering; query filters (`HasQueryFilter`) referencing a complex leaf; entity with TPH hierarchy where a derived type owns the complex property (discriminator + complex path).
- [ ] **Step 2: Run; classify each failure** (supported-but-broken vs correctly declines). Anything that is a legitimate decline asserts `MarkNotNativelyRepresentable` routing via `NativeModeAssert`'s decline helper and is recorded in Query `AGENTS.md` (Task 16).
- [ ] **Step 3: Implement the supported ones** at the owning layer.
- [ ] **Step 4: Run three versions; full EF10 `FunctionalTests`.** Green.
- [ ] **Step 5: Commit** `EF-168a: complex types compose with set ops, joins, Include and hierarchies`.

---

## Phase 4 — Optional, equality, collections

### Task 12: Optional complex properties and complex equality

**Files:**
- Modify: `MongoExpressionTranslator.EntityEquality.cs`, `MongoExpressionTranslator.Members.cs` (`TryResolveEntityTypedOperand` analog for complex operands)
- Test: `tests/…FunctionalTests/ComplexTypes/ComplexTypeNullAndEqualityTests.cs`

- [ ] **Step 1: Write failing tests** (`#if !EF8 && !EF9` per matrix): stored `null`, missing, and `{}` for an optional complex property — `Where(c => c.Address == null)`, `!= null`, `c.Address.City == "X"` (null/missing absorb to false), `c.Address != null && c.Address.City == "X"`, projections of `c.Address` for each stored state, **Review Focus 2**. Equality: `Where(c => c.Address == other)` where `other` is a captured complex instance, and `c.A == c.B` between two complex paths: **ruling**: implement member-wise equality (`$and` of leaf `$eq`s, nested recursion, null-safe) because a silent decline would route to a driver path that throws for entity-serializer `Serialize`; if a member type cannot be compared natively (e.g., converter without stored-alike), decline through `MarkNotNativelyRepresentable` and test that decline. Record the ruling in the spec.
- [ ] **Step 2: Run; expect failures.**
- [ ] **Step 3: Implement** using the existing `nullSafe` element-ref and negation rules (negation is exact complement or decline).
- [ ] **Step 4: Run three versions (matrix-gated); full EF10 `FunctionalTests`.** Green.
- [ ] **Step 5: Commit** `EF-168a: optional complex properties and member-wise complex equality`.

### Task 13: Complex collections (per matrix)

**Files:**
- Modify: `StructuralPath.cs` (collection variant), `MongoExpressionTranslator` quantifier paths (`TryResolveOwnedCollectionPath` generalized), `NativeProjectionBinder` for collection projections
- Test: `tests/…FunctionalTests/ComplexTypes/ComplexCollectionNativeQueryTests.cs`

- [ ] **Step 1: Write failing tests** (`NativeAndParity`): `Any()`, `Any(a => a.City == "X")`, `All(...)`, `Count()`, `Count(pred)`, `Select(c => c.Addresses)`, `Select(c => c.Addresses.Select(a => a.City))`, `Contains` on leaf, `Where(c => c.Addresses.Count > 1)`, indexer `c.Addresses[0].City` (decline if the pipeline can't express it; assert the decline), nested complex collections; **Review Focus 5** empty, null and missing arrays; elements with unmapped extra fields.
- [ ] **Step 2: Run; classify failures.**
- [ ] **Step 3: Implement** by generalizing the owned-collection quantifier path to take a `StructuralPath` collection hop; do not copy the owned code.
- [ ] **Step 4: Run three versions; full EF10 `FunctionalTests`.** Green.
- [ ] **Step 5: Commit** `EF-168a: native queries over complex collections`.

---

## Phase 5 — Bulk

### Task 14: `ExecuteUpdate` / `ExecuteDelete` on the bridge

**Files:**
- Modify: `src/…/Query/Visitors/MongoEFToLinqTranslatingExpressionVisitor*.cs` (only if member access over a complex property doesn't translate)
- Test: `tests/…FunctionalTests/ComplexTypes/ComplexTypeBulkTests.cs` (EF9+ only; EF8 follows `BulkOperationsUnsupportedOnEf8Tests`)

- [ ] **Step 1: Write failing tests**: `ExecuteUpdate(s => s.SetProperty(c => c.Address.City, "Y"))`, setting a nested leaf, setting a whole complex property to a value (decline/throw ruling: if the driver bridge cannot, assert the specific exception and document), `ExecuteDelete(c => c.Address.City == "X")`, raw stored document assertions after each.
- [ ] **Step 2: Run on EF9, EF10; EF8 asserts the existing unsupported behavior.**
- [ ] **Step 3: Implement** only what fails and belongs in the bridge; rely on Task 7's member lookup.
- [ ] **Step 4: Run three versions.** Green.
- [ ] **Step 5: Commit** `EF-168a: bulk update and delete over complex properties`.

---

## Phase 6 — Hardening

### Task 15: Robustness and discrimination tests

**Files:**
- Test: `tests/…FunctionalTests/ComplexTypes/ComplexTypeRobustnessTests.cs`; unit additions beside the earlier test files

- [ ] **Step 1: Write the robustness tests**: deep nesting (5+ levels); long element names; BSON with element order permuted; duplicate-shaped shared complex types; `[BsonIgnore]`d leaf not written or read; `[BsonRequired]` leaf; enum/Guid/decimal/DateTimeOffset/TimeSpan/byte[] leaves; value-converted leaf in predicates (stored-alike rule); NoTracking vs TrackAll vs identity-resolution; `DbContext` model caching across two contexts with different element names (serializer-cache staleness trap noted in Serializers `AGENTS.md`).
- [ ] **Step 2: Mutation discrimination**: for each guard/decline added in Tasks 8–13, temporarily remove the guard, confirm the associated test **fails**, restore it; record each pair (guard, test) in a table in the PR description.
- [ ] **Step 3: Run all three versions, full suites.** Use `/test-all`. Green, with the failure-classification note for anything pre-existing (compare against `EF-322c` tip results, not memory).
- [ ] **Step 4: Commit** `EF-168a: complex type robustness and guard-discrimination tests`.

### Task 16: Review, docs, breaking-change check

**Files:**
- Modify: `src/…/Query/AGENTS.md`, `Storage/AGENTS.md`, `Serializers/AGENTS.md`, `Metadata/AGENTS.md` (invariants introduced: `StructuralPath` is the only mixed-hop walker; writer lives beside, not inside, `WriteOwnedEntities`; complex serializer contract; documented declines)
- Modify: `BREAKING-CHANGES.md` only if the check below finds a break
- Create: `docs/complex-types.md` (user-facing: mapping, stored shape, supported queries, per-version matrix, unsupported shapes)

- [ ] **Step 1: Run `/review-ef-core-provider`** over the branch (all area + cross-cutting reviewers). Triage each finding: verify, fix or reject with reason.
- [ ] **Step 2: Breaking-change check** against the latest release tag per `AGENTS.md`: does any previously-working model/behavior change (e.g., models that today ignore complex properties and now persist them)? Compile one probe against the published package if in doubt; record the result.
- [ ] **Step 3: Update the AGENTS.md files and write `docs/complex-types.md`.**
- [ ] **Step 4: Final full-suite verification** on EF8/EF9/EF10 via `/test-all`; paste the pass/fail totals into the PR description, re-summed from the per-version outputs.
- [ ] **Step 5: Commit** `EF-168a: document complex type support; review fixes`. Do **not** push or open a PR without the owner's go-ahead.

---

## Self-Review

- **Spec coverage:** metadata helper (T2), conventions/validation (T3), writer (T4–6), serializers (T7), `StructuralPath` (T8), native read path incl. materialization (T9–11), optional/equality/collections (T12–13), bulk on bridge (T14), hardening/reviews/docs/breaking check (T15–16), probe of the EF matrix (T1). The spec's three open items are decided in-plan: matrix → T1, equality → T12 ruling, partial updates → T5 decision rule.
- **Placeholders:** the only deliberately deferred detail is the exact extra parameters of `ComplexTypeMaterializationBuilder.Build` (T10), fixed by the two real callers; everything else names concrete files, tests and rulings.
- **Type consistency:** `GetElementName(IReadOnlyComplexProperty)` (T2) is consumed in T3/T7/T8; `StructuralPath.TryResolve` (T8) in T9–13; `ComplexValueWriter.WriteComplexProperties` (T4) in T5–6.
- **Review Focus:** each of the five lines is pinned in T3, T6/T12, T9, T10, T13 respectively.
