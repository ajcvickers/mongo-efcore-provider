### Task 10: Require a real `MongoTransaction` when joining an ambient transaction

`RunInBulkTransaction` trusts a non-null `Database.CurrentTransaction` and runs the body directly, but the session is fetched with `as MongoTransaction` in two places (`GetBulkCollectionAndSession` and `TranslateQuery`). If the ambient transaction is not a `MongoTransaction`, both casts yield null and **both phases run with no session — outside any transaction** — while `RunInBulkTransaction` believes it is inside one.

That matters because the transaction is what makes two-phase safe: phase 2 filters on `_id` alone and never re-evaluates the user's predicate, so if that predicate is an authorization or tenancy check, only transaction isolation prevents acting on a document that no longer satisfies it. Silently losing the session silently loses that guarantee.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `private static IClientSessionHandle RequireBulkSession(DatabaseFacade database)` — returns the session of the ambient `MongoTransaction`, throwing `InvalidOperationException` when `CurrentTransaction` is not a usable `MongoTransaction`. Used by `RunInBulkTransaction` and `RunInBulkTransactionAsync`.

- [ ] **Step 1: Write the failing test**

In `ExecuteDeleteTests.cs`:

```csharp
    private sealed class NotAMongoTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public void Commit() { }
        public void Rollback() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public void ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected()
    {
        using var db = CreateSeededContext();
        var manager = (MongoTransactionManager)db.Database.GetService<IDbContextTransactionManager>();
        manager.CurrentTransaction = new NotAMongoTransaction();

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDelete());

        Assert.Contains("transaction", ex.Message);
        Assert.Equal(3, db.Entities.Count());
    }
```

The `MongoTransactionManager.CurrentTransaction` setter is reachable from tests via `InternalsVisibleTo` — `NorthwindBulkUpdatesMongoFixture.UseTransaction` already uses exactly this. Copy its cast and property access. Add `using Microsoft.EntityFrameworkCore.Storage;`, `using Microsoft.EntityFrameworkCore.Infrastructure;`, `using System.Threading;` and `using System.Threading.Tasks;` as needed.

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDelete_two_phase_with_a_foreign_ambient_transaction"
```

Expected: FAIL — no exception is thrown and one document is deleted, because the operation silently ran with `session == null`.

- [ ] **Step 3: Add the guard**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, add next to `RunInBulkTransaction`:

```csharp
    // Two-phase correctness depends on phase 1 and phase 2 sharing one transaction: phase 2 filters on _id
    // alone and never re-evaluates the user's predicate, so isolation is what stops it acting on a document
    // that no longer matches. The session is read from CurrentTransaction with an `as MongoTransaction` cast,
    // which yields null for any other IDbContextTransaction implementation — and a null session means both
    // phases quietly run outside the transaction. Fail closed instead.
    private static IClientSessionHandle RequireBulkSession(DatabaseFacade database)
        => (database.CurrentTransaction as MongoTransaction)?.Session
           ?? throw new InvalidOperationException(
               "This bulk delete or update requires a MongoDB transaction, but the ambient transaction on this "
               + "DbContext is not a MongoDB transaction with an active session. Open the transaction with "
               + "Database.BeginTransaction() on a MongoDB-backed context.");
```

In both `RunInBulkTransaction` and `RunInBulkTransactionAsync`, replace the ambient-transaction fast path:

```csharp
        var database = queryContext.Context.Database;
        if (database.CurrentTransaction != null)
        {
            // Validate the ambient transaction is usable before running either phase; we join it and never
            // commit it, because the caller owns it.
            RequireBulkSession(database);
            return body();
        }
```

(`RunInBulkTransactionAsync` uses `return await body().ConfigureAwait(false);`.)

Add `using Microsoft.EntityFrameworkCore.Infrastructure;` for `DatabaseFacade` if it is not already imported.

- [ ] **Step 4: Run the test and the transaction suites**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. The conformance suite is the important check — its fixture enlists a real `MongoTransaction` via `UseTransaction`, so `RequireBulkSession` must accept it. If the suite now fails, the fixture is assigning something whose `Session` is null and the guard has found a genuine problem in the fixture; fix the fixture, not the guard.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "fix: require a usable MongoDB session when joining an ambient transaction for bulk ops"
```

---

