### Task 4: Reshape the bulk event payload and collapse the duplicate event definitions

> ### AMENDED FOR `main` (2026-07-30) — read this before the steps below
>
> Two of this task's four original findings are **already fixed** in `main` and are dropped: `TargetCount` is already `long?`, and the all-zero-counts payload mismatch was solved by splitting `MongoBulkWriteEventData` into per-operation types. The step bodies below were written against the old shared `MongoBulkWriteEventData` and **do not match the code**. Use the step bodies for their *intent and message wording*; use this block for the actual types and signatures.
>
> **Actual state in `main`:**
> - `Diagnostics/MongoBulkDeleteEventData.cs` — `MongoBulkDeleteEventData(EventDefinitionBase, Func<EventDefinitionBase, EventData, string>, TimeSpan elapsed, CollectionNamespace collectionNamespace, long deleteCount, long? targetCount)`, properties `Elapsed`, `CollectionNamespace`, `DeleteCount`, `TargetCount`.
> - `Diagnostics/MongoBulkUpdateEventData.cs` — same shape with `long modifyCount` / `ModifyCount`.
> - `Diagnostics/MongoLoggerUpdateExtensions.cs` aliases: `BulkExecutingEventDefinition = EventDefinition<string, CollectionNamespace>`, `BulkExecutingTwoPhaseEventDefinition = EventDefinition<string, CollectionNamespace, long>`, `BulkExecutedEventDefinition = EventDefinition<string, CollectionNamespace, long>`.
> - `Diagnostics/MongoLoggingDefinitions.cs` still declares six bulk fields including `LogExecutingBulkDeleteTwoPhase` and `LogExecutingBulkUpdateTwoPhase`.
> - Call sites are in `Storage/MongoBulkOperationExecutor.cs`, not the query visitor.
>
> **Remaining scope — exactly two changes:**
>
> **(A) One `EventDefinition` per `EventId`.** Today `LogExecutingBulkDelete` and `LogExecutingBulkDeleteTwoPhase` are two definitions of different arity registered under the single id `MongoEventId.ExecutingBulkDelete` (same for update), so a `DiagnosticSource` consumer cannot tell the shapes apart and has to null-check `TargetCount` to find out which it received. Collapse each pair into one definition:
> - Replace the two `Executing` aliases with one: `BulkExecutingEventDefinition = EventDefinition<string, CollectionNamespace, string, string>` — elapsed, namespace, strategy, target count.
> - Delete `LogExecutingBulkDeleteTwoPhase` and `LogExecutingBulkUpdateTwoPhase` from `MongoLoggingDefinitions.cs`, leaving four bulk fields.
> - Delete `ExecutingBulkDeleteTwoPhaseMessage` and `ExecutingBulkUpdateTwoPhaseMessage`; the single message generator handles both.
> - Message text: `"Executing Bulk Delete ({elapsed} ms) Collection='{collectionNamespace}', Strategy={strategy}, Targets={targetCount}"` (and the `Update` equivalent).
> - `strategy` is `"SingleCommand"` or `"TwoPhase"`, from `plan.Strategy.ToString()`. `targetCount` renders as the number for two-phase and `"-"` for single-command, via a shared `private static string FormatTargetCount(long? targetCount) => targetCount?.ToString(CultureInfo.InvariantCulture) ?? "-";`.
> - Public signatures become `ExecutingBulkDelete(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, string strategy, long? targetCount)` and the `Update` equivalent — one method each, no overload pair.
>
> **(B) Carry the matched count on update.** `ExecutedBulkUpdate` is called with `result.ModifiedCount` while the executor returns `result.MatchedCount`, so the event cannot be reconciled with the value `ExecuteUpdate` gives the caller.
> - Add `long matchedCount` to `MongoBulkUpdateEventData`'s constructor (after `modifyCount`) and a `public long MatchedCount { get; }` property, documented as "The number of documents matched — the value returned to the caller; `ModifyCount` is the genuinely-modified subset."
> - Add alias `BulkExecutedUpdateEventDefinition = EventDefinition<string, CollectionNamespace, long, long>`; `LogExecutedBulkUpdate` uses it with message `"Executed Bulk Update ({elapsed} ms) Collection='{collectionNamespace}', Matched={matched}, Modified={modified}"`.
> - Signature becomes `ExecutedBulkUpdate(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, long matchedCount, long modifyCount)`.
> - `ExecutedBulkDelete` and `MongoBulkDeleteEventData` keep their arity — a delete has no matched-vs-modified distinction.
>
> **Not in this task:** `Command` and `LogSensitiveData` on the event data, and the phase-spanning stopwatch. Those are Task 5.
>
> **Also apply:** replace `duration.TotalMilliseconds.ToString()` with `ToString(CultureInfo.InvariantCulture)` at every bulk call site you touch, adding `using System.Globalization;` if absent. Leave the pre-existing non-bulk `ExecutingBulkWrite`/`ExecutedBulkWrite` alone.

