### Task 4: `MongoQuantifierExpression` node type + rendering

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQuantifierExpression.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoQueryLanguageRenderer.cs`
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs` (append)

**Interfaces:**
- Produces: `MongoQuantifierExpression(MongoElementRefExpression ArrayPath, MongoExpression ElementPredicate, MongoQuantifierKind Kind)`. `MongoQuantifierKind` is currently `private` inside `MongoExpressionTranslator.MethodCalls.cs` (Task 5's file) — widen it to `internal` in that same file so this node type (in `Expressions/`, a different namespace) can reference it.

- [ ] **Step 1: Widen `MongoQuantifierKind` to internal**

In `MongoExpressionTranslator.MethodCalls.cs`, change:

```csharp
    /// <summary>Which quantifier <see cref="TryMatchQuantifierMethod"/> matched.</summary>
    private enum MongoQuantifierKind
```

to:

```csharp
    /// <summary>Which quantifier <see cref="TryMatchQuantifierMethod"/> matched.</summary>
    internal enum MongoQuantifierKind
```

- [ ] **Step 2: Write the failing test**

Append to `MongoAggregationExpressionRendererTests.cs`:

```csharp
    [Fact]
    public void MongoQuantifierExpression_Any_renders_as_anyElementTrue_over_map()
    {
        var elementPredicate = new MongoBinaryExpression(
            MongoBinaryOperator.GreaterThan,
            new MongoFieldExpression(NameProperty(), "Rank"),
            new MongoConstantExpression(5, forSerialization: null));
        var node = new MongoQuantifierExpression(
            new MongoElementRefExpression("Posts", typeof(object)), elementPredicate, MongoExpressionTranslator.MongoQuantifierKind.Any);

        var rendered = MongoAggregationExpressionRenderer.Render(node, new PlaceholderTable());

        Assert.Equal(
            BsonDocument.Parse(
                "{ $anyElementTrue: { $map: { input: { $ifNull: ['$Posts', []] }, as: 'this', "
                + "in: { $gt: ['$$this.Rank', 5] } } } }"),
            rendered);
    }

    [Fact]
    public void MongoQuantifierExpression_All_renders_as_allElementsTrue_over_map()
    {
        var elementPredicate = new MongoBinaryExpression(
            MongoBinaryOperator.GreaterThan,
            new MongoFieldExpression(NameProperty(), "Rank"),
            new MongoConstantExpression(5, forSerialization: null));
        var node = new MongoQuantifierExpression(
            new MongoElementRefExpression("Posts", typeof(object)), elementPredicate, MongoExpressionTranslator.MongoQuantifierKind.All);

        var rendered = MongoAggregationExpressionRenderer.Render(node, new PlaceholderTable());

        Assert.Equal(
            BsonDocument.Parse(
                "{ $allElementsTrue: { $map: { input: { $ifNull: ['$Posts', []] }, as: 'this', "
                + "in: { $gt: ['$$this.Rank', 5] } } } }"),
            rendered);
    }

    [Fact]
    public void MongoQuantifierExpression_can_render()
    {
        var node = new MongoQuantifierExpression(
            new MongoElementRefExpression("Posts", typeof(object)),
            new MongoFieldExpression(NameProperty(), "Active"),
            MongoExpressionTranslator.MongoQuantifierKind.Any);

        Assert.True(MongoAggregationExpressionRenderer.CanRender(node));
    }
```

Add `using MongoDB.EntityFrameworkCore.Query.NativeTranslation;` if not already present (needed to reference `MongoExpressionTranslator.MongoQuantifierKind` — internal but same assembly).

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoQuantifierExpression"`
Expected: FAIL to compile — `MongoQuantifierExpression` does not exist yet.

- [ ] **Step 4: Create the node type**

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
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// Represents a CORRELATED quantifier over an owned (embedded) array — <c>b.Posts.Any(p =&gt; p.X == b.Y)</c>
/// or the <c>All</c> equivalent — whose element predicate references the immediately enclosing entity.
/// Renders as <c>$anyElementTrue</c>/<c>$allElementsTrue</c> over a <c>$map</c>.
/// </summary>
/// <remarks>
/// <para>
/// The UNCORRELATED quantifier path is unchanged: it still uses <see cref="MongoElemMatchExpression"/>
/// (<c>$elemMatch</c>), which is more index-friendly. This node exists ONLY for the correlated case, where
/// <c>$elemMatch</c> cannot reference the enclosing document at all — <c>$anyElementTrue</c>/
/// <c>$allElementsTrue</c> over a <c>$map</c> is the only MQL form that can, since the <c>$map</c>'s own
/// <c>in</c> expression is an ordinary aggregation expression with the enclosing document still reachable.
/// </para>
/// <para>
/// UNLIKE the uncorrelated <c>All</c> path (a NEGATED <see cref="MongoElemMatchExpression"/> over the exact
/// complement, since <c>$elemMatch</c> has no native "for all" form), <c>All</c> here needs NO negation:
/// <c>$allElementsTrue</c> is itself a "for all" operator, so <see cref="ElementPredicate"/> is the predicate
/// translated DIRECTLY, exactly as <see cref="Kind"/><c> == Any</c>'s is. This sidesteps
/// <see cref="NativeTranslation.MongoExpressionNegator"/> entirely for the correlated path.
/// </para>
/// <para>
/// Aggregation-expression-ONLY — no query-dialect form. It has no query-dialect analogue at all (unlike
/// <see cref="MongoElemMatchExpression"/>, which IS query-dialect), so
/// <see cref="NativeTranslation.MongoQueryLanguageRenderer.IsQueryDialectRenderable"/> declines it
/// unconditionally, and the renderer's own top-level "no dialect form → wrap in <c>$expr</c>" fallback
/// (<see cref="NativeTranslation.MongoQueryLanguageRenderer"/>'s <c>RenderNode</c> catch-all) wraps it
/// automatically — no bespoke <c>$expr</c>-wrapping code is needed for this node.
/// </para>
/// </remarks>
internal sealed class MongoQuantifierExpression : MongoExpression
{
    /// <summary>
    /// Creates a <see cref="MongoQuantifierExpression"/>.
    /// </summary>
    /// <param name="arrayPath">
    /// The dotted document path of the embedded array, relative to the enclosing (outer) document root —
    /// e.g. <c>"Posts"</c>. Always OUTER-relative, unlike <see cref="MongoElemMatchExpression.ArrayPath"/>,
    /// because this node exists specifically for the correlated case.
    /// </param>
    /// <param name="elementPredicate">
    /// The predicate each candidate element is tested against, with ELEMENT-RELATIVE field paths for the
    /// inner scope (rendered against the <c>$map</c>'s own <c>as</c> variable) and OUTER-relative
    /// (<see cref="MongoOuterFieldExpression"/>) field paths for anything reaching the enclosing entity.
    /// </param>
    /// <param name="kind">Whether this is an <c>Any</c> or <c>All</c> quantifier.</param>
    public MongoQuantifierExpression(MongoElementRefExpression arrayPath, MongoExpression elementPredicate, MongoQuantifierKind kind)
    {
        ArrayPath = arrayPath;
        ElementPredicate = elementPredicate;
        Kind = kind;
    }

    /// <summary>The dotted document path of the embedded array, relative to the enclosing (outer) document root.</summary>
    public MongoElementRefExpression ArrayPath { get; }

    /// <summary>The predicate each candidate element is tested against.</summary>
    public MongoExpression ElementPredicate { get; }

    /// <summary>Whether this is an <c>Any</c> or <c>All</c> quantifier.</summary>
    public MongoQuantifierKind Kind { get; }

    /// <inheritdoc />
    public override Type Type => typeof(bool);
}
```

- [ ] **Step 5: Add the `Render` arm**

In `MongoAggregationExpressionRenderer.cs`, add a case to the `Render` switch, immediately before the final `_ => throw ...` catch-all:

```csharp
            MongoDatePartExpression datePart => RenderDatePart(datePart, placeholders, elementVariable),
            MongoQuantifierExpression quantifier => RenderQuantifier(quantifier, placeholders, elementVariable),
            _ => throw new NativeTranslationNotSupportedException(
                $"MongoAggregationExpressionRenderer does not support node type '{node.GetType().Name}'.")
```

and add the private method, near `RenderFilteredSize` (same nesting-variable-naming convention):

```csharp
    private static BsonValue RenderQuantifier(
        MongoQuantifierExpression node, PlaceholderTable placeholders, string? elementVariable)
    {
        // Same nesting-variable convention as RenderFilteredSize: each nesting level gets its own $map `as`
        // name, derived from the enclosing one, so nested quantifiers/filters never collide.
        var variable = elementVariable is null ? "e" : elementVariable + "e";

        var map = new BsonDocument("$map", new BsonDocument
        {
            // $ifNull is MANDATORY, same reasoning as RenderFilteredSize/RenderSize: $map over a missing or
            // explicitly-null array is a hard server error. [] yields a $map result of [], and
            // $anyElementTrue/$allElementsTrue over [] answer false/true respectively — exactly LINQ's
            // Any/All-over-an-empty-sequence semantics.
            { "input", new BsonDocument("$ifNull", new BsonArray { Render(node.ArrayPath, placeholders, elementVariable), new BsonArray() }) },
            { "as", variable },
            { "in", Render(node.ElementPredicate, placeholders, variable) }
        });

        var op = node.Kind == MongoExpressionTranslator.MongoQuantifierKind.All ? "$allElementsTrue" : "$anyElementTrue";
        return new BsonDocument(op, map);
    }
```

- [ ] **Step 6: Add the `CanRender` arm**

In `MongoAggregationExpressionRenderer.cs`, add to the `CanRender` switch, immediately before its final `_ => false`:

```csharp
            MongoDatePartExpression datePart => CanRender(datePart.Operand),
            MongoQuantifierExpression quantifier => CanRender(quantifier.ArrayPath) && CanRender(quantifier.ElementPredicate),
            _ => false
```

- [ ] **Step 7: Add the `IsQueryDialectRenderable` arm**

In `MongoQueryLanguageRenderer.cs`, add immediately after the `MongoOuterFieldExpression => false,` arm added in Task 2:

```csharp
            MongoOuterFieldExpression => false,
            MongoQuantifierExpression => false,
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoQuantifierExpression"`
Expected: PASS (3 tests). If the exact BSON key ordering in the `Assert.Equal(BsonDocument.Parse(...), rendered)` comparisons doesn't match on the first run, adjust the expected strings to match the ACTUAL rendered output (`Console.WriteLine(rendered.ToJson())` while debugging) rather than fighting `BsonDocument` equality semantics — `BsonDocument.Equals` is order-sensitive, and this plan's hand-written expected strings may not exactly match the dictionary insertion order used above; the important thing verified by the test is the fields/operators present, not this plan's exact string.

- [ ] **Step 9: Run the full unit test suite**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build`
Expected: PASS, no regressions.

- [ ] **Step 10: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQuantifierExpression.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.MethodCalls.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoQueryLanguageRenderer.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs
git commit -m "EF-421: add MongoQuantifierExpression node and \$anyElementTrue/\$allElementsTrue rendering"
```

---

