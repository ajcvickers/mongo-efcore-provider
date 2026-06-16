# Shaper Layout Rework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the query shaper's heuristic re-derivation of document field locations (`GetCrossCollectionFieldName` / `GetCrossCollectionRootDocument` / `UsesDriverJoinFields` reads) with a translator-authored `DocumentLayout` descriptor the shaper consumes verbatim, eliminating the silent-wrong-data class.

**Architecture:** The translator authors a `DocumentLayout` tree incrementally as it builds the pipeline, then finalizes it **once** (resolving `_outer`/`_inner` vs `_lookup_<Nav>` from `UsesDriverJoinFields` a single time, centrally) into concrete BSON paths. The shaper reads concrete paths from the finalized layout instead of recomputing them per binding. MQL stays frozen so the existing ~1,100 baselines are the behavior-preservation proof. EF10 only; EF8/EF9 backport is a follow-on.

**Tech Stack:** C# / .NET 10, EF Core 10 internals, MongoDB C# driver LINQ v3, xUnit + FluentAssertions. Build config `Debug EF10`. Tests run serially against a Docker testcontainer (or `MONGODB_URI`).

**Reference spec:** `docs/superpowers/specs/2026-06-16-shaper-layout-rework-design.md`

---

## File Structure

**New files:**
- `src/MongoDB.EntityFrameworkCore/Query/Layout/DocumentLayout.cs` — the immutable descriptor node (one per shaped slot). Depends only on EF metadata + primitives.
- `src/MongoDB.EntityFrameworkCore/Query/Layout/DocumentLayoutKind.cs` — the node-kind enum.
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/DocumentLayoutTests.cs` — unit tests for the type and path composition (no DB).
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/DocumentLayoutBuilderTests.cs` — layout-tree-shape assertions per query pattern (no DB; uses an in-memory-modelled context to translate and inspect `ResultLayout`).

**Modified files:**
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQueryExpression.cs` — add `ResultLayout` carrier property + `SetResultLayout`.
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQueryExpression.Lookup.cs` — add `FinalizeLayout()` that bakes concrete paths from `UsesDriverJoinFields` once.
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs` (+ `.Lookup.cs`) — author layout nodes at each placement decision (producer).
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingRemovingExpressionVisitor.cs` — consume layout; delete heuristics (consumer).
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoMixedProjectionBindingRemovingExpressionVisitor.cs` — drop its `UsesDriverJoinFields` read (consumer).

---

## Phase 0 — Scaffold & confirm baseline

### Task 0: Confirm a green EF10 baseline before any change

**Files:** none (verification only)

- [ ] **Step 1: Build EF10**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" -v quiet`
Expected: build succeeds, 0 errors.

- [ ] **Step 2: Run the full EF10 suite to capture the green baseline**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build`
Expected: 0 failures (skips are fine). Record the passed/skipped counts — this is the behavior-preservation oracle for the whole plan.

- [ ] **Step 3: Commit nothing; note the baseline counts in the task tracker.**

---

### Task 1: `DocumentLayoutKind` enum

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/Layout/DocumentLayoutKind.cs`

- [ ] **Step 1: Write the enum**

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

namespace MongoDB.EntityFrameworkCore.Query.Layout;

/// <summary>
/// The role a <see cref="DocumentLayout"/> node plays in the shaped result. Phase 1 uses
/// <see cref="Entity"/>, <see cref="Navigation"/>, and <see cref="Collection"/>; further kinds
/// (e.g. Grouping) are added when new query-feature shapes are implemented.
/// </summary>
internal enum DocumentLayoutKind
{
    /// <summary>The query-root entity.</summary>
    Entity,

    /// <summary>A reference (singleton) navigation joined into the document.</summary>
    Navigation,

