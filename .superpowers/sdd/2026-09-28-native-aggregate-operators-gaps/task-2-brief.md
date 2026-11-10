### Task 2: `Contains` over set interfaces and an empty inline list

Closes gaps **B1** and **B2** (3 tests).

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.MethodCalls.cs` (`TryMatchContainsMethod` ~line 376–390; `TranslateInValues` ~line 514)
- Modify: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoExpressionTranslatorTests.cs` (append tests)
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeLocalCollectionContainsTests.cs`
- Modify (baselines): `NorthwindAggregateOperatorsQueryMongoTest.cs`

**Interfaces:**
- Produces: the functional test class `NativeLocalCollectionContainsTests`, with helpers `Seed(string name)` and `Names(string name, Func<IQueryable<Row>, IQueryable<Row>> query)`. Tasks 3 and 4 append to it.

- [ ] **Step 1: Write failing unit tests** (append to `MongoExpressionTranslatorTests`, next to "Test 15"; add `using System.Collections.Immutable;` if absent)

```csharp
    [Theory]
    [InlineData(typeof(ISet<int>))]
    [InlineData(typeof(IReadOnlySet<int>))]
    [InlineData(typeof(IImmutableSet<int>))]
    public void Instance_set_interface_contains_is_matched(Type setInterface)
    {
        var set = Expression.Parameter(setInterface, "set");
        var item = Expression.Parameter(typeof(int), "item");
        var call = Expression.Call(set, setInterface.GetMethod("Contains", [typeof(int)])!, item);

        Assert.True(MongoExpressionTranslator.TryMatchContainsMethod(call, out var collection, out var matchedItem));
        Assert.Same(set, collection);
        Assert.Same(item, matchedItem);
    }

    [Fact]
    public void Negated_contains_over_empty_inline_list_translates_to_negated_in()
    {
        var translator = NewTranslator(GetEntityType<Customer>());
        var body = PredicateBody<Customer>(c => !new List<int>().Contains(c.Age));

        Assert.True(translator.TryTranslate(body, out var result));
        // Must be an EMPTY haystack; negation handled by the negator/renderer (renders $nin: []).
        var rendered = result!.ToString();
        Assert.NotNull(rendered);
    }
```

The second test's final assertion is weak on purpose: the exact node the negation produces (`MongoInExpression { Negated: true }` vs. a `MongoUnaryExpression` wrapper) is decided by the existing negator. Before the change, `TryTranslate` returns `false`, so the `Assert.True` is the real gate. After making it pass, replace the last two lines with an assertion on the concrete node you observe (e.g. `var inExpr = Assert.IsType<MongoInExpression>(result); Assert.True(inExpr.Negated); Assert.Empty((System.Collections.IEnumerable)((MongoConstantExpression)inExpr.Values).Value!);`). Adjust it to the real shape, then keep it.

- [ ] **Step 2: Write failing functional tests.** Create `NativeLocalCollectionContainsTests.cs` (header/BOM from `NativeStringSequenceProjectionTests.cs`):

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// <c>Where(e =&gt; localCollection.Contains(e.Member))</c> shapes EF's Northwind aggregate-operator suite exercises
/// beyond a plain List/HashSet/array: set-interface-typed parameters, an empty inline list, a tuple needle, and a
/// closed <c>Enumerable.Where</c> filter applied to the local collection.
/// </summary>
[XUnitCollection("QueryTests")]
public class NativeLocalCollectionContainsTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Row
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public int A { get; set; }
        public int B { get; set; }
    }

    private IMongoCollection<Row> Seed(string name)
    {
        var collection = database.MongoDatabase.GetCollection<Row>(
            TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8]);
        collection.InsertMany(
        [
            new Row { Id = ObjectId.GenerateNewId(), Name = "a", A = 1, B = 2 },
            new Row { Id = ObjectId.GenerateNewId(), Name = "b", A = 3, B = 4 },
            new Row { Id = ObjectId.GenerateNewId(), Name = "c", A = 1, B = 4 },
        ]);
        return collection;
    }

    private static SingleEntityDbContext<Row> CreateContext(IMongoCollection<Row> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(collection, optionsBuilderAction: b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    /// <summary>Runs <paramref name="query"/> natively and against DriverLinq; returns the sorted Names.</summary>
    private List<string> Names(string name, Func<IQueryable<Row>, IQueryable<Row>> query)
    {
        var collection = Seed(name);
        return NativeModeAssert.NativeAndParity(mode =>
        {
            using var db = CreateContext(collection, mode);
            return query(db.Entities).Select(r => r.Name).AsEnumerable().Order().ToList();
        });
    }

    [Fact]
    public void IReadOnlySet_parameter_contains_goes_native()
    {
        IReadOnlySet<string> names = new HashSet<string> { "a", "c" };
        Assert.Equal(["a", "c"], Names(nameof(IReadOnlySet_parameter_contains_goes_native), q => q.Where(r => names.Contains(r.Name))));
    }

    [Fact]
    public void IImmutableSet_parameter_contains_goes_native()
    {
        IImmutableSet<string> names = ImmutableHashSet<string>.Empty.Add("b");
        Assert.Equal(["b"], Names(nameof(IImmutableSet_parameter_contains_goes_native), q => q.Where(r => names.Contains(r.Name))));
    }

    [Fact]
    public void ISet_parameter_contains_goes_native()
    {
        ISet<string> names = new HashSet<string> { "a" };
        Assert.Equal(["a"], Names(nameof(ISet_parameter_contains_goes_native), q => q.Where(r => names.Contains(r.Name))));
    }

    [Fact]
    public void Negated_contains_over_empty_inline_list_returns_every_row_natively()
        => Assert.Equal(["a", "b", "c"], Names(nameof(Negated_contains_over_empty_inline_list_returns_every_row_natively),
            q => q.Where(r => !new List<string>().Contains(r.Name))));

    [Fact]
    public void Contains_over_empty_inline_list_returns_no_rows_natively()
        => Assert.Empty(Names(nameof(Contains_over_empty_inline_list_returns_no_rows_natively),
            q => q.Where(r => new List<string>().Contains(r.Name))));
}
```

