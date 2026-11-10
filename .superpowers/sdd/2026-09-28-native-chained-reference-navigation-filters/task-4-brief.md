### Task 4: Spec suite — flip the six tests, rebaseline, sweep all EF versions

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindNavigationsQueryMongoTest.cs`
  (baselines only, and only if they change)
- Possibly modify: other `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/*MongoTest.cs` baselines for
  tests that now also go native

**Interfaces:**
- Consumes: Tasks 1–3.
- Produces: nothing.

- [ ] **Step 1: Run the target class under NativeOnly**

```bash
MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindNavigationsQueryMongoTest"
```

Expected: `Failed: 0, Passed: 147` (baseline mismatches don't fail here if the MQL is unchanged; if any test fails
**only** on `AssertMql`, go to Step 3). If `Select_Where_Navigation_Deep` still throws
`NativeTranslationNotSupportedException`, the likeliest cause is that its `Take(1)` arrived after the confirming
Select and hit the post-confirmed-join guard in `PopulateNativeSlots`. Confirm that with a stack trace, stop and
report. Don't loosen the guard.

- [ ] **Step 2: Run the target class in default mode**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindNavigationsQueryMongoTest"
```

Expected: 147 passed. Any failure is either a baseline mismatch (Step 3) or a data failure, which means stop and
debug it.

- [ ] **Step 3: Regenerate baselines for the mismatching overrides only**

For each failing test method `<M>` (expect some subset of the six):

```bash
EF_TEST_REWRITE_BASELINES=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindNavigationsQueryMongoTest.<M>"
git diff tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindNavigationsQueryMongoTest.cs
```

The rewriting run still reports the test as failed; that's expected. Review the diff: each new pipeline must have
two `$lookup`/`$unwind` blocks, with the filter's `$match` **after** both (and `$sort`/`$limit` after the `$match`
for `Select_Where_Navigation_Deep`). `Select_Where_Navigation_Null_Deep` must test the second-level alias
(`_lookup_Manager_1`), not `_lookup_Manager`. Then rebuild and rerun Steps 1–2 until both are green.

- [ ] **Step 4: Sweep the whole spec suite on EF10, both modes**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj -c "Debug EF10" --no-build
MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj -c "Debug EF10" --no-build
```

Other Northwind classes may contain multi-hop filters that now go native, and their MQL can shift. For each new
default-mode failure that is **only** an `AssertMql` mismatch, rebaseline it as in Step 3 with a tight `--filter`
and review the diff. For a data failure, stop and debug. The NativeOnly run should fail on strictly fewer tests
than before this plan (compare with a run on the pre-Task-1 commit if in doubt), and on no test that passed before.

- [ ] **Step 5: Run all three EF versions**

Invoke the `/test-all` skill. Then run the target class under NativeOnly on EF8 and EF9. Those versions lower
optional references through the `LeftJoin` shim, a different join-recording path:

```bash
for v in EF8 EF9; do
  dotnet build MongoDB.EFCoreProvider.sln -c "Debug $v"
  MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
    -c "Debug $v" --no-build --filter "FullyQualifiedName~NorthwindNavigationsQueryMongoTest"
done
```

Expected: all green apart from the known pre-existing EF8/EF9 `OfType_Select*` NativeOnly failures (Global
Constraints). If one of the six still falls back only on EF8/EF9, report which one. Don't add an `#if` to paper over
it without flagging it.

- [ ] **Step 6: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/
git commit -m "EF-322: native multi-hop reference-navigation filters in NorthwindNavigationsQueryMongoTest; rebaseline MQL"
```

Only commit if there are changes; if no baseline changed, skip this step and say so.
