### Task 8: `Queryable.Contains` as a terminal operator

Closes gap **F** (3 tests).

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeCardinalityBinder.cs` (new `TryBindContains` + private `TryTranslateContainsItem`)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` (`TranslateContains`, ~line 2453)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs` (`IsNativeRepresentableSlotOperator`, ~line 717)
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeContainsTerminalTests.cs`
- Modify (baselines): `NorthwindAggregateOperatorsQueryMongoTest.cs`

**Interfaces:**
- Consumes: `NativeCardinalityBinder.TryGetBareServerValueProjection` (Task 1); `TryBindAggregate(mongoQ, MongoAggregateOperator.Any, selector: null, predicate, typeof(bool))` (existing Any+predicate arm); `MongoExpressionTranslator.Unwrap` (internal static); `NativeQueryParameter.TryGetQueryParameterName`. Entity equality against a constant or query-parameter entity is `MongoExpressionTranslator.EntityEquality.cs`; `e == null` is `TranslateComparisonCore`'s `$$ROOT`-vs-null arm.
- Produces: `internal static bool NativeCardinalityBinder.TryBindContains(MongoQueryExpression mongoQ, Expression sourceShaper, Expression item)`.

Semantics: `source.Contains(item)` ≡ `source.Any(x => x == item)`.
- **Scalar source** (`Select(c => c.CustomerID).Contains("ALFKI")`): the bare projection must be a plain `MongoFieldExpression` on a query with **no joins** (a joined field would be matched before its `$lookup`). The conjunct is `field == item`, appended to `PipelineOps` *after* any recorded paging, so `Take(1).Contains(v)` tests only the paged rows.
- **Entity source** (`Where(...).Contains(entity)` / `.Contains(null)`): `Any(e => e == item)` through the existing entity-equality translation. Excludes a client-wrapped whole-entity shaper.
- Anything else declines.

- [ ] **Step 1: Failing functional tests.** Create `NativeContainsTerminalTests.cs`, reusing the `Row`/`Seed`/`CreateContext` pattern from `NativeSelectorlessAggregateTests`, with `Row { ObjectId Id; string Name; int Value }` seeded ("a",1), ("b",2), ("c",3):

```csharp
    private List<bool> Run(string name, Func<SingleEntityDbContext<Row>, bool> query)
    {
        var collection = Seed(name);
        return NativeModeAssert.NativeAndParity(mode =>
        {
            using var db = CreateContext(collection, mode);
            return [query(db)];
        });
    }

    [Fact]
    public void Scalar_select_contains_constant_goes_native()
    {
        Assert.Equal([true], Run(nameof(Scalar_select_contains_constant_goes_native) + "T", db => db.Entities.Select(r => r.Name).Contains("b")));
        Assert.Equal([false], Run(nameof(Scalar_select_contains_constant_goes_native) + "F", db => db.Entities.Select(r => r.Name).Contains("zzz")));
    }

    [Fact]
    public void Scalar_select_contains_parameter_goes_native()
    {
        var wanted = "c";
        Assert.Equal([true], Run(nameof(Scalar_select_contains_parameter_goes_native), db => db.Entities.Select(r => r.Name).Contains(wanted)));
    }

    // Review Focus 3.
    [Fact]
    public void Take_before_Contains_only_tests_the_paged_rows()
        => Assert.Equal([false], Run(nameof(Take_before_Contains_only_tests_the_paged_rows),
            db => db.Entities.OrderBy(r => r.Value).Select(r => r.Name).Take(1).Contains("b")));

    [Fact]
    public void Entity_contains_goes_native()
    {
        var collection = Seed(nameof(Entity_contains_goes_native));
        var result = NativeModeAssert.NativeAndParity(mode =>
        {
            using var db = CreateContext(collection, mode);
            var b = db.Entities.Single(r => r.Name == "b");
            return [db.Entities.Where(r => r.Value >= 2).Contains(b), db.Entities.Where(r => r.Value >= 3).Contains(b)];
        });
        Assert.Equal([true, false], result);
    }

    [Fact]
    public void Entity_contains_null_is_false_natively()
        => Assert.Equal([false], Run(nameof(Entity_contains_null_is_false_natively), db => db.Entities.Where(r => r.Value > 0).Contains(null!)));

    // Review Focus 5 — a computed projection has no single backing field to match against: decline, don't guess.
    [Fact]
    public void Contains_over_computed_projection_declines_cleanly()
    {
        var collection = Seed(nameof(Contains_over_computed_projection_declines_cleanly));
        var result = NativeModeAssert.DeclinesCleanly(mode =>
        {
            using var db = CreateContext(collection, mode);
            return [db.Entities.Select(r => r.Name + "!").Contains("b!")];
        });
        Assert.Equal([true], result);
    }
```

If `Contains_over_computed_projection_declines_cleanly` can't run because DriverLinq itself fails for the shape, replace it with an `Assert.Throws<NativeTranslationNotSupportedException>` under `NativeOnly` only, and note that in the commit message.

- [ ] **Step 2: Run → the native tests fail** with `NativeTranslationNotSupportedException` ("Query projects a non-entity result"). The decline test may already pass.

