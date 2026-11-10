# Task 4 report: bring in Negate, probe, regenerate, verify

Status: DONE_WITH_CONCERNS. Branch `EF-322c-setops` in ~/code/provider2. Nothing was pushed, and `EF-322c` in the main clone was not touched.

## Rebase

- I fetched with `git fetch /Users/arthur.vickers/code/mongo-efcore-provider EF-322c:refs/remotes/mainclone/EF-322c`, which brought in tip 1f915fb8. Local `EF-322c` was not touched.
- Then I ran `git rebase mainclone/EF-322c`. **It applied with no conflicts**, including NorthwindSetOperationsQueryMongoTest.cs, where Negate changed only function_unary and unary_function and Tasks 1-3 changed other overrides.
- The base carries five commits beyond 8c5f00e1, not just Negate:
  - aa5d76df: plans
  - 5498305b: Negate
  - 013e8f1b and eb79d5a2: PK never-null
  - 1f915fb8: null-propagated scalars
- Tasks 1-3 moved as follows: 4ce4a0c4 → 27d7c861, 0280a5b2 → 1b67cf82, 16c9c9fe → 37a49bbc.
- The backup branch `backup/EF-322c-setops-pre-rebase` (16c9c9fe) is untouched.

## Probe: Distinct-wrapped constant on source1

**Result: wrong data, as the reviewer predicted.** It is now fixed by a decline in 43837300.

Under NativeOnly, native returned the constant for every combined row. Seed (1,8),(2,20):

| Query | Native returned | Correct |
|---|---|---|
| `Select(x=>8).Distinct().Union(Select(x=>x.N))` | [8,8] | [8,20] |
| `...Distinct().Concat(...)` | [8,8,8] | [8,8,20] |
| `...Distinct().Union(Select(x=>9))` | [8,8] | [8,9] |
| parameter `p` | [8,8] | [8,20] |
| `new { A = 8, x.N }.Distinct().Union(new { A = x.Id, N = 7 })` | ["8:20","8:7","8:7","8:8"] | ["1:7","2:7","8:20","8:8"] |

The cause is what the reviewer described:
- TryBindDistinctFromProjection moves the projected values into its grouping key parts and projects `MongoElementRefExpression`s to them. HasShaperUnsafeConstantLeaf therefore sees no constant, but the shaper still has the constant baked in.
- This bug predates this branch's work.

**Second hole, found while probing neighbouring grouped shapes:**
- `GroupBy(x => x.N).Select(g => 8).Union(Select(x => x.N))` threw `InvalidOperationException: Document element '_v0' is missing but required` in Native and NativeOnly. Driver-LINQ returns the correct [1,8,10,20].
- The cause is that CanRebindConstantLeafToDocument (Task 2) admitted a grouped select, whose shaper doesn't read the member that BindSelectManyMember binds.
- **Task 2 introduced this.** Before it, every source1 constant declined and fell back.
- The server pipeline was correct; only the read failed.
- The same shape as source2, and standalone, were already fine.

**Fix (43837300), a decline in both cases:**
- `HasShaperUnsafeConstantLeaf` now also returns true for `Select is { IsDistinct: true, Grouping: {} }` with a constant or parameter key part.
  - This is scoped to IsDistinct because a real GroupBy's constant key (`GroupBy(x => 8).Select(g => new { A = g.Key, C = g.Count() })`) is read back from `_id` and is correct natively. The test for that shape shows the scoping is needed.
  - The comment also carries the deferred cross-reference to the CanRebind exception.
- `CanRebindConstantLeafToDocument` gains `mongo1.Select.Grouping == null`. Gate and rewrite still share this one predicate.

**Tests added:**
- NativeSetOperationProjectionTests, `DeclinesCleanly` (NativeOnly throws, and Native equals DriverLinq):
  - the five Distinct shapes above;
  - `Union_of_grouped_constant_result_and_column_declines`;
  - `Union_of_grouped_anonymous_operand_with_a_constant_member_declines`, which already declined before the fix.
- NativeSetOperationProjectionTests, `NativeAndParity`:
  - `Union_of_column_and_grouped_constant_result_goes_native`;
  - `Union_of_grouped_constant_key_and_anonymous_column_operand_goes_native`.
- Unit test: `Grouped_constant_result_does_not_rebind`, which also asserts the leaf is a MongoConstantExpression.

**Mutations** (EF10; NativeSetOperationProjectionTests + NativeSetOpsTests, 182 tests; the file was restored and `cmp`-verified after each):

