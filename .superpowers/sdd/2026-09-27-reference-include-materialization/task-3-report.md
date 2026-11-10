# Task 3 report — Branch 3A (Hypothesis A)

**Status:** DONE_WITH_CONCERNS (both target tests pass with correct data; concerns listed at the end)

## Commits

- `382dccbb` EF-322: materialize a reference Include on a native join-scope leaf correctly (Branch 3A: test 1, Shapes 1/2/4)
- `89c3446e` EF-322: nest a collection Include's $lookup inside the join that produces its declaring entity (test 2's separate root cause)

Each commit was checked on its own. On `382dccbb` alone, the only failures in the EF10 Query sweep are the 8 pre-existing test-2 failures.

## Step by step

### Steps 1–2: "register a lookup for the reference Include" — the brief's guess was wrong, and I did not do it

I traced how the reference Include is actually built. EF Core's nav-expansion lowers
`Orders.Include(o => o.Owner).Select(o => new { o, o.OwnerId })` into a join onto Owners, and sets the
Include's `NavigationExpression` to that join's own `ti.Inner`. So **the join's `$lookup` already is the
Include's lookup**. A second registration would only duplicate it. That is why the join scope reports
`Levels.Count=1, Joins.Count=1` with no explicit `.Join()`. The two bare reference-Include tests that
already passed use `TryConfirmReferenceIncludeChain` and share no code with this path, so they don't
show the output shape to copy. The shape to copy is the existing whole-entity **Inner** leaf arm. The
read side already reads an inner entity from its root-level `_lookup_<Nav>` field.

So, in `NativeJoinScopeProjectionBinder` (the staging side):
- New `TryResolveReferenceIncludeLevel` admits a reference Include only when all of these hold:
  - it is a non-embedded reference;
  - its `NavigationExpression` resolves to a join-scope Inner leaf at level k ≥ 1. Resolution uses parameter identity and the member-name chain via `TryResolveScopeDepth`, never CLR type;
  - `mongoQ.Joins[k-1].Navigation` is the same navigation.
- When admitted, that level's Inner document is staged into `$project` through a new shared
  `TryStageInnerLevel` helper. I pulled this out of the existing Inner-leaf arm, and it keeps the
  per-level dedup and the "assert, don't assume" check.
- Everything else still declines: owned references, a ThenInclude on the reference's own target, or a
  target this scope does not produce.

`MongoProjectionBindingExpressionVisitor`'s non-collection branch is **not** changed.

