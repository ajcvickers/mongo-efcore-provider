### Task 26: Minor cleanups

Small items with no behaviour risk, batched into one reviewable change.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `RunInBulkTransaction` and `RunInBulkTransactionAsync` lose their unused `MongoNonQueryExpression nonQuery` parameter. New signatures:
  - `private static int RunInBulkTransaction(QueryContext queryContext, Func<int> body)`
  - `private static Task<int> RunInBulkTransactionAsync(QueryContext queryContext, Func<Task<int>> body)`

- [ ] **Step 1: Drop the unused parameter**

Remove `MongoNonQueryExpression nonQuery` from both `RunInBulkTransaction` and `RunInBulkTransactionAsync` — it is never referenced in either body — and update the four call sites in the two-phase executors.

- [ ] **Step 2: Remove the unreachable `break`**

In `ClassifyBulkSource`'s rejection path, `ThrowBulkSourceNotSupported` is `[DoesNotReturn]`, so any `break` after it is unreachable. After Task 14 the switch became if/else chains, so verify with a build warning check:

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" -warnaserror 2>&1 | grep -i "unreachable" || echo "none"
```

Remove whatever it reports.

- [ ] **Step 3: Add a `VisitChildren` intent test**

`MongoNonQueryExpression.VisitChildren` returns `this` without visiting, which is deliberate (the node is a marker, and its `SourceQuery` is not a child EF should rewrite) but undocumented and untested. Document it:

```csharp
    /// <summary>
    /// This node is an opaque marker: its source query and setters are consumed directly by
    /// <see cref="Visitors.MongoShapedQueryCompilingExpressionVisitor"/> and must not be rewritten by generic
    /// visitors, so no children are visited.
    /// </summary>
    protected override Expression VisitChildren(System.Linq.Expressions.ExpressionVisitor visitor) => this;
```

and add to `MongoNonQueryExpressionUnwrapTests`:

```csharp
    [Fact]
    public static void VisitChildren_returns_the_same_node()
    {
        using var db = new NonQueryDbContext();
        var node = new MongoNonQueryExpression(new MongoQueryExpression(db.Model.GetEntityTypes().First()));

        var actual = new NoOpVisitor().Visit(node);

        Assert.Same(node, actual);
    }

    private sealed class NoOpVisitor : System.Linq.Expressions.ExpressionVisitor;
```

`NonQueryDbContext` lives in the sibling `MongoNonQueryExpressionTests` file; either make it `internal` and shared, or duplicate the four-line context into this file rather than reaching across test classes.

- [ ] **Step 4: Build and run the full bulk surface**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~Bulk|FullyQualifiedName~Execute"
```

Expected: all three configurations build; tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/ tests/MongoDB.EntityFrameworkCore.UnitTests/
git commit -m "refactor: drop the unused bulk transaction parameter and document the marker node's opacity"
```

---

## Final verification

- [ ] **Run the full suite on all three configurations**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10"
```

Expected: zero failures. Only environment-gated skips (Queryable Encryption, and the standalone-deployment test) are acceptable, and their count must not have grown beyond the encryption tests added in Task 24 plus the one pre-existing standalone skip.

- [ ] **Confirm no finding was silently dropped**

Walk the review-to-task map below and confirm each row has a landed commit. Any row you chose not to do must be recorded as a deliberate decision — in the README for behaviour, or in a ticket for deferred work — not left implicit.

## Review-to-task map

