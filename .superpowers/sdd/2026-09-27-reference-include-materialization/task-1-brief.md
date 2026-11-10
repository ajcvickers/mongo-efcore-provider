### Task 1: Investigate Shape 3's actual code path (pure spike, no production code)

**Files:**
- Create: `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md` (the task's only committed
  artifact)
- Temporarily edit, then revert before commit (never commit these edits):
  - `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
    (`TranslateSelect` ~line 417-433, `TryGetReferenceIncludeChain` ~line 1522-1548,
    `TryConfirmReferenceIncludeChain` ~line 1771, `IsSingleEligibleNativeJoinScope` ~line 1017,
    `BindResultMember` ~line 3390-3430)
  - `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs`
    (`TryBindProjection` ~line 630 onward)

**Interfaces:**
- Consumes: nothing from a prior task (this is the first task).
- Produces: a written, dated, on-disk determination of "Hypothesis A" vs. "Hypothesis B" vs. "neither" for
  Shape 3, which Task 3 reads and branches on. Also produces (as a side observation, written into the same
  doc) whatever it learns about Shape 4 and the two already-passing bare reference-Include tests' code path,
  since Task 3's Review Focus depends on knowing whether they share code with Shape 3.

- [ ] **Step 1: Confirm the environment is ready**

```bash
docker info >/dev/null 2>&1 || open -a Docker
```

If Docker was just started, wait for `docker info` to succeed before continuing (it took about 20s the last
time this repo's tests needed it).

- [ ] **Step 2: Build EF10 debug**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```

Expected: build succeeds (0 errors).

- [ ] **Step 3: Reproduce the CURRENT failing behavior of both target spec tests, unmodified**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest.Include_reference_when_entity_in_projection|FullyQualifiedName~NorthwindIncludeQueryMongoTest.Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join"
```

Record the exact failure mode (assertion diff showing `null` vs. `"FOLKO"`, or an empty vs. populated
collection, or an exception — write down which). This is the baseline the rest of this task's logging must
explain.

- [ ] **Step 4: Add temporary `Console.WriteLine` probes**

At the top of each of these five methods/arms, add one line identifying which arm fired and with what
resolved outcome (chain recognized? confirmed? declined? — read the existing local variable right after the
call and print it):

```csharp
Console.WriteLine($"[SHAPE3-PROBE] TryGetReferenceIncludeChain: recognized={referenceIncludeChain != null}");
```

(one line per site, adapted to that site's own locals — e.g. at `TryConfirmReferenceIncludeChain`'s entry
print the incoming `chain.Count`/`transitiveLevels.Count`, at `IsSingleEligibleNativeJoinScope`'s entry print
`mongoQueryExpression.Select.JoinScope?.Levels.Count`, at `BindResultMember`'s entry print
`allCollectionIncludes` right after the unwrap loop completes, at `NativeJoinScopeProjectionBinder.TryBindProjection`'s
entry print whether it was even called for this query).

- [ ] **Step 5: Rebuild and re-run the two target spec tests with probes active**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest.Include_reference_when_entity_in_projection|FullyQualifiedName~NorthwindIncludeQueryMongoTest.Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join" \
  -- xunit.parallelizeAssembly=false
```

Capture stdout (the `[SHAPE3-PROBE]` lines) alongside the assertion failure. This tells you, per test: which
arm(s) fired, in what order, and what each returned.

- [ ] **Step 6: Run the two already-passing bare reference-Include tests with the SAME probes active**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Optional_reference_Include_goes_native_with_a_left_outer_unwind|FullyQualifiedName~Required_reference_Include_goes_native_with_an_inner_unwind"
```

Capture the same probe output. Compare against Step 5's output — do these working tests hit the SAME arms,
or different ones? This directly answers the design doc's Review Focus question about whether a Task 3 fix
risks the working path.

- [ ] **Step 7: Run the Shape 4 control repro with the same probes active**

Add a throwaway `[Fact]` (not committed) to a scratch test file, or run interactively, exercising:

```csharp
db.Orders.Include(o => o.Owner).Select(o => new { o, o.OwnerId })
```

against the `JoinTestDbContext`/`SeedOwnersAndOrders()` fixture already used by
`NativeJoinTests.cs` (same context helpers this file's other tests use), under
`MongoQueryMode.NativeOnly`. Capture probe output.

- [ ] **Step 8: Remove every probe added in Step 4 and Step 7's throwaway fact**

```bash
git diff --stat  # confirm only the two visitor files show diffs, and the throwaway fact file if any was added as a new untracked file
git checkout -- src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
                 src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs
```

Delete the throwaway scratch fact file if one was created. Rebuild once more to confirm the tree is back to
its pre-Task-1 state:

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git status  # must show no diff in the two visitor files
```

- [ ] **Step 9: Write the findings doc**

Create `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md` containing:
- Which hypothesis (A, B, or neither) the probe output supports for
  `Include_reference_when_entity_in_projection`, with the literal probe output quoted.
- The same for `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join` — note
  explicitly if it takes a DIFFERENT path than the first test (it is the MIXED reference+collection shape,
  so it may hit `TryGetMixedReferenceAndCollectionIncludeChain` instead of `TryGetReferenceIncludeChain`, and
  that difference must be recorded, not assumed away).
- Whether the two already-passing bare reference-Include tests share any of the same arms — if the answer is
  yes, name exactly which ones, since Task 3 must not touch those without re-verifying them.
- Whether Shape 4's control repro hits `TryGetReferenceIncludeChain`/`TryConfirmReferenceIncludeChain` (a pure
  reference chain, no join) or the join-scope arms (unexpected, would mean EF's nav-expansion inserts an
  internal join even for this simpler shape too).
- A one-paragraph recommendation for which of Task 3's two branches (3A/3B) to execute, or a third option if
  the evidence supports neither hypothesis as written.

- [ ] **Step 10: Commit the findings doc only**

```bash
git add docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md
git commit -m "$(cat <<'EOF'
EF-322: record Shape 3 code-path investigation findings

Pure investigation spike (design doc's mandated Task 1) — determines whether
Include_reference_when_entity_in_projection and
Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join
reach the join-scope BindResultMember/TryConfirmReferenceIncludeChain machinery
(Hypothesis A) or the separate TryGetReferenceIncludeChain/TryConfirmReferenceIncludeChain
arm (Hypothesis B), via temporary diagnostic logging (removed before this commit —
diff is the findings doc only). No production code changes.
EOF
)"
git status  # confirm only the findings doc is staged/committed
```

---

