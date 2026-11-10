### Task 3: Translation — conditional expressions (`MongoExpressionTranslator.TranslateOperand`)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs:851-880` (`TranslateOperand`, right after the existing `Convert`-handling block)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoConditionalAndDateTimeTranslatorTests.cs`

**Interfaces:**
- Consumes: `MongoConditionalExpression` (Task 1), `TryTranslate` (existing predicate translator), `TryTranslateValue`/`TranslateOperand` (existing, this task extends them).
- Produces: `TranslateOperand`/`TryTranslateValue` now translate a `ConditionalExpression` C# node into a `MongoConditionalExpression`.

- [ ] **Step 1: Write the failing test**

Create the test file with its own small entity fixture (do not modify the large shared `Order`/`Customer` fixtures in `MongoExpressionTranslatorTests.cs`):

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

public class MongoConditionalAndDateTimeTranslatorTests
{
    private class Row
    {
        public ObjectId Id { get; set; }
        public int Amount { get; set; }
        public bool Flag { get; set; }
        public DateTime Occurred { get; set; }
        public DateTimeOffset? OccurredOffset { get; set; }
    }

    private static (MongoExpressionTranslator Translator, Expression Body) BuildValueBody(
        Expression<Func<Row, object>> valueSelector)
    {
        using var db = SingleEntityDbContext.Create<Row>();
        var entityType = db.Model.FindEntityType(typeof(Row))!;
        var body = valueSelector.Body is UnaryExpression { NodeType: ExpressionType.Convert } unary
            ? unary.Operand
            : valueSelector.Body;
        return (new MongoExpressionTranslator(entityType), body);
    }

    [Fact]
    public void Conditional_with_field_condition_and_constant_branches_translates()
    {
        var (translator, body) = BuildValueBody(r => r.Flag ? 1 : 2);

        Assert.True(translator.TryTranslateValue(body, out var result));

        var conditional = Assert.IsType<MongoConditionalExpression>(result);
        Assert.IsType<MongoFieldExpression>(conditional.Test);
        Assert.Equal(1, Assert.IsType<MongoConstantExpression>(conditional.IfTrue).Value);
        Assert.Equal(2, Assert.IsType<MongoConstantExpression>(conditional.IfFalse).Value);
    }

    [Fact]
    public void Conditional_with_null_check_condition_translates()
    {
        var (translator, body) = BuildValueBody(r => r.OccurredOffset == null ? (DateTime?)null : r.Occurred);

        Assert.True(translator.TryTranslateValue(body, out var result));

        var conditional = Assert.IsType<MongoConditionalExpression>(result);
        // The Test is a comparison (r.OccurredOffset == null), not a bare field.
        Assert.IsType<MongoBinaryExpression>(conditional.Test);
        Assert.Null(Assert.IsType<MongoConstantExpression>(conditional.IfFalse is MongoConstantExpression
            ? conditional.IfFalse : conditional.IfTrue).Value is null ? null : (object?)null);
    }

    [Fact]
    public void Conditional_with_nested_conditional_branch_translates()
    {
        var (translator, body) = BuildValueBody(r => r.Flag ? (r.Amount > 0 ? 1 : 2) : 3);

        Assert.True(translator.TryTranslateValue(body, out var result));

        var outer = Assert.IsType<MongoConditionalExpression>(result);
        Assert.IsType<MongoConditionalExpression>(outer.IfTrue);
    }

    [Fact]
    public void Conditional_declines_when_a_branch_is_unsupported()
    {
        // string.Concat has no native translation at all, so a branch that reaches it must decline the
        // WHOLE conditional, not silently drop that branch.
        var (translator, body) = BuildValueBody(r => r.Flag ? r.Amount : int.Parse("x"));

        Assert.False(translator.TryTranslateValue(body, out _));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoConditionalAndDateTimeTranslatorTests"`
Expected: FAIL — `TryTranslateValue` currently returns `false` for a `ConditionalExpression` body (no case handles it), so `Conditional_with_field_condition_and_constant_branches_translates` etc. fail their `Assert.True`. `Conditional_declines_when_a_branch_is_unsupported` passes already by accident (everything declines today) — that's fine, it becomes a real regression guard once Step 3 lands.

- [ ] **Step 3: Write the implementation**

In `MongoExpressionTranslator.cs`, insert a new case into `TranslateOperand` immediately after the existing `Convert`-handling `if` block (after line 880's closing brace, before line 882's `if (TryResolveMember(node, ...))`):

```csharp
        // A ternary. Test is translated via the ORDINARY predicate translator (TryTranslate — the same one
        // Where uses), so anything already supported there as a predicate (nullable equality/comparison,
        // field-to-field, etc.) works as a $cond condition too. Both branches recurse through this same
        // method, so a branch may itself be a cast, arithmetic, nested conditional, or date-part expression.
        // A branch that declines declines the WHOLE conditional — there is no partial/fallback rendering for
        // one branch of a $cond.
        if (node is ConditionalExpression conditional)
        {
            if (!TryTranslate(conditional.Test, out var test))
                return null;

            var ifTrue = TranslateOperand(conditional.IfTrue, allowNumericWidening);
            if (ifTrue is null)
                return null;

            var ifFalse = TranslateOperand(conditional.IfFalse, allowNumericWidening);
            if (ifFalse is null)
                return null;

            return new MongoConditionalExpression(test, ifTrue, ifFalse);
        }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoConditionalAndDateTimeTranslatorTests"`
Expected: PASS, all 4 tests.

- [ ] **Step 5: Run the full unit test suite to check for regressions**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"`
Expected: PASS, zero regressions.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoConditionalAndDateTimeTranslatorTests.cs
git commit -m "Translate ConditionalExpression to MongoConditionalExpression in TranslateOperand"
```

---