- [ ] **Step 3: Run to verify failures**

Build, then run `dotnet test $UNIT -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoExpressionTranslatorTests"` and `dotnet test $FUNC -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeLocalCollectionContainsTests"`. The new tests fail: the unit tests on `Assert.True`, the functional tests with `NativeTranslationNotSupportedException`. (`ISet` may already fail the same way.)

- [ ] **Step 4: Implement**

In `TryMatchContainsMethod`, add `using System.Collections.Immutable;` and widen the whitelist (and update the method's `<summary>` list):

```csharp
                if (def == typeof(List<>) || def == typeof(HashSet<>) || def == typeof(IList<>) || def == typeof(ICollection<>)
                    || def == typeof(ISet<>) || def == typeof(IReadOnlySet<>) || def == typeof(IImmutableSet<>))
```

Keep it an explicit whitelist. Do **not** generalize to "any type implementing `IEnumerable<T>`": that would capture `string.Contains(char)`.

In `TranslateInValues`, directly after the `if (unwrapped is ConstantExpression { Value: System.Collections.IEnumerable } constant)` line:

```csharp
        // `new List<string>()` inline (EF's Contains_with_local_collection_empty_inline): EF's parameter extraction
        // leaves an argument-less collection construction as a bare NewExpression rather than folding it to a
        // constant. It is always empty, so it is an empty $in/$nin haystack.
        if (unwrapped is NewExpression { Arguments.Count: 0 } emptyCollection
            && typeof(System.Collections.IEnumerable).IsAssignableFrom(emptyCollection.Type))
        {
            return new MongoConstantExpression(Array.CreateInstance(elementType, 0), property);
        }
```

- [ ] **Step 5: Run unit + functional tests → pass.** Tighten the empty-list unit assertion as described in Step 1.

- [ ] **Step 6: Spec tests.** For `IReadOnlySet_Contains_with_parameter IImmutableSet_Contains_with_parameter Contains_with_local_collection_empty_inline`: run native-only (they should pass outright, or fail only on `AssertMql`), regenerate if needed, `git diff`, rebuild, and rerun in both modes → green.

- [ ] **Step 7: Regression sweep** (native-only failures −6 vs. the previous task; no new failures). Also run `dotnet test $UNIT -c "Debug EF10" --no-build` in full, because `TryMatchContainsMethod` has four callers (`EntityEquality.cs:288`, `MongoExpressionTranslator.cs:997,1020,2157`, `NativeJoinScopeTranslator.cs:373`).

- [ ] **Step 8: Commit** — `git commit -m "EF-322: native Contains over ISet/IReadOnlySet/IImmutableSet and an empty inline list"`

---

