# Task 2 (C) report: constant/parameter bare leaf on source1

Status: DONE_WITH_CONCERNS. Commit `0280a5b2` on `EF-322c-setops` (parent 4ce4a0c4). Not pushed.

## What was implemented

`src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`, `TryTranslateSetOperation`, projected-operand branch:
- Gate: `!HasShaperUnsafeConstantLeaf(mongo1)` became
  `(!HasShaperUnsafeConstantLeaf(mongo1) || (rebindConstantLeaf = CanRebindConstantLeafToDocument(source1, mongo1)))`.
- Admitted branch: rebinding happens only when the flag is set, after every check has passed and before the re-alias/append:
  `source1 = source1.UpdateShaperExpression(BindSelectManyMember(mongo1, mongo1.Select.Projection[0].Alias, source1.ShaperExpression))`.
  - `BindSelectManyMember` is the right binder. It registers the value under the Select's alias (`_v`) and returns a `ProjectionBindingExpression`, which the DOM shaper reads raw by alias.
  - This is the same raw alias read a bare column or computed leaf gets, and the MQL test confirms the alias stays `_v`.
- `internal static bool CanRebindConstantLeafToDocument(ShapedQueryExpression source1, MongoQueryExpression mongo1)` requires:
  - `IsBareScalarOperand(source1, mongo1)`;
  - `mongo1.Select.Projection[0].Expression is MongoConstantExpression or MongoParameterExpression`;
  - `NativeSlotPopulator.TryProbeBareValueRenders(leaf, source1.ShaperExpression.Type)`;
  - `IsRoundTripSafeConstantLeafType(source1.ShaperExpression.Type)`, which admits only `int`, `long`, `double`, `bool` and `string` (null string included).
- It is `internal`, not `private` as the brief said, for the unit test (same as Task 1).
- Nothing else is allow-listed: DateTime, Guid, enum, decimal, char and `Nullable<T>` decline.
- Select Task 4 has not landed. The leaf-kind conjunct and the probe stand in for `HasClientEvaluatedProjectionLeaf`, as the plan says.
- Negate was not touched. The src BOM is preserved (EF BB BF), and so is the BOM of `NativeSetOpsTests.cs` and of the spec file.

## Tests

`tests/.../FunctionalTests/Query/NativeSetOperationProjectionTests.cs` (+19 tests). I added a `Seed(name, rows)` overload and a seed containing an 8, (1,8),(2,20).
- `NativeAndParity` tests:
  - Review Focus 3, dedup when the constant equals a column value:
    - `Union_of_constant_and_column_holds_a_matching_value_once` → [8,20]
    - `Concat_of_constant_and_column_keeps_every_copy_of_a_matching_value` → [8,8,8,20]
    - `Filtered_constant_operand_against_a_matching_column_dedups_only_under_Union`: Concat [8,8,20] (two 8s), Union [8,20]
  - constant with a key column, a computed value, a grouped count, and another constant;
  - string "A" Concat/Union `(string)null`, null ++ "A", and a `"$N"` literal;
  - long, double and bool;
  - `int p` parameter. It is re-executed with p=30 on the same compiled query ([8,20,30]), and there is a Concat as well;
  - an MQL test: `{ "$project" : { "_v" : { "$literal" : 8 }, "_id" : 0 } }` plus `$unionWith` with `{ "_v" : "$N", "_id" : 0 }`.
- `DeclinesCleanly` tests. These cover Review Focus 2 (the round-trip of a constant): DateTime parameters, Guid, enum constants, and `new { A = 8, x.N }`.
- NativeOnly-throws only, because driver-LINQ also fails on each shape so there is no oracle:
  - `new { A = 8 }`: FormatException "Expected element name to be '_v', not 'A'";
  - `new string('a', x.N)`, which driver-LINQ can't translate;
  - a client method `ClientTwice(x.N)`: "Expression not supported".

`tests/.../UnitTests/Query/NativeTranslation/NativeSetOperationConstantLeafRebindTests.cs` (new, 6 tests) checks the predicate on real translations:
- true: int, long, double, bool, string and null-string constants;
- false: a bare computed value or column, DateTime, decimal, `int?`, anonymous types, and `new string('a', x.N)`.
- Parameters need EF's funcletizer, which this harness skips, so they are covered only functionally.

`tests/.../FunctionalTests/Query/NativeSetOpsTests.cs`: `Constant_projected_operand_as_source1_still_declines_to_protect_the_shared_shaper` pinned exactly the decline this task lifts. The full EF10 run caught it. I renamed it `..._goes_native_and_reads_each_row_from_the_document`. It now asserts NativeOnly returns [2,3,4,5,6,8] and matches DriverLinq.

## RED / GREEN (EF10, MONGODB_URI/ATLAS_URI unset)

- RED (tests only, before the src change): the class ran 15 failed / 19 passed. All 13 positive tests failed with `NativeTranslationNotSupportedException: Query projects a non-entity result...`.
  - Two of the 15 were bad declines tests: they had no driver-LINQ oracle. I changed them to NativeOnly-only assertions.
  - A third test was rewritten to use constants on both sides.
  - The pre-existing decline tests passed, as expected.
