### Task 14: Match canonical `QueryableMethods` definitions and accept `Order`/`OrderDescending`

`ClassifyBulkSource` switches on bare `Method.Name`, which `src/MongoDB.EntityFrameworkCore/Query/AGENTS.md` lists as a pitfall:

> **Reference-equality on `MethodInfo`.** Translators that match by `MethodInfo` must use canonical constants — `QueryableMethods` for the top-level dispatch in `MongoQueryableMethodTranslatingExpressionVisitor` […]

Every other dispatch in the same file pairs `nameof(...)` with `when methodDefinition == QueryableMethods.X`. Name-only matching silently admits overloads with different semantics — indexed `Where`, `Distinct(IEqualityComparer<T>)`, `OrderBy(keySelector, IComparer<T>)`, `Take(Range)` — which then fail deeper with a driver-shaped error instead of the canonical rejection. The indexed `Where` is the worst: `BuildFilter` rebinds only `Parameters[0]`, leaving the index parameter unbound in the lambda it builds.

`Queryable.Order()` and `Queryable.OrderDescending()` (.NET 7, available on `net8.0`) are also missing, so they are rejected despite being exactly equivalent to `OrderBy(x => x)`.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: the classifier shape from Task 13.
- Produces: no signature changes. `ClassifyBulkSource` now compares `method.GetGenericMethodDefinition()` against `QueryableMethods` constants and a small local set for the operators `QueryableMethods` does not expose.

- [ ] **Step 1: Write the failing tests**

```csharp
    [Fact]
    public void ExecuteDelete_with_indexed_Where_throws_the_canonical_failure()
    {
        using var db = CreateSeededContext();

        // Queryable.Where(source, (item, index) => ...) is a different operator with different semantics.
        // It must be rejected cleanly, not admitted and then rendered with an unbound index parameter.
        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.Where((o, i) => i == 0).ExecuteDelete());

        Assert.Contains("could not be translated", ex.Message);
        Assert.Equal(3, db.Entities.Count());
    }

    [Fact]
    public void ExecuteDelete_with_Order_deletes_matching()
    {
        using var db = CreateSeededContext();

        // Queryable.Order() is keyless ordering, equivalent to OrderBy(x => x) — and equally irrelevant to
        // which documents a delete affects.
        var deleted = db.Entities.Where(o => o.Status == "open").Order().ExecuteDelete();

        Assert.Equal(2, deleted);
    }
```

`Order()` requires `Order` to be comparable. If the test entity is not `IComparable`, use `db.Entities.Select(o => o).Order()` — no; that introduces a `Select`. Instead make the test model implement `IComparable<Order>` by `_id`, or if that is intrusive, replace this test with one over a keyless-orderable projection-free shape. Read the model first and pick whichever needs no `Select` in the chain; if neither works, drop this test and keep only the classifier entry plus a comment recording that it is unreachable from C# for this model.

- [ ] **Step 2: Run them to verify they fail**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDelete_with_indexed_Where|FullyQualifiedName~ExecuteDelete_with_Order_deletes_matching"
```

Expected: both FAIL. The indexed-`Where` case throws something other than the canonical message (or deletes documents); `Order()` throws "The 'Order' operator is not supported in a bulk delete or update".

- [ ] **Step 3: Match on method definitions**

Replace the `switch` inside `ClassifyBulkSource` with definition comparisons:

```csharp
        while (expression is MethodCallExpression methodCallExpression)
        {
            var method = methodCallExpression.Method;
            if (method.DeclaringType != typeof(Queryable))
            {
                ThrowBulkSourceNotSupported(mongoQueryExpression, method.Name);
            }

            var definition = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;

            if (definition == QueryableMethods.Where)
            {
                // Scopes the operation; BuildFilter and the read path both handle it.
            }
            else if (IsOrderingOperator(definition))
            {
                // Does not change which documents are affected.
            }
            else if (definition == QueryableMethods.Skip || definition == QueryableMethods.Take)
            {
                requiresTwoPhase = true;
            }
            else
            {
                ThrowBulkSourceNotSupported(mongoQueryExpression, method.Name);
            }

            expression = methodCallExpression.Arguments[0];
        }
```

and add the helper plus the definition set:

```csharp
    // QueryableMethods exposes Where/Skip/Take/Distinct but not the ordering operators or the .NET 7 keyless
    // Order/OrderDescending, so those are resolved once from Queryable itself. Matching definitions rather than
    // names keeps overloads with different semantics — indexed Where, Distinct(IEqualityComparer<T>),
    // OrderBy(keySelector, IComparer<T>), Take(Range) — out of the supported set (Query/AGENTS.md).
    private static readonly HashSet<MethodInfo> OrderingOperatorDefinitions =
        typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.IsGenericMethodDefinition
                        && m.GetParameters().Length == 2
                        && m.Name is nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending)
                            or nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending))
            .Concat(
                typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.IsGenericMethodDefinition
                                && m.GetParameters().Length == 1
                                && m.Name is nameof(Queryable.Order) or nameof(Queryable.OrderDescending)))
            .Append(QueryableMethods.Distinct)
            .ToHashSet();

    private static bool IsOrderingOperator(MethodInfo definition)
        => OrderingOperatorDefinitions.Contains(definition);
```

Add `using System.Collections.Generic;`, `using System.Reflection;` and `using System.Linq;` if any are missing. `QueryableMethods` lives in `Microsoft.EntityFrameworkCore.Query`, which the file already imports.

Verify the reflection actually resolves `Order`/`OrderDescending` on the pinned framework — add a temporary `Debug.Assert(OrderingOperatorDefinitions.Count >= 7)` while developing, and remove it before committing.

- [ ] **Step 4: Run the tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. If a conformance case used an overload that name-matching previously admitted and that now rejects cleanly, move it to `AssertTranslationFailed` with a `// Fails:` comment and note it for Task 25.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "fix: classify bulk sources by canonical method definition; support Order/OrderDescending"
```

---

