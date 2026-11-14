---
area: Storage, update pipeline & transactions
scope: ["src/MongoDB.EntityFrameworkCore/Storage/**"]
reviewer-agent: storage-reviewer
adjacent-areas: [Query, Metadata, Serializers, Infrastructure]
---

# Storage — AGENTS.md

Runtime execution layer: wraps `IMongoClient`/`IMongoDatabase`/collections (`MongoClientWrapper`), turns EF's
`IUpdateEntry` set into writes (`MongoDatabaseWrapper.SaveChanges` → `MongoUpdate.CreateAll` →
`MongoUpdateBatch.CreateBatches` → per-collection `BulkWrite` → `AssertWritesApplied` throws
`DbUpdateConcurrencyException` on count mismatch), manages transactions (`MongoTransaction*`), creates
databases/collections/indexes (`MongoDatabaseCreator`), and supplies type mappings and converters.
Only Storage may call the driver client/collection/session types directly.

## Invariants

- **Two transaction paths must stay in sync.** `SaveChanges` starts its implicit transaction via
  `MongoTransaction.Start` (honoring `AutoTransactionBehavior.WhenNeeded`); the bulk two-phase path goes through
  `database.BeginTransaction()` and always begins. A policy change must land in both. A context enlisted via
  `UseTransaction` has a current transaction, so it takes the explicit-transaction path (no implicit transaction).
- **A `MongoTransaction` is owned or non-owned (`IsOwner`).** A non-owned one (from `UseTransaction`, built by
  `MongoTransaction.Enlist`) only detaches on `Dispose` (never ends the transaction or disposes the session) and rejects
  `Commit`/`Rollback` with "not started by this context"; only the owner ends the transaction. A `MongoTransaction` is bound to the
  session's transaction number as well as the session: if the caller ends it and starts another on the same session, `ActiveSession`
  still throws. Sessions are not thread-safe:
  contexts sharing one are used sequentially.
- **Issue commands on the transaction's session only through `MongoTransaction.ActiveSession`**, never a cached session: it throws
  `InvalidOperationException` ("is no longer active") if the driver has ended the transaction (or the session now holds a different
  one), instead of letting commands run outside the transaction. It does not detect a server-side abort after a failed write; that `TransientTransactionError` must surface
  unchanged (no wrapping, no swallowing).
- **Owned-entity root promotion** (`GetAllChangedRootEntries`): an Unchanged root with a modified owned child is
  promoted to Modified; don't filter the entry list before promotion.
- **Concurrency-filter values are *original* values** (`GetOriginalValue`); current values corrupt conflict
  detection.
- **A conflict goes through EF's update logger before it throws** (`AssertWritesApplied[Async]`): it logs
  `CoreEventId.OptimisticConcurrencyException` and runs `ISaveChangesInterceptor.ThrowingConcurrencyException[Async]`, and an
  interceptor that suppresses it makes `SaveChanges` carry on, counting the unapplied operations as applied (as the relational
  providers do). Keep the sync and async paths in step, and keep entity values, keys and tokens out of the exception text: it is
  logged with no sensitive-data gate.
- **RowVersion ordering is contract**: `CreateWhereFilter` (pre-increment) → `SetStoreGeneratedValues` (increments
  in memory) → `WriteEntity` (post-increment into `$set`). The in-memory value is already advanced if
  serialization then fails.
- **A row version is only generated for a non-nullable `int`/`uint`/`long`/`ulong` or a `byte[]`** (`RowVersion.IsRowVersion`).
  A `byte[]` is the same counter as a `ulong`, held as 8 big-endian bytes (empty/null counts as 0, any other length throws on
  write, before the in-memory value changes); it is stored as binary in `_version`, like the numeric types. Any other
  `IsRowVersion`/`[Timestamp]` property (or concurrency token with `ValueGenerated.OnAddOrUpdate`) would never change, so
  `MongoModelValidator.ValidateRowVersionTypes` rejects it (otherwise a concurrency check that never fails). A new generated
  type must be added to both.
