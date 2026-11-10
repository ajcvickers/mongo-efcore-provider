### Task 8: Enforce the single-`TSource`-parameter precondition when rebinding setter values

`ParameterRebindingExpressionVisitor` rebinds *every* parameter whose type matches `TSource` onto the shared entity parameter, and its comment states the precondition it relies on: "the value body has exactly one TSource-typed parameter (the setter's own), so type-based rebinding is safe for the supported single-setter value shapes." Nothing enforces that. If a second `TSource`-typed parameter ever appears — a self-referencing owned type, or a widening of the supported setter shapes — every one of them collapses onto the outer parameter and the update renders **silently wrong**, with no exception. On a write path, converting that into a loud rejection is the right trade.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs` (add a sibling test class file instead — see Step 1)
- Create: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkSetterRebindingTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `internal static Expression RebindSetterValueParameters(Expression body, ParameterExpression target)` in `MongoShapedQueryCompilingExpressionVisitor` — replaces the `ParameterRebindingExpressionVisitor` instantiation in `RenderSelfReferencingValue`, and throws `InvalidOperationException` when the body contains more than one distinct parameter of `target.Type`.

- [ ] **Step 1: Write the failing test**

Create `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkSetterRebindingTests.cs` with the standard licence header, then:

```csharp
// The self-referencing setter renderer rebinds parameters by type because translation discards parameter
// identity. That is only sound while the value body has exactly one parameter of the entity type. This
// pins the guard: two distinct entity-typed parameters must be rejected, not silently merged.

#if !EF8

using System;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Visitors;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query;

public static class BulkSetterRebindingTests
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public int Quantity { get; set; }
    }

    [Fact]
    public static void Rebinds_a_single_entity_parameter_onto_the_target()
    {
        var target = Expression.Parameter(typeof(Order), "e");
        var other = Expression.Parameter(typeof(Order), "o");
        var body = Expression.Add(
            Expression.Property(other, nameof(Order.Quantity)), Expression.Constant(1));

        var actual = MongoShapedQueryCompilingExpressionVisitor.RebindSetterValueParameters(body, target);

        var rebound = Assert.IsAssignableFrom<BinaryExpression>(actual);
        Assert.Same(target, ((MemberExpression)rebound.Left).Expression);
    }

    [Fact]
    public static void Leaves_parameters_of_other_types_alone()
    {
        var target = Expression.Parameter(typeof(Order), "e");
        var unrelated = Expression.Parameter(typeof(int), "i");
        var body = Expression.Add(unrelated, Expression.Constant(1));

        var actual = MongoShapedQueryCompilingExpressionVisitor.RebindSetterValueParameters(body, target);

        Assert.Same(unrelated, ((BinaryExpression)actual).Left);
    }

    [Fact]
    public static void Throws_when_the_body_has_two_distinct_entity_parameters()
    {
        var target = Expression.Parameter(typeof(Order), "e");
        var first = Expression.Parameter(typeof(Order), "a");
        var second = Expression.Parameter(typeof(Order), "b");
        var body = Expression.Add(
            Expression.Property(first, nameof(Order.Quantity)),
            Expression.Property(second, nameof(Order.Quantity)));

        var ex = Assert.Throws<InvalidOperationException>(
            () => MongoShapedQueryCompilingExpressionVisitor.RebindSetterValueParameters(body, target));

        Assert.Contains("more than one", ex.Message);
    }
}

#endif
```

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkSetterRebindingTests"
```

Expected: does not compile — `RebindSetterValueParameters` does not exist. That counts as a failing test; proceed.

- [ ] **Step 3: Replace the visitor with a guarded entry point**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, replace the `ParameterRebindingExpressionVisitor` class with:

```csharp
    /// <summary>
    /// Rebinds the <see cref="ParameterExpression"/> in a translated self-referencing setter body to the shared
    /// entity parameter, so the assembled value lambda has exactly one parameter as required by the renderer.
    /// Rebinding is by type rather than identity because translation through
    /// <see cref="MongoEFToLinqTranslatingExpressionVisitor"/> discards the original parameter identity — which is
    /// only sound while there is exactly one parameter of the entity type. More than one is rejected rather than
    /// silently merged, because merging renders a wrong update with no error.
    /// </summary>
    internal static Expression RebindSetterValueParameters(Expression body, ParameterExpression target)
    {
        var collector = new EntityParameterCollector(target.Type);
        collector.Visit(body);

        if (collector.Parameters.Count > 1)
        {
            throw new InvalidOperationException(
                $"A self-referencing bulk update setter value referenced more than one '{target.Type.ShortDisplayName()}' "
                + "parameter, which the MongoDB provider cannot render as a single aggregation expression.");
        }

        return new ParameterRebindingExpressionVisitor(target).Visit(body)!;
    }

    private sealed class EntityParameterCollector(Type entityType) : System.Linq.Expressions.ExpressionVisitor
    {
        public HashSet<ParameterExpression> Parameters { get; } = [];

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Type == entityType)
            {
                Parameters.Add(node);
            }

            return base.VisitParameter(node);
        }
    }

    private sealed class ParameterRebindingExpressionVisitor(ParameterExpression target)
        : System.Linq.Expressions.ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node.Type == target.Type ? target : base.VisitParameter(node);
    }
```

Then in `RenderSelfReferencingValue`, replace the rebinding line:

```csharp
        translatedBody = RebindSetterValueParameters(translatedBody, entityParameter);
```

`ShortDisplayName()` comes from `Microsoft.EntityFrameworkCore.Internal`; if that using is not already present in the file, use `target.Type.Name` instead rather than adding an internal-namespace using.

- [ ] **Step 4: Run the tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkSetterRebindingTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS, 3 new unit tests, and no regression in the self-referencing functional tests.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkSetterRebindingTests.cs
git commit -m "fix: reject setter values with multiple entity parameters instead of merging them silently"
```

---

