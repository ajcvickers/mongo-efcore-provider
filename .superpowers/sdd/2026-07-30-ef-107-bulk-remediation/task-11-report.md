# Task 11 report — Translate before opening the transaction, and give phase 1 the canonical failure

## What changed and why

Two ordering defects in the two-phase (`OrderBy`/`Skip`/`Take`/`Distinct`-scoped) `ExecuteUpdate`/`ExecuteDelete`
path, both in the current (post-Task-10) architecture where `MongoShapedQueryCompilingExpressionVisitor.CreateBulkPlan`
(Query) builds a `MongoBulkPlan` carrying deferred `Func<QueryContext, …>` delegates that
`MongoBulkOperationExecutor` (Storage) invokes at runtime:

1. **`BuildUpdate` was invoked lazily inside the transaction body, only when `ids.Count != 0`.** Hoisted: both
   `ExecuteTwoPhaseUpdate` and `ExecuteTwoPhaseUpdateAsync` now call `plan.BuildUpdate!(queryContext)` *before*
   calling `RunInBulkTransaction`/`RunInBulkTransactionAsync`, and pass the built `update` into the body as a
   captured local. `BuildUpdate` was already wrapped in `TranslateBulkOrThrow` by `CreateBulkPlan`, so an
   unsupported setter now surfaces as EF's canonical translation failure before any transaction is opened, and
   before phase 1 runs at all. This deliberately changes behavior: translation of the update document now
   always runs, even when phase 1 later finds nothing to update — failing fast with the correct error beats
   silently skipping translation on an empty match (explicitly directed in the task).

2. **`RunInBulkTransaction`/`RunInBulkTransactionAsync` had one `try` spanning `BeginTransaction` and the body.**
   Any exception, including a phase-1 translation failure, got the same "is this a transactions-unsupported
   error?" check, so on a standalone deployment a translation failure could theoretically get relabelled — and,
   more importantly, an untranslatable query on *any* deployment would still pay for and roll back a transaction
   before its own error surfaced. Split into two `try` blocks: the first wraps only `database.BeginTransaction()`
   (mapped to `StandaloneTransactionError` on `IsTransactionsUnsupported`), the second wraps `body()` (any
   failure triggers `SafeRollback`/`SafeRollbackAsync` and rethrows untouched — no exception-type check). Applied
   to both the sync and async orchestrators; the ambient-transaction fast path (`RequireBulkSession`) and the
   `AutoTransactionBehavior.Never` check ahead of `BeginTransaction` are both preserved exactly as they were.

3. **Phase 1 (`BuildTargetIdQuery`) was never wrapped by `TranslateBulkOrThrow` at all** — see the design
   question below for what this actually required.

## Where phase-1 translation actually throws, and how I established it

I added a temporary probe test (`db.Entities.Where(o => o.Quantity > db.Entities.GroupBy(x => x.Status)
.Where(g => g.Count() > 100).Select(g => g.Key.Length).FirstOrDefault()).OrderBy(o => o.Quantity).Take(1)
.ExecuteUpdate(...)` — a `GroupBy` subquery inside a `Where` predicate, combined with `OrderBy`+`Take` to force
the `TwoPhase` strategy) and ran it against the **unmodified** code, catching the exception and printing its
stack trace. Result: `MongoDB.Driver.Linq.ExpressionNotSupportedException`, thrown from
`MongoDB.Driver.Linq.Linq3Implementation.MongoQuery<TDocument,TOutput>.GetEnumerator()` → `.Execute()`, called
from `MongoBulkOperationExecutor.MaterializeIds`'s `foreach` loop — i.e. **at enumeration time, in Storage**, not
from `BuildIdDocumentQuery`'s build call (which just constructs the `IQueryable` via
`executableQuery.Provider.CreateQuery<BsonDocument>(...)` and never throws for this shape). The probe test and
its output are not part of the final diff; the finding is recorded here per the report contract.

## Which wrapping approach I chose, and why

**Both**, because the two throw sites are real and distinct:

