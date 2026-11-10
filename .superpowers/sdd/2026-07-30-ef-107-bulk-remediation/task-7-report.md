# Task 7 report: Guard provider-owned setter targets, and derive the `$set` key from the serialization info

## What changed and why

`BuildSetter` in `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
previously accepted any mapped root scalar property as a bulk-update setter target, including properties the
provider owns the meaning of: the primary key (which maps to the immutable `_id` element and is rejected only at
the server, as a raw `MongoCommandException`), the type discriminator (`_t` — silently rewriting a document's
declared entity type), and concurrency tokens (bulk operations don't check concurrency, so a bulk write to the
token would silently disarm optimistic concurrency for every later `SaveChanges` on that document).

Added three guards immediately after the existing `if (property == null)` block, each calling a new
`[DoesNotReturn] RejectSetterTarget(mongoQueryExpression, property, reason)` helper (placed next to the existing
`ThrowBulkSourceNotSupported`) that adds translation-error details and throws
`InvalidOperationException(CoreStrings.NonQueryTranslationFailedWithDetails(...))` — the same canonical
non-query-translation-failure shape used elsewhere in this visitor.

Separately, `BuildUpdate` in `MongoShapedQueryCompilingExpressionVisitor.cs` keyed every `$set` entry with
`setter.Property.GetElementName()`, while `BsonSerializerFactory.GetPropertySerializationInfo` maps
composite-primary-key parts to a *path* (`["_id", elementName]`), not a name. The two disagreed. Added a private
static `GetSetElementKey(IProperty property)` helper next to `SerializeConstant` that asks
`BsonSerializerFactory.GetPropertySerializationInfo` for the authoritative location and joins `ElementPath` with
`.` when present, falling back to `ElementName` otherwise. Replaced all three `setter.Property.GetElementName()`
call sites in `BuildUpdate` (the constant-only `$set` branch, and both branches — self-referencing and constant —
of the pipeline-form `$set` stage) with `GetSetElementKey(setter.Property)`.

Note: the brief's Step 4 said "four uses" — there are only three in the pinned source (confirmed by grep before
editing), matching what the task instructions had already corrected. All three were replaced.

## Exact guard error messages

Each guard's `AddTranslationErrorDetails` message (surfaced inside EF's
`CoreStrings.NonQueryTranslationFailedWithDetails` wrapper, which is what `ex.Message` ultimately contains):

- Primary key: `The property '{DeclaringType}.{Name}' cannot be updated by a bulk update because it is part of the primary key, which maps to the immutable '_id' element.`
- Discriminator: `The property '{DeclaringType}.{Name}' cannot be updated by a bulk update because it is the type discriminator; changing it would change the entity type of the stored document.`
- Concurrency token: `The property '{DeclaringType}.{Name}' cannot be updated by a bulk update because it is a concurrency token. Bulk operations do not check concurrency tokens, so writing one would disable optimistic concurrency for later SaveChanges calls on the same document.`

Each message contains the exact substring its test asserts (`"primary key"`, `"discriminator"`,
`"concurrency token"`).

## `ElementPath`'s declared type and how it was handled

Decompiled `MongoDB.Bson.Serialization.BsonSerializationInfo` from the pinned driver (`3.10.0`, per
`Versions.props`) with `ilspycmd`. `ElementPath` is declared as `IReadOnlyList<string>` (backing field
`IReadOnlyList<string> _elementPath`), which is nullable-by-default and exposes `Count`, so the brief's
`info.ElementPath is { Count: > 0 } path ? string.Join('.', path) : info.ElementName` pattern compiles and behaves
exactly as written — no adaptation was needed. (Also confirmed `ElementName`'s getter throws if `_elementPath` is
non-null, which is why the ternary must check `ElementPath` first — it does.)

## `IsTypeDiscriminator`

Left `MongoPropertyExtensions.IsTypeDiscriminator` as `private`, per the brief's context note. `BuildSetter`
compares `property == entityType.FindDiscriminatorProperty()` directly instead, matching what
`IsTypeDiscriminator` does internally anyway.

## Test models reused vs. added

- **Reused:** `ExecuteUpdateTests.Order` (already has `ObjectId _id`) for the primary-key-guard test — no new
  model needed.
- **Added** (as the brief specifies, verified no equivalent already existed in `ExecuteUpdateTests.cs` or
  elsewhere in `FunctionalTests/Query/`): `VersionedOrder` + `ConfigureVersionedModel` (row-version/concurrency
  token case), and the `Animal`/`Cat`/`Dog` hierarchy + `ConfigureHierarchyModel` (discriminator case). No
  existing `BulkOperationsInheritanceTests`-style model in this file provided a discriminator-mapped hierarchy
  reachable from `ExecuteUpdateTests`, so these were added exactly as the brief's Step 1 specified, as nested
  private types alongside the existing `Order`/`OrderWithConverter` models.
- **Regression test model:** reused the existing `Order` class already declared in
  `BulkOperationsCommandTests.cs`; no new model type added there. The new test configures a
  `HasElementName("state")` mapping on `Order.Status` inline via a local `ConfigureCustomElementNameModel`
  function, mirroring the pattern in `ExecuteUpdateTests.ConfigureModel`.

## Regression test added (command-observability)

Added `Update_command_uses_the_property_element_name_when_it_has_a_custom_mapping` to
`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs`, following the file's
established `DiagnosticSourceSpy` + `SingleEntityDbContext.Create(..., sensitiveDataLogging: true)` pattern (it
opts into sensitive-data logging explicitly, same as every other command-content assertion in that file). It:

1. Seeds one `Order` **through the EF context** (not a raw driver `InsertOne`) with `Status.HasElementName("state")`
   configured, so the seeded document is actually stored under `state` — seeding via the raw driver would have
   stored it under the CLR property name `Status`, silently mismatching the custom mapping and producing a
   `MatchedCount == 0` that made the "Executing" and "Executed" event predicates collide (this was caught by a
   first failed run — `Single()` threw "Sequence contains more than one matching element" — before the fix).
2. Runs `ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"))`.
3. Asserts the captured `MongoBulkUpdateEventData.Command` contains `"state" : "shipped"` and does **not**
   contain `"Status"` anywhere — i.e. the `$set` key (and the `Where` filter, which shares the same entity
   serializer) both honor the custom element name, confirming `GetSetElementKey` didn't regress the ordinary
   (non-path) case.

