### Task 5: Projection-binder wiring (`NativeProjectionBinder` + `MongoProjectionBindingExpressionVisitor`)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeProjectionBinder.cs:478-484` (the generic computed-leaf admission gate)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs:176-210` (add ONE case, for `ConditionalExpression` — a date-part member chain needs none, see Step 3)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeProjectionBinderConditionalAndDateTimeTests.cs`

**Interfaces:**
- Consumes: `MongoConditionalExpression`, `MongoDatePartExpression`, `MongoDateTimeOffsetLocalExpression`, `MongoElementRefExpression` (existing) from Tasks 1/3/4.
- Produces: `NativeProjectionBinder.TryPopulateNativeProjection` now admits a `ConditionalExpression` or date-member-chain projection leaf. `MongoProjectionBindingExpressionVisitor.Visit` now additionally registers a whole `ConditionalExpression` for the shaper (a date-member chain already rides its pre-existing bare-`MemberExpression` case).

- [ ] **Step 1: Write the failing test**

Follow the `NativeProjectionBinderEnumCastTests.cs` pattern (constructing a `MongoQueryExpression` over a `SingleEntityDbContext`, calling `NativeProjectionBinder.TryPopulateNativeProjection` directly, and asserting on `mongoQ.Select.Projection`/`mongoQ.Select.Route`):

```csharp
/* Copyright 2023-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

public class NativeProjectionBinderConditionalAndDateTimeTests
{
    private class Row
    {
        public ObjectId Id { get; set; }
        public bool Flag { get; set; }
        public DateTimeOffset? OccurredOffset { get; set; }
    }

    private class RowDto
    {
        public ObjectId Id { get; set; }
        public DateTime? DT { get; set; }
    }

    private static MongoQueryExpression TestQuery()
    {
        using var db = SingleEntityDbContext.Create<Row>();
        return new MongoQueryExpression(db.Model.FindEntityType(typeof(Row))!);
    }

    [Fact]
    public void Conditional_wrapped_projection_leaf_is_admitted()
    {
        var mongoQ = TestQuery();
        Expression<Func<Row, RowDto>> selector = r => new RowDto
        {
            Id = r.Id,
            DT = r.OccurredOffset == null ? (DateTime?)null : r.OccurredOffset.Value.DateTime.Date
        };

        Assert.True(NativeProjectionBinder.TryPopulateNativeProjection(mongoQ, selector));

        Assert.Equal(NativeRoute.Projection, mongoQ.Select.Route);
        var dtProjection = Assert.Single(mongoQ.Select.Projection, p => p.Alias == "DT");
        Assert.IsType<MongoConditionalExpression>(dtProjection.Expression);
    }

    [Fact]
    public void Conditional_bare_projection_leaf_is_admitted()
    {
        var mongoQ = TestQuery();
        Expression<Func<Row, DateTime?>> selector =
            r => r.OccurredOffset == null ? (DateTime?)null : r.OccurredOffset.Value.DateTime.Date;

        Assert.True(NativeProjectionBinder.TryPopulateNativeProjection(mongoQ, selector));

        Assert.Equal(NativeRoute.Projection, mongoQ.Select.Route);
        var projection = Assert.Single(mongoQ.Select.Projection);
        Assert.IsType<MongoConditionalExpression>(projection.Expression);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeProjectionBinderConditionalAndDateTimeTests"`
Expected: FAIL — `TryPopulateNativeProjection` returns `false` for both (the `TryTranslateLeaf` generic gate's node-kind allowlist doesn't include `MongoConditionalExpression`/`MongoDatePartExpression` yet).

- [ ] **Step 3: Write the implementation**

In `NativeProjectionBinder.cs`, widen the generic computed-leaf gate (currently at line 478-484):

```csharp
        if (translator.TryTranslateValue(leafExpression, out var value)
            && (value is MongoSizeExpression or MongoFilteredSizeExpression or MongoConvertExpression
                    or MongoConditionalExpression or MongoDatePartExpression or MongoDateTimeOffsetLocalExpression
                    or MongoElementRefExpression
                || (leafExpression is UnaryExpression { NodeType: ExpressionType.Convert } && value is MongoFieldExpression)))
        {
            result = value;
            return true;
        }
```

