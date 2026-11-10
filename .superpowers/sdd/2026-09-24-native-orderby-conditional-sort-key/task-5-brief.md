### Task 5: Regenerate the two target spec-test baselines and sweep for related staleness

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs` (`Repro9735` and `Include_collection_with_multiple_conditional_order_by`)
- Modify (as needed, see Step 4): the same two test methods in `NorthwindStringIncludeQueryMongoTest.cs`, `NorthwindIncludeNoTrackingQueryMongoTest.cs`, `NorthwindEFPropertyIncludeQueryMongoTest.cs` — Phase 1 found these sibling classes share near-identical Include test method bodies against equivalent models, so the same staleness pattern may recur there.

**Interfaces:**
- Consumes: `EF_TEST_REWRITE_BASELINES=1` (documented in `tests/MongoDB.EntityFrameworkCore.SpecificationTests/AGENTS.md`), scoped to exactly one `FullyQualifiedName` per run per this plan's Global Constraints.
- Produces: updated `AssertMql(...)` baseline strings; no code interface for later tasks.

- [ ] **Step 1: Build EF10 and confirm the two target tests now fail only on their MQL baseline, not on translation**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest.Repro9735"
```

Expected: **FAIL** with an `Assert.Equal` string-mismatch (NOT a `NativeTranslationNotSupportedException`) — this confirms the query now translates natively and only the recorded baseline text is stale. If it still throws `NativeTranslationNotSupportedException`, stop: Task 3's fix did not reach this shape and needs debugging before continuing (Repro9735's actual shape is `OrderBy(b => b.Customer.CustomerID != null).ThenBy(b => b.Customer != null ? b.Customer.CustomerID : string.Empty)` — the FIRST key is not itself a ternary, only a boolean null-check; re-read `TryTranslateComputedSortKey`, which runs BEFORE the new conditional arm, to confirm it does or does not already handle a bare `!= null` boolean sort key, since that key might need its own small extension this plan did not anticipate).

- [ ] **Step 2: Regenerate `Repro9735`'s baseline, scoped to exactly this one test**

```bash
EF_TEST_REWRITE_BASELINES=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest.Repro9735"
```

The run reports failed — that is the expected rewrite signal. Immediately check for corruption, then diff:

```bash
grep -n '^"""\)' tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs | grep -v '""");'
git diff tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs
```

Expected: the grep for the corruption signature returns **nothing**. The diff shows a new `AssertMql(...)` string for `Repro9735` only. Remove the stale `// Failed: Throws ExpressionNotSupportedException (query not translated)` comment immediately above `await base.Repro9735(async);` (per the spec-test AGENTS.md convention: `// Failed:` markers are removed once fixed, never added new).

- [ ] **Step 3: Rebuild and rerun without the rewrite flag to confirm green**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest.Repro9735"
```

Expected: **PASS**. Repeat Steps 1-3 for `Include_collection_with_multiple_conditional_order_by` (same file, same commands with the filter changed to that method name — it has no `// Failed:` marker to remove).

- [ ] **Step 4: Sweep the sibling Include spec-test classes for the same two tests**

Phase 1 found `NorthwindStringIncludeQueryMongoTest.cs`, `NorthwindIncludeNoTrackingQueryMongoTest.cs`, and `NorthwindEFPropertyIncludeQueryMongoTest.cs` carry near-duplicate Include test bodies. Check each for the same two methods and their current status:

```bash
grep -n "public override async Task Repro9735\|public override async Task Include_collection_with_multiple_conditional_order_by" \
  tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindStringIncludeQueryMongoTest.cs \
  tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeNoTrackingQueryMongoTest.cs \
  tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindEFPropertyIncludeQueryMongoTest.cs
```

For each match found, repeat Steps 1-3 (build, scoped single-test `EF_TEST_REWRITE_BASELINES=1` rewrite, corruption grep, diff, rebuild, rerun green) against that file and test name. If a class doesn't override one or both methods at all (inheriting the base class's own un-overridden behavior, or the method doesn't exist in that base — check by grepping the class file for the method name with no match), skip it — there is nothing to regenerate.

- [ ] **Step 5: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs
# Add any sibling files actually changed in Step 4 — only ones with a real diff.
git commit -m "EF-322: regenerate Repro9735 / Include_collection_with_multiple_conditional_order_by baselines"
```

---