| Review finding | Task |
|---|---|
| Bare `OrderBy`/`Distinct` misclassified as two-phase | 13 |
| Classifier matches `Method.Name`, not `QueryableMethods` | 14 |
| `Order()`/`OrderDescending()` unsupported | 14 |
| `OfType`/`Cast` rejected though the read path supports them | 15 |
| Stale `ValidateBulkSource` comment; `BuildFilter` drops inner predicates | 11, 13 |
| Phase-1 translation failures escape `TranslateBulkOrThrow` | 11 |
| Transaction opened before translation, producing a misleading error | 11 |
| Composite-key `$set` written to the wrong element path | 7 |
| No guard on key / discriminator / concurrency-token setters | 7 |
| `ParameterRebindingExpressionVisitor` precondition unenforced | 8 |
| `FindProperty(string)` instead of `FindProperty(MemberInfo)` | 9 |
| `"SetProperty"` matched by name only | 9 |
| Duplicate setters silently last-wins | 9 |
| Per-execution `Compile()` and reflection `Invoke` | 19 |
| Two-phase elapsed excludes phase 1 | 5 |
| `ExecutedBulkUpdate` reports only `ModifiedCount` | 4 |
| Two definitions share one `EventId` | 4 |
| `TargetCount` is `int?` among `long` siblings | 4 |
| No command/MQL logging | 5 |
| No error event on failure | *deferred — record as a ticket in Final verification* |
| Logger resolved by service location | 6 |
| Phase 1 fetches whole documents | 16 |
| Async phase 1 materialises everything | 17 |
| Unbounded `$in` | 18 |
| Empty target set still costs transaction round trips | *accepted — noted in Task 18's batching comment* |
| Ambient transaction not verified to be a `MongoTransaction` | 10 |
| Cancellation token passed to rollback | 12 |
| `SafeRollback` catches everything unfiltered | 12 |
| `IsTransactionsUnsupported` matches code 20 too broadly | 3 |
| `UnwrapBulkOperator` strips any `EntityFrameworkQueryableExtensions` call | 2 |
| Exception messages print inline setter values | *accepted — matches EF behaviour; noted in the security review* |
| README "atomic" claim is wrong | 25 |
| Nested-setter gap undocumented | 25 |
| Duplicate `EF-X016` ticket id | 25 |
| Deleted `FakeQueryableExtensions` regression test | 1 |
| EF8 `ExecuteUpdate` untested | 1 |
| `IsTransactionsUnsupported` untested | 3 |
| `ClassifyBulkSource` untested directly | 13, 14 (behavioural), 18 (batching unit test) |
| Async two-phase shapes untested | 21 |
| Query filters × bulk untested; wrong compliance exclusion reason | 20 |
| TPH × bulk untested | 15, 20 |
| `Where` interleaved with ordering untested | 13 |
| Setter shapes untested (null, enum/Guid, owned, missing field) | 22 |
| Event payload untested | 23 |
| Encryption × bulk untested | 24 |
| Conformance suite always runs inside a transaction | *accepted — functional tests cover the sessionless path; a standalone CI leg is out of scope here* |
| Unused `nonQuery` parameter; unreachable `break`; `VisitChildren` | 26 |

## Self-review notes

Checked against the five reviews:

- **Coverage:** every finding appears in the map above, either with a task or with an explicit acceptance. Four are deliberately deferred or accepted and say so.
- **Type consistency:** `PhaseOneResult` (Task 16) is consumed by Tasks 17 and 18; `CombineBulkCommand` (Task 18) supersedes the inline string interpolation introduced in Task 16 Step 4 — Task 18 states that explicitly. `SafeRollbackAsync` loses its token in Task 12, and Task 11's restructure of `RunInBulkTransactionAsync` references the post-Task-12 signature, so **Task 11 must be applied before Task 12 but written to expect Task 12's signature**; if executing strictly in order, keep the token parameter in Task 11 and drop it in Task 12. `RunInBulkTransaction` loses its `nonQuery` parameter only in Task 26, so Tasks 10–19 use the original signature.
- **Ordering hazards flagged in-place:** Tasks 7, 9, 11, 13 and 14 can each move conformance cases between `base.*` and `AssertTranslationFailed`; each says so and defers the `docs/failing-spec-tests.md` reconciliation to Task 25.
- **Known unknowns:** four driver/EF API details could not be verified without building — `AppendStage`'s arity (Task 16), `BsonSerializationInfo.ElementPath` (Task 7), `SetPropertyCalls<>`'s namespace (Task 9), and EF's `DiagnosticSource` key format (Task 23). Each has an explicit fallback and a "print, then fix" instruction rather than a guess.
