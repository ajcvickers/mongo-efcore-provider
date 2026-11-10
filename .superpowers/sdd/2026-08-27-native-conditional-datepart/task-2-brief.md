### Task 2: Renderer arms (`MongoAggregationExpressionRenderer`)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs:53-73` (the `Render` switch) and `:103-134` (the `CanRender` switch)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererDatePartTests.cs`

**Interfaces:**
- Consumes: `MongoConditionalExpression`, `MongoDatePartExpression`, `MongoDateTimeOffsetLocalExpression` from Task 1.
- Produces: `MongoAggregationExpressionRenderer.Render`/`CanRender` now handle all three node kinds.

- [ ] **Step 1: Write the failing test**

First, find the existing unit-test pattern for this renderer:

```bash
grep -n "class MongoAggregationExpressionRendererTests" -A 40 tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs | head -60
```

Then add a new test file using the same `PlaceholderTable`/`Render`/`CanRender` call pattern that file uses (construct a `PlaceholderTable`, call `MongoAggregationExpressionRenderer.Render(node, placeholders)`, assert on the resulting `BsonDocument`/`BsonValue` via `.ToJson()` or direct traversal):

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
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

public class MongoAggregationExpressionRendererDatePartTests
{
    // Mirrors MongoAggregationExpressionRendererTests' own Customer/GetProperty<T> pattern exactly, so a
    // MongoFieldExpression built here carries a real IProperty rather than a null double — some renderer
    // paths (e.g. AllFieldsDefaultSerialized, reached transitively from CanRender for other node kinds in
    // this same file) do read Property, so a null double would throw a NullReferenceException instead of the
    // renderer's own exception type, masking the actual thing under test.
    private class Row
    {
        public ObjectId Id { get; set; }
        public DateTime Occurred { get; set; }
        public DateTimeOffset OccurredOffset { get; set; }
    }

    private static IProperty GetProperty(string propertyName)
    {
        using var db = SingleEntityDbContext.Create<Row>();
        return db.Model.FindEntityType(typeof(Row))!.FindProperty(propertyName)!;
    }

    private static MongoFieldExpression Field(string propertyName, string elementName)
        => new(GetProperty(propertyName), elementName);

    [Fact]
    public void Conditional_renders_as_cond()
    {
        var placeholders = new PlaceholderTable();
        var node = new MongoConditionalExpression(
            new MongoConstantExpression(true, forSerialization: null),
            new MongoConstantExpression(1, forSerialization: null),
            new MongoConstantExpression(2, forSerialization: null));

        var rendered = MongoAggregationExpressionRenderer.Render(node, placeholders);

        Assert.Equal(
            new BsonDocument("$cond", new BsonDocument { { "if", true }, { "then", 1 }, { "else", 2 } }),
            rendered);
    }

    [Fact]
    public void DateTimeOffsetLocal_renders_as_dateAdd_of_DateTime_and_Offset_subfields()
    {
        var placeholders = new PlaceholderTable();
        var node = new MongoDateTimeOffsetLocalExpression(Field("OccurredOffset", "Occurred"));

        var rendered = MongoAggregationExpressionRenderer.Render(node, placeholders);

        Assert.Equal(
            new BsonDocument("$dateAdd", new BsonDocument
            {
                { "startDate", "$Occurred.DateTime" },
                { "unit", "minute" },
                { "amount", "$Occurred.Offset" }
            }),
            rendered);
    }

    [Theory]
    [InlineData(MongoDatePart.Year, "$year")]
    [InlineData(MongoDatePart.Month, "$month")]
    [InlineData(MongoDatePart.Day, "$dayOfMonth")]
    [InlineData(MongoDatePart.Hour, "$hour")]
    [InlineData(MongoDatePart.Minute, "$minute")]
    [InlineData(MongoDatePart.Second, "$second")]
    [InlineData(MongoDatePart.Millisecond, "$millisecond")]
    [InlineData(MongoDatePart.DayOfYear, "$dayOfYear")]
    public void DatePart_renders_as_the_matching_operator(MongoDatePart part, string operatorName)
    {
        var placeholders = new PlaceholderTable();
        var node = new MongoDatePartExpression(Field("Occurred", "Occurred"), part);

        var rendered = MongoAggregationExpressionRenderer.Render(node, placeholders);

        Assert.Equal(new BsonDocument(operatorName, "$Occurred"), rendered);
    }

    [Fact]
    public void DatePart_DayOfWeek_subtracts_one_to_match_dotnet_numbering()
    {
        // MongoDB's $dayOfWeek returns 1 (Sunday)..7 (Saturday); .NET's DayOfWeek is 0 (Sunday)..6 (Saturday).
        var placeholders = new PlaceholderTable();
        var node = new MongoDatePartExpression(Field("Occurred", "Occurred"), MongoDatePart.DayOfWeek);

        var rendered = MongoAggregationExpressionRenderer.Render(node, placeholders);

        Assert.Equal(
            new BsonDocument("$subtract", new BsonArray { new BsonDocument("$dayOfWeek", "$Occurred"), 1 }),
            rendered);
    }

    [Fact]
    public void DatePart_Date_renders_as_dateTrunc_day()
    {
        var placeholders = new PlaceholderTable();
        var node = new MongoDatePartExpression(Field("Occurred", "Occurred"), MongoDatePart.Date);

        var rendered = MongoAggregationExpressionRenderer.Render(node, placeholders);

        Assert.Equal(
            new BsonDocument("$dateTrunc", new BsonDocument { { "date", "$Occurred" }, { "unit", "day" } }),
            rendered);
    }

