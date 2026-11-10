# Task 23 Report — Assert the event payload, not just the rendered message

## Summary of what changed vs. the brief

The brief assumed a green field: no spy existed, one event-data type carried everything, and nothing
asserted the payload directly. All three premises were stale. Investigating the current state of the repo
found:

- `DiagnosticSourceSpy` (Task 4) already exists and is already the established idiom (`tests/.../Utilities/DiagnosticSourceSpy.cs`).
- `BulkOperationsCommandTests.cs` (landed as part of the EF-361 command/redaction work, commits `d7ff744`/`8f8ec3c`) **already thoroughly covers** the brief's second test almost verbatim: `Command_is_redacted_when_sensitive_logging_disabled` asserts `TargetCount == null`, `LogSensitiveData == false`, `Command == "?"` on a single-command delete's *Executing* payload, via `DiagnosticSourceSpy`, using exactly the `OfType<MongoBulkDeleteEventData>().Single(predicate)` idiom. Writing the brief's second test verbatim would have been near-duplicate test infrastructure/coverage.
- What genuinely has **no** coverage anywhere in the suite: the ***Executed*** events' payload properties. `ExecuteDeleteTests`/`ExecuteUpdateTests` verify `MatchedCount`/`ModifyCount`/`DeleteCount` only by substring-matching the `ILogger`-rendered message ("Matched=2", "Deleted=7"), never the `EventData` properties. And `Strategy` on the *Executed* event is **not rendered into the log message at all** (`LogExecutedBulkDelete`/`LogExecutedBulkUpdate`'s `LoggerMessage.Define` format strings have no `{strategy}` placeholder) — grepped the whole test tree; zero prior assertions of `.Strategy` on an Executed payload. I retargeted the new file at this real, previously-unverified gap rather than re-proving what `BulkOperationsCommandTests.cs` already proves.

## What I added

File: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsEventDataTests.cs` (new, test-only, no `src/` changes). `#if !EF8` guarded (confirmed excluded from the EF8 build — 0 warnings, `MongoBulkDeleteEventData`/`MongoBulkUpdateEventData` don't exist on EF8). No BOM (matches every other file in `Query/`, none of which have one — checked `xxd` on four neighbours before writing). Standard licence header copied verbatim from `ExecuteDeleteTests.cs`. `[XUnitCollection("QueryTests")]` applied, per the corrections.

Two tests:

1. **`Two_phase_update_event_data_carries_target_matched_and_modified_counts`** — two-phase `ExecuteUpdate` (`OrderBy(Quantity).Take(2)`, 2 of 3 seeded docs targeted). Asserts:
   - The *Executing* `MongoBulkUpdateEventData`: `Strategy == "TwoPhase"`, `TargetCount == 2`.
   - The *Executed* `MongoBulkUpdateEventData`: `Strategy == "TwoPhase"`, `TargetCount == null`, `MatchedCount == 2`, `ModifyCount == 2`.
2. **`Single_command_delete_executed_event_data_carries_the_delete_count_and_the_strategy_the_log_message_omits`** — single-command `ExecuteDelete` (`Where`-only, 2 of 3 seeded docs targeted). Asserts:
   - The *Executed* `MongoBulkDeleteEventData`: `Strategy == "SingleCommand"`, `TargetCount == null`, `DeleteCount == 2`.
   - The ILogger-rendered `ExecutedBulkDelete` message does **not** contain `"SingleCommand"` — the concrete proof that `Strategy` reaches `DiagnosticSource` subscribers on a surface the `ILogger` path never exposes.

## The actual event-data type hierarchy (Correction 2)

Three sibling `EventData` subclasses, all under `src/MongoDB.EntityFrameworkCore/Diagnostics/`, none related by inheritance beyond the common `EventData` base:

