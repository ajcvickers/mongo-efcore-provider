# Transaction Enlistment and Sharing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A `DbContext` can enlist in a MongoDB transaction it does not own (a caller-supplied session, or another context's transaction), several contexts can share one transaction, and a transaction that has ended can never be used silently.

**Architecture:** `MongoTransaction` gains a non-owning mode in which `Dispose` detaches instead of aborting and disposing the session, and `Commit`/`Rollback` are rejected. Every read of the transaction's session goes through one guarded accessor that throws if the session's transaction has ended. Enlistment is exposed as `DatabaseFacade.UseTransaction(...)` extension methods backed by two new `IMongoTransactionManager` members.

**Tech Stack:** C#, EF Core 8-11 (build configurations `Debug EF8|EF9|EF10|EF11`), MongoDB C# driver 3.11.2, xUnit v3 (FunctionalTests), a real MongoDB replica set (Atlas-local container started by the tests).

**Spec:** GitHub issue #87 (https://github.com/ajcvickers/mongo-efcore-provider/issues/87), including the probe findings and decisions in it. Decision 3 of that issue is corrected below under Global Constraints.

## Global Constraints

- Branch `issue-87-transactions`, created from `release`. Stay in this checkout (`D:\code\provider4`); never touch a sibling clone, its branches or its containers.
- Commit messages and the PR title have **no JIRA prefix** (this fork does not use it). Plain title, optionally ending `(Refs #87)`. Commits end with `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`; the PR body ends with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.
- Ambient `System.Transactions` stays unsupported. Savepoints stay unsupported. Do not change `MongoTransactionEnlistmentManager`.
- No new public constructor or signature change on `MongoTransaction` (its primary constructor is public). `MongoTransaction` stays sealed.
- Non-owned transactions: `Dispose`/`DisposeAsync`/`ResetState` only detach; `Commit`/`Rollback` (sync, async, and the manager-level `CommitTransaction`/`RollbackTransaction`) throw `InvalidOperationException`. The session's lifetime stays with its owner.
- Guard exact wording to pin in tests: `"is no longer active"` (guard), `"not started by this context"` (commit/rollback rejection), `"active transaction"` (enlisting a session with no transaction), `"MongoDB transaction"` (enlisting a foreign `IDbContextTransaction`).
- **Correction to #87 decision 3:** the guard checks `session.IsInTransaction`. That detects a commit or abort made through the driver (by the owner or the caller). It does **not** detect a server-side abort after a failed write: the client still reports `IsInTransaction == true` (probed), and the next operation fails loudly with a `MongoCommandException` labelled `TransientTransactionError`, which must be left to surface unchanged. On a sharded cluster that failure can appear only at `Commit`.
- The guard applies to owned and non-owned transactions alike, wherever the provider reads the session of `Database.CurrentTransaction`.
- Preserve each existing file's BOM; new files copy the BOM-or-not of their neighbours. Annotate nullability. Public members get XML docs. Library code uses `ConfigureAwait(false)`; the new enlistment methods are synchronous (no I/O).
- The code must compile for all four configurations; no `#if` is expected for this work.
- Use `Debug EF10` for the edit-test loop. Run the tests with `MONGODB_URI` and `ATLAS_URI` unset (the tests boot their own container).
- `AGENTS.md` files: update the area guide (`Storage/AGENTS.md`) because this change applies upstream too (Task 4). Do not edit the root `AGENTS.md` (its JIRA convention is fork-specific and stays as is).
- Out of scope: ambient transactions, savepoints, the retrying execution strategy (#88), the spec-test change that removes the reseed (#77).

## Review Focus

- The owner disposes (or a third context disposes) while another context still uses the shared transaction: the session must stay alive (the #77 failure, `ObjectDisposedException`). Pinned in Task 2.
- The owner commits or rolls back while an enlisted context still has pending changes: its `SaveChanges` must throw, not write outside a transaction, and nothing may be persisted. Pinned in Tasks 1 and 2.
- `UseTransaction` called with a session that has no active transaction, a context that already has a transaction, or a foreign `IDbContextTransaction`: `InvalidOperationException`, state unchanged. Pinned in Task 2.
- A failed write inside a shared transaction: the next operation and the owner's `Commit` fail, nothing is persisted, and the guard does not mask the server's error. Pinned in Task 2.
- Queries and bulk `ExecuteDelete`/`ExecuteUpdate` from an enlisted context run inside the shared transaction (see uncommitted writes, roll back with the owner). Pinned in Task 2.

---

### Task 1: Session guard (`MongoTransaction.ActiveSession`)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Storage/MongoTransaction.cs` (replace the `Session` property at line 104)
- Modify: `src/MongoDB.EntityFrameworkCore/Storage/MongoDatabaseWrapper.cs:86,131`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs:1650,1659,1669,1896`
- Modify: `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs:507,569`
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Storage/TransactionTests.cs`

**Interfaces:**
- Produces: `internal IClientSessionHandle ActiveSession { get; }` on `MongoTransaction`. Returns the session if `session.IsInTransaction`, else throws `InvalidOperationException` whose message contains `"is no longer active"` and the transaction id. The `Session` property is removed; every use becomes `ActiveSession`. Nothing else in the file changes.

- [ ] **Step 1: Create the branch** `git switch -c issue-87-transactions` (from `release`, clean tree).

- [ ] **Step 2: Write the failing tests** in `TransactionTests`. Each begins an explicit transaction with `db.Database.BeginTransaction()` and ends the session's transaction behind the context's back through one private helper, `EndTransactionBehindTheContextsBack(DbContext db)`, which calls `AbortTransaction()` on `((MongoTransaction)db.Database.CurrentTransaction!).Session` (the property that exists before Step 4; Step 4 renames it to `ActiveSession` in this helper, which is valid because the transaction is still active when the helper runs):
  - `SaveChanges_after_the_transaction_session_was_aborted_throws_instead_of_writing_outside_a_transaction` (sync) and `..._async`: add an entity, abort the session, `Assert.Throws<InvalidOperationException>` containing `"is no longer active"`; a fresh context then sees 0 documents.
  - `Query_after_the_transaction_session_was_aborted_throws`: `db.Entities.ToList()` throws the same.
  - `ExecuteDelete_after_the_transaction_session_was_aborted_throws`: `db.Entities.ExecuteDelete()` throws the same (this path is `#if !EF8`; guard the test the way `ExecuteDeleteTests.cs` does).

- [ ] **Step 3: Run to verify they fail**
  Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"` then `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~TransactionTests"`
  Expected: the new tests FAIL (writes/queries succeed outside a transaction).

- [ ] **Step 4: Implement `ActiveSession`** and switch the seven call sites listed above to it. Update the Step 2 helper to use `ActiveSession`.

- [ ] **Step 5: Run the full `TransactionTests` class on EF10**
  Expected: PASS, including every pre-existing test.

- [ ] **Step 6: Commit**
  `git add -A && git commit -m "Transactions: fail when the session's transaction has ended instead of running outside it (Refs #87)"`

---

### Task 2: Non-owned transactions and `UseTransaction`

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Storage/MongoTransaction.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Storage/IMongoTransactionManager.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Storage/MongoTransactionManager.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Extensions/MongoDatabaseFacadeExtensions.cs`
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Storage/TransactionTests.cs`

**Interfaces:**
- Consumes: `MongoTransaction.ActiveSession` (Task 1).
- Produces on `MongoTransaction`:
  - `internal static MongoTransaction Enlist(IClientSessionHandle session, DbContext context, string ownerDescription, IMongoTransactionManager? transactionManager, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> transactionLogger)`: new `TransactionId` (`Guid.NewGuid()`), state `Active`, `IsOwner == false`.
  - `internal bool IsOwner { get; }` (true for every existing creation path).
  - Non-owned behaviour: `Commit`, `CommitAsync`, `Rollback`, `RollbackAsync` throw `InvalidOperationException` containing `"not started by this context"` and `ownerDescription`; `Dispose`/`DisposeAsync` set the state to `Disposed` **first**, then call `transactionManager?.ResetState()` / `ResetStateAsync()` (the state check makes the manager's re-entrant `Dispose` a no-op), and never abort or dispose the session.
- Produces on `IMongoTransactionManager`: `IDbContextTransaction UseTransaction(IClientSessionHandle session)` and `IDbContextTransaction UseTransaction(IDbContextTransaction transaction)`. Both throw `InvalidOperationException` (before changing state) when: `CurrentTransaction != null` or an ambient transaction exists (reuse `EnsureNoTransactions`); the session has no active transaction (message contains `"active transaction"`); the argument is not a `MongoTransaction` (message contains `"MongoDB transaction"`); or the argument's `ActiveSession` throws. `ArgumentNullException` for null arguments. Owner descriptions: `"the code that supplied the session"` and `$"{owner.Context.GetType().Name} (transaction {owner.TransactionId})"`. Sets and returns `CurrentTransaction`.
- Produces on `MongoDatabaseFacadeExtensions`: `public static IDbContextTransaction UseTransaction(this DatabaseFacade databaseFacade, IClientSessionHandle session)` and `public static IDbContextTransaction UseTransaction(this DatabaseFacade databaseFacade, IDbContextTransaction transaction)`, both delegating to the manager through the existing `GetMongoTransactionManager`.
- Manager-level `CommitTransaction`/`RollbackTransaction` (and async) need no change: they call the transaction's `Commit`/`Rollback`, which now throw; confirm they do not null `CurrentTransaction` before that call.

- [ ] **Step 1: Write the failing tests** (all in `TransactionTests`; use `SingleEntityDbContext.Create(collection)`; a second client for the cross-client test comes from `new MongoClient(database.Client.Settings.Clone())`, with its own collection handle passed to `Create`; add a small private `StubTransaction : IDbContextTransaction` for the foreign-transaction case):
  - `UseTransaction_with_a_session_enlists_the_context_and_leaves_the_session_to_its_owner`: start a session and `StartTransaction`; `db.Database.UseTransaction(session)`; add and `SaveChanges`; a separate context sees 0 documents; `db.Dispose()`; `Assert.True(session.IsInTransaction)`; `session.CommitTransaction()`; a separate context now sees 1.
  - `Two_contexts_can_share_one_transaction_and_disposing_one_does_not_end_it` (the #77 case): owner `BeginTransaction()`; second context `UseTransaction(owner.Database.CurrentTransaction!)`, saves one entity, is disposed; owner saves another entity (no exception); a third context sees 0; owner `CommitTransaction()`; a fourth sees 2.
  - `Rolling_back_the_owner_discards_writes_of_enlisted_contexts_and_their_next_SaveChanges_throws`: after owner `RollbackTransaction()`, nothing persisted, and the enlisted context's `SaveChanges` throws `"is no longer active"`.
  - `Commit_and_Rollback_are_rejected_on_an_enlisted_transaction` (sync and async, via the returned transaction and via `Database.CommitTransaction()`/`RollbackTransaction()`): throws containing `"not started by this context"`; afterwards `session.IsInTransaction` is still true and the owner can still commit.
  - `Disposing_the_returned_transaction_detaches_without_ending_the_session`: `using` block around `UseTransaction`; after it `db.Database.CurrentTransaction` is null and the session is still in its transaction.
  - `UseTransaction_rejects_invalid_arguments_without_changing_state`: session without `StartTransaction` -> `"active transaction"`; context already in a transaction -> `"already in a transaction"`; `StubTransaction` -> `"MongoDB transaction"`; null -> `ArgumentNullException`; ended owner transaction -> `"is no longer active"`. After each, `CurrentTransaction` is unchanged.
  - `Contexts_on_different_client_instances_can_share_a_transaction`: owner on the fixture's client, second context on the second client; both save; owner commit shows both.
  - `Enlisted_context_queries_and_bulk_deletes_run_inside_the_shared_transaction`: owner saves an entity; enlisted context `Entities.Count()` is 1 while a context outside the transaction sees 0; enlisted `ExecuteDelete()` (guard `#if !EF8` like `ExecuteDeleteTests.cs`) then owner `RollbackTransaction()` leaves the originally committed documents intact.
  - `A_failed_write_in_a_shared_transaction_aborts_it_and_nothing_is_persisted`: owner saves entity A; enlisted context saves a second entity with the same `_id` (throws a write exception); the owner's next `SaveChanges` fails with a `MongoCommandException` (assert its `ErrorLabels` contain `"TransientTransactionError"`, and that the message does **not** contain `"is no longer active"`); owner `CommitTransaction()` throws; a fresh context sees 0 documents.

- [ ] **Step 2: Run to verify they fail** (compile errors for the missing API count). Same command as Task 1.

- [ ] **Step 3: Implement `MongoTransaction.Enlist`, `IsOwner` and the non-owned `Commit`/`Rollback`/`Dispose` behaviour** as specified in Interfaces, keeping the existing state machine for owned transactions untouched.

- [ ] **Step 4: Implement the two manager methods and the two facade extensions** as specified.

- [ ] **Step 5: Run `TransactionTests` on EF10**
  Expected: PASS, including every pre-existing test.

- [ ] **Step 6: Commit**
  `git add -A && git commit -m "Transactions: enlist a context in a transaction it does not own (Refs #87)"`

---

### Task 3: Diagnostics for enlisting

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoEventId.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggingDefinitions.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerTransactionExtensions.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Storage/MongoTransaction.cs` (call it from `Enlist`)
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/LoggingTests.cs`

**Interfaces:**
- Consumes: `MongoTransaction.Enlist` (Task 2).
- Produces: `MongoEventId.TransactionEnlisted` (category `DbLoggerCategory.Database.Transaction`, level Information, message `"Enlisted in transaction {transactionId}."`, payload `MongoTransactionEventData`); `internal static void TransactionEnlisted(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics, MongoTransaction transaction)`; field `LogEnlistedTransaction` in `MongoLoggingDefinitions`. Model every part on the existing `TransactionCommitting` event. Append the new `Id.TransactionEnlisted` at the **end** of the private `Id` enum (values must never change), not next to the other transaction events.

- [ ] **Step 1: Write the failing test** `UseTransaction_logs_TransactionEnlisted_and_no_commit_events` in `LoggingTests` (follow the existing transaction-logging test's log capture): enlisting logs exactly one `TransactionEnlisted` entry containing `"Enlisted in transaction"`; disposing the enlisted context logs no `TransactionCommitting`, `TransactionRollingBack` or `TransactionRolledBack` entry for it.
- [ ] **Step 2: Run to verify it fails** (`--filter "FullyQualifiedName~LoggingTests"`).
- [ ] **Step 3: Implement the event, definition and extension**; call `TransactionEnlisted` at the end of `Enlist`.
- [ ] **Step 4: Run `LoggingTests` and `TransactionTests`**. Expected: PASS.
- [ ] **Step 5: Commit** `git commit -am "Transactions: log when a context enlists in a transaction (Refs #87)"`

---

### Task 4: Documentation and breaking-change note

**Files:**
- Modify: `README.md` (the transactions bullet near line 91 and the bulk-operation note near line 104: add a short "Sharing a transaction" paragraph with the `UseTransaction` forms, the commit/rollback rule, the guard, the failed-write behaviour, and the limits from #87: `$search`/`$vectorSearch` and some DDL are rejected by the server inside a transaction; sessions are not thread-safe, so sequential use only)
- Modify: `BREAKING-CHANGES.md` (an entry for the two new `IMongoTransactionManager` members, which affects anyone who implements that interface, and for the guard: code that previously kept using a context after its transaction was ended through the driver now throws)

- Modify: `src/MongoDB.EntityFrameworkCore/Storage/AGENTS.md` (this change applies upstream too, so the area guide is updated; do not touch the root `AGENTS.md`)

- [ ] **Step 1: Write the README and BREAKING-CHANGES entries**, matching the tone and structure of the neighbouring entries.
- [ ] **Step 2: Update `Storage/AGENTS.md` invariants.** Extend the transaction invariants with: a `MongoTransaction` is owned or non-owned (`IsOwner`); a non-owned one (from `UseTransaction`) detaches on dispose and rejects `Commit`/`Rollback`; every read of the transaction's session goes through `MongoTransaction.ActiveSession` (never a cached session), which throws if the driver ended the transaction; the guard does not detect a server-side abort after a failed write, and that `TransientTransactionError` must surface unchanged. Also amend the "two transaction paths" invariant to say enlisted contexts take the explicit-transaction path.
- [ ] **Step 3: Commit** `git commit -am "Transactions: document sharing a transaction between contexts (Refs #87)"`

---

### Task 5: Verification and hand-off

- [ ] **Step 1: Run the full suites** with the `/test-all` skill and again with `/test-all --native` (EF8-EF11; EF11 spec tests run through their own executable, see root `AGENTS.md`). Re-run any suspicious failure on its own before concluding (other sessions may be running tests).
- [ ] **Step 2: Report** the results faithfully, including any failure, and stop. Do not push or open a PR until asked; when asked, create the PR through `gh api` (REST), with `Refs #87`, not `Fixes`, and the footer from Global Constraints.
