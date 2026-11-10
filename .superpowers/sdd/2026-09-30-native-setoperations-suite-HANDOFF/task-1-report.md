# Task 1 (A) report: bare-scalar operand alias alignment

Status: DONE_WITH_CONCERNS. Commit `0deedefd` on `EF-322c-setops` (base 8c5f00e1). Not pushed.

## What was implemented

`src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`, `TryTranslateSetOperation`, projected-operand branch:
- The gate is now `ProjectionShapesMatch(...) || (alignBareScalarAliases = CanAlignBareScalarAliases(source1, mongo1, source2, mongo2))`. The rewrite runs only when that local flag is true, so it uses the same predicate result the gate admitted through.
- Inside the admitted branch, before `AppendSetOperation`, `mongo2.Select.ReplaceSingleProjectionAlias(mongo1.Select.Projection[0].Alias)` keeps the expression and source. Translation happens before `NativeDateTimeKindReadBack.HasKindMismatch` runs (at compile time), so that check pairs the re-aliased leaves.
- `private static bool IsBareScalarOperand(ShapedQueryExpression, MongoQueryExpression)` is true when there is exactly one projection, it is not a `MongoDocumentConstructionExpression`, and `ShaperExpression.RemoveConvert()` is not a `NewExpression`/`MemberInitExpression`.
- `internal static bool CanAlignBareScalarAliases(...)` (internal, not private, so the unit test can call it) requires:
  - both operands are bare scalars;
  - `mongo2.Select.PostGroupOps.Count == 0` (new; see below);
  - `ReadsOnlyDefaultSerializedFields` for both operands (new): `AllFieldsDefaultSerialized` over projections, group key parts and accumulator operands.

`src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs`: added `internal void ReplaceSingleProjectionAlias(string alias)`. It throws if `Count != 1`.

The two extra conjuncts go beyond the brief. Both were proven necessary by mutation (below):
- **PostGroupOps:** a post-Distinct `Where`/sort/page on source2 reads its own flattened alias. Re-aliasing leaves that op pointing at nothing, and native silently returns wrong rows.
- **Default serialization:** every combined row is read through source1's shaper, so a value-converted column on either side would be read with the wrong serializer.

The MQL is as the brief predicted:
- `{_id: {$add: ["$N", 1]}}` inside `$unionWith` (asserted in a functional test).
- The GroupBy operand flattening becomes `{ "$project" : { "_id" : "$_v" } }` (column_function baseline).
- The function_column operand becomes `{ "$project" : { "_v" : "$_id", "_id" : 0 } }`.

BOMs preserved: both src files start with EF BB BF. The spec file keeps its BOM. The new test files have no BOM, matching sibling test files.

## Tests added

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeSetOperationProjectionTests.cs` (new, `NativeAndParity`/`DeclinesCleanly`). Seed (Id,N) = (1,10),(2,20),(3,1),(4,10). Cases:
  - key column ∪ N+1, and N+1 ∪ key column (hand oracle [1,2,3,4,11,21]);
  - Concat key column ++ N*2, which keeps duplicates [1,2,2,3,4,20,20,40];
  - non-key column N ∪ Id+100;
  - two different columns;
  - column ∪ constant 7;
  - column ++ GroupBy count, and GroupBy count ∪ column;
  - an MQL-shape test;
  - two anonymous-operand pins (these already go native because member aliases match);
  - `Union_with_filtered_distinct_operand_of_a_different_alias_declines`;
  - `Union_of_value_converted_column_and_computed_scalar_declines`. This one asserts only the NativeOnly decline, because driver-LINQ also fails with a FormatException on this shape, so there is no parity oracle.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeDateTimeKindReadbackTests.cs` (+3):
  - PlainDate ∪ UtcDate reads back as default kind;
  - Local column ++ `GroupBy.Max(LocalDate)`, in both orders, reads back as Local;
  - mixing Local and default kinds (column/column in both orders, and column/aggregate) declines.
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeSetOperationAliasAlignmentTests.cs` (new): the predicate on real translations.
  - true: bare column vs computed, and bare column vs grouped count;
  - false: single-member anonymous and multi-member anonymous operands.
  - This is the brief's fallback. EF rejects different-typed operands, and anonymous members alias by name, so no LINQ-reachable construction pair with different aliases exists.

## RED / GREEN evidence (EF10, MONGODB_URI/ATLAS_URI unset)

- RED: `dotnet test FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeSetOperationProjectionTests|FullyQualifiedName~NativeDateTimeKindReadbackTests.Set_operation_of"` gave `Failed: 12, Passed: 3`. Every positive test failed with `NativeTranslationNotSupportedException : Query projects a non-entity result and MongoQueryMode.NativeOnly forbids the driver-LINQ fallback.`
  - After the tests were reworked, re-running with the gate mutated to the pre-change behaviour (`CanAlign => false`) failed all 11 positive functional tests and the 2 positive unit tests.
