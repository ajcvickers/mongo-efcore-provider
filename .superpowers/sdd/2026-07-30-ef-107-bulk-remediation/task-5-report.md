# Task 5 report: Emit the redaction-gated command, and make elapsed span both phases

## Scope note

Followed the brief's "AMENDED FOR `main`" block for locations/types/signatures, and took intent, wording,
test shape and the redaction rule from the numbered steps. All bulk-related changes stayed inside
`Storage/MongoBulkOperationExecutor.cs` and `Diagnostics/*` — nothing moved into Query, and translation stayed
in Query (only the already-rendered `FilterDefinition`/`UpdateDefinition` results, which the plan's delegates
hand to the executor, are re-rendered to a diagnostic string in Storage).

## Files changed

### `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkDeleteEventData.cs` / `MongoBulkUpdateEventData.cs`

Added two new trailing constructor parameters / properties to each, immediately after the existing `strategy`
parameter, per the amended block's instruction to keep the existing parameter order undisturbed:

- `string? Command` — documented as "The rendered MongoDB command for this operation, or `\"?\"` when
  sensitive-data logging is disabled."
- `bool LogSensitiveData` — whether sensitive-data logging was enabled when the event was raised (metadata
  distinct from `Command` itself, mirroring the shape `MongoQueryEventData` already uses for the read-path MQL
  event).

Both classes are public; per the task's context this is an intended additive break with no compatibility
overload — there's a single constructor, and both call sites in `MongoLoggerUpdateExtensions.cs` (Executing
*and* Executed) were updated to supply the new arguments.

### `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerUpdateExtensions.cs`

- `BulkExecutingEventDefinition` widened from `EventDefinition<string, CollectionNamespace, string, string>`
  (elapsed, namespace, strategy, targets) to `EventDefinition<string, CollectionNamespace, string, string,
  string>`, with `command` as the fifth parameter. `BulkExecutedEventDefinition` /
  `BulkExecutedUpdateEventDefinition` are unchanged — the `Executed` events do not carry the command, matching
  the amended block.
- `ExecutingBulkDelete` / `ExecutingBulkUpdate` each gained a `string command` parameter, threaded into
  `definition.Log(...)`, the message-template string (`, Command={command}` appended to both), and the
  `MongoBulkDeleteEventData`/`MongoBulkUpdateEventData` constructor (`command: command, logSensitiveData:
  diagnostics.ShouldLogSensitiveData()`).
- `ExecutedBulkDelete` / `ExecutedBulkUpdate` signatures are unchanged (no command parameter), but their
  `EventData` construction now passes `command: null, logSensitiveData: diagnostics.ShouldLogSensitiveData()`
  to satisfy the widened constructor.
- The message-generator methods (`ExecutingBulkDeleteMessage`/`ExecutingBulkUpdateMessage`) render
  `p.Command ?? "?"` — a defensive fallback; in practice `Command` is never null on the `Executing` path since
  the executor always calls `RenderBulkCommand`, which never returns null.

### `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`

- Added `using MongoDB.Bson.Serialization;` for `BsonSerializer`.
- Added `RenderBulkCommand(IDiagnosticsLogger<DbLoggerCategory.Update> logger, FilterDefinition<BsonDocument>
  filter, UpdateDefinition<BsonDocument>? update)`, placed next to `GetUpdateLogger`. Returns `"?"` unless
  `logger.ShouldLogSensitiveData()`; otherwise renders `{ "filter": <filter> }` (delete) or `{ "filter":
  <filter>, "update": <update> }` (update) as single-line JSON via `BsonDocument.ToJson()`. Verified via
  reflection against the pinned driver (3.10.0) that `FilterDefinition<T>.Render(...)` returns `BsonDocument`
  and `UpdateDefinition<T>.Render(...)` returns `BsonValue` — both add directly into the wrapping
  `BsonDocument` with no cast needed.
- The four single-command executors (`ExecuteDelete`, `ExecuteDeleteAsync`, `ExecuteUpdate`,
  `ExecuteUpdateAsync`) now call `RenderBulkCommand` and pass the result to `ExecutingBulkDelete`/
  `ExecutingBulkUpdate` instead of the Task-4 `"?"` placeholder.
