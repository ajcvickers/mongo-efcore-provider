# Task 8 report: Enforce the single-`TSource`-parameter precondition when rebinding setter values

## What changed and why

`ParameterRebindingExpressionVisitor` (in
`src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`) rebinds every
parameter of the entity type onto a single shared parameter, by type rather than by identity — because
translation through `MongoEFToLinqTranslatingExpressionVisitor` discards the original parameter identity. Its own
doc comment stated the precondition this relies on: exactly one parameter of the entity type in the value body.
Nothing enforced that. If a second entity-typed parameter appeared (e.g. a self-referencing navigation collection
used inside an aggregate, `e.DirectReports.Max(r => r.Rank)`), both `e` and `r` would collapse onto the same
outer parameter and the setter would render as something like `Max(_ => e.Rank)` — silently wrong, no exception.

Added `internal static Expression RebindSetterValueParameters(Expression body, ParameterExpression target)` as
the new entry point, replacing the direct `ParameterRebindingExpressionVisitor` instantiation at the call site
(`RenderSelfReferencingValue`, was line 596). It first walks the body with a new private
`EntityParameterCollector : ExpressionVisitor` that collects distinct `ParameterExpression`s whose `Type` equals
`target.Type` into a `HashSet<ParameterExpression>`. If more than one distinct parameter is found, it throws
`InvalidOperationException` before any rebinding happens. Otherwise it delegates to the (unchanged)
`ParameterRebindingExpressionVisitor` exactly as before. `ParameterRebindingExpressionVisitor` itself, its
`VisitParameter` override, and the `RenderAggregateExpression`/`GetSetElementKey`/redaction-gate code around it
were left untouched.

## Exception message and `ShortDisplayName()` vs. `Name`

Checked the file's `using` directives first — no `Microsoft.EntityFrameworkCore.Internal` using is present
anywhere in `MongoShapedQueryCompilingExpressionVisitor.cs` (only `Microsoft.EntityFrameworkCore`,
`Microsoft.EntityFrameworkCore.Diagnostics`, `Microsoft.EntityFrameworkCore.Infrastructure`,
`Microsoft.EntityFrameworkCore.Metadata`, `Microsoft.EntityFrameworkCore.Query`). Per the brief's fallback
instruction, used `target.Type.Name` rather than adding an internal-namespace using.

Exact message produced (confirmed via the reachability probe below, with `TSource = StaffMember`):

```
A self-referencing bulk update setter value referenced more than one 'StaffMember' parameter, which the MongoDB provider cannot render as a single aggregation expression.
```

The unit test only asserts `Assert.Contains("more than one", ex.Message)`, so the `Name` vs. `ShortDisplayName()`
choice doesn't affect test outcomes for non-generic types like the ones in the brief's tests and this repo's
entity models.

## Reachability: is the two-entity-parameter shape reachable through the public API?

**Yes, confirmed live and reachable on EF10** (and by the same mechanism, EF9 — the guard code is `#if !EF8` and
this doesn't depend on the EF10-only cross-collection navigation feature at the query-translation layer; the
*model shape* used to prove it happens to be gated `#if !EF8 && !EF9` elsewhere in this codebase's own tests, but
that gate is about an unrelated cross-collection `Include` feature, not about whether this setter shape can be
built).

Tried a same-CLR-type owned collection first (`Employee.Reports : List<Employee>` configured via `OwnsMany`) —
EF Core's model builder itself rejects this up front (`InvalidOperationException`: "The entity type 'Employee'
cannot be configured as owned because it has already been configured as a non-owned"), because the same type
can't simultaneously be a `DbSet<T>` root and an owned type of itself. So the brief's suggested "owned collection"
framing isn't literally constructible.

The reachable shape instead uses a **self-referencing one-to-many reference navigation within the same
collection**, exactly the pattern already used elsewhere in this test suite for cross-collection includes
(`StaffMember`/`DirectReports`/`ManagerId` in `CrossCollectionIncludeTests.cs`):

