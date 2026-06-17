# Native MQL: captured-expression shapes (diagnostic)

This records the verbatim `MongoQueryExpression.CapturedExpression.ToString()` for the five benchmark smoke query shapes, captured by temporary instrumentation in `MongoShapedQueryCompilingExpressionVisitor.TranslateQuery<TSource>` (now reverted), so the native MQL translator can be built to match the real expression shapes.

## Captured expressions

### 1. `ctx.Customers.Where(c => c.Active).ToList()`
```
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(c => c.Active).Select(c => [Microsoft.EntityFrameworkCore.Query.IncludeExpression])
```

### 2. `ctx.Customers.Select(c => new { c.Name, c.Count }).ToList()`
```
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Select(c => new <>f__AnonymousType0`2(Name = c.Name, Count = c.Count))
```

### 3. `ctx.Customers.OrderBy(c => c.Count).Take(10).ToList()`
```
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(c => c.Count).Take([Microsoft.EntityFrameworkCore.Query.QueryParameterExpression]).Select(c => [Microsoft.EntityFrameworkCore.Query.IncludeExpression])
```

### 4. `ctx.Customers.ToList()` (tracked)
```
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Select(c => [Microsoft.EntityFrameworkCore.Query.IncludeExpression])
```

### 5. `ctx.Customers.AsNoTracking().ToList()`
```
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Select(c => [Microsoft.EntityFrameworkCore.Query.IncludeExpression])
```

(Shapes 4 and 5 produce an identical captured-expression string — the tracked/no-tracking distinction is NOT visible in the captured chain.)

## Notes

**(a) How is a property access represented in the predicate/selector?**
It is a plain CLR member access, `e.PropName` — NOT an `EF.Property<T>(e, "PropName")` method call. Exact substrings:
- Predicate: `c => c.Active`
- Selector members: `Name = c.Name, Count = c.Count`
- OrderBy key: `c => c.Count`

**(b) How does the `Take(10)` count appear?**
It is NOT a literal `Constant(10)`. It appears as an EF query parameter node: `Take([Microsoft.EntityFrameworkCore.Query.QueryParameterExpression])`. The exact value (10) is not in the captured string; it is carried as a `QueryParameterExpression` whose value is supplied at execution time from the `QueryContext` parameter dictionary. Exact substring: `.Take([Microsoft.EntityFrameworkCore.Query.QueryParameterExpression])`.

**(c) Does a trailing `Select` appear in the captured chain?**
Yes — for entity-returning queries (shapes 1, 3, 4, 5) the chain ends with a synthetic trailing `.Select(c => [Microsoft.EntityFrameworkCore.Query.IncludeExpression])`. This is the entity projection (an `IncludeExpression` over the entity), appended by EF even when the user wrote no `Select`. For an explicit user projection to an anonymous type (shape 2) the trailing node is the user's own `Select` to `new <>f__AnonymousType0`2(...)` and there is no extra `IncludeExpression` Select. So: the projection IS part of the captured chain as the final `Select`, not handled entirely elsewhere.

**(d) For `Where(c => c.Active)`, how does the bare boolean appear?**
It appears as the bare member access with no comparison — `c => c.Active`. There is NO `== True` / `Equal(..., Constant(true))` wrapper in the captured expression; the predicate body is just the boolean property access.