| Mutation | Result |
|---|---|
| Drop the Distinct key-part disjunct | The 5 Distinct tests fail with "No exception was thrown" under NativeOnly, meaning the query went native with the wrong data above |
| Drop `Grouping == null` | `Union_of_grouped_constant_result_and_column_declines` fails (InvalidOperationException '_v0'), and so does the unit test |
| Check key parts of any grouping, not just IsDistinct | `Union_of_grouped_constant_key_and_anonymous_column_operand_goes_native` fails because the query declines |

## NativeOnly SetOps spec (Step 2)

After the rebase, EF10 under NativeOnly had 2 failures, both `Union_over_column_unary` (sync and async). Both were **MQL baseline only**; the data passed. The Negate commit had written its baseline in a `_v`/`_v` shape, but the query now runs through the Task 1 alias alignment, which keeps `_id`. I regenerated it (`EF_TEST_REWRITE_BASELINES=1`, tight filter), re-indented it, rebuilt and reran. It is now **0 failures on EF8, EF9 and EF10, in NativeOnly and in default mode**. I added no `// Fails:`.

The 9 predicted churners:

| Test | Where it changed |
|---|---|
| column_binary, column_constant, column_function, function_column | Task 1 |
| constant_function, columns_with_different_nullability | Task 2 |
| function_unary, unary_function | Upstream in the Negate commit, unchanged here |
| column_unary | Regenerated in this task (38770d19) |

So all 9 did change, but only column_unary needed regenerating in Task 4. Concat_with_one_side_being_GroupBy_aggregate (Task 3) also changed. The Negate commit left function_unary and unary_function unindented; I left those lines as they are.

## Full-suite verification (Step 3)

**Before** = the rebase base 1f915fb8 (main-clone EF-322c tip, which includes Negate). I measured it here with a detached checkout, then switched back. **After** = the branch tip. The after-runs used binaries built at 38770d19; 6038952f changes only comments and AGENTS.md. All three configurations were rebuilt at 6038952f with 0 warnings and 0 errors, and NativeSetOperationProjectionTests was rerun on that build: 44/44.

Each run was a single `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EFx" --no-build` in default mode, plus the SetOps class with `MONGODB_EF_NATIVE_ONLY=1`. MONGODB_URI and ATLAS_URI were unset, and each process ran in its own container. Cells are passed/skipped/failed (total).

| EF10 | Before | After | Δ |
|---|---|---|---|
| SetOps NativeOnly | 153/0/30 (183) | 183/0/0 (183) | −30 failed |
| Spec, full, default | 4768/14/0 (4782) | 4768/14/0 (4782) | 0 |
| Functional, full, default | 4174/52/0 (4226) | 4231/52/0 (4283) | +57 tests |
| Unit | 1554/0/0 | 1566/0/0 | +12 tests |

| EF9 | Before | After | Δ |
|---|---|---|---|
| SetOps NativeOnly | 153/0/30 (183) | 183/0/0 (183) | −30 failed |
| Spec, full, default | 5027/11/0 (5038) | 5027/11/0 (5038) | 0 |
| Functional, full, default | 4099/52/0 (4151) | 4156/52/0 (4208) | +57 tests |
| Unit | 1561/0/0 | 1573/0/0 | +12 tests |

| EF8 | Before | After | Δ |
|---|---|---|---|
| SetOps NativeOnly | 147/0/30 (177) | 177/0/0 (177) | −30 failed |
| Spec, full, default | 4714/11/0 (4725) | 4714/11/0 (4725) | 0 |
| Functional, full, default | 4051/51/0 (4102) | 4108/51/0 (4159) | +57 tests |
| Unit | 1561/0/0 | 1573/0/0 | +12 tests |

**The 30 "before" NativeOnly failures** are the same 15 tests × sync/async on every version (the name lists were diffed and are identical):
- Concat_with_one_side_being_GroupBy_aggregate
- Union_over_binary_column, column_binary, column_constant, column_function, column_unary
- Union_over_columns_with_different_nullability
- Union_over_constant_binary, constant_column, constant_constant, constant_function, constant_unary
- Union_over_function_column, scalarsubquery_column, unary_column

Against the plan's 22: 22 − 7 pure-N tests (flipped by Negate alone) = 15. That matches 6 A + 4 C + 1 C+A + 2 A+N + 1 C+N + 1 J = 15.

Logs: `/private/tmp/claude-502/-Users-arthur-vickers-code-provider2/1e74fee7-dd23-4ba4-ae57-0d16411d74fb/scratchpad/task4/{before,after}/`

