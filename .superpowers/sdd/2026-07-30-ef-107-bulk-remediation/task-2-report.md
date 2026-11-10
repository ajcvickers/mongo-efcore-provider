# Task 2 report — Unit-test and narrow `UnwrapBulkOperator`

## What changed and why

`MongoNonQueryExpression.UnwrapBulkOperator` (in
`src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs`) previously
unwrapped **any** `MethodCallExpression` whose method was declared on
`EntityFrameworkQueryableExtensions` from the head of a captured query chain — not just the
four bulk operators (`ExecuteDelete`, `ExecuteDeleteAsync`, `ExecuteUpdate`,
`ExecuteUpdateAsync`). That type also declares non-bulk methods such as `AsNoTracking`,
`IgnoreQueryFilters`, `TagWith`, etc. If any of those appeared as the outermost call in a
captured chain, the old implementation would silently discard it instead of leaving it for the
classifier to see and reject. This had no covering test.

Fix: added a `BulkOperatorNames` array and narrowed the predicate to also check
`call.Method.Name` against that list via `Array.IndexOf`, per the brief's exact replacement
text. Added `using System;` to the file's using block (it was not already present) for
`Array.IndexOf`.

New test file created (server-free, no DB):
`tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs`,
guarded by `#if !EF8` (matches the brief; runs on EF9/EF10 only, but must still let EF8 compile
since the guard excludes the whole file body from that build).

## The observed failure before the fix (the evidence the fix was necessary)

First attempt at the third test used the brief's literal reflection call:
`GetMethod(nameof(AsNoTracking), new[] { typeof(IQueryable<Product>) })`. On this repo's EF10
package, that overload-array lookup returned `null` (a reflection binder quirk with generic
extension methods, not an overload-ambiguity), so `Expression.Call` itself threw before the
`Assert.Same` under test ever ran:

```
Failed MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions.MongoNonQueryExpressionUnwrapTests.Does_not_unwrap_a_non_bulk_EntityFrameworkQueryableExtensions_call [< 1 ms]
  Error Message:
   System.ArgumentNullException : Value cannot be null. (Parameter 'method')
  Stack Trace:
     at System.ArgumentNullException.Throw(String paramName)
   at System.ArgumentNullException.ThrowIfNull(Object argument, String paramName)
   at System.Linq.Expressions.Expression.Call(MethodInfo method, Expression arg0)
   at MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions.MongoNonQueryExpressionUnwrapTests.Does_not_unwrap_a_non_bulk_EntityFrameworkQueryableExtensions_call() ...
```

This is the "reflection lookup differs on your EF version" case the brief anticipated. I probed
both `AsNoTracking` and `IgnoreQueryFilters` with the type-array `GetMethod` overload in
isolation (a throwaway `net10.0` console probe referencing `Microsoft.EntityFrameworkCore`
10.0.0) — both returned `null` for that lookup style, even though `AsNoTracking` has exactly one
overload (`IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity>)`). Switching to the
brief's other reflection idiom (the same one already used for `ExecuteDelete` in test 1) —
`GetMethod(nameof(AsNoTracking))` with no type array (safe since there is only one overload),
then `.MakeGenericMethod(typeof(Product))` — resolved correctly. With that fix to the *test's*
reflection call (not the production code), the test then failed as intended, against the
still-unfixed production code:

```
Failed MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions.MongoNonQueryExpressionUnwrapTests.Does_not_unwrap_a_non_bulk_EntityFrameworkQueryableExtensions_call [1 ms]
  Error Message:
   Assert.Same() Failure: Values are not the same instance
Expected: MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions.MongoNonQueryExpressionUnwrapTests+Product[].Where(p => (p.Name == "x")).AsNoTracking()
Actual:   MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions.MongoNonQueryExpressionUnwrapTests+Product[].Where(p => (p.Name == "x"))
```

This confirms the pre-fix implementation stripped `AsNoTracking()` off the chain (`Actual` is
missing the `.AsNoTracking()` call that `Expected`, i.e. `captured` itself, retains) — exactly
the bug the brief describes. The other three tests (`Unwraps_ExecuteDelete_...`,
`Returns_expression_unchanged_when_not_a_bulk_operator`, `Returns_null_for_null`) passed both
before and after.

## Which reflection lookup I used, and why

