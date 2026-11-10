### Task 13: Only require two-phase when paging is present

`ClassifyBulkSource` routes any `OrderBy`/`OrderByDescending`/`ThenBy`/`ThenByDescending`/`Distinct` to two-phase even with no `Skip`/`Take`. Ordering cannot change *which* documents a bulk operation affects without paging, and `Distinct` over a single collection's entities is a no-op — documents are already distinct by `_id`. The PR's own test says so: `ExecuteDelete_with_OrderBy_and_no_Take_deletes_everything` is commented "semantically equivalent to the single-command path".

The cost of the misclassification is not just speed. It makes the operation require a transaction-capable deployment, makes it throw under `AutoTransactionBehavior.Never`, reads every matching document to the client, and forces the server to perform a `$sort` that cannot affect the result — where an unindexed blocking sort over 100 MB fails outright with `QueryExceededMemoryLimitNoDiskUseAllowed`. A no-op `.OrderBy(...)` can turn a working delete into a hard error.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: the `Strategy=` message segment from Task 4 and the command tests' `SpyLoggerProvider` pattern from Task 5.
- Produces: `ClassifyBulkSource` returns `SingleCommand` for chains of `Where`/`OrderBy`/`OrderByDescending`/`ThenBy`/`ThenByDescending`/`Distinct` with no `Skip`/`Take`, and `TwoPhase` only when `Skip` or `Take` is present. Its signature is unchanged.

- [ ] **Step 1: Write the failing tests**

In `ExecuteDeleteTests.cs`:

```csharp
    [Fact]
    public void ExecuteDelete_with_OrderBy_and_no_paging_uses_the_single_command_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        using var db = CreateSeededContext(loggerFactory);

        var deleted = db.Entities.Where(o => o.Status == "open").OrderBy(o => o.Quantity).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Contains("Strategy=SingleCommand", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
    }

    [Fact]
    public void ExecuteDelete_with_Distinct_and_no_paging_uses_the_single_command_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        using var db = CreateSeededContext(loggerFactory);

        var deleted = db.Entities.Where(o => o.Status == "open").Distinct().ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Contains("Strategy=SingleCommand", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
    }

    [Fact]
    public void ExecuteDelete_with_OrderBy_and_no_paging_works_under_AutoTransactionBehavior_Never()
    {
        using var db = CreateSeededContext();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        // Ordering without paging does not change the affected set, so it must not demand a transaction.
        var deleted = db.Entities.Where(o => o.Status == "open").OrderBy(o => o.Quantity).ExecuteDelete();

        Assert.Equal(2, deleted);
    }

    [Fact]
    public void ExecuteDelete_with_OrderBy_Take_still_uses_the_two_phase_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        using var db = CreateSeededContext(loggerFactory);

        var deleted = db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Contains("Strategy=TwoPhase", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
    }
```

`CreateSeededContext` currently takes no arguments. Add an optional `ILoggerFactory? loggerFactory = null` parameter to it and forward it to `SingleEntityDbContext.Create`, so no existing caller changes.

- [ ] **Step 2: Run them to verify three fail**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDelete_with_OrderBy_and_no_paging|FullyQualifiedName~ExecuteDelete_with_Distinct_and_no_paging|FullyQualifiedName~ExecuteDelete_with_OrderBy_Take_still"
```

Expected: the three new `no_paging` tests FAIL (`Strategy=TwoPhase` is logged, and the `AutoTransactionBehavior.Never` one throws); `ExecuteDelete_with_OrderBy_Take_still_uses_the_two_phase_path` PASSES.

- [ ] **Step 3: Split ordering from paging in the classifier**

Replace `ClassifyBulkSource`:

```csharp
    /// <summary>
    /// Classifies the captured source chain of a bulk delete/update.
    /// <para>
    /// <c>Where</c> predicates scope the operation directly. <c>OrderBy</c>/<c>OrderByDescending</c>/
    /// <c>ThenBy</c>/<c>ThenByDescending</c>/<c>Distinct</c> do not change *which* documents are affected —
    /// ordering only matters in combination with paging, and <c>Distinct</c> over one collection's entities is a
    /// no-op because documents are already distinct by <c>_id</c> — so on their own they stay on the atomic
    /// single-command path.
    /// </para>
    /// <para>
    /// <c>Skip</c>/<c>Take</c> do change the affected set and cannot be expressed as a single
    /// <c>deleteMany</c>/<c>updateMany</c>, so they require the two-phase (query target <c>_id</c>s, then act by
    /// <c>$in</c>) path. Any other operator is not expressible as a server-side bulk scope and produces EF's
    /// canonical non-query translation failure. A TPH discriminator filter rides along as a Where.
    /// </para>
    /// </summary>
    private MongoNonQueryExpression.BulkStrategy ClassifyBulkSource(MongoQueryExpression mongoQueryExpression)
    {
        var expression = MongoNonQueryExpression.UnwrapBulkOperator(mongoQueryExpression.CapturedExpression);
        var requiresTwoPhase = false;

        while (expression is MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType != typeof(Queryable))
            {
                ThrowBulkSourceNotSupported(mongoQueryExpression, methodCallExpression.Method.Name);
            }

            switch (methodCallExpression.Method.Name)
            {
                // Scope the operation, or are irrelevant to which documents it affects.
                case nameof(Queryable.Where):
                case nameof(Queryable.OrderBy):
                case nameof(Queryable.OrderByDescending):
                case nameof(Queryable.ThenBy):
                case nameof(Queryable.ThenByDescending):
                case nameof(Queryable.Distinct):
                    break;

                // Change the affected set; not expressible as one command.
                case nameof(Queryable.Skip):
                case nameof(Queryable.Take):
                    requiresTwoPhase = true;
                    break;

                default:
                    ThrowBulkSourceNotSupported(mongoQueryExpression, methodCallExpression.Method.Name);
                    break;
            }

            expression = methodCallExpression.Arguments[0];
        }

        return requiresTwoPhase
            ? MongoNonQueryExpression.BulkStrategy.TwoPhase
            : MongoNonQueryExpression.BulkStrategy.SingleCommand;
    }
