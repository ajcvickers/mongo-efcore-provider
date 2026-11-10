### Task 3: Regenerate MQL baselines across the Include/Join spec-test surface

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs`
- Modify (as needed, see Step 1): `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindStringIncludeQueryMongoTest.cs`, `NorthwindIncludeNoTrackingQueryMongoTest.cs`, `NorthwindEFPropertyIncludeQueryMongoTest.cs`, `NorthwindJoinQueryMongoTest.cs`, `NorthwindMiscellaneousQueryMongoTest.cs`

**Interfaces:**
- Consumes: `EF_TEST_REWRITE_BASELINES=1` env var (documented in `tests/MongoDB.EntityFrameworkCore.SpecificationTests/AGENTS.md`) — when set, a failing `AssertMql(...)` call rewrites its own baseline in place from captured MQL, and the test still reports failed (that's the "a rewrite happened" signal).
- Produces: updated `AssertMql(...)` baseline strings in the affected test files, and (where EF8/EF9 output now matches EF10's exactly) removal of the now-redundant `#if EF8 || EF9 ... #else ... #endif` conditional around the assertion, collapsing to a single unconditional `AssertMql(...)` call.

This task is inherently generative — the new native MQL text cannot be hand-authored correctly without running the translator, so every baseline change in this task must come from `EF_TEST_REWRITE_BASELINES=1`, never typed by hand.

- [ ] **Step 1: Build EF9 and find the current NativeOnly failure set for the Include spec classes**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest|FullyQualifiedName~NorthwindStringIncludeQueryMongoTest|FullyQualifiedName~NorthwindIncludeNoTrackingQueryMongoTest|FullyQualifiedName~NorthwindEFPropertyIncludeQueryMongoTest|FullyQualifiedName~NorthwindJoinQueryMongoTest|FullyQualifiedName~NorthwindMiscellaneousQueryMongoTest" \
  --logger "console;verbosity=normal" > /tmp/ef9_include_after_fix.txt 2>&1
grep "^  Failed" /tmp/ef9_include_after_fix.txt | grep -oE "[A-Za-z0-9_]+Test\.[A-Za-z0-9_]+" | sort -u
```

Compare this list against the known Phase-2-scoped shared gaps (`Include_reference_when_entity_in_projection`, `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join`, `Include_with_complex_projection_does_not_change_ordering_of_projection`, `Repro9735`, `Include_collection_with_multiple_conditional_order_by`) plus `Include_property`/`Include_property_after_navigation`/`Include_property_expression_invalid`/`Then_include_property_expression_invalid`/`Include_specified_on_non_entity_not_supported` (these assert `AssertMql()` with no arguments — i.e., they expect an exception before any MQL is logged — and are unaffected by this fix either way). Any name in the grep output **not** in that expected set is new information: read that test's current override in its source file before Step 2 to understand what still needs fixing, and if it looks unrelated to the LeftJoin-shim gap, stop and flag it to your reviewer rather than guessing at a fix — it's outside this plan's diagnosed root cause.

- [ ] **Step 2: Regenerate baselines for the classes with real fallback-driven MQL differences**

For each spec class from Step 1 that still shows failures beyond the expected Phase-2 set, or that has visibly different `#if EF8 || EF9 ... #else ... #endif` MQL branches in its source, run a scoped rewrite:

```bash
EF_TEST_REWRITE_BASELINES=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF9" --no-build --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest"
```

The run reports failed — that is the expected rewrite signal, not a problem. Immediately diff what changed:

```bash
git diff tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs
```

For each changed test method, check whether the diff makes the `#if EF8 || EF9` branch's `AssertMql(...)` string byte-for-byte identical to the `#else` (EF10) branch's string. If so, collapse the `#if`/`#else`/`#endif` into a single unconditional `AssertMql(...)` call using the (now-shared) MQL text, and delete the redundant duplicate. If the two branches still differ after regeneration, leave both branches in place — that's a genuine remaining version difference, not a leftover from this fix.

Also check for now-obsolete `// Failed: ...` comments (the transitional marker for not-yet-rebaselined Include work) on any test this task fixes — remove them, per the spec-test convention that `// Failed:` markers are removed once fixed and never added new. Leave `// Fails: ...EF-XXXX` comments alone (those mark durable, ticketed gaps unrelated to this fix).

Repeat the rewrite + diff + collapse-or-keep process for each of the other classes named in Task 3's file list that showed affected output in Step 1.

- [ ] **Step 3: Rebuild and rerun without the rewrite flag to confirm green**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest|FullyQualifiedName~NorthwindStringIncludeQueryMongoTest|FullyQualifiedName~NorthwindIncludeNoTrackingQueryMongoTest|FullyQualifiedName~NorthwindEFPropertyIncludeQueryMongoTest|FullyQualifiedName~NorthwindJoinQueryMongoTest|FullyQualifiedName~NorthwindMiscellaneousQueryMongoTest"
```

Expected: **all PASS** (normal mode, fallback allowed — this proves the new baselines are correct, independent of whether the query went native).

- [ ] **Step 4: Repeat Steps 1-3 for EF8**

Same commands, `-c "Debug EF8"` throughout. EF8 and EF9 share the exact same shim and candidate-recording code path, so expect the same set of affected tests and the same baseline outcomes; if EF8's results diverge from EF9's for any test, treat that as new information to flag rather than assume it's the same fix.

- [ ] **Step 5: Confirm EF10 is untouched**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest|FullyQualifiedName~NorthwindStringIncludeQueryMongoTest|FullyQualifiedName~NorthwindIncludeNoTrackingQueryMongoTest|FullyQualifiedName~NorthwindEFPropertyIncludeQueryMongoTest|FullyQualifiedName~NorthwindJoinQueryMongoTest|FullyQualifiedName~NorthwindMiscellaneousQueryMongoTest"
```

Expected: **all PASS**, with zero diff to any `#else`/unconditional EF10 baseline text (only the EF8/EF9 side of any collapsed conditional should have changed in Step 2). Run `git diff --stat` over the same file list and confirm no line inside an `#if !EF8 && !EF9` or EF10-only region changed.

- [ ] **Step 6: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindStringIncludeQueryMongoTest.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeNoTrackingQueryMongoTest.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindEFPropertyIncludeQueryMongoTest.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindJoinQueryMongoTest.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindMiscellaneousQueryMongoTest.cs
git commit -m "EF-322: regenerate EF8/EF9 Include/Join MQL baselines now that reference-Include goes native"
```

(Only `git add` the files that actually changed in Steps 2-4; skip any from the list above with no diff.)

---