```csharp
class StaffMember
{
    public ObjectId _id { get; set; }
    public int Rank { get; set; }
    public ObjectId? ManagerId { get; set; }
    public StaffMember Manager { get; set; }
    public List<StaffMember> DirectReports { get; set; }
}
// modelBuilder: b.HasOne(s => s.Manager).WithMany(s => s.DirectReports).HasForeignKey(s => s.ManagerId);

db.Entities.Where(s => s.EmployeeName == "Boss")
    .ExecuteUpdate(s => s.SetProperty(e => e.Rank, e => e.DirectReports.Max(r => r.Rank)));
```

Ran this (in a throwaway, non-committed probe test, deleted before the final commit) **before** this task's fix:
it did not throw — it built and executed successfully, meaning it silently rendered the wrong aggregation (both
`e` and `r` collapsed onto the same parameter, so the translated `Max` had no free variable to range over and
degenerated to `e.Rank` itself — a no-op "update" rather than the true max of `DirectReports`). This is exactly
the failure mode the task brief describes as the motivating risk, and it is real, not hypothetical.

After this task's fix, the same code throws:

```
System.InvalidOperationException : A self-referencing bulk update setter value referenced more than one 'StaffMember' parameter, which the MongoDB provider cannot render as a single aggregation expression.
```

at `RebindSetterValueParameters` → `RenderSelfReferencingValue` → `BuildUpdate` → `CreateBulkPlan` →
`MongoBulkOperationExecutor.ExecuteUpdate`, confirmed by stack trace.

This is a live, previously-silent-wrong-result bug that this task's guard now catches. The probe test file was
deleted (not committed) after confirming the behavior; it is not part of this task's deliverable and is not
referenced by the committed diff.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` — replaced the
  `ParameterRebindingExpressionVisitor` instantiation at the `RenderSelfReferencingValue` call site with
  `RebindSetterValueParameters(translatedBody, entityParameter)`; added `RebindSetterValueParameters` and the new
  private `EntityParameterCollector` class; kept `ParameterRebindingExpressionVisitor` as a private nested class,
  unchanged except that its own doc comment (which stated the now-enforced precondition) was folded into
  `RebindSetterValueParameters`'s doc comment instead.
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkSetterRebindingTests.cs` — new file, exactly as the
  brief's Step 1 specifies (three tests: single-parameter rebind, unrelated-type parameters left alone, two
  distinct entity-typed parameters throws). Per the parent's context note, omitted redundant `using System;` and
  `using Xunit;` (both global via `ImplicitUsings`/`Usings.cs` in this test project), kept
  `using System.Linq.Expressions;`, `using MongoDB.Bson;`, and
  `using MongoDB.EntityFrameworkCore.Query.Visitors;` since none of those are global.
- Did **not** touch `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs`,
  per the parent's explicit instruction to ignore that part of the brief's confused Files list.

## Test commands and output

All runs with `MONGODB_URI` and `ATLAS_URI` unset (TestContainers boots `mongodb/mongodb-atlas-local`; the image
was already present locally, so no pull was needed).

**EF10 — new unit tests:**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~BulkSetterRebindingTests"
```
→ `Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3` (UnitTests.dll) — the three tests named in the brief:
`Rebinds_a_single_entity_parameter_onto_the_target`, `Leaves_parameters_of_other_types_alone`,
`Throws_when_the_body_has_two_distinct_entity_parameters`.

**EF10 — self-referencing functional tests + BulkOperationsCommandTests:**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
```
→ `Passed! - Failed: 0, Passed: 42, Skipped: 0, Total: 42` (FunctionalTests.dll) — includes
`ExecuteUpdate_self_referencing_increments_matched_docs`, `ExecuteUpdateAsync_self_referencing_increments_matched_docs`,
`ExecuteUpdate_mixed_constant_and_self_referencing_in_one_call`, and `ExecuteUpdate_two_phase_self_referencing_setter`,
all passing under the new `RebindSetterValueParameters` guard (each has exactly one entity-typed parameter in its
setter value, so the guard is a no-op for them, as expected).

