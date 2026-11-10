### Task 8 (REVISED framing 2026-08-26): Differential-correctness oracle fixture and full regression pass

**Framing update:** per EF-439, genuine two-sided joins now permanently execute via the driver-LINQ fallback, not natively. The oracle theory below is still valuable — it proves matched/unmatched/dangling-FK correctness end-to-end — but drop `MongoQueryMode.NativeOnly` from its context (that mode would now always throw for these shapes, by design) and use the default `MongoQueryMode.Native` instead, which exercises the fallback. Rename the test to drop the word "native" so it doesn't misdescribe what it's proving.

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`

**Interfaces:** none new — test-only task.

- [ ] **Step 1: Read `NativeOwnedCollectionAllTests.cs`'s oracle-`[Theory]` pattern before writing this**

Per the spec's own reference, read `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionAllTests.cs` for the exact established shape: a `[Theory]` feeding a fixture deliberately covering matched/unmatched/edge states, asserting the native result equals an in-memory LINQ oracle evaluated over the *same* `Expression` object. Mirror that structure exactly rather than inventing a new one.

- [ ] **Step 2: Write the Join oracle theory**

```csharp
    public static IEnumerable<object[]> JoinOracleCases()
    {
        // Each case is a Func<IQueryable<Owner>, IQueryable<Order>, IQueryable<...>> so the SAME expression
        // tree runs against both the native-backed DbSet and the in-memory seed collection, following the
        // NativeOwnedCollectionAllTests.cs oracle pattern exactly.
        yield return
        [
            (Func<IQueryable<Owner>, IQueryable<Order>, IQueryable<object>>)((owners, orders) =>
                owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total }))
        ];
        yield return
        [
            (Func<IQueryable<Owner>, IQueryable<Order>, IQueryable<object>>)((owners, orders) =>
                owners.LeftJoin(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, Total = (decimal?)r.Total }))
        ];
    }

    [Theory]
    [MemberData(nameof(JoinOracleCases))]
    public void Join_result_matches_in_memory_oracle_including_unmatched_rows(
        Func<IQueryable<Owner>, IQueryable<Order>, IQueryable<object>> query)
    {
        // Seed must include: an Owner with a matching Order, an Owner with NO Order (left-join/unmatched
        // case), and an Order whose OwnerId matches nothing (dangling FK, dropped by inner Join).
        // Runs under the default MongoQueryMode.Native — genuine two-sided joins execute via the driver-LINQ
        // fallback (see EF-439), not natively; this proves that fallback path is correct, not new native
        // coverage. Do NOT use MongoQueryMode.NativeOnly here — it would always throw for this shape, by design.
        var seed = SeedOwnersAndOrdersWithUnmatchedRows();
        using var db = CreateContext(seed, MongoQueryMode.Native,
            nameof(Join_result_matches_in_memory_oracle_including_unmatched_rows));

        var actualResult = query(db.Owners, db.Orders).AsEnumerable().ToList();
        var oracleResult = query(seed.Owners.AsQueryable(), seed.Orders.AsQueryable()).ToList();

        Assert.Equal(oracleResult, actualResult);
    }
```

- [ ] **Step 3: Run the theory and fix any real mismatch it surfaces**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~Join_result_matches_in_memory_oracle"`
Expected: PASS for both cases (this exercises the pre-existing driver-LINQ fallback, which already worked before this plan — a failure here would be a genuine, unrelated regression, not something this plan's own code should be able to cause, since none of Tasks 1-6's dormant/live code paths are reachable for this shape).

- [ ] **Step 4: Run the complete regression surface — Query unit tests, Query functional tests, and the full spec suite**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query"
MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj -c "Debug EF10" --no-build
```

Expected: all PASS. Per EF-439, do NOT expect any NEW passes in the `MONGODB_EF_NATIVE_ONLY=1` run attributable to Join/LeftJoin shapes — that would indicate either a leftover expectation mismatch in this revised plan or (more concerning) that the "indistinguishable at bind time" finding was wrong; investigate rather than assume it's progress. Any genuinely NEW failure anywhere in this run is still a regression to investigate before proceeding.

- [ ] **Step 5: Invoke the `/test-all` skill (or run all three EF configs manually) for full multi-version confirmation**

Run: `.claude/skills/test-all/` (via the `/test-all` slash command) or manually:

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" --no-build
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build
```

Expected: all three EF versions green.