- GREEN:
  - `NativeSetOperationProjectionTests`: 35/35.
  - Unit tests (rebind + alignment): 11/11.
  - Full EF10 solution before the NativeSetOpsTests edit: Unit 1565/1565; Spec 4768 passed / 14 skipped / 0 failed; Functional 4179 passed / 52 skipped / 1 failed. The failure was the stale decline test above.
  - After the edit I reran the relevant classes, not the whole suite: all green (see EF8/EF9 below for the classes).

## Mutation matrix (EF10; each build had 0 errors; restored, and `cmp` matches)

| Mutation | Failing tests |
|---|---|
| none | 0 |
| skip the shaper rebind but still admit (Step 5) | 13 functional positives. Wrong data, only 8s: `holds_a_matching_value_once` gives [8,8] against [8,20]; `keeps_every_copy` gives [8,8,8,8]; the filtered one gives [8,8,8] |
| predicate `=> true` | functional DateTime, enum, and both anonymous declines; 5 unit tests |
| predicate `=> false` (pre-change gate) | all 13 functional positives; unit `Bare_constants_of_round_trip_safe_types_rebind` |
| drop `IsBareScalarOperand` | `Union_of_string_constructor_operands_with_a_constant_argument_declines`; unit `String_constructor_with_a_constant_first_argument_does_not_rebind` |
| drop leaf-kind conjunct | unit `Bare_non_constant_leaves_do_not_rebind` only. **Unreachable from the gate**: HasShaperUnsafeConstantLeaf being true on a bare operand already implies it |
| drop allow-list | `Union_of_enum_constants_declines`, `Union_of_DateTime_parameters_declines`; unit tests for `Nullable_int`, decimal and DateTime |
| drop `TryProbeBareValueRenders` | **none**, even with the allow-list also dropped. See concern 1 |

Anonymous types are rejected by both the bare check and the allow-list, since an anonymous type is never allow-listed. The discriminating case for the bare conjunct is therefore `new string(char, int)`: a string-typed construction whose first ctor argument is a constant.

## Spec suite (EF10, NorthwindSetOperationsQueryMongoTest)

- `MONGODB_EF_NATIVE_ONLY=1`: 32 failing before, 22 after. "Before" was measured with the predicate forced to false, which is semantically the pre-change gate.
- The 10 that flipped are the brief's 5 tests × async:
  - Union_over_constant_function
  - _constant_constant
  - _constant_binary
  - _constant_column
  - Union_over_columns_with_different_nullability
- Diffing the failure lists shows removals only, so there are no new failures.
- Baselines: I regenerated `Union_over_constant_function` and `Union_over_columns_with_different_nullability` with `EF_TEST_REWRITE_BASELINES=1` and a tight filter. I re-indented them to match their neighbours, then rebuilt and reran without the variable.
  - constant_function now uses `$sum:1` in the operand's `$group`.
  - different_nullability now gets `$literal` wraps.
  - constant_binary, constant_column and constant_constant needed no change: the driver-LINQ MQL was identical.
- Default mode: 183/183.

## EF8 / EF9

Debug EF8, EF9 and EF10 all build with 0 warnings and 0 errors. On each I ran NativeSetOperationProjectionTests, NativeSetOpsTests, NativeDateTimeKindReadbackTests, the two unit classes, the SetOperations spec suite and the GroupBy spec suite:

| Version | Unit | Functional | Spec |
|---|---|---|---|
| EF10 | 18 | 221 | 692 (+4 skipped) |
| EF8 | 18 | 221 | 684 (+4 skipped) |
| EF9 | 18 | 221 | 690 (+4 skipped) |

All pass. I did not run the full EF8/EF9 suites. Oddity: the test DLLs report `(net10.0)` under the EF8/EF9 configs too; I didn't investigate.

## Concerns

1. **The `TryProbeBareValueRenders` conjunct is unprovable by mutation.** NativeProjectionBinder has already probed the leaf at bind time, since a failing constant never enters `Select.Projection`, and every allow-listed type always renders. It is kept because the plan mandates it as the stand-in for Select Task 4. It is dead unless someone widens the allow-list or adds a new route into the projection.
2. **The leaf-kind conjunct is reachable only from the unit test.** From the gate it is implied: the conjunct runs only after HasShaperUnsafeConstantLeaf is true, and the operand is bare.
3. **Guid declines before this predicate.** Both the probe and the allow-list were mutated away and the Guid test still declined, so it is pinned but not by this code.
4. **DateTime** in real queries: an inline `new DateTime(2020,1,1)` binds as three ctor arguments, not a constant, so the test captures the values as parameters.
5. **The full EF10 suite was not rerun after the NativeSetOpsTests edit.** Only the classes that touch it were rerun (all green).
6. **Stale comment:** the HasShaperUnsafeConstantLeaf comment still describes the unsafe case in general. The new gate comment explains the exception. The now-stale comment block in NativeSetOpsTests was replaced.