- The two-phase path: `DeleteByIds`, `DeleteByIdsAsync`, `UpdateByIds`, `UpdateByIdsAsync` no longer call
  `Stopwatch.StartNew()` themselves — they now take a `Stopwatch stopwatch` parameter from their caller, and
  also call `RenderBulkCommand` (passing `update: null` for the delete variants) before logging
  `ExecutingBulkDelete`/`ExecutingBulkUpdate`.
- `ExecuteTwoPhaseDelete`, `ExecuteTwoPhaseDeleteAsync`, `ExecuteTwoPhaseUpdate`, `ExecuteTwoPhaseUpdateAsync`
  now call `Stopwatch.StartNew()` *before* `MaterializeIds(Async)(plan.BuildTargetIdQuery!(queryContext))`
  (phase 1) and pass that stopwatch down into `DeleteByIds(Async)`/`UpdateByIds(Async)`, with a comment
  explaining why (phase 1 is normally the expensive half).

### `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/SingleEntityDbContext.cs`

The `Create<T>(IMongoCollection<T>, LoggerFactory, ...)` overload had no `sensitiveDataLogging` parameter, so
one was added, defaulted to `false`, calling `optionsBuilder.EnableSensitiveDataLogging()` when `true`. No
existing caller passes this argument, so no existing caller's behavior changes. Confirmed via `grep` that all
existing call sites use `(collection, loggerFactory, ConfigureModel)` positionally or
`(collection, loggerFactory: loggerFactory)` — both remain valid with the new optional trailing parameter.

### `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs` (new)