- [ ] **Step 3: Implement `TryBindContains`** in `NativeCardinalityBinder` (add `using System.Diagnostics.CodeAnalysis;` and `using Microsoft.EntityFrameworkCore.Metadata;` if absent):

```csharp
    /// <summary>
    /// Binds a terminal <c>source.Contains(item)</c> as the equivalent <c>source.Any(x =&gt; x == item)</c>:
    /// over a bare scalar-field <c>Select</c> (<c>Select(c =&gt; c.CustomerID).Contains("ALFKI")</c>) as an equality
    /// <c>$match</c> on that field, or over a whole-entity source (<c>Where(...).Contains(order)</c>,
    /// <c>.Contains(null)</c>) via the ordinary entity-equality translation. Returns <see langword="false"/> for any
    /// other shape (a computed or joined projection, a client-wrapped shaper), so the caller marks the query non-native.
    /// </summary>
    internal static bool TryBindContains(MongoQueryExpression mongoQ, Expression sourceShaper, Expression item)
    {
        var select = mongoQ.Select;

        if (TryGetBareServerValueProjection(mongoQ, sourceShaper) is { } bareSource)
        {
            // Joins.Count == 0: a joined field (`_lookup_X.City`) would be matched in PipelineOps, BEFORE its $lookup.
            if (mongoQ.Joins.Count != 0
                || bareSource.Expression is not MongoFieldExpression field
                || !TryTranslateContainsItem(item, field.Property, out var itemNode))
            {
                return false;
            }

            if (!TryBindAggregate(mongoQ, MongoAggregateOperator.Any, selector: null, predicate: null, typeof(bool)))
                return false;

            // Committed only after TryBindAggregate succeeded (no mutate-then-decline). AddPredicateConjunct appends
            // at the TAIL of the op list, so a Take/Skip recorded earlier still runs first.
            select.AddPredicateConjunct(new MongoBinaryExpression(MongoBinaryOperator.Equal, field, itemNode));
            return true;
        }

        var elementType = sourceShaper.Type;
        if (select.Route != NativeRoute.WholeEntity
            || select.HasClientWrappedWholeEntityShaper
            || !mongoQ.CollectionExpression.EntityType.ClrType.IsAssignableFrom(elementType)
            || !elementType.IsAssignableFrom(item.Type))
        {
            return false;
        }

        var element = Expression.Parameter(elementType, "e");
        var comparand = item.Type == elementType ? item : Expression.Convert(item, elementType);
        var predicate = Expression.Lambda(Expression.Equal(element, comparand), element);
        return TryBindAggregate(mongoQ, MongoAggregateOperator.Any, selector: null, predicate, typeof(bool));
    }

    private static bool TryTranslateContainsItem(
        Expression item, IProperty property, [NotNullWhen(true)] out MongoExpression? node)
    {
        var unwrapped = MongoExpressionTranslator.Unwrap(item);
        node = unwrapped switch
        {
            ConstantExpression constant => new MongoConstantExpression(constant.Value, property),
            _ when NativeQueryParameter.TryGetQueryParameterName(unwrapped, out var name)
                => new MongoParameterExpression(name, property),
            _ => null
        };
        return node != null;
    }
```

If `Expression.Equal(element, comparand)` throws for an entity type without an `==` operator, it won't: reference types fall back to reference equality in expression trees. If the entity type is a struct it would, but EF entity types can't be structs.

- [ ] **Step 4: Wire it up.** In the QMTEV:

```csharp
    protected override ShapedQueryExpression TranslateContains(ShapedQueryExpression source, Expression item)
    {
        var mongoQ = (MongoQueryExpression)source.QueryExpression;
        if (!NativeCardinalityBinder.TryBindContains(mongoQ, source.ShaperExpression, item))
            mongoQ.Select.MarkNotNativelyRepresentable();
        return ReshapeShaperExpression(source, typeof(bool));
    }
```

In `NativeSlotPopulator.IsNativeRepresentableSlotOperator` add `|| methodDefinition == QueryableMethods.Contains` next to `AnyWithoutPredicate`. Both edits are required: `Query/AGENTS.md` says "a `Translate*`-override operator needs both the override and `IsNativeRepresentableSlotOperator`".

- [ ] **Step 5: Run → pass.** Then run `NativeCardinalityTests`, `NativeEntityEqualityTests` and `NativeGateRoutingTests` → green.

- [ ] **Step 6: Spec tests** `Contains_top_level Contains_over_entityType_should_rewrite_to_identity_equality Contains_over_entityType_with_null_should_rewrite_to_false`: native-only → regenerate → both modes green. Expected shapes: `Customers.{ "$match" : { "_id" : "ALFKI" } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }, { "$limit" : 1 }` for `Contains_top_level`, and a `$$ROOT`-vs-null `$expr` match for the null case. Accept whatever the renderer produces, as long as the results are correct.

- [ ] **Step 7: Regression sweep** (−6). The target class must now be **0 failures** under `MONGODB_EF_NATIVE_ONLY=1` on EF10.

- [ ] **Step 8: Commit** — `git commit -m "EF-322: native Queryable.Contains terminal operator"`

---

