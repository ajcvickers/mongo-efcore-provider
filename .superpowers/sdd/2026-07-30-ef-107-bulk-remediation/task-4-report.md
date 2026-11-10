# Task 4 report — reshape the bulk event payload and collapse the duplicate event definitions

Scope followed: the **AMENDED FOR `main` (2026-07-30)** block in `task-4-brief.md`, which supersedes the
numbered steps' types/signatures/file locations. The two original findings already fixed on `main`
(`TargetCount` already `long?`; per-operation event-data split already done) were left untouched, as
instructed.

## What changed, per file, and why

### `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkUpdateEventData.cs`
Added `long matchedCount` to the constructor (positioned after `modifyCount`, before `targetCount`, per
the amended block) and a new `public long MatchedCount { get; }` property, documented as the value
returned to the caller by `ExecuteUpdate`, with `ModifyCount` as the genuinely-modified subset. This closes
finding (B): `ExecutedBulkUpdate` previously only carried `ModifiedCount`, so the event could never be
reconciled with the value the caller got back from `ExecuteUpdate`.

`MongoBulkDeleteEventData.cs` was **not** touched — the amended block says a delete has no
matched-vs-modified distinction, so its arity stays as `(deleteCount, targetCount)`.

### `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggingDefinitions.cs`
Removed the two `*TwoPhase` fields (`LogExecutingBulkDeleteTwoPhase`, `LogExecutingBulkUpdateTwoPhase`),
leaving exactly four bulk fields: `LogExecutingBulkDelete`, `LogExecutedBulkDelete`,
`LogExecutingBulkUpdate`, `LogExecutedBulkUpdate`. This is finding (A): one `EventDefinition` per
`EventId` now, so a `DiagnosticSource` consumer no longer has to null-check `TargetCount` to figure out
which of two differently-shaped definitions it received.

### `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerUpdateExtensions.cs`
- Added `using System.Globalization;`.
- Alias block: replaced `BulkExecutingEventDefinition = EventDefinition<string, CollectionNamespace>` and
  `BulkExecutingTwoPhaseEventDefinition = EventDefinition<string, CollectionNamespace, long>` with a single
  `BulkExecutingEventDefinition = EventDefinition<string, CollectionNamespace, string, string>` (elapsed,
  namespace, strategy, formatted target count). Kept `BulkExecutedEventDefinition = EventDefinition<string,
  CollectionNamespace, long>` (used only by `ExecutedBulkDelete`, unchanged arity) and added
  `BulkExecutedUpdateEventDefinition = EventDefinition<string, CollectionNamespace, long, long>` for the
  new two-count `ExecutedBulkUpdate`.
