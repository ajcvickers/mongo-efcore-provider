### Task 16: Project `{_id: 1}` in phase 1

`BuildIdDocumentQuery` emits `[$match, $sort, $skip, $limit]` with no `$project`, so phase 1 transfers **whole documents** and keeps one field. The in-code comment calls a projection "a future optimization … to reduce transfer", which undersells it: besides wire bytes and client-side BSON parsing, the absence of a projection forecloses a covered index scan. With `{_id: 1}` and a supporting index, `Where(o => o.Status == "open").OrderBy(o => o.Quantity).Take(10)` can be satisfied from the index alone, because MongoDB index entries carry `_id`. Today the server must fetch every candidate document to return a field it already had.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs`

**Interfaces:**
- Consumes: `RenderBulkCommand` from Task 5.
- Produces:
  - `BuildIdDocumentQuery` appends a `{ $project: { _id: 1 } }` stage before returning.
  - `private static string RenderPhaseOnePipeline(IDiagnosticsLogger<DbLoggerCategory.Update> logger, IQueryable<BsonDocument> query)` — returns `"?"` unless sensitive logging is enabled, else the driver's rendered pipeline string.
  - `SelectTargetIds` / `SelectTargetIdsAsync` gain an `out string phaseOnePipeline` / return a tuple so the caller can pass it to the `Executing` event. Use a small holder to keep the async signature clean:
    `private sealed record PhaseOneResult(List<BsonValue> Ids, string Pipeline);`
    and both methods return `PhaseOneResult`.

- [ ] **Step 1: Write the failing test**

In `BulkOperationsCommandTests.cs`:

```csharp
    [Fact]
    public void Two_phase_delete_projects_only_the_id_in_phase_one()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(nameof(Two_phase_delete_projects_only_the_id_in_phase_one));
        collection.InsertMany(
        [
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 },
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 2 }
        ]);

        using var db = SingleEntityDbContext.Create(
            collection, loggerFactory: loggerFactory, sensitiveDataLogging: true);

        db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDelete();

        var executing = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete);
        Assert.Contains("Strategy=TwoPhase", executing);
        Assert.Contains("$project", executing);
        Assert.Contains("_id", executing);
    }
```

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~Two_phase_delete_projects_only_the_id_in_phase_one"
```

Expected: FAIL — the message contains no `$project` because phase 1 is not logged at all and no projection is emitted.

- [ ] **Step 3: Append the projection and surface the pipeline**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, replace `BuildIdDocumentQuery`'s return and add the renderer:

```csharp
    // Phase 1 only needs each matched document's _id. Appending a { $project: { _id: 1 } } stage keeps whole
    // documents off the wire and — with a supporting index — lets the server answer from the index alone,
    // because index entries carry _id. The stage is appended last, after any Distinct/Skip/Take, so it cannot
    // change which documents are selected.
    private static readonly BsonDocument IdOnlyProjectionStage = new("$project", new BsonDocument("_id", 1));

    private static IQueryable<BsonDocument> BuildIdDocumentQuery<TSource>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        var (_, executableQuery) = TranslateQuery<TSource>(
            queryContext, entityType, bsonSerializerFactory, nonQuery.SourceQuery, ResultCardinality.Enumerable,
            (translator, expression) =>
                translator.Translate(MongoNonQueryExpression.UnwrapBulkOperator(expression)!, ResultCardinality.Enumerable));

        var documents = executableQuery.Provider.CreateQuery<BsonDocument>(executableQuery.Query);

        return documents is IMongoQueryable<BsonDocument> mongoQueryable
            ? mongoQueryable.AppendStage<BsonDocument, BsonDocument, BsonDocument>(IdOnlyProjectionStage)
            : documents;
    }

    private static string RenderPhaseOnePipeline(
        IDiagnosticsLogger<DbLoggerCategory.Update> logger, IQueryable<BsonDocument> query)
        => logger.ShouldLogSensitiveData() ? query.ToString() ?? "?" : "?";
```

`AppendStage`'s exact generic arity and containing type vary across driver versions. Verify with:

```bash
grep -rn "AppendStage" ~/.nuget/packages/mongodb.driver/*/lib/net*/MongoDB.Driver.xml | head
```

If `IMongoQueryable<T>.AppendStage` is unavailable, fall back to a LINQ projection that the driver can translate — `documents.Select(d => new BsonDocument("_id", d["_id"]))` — and if *that* does not translate either, leave `BuildIdDocumentQuery` unprojected, mark this task blocked, record the driver limitation in a comment, and keep Task 17 (which delivers most of the memory win independently). Do not silently ship a projection you have not confirmed appears in the pipeline: the test in Step 1 is the gate.

- [ ] **Step 4: Thread the pipeline into the `Executing` event**

Change `SelectTargetIds`/`SelectTargetIdsAsync` to return the holder:

```csharp
    private sealed record PhaseOneResult(List<BsonValue> Ids, string Pipeline);

    private static PhaseOneResult SelectTargetIds<TSource>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        var documents = BuildIdDocumentQuery<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery);
        var pipeline = RenderPhaseOnePipeline(GetUpdateLogger(queryContext), documents);

        var ids = new List<BsonValue>();
        foreach (var document in documents)
        {
            ids.Add(document["_id"]);
        }

        return new PhaseOneResult(ids, pipeline);
    }
```

Update the four two-phase executors to destructure the result, and pass the pipeline into the `*ByIds` methods so the `Executing` command becomes both halves:

```csharp
        updateLogger.ExecutingBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, nonQuery.Strategy.ToString(), ids.Count,
            phaseOnePipeline == "?"
                ? RenderBulkCommand(updateLogger, filter, update)
                : $"{{ \"phase1\": {phaseOnePipeline}, \"phase2\": {RenderBulkCommand(updateLogger, filter, update)} }}");
```

- [ ] **Step 5: Run the command tests and the bulk suites**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsInheritanceTests"
```

Expected: PASS. The inheritance tests matter most here — the projection must not strip the discriminator *before* the `_t` filter is applied. Because the stage is appended last that should hold; if `ExecuteDelete_with_OfType_deletes_only_that_type` regresses, the stage is being inserted in the wrong position and must be moved.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs
git commit -m "perf: project only _id in bulk phase one and log the phase-one pipeline"
```

---