**EF10 — Northwind bulk-update conformance suite:**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```
→ `Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181` (SpecificationTests.dll) — unchanged.

**EF9 — same three filters:**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~BulkSetterRebindingTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```
→ UnitTests.dll: `Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3`
→ FunctionalTests.dll: `Passed! - Failed: 0, Passed: 43, Skipped: 0, Total: 43`
→ SpecificationTests.dll: `Passed! - Failed: 0, Passed: 169, Skipped: 0, Total: 169`

**EF8 — build only (bulk code and both touched test files are inside `#if !EF8`, so no bulk tests exist to run
under EF8; confirmed the filter matches zero tests, not that it silently skipped):**
```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```
→ `Build succeeded. 0 Warning(s), 0 Error(s)`
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" --no-build --filter "FullyQualifiedName~BulkSetterRebindingTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```
→ "No test matches the given testcase filter" for all three test DLLs (expected — everything relevant is guarded
by `#if !EF8`).

## Noticed but deliberately not changed

- `ParameterRebindingExpressionVisitor`'s own `VisitParameter` body and its by-type rebinding strategy are
  unchanged — only the precondition around it is now enforced. Not touched: `RenderAggregateExpression`,
  `GetSetElementKey`, the redaction gate in `Storage/MongoBulkOperationExecutor.RenderBulkCommand`, or any other
  code outside the new entry point and its call site.
- Did not attempt to make the two-entity-parameter self-referencing-navigation shape *work* (e.g. by rendering a
  driver-native `$max` over the embedded/looked-up array with correct parameter scoping) — the task brief
  explicitly scopes this to converting silent wrong results into a loud, actionable error, not to widening
  supported setter-value shapes. That would be new feature work with its own translation-correctness questions
  (does the driver's aggregation-expression renderer even support a nested lambda parameter distinct from the
  outer one at all under `$set`?) and is out of scope here.
- Did not create or modify `MongoNonQueryExpressionUnwrapTests.cs`, per explicit instruction.
- The throwaway probe file used to confirm real-world reachability (`ScratchProbeTests.cs`, in
  `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/`) was deleted before running the final test sweep and
  before committing — it is not part of the diff and was never staged.
- Left the plan document, `.superpowers/sdd/.../task-8-brief.md`, and `MongoEventId` untouched, per the global
  constraints.

---

## Fix round 1

Addressed four review findings. The guard's placement, its by-type filtering, its identity-based dedup
(`HashSet<ParameterExpression>`), its accessibility, and its `#if !EF8` gating were all explicitly approved and
left unchanged. No change to `GetSetElementKey`, the setter guards from the previous task, or the redaction gate.

### Important 1 — committed the reachability evidence as a durable functional test

The prior round's central claim ("this shape is reachable and previously rendered a silent no-op") rested only on
a probe test that was run once and deleted — nothing in the repo could reproduce it. Fixed by adding
`ExecuteUpdate_self_referencing_setter_with_nested_entity_typed_parameter_throws` to
`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`, next to
`ExecuteUpdate_self_referencing_value_converter_property_throws`.

`StaffMember`/`DirectReports`/`ManagerId` in `CrossCollectionIncludeTests.cs` is a `private` nested class, so it
isn't reachable from `ExecuteUpdateTests.cs` — added an equivalent model (`Employee`/`DirectReports`/`ManagerId`,
same shape: a self-referencing one-to-many navigation within one collection, configured via
`HasOne(e => e.Manager).WithMany(e => e.DirectReports).HasForeignKey(e => e.ManagerId)`) as its own private nested
type in `ExecuteUpdateTests.cs`, with a comment cross-referencing the original. The test:

1. Seeds one manager `Employee` and two direct reports (`Rank` 5 and 9) via the FK.
2. Runs `db.Entities.Where(e => e._id == manager._id).ExecuteUpdate(s => s.SetProperty(e => e.Rank, e => e.DirectReports.Max(r => r.Rank)))`.
3. Asserts `InvalidOperationException` and that the message contains `"more than one"`, `"Employee"`, and
   `"Rank"`.

