### Task 6: `string.FirstOrDefault()` as a client-applied string leaf

Closes gap **D** (1 test).

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeProjectionBinder.cs` (`IsStringSequenceMaterializationCall`, line ~580, and its doc comment)
- Modify: `tests/.../UnitTests/Query/NativeTranslation/NativeProjectionBinderStringSequenceTests.cs` (append)
- Modify: `tests/.../FunctionalTests/Query/NativeStringSequenceProjectionTests.cs` (append)
- Modify (baselines): `NorthwindAggregateOperatorsQueryMongoTest.cs`

Rationale: the read side of this leaf (`MongoProjectionBindingRemovingExpressionVisitor` ~line 258 and `MongoMixedProjectionBindingRemovingExpressionVisitor.TryBindStringSequenceLeaf`) rebuilds `Expression.Call(call.Method, rawStringRead)`. That works for any single-argument `Enumerable` call on a `string`, so a scalar `char` result needs no reader changes. MongoDB has no char type (the driver serializes `char` as Int32), so pushing down the raw string and applying `FirstOrDefault` on the client is the right native form. Today's driver path does full client evaluation (the baseline pipeline is empty), so this is a strict improvement.

- [ ] **Step 1: Failing unit test** (append to `NativeProjectionBinderStringSequenceTests`):

```csharp
    [Fact]
    public void Bare_FirstOrDefault_over_string_leaf_is_admitted_as_a_bare_field_leaf()
    {
        var mongoQ = TestQuery();
        Expression<Func<Customer, char>> selector = c => c.City.FirstOrDefault();

        Assert.True(NativeProjectionBinder.TryPopulateNativeProjection(mongoQ, selector));

        Assert.Equal(NativeRoute.Projection, mongoQ.Select.Route);
        var projection = Assert.Single(mongoQ.Select.Projection);
        Assert.Equal("City", Assert.IsType<MongoFieldExpression>(projection.Expression).ElementName);
        Assert.True(mongoQ.Select.HasStringSequenceProjectionLeaf);
    }
```

(Add `using System.Linq;` if the file lacks it.)

- [ ] **Step 2: Failing functional test** (append to `NativeStringSequenceProjectionTests`; it uses the file's own `Seed`, `CreateContext` and `AllModes`):

```csharp
    [Fact]
    public void FirstOrDefault_over_string_projection_goes_native_in_every_mode()
    {
        var collection = Seed(nameof(FirstOrDefault_over_string_projection_goes_native_in_every_mode));
        collection.InsertOne(new Row { Id = ObjectId.GenerateNewId(), Label = "c", City = "" });

        foreach (var mode in AllModes)
        {
            using var db = CreateContext(collection, mode);
            var bare = db.Entities.AsNoTracking().OrderBy(x => x.Label).Select(x => x.City.FirstOrDefault()).ToList();
            Assert.Equal(['L', 'B', '\0'], bare);

            var wrapped = db.Entities.AsNoTracking().OrderBy(x => x.Label)
                .Select(x => new { x.Label, Initial = x.City.FirstOrDefault() }).ToList();
            Assert.Equal([('a', 'L'), ('b', 'B'), ('c', '\0')], wrapped.Select(r => (r.Label[0], r.Initial)));
        }
    }
```

- [ ] **Step 3: Run → fail** (the unit test on `Assert.True`; the functional test under `NativeOnly`).

- [ ] **Step 4: Implement.** Extend `IsStringSequenceMaterializationCall`:

```csharp
    internal static bool IsStringSequenceMaterializationCall(MethodCallExpression call)
        => call is { Method.IsGenericMethod: true, Arguments: [var source] }
           && source.Type == typeof(string)
           && call.Method.GetGenericMethodDefinition() is var definition
           && (definition == EnumerableMethods.AsEnumerable
               || definition == EnumerableMethods.ToList
               || definition == EnumerableMethods.ToArray
               || definition == EnumerableMethods.FirstOrDefaultWithoutPredicate);
```

Update its `<summary>`: add that `FirstOrDefault()` (EF's `String_FirstOrDefault_in_projection_does_not_do_client_eval`) is admitted for the same reason. It needs only the raw string server-side, and the shaper re-applies the call, producing a scalar `char` rather than a sequence.

- [ ] **Step 5: Run → pass.** Run the whole `NativeStringSequenceProjectionTests` and `NativeProjectionBinderStringSequenceTests` classes: all modes must stay green.

- [ ] **Step 6: Spec test** `String_FirstOrDefault_in_projection_does_not_do_client_eval`: native-only → regenerate. Expected `Customers.{ "$project" : { "_v" : "$_id", "_id" : 0 } }`, or whatever the bare-field projection renders, instead of `Customers.`. Both modes green.

- [ ] **Step 7: Regression sweep** (−2). **Commit** — `git commit -m "EF-322: native string.FirstOrDefault() projection leaf"`

---

