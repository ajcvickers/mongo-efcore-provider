### Task 7 (REVISED framing, same content): Declining regression tests for explicit non-goals

**Framing update:** these shapes now decline for the SAME underlying reason as every genuine two-sided join (the Task 4 `TransparentIdentifier` guard, now understood to be permanent per EF-439), not for their originally-assumed distinct reasons (GroupJoin's own array shape, chained-join detection, etc. — those guards may still exist and may also independently decline, but the guard that fires first today is the blanket one). The tests below are still valid and worth keeping: they pin that these shapes decline *cleanly* (a `NativeTranslationNotSupportedException`, not a crash) and that results stay correct via fallback — that guarantee doesn't change, only the internal reason does.

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`

**Interfaces:** none new — test-only task.

- [ ] **Step 1: Add a declining test for GroupJoin's own array/grouped result shape**

```csharp
    [Fact]
    public void GroupJoin_array_result_shape_still_declines_cleanly_in_NativeOnly()
    {
        // Owned by EF-436, not this ticket — must stay declining, not silently "fixed" by this work.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(GroupJoin_array_result_shape_still_declines_cleanly_in_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .GroupJoin(db.Orders, o => o.Id, r => r.OwnerId, (o, rs) => new { o.Name, Orders = rs })
                .AsEnumerable()
                .ToList());
    }
```

- [ ] **Step 2: Add a declining test for a chained (second) join**

```csharp
    [Fact]
    public void Chained_second_join_still_declines_cleanly_in_NativeOnly()
    {
        var seed = SeedOwnersOrdersAndLines(); // extend the seed helper with a third related set
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Chained_second_join_still_declines_cleanly_in_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Join(db.OrderLines, x => x.r.Id, l => l.OrderId, (x, l) => new { x.o.Name, l.Sku })
                .AsEnumerable()
                .ToList());
    }
```

- [ ] **Step 3: Add a declining test for a query-filtered join target**

```csharp
    [Fact]
    public void Query_filtered_join_target_still_declines_cleanly_in_NativeOnly()
    {
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Query_filtered_join_target_still_declines_cleanly_in_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders.Where(r => r.Total > 0), o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
                .AsEnumerable()
                .ToList());
    }
```

- [ ] **Step 4: Add a declining test for a navigation-less key-equality join**

```csharp
    [Fact]
    public void Navigation_less_key_equality_join_still_declines_cleanly_in_NativeOnly()
    {
        // A Join on two properties with no corresponding model navigation between the two entity types.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Navigation_less_key_equality_join_still_declines_cleanly_in_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Region, r => r.Region, (o, r) => new { o.Name, r.Total })
                .AsEnumerable()
                .ToList());
    }
```

(Requires the seed fixture's `Owner`/`Order` types to share a non-FK `Region` property with no declared navigation between them — add this to the shared fixture helper if it doesn't already exist, following whatever minimal-entity pattern `SeedOwnersAndOrders` already established in Task 4.)

- [ ] **Step 5: Run all four new tests and confirm each throws for the expected reason (not a coincidental unrelated failure)**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"`
Expected: PASS — all four `Assert.Throws` succeed. If any instead throws before reaching the exercised code path (e.g. a fixture-setup exception), fix the fixture, not the assertion.

- [ ] **Step 6: Build all three EF configs and commit**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs
git commit -m "EF-392: pin explicit Join non-goals (GroupJoin array shape, chained joins, filtered target, navigation-less join) as declining"
```

---