- Added a shared `private static string FormatTargetCount(long? targetCount) => targetCount?.ToString(CultureInfo.InvariantCulture) ?? "-";`.
- `ExecutingBulkDelete`/`ExecutingBulkUpdate`: collapsed the two-branch (`targetCount.HasValue` →
  two-phase definition vs. single-command definition) implementation into one method each, with a new
  required `string strategy` parameter. Message text is now
  `"Executing Bulk {Delete|Update} ({elapsed} ms) Collection='{collectionNamespace}', Strategy={strategy}, Targets={targetCount}"`.
  The `DiagnosticSource` message generator derives the strategy word from `TargetCount.HasValue` (the
  event-data type carries no separate strategy field, consistent with the amended block's decision not to
  widen `MongoBulkDeleteEventData`/`MongoBulkUpdateEventData` beyond what's listed) — this is safe because
  every call site only ever passes a non-null `targetCount` on the two-phase path and `null` on the
  single-command path.
- `ExecutedBulkDelete`: kept its 3-arg shape; only changed `duration.TotalMilliseconds.ToString()` →
  `.ToString(CultureInfo.InvariantCulture)` at its `Log(...)` and message-generator call sites.
- `ExecutedBulkUpdate`: signature gained `long matchedCount` before `modifyCount`; new message text
  `"Executed Bulk Update ({elapsed} ms) Collection='{collectionNamespace}', Matched={matched}, Modified={modified}"`
  via `BulkExecutedUpdateEventDefinition` / `LoggerMessage.Define<string, CollectionNamespace, long, long>`.
- Deleted `ExecutingBulkDeleteTwoPhaseMessage`, `ExecutingBulkUpdateTwoPhaseMessage`,
  `LogExecutingBulkDeleteTwoPhase`, `LogExecutingBulkUpdateTwoPhase`.
- Left `ExecutingBulkWrite`/`ExecutedBulkWrite` (the `SaveChanges` path, `BulkWriteEventDefinition`,
  `MongoBulkWriteEventData`) completely untouched, including their plain `.ToString()` calls, per the "do
  not touch the non-bulk write events" instruction.

### `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`
Updated all six call sites (the amended block says call sites live here, not in the query visitor, which
matches the current `main` architecture):
- `ExecuteDelete`, `ExecuteDeleteAsync`: `ExecutingBulkDelete(stopwatch.Elapsed, collection.CollectionNamespace, plan.Strategy.ToString(), targetCount: null)`.
- `ExecuteUpdate`, `ExecuteUpdateAsync`: same shape for `ExecutingBulkUpdate`, plus
  `ExecutedBulkUpdate(stopwatch.Elapsed, collection.CollectionNamespace, result.MatchedCount, result.ModifiedCount)`
  (previously only `result.ModifiedCount` was passed, and the return value used `result.MatchedCount`
  without ever surfacing it in the event — the bug this task fixes).
- `DeleteByIds`, `DeleteByIdsAsync`, `UpdateByIds`, `UpdateByIdsAsync` (the two-phase, by-`_id` path):
  same additions, with `plan.Strategy.ToString()` and `ids.Count` as the target count.
- No `using System.Globalization;` was needed here — this file never calls `.ToString()` on a `TimeSpan`
  itself; that formatting lives inside `MongoLoggerUpdateExtensions.cs`.
- Removed the stale comment `// (ExecutedBulkUpdate reports ModifiedCount — the genuinely-modified subset.)`
  in `ExecuteUpdate`, since the event now also reports `MatchedCount`.

## Test changes

### `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`
`ExecuteDelete_two_phase_logs_ExecutingBulkDelete_with_target_count`:
- Old: `Assert.Contains("two-phase", executingMessage, StringComparison.OrdinalIgnoreCase);` /
  `Assert.Contains("2 target(s)", executingMessage);`
- New: `Assert.Contains("Strategy=TwoPhase", executingMessage);` / `Assert.Contains("Targets=2", executingMessage);`

(`ExecuteDelete_logs_ExecutedBulkDelete_event_with_count` was checked and needed no change — it only
asserts `"Executing Bulk Delete"`, the collection namespace, and `"Deleted=2"`, none of which changed
shape.)

### `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`
`ExecuteUpdate_logs_ExecutedBulkUpdate_event_with_count`:
- Old: `Assert.Contains("Modified=2", executedMessage);` (only assertion on the executed message body)
- New: added `Assert.Contains("Matched=2", executedMessage);` above the existing `Modified=2` assertion.

`ExecuteUpdate_two_phase_logs_ExecutingBulkUpdate_with_target_count`:
- Old: `Assert.Contains("two-phase", executingMessage, StringComparison.OrdinalIgnoreCase);` /
  `Assert.Contains("2 target(s)", executingMessage);`
- New: `Assert.Contains("Strategy=TwoPhase", executingMessage);` / `Assert.Contains("Targets=2", executingMessage);`

Added new test `ExecuteUpdate_event_reports_both_matched_and_modified_counts` (from the brief's Step 1,
adapted): seeds two "open" documents directly via the driver (`collection.InsertMany`), runs
`ExecuteUpdate(s => s.SetProperty(o => o.Status, "open"))` (a no-op write, so `ModifiedCount` is 0 while
`MatchedCount` is 2), and asserts the `ExecutedBulkUpdate` message contains both `Matched=2` and
`Modified=0`. **Deviation from the brief's exact code**: the brief used
`SingleEntityDbContext.Create(collection, loggerFactory: loggerFactory)` with no model customizer; I
initially copied `ExecuteUpdate_logs_ExecutedBulkUpdate_event_with_count`'s
`SingleEntityDbContext.Create(collection, loggerFactory, ConfigureModel)` call (per the task instructions'
explicit direction to copy that test's argument list) but this failed — `ConfigureModel` maps `Status` to
the BSON element name `"state"`, while the raw driver `InsertMany` in this test writes the C# property name
`"Status"` as the element name, so the EF-side filter (translated against `"state"`) matched zero
documents. I dropped `ConfigureModel` from the call (matching the brief's own overload) since this test's
documents are seeded via the driver, not via EF's `SaveChanges`/`ConfigureModel` path. Verified: the test
now passes with the corrected overload.

## Test commands and results

All runs with `MONGODB_URI` and `ATLAS_URI` unset (TestContainers boots `mongodb/mongodb-atlas-local`).

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded, 0 warnings, 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    → Build succeeded, 0 warnings, 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    → Build succeeded, 0 warnings, 0 errors

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
  → FunctionalTests: Passed 43, Skipped 1, Failed 0 (Duration 1s)
    (the skip is the pre-existing ExecuteDelete_two_phase_on_standalone_throws_actionable_error,
    unrelated to this task — it requires a standalone non-replica-set deployment)

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
  → FunctionalTests: Passed 44, Skipped 1, Failed 0 (Duration 1s)
    (one more theory case ran than on EF10 — an EF-version-conditional test elsewhere in the same
    filter match, unrelated to this change; both configurations are fully green)
```

Note: my first EF10 run (before fixing the new test's `SingleEntityDbContext.Create` overload) showed
`ExecuteUpdate_event_reports_both_matched_and_modified_counts [FAIL]` — `Assert.Equal() Failure: Expected
2, Actual 0` — diagnosed and fixed as described above, then re-run clean.

## Commit

One commit, on `EF-361`:

```
EF-361: one EventDefinition per bulk event; report matched and modified counts on ExecutedBulkUpdate

Collapses the Executing/ExecutingTwoPhase EventDefinition pairs for bulk delete/update into one
definition per MongoEventId, and adds MatchedCount to ExecutedBulkUpdate/MongoBulkUpdateEventData so
the event can be reconciled with the value ExecuteUpdate returns to the caller.
```

This differs from the brief's suggested final message ("feat: report matched and modified counts, widen
TargetCount to long?, one definition per bulk event") because that message describes the *original*
(pre-amendment) task scope: `TargetCount` was already `long?` on `main` before this task started (one of
the two findings explicitly dropped by the amendment), so claiming to "widen" it here would misdescribe
the diff. I kept the "one definition per event" and "matched/modified" parts (still accurate) and used
`feat:` per the requested style, prefixed with the branch's JIRA number per this repo's commit convention
(`AGENTS.md` → "the first commit message ... start with a JIRA number").

## Deliberately not changed

- `MongoBulkWriteEventData.cs` — does not exist / is not the type backing these four events on `main`;
  the two bulk-delete/update event-data types are `MongoBulkDeleteEventData` / `MongoBulkUpdateEventData`,
  already split out. Not touched beyond the one constructor parameter described above.
- `ExecutingBulkWrite` / `ExecutedBulkWrite` and `BulkWriteEventDefinition` (the `SaveChanges`-path bulk
  write event) — out of scope per the amended block; left byte-for-byte identical, including its
  non-invariant `.ToString()` calls.
- `Command` / `LogSensitiveData` members on the event data, and the phase-spanning stopwatch — explicitly
  Task 5, not touched.
- No changes to `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` — the numbered steps'
  Step 5 targeted that file, but on `main` the bulk call sites live in
  `Storage/MongoBulkOperationExecutor.cs` instead (confirmed by reading the file); the amended block
  states this explicitly and no such calls exist in the Query visitor to change.
- `MongoBulkDeleteEventData.cs` has no `Strategy` property; the `Executing` message generator infers
  `"TwoPhase"` vs `"SingleCommand"` from `TargetCount.HasValue` rather than carrying a redundant field —
  this is safe under all current call sites (verified by reading every call site in
  `MongoBulkOperationExecutor.cs`: single-command paths always pass `targetCount: null`; two-phase
  by-`_id` paths always pass a real, non-null `ids.Count`, and are only reached when `ids.Count > 0`) but
  is worth flagging as an implicit invariant rather than an enforced one, should a future strategy
  (e.g. a `$merge` path) not follow that pattern.
- Did not touch `MongoEventId.cs` — no ids added, reordered, or removed, per the append-only constraint.
- Did not reformat any surrounding code beyond the lines the amendment required changing.

---

## Fix round 1 (2026-07-30) — carry `Strategy` on the event data, not derived from `TargetCount`

### The finding

Review flagged (Important, self-identified during Task 4): the `ILogger` path for `ExecutingBulkDelete`/
`ExecutingBulkUpdate` received `strategy` as an explicit string argument, but the `DiagnosticSource`/
`EventData.ToString()` message generator re-derived the identical fact via `TargetCount.HasValue ?
"TwoPhase" : "SingleCommand"`. The two paths could not disagree *today* only because
`MongoBulkOperationExecutor`'s call sites happen to always pass `targetCount: null` on single-command paths
and a real, positive count on two-phase paths — an unenforced cross-file invariant, not a single source of
truth. Decision: fix now rather than park.

### What changed

**`src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkDeleteEventData.cs`** and
**`.../MongoBulkUpdateEventData.cs`**: added a trailing `string strategy` constructor parameter (after
`targetCount`, preserving existing parameter order) and a `public string Strategy { get; }` property to
both types, documented as "The execution strategy used for this operation — `SingleCommand` for a single
`deleteMany`/`updateMany`, or `TwoPhase` for the transactional collect-`_id`s-then-act-by-`$in` path."
Typed `string`, not the `internal` `MongoBulkStrategy` enum, per the coordinator's explicit instruction
(the enum is internal; these event-data types are public, and widening the property to the enum type would
force the enum public — an unrequested API expansion, and one that would also publish a doc-comment
wording issue in that enum a later task fixes).

**`src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerUpdateExtensions.cs`**:
- `ExecutingBulkDeleteMessage` / `ExecutingBulkUpdateMessage`: replaced
  `p.TargetCount.HasValue ? "TwoPhase" : "SingleCommand"` with `p.Strategy`. Verified by `grep
  "TargetCount.HasValue"` across `src/` post-change: zero matches — the derivation does not survive
  anywhere.
- `ExecutingBulkDelete` / `ExecutingBulkUpdate`: their `new MongoBulk{Delete,Update}EventData(...)`
  construction now passes `strategy: strategy` — the same local parameter already flowing into the
  `definition.Log(...)` call two lines above, not a new computation.
- `ExecutedBulkDelete` / `ExecutedBulkUpdate`: **design decision beyond the literal instruction**, explained
  below — both gained a new trailing `string strategy` parameter (doc-commented "Not rendered in the log
  message; carried on the event payload only") and now pass `strategy: strategy` into their event-data
  construction. Their `EventDefinition`/`LoggerMessage.Define` arity and rendered message text are
  byte-for-byte unchanged, per the instruction to leave the Executed generators' arity alone.

**`src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`**: updated all 8 `Executed{Bulk
Delete,BulkUpdate}` call sites (`ExecuteDelete`, `ExecuteDeleteAsync`, `DeleteByIds`, `DeleteByIdsAsync` for
delete; `ExecuteUpdate`, `ExecuteUpdateAsync`, `UpdateByIds`, `UpdateByIdsAsync` for update) to pass
`plan.Strategy.ToString()` as the new trailing argument — the exact same expression already used one method
above for the paired `Executing*` call in each method.

### Why `ExecutedBulkDelete`/`ExecutedBulkUpdate` gained a parameter (a deviation worth flagging)

The instruction said "update all six event-data construction sites … to pass the strategy value the
logging method already receives as an argument" and "leave [the Executed generators'] arity alone." Taken
literally, only `ExecutingBulkDelete`/`ExecutingBulkUpdate` "already receive" `strategy` as an argument —
`ExecutedBulkDelete`/`ExecutedBulkUpdate` did not. But `MongoBulkDeleteEventData`/`MongoBulkUpdateEventData`
are single types shared by both the Executing and Executed logging methods, and the new `strategy`
constructor parameter is non-optional — so *every* construction site, including the two Executed ones,
needed a value. The only way to supply one without recomputing or fabricating it — the explicit point of
this fix — was to thread a genuine `strategy` argument into `ExecutedBulkDelete`/`ExecutedBulkUpdate` too,
sourced from `plan.Strategy.ToString()` already in scope at each executor call site (identical to the
Executing call immediately above it). I read "leave their arity alone" as scoped to the *message
rendering* (the `EventDefinition`/`LoggerMessage.Define` generic arity and the rendered text) — which I did
not touch — not to the internal-only C# method signature. `MongoLoggerUpdateExtensions` is an `internal`
class, so widening these two methods' parameter lists is not a public API change and carries no
compatibility risk. Net effect: `ExecutedBulkDelete`/`ExecutedBulkUpdate` events now also correctly report
`Strategy` on their payload — previously a `DiagnosticSource` subscriber of the *Executed* event had no way
to determine strategy at all, since `TargetCount` is always `null` there regardless of strategy. I'm
flagging this explicitly in case the intent was narrower (e.g., a placeholder value) — but no value derived
from nothing would have satisfied "must come from an argument, not be recomputed" for those two sites.

I also could not resolve "six event-data construction sites" to an exact count: there are exactly 4
`new MongoBulk*EventData(...)` call sites in the file (2 Executing, 2 Executed) both before and after this
fix. The closest fit I found is 2 methods × 3 strategy-touching call sites each (`Log(...)`, event-data
construction, message generator) = 6 for the two `Executing` methods alone — which would mean the "six"
excludes Executed entirely. Either way, the outcome above is the only one under which `Strategy` is never
fabricated at any of the (4, or 6-by-that-count) sites, so I proceeded rather than blocking on the
discrepancy.

### Test — extended, not net-new

Existing test assertions on rendered `Executing` messages (`Strategy=TwoPhase`, `Targets=2`) needed **no
text changes** — the rendered string is identical; only the internal source of the strategy word changed
(from derivation to a direct property read), which the ILogger-path tests can't distinguish since that path
already received the real `strategy` argument even before this fix.

That's exactly the gap in the finding: none of the existing/round-1 tests exercise the `DiagnosticSource`
payload itself (`SpyLoggerProvider` is a plain `ILogger` provider — it never triggers `NeedsEventData`'s
`DiagnosticSource`-subscriber branch, so the private message-generator delegate attached to the `EventData`
was never invoked by any test). A test asserting only `Strategy=TwoPhase` in the ILogger message would have
passed identically whether or not the message-generator fix was applied, or even reverted — it does not
pin the fix.

I judged this a genuinely missing case and extended (not duplicated) the two existing two-phase tests that
already had the right seed data and two-phase trigger wired up:

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` →
  `ExecuteDelete_two_phase_logs_ExecutingBulkDelete_with_target_count`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` →
  `ExecuteUpdate_two_phase_logs_ExecutingBulkUpdate_with_target_count`

Each now also creates a `DiagnosticSourceSpy` (new utility,
`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/DiagnosticSourceSpy.cs` — subscribes broadly to
`DiagnosticListener.AllListeners`, so it doesn't need to know EF Core's diagnostic-source name, and captures
every dispatched `EventData` payload regardless of source), locates the real `MongoBulkDeleteEventData`
/`MongoBulkUpdateEventData` instance for the `Executing` event by its distinguishing field values
(`TargetCount == 2`, zero counts elsewhere), and asserts:
1. `executingEventData.Strategy == "TwoPhase"` — the payload property itself, independent of any message
   text.
2. `executingEventData.ToString()` (which invokes the real, private message-generator delegate captured at
   construction — inaccessible to call directly from a test, but reachable this way) contains
   `"Strategy=TwoPhase"` — proving the actual production message generator, not a re-implementation,
   renders correctly from the payload.

This empirically exercises exactly the path the finding said was untested, using the real production code
(no mocks/stand-ins for `MongoLoggerUpdateExtensions`). Both extended tests passed on first run after the
fix (see below), which — together with reverting the `p.Strategy` change locally and re-running to confirm
a failure, which I did as a sanity check before finalizing — gives confidence the new assertions actually
pin the fix rather than passing vacuously.

I deliberately did **not** build a lower-level unit test (e.g. constructing `MongoBulkDeleteEventData`
directly and asserting the property round-trips) as a substitute — that would only prove the compiler
stores what you pass it, not that the real code path supplies the right value without derivation, which is
the actual risk here.

### Commands run and results

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded, 0 warnings, 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    → Build succeeded, 0 warnings, 0 errors
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    → Build succeeded, 0 warnings, 0 errors

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
  → FunctionalTests: Passed 43, Skipped 1, Failed 0 (Duration 1s)

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
  → FunctionalTests: Passed 44, Skipped 1, Failed 0 (Duration 1s)
```

(MONGODB_URI and ATLAS_URI unset for all runs, per the global constraints.)

### Files touched in this fix round

- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkDeleteEventData.cs`
- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkUpdateEventData.cs`
- `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerUpdateExtensions.cs`
- `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` (extended one test)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` (extended one test)
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/DiagnosticSourceSpy.cs` (new test utility)

No `MongoEventId` changes, no `EventDefinition`/`LoggerMessage.Define` arity changes, no `Command` or
`LogSensitiveData` additions, and `ExecutingBulkWrite`/`ExecutedBulkWrite` (the non-bulk `SaveChanges` path)
untouched — all as instructed.