(`MongoConditionalExpression`, `MongoDatePartExpression`, `MongoDateTimeOffsetLocalExpression`, and `MongoElementRefExpression` all render as BSON *documents* or field-reference *strings*, never as a bare scalar `$project` could misread as an inclusion/exclusion flag — the same safety argument this gate's own comment already makes for `MongoSizeExpression`/`MongoConvertExpression`.)

**A date-time member chain needs NO new case here — verified, not assumed.** `MongoProjectionBindingExpressionVisitor.Visit`'s switch already has an unconditional `case MemberExpression memberExpression:` (line 155, no `when` guard, ordered BEFORE the arithmetic/cast cases) that registers the *whole* `MemberExpression` it's given — however deeply nested — as one opaque `_projectionMapping` entry and returns immediately:

```csharp
            case MemberExpression memberExpression:
                var currentProjectionMember = GetCurrentProjectionMember();
                _projectionMapping[currentProjectionMember] = memberExpression;

                return new ProjectionBindingExpression(_queryExpression, currentProjectionMember, expression.Type);
```

A date-member chain like `r.OccurredOffset.Value.DateTime.Date` is, from this visitor's point of view, just one `MemberExpression` (`.Date`, whose receiver happens to itself be nested member accesses) — so this pre-existing case already catches and correctly registers it whole, with no risk of the default walk visiting the receiver separately. The read side needs no change either: `MongoProjectionBindingRemovingExpressionVisitor.TryResolveFieldAccess` walks the receiver via `TryResolveFieldAccessSource`, which returns `(EntityType: null, DocumentExpression: null)` for a receiver that is itself a nested `MemberExpression` (not one of that method's recognized root shapes — `EntityTypedExpression`/`RootReferenceExpression`/`ObjectAccessExpression`/`StructuralTypeShaperExpression`/`ParameterExpression`) — so `fieldAccess.Property` comes back `null`, and `VisitExtension`'s `ProjectionBindingExpression` case falls through to its existing generic raw-alias read (`BsonBinding.CreateGetElementValue(DocParameter, projection.Alias, projectionBindingExpression.Type)`), the exact same path an arithmetic/cast leaf already uses. **Do not add a `MemberExpression` case for this** — it would be unreachable dead code shadowed by the pre-existing catch-all at line 155.

`ConditionalExpression` is NOT a `MemberExpression`, so it is NOT caught by that case — without a dedicated arm it falls through to `default: return base.Visit(expression);`, which recurses into `Test`/`IfTrue`/`IfFalse` independently, the same wrong-data hazard the arithmetic case's own comment describes. Add ONE new case to the `Visit` switch, alongside the existing arithmetic (`BinaryExpression`) and cast (`UnaryExpression{Convert}`) cases (after the cast case, before the `MethodCallExpression methodCallExpression` case around line 212):

```csharp
            // Native conditional projection leaf: register the WHOLE ConditionalExpression node as ONE
            // projection member, exactly like the arithmetic and cast cases above (NOT caught by the earlier
            // unconditional `case MemberExpression memberExpression:` — a ConditionalExpression is not a
            // MemberExpression). Without this, the default recursive walk would visit Test/IfTrue/IfFalse
            // independently, writing the SAME ProjectionMember slot three times and silently producing wrong
            // data. The Route == Projection guard is load-bearing for the same reason as the arithmetic case's:
            // it confines this mapping to a projection NativeProjectionBinder already accepted in full, so a
            // mixed/fallback shape falls through to the ordinary default walk untouched.
            case ConditionalExpression when _queryExpression.Select.Route == NativeRoute.Projection:
                var conditionalMember = GetCurrentProjectionMember();
                _projectionMapping[conditionalMember] = expression;
                return new ProjectionBindingExpression(_queryExpression, conditionalMember, expression.Type);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeProjectionBinderConditionalAndDateTimeTests"`
Expected: PASS, both tests.

- [ ] **Step 5: Run the full unit test suite to check for regressions**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"`
Expected: PASS, zero regressions.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeProjectionBinder.cs \
        src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeProjectionBinderConditionalAndDateTimeTests.cs
git commit -m "Wire conditional and date-part projection leaves into NativeProjectionBinder and the shaper visitor"
```

---