Two findings in one change, because they touch the same public types and the same call sites and cannot land independently without churning them twice: `ExecutedBulkUpdate` reports only `ModifiedCount` so the event cannot be reconciled with the value `ExecuteUpdate` returns; and two `EventDefinition`s of different arity share one `EventId`, so a `DiagnosticSource` consumer cannot tell the two message shapes apart.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkWriteEventData.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggingDefinitions.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerUpdateExtensions.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces, and relied on by Tasks 5, 6, 22 and 24:
  - `MongoBulkWriteEventData` gains `long? TargetCount` (was `int?`), `long MatchedCount`, `string? Command`, `bool LogSensitiveData`.
  - New constructor:
    `MongoBulkWriteEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, TimeSpan elapsed, CollectionNamespace collectionNamespace, long insertCount, long deleteCount, long modifyCount, long matchedCount, long? targetCount, string? command, bool logSensitiveData)`
  - Logger extension signatures become:
    - `ExecutingBulkDelete(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, string strategy, long? targetCount, string command)`
    - `ExecutedBulkDelete(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, long deleteCount)`
    - `ExecutingBulkUpdate(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, string strategy, long? targetCount, string command)`
    - `ExecutedBulkUpdate(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, long matchedCount, long modifyCount)`
  - `MongoLoggingDefinitions` keeps exactly one field per bulk event: `LogExecutingBulkDelete`, `LogExecutedBulkDelete`, `LogExecutingBulkUpdate`, `LogExecutedBulkUpdate`. The `*TwoPhase` fields are deleted.
  - The `command` argument is supplied already-redacted by the caller in Task 5. In this task callers pass `"?"`.
  - Strategy strings are exactly `"SingleCommand"` and `"TwoPhase"`, produced by `nonQuery.Strategy.ToString()`.

- [ ] **Step 1: Write the failing test that pins matched-vs-modified in the event**

In `ExecuteUpdateTests.cs`, add:

```csharp
    [Fact]
    public void ExecuteUpdate_event_reports_both_matched_and_modified_counts()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(
            nameof(ExecuteUpdate_event_reports_both_matched_and_modified_counts));
        collection.InsertMany(
        [
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 },
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 }
        ]);

        using var db = SingleEntityDbContext.Create(collection, loggerFactory: loggerFactory);

        // Both documents match; only the one whose Status differs is actually modified.
        var updated = db.Entities.Where(o => o.Status == "open").ExecuteUpdate(s => s.SetProperty(o => o.Status, "open"));

        Assert.Equal(2, updated);

        var executed = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedBulkUpdate);
        Assert.Contains("Matched=2", executed);
        Assert.Contains("Modified=0", executed);
    }
```

