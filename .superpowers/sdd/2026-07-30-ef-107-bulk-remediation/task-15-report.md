# Task 15 Report: Support `OfType` (not `Cast`) via the two-phase read path

## Status: DONE_WITH_CONCERNS (see Cast verdict — a deliberate scope reduction from the brief, not a defect)

## What was implemented

`ClassifyBulkSource` (`src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`)
now admits `QueryableMethods.OfType` as a **two-phase** operator, alongside `Skip`/`Take`:

```csharp
else if (definition == QueryableMethods.OfType)
{
    // Type scoping is expressed by the driver as a { _t: ... } filter, which only the read path
    // knows how to build. Route through two-phase so phase 1 reuses that translation rather than
    // reimplementing discriminator filtering in BuildFilter — getting that wrong on a delete would
    // destroy documents of the wrong type.
    //
    // Cast is deliberately not admitted here even though it looks like the same shape: unlike
    // OfType, this provider's TranslateCast does not narrow by type at all (it is a pure shaper
    // reshape), and routing Cast<TDerived>() through this same two-phase path throws
    // ExpressionNotSupportedException out of the driver's LINQ translator with no command ever
    // reaching the wire -- confirmed empirically, see BulkOperationsInheritanceTests. So Cast falls
    // through to the rejection below, same as before this change.
    requiresTwoPhase = true;
}
```

`Cast` is **not** admitted — see verdict below. It falls through to the existing `ThrowBulkSourceNotSupported` rejection, unchanged in effect from before this task.

`AffectedSetNeutralDefinitions` / `BuildFilter` (single-command path) were **not** touched, per Correction 1 — `OfType` narrows the affected set, so it must never be treated as neutral. `BuildFilter` continues to run only on the single-command path (`CreateBulkPlan` sets `BuildFilter = isTwoPhase ? null : ...`, unchanged).

Correction 2 (no reflection fallback): confirmed empirically via reflection over the actual pinned assemblies (not just source inspection) that `QueryableMethods.OfType` and `QueryableMethods.Cast` (1-generic-parameter `IQueryable` overload) exist as public static properties on both `Microsoft.EntityFrameworkCore.dll` 9.0.16 (EF9, `net8.0`) and 10.0.8 (EF10, `net10.0`) — the exact versions pinned in `Versions.props`. No reflection fallback needed; used the canonical constants directly.

## The `Cast` verdict: NOT admitted — evidence

This is the key finding of the task, and it overrides the brief (which admitted `Cast` with zero test coverage on an unverified assumption).

**Source-level evidence.** `TranslateCast` (read path, same file, line ~613) is:
```csharp
protected override ShapedQueryExpression TranslateCast(ShapedQueryExpression source, Type castType)
    => ReshapeShaperExpression(source, castType);
```
This is a pure shaper reshape — it does **not** swap the shaper's structural/entity type the way `TranslateOfType` does (`entityShaperExpression.WithType(resultEntityType)`), so it cannot be the mechanism that produces a `{ _t: ... }` discriminator filter. `Cast` was never going to filter by type in this provider, independent of the two-phase question.

**Empirical evidence — the wire.** Built a throwaway test attaching a `CommandStartedEvent` subscriber (`MongoDB.Driver.Core.Events`) to a dedicated `MongoClient`, seeded 2 Cat + 1 Dog into a TPH hierarchy, then ran `db.Entities.Cast<Cat>().ExecuteDelete()` with `Cast` temporarily admitted into the classifier. Result: **no `aggregate` command ever reached the wire.** The call threw:

```
System.InvalidOperationException: The LINQ expression 'DbSet<Animal>()
    .Cast<Cat>()
    .ExecuteDelete()' could not be translated. Additional information: Expression not supported:
    EFTest-...-1.SCRATCH....Aggregate([]).As(value(...EntitySerializer`1[...Animal])).Cast().
 ---> MongoDB.Driver.Linq.ExpressionNotSupportedException: Expression not supported: ...Aggregate([]).As(...).Cast().
   at MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators.AsMethodToPipelineTranslator.Translate(...)
   ...
   at MongoDB.EntityFrameworkCore.Storage.MongoBulkOperationExecutor.MaterializeIds(...)