## Docs (Step 4, 6038952f)

- **`docs/native-query-status-EF-322.md` does not exist on this branch.** It is not in the history of HEAD, of the backup branch, or of the main clone's EF-322c. It was removed by "Remove AI docs" (637b9b1b) on origin/EF-322-Native-LINQ. I did not recreate a status doc without being asked. The supported-shape list is below so it can go wherever the owner wants it.
- `src/MongoDB.EntityFrameworkCore/Query/AGENTS.md` has a new invariant bullet after "Set ops form a tree": projected set-op operands are read through source1's shaper, so bare scalars re-alias, a source1 constant or parameter (including inside a projected Distinct's key parts) declines unless the rebind applies (bare, ungrouped, int/long/double/bool/string), source1 may be a single-level confirmed join scope, and source2 has no lookups.
- Deferred minors that were folded in:
  - a comment on `Union_of_Guid_constants_declines`: the Guid decline happens before the rebind predicate;
  - a comment on the grouped-accumulator decline test: the decline is deliberately conservative;
  - the HasShaperUnsafeConstantLeaf cross-reference, which is in 43837300.

**Supported operand shapes** (projected Union/Concat, and Intersect/Except where noted):
- Bare scalars with different aliases, e.g. column `_id`/`N` against computed, constant or grouped `_v`. Source2 is re-aliased to source1's alias. This requires default serialization on both sides and no post-Distinct ops on source2.
- A bare constant or parameter leaf on source1 of type int, long, double, bool or string (including a null string), on an ungrouped select. It is rebound to read `_v` per document.
- A join-scope source1 with a single-level confirmed join, for every set-op kind including Intersect/Except.

**Shapes that still decline:**
- source2 with lookups or a join (`*_scalarsubquery` on the right; driver-LINQ fails on these too);
- constants inside anonymous or DTO projections, plain or grouped;
- DateTime, Guid, enum, decimal, char and nullable constants;
- a projected-Distinct or grouped constant on source1;
- multi-level join chains on source1;
- a value-converted column in a re-aliased pair.

## Commits (on top of mainclone/EF-322c 1f915fb8)

| SHA | Subject |
|---|---|
| 27d7c861 | EF-322: native Union/Concat of bare scalar operands with different aliases (Task 1, rebased) |
| 1b67cf82 | EF-322: native set operations with a constant/parameter bare operand on source1 (Task 2, rebased) |
| 37a49bbc | EF-322: native projected set operation over a join-scope source1 (Task 3, rebased) |
| 43837300 | EF-322: decline set operations whose first operand is a Distinct or grouped constant |
| 38770d19 | EF-322: regenerate the Union_over_column_unary baseline after native Negate |
| 6038952f | EF-322: document projected set-op operand shapes in the Query invariants |

## Concerns

1. **Task 2 had a regression, now fixed.** A grouped constant result on source1 crashed in default mode where it used to fall back. It was not caught earlier because no test covered a grouped source1 with a constant. The fix is in 43837300; the reviewer should look at it as a correction to Task 2.
2. **The status doc is missing** (see Docs). Step 4's status-doc part was not done. The supported-shape list above is ready to paste.
3. **The Distinct and grouped constant shapes decline rather than go native.** Rebinding them (reading the flattened alias) is possible but was not attempted.
4. **The base is five commits beyond 8c5f00e1, not just Negate.** The "before" numbers are therefore at 1f915fb8, not 8c5f00e1.
5. The Negate commit left two baselines (function_unary, unary_function) without indentation. They are cosmetic and I left them alone.
6. I did not run /review-ef-core-provider, as instructed.

---

## Fix wave 1: whole-branch review minors (commit b019be51, on top of 6038952f)

### Changes (MQTEV)

1. **`CanRebindConstantLeafToDocument`** gets a new conjunct:
   `!mongo1.Projection.Any(pe => string.Equals(pe.Alias, mongo1.Select.Projection[0].Alias, StringComparison.OrdinalIgnoreCase))`.
   - `mongo1.Projection` is the read-side `ProjectionExpression` list, and `Alias` is `string?`.
   - It blocks a double-bind. AddToProjection renames a colliding alias from `_v` to `_v0`, and the pipeline never emits `_v0`.
   - The comment explains the overlap with `Grouping == null` and that the conjunct can't be killed on its own.