A comment above the test records the pre-fix silent-no-op fact directly (not just in this report): before
`RebindSetterValueParameters` existed, `e` (the setter's own parameter) and `r` (the nested `Max` lambda's
parameter) are both type `Employee`, so type-based rebinding collapsed both onto the shared parameter; the
aggregation then had no free variable to range over and degenerated to `e.Rank` — a no-op "update" of `Rank` to
its own current value, instead of the true max of `DirectReports`, with no exception.

**Configuration note asked for explicitly:** this fixture does **not** need to be restricted to one EF
configuration. Unlike `CrossCollectionIncludeTests.cs`'s own use of this shape (gated `#if !EF8 && !EF9` there,
because that file's tests actually execute the cross-collection `$lookup` translation), this test never reaches
that machinery — `RebindSetterValueParameters` throws while still inside setter-value translation, before any
navigation/`$lookup` rendering would be attempted. Confirmed empirically: built and ran this exact test under both
`Debug EF9` and `Debug EF10` before finalizing it, and it passes on both (see below) — so it lives directly in
`ExecuteUpdateTests.cs`'s ordinary `#if !EF8` region with no additional guard.

### Important 2 — threaded the property through so the message names the offending setter

`RebindSetterValueParameters`'s signature changed from `(Expression body, ParameterExpression target)` to
`(Expression body, ParameterExpression target, IProperty property)`. The call site in `RenderSelfReferencingValue`
now passes `setter.Property`. `property` is used only in the throw branch, to build
`{property.DeclaringType.DisplayName()}.{property.Name}` — the exact `DeclaringType.DisplayName()`/`Name` pattern
already used by `RejectSetterTarget` elsewhere in this file (task 7), for consistency.

**Exact new message** (captured verbatim from a live run of the new functional test, by temporarily forcing an
assertion failure to print `ex.Message` in full, then reverting):

```
The self-referencing bulk update setter for 'Employee.Rank' referenced more than one 'Employee' parameter, which the MongoDB provider cannot render as a single aggregation expression; compute the value first and pass it as a constant/parameter, or perform the update via the change tracker instead.
```

One sentence, names the offending setter (`Employee.Rank`), keeps the entity-type name in the "more than one '...'
parameter" clause (which both the new unit test and the new functional test assert on independently), and states
two concrete alternatives.

The doc comment on `RebindSetterValueParameters` gained one sentence noting `property` exists only to let the
exception name the offending `SetProperty` call — no other doc-comment content changed.

### Minor 3 — strengthened `Leaves_parameters_of_other_types_alone`

The body now mixes an `Order`-typed parameter (`other`) with a differently-typed one (`unrelated`, `int`) in the
same `Add` expression, instead of only ever containing the unrelated parameter. The test now asserts both halves:
the `Order`-typed one rebinds to `target` (`Assert.Same(target, ((MemberExpression)rebound.Left).Expression)`) and
the `int`-typed one is untouched (`Assert.Same(unrelated, rebound.Right)`). A collector that (bug) omitted the
type filter and rebound everything would now fail this test, which the prior version could not catch.

### Minor 4 — added an occurrences-vs-distinct-parameters test

Added `Rebinds_repeated_references_to_the_same_parameter`: a body referencing the *same* `ParameterExpression`
instance twice (`other.Quantity + other.Quantity`, mirroring a setter value like `o.Quantity + o.Quantity`) must
rebind successfully, not throw — pinning that the `HashSet<ParameterExpression>` collector dedupes by identity
rather than counting occurrences.

### Unit-test fixture change (needed for Important 2, incidental to Minor 3/4)

All three original tests (`Rebinds_a_single_entity_parameter_onto_the_target`,
`Leaves_parameters_of_other_types_alone`, `Throws_when_the_body_has_two_distinct_entity_parameters`) plus the two
new ones now pass a third argument, `QuantityProperty` — a real `IProperty` (not a fake/mock), obtained the same
way `BsonBindingTests.cs` already does it elsewhere in this project: run `MongoConventionSetBuilder.Build()` over
a plain `ModelBuilder`, add `Order`, and read back `IEntityType.GetProperty(nameof(Order.Quantity))`. This
required casting the result from `IMutableProperty` to `IProperty` (`IMutableProperty` does not itself implement
`IProperty` in this EF metadata hierarchy — confirmed by a compiler error, `CS0266`, that named the explicit
conversion). Added `using Microsoft.EntityFrameworkCore;`, `using Microsoft.EntityFrameworkCore.Metadata;`, and
`using MongoDB.EntityFrameworkCore.Metadata.Conventions;` to the test file for this; none of these are covered by
the project's global usings.

