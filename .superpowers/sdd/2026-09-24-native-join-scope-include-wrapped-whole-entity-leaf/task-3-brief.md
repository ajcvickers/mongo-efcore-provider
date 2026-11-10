### Task 3: Promote both spikes to permanent functional differential-correctness tests

**Files:**
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`

**Interfaces:**
- Consumes: the existing `Owner`/`Order`/`OrderLine` fixture, `SeedOwnersAndOrders()`, `CreateContext(Seed,
  MongoQueryMode, string)` (all already defined in this file, unchanged).
- Produces: nothing new for later tasks — Task 4's regression sweep runs this file as part of the existing
  `~Query` filter, no direct dependency.

- [ ] **Step 1: Write the two failing tests**

Add to `NativeJoinTests.cs`, right after `Computed_leaf_beside_a_whole_entity_leaf_declines_and_still_reads_correctly`:

```csharp
[Fact]
public void Whole_outer_entity_leaf_that_is_also_reference_included_goes_native()
{
    // Design doc spike 1 / Include_reference_when_entity_in_projection's shape, promoted: the Outer side of
    // an explicit Join ALSO carries `.Include(o => o.Orders)` (a collection nav on Owner, unrelated to the
    // join's own key `o.Id == r.OwnerId`), and the trailing Select captures Owner WHOLE alongside a scalar
    // Inner leaf.
    var seed = SeedOwnersAndOrders();

    static List<(string Name, decimal Total, int OwnerOrderCount)> Run(JoinTestDbContext db) =>
        db.Owners.Include(o => o.Orders)
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r.Total })
            .AsEnumerable()
            .OrderBy(x => x.o.Name).ThenBy(x => x.Total)
            .Select(x => (x.o.Name, x.Total, x.o.Orders.Count))
            .ToList();

    var expected = seed.Owners
        .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r.Total })
        .OrderBy(x => x.o.Name).ThenBy(x => x.Total)
        .Select(x => (x.o.Name, x.Total, seed.Orders.Count(order => order.OwnerId == x.o.Id)))
        .ToList();

    using var nativeOnly = CreateContext(seed, MongoQueryMode.NativeOnly,
        nameof(Whole_outer_entity_leaf_that_is_also_reference_included_goes_native) + "_nativeOnly");
    Assert.Equal(expected, Run(nativeOnly));

    using var driverLinq = CreateContext(seed, MongoQueryMode.DriverLinq,
        nameof(Whole_outer_entity_leaf_that_is_also_reference_included_goes_native) + "_driverLinq");
    Assert.Equal(expected, Run(driverLinq));
}

[Fact]
public void Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join()
{
    // Design doc spike 2 / Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join's
    // shape, promoted: the INNER side of a LeftJoin ALSO carries `.Include(r => r.OrderLines)` — a
    // navigation UNRELATED to the join's own key (`o.Id == r.OwnerId`) — and the trailing Select captures
    // both sides WHOLE. Left-outer row preservation (an Owner with no Order) is exercised via
    // SeedOwnersAndOrdersWithUnmatchedRows so the unmatched row's `r` materializes null rather than being
    // silently dropped.
    var seed = SeedOwnersAndOrdersWithUnmatchedRows();

    static List<(string OwnerName, decimal? Total, int LineCount)> Run(JoinTestDbContext db) =>
        db.Owners
            .LeftJoin(db.Orders.Include(r => r.OrderLines), o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .AsEnumerable()
            .OrderBy(x => x.o.Name)
            .Select(x => (x.o.Name, x.r?.Total, x.r?.OrderLines.Count ?? 0))
            .ToList();

    var expected = seed.Owners
        .LeftJoin(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
        .OrderBy(x => x.o.Name)
        .Select(x => (x.o.Name, x.r?.Total, x.r == null ? 0 : seed.OrderLines.Count(l => l.OrderId == x.r.Id)))
        .ToList();

    using var nativeOnly = CreateContext(seed, MongoQueryMode.NativeOnly,
        nameof(Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join) + "_nativeOnly");
    Assert.Equal(expected, Run(nativeOnly));

    using var driverLinq = CreateContext(seed, MongoQueryMode.DriverLinq,
        nameof(Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join) + "_driverLinq");
    Assert.Equal(expected, Run(driverLinq));
}
```

- [ ] **Step 2: Run to verify they fail**

Build first: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~Whole_outer_entity_leaf_that_is_also_reference_included_goes_native|FullyQualifiedName~Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join"`

Expected: Since Task 1 already fixed the production code, BOTH tests are expected to PASS already at this
point (Tasks in this plan build on each other; this is a functional-level re-proof, not a second fix cycle).
If either FAILS, read the failure carefully — a `NativeTranslationNotSupportedException` under
`MongoQueryMode.NativeOnly` here means Task 1's fix does not fully cover the real end-to-end shape (something
the unit-level harness didn't exercise, e.g. an actual round trip through the shaper/materializer), and this
becomes a systematic-debugging task, not a plan defect to route around.

- [ ] **Step 3: Run the whole file to confirm no regressions**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"`

Expected: PASS, all tests in the file (existing + 2 new), 0 failures.

- [ ] **Step 4: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs
git commit -m "EF-322: promote Include-wrapped whole-entity-leaf spikes to permanent functional tests"
```

---

