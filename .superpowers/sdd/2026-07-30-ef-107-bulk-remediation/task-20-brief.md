### Task 20: Cover global query filters, and correct the compliance exclusions

`HasQueryFilter` is this provider's soft-delete and multi-tenancy mechanism — `CrossCollectionIncludeTests.cs:414` describes it in exactly those terms, and `:306` notes filters "lower to a `Where`". Reading the code, filters *are* honoured: nav expansion injects them before `_finalExpression` is captured, so they appear as `Where` nodes that `ClassifyBulkSource` accepts and `BuildFilter` `AndAlso`s together. Nothing verifies it. If it were ever wrong, `ExecuteDelete` would destroy another tenant's data.

EF ships a conformance suite for this, and `MongoComplianceTest.cs` keeps it excluded under a comment this PR added:

> `// … These EF conformance suites target owned-collection, table-sharing, cross-collection/navigation, and join scenarios outside that supported subset, so they remain ignored here`
> applied to `BulkUpdatesTestBase<>`, `FiltersInheritanceBulkUpdatesTestBase<>`, `InheritanceBulkUpdatesTestBase<>`, `NonSharedModelBulkUpdatesTestBase`

That reason fits `NonSharedModelBulkUpdatesTestBase`. It does not fit `FiltersInheritanceBulkUpdatesTestBase`: this provider maps each hierarchy to one collection per root, so a query-filtered bulk delete over a hierarchy is a single-collection `Where`-scoped operation — inside the subset the comment says is supported.

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsQueryFilterTests.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoComplianceTest.cs`

**Interfaces:**
- Consumes: `SpyLoggerProvider` and the `Strategy=` message segment from Tasks 4–5.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Write the tests**

Create `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsQueryFilterTests.cs` with the standard licence header, then:

```csharp
// Global query filters are how soft-delete and multi-tenancy are expressed in this provider. If a bulk
// operation ignored them it would delete or overwrite documents the application believes are invisible, so
// this is covered on both execution strategies.

#if !EF8

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

[XUnitCollection("QueryTests")]
public class BulkOperationsQueryFilterTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Doc
    {
        public ObjectId _id { get; set; }
        public string Tenant { get; set; } = null!;
        public bool Deleted { get; set; }
        public int Quantity { get; set; }
    }

    private static void SoftDelete(ModelBuilder mb)
        => mb.Entity<Doc>().HasQueryFilter(d => !d.Deleted);

    private static void SingleTenant(ModelBuilder mb)
        => mb.Entity<Doc>().HasQueryFilter(d => d.Tenant == "a");

    private IMongoCollection<Doc> Seed(string name)
    {
        var collection = database.CreateCollection<Doc>(name);
        collection.InsertMany(
        [
            new Doc { _id = ObjectId.GenerateNewId(), Tenant = "a", Deleted = false, Quantity = 1 },
            new Doc { _id = ObjectId.GenerateNewId(), Tenant = "a", Deleted = true, Quantity = 2 },
            new Doc { _id = ObjectId.GenerateNewId(), Tenant = "b", Deleted = false, Quantity = 3 }
        ]);
        return collection;
    }

    [Fact]
    public void Single_command_delete_honours_the_soft_delete_filter()
    {
        var collection = Seed(nameof(Single_command_delete_honours_the_soft_delete_filter));
        using var db = SingleEntityDbContext.Create(collection, SoftDelete);

        var deleted = db.Entities.ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(1, collection.CountDocuments(FilterDefinition<Doc>.Empty));
    }

    [Fact]
    public void Single_command_delete_honours_the_tenant_filter_combined_with_a_user_predicate()
    {
        var collection = Seed(nameof(Single_command_delete_honours_the_tenant_filter_combined_with_a_user_predicate));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var deleted = db.Entities.Where(d => d.Quantity >= 1).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(1, collection.CountDocuments(FilterDefinition<Doc>.Empty));
        Assert.Equal("b", collection.Find(FilterDefinition<Doc>.Empty).Single().Tenant);
    }

    [Fact]
    public void Two_phase_delete_honours_the_tenant_filter()
    {
        var collection = Seed(nameof(Two_phase_delete_honours_the_tenant_filter));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var deleted = db.Entities.OrderBy(d => d.Quantity).Take(5).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal("b", collection.Find(FilterDefinition<Doc>.Empty).Single().Tenant);
    }

    [Fact]
    public void Single_command_update_honours_the_tenant_filter()
    {
        var collection = Seed(nameof(Single_command_update_honours_the_tenant_filter));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(d => d.Quantity, 99));

        Assert.Equal(2, updated);
        Assert.Equal(3, collection.Find(Builders<Doc>.Filter.Eq(d => d.Tenant, "b")).Single().Quantity);
    }

    [Fact]
    public void IgnoreQueryFilters_widens_the_scope_as_EF_intends()
    {
        var collection = Seed(nameof(IgnoreQueryFilters_widens_the_scope_as_EF_intends));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var deleted = db.Entities.IgnoreQueryFilters().ExecuteDelete();

        Assert.Equal(3, deleted);
    }
}

