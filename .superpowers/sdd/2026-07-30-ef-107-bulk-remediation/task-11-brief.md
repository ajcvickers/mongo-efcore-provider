### Task 11: Translate before opening the transaction, and give phase 1 the canonical failure

Two ordering problems in the two-phase path:

1. `TranslateBulkOrThrow` wraps `BuildFilter` and `BuildUpdate` but **not** `SelectTargetIds` / `BuildIdDocumentQuery`. A source that only fails when phase 1 is translated — `docs/failing-spec-tests.md` cites a `GroupBy` subquery inside the `Where` — surfaces a raw driver or `ExpressionNotSupportedException` instead of EF's canonical non-query translation failure. That is the `EF-X002` defect class this repo already tracks.
2. Everything happens inside `RunInBulkTransaction`, whose catch maps *any* transaction-unsupported-looking failure to `StandaloneTransactionError`. On a standalone deployment `BeginTransaction` fails first, so an untranslatable query reports "the current MongoDB deployment does not support multi-document transactions" — an actively misleading diagnostic for a query that was never translatable.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: `RequireBulkSession` from Task 10.
- Produces: `BuildUpdate` is invoked by the two-phase executors *before* `RunInBulkTransaction`, so the `update` is passed into the body as a captured local. `RunInBulkTransaction` and `RunInBulkTransactionAsync` keep their signatures.

- [ ] **Step 1: Write the failing test**

In `ExecuteUpdateTests.cs`:

```csharp
    [Fact]
    public void ExecuteUpdate_two_phase_with_an_invalid_setter_fails_before_starting_a_transaction()
    {
        using var db = CreateSeededContext();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        // AutoTransactionBehavior.Never makes transaction startup impossible, so if the setter is validated
        // first the message must be about the setter, not about transactions.
        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.OrderBy(o => o.Quantity).Take(1)
                .ExecuteUpdate(s => s.SetProperty(o => o._id, ObjectId.GenerateNewId())));

        Assert.Contains("primary key", ex.Message);
        Assert.DoesNotContain("AutoTransactionBehavior", ex.Message);
    }
```

Note: the primary-key setter is rejected at *translation* time by Task 7, so this test also passes today. Verify that before relying on it — if it passes, replace the setter with one that only fails when the update document is built, and if no such shape exists in the supported subset, keep this test as a regression guard and rely on Step 3's restructure being verified by the conformance suite instead. Record whichever choice you make in a comment on the test.

- [ ] **Step 2: Run it and record the outcome**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdate_two_phase_with_an_invalid_setter_fails_before_starting_a_transaction"
```

- [ ] **Step 3: Give `BeginTransaction` its own mapped try, so body failures propagate untouched**

Replace `RunInBulkTransaction`:

```csharp
    private static int RunInBulkTransaction(QueryContext queryContext, MongoNonQueryExpression nonQuery, Func<int> body)
    {
        var database = queryContext.Context.Database;
        if (database.CurrentTransaction != null)
        {
            RequireBulkSession(database);
            return body();
        }

        if (database.AutoTransactionBehavior == AutoTransactionBehavior.Never)
        {
            throw NoAutoTransactionError();
        }

        // Transaction startup gets its own try so that only startup failures are mapped to
        // StandaloneTransactionError. Failures from the body — including translation failures raised when
        // phase 1 is built — must propagate with their own message rather than being relabelled as
        // "this deployment does not support transactions".
        IDbContextTransaction transaction;
        try
        {
            transaction = database.BeginTransaction();
        }
        catch (Exception exception) when (IsTransactionsUnsupported(exception))
        {
            throw StandaloneTransactionError(exception);
        }

        try
        {
            var result = body();
            transaction.Commit();
            return result;
        }
        catch
        {
            SafeRollback(transaction);
            throw;
        }
        finally
        {
            transaction.Dispose();
        }
    }
