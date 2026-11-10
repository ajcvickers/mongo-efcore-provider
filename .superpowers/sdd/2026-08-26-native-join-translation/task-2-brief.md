### Task 2: Add a join-scope marker to `MongoSelectDefinition`

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs:394` (`HasTerminalOperator`) and add a new property/setter near the existing `UnwindSources`/`UnwindSource` block (`MongoSelectDefinition.cs:460-480`)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoSelectDefinitionTests.cs` (add cases; if this file doesn't exist yet, check `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/` for the closest existing sibling — e.g. `MongoSelectDefinitionTrailingOpsTests.cs` — and follow its class/fixture pattern)

**Interfaces:**
- Produces:
  - `internal sealed class MongoJoinScope(string? outerMemberName, string? innerMemberName, IEntityType outerEntityType, IEntityType innerEntityType, string innerPrefix, bool isLeftOuter)` — records which named member (if any) of a join's result-selector anonymous type captures the whole outer/inner entity, so a later operator's lambda can resolve `x.<name>.<Foo>` by parameter identity through the shared `MongoTransparentScopeResolver`. `outerMemberName`/`innerMemberName` are `null` when that side was not captured whole (already flattened to scalar leaves at Join time — no scope tracking needed for that side).
  - `internal MongoJoinScope? JoinScope { get; set; }` on `MongoSelectDefinition`.
  - `HasTerminalOperator` becomes `IsGroupBy || IsDistinct || IsSetOp || Grouping != null || UnwindSources.Count > 0 || JoinScope != null`.
- Consumes: nothing new (this task only adds state; Task 3 populates it).

- [ ] **Step 1: Write a failing unit test asserting `HasTerminalOperator` becomes true once `JoinScope` is set**

```csharp
// Add to (or create) tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoSelectDefinitionTests.cs
using Microsoft.EntityFrameworkCore.Metadata;
using Moq; // or the repo's existing IEntityType test-double pattern — check MongoSelectDefinitionTrailingOpsTests.cs
using MongoDB.EntityFrameworkCore.Query.Expressions;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions;

public class MongoSelectDefinitionJoinScopeTests
{
    [Fact]
    public void HasTerminalOperator_is_true_once_a_join_scope_is_set()
    {
        var select = new MongoSelectDefinition();
        Assert.False(select.HasTerminalOperator);

        var outerEntityType = Mock.Of<IEntityType>();
        var innerEntityType = Mock.Of<IEntityType>();
        select.JoinScope = new MongoJoinScope(
            outerMemberName: "o", innerMemberName: "i",
            outerEntityType, innerEntityType, innerPrefix: "_lookup_Orders", isLeftOuter: false);

        Assert.True(select.HasTerminalOperator);
    }
}
```

(If the constructor for `MongoSelectDefinition` isn't parameterless, check the actual constructor in `MongoSelectDefinition.cs` and adjust — other tests in the same directory show the real construction pattern to follow.)

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoSelectDefinitionJoinScopeTests"`
Expected: build error, `MongoJoinScope` does not exist / `JoinScope` property does not exist.

- [ ] **Step 3: Create `MongoJoinScope`**

```csharp
// src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs
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

using Microsoft.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// Records a native two-sided <c>Join</c>/<c>LeftJoin</c>'s scope shape, for a result selector that captures a
/// whole side (<c>(o, i) =&gt; new { o, i }</c>) rather than only already-flattened scalar leaves. When a side
/// is captured whole, a later <c>Where</c>/<c>OrderBy</c>/<c>Select</c> lambda's member access on that named
/// member (e.g. <c>x.o.Name</c>) must resolve through <see cref="MongoTransparentScopeResolver"/> against the
/// matching entity type, exactly like SelectMany's <c>ti.Outer</c>/<c>ti.Inner</c> — except the member names
/// here are whatever the user's own anonymous-type/DTO member names were, not fixed literals.
/// </summary>
internal sealed class MongoJoinScope(
    string? outerMemberName, string? innerMemberName,
    IEntityType outerEntityType, IEntityType innerEntityType, string innerPrefix, bool isLeftOuter)
{
    /// <summary>The result-selector member name capturing the whole OUTER entity, or <see langword="null"/>
    /// if the outer side was already flattened to scalar leaves at Join time (no scope tracking needed).</summary>
    public string? OuterMemberName { get; } = outerMemberName;

    /// <summary>The result-selector member name capturing the whole INNER entity, or <see langword="null"/>
    /// if the inner side was already flattened to scalar leaves at Join time.</summary>
    public string? InnerMemberName { get; } = innerMemberName;

    public IEntityType OuterEntityType { get; } = outerEntityType;

    public IEntityType InnerEntityType { get; } = innerEntityType;

    /// <summary>The <c>$lookup</c> alias (<c>_lookup_&lt;Navigation&gt;</c>) inner-scope field refs are
    /// prefixed with — mirrors <see cref="MongoUnwindSource.InnerScopePath"/>.</summary>
    public string InnerPrefix { get; } = innerPrefix;

    /// <summary>Whether this join is left-outer (<c>LeftJoin</c>/<c>GroupJoin</c>) or inner (<c>Join</c>) —
    /// drives the emitted <c>$unwind</c>'s <c>preserveNullAndEmptyArrays</c>.</summary>
    public bool IsLeftOuter { get; } = isLeftOuter;
}
```

- [ ] **Step 4: Add the `JoinScope` property and extend `HasTerminalOperator` on `MongoSelectDefinition`**

In `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs`, near the existing `UnwindSources`/`UnwindSource` block (around line 466-480):

```csharp
/// <summary>The native two-sided join scope recorded by <c>MongoJoinBinder</c>, or <see langword="null"/>
/// if this select has no native join, or its join's result selector was already fully flattened to
/// scalar leaves (no whole-side capture, so no scope tracking is needed).</summary>
internal MongoJoinScope? JoinScope { get; set; }
```

And change line 394:

```csharp
internal bool HasTerminalOperator
    => IsGroupBy || IsDistinct || IsSetOp || Grouping != null || UnwindSources.Count > 0 || JoinScope != null;
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoSelectDefinitionJoinScopeTests"`
Expected: PASS.

- [ ] **Step 6: Build all three EF configs and commit**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git add src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs \
        src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoSelectDefinitionJoinScopeTests.cs
git commit -m "EF-392: add MongoJoinScope and extend HasTerminalOperator for native joins"
```

**Coordination note:** before this commit, run `git diff main -- src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs` (or the equivalent against whatever the EF-411 agent has landed on `main`/the shared branch by now) to check for a conflicting edit to `HasTerminalOperator` or the surrounding region, and rebase this one-line change on top if so, rather than force-merging.

---

