### Task 4: Unblock the original plan's Task 4 — regenerate the two Northwind spec baselines

**Files:** none created — regenerates existing `AssertMql` baselines and runs verification only.
- Modify (via `EF_TEST_REWRITE_BASELINES=1`, not by hand):
  `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs`,
  `NorthwindStringIncludeQueryMongoTest.cs`, `NorthwindIncludeNoTrackingQueryMongoTest.cs`,
  `NorthwindEFPropertyIncludeQueryMongoTest.cs`

**Interfaces:**
- Consumes: Tasks 1–3, committed.

- [ ] **Step 1: Confirm both target tests now pass under NativeOnly, for EF10**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest.Include_reference_when_entity_in_projection|FullyQualifiedName~NorthwindIncludeQueryMongoTest.Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join" \
  --logger "console;verbosity=normal"
```

Expected: both FAIL — NOT with `NativeTranslationNotSupportedException`, and NOT with a `FormatException` or a
wrong-data assertion failure from EF Core's own base test body (that would mean Tasks 1–3 didn't actually fix
the general case, only the two specific functional-test shapes — STOP and use
`superpowers:systematic-debugging`). The only acceptable failure here is an `AssertMql` mismatch: the query now
runs correctly and returns correct data, it just emits DIFFERENT MQL than the stale, pre-fix baseline recorded.

- [ ] **Step 2: Regenerate `Include_reference_when_entity_in_projection`'s baseline, one class and one test at a time**

```bash
for cls in NorthwindIncludeQueryMongoTest NorthwindStringIncludeQueryMongoTest NorthwindIncludeNoTrackingQueryMongoTest NorthwindEFPropertyIncludeQueryMongoTest; do
  EF_TEST_REWRITE_BASELINES=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
    -c "Debug EF10" --no-build --filter "FullyQualifiedName~${cls}.Include_reference_when_entity_in_projection"
done
```

Expected: each run reports "Failed" (the rewrite signal — see
`tests/MongoDB.EntityFrameworkCore.SpecificationTests/AGENTS.md`). Do NOT proceed without checking each file.

- [ ] **Step 3: Check for corruption, then diff**

```bash
git diff --stat tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/
for f in NorthwindIncludeQueryMongoTest NorthwindStringIncludeQueryMongoTest NorthwindIncludeNoTrackingQueryMongoTest NorthwindEFPropertyIncludeQueryMongoTest; do
  grep -n '^""")[^;]' "tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/${f}.cs"
done
git diff tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs
```

Expected: the corruption-signature `grep` prints NOTHING for any file (exit code 1). The diff shows exactly ONE
changed `AssertMql(...)` block per file, all four with the SAME new MQL shape. If corruption IS found, stop —
restore from git before re-running the rewrite more narrowly.

- [ ] **Step 4: Repeat Steps 2–3 for `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join`**

Same loop, same corruption check, substituting the test name.

- [ ] **Step 5: Rebuild and confirm both tests pass, for EF10**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest.Include_reference_when_entity_in_projection|FullyQualifiedName~NorthwindIncludeQueryMongoTest.Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join"
```

Expected: PASS, 2/2 (or 4/4 counting `async: true/false` for each).

- [ ] **Step 6: Repeat Steps 1–5 for EF9 and EF8**

Same commands with `-c "Debug EF9"` then `-c "Debug EF8"`. Since this fix touches no `#if`-guarded code, expect
the identical outcome on all three versions.

- [ ] **Step 7: Commit the baseline changes**

```bash
git add tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs \
  tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindStringIncludeQueryMongoTest.cs \
  tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeNoTrackingQueryMongoTest.cs \
  tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindEFPropertyIncludeQueryMongoTest.cs
git commit -m "EF-322: regenerate Include_reference_when_entity_in_projection / Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join baselines"
```

---

