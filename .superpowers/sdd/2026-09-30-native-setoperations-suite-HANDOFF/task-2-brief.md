### Task 2 (C): A server-rendered constant or parameter leaf on source1

**Flips:** Union_over_constant_function, _constant_constant, _constant_binary,
Union_over_columns_with_different_nullability, and (with Task 1) Union_over_constant_column.

**Files:**
- Modify: MQTEV `TryTranslateSetOperation` (the gate at `:3026` and the admitted branch) and `HasShaperUnsafeConstantLeaf` (`:3167`)
- Test: the same functional class as Task 1

**Interfaces:**
- Consumes (if Select Task 4 has landed): `MongoSelectDefinition.HasClientEvaluatedProjectionLeaf`, which must be false.
- Produces: `private static bool CanRebindConstantLeafToDocument(ShapedQueryExpression source1, MongoQueryExpression mongo1)`.
  It is true when:
  - source1 is a bare scalar (Task 1's `IsBareScalarOperand`);
  - its single projection is `MongoConstantExpression` or `MongoParameterExpression`;
  - `NativeSlotPopulator.TryProbeBareValueRenders` succeeds;
  - the CLR type is in the round-trip-safe allow-list (Step 3).

- [ ] **Step 1: Write the failing tests.** Cover exact values for each of:
  - `Select(x => 8).Union(Select(x => x.N))`, including a row where `N == 8`, so the Union holds one 8 and the Concat holds two
  - `Select(x => "A").Concat(Select(x => (string)null))`
  - `Select(x => p).Union(Select(x => x.N))` with `int p` captured, so it is a parameter leaf
  - `DeclinesCleanly` for a constant `DateTime`, a `Guid` and an enum constant on source1, unless Step 3 proves they round-trip
  - `DeclinesCleanly` for `Select(x => new { A = 8, x.N }).Union(…)`: a constant inside an anonymous type stays out of scope
- [ ] **Step 2: Run them and confirm they fail.**
- [ ] **Step 3: Implement.**
  - In the gate, replace `!HasShaperUnsafeConstantLeaf(mongo1)` with
    `(!HasShaperUnsafeConstantLeaf(mongo1) || CanRebindConstantLeafToDocument(source1, mongo1))`.
  - In the admitted branch, when the rebind applies, point source1's shaper at the document instead of the baked-in constant:

    ```csharp
    source1 = source1.UpdateShaperExpression(
        BindSelectManyMember(mongo1, mongo1.Select.Projection[0].Alias, source1.ShaperExpression));
    ```

    This is the triage prototype; confirm that `BindSelectManyMember` is the right binder for a scalar alias read, and
    if it isn't, use the one Task 1's column side goes through.
  - The allow-list covers what the prototype proved: `int`, `long`, `double`, `bool`, `string` and `null` string. Add
    `decimal`, `Guid`, enums and `DateTime` only after writing their round-trip test.
- [ ] **Step 4: Run and confirm they pass.** Regenerate baselines: constant_function and
  columns_with_different_nullability (it gets a `$literal` wrap).
- [ ] **Step 5: Mutation.** Skip the shaper rebind while still admitting, and `Select(x => 8).Union(Select(x => x.N))` must
  return only 8s. That is the wrong-data trap this guard exists for. Restore it.
- [ ] **Step 6: Commit.** Message: `EF-322: native set operations with a constant/parameter bare operand on source1`.

