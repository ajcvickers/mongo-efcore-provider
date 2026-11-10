### Task 9: Cross-version verification and review

**Files:** baselines only, if EF8/EF9 differ.

- [ ] **Step 1: Full suites on all three versions.** Invoke the `/test-all` skill (builds and tests EF8, EF9 and EF10 in parallel). Expected: no new failures vs. `main`-equivalent baselines. Any EF8/EF9-only failure in the touched tests gets fixed in the owning task's code, or with an EF-version `#if` baseline in the spec file, following the existing `#if` pattern in `NorthwindAggregateOperatorsQueryMongoTest.cs`.

- [ ] **Step 2: Native-only on EF8 and EF9 for the target class.**

```bash
for c in "Debug EF8" "Debug EF9"; do
  MONGODB_EF_NATIVE_ONLY=1 dotnet test $SPEC -c "$c" --no-build --filter "FullyQualifiedName~NorthwindAggregateOperatorsQueryMongoTest" | tail -2
done
```

Expected `Failed: 0` for both, except `Return_type_of_singular_operator_is_preserved`, which doesn't exist on EF8. EF8 hands inline arrays over as `NewArrayExpression`, so if Task 2's or Task 4's shapes differ there, fix them in the relevant recognizer (e.g. `TranslateInValues` already handles `NewArrayInit`), not by skipping.

- [ ] **Step 3: Whole-suite native-only diff (EF10).** Re-run the Task 0 script against a fresh native-only Query trx and diff against `baseline-native-failures.txt`. The only differences allowed are the 48 fixed entries (plus any other classes' tests that went native as a side effect; list them in the PR description).

- [ ] **Step 4: Branch review.** Invoke `/review-ef-core-provider`. It runs `query-reviewer`, `ef-conformance-reviewer`, `api-stability-reviewer` and the rest over the branch diff. Address its findings, re-run Steps 1–3, and commit fixes as `EF-322: address review findings`.
