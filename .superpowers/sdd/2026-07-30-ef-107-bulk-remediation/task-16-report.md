# Task 16 report — Project `{_id: 1}` in phase 1

**Status: DONE**
**Commit: `4f2f29f` — perf: project only _id in bulk phase one and log the phase-one pipeline**

## What I implemented

1. **`BuildIdDocumentQuery` (Query) appends `{ $project: { _id: 1 } }`** to the translated phase-1 pipeline,
   as the last stage:

   ```csharp
   var documents = executableQuery.Provider.CreateQuery<BsonDocument>(executableQuery.Query);
   return documents.AppendStage(
       new BsonDocumentPipelineStageDefinition<BsonDocument, BsonDocument>(
           new BsonDocument("$project", new BsonDocument("_id", 1))));
   ```

   Per Correction 4: two generic parameters, a static extension on `IQueryable<TSource>` (no `IMongoQueryable`
   cast), and a real `PipelineStageDefinition<TSource, TResult>`. The fallback ladder was not needed — the
   first option worked and is confirmed on the wire.

   A fresh stage `BsonDocument` is built per call rather than a `static readonly`: the driver's
   `AstUniversalStage.Render()` returns the very instance it was handed, and that instance then travels into the
   rendered pipeline, the `aggregate` command, and `IMongoQueryProvider.LoggedStages` — a shared static would be
   aliased into every execution's rendered output. That reasoning is in the code comment.

2. **The phase-1 pipeline is surfaced in the `Executing` event's `Command`**, composed inside the *existing*
   `RenderBulkCommand` gate (Correction 2). The four two-phase executors now hoist the target-id query into a
   local and pass it to `DeleteByIds`/`DeleteByIdsAsync`/`UpdateByIds`/`UpdateByIdsAsync`, which forward it to
   `RenderBulkCommand(updateLogger, filter, update, targetIdQuery)` — still inside the `() => …` thunk.

3. **XML-doc truthfulness:** `MongoBulkDeleteEventData.Command`, `MongoBulkUpdateEventData.Command` (ctor param
   + property) and the two `renderCommand` param docs now describe the composite shape. Doc comments only — no
   signature, event-ID, or payload changes. `MongoEventId` untouched; `Strategy` still a string.

### Files changed

- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`
- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkDeleteEventData.cs` (doc only)
- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkUpdateEventData.cs` (doc only)
- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerUpdateExtensions.cs` (doc only)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs`

No code moved across the Query↔Storage seam: translation (the `AppendStage`) stayed in Query, execution and
diagnostics rendering stayed in Storage. BOMs verified byte-for-byte unchanged on all six files (the test file
has no BOM and still has none).

## Evidence the projection landed: rendered phase-1 pipeline before / after

Captured on the wire with a `CommandStartedEvent` subscriber (the technique
`BulkOperationsInheritanceTests` uses), for `db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDelete()`,
via a throwaway probe test that was deleted before committing:

- **Before:** `[{ "$sort" : { "Quantity" : 1 } }, { "$limit" : 1 }]`
- **After:** `[{ "$sort" : { "Quantity" : 1 } }, { "$limit" : 1 }, { "$project" : { "_id" : 1 } }]`

So the stage reaches the server, and it is **last** — after `$sort`/`$limit`, which is why it cannot change
which documents phase 1 selects.

## What `ToString()` returned, and the composite format I chose

The parent's suspicion was right. `MongoQuery<TDocument,TOutput>.ToString()` → `ExecutableQuery.ToString()` →
`$"{CollectionNamespace}.Aggregate({pipeline})"`. Measured, for the same query shape:

```
EFTest-2026-07-31T19-14-27-1.Probe.Aggregate([{ "$sort" : { "Quantity" : 1 } }, { "$limit" : 1 }, { "$project" : { "_id" : 1 } }])
```

That is **not JSON**, so the brief's `{ "phase1": <pipeline>, "phase2": … }` wrapper would have emitted
malformed JSON claiming to be structured. I did not use `ToString()` at all.

Instead the phase-1 pipeline is read from **`IMongoQueryProvider.LoggedStages`** — the `BsonDocument[]` the
driver rendered and handed to the `aggregate` command (`ExecutableQuery.Execute` does
`_loggedStages = RenderPipeline()` and builds the pipeline definition from that same array). This is the same
source the read path's MQL logging already uses (`MongoLoggerExtensions.ExecutedMqlQuery` →
`mongoExecutableQuery.Provider.LoggedStages`), so it is both precedented in this codebase and *stronger*
evidence than a re-translation: it reports the pipeline that actually ran.

Because it is real BSON, the whole `Command` value stays **one valid JSON document**, consistent with how
`Command=` is rendered today (`BsonDocument.ToJson()`), with phase 1 first because it runs first:

```json
{ "phase1Pipeline" : [{ "$sort" : { "Quantity" : 1 } }, { "$limit" : 1 }, { "$project" : { "_id" : 1 } }],
  "filter" : { "_id" : { "$in" : [...] } },
  "update" : { "$set" : { ... } } }