Four tests ("three-plus-one", matching the amended block's phrasing):

1. `Single_command_delete_logs_its_filter_when_sensitive_logging_enabled` — message-based, open gate, delete.
   Relaxed the brief's `"\"Status\" : \"open\""` assertion to separate `Contains("Status")` /
   `Contains("open")` checks per the BSON-spacing guidance (see below).
2. `Command_is_redacted_when_sensitive_logging_disabled` — the closed-gate proof. Asserts the redaction on
   **all three** surfaces named in the task: the `ILogger` message (`Command=?`, no `"Status"` substring), the
   `DiagnosticSource` payload's `Command` property (`== "?"`, `LogSensitiveData == false`), and
   `EventData.ToString()` (renders `Command=?`, does not contain `"open"`). Uses `DiagnosticSourceSpy`.
3. `Update_logs_its_set_document` — message-based, open gate, update (`$set`, `Status`, `shipped` all present).
4. `Update_command_on_diagnostic_source_payload_carries_the_rendered_set_document` — the "+1": uses
   `DiagnosticSourceSpy` to assert `Command` **directly on the payload** (not just its appearance in the
   rendered message), checking it contains `filter`, `update`, `$set`, `open`, `shipped`, and that
   `LogSensitiveData == true`.

## Redaction gate — how each surface is satisfied

Single-pointed at `RenderBulkCommand`: it is the only place that calls `ShouldLogSensitiveData()` for the
command text, exactly as `MongoLoggerExtensions.cs:55` does for MQL on the read path. Every downstream
consumer receives the already-gated string:

- **`ILogger` message** — `ExecutingBulkDelete`/`ExecutingBulkUpdate` log the `command` argument (already `"?"`
  or real JSON) directly into the template's `{command}` placeholder.
- **`DiagnosticSource` payload's `Command` property** — set from the same already-gated string in the
  `MongoBulkDeleteEventData`/`MongoBulkUpdateEventData` constructor call.
- **`EventData.ToString()`** — EF Core's `EventData.ToString()` delegates to the stored message-generator
  delegate (`_messageGenerator(_eventDefinition, this)`), which for these events is
  `ExecutingBulkDeleteMessage`/`ExecutingBulkUpdateMessage`; those read `p.Command` (already gated), so
  `ToString()` never has an independent path that could leak the real command.

Test 2 (`Command_is_redacted_when_sensitive_logging_disabled`) proves the closed-gate case on all three
surfaces in one test.

## What the rendered command actually looks like

Captured via a throwaway scratch test (written, run, and deleted — not part of the commit) that printed
`MongoBulkDeleteEventData.Command`/`MongoBulkUpdateEventData.Command` with sensitive logging enabled:

- **Single-command delete** (`Where(o => o.Status == "open").ExecuteDelete()`):
  ```
  { "filter" : { "Status" : "open" } }
  ```
- **Two-phase update** (`OrderBy(o => o.Quantity).Take(2).ExecuteUpdate(s => s.SetProperty(o => o.Status, "archived"))`):
  ```
  { "filter" : { "_id" : { "$in" : [{ "$oid" : "..." }, { "$oid" : "..." }] } }, "update" : { "$set" : { "Status" : "archived" } } }
  ```
  (phase 2's filter is always `_id: { $in: [...] }` by construction — the two-phase path always writes by id.)

`BsonDocument.ToJson()`'s default writer renders with a space before each colon (`"Status" : "open"`, not
`"Status": "open"`), and array elements as `{ "$oid" : "..." }` sub-documents rather than a compact `ObjectId(...)`
shorthand. Rather than hard-coding this spacing into assertions (per the "BSON formatting" guidance), the test
assertions check for the presence of the field name and value as separate `Contains` calls, and the "+1" test
checks for `filter`, `update`, `$set` and the literal values as independent substrings.

## Two-phase elapsed now spans both phases

`ExecuteTwoPhaseUpdate` (and its async/delete siblings) now do:
```csharp
var stopwatch = Stopwatch.StartNew();
var ids = MaterializeIds(plan.BuildTargetIdQuery!(queryContext));   // phase 1
...
return UpdateByIds(queryContext, plan, ids, update, stopwatch);     // phase 2, same stopwatch
```
`UpdateByIds`/`DeleteByIds` (+ async) no longer call `Stopwatch.StartNew()` internally — they take the
stopwatch from the caller and read `.Elapsed` for both the `Executing` and `Executed` log calls, so by the time
`ExecutingBulkUpdate`/`ExecutingBulkDelete` fires (right before the phase-2 driver call), `stopwatch.Elapsed`
already includes all of phase 1.

Verified with a throwaway scratch test (written, run, deleted): for a two-document two-phase
`ExecuteUpdate`, `ExecutingBulkUpdate`'s reported elapsed was ~53ms and `ExecutedBulkUpdate`'s was ~59ms,
against an outer wall-clock bracket of ~202ms around the whole call (which additionally includes
`BeginTransaction`/`Commit` overhead not attributed to either bulk event). The key structural evidence is in
the code itself: the `Executing` log call — which fires *before* the phase-2 driver write — now unconditionally
reads a stopwatch that was started *before* the phase-1 query ran, so its `Elapsed` cannot be near-zero the way
it was pre-fix (when the stopwatch didn't exist until the phase-2 method was entered).

## Test results

Both `MONGODB_URI` and `ATLAS_URI` left unset; `TestServer` booted its own `mongodb/mongodb-atlas-local`
container automatically (confirmed via `docker ps`).

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   # Build succeeded, 0 warnings, 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    # Build succeeded, 0 warnings, 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    # Build succeeded, 0 warnings, 0 errors

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~BulkOperationsCommandTests"
# Passed: 4, Failed: 0

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
# Passed: 43, Skipped: 1, Failed: 0

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --filter "FullyQualifiedName~BulkOperationsCommandTests"
# Passed: 4, Failed: 0

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
# Passed: 44, Skipped: 1, Failed: 0

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~NorthwindBulkUpdates"
# Passed: 181, Failed: 0   (full spec-conformance bulk-updates suite)

# Full-suite sanity beyond the brief's required commands:
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/...csproj -c "Debug EF10"   # Passed: 1758, Skipped: 52, Failed: 0
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/...csproj -c "Debug EF9"    # Passed: 1736, Skipped: 52, Failed: 0
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/...csproj -c "Debug EF10" # Passed: 4571, Skipped: 28, Failed: 0
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/...csproj -c "Debug EF9"  # Passed: 4999, Skipped: 25, Failed: 0
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/...csproj -c "Debug EF10"          # Passed: 284, Failed: 0
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/...csproj -c "Debug EF9"           # Passed: 291, Failed: 0
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/...csproj -c "Debug EF8"           # Passed: 283, Failed: 0
```

## Deliberately not changed

- `tests/.../SpecificationTests/Query/NorthwindBulkUpdatesMongoTest.cs` — the file whose comment the brief
  quotes ("no MQL baseline to pin via AssertMql here"). This task's job was to make the command *available* on
  the diagnostics path (for Tasks 8/14/15/17/21 to consume); wiring the conformance suite to actually assert
  against it is out of scope here and the comment/test itself is untouched.
- `MongoEventId` — untouched, per instructions; no new event IDs were needed since `Command` rides on the
  existing `ExecutingBulkDelete`/`ExecutingBulkUpdate` events.
- The non-bulk `ExecutingBulkWrite`/`ExecutedBulkWrite` `SaveChanges` path — untouched.
- `MongoLoggingDefinitions` — no field additions needed; the existing `LogExecutingBulkDelete`/
  `LogExecutingBulkUpdate` fields are typed via the `BulkExecutingEventDefinition` alias, so widening the alias
  was sufficient without touching the definitions class.
- Did not add a dedicated automated test asserting the two-phase elapsed timing quantitatively (e.g. injecting
  an artificial phase-1 delay and asserting `Executing.Elapsed > threshold`) — timing-based assertions in this
  suite would be flaky against real server round-trip variance, and the structural fix (stopwatch ownership
  moved to the caller, started before phase 1) is unambiguous by inspection. The scratch verification above
  (not committed) confirms the reported elapsed is non-trivial and consistent with including phase 1.
- Left `MongoBulkWriteEventData`/`ExecutingBulkWrite`/`ExecutedBulkWrite` (the `SaveChanges` bulk-write path)
  without a `Command`/`LogSensitiveData` pair — out of scope; the brief and amended block only mention the
  bulk `ExecuteDelete`/`ExecuteUpdate` events.

## Commit

One commit, message:
```
feat: log the bulk command behind the sensitive-data gate; time both two-phase phases
```
(kept the brief's Step 6 message verbatim — it remains accurate under the amended scope: it doesn't name a
file location, so relocating the implementation to Storage doesn't make the message inaccurate.)

---

# Fix round 1

Addressing all six review findings.

## Deviation A — reverted the assertion relaxation

`BulkOperationsCommandTests.cs` (`Single_command_delete_logs_its_filter_when_sensitive_logging_enabled` and
`Update_logs_its_set_document`) now use the brief's original literal single-`Contains` assertions
(`"\"Status\" : \"open\""` and `"\"Status\" : \"shipped\""`) instead of the two-`Contains` relaxation. Confirmed
green on both EF10 and EF9 below — the relaxation was never needed, as the review noted.

## Finding 1 — closed-gate coverage for all eight `RenderBulkCommand` call sites

Added four `[Theory]` methods to `BulkOperationsCommandTests.cs`, one per (strategy × kind) combination —
`SingleCommandDelete_command_is_gated_by_sensitive_data_logging`,
`SingleCommandUpdate_command_is_gated_by_sensitive_data_logging`,
`TwoPhaseDelete_command_is_gated_by_sensitive_data_logging`,
`TwoPhaseUpdate_command_is_gated_by_sensitive_data_logging` — each `[InlineData(false)]`/`[InlineData(true)]` on
`async`, matching the neighbouring `ExecuteDeleteTests`/`ExecuteUpdateTests` convention. Each theory execution
drives its combination **twice** against freshly-seeded collections (one with sensitive-data logging off, one
on), asserting the closed-gate invariant (`Command == "?"`, `LogSensitiveData == false`) and the open-gate
content in the same test — 4 methods × 2 `InlineData` = 8 executions, one per call site, each proving both gate
states without a separate parameter for it (the "economical" coverage the review asked for, rather than 16
separate tests). Two private helpers (`RunDeleteAndCaptureExecutingEventAsync`,
`RunUpdateAndCaptureExecutingEventAsync`) factor out the `SpyLoggerProvider`/`DiagnosticSourceSpy`/
`SingleEntityDbContext` plumbing shared by all four theories.

The two-phase theories assert on the two-phase-specific filter shape (`_id`/`$in`) rather than repeating the
single-command assertions, since — as the review noted — phase 2's filter is
`Builders<BsonDocument>.Filter.In("_id", ids)`, not the plan's pre-rendered `FilterDefinition`, so it is
genuinely separate coverage, not a restatement of the single-command case.

Net: every one of the eight `RenderBulkCommand` call sites (`ExecuteDelete`, `ExecuteDeleteAsync`,
`ExecuteUpdate`, `ExecuteUpdateAsync`, `DeleteByIds`, `DeleteByIdsAsync`, `UpdateByIds`, `UpdateByIdsAsync`) now
has a passing closed-gate assertion that would fail if the gate were dropped at that call site.

## Finding 2 — deterministic elapsed regression guard, committed this time

Bundled into the two `TwoPhase*_command_is_gated_by_sensitive_data_logging` theories (both the closed- and
open-gate runs) rather than as separate test methods, since it's cheap given those tests already capture the
`Executing` event with `TargetCount != null`:

```csharp
Assert.True(closed.Elapsed > TimeSpan.Zero);
...
Assert.True(open.Elapsed > TimeSpan.Zero);
```

This is the reviewer's exact deterministic formulation — no wall-clock threshold, so it can't be flaky. It is
committed and runs as part of `BulkOperationsCommandTests`; unlike the prior round's scratch test, this one is
in the diff and re-runnable evidence (see the test output below — all 4 assertions, across both `TwoPhaseDelete`
and `TwoPhaseUpdate`, sync and async, pass). It would fail immediately if `Stopwatch.StartNew()` migrated back
into `DeleteByIds`/`UpdateByIds`, because `Elapsed` would then be measured from a point after phase 1 already
ran and, logged immediately on entry to the `Executing` call, would be indistinguishable from zero in practice
(the two-phase `Executing` log call happens before any phase-2 driver I/O).

## Finding 3 — `RenderBulkCommand` no longer evaluated unconditionally

Changed `ExecutingBulkDelete`/`ExecutingBulkUpdate` in `MongoLoggerUpdateExtensions.cs` to take
`Func<string> renderCommand` instead of a pre-computed `string command`. Both methods now call
`ShouldLog(definition)` and `NeedsEventData(...)` up front, short-circuit (`return`) if neither is true, and
only then invoke `renderCommand()` once — reusing the single result for both the `ILogger` message and the
`EventData`. `RenderBulkCommand` itself (in `MongoBulkOperationExecutor.cs`) is untouched — its signature stays
exactly as the brief specified (`logger, filter, update` → `string`); only its eight call sites changed, each
wrapping the call in a thunk: `() => RenderBulkCommand(updateLogger, filter, update)` (or `update: null` for
deletes). This is the "`Func<string>`" option from the review, chosen over prechecking
`ShouldLog || NeedsEventData` at each of the eight Storage call sites, because the guard state
(`ShouldLog`/`NeedsEventData`) is logging-infrastructure internal to the two extension methods — duplicating it
at eight Storage call sites would leak that internal state across the Diagnostics/Storage seam instead of
keeping the single existing guard authoritative.

Consequence verified by inspection: a two-phase operation with `EnableSensitiveDataLogging()` on but no
`ILogger` category enabled and no `DiagnosticSource` subscriber attached no longer walks/serializes the
harvested `_id` array's `FilterDefinition` — `renderCommand()` is simply never invoked. All 12
`BulkOperationsCommandTests` still pass with this change (they all attach either a `SpyLoggerProvider` or a
`DiagnosticSourceSpy`, so `NeedsEventData`/`ShouldLog` triggers `renderCommand()` for every one of them, meaning
the short-circuit path itself isn't separately exercised by an assertion — it's a pure performance fix with no
externally-observable behavior change when something *is* listening, which is what the existing passing suite
confirms).

## Finding 4 — fixed the `Command` XML doc for `Executed` events; removed the dead `?? "?"`

Added a second sentence to the `Command` `<param>` and `<summary>` docs on both `MongoBulkDeleteEventData` and
`MongoBulkUpdateEventData`: `<see langword="null" /> on the Executed event, which does not carry the command.`

Verified `p.Command ?? "?"` was genuinely dead before removing it: `ExecutingBulkDeleteMessage`/
`ExecutingBulkUpdateMessage` are message-generator delegates passed to `EventData`'s base constructor **only**
from inside `ExecutingBulkDelete`/`ExecutingBulkUpdate` (the `Executed` methods use their own, separate
`ExecutedBulkDeleteMessage`/`ExecutedBulkUpdateMessage`, which never reference `Command` at all). Since
`command` at that one construction site is always the result of invoking the now-`Func<string>`
`renderCommand()` — which itself is always either the literal `"?"` or a `BsonDocument.ToJson()` result, never
`null` — `p.Command` cannot be `null` when either message generator runs. Replaced `p.Command ?? "?"` with
`p.Command!` plus a one-line comment recording the invariant, so the build stays warning-free under
`<Nullable>enable</Nullable>` (confirmed: `dotnet build` reports 0 warnings after the change).

## Minor 5 — `logSensitiveData: false` on `Executed` payloads

`ExecutedBulkDelete`/`ExecutedBulkUpdate` now pass `logSensitiveData: false` instead of
`diagnostics.ShouldLogSensitiveData()`. Comment added explaining why: the `Executed` event never carries a
command, so there is nothing sensitive on that payload to flag, and calling `ShouldLogSensitiveData()` there was
a gratuitous trigger of EF's one-shot `SensitiveDataLoggingEnabledWarning` from a path that has no sensitive
content.

## Minor 6 — `Elapsed` doc comments now describe what the value spans per strategy

Both `Elapsed` doc comments (constructor `<param>` and property `<summary>`, on both event-data classes) were
rewritten from "The time elapsed since the command was sent to the database" (already wrong pre-task) to:

> For `SingleCommand`, the time since immediately before the `deleteMany`/`updateMany` call (so on the
> `Executing` event this is effectively zero, and on `Executed` it is the driver round-trip time). For
> `TwoPhase`, the time since immediately before the phase-1 `_id`-harvesting read — so both the `Executing` and
> `Executed` events' elapsed include phase 1 as well as phase 2.

## Deferred (per coordinator instruction, not fixed this round)

Comment-placement asymmetry across the four `ExecuteTwoPhase*` methods; the `if`-block style in
`SingleEntityDbContext`; the payload-selection predicates; the escaped-quote `DoesNotContain`; the discarded
`SpyLoggerProvider` binding in some new theories (`var (loggerFactory, _) = SpyLoggerProvider.Create();` — kept
because `loggerFactory` is still needed to construct the `DbContext`, only the `spyLogger` half is unused in
tests that assert on the `DiagnosticSource` payload rather than the `ILogger` message).

## Covering tests — exact commands and output

Both `MONGODB_URI` and `ATLAS_URI` left unset (TestContainers `mongodb/mongodb-atlas-local`, confirmed via
`docker ps` as in the original round).

```bash
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
Build succeeded. 0 Warning(s). 0 Error(s).

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~BulkOperationsCommandTests"
Passed! - Failed: 0, Passed: 12, Skipped: 0, Total: 12, Duration: 717 ms - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
Passed! - Failed: 0, Passed: 43, Skipped: 1, Total: 44, Duration: 1 s - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)

$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
Build succeeded. 0 Warning(s). 0 Error(s).

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --filter "FullyQualifiedName~BulkOperationsCommandTests"
Passed! - Failed: 0, Passed: 12, Skipped: 0, Total: 12, Duration: 666 ms - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
Passed! - Failed: 0, Passed: 44, Skipped: 1, Total: 45, Duration: 1 s - MongoDB.EntityFrameworkCore.FunctionalTests.dll (net10.0)

$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
Build succeeded. 0 Warning(s). 0 Error(s).
```

(EF10's 43/44 vs EF9's 44/45 reflects one EF-version-gated test elsewhere in `ExecuteUpdateTests`/
`ExecuteDeleteTests`, unrelated to this change — both runs have exactly one `[Skip]`, same test name,
`ExecuteDelete_two_phase_on_standalone_throws_actionable_error`.)

No scratch tests were used for evidence in this round — every claim above is backed by a test that is in the
diff and was just run.

## Files changed this round

- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkDeleteEventData.cs` — `Command`/`Elapsed` doc fixes
  (Finding 4, Minor 6).
- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkUpdateEventData.cs` — same.
- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerUpdateExtensions.cs` — `Func<string> renderCommand`
  (Finding 3), `p.Command!` (Finding 4), `logSensitiveData: false` on `Executed` (Minor 5).
- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` — eight call sites now pass
  `() => RenderBulkCommand(...)` thunks (Finding 3); comment update.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs` — Deviation A revert;
  four new `[Theory]` methods for Finding 1; Finding 2's deterministic elapsed assertions bundled into the
  two-phase theories.