### Covering tests re-run

All runs with `MONGODB_URI` and `ATLAS_URI` unset (TestContainers `mongodb/mongodb-atlas-local`; image already
present locally).

**EF10 — new/changed unit tests (now 4, not 3):**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~BulkSetterRebindingTests"
```
→ `Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4` (UnitTests.dll) —
`Rebinds_a_single_entity_parameter_onto_the_target`, `Leaves_parameters_of_other_types_alone` (strengthened),
`Rebinds_repeated_references_to_the_same_parameter` (new), `Throws_when_the_body_has_two_distinct_entity_parameters`.

**EF10 — the four self-referencing functional tests, the new functional test, and `BulkOperationsCommandTests`:**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
```
→ `Passed! - Failed: 0, Passed: 43, Skipped: 0, Total: 43` (FunctionalTests.dll) — 42 from before plus
`ExecuteUpdate_self_referencing_setter_with_nested_entity_typed_parameter_throws`. Confirmed by name that
`ExecuteUpdate_self_referencing_increments_matched_docs`,
`ExecuteUpdateAsync_self_referencing_increments_matched_docs`,
`ExecuteUpdate_mixed_constant_and_self_referencing_in_one_call`, and
`ExecuteUpdate_two_phase_self_referencing_setter` are all included in this run and none regressed.

**EF10 — Northwind bulk-update conformance suite:**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```
→ `Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181` (SpecificationTests.dll) — unchanged.

**EF9 — spot-check that the new functional test's fixture (self-referencing FK navigation) doesn't depend on the
EF10-only cross-collection `$lookup` feature, run in isolation before the full-suite run above:**
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~ExecuteUpdate_self_referencing_setter_with_nested_entity_typed_parameter_throws"
```
→ `Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1` (FunctionalTests.dll).

**EF8 — build only (nothing changed about what's excluded; the whole file stays under `#if !EF8`):**
```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```
→ `Build succeeded. 0 Warning(s), 0 Error(s)`.

### Files touched in this round

- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` —
  `RebindSetterValueParameters` signature gained `IProperty property`; its call site in
  `RenderSelfReferencingValue` passes `setter.Property`; the exception message now names the setter
  (`DeclaringType.Name`) and states two remedies; one added sentence in the doc comment. Guard placement,
  filtering, and dedup logic unchanged.
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkSetterRebindingTests.cs` — added the `QuantityProperty`
  fixture and its three required `using`s; updated all call sites to the three-argument form; strengthened
  `Leaves_parameters_of_other_types_alone`; added `Rebinds_repeated_references_to_the_same_parameter`; the
  distinct-parameters test now also asserts the message names `Order` and `Quantity`.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` — added the `Employee` model,
  `ConfigureSelfReferencingNavigationModel`, one new `using` (`System.Collections.Generic`), and the new
  `ExecuteUpdate_self_referencing_setter_with_nested_entity_typed_parameter_throws` test.

No change to `GetSetElementKey`, the setter-target guards from the previous task (primary key / discriminator /
concurrency token), or `Storage/MongoBulkOperationExecutor`'s redaction gate.

### Noticed but deliberately not changed (this round)

- Did not add an EF-version guard around the new functional test's `Employee` model — confirmed by running it
  under both EF9 and EF10 that none is needed, rather than copying the `#if !EF8 && !EF9` gate from
  `CrossCollectionIncludeTests.cs` on the assumption that it would apply here too.
- Did not change `ParameterRebindingExpressionVisitor`'s `VisitParameter` body, `EntityParameterCollector`, or
  the `#if !EF8` gating anywhere — the review approved all of these as-is.
- Did not attempt to make the two-entity-parameter shape *work*; the fix remains scoped to turning the silent
  wrong result into a loud, actionable, reproducible-in-CI error.