```

The driver's own LINQ v3 translator rejects the `.As(serializer).Cast()` shape outright — `MaterializeIds`'s narrow `catch (ExpressionNotSupportedException)` catches it and EF's canonical "could not be translated" wrapper surfaces. This is **not** "phase 1 doesn't filter by type" (which would be a silent-data-destruction risk); it's a hard translation failure with no command sent at all. Confirmed with the OfType equivalent still succeeding in the same run, isolating the difference to `Cast` itself.

**Decision per Correction 3's explicit branch:** "If Cast does not produce a discriminator filter... stop... do not ship Cast. Implement OfType only, leave Cast rejected." Cast produces no filter (nor any translated query at all), so I reverted the `Cast` branch, kept only `OfType`, and restored the pre-task rejection message text (with `OfType` now added to the admitted-operator enumeration, `Cast` left off). Rejecting `Cast` up front at the classifier also gives a strictly better error message (the clean "not supported in a bulk delete or update" vs. the confusing driver-internal `ExpressionNotSupportedException` message a user would otherwise get) — so leaving it rejected is also the better UX, not just the safer one.

The temporary wire-capture/exception-repro tests used to gather this evidence were removed before committing; the committed test suite instead has a permanent `Assert.Throws` test (`ExecuteDelete_with_Cast_is_rejected_by_the_bulk_source_classifier`) pinning that `Cast` stays rejected, so a future accidental re-admission is caught.

## Confirmation that phase 1 emits the `_t` filter for `OfType` — the rendered command

Same wire-capture technique, kept as a permanent test this time (`ExecuteDelete_with_OfType_phase1_query_carries_the_discriminator_filter_on_the_wire`): seeded 2 Cat + 1 Dog, ran `db.Entities.OfType<Cat>().ExecuteDelete()` through a client with a `CommandStartedEvent` subscriber, and captured the actual `aggregate` command phase 1 sent:

```json
{
  "aggregate": "SCRATCH_capture_phase1_wire_command_for_OfType",
  "pipeline": [{ "$match": { "_t": "cat" } }],
  "cursor": {},
  "$db": "EFTest-...",
  "lsid": { "id": { "$binary": { ... } } },
  "txnNumber": 1,
  "startTransaction": true,
  "autocommit": false
}
```

Phase 1's pipeline is exactly `[{ "$match": { "_t": "cat" } }]` — the discriminator filter survives `CreateQuery<BsonDocument>`/`BuildIdDocumentQuery`'s reinterpretation intact. The stop-condition in the task ("if phase 1 does not emit the `_t` filter... revert the classifier change") does **not** apply for `OfType`. Deleted 2, left the Dog — matches the pipeline.

## TDD evidence

**RED** — before Step 3, ran:
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~BulkOperationsInheritanceTests"
```
Result: 3 failed, 1 passed.
- `ExecuteDelete_with_OfType_deletes_only_that_type`: `InvalidOperationException`... "The 'OfType' operator is not supported in a bulk delete or update. Only 'Where' predicates and the 'OrderBy', ..., 'Skip', 'Take', and 'Distinct' operators can scope a bulk operation."
- `ExecuteUpdate_with_OfType_updates_only_that_type`: same message, `'OfType'`.
- `ExecuteDelete_with_Cast_deletes_only_that_type` (working version, pre-verdict): same message, `'Cast'`.
- `ExecuteDelete_with_an_is_predicate_deletes_only_that_type_on_the_single_command_path`: **passed** (single-command path, unaffected by the classifier gap).

This matched expectations exactly (classifier rejection, `is`-predicate test unaffected).

