### Task 5: Full regression sweep across EF8/EF9/EF10

**Files:** none — verification only.

- [ ] **Step 1: `MONGODB_EF_NATIVE_ONLY=1` full `~Query` sweep, EF10**

```bash
MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query" --logger "console;verbosity=normal" > /tmp/ef10_include_materialization_nativeonly.txt 2>&1
tail -8 /tmp/ef10_include_materialization_nativeonly.txt
grep "^  Failed" /tmp/ef10_include_materialization_nativeonly.txt | grep -oE "[A-Za-z0-9_]+Test\.[A-Za-z0-9_]+" | sort -u
```

Expected failure set: `Include_with_complex_projection_does_not_change_ordering_of_projection` (Root Cause B,
out of scope) plus the same pre-existing, unrelated `NorthwindMiscellaneousQueryMongoTest` failures already
known from the prior plan's own final report (`Contains_over_concatenated_columns_both_fixed_length`,
`OrderBy_object_type_server_evals`, `Projection_skip_projection`, `Projection_skip_take_projection`,
`Projection_take_projection`). Any NEW name beyond that known set is a regression — STOP and use
`superpowers:systematic-debugging`.

- [ ] **Step 2: Repeat Step 1 for EF9 and EF8**

Same commands with `-c "Debug EF9"` then `-c "Debug EF8"`. Expect the identical failure-set delta.

- [ ] **Step 3: Full three-version regular (non-NativeOnly) test-all run**

```bash
/test-all
```

Expected: green across EF8, EF9, and EF10 for the full solution (unit, functional, and specification test
projects), excluding the same known pre-existing gaps.

- [ ] **Step 4: Final report**

Summarize for your human partner or the branch reviewer: confirmation that both target tests
(`Include_reference_when_entity_in_projection`, `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join`)
pass under `NativeOnly` with correct data on all three EF versions; whether Task 1 Step 6's alias-collision
mitigation was needed in practice or not (and why); confirmation that the non-join-scope wrapped-reference-Include
data gap found during the original investigation (`Orders.Include(o => o.Owner).Select(o => new { o,
o.OwnerId })`, `Owner` materializing empty) is UNCHANGED by this plan — still present, still out of scope, still
needing its own separate ticket. No commit for this task — it's verification only.