- **`MongoBulkWriteEventData`** — unrelated to `ExecuteDelete`/`ExecuteUpdate`. It backs `ExecutingBulkWrite`/`ExecutedBulkWrite` (the `SaveChanges` bulk-write path — `InsertCount`/`DeleteCount`/`ModifyCount`). Not `#if !EF8` gated; not used anywhere in this task.
- **`MongoBulkDeleteEventData`** — backs **both** `ExecutingBulkDelete` and `ExecutedBulkDelete`. It's the *same type* on both events, not two separate types, with different fields populated per event:
  - `ExecutingBulkDelete` constructs it with `deleteCount: 0`, `targetCount:` (the batch's own id count, or null for single-command), `command:` (the rendered filter, or `"?"` when sensitive logging is off — **never null**), `logSensitiveData:` derived from `diagnostics.ShouldLogSensitiveData()`.
  - `ExecutedBulkDelete` constructs it with `deleteCount:` (the run total), `targetCount: null` (always), `command: null` (always — hardcoded), `logSensitiveData: false` (always — hardcoded, with a comment explaining there's nothing sensitive on this payload).
- **`MongoBulkUpdateEventData`** — same shape as above for `ExecutingBulkUpdate`/`ExecutedBulkUpdate`, plus a second counter (`MatchedCount` alongside `ModifyCount`).

So the brief's premise ("one type, `TargetCount`/`MatchedCount`/`ModifyCount` all on it") was wrong in *which* type (it named `MongoBulkWriteEventData`, which has none of those three properties) but right that Executing and Executed share a type — just the wrong one, and the shared-type detail wasn't the actual gap (that's how `BulkOperationsCommandTests.cs` already discriminates).

## How I discriminated events given the unordered `ConcurrentBag`

Per Correction 3, never by position. I used **`Command`**, not counts: `ExecutingBulk{Delete,Update}` always passes a non-null command string (the real filter, or `"?"` when redacted); `ExecutedBulk{Delete,Update}` always passes `command: null`. So `.Where(e => e.Command is not null)` / `is null` cleanly separates the two regardless of counts. This is strictly more robust than the existing precedent's count-based predicates (e.g. `e.TargetCount == null && e.DeleteCount == 0`), which would be ambiguous on an operation matching zero documents — both Executing and Executed would then report identical zeros. I noted this explicitly in the test comments as the reason for the choice, since it's a real edge case the existing precedent doesn't have to handle (its fixtures always have nonzero counts).

## How I know `Command == "?"` reflects redaction, not absence — for the existing precedent I relied on

I did **not** re-assert this in my new file (it's `BulkOperationsCommandTests.cs`'s job, and it already does it thoroughly), but since the task asks me to be able to say how it's known: `RenderBulkCommand` is invoked unconditionally whenever either `ShouldLog` or `NeedsEventData` is true (`var command = renderCommand();` runs before the `if (shouldLog)`/`if (needsEventData)` branches in `ExecutingBulkDelete`/`ExecutingBulkUpdate`), and the render delegate itself is "already gated behind `ShouldLogSensitiveData()`" per its own XML doc — it returns the literal string `"?"` when the gate is closed, not an unset/empty value. The proof that this is redaction and not "never rendered" is structural: `Command` on an *Executing* event is **never `null`** (confirmed by reading both constructors — the only place `command: null` is passed is `Executed*`, hardcoded). A "never rendered" bug would show up as `Command == null` on Executing, not `"?"`. `BulkOperationsCommandTests.cs`'s `*_command_is_gated_by_sensitive_data_logging` theories additionally close the loop empirically by running the *same* call with the gate open and asserting the real filter text appears — that's the conclusive "the gate, not absence, produced `?`" evidence, and it already exists.

## For each new test, what would have to break for it to fail

- **`Two_phase_update_event_data_carries_target_matched_and_modified_counts`**:
  - `ExecutingBulkUpdate` reporting the wrong `TargetCount` for its batch (e.g. the phase-1 total instead of this command's own share, or a stale/zero value).
  - `Strategy` on either event reporting `"SingleCommand"` for what is actually a two-phase operation (a real bug class: `MongoBulkPlan.Strategy` disagreeing with which code path actually ran).
  - `ExecutedBulkUpdate` reporting the wrong `MatchedCount` or `ModifyCount` (including a cross-assignment bug, e.g. `ModifyCount` fed from the matched accumulator or vice versa).
  - If a future change made `ExecutedBulkUpdate` populate `Command` (breaking the redaction-boundary invariant that only `Executing` renders it), the `Where(e => e.Command is null)` filter would find zero elements and `Assert.Single` would throw — loud failure, not a silent pass.
  - If the batch ceiling were lowered below 2 (not currently possible without a code change) or the fixture raised above 50,000 ids, more than one Executing event would fire and `Assert.Single(executing)` would throw — documented in-line as the assertion's known boundary (Correction 4).
- **`Single_command_delete_executed_event_data_carries_the_delete_count_and_the_strategy_the_log_message_omits`**:
  - `ExecutedBulkDelete` reporting the wrong `DeleteCount`.
  - `Strategy` on the Executed payload reporting `"TwoPhase"` for a single-command operation, or being null/missing.
  - `TargetCount` becoming non-null on a single-command Executed event (would indicate the single-command/two-phase code paths got their Executed construction crossed).
  - The rendered `ExecutedBulkDelete` message starting to include the literal substring `"SingleCommand"` (e.g. if a future change added `{strategy}` to that format string) — this assertion would then need updating, and is explicitly called out in the test's own comment as the one case where the fix is "update the test," not "something regressed."

## XML doc accuracy — deferred item, not fixed here

Confirmed the specific inaccuracy flagged in Correction 5: the `LogSensitiveData` XML doc on both `MongoBulkDeleteEventData` and `MongoBulkUpdateEventData` reads "Whether sensitive-data logging was enabled when this event was raised" with no caveat — but on the *Executed* event it's unconditionally `false` regardless of the actual `EnableSensitiveDataLogging` setting (hardcoded at the `ExecutedBulkDelete`/`ExecutedBulkUpdate` call sites, with an inline comment explaining why). A reader relying solely on the public XML doc would reasonably expect `LogSensitiveData` on an Executed payload to reflect the real setting; it doesn't. Per the task's explicit instruction, I did not fix this doc — flagging it as a known, deferred item.

I did not find any other XML-doc inaccuracies while reading `MongoBulkDeleteEventData.cs`/`MongoBulkUpdateEventData.cs`/`MongoBulkWriteEventData.cs` in full.

## Testing performed

All runs with `MONGODB_URI` and `ATLAS_URI` unset (confirmed via `echo` before running — TestContainers atlas-local).

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"  → Build succeeded, 0 Warning(s), 0 Error(s)
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"   → Build succeeded, 0 Warning(s), 0 Error(s)
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"   → Build succeeded, 0 Warning(s), 0 Error(s)  (new file excluded by #if !EF8)

dotnet test ... -c "Debug EF10" --filter "FullyQualifiedName~BulkOperationsEventDataTests"
  → Passed! Failed: 0, Passed: 2, Skipped: 0, Total: 2

dotnet test ... -c "Debug EF9" --filter "FullyQualifiedName~BulkOperationsEventDataTests"
  → Passed! Failed: 0, Passed: 2, Skipped: 0, Total: 2

dotnet test ... -c "Debug EF10" --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
  → Passed! Failed: 0, Passed: 90, Skipped: 1 (ExecuteDelete_two_phase_on_standalone_throws_actionable_error,
    intentionally skipped — requires a standalone non-replica-set server), Total: 91
    (confirms the process-wide DiagnosticSourceSpy subscription in the new file's tests does not perturb these
    suites when run in the same process — the two spies are disposed via `using` and don't leak across tests)

dotnet test ... -c "Debug EF10" --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
  → Passed! Failed: 0, Passed: 181, Skipped: 0, Total: 181   (matches the 181/181 baseline exactly)
```

An initial build surfaced two `xUnit2031` analyzer warnings ("Do not use a Where clause to filter before
calling Assert.Single") on the `Assert.Single(collection.Where(...))` pattern for the *Executed* lookups —
fixed by switching to `Assert.Single(collection, predicate)`, the overload the analyzer wants. Rebuilt all
three configurations after the fix: 0 warnings, 0 errors on every one. Also renamed the first test from an
earlier "…_executed_event_data_carries_the_total…" working title back to
`Two_phase_update_event_data_carries_target_matched_and_modified_counts` (matching the brief's original name)
since the test asserts both the Executing event's `TargetCount` and the Executed event's totals, and the
narrower name undersold what it checks; the second test's name already accurately scoped to what it asserts,
so it was left as-is. Rebuilt/retested all three configurations after the rename.

## Files changed

- **Added**: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsEventDataTests.cs`

No other files touched. No `src/` changes.

## Self-review

- **Test names match their assertions**: yes, per the naming discussion above — both names now describe the actual concrete claims (which properties, on which event, and for `Single_command_delete_...`, the log-message-omission claim).
- **Would each test fail if the payload regressed?**: yes — see the "what would have to break" section above; each assertion targets a specific, plausible regression, not a tautology.
- **Any assertion checking a hardcoded constant?**: no. I deliberately avoided asserting `LogSensitiveData` on any *Executed* payload (Correction 5) since that's hardcoded `false` unconditionally and would be a test that cannot fail. I did assert `Command == null` and `TargetCount == null` on Executed payloads — these are also currently hardcoded, but I judged them worth keeping (unlike `LogSensitiveData`) because they encode a real security/redaction-boundary invariant ("Executed never leaks a rendered command or a target-id count") that a plausible future change (e.g. someone deciding to enrich the Executed event with diagnostic detail) could break, and breaking it would be a real regression worth catching — not merely restating "it's currently `null`" with no failure mode. Flagging this judgment call explicitly in case it reads as borderline.
- **Test output pristine?**: yes — no stray prints; the brief's "print event keys" debugging step was unnecessary because this file discriminates by payload type + the `Command` property (per Correction 3), not by `DiagnosticListener`/event-key name matching, so there was never a print to remove. Confirmed via `grep` there's no `Console.Write`/`Debug.Write` in the file, and the two test runs above show clean pass output with no extraneous console noise.

## Concerns

None blocking. One judgment call flagged above (keeping `Command`/`TargetCount == null` assertions on Executed payloads despite them currently being hardcoded constants) — I believe it's the right call given the security-boundary rationale, but noting it since Correction 5 was specifically about not asserting hardcoded values.