    [Fact]
    public void CanRender_is_true_for_all_three_new_node_kinds()
    {
        var conditional = new MongoConditionalExpression(
            new MongoConstantExpression(true, forSerialization: null),
            new MongoConstantExpression(1, forSerialization: null),
            new MongoConstantExpression(2, forSerialization: null));
        var local = new MongoDateTimeOffsetLocalExpression(Field("OccurredOffset", "Occurred"));
        var datePart = new MongoDatePartExpression(Field("Occurred", "Occurred"), MongoDatePart.Year);

        Assert.True(MongoAggregationExpressionRenderer.CanRender(conditional));
        Assert.True(MongoAggregationExpressionRenderer.CanRender(local));
        Assert.True(MongoAggregationExpressionRenderer.CanRender(datePart));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoAggregationExpressionRendererDatePartTests"`
Expected: FAIL — `Render`/`CanRender` throw `NativeTranslationNotSupportedException` / return `false` for the unhandled node kinds. If `Field(...)` throws a `NullReferenceException` instead (because some code path dereferences `.Property`), stop and read `MongoFieldExpression`'s constructor plus every existing renderer arm that touches `MongoFieldExpression.Property` before proceeding — build a minimal fake `IProperty` fixture matching whatever `MongoAggregationExpressionRendererTests.cs` already uses for its own `MongoFieldExpression` construction instead of `null!`.

- [ ] **Step 3: Write the implementation**

In `MongoAggregationExpressionRenderer.cs`, extend the `Render` switch (currently ending with the `MongoConvertExpression` arm at line ~64-70, right before the `_ => throw` catch-all at line 71):

```csharp
            MongoConvertExpression convert
                => new BsonDocument(
                    MongoConvertExpression.ToOperatorFor(convert.Type)
                        ?? throw new NativeTranslationNotSupportedException(
                            $"MQL has no conversion operator for '{convert.Type.Name}'. A convert to an "
                            + "unrenderable target should have been declined at translate time."),
                    Render(convert.Operand, placeholders, elementVariable)),
            MongoConditionalExpression conditional
                => new BsonDocument("$cond", new BsonDocument
                {
                    { "if", Render(conditional.Test, placeholders, elementVariable) },
                    { "then", Render(conditional.IfTrue, placeholders, elementVariable) },
                    { "else", Render(conditional.IfFalse, placeholders, elementVariable) }
                }),
            MongoDateTimeOffsetLocalExpression local
                => new BsonDocument("$dateAdd", new BsonDocument
                {
                    { "startDate", FieldRef(local.Operand.ElementName + ".DateTime", elementVariable) },
                    { "unit", "minute" },
                    { "amount", FieldRef(local.Operand.ElementName + ".Offset", elementVariable) }
                }),
            MongoDatePartExpression datePart => RenderDatePart(datePart, placeholders, elementVariable),
            _ => throw new NativeTranslationNotSupportedException(
                $"MongoAggregationExpressionRenderer does not support node type '{node.GetType().Name}'.")
```

Add the new `RenderDatePart` helper near `RenderSize`/`RenderFilteredSize` (after `FieldRef`, around line 176):

```csharp
    // MongoDB's $dayOfWeek returns 1 (Sunday)..7 (Saturday); .NET's DayOfWeek enum is 0 (Sunday)..6 (Saturday).
    // The subtraction is mandatory, not defensive — omitting it silently shifts every day of the week by one.
    private static BsonValue RenderDatePart(MongoDatePartExpression node, PlaceholderTable placeholders, string? elementVariable)
    {
        var operand = Render(node.Operand, placeholders, elementVariable);
        return node.Part switch
        {
            MongoDatePart.Year => new BsonDocument("$year", operand),
            MongoDatePart.Month => new BsonDocument("$month", operand),
            MongoDatePart.Day => new BsonDocument("$dayOfMonth", operand),
            MongoDatePart.Hour => new BsonDocument("$hour", operand),
            MongoDatePart.Minute => new BsonDocument("$minute", operand),
            MongoDatePart.Second => new BsonDocument("$second", operand),
            MongoDatePart.Millisecond => new BsonDocument("$millisecond", operand),
            MongoDatePart.DayOfYear => new BsonDocument("$dayOfYear", operand),
            MongoDatePart.DayOfWeek
                => new BsonDocument("$subtract", new BsonArray { new BsonDocument("$dayOfWeek", operand), 1 }),
            MongoDatePart.Date => new BsonDocument("$dateTrunc", new BsonDocument { { "date", operand }, { "unit", "day" } }),
            _ => throw new NativeTranslationNotSupportedException($"Unhandled {nameof(MongoDatePart)} '{node.Part}'.")
        };
    }
```

Extend the `CanRender` switch (currently ending with the `MongoConvertExpression` arm at line ~131-132, right before the `_ => false` catch-all at line 133):

```csharp
            MongoConvertExpression convert
                => MongoConvertExpression.ToOperatorFor(convert.Type) is not null && CanRender(convert.Operand),
            MongoConditionalExpression conditional
                => CanRender(conditional.Test) && CanRender(conditional.IfTrue) && CanRender(conditional.IfFalse),
            MongoDateTimeOffsetLocalExpression local => CanRender(local.Operand),
            MongoDatePartExpression datePart => CanRender(datePart.Operand),
            _ => false
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoAggregationExpressionRendererDatePartTests"`
Expected: PASS, all cases.

- [ ] **Step 5: Run the full unit test suite to check for regressions**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"`
Expected: PASS, zero regressions (this switch change is purely additive).

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererDatePartTests.cs
git commit -m "Render MongoConditionalExpression/MongoDatePartExpression/MongoDateTimeOffsetLocalExpression as $cond/date operators/$dateAdd"
```

---