## Conformance cases reclassified

**None.** `NorthwindBulkUpdatesMongoTest` (181 tests, EF10) and its EF9 run (169 tests via the SpecificationTests
filter above) both pass unchanged with the new guards in place — no Northwind bulk-update case sets a primary
key, discriminator, or concurrency-token property, so nothing needed to move from `base.*` to
`AssertTranslationFailed`.

## Test commands and output

All runs with `MONGODB_URI` and `ATLAS_URI` unset (TestContainers `mongodb/mongodb-atlas-local`).

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdate_targeting_the_primary_key_throws|FullyQualifiedName~ExecuteUpdate_targeting_the_discriminator_throws|FullyQualifiedName~ExecuteUpdate_targeting_a_concurrency_token_throws"
```
→ `Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3` (FunctionalTests.dll)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
```
→ `Passed! - Failed: 0, Passed: 41, Skipped: 0, Total: 41` (FunctionalTests.dll)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```
→ `Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181` (SpecificationTests.dll)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests"
```
→ `Passed! - Failed: 0, Passed: 18, Skipped: 1, Total: 19` (FunctionalTests.dll; the 1 skip is
`ExecuteDelete_two_phase_on_standalone_throws_actionable_error`, pre-existing and unrelated to this change)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```
→ FunctionalTests.dll: `Passed! - Failed: 0, Passed: 60, Skipped: 1, Total: 61`
→ SpecificationTests.dll: `Passed! - Failed: 0, Passed: 169, Skipped: 0, Total: 169`

Also confirmed `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"` succeeds (bulk code and both touched test
files are entirely inside `#if !EF8`, so nothing new runs under EF8, but the build must still be clean).

