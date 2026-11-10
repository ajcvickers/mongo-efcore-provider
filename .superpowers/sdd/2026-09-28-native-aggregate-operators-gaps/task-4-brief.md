### Task 4: Fold a closed `Enumerable.Where` over a local collection

Closes gap **B4** (2 tests).

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/Visitors/LocalCollectionFilterFoldingVisitor.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/MongoQueryTranslationPreprocessor.cs` (`Process`)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/AGENTS.md` (the preprocessor line in "Pipeline at a glance")
- Modify: `tests/.../FunctionalTests/Query/NativeLocalCollectionContainsTests.cs` (append)
- Modify (baselines): `NorthwindAggregateOperatorsQueryMongoTest.cs`

**Interfaces:**
- Consumes: `NativeQueryParameter.TryGetQueryParameterName(Expression, out string?)`; EF's `QueryCompilationContext.RegisterRuntimeParameter(string, LambdaExpression)`, which returns `ParameterExpression` on EF8/EF9 and `QueryParameterExpression` on EF10 (typed here as `Expression`); `QueryCompilationContext.QueryContextParameter`.
- Produces: `internal sealed class LocalCollectionFilterFoldingVisitor(QueryCompilationContext) : ExpressionVisitor`.

Design: EF's funcletizer leaves `Enumerable.Where(<constant or query-parameter collection>, e => e != null)` unevaluated inside a `Contains`. Folding the closed filter on the client turns it into a plain, indexable `$in` on **both** query paths (MQL is not contract, so this is fine):
- constant source: evaluate it once at compile time into a `T[]` constant;
- parameter source: register a runtime parameter whose extractor re-applies the filter to the parameter's value on **every execution** (Review Focus 1).

The fold only happens when the predicate lambda is closed: it references nothing but its own parameter, with no query parameters, extension nodes or nested lambdas.

- [ ] **Step 1: Failing functional tests** (append to `NativeLocalCollectionContainsTests`):

```csharp
    [Fact]
    public void Filtered_inline_list_contains_goes_native()
        => Assert.Equal(["a"], Names(nameof(Filtered_inline_list_contains_goes_native),
            q => q.Where(r => new List<string?> { "a", null, "zzz" }.Where(n => n != null).Contains(r.Name))));

    // Review Focus 1: the same compiled query executed twice with a different captured value.
    [Fact]
    public void Filtered_parameter_collection_is_re_evaluated_per_execution()
    {
        var collection = Seed(nameof(Filtered_parameter_collection_is_re_evaluated_per_execution));
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.DriverLinq })
        {
            var id = "a";
            using (var db = CreateContext(collection, mode))
                Assert.Equal(["a"], db.Entities.Where(r => new List<string?> { "zzz", id }.Where(n => n != null).Contains(r.Name))
                    .Select(r => r.Name).ToList());

            id = "b";
            using (var db = CreateContext(collection, mode))
                Assert.Equal(["b"], db.Entities.Where(r => new List<string?> { "zzz", id }.Where(n => n != null).Contains(r.Name))
                    .Select(r => r.Name).ToList());
        }
    }

    [Fact]
    public void Filter_capturing_another_variable_is_not_folded_and_declines_cleanly()
    {
        var excluded = "a";
        var names = new List<string> { "a", "b" };
        var collection = Seed(nameof(Filter_capturing_another_variable_is_not_folded_and_declines_cleanly));
        var result = NativeModeAssert.DeclinesCleanly(mode =>
        {
            using var db = CreateContext(collection, mode);
            return db.Entities.Where(r => names.Where(n => n != excluded).Contains(r.Name)).Select(r => r.Name).ToList();
        });
        Assert.Equal(["b"], result);
    }
```

- [ ] **Step 2: Run → the first two fail** (`NativeTranslationNotSupportedException`). The third should already pass. If the third fails because DriverLinq itself can't translate the shape, replace it with a test that only asserts `Assert.Throws<NativeTranslationNotSupportedException>` under `NativeOnly`, and note that in the commit message.

- [ ] **Step 3: Create the visitor** `src/MongoDB.EntityFrameworkCore/Query/Visitors/LocalCollectionFilterFoldingVisitor.cs` (license header copied from `MongoQueryTranslationPreprocessor.cs`, same BOM status):

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.Query.Visitors;