**GREEN** — after Step 3 (and after settling the Cast verdict, converting that test to `Assert.Throws`):
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~BulkOperationsInheritanceTests|FullyQualifiedName~DiscriminatorTests"
```
Result: `Passed! - Failed: 0, Passed: 31, Skipped: 0, Total: 31` (6 in the new file + 25 in `DiscriminatorTests`).

EF9:
```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~BulkOperationsInheritanceTests"
```
Result: `Passed! - Failed: 0, Passed: 5, Skipped: 0, Total: 5`.

## Full test results

- `NorthwindBulkUpdatesMongoTest`, EF10: **181/181** (matches stated baseline).
- `NorthwindBulkUpdatesMongoTest`, EF9: **169/169** (matches stated baseline).
- Full Northwind spec suite, EF10: 4385 passed, 23 skipped, 0 failed.
- Full Northwind spec suite, EF9: 4793 passed, 20 skipped, 0 failed.
- Full `FunctionalTests` assembly, EF10: 1793 passed, 52 skipped (Docker/Atlas/encryption-gated), 0 failed.
- `ExecuteUpdateTests` / `ExecuteDeleteTests` / `BulkOperationsCommandTests` / `BulkOperationsInheritanceTests` together, EF10: 82 passed, 1 skipped (standalone-deployment test, expected), 0 failed.
- Build: `Debug EF8`, `Debug EF9`, `Debug EF10` all succeed, 0 warnings, 0 errors (re-verified after the final cosmetic cleanup pass on the test file).

No conformance test changed status; `NorthwindBulkUpdatesMongoTest.cs` has no `OfType` usage, consistent with the pre-check.

## Stale comments found and fixed (Correction 4)

1. **The named site** — `tests/.../ExecuteUpdateTests.cs`, `ExecuteUpdate_targeting_the_discriminator_throws_when_scoped_to_a_derived_type` (~line 432). Old comment claimed an explicit `OfType<Cat>()` "is rejected earlier by the bulk-source classifier, which only admits Where/OrderBy/Skip/Take/Distinct" — both false now (OfType is admitted) and already stale before this task (omitted `ThenBy`/`Order`/`OrderDescending`, added by Task 14). Rewrote to state `OfType<Cat>()` is now admitted too but takes the two-phase path rather than this test's single-command one, and pointed at `BulkOperationsInheritanceTests` for that coverage. Did not change the test body (kept the `Where(a is Cat)` shape, as instructed).

2. **`ThrowBulkSourceNotSupported`'s message enumeration** (`MongoQueryableMethodTranslatingExpressionVisitor.cs`) — listed the admitted operators for the generic "not supported" error; added `'OfType'` to the list (and correctly left `'Cast'` off, per the verdict).

3. **`ClassifyBulkSource`'s XML doc comment** — extended to describe `OfType`'s two-phase admission and explicitly document why `Cast` is excluded (previously only Skip/Take were described post-Task-14; Task 14 had already added a forward-looking note about `OfType`/`Cast` to `AffectedSetNeutralDefinitions`'s doc, which I left as-is since it was already correct — it doesn't claim they're neutral, it explains why they're *not* in that set).

4. **`EnsureBulkKeyOrThrow`'s error message** (`MongoShapedQueryCompilingExpressionVisitor.cs`) — this guard runs for *any* two-phase strategy (checked via `nonQueryExpression.Strategy == TwoPhase`), not just Skip/Take paging, but its message hardcoded "...to use paging ('Skip'/'Take') in a bulk delete or update." A keyless entity now reaching two-phase via `OfType` would have hit this guard with a message blaming paging it never used. Fixed to "...to use paging ('Skip'/'Take') or an 'OfType' type filter in a bulk delete or update." This is a genuine (if narrow) finding this task's classification change exposed, not just a comment.

5. **`BuildIdDocumentQuery`'s comment** — listed the read-path operators it reuses ("Where/OrderBy/Skip/Take/Distinct"); added `OfType` and a pointer to why reuse is what makes the discriminator filter come for free.

Swept `src/` and `tests/` for other claims that OfType/Cast are rejected or that enumerate admitted bulk operators (`grep` for "bulk-source classifier", "only admits", "rejected earlier", "is not supported in a bulk"); no other stale sites found. `Query/AGENTS.md` doesn't mention the classifier's admitted-operator list at all, so nothing to fix there.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` — classifier admits `OfType`, doc comment updated, rejection-message enumeration updated.
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` — `EnsureBulkKeyOrThrow` message + `BuildIdDocumentQuery` comment updated for accuracy under the new operator.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` — stale comment fixed (site 1 above).
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsInheritanceTests.cs` — new file, 5 tests: `OfType` delete, `OfType` update, `OfType` phase-1 wire-command assertion, `Cast` rejection, `is`-predicate single-command control.

Not touched (deliberately, per Correction 1): `MongoNonQueryExpression.cs` (`AffectedSetNeutralDefinitions`/`IsAffectedSetNeutral`) — its existing doc comment already anticipated and correctly described this task's outcome ("OfType/Cast are deliberately excluded... they belong on the two-phase side... not here"), so no edit was needed there.

## Self-review

- **Completeness.** Classifier change, doc updates, stale-comment sweep, and tests are all in place; both Northwind baselines hold; all three configs build.
- **Naming.** Test names follow the existing `Subject_Condition_Expectation` convention used throughout `Query/*.cs` (e.g. `ExecuteDelete_with_OfType_deletes_only_that_type`, matching `ExecuteDelete_with_an_is_predicate_...` already in the brief).
- **YAGNI.** Did not generalize the classifier to a lookup table or add a Cast fallback path "just in case" — kept the change to exactly the two `if` branches the brief specified, minus the one branch the evidence ruled out.
- **Would each new test actually fail if the code were wrong?**
  - `ExecuteDelete_with_OfType_deletes_only_that_type` / `ExecuteUpdate_with_OfType_updates_only_that_type`: yes — if the discriminator filter were dropped, phase 1 would harvest all 3 ids (not 2), and the delete/update count and the surviving `Dog` assertions would fail.
  - `ExecuteDelete_with_OfType_phase1_query_carries_the_discriminator_filter_on_the_wire`: this is the direct answer to "would the OfType delete test fail if the discriminator filter were dropped" — yes, and more specifically than the row-count test: it asserts the literal phase-1 pipeline contains `{ "$match": { "_t": "cat" } }`, so it would fail even in a hypothetical where a dropped filter still happened to return the right rows by luck (e.g. a test double or a coincidentally-filtered collection).
  - `ExecuteDelete_with_Cast_is_rejected_by_the_bulk_source_classifier`: would fail (no exception, or a different exception) if `Cast` were accidentally re-admitted.
  - `ExecuteDelete_with_an_is_predicate_...`: unchanged from the brief; still a real control confirming the single-command path and its `Strategy=SingleCommand` log line are untouched.
- **Test output pristine.** No `Console.WriteLine`/debug scaffolding left in the committed file (the wire-capture scratch tests used during investigation were removed; only the permanent, assertion-bearing version remains).
- **Comments true after the change.** Re-read every comment touched or added; cross-checked the `EnsureBulkKeyOrThrow` and `BuildIdDocumentQuery` claims against the actual call sites (`VisitNonQuery` calls `EnsureBulkKeyOrThrow` for any `TwoPhase` strategy, confirmed by reading the surrounding code, not assumed).

## Other findings, out of scope for this task

- `tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoComplianceTest.cs` (~line 118-132) ignores `Microsoft.EntityFrameworkCore.BulkUpdates.InheritanceBulkUpdatesTestBase<>` and `FiltersInheritanceBulkUpdatesTestBase<>` with a comment characterizing the currently-supported bulk subset as "single-collection, Where-scoped queries with scalar setters" plus (per a later comment) the Skip/Take/OrderBy/Distinct two-phase cases. Now that `OfType` is also supported via two-phase, these EF-conformance inheritance test bases may partially or fully be back in scope — I did not investigate what they actually assert (would require pulling the upstream test source, which isn't vendored, and is a nontrivial side quest) and have **not** touched this file, consistent with the brief's caution about not touching conformance-suite classification outside this task's remit. Flagging for whoever owns spec-conformance sweeps (possibly overlapping with Task 25's `docs/failing-spec-tests.md` ownership) to assess separately.
- `docs/failing-spec-tests.md`'s EF-X012 entry (`OfType_Select`, `OfType_Select_OfType_Select` in `NorthwindAggregateOperatorsQueryMongoTest.cs`) is unaffected — those are read-path `OfType`+`Select` translation gaps, unrelated to the bulk classifier this task touches. Not re-tagged, not claimed fixed, per instructions.

## Concerns

- The brief shipped with an unverified assumption about `Cast` and zero test coverage for it; had I followed it literally, `Cast<T>().ExecuteDelete()` on a TPH hierarchy would have gone from a clean rejection to a confusing driver-internal `ExpressionNotSupportedException`, a user-facing regression in error quality (though not a data-safety regression, since no documents get touched — see evidence above). I'm confident in the empirical evidence but flag this explicitly since it's a real deviation from the brief's Step 3/Interfaces section ("Produces: ClassifyBulkSource returns TwoPhase for any chain containing QueryableMethods.OfType or QueryableMethods.Cast").
- Using `DONE_WITH_CONCERNS` rather than plain `DONE` solely to make sure this Cast-scope deviation and the `MongoComplianceTest.cs` observation get eyes-on before being considered fully closed, not because I have doubts about the correctness of what was actually shipped (OfType two-phase admission is verified at both the row-count and wire-command level, in both EF9 and EF10).

---

# Fix Report: Review Round 1 (Important 1-3, Minors 4-7)

All three Important findings and all four Minors from the round-1 review are addressed below, each with the empirical evidence the review demanded before any fix was applied.

## Important 1 — `NoAutoTransactionError`/`StandaloneTransactionError` also blamed "paging" only

**Fix.** `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`, both messages now read "uses paging ('Skip'/'Take') or an 'OfType' type filter" instead of "uses paging ('Skip'/'Take')". Added a comment above both explaining these are hand-maintained (not derived from the plan) and recording that they've now been wrong twice in opposite directions across three tasks.

**Design option recorded, not built** (per the dispatch's instruction): `MongoBulkPlan` could carry the specific operator name(s) that forced `TwoPhase` classification, and these two messages could interpolate that instead of hardcoding an operator list — removing the hand-sync requirement with `ClassifyBulkSource` for good. Not built here; flagging for whoever next touches this seam (possibly worth folding into whatever "plan carries more classification metadata" work Task 19 or later ends up doing, since Important 3 below also touches plan/classification boundaries).

**Verification.** No test asserts either message's exact string (confirmed by grep, matching the reviewer's own finding); `ExecuteDeleteTests`/`ExecuteUpdateTests`' `AutoTransactionBehavior.Never` tests (`ExecuteDelete_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing`, `ExecuteUpdate_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing`, etc.) still pass — see the "Verification run" section below.

## Important 2 — stale doc comment in `MongoNonQueryExpression.cs`

**Fix.** `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs:101-103`. Split the single sentence that claimed both `OfType` and `Cast` "narrow the affected set via a discriminator filter" into two: `OfType` narrows by type and is excluded from `AffectedSetNeutralDefinitions` because it *is* two-phase (with a `cref` to `ClassifyBulkSource`); `Cast` is excluded for the opposite reason — it does not narrow at all, and is rejected outright by the same classifier rather than being two-phase either.

Accepting the review's correction at face value: my Task 15 report's claim that this doc "was already correct" was wrong, and I should have re-verified it against the Cast finding rather than asserting it without checking — noted so the same mistake isn't repeated.

## Important 3 — self-referencing setter + `OfType<TDerived>()`, investigated with a real probe

**Symptom observed (verbatim), reproduced by temporarily admitting the combination and running `db.Entities.OfType<Cat>().ExecuteUpdate(s => s.SetProperty(c => c.Name, c => c.Name + "!"))` against the seeded Cat/Cat/Dog hierarchy, on EF10:**

```
System.InvalidOperationException: The LINQ expression 'DbSet<Animal>()
    .OfType<Cat>()
    .ExecuteUpdate(new Tuple<Delegate, object>[]{ new Tuple<Delegate, object>(
        e => e.Name, 
        e => e.Name + "!"
    ) })' could not be translated. Additional information: Exception has been thrown by the target of an invocation.
 ---> System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> MongoDB.Driver.Linq.ExpressionNotSupportedException: Expression not supported: e.
   at MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.ParameterExpressionToAggregationExpressionTranslator.Translate(...)
   ...
   at MongoDB.EntityFrameworkCore.Query.Visitors.MongoShapedQueryCompilingExpressionVisitor.RenderAggregateExpression[TSource,TResult](...) :642
   at MongoDB.EntityFrameworkCore.Query.Visitors.MongoShapedQueryCompilingExpressionVisitor.RenderSelfReferencingValue[TSource](...) :631
```

This is squarely the "internal or confusing error" bucket the dispatch described: a `TargetInvocationException` (leaked reflection-invoke wrapper) wrapping a raw driver `ExpressionNotSupportedException` ("Expression not supported: e." — `e` being the generic parameter name EF's expression printer uses for both the selector and value lambdas, not a real identifier), with the outer EF-visible message reduced to the useless "Exception has been thrown by the target of an invocation."

**Root cause, confirmed by reading the code, not guessed:** `CreateBulkPlan<TSource>` (`MongoShapedQueryCompilingExpressionVisitor.cs`) is generic over the *root* entity type (`Animal`), and `RenderSelfReferencingValue`'s `entityParameter` is `Expression.Parameter(typeof(TSource), "e")` — also `Animal`-typed. But EF types a bulk-update setter's selector/value lambdas over the queryable's *current element type*, which `OfType<Cat>()` narrows to `Cat`. `RebindSetterValueParameters`/`EntityParameterCollector`/`ParameterRebindingExpressionVisitor` only match parameters whose `.Type == entityType` (i.e. `Animal`), so the actual `Cat`-typed parameter inside the self-referencing value body is neither collected (no ">1 parameters" exception fires — it finds zero matches, not two) nor rebound, and is handed to the driver's aggregation-expression renderer as a free variable it has never seen bound anywhere.

**Fix chosen — not structural, per the decision tree.** Rather than reworking `CreateBulkPlan` to carry the derived type (which *would* be structural, and which the dispatch said to stop and report rather than improvise), the combination is now rejected up front, at build/translate time, in `BuildSetter` (`MongoQueryableMethodTranslatingExpressionVisitor.cs`) — the same place every other setter-target rejection already lives (primary key, discriminator, value-converter self-reference). The check: if the setter is self-referencing *and* the property-selector lambda's parameter type differs from the root entity's CLR type, reject with a message naming the actual limitation:

```
Self-referencing ExecuteUpdate on property 'Name' cannot be combined with an 'OfType' type filter: the setter
value references the entity as 'Cat', but the MongoDB provider can only evaluate a self-referencing setter
against the queried root type ('Animal'). Use a constant value, compute the value first, or perform the update
via the change tracker instead.
```

Re-ran the same probe after the fix — clean rejection, no driver exception, no reflection-wrapper noise:

```
System.InvalidOperationException: The LINQ expression '...' could not be translated. Additional information:
Self-referencing ExecuteUpdate on property 'Name' cannot be combined with an 'OfType' type filter: ...
```

Non-self-referencing (constant) setters are provably unaffected: `SerializeConstant`/`EvaluateToConstant` never build a parameter-bound lambda at all, so there's no rebinding step to fail — confirmed by `ExecuteUpdate_with_OfType_updates_only_that_type` (constant setter, still passes).

**Test added:** `ExecuteUpdate_with_OfType_and_a_self_referencing_setter_is_rejected_with_a_clear_message` in `BulkOperationsInheritanceTests.cs`, asserting the clean rejection message. This would fail (no exception, or the old confusing one) if the `BuildSetter` guard were removed or the driver's translation behavior ever changed.

Data safety was never at stake in this path — worst case was a translation failure, never a wrong write — consistent with the dispatch's framing.

## Minor 4 — `BsonDocument` string-indexer dead `?.`

**Fix.** `BulkOperationsInheritanceTests.cs`, the phase-1 wire-command assertion now reads:
```csharp
Assert.Contains(pipeline, stage => stage.AsBsonDocument.GetValue("$match", null)?.AsBsonDocument.GetValue("_t", null) == "cat");
```
using the `GetValue(name, defaultValue)` overload instead of the throwing string indexer, so a missing `$match` or `_t` key produces `false` (a clean assertion diff) rather than an exception from inside the predicate.

## Minor 5 — `OfType<TRootType>()` needlessly forced onto the (non-atomic, transaction-requiring) two-phase path

**Fix.** `ClassifyBulkSource` (`MongoQueryableMethodTranslatingExpressionVisitor.cs`) now compares the `OfType` call's actual generic argument against the queried root entity's `ClrType` (`mongoQueryExpression.CollectionExpression.EntityType.ClrType`) and only sets `requiresTwoPhase = true` when they differ — mirroring `TranslateOfType`'s own no-op check (`entityType.ClrType == resultType return source`). `OfType<TRoot>()` now stays on the single-command path, exactly like the bare `db.Entities.ExecuteDelete()` it is semantically identical to.

**Test added:** `ExecuteDelete_with_OfType_of_the_root_type_stays_on_the_single_command_path`, asserting `Strategy=SingleCommand` in the executing-bulk-delete log (same idiom as the existing `is`-predicate single-command test) after `db.Entities.OfType<Animal>().ExecuteDelete()`.

## Minor 6 — Cast rationale duplicated four times

**Fix.** Kept the XML doc on `ClassifyBulkSource` as the canonical statement (unchanged in substance from the prior fix round, still accurate). Reduced the other three sites to one-line pointers at it:
- The in-branch `OfType` body comment (the one specifically flagged, since it explained the untaken path from inside the branch that is taken) now just says "See this method's XML doc for why Cast does not get the same treatment."
- The `else`/rejection arm gained a one-line comment ("Cast falls through to here, same as before OfType was admitted — see this method's XML doc.") so a reader following the `else` branch isn't left wondering why Cast lands there.
- The test file's header comment and the `Cast`-rejection test's own comment were both trimmed to point at `ClassifyBulkSource`'s doc comment instead of re-deriving the rationale.

## Minor 7 — `MongoComplianceTest.cs` incomplete enumeration

**Fix.** `tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoComplianceTest.cs:127-128`: "OrderBy/Skip/Take/Distinct-scoped cases" → "OrderBy/Skip/Take/Distinct/OfType-scoped cases". Did not touch the `Ignore`-list itself (lines 118-126, including `InheritanceBulkUpdatesTestBase<>`) — that decision stays Task 20's, as instructed.

## Verification run (this round)

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   # 0 warnings, 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    # 0 warnings, 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    # 0 warnings, 0 errors

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~BulkOperationsInheritanceTests|FullyQualifiedName~DiscriminatorTests"
# Passed! - Failed: 0, Passed: 33, Skipped: 0, Total: 33

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~BulkOperationsInheritanceTests"
# Passed! - Failed: 0, Passed: 7, Skipped: 0, Total: 7

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~BulkOperationsInheritanceTests"
# Passed! - Failed: 0, Passed: 84, Skipped: 1, Total: 85   (skip = standalone-deployment test, expected/unrunnable here)

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~BulkOperationsInheritanceTests"
# Passed! - Failed: 0, Passed: 85, Skipped: 1, Total: 86

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
# Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181   (baseline held)

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
# Passed! - Failed: 0, Passed: 169, Skipped: 0, Total: 169   (baseline held)

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~Northwind"
# Passed! - Failed: 0, Passed: 4385, Skipped: 23, Total: 4408   (full Northwind suite, unaffected)

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoComplianceTest"
# Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

## Files changed (this round, in addition to the round-1 commit)

- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` — `BuildSetter` gains the OfType/self-referencing-setter guard (Important 3); `ClassifyBulkSource` gains the `OfType<TRoot>()` no-op narrowing (Minor 5) and the Cast-comment de-duplication (Minor 6).
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs` — doc comment split for `OfType` vs `Cast` (Important 2).
- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` — both transaction-error messages generalized (Important 1).
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsInheritanceTests.cs` — new tests for Important 3 and Minor 5; null-safe indexer fix for Minor 4; comment trims for Minor 6.
- `tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoComplianceTest.cs` — enumeration fix (Minor 7).

## Self-review of this round

- Important 3's fix was verified against a real reproduction both before (confusing driver exception, captured verbatim above) and after (clean rejection, captured verbatim above) the fix — not asserted from code-reading alone.
- Checked that the new `BuildSetter` guard does not fire for the already-passing `ExecuteUpdate_with_OfType_updates_only_that_type` (constant setter) or for any non-`OfType`-scoped self-referencing setter elsewhere in `ExecuteUpdateTests.cs` (full `ExecuteUpdateTests` run above stayed green).
- Checked Minor 5's narrowing against a double edge case: nothing in the admitted-operator set (Where/ordering/Distinct/Skip/Take/OfType) can change the "current element type" other than `OfType` itself, so comparing a given `OfType` call's argument against the *root* type is sound for every reachable chain shape, not just the single-`OfType` common case.
- Re-verified this time, rather than asserting from memory, that `MongoNonQueryExpression.cs`'s doc comment (Important 2) is now actually true for both `OfType` and `Cast` by reading it back after editing.
- No new stale-comment sweep beyond the four sites reviewed found further issues on a second grep pass for "paging" and "OfType"/"Cast" rejection claims across `src/` and `tests/`.

## Concerns carried into this round

- None new. The `MongoComplianceTest.cs` `InheritanceBulkUpdatesTestBase<>` observation from the round-1 report still stands as a flag for Task 20/25, untouched here per the reviewer's own instruction not to touch the `Ignore`-list decision.