## `GetSetElementKey`'s path branch is unreachable by design

Confirmed via `BsonSerializerFactory.GetPropertySerializationInfo`: the only case that returns a
`BsonSerializationInfo` with a non-null `ElementPath` is `property.IsPrimaryKey() && ... FindPrimaryKey().Properties.Count > 1`
(a composite-key part). `BuildSetter`'s new `property.IsPrimaryKey()` guard now rejects every primary-key property
as a setter target before it ever reaches `BuildUpdate`, so `GetSetElementKey`'s `ElementPath` branch cannot be
hit through any currently-reachable path. This is expected and intentional (defence-in-depth for a future
property kind that might map to a path) — no test was contrived to fake reachability, and the code was not
skipped despite being currently unreachable, per the task's explicit instruction.

## Open question for the team (restated, unchanged by this task)

**Judgement call to confirm before merging:** rejecting a bulk setter on a concurrency token / row-version
property is a *behavior decision*, not a bug fix — relational EF providers permit this. The argument implemented
here for rejecting it: bulk operations already skip concurrency *checking* (documented in the README), so
permitting a bulk *write* to the token would silently disarm optimistic concurrency for every subsequent
`SaveChanges` on that document. If the team decides writes to a concurrency token should be permitted instead,
the fix is to delete the `property.IsConcurrencyToken` guard block in `BuildSetter` and its corresponding test
(`ExecuteUpdate_targeting_a_concurrency_token_throws` in `ExecuteUpdateTests.cs`), and record the decision as a
README bullet instead. This task implemented the guard as specified, per instruction that the confirmation is a
pre-merge step rather than a pre-implementation one — the decision is still open.

## Noticed but deliberately not changed

- The brief's Step 4 mentioned "four uses" of `GetElementName()` in `BuildUpdate`; only three exist in the pinned
  source. Handled per the task's correction — no fourth site was invented.
- Did not widen `MongoPropertyExtensions.IsTypeDiscriminator`'s accessibility, per explicit instruction; used the
  `entityType.FindDiscriminatorProperty()` comparison instead.
- Did not touch the plan document, `docs/failing-spec-tests.md`, the redaction gate in
  `MongoBulkOperationExecutor.RenderBulkCommand`, or any other production code outside `BuildSetter`,
  `RejectSetterTarget`, `GetSetElementKey`, and the three `$set`-keying call sites.
- Did not add any conformance-suite reclassification since none was triggered — `docs/failing-spec-tests.md`
  remains unchanged (that update is scoped to Task 25 and only applies if a reclassification happens).

---

## Fix round 1

Addressed three review findings. No change to the guards, their messages, `GetSetElementKey`'s body, or the
concurrency-token decision (still open — see restatement above).

### Important — `GetSetElementKey`'s doc comment didn't disclose the path branch is unreachable

Added a `<remarks>` block to `GetSetElementKey` in `MongoShapedQueryCompilingExpressionVisitor.cs` stating: the
`ElementPath` branch is unreachable today because the only property kind `GetPropertySerializationInfo` maps to a
path is a composite-primary-key part, and `BuildSetter`'s `IsPrimaryKey()` guard already rejects every
primary-key setter before reaching here; it's kept deliberately as defence-in-depth so the method stays the
single authority on "where does this property live," and it becomes load-bearing again if the primary-key guard
is ever removed or narrowed. The existing `<summary>` (what the method does) is unchanged.

### Minor 2 — regression test's limits weren't stated

Added a comment block above the assertion in
`Update_command_uses_the_property_element_name_when_it_has_a_custom_mapping`
(`BulkOperationsCommandTests.cs`) recording that: for any non-composite-key property, `ElementPath` is null so
`GetSetElementKey` falls back to `ElementName` — the same value `GetElementName()` returned pre-EF-361 — so this
test's "state" assertion would have passed unchanged before the migration too. It pins the correct,
currently-reachable behavior and would catch a regression that keys on `property.Name` instead of the mapped
element, but does not distinguish the `GetElementName()` → `GetSetElementKey()` migration and does not exercise
the `ElementPath`-joining branch at all. The assertion itself was not changed.

