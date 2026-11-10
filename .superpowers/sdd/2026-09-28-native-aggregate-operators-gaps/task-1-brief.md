### Task 1: Selector-less Sum/Min/Max/Average over a bare scalar Select

Closes gap **A** (11 tests). Also adds the `TryGetBareServerValueProjection` helper that Task 8 reuses.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeCardinalityBinder.cs` (`TryBindAggregate` signature and operand block, near lines 118 and 236–251; new helper)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` (`BindAggregateOrFallback`, ~line 2479)
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeSelectorlessAggregateTests.cs`
- Modify (baselines): `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindAggregateOperatorsQueryMongoTest.cs`

**Interfaces:**
- Produces: `internal static MongoProjection? NativeCardinalityBinder.TryGetBareServerValueProjection(MongoQueryExpression mongoQ, Expression sourceShaper)`. It returns the single `_v`-aliased projection when the source is a bare, server-computed scalar `Select` that the shaper reads back unchanged, else `null`.
- Produces: `TryBindAggregate(..., Type resultType, MongoProjection? bareSourceProjection = null)` (new optional trailing parameter).

- [ ] **Step 1: Write the failing functional tests**

Create `NativeSelectorlessAggregateTests.cs` (license header + BOM copied from `NativeCardinalityTests.cs`):

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// A selector-less Sum()/Min()/Max()/Average() reducing the value a preceding bare scalar Select projected
/// (<c>Select(e =&gt; e.Value).Sum()</c>) — EF's Northwind <c>*_with_no_arg</c> family. The aggregate reduces the
/// Select's own <c>$project</c> output alias.
/// </summary>
[XUnitCollection("QueryTests")]
public class NativeSelectorlessAggregateTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Row
    {
        public ObjectId Id { get; set; }
        public int Value { get; set; }
        public int? NullableValue { get; set; }
    }

    private IMongoCollection<Row> Seed(string name, params (int Value, int? NullableValue)[] rows)
    {
        var collection = database.MongoDatabase.GetCollection<Row>(
            TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8]);
        if (rows.Length > 0)
            collection.InsertMany(rows.Select(r => new Row { Id = ObjectId.GenerateNewId(), Value = r.Value, NullableValue = r.NullableValue }));
        return collection;
    }

    private static SingleEntityDbContext<Row> CreateContext(IMongoCollection<Row> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(collection, optionsBuilderAction: b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    private static readonly (int, int?)[] OneTwoThree = [(1, 10), (2, null), (3, 30)];

    private List<T> Run<T>(string name, (int, int?)[] rows, Func<IQueryable<Row>, T> query)
    {
        var collection = Seed(name, rows);
        return NativeModeAssert.NativeAndParity(mode =>
        {
            using var db = CreateContext(collection, mode);
            return [query(db.Entities)];
        });
    }

    [Fact]
    public void Sum_over_bare_member_select_goes_native()
        => Assert.Equal([6], Run(nameof(Sum_over_bare_member_select_goes_native), OneTwoThree, q => q.Select(e => e.Value).Sum()));

    [Fact]
    public void Min_over_bare_member_select_goes_native()
        => Assert.Equal([1], Run(nameof(Min_over_bare_member_select_goes_native), OneTwoThree, q => q.Select(e => e.Value).Min()));

    [Fact]
    public void Max_over_bare_member_select_goes_native()
        => Assert.Equal([3], Run(nameof(Max_over_bare_member_select_goes_native), OneTwoThree, q => q.Select(e => e.Value).Max()));

    [Fact]
    public void Average_over_bare_member_select_goes_native()
        => Assert.Equal([2.0], Run(nameof(Average_over_bare_member_select_goes_native), OneTwoThree, q => q.Select(e => e.Value).Average()));

    [Fact]
    public void Sum_over_computed_select_goes_native()
        => Assert.Equal([12], Run(nameof(Sum_over_computed_select_goes_native), OneTwoThree, q => q.Select(e => e.Value * 2).Sum()));

    [Fact]
    public void Average_over_widening_cast_select_goes_native()
        => Assert.Equal([2.0], Run(nameof(Average_over_widening_cast_select_goes_native), OneTwoThree,
            q => q.Where(e => e.Value > 0).OrderBy(e => e.Value).Select(e => (long)e.Value).Average()));

    [Fact]
    public void Max_over_nullable_select_ignores_nulls()
        => Assert.Equal([30], Run(nameof(Max_over_nullable_select_ignores_nulls), OneTwoThree, q => q.Select(e => e.NullableValue).Max()));

    // Review Focus 2: empty-input contract.
    [Fact]
    public void Sum_over_empty_nullable_cast_select_returns_zero()
        => Assert.Equal([0], Run(nameof(Sum_over_empty_nullable_cast_select_returns_zero), [],
            q => q.Select(e => (int?)e.Value).Sum()));

    [Fact]
    public void Max_over_empty_nullable_select_returns_null()
        => Assert.Equal([null], Run(nameof(Max_over_empty_nullable_select_returns_null), [],
            q => q.Select(e => e.NullableValue).Max()));

    [Fact]
    public void Min_over_empty_non_nullable_select_throws_under_NativeOnly()
    {
        var collection = Seed(nameof(Min_over_empty_non_nullable_select_throws_under_NativeOnly));
        using var db = CreateContext(collection, MongoQueryMode.NativeOnly);
        Assert.Throws<InvalidOperationException>(() => db.Entities.Select(e => e.Value).Min());
    }

    // Review Focus 5: the shaper does client work on top of the pushed-down value — must NOT aggregate the raw field.
    private static int TimesTen(Row row) => row.Value * 10;

    [Fact]
    public void Aggregate_over_client_method_projection_declines()
    {
        var collection = Seed(nameof(Aggregate_over_client_method_projection_declines), OneTwoThree);
        using var db = CreateContext(collection, MongoQueryMode.NativeOnly);
        Assert.Throws<NativeTranslationNotSupportedException>(() => db.Entities.Select(e => TimesTen(e)).Sum());
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test $FUNC -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeSelectorlessAggregateTests"`

