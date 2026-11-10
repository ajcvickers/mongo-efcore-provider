### Task 17: Stream the async phase-1 cursor

`SelectTargetIdsAsync` calls `ToListAsync` into a `List<BsonDocument>` and *then* copies out the ids, so peak memory is every matched document plus the id list. `SelectTargetIds` enumerates the cursor lazily and keeps only ids, so its peak is one cursor batch plus the ids. The async API — the one users are steered toward — is strictly worse than the sync one for identical results.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`

**Interfaces:**
- Consumes: `PhaseOneResult` from Task 16.
- Produces: no signature changes beyond Task 16's.

- [ ] **Step 1: Replace the materialisation with cursor iteration**

```csharp
    private static async Task<PhaseOneResult> SelectTargetIdsAsync<TSource>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        var documents = BuildIdDocumentQuery<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery);
        var pipeline = RenderPhaseOnePipeline(GetUpdateLogger(queryContext), documents);
        var cancellationToken = queryContext.CancellationToken;

        // Iterate the cursor rather than ToListAsync: materialising the whole result would hold every matched
        // document in memory at once, where the sync path holds only the current batch. Same results, bounded
        // peak memory.
        var ids = new List<BsonValue>();
        using var cursor = await ((IAsyncCursorSource<BsonDocument>)documents)
            .ToCursorAsync(cancellationToken).ConfigureAwait(false);

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var document in cursor.Current)
            {
                ids.Add(document["_id"]);
            }
        }

        return new PhaseOneResult(ids, pipeline);
    }
```

If `ToCursorAsync` is not exposed as an extension on `IAsyncCursorSource<T>` in the pinned driver, use `IAsyncCursorSourceExtensions.ToCursorAsync(...)` explicitly, mirroring how the previous code qualified `IAsyncCursorSourceExtensions.ToListAsync`.

- [ ] **Step 2: Give the sync path a capacity-free but non-doubling start**

`SelectTargetIds` allocates `new List<BsonValue>()` with no capacity while the old async version passed one. Neither can know the count up front now, so leave both as `new List<BsonValue>()` and add a comment so a future reader does not "restore" the asymmetry:

```csharp
        // No capacity hint: the count is unknown until the cursor is drained, and guessing high would waste
        // more than the doubling costs.
        var ids = new List<BsonValue>();
```

- [ ] **Step 3: Run the async bulk tests on both EF versions**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS. Task 23 adds the async coverage that makes this path properly exercised; until then the diagnostics theories and the cancellation test are what cover it.

- [ ] **Step 4: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs
git commit -m "perf: stream the async bulk phase-one cursor instead of materialising every document"
```

---

