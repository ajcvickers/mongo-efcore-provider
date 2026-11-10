### Task 12: Fix cancellation cleanup and add cancellation coverage

`RunInBulkTransactionAsync`'s catch calls `SafeRollbackAsync(transaction, cancellationToken)` with **the token that just cancelled the operation**. `RollbackAsync` on an already-cancelled token faults immediately, that fault is swallowed by the bare `catch { }`, and the transaction is therefore never aborted client-side — it lingers until `transactionLifetimeLimitSeconds`. Cleanup must not observe the caller's cancellation. Separately, both `SafeRollback` helpers catch absolutely everything with no filter.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `SafeRollbackAsync` loses its `CancellationToken` parameter — new signature `private static async Task SafeRollbackAsync(IDbContextTransaction transaction)`.

- [ ] **Step 1: Write the failing test**

In `ExecuteDeleteTests.cs`:

```csharp
    [Fact]
    public async Task ExecuteDeleteAsync_two_phase_cancelled_leaves_data_unchanged()
    {
        using var db = CreateSeededContext();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDeleteAsync(cts.Token));

        Assert.Equal(3, db.Entities.Count());
    }
```

- [ ] **Step 2: Run it to verify behaviour**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteAsync_two_phase_cancelled_leaves_data_unchanged"
```

Expected: FAIL or hang. If it passes, it is passing for the wrong reason — the data is unchanged because nothing was written, not because the transaction was aborted. Either way proceed to Step 3; the assertion is worth keeping as the regression guard, and Step 3 is the substantive fix.

- [ ] **Step 3: Stop passing the caller's token into cleanup, and filter the catches**

```csharp
    private static void SafeRollback(IDbContextTransaction transaction)
    {
        try
        {
            transaction.Rollback();
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or StackOverflowException))
        {
            // A failed or non-transactional session may not be rollback-able; the original error wins. The
            // transaction is never durable while uncommitted, so the worst case is that the server reclaims it
            // at transactionLifetimeLimitSeconds.
        }
    }

    // Deliberately takes no CancellationToken: this runs on the failure path, and when the failure *is*
    // cancellation, passing the caller's already-cancelled token would make RollbackAsync fault immediately
    // and leave the transaction open until the server's lifetime limit expires.
    private static async Task SafeRollbackAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or StackOverflowException))
        {
            // See SafeRollback.
        }
    }
```

Update the one call site in `RunInBulkTransactionAsync` to `await SafeRollbackAsync(transaction).ConfigureAwait(false);`.

Also change the `finally` disposal so cleanup does not observe cancellation — `await transaction.DisposeAsync().ConfigureAwait(false)` already takes no token, so no change is needed there; and change `await transaction.CommitAsync(cancellationToken)` to keep the token, since commit *should* honour cancellation.

- [ ] **Step 4: Run the async bulk tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS, including the new cancellation test.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "fix: roll back bulk transactions with an uncancelled token and filter the rollback catches"
```

---

## Phase 4 — Classification