/// <summary>
/// Folds a CLOSED <c>Enumerable.Where(localCollection, e =&gt; …)</c> that is the collection argument of an
/// <c>Enumerable.Contains</c> — e.g. <c>new List&lt;string&gt; { "ABCDE", id }.Where(e =&gt; e != null).Contains(c.CustomerID)</c>
/// (EF's <c>Contains_with_local_enumerable_inline</c>/<c>_closure_mix</c>) — into a plain local collection, so the
/// Contains translates to an ordinary indexable <c>$in</c> on both the native and driver-LINQ paths.
/// </summary>
/// <remarks>
/// A constant source is filtered once, here, at compile time. A query-parameter source cannot be (its value is
/// per-execution), so it is replaced by an EF runtime parameter (<see cref="QueryCompilationContext.RegisterRuntimeParameter"/>)
/// whose extractor re-applies the filter to the parameter's value on every execution. Only a predicate that
/// references nothing but its own lambda parameter is folded; anything else is left untouched.
/// </remarks>
internal sealed class LocalCollectionFilterFoldingVisitor(QueryCompilationContext queryCompilationContext) : ExpressionVisitor
{
    private static readonly MethodInfo EnumerableWhere = typeof(Enumerable).GetMethods()
        .Single(m => m.Name == nameof(Enumerable.Where)
                     && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));

    private static readonly MethodInfo EnumerableToArray = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!;

    private int _foldedParameterCount;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node is { Method: { IsStatic: true, Name: nameof(Enumerable.Contains) }, Arguments.Count: 2 }
            && node.Method.DeclaringType == typeof(Enumerable)
            && TryFold(node.Arguments[0], out var folded))
        {
            return node.Update(null, [folded, Visit(node.Arguments[1])]);
        }

        return base.VisitMethodCall(node);
    }

    private bool TryFold(Expression collection, out Expression folded)
    {
        folded = collection;

        if (collection is not MethodCallExpression { Method.IsGenericMethod: true } whereCall
            || whereCall.Method.GetGenericMethodDefinition() != EnumerableWhere
            || whereCall.Arguments[1] is not LambdaExpression predicate
            || !FreeVariableFinder.IsClosed(predicate))
        {
            return false;
        }

        var source = whereCall.Arguments[0];
        var toArray = EnumerableToArray.MakeGenericMethod(whereCall.Method.GetGenericArguments()[0]);

        if (source is ConstantExpression)
        {
            var evaluate = Expression.Lambda<Func<object?>>(
                Expression.Convert(Expression.Call(toArray, whereCall), typeof(object)));
            folded = Expression.Constant(evaluate.Compile()(), toArray.ReturnType);
            return true;
        }

        if (NativeQueryParameter.TryGetQueryParameterName(source, out var parameterName))
        {
            var queryContext = QueryCompilationContext.QueryContextParameter;
            var sourceValue = Expression.Convert(
                Expression.Property(ParameterValues(queryContext), "Item", Expression.Constant(parameterName)),
                source.Type);
            var extractor = Expression.Lambda(
                Expression.Call(toArray, Expression.Call(whereCall.Method, sourceValue, predicate)), queryContext);

            // "__" prefix: on EF8/EF9 a query parameter is a ParameterExpression recognized by that prefix
            // (NativeQueryParameter.TryGetQueryParameterName); harmless on EF10.
            folded = queryCompilationContext.RegisterRuntimeParameter(
                $"__mongo_filtered_{_foldedParameterCount++}", extractor);
            return true;
        }

        return false;
    }

    private static Expression ParameterValues(ParameterExpression queryContext)
#if EF8 || EF9
        => Expression.Property(queryContext, nameof(QueryContext.ParameterValues));
#else
        => Expression.Property(queryContext, nameof(QueryContext.Parameters));
#endif

    private sealed class FreeVariableFinder(IReadOnlyCollection<ParameterExpression> bound) : ExpressionVisitor
    {
        private bool _found;

        public static bool IsClosed(LambdaExpression lambda)
        {
            var finder = new FreeVariableFinder(lambda.Parameters);
            finder.Visit(lambda.Body);
            return !finder._found;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _found |= !bound.Contains(node);
            return node;
        }

        // A QueryParameterExpression (EF10) or any other provider/EF extension node: not closed.
        protected override Expression VisitExtension(Expression node)
        {
            _found = true;
            return node;
        }

        // Nested lambdas: out of scope, keep the fold narrow.
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            _found = true;
            return node;
        }
    }
}
```

`Expression.Property(dict, "Item", key)` needs the dictionary's indexer. EF10's `Parameters` is `Dictionary<string, object?>` and EF8/9's `ParameterValues` is `IReadOnlyDictionary<string, object?>`; both expose `Item`. If `Expression.Property(…, "Item", …)` fails to resolve on the interface type, use `Expression.MakeIndex(dict, dict.Type.GetProperty("Item"), [Expression.Constant(parameterName)])`, looking the property up on `typeof(IReadOnlyDictionary<string, object?>)` for EF8/9.

- [ ] **Step 4: Wire it into the preprocessor.** In `MongoQueryTranslationPreprocessor.Process`, directly after the `EntityFrameworkDetourExpressionVisitor` line:

```csharp
        query = new LocalCollectionFilterFoldingVisitor(QueryCompilationContext).Visit(query);
```

- [ ] **Step 5: Update `Query/AGENTS.md`.** Change the preprocessor line in "Pipeline at a glance" to:

```
   ▼  MongoQueryTranslationPreprocessor      (hoist final predicates; fold closed Where over a local Contains collection; lift VectorSearch out before nav expansion)
```

- [ ] **Step 6: Build all three configurations** (the `#if` must compile everywhere):

```bash
for c in "Debug EF8" "Debug EF9" "Debug EF10"; do dotnet build MongoDB.EFCoreProvider.sln -c "$c" 2>&1 | grep -E "Error\(s\)"; done
```

Expected `0 Error(s)` ×3.

- [ ] **Step 7: Run the functional tests (EF10) → pass.**

- [ ] **Step 8: Spec tests** `Contains_with_local_enumerable_inline Contains_with_local_enumerable_inline_closure_mix`: native-only → regenerate → both modes green. Expected MQL becomes an indexable `{ "$match" : { "_id" : { "$in" : ["ABCDE", "ALFKI"] } } }` (and `"ANATR"` for the second closure-mix query).

- [ ] **Step 9: Regression sweep** (−4). Also run the **normal-mode** spec Query sweep and grep the diff for unexpectedly changed baselines: this rewrite also changes the driver-LINQ path.

- [ ] **Step 10: Commit** — `git commit -m "EF-322: fold closed Enumerable.Where over a local Contains collection"`

---