- GREEN:
  - same filter: 16/16 pass; unit tests 4/4 pass;
  - full EF10 solution: Unit 1558/1558, Spec 4768 passed / 14 skipped / 0 failed, Functional 4158 passed / 52 skipped / 0 failed.

## Mutation results (EF10)

| Mutation | Fails |
|---|---|
| `CanAlignBareScalarAliases => true` | unit `Single_member_anonymous_operand_does_not_align`, `Multi_member_anonymous_operands_do_not_align`; functional `Union_with_filtered_distinct_...`, `Union_of_value_converted_...` |
| drop bare-scalar checks only | the two unit tests |
| drop PostGroupOps check | `Union_with_filtered_distinct_...`. With the parity leg, native returns `[1,2,3,4]` against driver `[1,2,3,4,10,20]`, i.e. silently wrong data |
| drop default-serialization check | `Union_of_value_converted_...`: FormatException at materialization instead of a translation decline |
| `=> false` | all positives |

The code was restored after each mutation, and the committed file is the unmutated one.

## Spec suite (EF10)

- `MONGODB_EF_NATIVE_ONLY=1` over `NorthwindSetOperationsQueryMongoTest`: 44 failing before, 32 after. The 12 that flipped are the brief's 6 × async:
  - Union_over_column_function
  - Union_over_column_constant
  - Union_over_column_binary
  - Union_over_function_column
  - Union_over_binary_column
  - Union_over_scalarsubquery_column
- No newly failing tests. The remaining failures are the C/N/J buckets.
- Baselines regenerated (`EF_TEST_REWRITE_BASELINES=1` with a tight filter) for column_function, column_constant, column_binary and function_column. These were the old driver-LINQ MQL; default mode is now native. The rewriter dropped the indentation of the raw strings, and I re-indented three of them to match their neighbours. I then rebuilt and reran without the variable.
- Default mode, SetOperations + `NorthwindGroupByQueryMongoTest`: 692 passed / 4 skipped / 0 failed. The only MQL diffs are the four regenerated baselines.

## EF8 / EF9

`Debug EF8` and `Debug EF9` build with 0 warnings and 0 errors. I ran the relevant classes on each: the new tests, NativeSetOpsTests, NativeDateTimeKindReadbackTests, the SetOps and GroupBy spec suites, and the unit tests.
- EF8: 11 unit, 684 spec (+4 skipped), 199 functional, all pass.
- EF9: 11 unit, 690 spec (+4 skipped), 199 functional, all pass.
- I did not run the full EF8/EF9 suites.

## Concerns

1. The brief's DateTime case `x.D.AddDays(1)` can't be tested. A bare computed date (`Select(o => o.PlainDate.AddDays(1))`) doesn't bind natively even on its own. I covered Review Focus #1 with bare column/column and column/grouped-`Max` pairs instead.
2. The two guards beyond the brief (PostGroupOps and default serialization) are mutation-proven. The value-converted shape has no driver-LINQ oracle, because driver-LINQ fails on it too.
3. `CanAlignBareScalarAliases` is `internal`, not `private` as the brief specified, so the unit test can call it.
4. The brief's `DeclinesCleanly` DTO control (two DTOs through a common base) can't be built: EF throws InvalidOperationException ("different 'Include' operations") before the provider sees it. It was replaced by the predicate unit tests.
5. Union_over_scalarsubquery_column and binary_column needed no baseline change.