```

Apply the same restructure to `RunInBulkTransactionAsync`, using `await database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)`, `await transaction.CommitAsync(cancellationToken)`, `await SafeRollbackAsync(transaction)` (see Task 12 for its signature change) and `await transaction.DisposeAsync()`.

- [ ] **Step 4: Wrap phase 1 and hoist `BuildUpdate` out of the transaction**

Replace `ExecuteTwoPhaseUpdate` and `ExecuteTwoPhaseDelete` (and their async twins) so the update document is built first and phase 1 is wrapped:

```csharp
    private static int ExecuteTwoPhaseUpdate<TSource>(
        QueryContext queryContext, IReadOnlyEntityType entityType, BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        // Built before the transaction: an unsupported setter is a translation error and must not be reported
        // as a transaction problem, nor cost a transaction round trip.
        var update = TranslateBulkOrThrow(nonQuery,
            () => BuildUpdate<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery));

        return RunInBulkTransaction(queryContext, nonQuery, () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var ids = TranslateBulkOrThrow(nonQuery,
                () => SelectTargetIds<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery));
            return ids.Count == 0 ? 0 : UpdateByIds(queryContext, nonQuery, ids, update, stopwatch);
        });
    }
```

`ExecuteTwoPhaseDelete` gets the same `TranslateBulkOrThrow` wrapper around `SelectTargetIds` and has no update to hoist. The async twins wrap `SelectTargetIdsAsync`; because `TranslateBulkOrThrow` is synchronous, wrap the *awaited result* rather than the task — extract the body into a local async function and wrap the `await` in a try/catch that mirrors `TranslateBulkOrThrow`'s mapping, or add an async overload:

```csharp
    private static async Task<T> TranslateBulkOrThrowAsync<T>(MongoNonQueryExpression nonQuery, Func<Task<T>> translate)
    {
        try
        {
            return await translate().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                CoreStrings.NonQueryTranslationFailedWithDetails(
                    nonQuery.SourceQuery.CapturedExpression?.Print(), exception.Message),
                exception);
        }
    }
```

Use the async overload in `ExecuteTwoPhaseDeleteAsync` and `ExecuteTwoPhaseUpdateAsync`.

- [ ] **Step 5: Update the stale `BuildFilter` invariant comment and assert the strategy**

`BuildFilter`'s comment names `ValidateBulkSource`, which does not exist, and claims the classifier rejects every operator but `Where` — no longer true. `BuildFilter` walks only `Where` nodes and stops at the first non-`Where`, silently dropping the inner predicates, so `Where(a).OrderBy(k).Where(b)` would filter on `b` alone and delete more than requested. It is unreachable today only because of strategy dispatch. Make that explicit:

```csharp
        // Only the single-command strategy may reach here. The walk below stops at the first non-Where node and
        // would silently drop every predicate inside it (Where(a).OrderBy(k).Where(b) would filter on b alone),
        // so a two-phase chain must never be routed through this method — it uses the read path instead.
        Debug.Assert(
            nonQuery.Strategy == MongoNonQueryExpression.BulkStrategy.SingleCommand,
            "BuildFilter is only valid for the SingleCommand strategy.");

        var sharedParameter = Expression.Parameter(typeof(TSource), "e");
```

and after the walk, before the `combinedBody == null` check:

```csharp
        if (expression is MethodCallExpression unexpected)
        {
            throw new InvalidOperationException(
                CoreStrings.NonQueryTranslationFailedWithDetails(
                    nonQuery.SourceQuery.CapturedExpression?.Print(),
                    $"The '{unexpected.Method.Name}' operator cannot scope a single-command bulk operation."));
        }
```

- [ ] **Step 6: Run everything**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. Several conformance cases currently tagged `EF-X002` ("throws a different exception") may now produce the canonical message; if so, change their assertion from `Assert.ThrowsAnyAsync<Exception>` to `AssertTranslationFailed` and note the move for Task 25's `docs/failing-spec-tests.md` update.

- [ ] **Step 7: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/
git commit -m "fix: translate before opening the bulk transaction and give phase 1 the canonical failure"
```

---

