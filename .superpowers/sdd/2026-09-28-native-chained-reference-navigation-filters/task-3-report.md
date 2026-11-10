# Task 3: Real-database tests for multi-hop filters — report

## Status: BLOCKED

## What was created

`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeChainedReferenceNavigationFilterTests.cs`,
created verbatim from the brief's Step 1 content. UTF-8, no BOM (per the binding constraint in the task
prompt — note this differs from the neighbouring test files, which do have a BOM; I followed the explicit
instruction in the task prompt over the repo-wide "preserve BOMs" default since this is a new file, not an
edit to an existing one).

No compile adjustments were needed — the file compiled as written against the actual utilities
(`NativeModeAssert.NativeAndParity<T>`, `NativeModeAssert.DeclinesCleanly<T>`, `TemporaryDatabaseFixture`,
`TemporaryDatabaseFixtureBase.CreateCollectionName(string)`, the `[XUnitCollection("...")]` global-usage
attribute) with no `using` fixes required.

## Commands run

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
# Build succeeded, 0 errors (12 pre-existing SSH.NET NU1903 advisory warnings, unrelated).

unset MONGODB_URI ATLAS_URI
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeChainedReferenceNavigationFilterTests"
```

Result: **10 passed, 1 failed**, out of 11 total. TestContainers spun up a `mongodb/mongodb-atlas-local`
container as expected (no `MONGODB_URI`/`ATLAS_URI` set).

## Per-test results

| Test | Result |
|---|---|
| `Equality_on_second_hop_matches_only_fully_joined_rows` | PASS |
| `Inequality_on_second_hop_includes_rows_with_a_missing_hop` | PASS |
| `Null_check_on_second_hop_treats_null_and_dangling_alike` | PASS |
| `Not_null_check_on_second_hop_matches_only_resolved_rows` | PASS |
| `Conjunction_of_root_and_second_hop_filters` | PASS |
| `Local_collection_contains_over_second_hop` | PASS |
| `Ordering_and_paging_after_a_second_hop_filter` | PASS |
| `Comparison_spanning_two_hops_declines_cleanly` | **FAIL** — see below |
| `Paging_before_a_second_hop_filter_pages_first` | PASS |
| `Self_referencing_second_hop_null_check` | PASS |
| `Self_referencing_second_hop_comparison` | PASS |

## The failure

`Comparison_spanning_two_hops_declines_cleanly`:

```
Assert.Equal() Failure: Collections differ
Expected: [1]
Actual:   [1, 4, 5]
```

This is the final `Assert.Equal([1], result)` in the test body — the check against the brief's
hand-computed expectation. It is **not** the `NativeOnly` throw check and **not** the `Native == DriverLinq`
parity check inside `NativeModeAssert.DeclinesCleanly`; both of those passed silently (no separate failure
was reported for them), which is only possible if:

1. `MongoQueryMode.NativeOnly` did throw `NativeTranslationNotSupportedException` for
   `o.Customer!.Name == o.Customer!.Region!.Name` (the arm declines cleanly, as intended — no misrouting), and
2. `MongoQueryMode.Native` and `MongoQueryMode.DriverLinq` **agree** with each other, both producing
   `[1, 4, 5]`.

So this lands squarely in the third failure-interpretation bucket from the brief: **"Both modes agree but
differ from the hand-computed rows… Stop and report with both result lists. Don't edit the expectation to
match."**

### Diagnosis

I believe the hand-computed expected list `[1]` in the brief is wrong, not the arms or the decline path.
Walking the seed data under EF Core's default (non-relational) null semantics, where `==` between two
possibly-null operands follows C# semantics (`null == null` is `true`):

- Order 1: `Customer.Name` = "North", `Customer.Region.Name` = "North" → equal → **included**. (Matches brief.)
- Order 2: `Customer.Name` = "Bravo", `Region` doesn't resolve (`RegionId` is null) so `Region.Name` = null →
  "Bravo" != null → excluded. (Matches brief.)
- Order 3: `Customer.Name` = "Charlie", `Region` doesn't resolve (dangling `RegionId`) so `Region.Name` = null
  → "Charlie" != null → excluded. (Matches brief.)
- Order 4: `CustomerId` is dangling, so `Customer` itself doesn't resolve → both `Customer.Name` and
  `Customer.Region.Name` read as null under nav-expansion's null-propagating semantics → `null == null` →
  **true** → included.
- Order 5: `CustomerId` is null, same reasoning as order 4 → `null == null` → **true** → included.

That produces `[1, 4, 5]`, exactly what both Native and DriverLinq computed. The brief's expectation of `[1]`
appears to have implicitly assumed `null == null` is false (i.e., relational/SQL three-valued-logic
semantics), but EF Core's *default* comparison translation is C#-null-semantics, not relational-null-semantics
— `UseRelationalNulls` is a relational-provider-only opt-in and isn't in play here, so `null == null` being
`true` is the expected default EF behavior for this provider too.

I did not find any evidence of a production bug: the arm being tested here (a comparison spanning two hops)
correctly declines under `NativeOnly` (throws) and correctly falls back to matching driver-LINQ results under
`Native`. The disagreement is entirely between "both real execution paths" and the brief's expected-rows table,
which is the case the brief explicitly tells me not to paper over by editing the expectation.

I did **not** temporarically instrument any production code (e.g. `MarkNotNativelyRepresentable`) — the
build-in xUnit failure output already gave me both the expected and observed lists, and `Assert.Throws` /
the internal parity assertion inside `DeclinesCleanly` already told me both modes agree, so no extra
diagnostics were needed. `git status`/`git diff` for `src/` confirm no production files were touched.

## Self-review of the diff

- Only one new file added: the test class, byte-for-byte as specified in the brief (verified by diff against
  the brief's fenced code block).
- No production code changed (`git diff --stat -- src/` is empty).
- File is UTF-8, no BOM (confirmed with `xxd`), matches the "without BOM" instruction in the task prompt.
- No stray `Console.WriteLine`/diagnostic instrumentation left in the file or anywhere else.
- Did not commit, per "If not [all pass], follow the failure-interpretation rules" — those rules direct
  stopping and reporting, not committing a file with a known-failing test whose fix is out of scope for this
  task (test expectations aren't mine to edit; production code isn't mine to touch).

## Concerns / what the controller needs to decide

- `Comparison_spanning_two_hops_declines_cleanly`'s final assertion (`Assert.Equal([1], result)`) has an
  expected-rows list that looks incorrect under EF Core's actual null-comparison semantics. Both Native and
  DriverLinq agree on `[1, 4, 5]`. Recommend correcting the brief/test's expected list to `[1, 4, 5]` (which
  would make this test pass while still exercising the "declines cleanly, falls back correctly" contract) —
  but that's a call for the controller/task author, not something I changed unilaterally.
- All other 10 tests pass outright, including the ones exercising Review Focus 1, 2, 4, and 5 (multi-hop
  null/dangling-reference handling, paging-before-filter ordering, and self-referencing second-hop joins).
  These give solid real-DB evidence that Tasks 1 and 2's production arms are correct for the shapes they claim
  to support.
- The file is left uncommitted in the working tree pending the controller's decision on the one failing
  assertion.

## Fix report (post-ruling)

The coordinator confirmed the diagnosis: the brief's expected `[1]` assumed SQL null semantics; EF Core's
default C# null semantics make orders 4 and 5 match (`null == null`). Applied exactly the coordinator's
directed change, nothing else.

### Change

In `Comparison_spanning_two_hops_declines_cleanly`:

```diff
-        Assert.Equal([1], result);
+        // EF C# null semantics: orders 4 and 5 have no Customer, so both sides are null and null == null holds.
+        Assert.Equal([1, 4, 5], result);
```

### Commands and output

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
# Build succeeded. 0 Errors, 12 pre-existing SSH.NET NU1903 advisory warnings (unrelated).

unset MONGODB_URI ATLAS_URI
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeChainedReferenceNavigationFilterTests"
```

```
Passed!  - Failed: 0, Passed: 11, Skipped: 0, Total: 11, Duration: 928 ms
```

All 11 tests pass, including the fixed `Comparison_spanning_two_hops_declines_cleanly`.

### Commit

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeChainedReferenceNavigationFilterTests.cs
git commit -m "EF-322: real-DB tests for native multi-hop reference-navigation filters"
```

Commit `cf1a8f4a` on branch `EF-322-Native-LINQ-rebased`. 1 file changed, 267 insertions (new file). No
production code touched; no other files modified.

## Final status: DONE
