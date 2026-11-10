### Task 2: Attempt to un-gate the two dependent reducer/null-predicate reference-Include tests

**Files:**
- Modify (tentative): `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeReferenceIncludeTests.cs:1035-1103` (the `#if !EF8 && !EF9` block containing `Optional_reference_Include_with_a_reducer_and_a_navigation_null_predicate_falls_back_correctly` and `Native_and_DriverLinq_agree_on_reference_Include_with_a_reducer_and_a_navigation_null_predicate`)

**Interfaces:**
- Consumes: nothing new — this task only removes/keeps an `#if` guard depending on measured outcome.
- Produces: nothing new. This task's outcome is binary (un-gate and commit, or leave as-is) — it does not block Task 3 or Task 4 either way.

These two tests exercise `Include(o => o.Carrier).First(o => o.Carrier == null)` under `MongoQueryMode.Native` (not `NativeOnly`) — a shape whose *own* predicate correctly declines to native and must fall back correctly. The existing code comment (`ReattachComposedOperator`'s fix) describes a *separate*, already-fixed bug in the driver-LINQ rewrite path unrelated to the candidate-join recording this plan changes. It is not yet known whether that fix is version-agnostic or itself EF10-scoped, so this task measures it directly rather than assuming.

- [ ] **Step 1: Temporarily remove the guard and run under EF9**

Edit the file to remove the `#if !EF8 && !EF9` / `#endif` pair around both methods (same mechanical edit style as Task 1 Step 1 — delete the two directive lines, keep the method bodies unchanged).

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~Optional_reference_Include_with_a_reducer_and_a_navigation_null_predicate_falls_back_correctly|FullyQualifiedName~Native_and_DriverLinq_agree_on_reference_Include_with_a_reducer_and_a_navigation_null_predicate"
```

(Rebuild first if Task 1's build is stale: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"`.)

- [ ] **Step 2: Branch on the result**

If both tests **PASS** on EF9: rebuild and run the same filter under `-c "Debug EF8"` too. If they pass there as well, keep the guard removed (the tests now run on all three EF versions) and go to Step 3a.

If either test **FAILS** on EF9 or EF8: put the `#if !EF8 && !EF9` / `#endif` guard back exactly as it was (`git checkout -- tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeReferenceIncludeTests.cs` to discard this task's edit), and go to Step 3b instead — do not spend further time debugging this separate fallback-path issue; it is out of scope for this plan.

- [ ] **Step 3a (tests passed on all versions): Commit the un-gate**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeReferenceIncludeTests.cs
git commit -m "EF-322: run the reference-Include reducer/null-predicate fallback tests on EF8/EF9 too"
```

- [ ] **Step 3b (tests failed on EF8/EF9): Leave the guard in place, no commit needed**

Confirm `git status` shows no changes to this file (the `git checkout --` in Step 2 already reverted it). Note in your final report to the plan's reviewer that this specific fallback-path shape remains EF10-only and is a known follow-up, not blocking this plan.

---