- [ ] **Step 6: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs
git commit -m "EF-392: add differential-correctness oracle theory for Join/LeftJoin fallback (matched/unmatched/dangling-FK rows); ref EF-439"
```

---

## Self-Review

**Spec coverage:**
- Two-scope resolver extraction as an early, isolated commit → Task 1. ✓
- `MongoJoinBinder` invoked from `TranslateJoinCore` only for a genuine two-sided join (Include sugar handled separately by the unchanged `TryConfirmReferenceInclude`) → Tasks 3-4. ✓
- Join-scope marker on `MongoSelectDefinition` extending `HasTerminalOperator` → Task 2. ✓
- New `AppendLookupStages` branch reusing `MongoLookupStage`/`MongoUnwindStage` keyed on `isLeftOuter` → Task 5. ✓
- Explicit non-goals get their own declining regression tests → Task 7 (GroupJoin array shape, chained joins, query-filtered target, navigation-less join — the fourth was surfaced during grounding as a real additional boundary the spec's "widened `$lookup` eligibility" language implied but didn't enumerate by name; included for completeness). ✓
- Testing per the spec (`NativeJoinTests`, differential-correctness oracle, unit tests, SelectMany/reference-Include regression) → Tasks 1 (unit), 3 (unit), 4/6/7 (functional), 8 (oracle + full regression). ✓
- EF-411 coordination (small early commit, check before merging `MongoSelectDefinition.cs`) → Task 2's coordination note. ✓

**Placeholder scan:** No "TBD"/"TODO"/"implement later" found. Two spots are flagged as genuine open design decisions rather than placeholders — Task 6 Step 1's revision of `MongoJoinBinder` (the flattened-leaf translation call needs a implementer to re-verify against the real `MongoExpressionTranslator` surface beyond what was read while writing this plan) and Task 6 Step 4 (explicitly rejects force-fitting `MongoTransparentScopeResolver` and specifies the concrete alternative to write instead). These are marked as the plan's own highest-uncertainty points with a stated fallback approach, not left blank.

**Type consistency:** `MongoJoinScope`'s constructor/property names (`OuterMemberName`, `InnerMemberName`, `OuterEntityType`, `InnerEntityType`, `InnerPrefix`, `IsLeftOuter`) are used identically across Tasks 2, 3, 5, and 6. `MongoJoinBinder.TryBindJoin`'s signature is introduced in Task 3 and its call site in Task 4 matches exactly (`outerQueryExpression, innerQueryExpression, resultSelector, joinInfo`); Task 6 revises the method's *body* but not its signature. `MongoTransparentScopeResolver.TryResolveScopeDepth`/`ScopeRerootingVisitor` from Task 1 are referenced by `NativeSelectManyBinder`'s updated call sites in Task 1 Step 5, and explicitly declined for reuse in Task 6 Step 4 (with the reasoning recorded) rather than silently diverging.

**Deviation from the spec worth flagging to the reviewer:** the spec's Task 1 framing ("extract a shared two-scope parameter-identity resolver out of NativeSelectManyBinder") turned out, on reading the real code, to already partly exist as `MongoExpressionTranslator`'s two-scope constructor (reused as-is, no extraction needed) — the genuine extraction target is the narrower *scope-depth/re-rooting walker* (`TryResolveScopeDepth`/`ScopeRerootingVisitor`), which Task 1 does extract and generalize. That extraction remains live and valuable regardless of everything below.

**MAJOR REVISION (2026-08-26, post-Task-5):** Task 6's original plan (a bespoke direct-check resolver for Join's flat two-sibling shape, explicitly avoiding `MongoTransparentScopeResolver`) was never executed. Its implementer instead discovered that the premise underlying Tasks 2/3/5/6 — that a genuine two-sided join is distinguishable from an Include-generated join at `TranslateJoinCore` — is false: EF Core's `NavigationExpandingExpressionVisitor` normalizes both into the identical `TransparentIdentifier(Outer, Inner)` shape before either binder runs. (Ironically, had this premise held, `MongoTransparentScopeResolver` at `sourceCount: 1` *would* have been the right tool after all — the "don't force-fit it" instruction was itself downstream of the same false premise.) The human maintainer ruled: do not build a disambiguation workaround; genuine two-sided joins stay on the pre-existing, correct driver-LINQ fallback indefinitely. Filed as [EF-439](https://jira.mongodb.org/browse/EF-439). Task 6 was revised in place to document Tasks 2/3/5 as intentionally-retained dormant scaffolding and to correct the smoke tests' expectations; Tasks 7-8's tests remain valid but their framing was corrected to stop implying native execution. See the design spec's "Blocker found during implementation" section for the full account. This is exactly the kind of finding `writing-plans`/`subagent-driven-development`'s process is built to surface — a plan built on a wrong assumption about the platform it targets — not a failure of execution at any individual task.
