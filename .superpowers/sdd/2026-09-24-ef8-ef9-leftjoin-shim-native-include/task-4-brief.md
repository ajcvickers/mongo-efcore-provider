### Task 4: Full regression sweep and sign-off

**Files:** none modified — this task only runs tests. If it surfaces a regression, stop and report it rather than attempting an ad hoc fix; the specific remediation depends on what's found.

**Interfaces:**
- Consumes: everything from Tasks 1-3.
- Produces: the evidence that Phase 1 is complete and safe to hand off.

- [ ] **Step 1: NativeOnly sweep over the full Query suite for EF9, both spec and functional tests**

This is broader than Include — the fix touches shared join-candidate-recording code used by every native `Join`/`GroupJoin`/`LeftJoin`.

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF9" --no-build --filter "FullyQualifiedName~Query" --logger "console;verbosity=normal" > /tmp/ef9_spec_query_nativeonly.txt 2>&1
MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF9" --no-build --filter "FullyQualifiedName~Query" --logger "console;verbosity=normal" > /tmp/ef9_functional_query_nativeonly.txt 2>&1
tail -8 /tmp/ef9_spec_query_nativeonly.txt
tail -8 /tmp/ef9_functional_query_nativeonly.txt
```

Expected: the spec-test failure list contains only the known Phase-2-scoped shapes (the 5 shared cross-version gaps, doubled for `async: true/false`, plus the `Include_property*`/`Include_specified_on_non_entity_not_supported` no-MQL-expected tests) — no new failures introduced by this fix, and no previously-passing test now failing. The functional-test run should show **zero** `NativeTranslationNotSupportedException` failures outside tests that explicitly `Assert.Throws` for one (i.e., any unexpected failure here is a regression — stop and report it, don't patch around it).

- [ ] **Step 2: Repeat Step 1 for EF8**

Same commands with `-c "Debug EF8"`. Compare the failure list against EF9's — they should match, since both share the identical shim and code path.

- [ ] **Step 3: Full three-version regular (non-NativeOnly) test-all run**

```bash
/test-all
```

Expected: green across EF8, EF9, and EF10 for the full solution (unit, functional, and specification test projects) — this is the final proof that nothing outside the Query/Include surface regressed (Storage, Metadata, Serializers, etc. are all exercised by the full suite even though this fix never touches their code).

- [ ] **Step 4: Final report**

Summarize, for your human partner or the branch reviewer: the EF9 and EF8 `NativeOnly` failure counts before this plan (74/235 baseline, measured pre-fix) versus after (expected: matching EF10's ~10/237, i.e. the same 5 distinct methods), which spec-test files got baseline updates, and the outcome of Task 2's stretch attempt (un-gated or left gated, and why). No commit for this task — it's verification only.
