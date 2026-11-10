### Task 1: New IR node types

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoConditionalExpression.cs`
- Create: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoDatePartExpression.cs`
- Create: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoDateTimeOffsetLocalExpression.cs`
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoConditionalAndDatePartExpressionTests.cs`

**Interfaces:**
- Produces: `MongoConditionalExpression(MongoExpression Test, MongoExpression IfTrue, MongoExpression IfFalse)`, `.Type` = `IfTrue.Type`.
- Produces: `MongoDatePart` enum: `Year, Month, Day, Hour, Minute, Second, Millisecond, DayOfWeek, DayOfYear, Date`.
- Produces: `MongoDatePartExpression(MongoExpression Operand, MongoDatePart Part)`, `.Type` = `typeof(int)` except `Date` → `typeof(DateTime)` and `DayOfWeek` → `typeof(DayOfWeek)`.
- Produces: `MongoDateTimeOffsetLocalExpression(MongoFieldExpression Operand)` — **`Operand` is typed as `MongoFieldExpression`, not the general `MongoExpression`**, because the renderer must dot `.DateTime`/`.Offset` onto its element path, which is only valid MQL when the operand is a plain field reference. `.Type` = `typeof(DateTime)`.

- [ ] **Step 1: Write the failing test**

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
using MongoDB.EntityFrameworkCore.Query.Expressions;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions;

public class MongoConditionalAndDatePartExpressionTests
{
    [Fact]
    public void MongoConditionalExpression_type_is_the_IfTrue_branch_type()
    {
        var test = new MongoConstantExpression(true, forSerialization: null);
        var ifTrue = new MongoConstantExpression(1, forSerialization: null);
        var ifFalse = new MongoConstantExpression(2, forSerialization: null);

        var conditional = new MongoConditionalExpression(test, ifTrue, ifFalse);

        Assert.Same(test, conditional.Test);
        Assert.Same(ifTrue, conditional.IfTrue);
        Assert.Same(ifFalse, conditional.IfFalse);
        Assert.Equal(typeof(int), conditional.Type);
    }

    [Theory]
    [InlineData(MongoDatePart.Year, typeof(int))]
    [InlineData(MongoDatePart.Month, typeof(int))]
    [InlineData(MongoDatePart.Day, typeof(int))]
    [InlineData(MongoDatePart.Hour, typeof(int))]
    [InlineData(MongoDatePart.Minute, typeof(int))]
    [InlineData(MongoDatePart.Second, typeof(int))]
    [InlineData(MongoDatePart.Millisecond, typeof(int))]
    [InlineData(MongoDatePart.DayOfYear, typeof(int))]
    [InlineData(MongoDatePart.Date, typeof(DateTime))]
    [InlineData(MongoDatePart.DayOfWeek, typeof(DayOfWeek))]
    public void MongoDatePartExpression_type_matches_the_part(MongoDatePart part, Type expectedType)
    {
        var operand = new MongoConstantExpression(DateTime.UtcNow, forSerialization: null);

        var datePart = new MongoDatePartExpression(operand, part);

        Assert.Same(operand, datePart.Operand);
        Assert.Equal(part, datePart.Part);
        Assert.Equal(expectedType, datePart.Type);
    }

