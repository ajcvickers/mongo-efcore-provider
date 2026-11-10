### Task 4: Whole-branch regression sweep and cleanup

**Files:** none new — this task runs the full suite and, if Task 2's guard or Task 3's fix touched any
version-conditional code path, repeats against EF8/EF9 too.

**Interfaces:**
- Consumes: everything committed in Tasks 1-3.
- Produces: a verified-clean branch state, ready for `superpowers:finishing-a-development-branch`.

- [ ] **Step 1: Run the full EF10 suite**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build
```

Expected: PASS (0 failures).

- [ ] **Step 2: If Task 2 or Task 3 touched any file with an `#if EF8`/`#if EF9`/`#if !EF8` guard, run all three EF versions**

Check first:

```bash
git diff main...HEAD --name-only | xargs grep -l "#if EF8\|#if EF9\|#if !EF8" 2>/dev/null
```

If any file is listed, invoke the `/test-all` skill (builds and runs against EF8, EF9, EF10 in parallel). If
nothing is listed, skip this step — no version-conditional code was touched.

- [ ] **Step 3: Confirm the two already-passing bare reference-Include tests are still passing**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Optional_reference_Include_goes_native_with_a_left_outer_unwind|FullyQualifiedName~Required_reference_Include_goes_native_with_an_inner_unwind"
```

Expected: PASS.

- [ ] **Step 4: Delete the temporary findings doc's staleness risk — leave it in place, but cross-reference it**

No code change; add one line to `docs/superpowers/specs/2026-09-25-reference-include-materialization-design.md`
noting it is now implemented, with a pointer to this plan and the findings doc, so a future reader doesn't
re-open a closed investigation.

- [ ] **Step 5: Commit**

```bash
git add docs/superpowers/specs/2026-09-25-reference-include-materialization-design.md
git commit -m "EF-322: mark reference-Include materialization design doc as implemented"
```