---

## Fix round 1 (review finding: default-serialization guard only partly mutation-proven)

Commit amended: `0deedefd` became `4ce4a0c4`. It is still the only commit on EF-322c-setops above 8c5f00e1, and it is not pushed.

### What changed

`NativeSetOperationProjectionTests.cs`:
- Refactored the converted-column fixture into `CreateCodedContext` and `AssertCodedSetOperationDeclines`. The converter is `v => "c" + v`, so the stored values are "c5" and "c6".
- Existing test: `Union_of_value_converted_column_and_computed_scalar_declines`. The converted column is on source2.
- New `Union_of_computed_scalar_after_a_value_converted_column_declines`: `Select(x => x.Code).Union(Select(x => x.N + 1))`, with the converted column on source1.
- New `Union_with_a_grouped_accumulator_over_a_value_converted_column_declines`: `Select(x => x.Id)` combined with `GroupBy(x => x.N).Select(g => g.Count(x => x.Code > 5))`, in both orders.
  - It is paired with the key column on purpose. A pairing with `N + 1` is `_v`/`_v`, which ProjectionShapesMatch admits without ever consulting CanAlign. My first attempt used that pairing and caught this: I checked the MQL.

MQTEV: comment only. It notes that the key-part conjunct is defence in depth.

### Group key over a converted property

This is not expressible as a native operand. Each of these declines on its own under NativeOnly, before any set op:
- `GroupBy(x => x.Code).Select(g => g.Key)`
- `GroupBy(x => x.Code + 1)`
- `GroupBy(x => x.Code > 5)`
- `g.Max/Min/Sum(x => x.Code)`
- `Select(x => x.Code).Distinct()`

All of them fail with "groups without a supported aggregate projection" or "projects a non-entity result". The only grouped shape that reaches the gate with a converted field is a filtered-count condition.

### Mutation matrix

Each run is EF10, `--filter FullyQualifiedName~NativeSetOperationProjectionTests`, 15 tests, and every mutated build compiled with 0 errors:

| Mutation | Failing tests |
|---|---|
| none | 0 (15/15 pass) |
| drop `ReadsOnlyDefaultSerializedFields(mongo1.Select)` | `Union_of_computed_scalar_after_a_value_converted_column_declines`, `Union_with_a_grouped_accumulator_...` |
| drop `ReadsOnlyDefaultSerializedFields(mongo2.Select)` | `Union_of_value_converted_column_and_computed_scalar_declines`, `Union_with_a_grouped_accumulator_...` |
| projection conjunct → `true` | both converted-column tests |
| grouping branch → `true` | `Union_with_a_grouped_accumulator_...` |
| accumulator conjunct → `true` | `Union_with_a_grouped_accumulator_...` |
| key-part conjunct → `true` | **none**: unreachable, see above |

Restored afterwards; `cmp` against the saved unmutated file matches.

### Covering reruns (EF10, final state)

- `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "…NativeSetOperationProjectionTests|…NativeSetOperationAliasAlignmentTests|…NativeDateTimeKindReadbackTests|…NativeSetOpsTests"` gave Unit 11/11, Functional 201/201, Spec 0 matched.
- SetOps spec, default mode: 183/183 pass.
- SetOps spec, `MONGODB_EF_NATIVE_ONLY=1`: 32 failing, the same set as after the first commit's baseline regeneration (diffed).
- EF8 and EF9 still build with 0 errors. I did not rerun the tests there for this round.

### New concern

The accumulator conjunct is conservative. For `Id ∪ GroupBy(N).Count(x => x.Code > 5)` (both orders), with the accumulator conjunct removed, native returns the correct `[0,1,2]`. The converter is applied to the comparison literal (`{$gt: ["$Code", {$literal: "c5"}]}`), and the grouped output is a server-computed int, so re-aliasing it is safe.

So the grouping branch now declines a shape native would answer correctly, only so that the guard can be mutation-tested. Given that key parts are unreachable and accumulator outputs are server-computed, the grouping branch arguably should be dropped, keeping only the projection conjunct. That is a controller decision; as instructed, it stays in.
