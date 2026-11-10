# SDD ledger — plan: docs/superpowers/plans/2026-07-30-ef-107-bulk-remediation.md

Branch: EF-361 (off main @ 58e05a0). Repo: ~/code/provider2. No worktree (human directive).
Controllerable note: task bodies cite pre-refactor file paths; the plan Reconciliation section holds the remap. Carry it in each dispatch.

Task 1: complete (commits a38aeba..8e1f728, review clean) — implementer aaea6665fb510ae81
Task 2: complete (commits 8e1f728..74fc9e5, review clean) — implementer a49b44a601fd086b0
Task 2: minor (deferred): redundant 'using System.Linq;'/'using Xunit;' in MongoNonQueryExpressionUnwrapTests.cs — Usings.cs already globals both (came from the plan's own code block)
Task 3: implemented (commits 74fc9e5..5e94714) — implementer a5fe9a33198dd5b12; review: spec OK, quality approved with 1 Important (stale 'three shapes' comment) + 1 Minor (test file belongs in UnitTests/Storage/)
Task 3: fix round 1/5 IN FLIGHT — resumed a5fe9a33198dd5b12 with both findings
Task 3: fix round 1/5 (2 addressed, 0 open — comment accuracy, test moved to UnitTests/Storage; commits 5e94714..264d7f2)
Task 3: complete (commits 74fc9e5..264d7f2, review clean)
Plan amended: Tasks 4 and 5 re-authored for main's post-refactor shapes (commit edf6aac)
Task 4: implemented (commits edf6aac..b2ea330) — implementer a5bb8661fdb6bb3b1; review: spec OK, quality approved with 1 Important (strategy word has two sources of truth: explicit arg on the ILogger path, re-derived from TargetCount.HasValue in the DiagnosticSource message generator) + 1 Minor (eager plan.Strategy.ToString(), not worth changing)
Task 4: Important finding is plan-mandated (my amendment chose not to widen the event data with a Strategy field) — escalated to human per skill
Task 4: fix round 1/5 (1 addressed, 0 open — Strategy now carried on the event data as string; TargetCount.HasValue derivation deleted repo-wide; commits b2ea330..66d6794)
Task 4: complete (commits edf6aac..66d6794, review clean)
Task 4: minor (deferred): eager plan.Strategy.ToString() at 8 call sites — reviewer judged not worth changing
Task 4: minor (deferred): implementer's 'reintroduced the bug to confirm the test fails' claim is asserted but not evidenced with command/output; re-reviewer independently traced the DiagnosticSource path and judged the test genuine
Task 4: note: my fix message said 'six event-data construction sites'; the real count is 4 — my imprecision, not an implementer error
Task 4: NEW INTERFACE for later tasks: tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/DiagnosticSourceSpy (static Create(), ConcurrentBag of captured events) — asserts the DiagnosticSource payload path, which SpyLoggerProvider cannot reach. Task 23 planned to build this; it now exists.
Task 5: implemented (commits 66d6794..d7ff744) — implementer aa0b0a306339ad27d; review (opus): spec OK with 2 deviations, quality NOT APPROVED — 4 Important + 7 Minor
Task 5: minor (deferred): #7 phase-spanning comment present on 2 of 4 ExecuteTwoPhase* methods, duplicated verbatim between the two sync ones
Task 5: minor (deferred): #8 SingleEntityDbContext sensitiveDataLogging uses an if-block; GuidesDbContext.cs:41 idiom is inline .EnableSensitiveDataLogging(flag). Parameter is strictly redundant (optionsBuilderAction already worked) but a readability win
Task 5: minor (deferred): #9 payload-selection predicates in BulkOperationsCommandTests are structural coincidences (TargetCount==null && DeleteCount==0 would also match Executed on a zero-match delete); Command != null states the intent. Same shape pre-exists at ExecuteDeleteTests.cs:292
Task 5: minor (deferred): #10 Assert.DoesNotContain("\"Status\"") keys on escaped-quote form; plain "Status" is strictly stronger and safe
Task 5: minor (deferred): #11 fourth test builds a SpyLoggerProvider it discards to satisfy a required LoggerFactory
Task 5: reviewer note (no action): CombineBulkCommand is Task 18's scope and correctly absent here
Task 5: reviewer note: gate is stronger than the read path it copies — MongoQueryEventData stores raw MQL on a public property and gates only at render time, so a subscriber can read unredacted MQL off it today. Worth a separate ticket.
Task 5: fix round 1/5 IN FLIGHT — resumed aa0b0a306339ad27d with Deviation A + Findings 1,2,3,4 (+ Minors 5,6 bundled as same edits)
Task 5: fix round 1/5 (6 addressed, 0 open — assertions restored, 8/8 closed-gate coverage via Theory, deterministic Elapsed guard, lazy Func<string> rendering, docs corrected; commits d7ff744..8f8ec3c)
Task 5: complete (commits 66d6794..8f8ec3c, review clean)
Task 5: minor (deferred, NEW from fix): LogSensitiveData XML doc now inaccurate on Executed payloads — they hardcode false after the Minor-5 fix. Same defect class as Finding 4. One sentence to fix, on a PUBLIC type
Task 5: minor (deferred, NEW from fix): 3 redundant usings in BulkOperationsCommandTests.cs (ImplicitUsings covers System, System.Linq, System.Threading.Tasks); only MongoDB.Driver is needed
Task 5: minor (deferred): Elapsed doc's two-phase half omits that phase-1 TRANSLATION is inside the window, not just the read
Task 5: minor (deferred): the laziness itself is untested — nothing fails if renderCommand() moves back above the early return. A counting Func<string> + no-listener logger would pin it
Task 5: EVIDENCE GAP for final review: fix round did not re-run NorthwindBulkUpdates (181 tests) or the full functional/spec suites, which round 1 did. Shared logging signature + all 8 executor call sites changed. FINAL REVIEW MUST RE-ESTABLISH THE FULL BASELINE.
Task 5: guard strength note: the Elapsed>0 guard is placement-sensitive — it catches StartNew() returning to just before the Executing call, but would pass if a regression put it at the top of *ByIds. Inherent to the formulation.
Task 6: complete (commits 8f8ec3c..5dee869, review clean) — implementer a2cdb46473f9c6f76
Task 6: minor (deferred): UpdateLogger's XML doc gives no cue that the property has zero consumers on EF8 (bulk is #if !EF8) — reads as dead code there
Task 6: minor (deferred) NEEDS A DECISION: MongoQueryContextFactory's public ctor also gained a required 3rd param — a SECOND public API break alongside MongoQueryContext's. Repo AGENTS.md has a strict no-non-major-public-API-breaks policy. Both types are DI-constructed (sole call site is the factory's own Create()), so practical risk is nil, but may need a BREAKING-CHANGES entry.

=== PHASE 1 COMPLETE (Tasks 1-6) ===
Task 6: fix round 1/5 (1 addressed, 0 open — BREAKING-CHANGES.md entry for both ctor breaks; commits 5dee869..f468333)
Task 6: complete (commits 8f8ec3c..f468333, review clean)

=== PHASE 2 START (Tasks 7-9, data integrity) ===
Task 7: implemented (commits f468333..cd0111e) — implementer af1261333c8265ba6; review: spec OK, quality approved with 1 Important (GetSetElementKey doc doesn't disclose its path branch is now unreachable) + 3 Minor
Task 7: reviewer independently verified via decompiled EF Core: IsPrimaryKey() covers composite parts + shadow keys; IsConcurrencyToken covers IsRowVersion(); discriminator comparison is robust to derived-type-rooted queries by reference equality; guard order PK->discriminator->token is right
Task 7: reviewer confirmed 181/181 Northwind pass is CORRECT not suspicious — grepped the real NorthwindBulkUpdatesTestBase: every SetProperty target is an ordinary scalar, no discriminator hierarchy or concurrency tokens in that fixture
Task 7: minor (deferred): brief's Files list didn't anticipate BulkOperationsCommandTests.cs, but the global constraint allows 'and tests' — compliance nit only
Task 7: fix round 1/5 IN FLIGHT — resumed af1261333c8265ba6 with Finding 1 + Minors 2,3 bundled
Task 7: fix round 1/5 (3 addressed, 0 open — unreachability documented, test limits commented, derived-type discriminator test via Where(a => a is Cat) since OfType is still classifier-rejected until Task 15; commits cd0111e..6d4bc8e)
Task 7: complete (commits f468333..6d4bc8e, review clean)
Task 7: note: implementer used throwaway _TEMP_* probe tests to determine OfType-vs-is behaviour then removed them; the shipped evidence is the committed test, so this is fine (unlike Task 5 round 0, where a deleted scratch test WAS the evidence)
Task 7: OPEN PRE-MERGE DECISION: concurrency-token guard is implemented + tested. Relational EF permits setting tokens. To back out: delete the IsConcurrencyToken block in BuildSetter + its test, and record the decision in the README bullet.
Task 8: implemented (commits 6d4bc8e..1307e7a) — implementer a990cd49929be54b9; review: spec OK, quality approved with 2 Important + 2 Minor (all four inherited from the plan's own Step-1/Step-3 text, not implementer deviations)
Task 8: KEY FINDING — the two-entity-parameter shape is LIVE-REACHABLE via a same-collection self-referencing navigation (StaffMember.DirectReports/ManagerId + SetProperty(e => e.Rank, e => e.DirectReports.Max(r => r.Rank))). Pre-fix it silently rendered a NO-OP instead of the true max. This was a real silent-wrong-write bug, not a hypothetical.
Task 8: but that claim rests on a deleted, uncommitted probe test — same pattern criticised in Task 5. Reviewer judged the criticism materially applies. Fix round adds the committed functional regression test.
Task 8: fix round 1/5 IN FLIGHT — resumed a990cd49929be54b9 with Findings 1,2 + Minors 3,4
Task 8: fix round 1/5 (4 addressed, 0 open — committed functional reachability test, message now names the property + offers alternatives, both unit tests strengthened; commits 1307e7a..1c2a44d)
Task 8: complete (commits 6d4bc8e..1c2a44d, review clean)
Task 8: re-reviewer confirmed the new functional test drives the real shape via public ExecuteUpdate and would fail as 'expected throw, got success' with the guard removed — i.e. it detects the guard's absence, not an unrelated symptom. EF9 isolated run empirically confirms no EF-version gate needed.
Task 8: minor (deferred): report prose mis-cites RejectSetterTarget as being in the same file (it is in MongoQueryableMethodTranslatingExpressionVisitor.cs) — prose only, no code impact

NEXT: Task 9 (FindProperty(MemberInfo), SetProperty declaring-type match, duplicate-setter rejection) — brief paths are Query-side and accurate, no remap needed. Consumes RejectSetterTarget from Task 7.

DECISION 2026-07-30 (Arthur): concurrency-token setters are PERMITTED — valid use case, relational EF allows it.
  - Plan amended to record it (commit 832ad8b); Task 7's judgement-call block now marked RESOLVED with a do-not-reinstate note.
  - Task 7 back-out IN FLIGHT — resumed af1261333c8265ba6: remove the IsConcurrencyToken guard, replace the rejection test with a positive 'can set a concurrency token' test, correct README:98 to say bulk ops neither check NOR protect tokens.
  - Task 25 (README rewrite) MUST preserve the 'neither checked nor protected' wording.
Task 7 follow-up: complete (commits 832ad8b..593c6f9, review clean) — IsConcurrencyToken guard removed, replaced by positive test ExecuteUpdate_can_set_a_concurrency_token asserting the PERSISTED value via a third fresh DbContext; README:98 now reads 'concurrency tokens are neither checked nor protected — a bulk update may set one explicitly'. Northwind re-confirmed 181/181. Task 7 now ships TWO setter guards: primary key + discriminator.
Task 9: complete (commits 593c6f9..a6a9ea4, review clean, no fix round) — implementer a7933e59b9cb77690
Task 9: reviewer verified Risk A is unreachable: SetProperty is a fluent INSTANCE method on SetPropertyCalls<TSource>, so every call in a chain shares the same closed-generic declaring type — the tightened pattern is all-or-nothing, no silent partial parse possible
Task 9: reviewer verified Risk B non-vacuous: the two TranslateExecuteUpdate bodies are #if EF10/#else mutually exclusive, so running the one test under each configuration genuinely exercises both paths
Task 9: minor (deferred): no test exercises a MemberInfo/name DIVERGENCE — the exact case Item 1 exists to fix. Real coverage gap, disclosed by the implementer
Task 9: minor (deferred): TDD order deviation — test and fix authored in one pass, no red-run transcript (disclosed)
Task 9: minor (deferred): RejectDuplicateSetters duplicates the DisplayName()/Name message formatting from RejectSetterTarget

=== PHASE 2 COMPLETE (Tasks 7-9) ===
NEXT: Phase 3 = Tasks 10, 11, 12 (fail-closed correctness). ALL THREE NEED THE STORAGE REMAP: RunInBulkTransaction, SafeRollback*, GetBulkCollectionAndSession, the six Execute* methods and MaterializeIds* are in Storage/MongoBulkOperationExecutor.cs, NOT the query visitor. Task 11 also depends on Task 12's SafeRollbackAsync signature — see the plan's self-review note.
Task 10: implemented (commits a6a9ea4..24e1c27) — implementer ac3dbf8747b543803; review: spec OK, quality approved, 2 Minor only
Task 10: reviewer verified the predicate closes the hole exactly — (CurrentTransaction as MongoTransaction)?.Session ?? throw requires a non-null Session, so a MongoTransaction with a null Session also throws; and grep confirmed the four *ByIds methods are reachable ONLY via the two RunInBulkTransaction* bodies, so no bypass exists
Task 10: reviewer independently confirmed IDbContextTransaction has 14 members on all three pinned EF versions (plan's stub listed 7) — plan defect, handled correctly by the implementer
Task 10: minor -> ESCALATED TO FIX ROUND: async fast path has no negative test; only the sync guard is proven to fire. Same shape as Task 5's '1 of 8 call sites' finding, which was Important. Async is the more commonly used path.
Task 10: minor (deferred): report overstates its pre-fix evidence — 'one document was in fact deleted' is inferred, not shown, since Assert.Throws short-circuits before the count assertion. Asking for the wording to be corrected.
Task 10: fix round 1/5 IN FLIGHT — resumed ac3dbf8747b543803
Task 10: fix round 1/5 (2 addressed, 0 open — test converted to [Theory] covering sync+async with Assert.ThrowsAsync; pre-fix behaviour MEASURED not inferred; commits 24e1c27..e5b54ae)
Task 10: complete (commits a6a9ea4..e5b54ae, review clean)
Task 10: MEASURED pre-fix behaviour: with a foreign ambient transaction, BOTH ExecuteDelete() and ExecuteDeleteAsync() two-phase silently deleted exactly 1 document. Guard temporarily disabled to measure, restored via git checkout --, nothing from the experiment committed; re-reviewer verified the committed guard is unchanged (helper at :311, calls at :325 and :371)

NEXT: Task 11 (translate before opening the transaction; give phase 1 the canonical failure). NEEDS STORAGE REMAP. Also: Task 11 restructures RunInBulkTransactionAsync while Task 12 changes SafeRollbackAsync's signature — executed in order, Task 11 must KEEP the CancellationToken parameter and Task 12 drops it. Task 11 Step 5 is already done in main (BuildFilter fail-loud guard exists) — see the plan's reconciliation section.
Task 11: implemented (commits e5b54ae..7f6608e) — implementer a0dfd3b9682a21359; review (opus): spec OK, quality NOT APPROVED — 1 Critical + 2 Important + 4 Minor
Task 11: CRITICAL — MaterializeIds/Async copied TranslateBulkOrThrow's 'catch everything that is not InvalidOperationException' filter to an I/O site, so OperationCanceledException and driver errors (WriteConflict, SnapshotUnavailable, timeouts, network) from the phase-1 READ get relabelled 'could not be translated'. Reintroduces at a new site exactly the mislabelling class this task exists to remove.
Task 11: IMPORTANT — the try-split makes IsTransactionsUnsupported's arm 2 ('Transaction numbers are only allowed...') DEAD: it is a server error on a command carrying a txnNumber, so it can only arise from the body, which the restructure stopped mapping. Consequence: on some non-transactional topologies the user now gets a raw MongoCommandException instead of StandaloneTransactionError. Only covering test is [Fact(Skip)].
Task 11: IMPORTANT — no regression test for the defect the task fixes; the working phase-1 reproducer was deleted again (3rd occurrence of this pattern on this branch)
Task 11: reviewer independently CONFIRMED the 3 conformance reclassifications are legitimate tightening: AssertTranslationFailed is strictly stronger (exact-type ThrowsAsync + message contains), and the sources hit ClassifyBulkSource's default arm at query-compile time so they never produce a MongoBulkPlan at all — genuinely a pre-existing stale tag, unaffected by this task
Task 11: reviewer confirmed docs/failing-spec-tests.md now contradicts 3 test tags in 3 places (:84 count 46, :113 listing, :174 parenthetical) — Task 25 must reconcile; also the doc's stated CAUSE for those three is simply wrong
Task 11: fix round 1/5 IN FLIGHT — resumed a0dfd3b9682a21359 with Critical 1, Important 2, Important 3, Minors 4-6
Task 11: fix round 1/5 (6 addressed, 0 open — catch narrowed to ExpressionNotSupportedException only; narrow body-side re-map restored filtered on IsTransactionsUnsupported; reproducer + cancellation companion committed; Minors 4-6 done; commits 7f6608e..b02e480)
Task 11: complete (commits e5b54ae..b02e480, review clean)
Task 11: VERIFIED IN DRIVER SOURCE — MongoQuery.GetEnumerator() is NOT an iterator: it calls Execute() -> ExpressionToExecutableQueryTranslator.Translate() -> provider.Execute(), so translation AND the initial find happen eagerly inside GetEnumerator(); MoveNext() only pulls later getMore batches. Async: ToListAsync = ToCursorAsync + cursor.ToListAsync, and ToCursorAsync translates before any I/O — so cursor iteration cannot produce ExpressionNotSupportedException and spanning translation+iteration in one try is harmless.
Task 11: IMPORTANT TAXONOMY NOTE for future work — ExpressionNotSupportedException DERIVES FROM NotSupportedException, and IsTransactionsUnsupported's arm 2 matches NotSupportedException + a message substring. The type alone does not separate them; only the message check does.
Task 11: minor (deferred, NEW from fix): the 'two independent reasons' comment at MongoBulkOperationExecutor.cs:428-433 is overstated — reason (1) alone does not exclude a raw driver LINQ failure from arm 2, because of the inheritance above
Task 11: minor (deferred, NEW from fix): narrowing means a materialization-site translator failure of a type OTHER than ExpressionNotSupportedException would escape as a raw driver exception rather than the canonical message (TranslateBulkOrThrow's own comment mentions ArgumentException as a driver-LINQ failure mode)
Task 11: minor (deferred): cancellation test discriminates by construction; never run against the broad filter to confirm. If an EF/driver pre-flight check later observes cancellation earlier, it silently stops discriminating
Task 11: FOR FINAL REVIEW — ExecuteDelete_two_phase_on_standalone_throws_actionable_error is still an empty [Fact(Skip)] placeholder, so IsTransactionsUnsupported's WIRING (both arms) is unpinned in the default run. That is how the dead arm shipped unnoticed in the first place.

NEXT: Task 12 (cancellation cleanup) — drops SafeRollbackAsync's CancellationToken param (Task 11 deliberately kept it), adds exception filters to both SafeRollback helpers, and adds a cancellation test. Storage remap applies.

================================================================
RESUME HERE (written 2026-07-31, before a context clear)
================================================================
State: Tasks 1-11 complete. 23 commits on EF-361, head b02e480, off main @ 58e05a0.
Nothing in flight. Task 12 was NOT dispatched.

HOW TO RESUME: read this whole ledger + docs/superpowers/plans/2026-07-30-ef-107-bulk-remediation.md
(especially its "Reconciliation with main" section, and the AMENDED/RESOLVED blocks inside Tasks 4, 5 and 7).
Trust this ledger and `git log` over recollection. Use superpowers:subagent-driven-development.

CONTROLLER DUTIES THAT MUST NOT BE LOST:
1. THE STORAGE REMAP. Plan task bodies cite Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs
   for execution code. Execution actually lives in Storage/MongoBulkOperationExecutor.cs:
   RunInBulkTransaction(+Async), SafeRollback(+Async), RequireBulkSession, IsTransactionsUnsupported,
   NoAutoTransactionError, StandaloneTransactionError, GetBulkCollectionAndSession, GetUpdateLogger,
   the six Execute* methods, the four *ByIds, MaterializeIds(+Async), RenderBulkCommand, CombineBulkCommand.
   Translation stays in Query: CreateBulkPlan, TranslateBulkOrThrow, BuildBulkTranslationFailure,
   BuildFilter, BuildUpdate, BuildIdDocumentQuery, GetSetElementKey, SerializeConstant,
   RenderSelfReferencingValue, EvaluateToConstant, RebindSetterValueParameters.
   Still need the remap: Tasks 16, 17, 18 (16/17 touch BuildIdDocumentQuery in Query + MaterializeIds in Storage).
2. Carry global constraints verbatim into every dispatch: three-config build (EF8/EF9/EF10); bulk code
   inside #if !EF8; MongoEventId append-only; redaction single-pointed (ShouldLogSensitiveData gate in
   RenderBulkCommand); don't "fix" the Query<->Storage bulk seam (both AGENTS.md files say so);
   MONGODB_URI/ATLAS_URI unset so TestContainers boots atlas-local (image already pulled).
3. Run NorthwindBulkUpdatesMongoTest on EF9 AND EF10 for any task touching classification or setters.
   Baselines: 181/181 EF10, 169/169 EF9.
4. Tell implementers to COMMIT reproducers, never cite a deleted scratch test as evidence. This had to be
   said three times (Tasks 5, 8, 11).

TASK 12 DESIGN WORK ALREADY DONE (do not redo):
 - SafeRollback is at Storage/MongoBulkOperationExecutor.cs:504; SafeRollbackAsync at :510 (still takes a
   CancellationToken, deliberately left by Task 11 for Task 12 to remove); sole async call site is :485.
 - THE PLAN'S STEP 1 TEST IS NEARLY VACUOUS: asserting "data unchanged" passes with or without the fix,
   because nothing was written anyway. Do not rely on it.
 - USE THE DIAGNOSTICS INSTEAD: MongoEventId.TransactionRollingBack / TransactionRolledBack exist
   (MongoEventId.cs:145,154). With the bug, RollbackAsync faults on the cancelled token before the rollback
   completes, so TransactionRolledBack never fires; with the fix it does. Assert on that via
   SpyLoggerProvider.Create() / GetLogMessageByEventId, copying the shape of
   ExecuteUpdate_two_phase_auto_transaction_logs_transaction_lifecycle in ExecuteUpdateTests.
 - Require the implementer to run the new test against UNFIXED code and quote what it saw.
 - DO NOT duplicate ExecuteUpdateTests.cs:348
   ExecuteUpdateAsync_two_phase_propagates_cancellation_unchanged_during_phase_1_materialization (added by
   Task 11) — that covers exception CLASSIFICATION, not rollback completion. Name Task 12's test for
   rollback/cleanup so the distinction is obvious.
 - Commit keeps its cancellation token; only CLEANUP ignores it. DisposeAsync takes none.

OPEN ITEMS NEEDING NO FURTHER INPUT (decided):
 - Concurrency-token setters are PERMITTED (Arthur, 2026-07-30). Two setter guards only: PK + discriminator.
   Task 25's README rewrite must keep "neither checked nor protected".

OPEN ITEMS TO RAISE WITH ARTHUR AT SOME POINT (not blocking):
 - MongoQueryEventData stores raw MQL on a public property and gates only at render time, so a
   DiagnosticSource subscriber can read unredacted MQL today. PRE-EXISTING, outside this plan. Suggest a
   separate ticket rather than widening EF-361.

================================================================
=== PHASE 3 (cont.) — Task 12 ===
Task 12: housekeeping — a controller-authored draft test (43 lines in ExecuteDeleteTests.cs) was found
  UNCOMMITTED in the working tree at resume. Controller-authored code skips review and would have robbed the
  implementer of the run-against-unfixed-code step, so it was parked to
  task-12-controller-draft-DISCARDED.diff and reverted. Tree clean at b02e480 before dispatch.
Task 12: round 0 BLOCKED at RED (implementer ac6b87f6999471ef2, sonnet) — the diagnostics signal from the
  RESUME block DOES NOT DISCRIMINATE. Test passed against unfixed code. Nothing committed, tree clean.
Task 12: PLAN DEFECT — mechanism wrong, conclusion right. VERIFIED IN DRIVER SOURCE (~/code/driver2, 3.10.0):
  - CoreSession.AbortTransaction(Async) wraps the abort in `catch { /* ignore all errors per spec */ }`
    (Core/Bindings/CoreSession.cs:189-227). So the fault NEVER reaches the provider's SafeRollbackAsync
    `catch {}` — the brief's stated mechanism ("faults immediately, swallowed by the bare catch{}") is wrong,
    and MongoTransaction.RollbackAsync's own catch is unreachable via this path. TransactionRolledBack
    (MongoTransaction.cs:213) therefore fires with a cancelled token too => useless as a signal.
  - BUT the harm is real: RetryableWriteOperationExecutor.ExecuteAsync calls
    operationContext.ThrowIfTimedOutOrCanceled() at :49, BEFORE the wire write, so with an already-cancelled
    token the abortTransaction command is never sent. And CoreSession's unconditional
    `finally { SetState(Aborted) }` then disables the dispose-time safety net at CoreSession.cs:367
    (`AbortTransaction(CancellationToken.None)`, gated on state Starting/InProgress). Net: the transaction is
    NOT aborted and lingers until transactionLifetimeLimitSeconds holding locks. Step 3 IS a real fix.
  => The signal must live at the COMMAND layer, not the ILogger layer.
DECISION 2026-07-31 (Arthur), two calls on Task 12:
  1. TEST = command monitoring. Inject an IMongoClient whose MongoClientSettings.ClusterConfigurator captures
     CommandStartedEvent; assert an `abortTransaction` command was sent. Absent with bug, present with fix.
     Feasible via the existing UseMongoDB(IMongoClient, ...) overload; ConnectionTests.cs and
     Query/SharedClrTypeProjectionTests.cs already inject clients. A small reusable helper is in scope.
  2. CATCH FILTER = drop StackOverflowException, keep OutOfMemoryException. SOE is uncatchable in .NET Core,
     so the plan's verbatim filter contains dead code. This OVERRIDES the brief's Step 3 code block.
Task 12: NEW TEST INFRA to reuse in later tasks (esp. 20-23) once it lands: the CommandStartedEvent capture
  helper. Any later task needing "was command X actually sent?" should use it rather than ILogger assertions.
Task 12: implemented (commits b02e480..19ea8fe) — implementer ac6b87f6999471ef2 (sonnet); review (opus):
  spec OK against all 3 amendments, quality APPROVED with 1 Important + 5 Minor.
Task 12: THIRD test redesign, by the implementer, and it is CORRECT: a PRE-cancelled token means phase 1 never
  reaches the wire, so no transaction is ever started server-side and the driver legitimately skips
  abortTransaction even WITH the fix. So the test cancels from a CommandSucceededEvent hook AFTER phase 1
  succeeds. Test name: ExecuteDeleteAsync_two_phase_cancelled_after_phase_1_still_aborts_transaction_on_the_wire.
Task 12: reviewer independently verified the ordering is HAPPENS-BEFORE, not a timing window:
  CommandEventHelper.ProcessCommandResponseMessage (driver2 Core/Connections/CommandEventHelper.cs:364) is
  called from BinaryConnection.cs:464, one statement before `return message` at :465 — so the subscriber runs
  synchronously on the receive path, strictly before phase 1's result unwinds. No Task.Delay race. Reviewer also
  confirmed RED output ["aggregate","aggregate","isMaster"] vs GREEN abortTransaction+txnNumber:1 isolates the
  fix causally, and that there is no plausible path to a false PASS.
Task 12: ⚠️ ("EF8 build unverified") RESOLVED BY CONTROLLER, not a real gap:
  `dotnet build -c "Debug EF8"` => Build succeeded, 0 Warning(s), 0 Error(s).
  New test also run under `Debug EF9` => 1 passed. CommandCaptureSpy is unguarded but EF-version-agnostic.
Task 12: fix round 1/5 (6 addressed, 0 open — ClusterConfigurator +=, using var spyClient, abortTransaction
  SUCCEEDED assertion via CommandSucceededEvent, Interlocked.Exchange flags, IReadOnlyCollection, reentrancy
  comment; commits 19ea8fe..eb28d0d)
Task 12: complete (commits b02e480..eb28d0d, review clean)
Task 12: re-reviewer confirmed the discriminating assertion is now STRICTLY STRONGER, not merely reshuffled —
  CommandSucceededEvent only fires on a server-acknowledged success, so a wire-failed-but-driver-swallowed abort
  leaves abortTransactionSucceeded at 0 and fails. RED re-confirmed after the polish edits.
Task 12: report had a FACTUAL ERROR caught by review — it claimed MongoClient is not IDisposable. It is
  (IMongoClient : IDisposable, MongoClient.Dispose()). Retracted in the report. Reminder that report prose is
  claims, not evidence.
Task 12: LESSON FOR THE FINAL REVIEW — this task's test design was wrong THREE times (vacuous count assertion;
  TransactionRolledBack, defeated by the driver's per-spec error swallowing; pre-cancelled token, which means no
  transaction is ever started so abortTransaction is skipped even WITH the fix). Only the 4th shape discriminates.
  Tasks 5, 8, 11 and 12 all had test-falsifiability defects. THE FINAL REVIEW SHOULD AUDIT TEST FALSIFIABILITY
  AS A NAMED CONCERN, not as one finding among many: for each new test on this branch, ask what would have to
  break for it to fail.

Task 13: implemented (commits eb28d0d..c8c7d2d) — implementer a513519d48f8abee5 (sonnet); review (opus):
  spec OK against all 4 amendments, quality APPROVED with 1 Important + 5 Minor. Northwind 181/181 EF10,
  169/169 EF9 (both baselines matched). EF8/EF9/EF10 all build clean.
Task 13: CONTROLLER CORRECTIONS carried into the dispatch (all four verified first):
  (1) brief's Files list named only the method-translating visitor; BuildFilter is in
      MongoShapedQueryCompilingExpressionVisitor.cs — TWO source files, not one.
  (2) brief's Step 5 test was VACUOUS: seed is {open,10},{open,20},{closed,30} and its predicate
      Quantity > 1 matches both open orders, so the test passed even if the second Where were dropped.
      Corrected the PREDICATE to Quantity > 15 (keeping the brief's 1-deleted/2-remaining assertions);
      the brief's own instruction to "adjust the counts" would have preserved the vacuity.
  (3) brief wrongly said CreateSeededContext takes no arguments — it has [CallerMemberName]; the new
      ILoggerFactory? had to go FIRST or every new caller silently shares one collection.
  (4) the :474 invariant comment became false and had to be rewritten.
Task 13: my imprecision — I passed along the brief's "keep the Debug.Assert on the strategy" without checking;
  no such assert exists. Implementer correctly reported this and gave a structural argument instead, which the
  reviewer verified: CreateBulkPlan sets BuildFilter = isTwoPhase ? null : ..., and plan.BuildFilter! is
  dereferenced only in the four SingleCommand arms. No assert needed.
Task 13: reviewer VERIFIED THE EQUIVALENCE CLAIM (the crux) and closed the one hole it found: name-based
  matching would admit Queryable.Distinct(IQueryable<T>, IEqualityComparer<T>), which is NOT a no-op and would
  silently delete the un-deduplicated set. Unreachable, because ClassifyBulkSource runs only after EF's own
  translation succeeded and EF gates Distinct on the canonical 1-arg QueryableMethods.Distinct, so the comparer
  overload fails translation first. Task 14's canonical-MethodInfo migration closes it permanently.
Task 13: reviewer independently confirmed the Step 5 test discriminates in BOTH directions (dropping the inner
  OR the outer Where both yield 2 deleted vs the asserted 1) — stronger than the amendment required.
Task 13: ⚠️ ("README:99-100 now stale") RESOLVED BY CONTROLLER, not a real gap: Task 25's plan text ALREADY
  rewrites these exact bullets — it moves OrderBy/ThenBy/Distinct into the single-command bullet ("which do not
  affect which documents are matched") and Skip/Take/OfType/Cast into the two-phase bullet. Task 25 must also
  still keep the "neither checked nor protected" concurrency-token wording (see 2026-07-30 decision above).
Task 13: !!! DIRECTIVE FOR TASK 15 !!! The admitted-operator set is now DUPLICATED in two switches
  (MongoQueryableMethodTranslatingExpressionVisitor.cs:427-441 and MongoShapedQueryCompilingExpressionVisitor.cs
  :477-501). OrderBy/ThenBy/Distinct were added to BOTH because they are affected-set-neutral. OfType/Cast are
  NOT neutral — they narrow the affected set via a discriminator filter. Task 15 MUST NOT add OfType/Cast to
  BuildFilter's skip list the way this task added the ordering operators; doing so would silently delete
  documents of the wrong subtype. Reviewer suggests a shared IsAffectedSetNeutral(string) predicate near
  MongoNonQueryExpression.BulkOperatorNames (:60) to make the distinction un-copyable.
Task 13: minor (deferred): the two-switch duplication itself (see the Task 15 directive above for why it is
  live scope rather than cosmetic).
Task 13: minor (deferred): ExecuteDelete_with_OrderBy_Take_still_uses_the_two_phase_path is redundant —
  ExecuteDeleteTests.cs:492 already asserts Strategy=TwoPhase for the identical shape plus Targets=2 and the
  DiagnosticSource payload. Plan-mandated; served its purpose as the brief's Step 2 control.
Task 13: fix round 1/5 (4 addressed, 0 open — three user-facing messages narrowed to "uses paging
  ('Skip'/'Take')", LoggerFactory? typing removes the downcast, bare-Skip + ThenBy-mid-chain tests added,
  report probe description corrected; commits c8c7d2d..9379382)
Task 13: complete (commits eb28d0d..9379382, review clean)
Task 13: implementer's own sweep found a 4th stale site beyond the three named —
  tests/.../SpecificationTests/Query/NorthwindBulkUpdatesMongoTest.cs:33-36. Re-reviewer grepped src/ and tests/
  for "ordering, paging" / "paging, or" / "uses ordering" and found nothing left. ThrowBulkSourceNotSupported
  (MongoQueryableMethodTranslatingExpressionVisitor.cs:491-496) correctly left alone — it lists what is
  supported at all, not a transaction/key claim.
Task 13: Northwind re-confirmed after the Storage message change: 181/181 EF10, 169/169 EF9.

Task 14: implemented (commits 9379382..67c8cca) — implementer a2eda9bf24569458f (sonnet), returned
  DONE_WITH_CONCERNS; review (opus): spec ❌ (one false doc claim), quality NEEDS FIXES — 1 Important + 4 Minor.
  Northwind 181/181 EF10, 169/169 EF9; no conformance test changed status; all 3 configs build clean.
Task 14: CONTROLLER CORRECTIONS carried into the dispatch: (1) the brief omitted BuildFilter, and admitting
  Order/OrderDescending in the classifier ALONE would have made a single-command Order() chain throw at
  BuildFilter's default arm — a correctness bug the brief created; (2) the brief's IsOrderingOperator name is
  wrong (the set contains Distinct); required ONE shared accurately-named set consulted by both switches, with
  BuildFilter converted to definition matching too; (3) replaced the brief's throwaway Debug.Assert with durable
  evidence + a pinning unit test; (4) settle Order()'s reachability empirically before writing tests around it.
Task 14: !!! BOTH OF THE PLAN'S STEP-1 TESTS ARE NON-DISCRIMINATING, AND THE TASK'S PREMISE WAS NEVER LIVE !!!
  Implementer discovered and reviewer INDEPENDENTLY CONFIRMED against an EF Core checkout (~/code/efcore):
  - Order()/OrderDescending() never reach provider code: EF's QueryableMethodNormalizingExpressionVisitor
    .TryNormalizeOrderAndOrderDescending (v9.0.16 :529-547; release/10.0 :554) rewrites them to
    OrderBy(x => x)/OrderByDescending(x => x) UNCONDITIONALLY for all source types during preprocessing.
    (The implementer's report misattributes this to NavigationExpandingExpressionVisitor and to entity-typed
    sources; the reviewer corrected both. EF 8.0.x has no such normalizer — irrelevant, bulk is EF9+.)
  - Indexed Where / OrderBy(keySelector, IComparer) / Distinct(IEqualityComparer) are rejected by EF Core for
    EVERY provider before provider code runs: NavigationExpandingExpressionVisitor guards each case with
    `when genericMethod == QueryableMethods.X` and its default arm throws CoreStrings.TranslationFailed, with a
    comment enumerating exactly these hazards (v9.0.16 :650, release/10.0 :683; second throw at :803).
  => The brief's premise (name matching admits the indexed Where, and BuildFilter then builds a lambda with an
     unbound index parameter) WAS NEVER REACHABLE. Task 14 changes no reachable behaviour. Its real value is
     convention alignment with Query/AGENTS.md's MethodInfo pitfall + removing a latent trap if EF ever loosens
     nav expansion. DO NOT let the changelog or PR description imply a user-visible fix.
  => The only test that can fail from this task's own code is the pinning unit test. Substantive regression
     coverage for both rewritten switches is the PRE-EXISTING ExecuteDeleteTests.cs:231/248/473 trio + Northwind.
Task 14: reviewer confirmed the shared set is a REAL single source of truth (one definition site at
  MongoNonQueryExpression.cs:111-130, consumed at MongoQueryableMethodTranslatingExpressionVisitor.cs:436 and
  MongoShapedQueryCompilingExpressionVisitor.cs:496), that the neutral branch still falls through to
  `expression = call.Arguments[0]` so inner Wheres are still collected, that Where is correctly NOT in the
  neutral set, and that OfType/Cast are correctly absent (Task 15's scope).
Task 14: minor (deferred): responsibility drift — an expression NODE class now hosts a reflection-built operator
  table (MongoNonQueryExpression.cs grew 87->137 lines). Reviewer judged it defensible; a separate
  BulkSourceOperators static class in the same folder would read better. Not worth churn now.
Task 14: fix round 1/5 (4 addressed, 0 open — set now built from the 7 canonical QueryableMethods constants,
  IReadOnlySet, #if !EF8 guard on the set + helper, XML doc corrected, pinning test re-pointed at the constants
  and the tautological one dropped, both misattributed comments fixed; commits 67c8cca..2d8c40e)
Task 14: complete (commits 9379382..2d8c40e, review clean)
Task 14: EF8 guard EMPIRICALLY VERIFIED — build fails without it with
  "error CS0117: 'QueryableMethods' does not contain a definition for 'Order'", passes with it; and
  AffectedSetNeutralDefinitionsTests is correctly absent from the EF8 unit run (283 vs 297 EF9 / 290 EF10).
Task 14: minor (deferred, NEW from fix): MongoNonQueryExpression.cs:93 XML doc now claims "this whole type is
  #if !EF8" — FALSE. Only the AffectedSetNeutralDefinitions/IsAffectedSetNeutral member (:84-128) is guarded;
  the class itself (:31-135) compiles unconditionally on EF8 and is merely never instantiated there (all three
  `new MongoNonQueryExpression(...)` sites are inside the method-translating visitor's own #if !EF8).
  4-word fix ("this whole type" -> "this member"). Same defect CLASS as the Important finding that prompted the
  rewrite, which is why it is worth the final review's attention rather than silent discard.
Task 14: re-reviewer confirmed no unguarded code references guarded members (both consumers sit inside their own
  #if !EF8 regions: method-translating visitor :188-501, shaped-query visitor :392-763), the set is exactly the
  7 intended members with Where/OfType/Cast absent, and BuildFilter still collects every Where across neutral
  operators on both sides.

Task 15: implemented (commits 2d8c40e..b36e3b9) — implementer a8c3876e5e0ee50cf (sonnet), DONE_WITH_CONCERNS;
  review (opus): spec ❌ (incomplete stale-prose sweep), quality NEEDS FIXES — 3 Important + 4 Minor.
  Northwind 181/181 EF10, 169/169 EF9; all 3 configs build clean; BulkOperationsInheritanceTests 5/5.
Task 15: !!! CAST IS NOT ADMITTED — PLAN DEVIATION, RATIFIED BY CONTROLLER !!! The brief asked for OfType AND
  Cast on the premise that both are "type scoping expressed as a {_t: ...} filter". FALSE for Cast:
  - TranslateCast is `=> ReshapeShaperExpression(source, castType)` (MongoQueryableMethodTranslating
    ExpressionVisitor.cs:627-628), inside the literal "#region Methods that just require shaper reshaping".
    TranslateOfType by contrast does source.UpdateShaperExpression(...WithType(resultEntityType)) (:605-608) —
    that structural type swap IS the mechanism, and Cast has no equivalent.
  - Empirically, Cast<Cat>().ExecuteDelete() routed through two-phase throws ExpressionNotSupportedException from
    the driver's LINQ translator with NO COMMAND ON THE WIRE (CommandStartedEvent capture).
  => Admitting Cast would only move the throw from the classifier to MaterializeIds, trading a message that names
     the operator and lists alternatives for a driver-internal exception. Per AGENTS.md, changing the exception
     type for an UNSUPPORTED feature is explicitly not a break, so there is no compatibility argument either.
     Cast stays rejected, pinned by a test asserting the CLASSIFIER's message (so re-admitting it fails).
     Reviewer independently confirmed and agreed. ARTHUR CAN REVERSE THIS; nothing downstream depends on it.
Task 15: reviewer INDEPENDENTLY CONFIRMED the safety property by running the class: phase 1 emits
  {$match: {_t: "cat"}} on the wire, Assert.Single(aggregateCommands) proves the captured command IS phase 1, and
  the spy client is genuinely wired (SingleEntityDbContext caches options on the COLLECTION INSTANCE, and the test
  passes its own collection from its own MongoClient). Also confirmed OfType/Cast never leaked into
  AffectedSetNeutralDefinitions and that BuildFilter's walk still THROWS on anything non-Where/non-neutral — so
  the destroy-the-whole-hierarchy mode has two independent guards, not one.
Task 15: MY IMPRECISION — Important 2 (the false "OfType/Cast narrow the affected set via a discriminator filter,
  so they belong on the two-phase side" doc at MongoNonQueryExpression.cs:101-103) is verbatim MY wording from the
  Task 14 dispatch. It was true of OfType and false of Cast, and Task 15 is what established that. Worse, it now
  instructs a future maintainer to admit Cast — the exact thing this task ruled out.
Task 15: THE STALE-PROSE TREADMILL — MongoBulkOperationExecutor.cs:539,545 say "uses paging ('Skip'/'Take')"
  BECAUSE TASK 13's FIX ROUND NARROWED THEM TO THAT. Task 15 adds a second two-phase trigger and makes them wrong
  again. These two messages have now been wrong twice, in opposite directions, in three tasks. They are thrown
  from RunInBulkTransaction(+Async), which wrap EVERY two-phase execution with no knowledge of which operator
  caused it — so any future task that adds a two-phase trigger breaks them again. FINAL REVIEW: consider whether
  the message should derive the cause from the plan rather than hardcoding an operator list.
Task 15: NEWLY REACHABLE GAP (reviewer, unverified — sent to fix round): self-referencing setters combined with
  OfType<TDerived>(). CreateBulkPlan closes over the ROOT entity type (MongoShapedQueryCompilingExpressionVisitor
  .cs:75, :85-87 => Animal) and the setter rebinder matches on EXACT type equality (:727 node.Type == entityType,
  :740 node.Type == target.Type), so an OfType<Cat>() source gives EF a Cat-typed lambda parameter that is neither
  collected nor rebound, leaving a free parameter in the lambda built at :639-641. Was a clean rejection before
  this task. Shipped ExecuteUpdate test uses a CONSTANT setter, which takes the SerializeConstant path and never
  exercises rebinding.
Task 15: FOR TASK 20 (compliance exclusions): MongoComplianceTest.cs Ignores InheritanceBulkUpdatesTestBase<> and
  FiltersInheritanceBulkUpdatesTestBase<>; with OfType now supported, re-check whether they can be enabled. Both
  the implementer and reviewer agreed the Ignore-list decision is Task 20's, not Task 15's.
Task 15: FOR TASK 25 (docs): docs/failing-spec-tests.md EF-X012 says "Queryable.OfType<T>() is not translated" —
  misleading; OfType IS translated on the read path (TranslateOfType). The 2 affected tests are OfType+Select
  shapes in NorthwindAggregateOperatorsQueryMongoTest, unaffected by this task.
Task 15: fix round 1/5 (7 addressed, 0 open; commits b36e3b9..6e8a884)
Task 15: complete (commits 2d8c40e..6e8a884, review clean)
Task 15: Important 3 RESOLVED NON-STRUCTURALLY. Reproduced symptom was TargetInvocationException wrapping driver
  ExpressionNotSupportedException("Expression not supported: e.") with the EF-visible message reduced to
  "Exception has been thrown by the target of an invocation" — i.e. the confusing-error bucket. Fixed by a guard in
  BuildSetter (MongoQueryableMethodTranslatingExpressionVisitor.cs:363-374) rejecting
  isSelfReferencing && propertySelector.Parameters[0].Type != entityType.ClrType, with a message naming the real
  limitation. Pinned by ExecuteUpdate_with_OfType_and_a_self_referencing_setter_is_rejected_with_a_clear_message.
  NO CreateBulkPlan rework was needed.
Task 15: OVER-REJECTION RISK CHECKED AND CLEAN (re-reviewer): BuildSetter's entityType is
  CollectionExpression.EntityType, which TranslateOfType never mutates (it rewrites only the shaper via WithType,
  :637), so entityType.ClrType is ALWAYS the root type. Without OfType, EF types the selector parameter over that
  same root type, so the guard can only fire when a genuinely narrowing OfType<TDerived>() is present. Verified
  against TASK 8's regression shape (ExecuteUpdateTests.cs:311/:512, Employee/DirectReports
  SetProperty(e => e.Rank, e => e.DirectReports.Max(r => r.Rank))): no OfType, guard never fires. Ordering/
  Skip/Take/Distinct cannot trigger it either since they never change the element type.
Task 15: Minor 5 was NARROWED, not just documented — ClassifyBulkSource (:481-498) now compares
  method.GetGenericArguments()[0] against CollectionExpression.EntityType.ClrType and forces two-phase ONLY when
  they differ, so OfType<RootType>() stays on the atomic single-command path. Pinned by
  ExecuteDelete_with_OfType_of_the_root_type_stays_on_the_single_command_path asserting Strategy=SingleCommand.
Task 15: the two-phase error messages now read "uses paging ('Skip'/'Take') or an 'OfType' type filter", and the
  implementer added a comment above them recording that this is a HAND-SYNCED enumeration that will go stale again
  if a future operator becomes two-phase. Fragility acknowledged rather than hidden. See the treadmill note above.

Task 16: complete (commits 6e8a884..4f2f29f, review clean, NO FIX ROUND) — implementer aaa9c9249ce3af826 (opus);
  review (opus): spec ✅, quality APPROVED, 0 Critical / 0 Important / 7 Minor.
Task 16: CONTROLLER CORRECTIONS carried into the dispatch (5, all verified first): (1) brief's SelectTargetIds/
  SelectTargetIdsAsync DO NOT EXIST — real names MaterializeIds/MaterializeIdsAsync in Storage :312/:336, called
  from :157/:165/:193/:211, so the task spans Query AND Storage; (2) brief's RenderPhaseOnePipeline added a SECOND
  ShouldLogSensitiveData gate, violating the single-gate constraint documented at :584-586; (3) brief's Step 4
  composed the command EAGERLY, which would have regressed Task 5's laziness fix; (4) brief's AppendStage call had
  wrong arity (3 generic params; real signature is AppendStage<TSource,TResult> taking a PipelineStageDefinition,
  driver2 MongoQueryable.cs:86-101); (5) brief's mandated comment claimed "index entries carry _id" — FALSE:
  secondary index entries carry indexed keys + an internal RecordId, so {_id:1} is covered ONLY if _id is in the
  index key or the collection is clustered on _id.
Task 16: implementer made TWO deviations the reviewer judged BETTER than the brief: no PhaseOneResult record and no
  MaterializeIds signature change (the queryable is already in scope at all four sites, so +163/-23 with no new
  types and no seam churn); and per-call stage document instead of a static readonly, justified by verified driver
  behaviour (AstUniversalStage.Render() returns the same instance).
Task 16: THE GATE DESIGN IS BETTER THAN THE BRIEF'S — phase 1 renders INSIDE RenderBulkCommand, after its sole
  early `return "?"`, so the eight PRE-EXISTING closed-gate assertions cover the new content with zero new security
  tests. Reviewer traced every consumer of targetIdQuery and confirmed Command is the only carrier, and that the
  closed-gate assertions are EXACT EQUALITY (Assert.Equal("?", closed.Command) at BulkOperationsCommandTests.cs
  :205/:238/:272/:322 + :95, each a Theory over sync/async) — so provably zero phase-1 bytes escape. No redaction
  assertion was weakened.
Task 16: used IMongoQueryProvider.LoggedStages instead of ToString() (which renders `ns.Aggregate([...])`, not
  JSON), so Command stays ONE VALID JSON DOCUMENT: { phase1Pipeline, filter, update }. LoggedStages is the array
  the aggregate command actually carried, and follows existing precedent at Diagnostics/MongoLoggerExtensions.cs:40.
Task 16: CLOSED A TASK 5 GAP — added a laziness test (BulkOperationsCommandTests.cs:391) that is a genuine pin:
  ExecutingBulkDelete returns before `var command = renderCommand()`, so hoisting is exactly what it detects;
  counting thunk must observe 0 and would observe 2 on eager rendering. This was progress.md:36's open item.
Task 16: reviewer checked the Distinct+paging edge case: the driver's DistinctMethodToPipelineTranslator emits
  $group FOLLOWED BY $replaceRoot on $_id, so documents entering the appended $project are whole documents and
  _id resolves. Projection is safe there.
Task 16: NO BREAKING-CHANGES ENTRY NEEDED — reviewer verified the premise rather than accepting it:
  MongoBulkDeleteEventData/MongoBulkUpdateEventData ARE public, but both files and MongoBulkOperationExecutor.cs
  are ABSENT at v10.0.2 (latest released on any line: v10.0.2 / v9.1.2 / v8.4.2, 2026-06-03). The whole bulk
  feature has never shipped, so per AGENTS.md this is not a break.
Task 16: ⚠️ RESOLVED BY CONTROLLER: (a) all three configs verified — Debug EF8/EF9/EF10 each "Build succeeded,
  0 Warning(s), 0 Error(s)"; NorthwindBulkUpdatesMongoTest EF10 re-run independently => 181/181.
  (b) the before/after wire capture came from a DELETED throwaway probe, but is independently corroborated by the
  committed assertion at BulkOperationsCommandTests.cs:383-384 (phase1Pipeline.Last() == {"$project":{"_id":1}},
  which fails both if the stage is dropped AND if it is inserted earlier). Provenance note, not a gap.
Task 16: minor (deferred): unmeasured perf claim survives at MongoShapedQueryCompilingExpressionVisitor.cs:446-447
  ("the bulk of phase 1's cost") — soften or drop; Amendment 5 exists to stop exactly this.
Task 16: minor (deferred): MongoBulkOperationExecutor.cs:612 `targetIdQuery = null` default makes omission SILENT —
  a future fifth two-phase site that forgets it compiles and drops phase1Pipeline. Making it required costs only
  spelling `targetIdQuery: null` at the four single-command sites. Cheap fail-loud win.
Task 16: minor (deferred): PUBLIC XML doc on MongoBulkDeleteEventData.cs:54-56 / MongoBulkUpdateEventData.cs
  :104-106 promises "the aggregation pipeline phase one used" but omits that phase1Pipeline can be JSON null — a
  consumer parsing per the doc would NRE on the degraded case. One clause.
Task 16: minor (deferred): TASK 5's KNOWN-FALSE DOC STILL NOT TAKEN — LogSensitiveData's XML doc on both event-data
  types (:60-63 / :110-113) is inaccurate on Executed payloads (hardcoded false). The implementer edited the block
  four lines above and left it. This is now the SECOND task to walk past it. FINAL REVIEW: take it.
Task 16: minor (deferred): flat Command key naming (`filter` unlabelled as phase 2's); comment at :455-458 names
  two test methods by full name (rename-rot); and no diagnostics exist for a two-phase op matching ZERO ids or
  throwing in phase 1 (:159/:197 short-circuit to 0 before any Executing event) — the last is PRE-EXISTING, not
  introduced here, but it means the phase-1 pipeline is unavailable exactly when most diagnostic.

Task 17: implemented (commits 4f2f29f..ea0e01d) — implementer a15837c68aeaccdeb (sonnet); review (sonnet):
  spec ✅ all 5 amendments, quality APPROVED but 2 Important (both about EVIDENCE, not code) + 2 Minor.
  Northwind 181/181 EF10, 169/169 EF9; all 3 configs clean. Diff is 26/11 in ONE method.
Task 17: CONTROLLER CORRECTIONS (5): (1) brief named Query/...ShapedQueryCompilingExpressionVisitor.cs and
  SelectTargetIdsAsync — real target is Storage MaterializeIdsAsync:348; (2) brief's "Consumes PhaseOneResult from
  Task 16" is stale — Task 16 deliberately never created it; (3) !!! THE BRIEF'S REPLACEMENT BODY HAD NO
  TRY/CATCH, which would have DELETED THE FIX FOR TASK 11's CRITICAL FINDING !!! ; (4) Step 2's capacity premise
  was false (sync already had no capacity hint; only async loses one); (5) verify Task 16's phase1Pipeline still
  populates on the async path.
Task 17: try scope, cursor disposal, ConfigureAwait(false) on both awaits, and unchanged token flow all verified
  CLEAN by the reviewer against driver source. Try wraps ONLY ToCursorAsync (:363-371); MoveNextAsync loop
  (:379-385) sits outside it inside `using (cursor)`. Confirmed in driver source that ToCursorAsync translates
  synchronously via ExpressionToExecutableQueryTranslator.Translate BEFORE _provider.ExecuteAsync
  (driver2 Linq3Implementation/MongoQuery.cs:97-102), so MoveNextAsync cannot throw ExpressionNotSupported.
Task 17: phase1Pipeline SAFE — reviewer verified in driver source that MongoQueryProvider.ExecuteAsync sets
  _executedQuery BEFORE executing (:156-157) and LoggedStages => _executedQuery?.LoggedStages (:108), and that
  ToCursorAsync and ToListAsync route through the SAME _provider.ExecuteAsync. Population is identical either way.
Task 17: !!! IMPORTANT EVIDENCE FINDING — NONE of the three pinned regression tests would fail if this change were
  reverted to ToListAsync. The translation-failure and cancellation tests pass under both (identical narrow-catch
  semantics around a call that itself invokes ToCursorAsync internally); the Task 12 transaction-abort test cancels
  only AFTER phase 1's aggregate succeeded, so it is insensitive to how ids are extracted. The green suites prove
  BEHAVIOURAL EQUIVALENCE (same ids, same exception classification) and nothing about the change under review.
  => Peak memory is not directly observable, so "streaming vs materializing" may not be falsifiable by a
     reasonable test. But hand-rolling the drain loop introduces a NEW risk the driver used to own: that the loop
     drains EVERY batch. A >101-document test (default batch size) DOES pin that and WOULD fail on a truncating
     loop. Sent to fix round on that framing.
Task 17: report inaccuracy sent to fix round: it claims
  ExecuteUpdateAsync_two_phase_propagates_cancellation_unchanged_during_phase_1_materialization "exercises the
  MoveNextAsync loop's cancellation plumbing outside the try". It does not — that test cancels BEFORE the call
  (ExecuteUpdateTests.cs:349-365), so the token surfaces inside ToCursorAsync, i.e. INSIDE the try. No existing test
  covers a failure/cancellation during cursor ITERATION (a mid-loop getMore).
Task 17: my imprecision — my amendment cited a "~20-line comment at :305-324" as the async method's; it is actually
  the comment above the SYNC MaterializeIds (:304-323, untouched and still accurate). The substantive requirement
  (preserve the narrow-catch rationale, updated for the new shape) was met regardless.
Task 17: fix round 1/5 (2 addressed, 0 open; commits ea0e01d..f327a0a, test-only +60/-0)
Task 17: complete (commits 4f2f29f..f327a0a, review clean)
Task 17: NEW TEST — ExecuteDeleteAsync_two_phase_streams_every_id_across_multiple_getMore_batches
  (ExecuteDeleteTests.cs:413-471). Seeds 250 docs, drives two-phase async delete via bare Skip(1), asserts the full
  affected count, AND self-checks multi-batch spanning with Assert.True(getMoreCount > 0). VERIFIED FALSIFIABLE:
  fails when the drain loop's `while` is changed to `if` (keeps ~101 of 249 — far too large a gap to pass by
  coincidence). VERIFIED MULTI-BATCH: DEBUGPROBE showed getMoreCount=1 for 250 docs.
  The getMoreCount>0 self-check is the important part: if a future server default stopped spanning batches, the
  test fails loudly instead of silently degrading to single-batch coverage.
Task 17: the report now states its own limits plainly (task-17-report.md:174-195): the three Task 11/12 tests prove
  behavioural equivalence ONLY; peak memory is not directly observable; and NO test covers a mid-loop getMore
  failure or cancellation. That last gap is TASK 23's territory — a CommandCaptureSpy-driven mid-iteration failure
  test was deliberately NOT built here.

Task 18: implemented (commits f327a0a..78a64fb) — implementer a18840adc92f79a2e (opus); review (opus): spec ✅
  all 6 amendments, quality APPROVED, 1 Important + 8 Minor. Northwind 181/181 EF10, 169/169 EF9; full
  FunctionalTests EF10 1802p/0f; all 3 builds clean.
Task 18: CONTROLLER CORRECTIONS (6): (1) brief said Query — it's Storage; (2) brief invented the *ByIds
  signatures; (3) !!! brief's CombineBulkCommand would have moved command composition BACK OUTSIDE Task 16's
  single redaction gate and reintroduced a "?" sentinel as a second decision point — DO NOT BUILD IT !!!;
  (4) PhaseOneResult doesn't exist; (5) brief's functional test drove exactly ONE batch (25 docs vs a 50,000
  ceiling) and its comment claimed a batch-size override the code lacked — required a real multi-batch test with an
  internal-only seam; (6) SpyLogging's GetLogMessageByEventId uses Assert.Single so it THROWS once Executing fires
  per batch — required a plural accessor without touching the singular one.
Task 18: multi-batch test VERIFIED FALSIFIABLE against THREE separately injected bugs, and the reviewer confirmed
  each from the shipped assertions rather than the narrative: batching removed (1 delete command vs 4, caught by
  two independent assertions), accumulation overwritten (total 1 vs 7, plus Deleted=7 / Matched=7 / Modified=7),
  per-batch commit (4 commitTransaction vs 1). Test observes the WIRE via CommandCaptureSpy, not provider beliefs.
Task 18: seam = internal static IDisposable OverrideMaxIdsPerBulkCommand(int) — scoped, restores in Dispose even if
  the test throws, guards batchSize<=0 (which would otherwise INFINITE-LOOP BatchIds rather than fail), and
  batchSize is captured into the iterator state machine so a mid-iteration Dispose can't change it. Default stays
  50,000 so every pre-existing fixture still drives exactly one batch — Claim 3 holds BY CONSTRUCTION.
Task 18: the comment correctly REJECTS the brief's own framing — it states batching buys NO relief from
  transactionLifetimeLimitSeconds and that more round trips make that limit easier to reach.
Task 18: ⚠️ RESOLVED BY CONTROLLER — reviewer's scope note: the brief's THIRD motivation (unbounded buffering as a
  resource-exhaustion concern) is NOT addressed. MaterializeIds/Async (:451/:475) still accumulate every id into
  one unbounded List<BsonValue>; only the COMMAND is bounded. Not a missed deliverable (Produces asked only for
  chunking), and Task 17 bounded the *document* buffering. But the memory half of the stated threat model is still
  open: 500k ObjectIds is ~6 MB of client memory with no ceiling. FOR THE FINAL REVIEW / possible follow-up ticket
  — do not widen EF-361 for it.
Task 18: reviewer's aliasing verdict on BatchIds returning the caller's list in the single-batch case: SAFE today
  and LESS NOVEL than the report implied — the pre-change code already passed the caller's own list straight to
  Builders.Filter.In, so no call site gains aliasing it didn't have. The driver retains the IEnumerable by
  reference and enumerates lazily at Render time (driver2 FilterDefinitionBuilder.cs:1911/:1917/:1950), but the
  filter is rendered and sent synchronously inside the same iteration and the payload keeps only the rendered
  string. Reviewer would still DROP the fast path (saves ~400 KB of reference copy against a round trip that
  serializes the same values) — sent to fix round as a Minor.
Task 18: fix round 1/5 (7 addressed, 0 open; commits 78a64fb..812ad18)
Task 18: complete (commits f327a0a..812ad18, review clean)
Task 18: all four public TargetCount doc sites now say "the ids in the $in filter of the command THIS EVENT
  describes", name the Executed event's DeleteCount/MatchedCount/ModifyCount as the authoritative total, and state
  that N Executing pair with 1 Executed. Re-reviewer confirmed the four sites are mutually consistent and that each
  cross-references its own type's correct property — and that no NEW false claim crept in (which had happened on
  Task 14 while fixing a false doc claim).
Task 18: Minor 2 now genuinely discriminates — re-reviewer verified the arithmetic: OrderBy(Quantity) with
  batchSize=2 yields batches [0,1],[2,3],[4,5],[6]; the two pre-archived docs at Quantity 0 and 3 land in batch 1
  and batch 2 (different batches, neither in the final batch), giving Matched=7 / Modified=5. Injecting
  `modified += result.MatchedCount` fails both sync and async with "Not found: Modified=5".
Task 18: minor (deferred): ExecuteDeleteTests.cs:478 comment still names the pre-rename `MaxIdsPerBulkCommand`.
  Cosmetic prose only; the field's code use sites were all renamed.

Task 19: complete (commits 812ad18..df9c21f, review clean, NO FIX ROUND) — implementer aa8fdf1fbe8931470 (opus);
  review (opus): spec ✅ all 6 amendments, quality APPROVED, 0 Critical / 0 Important / 5 Minor.
Task 19: THE IMPLEMENTER DISPROVED THE BRIEF'S OWN INVARIANT and replaced it with a stronger structural one.
  The brief justified caching with "the fallback is reached solely for expressions with no query-parameter
  dependency" + a Debug.Assert. Both wrong:
  - `o => capturedPrefix + Guid.NewGuid()` DOES reach the fallback with a query parameter embedded, on EF9 AND EF10.
  - THE REAL INVARIANT: what is cached is the compiled DELEGATE, never the value. The cached delegate is a
    PARAMETERLESS Func<object?>, so it structurally cannot depend on queryContext at all; and a tree that does
    depend on a query parameter cannot compile into a parameterless lambda. Reviewer verified independently with a
    probe: a delegate built over MemberExpression(Constant(closureObj), field) RE-READS the field at each invoke
    (returned 1 then 42 across a mutation), so it captures the reference, not the value.
  - Slot is written only AFTER a successful Compile() (:743-748), so throwing shapes keep throwing forever and the
    slot stays null. Reviewer confirmed the ordering and that a throw from compiled() still leaves a valid cached
    delegate — also correct.
Task 19: QueryParameterExpression derives directly from Expression, NOT ParameterExpression
  (efcore src/EFCore/Query/QueryParameterExpression.cs:14; NodeType => Extension; VisitChildren returns this).
  So the brief's proposed VisitParameter detector was DEAD CODE on EF10 — exactly where it mattered. Not shipped.
Task 19: THE BRIEF'S NOMINATED TEST NEVER REACHES THE FALLBACK. ExecuteUpdate_with_captured_variable_set_value
  captures a local, which EF funcletizes into a query parameter, so it lands on the parameter branch. Reviewer
  confirmed. Instrumentation across the whole bulk suite found 84 evaluations = 80 query parameters + 4 constants +
  ZERO fallback hits. The new test is the fallback's ONLY coverage, on both EF9 and EF10.
Task 19: new test ExecuteUpdateTests.cs:180-207 executes the same shape twice in one process using
  s.SetProperty(o => o.Status, o => Guid.NewGuid().ToString()) (ignores its parameter => non-self-referencing =>
  fallback). Observed failure mode: caching the VALUE instead of the delegate fails with
  "Assert.NotEqual() Failure: Strings are equal" on both configurations. No leftover instrumentation (reviewer
  grepped for AppendAllText/Console.Write/scratchpad — clean; diff touches exactly 3 files).
Task 19: Setter is now a `sealed class` (was a record). The brief's rationale was BACKWARDS — a body-declared field
  DOES participate in synthesized record equality. Reviewer confirmed nothing relies on Setter equality
  (RejectDuplicateSetters uses HashSet<IProperty>; unit tests use Assert.Same on Property).
Task 19: implementer added `volatile` beyond the brief — reviewer judged it a genuine improvement: the benign-race
  argument covers duplicate CONSTRUCTION but not half-PUBLICATION, and invoking a partially published delegate is a
  crash, not a wrong value.
Task 19: ⚠️ RESOLVED BY CONTROLLER: Debug EF8/EF9/EF10 all "Build succeeded, 0 Warning(s), 0 Error(s)" (this also
  independently confirms the reviewer's CS0649 analysis — the unassigned-on-EF8 volatile field is suppressed by
  InternalsVisibleTo); Northwind re-run independently => 181/181 EF10, 169/169 EF9. Claim 5's aggregate
  instrumentation counts are unreproducible (the instrumentation was correctly not committed) but nothing depends
  on the numbers, only the conclusion — not a gap.
Task 19: minor (deferred): MongoShapedQueryCompilingExpressionVisitor.cs:731-733 comment claims the Compile() throw
  is canonicalized by TranslateBulkOrThrow. FALSE ON EF9 — that filter is `when (exception is not
  InvalidOperationException)` (:408), and EF9's shape throws InvalidOperationException("variable '__prefix_0' ...
  is not defined"), which passes through raw. EF10 throws ArgumentException("must be reducible node") and IS
  wrapped. Reviewer probed both. Behaviour is pre-existing; only the comment is wrong.
Task 19: minor (deferred): the new test cannot detect that it has STOPPED exercising the cache — if EF's query cache
  ever fails to serve the second execution from the first, a fresh Setter and delegate are used, two different
  Guids are still written, and the test passes while covering nothing. Mitigation options: a comment saying it
  degrades to a correctness check, or a unit test calling the evaluator twice with the SAME Setter instance.
Task 19: minor (deferred): 26 comment lines for 6 lines of code at :710-735, part of it arguing against a design
  that exists nowhere in the tree (addressed to the reviewer rather than a future maintainer).
Task 19: minor (deferred): CompiledValue is dead on EF8 (Setter sits outside the file's #if !EF8 region) — cosmetic
  only, since the clean EF8 build is confirmed.
Task 19: minor (deferred): MongoNonQueryExpression.cs:128's "this whole type is #if !EF8" is STILL FALSE — now
  flagged by a THIRD reviewer (Tasks 14, 16, 19). The new field's EF8 deadness is exactly what that comment would
  mislead a reader about. FINAL REVIEW: this one is four words and has survived three tasks.
Task 19: FOLLOW-UP TICKET CANDIDATE (pre-existing, not this plan): a value lambda that IGNORES its parameter is
  evaluated ONCE PER EXECUTION, not per document — so SetProperty(o => o.Status, o => Guid.NewGuid().ToString())
  writes ONE Guid to every matched document. A user would plausibly expect one per document. Task 19's new test
  DEPENDS on this shape being accepted, so whoever fixes it must revisit that test.
Task 19: note: fallback reachability differs materially between EF9 and EF10 (EF10 parameterizes non-lambda
  DateTime.UtcNow / Random.Next setter values; EF9 does not), so the fallback is hotter on EF9. Both #if arms
  untouched. The value-lambda shape the new test uses is the one shape reaching the fallback identically on both.

=== PHASE 5 COMPLETE (Tasks 16-19, performance) ===
NEXT: Phase 6 = Tasks 20-26 (coverage completion + docs + cleanups). Task 20 = cover global query filters and
  correct the compliance exclusions. CARRY THE TASK 15 DIRECTIVE: MongoComplianceTest.cs Ignores
  InheritanceBulkUpdatesTestBase<> and FiltersInheritanceBulkUpdatesTestBase<>; with OfType now supported (Task 15),
  re-check whether they can be enabled — both the Task 15 implementer and reviewer agreed that decision is Task 20's.

=== PHASE 6 IN PROGRESS (Tasks 20-26) ===
Task 20: implemented (commits df9c21f..f96ce5c) — implementer a2d5beca694e64925 (sonnet); review (sonnet):
  spec ❌ (one dishonest compliance reason), quality NEEDS FIXES — 1 Important + 1 Minor. Test-only: no src/ changes.
  Northwind 181/181 EF10, 169/169 EF9; MongoComplianceTest passes both; all 3 builds clean.
Task 20: !!! THE SAFETY RESULT — GLOBAL QUERY FILTERS *ARE* HONOURED BY BULK ExecuteDelete/ExecuteUpdate, on BOTH
  the single-command and two-phase strategies, on BOTH EF9 and EF10. No production change was needed. This was the
  most severe open risk in the feature (a filter miss would delete another tenant's data) and it is now pinned by
  five discriminating tests. Reviewer verified each one independently against the 3-doc seed and confirmed none
  would pass if the filter were ignored — including that Take(5) genuinely routes two-phase per ClassifyBulkSource
  (:454-512) and AffectedSetNeutralDefinitions (:152-158), not by accident of the test's name.
Task 20: IgnoreQueryFilters() works end-to-end on the bulk path with NO classifier change and no translation error —
  the test asserts 3 deleted against a filter that otherwise narrows to 2, so a silent no-op would yield 2.
Task 20: CONTROLLER CORRECTIONS (4): (1) brief's usings omitted MongoDB.Driver; (2) !!! STEP 4 DESCOPED BY ME !!!
  the brief said to create FiltersInheritanceBulkUpdatesMongoTest "following the structure of
  NorthwindBulkUpdatesMongoTest", but that file REUSES an existing Northwind fixture and there is NO inheritance
  fixture anywhere in SpecificationTests (every suite there is Northwind-based plus AdHocJsonQueryMongoTest), so it
  would have meant building a whole new hierarchy fixture — instructed to ASSESS and report, not build;
  (3) Step 3's reasons must be VERIFIED against upstream test bodies, not asserted; (4) don't duplicate
  NorthwindQueryFiltersQueryMongoTest's read-path coverage.
Task 20: STEP 4 ASSESSMENT (implementer's recommendation): keep BOTH FiltersInheritanceBulkUpdatesTestBase<> and
  InheritanceBulkUpdatesTestBase<> ignored, and route the InheritanceModel fixture to ITS OWN TASK — the work is
  comparable to a new NorthwindQueryMongoFixture-sized effort, not a structural copy. THIS ANSWERS THE TASK 15
  DIRECTIVE. ARTHUR'S CALL whether to add that task to the plan or leave it as a follow-up ticket.
Task 20: the TPT/TPC reason inherited from the original blanket comment was WRONG and the implementer corrected it.
  Reviewer verified: InheritanceBulkUpdatesTestBase<> has no TPT/TPC-specific content — its
  Delete_where_using_hierarchy/_derived operate on Country.Animals CROSS-COLLECTION subqueries; the TPT-specific
  overrides live in a different, relational-only class (TPTInheritanceBulkUpdatesTestBase<>) that is not the one
  ignored here. Correction is right, and right for the reason given.
Task 20: BulkUpdatesTestBase<> ignore entry is EMPIRICALLY REDUNDANT — implementer removed it, re-ran
  MongoComplianceTest, still passed; reviewer confirmed by reading ComplianceTestBase.Implements/GetBaseTypes
  (it walks the full base chain matching open generic definitions) and that NorthwindBulkUpdatesTestBase<TFixture>
  derives from BulkUpdatesTestBase<TFixture>. Left in place; the comment DISCLOSES the redundancy, so the reviewer
  judged it harmless belt-and-braces rather than misleading. No action.
Task 20: ⚠️ RESOLVED: the two-phase test's routing was reasoned from the classifier rather than a captured wire
  command, but the reviewer independently verified the classifier path, so this is not a gap.
Task 20: fix round 1/5 (2 addressed, 1 NEW open; commits f96ce5c..2fa159a). Important 1 and the added two-phase
  update test both ADDRESSED. Re-reviewer verified the 8/11-vs-3/11 split of NonSharedModelBulkUpdatesTestBase
  method-by-method at BOTH v9.0.16 and v10.0.8, and confirmed the implementer's own follow-on discovery
  (NonSharedModelBulkUpdatesTestBase derives from NonSharedModelTestBase, NOT BulkUpdatesTestBase<>) without
  contradicting the separately-verified fact that NorthwindBulkUpdatesTestBase<TFixture> DOES derive from it.
  New test Two_phase_update_honours_the_tenant_filter confirmed to route two-phase and discriminate on both axes.
Task 20: !!! MY ERROR, PROPAGATED INTO A CODE COMMENT !!! My Task 20 dispatch asserted "there is no inheritance
  fixture anywhere in SpecificationTests — every suite there is Northwind-based plus AdHocJsonQueryMongoTest".
  I had run `ls ... | head -20`, which truncated the listing immediately before the VectorSearch files. The
  implementer faithfully wrote my claim into MongoComplianceTest.cs:133 as "verified: every SpecificationTests/Query
  suite is Northwind-based plus AdHocJsonQueryMongoTest". The re-reviewer caught it.
  ACTUAL CONTENTS (28 files): 24 Northwind-*, AdHocJsonQueryMongoTest.cs, VectorSearchMongoTest.cs,
  VectorSearchExactMongoTest.cs, VectorSearchMongoTestBase.cs.
  The CONCLUSION still holds — none of those is a hierarchy/inheritance fixture — but the supporting enumeration
  was false, which is precisely the honesty failure this task existed to fix. Lesson for the controller: do not
  hand a subagent an enumeration derived from a truncated listing, and do not write "every"/"all" into a dispatch
  unless the full set was actually inspected.
Task 20: fix round 2/5 (1 addressed, 0 open; commits 2fa159a..3b7e523, comment-only)
Task 20: complete (commits df9c21f..3b7e523, review clean, 2 fix rounds)
Task 20: the implementer independently re-derived the directory listing rather than trusting MY corrected version —
  correct instinct, and the re-reviewer then verified it a third time (28 files) AND checked the load-bearing
  conclusion directly by grepping the four non-Northwind files for Animal|Bird|Eagle|Kiwi|Plant|Country|
  InheritanceModel|Hierarchy (no matches). It also re-verified the upstream Country.Animals claim
  (FiltersInheritanceBulkUpdatesTestBase's four hierarchy methods all query ss.Set<Country>().Where(e => e.Animals...)
  — cross-collection in the document model regardless of fixture) and re-counted the 11 NonSharedModel methods.
Task 20: FINAL COMPLIANCE STATE — BulkUpdatesTestBase<> (redundant, disclosed), FiltersInheritanceBulkUpdatesTestBase<>
  (no hierarchy fixture exists; hierarchy methods are cross-collection anyway; substitute coverage named),
  InheritanceBulkUpdatesTestBase<> (cross-collection Country.Animals subqueries — the inherited TPT/TPC reason was
  wrong and is corrected), NonSharedModelBulkUpdatesTestBase (8/11 owned or table-sharing, 3/11 plain
  cross-collection nav). Every reason now states what was actually read.

Task 21: complete (commits 3b7e523..6da4f93, review clean, NO FIX ROUND) — implementer ae6a7ad9869a6b0d3 (sonnet);
  review (sonnet): spec ✅ all 4 amendments, quality APPROVED, 0 Critical / 0 Important / 2 Minor.
  All 16 brief-named tests found and converted (9 delete + 7 update); no src/ changes.
Task 21: !!! MY CORRECTION WAS LOAD-BEARING — THE COLLECTION COLLISION WAS REAL, NOT MASKED !!!
  CreateSeededContext used [CallerMemberName], which resolves to the METHOD name and is therefore IDENTICAL for both
  [InlineData] cases of a theory. The implementer probed it: TemporaryDatabaseFixtureBase.CreateCollection silently
  REUSES an existing collection rather than dropping or erroring, so without a discriminator both theory cases would
  have shared one collection and the second would have run against the first's leftovers. Fixed by threading
  `async` through to CreateCollection's `params object?[] values` (which appends "True"/"False" via
  CreateCollectionName). Re-reviewer counted the call sites: 9 in ExecuteDeleteTests + 7 in ExecuteUpdateTests,
  matching the 16 conversions 1:1, with [CallerMemberName] still last in both helper signatures.
Task 21: reviewer diffed EVERY converted body line-by-line and confirmed NO assertion was weakened, relaxed or
  reworded — only the seeding call, the operation call, and (for 3 tests) the transaction API calls changed. It also
  confirmed every async branch resolves to a real awaited *Async call rather than the sync method wrapped in an
  async Task, which was the main failure mode for this kind of conversion.
Task 21: Amendment 3 decided deliberately — the three transaction tests use genuinely async
  BeginTransactionAsync/CommitAsync/RollbackAsync on the async case, documented in-line, so the async coverage is
  not partial. Amendment 4's ThrowsAsync pinning is present in both AutoTransactionBehavior.Never conversions with
  the required reasoning comment.
Task 21: counts EF10 71->87, EF9 72->88; run twice, no flakes. CONTROLLER-VERIFIED: EF10 87 total (86 passed,
  1 pre-existing skip) and all three configs "Build succeeded, 0 Warning(s), 0 Error(s)". Northwind not re-run by me
  — Task 21 touched only FunctionalTests and Northwind lives in SpecificationTests, so there is no path; the
  implementer reports 181/181 and 169/169.
Task 21: three tests deliberately left unconverted, judged sound: the invalid-setter test throws during
  translation before any two-phase code runs; ExecuteDelete_with_OrderBy_Take_still_uses_the_two_phase_path
  duplicates a shape now covered on both paths; and the bare-Skip strategy test's async drain coverage comes from
  Task 17's ExecuteDeleteAsync_two_phase_streams_every_id_across_multiple_getMore_batches (:465-522).
Task 21: minor (deferred): the report's rationale for the bare-Skip non-conversion omits WHERE that shape's async
  coverage actually lives (Task 17's streaming test), which could mislead a future reader into thinking it has none.
Task 21: minor (deferred): the three transaction tests duplicate assertions/comments across their if/else branches.

Task 22: complete (commits 6da4f93..3e209f3, review clean, NO FIX ROUND) — implementer a99a9a9bd08b5962e (sonnet),
  DONE_WITH_CONCERNS; review (sonnet): spec ✅ both amendments, quality APPROVED, 0 Critical / 0 Important / 2 Minor.
  ExecuteUpdateTests 49/49 EF10, 50/50 EF9; Northwind 181/181 EF10; all 3 builds clean. Test-only, +168 lines.
Task 22: !!! SerializeConstant's XML CLAIM IS NOW SUBSTANTIATED — bulk and SaveChanges write BYTE-IDENTICAL BSON for
  enum (int32), Guid (binary subtype 04) and DateTime. My Amendment 1 required this: the brief's test only
  round-tripped through EF, which proves self-consistency, not equivalence with the write pipeline. The shipped test
  writes one document via each path, reads BOTH as raw BsonDocuments via
  collection.Database.GetCollection<BsonDocument>(...), and asserts BsonType equality BEFORE value equality — so it
  would catch Guid-as-string vs binary, or enum-as-string vs int32. Reviewer verified the comparison is genuinely
  raw and that the bulk update is scoped by _id so the SaveChanges document stays untouched.
Task 22: my Amendment 2 also paid off — the brief's missing-field test asserted ONLY the affected count while being
  named "..._writes_null", so it would have passed even if the field were left untouched. It now asserts
  raw.Contains("Quantity") AND raw["Quantity"].BsonType == BsonType.Null, which distinguishes EXPLICIT NULL from
  FIELD ABSENT — either check alone would not. Behaviour confirmed: pipeline-form $set over a missing field writes
  explicit BSON null, matching the brief's prediction.
Task 22: !!! NEW PRODUCTION DEFECT FOUND INCIDENTALLY — EF9-ONLY, OUTSIDE THIS PLAN !!!
  ExecuteUpdate over an entity with an AUTO-INCLUDED OWNED NAVIGATION is rejected outright by ClassifyBulkSource
  with "The 'Select' operator is not supported in a bulk delete or update" — for ANY setter, including a plain
  scalar one. Mechanism independently verified by the reviewer against ClassifyBulkSource (:454-511, throw at :503,
  message at :548-553): on EF9 nav expansion leaves a Select(Include(...)) auto-include wrapper in the captured
  chain, and the classifier tolerates only Where/ordering/Skip/Take/OfType/set-neutral operators.
  A MODEL-LEVEL CONFIGURATION THEREFORE BREAKS BULK UPDATES ENTIRELY ON THAT ENTITY, with a confusing message that
  names an operator the user never wrote. Implementer worked around it in the test model with AutoInclude(false),
  visibly commented at ExecuteUpdateTests.cs:53-58, and did NOT touch src/. Reviewer judged it a genuine defect,
  correctly out of scope, and disclosed rather than hidden.
  ⚠️ OPEN: whether this is truly EF9-only or reachable on EF10 under other shapes (owned collections, deeper
  nesting) is UNVERIFIED. FOR ARTHUR: this wants its own ticket, not a widening of EF-361.
Task 22: Task 9's MemberInfo/name-divergence gap remains OPEN and the skip is judged sound by the reviewer:
  FindProperty(MemberInfo) vs FindProperty(name) can only diverge when two CLR members share a name on one entity
  type (property hiding via `new`, or explicit interface implementation), which needs a contrived model. Consistent
  with Task 9's own decision. STILL UNCOVERED — for the final review's triage.
Task 22: minor (deferred) -> FOR TASK 25: ExecuteUpdateTests.cs:181 comments that the nested/owned-target rejection
  "is stated in the README". It is NOT — README:98 describes bulk support but never mentions nested/owned rejection.
  Plan-mandated wording (copied from the brief), so not an implementer error, but it is a forward reference to docs
  that do not exist yet. TASK 25 MUST MAKE IT TRUE (or the comment must be softened).
Task 22: minor (deferred): ExecuteUpdateTests.cs:158's DateTimeKind.Utc assertion is non-discriminating — the driver
  always deserializes BSON dates as UTC, so it cannot fail.

Task 23: complete (commits 3e209f3..41d4b40, review clean, 1 message-only fix round) — implementer
  a18591c80c7ea105e (sonnet), DONE_WITH_CONCERNS; review (sonnet): spec ❌ (stale commit message only), quality
  APPROVED, 0 Critical / 1 Important (message) / 2 Minor. Test-only, +148 lines, 2/2 EF10 and EF9;
  ExecuteDelete+ExecuteUpdate 90/91 EF10; Northwind 181/181 EF10; 3 builds clean.
Task 23: !!! THE BRIEF'S TEST WOULD HAVE CAPTURED NOTHING — MY CORRECTION 2 CAUGHT IT !!!
  The brief's collector filtered on MongoBulkWriteEventData. That type is the SaveChanges bulk-write payload and is
  UNRELATED to ExecuteDelete/ExecuteUpdate: it has no TargetCount/Strategy/Command members, and is constructed only
  by ExecutingBulkWrite/ExecutedBulkWrite (MongoLoggerUpdateExtensions.cs:69,141), called only from
  Storage/MongoDatabaseWrapper.cs. Zero references from the bulk-operations path. Verified by the reviewer via grep.
Task 23: ACTUAL TYPE HIERARCHY (three unrelated EventData subclasses): MongoBulkDeleteEventData and
  MongoBulkUpdateEventData each back BOTH their Executing and Executed events — same type, different fields
  populated. Executing (built at :240/:401): targetCount set, command real or "?". Executed (built at :308/:472):
  totals set, targetCount null, command null HARDCODED. MongoBulkWriteEventData is the SaveChanges path only.
Task 23: implementer RETARGETED the brief's second test and the reviewer confirmed the retarget was justified —
  BulkOperationsCommandTests.cs:74-98 already asserts exactly what it would have re-proven (Executing delete:
  TargetCount null, LogSensitiveData false, Command "?"). The new file instead covers the Executed event's
  Strategy/DeleteCount, and independently established that LogExecutedBulkDelete's format string
  (MongoLoggerUpdateExtensions.cs:344-347) has NO {strategy} placeholder — so Strategy reaches no ILogger
  subscriber on that event at all. Genuine previously-unverified gap. Nothing requested was dropped.
Task 23: judgment call on two constant assertions, resolved: KEEP Command == null on Executed (Command is the
  property actually gated by ShouldLogSensitiveData on the Executing side, so its null-ness is a real
  redaction-boundary check that would catch a future ungated population). Assert.Null(TargetCount) on Executed is
  weaker — an API-shape invariant, not a security one, and constant today — kept because it was DISCLOSED as a
  judgment call rather than presented as meaningful. Minor, deferred.
Task 23: fix round 1/5 was MESSAGE-ONLY (git commit --amend, be728e7 -> 41d4b40). The original message named
  MongoBulkWriteEventData — the one type the file does not touch — carried over verbatim from the brief. NO scoped
  re-review was dispatched: an amend with an unchanged tree produces an empty diff, so there is nothing for a
  re-review to inspect. CONTROLLER-VERIFIED instead: message now names the correct types, `git show --stat HEAD`
  identical (1 file, +148), and grep confirms 0 occurrences of MongoBulkWriteEventData in the file.
Task 23: minor (deferred): both tests repeat the "Command discriminates Executing from Executed" rationale.

Task 24: implemented (commits 41d4b40..fcea873) — implementer ad11e67d45152643f (sonnet), DONE_WITH_CONCERNS;
  review (sonnet): spec ✅ all 3 amendments, quality APPROVED, 0 Critical / 1 Important (plan-mandated,
  unresolvable in this environment) / 2 Minor. Test-only, +245 lines, one new file.
Task 24: !!! THE TESTS NEVER EXECUTED — THIS IS CI-ONLY COVERAGE !!! All four SKIP, reason verbatim: "These
  Queryable Encryption tests require MongoDB 8.0 or later as declared by the VERSION environment variable."
  (Note: the gate is a MONGODB VERSION check via AutoEncryptionTestsBase.QueryableEncryptionTheory.Skip:49-56, NOT
  the CRYPT_SHARED_LIB_PATH gate I predicted — though that is also unset.) Gating-not-absence CONFIRMED: the
  ~Encryption filter went 48->52 tests, 2 passed/46 skipped -> 2 passed/50 skipped — purely additive, pass count
  unchanged, tests enumerated then skipped. COMPILATION AND GATING ARE THE ONLY THINGS VERIFIED; every assertion
  is unexecuted.
Task 24: THE BRIEF'S SNIPPET HAD TWO REAL BUGS, both found by the implementer and independently confirmed:
  (1) test 1 used IsEncrypted (QueryableEncryptionType.NotQueryable — "without query ability", per
      Extensions/QueryableEncryptionBuilderExtensions.cs:31-45) on the very field it then filters by equality.
      With that, Where(p => p.SSN == targetSsn).ExecuteDelete() would have matched NOTHING — which is one of the
      exact confidentiality failure modes this task exists to detect. Shipped code correctly uses
      IsEncryptedForEquality, following the sibling test at QueryableEncryptionTests.cs:947-980.
  (2) raw key casing: Patient.SSN carries [BsonElement("ssn")] (AutoEncryptionTestsBase.cs:167-168), so the brief's
      raw["SSN"] would have thrown KeyNotFoundException instead of asserting. Shipped code uses raw["ssn"].
Task 24: the brief ALSO wrongly claimed AutoEncryptionTestsBase supplies CreateContext/CreateEncryptedCollections/
  Patients. It does not (it has only CreateDataKey, CreateSamplePatients, the models, QueryableEncryptionTheory,
  ShouldRunQueryableEncryptionTests). Local equivalents were written and the reviewer confirmed they are
  near-verbatim copies of the same private members in QueryableEncryptionTests.cs:1547-1594 — idiomatic, not bespoke.
Task 24: sibling XUnitCollection names disagree ("Encryption" 2-of-3 vs "EncryptionTests"). Reviewer confirmed it is
  INERT here: XUnitCollection is a global alias for Xunit.CollectionAttribute (Usings.cs:17), NO
  [CollectionDefinition] exists anywhere, and DisableTestParallelization is assembly-wide. Organizational only.
Task 24: fix round 1/5 (2 addressed, 1 NEW Important open; commits fcea873..6a58d69). Test 3 renamed to
  Bulk_update_with_a_self_referencing_setter_does_not_expose_an_encrypted_sibling_field, given test 4's disjunction,
  comment now states the field-vs-collection scope question is NOT settled; all three raw checks converted to
  .ToList() + Assert.All (no .First() remains anywhere in the file).
Task 24: !!! THE FIX INTRODUCED A SUBTLER VERSION OF THE HOLE IT CLOSED — caught by the scoped re-review !!!
  Assert.Equal(patients.Length, updated) was placed INSIDE the try at :148-152. Xunit.Sdk.EqualException derives
  from Exception, so `catch (Exception)` now swallows a genuine "update succeeded but touched the wrong number of
  documents" defect and MISCLASSIFIES it as "rejected outright: acceptable". Test 4 (:191-199), which was held up as
  the correct reference, has only the bare ExecuteUpdate call in its try. Setup is correctly outside the try, so the
  originally-named scenario does not occur — but a materially similar hole appeared one line later, and it is worse
  than round 0, where the unguarded assertion would have failed visibly.
  This is exactly why the process requires a scoped re-review of every fix round rather than trusting a small diff.
Task 24: fix round 2/5 (1 addressed, 0 open; commits 6a58d69..7c58035)
Task 24: complete (commits 41d4b40..7c58035, review clean, 2 fix rounds)
Task 24: final shape — `int? updated = null;` outside the try; the try contains ONLY the bare ExecuteUpdate call;
  the count is asserted after the try/catch under `if (updated is { } updatedCount)`, which is true exactly on the
  success branch (ExecuteUpdate returns non-nullable int, so assignment and return are atomic — there is no path
  where it returns a value yet the nullable stays null). Both branches still converge unconditionally on
  Assert.All(rawDocuments, raw => Assert.Equal(BsonType.Binary, ...)). Re-reviewer verified test 4 by DIRECT FILE
  READ rather than inferring from the diff's silence, confirming its try also holds only the bare call.
Task 24: !!! STANDING CAVEAT FOR THE FINAL REVIEW AND FOR CI !!! This file is CI-ONLY coverage. Four tests, ZERO
  executed. Only compilation, discovery and gating were verified. The implementer's stated prior, for whoever reads
  the first real CI run: it expects the SUCCESS branch (field-scoped rejection), not collection-wide rejection.
  If CI takes the other branch, test 3 still passes by design — the disjunction was built for exactly that.
Task 24: TRANSFERABLE LESSON, recorded because it nearly shipped twice: broadening a `catch` to tolerate an EXPECTED
  failure also makes it tolerate UNEXPECTED ones. Any assertion sharing scope with a broad catch stops being an
  assertion. xUnit's EqualException derives from Exception, so `Assert.*` inside `catch (Exception)` scope is
  silently neutralised.

Task 25: implemented (commits 7c58035..fd65bc3) — implementer a62f95302e01d7361 (opus), DONE_WITH_CONCERNS;
  review (opus): spec ✅ all 9 amendments, quality APPROVED, 0 Critical / 2 Important / 4 Minor.
  Northwind 181/181 EF10, 169/169 EF9; compliance 1/1 both; full EF10 SpecificationTests 4571p/0f/28s; 3 builds clean.
  src/ change verified COMMENT-ONLY (20 lines, every one a // or ///).
Task 25: BOTH PLANTED BRIEF ERRORS WERE REFUSED WITH CODE CITED — (1) the brief's "concurrency tokens are neither
  checked nor WRITABLE" would have reversed Arthur's 2026-07-30 decision; README keeps "neither checked nor
  protected — a bulk update may set one explicitly", substantiated by the explicit "Concurrency tokens are
  deliberately NOT guarded here" comment and by ExecuteUpdateTests.cs:569-594 asserting the PERSISTED Version bytes.
  (2) Cast is on the NOT-SUPPORTED list, not the two-phase list, substantiated by the classifier default arm and
  BulkOperationsInheritanceTests.cs:168-177.
Task 25: THE DOC WAS MORE WRONG THAN EITHER OF US THOUGHT — my Correction 7 was UNDERSTATED. EF-X012 appears NOWHERE
  in the test sources (grep exit 1). Those two tests PASS on EF10 and are tagged EF-X020 on EF8/EF9; the cause was
  never OfType at all but the cross-collection $lookup from .Select(o => o.Customer.City) — confirmed by the reviewer
  reading the MQL baseline. Row marked obsolete with count 0, following the existing EF-X021 precedent at :214.
Task 25: MY SUMMARY WAS WRONG TWICE, both corrected against the source of truth rather than against me:
  (a) the EF-X002 count is 45, not the 46 I passed along (that was the pre-change row value);
  (b) the Task 22 comment is at ExecuteUpdateTests.cs:777, not :181 — line numbers shifted when Task 21 converted
      many tests to theories, and I quoted a stale number.
  Also the doc's stated MECHANISM for the three Task 11 shapes was wrong (it blamed a driver exception from the
  asserter's snapshot read); the real cause is ClassifyBulkSource's default arm at query-compile time, and the tags
  name a mechanism the doc lacked (a trailing non-entity Select over the join's TransparentIdentifier).
Task 25: ids renumbered EF-X016 -> EF-X022 across 47 comments; VERIFIED COUNT 57 distinct ticket ids / 20 EF-X###
  placeholders, reproduced independently by the reviewer running the command the document documents. sort -u yields
  exactly EF-X001..EF-X011, EF-X013..EF-X020, EF-X022 — EF-X012 and EF-X021 absent as the doc now states.
Task 25: DISCOVERED PRE-EXISTING STALENESS BEYOND THE NAMED PLACES (scoped out, correctly): the closing line said 50
  against an actual pre-change 56, and three table rows are still off — EF-X001 says 144 vs 165 actual, EF-X003 19
  vs 18, EF-X020 168 vs 172. Each needs the tag-vs-#if-guard disambiguation to restate honestly. FOLLOW-UP TICKET.
Task 25: fix round 1/5 (4 addressed, 0 open; commits fd65bc3..5f4fcb1, comment-only in src/ and tests/ — verified
  by `git diff -U0 -- src/ tests/` adding zero non-comment lines)
Task 25: complete (commits 7c58035..5f4fcb1, review clean, 1 fix round)
Task 25: the atomicity sweep is now AUDITABLE — sweep paths stated explicitly (repo root, *.cs/*.md/*.csproj/*.props/
  *.xml/*.json/*.yml, excluding bin/obj and the plan archives). Re-reviewer reconciled my count of 16 against the
  implementer's 14: mine counts matched WORDS over 4 paths (4 lines contain both "atomic" and "atomicity"), theirs
  counts SITES over a wider scope including .claude/agents/storage-reviewer.md. 12 sites + 2 = 14. Both correct for
  their method. EVERY remaining occurrence was inspected and is accurate: the four naming the single-command bulk
  path all state per-document-only atomicity as a LIMITATION; the rest describe transaction-backed atomicity
  (MongoBulkOperationExecutor.cs:237 batching, TransactionTests, CrossCollectionRelationshipTests) or are unrelated
  (the volatile delegate slot at MongoShapedQueryCompilingExpressionVisitor.cs:737).
Task 25: re-reviewer independently verified Minor 3's arithmetic by walking every #if block — EF10-guarded tags at
  :219/:225/:231/:362/:374/:381/:387 (7) and EF9-guarded at :249/:426 (2) = the 9 cited. And that
  ExecuteDeleteTests.cs:540's cross-reference resolves to the INTACT :237 batching comment, not the reworded :71
  banner, so the referring sentence is still true.
Task 25: count re-verified at 57 after the second round of test-comment edits; the three out-of-scope stale rows
  (EF-X001, EF-X003, EF-X020) confirmed untouched.

SUPERSEDED: Task 25: fix round 1/5 IN FLIGHT — resumed a62f95302e01d7361 with Importants 1-2 (two surviving "atomic" claims in
  tests/ comments, missed because the sweep grep was scoped to src/ + the two markdown files while the report
  claimed completeness) + Minors 3, 5, 6 (three one-line accuracy fixes). Deferring Minor 4 (the three stale table
  rows) to the follow-up. FIX_BASE for the re-review = fd65bc3.

SUPERSEDED: NEXT: Task 25 (documentation). THIS TASK CARRIES THE MOST ACCUMULATED DIRECTIVES OF ANY ON THE BRANCH — see the
  consolidated list I will hand it, drawn from Tasks 7, 11, 13, 15, 18 and 22. Critically: the plan's own Task 25
  text says two-phase covers "Skip/Take, or OfType/Cast" — CAST IS NOT SUPPORTED (Task 15 established it does not
  translate at all and left it classifier-rejected), so that sentence must not be copied verbatim.

SUPERSEDED: NEXT: Task 24 (cover bulk operations against encrypted collections). !!! WATCH FOR AN ALWAYS-SKIPPED SUITE !!!
  Per the repo AGENTS.md, if CRYPT_SHARED_LIB_PATH is unset the encryption collection's SupportsEncryption returns
  false and the relevant tests SKIP. A suite that always skips in this environment is worse than none if it is
  reported as coverage — the implementer must state plainly whether the tests actually EXECUTED or were skipped.

SUPERSEDED: NEXT: Task 23 (assert the event payload, not just the rendered message). NOTE: Task 4 ALREADY BUILT the
  DiagnosticSourceSpy this task was planned to create — tests/.../Utilities/DiagnosticSourceSpy (static Create(),
  ConcurrentBag of captured events). Task 16 added phase1Pipeline to the payload and Task 18 changed TargetCount to
  per-batch semantics with N Executing per 1 Executed — both are payload facts this task must respect. Task 5's
  known-false LogSensitiveData XML doc on Executed payloads is STILL not fixed (walked past by Tasks 5, 16, 19).

SUPERSEDED: NEXT: Task 22 (cover the remaining setter shapes). Relevant ledger context: Task 9's deferred minor — "no test
  exercises a MemberInfo/name DIVERGENCE, the exact case Item 1 exists to fix" — may belong here. Task 19's new
  fallback test uses SetProperty(o => o.Status, o => Guid.NewGuid().ToString()); Task 15 added a BuildSetter guard
  rejecting OfType<TDerived>() + self-referencing setters. Task 7 ships TWO setter guards (primary key +
  discriminator); concurrency tokens are PERMITTED.

SUPERSEDED: NEXT: Task 21 (give the async path the same shape coverage as the sync path). Relevant ledger context: Task 17's
  streaming rewrite left NO test covering a failure or cancellation during cursor ITERATION (a mid-loop getMore) and
  explicitly deferred it here; Task 16 added async theory cases asserting phase1Pipeline; Task 12 added the
  cancelled-rollback wire test. CommandCaptureSpy (Task 12) and SpyLoggerProvider.GetLogMessagesByEventId (Task 18)
  are both available.

SUPERSEDED: === PHASE 5: Tasks 16, 17, 18 COMPLETE. Task 19 remains. ===
SUPERSEDED: Task 18: fix round 1/5 IN FLIGHT — resumed a18840adc92f79a2e with Important 1 + Minors 2,3,4,5,6,7.
  Deferred: Minor 8 (four near-duplicate loop bodies — reviewer judged the sync/async x delete/update matrix
  already existed and factoring would trade duplication for indirection) and Minor 9 (CommandCaptureSpy counts
  CommandStartedEvent i.e. ATTEMPTS, so a retried write would inflate counts — pre-existing, low risk inside an
  explicit transaction). FIX_BASE for the scoped re-review = 78a64fb.

=== PHASE 5 IN PROGRESS (Tasks 16-19, performance) ===
NEXT: Task 18 (bound the $in list). Task 5's reviewer noted CombineBulkCommand is Task 18's scope. NEEDS THE
  STORAGE REMAP. Batching interacts with: Task 16's phase1Pipeline/Command composition, Task 12's rollback
  cleanup, and the single-transaction guarantee (README says large target sets are written in batches WITHIN the
  single transaction — see the Task 25 plan text). Northwind 181/181 EF10, 169/169 EF9 if classification or
  shared executor code is touched.

=== PHASE 4 COMPLETE (Tasks 13-15, classification) ===
NEXT: Phase 5 = Tasks 16, 17, 18, 19 (performance). TASKS 16, 17, 18 ALL NEED THE STORAGE REMAP — 16/17 touch
  BuildIdDocumentQuery (Query) AND MaterializeIds/MaterializeIdsAsync (Storage/MongoBulkOperationExecutor.cs).
  Task 16 = project {_id: 1} in phase 1. Note Task 15 left a comment at MongoShapedQueryCompilingExpressionVisitor
  .cs:443-444 anticipating exactly this projection, and Task 15's wire-capture test asserts the phase-1 pipeline
  contains {$match: {_t: "cat"}} — ADDING A PROJECTION STAGE MUST NOT BREAK THAT ASSERTION (it uses
  GetValue(name, null) so a shape change fails cleanly rather than throwing).

SUPERSEDED NOTE (kept for history): Task 15 (support OfType/Cast via the two-phase read path). !!! READ THE TASK 13 DIRECTIVE ABOVE !!!
  OfType/Cast are NOT affected-set-neutral — they narrow the affected set via a discriminator filter, so they
  belong with Skip/Take on the TWO-PHASE side and MUST NOT be added to AffectedSetNeutralDefinitions or to
  BuildFilter's neutral branch. Task 7 also left a note: OfType was classifier-rejected until this task, which is
  why Task 7's derived-type discriminator test had to use Where(a => a is Cat) instead of OfType.
  Classification work => Northwind on EF9 AND EF10 required (181/181, 169/169).

=== PHASE 4 IN PROGRESS (Tasks 13-15, classification) ===
NEXT: Task 14 (match canonical QueryableMethods definitions; accept Order/OrderDescending). This is the task
  that permanently closes the Distinct-comparer-overload hole Task 13's review identified as unreachable-by-luck.
  Classification work => Northwind on EF9 AND EF10 required (181/181, 169/169).

=== PHASE 3 COMPLETE (Tasks 10-12) ===
NEXT: Phase 4 = Tasks 13, 14, 15 (classification). Task 13 = "Only require two-phase when paging is present".
  Classification work => MUST run NorthwindBulkUpdatesMongoTest on EF9 AND EF10 (baselines 181/181 EF10,
  169/169 EF9). Remap: 13/14/15 are Query-side (ClassifyBulkSource) — verify per task; the RESUME block flags
  only 16/17/18 as still needing the Storage remap.

################################################################
=== ALL 26 TASKS COMPLETE — FINAL VERIFICATION (controller) ===
################################################################
Branch EF-361, 49 commits off main @ 58e05a0, head f883aaf.

Task 26: complete (commits 5f4fcb1..f883aaf, review clean, NO FIX ROUND) — implementer a32bb507bebb57473 (sonnet);
  review: spec ✅ all 4 amendments, APPROVED, 0 Critical / 0 Important / 2 Minor. Comment-only in src/ + 1 new test.
  STEPS 1 AND 2 WERE ALREADY DONE by intervening tasks and the implementer correctly CHANGED NOTHING rather than
  manufacturing a diff — RunInBulkTransaction/Async already had the clean two-parameter signatures (:530, :595) from
  Task 11's restructure, and Task 14's if/else conversion had removed the unreachable break (-warnaserror confirms).
  The new VisitChildren test's NON-VACUITY WAS PROVEN both ways: recursing into SourceQuery reaches
  MongoQueryExpression's INHERITED base VisitChildren (NodeType => Extension, CanReduce false, no override) which
  throws ArgumentException("must be reducible node"); a rebuild mutation fails Assert.Same. Reviewer re-derived this
  independently.
  AND THE THRICE-FLAGGED FALSE COMMENT IS FINALLY GONE — MongoNonQueryExpression.cs:128's "this whole type is
  #if !EF8" (introduced Task 14, flagged again by Tasks 16 and 19). Four words, three reviews.

FULL SUITE, ALL THREE CONFIGURATIONS — CONTROLLER-RUN, ZERO FAILURES:
  Debug EF8 : Unit 283/283, Functional 1684 passed / 51 skipped, Spec 4686 passed / 25 skipped  (6653 passed)
  Debug EF9 : Unit 305/305, Functional 1809 passed / 56 skipped, Spec 4999 passed / 25 skipped  (7113 passed)
  Debug EF10: Unit 298/298, Functional 1831 passed / 56 skipped, Spec 4571 passed / 28 skipped  (6700 passed)
  SKIP GROWTH ACCOUNTED FOR, per the plan's own acceptance criterion: EF9/EF10 Functional carry 5 more skips than
  EF8 = Task 24's 4 encryption tests (all #if !EF8, hence absent on EF8) + the 1 pre-existing standalone-deployment
  skip. No unexplained skips.
  All three configurations also build with 0 Warning(s) / 0 Error(s), including under -warnaserror.

THE ONE VISIBLE SKIP IS A KNOWN FINAL-REVIEW ITEM: ExecuteDelete_two_phase_on_standalone_throws_actionable_error is
  still an empty [Fact(Skip)] placeholder. Task 11's reviewer flagged it: IsTransactionsUnsupported's WIRING (BOTH
  arms) is therefore unpinned in the default run, "which is how the dead arm shipped unnoticed in the first place."
  It needs an explicit disposition before merge — implement, delete, or record as a deliberate gap with a ticket.

################################################################
=== FINAL WHOLE-BRANCH REVIEW (opus) — "Ready to merge: WITH FIXES" ===
################################################################
Verdict: 0 Critical, 4 Important, ~14 Minor. Reviewer confirmed the two properties that mattered most:
  - MQL REDACTION IS GENUINELY SINGLE-POINTED. It traced all 8 render sites, both Executed sites (which pass
    command: null AND logSensitiveData: false), and confirmed phase 1 runs via
    executableQuery.Provider.CreateQuery<BsonDocument>() rather than MongoClientWrapper.Execute — so there is NO
    second, ungated ExecutedMqlQuery path for the id-harvesting read. It grepped for one; there is none.
  - CLASSIFICATION CAN NO LONGER DRIFT between the two layers that consume it (one shared IReadOnlySet consumed by
    both ClassifyBulkSource and BuildFilter), and definition matching makes an indexed Where reaching BuildFilter
    with an unbound index parameter STRUCTURALLY IMPOSSIBLE.
  - It also confirmed the Query<->Storage seam was respected: MongoBulkPlan carries only delegates + a collection
    name, no IMongoCollection call leaked into Query and no expression tree into Storage.

!!! THE FINDING WE ALL MISSED — QUERYABLE ENCRYPTION FORBIDS MULTI-DOCUMENT WRITES !!!
  Verified by the reviewer against MongoDB's own docs, quoted verbatim: "Queryable Encryption doesn't support
  multi-document update or delete operations. db.collection.updateMany() and db.collection.bulkWrite() with more
  than one update or delete operation aren't supported." updateMany/deleteMany is ALL the bulk feature ever emits,
  on both strategies. Every test in Task 24's suite runs against a QE collection.
  => On the first CI run with a crypt provider: tests 1 and 2 go RED; tests 3 and 4 go GREEN PROVING NOTHING.
  Corroborating: NO pre-existing encryption test issues a multi-document write — all of QueryableEncryptionTests
  goes through SaveChanges, which emits per-document ops.
  SECOND, COMPOUNDING CAUSE ON EF9: the shared Patient entity has FOUR owned navigations, which are auto-included
  by convention — exactly the defect Task 22 documented. So the EF9 auto-include defect hides in precisely the one
  file nobody can run. ExecuteUpdateTests works around it with AutoInclude(false); this file does not.
  This is the intersection of THREE separately-known ledger items that nobody connected.

NEW FINDING (Important 3) — DERIVED TPH DbSet GETS NO DISCRIMINATOR FILTER. MongoEntityTypeExtensions.cs:72-76 maps
  a derived type to its ROOT's collection; CreateShapedQueryExpression (:177-186) adds no discriminator predicate
  (unlike EF's relational visitor); BuildFilter returns FilterDefinition.Empty with no Where. So with both
  DbSet<Animal> and DbSet<Cat>, db.Cats.ExecuteDelete() issues deleteMany({}) against the Animals collection.
  PRE-EXISTING and provider-wide (reads are affected too, so not a break) — but this branch put a DELETE on that
  path, escalating "wrong rows returned" to "wrong documents destroyed". Reached by CODE READING, not by a test.

ARTHUR'S DECISIONS (2026-08-01), all three as recommended:
  1. TPH gap: VERIFY WITH A TEST FIRST, then guard in VisitNonQuery on entityType.BaseType != null if it
     reproduces, plus a ticket for the read-path gap. (Reviewer confirmed such a guard would NOT break OfType —
     TranslateOfType leaves CollectionExpression.EntityType as the root.)
  2. Encryption suite: assert CLEAN REJECTION on QE, type both catches, add AutoInclude(false), fix the header's
     false CSFLE claim. Do NOT write a second CSFLE suite on an unverified premise.
  3. Empty [Fact(Skip)]: DELETE IT and ticket the coverage, paired with the unit-testable extraction of the
     transaction-error mapping.

UNDISCHARGED PLAN OBLIGATION FOUND BY THE REVIEW: review-map row 20 ("No error event on failure") carries its own
  disposition — "deferred — record as a ticket in Final verification" — and no such ticket was ever recorded.
  RECORDED HERE NOW as an explicit deferral for Arthur to file.