#endif
```

If `HasQueryFilter(...)` is not available on the pinned EF9 API in this form, copy the exact call shape from `GlobalQueryTests.cs:33`.

- [ ] **Step 2: Run them**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsQueryFilterTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~BulkOperationsQueryFilterTests"
```

Expected: PASS. **If any of the first four fail, stop everything and escalate** — a bulk operation ignoring a query filter is the most severe possible outcome in this feature, and the fix belongs ahead of every other task in this plan. `IgnoreQueryFilters_widens_the_scope_as_EF_intends` may legitimately fail with a translation error if EF leaves the call in the chain; if so, that is a genuine (smaller) finding: record it and either handle `IgnoreQueryFilters` in the classifier or convert the test to `Assert.Throws` with a comment naming the limitation.

- [ ] **Step 3: Replace the blanket compliance comment with per-suite reasons**

In `MongoComplianceTest.cs`, replace the single comment above the four bulk suites with one reason each:

```csharp
        // Bulk ExecuteUpdate/ExecuteDelete ARE supported (EF9+) for a single collection scoped by Where /
        // ordering / paging with scalar setters. Each suite below is excluded for its own reason:

        // Owned-collection and table-sharing update scenarios, which have no document-model equivalent.
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.NonSharedModelBulkUpdatesTestBase),
        // Exercises the full relational bulk surface (joins, set operations, multiple-table updates); the
        // supported subset is covered by NorthwindBulkUpdatesMongoTest instead.
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.BulkUpdatesTestBase<>),
        // TPT/TPC-shaped inheritance updates. This provider maps one collection per hierarchy root, so these
        // cases have no analogue; TPH bulk behaviour is covered by
        // FunctionalTests/Query/BulkOperationsInheritanceTests.cs.
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.InheritanceBulkUpdatesTestBase<>),
```

and **remove** `FiltersInheritanceBulkUpdatesTestBase<>` from the ignore list, replacing it with an implementation. If implementing it turns out to require model shapes the provider does not support, put it back with a reason that is specifically about *those shapes* — not the blanket join/table-sharing claim — and reference `BulkOperationsQueryFilterTests` as the substitute coverage.

- [ ] **Step 4: Implement the filters-inheritance suite, or record why not**

Create `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/FiltersInheritanceBulkUpdatesMongoTest.cs` following the structure of `NorthwindBulkUpdatesMongoTest.cs` (same fixture pattern, same `// Fails: <reason> <ticket>` convention for out-of-subset cases). Run:

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~FiltersInheritanceBulkUpdatesMongoTest|FullyQualifiedName~MongoComplianceTest"
```

Expected: `MongoComplianceTest` PASSES (it enforces that every non-ignored EF test base has an implementation), and the new suite either passes or has each failing case tagged.

- [ ] **Step 5: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsQueryFilterTests.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/
git commit -m "test: cover global query filters with bulk operations and give each compliance exclusion a reason"
```

---

