### Task 2: `MongoOuterFieldExpression` node type + rendering

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoOuterFieldExpression.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs:53-88` (`Render`), `:118-153` (`CanRender`)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoQueryLanguageRenderer.cs:446-496` (`IsQueryDialectRenderable`)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs:339-380` (`AllFieldsDefaultSerialized`)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs` (create if none exists — `Glob tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/*Renderer*Tests.cs` first; if a file for this renderer already exists, add to it)

**Interfaces:**
- Produces: `MongoOuterFieldExpression(IProperty Property, string ElementName)` — a `MongoExpression` subtype.
- Consumes (Task 1): nothing yet — this node isn't constructed by the translator until Task 3.

- [ ] **Step 1: Check for an existing renderer unit test file**

Run: `find tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation -iname "*Aggregation*"`

If a file exists, read it fully first to match its exact helper/fixture pattern (a placeholder table, a fake `IProperty`, etc.) before adding to it. If none exists, Step 2's test goes into a new file following the pattern below (modeled on how `MongoFieldExpression`/`MongoElementRefExpression` are constructed and rendered elsewhere in this codebase — a minimal fake `IProperty` via a throwaway EF model, e.g. `new ModelBuilder().Entity<T>().Property(p => p.X).Metadata`).

- [ ] **Step 2: Write the failing test**

```csharp
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

public class MongoAggregationExpressionRendererTests
{
    private class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IProperty NameProperty()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<Widget>().Property(w => w.Name);
        var model = modelBuilder.Model.FinalizeModel();
        return model.FindEntityType(typeof(Widget))!.FindProperty(nameof(Widget.Name))!;
    }

    [Fact]
    public void MongoOuterFieldExpression_renders_at_document_root_with_no_element_variable()
    {
        var node = new MongoOuterFieldExpression(NameProperty(), "Name");

        var rendered = MongoAggregationExpressionRenderer.Render(node, new PlaceholderTable());

        Assert.Equal((BsonValue)"$Name", rendered);
    }

    [Fact]
    public void MongoOuterFieldExpression_renders_at_document_root_even_inside_a_filter_scope()
    {
        // The whole point of this node: unlike MongoFieldExpression, an elementVariable in scope must NOT
        // change its rendering — it always means "the enclosing document", never "the filter's own element".
        var node = new MongoOuterFieldExpression(NameProperty(), "Name");

        var rendered = MongoAggregationExpressionRenderer.Render(node, new PlaceholderTable(), elementVariable: "e");

        Assert.Equal((BsonValue)"$Name", rendered);
    }

    [Fact]
    public void MongoOuterFieldExpression_can_render()
    {
        Assert.True(MongoAggregationExpressionRenderer.CanRender(new MongoOuterFieldExpression(NameProperty(), "Name")));
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoOuterFieldExpression"`
Expected: FAIL to compile — `MongoOuterFieldExpression` does not exist yet.

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
using Microsoft.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// A field reference that always resolves at DOCUMENT ROOT, even when rendered inside a <c>$filter</c>/<c>$map</c>
/// scope that has bound its own element to a variable (see
/// <see cref="NativeTranslation.MongoAggregationExpressionRenderer"/>'s <c>elementVariable</c> parameter).
/// </summary>
/// <remarks>
/// <para>
/// A deliberate SIBLING of <see cref="MongoFieldExpression"/> rather than a flag on it (this codebase's own
/// convention for a node kind needing different handling at multiple existing sites — see
/// <see cref="MongoFilteredSizeExpression"/>'s remarks for the same reasoning). Used by a TWO-SCOPE
/// <see cref="NativeTranslation.MongoExpressionTranslator"/> to resolve a member rooted on its OUTER
/// parameter: outside any <c>$filter</c>/<c>$map</c> (e.g. a correlated <c>SelectMany</c> inner filter,
/// rendered as a top-level <c>$expr</c>) this renders identically to an ordinary <see cref="MongoFieldExpression"/>
/// at the same path — <c>elementVariable</c> is <see langword="null"/> there too. INSIDE a
/// <see cref="MongoFilteredSizeExpression"/>'s <c>$filter</c> or a quantifier's <c>$map</c>, however, an
/// ordinary <see cref="MongoFieldExpression"/> would be misread as "the filter's own element" (rendered
/// <c>"$$" + elementVariable + "." + path</c>) rather than the enclosing document this field actually belongs
/// to — this node exists specifically to keep rendering as <c>"$" + path</c> in that position too.
/// </para>
/// <para>
/// Aggregation-expression-ONLY: it has no query-dialect (<c>$match</c>) form at all, since it only has
/// meaning as an operand inside an aggregation expression that itself distinguishes an inner vs. outer scope.
/// <see cref="NativeTranslation.MongoQueryLanguageRenderer.IsQueryDialectRenderable"/> declines it
/// unconditionally.
/// </para>
/// </remarks>
internal sealed class MongoOuterFieldExpression : MongoExpression
{
    /// <summary>
    /// Creates a <see cref="MongoOuterFieldExpression"/> for the given property.
    /// </summary>
    /// <param name="property">The EF Core <see cref="IProperty"/> this field corresponds to.</param>
    /// <param name="elementName">The document element name, relative to the OUTER (enclosing) document root.</param>
    public MongoOuterFieldExpression(IProperty property, string elementName)
    {
        Property = property;
        ElementName = elementName;
    }