```

Single-command operations are unchanged (`{ "filter" : … }`, plus `"update"`), since `targetIdQuery` defaults
to `null`.

`LoggedStages` is populated only after the query has executed, which it always has at these call sites (phase 2
runs only after phase 1 materialized ≥1 id — confirmed for both sync `Execute` and async `ExecuteAsync`, which
each set `_executedQuery`). If it were ever null — or the provider were not the driver's — the value is reported
as JSON `null` rather than throwing or as an empty stage array (which would read as "phase 1 sent no stages").

## How the single redaction gate and the laziness were kept

**Gate.** `RenderPhaseOnePipeline` contains no `ShouldLogSensitiveData()` check; it is a private helper called
only from inside `RenderBulkCommand`, *after* that method's one early `return "?"`. Exactly one place decides
whether MQL is disclosed, per `Diagnostics/AGENTS.md` ("Redaction is single-pointed"). A welcome consequence:
the eight existing `*_command_is_gated_by_sensitive_data_logging` assertions (`Assert.Equal("?", closed.Command)`)
now cover the phase-1 pipeline for free — **no redaction assertion was changed or weakened**.

**Laziness.** All eight call sites still pass `() => RenderBulkCommand(...)`; nothing is composed eagerly in the
argument position. The phase-1 rendering happens inside that thunk, so an operation with no `ILogger` and no
`DiagnosticSource` subscriber pays nothing — as before, and now for one more thing.

I also pinned that laziness, which Task 5's ledger notes was untested
(`Executing_events_do_not_render_the_command_when_nobody_is_listening`): it drives
`ExecutingBulkDelete`/`ExecutingBulkUpdate` directly, off a context with no logger factory and no diagnostic
subscriber, with a thunk that counts its own invocations, and asserts 0. **Verified it has teeth:** temporarily
hoisting `var command = renderCommand();` above the `if (!shouldLog && !needsEventData) return;` in
`MongoLoggerUpdateExtensions` (both methods) makes it fail with `Assert.Equal() Failure: Values differ`; the
hoist was reverted with `git checkout`.

## TDD evidence

### RED

```
$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
    --filter "FullyQualifiedName~Two_phase_delete_projects_only_the_id_in_phase_one"

  Failed MongoDB.EntityFrameworkCore.FunctionalTests.Query.BulkOperationsCommandTests.Two_phase_delete_projects_only_the_id_in_phase_one [263 ms]
  Error Message:
   Assert.Contains() Failure: Sub-string not found
String:    "Executing Bulk Delete (51.9111 ms) Collec"···
Not found: "$project"

