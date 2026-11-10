### Task 6: Full regression sweep across all three EF versions

**Files:** none modified — this task only runs tests. If it surfaces a regression, use `superpowers:systematic-debugging` before attempting a fix; do not patch around a red suite.

**Interfaces:**
- Consumes: everything from Tasks 1-5.
- Produces: the evidence that Phase 2 Group A is complete.

- [ ] **Step 1: NativeOnly sweep over the full Query suite for EF10**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query" --logger "console;verbosity=normal" > /tmp/ef10_spec_query_nativeonly.txt 2>&1
tail -8 /tmp/ef10_spec_query_nativeonly.txt
grep "^  Failed" /tmp/ef10_spec_query_nativeonly.txt | grep -oE "[A-Za-z0-9_]+Test\.[A-Za-z0-9_]+" | sort -u
```

Expected: `Repro9735` and `Include_collection_with_multiple_conditional_order_by` (and any sibling-class copies fixed in Task 5) no longer appear in the failure list. The remaining failures are the 3 other original Phase-2 gaps not addressed by this plan (`Include_reference_when_entity_in_projection`, `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join`, `Include_with_complex_projection_does_not_change_ordering_of_projection` — deliberately out of scope, "Group B") plus any pre-existing, unrelated failures already known from Phase 1 (`NorthwindMiscellaneousQueryMongoTest`'s `Contains_over_concatenated_columns_both_fixed_length`, `OrderBy_object_type_server_evals`, `Projection_skip_projection`, `Projection_skip_take_projection`, `Projection_take_projection`). Any NEW name in this list beyond that known set is a regression — stop and investigate via `superpowers:systematic-debugging` rather than proceeding.

- [ ] **Step 2: Repeat Step 1 for EF9 and EF8**

Same commands with `-c "Debug EF9"` then `-c "Debug EF8"`. Expect the identical failure-set delta (the two target tests fixed, same known-unrelated set remaining) on all three versions, since this fix touches version-agnostic code (no `#if EF8`/`#if EF9` guards were added anywhere in this plan).

- [ ] **Step 3: Full three-version regular (non-NativeOnly) test-all run**

```bash
/test-all
```

Expected: green across EF8, EF9, and EF10 for the full solution (unit, functional, and specification test projects), excluding the 5 known pre-existing, unrelated `NorthwindMiscellaneousQueryMongoTest` gaps documented in Phase 1's own final report.

- [ ] **Step 4: Final report**

Summarize, for your human partner or the branch reviewer: confirmation that `Repro9735` and `Include_collection_with_multiple_conditional_order_by` now pass under `NativeOnly` on all three EF versions; which sibling spec-test classes (if any) needed the same baseline regeneration in Task 5 Step 4; and a reminder that Phase 2's remaining "Group B" (`Include_reference_when_entity_in_projection`, `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join`, `Include_with_complex_projection_does_not_change_ordering_of_projection`) is a separate, not-yet-planned unit of work — an Include'd entity wrapped inside a non-entity (anonymous) projection, hitting `MongoShapedQueryCompilingExpressionVisitor`'s generic "projects a non-entity result" catch-all rather than the ORDER BY gap this plan fixed. No commit for this task — it's verification only.
