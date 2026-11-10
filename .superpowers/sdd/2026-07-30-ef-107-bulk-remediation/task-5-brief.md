### Task 5: Emit the redaction-gated command, and make elapsed span both phases

> ### AMENDED FOR `main` (2026-07-30) — read this before the steps below
>
> The findings are unchanged and both still present, but the code moved. The step bodies below place everything in `MongoShapedQueryCompilingExpressionVisitor`; **use these locations instead:**
>
> - **`RenderBulkCommand` and `CombineBulkCommand` go in `Storage/MongoBulkOperationExecutor.cs`.** That is where the rendered `FilterDefinition<BsonDocument>` and `UpdateDefinition<BsonDocument>` exist — `MongoBulkPlan` hands them over as the results of its `BuildFilter`/`BuildUpdate` delegates. Do not move rendering into Query.
> - **The stopwatch fix is in the executor.** `ExecuteTwoPhaseDelete`, `ExecuteTwoPhaseDeleteAsync`, `ExecuteTwoPhaseUpdate` and `ExecuteTwoPhaseUpdateAsync` currently let `DeleteByIds`/`UpdateByIds` (+async) start their own `Stopwatch`, so elapsed covers only phase 2. Start it in the four `ExecuteTwoPhase*` methods before the phase-1 call and pass it into the `*ByIds` methods, exactly as the step body describes — just in the executor's methods.
> - **`SelectTargetIds`/`SelectTargetIdsAsync` are named `MaterializeIds`/`MaterializeIdsAsync`** in the executor, and they take the already-built `IQueryable<BsonDocument>` rather than building it. Phase 1's query comes from `plan.BuildTargetIdQuery(queryContext)`, invoked in the `ExecuteTwoPhase*` methods.
> - **Event data:** add `string? Command` and `bool LogSensitiveData` to **both** `MongoBulkDeleteEventData` and `MongoBulkUpdateEventData` (Task 4 already added `MatchedCount` to the update one). Both get a new trailing constructor parameter pair; document `Command` as "The rendered MongoDB command for this operation, or `\"?\"` when sensitive-data logging is disabled."
> - **Definition arity:** Task 4 collapsed the `Executing` definitions to `EventDefinition<string, CollectionNamespace, string, string>` (elapsed, namespace, strategy, targets). Extend that to `EventDefinition<string, CollectionNamespace, string, string, string>` with `command` last, and append `, Command={command}` to both `Executing` message templates. The `Executed` definitions do not carry the command.
> - **`SingleEntityDbContext.Create`:** check its real overloads before writing the tests. Earlier tasks in this plan found the argument order is `(collection, loggerFactory, configureModel)` in some call sites — copy a working call from `ExecuteUpdateTests.cs` rather than trusting the step body's `sensitiveDataLogging:` named argument, and add the parameter only if it genuinely does not exist.
>
> Everything else in this task — the three-plus-one tests, the redaction gate, the `"?"` default — is unchanged.

The bulk events record namespace, elapsed and counts but never the rendered filter or update document, so nothing can assert what command was produced — the conformance suite's own comment cites this as the reason it has no MQL baseline. And the two-phase stopwatch is created inside `UpdateByIds`/`DeleteByIds`, *after* phase 1 has finished, so the reported duration omits the expensive half.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs`

**Interfaces:**
- Consumes: the logger signatures from Task 4.
- Produces, relied on by Tasks 8, 14, 15, 17 and 21:
  - `private static string RenderBulkCommand(IDiagnosticsLogger<DbLoggerCategory.Update> logger, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument>? update)` — returns `"?"` unless `logger.ShouldLogSensitiveData()`, else `{ "filter": <filter>, "update": <update> }` as a single-line JSON string with `update` omitted for deletes.
  - Both two-phase executors create their `Stopwatch` before phase 1 and pass it down, so `Executing`/`Executed` elapsed covers phase 1 + phase 2.

- [ ] **Step 1: Write the failing test**

Create `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs` with the standard licence header, then:

```csharp
// Pins the command the bulk path actually sends. Counts and before/after state cannot distinguish a
// correct $set from one written to the wrong element, which is what these assertions are for.
// Command content is sensitive, so it only appears when EnableSensitiveDataLogging is on — these tests
// opt in explicitly and one asserts the redacted default.

#if !EF8

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

[XUnitCollection("QueryTests")]
public class BulkOperationsCommandTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!;
        public int Quantity { get; set; }
    }

    [Fact]
    public void Single_command_delete_logs_its_filter_when_sensitive_logging_enabled()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(
            nameof(Single_command_delete_logs_its_filter_when_sensitive_logging_enabled));
        collection.InsertOne(new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 });

        using var db = SingleEntityDbContext.Create(
            collection, loggerFactory: loggerFactory, sensitiveDataLogging: true);

        db.Entities.Where(o => o.Status == "open").ExecuteDelete();

        var executing = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete);
        Assert.Contains("Strategy=SingleCommand", executing);
        Assert.Contains("\"Status\" : \"open\"", executing);
    }

    [Fact]
    public void Command_is_redacted_when_sensitive_logging_disabled()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(nameof(Command_is_redacted_when_sensitive_logging_disabled));
        collection.InsertOne(new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 });

        using var db = SingleEntityDbContext.Create(collection, loggerFactory: loggerFactory);

        db.Entities.Where(o => o.Status == "open").ExecuteDelete();

        var executing = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete);
        Assert.Contains("Command=?", executing);
        Assert.DoesNotContain("\"Status\"", executing);
    }

    [Fact]
    public void Update_logs_its_set_document()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(nameof(Update_logs_its_set_document));
        collection.InsertOne(new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 });

        using var db = SingleEntityDbContext.Create(
            collection, loggerFactory: loggerFactory, sensitiveDataLogging: true);

        db.Entities.Where(o => o.Status == "open").ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));

        var executing = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkUpdate);
        Assert.Contains("$set", executing);
        Assert.Contains("\"Status\" : \"shipped\"", executing);
    }
}

#endif
```

