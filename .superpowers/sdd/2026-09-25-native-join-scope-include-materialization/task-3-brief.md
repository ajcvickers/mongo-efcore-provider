### Task 3: Prove the fix generalizes to a chain (depth > 1)

**Files:**
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`

**Interfaces:**
- Consumes: `SeedOwnersOrdersAndLines()`, `TranslateThreeSourceJoinQuery`-equivalent functional harness (this
  file already has multi-level chain tests, e.g.
  `Chain_scalar_leaf_beside_a_whole_entity_leaf_at_a_non_adjacent_chain_level_reads_correctly` — mirror its
  `CreateContext`/seed usage, not its assertions).

- [ ] **Step 1: Write the new test**

Add to `NativeJoinTests.cs`, immediately after
`Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join`:

```csharp
[Fact]
public void Whole_root_entity_leaf_that_is_also_collection_included_goes_native_in_a_two_level_chain()
{
    // Root cause A's own Task 2 re-rooted its chain-depth unit proof onto the ROOT leaf specifically
    // (owners.Include(o => o.Orders)) rather than a non-root chain level, after measuring that Including a
    // non-root level's own reference nav can coincidentally widen the recognized join-scope chain by one
    // level (see that plan's ledger). Mirror the same choice here at the FUNCTIONAL level: a genuine
    // Levels.Count == 2 chain, with the Include on the chain's ROOT (Owner.Orders) rather than either
    // joined-in level, proving Task 1/2's BindResultMember fix generalizes to scope depth > 1 without
    // reproducing that unrelated, pre-existing chain-detection hazard.
    var seed = SeedOwnersOrdersAndLines();

    static List<(string OwnerName, int OwnerOrderCount, string Sku)> Run(JoinTestDbContext db) =>
        db.Owners.Include(o => o.Orders)
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Join(db.OrderLines, e => e.r.Id, l => l.OrderId, (e, l) => new { e.o, LineSku = l.Sku })
            .AsEnumerable()
            .OrderBy(x => x.o.Name).ThenBy(x => x.LineSku)
            .Select(x => (x.o.Name, x.o.Orders.Count, x.LineSku))
            .ToList();

    var expected = seed.Owners
        .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
        .Join(seed.OrderLines, e => e.r.Id, l => l.OrderId, (e, l) => new { e.o, LineSku = l.Sku })
        .OrderBy(x => x.o.Name).ThenBy(x => x.LineSku)
        .Select(x => (x.o.Name, seed.Orders.Count(order => order.OwnerId == x.o.Id), x.LineSku))
        .ToList();

    using var nativeOnly = CreateContext(seed, MongoQueryMode.NativeOnly,
        nameof(Whole_root_entity_leaf_that_is_also_collection_included_goes_native_in_a_two_level_chain) + "_nativeOnly");
    Assert.Equal(expected, Run(nativeOnly));

    using var driverLinq = CreateContext(seed, MongoQueryMode.DriverLinq,
        nameof(Whole_root_entity_leaf_that_is_also_collection_included_goes_native_in_a_two_level_chain) + "_driverLinq");
    Assert.Equal(expected, Run(driverLinq));
}
```

- [ ] **Step 2: Build and run the test**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~Whole_root_entity_leaf_that_is_also_collection_included_goes_native_in_a_two_level_chain"
```

This test's Include target (`Owner.Orders`) is the SAME navigation the first `Join`'s key resolves to — the
identical alias-collision shape Task 1 Step 6 either did or didn't need to handle. If Task 1 Step 6 ran and
added the rename mitigation, it lives in `TranslateSelect`'s wrapped-leaf-join arm, which has no
`scope.Levels.Count`-specific branch either — expect it to apply here unchanged. If Task 1 Step 6 did NOT run
(outcome (a), the bridge alone was sufficient), expect the same here too. Either way:

Expected: PASS. `BindResultMember`'s unwrap/rewrap/bridge logic does not branch on `scope.Levels.Count`, so it
is depth-agnostic by construction, exactly like root cause A's own unwrap. If it FAILS, that is new information
(a depth-specific gap the design doc's analysis did not anticipate, OR the alias-collision mitigation turning
out to be depth-1-specific in a way Step 6 didn't foresee) — use `superpowers:systematic-debugging` rather than
patching this test to match wrong behavior.

- [ ] **Step 3: Run the whole file once more**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"
```

Expected: PASS, all tests, 0 failures.

- [ ] **Step 4: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs
git commit -m "EF-322: prove Include materialization fix generalizes to a chained join scope"
```

---