    /// <summary>A collection navigation joined into the document.</summary>
    Collection
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/MongoDB.EntityFrameworkCore/MongoDB.EntityFrameworkCore.csproj -c "Debug EF10" -v quiet`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Layout/DocumentLayoutKind.cs
git commit -m "Add DocumentLayoutKind enum (shaper layout rework, phase 0)"
```

---

### Task 2: `DocumentLayout` node with relative-path composition

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/Layout/DocumentLayout.cs`
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/DocumentLayoutTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
/* Copyright header (same Apache 2.0 header as other files) */

using System.Linq;
using FluentAssertions;
using MongoDB.EntityFrameworkCore.Query.Layout;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.Layout;

public static class DocumentLayoutTests
{
    [Fact]
    public static void Root_entity_absolute_path_is_empty_by_default()
    {
        var root = DocumentLayout.ForEntity(relativePath: "");
        root.GetAbsolutePath().Should().Be("");
    }

    [Fact]
    public static void Child_absolute_path_composes_relative_paths_down_the_tree()
    {
        var root = DocumentLayout.ForEntity(relativePath: "");
        var nav = root.AddChild(DocumentLayout.ForNavigation(relativePath: "_lookup_Customer"));
        var thenNav = nav.AddChild(DocumentLayout.ForNavigation(relativePath: "_lookup_Region"));

        thenNav.GetAbsolutePath().Should().Be("_lookup_Customer._lookup_Region");
    }

    [Fact]
    public static void Absolute_path_override_short_circuits_ancestor_composition()
    {
        // The _outer/_inner sibling case: root is "_outer" but the joined reference is a SIBLING
        // under "_inner", not nested under "_outer". The override pins the absolute path.
        var root = DocumentLayout.ForEntity(relativePath: "_outer");
        var nav = root.AddChild(DocumentLayout.ForNavigation(relativePath: "ignored"));
        nav.SetAbsolutePathOverride("_inner");
        var thenNav = nav.AddChild(DocumentLayout.ForNavigation(relativePath: "_lookup_Region"));

        nav.GetAbsolutePath().Should().Be("_inner");
        thenNav.GetAbsolutePath().Should().Be("_inner._lookup_Region");
    }
}
```

- [ ] **Step 2: Run the test to verify it fails to compile (type doesn't exist)**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --filter "FullyQualifiedName~DocumentLayoutTests"`
Expected: FAIL — compile error, `DocumentLayout` not found.

- [ ] **Step 3: Write the implementation**

```csharp
/* Copyright header (same Apache 2.0 header as other files) */

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Query.Layout;

/// <summary>
/// A node in the document-layout tree authored by the query translator. Each node records WHERE in the
/// returned <c>BsonDocument</c> a shaped slot's value physically lives, so the shaper reads a recorded
/// path instead of re-deriving it. Paths are stored relative to the parent; <see cref="GetAbsolutePath"/>
/// composes them down the tree, unless a node carries an <see cref="SetAbsolutePathOverride"/> (used for
/// the driver-native LeftJoin <c>_outer</c>/<c>_inner</c> sibling layout, where a child is NOT nested
/// under its parent's path).
/// </summary>
internal sealed class DocumentLayout
{
    private readonly List<DocumentLayout> _children = [];
    private string? _absolutePathOverride;

    private DocumentLayout(DocumentLayoutKind kind, string relativePath)
    {
        Kind = kind;
        RelativePath = relativePath;
    }

    public DocumentLayoutKind Kind { get; }

    /// <summary>The BSON path of this node relative to its parent ("" means same document as parent).</summary>
    public string RelativePath { get; }

    public DocumentLayout? Parent { get; private set; }

    public IReadOnlyList<DocumentLayout> Children => _children;

    public INavigation? Navigation { get; private set; }

    public static DocumentLayout ForEntity(string relativePath)
        => new(DocumentLayoutKind.Entity, relativePath);

    public static DocumentLayout ForNavigation(string relativePath)
        => new(DocumentLayoutKind.Navigation, relativePath);

    public static DocumentLayout ForCollection(string relativePath)
        => new(DocumentLayoutKind.Collection, relativePath);

    public DocumentLayout WithNavigation(INavigation navigation)
    {
        Navigation = navigation;
        return this;
    }

    public DocumentLayout AddChild(DocumentLayout child)
    {
        child.Parent = this;
        _children.Add(child);
        return child;
    }

    /// <summary>
    /// Pin this node's absolute path, short-circuiting ancestor composition. Used for the driver-native
    /// LeftJoin layout where the joined reference is a sibling (<c>_inner</c>) of the root (<c>_outer</c>),
    /// not nested beneath it.
    /// </summary>
    public void SetAbsolutePathOverride(string absolutePath)
        => _absolutePathOverride = absolutePath;

    /// <summary>
    /// The absolute BSON path to this node, composed from ancestors' relative paths unless an override is set.
    /// </summary>
    public string GetAbsolutePath()
    {
        if (_absolutePathOverride != null)
        {
            return _absolutePathOverride;
        }

        if (Parent == null)
        {
            return RelativePath;
        }

        var parentPath = Parent.GetAbsolutePath();
        if (RelativePath.Length == 0)
        {
            return parentPath;
        }

        return parentPath.Length == 0 ? RelativePath : $"{parentPath}.{RelativePath}";
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --filter "FullyQualifiedName~DocumentLayoutTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Layout/DocumentLayout.cs tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/DocumentLayoutTests.cs
git commit -m "Add DocumentLayout node with path composition (shaper layout rework, phase 0)"
```

---

### Task 3: Carrier property on `MongoQueryExpression`

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQueryExpression.cs:52` (after `CapturedExpression`)

- [ ] **Step 1: Add the property**

After the `CapturedExpression` property (line 52), add:

```csharp
    /// <summary>
    /// The finalized <see cref="Layout.DocumentLayout"/> describing where each shaped slot's value lives
    /// in the returned document. Authored by the translator during projection binding and finalized once
    /// (see <c>FinalizeLayout</c>); read verbatim by the shaper. <see langword="null"/> until authored.
    /// </summary>
    public Layout.DocumentLayout? ResultLayout { get; private set; }

    /// <summary>Set the authored result layout. Called once by the projection-binding visitor.</summary>
    public void SetResultLayout(Layout.DocumentLayout layout)
        => ResultLayout = layout;
```

Add `using MongoDB.EntityFrameworkCore.Query.Layout;` is NOT required because we fully-qualify; leave usings unchanged to minimize diff.

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/MongoDB.EntityFrameworkCore/MongoDB.EntityFrameworkCore.csproj -c "Debug EF10" -v quiet`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQueryExpression.cs
git commit -m "Add ResultLayout carrier to MongoQueryExpression (shaper layout rework, phase 0)"
```

---

## Phase 1a — Produce the layout (build it, don't consume it yet)

The goal of 1a: author the layout from the producer, finalize it once, and add a **debug-only side-by-side assertion** that the layout's path for each cross-collection access equals what the current heuristic (`GetCrossCollectionFieldName`/`GetCrossCollectionRootDocument`/`_outer`) returns. The shaper still uses the heuristic — layout is inert except for the assertion. The suite must stay green.

### Task 4: `FinalizeLayout()` — bake concrete paths from `UsesDriverJoinFields` once

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQueryExpression.Lookup.cs` (add method near `UsesDriverJoinFields`, line 117)

**Context:** `UsesDriverJoinFields` (line 116) is `true` when there is ≥1 inner collection and no `ForceUnwind` lookup. In that mode the root is `_outer` and the lone joined reference is `_inner`; otherwise each cross-collection result is `_lookup_<Nav>` (the alias already baked into the access node's `Name`). `FinalizeLayout` resolves these into concrete absolute-path overrides on the relevant nodes a single time.

- [ ] **Step 1: Write the failing test**

Add to `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/DocumentLayoutTests.cs`:

```csharp
    [Fact]
    public static void Finalize_driver_join_mode_pins_root_to_outer_and_lone_reference_to_inner()
    {
        var root = DocumentLayout.ForEntity(relativePath: "");
        var nav = root.AddChild(DocumentLayout.ForNavigation(relativePath: "_lookup_Customer"));

        DocumentLayout.FinalizeDriverJoinMode(root, loneReference: nav);

        root.GetAbsolutePath().Should().Be("_outer");
        nav.GetAbsolutePath().Should().Be("_inner");
    }
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --filter "FullyQualifiedName~Finalize_driver_join_mode"`
Expected: FAIL — `FinalizeDriverJoinMode` not found.

- [ ] **Step 3: Add the finalizer to `DocumentLayout`**

In `DocumentLayout.cs`, add:

```csharp
    /// <summary>
    /// Apply the driver-native LeftJoin layout: the root entity moves under <c>_outer</c> and the lone
    /// joined reference becomes its sibling under <c>_inner</c> (not nested under <c>_outer</c>). Called
    /// exactly once during query-expression finalization when <c>UsesDriverJoinFields</c> is true.
    /// </summary>
    public static void FinalizeDriverJoinMode(DocumentLayout root, DocumentLayout loneReference)
    {
        root.SetAbsolutePathOverride("_outer");
        loneReference.SetAbsolutePathOverride("_inner");
    }
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --filter "FullyQualifiedName~Finalize_driver_join_mode"`
Expected: PASS.

- [ ] **Step 5: Add `FinalizeLayout()` to `MongoQueryExpression.Lookup.cs`**

After `UsesDriverJoinFields` (line 117), add:

```csharp
    /// <summary>
    /// Resolve the authored <c>ResultLayout</c>'s abstract paths into concrete BSON paths a single time,
    /// using <see cref="UsesDriverJoinFields"/> as the one read of the driver-join decision. In driver-join
    /// mode the root entity is pinned to <c>_outer</c> and the lone reference navigation to <c>_inner</c>;
    /// otherwise the relative <c>_lookup_&lt;Nav&gt;</c> paths already authored stand as-is.
    /// </summary>
    public void FinalizeLayout()
    {
        if (ResultLayout == null || !UsesDriverJoinFields)
        {
            return;
        }

        // Driver-native LeftJoin emits exactly one joined reference (a single inner collection, no
        // forced-unwind). Find that lone Navigation child and pin the _outer/_inner sibling layout.
        var loneReference = ResultLayout.Children
            .FirstOrDefault(c => c.Kind == Layout.DocumentLayoutKind.Navigation);
        if (loneReference != null)
        {
            Layout.DocumentLayout.FinalizeDriverJoinMode(ResultLayout, loneReference);
        }
    }
```

`FinalizeLayout` must be called by the projection-binding visitor after all lookups are registered (wired in Task 6). Add `using System.Linq;` if not already present in the file (it is — line 18).

- [ ] **Step 6: Build to verify it compiles**

Run: `dotnet build src/MongoDB.EntityFrameworkCore/MongoDB.EntityFrameworkCore.csproj -c "Debug EF10" -v quiet`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Layout/DocumentLayout.cs src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQueryExpression.Lookup.cs tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/DocumentLayoutTests.cs
git commit -m "Add FinalizeLayout for driver-join mode (shaper layout rework, phase 1a)"
```

---

### Task 5: Author layout nodes in the producer

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs` and `.Lookup.cs`

**Context:** The producer walks the include tree and creates `ObjectAccessExpression`/`LookupExpression` nodes whose `Name`/`As` carry the `_lookup_<Nav>` alias, and registers them on `MongoQueryExpression` via `AddLookup`/`AddInnerCollection`. We add a parallel `DocumentLayout` node at each of those sites, then call `FinalizeLayout()` once at the end.

- [ ] **Step 1: Add a layout-building field and root node**

In `MongoProjectionBindingExpressionVisitor.cs`, add a field:

```csharp
    private Layout.DocumentLayout? _layoutRoot;
    private readonly Dictionary<Expressions.ObjectAccessExpression, Layout.DocumentLayout> _layoutByAccess = new();
```

Where the root `EntityProjectionExpression`/`RootReferenceExpression` for the query root is established, initialize:

```csharp
    _layoutRoot = Layout.DocumentLayout.ForEntity(relativePath: "");
```

- [ ] **Step 2: Author a node at each cross-collection placement**

At the site where the producer assigns a cross-collection `ObjectAccessExpression`'s alias / registers its lookup (the `_lookup_<Nav>` alias decision in `.Lookup.cs`), add — using the navigation and the alias already computed there as `as`:

```csharp
    // Author the layout node mirroring this placement decision. Path is the _lookup_<Nav> alias
    // (relative to the parent document); driver-join _outer/_inner is resolved later in FinalizeLayout.
    var parentLayout = parentAccess != null && _layoutByAccess.TryGetValue(parentAccess, out var p)
        ? p
        : _layoutRoot!;
    var layoutNode = navigation.IsCollection
        ? Layout.DocumentLayout.ForCollection(relativePath: lookupAlias).WithNavigation(navigation)
        : Layout.DocumentLayout.ForNavigation(relativePath: lookupAlias).WithNavigation(navigation);
    parentLayout.AddChild(layoutNode);
    _layoutByAccess[crossCollectionAccess] = layoutNode;
```

(`lookupAlias`, `navigation`, `parentAccess`, and `crossCollectionAccess` are the local variables already present at that placement site; if the local names differ, use the existing ones — do not introduce new computations.)

- [ ] **Step 3: Publish + finalize the layout**

At the end of the producer's top-level visit (after all includes/lookups are processed, before returning), add:

```csharp
    if (_layoutRoot != null)
    {
        _queryExpression.SetResultLayout(_layoutRoot);
        _queryExpression.FinalizeLayout();
    }
```

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build src/MongoDB.EntityFrameworkCore/MongoDB.EntityFrameworkCore.csproj -c "Debug EF10" -v quiet`
Expected: PASS.

- [ ] **Step 5: Run the full EF10 suite — layout is inert, must stay green**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build`
Expected: same green counts as Task 0. (Layout is built but not consumed; behavior is unchanged.)

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.Lookup.cs
git commit -m "Author DocumentLayout from the projection-binding producer (shaper layout rework, phase 1a)"
```

---

### Task 6: Debug-only side-by-side assertion (layout vs heuristic)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingRemovingExpressionVisitor.cs:294-295` and `:581-584`

**Context:** This is the safety net that lets Phase 1b delete the heuristic with confidence. At each cross-collection resolution site, assert (under `#if DEBUG`) that the layout's absolute path equals the heuristic's computed path. The heuristic still drives behavior in 1a.

- [ ] **Step 1: Add a debug assertion helper**

In `MongoProjectionBindingRemovingExpressionVisitor.cs`, add:

```csharp
    [System.Diagnostics.Conditional("DEBUG")]
    private void AssertLayoutMatchesHeuristic(
        Expressions.ObjectAccessExpression crossCollectionAccess, string heuristicFieldName)
    {
        var layout = _queryExpression.ResultLayout;
        if (layout == null)
        {
            return;
        }

        var node = FindLayoutNode(layout, crossCollectionAccess.Navigation);
        if (node == null)
        {
            return; // not all accesses have a node in 1a; gaps become hard-fails in 1b
        }

        var layoutLeaf = node.GetAbsolutePath();
        // Compare only the final path segment the heuristic produced (it returns a single field name).
        var lastSegment = layoutLeaf.Contains('.') ? layoutLeaf[(layoutLeaf.LastIndexOf('.') + 1)..] : layoutLeaf;
        System.Diagnostics.Debug.Assert(
            string.Equals(lastSegment, heuristicFieldName, StringComparison.Ordinal),
            $"Layout path '{layoutLeaf}' (leaf '{lastSegment}') disagrees with heuristic field '{heuristicFieldName}' "
            + $"for navigation '{crossCollectionAccess.Navigation?.Name}'.");
    }

    private static Layout.DocumentLayout? FindLayoutNode(Layout.DocumentLayout node, INavigation? navigation)
    {
        if (navigation != null && ReferenceEquals(node.Navigation, navigation))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = FindLayoutNode(child, navigation);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
```

- [ ] **Step 2: Call the assertion at the two cross-collection sites**

At line 295 (inside `VisitBinary`, the `ObjectAccessExpression crossCollectionAccess` case), immediately after `fieldName = GetCrossCollectionFieldName(crossCollectionAccess);` add:

```csharp
                                AssertLayoutMatchesHeuristic(crossCollectionAccess, fieldName);
```

At line 581-584 (inside `CreateGetValueExpression`'s switch), refactor the inline call so the field name is named, then assert:

```csharp
                ObjectAccessExpression crossCollectionAccess when IsCrossCollectionAccess(crossCollectionAccess)
                    => CreateGetValueExpression(
                        GetCrossCollectionRootDocument(crossCollectionAccess),
                        GetCrossCollectionFieldNameAsserted(crossCollectionAccess), false, typeof(BsonDocument)),
```

and add the helper:

```csharp
    private string GetCrossCollectionFieldNameAsserted(Expressions.ObjectAccessExpression crossCollectionAccess)
    {
        var fieldName = GetCrossCollectionFieldName(crossCollectionAccess);
        AssertLayoutMatchesHeuristic(crossCollectionAccess, fieldName);
        return fieldName;
    }
```

- [ ] **Step 3: Build DEBUG and run the full EF10 suite**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" -v quiet && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build`
Expected: green, AND no `Debug.Assert` dialog/failure. Any assertion failure here is a real layout/heuristic divergence — **fix the producer (Task 5) until the suite is green with assertions live.** This is the gate to Phase 1b.

- [ ] **Step 4: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingRemovingExpressionVisitor.cs
git commit -m "Add debug side-by-side layout-vs-heuristic assertion (shaper layout rework, phase 1a)"
```

---

### Task 7: Layout-tree shape unit tests for the include-shape matrix

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/DocumentLayoutBuilderTests.cs`

**Context:** Assert the authored layout tree for representative shapes — the new regression surface that does not exist today. Use the unit-test project's existing model-building helpers to compile a query and inspect `MongoQueryExpression.ResultLayout`. If the unit-test project cannot reach a translated `MongoQueryExpression` without a DB, place these in the functional project instead and gate on the same matrix; prefer unit if a translation-only entry point exists (check `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/` for the existing compile-without-execute pattern before writing).

- [ ] **Step 1: Write the matrix tests**

For each shape below, assert the layout tree's absolute paths. Cover: (a) no-include single entity → root path `""`; (b) single reference Include in driver-join mode → root `_outer`, nav `_inner`; (c) reference + ThenInclude reference in driver-join → `_outer`, `_inner`, `_inner._lookup_<Then>`; (d) collection Include in flat mode → root `""`, collection `_lookup_<Coll>`; (e) the self-ref shape from the EF-117 notes (`Multiple_complex_includes_self_ref`). Write one `[Fact]` per shape asserting `node.GetAbsolutePath()` for each navigation. (Use the actual model entities available in the test project; mirror the existing query unit-test setup.)

- [ ] **Step 2: Run the matrix tests**

Run: `dotnet test ... --filter "FullyQualifiedName~DocumentLayoutBuilderTests"`
Expected: PASS for every shape. A failing shape means the producer authored the wrong path — fix Task 5.

- [ ] **Step 3: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/DocumentLayoutBuilderTests.cs
git commit -m "Add layout-tree shape tests for the include matrix (shaper layout rework, phase 1a)"
```

---

## Phase 1b — Consume the layout & delete the heuristic

### Task 8: Switch the consumer to read layout paths

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingRemovingExpressionVisitor.cs`

**Context:** Replace the heuristic reads with layout reads at the enumerated sites. The layout node for a cross-collection access is found by navigation (`FindLayoutNode` from Task 6). Its `GetAbsolutePath()` is the BSON path to read from; the root document is `DocParameter`, except for a nested-under-collection-element reference, where the bound element document is used (the `GetCrossCollectionRootDocument` redirection — preserved, but keyed off the layout's parent kind being `Collection`).

- [ ] **Step 1: Add a layout-path resolver replacing `GetCrossCollectionFieldName`**

```csharp
    /// <summary>
    /// Resolve, from the authored layout, the (document, field) a cross-collection access reads from.
    /// Replaces the GetCrossCollectionFieldName/GetCrossCollectionRootDocument/UsesDriverJoinFields heuristic.
    /// </summary>
    private (Expression Document, string Field) ResolveCrossCollectionLayout(
        Expressions.ObjectAccessExpression crossCollectionAccess)
    {
        var layout = _queryExpression.ResultLayout
            ?? throw new InvalidOperationException("Query has no authored ResultLayout.");
        var node = FindLayoutNode(layout, crossCollectionAccess.Navigation)
            ?? throw new InvalidOperationException(
                $"No layout recorded for navigation '{crossCollectionAccess.Navigation?.Name}'.");

        var absolute = node.GetAbsolutePath();
        var field = absolute.Contains('.') ? absolute[(absolute.LastIndexOf('.') + 1)..] : absolute;

        // Nested under a collection element: read the joined field from the bound element document.
        if (node.Parent is { Kind: Layout.DocumentLayoutKind.Collection }
            && crossCollectionAccess.AccessExpression is RootReferenceExpression
            && _projectionBindings.TryGetValue(crossCollectionAccess.AccessExpression, out var elementDoc))
        {
            return (elementDoc, field);
        }

        return (DocParameter, field);
    }
```

- [ ] **Step 2: Rewrite the `VisitBinary` cross-collection case (lines 294-296)**

Replace:

```csharp
                                innerAccessExpression = GetCrossCollectionRootDocument(crossCollectionAccess);
                                fieldName = GetCrossCollectionFieldName(crossCollectionAccess);
                                fieldRequired = false;
```

with:

```csharp
                                var resolved = ResolveCrossCollectionLayout(crossCollectionAccess);
                                innerAccessExpression = resolved.Document;
                                fieldName = resolved.Field;
                                fieldRequired = false;
```

- [ ] **Step 3: Rewrite the root-entity `_outer` case (lines 307-311)**

Replace the `RootReferenceExpression when _queryExpression.UsesDriverJoinFields` case with a read of the layout root's absolute path:

```csharp
                            case RootReferenceExpression when _queryExpression.ResultLayout?.GetAbsolutePath() is { Length: > 0 } rootPath:
                                // Layout pinned the root (driver-native LeftJoin → "_outer").
                                innerAccessExpression = DocParameter;
                                fieldName = rootPath;
                                break;
```

- [ ] **Step 4: Rewrite `CreateGetValueExpression`'s switch (lines 575-584)**

Replace the `RootReferenceExpression when _queryExpression.UsesDriverJoinFields` arm and the cross-collection arm:

```csharp
                RootReferenceExpression when _queryExpression.ResultLayout?.GetAbsolutePath() is { Length: > 0 } rootPath
                    => CreateGetValueExpression(DocParameter, rootPath, required, typeof(BsonDocument)),
                RootReferenceExpression => CreateGetValueExpression(DocParameter, null, required, typeof(BsonDocument)),
                ObjectAccessExpression crossCollectionAccess when IsCrossCollectionAccess(crossCollectionAccess)
                    => ResolveCrossCollectionLayoutExpression(crossCollectionAccess),
```

and add:

```csharp
    private Expression ResolveCrossCollectionLayoutExpression(Expressions.ObjectAccessExpression crossCollectionAccess)
    {
        var resolved = ResolveCrossCollectionLayout(crossCollectionAccess);
        return CreateGetValueExpression(resolved.Document, resolved.Field, false, typeof(BsonDocument));
    }
```

- [ ] **Step 5: Build + run full EF10 suite (heuristic and layout both present; layout now drives)**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" -v quiet && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build`
Expected: same green counts as Task 0. MQL is frozen, so any green→red is a real regression — debug via the failing query's layout tree (Task 7 style).

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingRemovingExpressionVisitor.cs
git commit -m "Consume DocumentLayout in the shaper (shaper layout rework, phase 1b)"
```

---

### Task 9: Delete the heuristic and the debug assertion; drop the mixed-visitor flag read

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingRemovingExpressionVisitor.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoMixedProjectionBindingRemovingExpressionVisitor.cs:107-111`

- [ ] **Step 1: Delete the dead heuristic methods and helpers**

Delete `GetCrossCollectionFieldName` (505-506), `GetCrossCollectionRootDocument` (516-530), `GetCrossCollectionFieldNameAsserted` and `AssertLayoutMatchesHeuristic` (from Task 6). Keep `FindLayoutNode` (now used by `ResolveCrossCollectionLayout`) and `IsCrossCollectionAccess`.

- [ ] **Step 2: Replace the mixed visitor's `UsesDriverJoinFields` redirect (lines 107-111)**

In `MongoMixedProjectionBindingRemovingExpressionVisitor.cs`, the block that redirects root scalar reads to `_outer` when `UsesDriverJoinFields`:

```csharp
                    var docExpr = fieldAccess.DocumentExpression ?? _docParameter;
                    if (_queryExpression.UsesDriverJoinFields
                        && ReferenceEquals(docExpr, _docParameter))
                    {
                        docExpr = CreateGetValueExpression(_docParameter, "_outer", true, typeof(BsonDocument));
                    }
```

Replace with a read of the layout root path:

```csharp
                    var docExpr = fieldAccess.DocumentExpression ?? _docParameter;
                    if (_queryExpression.ResultLayout?.GetAbsolutePath() is { Length: > 0 } rootPath
                        && ReferenceEquals(docExpr, _docParameter))
                    {
                        docExpr = CreateGetValueExpression(_docParameter, rootPath, true, typeof(BsonDocument));
                    }
```

- [ ] **Step 3: Build + run full EF10 suite**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" -v quiet && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build`
Expected: same green counts as Task 0.

- [ ] **Step 4: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingRemovingExpressionVisitor.cs src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoMixedProjectionBindingRemovingExpressionVisitor.cs
git commit -m "Delete field-location heuristic; shaper reads layout only (shaper layout rework, phase 1b)"
```

---

### Task 10: Grep-guard regression test

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/ShaperHasNoFieldHeuristicTest.cs`

**Context:** Enforce the invariant from the spec: the consumer contains no `_inner`/`_outer`/`_lookup_` string literals and no `UsesDriverJoinFields` reads. This is a source-level guard so the heuristic can't creep back.

- [ ] **Step 1: Write the guard test**

```csharp
/* Copyright header */

using System.IO;
using FluentAssertions;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.Layout;

public static class ShaperHasNoFieldHeuristicTest
{
    [Fact]
    public static void Removing_visitor_has_no_field_location_heuristic()
    {
        // Resolve the source file relative to the test assembly's repo checkout.
        var path = TestSourceLocator.Resolve(
            "src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingRemovingExpressionVisitor.cs");
        var source = File.ReadAllText(path);

        source.Should().NotContain("UsesDriverJoinFields",
            "the shaper must read paths from ResultLayout, not recompute the driver-join flag");
        source.Should().NotContain("\"_inner\"");
        source.Should().NotContain("\"_outer\"");
        source.Should().NotContain("GetCrossCollectionFieldName");
        source.Should().NotContain("GetCrossCollectionRootDocument");
    }
}
```

If no `TestSourceLocator` helper exists, inline the path resolution using `AppContext.BaseDirectory` walked up to the repo root (check `tests/.../UnitTests/` for an existing repo-root helper first; reuse it).

- [ ] **Step 2: Run the guard test**

Run: `dotnet test ... --filter "FullyQualifiedName~ShaperHasNoFieldHeuristicTest"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/ShaperHasNoFieldHeuristicTest.cs
git commit -m "Add grep-guard test against field-location heuristic regressions (shaper layout rework, phase 1b)"
```

---

### Task 11: EF-117 case-file regression tests

**Files:**
- Create/extend: a focused layout test per EF-117 problem case in `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/DocumentLayoutBuilderTests.cs`

**Context:** Turn the wrong-data/missing-element cases from the ComplexNav notes into layout-tree assertions so they cannot silently regress: `Multiple_complex_includes_self_ref`, `Include_collection_multiple`, the X023 (`Include_collection_then_reference`, `Include_collection_ThenInclude_two_references`) and X024 (reference-Include-missing-element) clusters.

- [ ] **Step 1: Add one layout assertion per case**

For each case, build the query's model shape and assert the authored layout paths match the intended document layout (root path; each navigation's absolute path; nested-reference parent kind is `Collection` where applicable). Write the expected paths explicitly per case — do not share a parameterized expectation, so a regression names the exact case.

- [ ] **Step 2: Run them**

Run: `dotnet test ... --filter "FullyQualifiedName~DocumentLayoutBuilderTests"`
Expected: PASS for all cases.

- [ ] **Step 3: Run the full EF10 suite one final time**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build`
Expected: same green counts as Task 0. This is the Phase 1b completion gate.

- [ ] **Step 4: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Layout/DocumentLayoutBuilderTests.cs
git commit -m "Add EF-117 case-file layout regression tests (shaper layout rework, phase 1b)"
```

---

## Done criteria (this spec)

- `DocumentLayout` authored by the producer, finalized once, consumed by the shaper.
- `GetCrossCollectionFieldName`, `GetCrossCollectionRootDocument`, and all `UsesDriverJoinFields` reads in the consumer/mixed visitor deleted; grep-guard test green.
- Full EF10 suite green with identical MQL baselines (behavior-preserving).
- Layout-tree unit tests cover the include matrix and the EF-117 case file.

## Out of scope (follow-on specs)

- EF8/EF9 backport behind `#if` guards.
- Phase 2: collapse the three shaper paths, normalize layout paths (MQL may change), and add GroupBy / complex-type / set-op layout kinds.