```

- [ ] **Step 4: Make `BuildFilter` skip the ordering operators it may now see**

`BuildFilter` is now reachable for chains containing `OrderBy`/`Distinct`, and its walk currently stops at the first non-`Where`, which would silently drop inner predicates — exactly the hazard Task 11 Step 5 made explicit. Change the walk to step over the ordering operators instead of stopping:

```csharp
        Expression? combinedBody = null;
        var expression = MongoNonQueryExpression.UnwrapBulkOperator(nonQuery.SourceQuery.CapturedExpression);

        while (expression is MethodCallExpression { Method: { DeclaringType: var declaringType } method } call
               && declaringType == typeof(Queryable))
        {
            switch (method.Name)
            {
                case nameof(Queryable.Where):
                    var predicate = call.Arguments[1].UnwrapLambdaFromQuote();
                    var translatedBody = translator.Visit(predicate.Body)!;
                    translatedBody = ReplacingExpressionVisitor.Replace(
                        predicate.Parameters[0], sharedParameter, translatedBody);
                    combinedBody = combinedBody == null
                        ? translatedBody
                        : Expression.AndAlso(combinedBody, translatedBody);
                    break;

                // Do not affect which documents match; skip over them and keep collecting predicates from
                // further in. Stopping here would silently drop every inner Where.
                case nameof(Queryable.OrderBy):
                case nameof(Queryable.OrderByDescending):
                case nameof(Queryable.ThenBy):
                case nameof(Queryable.ThenByDescending):
                case nameof(Queryable.Distinct):
                    break;

                default:
                    throw new InvalidOperationException(
                        CoreStrings.NonQueryTranslationFailedWithDetails(
                            nonQuery.SourceQuery.CapturedExpression?.Print(),
                            $"The '{method.Name}' operator cannot scope a single-command bulk operation."));
            }

            expression = call.Arguments[0];
        }
```

Delete the trailing `if (expression is MethodCallExpression unexpected)` block added in Task 11 Step 5 — the `default` arm now covers it — but keep the `Debug.Assert` on the strategy.

- [ ] **Step 5: Add the interleaved-`Where` test that this makes reachable**

In `ExecuteDeleteTests.cs`:

```csharp
    [Fact]
    public void ExecuteDelete_with_Where_either_side_of_OrderBy_applies_both_predicates()
    {
        using var db = CreateSeededContext();

        // Both predicates must survive: the walk steps over OrderBy rather than stopping at it.
        var deleted = db.Entities
            .Where(o => o.Status == "open")
            .OrderBy(o => o.Quantity)
            .Where(o => o.Quantity > 1)
            .ExecuteDelete();

        Assert.Equal(1, deleted);
        Assert.Equal(2, db.Entities.Count());
    }
```

Adjust the expected counts to the seed data in `CreateSeededContext` — read it and compute the correct values rather than assuming.

- [ ] **Step 6: Run the full bulk and conformance suites on EF9 and EF10**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. Existing tests that assert *outcomes* for bare-`OrderBy`/`Distinct` shapes keep passing because the affected set is identical — that equivalence is the whole justification for this change, so if any outcome assertion changes, stop and investigate rather than editing the assertion.

- [ ] **Step 7: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/ \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "perf: keep ordering-only and Distinct bulk operations on the atomic single-command path"
```

---

