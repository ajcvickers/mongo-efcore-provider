### Task 19: Cache the compiled setter value and the aggregate renderer

Two costs recur on **every execution** of an already-compiled query:

- `CompileAndEvaluate` calls `Expression.Lambda<Func<object?>>(…).Compile()()`. It is a fallback (constants, closure members and query parameters are handled first) but when reached it costs more than everything else in `BuildUpdate` combined.
- `RenderSelfReferencingValue` calls `RenderAggregateExpressionMethodInfo.MakeGenericMethod(typeof(TSource), resultType).Invoke(...)` per self-referencing setter, per execution.

Both are cacheable for the same reason: `VisitNonQuery` bakes the marker node in with `Expression.Constant(nonQueryExpression)`, so EF's query cache hands back **the same `MongoNonQueryExpression` instance** every execution.

Per-execution *translation* (`BuildFilter`, `BuildUpdate`, `GetEntitySerializer`) is deliberately left alone: the read path re-translates per execution too, so that is provider architecture rather than a bulk-specific regression.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `MongoNonQueryExpression.Setter` gains a mutable cache slot. Because it is a `record` with an `IProperty`, `Expression` and `bool`, add the slot as a field rather than a positional member so record equality is unaffected:
  ```csharp
  public sealed record Setter(IProperty Property, Expression ValueExpression, bool IsSelfReferencing)
  {
      internal Func<object?>? CompiledValue;
  }
  ```

- [ ] **Step 1: Add the cache slot**

Apply the `Setter` change above in `MongoNonQueryExpression.cs`, with a comment:

```csharp
    /// <summary>
    /// A single parsed <c>SetProperty</c>. <see cref="CompiledValue"/> is a lazily-populated cache for the
    /// compile-and-evaluate fallback in the execution path: this node is baked into the compiled query as a
    /// constant, so the same instance is reused for every execution and the delegate only needs building once.
    /// It is a field rather than a record member so it plays no part in record equality.
    /// </summary>
    public sealed record Setter(IProperty Property, Expression ValueExpression, bool IsSelfReferencing)
    {
        internal Func<object?>? CompiledValue;
    }
```

- [ ] **Step 2: Use it in the evaluator**

Change `SerializeConstant` to pass the setter through and `EvaluateToConstant` to consult the cache:

```csharp
    private static BsonValue SerializeConstant(QueryContext queryContext, MongoNonQueryExpression.Setter setter)
    {
        var value = EvaluateToConstant(queryContext, setter);
        var serializationInfo = BsonSerializerFactory.GetPropertySerializationInfo(setter.Property);
        return serializationInfo.SerializeValue(value);
    }

    private static object? EvaluateToConstant(QueryContext queryContext, MongoNonQueryExpression.Setter setter)
    {
        var expression = setter.ValueExpression;
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            expression = convert.Operand;
        }

        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        if (expression is MemberExpression { Expression: ConstantExpression closureConstant } member)
        {
            switch (member.Member)
            {
                case FieldInfo field:
                    return field.GetValue(closureConstant.Value);
                case PropertyInfo prop:
                    return prop.GetValue(closureConstant.Value);
            }
        }

#if EF8 || EF9
        if (expression is ParameterExpression param
            && param.Name?.StartsWith(QueryCompilationContext.QueryParameterPrefix, StringComparison.Ordinal) == true
            && queryContext.ParameterValues.TryGetValue(param.Name, out var value))
        {
            return value;
        }
#else
        if (expression is Microsoft.EntityFrameworkCore.Query.QueryParameterExpression queryParam)
        {
            return queryContext.Parameters[queryParam.Name];
        }
#endif

        // Compile-and-evaluate fallback. The setter belongs to a node baked into the compiled query, so cache
        // the delegate: without this it is rebuilt on every execution, which dominates the cost of building the
        // update document. Failures are intentionally allowed to propagate — throwing on an un-evaluatable
        // setter value is preferable to silently writing null.
        var compiled = setter.CompiledValue;
        if (compiled == null)
        {
            compiled = Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile();
            setter.CompiledValue = compiled;
        }

        return compiled();
    }
```

Delete the now-unused `CompileAndEvaluate` method.

Note the behaviour change this makes explicit: caching is only valid because the fallback branch is reached solely for expressions with no query-parameter dependency. If a future change routes a parameter-dependent expression here, the cache would return a stale value — so add an assertion guarding that assumption:

```csharp
        Debug.Assert(
            !ContainsQueryParameter(expression),
            "The compile-and-evaluate fallback caches its delegate, so it must never be reached for a "
            + "parameter-dependent expression.");
```

with:

```csharp
    private static bool ContainsQueryParameter(Expression expression)
        => new QueryParameterDetector().Detect(expression);

    private sealed class QueryParameterDetector : System.Linq.Expressions.ExpressionVisitor
    {
        private bool _found;

        public bool Detect(Expression expression)
        {
            _found = false;
            Visit(expression);
            return _found;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _found = true;
            return base.VisitParameter(node);
        }
    }
```

- [ ] **Step 3: Cache the aggregate renderer**

```csharp
    // MakeGenericMethod + Invoke per setter per execution is pure overhead on a path that exists to be fast.
    private static readonly ConcurrentDictionary<(Type Source, Type Result), MethodInfo> RenderAggregateExpressionCache = new();

    private static MethodInfo GetAggregateRenderer(Type sourceType, Type resultType)
        => RenderAggregateExpressionCache.GetOrAdd(
            (sourceType, resultType),
            static key => RenderAggregateExpressionMethodInfo.MakeGenericMethod(key.Source, key.Result));
```

and in `RenderSelfReferencingValue` replace the two lines that build and invoke the renderer:

```csharp
        var renderer = GetAggregateRenderer(typeof(TSource), setter.Property.ClrType);
        return (BsonValue)renderer.Invoke(null, [entityParameter, translatedBody, efSerializer])!;
```

Add `using System.Collections.Concurrent;`.

- [ ] **Step 4: Run the bulk suites on both EF versions**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --filter "FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. `ExecuteUpdate_with_captured_variable_set_value` is the test that most directly exercises the caching path — if it passes on a repeated run within one process, the cache is not returning stale values for the shapes in use.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/
git commit -m "perf: cache the compiled setter fallback delegate and the aggregate-expression renderer"
```

---

## Phase 6 — Coverage completion