    /// <summary>The EF Core property metadata for this field.</summary>
    public new IProperty Property { get; }

    /// <summary>The document element name, relative to the outer document root.</summary>
    public string ElementName { get; }

    /// <inheritdoc />
    public override Type Type => Property.ClrType;
}
```

- [ ] **Step 5: Add the `Render`/`CanRender` arms**

In `MongoAggregationExpressionRenderer.cs`, add a case to the `Render` switch, immediately after the existing `MongoFieldExpression`/`MongoElementRefExpression` arms:

```csharp
            MongoFieldExpression field => FieldRef(field.ElementName, elementVariable),
            MongoElementRefExpression elementRef => FieldRef(elementRef.Path, elementVariable),
            // Always at document root, REGARDLESS of elementVariable — see the node's own remarks.
            MongoOuterFieldExpression outer => FieldRef(outer.ElementName, elementVariable: null),
```

and to `CanRender`, immediately after the existing `MongoFieldExpression or MongoElementRefExpression => true` arm:

```csharp
            MongoFieldExpression or MongoElementRefExpression or MongoOuterFieldExpression => true,
```

(replacing the existing line, not adding a duplicate).

- [ ] **Step 6: Add the `IsQueryDialectRenderable` arm**

In `MongoQueryLanguageRenderer.cs`, add immediately after the existing `MongoDateTimeOffsetLocalExpression => false,` line:

```csharp
            MongoDateTimeOffsetLocalExpression => false,
            // No query-dialect form at all — see the node's own remarks. Explicit rather than left to the
            // catch-all, matching the style of MongoConditionalExpression/MongoDatePartExpression above.
            MongoOuterFieldExpression => false,
```

- [ ] **Step 7: Add the `AllFieldsDefaultSerialized` arm**

In `MongoExpressionTranslator.cs`, in `AllFieldsDefaultSerialized`, add immediately after the existing `MongoFieldExpression f => ...` arm:

```csharp
            MongoFieldExpression f => NativeGroupByBinder.HasDefaultKeySerialization(f.Property),
            // Same correctness check as the MongoFieldExpression arm above, over the OUTER property instead.
            MongoOuterFieldExpression outerField => NativeGroupByBinder.HasDefaultKeySerialization(outerField.Property),
```

- [ ] **Step 8: Run the test to verify it passes**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoOuterFieldExpression"`
Expected: PASS (3 tests).

- [ ] **Step 9: Run the full unit test suite**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build`
Expected: PASS, no regressions.

- [ ] **Step 10: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoOuterFieldExpression.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoQueryLanguageRenderer.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs
git commit -m "EF-421: add MongoOuterFieldExpression node and rendering"
```

---

