### Task 6: Resolve the update logger once via `MongoQueryContext`

`GetUpdateLogger` calls `queryContext.Context.GetService<IDiagnosticsLogger<DbLoggerCategory.Update>>()` inside a compiled-query executor. That is a service-locator lookup on the execution path and it bypasses the plumbing the read path uses (`QueryContext.QueryLogger`, supplied by the context factory).

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/MongoQueryContext.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Factories/MongoQueryContextFactory.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `MongoQueryContext.UpdateLogger` of type `IDiagnosticsLogger<DbLoggerCategory.Update>`, set from the constructor. `GetUpdateLogger(QueryContext queryContext)` keeps its signature and returns `((MongoQueryContext)queryContext).UpdateLogger`.

- [ ] **Step 1: Add the property**

In `MongoQueryContext.cs`, extend the constructor and add the property:

```csharp
    /// <param name="updateLogger">The <see cref="DbLoggerCategory.Update"/> logger used by bulk operations.</param>
    public MongoQueryContext(
        QueryContextDependencies dependencies,
        IMongoClientWrapper mongoClient,
        IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger)
        : base(dependencies)
    {
        MongoClient = mongoClient;
        UpdateLogger = updateLogger;
    }

    /// <summary>
    /// The <see cref="DbLoggerCategory.Update"/> logger used to emit bulk delete/update diagnostics. Bulk
    /// operations bypass SaveChanges, so MongoDatabaseWrapper's logger never fires for them.
    /// </summary>
    public virtual IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger { get; }
```

Add `using Microsoft.EntityFrameworkCore.Diagnostics;` if absent.

- [ ] **Step 2: Supply it from the factory**

In `MongoQueryContextFactory.cs`, inject `IDiagnosticsLogger<DbLoggerCategory.Update>` alongside the existing dependencies (follow the constructor-injection style already in that file) and pass it into the `MongoQueryContext` construction.

- [ ] **Step 3: Point the executors at it**

Replace `GetUpdateLogger`:

```csharp
    // Bulk operations bypass SaveChanges, so MongoDatabaseWrapper's bulk-write logging never fires for them.
    // The Update-category logger is supplied by MongoQueryContextFactory, matching how the read path gets
    // its query logger — rather than resolving it from the context's service provider per execution.
    private static IDiagnosticsLogger<DbLoggerCategory.Update> GetUpdateLogger(QueryContext queryContext)
        => ((MongoQueryContext)queryContext).UpdateLogger;
```

Remove the now-unused `using Microsoft.EntityFrameworkCore.Infrastructure;` only if nothing else in the file needs it.

- [ ] **Step 4: Run the bulk and logging suites**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~LoggingTests"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```

Expected: PASS; EF8 builds (the `UpdateLogger` property and factory change are outside `#if !EF8`, so they compile on every configuration — verify this, and if the EF8 build breaks because nothing consumes the property, leave the property unguarded and only guard its use).

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/
git commit -m "refactor: supply the bulk update logger via MongoQueryContext instead of service location"
```

---

## Phase 2 — Data integrity (setters)