    [Fact]
    public void MongoDateTimeOffsetLocalExpression_type_is_DateTime()
    {
        var field = new MongoFieldExpression(property: null!, "DateTimeOffsetField");

        var local = new MongoDateTimeOffsetLocalExpression(field);

        Assert.Same(field, local.Operand);
        Assert.Equal(typeof(DateTime), local.Type);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~MongoConditionalAndDatePartExpressionTests"`
Expected: FAIL to compile — `MongoConditionalExpression`, `MongoDatePart`, `MongoDatePartExpression`, `MongoDateTimeOffsetLocalExpression` don't exist yet. (`MongoFieldExpression`'s constructor accepting a null `IProperty` for a throwaway test double: check its actual signature first — it takes `(IProperty property, string elementName)`; passing `null!` is fine since this test never calls a method that dereferences `Property`.)

- [ ] **Step 3: Write the implementation**

`src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoConditionalExpression.cs`:

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

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// A ternary conditional (<c>test ? ifTrue : ifFalse</c>), rendered in the aggregation-expression dialect as
/// <c>$cond</c>.
/// </summary>
/// <remarks>
/// <see cref="Test"/> is always rendered via <c>MongoAggregationExpressionRenderer.Render</c> directly, never
/// via <c>MongoQueryLanguageRenderer.RenderNode</c>'s query/aggregation dual-dialect dispatch — a
/// <c>$cond.if</c> lives inside <c>$project</c>'s expression context, where the query (<c>$match</c>) dialect
/// is never valid, the same rule that already governs everything nested inside <c>$expr</c>.
/// <para>
/// This node is deliberately NOT admitted by <c>MongoQueryLanguageRenderer.IsQueryDialectRenderable</c> — it
/// has no query-dialect form, and <c>$expr</c> (which is what a native <c>$project</c>/<c>$match</c> would
/// need to wrap it in) is a hard server error inside <c>$elemMatch</c>.
/// </para>
/// </remarks>
internal sealed class MongoConditionalExpression(MongoExpression test, MongoExpression ifTrue, MongoExpression ifFalse)
    : MongoExpression
{
    /// <summary>The boolean condition.</summary>
    public MongoExpression Test { get; } = test;

    /// <summary>The value when <see cref="Test"/> is true.</summary>
    public MongoExpression IfTrue { get; } = ifTrue;

    /// <summary>The value when <see cref="Test"/> is false.</summary>
    public MongoExpression IfFalse { get; } = ifFalse;

    /// <inheritdoc />
    public override Type Type { get; } = ifTrue.Type;
}
```

`src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoDatePartExpression.cs`:

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

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// The date-part components this provider translates natively. <c>TimeOfDay</c> is deliberately absent — MQL
/// has no single clean composition for it, and a decline falls back gracefully to the existing driver-LINQ
/// bridge (<c>MongoEFToLinqTranslatingExpressionVisitor</c>), which already handles it.
/// </summary>
internal enum MongoDatePart
{
    Year,
    Month,
    Day,
    Hour,
    Minute,
    Second,
    Millisecond,
    DayOfWeek,
    DayOfYear,
    Date
}

/// <summary>
/// Extracts one <see cref="MongoDatePart"/> component from a datetime-valued <see cref="Operand"/>, rendered
/// in the aggregation-expression dialect as the matching MQL date operator (<c>$year</c>, <c>$month</c>,
/// <c>$dayOfMonth</c>, <c>$hour</c>, <c>$minute</c>, <c>$second</c>, <c>$millisecond</c>, <c>$dayOfWeek</c>,
/// <c>$dayOfYear</c>, or <c>$dateTrunc</c> for <see cref="MongoDatePart.Date"/>).
/// </summary>
/// <remarks>
/// <see cref="Operand"/> is deliberately typed as the general <see cref="MongoExpression"/>, not a bare field:
/// every one of MQL's date-extraction operators accepts any date-valued EXPRESSION, not just a field
/// reference, which is what lets this node wrap a <see cref="MongoDateTimeOffsetLocalExpression"/> (the
/// reconstructed local time for a <c>DateTimeOffset</c> source) as well as a plain <c>DateTime</c> field.
/// <para>
/// Like <see cref="MongoConvertExpression"/>, this node has no query-dialect form and must never be admitted
/// by <c>MongoQueryLanguageRenderer.IsQueryDialectRenderable</c>.
/// </para>
/// </remarks>
internal sealed class MongoDatePartExpression(MongoExpression operand, MongoDatePart part) : MongoExpression
{
    /// <summary>The datetime-valued expression to extract a component from.</summary>
    public MongoExpression Operand { get; } = operand;

    /// <summary>Which component to extract.</summary>
    public MongoDatePart Part { get; } = part;

    /// <inheritdoc />
    public override Type Type { get; } = part switch
    {
        MongoDatePart.Date => typeof(DateTime),
        MongoDatePart.DayOfWeek => typeof(DayOfWeek),
        _ => typeof(int)
    };
}
```

`src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoDateTimeOffsetLocalExpression.cs`:

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

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// Reconstructs the "local" <see cref="DateTime"/> from a stored <c>DateTimeOffset</c> field — this provider
/// stores <c>DateTimeOffset</c> as a subdocument with <c>DateTime</c> (UTC) and <c>Offset</c> (minutes)
/// fields (<c>Storage/Serializers.BsonSerializerFactory</c> — the driver's own <c>DateTimeOffsetSerializer</c>,
/// <c>BsonType.Document</c>), so the local value is the sum of the two, rendered via <c>$dateAdd</c>.
/// </summary>
/// <remarks>
/// This is the same reconstruction <c>MongoEFToLinqTranslatingExpressionVisitor</c> already performs for the
/// driver-LINQ bridge (EF-218, working around CSHARP-5296), re-expressed as a native aggregation-expression
/// node.
/// <para>
/// <see cref="Operand"/> is deliberately typed as <see cref="MongoFieldExpression"/>, not the general
/// <see cref="MongoExpression"/> — the renderer dots <c>.DateTime</c>/<c>.Offset</c> onto its element path,
/// which is only valid MQL when the operand is a plain field reference; there is no way to sub-field-access an
/// arbitrary COMPUTED document value without <c>$getField</c>, which this node does not attempt.
/// </para>
/// </remarks>
internal sealed class MongoDateTimeOffsetLocalExpression(MongoFieldExpression operand) : MongoExpression
{
    /// <summary>The <c>DateTimeOffset</c>-typed field to reconstruct the local time from.</summary>
    public MongoFieldExpression Operand { get; } = operand;

    /// <inheritdoc />
    public override Type Type { get; } = typeof(DateTime);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoConditionalAndDatePartExpressionTests"`
Expected: PASS, 10 tests.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoConditionalExpression.cs \
        src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoDatePartExpression.cs \
        src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoDateTimeOffsetLocalExpression.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoConditionalAndDatePartExpressionTests.cs
git commit -m "Add MongoConditionalExpression, MongoDatePartExpression, MongoDateTimeOffsetLocalExpression IR nodes"
```

---

