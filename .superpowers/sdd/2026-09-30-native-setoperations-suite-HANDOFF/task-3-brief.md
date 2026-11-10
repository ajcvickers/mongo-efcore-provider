### Task 3 (J): A join-scope source1 in a projected set operation

**Flips:** Concat_with_one_side_being_GroupBy_aggregate.

**Files:**
- Modify: MQTEV `IsPlainProjectedSelect` (the `!mongo.IsJoinQuery` at `:3124`). Admit it only when
  `allowPreCombineLookups` holds **and** the select is a confirmed single-level join. Reuse the existing
  `IsSingleEligibleNativeJoinScope` / `HasConfirmedJoinLookup` predicates; do not restate them.
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs`, the OperandsProjected
  branch (`:84-108`). After `AppendLookupStages`, emit `PostJoinOps` and `PostLookupPagingOps`, which today are emitted
  only when `SetOperation == null` (`:65-76`).
- Test: `NativeCombinationQueryTests.cs` or `NativeGroupByOverJoinTests.cs` (extend)

- [ ] **Step 1: Write the failing tests.**
  - `Orders.Where(o => o.Customer.City == "<a city that matches>").Select(o => new { o.OrderDate }).Union(Orders.GroupBy(...).Select(...))`,
    with a hand oracle and `NativeAndParity`.
  - The same with a city that matches nothing.
  - The same with `Skip`/`Take` on the join side.
  - **source2** as a join must still `DeclinesCleanly`, because operand lowering has no lookup plumbing.
- [ ] **Step 2: Run them and confirm they fail.**
- [ ] **Step 3: Implement both halves together.** Without the lowerer half, the inner-side `$match` is silently dropped
  and extra rows come back. The test with the matching city is what catches that.
- [ ] **Step 4: Run and confirm they pass.** Run `NorthwindJoinQueryMongoTest` and `NorthwindGroupByQueryMongoTest` in
  default mode and check for no data regressions.
- [ ] **Step 5: Mutation.** Remove the lowerer's `PostJoinOps` emission and the matching-city test must fail with extra rows.
- [ ] **Step 6: Commit.** Message: `EF-322: native projected set operation over a join-scope source1`.