Expected: every `*_goes_native`, `*_returns_*` and `Max_over_nullable_*` test fails with `NativeTranslationNotSupportedException: Query projects a non-entity result…`. `Min_over_empty_non_nullable_select_throws_under_NativeOnly` also fails, because the exception is the wrong type. `Aggregate_over_client_method_projection_declines` should already pass. If it doesn't, stop and report: that means the client-method shape reaches a different path than assumed.

- [ ] **Step 3: Add the helper and the operand arm in `NativeCardinalityBinder.cs`**

Add `using Microsoft.EntityFrameworkCore.Query;` (for `ProjectionBindingExpression`). Add this method directly after `TryBindReducer`:

```csharp
    /// <summary>
    /// The single server-computed value a preceding bare scalar <c>Select</c> projected — <c>Select(o =&gt; o.OrderID)</c>,
    /// <c>Select(o =&gt; o.OrderID * 2)</c>, <c>Select(o =&gt; (long)o.OrderID)</c> — when <paramref name="sourceShaper"/>
    /// reads exactly that value back with no client-side computation layered on top; otherwise
    /// <see langword="null"/>. A selector-less terminal (<c>Sum()</c>/<c>Min()</c>/<c>Max()</c>/<c>Average()</c>,
    /// <c>Contains(item)</c>) reduces this value.
    /// </summary>
    /// <remarks>
    /// The shaper check is what rejects a client-evaluated projection: a client method call or other client
    /// computation leaves a non-<see cref="ProjectionBindingExpression"/> node in the shaper. The string-sequence
    /// leaf is the one client-applied leaf that ERASES its call from the shaper (it is registered as one projection
    /// member — see <see cref="MongoSelectDefinition.HasStringSequenceProjectionLeaf"/>), so it is excluded by flag.
    /// </remarks>
    internal static MongoProjection? TryGetBareServerValueProjection(MongoQueryExpression mongoQ, Expression sourceShaper)
    {
        var select = mongoQ.Select;
        if (select.Grouping != null
            || select.HasTerminalOperator
            || select.HasStringSequenceProjectionLeaf
            || select.Projection is not [{ Alias: NativeProjectionBinder.SyntheticBareProjectionAlias } projection])
        {
            return null;
        }

        while (sourceShaper is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
            sourceShaper = convert.Operand;

        return sourceShaper is ProjectionBindingExpression ? projection : null;
    }
```