- `BuildTargetIdQuery`'s delegate in `CreateBulkPlan` is now wrapped in `TranslateBulkOrThrow`, exactly like
  `BuildFilter`/`BuildUpdate` — this catches any shape that fails at query-*construction* time (inside the
  EF→driver-LINQ bridge, which is Query-side C# code, not the driver's lazy pipeline builder).
- For the empirically-confirmed enumeration-time throw, I added `MongoBulkPlan.WrapTargetIdTranslationFailure`
  (`Func<Exception, Exception>?`, non-null only for `TwoPhase`), built in `CreateBulkPlan` as
  `exception => BuildBulkTranslationFailure(nonQuery, exception)`. `BuildBulkTranslationFailure` is a new shared
  private helper in `MongoShapedQueryCompilingExpressionVisitor` that `TranslateBulkOrThrow` itself now also
  calls, so the message-construction logic (which needs `nonQuery.SourceQuery.CapturedExpression`, owned by
  Query) has exactly one implementation. `MongoBulkOperationExecutor.MaterializeIds`/`MaterializeIdsAsync` take
  this delegate as a parameter and wrap their enumeration/`ToListAsync` call in a `try`/`catch (Exception
  exception) when (exception is not InvalidOperationException)` that invokes it — mirroring
  `TranslateBulkOrThrow`'s own filter. This is the "seam-respecting" option the task described: the message text
  is still constructed in Query (`CreateBulkPlan`); Storage only *invokes* a delegate, never builds
  `CoreStrings.NonQueryTranslationFailedWithDetails(...)` itself. `TranslateBulkOrThrow` was not moved and no new
  method was added to Storage that knows about the message format.

I chose to keep both wraps (rather than only the materialization one, since that's where my probe actually threw)
because the build-time wrap is cheap, consistent with the existing `BuildFilter`/`BuildUpdate` pattern, and is a
correct defense for any future/other shape that *does* throw at `BuildIdDocumentQuery` build time (e.g. a failure
inside `MongoEFToLinqTranslatingExpressionVisitor` itself, which runs synchronously during the build call, not
lazily). Belt-and-suspenders, not speculative — every code path through `BuildTargetIdQuery` and materialization
is now covered by exactly one canonicalizing wrap.

## Step 1 test: does it discriminate?

The brief's literal suggested shape (`SetProperty(o => o._id, ObjectId.GenerateNewId())` under
`AutoTransactionBehavior.Never`) was verified to **pass trivially, both before and after the fix** — I ran it
against unmodified `main` first and confirmed it green. Cause: a primary-key setter target is rejected by
`MongoQueryableMethodTranslatingExpressionVisitor.BuildSetter` during **LINQ translation** (i.e. while the
`ExecuteUpdate(...)` call itself is being translated by EF Core's query pipeline), which runs long before
`VisitNonQuery`/`CreateBulkPlan`/any `MongoBulkPlan` exists — so this task's transaction/translation reordering
is entirely irrelevant to that shape.

Per the brief's fallback instruction, I replaced the setter with one that only fails when `BuildUpdate` actually
executes: a self-referencing setter with more than one `Employee`-typed parameter
(`s.SetProperty(e => e.Rank, e => e.DirectReports.Max(r => r.Rank))`), which `RebindSetterValueParameters` (via
`RenderSelfReferencingValue`, called from `BuildUpdate`) rejects with "more than one" — a check
`MongoQueryableMethodTranslatingExpressionVisitor.BuildSetter` does not perform, so it only fires when
`BuildUpdate` is invoked at plan-execution time. Combined with a two-phase source (`OrderBy(e => e.Rank).Take(1)`)
and `AutoTransactionBehavior.Never`, I confirmed by running against unmodified code that the test **fails**
before the fix (message is the `AutoTransactionBehavior` ordering error, because `BuildUpdate` was never reached —
`RunInBulkTransaction` threw `NoAutoTransactionError` before calling `body()`) and **passes** after Steps 3–4
(the "more than one" setter error surfaces first). The test (`ExecuteUpdate_two_phase_with_an_invalid_setter_fails_before_starting_a_transaction`
in `ExecuteUpdateTests.cs`) genuinely discriminates; this is documented in an inline comment on the test itself.

## Conformance cases reclassified

Running the full `NorthwindBulkUpdatesMongoTest` suite (no failures either before or after re-tagging) surfaced
five cases tagged `EF-X002` with `Assert.ThrowsAnyAsync<Exception>`. I probed each individually (temporary probe
`[Fact]`, removed from the final diff) against both the unmodified and the fixed code:

- `Update_Concat_set_constant`, `Update_Union_set_constant` — **unchanged, left as-is.** Both throw
  `MongoDB.Driver.MongoCommandException: Command aggregate failed: Stage not supported inside of a
  multi-document transaction: $unionWith.` on both unmodified and fixed code. This confirms
  `docs/failing-spec-tests.md`'s existing explanation: the failure originates from the conformance asserter's own
  before/after snapshot query (which mirrors the `Concat`/`Union` source as `$unionWith`), not from this task's
  two-phase translate/transaction path. Not moved.

- `Update_with_LeftJoin` (EF10), `Update_with_LeftJoin_via_flattened_GroupJoin` (EF10), `Update_with_join_set_constant`
  (EF9+EF10) — **moved from `Assert.ThrowsAnyAsync<Exception>` to `AssertTranslationFailed`.** All three now
  throw (and, per the probe, **already threw before this task's fix too**) `InvalidOperationException` with EF's
  canonical "could not be translated" message, reported via
  `ClassifyBulkSource`/`ThrowBulkSourceNotSupported` — the captured chain for these shapes ends in a non-entity
  `Select` (projecting the join's `TransparentIdentifier`), which is rejected as an unsupported top-level operator
  before any translation of the join itself is attempted. This is unrelated to phase-1/transaction ordering; the
  existing `docs/failing-spec-tests.md` explanation for these three (driver `ExpressionNotSupportedException` from
  the conformance asserter's snapshot read of the join) was already stale by the time I started this task — I
  verified this empirically against both the pre-fix and post-fix code and got byte-identical output. Per the
  task's global constraint ("if any now produce the canonical translation-failure message, change that case's
  assertion... report every case you move"), I reclassified all three to `AssertTranslationFailed` and re-tagged
  their `// Fails:` comment from `EF-X002` to the `EF-X016` "joins / correlated subqueries unsupported" pattern
  already used for sibling join cases, with an inline note that the re-tag is not this task's own fix and was
  discovered during verification. I did not edit `docs/failing-spec-tests.md` (a later task reconciles it), but
  flag here that its EF-X002/EF-X016 sections should drop these three from EF-X002's list when that reconciliation
  happens.

No other conformance case changed outcome.

## Stopwatch / phase-spanning-elapsed behavior

Preserved intact. `ExecuteTwoPhaseUpdate`/`ExecuteTwoPhaseUpdateAsync`/`ExecuteTwoPhaseDelete`/`ExecuteTwoPhaseDeleteAsync`
still create the `Stopwatch` inside the transaction body, immediately before the phase-1 call, and thread it into
`UpdateByIds`/`DeleteByIds` (and their async twins) unchanged — only `BuildUpdate`'s call site moved (outside the
transaction, before the stopwatch starts), which is correct: the stopwatch is meant to span phase 1 (the
expensive read) plus phase 2 (the write), not update-document construction. `BulkOperationsCommandTests` (which
contains the `Elapsed > TimeSpan.Zero` assertions on two-phase `Executing` payloads) passes: 13/13 on EF10, 13/13
on EF9.

## Test commands and results

All runs with `MONGODB_URI`/`ATLAS_URI` unset (TestContainers `mongodb/mongodb-atlas-local`).

```
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded, 0 warnings, 0 errors
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    → Build succeeded, 0 warnings, 0 errors
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    → Build succeeded, 0 warnings, 0 errors
```

```
$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
    --filter "FullyQualifiedName~ExecuteUpdate_two_phase_with_an_invalid_setter_fails_before_starting_a_transaction"
Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
    --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
Passed! - Failed: 0, Passed: 65, Skipped: 1, Total: 66
  (the 1 skip is ExecuteDelete_two_phase_on_standalone_throws_actionable_error, a standalone-deployment-only test
  unrelated to this change)

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
    --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
Passed! - Failed: 0, Passed: 53, Skipped: 1, Total: 54

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
    --filter "FullyQualifiedName~BulkOperationsCommandTests"
Passed! - Failed: 0, Passed: 13, Skipped: 0, Total: 13

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
    --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181   ← EF10 Northwind count

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
    --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
Passed! - Failed: 0, Passed: 169, Skipped: 0, Total: 169   ← EF9 Northwind count (ran this in addition to the
  brief's EF10-only Step 6 command, since Update_with_join_set_constant — one of the reclassified cases — applies
  to EF9 too and I wanted both configurations verified for the reclassification)
```

## Things noticed but deliberately not changed

- The stale `// Fails: cross-collection join (translatable post-EF-117)...` comments on the three reclassified
  Northwind cases predate this task and were already inaccurate the moment EF-117's join work landed (or
  whichever earlier change made `ClassifyBulkSource` reject the `.Select(...)` before the join's own translation
  ran) — this task only touches the two-phase transaction/translation ordering, so I didn't investigate further
  which earlier commit made this true; I only re-verified and re-tagged.
- `docs/failing-spec-tests.md` still lists these three under EF-X002's affected set; per this task's global
  constraint I left the doc untouched (a later task reconciles it) but flagged the discrepancy above for that task.
- I did not touch `BuildFilter`'s Step-5 guard (`Debug.Assert`/fail-loud comment) — confirmed already present in
  `main` at the "Fail loud rather than silently drop" comment, per Correction B.
- No change was made to `MongoEventId`, `RenderBulkCommand`/`ShouldLogSensitiveData()`, `RequireBulkSession`,
  `GetSetElementKey`, `RebindSetterValueParameters`, or the primary-key/discriminator setter guards — all
  untouched, as required.

---

# Fix round 1

Addresses the coordinator's review findings on the initial implementation. Summary of what changed, in the
order the review raised it.

## CRITICAL 1 — narrowed the phase-1 catch

The original `MaterializeIds`/`MaterializeIdsAsync` copied `TranslateBulkOrThrow`'s `when (exception is not
InvalidOperationException)` filter verbatim onto a real I/O site (a `find` running inside the caller's
transaction), which would have mislabelled a cancelled `ExecuteUpdateAsync`/`ExecuteDeleteAsync`
(`OperationCanceledException`/`TaskCanceledException`) and genuine driver/server failures (`WriteConflict`,
`SnapshotUnavailable`, `MongoExecutionTimeoutException`, connection/topology errors, ...) as "could not be
translated" — reintroducing, at a new site, the exact defect class this task exists to remove, and one this
repo already pins as a regression risk for the transaction wrapper
(`UnitTests/Storage/BulkTransactionErrorClassificationTests.cs`).

**Exception type settled on: `MongoDB.Driver.Linq.ExpressionNotSupportedException`, and only that type.** This
is the exact type the committed reproducer (see Important 3) observes the driver's LINQ provider throw for the
`GroupBy`-subquery-inside-`Where` shape. I did not add `NotSupportedException`/`ArgumentException` speculatively
— the reviewer's instruction was to determine the set from the reproducer, and the reproducer only needs
`ExpressionNotSupportedException`. Both `MaterializeIds` and `MaterializeIdsAsync` now catch only that type; a
cancellation or any other exception propagates completely unchanged (no filter, no rethrow-and-rewrap — it
never enters the catch at all).

**A second, deeper problem surfaced while narrowing this**, worth recording since it changes *where* the try
block sits: I initially assumed (from the original, unnarrowed code) that the throw happens inside the
enumeration loop, so I first rewrote `MaterializeIds` with an explicit-enumerator loop that wrapped only
`MoveNext()`. That version's reproducer test failed — the raw `ExpressionNotSupportedException` reached the
test uncaught. Reading the reproducer's own stack trace explained why:
`MongoDB.Driver.Linq.Linq3Implementation.MongoQuery<TDocument,TOutput>.GetEnumerator()` calls its own
`Execute()` **inside `GetEnumerator()` itself** — i.e. the driver's `IQueryable<BsonDocument>` translates and
sends the query eagerly when the enumerator is obtained, not lazily on the first `MoveNext()`. So the try now
wraps `documents.GetEnumerator()` specifically (via an explicit `try`/`catch` around that one call, then a
`using (enumerator)` block for the iteration, which is exactly what the reviewer asked for structurally: the
per-document `ids.Add`/`Current` access sits fully outside any catch). `MaterializeIdsAsync`'s existing shape
(a `try` around the single `await ... ToListAsync(...)` call, with the `ids.Add` loop already outside it) turned
out to already be correctly scoped — `ToListAsync` translates, sends, and materializes in the one awaited call,
so there's no separate step to pull out — it only needed the exception-type narrowing, not a restructure.

## IMPORTANT 2 — restored the narrow body-side re-map

Verified the reviewer's diagnosis is correct by inspection: `IsTransactionsUnsupported`'s `NotSupportedException`
arm ("does not support transactions") is a client-side check `MongoTransaction.Start` raises, reachable only from
`BeginTransaction`/`BeginTransactionAsync` — already covered by the first `try`. Its `MongoException` arm
("Transaction numbers are only allowed on a replica set member or mongos") is a *server* response to a command
carrying a `txnNumber`, which by definition cannot happen before a transaction has started — so it can only ever
be observed from `body()`, which the initial restructure had stopped mapping at all, meaning a topology that
looks transactional at `BeginTransaction` time but turns out not to support transactions once the phase-1/phase-2
commands actually run would surface a raw `MongoCommandException` instead of the actionable
`StandaloneTransactionError`.

**The narrow body-side re-map is safe, and I restored it in both `RunInBulkTransaction` and
`RunInBulkTransactionAsync`.** In both, the body catch is now `catch (Exception exception)` and, after
`SafeRollback`/`SafeRollbackAsync`, re-maps to `StandaloneTransactionError(exception)` only when
`IsTransactionsUnsupported(exception)` is true; everything else still hits a bare `throw;`. This is safe for two
independent reasons, both now recorded in the code comment: (1) `IsTransactionsUnsupported` only matches
`exception is MongoException` or `exception is NotSupportedException`, and (2) every canonical translation
failure produced anywhere in the bulk path — `BuildBulkTranslationFailure`/`TranslateBulkOrThrow` on the Query
side, invoked either directly or via `WrapTargetIdTranslationFailure` — is constructed as (typed as, per Minor 4
below) `InvalidOperationException`, a type `IsTransactionsUnsupported` can never match. So the re-map cannot
swallow a translation failure regardless of which of the two facts holds; they're independently sufficient, not
just safe in combination. I did not find a case where a narrow re-map here is unsafe, so I implemented it as
asked rather than reporting a blocker.

## IMPORTANT 3 — committed the reproducer

Added `ExecuteUpdate_two_phase_wraps_a_translation_failure_thrown_during_phase_1_materialization` to
`ExecuteUpdateTests.cs` as a `[Theory]` with `[InlineData(false)]`/`[InlineData(true)]` (sync/async), asserting
`Assert.Throws<InvalidOperationException>`/`Assert.ThrowsAsync<InvalidOperationException>` with `"could not be
translated"` in the message. Shape: a `GroupBy` subquery inside a `Where` predicate
(`db.Entities.GroupBy(x => x.Status).Where(g => g.Count() > 100).Select(g => g.Key.Length).FirstOrDefault()`),
combined with `OrderBy(o => o.Quantity).Take(1)` to force the `TwoPhase` strategy. This is committed and now
exercises `WrapTargetIdTranslationFailure` on both the sync and async paths — it is the same shape used to
empirically locate the throw site in the original submission (there, via an uncommitted temporary probe, which
review correctly flagged as the third instance of this pattern on this branch).

Added a companion test, `ExecuteUpdateAsync_two_phase_propagates_cancellation_unchanged_during_phase_1_materialization`,
using an already-cancelled `CancellationTokenSource` passed to `ExecuteUpdateAsync` over the same two-phase
shape, asserting `Assert.ThrowsAnyAsync<OperationCanceledException>` — i.e. proving cancellation is *not* mapped
to the canonical translation failure, which is exactly what Critical 1's narrowed catch is for. Both tests are
cross-referenced from the narrowed catch's comment in `MongoBulkOperationExecutor.cs`.

## Minor 4 — narrowed the delegate's return type

`MongoBulkPlan.WrapTargetIdTranslationFailure` is now `Func<Exception, InvalidOperationException>?` (was
`Func<Exception, Exception>?`). `BuildBulkTranslationFailure` already returned `InvalidOperationException`, so
`CreateBulkPlan`'s assignment needed no change; `MaterializeIds`/`MaterializeIdsAsync`'s parameter type was
updated to match, so the executor's narrow catch is now statically guaranteed to produce the canonical exception
type rather than merely "some replacement exception".

## Minor 5 — `ExecuteTwoPhaseUpdateAsync` is `async` again

Added `async` back to `ExecuteTwoPhaseUpdateAsync` and `await`s `RunInBulkTransactionAsync(...)` rather than
returning its `Task` directly, restoring the convention that a synchronous failure while building `update` (a
`BuildUpdate` translation failure) surfaces as a faulted `Task<int>` rather than throwing synchronously out of
the method call — consistent with `ExecuteTwoPhaseDeleteAsync` and every other `*Async` method in this class,
and with how EF Core's async query pipeline expects `Task`-returning provider methods to behave.

## Minor 6 — documented the setter-vs-ambient-session ordering

Added a comment on `ExecuteTwoPhaseUpdate` (the hoisted `BuildUpdate` call) recording that hoisting also moved
setter translation ahead of `RunInBulkTransaction`'s `RequireBulkSession` check, so with both an invalid setter
and a foreign (non-MongoDB) ambient transaction, the caller now sees the setter's translation error rather than
the "requires a MongoDB transaction" error. No behavior change beyond what Task 11 already intended; this is
purely making the choice legible as intentional.

## Is the narrow body-side re-map safe? Yes — see IMPORTANT 2 above for the two independent reasons.

## Covering tests: commands and output

All runs with `MONGODB_URI`/`ATLAS_URI` unset (TestContainers `mongodb/mongodb-atlas-local`).

```
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded, 0 warnings, 0 errors
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    → Build succeeded, 0 warnings, 0 errors
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    → Build succeeded, 0 warnings, 0 errors
```

```
$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
    --filter "FullyQualifiedName~ExecuteUpdate_two_phase_wraps_a_translation_failure_thrown_during_phase_1_materialization|FullyQualifiedName~ExecuteUpdateAsync_two_phase_propagates_cancellation_unchanged_during_phase_1_materialization"
Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3
  (the Theory's two cases: async=false, async=true; plus the cancellation companion test)

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
    --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~BulkTransactionErrorClassificationTests"
Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4    (UnitTests.dll — BulkTransactionErrorClassificationTests)
Passed! - Failed: 0, Passed: 68, Skipped: 1, Total: 69  (FunctionalTests.dll — 65 prior + 3 new; the 1 skip is
  ExecuteDelete_two_phase_on_standalone_throws_actionable_error, standalone-deployment-only, unrelated)

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
    --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181   ← EF10 Northwind count, unchanged

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
    --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~BulkTransactionErrorClassificationTests"
Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4    (UnitTests.dll)
Passed! - Failed: 0, Passed: 69, Skipped: 1, Total: 70  (FunctionalTests.dll — BulkOperationsCommandTests isn't
  EF-version-gated beyond #if !EF8, so it's included here too)

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
    --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
Passed! - Failed: 0, Passed: 169, Skipped: 0, Total: 169   ← EF9 Northwind count, unchanged
```

## Reproducer shape (repeated here per the report contract)

```csharp
db.Entities
    .Where(o => o.Quantity > db.Entities.GroupBy(x => x.Status).Where(g => g.Count() > 100)
        .Select(g => g.Key.Length).FirstOrDefault())
    .OrderBy(o => o.Quantity).Take(1)
    .ExecuteUpdate(s => s.SetProperty(o => o.Status, "x"));
```

`OrderBy`+`Take` forces the `TwoPhase` strategy; the `GroupBy` subquery inside `Where` is not rejected by
`ClassifyBulkSource` (top-level chain only) nor by `BuildIdDocumentQuery`'s build call, and throws
`MongoDB.Driver.Linq.ExpressionNotSupportedException` specifically from
`MongoQuery<TDocument,TOutput>.GetEnumerator()` (sync) / the awaited `ToListAsync(...)` call (async) inside
`MaterializeIds`/`MaterializeIdsAsync`.