Used `AsNoTracking` (the brief's primary choice, not the `IgnoreQueryFilters` fallback), but
with the brief's *own* alternate reflection idiom rather than the type-array `GetMethod`
overload it initially specified: `GetMethod(nameof(AsNoTracking))!.MakeGenericMethod(typeof(Product))`.
Rationale: `AsNoTracking` has exactly one overload on this EF version (verified by listing
`typeof(EntityFrameworkQueryableExtensions).GetMethods()` filtered by name), so the
no-type-array `GetMethod(string)` overload resolves unambiguously, then `MakeGenericMethod`
closes it over `Product` — the identical pattern the brief already uses for `ExecuteDelete` in
the first test. I did not need the `IgnoreQueryFilters` fallback (confirmed via the same probe
that `IgnoreQueryFilters` has two overloads and would have needed a type array or `.First()`
filter too, so it offered no advantage). `AsNoTracking` is declared on
`EntityFrameworkQueryableExtensions` and is not one of the four bulk operator names, so it
satisfies the brief's requirement regardless of which method was chosen.

## Exact test commands run and outcomes

1. Before fix, initial (broken) reflection call:
   ```
   dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
     --filter "FullyQualifiedName~MongoNonQueryExpressionUnwrapTests"
   ```
   Result: 1 failed (`ArgumentNullException` from `Expression.Call`, not the intended
   `Assert.Same` failure — see above), 3 passed.

2. Before fix, after correcting the test's reflection call to `GetMethod(name).MakeGenericMethod(...)`:
   ```
   dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
     --filter "FullyQualifiedName~MongoNonQueryExpressionUnwrapTests"
   ```
   Result: 1 failed (`Assert.Same()` — the intended pre-fix failure, quoted above), 3 passed.

3. After production fix (Step 4a):
   ```
   dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
     --filter "FullyQualifiedName~MongoNonQueryExpressionUnwrapTests"
   ```
   Result: `Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4` (`MongoDB.EntityFrameworkCore.UnitTests.dll`, net10.0).

4. After production fix (Step 4b), pre-existing tests on EF9:
   ```
   dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
     --filter "FullyQualifiedName~MongoNonQueryExpression"
   ```
   Result: `Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10` (`MongoDB.EntityFrameworkCore.UnitTests.dll`) — the 6 pre-existing `MongoNonQueryExpressionTests` plus the 4 new `MongoNonQueryExpressionUnwrapTests` (name filter matches both classes on EF9; the `#if !EF8` guard has no effect on EF9).

5. EF8 build check (global constraint — bulk code and its tests are `#if !EF8`, but EF8 must
   still compile):
   ```
   dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
   ```
   Result: `Build succeeded. 0 Warning(s) 0 Error(s)`.

6. Bulk functional suites, no regression check (Step 5), `MONGODB_URI`/`ATLAS_URI` left unset so
   TestContainers boots `mongodb/mongodb-atlas-local` automatically (image already pulled from
   an earlier task — boot was fast):
   ```
   dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
     --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
   ```
   Result: `Passed! - Failed: 0, Passed: 42, Skipped: 1, Total: 43` (`MongoDB.EntityFrameworkCore.FunctionalTests.dll`, net10.0). The 1 skip
   (`ExecuteDelete_two_phase_on_standalone_throws_actionable_error`) is a pre-existing
   environment-conditional skip, unrelated to this change.

## Anything noticed but deliberately not changed

- The repo's `HEAD` at the start of this task (`8e1f728 test: restore foreign-queryable-extension
  regression test, add EF8 ExecuteUpdate case`) already contains other bulk-suite test changes
  from a prior task in this remediation plan; I left all of that alone and touched only the two
  files this task specifies.
- `Array.IndexOf(BulkOperatorNames, name)` is a linear scan over 4 short strings — negligible
  and exactly the brief's specified replacement; did not "optimize" to a `HashSet` or switch
  expression since the brief's code block is to be used verbatim.
- Did not reformat or otherwise touch the surrounding `MongoNonQueryExpression.cs` members
  (constructors, `Setter` record, `VisitChildren`, etc.) — only the using block and the
  `UnwrapBulkOperator` method plus its new `BulkOperatorNames` field were touched.
- Did not touch the pre-existing `MongoNonQueryExpressionTests.cs` sibling file, the plan
  document, or anything else under `.superpowers/` besides this report.

## Commit

One commit created with the message specified verbatim by the brief's Step 6:
`fix: unwrap only the four bulk operators, not every EntityFrameworkQueryableExtensions call`,
containing exactly:
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs`
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs`
