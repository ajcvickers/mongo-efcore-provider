### Task 2: Verify (and fix if needed) the Inner-leaf / LeftJoin case

**Files:**
- Test (already exists, uncommitted from Task 1's commit): `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`
- Modify (only if Step 2 below finds a gap): the same two `src/` files as Task 1.

**Interfaces:**
- Consumes: Task 1's fix, unconditionally over Outer/Inner (the unwrap/rewrap/bridge logic in `BindResultMember`
  does not branch on which scope index the leaf resolved to).

- [ ] **Step 1: Run the Inner-leaf test in isolation**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join"
```

Expected: PASS. This test's Include target (`Order.OrderLines`) is UNRELATED to the join's own key
(`Owner.Id`/`Order.OwnerId`), so the alias-collision hazard Task 1 Step 6 addressed does not apply here — Task
1's fix alone should be sufficient. If it FAILS, read the failure: a data mismatch on `LineCount` specifically
(not `Total`) would suggest a DIFFERENT collision (verify with the same MQL-dump technique as Task 1 Step 6
before assuming it's the same fix); anything else (an exception, a `Total` mismatch) means Task 1's fix has a
gap for the LeftJoin/Inner-leaf combination specifically — use `superpowers:systematic-debugging`, don't
extend Task 1 Step 6's fix blindly onto a different failure.

- [ ] **Step 2: Run the whole `NativeJoinTests.cs` file**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"
```

Expected: PASS, every test in the file, 0 failures.

- [ ] **Step 3: Commit (only if Step 1 required a code change; otherwise this task has nothing new to commit)**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
  src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs
git commit -m "EF-322: fix Include materialization for the LeftJoin Inner-leaf case"
```

If Step 1 passed with no code changes, skip this step — there is nothing to commit, and the two functional
tests were already committed in Task 1.

---