2. **Comment on `CanAlignBareScalarAliases`**: widened to cover post-Distinct and post-GroupBy ops. The latter includes a Where recorded through MongoProjectedAliasScope; if the re-alias orphaned it, every source2 row would be dropped.

### Tests

- **`NativeGroupByOverJoinTests.Union_of_a_join_scope_constant_operand_and_a_grouped_count_declines`**
  - Query: `Orders.Where(o => o.Owner!.Name == "Alice").Select(o => 7)` combined with `GroupBy(Region).Select(g => g.Count())`.
  - It uses **DeclinesCleanly, not NativeAndParity**. A probe showed that the standalone `Orders.Where(o => o.Owner!.Name == "Alice").Select(o => 7)` already throws under NativeOnly ("projects a non-entity result"), so the operand's Route is Fallback. It never reaches the gate, the rebind or the new conjunct.
  - Oracles, checked against CreateSeed:
    - Alice owns o1 and o2, so source1 = {7, 7}.
    - Counts by Region: North 3 (o1, o2, o4), South 1, East 1.
    - Union gives [1,3,7]; Concat gives [1,1,3,7,7].
- **NativeSetOperationProjectionTests, NativeOnly hand oracles.** Intersect/Except have no driver-LINQ oracle.
  - `Intersect_and_Except_of_key_column_and_computed_scalar_re_align_the_second_operand`: over Ids {1,2,3,4} and N+1 = {11,21,2,11}, Intersect is [2] and Except is [1,3,4].
  - `Intersect_and_Except_of_constant_and_column_compare_the_constant_with_each_row`, over seed (1,8),(2,20):
    - 8 against N: Intersect [8], Except [].
    - 8 against Id: Intersect [], Except [8].
- **Optional test, added:** `Union_of_key_column_and_distinct_column_goes_native`. `Id ∪ N.Distinct()` gives [1,2,3,4,10,20], checked by NativeAndParity.

### Mutations (EF10)

Suite: NativeSetOperationProjectionTests + NativeSetOpsTests + NativeGroupByOverJoinTests, 276 tests, plus NativeSetOperationConstantLeafRebindTests. The file was restored after each run and `cmp` matched.

| Mutation | Result |
|---|---|
| Drop the new alias conjunct | 0 fail. **Not killable** on its own; the code comment says so |
| Drop `Grouping == null`, keep the alias conjunct | 0 fail, including the unit tests. The grouped constant result already has a `_v` entry, so the alias conjunct rejects it too |
| Drop both | `Union_of_grouped_constant_result_and_column_declines` and unit `Grouped_constant_result_does_not_rebind` fail |
| Skip the re-alias while still admitting (`if (alignBareScalarAliases)` → `if (false)`) | 20 fail, **including both new Intersect/Except tests** |
| Skip the rebind while still admitting (`if (rebindConstantLeaf)` → `if (false)`) | 14 Union/Concat tests fail. **The Intersect/Except constant test passes** |

Why the Intersect/Except constant test survives the rebind mutation: a set difference outputs only source1's rows, and for those rows the baked-in constant is correct. So the rebind can't be discriminated for Intersect/Except; only the re-alias can.

**Consequence for the fix in 43837300:** since this wave, the `Grouping == null` conjunct can only be proven killable together with the alias conjunct. Its earlier standalone proof held before the alias conjunct existed.

### Verification

Command, per version:
- `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EFx" --no-build --filter "FullyQualifiedName~NativeSetOperation|FullyQualifiedName~NativeSetOpsTests|FullyQualifiedName~NativeGroupByOverJoinTests"`
- the SetOps spec with `MONGODB_EF_NATIVE_ONLY=1`, and again in default mode.

MONGODB_URI and ATLAS_URI were unset. All three builds had 0 warnings and 0 errors.

| Version | Unit | Functional | SetOps NativeOnly | SetOps default |
|---|---|---|---|---|
| EF10 | 12/12 | 276/276 | 183/183 | 183/183 |
| EF9 | 12/12 | 276/276 | 183/183 | 183/183 |
| EF8 | 12/12 | 276/276 | 177/177 | 177/177 |

I did not rerun the full suites for this wave. It adds tests and one conjunct that the mutation run shows kills nothing, so behaviour is unchanged.

Logs: `/private/tmp/claude-502/-Users-arthur-vickers-code-provider2/1e74fee7-dd23-4ba4-ae57-0d16411d74fb/scratchpad/task4/fix1/`

Commit list: the six commits above, plus b019be51 "EF-322: guard the set-op constant rebind against a double-bind; cover Intersect/Except operands".
