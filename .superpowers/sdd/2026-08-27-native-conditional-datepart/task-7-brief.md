### Task 7: Close out `BuiltInDataTypesMongoTest.Optional_datetime_reading_null_from_database`

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Mapping/BuiltInDataTypesMongoTest.cs`

**Interfaces:**
- Consumes: everything from Tasks 1-6.

- [ ] **Step 1: Confirm the current override still just delegates to `base`**

```bash
grep -n "Optional_datetime_reading_null_from_database" -A 1 tests/MongoDB.EntityFrameworkCore.SpecificationTests/Mapping/BuiltInDataTypesMongoTest.cs
```

Expected (two overrides, one per `#if !EF8`/`#else` branch — see the file's existing structure): `public override Task Optional_datetime_reading_null_from_database() => base.Optional_datetime_reading_null_from_database();` and the EF8 `void` equivalent. No change needed here — this override was never modified to decline under `NativeOnly` (that change was proposed and then explicitly reverted earlier in this same investigation), so it should now simply start passing.

- [ ] **Step 2: Run it under `NativeOnly` on EF10**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && MONGODB_EF_NATIVE_ONLY=1 dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~BuiltInDataTypesMongo"`
Expected: PASS, all 30 tests (this was the last of the six originally-failing tests from the earlier investigation in this session; `Can_read_back_bool_mapped_as_int_through_navigation` was explicitly left alone per prior instruction, and the two enum-cast tests were already fixed and committed under EF-215).

- [ ] **Step 3: Repeat for EF8 and EF9**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
MONGODB_EF_NATIVE_ONLY=1 dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" --no-build --filter "FullyQualifiedName~BuiltInDataTypesMongo"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
MONGODB_EF_NATIVE_ONLY=1 dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~BuiltInDataTypesMongo"
```

Expected: PASS on both (EF8/EF9 never had the `Can_read_back_bool_mapped_as_int_through_navigation` gap to begin with — that one's already declared `AssertTranslationFailed` on those two versions — so this test file should now be **fully green under `NativeOnly` on EF8 and EF9**, and green except for the one deliberately-untouched navigation test on EF10).

- [ ] **Step 4: Run the full three-EF-version regression suite**

Run the `/test-all` skill, or manually:

```bash
for cfg in EF8 EF9 EF10; do
  dotnet build MongoDB.EFCoreProvider.sln -c "Debug $cfg"
  dotnet test MongoDB.EFCoreProvider.sln -c "Debug $cfg" --no-build
done
```

Expected: PASS, zero `Passed → Failed` regressions across all three configurations' unit/functional/spec suites.

- [ ] **Step 5: No commit needed for this task**

This task is verification-only — Task 6 (or an earlier task) already committed the change that makes this test pass. If Step 2/3 reveal a failure, return to the relevant earlier task and fix it there (with its own commit), then re-run this task's verification.

---

