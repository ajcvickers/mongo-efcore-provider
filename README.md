# MongoDB Entity Framework Core Provider

[![MongoDB.EntityFrameworkCore](https://img.shields.io/nuget/v/MongoDB.EntityFrameworkCore.svg)](https://www.nuget.org/packages/MongoDB.EntityFrameworkCore/)

The MongoDB EF Core Provider enables MongoDB interaction with Entity Framework Core 8 or 9 on .NET 8.0 or later, and Entity Framework Core 10 on .NET 10.0 or later.

It supports MongoDB database server 5.0 or later, preferably in a transaction-enabled configuration.

## Getting Started

### Basic Setup
First, create a DbContext with the desired entities and configuration:

```csharp
internal class PlanetDbContext : DbContext
{
    public DbSet<Planet> Planets { get; init; }

    public static PlanetDbContext Create(IMongoDatabase database) =>
        new(new DbContextOptionsBuilder<PlanetDbContext>()
            .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
            .Options);

    public PlanetDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Planet>().ToCollection("planets");
    }
}
```

### Connection Options

#### Option 1: Direct Database Connection
```csharp
var mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_URI");
var mongoClient = new MongoClient(mongoConnectionString);
var db = PlanetDbContext.Create(mongoClient.GetDatabase("planets"));
db.Database.EnsureCreated();
var planet = db.Planets.FirstOrDefault(x => x.Name == "Earth");
```

#### Option 2: Using Dependency Injection
```csharp
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDbUri")!;
builder.Services.AddDbContext<PlanetDbContext>(options =>
{
    options.UseMongoDB(mongoConnectionString, DatabaseName);
});
```

If you need some more configuration, you can create the `MongoClient` and inject it into the DbContext:

```csharp
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDbUri")!;
var mongoUrl = new MongoUrl(mongoConnectionString);
var mongoClient = new MongoClient(mongoUrl);
builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddDbContext<PlanetDbContext>((provider, options) =>
{
    var client = provider.GetRequiredService<IMongoClient>();
    options.UseMongoDB(client, DatabaseName);
});
```

Later on, simply inject `PlanetDbContext` where it's needed and continue as you would normally.

## Supported Features

Entity Framework Core and MongoDB have a wide variety of features. This provider supports a subset of the functionality available in both, specifically:

- Querying with `Where`, `Find`, `First`, `Single`, `OrderBy`, `ThenBy`, `Skip`, `Take` etc.
- Vector search with the `VectorSearch` extension method on DbSet and fluent vector index configuration
- Top-level aggregate `Any`, `Count`, `LongCount`, `Sum`, `Min`, `Max`, `Average`, `All`
- Mapping properties to BSON elements using `[Column]` or `[BsonElement]` attributes or `HasElementName("name")` method
- Mapping entities to collections via `[Table("name")]`,  `ToCollection("name")` or by convention from the DbSet property name
- Single or composite keys of standard types including string, `Guid` and `ObjectId` etc.
- Properties with typical CLR types (`int`, `string`, `Guid`, `decimal`, `DateOnly` etc.) & MongoDB types (`ObjectId`, `Decimal128`)
- Properties that are arrays, lists, dictionaries (string keys) of simple CLR types including binary `byte[]`
- Owned entities (aka value types, sub-documents, embedded documents) both directly and in collection properties
- `BsonIgnore`, `BsonId`, `BsonDateTimeOptions`, `BsonElement`, `BsonRepresentation` and `BsonRequired` support
- Storage type configuration through EF ValueConverters or BSON representation attributes and fluent APIs
- Query and update logging of MQL (sensitive logging must be enabled)
- `EnsureCreated` & `EnsureDeleted` to ensure collections and the database created at app start-up
- Optimistic concurrency support through `IsConcurrencyToken`/`ConcurrencyCheckAttribute` & `IsRowVersion`/`TimestampAttribute`
- AutoTransactional `SaveChanges` & `SaveChangesAsync` - all changes committed or rolled-back together
- Sharing a transaction between contexts: `context.Database.UseTransaction(...)` makes a context write and query inside a transaction it did not start, so several contexts (even on different `MongoClient` instances connected to the same cluster, including a sharded cluster) can take part in one transaction. Pass another context's transaction, or an `IClientSessionHandle` that already has a transaction started:

  ```csharp
  using var owner = new BlogContext(blogOptions);   // DbContextOptions<BlogContext>
  using var other = new AuthorContext(authorOptions); // DbContextOptions<AuthorContext>

  using var transaction = owner.Database.BeginTransaction();
  owner.Blogs.Add(new Blog { Name = "Mongo" });
  owner.SaveChanges();

  other.Database.UseTransaction(transaction);
  other.Authors.Add(new Author { Name = "Ada" });
  other.SaveChanges();

  owner.Database.CommitTransaction(); // only the owner ends the transaction
  ```

  - The transaction is not owned by the context that joins it: disposing it (or the context) only detaches, and `Commit`/`Rollback` on it, or `Database.CommitTransaction()`/`RollbackTransaction()` on the joining context, throw `InvalidOperationException`. Only the owner (or the code that supplied the session) commits or aborts. This differs from relational providers, where `Commit`/`Rollback` on a transaction joined with `UseTransaction` act on it and only `Dispose` is skipped
  - A joined context is detached by disposing the transaction `UseTransaction` returned (or by disposing the context); `UseTransaction(null)` is not supported
  - Disposing a context rolls back a transaction that context started and has not committed or rolled back (as relational providers do), so contexts that joined it can no longer write; commit before disposing the owner
  - With the driver's `session.WithTransaction(...)`, a retry (after a `TransientTransactionError`) aborts the transaction and starts a new one on the same session, and a context enlisted before the retry then throws `InvalidOperationException` ("is no longer active"); that exception carries no `TransientTransactionError` label, so the retry stops. Create the context and call `UseTransaction(session)` inside the callback, and dispose it with the callback:

    ```csharp
    session.WithTransaction((s, ct) =>
    {
        using var db = new BlogContext(blogOptions);
        db.Database.UseTransaction(s); // enlist inside the callback, so every retry gets a fresh context
        db.Blogs.Add(new Blog { Name = "Mongo" });
        db.SaveChanges();
        return 0;
    });
    ```

  - Once the owner commits or aborts, `SaveChanges`, queries and bulk `ExecuteUpdate`/`ExecuteDelete` on a joined context throw `InvalidOperationException` ("is no longer active") rather than silently running outside a transaction
  - MongoDB has no savepoints: a failed write aborts the whole transaction. The next operation (or, on a sharded cluster, possibly only the commit) fails with the server's `TransientTransactionError`, and nothing from the transaction becomes visible
  - Schema operations never take part in the transaction: `EnsureCreated`/`EnsureDeleted` and the collection and index creation they perform run outside it, are committed immediately and are not rolled back. Seed data written by `EnsureCreated` does join the transaction
  - Server limits apply: `$search`/`$vectorSearch` queries are rejected inside a multi-document transaction. The provider passes the server error through unchanged. (The server also restricts `drop` and `createIndexes` inside a transaction, which only matters for your own driver code run on the shared session)
  - A client session is not thread-safe: contexts sharing a transaction must be used one at a time, never concurrently
  - Ambient `System.Transactions` (`TransactionScope`, `Database.EnlistTransaction`) and savepoints remain unsupported
  - Joining logs `MongoEventId.TransactionEnlisted` and detaching (disposing the joined context or its transaction) logs `MongoEventId.TransactionDetached`, both at Information and both naming the owner; a joined context logs no commit or rollback events. When a context's own transaction is disposed after something else already ended the session's transaction, `MongoEventId.TransactionRollbackSkipped` records that there was nothing to roll back
- Retrying transient transaction failures: `UseMongoDB(..., o => o.EnableRetryOnFailure())` (defaults: 6 retries, at most 30 seconds between them) makes EF Core's execution strategy (`context.Database.CreateExecutionStrategy()`, which `SaveChanges` also uses) re-run a unit of work when MongoDB aborts its transaction and labels the error `TransientTransactionError`, for example a write conflict between two concurrent transactions:
  ```csharp
  var strategy = db.Database.CreateExecutionStrategy();
  strategy.Execute(() =>
  {
      db.ChangeTracker.Clear();                  // a failed attempt leaves its entities tracked
      using var transaction = db.Database.BeginTransaction();
      db.Orders.First().Status = "Shipped";
      db.SaveChanges();
      transaction.Commit();
  });
  ```
  - Off by default, so nothing changes for existing applications. Retrying is always from the start of the delegate: the failed transaction was aborted, and MongoDB has no savepoints
  - Only the `TransientTransactionError` label is retried, not specific error codes. Duplicate-key and other deterministic write errors, `DbUpdateConcurrencyException` and cancellation are never retried, and neither is work that did not run inside a transaction (`AutoTransactionBehavior.Never`, or a `SaveChanges` with a single root entity under `WhenNeeded`): a failure there can leave earlier writes applied, and running them again could apply them twice. The driver's own retryable reads and writes still retry single operations as before
  - As with relational providers, a retrying strategy rejects a transaction started outside the delegate (`Database.BeginTransaction()` or `Database.UseTransaction(...)` followed by `SaveChanges`): "use the execution strategy returned by `CreateExecutionStrategy()` to execute all the operations in the transaction as a retriable unit". Without `EnableRetryOnFailure` such transactions work as before. `SaveChanges` without an outer transaction needs nothing extra: it retries its own implicit transaction
  - Each attempt starts a new session and transaction. A context that joined the failed transaction with `UseTransaction` (see above) throws "is no longer active" if used again, instead of writing outside a transaction, so create such contexts and join them inside the delegate
  - Independently of `EnableRetryOnFailure`, `Commit`/`CommitAsync` on a transaction the context started retries the commit on the same session when MongoDB reports `UnknownTransactionCommitResult` (the commit may or may not have been applied; committing again applies it at most once), for up to two minutes, unless the server's `maxTimeMS` expired. Each retry logs `MongoEventId.TransactionCommitRetrying` at Warning. A transaction the context only joined cannot be committed through EF, so the owner's code decides
  - Retries of the whole unit of work are logged by EF Core's `CoreEventId.ExecutionStrategyRetrying`. Behavior on a sharded cluster has not been tested
- `CamelCaseElementNameConvention` for helping map Pascal-cased C# properties to camel-cased BSON elements
- Type discriminators including `OfType<T>` and `Where(e => e is T)`
- Support for EF shadow properties and EF.Proxy for navigation traversal
- MongoDB full text search (Previously known as Atlas Search)
- MongoDB vector search (Previously known as Atlas Vector Search)
- [Client Side Field Level Encryption](https://www.mongodb.com/docs/manual/core/csfle/quick-start/) and [Queryable Encryption](https://www.mongodb.com/docs/manual/core/queryable-encryption/) compatibility
- Bulk `ExecuteUpdate` and `ExecuteDelete` (EF 9+) on a single collection; supports constant and self-referencing setters; bypasses the change tracker (concurrency tokens are neither checked nor protected: a bulk update may set one explicitly)
  - `Where`-scoped sources, including ones that also use `OrderBy`/`ThenBy`/`Distinct` (they do not change which documents match), execute as a single `deleteMany` / `updateMany` server command. **This is not an atomic multi-document operation:** MongoDB guarantees atomicity per document only, so concurrent readers may observe partial results and a failure part-way leaves the documents already written changed. Open an explicit transaction around the call if you need all-or-nothing semantics
  - Sources that use `Skip`/`Take`, or `OfType<TDerived>()` on a hierarchy, are supported via a transactional two-phase execution (phase 1 collects the target `_id`s, reading only the `_id`; phase 2 acts on them via `{ _id: { $in: [...] } }`, in batches of up to 50,000 ids inside the one transaction); requires a transaction-capable deployment (replica set or sharded cluster); under `AutoTransactionBehavior.Never` the caller must open an explicit transaction. `Cast` is not supported
  - Setters cannot target the primary key or the type discriminator (rejected before anything is written). Setting the same property twice keeps the last value
  - Global query filters are applied, and `IgnoreQueryFilters()` removes them, as for a read
  - Queryable Encryption: `ExecuteDelete` works (an equality filter on an encrypted field matches the intended documents), but `ExecuteUpdate` is rejected by Queryable Encryption because every bulk update is an `updateMany` ("Multi-document updates are not allowed with Queryable Encryption"); use `SaveChanges` instead. Client-Side Field Level Encryption has not been verified with bulk operations
  - If a transaction is already open on the context, the operation enlists in it rather than starting its own — MongoDB does not support nested transactions, so a two-phase bulk op cannot open an inner transaction the way some relational providers do
  - Not supported: joins, `GroupBy`, `SelectMany`, set operations, cross-document navigation predicates, or multiple-collection updates

## Limitations

A number of Entity Framework Core features are not currently supported but planned for future release. If you require use of these facilities
in the mean-time consider using the existing [MongoDB C# Driver's](https://github.com/mongodb/mongo-csharp-driver) LINQ provider which may support them.

### Planned for future releases

- Select projections with client-side operations
- GroupBy operations
- Includes/joins
- Geospatial

### Not supported, out-of-scope features

- Keyless entity types
- Migrations
- Database-first & model-first
- Document (table) splitting
- Temporal tables
- Timeseries
- GridFS

## Breaking changes

This project's version-numbers are aligned with Entity Framework Core and as-such we can not use the semver convention of constraining breaking changes solely to major version numbers. Please keep an eye on our [Breaking Changes](/BREAKING-CHANGES.md) document before upgrading to a new version of this provider.

## Documentation

- [MongoDB](https://www.mongodb.com/docs)
- [EF Core Provider Guide](https://www.mongodb.com/docs/entity-framework/current/)
- [EF Core Provider API Docs](https://mongodb.github.io/mongo-efcore-provider/10.0.0/api/index.html)

## Questions/Bug Reports

- [Forums](https://www.mongodb.com/community/forums/)
- [Jira](https://jira.mongodb.org/projects/EF/)

If you've identified a security vulnerability in a driver or any other MongoDB project, please report it according to the [instructions here](https://www.mongodb.com/docs/manual/tutorial/create-a-vulnerability-report).

## Contributing

Please see our [guidelines](CONTRIBUTING.md) for contributing to the driver.

Thank you to [everyone](https://github.com/mongodb/mongo-efcore-provider/graphs/contributors) who has contributed to this project.