- **`AutoTransactionBehavior.Never` removes optimistic concurrency's atomicity**, not the tokens.
- **Cross-collection atomicity comes from the transaction**, not `BulkWrite` (one per collection).
- **Write order within a collection is deletes, then updates, then inserts** (`MongoUpdateBatch.CreateBatches`, #102), because EF
  Core supplies the entries Added, Modified, Deleted and an insert would otherwise reach the server before the delete or update
  that frees the unique-indexed value it takes. The grouping covers the whole update list (one batch per collection, in order of
  first appearance), not just consecutive updates. Among the updates, `MongoUniqueValueOrderer` puts one that moves a unique
  index value away before the one that takes it (best effort: values compare as EF Core compares keys, so a case-insensitive
  collation or a multikey array is not seen, and keys with a `null` are skipped). Two updates that swap values are a cycle and
  fail with a duplicate key error, as the relational providers fail with a circular dependency. A delete and an add with the same
  primary key is not an ordering problem (EF drops the delete, #109).
- **A failed bulk write is reported as `DbUpdateConcurrencyException` when a delete or update before the failed write matched
  nothing** (`TryCreateConcurrencyExceptionForFailedWrite`): with deletes first, a delete that failed its concurrency check
  leaves the document holding the unique value a later insert then collides with, and the failed check is the cause. It goes
  through the same logging and `ThrowingConcurrencyException` interception as a count mismatch; the thrown exception keeps the
  driver exception as its inner exception, but the logged one does not (the driver's message holds the duplicate key's values).
  If an interceptor suppresses it, the original driver exception is thrown, since the write did fail.
- **Implicit transactions roll back on Dispose**; don't wrap the commit in a swallowing `catch`. The one deliberate exception is the
  commit retry loop in `MongoTransaction.Commit`/`CommitAsync`, which retries only a `MongoException` labelled
  `UnknownTransactionCommitResult` (not after `MaxTimeMSExpired`, nor after 120 seconds) and rethrows anything else. The opt-in
  retrying execution strategy (`EnableRetryOnFailure`) retries only a `TransientTransactionError`, and only work that ran in a
  transaction. Ambient
  `System.Transactions` are rejected; an owned `MongoTransaction` follows the `Active → Committed | RolledBack | Failed →
  Disposed` state machine, except that one whose session transaction was ended elsewhere goes `Active → Disposed`, and a non-owned
  one only goes `Active → Disposed` (it never commits or rolls back).
- **Disposing a context disposes its transaction manager**: `MongoTransactionManager` is `IDisposable`/`IAsyncDisposable` (the interface
  `IMongoTransactionManager` is not), and EF disposes scoped services with a non-pooled context, so `Dispose` is `ResetState`: it rolls back
  an owned `CurrentTransaction` (and disposes its session) and only detaches a non-owned one, never touching the owner's session. A pooled
  context is reset (`ResetState`) rather than disposed, with the same effect. Don't swallow exceptions from this path, but never let one
  leave the work half done: `MongoTransaction.Dispose*` always ends in `Disposed` with its session disposed and the manager detached
  (`try/finally`) even when the rollback throws, and `ResetState*` always clears `CurrentTransaction`. The manager's `Dispose`/`DisposeAsync`
  are `virtual` (EF disposes through the interfaces, so a hiding method would never run).
  A `MongoTransaction` built on a session with no transaction has no transaction number and is never active.
- **`EnsureCreated` is idempotent by design** (re-applies seeds, re-creates indexes, tolerates duplicate-key errors
  while seeding). Seeding goes through the normal update pipeline.
- **Query → Storage seam is `MongoClientWrapper.Execute(MongoExecutableQuery)`**; the wrapper doesn't translate.
  Bulk `ExecuteUpdate`/`ExecuteDelete` crosses as behavior: `MongoBulkOperationExecutor` runs a `MongoBulkPlan` whose
  translation delegates are invoked per execution (translation is parameter-value-dependent);
  `TranslateBulkOrThrow` maps failures to EF's canonical "could not be translated".
- **Complex properties are written by `ComplexValueWriter`, beside (not inside) `WriteOwnedEntities`**
  (`WriteEntity` calls both). A change to any member at any depth rewrites the whole TOP-LEVEL complex property in `$set`
  (last-writer-wins per complex property; per-member dotted `$set` is a deferred optimization); unchanged complex
  properties are skipped, inserts write everything. Bulk setters serialize a client value through
  `SerializeDetachedComplexValue`, the same code path, so bulk and `SaveChanges` store the same shape. Concurrency tokens
  are read from the entity type's own properties only; the model validator rejects tokens inside complex types.
- **Serializers come from `BsonSerializerFactory`** (`MongoUpdate.WriteProperty`); a `BsonWriter`/`BsonReader` here is
  almost certainly a layering mistake.
- **Queryable Encryption auto-schema is injected at client construction** in `MongoClientWrapper`.
