# Task 2 Report: Verify (and fix if needed) the Inner-leaf / LeftJoin case

## Summary

Task 2 verification is **COMPLETE** with **no code changes required**. Task 1's fix was sufficient for the Inner-leaf/LeftJoin case.

## Step 1: Run Inner-leaf test in isolation

**Command:**
```bash
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join"
```

**Result:**
```
Test run for /Users/arthur.vickers/code/provider2/tests/MongoDB.EntityFrameworkCore.FunctionalTests/bin/Debug EF10/net10.0/MongoDB.EntityFrameworkCore.FunctionalTests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 366 ms - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)
```

**Outcome:** ✓ PASS — The Inner-leaf test passed without any failures or exceptions.

## Step 2: Run whole NativeJoinTests.cs file

**Command:**
```bash
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"
```

**Result:**
```
Test run for /Users/arthur.vickers/code/provider2/tests/MongoDB.EntityFrameworkCore.FunctionalTests/bin/Debug EF10/net10.0/MongoDB.EntityFrameworkCore.FunctionalTests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    53, Skipped:     0, Total:    53, Duration: 2 s - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)
```

**Outcome:** ✓ PASS — All 53 tests in the file passed with zero failures and zero regressions.

## Step 3: Commit

**Outcome:** SKIPPED — No code changes were required. Step 1 passed with no modifications to any source files. The two acceptance tests (`Whole_outer_entity_leaf_that_is_also_reference_included_goes_native` and `Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join`) were already committed in Task 1's final commit (3415e526).

## Files Modified

**None.** No source files under `src/` were modified. No commits were created.

## Technical Notes

The test's Include target (`Order.OrderLines`) is unrelated to the join's own key (`Owner.Id`/`Order.OwnerId`). This means the alias-collision hazard that Task 1 Step 6 addressed does not apply to this case. Task 1's fix to the `BindResultMember` logic in `MongoQueryableMethodTranslatingExpressionVisitor.cs` and `MongoProjectionBindingExpressionVisitor.cs` is sufficiently general to handle both:

1. **Outer-entity leaf** (Task 1 direct case): Include target related to join key
2. **Inner-entity leaf** (Task 2 case): Include target unrelated to join key

The unwrap/rewrap/bridge logic does not branch on scope index, making it applicable to both scenarios.

## Conclusion

Task 2 verification confirms that Task 1's implementation correctly and completely addresses the Include materialization for Include-wrapped whole-entity join-scope leaves under left joins, for both outer and inner cases. No further work is required.
