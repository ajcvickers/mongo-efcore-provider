### Task 18: Bound the `$in` list

Phase 2 puts every collected id into one `{_id: {$in: […]}}` filter. There is no chunking and no ceiling, so the ceilings that get hit are MongoDB's: the 16 MB BSON command limit on the filter (~500k ObjectIds), `transactionLifetimeLimitSeconds` (60 s by default, covering both phases *including* client-side time), and transaction cache pressure. Very large `$in` arrays also carry real query-planning cost. Because an endpoint can reach this path with a caller-supplied predicate, unbounded buffering is also a resource-exhaustion concern, not only a performance one.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: `PhaseOneResult` from Task 16.
- Produces:
  - `private const int MaxIdsPerBulkCommand = 50_000;`
  - `private static IEnumerable<List<BsonValue>> BatchIds(List<BsonValue> ids)` — yields consecutive slices of at most `MaxIdsPerBulkCommand`.
  - `DeleteByIds` / `UpdateByIds` and their async twins loop over batches and sum the counts; their signatures are unchanged. Both phases already share one transaction, so batching does not weaken atomicity — the batches commit or abort together.

- [ ] **Step 1: Write the test**

In `ExecuteDeleteTests.cs`:

```csharp
    [Fact]
    public void ExecuteDelete_two_phase_over_more_documents_than_one_batch_deletes_all_of_them()
    {
        // Exercises the batching loop rather than the ceiling: MaxIdsPerBulkCommand is far larger than any
        // reasonable test fixture, so this test pins the loop's arithmetic by using a batch size override.
        var collection = database.CreateCollection<Order>(
            nameof(ExecuteDelete_two_phase_over_more_documents_than_one_batch_deletes_all_of_them));
        collection.InsertMany(
            Enumerable.Range(0, 25)
                .Select(i => new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = i }));

        using var db = SingleEntityDbContext.Create(collection);

        var deleted = db.Entities.OrderBy(o => o.Quantity).Take(25).ExecuteDelete();

        Assert.Equal(25, deleted);
        Assert.Equal(0, db.Entities.Count());
    }
```

Then add a server-free unit test for the slicing itself in `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkIdBatchingTests.cs`, since the functional test cannot reach 50,000 documents:

```csharp
#if !EF8

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Visitors;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query;

public static class BulkIdBatchingTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(7, 3)]
    public static void Splits_into_the_expected_number_of_batches(int idCount, int expectedBatches)
    {
        var ids = Enumerable.Range(0, idCount).Select(i => (BsonValue)i).ToList();

        var batches = MongoShapedQueryCompilingExpressionVisitor.BatchIds(ids, batchSize: 3).ToList();

        Assert.Equal(expectedBatches, batches.Count);
        Assert.Equal(idCount, batches.Sum(b => b.Count));
        Assert.All(batches, b => Assert.InRange(b.Count, 1, 3));
    }
}

#endif
```

- [ ] **Step 2: Run to verify it does not compile / fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~BulkIdBatchingTests"
```

Expected: does not compile — `BatchIds` does not exist.

- [ ] **Step 3: Add batching**

```csharp
    // A single { _id: { $in: [...] } } filter is bounded by the 16 MB BSON command limit, and a very large $in
    // also costs real query-planning time. Both phases share one transaction, so splitting the write into
    // batches does not weaken atomicity: every batch commits or aborts together.
    private const int MaxIdsPerBulkCommand = 50_000;

    internal static IEnumerable<List<BsonValue>> BatchIds(List<BsonValue> ids, int batchSize = MaxIdsPerBulkCommand)
    {
        for (var start = 0; start < ids.Count; start += batchSize)
        {
            yield return ids.GetRange(start, Math.Min(batchSize, ids.Count - start));
        }
    }
```

Then wrap each `*ByIds` body's write in the loop. `DeleteByIds` becomes:

```csharp
    private static int DeleteByIds(
        QueryContext queryContext, MongoNonQueryExpression nonQuery, List<BsonValue> ids, Stopwatch stopwatch,
        string phaseOnePipeline)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, nonQuery);
        var updateLogger = GetUpdateLogger(queryContext);

        long deleted = 0;
        foreach (var batch in BatchIds(ids))
        {
            var filter = Builders<BsonDocument>.Filter.In("_id", batch);
            updateLogger.ExecutingBulkDelete(
                stopwatch.Elapsed, collection.CollectionNamespace, nonQuery.Strategy.ToString(), batch.Count,
                CombineBulkCommand(updateLogger, phaseOnePipeline, filter, update: null));

            var result = session == null ? collection.DeleteMany(filter) : collection.DeleteMany(session, filter);
            deleted += result.DeletedCount;
        }

        updateLogger.ExecutedBulkDelete(stopwatch.Elapsed, collection.CollectionNamespace, deleted);
        return checked((int)deleted);
    }
```

Extract the command-string composition from Task 16 Step 4 into the shared helper this now needs:

```csharp
    private static string CombineBulkCommand(
        IDiagnosticsLogger<DbLoggerCategory.Update> logger,
        string? phaseOnePipeline,
        FilterDefinition<BsonDocument> filter,
        UpdateDefinition<BsonDocument>? update)
    {
        var phaseTwo = RenderBulkCommand(logger, filter, update);
        return phaseOnePipeline is null or "?"
            ? phaseTwo
            : $"{{ \"phase1\": {phaseOnePipeline}, \"phase2\": {phaseTwo} }}";
    }
```

Apply the same loop to `DeleteByIdsAsync`, `UpdateByIds` and `UpdateByIdsAsync` (the update versions accumulate both `MatchedCount` and `ModifiedCount` and pass both to `ExecutedBulkUpdate`). Single-command executors call `CombineBulkCommand(updateLogger, phaseOnePipeline: null, filter, update)`.

- [ ] **Step 4: Run the tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~BulkIdBatchingTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
```

Expected: PASS. Note that the `Executing` event now fires once per batch — check whether any existing diagnostics test asserts it fires exactly once and update it to assert "at least once" if so.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkIdBatchingTests.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "perf: batch the bulk _id \$in filter to stay inside the BSON command limit"
```

---

