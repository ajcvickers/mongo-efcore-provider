### Task 5: Reference-type upcast around a DTO projection

Closes gap **C** (1 test, EF9+).

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeProjectionBinder.cs` (top of `TryPopulateNativeProjection`, line ~44)
- Create: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeProjectionBinderUpcastTests.cs`
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeUpcastProjectionTests.cs`
- Modify (baselines): `NorthwindAggregateOperatorsQueryMongoTest.cs`

- [ ] **Step 1: Failing unit test** (pattern from `NativeProjectionBinderStringSequenceTests`; header/BOM from that file):

```csharp
using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

public class NativeProjectionBinderUpcastTests
{
    private class Customer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
    }

    private class IdDto { public string Name { get; set; } = ""; }
    private class IdAndCityDto : IdDto { public string City { get; set; } = ""; }

    private static MongoQueryExpression TestQuery()
    {
        using var db = SingleEntityDbContext.Create<Customer>();
        return new MongoQueryExpression(db.Model.FindEntityType(typeof(Customer))!);
    }

    [Fact]
    public void Upcast_member_init_projection_binds_the_construction()
    {
        var mongoQ = TestQuery();
        Expression<Func<Customer, IdDto>> selector = c => (IdDto)new IdAndCityDto { Name = c.Name, City = c.City };
        Assert.Equal(ExpressionType.Convert, selector.Body.NodeType); // guard: the compiler kept the Convert

        Assert.True(NativeProjectionBinder.TryPopulateNativeProjection(mongoQ, selector));
        Assert.Equal(NativeRoute.Projection, mongoQ.Select.Route);
        Assert.Contains(mongoQ.Select.Projection, p => p.Alias == "Name");
        Assert.Contains(mongoQ.Select.Projection, p => p.Alias == "City");
    }

    [Fact]
    public void Value_type_conversion_of_a_construction_is_not_unwrapped()
    {
        var mongoQ = TestQuery();
        Expression<Func<Customer, object>> selector = c => (object)new ValueTuple<string>(c.Name);
        // Boxing a struct is not a reference upcast; this test pins that the new arm does not fire for it.
        NativeProjectionBinder.TryPopulateNativeProjection(mongoQ, selector);
        Assert.DoesNotContain(mongoQ.Select.Projection, p => p.Alias == "Item1");
    }
}
```

If `Assert.Equal(ExpressionType.Convert, …)` fails because the C# compiler elides the upcast in the lambda, build the selector by hand instead: `Expression.Lambda<Func<Customer, IdDto>>(Expression.Convert(memberInit, typeof(IdDto)), c)`.

- [ ] **Step 2: Failing functional test** `NativeUpcastProjectionTests.cs`. Copy the `Row`/`Seed`/`CreateContext` pattern from `NativeSelectorlessAggregateTests`, with `Row { ObjectId Id; string Name; string City }`, seeded ("a","London"), ("b","Berlin"):

```csharp
    public class IdDto { public string Name { get; set; } = ""; }
    public class IdAndCityDto : IdDto { public string City { get; set; } = ""; }

    [Fact]
    public void First_over_covariant_dto_query_goes_native()
    {
        var collection = Seed(nameof(First_over_covariant_dto_query_goes_native));
        var result = NativeModeAssert.NativeAndParity(mode =>
        {
            using var db = CreateContext(collection, mode);
            IQueryable<IdDto> query = db.Entities.Where(r => r.Name == "b")
                .Select(r => new IdAndCityDto { Name = r.Name, City = r.City });
            var first = query.First();
            return [(first.Name, ((IdAndCityDto)first).City)];
        });
        Assert.Equal([("b", "Berlin")], result);
    }

    [Fact]
    public void Last_over_covariant_dto_query_goes_native()
    {
        var collection = Seed(nameof(Last_over_covariant_dto_query_goes_native));
        var result = NativeModeAssert.NativeAndParity(mode =>
        {
            using var db = CreateContext(collection, mode);
            IQueryable<IdDto> query = db.Entities.OrderBy(r => r.Name)
                .Select(r => new IdAndCityDto { Name = r.Name, City = r.City });
            return [query.Last().Name];
        });
        Assert.Equal(["b"], result);
    }
```

- [ ] **Step 3: Run → fail** (the unit test on `Assert.True`; the functional tests with `NativeTranslationNotSupportedException`).

- [ ] **Step 4: Implement.** At the very top of `NativeProjectionBinder.TryPopulateNativeProjection`, before the existing re-entrancy guard:

```csharp
        // A reference-type upcast wrapped around a constructed DTO — `x => (CustomerIdDto)new CustomerIdAndCityDto
        // { ... }` — which EF's nav-expansion produces when a covariant IQueryable<Derived> is consumed as
        // IQueryable<Base> (EF's Return_type_of_singular_operator_is_preserved). The upcast changes nothing
        // server-side, so bind the construction itself; the Convert stays on the shaper, which TranslateSelect
        // builds from its own (unmodified) selector.
        if (selector.Body is UnaryExpression { NodeType: ExpressionType.Convert, Operand: NewExpression or MemberInitExpression } upcast
            && !upcast.Type.IsValueType
            && !upcast.Operand.Type.IsValueType
            && upcast.Type.IsAssignableFrom(upcast.Operand.Type))
        {
            selector = Expression.Lambda(upcast.Operand, selector.Parameters);
        }
```

- [ ] **Step 5: Run unit + functional → pass.** If the unit test passes but the functional test returns empty or wrong members, then the shaper side (`MongoProjectionBindingExpressionVisitor`) keys the construction's members differently when they sit under a `Convert`. In that case, look at how its `Visit` handles `UnaryExpression Convert` over `MemberInit`, and make it bind the operand's members under the same `ProjectionMember`s the binder aliased. Fix it there, and add a unit test for it next to this one.

- [ ] **Step 6: Spec test** `Return_type_of_singular_operator_is_preserved` (EF10): native-only → regenerate (6 queries; the rewriter truncates at 9) → both modes green.

- [ ] **Step 7: Regression sweep** (−2). **Commit** — `git commit -m "EF-322: native projection through a reference-type upcast of a DTO"`

---

