### Task 2: Unit-test and narrow `UnwrapBulkOperator`

`UnwrapBulkOperator` strips *any* outermost method declared on `EntityFrameworkQueryableExtensions`, not just the two bulk operators. It is the only method on `MongoNonQueryExpression` with logic and the only one without a test.

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `MongoNonQueryExpression.UnwrapBulkOperator(Expression?)` keeps its existing signature `static Expression? UnwrapBulkOperator(Expression? capturedExpression)`. Behaviour narrows: it now only unwraps calls named `ExecuteDelete`, `ExecuteDeleteAsync`, `ExecuteUpdate` or `ExecuteUpdateAsync` declared on `EntityFrameworkQueryableExtensions`.

- [ ] **Step 1: Write the failing tests**

Create `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs`:

```csharp
/* Copyright 2023-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#if !EF8

using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions;

public static class MongoNonQueryExpressionUnwrapTests
{
    private class Product
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; } = null!;
    }

    private static IQueryable<Product> Source => new Product[0].AsQueryable();

    [Fact]
    public static void Unwraps_ExecuteDelete_returning_the_source_chain()
    {
        var chain = Source.Where(p => p.Name == "x");
        var captured = Expression.Call(
            typeof(EntityFrameworkQueryableExtensions)
                .GetMethod(nameof(EntityFrameworkQueryableExtensions.ExecuteDelete))!
                .MakeGenericMethod(typeof(Product)),
            chain.Expression);

        var actual = MongoNonQueryExpression.UnwrapBulkOperator(captured);

        Assert.Same(chain.Expression, actual);
    }

    [Fact]
    public static void Returns_expression_unchanged_when_not_a_bulk_operator()
    {
        var chain = Source.Where(p => p.Name == "x");

        var actual = MongoNonQueryExpression.UnwrapBulkOperator(chain.Expression);

        Assert.Same(chain.Expression, actual);
    }

    [Fact]
    public static void Does_not_unwrap_a_non_bulk_EntityFrameworkQueryableExtensions_call()
    {
        // AsNoTracking is declared on EntityFrameworkQueryableExtensions but is not a bulk operator.
        // Unwrapping it would silently discard it from the chain the classifier walks.
        var chain = Source.Where(p => p.Name == "x");
        var captured = Expression.Call(
            typeof(EntityFrameworkQueryableExtensions)
                .GetMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTracking), new[] { typeof(IQueryable<Product>) })!,
            chain.Expression);

        var actual = MongoNonQueryExpression.UnwrapBulkOperator(captured);

        Assert.Same(captured, actual);
    }

    [Fact]
    public static void Returns_null_for_null()
        => Assert.Null(MongoNonQueryExpression.UnwrapBulkOperator(null));
}

#endif
```

- [ ] **Step 2: Run the tests to verify the third one fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~MongoNonQueryExpressionUnwrapTests"
```

Expected: `Does_not_unwrap_a_non_bulk_EntityFrameworkQueryableExtensions_call` FAILS (`Assert.Same` — the current implementation unwraps it and returns `chain.Expression`). The other three PASS.

If `AsNoTracking`'s reflection lookup throws because the overload set differs on your EF version, substitute `nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters)` with `new[] { typeof(IQueryable<Product>) }`.

- [ ] **Step 3: Narrow the implementation**

In `MongoNonQueryExpression.cs`, replace the `UnwrapBulkOperator` body:

```csharp
    private static readonly string[] BulkOperatorNames =
    [
        "ExecuteDelete", "ExecuteDeleteAsync", "ExecuteUpdate", "ExecuteUpdateAsync"
    ];

    /// <summary>
    /// Unwraps the leading ExecuteDelete / ExecuteUpdate marker call (declared on
    /// <see cref="EntityFrameworkQueryableExtensions"/>) from a captured method chain, returning the underlying
    /// source query (e.g. <c>root.Where(...)</c>). EF captures the full chain including the bulk operator as the
    /// <see cref="MongoQueryExpression.CapturedExpression"/>; walkers that scope the operation need only the source.
    /// Only the four bulk operators are unwrapped — other methods on that type (AsNoTracking,
    /// IgnoreQueryFilters, TagWith, ...) must stay in the chain so the classifier can see and reject them
    /// rather than having them silently discarded.
    /// </summary>
    public static Expression? UnwrapBulkOperator(Expression? capturedExpression)
        => capturedExpression is MethodCallExpression { Method: { DeclaringType: var declaringType, Name: var name } } call
           && declaringType == typeof(EntityFrameworkQueryableExtensions)
           && Array.IndexOf(BulkOperatorNames, name) >= 0
            ? call.Arguments[0]
            : capturedExpression;
```

Add `using System;` to the file's using block if it is not already present.

- [ ] **Step 4: Run the tests to verify all pass**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~MongoNonQueryExpressionUnwrapTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~MongoNonQueryExpression"
```

Expected: PASS, 4 tests on EF10; the pre-existing `MongoNonQueryExpressionTests` still pass on EF9.

- [ ] **Step 5: Run the whole bulk suite to confirm no regression**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS, no change in counts.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs
git commit -m "fix: unwrap only the four bulk operators, not every EntityFrameworkQueryableExtensions call"
```

---

