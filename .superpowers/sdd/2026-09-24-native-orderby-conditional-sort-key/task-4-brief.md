### Task 4: Functional differential-correctness tests against a real database

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinScopeConditionalProjectionTests.cs` (add new test methods reusing the existing `Order`/`Customer`/`Region`/`JoinScopeDbContext` fixture — no new model classes needed)

**Interfaces:**
- Consumes: the existing `JoinScopeDbContext(TemporaryDatabaseFixture, string ordersCollection, string customersCollection, string regionsCollection, MongoQueryMode mode)` constructor and `CreateCollectionNames(string testName)` helper, unchanged.
- Produces: nothing new for later tasks — this is a leaf verification task.

Mirrors the file's own `Bare_nav_null_check_ternary_over_a_reference_join_matches_oracle` structure (seed data, `NativeOnly` + `Native` theory, in-memory LINQ oracle comparison), but for an `OrderBy` key instead of a `Select` projection.

- [ ] **Step 1: Write the failing functional test**

Add to `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinScopeConditionalProjectionTests.cs`, after the existing `Bare_nav_null_check_ternary_over_a_two_level_chain_matches_oracle` test and its `_under_DriverLinq_pins_known_fallback_bug` sibling (added in Phase 1's final review), before the `CreateCollectionNames` helper:

```csharp
    [Theory]
    [InlineData(MongoQueryMode.Native)]
    [InlineData(MongoQueryMode.NativeOnly)]
    public void OrderBy_bare_nav_null_check_ternary_matches_oracle(MongoQueryMode mode)
    {
        var (ordersName, customersName, regionsName) =
            CreateCollectionNames(nameof(OrderBy_bare_nav_null_check_ternary_matches_oracle));

        var matchedCustomerId = ObjectId.GenerateNewId();
        var order1Id = ObjectId.GenerateNewId();
        var order2Id = ObjectId.GenerateNewId();
        var order3Id = ObjectId.GenerateNewId();
        // A dangling FK: generated but never inserted into Customers, so the $lookup finds no match.
        var danglingCustomerId = ObjectId.GenerateNewId();

        using (var seed = new JoinScopeDbContext(database, ordersName, customersName, regionsName, MongoQueryMode.DriverLinq))
        {
            seed.Set<Customer>().Add(new Customer { Id = matchedCustomerId, Name = "Bravo" });
            seed.Set<Order>().AddRange(
                new Order { Id = order1Id, OrderNo = 1, CustomerId = matchedCustomerId },
                new Order { Id = order2Id, OrderNo = 2, CustomerId = null },
                new Order { Id = order3Id, OrderNo = 3, CustomerId = danglingCustomerId });
            seed.SaveChanges();
        }

        using var db = new JoinScopeDbContext(database, ordersName, customersName, regionsName, mode);

        var actual = db.Set<Order>()
            .OrderBy(o => o.Customer != null ? o.Customer.Name : NoneSentinel)
            .ThenBy(o => o.OrderNo)
            .Select(o => o.OrderNo)
            .ToList();

        // Independent in-memory oracle over the same seeded rows, sharing no code with the provider.
        var orderSeeds = new[]
        {
            new { OrderNo = 1, CustomerId = (ObjectId?)matchedCustomerId },
            new { OrderNo = 2, CustomerId = (ObjectId?)null },
            new { OrderNo = 3, CustomerId = (ObjectId?)danglingCustomerId }
        };
        var customerSeeds = new[] { new { Id = matchedCustomerId, Name = (string?)"Bravo" } };

        var oracle = orderSeeds
            .GroupJoin(customerSeeds, o => o.CustomerId, c => (ObjectId?)c.Id, (o, cs) => new { o, cs })
            .SelectMany(x => x.cs.DefaultIfEmpty(), (x, c) => new { x.o, c })
            .OrderBy(x => x.c != null ? x.c.Name : NoneSentinel)
            .ThenBy(x => x.o.OrderNo)
            .Select(x => x.o.OrderNo)
            .ToList();

        Assert.Equal(oracle, actual);
        Assert.Equal(3, actual.Count);
        // "Bravo" sorts before the sentinel "<none>", so the matched order comes first; the null-CustomerId
        // and dangling-FK rows (both null Customer) tie on the primary key and break the tie on OrderNo.
        Assert.Equal([1, 2, 3], actual);
    }
```

- [ ] **Step 2: Run it to verify it fails before Task 3's fix (skip if run after Task 3 — see note)**

If running this task strictly after Task 3 (as this plan orders it), the test will already **PASS** — Task 3 already implements the underlying capability, and this task only adds end-to-end proof against a real database. Run it once to confirm:

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~OrderBy_bare_nav_null_check_ternary_matches_oracle"
```

Expected: **PASS**, both `mode: Native` and `mode: NativeOnly`. If this were run against the pre-Task-3 source (e.g., to double check independently), `mode: NativeOnly` would fail with `NativeTranslationNotSupportedException` and `mode: Native` would still pass (via fallback) — that split is what proves the test is actually exercising native-vs-fallback, not just correctness in general.

- [ ] **Step 3: Run the whole functional test class to confirm no regression**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinScopeConditionalProjectionTests"
```

Expected: **all PASS** (7 tests: the original 6 plus this new one).

- [ ] **Step 4: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinScopeConditionalProjectionTests.cs
git commit -m "EF-322: add differential-correctness functional test for conditional ORDER BY"
```

---

