### Task 3: Tuple needle in `Contains`

Closes gap **B3** (1 test).

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs` (the `TryMatchContainsMethod` arm, ~lines 1020–1060)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs` (`RenderInValues`, ~line 629)
- Modify: `tests/.../FunctionalTests/Query/NativeLocalCollectionContainsTests.cs` (append)
- Modify (baselines): `NorthwindAggregateOperatorsQueryMongoTest.cs`

**Interfaces:**
- Consumes: `NativeLocalCollectionContainsTests.Names(...)` from Task 2. Existing private members of `MongoExpressionTranslator`: `IsTupleType(Type)`, `TryDecomposeTupleOperand(Expression, out MongoExpression[])` (TupleEquality.cs), `static AllFieldsDefaultSerialized(MongoExpression)`, `static Unwrap(Expression)`, `static TranslateInValuesRaw(Expression, Type)`.

- [ ] **Step 1: Failing functional tests** (append to `NativeLocalCollectionContainsTests`):

```csharp
    [Fact]
    public void Tuple_parameter_array_contains_constructed_tuple_goes_native()
    {
        var pairs = new[] { Tuple.Create(1, 2), Tuple.Create(3, 4) };
        Assert.Equal(["a", "b"], Names(nameof(Tuple_parameter_array_contains_constructed_tuple_goes_native),
            q => q.Where(r => pairs.Contains(new Tuple<int, int>(r.A, r.B)))));
    }

    [Fact]
    public void Tuple_contains_is_positional_not_set_membership()
    {
        // (4, 1) must NOT match row c = (A: 1, B: 4): tuple equality is positional.
        var pairs = new[] { Tuple.Create(4, 1) };
        Assert.Empty(Names(nameof(Tuple_contains_is_positional_not_set_membership),
            q => q.Where(r => pairs.Contains(Tuple.Create(r.A, r.B)))));
    }
```

- [ ] **Step 2: Run → both fail** with `NativeTranslationNotSupportedException`.

- [ ] **Step 3: Implement the needle arm.** In `MongoExpressionTranslator.cs`, inside the `case MethodCallExpression call when TryMatchContainsMethod(...)` arm, directly **before** the `if (TryTranslateValue(itemExpr, out var needleNode) ...` computed-needle block:

```csharp
                // A constructed System.Tuple/ValueTuple needle over entity fields — `ids.Contains(new Tuple<int,
                // int>(o.OrderID, o.ProductID))` (EF's Contains_with_local_tuple_array_closure) or the Tuple.Create
                // spelling. The positional sibling of the anonymous-type needle below, rendered as an MQL array exactly
                // like the tuple-equality operand (MongoExpressionTranslator.TupleEquality.cs). Only a query-PARAMETER
                // haystack is admitted: its elements serialize through the driver's own tuple serializer (one BSON
                // array per tuple — see MongoAggregationExpressionRenderer.RenderInValues), whereas a constant haystack
                // would render through BsonValue.Create, which cannot serialize a tuple.
                if (IsTupleType(itemExpr.Type)
                    && TryDecomposeTupleOperand(Unwrap(itemExpr), out var tupleElements)
                    && new MongoTupleExpression(tupleElements) is var tupleNeedle
                    && AllFieldsDefaultSerialized(tupleNeedle)
                    && MongoAggregationExpressionRenderer.CanRender(tupleNeedle))
                {
                    return TranslateInValuesRaw(collectionExpr, itemExpr.Type) is MongoParameterExpression tupleValues
                        ? new MongoComputedInExpression(tupleNeedle, tupleValues, negated: false)
                        : null;
                }
```

- [ ] **Step 4: Serialize tuple haystack elements as arrays.** In `MongoAggregationExpressionRenderer.RenderInValues`, parameter case, replace the `elementSerializer` initializer (add `using System.Runtime.CompilerServices;` and `using MongoDB.Bson.Serialization;` if absent):

```csharp
                var elementSerializer = parameter.ForSerialization is not null
                    ? BsonSerializerFactory.GetPropertySerializationInfo(parameter.ForSerialization).Serializer
                    : parameter.RawElementType is { } rawElementType
                        ? typeof(ITuple).IsAssignableFrom(rawElementType)
                            // The driver's own Tuple/ValueTuple serializers write one BSON array per tuple, matching how
                            // a MongoTupleExpression needle renders; BsonSerializerFactory.CreateTypeSerializer would fall
                            // through to a class-map (document) serializer for a tuple type.
                            ? BsonSerializer.LookupSerializer(rawElementType)
                            : BsonSerializerFactory.CreateTypeSerializer(rawElementType)
                        : StringSerializer.Instance;
```

- [ ] **Step 5: Run the functional tests → pass.** If they fail with zero rows, capture the MQL (see the `SpyLoggerProvider` usage in `NativeJoinTests`) and check that the haystack renders as `[[1, 2], [3, 4]]`, not `[{ "Item1": 1, … }]`.

- [ ] **Step 6: Spec test** `Contains_with_local_tuple_array_closure`: native-only → regenerate → both modes green. Expected MQL: `OrderDetails.{ "$match" : { "$expr" : { "$in" : [["$_id.OrderID", "$_id.ProductID"], [[1, 2], [10248, 11]]] } } }` (same as the driver's).

- [ ] **Step 7: Regression sweep** (−2). Also run `dotnet test $UNIT --no-build -c "Debug EF10" --filter "FullyQualifiedName~MongoExpressionNodeCoverageTests|FullyQualifiedName~MongoAggregationExpressionRenderer"`.

- [ ] **Step 8: Commit** — `git commit -m "EF-322: native tuple needle in local-collection Contains"`

---