Match the `SingleEntityDbContext.Create(...)` overload already used by `ExecuteUpdate_logs_ExecutedBulkUpdate_event_with_count` in this file — copy its argument list rather than inventing one.

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdate_event_reports_both_matched_and_modified_counts"
```

Expected: FAIL — the current message is `"… Modified={modified}"` with no `Matched=` segment.

- [ ] **Step 3: Widen the event data**

In `MongoBulkWriteEventData.cs`, keep both existing constructors and have them chain into a new one. Replace the second constructor and the `TargetCount` property with:

```csharp
    /// <summary>
    /// Constructs the event payload for a bulk operation, including two-phase and command detail.
    /// </summary>
    /// <param name="eventDefinition">The event definition.</param>
    /// <param name="messageGenerator">A delegate that generates a log message for this event.</param>
    /// <param name="elapsed">The time elapsed since the operation began, spanning every phase.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> being operated upon.</param>
    /// <param name="insertCount">The number of documents to insert in this bulk write operation.</param>
    /// <param name="deleteCount">The number of documents deleted by this bulk write operation.</param>
    /// <param name="modifyCount">The number of documents whose stored values actually changed.</param>
    /// <param name="matchedCount">
    /// The number of documents matched by the operation's filter. This is the value
    /// <c>ExecuteUpdate</c> returns; <paramref name="modifyCount"/> is the genuinely-modified subset.
    /// </param>
    /// <param name="targetCount">
    /// For a two-phase bulk delete/update — the number of documents identified in phase one to be acted on
    /// by <c>_id</c>; <see langword="null" /> for single-command bulk writes.
    /// </param>
    /// <param name="command">
    /// The rendered MongoDB command (filter, and update document for an update), or <c>"?"</c> when
    /// sensitive-data logging is disabled.
    /// </param>
    /// <param name="logSensitiveData">Indicates whether the application allows logging of sensitive data.</param>
    public MongoBulkWriteEventData(
        EventDefinitionBase eventDefinition,
        Func<EventDefinitionBase, EventData, string> messageGenerator,
        TimeSpan elapsed,
        CollectionNamespace collectionNamespace,
        long insertCount,
        long deleteCount,
        long modifyCount,
        long matchedCount,
        long? targetCount,
        string? command,
        bool logSensitiveData)
        : base(eventDefinition, messageGenerator)
    {
        Elapsed = elapsed;
        CollectionNamespace = collectionNamespace;
        InsertCount = insertCount;
        DeleteCount = deleteCount;
        ModifyCount = modifyCount;
        MatchedCount = matchedCount;
        TargetCount = targetCount;
        Command = command;
        LogSensitiveData = logSensitiveData;
    }

    /// <summary>
    /// The number of documents matched by this operation's filter. For a bulk update this is the value
    /// returned to the caller; <see cref="ModifyCount"/> is the subset whose stored values changed.
    /// </summary>
    public long MatchedCount { get; }

    /// <summary>
    /// For a two-phase bulk delete/update — the number of documents identified in phase one to be acted on
    /// by <c>_id</c>; <see langword="null" /> for single-command bulk writes.
    /// </summary>
    public long? TargetCount { get; }

    /// <summary>
    /// The rendered MongoDB command for this operation, or <c>"?"</c> when sensitive-data logging is disabled.
    /// </summary>
    public string? Command { get; }

    /// <summary>
    /// Indicates whether the application allows logging of sensitive data.
    /// </summary>
    public bool LogSensitiveData { get; }
```

Update both pre-existing constructors to chain with the new trailing arguments — the three-count `SaveChanges` constructor becomes:

```csharp
        : this(eventDefinition, messageGenerator, elapsed, collectionNamespace, insertCount, deleteCount, modifyCount,
            matchedCount: 0, targetCount: null, command: null, logSensitiveData: false)
```

and delete the intermediate `int? targetCount` constructor added by the original PR — nothing outside this assembly can be depending on it yet.

- [ ] **Step 4: Collapse to one definition per event and add the new message fields**

In `MongoLoggingDefinitions.cs`, delete `LogExecutingBulkDeleteTwoPhase` and `LogExecutingBulkUpdateTwoPhase`, leaving four fields.

In `MongoLoggerUpdateExtensions.cs`, replace the three `using` aliases added by the PR with two:

```csharp
#if !EF8
using BulkExecutingEventDefinition = EventDefinition<string, CollectionNamespace, string, string, string>;
using BulkExecutedDeleteEventDefinition = EventDefinition<string, CollectionNamespace, long>;
using BulkExecutedUpdateEventDefinition = EventDefinition<string, CollectionNamespace, long, long>;
#endif
```

Replace `ExecutingBulkDelete` and its two definition factories and two message generators with a single set:

```csharp
    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutingBulkDelete" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The time elapsed since the operation began, spanning every phase.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="strategy">Either <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="targetCount">
    /// For a two-phase operation, the number of ids collected in phase one; <see langword="null"/> otherwise.
    /// </param>
    /// <param name="command">The rendered command, already redacted to <c>"?"</c> when not permitted.</param>
    public static void ExecutingBulkDelete(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        string strategy,
        long? targetCount,
        string command)
    {
        var definition = LogExecutingBulkDelete(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics, duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture), collectionNamespace,
                strategy, FormatTargetCount(targetCount), command);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkWriteEventData(
                definition,
                ExecutingBulkDeleteMessage,
                duration,
                collectionNamespace,
                insertCount: 0,
                deleteCount: 0,
                modifyCount: 0,
                matchedCount: 0,
                targetCount: targetCount,
                command: command,
                logSensitiveData: diagnostics.ShouldLogSensitiveData());

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string FormatTargetCount(long? targetCount)
        => targetCount?.ToString(CultureInfo.InvariantCulture) ?? "-";

    private static string ExecutingBulkDeleteMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkExecutingEventDefinition)definition;
        var p = (MongoBulkWriteEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture), p.CollectionNamespace,
            p.TargetCount.HasValue ? "TwoPhase" : "SingleCommand", FormatTargetCount(p.TargetCount),
            p.Command ?? "?");
    }

    private static BulkExecutingEventDefinition LogExecutingBulkDelete(IDiagnosticsLogger logger)
        => (BulkExecutingEventDefinition)
            (((MongoLoggingDefinitions)logger.Definitions).LogExecutingBulkDelete ?? NonCapturingLazyInitializer.EnsureInitialized(
                ref ((MongoLoggingDefinitions)logger.Definitions).LogExecutingBulkDelete,
                logger,
                static logger => new BulkExecutingEventDefinition(
                    logger.Options,
                    MongoEventId.ExecutingBulkDelete,
                    LogLevel.Information,
                    "MongoEventId.ExecutingBulkDelete",
                    level => LoggerMessage.Define<string, CollectionNamespace, string, string, string>(
                        level,
                        MongoEventId.ExecutingBulkDelete,
                        "Executing Bulk Delete ({elapsed} ms) Collection='{collectionNamespace}', "
                        + "Strategy={strategy}, Targets={targetCount}, Command={command}"))));