### Step 3: `BindResultMember` (`MongoQueryableMethodTranslatingExpressionVisitor.cs`)
- Replaced the `allCollectionIncludes` decline with `allIncludesRebindable`. Collection wrappers keep
  their `NavigationExpression` untouched. For reference wrappers, the folded `NavigationExpression` (the
  join's Inner `StructuralTypeShaperExpression`) is rebound by index, the same way as the leaf.
- Every wrapper is checked before anything is rebound, so a decline leaves no stray `AddToProjection`
  entry. Anything else still falls through to the old `BindSelectManyMember` fallback.
- New helpers: `IsRebindableEntityShaper` and `RebindEntityShaper`. The second is the leaf's old rebind code, moved out.

### Step 4: promoted test
`Whole_entity_leaf_that_is_also_reference_included_declines_rather_than_silently_returning_null` is now
`Whole_entity_leaf_that_is_also_reference_included_materializes_correctly`. It runs under NativeOnly,
must not throw, and checks the data against an oracle (Owner name and Id on every row).

The Shape 2 test also went native with correct data, so I promoted it the same way:
`Reference_included_Inner_leaf_of_a_join_scope_never_returns_silently_wrong_data` is now
`..._materializes_correctly` and compares NativeOnly against DriverLinq. Task 2's unit test
`Declines_a_duplicated_whole_inner_entity_leaf_that_is_also_include_wrapped` is now
`Binds_a_duplicated_whole_inner_entity_leaf_that_is_also_reference_include_wrapped`. It asserts a 2-level
scope, both InnerPrefixes staged once as self-referential element refs, 2 lookups, and
`Route == Projection`.

### Step 5: Shape 4 test
The brief's Shape 4 query is word-for-word the promoted Step 4 test's query, and Task 1's findings put it
on the same join-scope arm. So `Reference_Include_with_no_join_in_the_query_materializes_correctly` sits in
`NativeJoinTests.cs` and adds different coverage:
- default Native mode;
- a dangling-FK seed. `Order.OwnerId` is a required FK, so EF lowers the Include to an inner join and the dangling row is dropped. The test checks that and compares against DriverLinq.

### Extra coverage for the ThenInclude risk (Review Focus)
- `Reference_included_whole_entity_leaf_goes_native_and_matches_driver_linq` is a theory over three
  shapes. Each goes native, matches DriverLinq, and has every Owner populated:
  - design-doc Shape 1: a reference Include on the outer side of an explicit `.Join()`, a 2-level chain;
  - the Include target also projected (`new { o, W = o.Owner }`), which exercises the InnerPrefix dedup;
  - a reference and a collection Include on the same leaf.
- `Reference_Include_with_a_ThenInclude_on_a_join_scope_leaf_falls_back_with_correct_data` covers
  reference→reference and reference→collection ThenInclude. Both throw
  `NativeTranslationNotSupportedException` under NativeOnly, and Native mode falls back to data identical to DriverLinq.

### Step 6: baselines
Regenerated with `EF_TEST_REWRITE_BASELINES=1` for both target tests in all 4 classes. New test-1 MQL:
`$match, $lookup Customers as _lookup_Customer, $unwind, $project { o: $$ROOT, _lookup_Customer, CustomerID }`.
All 4 sibling Include suites: 952/952 pass on EF10.

### Step 7: test 2 needed a separate fix (commit `89c3446e`)
I confirmed the findings doc's guess of a field mismatch and traced it exactly. A collection Include on the
**inner** side of a Join/LeftJoin over a collection navigation had its `$lookup` emitted at the document
root:
- `localField: "_id"` — the outer Customer's key;
- `as: "_lookup_OrderDetails"`.

The shaper reads `_lookup_Orders._lookup_OrderDetails`, so every inner collection came back empty in all
modes, DriverLinq included. I reproduced it in a functional test. The cause is the flat-mode intermediate
match in `MongoProjectionBindingExpressionVisitor`, which only looked at **reference** intermediates
(`l.IsReference && l.ForceUnwind`).

The fix:
- New `TryGetDeclaringJoinLookup` (`.Lookup.cs`) finds the intermediate from the Include's own entity
  binding. It uses the same `ParentAccessExpression` that `RewriteCollectionIncludeForLookup` builds the
  read side from, so the write and read sides can't disagree.
- For a forced-unwind **collection-nav** join lookup, the Include's `$lookup` is **nested into that
  join's sub-pipeline**. It is deduplicated, and `PipelineKind` is set to NestedInclude only if it was None.

I nested rather than prefixed because my first attempt, a dotted-`as` prefix, broke
`Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join`. `$lookup` with a
dotted `as` creates a phantom `_lookup_Orders: { _lookup_OrderLines: [] }` on unmatched left-outer rows,
and the shaper then builds an Order with no key and throws. That test's seed has no OrderLines at all,
which is why it never saw the empty-collection bug. The reference-intermediate prefix path is unchanged.

New regression test: `Collection_Include_on_the_inner_side_of_a_join_reads_the_inner_entitys_own_collection`.
- Shapes: Join and GroupJoin/DefaultIfEmpty on all EF versions; LeftJoin on EF10 only.
- Seed: real OrderLines, plus an Owner with no Orders.
- Checks: in-memory oracle in Native and DriverLinq modes (NativeOnly as well on EF10).
- It fails (3/3) with the fix reverted.

## Test results
- **Both target tests** × 4 sibling classes × sync/async: 16/16 pass in default mode **and** with
  `MONGODB_EF_NATIVE_ONLY=1`.
- **NativeOnly check over the 4 Include classes plus NorthwindJoinQuery**, before vs after: 34 → 18 failures.
  The 16 removed are exactly the target tests; none were added.
- **EF10 full solution** (no filter):
  - Unit: 1285/1285
  - Spec: 4599 passed, 14 skipped, 0 failed
  - Functional: 3284 passed, 52 skipped, 0 failed
- **EF10 `FullyQualifiedName~Query`**:
  - Unit: 1021/1021
  - Spec: 4536 passed, 13 skipped, 0 failed
  - Functional: 2122 passed, 45 skipped, 0 failed
- **EF9 and EF8 `~Query`**: unit and functional all pass. Spec has 10 failures, all in
  `NorthwindMiscellaneousQueryMongoTest` (`OrderBy_object_type_server_evals`, `Projection_*skip/take*`,
  `Contains_over_concatenated_columns_both_fixed_length`). They were already failing: confirmed on EF9 with my changes stashed (same 10).
- The two bare reference-Include tests (`NativeReferenceIncludeTests`) still pass (inside the sweep).
- No new compiler warnings. I fixed one CS8632 I had introduced in a `#nullable disable` file.

## Files changed
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs`
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs`
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.Lookup.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeProjectionBinderTests.cs`
- `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/Northwind{,String,EFProperty}IncludeQueryMongoTest.cs`, `NorthwindIncludeNoTrackingQueryMongoTest.cs` (baselines only)

All temporary probe and scratch tests were removed before committing.

## Self-review
- **Generalization:** works at any chain depth and on either side (Outer leaf, Inner leaf, explicit-join chain),
  and for several reference Includes on one leaf, because the loop collects them. Not supported, by design:
  a ThenInclude hanging off the reference target. It declines cleanly, and a test pins that.
- **Staging and materialization agree:** the staging side is the gate. The bind side only runs after
  `TryBindProjection` succeeds, and its own check (`IsRebindableEntityShaper`) is a safety net that falls
  back loudly.
- **Brief deviation:** Step 2's "add a LookupExpression/AddLookup for the reference nav" was not done,
  for the reason given above. The brief allowed this ("if different, mirror that method's output shape").
- **Step 5 deviation:** the Shape 4 test is not the brief's exact verbatim body, because that body would
  duplicate the promoted Step 4 test. Same name and same query; it covers Native mode plus a dangling FK.

## Concerns
1. **An existing bug with the same cause, not fixed:** the reference-intermediate prefix path
   (dotted `as: "_lookup_<Ref>._lookup_<Coll>"`) creates a phantom sub-document for an unmatched
   left-outer row. The result is `InvalidOperationException: Document element is missing for required
   non-nullable property 'Id'`, in both Native and DriverLinq. Reproduced with:
   ```
   Orders.LeftJoin(Owners.Include(w => w.Orders), r => r.OwnerId, w => w.Id, (r, w) => new { r, w })
   ```
   using a dangling OwnerId. It already failed before my change. It is out of scope, and it can't be
   nested the same way because the lowerer's reference arm needs `!HasPipeline`. It needs its own ticket.
2. The nested-pipeline form turns the join's `$lookup` into `let`/`$expr` form whenever an inner-side
   collection Include is present. The result is correct, but the MQL shape differs from the plain
   `localField` form. Whether the `$expr` equality uses an index is worth a look if that matters.
3. `o: "$$ROOT"` in the native `$project` also carries the `_lookup_<Nav>` sub-document, so the joined
   document is sent twice per row. This already happened for any Outer whole-entity leaf over a join; it
   is not new.

---

# Fix report (task-review finding) — commit `1f7ac9a6`

**Finding (Important):** `Reference_Include_with_a_ThenInclude_on_a_join_scope_leaf_falls_back_with_correct_data` could not detect wrong data. There were three reasons:
- In Native mode the query falls back to driver-LINQ, so comparing its result against DriverLinq compared the fallback path against itself.
- The `DoesNotContain("||")` guard misses a null ThenInclude target, which renders as `SKU-1|10|`.
- For reference→collection, change-tracker fixup fills `Owner.Orders` even if the Include's own `$lookup` returns nothing.

My original report's claim of "correct data in Native mode for ThenInclude chains" was therefore not established by that test.

**Fix (test-only, no production code changed):**
- **New oracle:** `ExpectedReferenceIncludeShape(seed, shape)` is an in-memory oracle computed from the seed alone. Both reference-Include theories now assert `Assert.Equal(oracle, result)`:
  - Native and DriverLinq for the ThenInclude theory;
  - NativeOnly and DriverLinq for the goes-native theory.

  The DriverLinq self-comparison and the `||` guard are gone.
- **No-tracking queries:** every shape in `RunReferenceIncludeShape` now runs `AsNoTracking()`. That includes `IncludedTargetAlsoProjected` (the Minor finding), `ExplicitJoin` and `MixedWithCollectionInclude`.
- **Reference→collection exception:** EF Core rejects `Include(o => o.Owner).ThenInclude(w => w.Orders)` under no-tracking ("Include path 'Owner->Orders' results in a cycle"). Every reference→collection ThenInclude in this model is such a cycle. That shape therefore stays tracked, but filters `Where(o => o.Total != 20m)`: the query then tracks only the Total 10 and Total 30 orders. It renders the full list of `Owner.Orders` totals, so fixup alone would give Alice `10` instead of `10,20`.
- **Minor finding:** rewrapped the overlong comment line in `BindResultMember`'s header.

**Mutation check.** I temporarily removed each ThenInclude and the NativeOnly `Throws`, then restored both:
```
ReferenceThenCollection: Expected ["10|Alice|10,20", "30|Bob|30"]  Actual ["10|Alice|10", "30|Bob|30"]
ReferenceThenReference:  Expected ["SKU-1|10|Alice", "SKU-2|10|Alice", "SKU-3|20|Alice", "SKU-4|30|Bob"]
                         Actual   ["SKU-1|10|", "SKU-2|10|", "SKU-3|20|", "SKU-4|30|"]
```
Both mutations fail the oracle assertion, so the test now catches a missing or empty ThenInclude.

**Results after the fix:**
- All 7 `NativeJoinTests.Reference_*` tests pass.
- Full `NativeJoinTests`: 65/65 on EF10 and 55/55 on EF8. Both builds have 0 errors.
- Both ThenInclude shapes still throw `NativeTranslationNotSupportedException` under NativeOnly, and the Native-mode fallback matches the oracle.
