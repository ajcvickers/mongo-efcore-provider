### Task 1 (A): Bare-scalar operand alias alignment

**Flips:** Union_over_column_function, _column_constant, _column_binary, _function_column, _binary_column and
_scalarsubquery_column. Also enables the A half of column_unary, unary_column and constant_column.

**Files:**
- Modify: MQTEV `TryTranslateSetOperation` (admitted branch, `:3024-3036`) and `ProjectionShapesMatch` (`:3173-3196`)
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeSetOperationProjectionTests.cs` (new, or extend `NativeCombinationQueryTests.cs`)
- Baselines: `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindSetOperationsQueryMongoTest.cs`

**Interfaces:**
- Produces: `private static bool IsBareScalarOperand(ShapedQueryExpression source, MongoQueryExpression mongo)`. It is
  true when `mongo.Select.Projection.Count == 1` and the shaper is **not** a `NewExpression`/`MemberInitExpression`.
  Detect this by shape. `IsBareProjection` is not set for a grouped `g => g.Count()`.
- Produces: `private static bool CanAlignBareScalarAliases(ShapedQueryExpression s1, MongoQueryExpression m1, ShapedQueryExpression s2, MongoQueryExpression m2)`,
  which the gate uses. The rewrite runs only when the gate admitted through it.

- [ ] **Step 1: Write the failing functional tests.** Seed an entity with `int Id` and `int N`, and cover:
  - `Select(x => x.Id).Union(Select(x => x.N + 1))`
  - `Select(x => x.N + 1).Union(Select(x => x.Id))`
  - `Select(x => x.Id).Concat(Select(x => x.N * 2))`, where Concat must keep duplicates
  - column over a `GroupBy(x => x.N).Select(g => g.Count())`
  - a DateTime column union a DateTime computed value (`x.D.AddDays(1)`), with a `Local`-kind case
  - `DeclinesCleanly` control: `Select(x => new { A = x.Id }).Union(Select(x => new { B = x.N }))`. This cannot be built
    with anonymous types, so use two DTOs with different member names through a common base, or skip it and rely on
    `ProjectionShapesMatch` staying strict for `New`/`MemberInit` (then assert it with a unit test on the predicate).

  ```csharp
  [Fact]
  public void Union_of_column_and_computed_scalar_goes_native()
  {
      var collection = Seed(nameof(Union_of_column_and_computed_scalar_goes_native));
      var results = NativeModeAssert.NativeAndParity(mode =>
      {
          using var db = CreateContext(collection, mode);
          return db.Entities.Select(x => x.Id).Union(db.Entities.Select(x => x.N + 1)).OrderBy(v => v).ToList();
      });
      Assert.Equal(new[] { 1, 2, 3, 11, 21 }, results); // hand oracle for the seed; adjust to the seed
  }
  ```
- [ ] **Step 2: Run them and confirm they fail.** Expect `NativeTranslationNotSupportedException` under NativeOnly.
- [ ] **Step 3: Implement.**
  - Replace `ProjectionShapesMatch(...)` in the gate with
    `(ProjectionShapesMatch(...) || CanAlignBareScalarAliases(source1, mongo1, source2, mongo2))`.
  - Inside the admitted branch, **before** `AppendSetOperation`, when the aliases differ: replace mongo2's single
    projection with one that carries mongo1's alias and the same expression and source. Source1's shaper already
    reads source1's alias.
  - `MongoPipelineFactory.RenderProject` (`:307-331`) already renders both `{_id: {$add…}}` and `{_v: "$_id", _id: 0}`.
    A GroupBy operand's flattening `$project` also works (`{_id: "$_v"}`). Confirm that from the MQL of each test.
  - Run the re-alias **before** anything that compares operand projections by alias, including `NativeDateTimeKindReadBack`.
- [ ] **Step 4: Run the tests and confirm they pass.** Also run the spec suite under NativeOnly and expect the 6 A-only
  tests to flip. Regenerate the baselines that differ (expect column_binary, column_constant, column_function and function_column).
- [ ] **Step 5: Mutation.** Make `CanAlignBareScalarAliases` return true without the `Count == 1` / non-New checks, and the
  anonymous-type control (or the predicate unit test) must fail. Restore it.
- [ ] **Step 6: Run the rest in default mode.** Run the SetOps suite and `NorthwindGroupByQueryMongoTest` (which has
  Union-over-GroupBy tests) in default mode. Any MQL diff must be explained.
- [ ] **Step 7: Commit.** Message: `EF-322: native Union/Concat of bare scalar operands with different aliases`.