Change the `TryBindAggregate` signature (add the last parameter and a `<param>` doc line):

```csharp
    internal static bool TryBindAggregate(
        MongoQueryExpression mongoQ,
        MongoAggregateOperator op,
        LambdaExpression? selector,
        LambdaExpression? predicate,
        Type resultType,
        MongoProjection? bareSourceProjection = null)
```

In the operand block, insert a new arm between the `isPostGroupBySelectlessMinMax` arm and the `else if (selector is null || !translator.TryTranslateValue(...))` arm:

```csharp
            else if (selector is null && bareSourceProjection is { } bareSource)
            {
                // Selector-less Sum()/Min()/Max()/Average() over a preceding bare scalar Select (EF's
                // Sum_with_no_arg family): reduce that Select's own $project output field. MongoSelectLowerer emits
                // the $project (its step 6) before this aggregate's $group (step 7), so "$_v" is in scope — the
                // same alias-reference technique the isPostGroupBySelectlessMinMax arm above uses.
                operand = new MongoElementRefExpression(bareSource.Alias, bareSource.Expression.Type);
            }
```

- [ ] **Step 4: Pass the bare projection from `BindAggregateOrFallback`**

In `MongoQueryableMethodTranslatingExpressionVisitor.BindAggregateOrFallback` replace the body with:

```csharp
        var mongoQ = (MongoQueryExpression)source.QueryExpression;
        var bareSource = selector is null && predicate is null
            ? NativeCardinalityBinder.TryGetBareServerValueProjection(mongoQ, source.ShaperExpression)
            : null;
        if (!NativeCardinalityBinder.TryBindAggregate(mongoQ, op, selector, predicate, resultType, bareSource))
            mongoQ.Select.MarkNotNativelyRepresentable();
        return ReshapeShaperExpression(source, resultType);
```

- [ ] **Step 5: Run the functional tests, verify they pass**

Run: build, then `dotnet test $FUNC -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeSelectorlessAggregateTests"` → all pass.

- [ ] **Step 6: Verify the 10 spec tests go native, then regenerate their baselines**

For each `M` in `Sum_with_no_arg Sum_with_no_arg_empty Min_with_no_arg Max_with_no_arg Average_with_no_arg Sum_with_binary_expression Average_with_binary_expression Sum_with_no_data_cast_to_nullable Min_with_non_matching_types_in_projection_introduces_explicit_cast Max_with_non_matching_types_in_projection_introduces_explicit_cast Average_with_non_matching_types_in_projection_doesnt_produce_second_explicit_cast`:

1. `MONGODB_EF_NATIVE_ONLY=1 dotnet test $SPEC ... --filter "FullyQualifiedName=$AGG.$M"`. It must fail **only** on `AssertMql` (a baseline mismatch), never with `NativeTranslationNotSupportedException`.
2. `EF_TEST_REWRITE_BASELINES=1 dotnet test $SPEC ... --filter "FullyQualifiedName=$AGG.$M"`.

Then `git diff` the spec file. Expected new shape, e.g. for `Sum_with_no_arg`: `Orders.{ "$project" : { "_v" : "$_id", "_id" : 0 } }, { "$group" : { "_id" : null, "v" : { "$sum" : "$_v" } } }`. Only these tests' `AssertMql` blocks may change. Rebuild and rerun all of them in **both** modes (with and without `MONGODB_EF_NATIVE_ONLY=1`) → green.

- [ ] **Step 7: Regression sweep**

Run the per-task sweep. Native-only failure count drops by 22 (11 methods × 2); no new failures (diff vs. baseline list).

- [ ] **Step 8: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeCardinalityBinder.cs \
  src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
  tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeSelectorlessAggregateTests.cs \
  tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindAggregateOperatorsQueryMongoTest.cs
git commit -m "EF-322: native selector-less Sum/Min/Max/Average over a bare scalar Select"
```


---