Failed!  - Failed: 1, Passed: 0, Skipped: 0, Total: 1
```

Expected for exactly that reason: no projection was emitted and phase 1 was not represented in `Command` at all.

### GREEN

```
$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
    --filter "FullyQualifiedName~Two_phase_delete_projects_only_the_id_in_phase_one"

Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1 - Duration: 268 ms
```

## Tests

| Run | Result |
|---|---|
| Builds: `Debug EF8`, `Debug EF9`, `Debug EF10` | all succeed, 0 warnings, 0 errors |
| EF10 `BulkOperations*` + `ExecuteDeleteTests` + `ExecuteUpdateTests` (brief Step 5, widened) | **86 passed, 1 skipped** (the pre-existing standalone-deployment skip) |
| EF9 same filter | **87 passed, 1 skipped** |
| EF8 same filter (`BulkOperationsUnsupportedOnEf8Tests`) | **2 passed** |
| EF10 `BulkOperationsCommandTests` | **15 passed** |
| EF9 `BulkOperationsCommandTests` | **15 passed** |
| EF10 `NorthwindBulkUpdates` spec | **181/181** (matches baseline) |
| EF9 `NorthwindBulkUpdates` spec | **169/169** (matches baseline) |
| EF10 whole Northwind spec suite | 4385 passed, 0 failed, 23 skipped |
| EF10 **whole FunctionalTests suite** | 1797 passed, 0 failed, 52 skipped (encryption etc.) |

Both `MONGODB_URI` and `ATLAS_URI` were unset throughout (TestContainers atlas-local). The inheritance /
wire-capture tests (`ExecuteDelete_with_OfType_deletes_only_that_type`,
`ExecuteDelete_with_OfType_phase1_query_carries_the_discriminator_filter_on_the_wire`) pass unchanged — the
appended-last position preserves `Assert.Single(aggregateCommands)` and the `_t` filter.

### New/changed tests

- `Two_phase_delete_projects_only_the_id_in_phase_one` (new). Beyond the brief's substring checks it parses the
  `Command` and asserts `phase1Pipeline`'s **last** element equals exactly `{ "$project" : { "_id" : 1 } }` —
  which pins the composite value is well-formed JSON, the projection is `{_id: 1}`, and the stage is last.
  Regression-sensitive: proven by the RED run; drop the projection and it fails, move it earlier and the
  `Last()` assertion fails.
- `Executing_events_do_not_render_the_command_when_nobody_is_listening` (new). Teeth verified above.
- `TwoPhaseDelete_/TwoPhaseUpdate_command_is_gated_by_sensitive_data_logging`: added
  `Assert.Contains("$project", open.Command)` to the open-gate half. This is what covers the **async** path —
  `LoggedStages` is populated by `ExecuteAsync` separately from `Execute`, and my parsing test is sync-only. It
  fails if `phase1Pipeline` comes back `null`. Their closed-gate assertions were **not** touched.

Test output is pristine: no new skips, no console noise, no leftover probe file (the throwaway
`TempPhaseOnePipelineProbe.cs` used for the before/after wire capture was deleted before commit; `git status`
is clean).

## Self-review findings

- **Completeness.** All four two-phase sites (delete/update × sync/async) carry the pipeline; single-command
  sites unchanged by default parameter. Comment at the old "future optimization" TODO replaced, not left stale.
- **Correction 5 honoured.** The comment claims only what is true: fewer bytes on the wire, less client-side
  BSON parsing. It states explicitly that this is *not* generally a covered index scan, and gives the accurate
  precondition (`_id` part of the index key, or a clustered-on-`_id` collection); otherwise the server still
  fetches each candidate document. No false performance claim anywhere in the diff.
- **Naming.** `targetIdQuery` matches the plan's `BuildTargetIdQuery`; `phase1Pipeline` names what the value is
  (a pipeline), not a vague "phase1".
- **YAGNI.** I did *not* add the brief's `PhaseOneResult` record or change `MaterializeIds`' signature — the
  target-id query is already in scope at all four call sites, so hoisting it into a local was enough. No new
  type, no new event ID, no payload field.
- **Comments checked line by line for truth,** including the two driver-behaviour claims (`AstUniversalStage.Render`
  returning the same instance; `LoggedStages` being the array the `aggregate` command carries) — both verified in
  the pinned driver source at `~/code/driver2`.

## Concerns

1. **`LoggedStages` is post-execution provider state.** It is the read path's own precedent and correct here,
   but it couples the rendered `Command` to the fact that phase 2 logs *after* phase 1 executed. If a future
   change ever logged `Executing` before materializing the ids, `phase1Pipeline` would silently become JSON
   `null` rather than failing loudly. The comment says so; the null branch itself is not covered by a test (I
   could not construct it without contriving a fake provider, which felt like more machinery than the branch
   deserves).
2. **`Command`'s two-phase shape changed** (a `phase1Pipeline` key was added ahead of `filter`). It is
   diagnostics-only, and bulk operations are unreleased on this branch, so no `BREAKING-CHANGES.md` entry —
   flagging it in case you disagree about the unreleased judgement.
3. **The projection hard-codes `_id`**, matching what `MaterializeIds` reads. If phase 1 ever needs a second
   field (e.g. a concurrency token to re-check in phase 2), the projection must grow with it. Nothing enforces
   that pairing beyond the comment.
4. **Three Diagnostics files are in the commit** even though the task named Query + Storage. They are XML doc
   comments only (no code), correcting descriptions that my change would otherwise have made incomplete. Easy to
   drop if you'd rather keep the commit to two files.