```

Apply the same shape to `ExecutingBulkUpdate` (message text `"Executing Bulk Update …"`, definition field `LogExecutingBulkUpdate`, event id `MongoEventId.ExecutingBulkUpdate`, message generator `ExecutingBulkUpdateMessage`).

Change `ExecutedBulkUpdate` to carry both counts:

```csharp
    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutedBulkUpdate" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The time elapsed since the operation began, spanning every phase.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="matchedCount">The number of documents matched — the value returned to the caller.</param>
    /// <param name="modifyCount">The number of documents whose stored values actually changed.</param>
    public static void ExecutedBulkUpdate(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        long matchedCount,
        long modifyCount)
    {
        var definition = LogExecutedBulkUpdate(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics, duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture), collectionNamespace,
                matchedCount, modifyCount);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkWriteEventData(
                definition,
                ExecutedBulkUpdateMessage,
                duration,
                collectionNamespace,
                insertCount: 0,
                deleteCount: 0,
                modifyCount: modifyCount,
                matchedCount: matchedCount,
                targetCount: null,
                command: null,
                logSensitiveData: diagnostics.ShouldLogSensitiveData());

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string ExecutedBulkUpdateMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkExecutedUpdateEventDefinition)definition;
        var p = (MongoBulkWriteEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture), p.CollectionNamespace,
            p.MatchedCount, p.ModifyCount);
    }
```

with the definition's message text `"Executed Bulk Update ({elapsed} ms) Collection='{collectionNamespace}', Matched={matched}, Modified={modified}"` and `LoggerMessage.Define<string, CollectionNamespace, long, long>`.

`ExecutedBulkDelete` keeps its arity; only change `duration.TotalMilliseconds.ToString()` to `ToString(CultureInfo.InvariantCulture)` and pass `matchedCount: deleteCount` into its event data.

Add `using System.Globalization;` to the file.

- [ ] **Step 5: Update the six call sites**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, every `ExecutingBulk*` call gains the strategy, target count and a placeholder command (Task 5 replaces the placeholder with the real one), and every `ExecutedBulkUpdate` call gains the matched count. For example, in `UpdateByIds`:

```csharp
        updateLogger.ExecutingBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, nonQuery.Strategy.ToString(), ids.Count, "?");

        var result = session == null
            ? collection.UpdateMany(filter, update)
            : collection.UpdateMany(session, filter, update);

        updateLogger.ExecutedBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, result.MatchedCount, result.ModifiedCount);
```

and in the single-command `ExecuteUpdate`:

```csharp
        updateLogger.ExecutingBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, nonQuery.Strategy.ToString(), targetCount: null, "?");
```

Apply the equivalent edits to `UpdateByIdsAsync`, `DeleteByIds`, `DeleteByIdsAsync`, `ExecuteDelete`, `ExecuteDeleteAsync`, `ExecuteUpdate` and `ExecuteUpdateAsync`.

- [ ] **Step 6: Fix the existing diagnostics tests' expectations**

The pre-existing tests assert on message substrings that have changed shape. Update `ExecuteDelete_two_phase_logs_ExecutingBulkDelete_with_target_count` and `ExecuteUpdate_two_phase_logs_ExecutingBulkUpdate_with_target_count` to assert `Assert.Contains("Strategy=TwoPhase", executingMessage)` and `Assert.Contains("Targets=2", executingMessage)` (substituting the target count the test seeds), and update `ExecuteUpdate_logs_ExecutedBulkUpdate_event_with_count` to expect `Matched=` alongside `Modified=`.

- [ ] **Step 7: Run the bulk suites on EF9 and EF10**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```

Expected: PASS on EF9/EF10 including the new matched/modified test; EF8 builds.

- [ ] **Step 8: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Diagnostics/ \
        src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/
git commit -m "feat: report matched and modified counts, widen TargetCount to long?, one definition per bulk event"
```

---