If `SingleEntityDbContext.Create` has no `sensitiveDataLogging` parameter, add one that calls `optionsBuilder.EnableSensitiveDataLogging()` — follow the existing parameter style in `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/SingleEntityDbContext.cs` and default it to `false` so no existing caller changes.

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsCommandTests"
```

Expected: the two sensitive-logging tests FAIL (no filter or `$set` text in the message); `Command_is_redacted_when_sensitive_logging_disabled` PASSES already because Task 4 hardcoded `"?"`.

- [ ] **Step 3: Add the renderer**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, next to `GetUpdateLogger`:

```csharp
    // Renders the command for diagnostics. Command content includes predicate values and the values being
    // written, so it is gated on ShouldLogSensitiveData() exactly like MQL is on the read path — see
    // Diagnostics/AGENTS.md ("Redaction is single-pointed").
    private static string RenderBulkCommand(
        IDiagnosticsLogger<DbLoggerCategory.Update> logger,
        FilterDefinition<BsonDocument> filter,
        UpdateDefinition<BsonDocument>? update)
    {
        if (!logger.ShouldLogSensitiveData())
        {
            return "?";
        }

        var registry = BsonSerializer.SerializerRegistry;
        var documentSerializer = registry.GetSerializer<BsonDocument>();
        var rendered = new BsonDocument("filter", filter.Render(new RenderArgs<BsonDocument>(documentSerializer, registry)));

        if (update != null)
        {
            rendered.Add("update", update.Render(new RenderArgs<BsonDocument>(documentSerializer, registry)));
        }

        return rendered.ToJson();
    }
```

If `UpdateDefinition<BsonDocument>.Render` returns a type other than `BsonValue` on the pinned driver (pipeline-form updates render as a `BsonArray`), assign it to `BsonValue` via `rendered.Add("update", (BsonValue)update.Render(...))`; if the overload set differs, use the same `RenderArgs` overload that `BuildFilter` already uses in this file.

- [ ] **Step 4: Pass the command and a phase-spanning stopwatch through**

Change the four `*ByIds` methods to accept the stopwatch and the command from their caller rather than starting their own. `UpdateByIds` becomes:

```csharp
    private static int UpdateByIds(
        QueryContext queryContext, MongoNonQueryExpression nonQuery, List<BsonValue> ids,
        UpdateDefinition<BsonDocument> update, Stopwatch stopwatch)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, nonQuery);
        var filter = Builders<BsonDocument>.Filter.In("_id", ids);

        var updateLogger = GetUpdateLogger(queryContext);
        updateLogger.ExecutingBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, nonQuery.Strategy.ToString(), ids.Count,
            RenderBulkCommand(updateLogger, filter, update));

        var result = session == null
            ? collection.UpdateMany(filter, update)
            : collection.UpdateMany(session, filter, update);

        updateLogger.ExecutedBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, result.MatchedCount, result.ModifiedCount);
        return checked((int)result.MatchedCount);
    }
```

and its caller starts the clock before phase 1:

```csharp
    private static int ExecuteTwoPhaseUpdate<TSource>(
        QueryContext queryContext, IReadOnlyEntityType entityType, BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
        => RunInBulkTransaction(queryContext, nonQuery, () =>
        {
            // The stopwatch spans phase 1 (the read that harvests target ids) as well as phase 2, because
            // phase 1 is normally the expensive half.
            var stopwatch = Stopwatch.StartNew();
            var ids = SelectTargetIds<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery);
            if (ids.Count == 0)
            {
                return 0;
            }

            var update = TranslateBulkOrThrow(nonQuery,
                () => BuildUpdate<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery));
            return UpdateByIds(queryContext, nonQuery, ids, update, stopwatch);
        });
```

Apply the same change to `ExecuteTwoPhaseUpdateAsync`/`UpdateByIdsAsync`, `ExecuteTwoPhaseDelete`/`DeleteByIds` and `ExecuteTwoPhaseDeleteAsync`/`DeleteByIdsAsync` (deletes pass `update: null` to `RenderBulkCommand`).

In the four single-command executors, replace the `"?"` placeholder from Task 4 with `RenderBulkCommand(updateLogger, filter, update)` (or `null` for the update argument in the delete executors).

- [ ] **Step 5: Run the new and existing tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsCommandTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~BulkOperationsCommandTests"
```

Expected: PASS. If an assertion fails only on BSON spacing (`"Status" : "open"` vs `"Status": "open"`), relax that assertion to `Assert.Contains("Status", executing)` plus `Assert.Contains("open", executing)` rather than guessing at `ToJson` formatting.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/
git commit -m "feat: log the bulk command behind the sensitive-data gate; time both two-phase phases"
```

---