### Minor 3 — no test rooted the discriminator guard at a derived type

Added `ExecuteUpdate_targeting_the_discriminator_throws_when_scoped_to_a_derived_type` to
`ExecuteUpdateTests.cs`. Tried both forms the coordinator suggested before picking one:

- `db.Entities.OfType<Cat>().ExecuteUpdate(...)` — **rejected before reaching the discriminator guard.**
  `ClassifyBulkSource` only admits `Where`/`OrderBy`/`OrderByDescending`/`ThenBy`/`ThenByDescending`/`Skip`/`Take`/`Distinct`;
  `OfType` hits its `default` case and throws "The 'OfType' operator is not supported in a bulk delete or
  update." — confirmed by a throwaway test run (see exact message below) before writing the real test.
- `db.Entities.Where(a => a is Cat).ExecuteUpdate(...)` — **reaches the discriminator guard.** EF Core lowers
  `a is Cat` to a discriminator-equality predicate that arrives as an ordinary `Where`, which
  `ClassifyBulkSource` accepts (matching its own doc comment: "A TPH discriminator filter rides along as a
  Where"). The resulting exception message was: `The property 'Animal.Kind' cannot be updated by a bulk update
  because it is the type discriminator; changing it would change the entity type of the stored document.`

Used the `is`-predicate form for the shipped test, with a comment explaining why `OfType` isn't used and
cross-referencing `ClassifyBulkSource`'s doc comment. The property selector in the SetProperty call remains
`a => a.Kind` on the base `Animal` type (through the `Where(a is Cat)`-filtered `IQueryable<Animal>`), which is
sufficient to root the *query scope* at the derived type — the fix asked for scoping the query to a derived type,
not for a derived-typed `SetProperty` selector (EF's `SetProperty` signature ties the setter's declared type to
the source's element type either way).

### Covering tests re-run (EF10, `MONGODB_URI`/`ATLAS_URI` unset)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdate_targeting_the_primary_key_throws|FullyQualifiedName~ExecuteUpdate_targeting_the_discriminator_throws|FullyQualifiedName~ExecuteUpdate_targeting_a_concurrency_token_throws"
```
→ `Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4` (FunctionalTests.dll) — the three original guard tests
plus the new derived-type discriminator test (both discriminator tests match the `~ExecuteUpdate_targeting_the_discriminator_throws` substring).

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
```
→ `Passed! - Failed: 0, Passed: 42, Skipped: 0, Total: 42` (FunctionalTests.dll; 42 = the prior 41 plus the new
derived-type discriminator test — the two throwaway `_TEMP_*` probe tests used to determine the `OfType` vs.
`is`-predicate question were removed before this run).

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```
→ `Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181` (SpecificationTests.dll) — unchanged.

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```
→ `Build succeeded. 0 Warning(s), 0 Error(s)`.

### Files touched in this round

- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` (doc comment only)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs` (comment only)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` (one new test)

No production behavior changed.

---

## Follow-up: concurrency-token decision resolved (guard removed)

Arthur decided the open judgement call: **bulk setters on concurrency tokens are permitted, not rejected.**
Relational EF allows this, and deliberately stamping a new token value via a bulk update is a legitimate use
case. The decision is recorded in the plan document (`docs/superpowers/plans/2026-07-30-ef-107-bulk-remediation.md`,
commit `832ad8b` — not edited by this task). This section covers the four follow-up changes.

### 1. Removed the `IsConcurrencyToken` guard

Deleted the `if (property.IsConcurrencyToken) { RejectSetterTarget(...) }` block from `BuildSetter` in
`MongoQueryableMethodTranslatingExpressionVisitor.cs`. The primary-key and discriminator guards are untouched.
Updated the comment above the remaining two guards, which previously said "or silently disarm optimistic
concurrency for every later SaveChanges on the document" (describing the now-removed third case); it now reads:

> Reject setters on fields the provider owns. Without these guards a bulk update can rewrite a document's type
> (the discriminator maps to _t) or attempt an immutable-_id write that only fails at the server. (Concurrency
> tokens are deliberately NOT guarded here: bulk operations bypass the change tracker and so neither check nor
> protect concurrency tokens, and explicitly setting one via a bulk update is a supported use case, not an
> oversight.)

### 2. Replaced the rejection test with a positive test

Deleted `ExecuteUpdate_targeting_a_concurrency_token_throws` and added
`ExecuteUpdate_can_set_a_concurrency_token` in its place in `ExecuteUpdateTests.cs`. It reuses the existing
`VersionedOrder` model and `ConfigureVersionedModel` helper (both were already present from the earlier guard
test and needed no changes). The test:

1. Seeds one `VersionedOrder` through the EF context with an initial `Version` byte array.
2. Runs `db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Version, newVersion))` and asserts the affected-row
   count is 1.
3. Re-reads the document through a **fresh** `DbContext` instance and asserts `stored.Version` equals the new
   byte array exactly — pinning the persisted value, not just the row count, so the test would fail if
   `GetSetElementKey`/`BuildUpdate` silently dropped the token from the `$set` document (e.g. if a future guard
   or key-mapping regression skipped it) rather than actually writing it.

A comment on the test records the decision is deliberate: bulk operations bypass the change tracker, so unlike
`SaveChanges` they neither check nor protect concurrency tokens, and setting one explicitly is a supported use
case rather than an oversight.

### 3. README wording

`README.md:98`, before:
> ...bypasses the change tracker (concurrency tokens are not checked)

after:
> ...bypasses the change tracker (concurrency tokens are neither checked nor protected — a bulk update may set
> one explicitly)

Minimal edit to the existing parenthetical only; the surrounding bullet list and its sub-bullets (atomic vs.
two-phase execution, transaction enlistment) were left untouched, per instruction that a later task rewrites the
whole bulk section.

### 4. Grep for stragglers

Grepped `src/` and `tests/` (excluding `bin/`/generated XML doc output, which only contain unrelated
`IsConcurrencyToken` XML-doc noise from EF Core's own assembly comments) for `IsConcurrencyToken` and for the
removed guard's message text ("would disable optimistic concurrency", "disable optimistic concurrency"). Found
no other references to the removed guard:

- `Storage/RowVersion.cs`, `Storage/MongoUpdate.cs`, `Infrastructure/MongoModelValidator.cs`, and
  `UnitTests/Infrastructure/MongoModelValidatorTests.cs` all use `IsConcurrencyToken` for **`SaveChanges`-path**
  concerns (row-version generation, the `_id` + concurrency-token update filter, and the owned-entity
  concurrency-token validator) — unrelated to the bulk-update guard removed here, and correctly left alone.
- No other test or source file referenced the removed guard's rejection message.
- Did not touch `docs/failing-spec-tests.md` or the plan document, per instruction.

### Test commands and output (EF10, `MONGODB_URI`/`ATLAS_URI` unset)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
```
→ `Passed! - Failed: 0, Passed: 43, Skipped: 0, Total: 43` (FunctionalTests.dll) — includes the new
`ExecuteUpdate_can_set_a_concurrency_token` test, the two remaining guard tests (primary key, discriminator —
both the root and the derived-type-scoped variant added in fix round 1), and all `BulkOperationsCommandTests`.

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```
→ `Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181` (SpecificationTests.dll) — unchanged; removing the
concurrency-token guard cannot break Northwind (it never exercised it), and the count is confirmed still 181/181
rather than assumed.

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```
→ `Build succeeded. 0 Warning(s), 0 Error(s)`.

### Files touched in this follow-up

- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` (removed
  guard + updated comment)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` (rejection test replaced with
  positive test)
- `README.md` (one parenthetical wording change)

No changes to `docs/failing-spec-tests.md` or the plan document.
