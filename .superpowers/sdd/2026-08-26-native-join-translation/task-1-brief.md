### Task 1: Generalize `NativeSelectManyBinder`'s scope-rerooting walker into a shared resolver

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoTransparentScopeResolver.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSelectManyBinder.cs:860-921` (the `ScopeRerootingVisitor` class and `TryResolveScopeDepth` method)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoTransparentScopeResolverTests.cs` (new)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeSelectManyBinderTests.cs` (regression check only, no new tests required)

**Interfaces:**
- Produces:
  - `internal static bool TryResolveScopeDepth(Expression? scopeAccess, ParameterExpression rootParam, IReadOnlyList<string> hopNames, out int scopeIndex)` on `MongoTransparentScopeResolver` — generalizes the old fixed `"Outer"`/`"Inner"` walk to an arbitrary ordered list of hop member names (SelectMany passes the two-element list `["Outer", "Inner"]` repeatedly per nesting level, matching today's behavior exactly).
  - `internal sealed class ScopeRerootingVisitor(ParameterExpression rootParam, IReadOnlyList<string> hopNames, int sourceCount, ParameterExpression[] scopeParams) : ExpressionVisitor` — moved from `NativeSelectManyBinder`, same `ResolvedScope`/`CrossScope` public surface, now parameterized on `hopNames` instead of the literal strings.
- Consumes: nothing new — pure refactor of existing private members.

This task is deliberately small and lands as its own commit **before** any join-specific work, to minimize diff overlap with the concurrent EF-411 agent's changes to `MongoSelectDefinition.cs` (post-terminal guards) — this task does not touch that file at all.

- [ ] **Step 1: Read the current `TryResolveScopeDepth` and `ScopeRerootingVisitor` to confirm the exact behavior being preserved**

Already confirmed by reading `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSelectManyBinder.cs:860-921`. The walk today does: peel a chain of `MemberExpression { Member.Name: "Outer" or "Inner" }` down to the bare `ti` parameter, reverse it, and classify as either `sourceCount` trailing-`"Outer"` hops (root, scope 0) or `(sourceCount - k)` leading `"Outer"` hops followed by one trailing `"Inner"` (scope `k`).

- [ ] **Step 2: Write a failing unit test for the generalized resolver**

```csharp
// tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoTransparentScopeResolverTests.cs
using System.Linq.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

public class MongoTransparentScopeResolverTests
{
    private sealed class Ti
    {
        public object Outer { get; set; } = null!;
        public object Inner { get; set; } = null!;
    }

    [Fact]
    public void Resolves_root_scope_for_pure_Outer_chain_matching_source_count()
    {
        var ti = Expression.Parameter(typeof(Ti), "ti");
        Expression access = Expression.Property(Expression.Property(ti, "Outer"), "Outer");

        var resolved = MongoTransparentScopeResolver.TryResolveScopeDepth(
            access, ti, hopNames: ["Outer", "Inner"], sourceCount: 2, out var scopeIndex);

        Assert.True(resolved);
        Assert.Equal(0, scopeIndex);
    }

    [Fact]
    public void Resolves_inner_scope_for_trailing_Inner_hop()
    {
        var ti = Expression.Parameter(typeof(Ti), "ti");
        Expression access = Expression.Property(Expression.Property(ti, "Outer"), "Inner");

        var resolved = MongoTransparentScopeResolver.TryResolveScopeDepth(
            access, ti, hopNames: ["Outer", "Inner"], sourceCount: 2, out var scopeIndex);

        Assert.True(resolved);
        Assert.Equal(1, scopeIndex);
    }

    [Fact]
    public void Supports_arbitrary_hop_names_for_a_join_scope()
    {
        var x = Expression.Parameter(typeof(Ti), "x");
        Expression access = Expression.Property(x, "Outer"); // reuse Ti shape; real Join uses caller-chosen names

        var resolved = MongoTransparentScopeResolver.TryResolveScopeDepth(
            access, x, hopNames: ["Outer"], sourceCount: 1, out var scopeIndex);

        Assert.True(resolved);
        Assert.Equal(1, scopeIndex);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails (type doesn't exist yet)**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoTransparentScopeResolverTests"`
Expected: build error, `MongoTransparentScopeResolver` does not exist.

- [ ] **Step 4: Create `MongoTransparentScopeResolver` by lifting the two members out of `NativeSelectManyBinder`, generalized on `hopNames`**

```csharp
// src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoTransparentScopeResolver.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Shared parameter-identity scope resolution for a nested "transparent identifier"-shaped parameter — a
/// single lambda parameter whose member-access chain (e.g. <c>ti.Outer.Inner</c>) walks down through a fixed
/// set of named hops to one of several logical scopes. Originally built for <c>SelectMany</c>'s
/// <c>TransparentIdentifier(Outer, Inner)</c> shape (hop names always literally <c>"Outer"</c>/<c>"Inner"</c>);
/// generalized to a caller-supplied <paramref name="hopNames"/> list so <c>Join</c>'s own (possibly
/// user-named) two-scope result selector can reuse the identical walk. See the scope-by-parameter-identity
/// invariant in <c>Query/AGENTS.md</c> — resolution is always by parameter identity, never by member name.
/// </summary>
internal static class MongoTransparentScopeResolver
{
    /// <summary>
    /// Peels a chain of member accesses named from <paramref name="hopNames"/> down to the bare
    /// <paramref name="rootParam"/> parameter, and resolves which scope it refers to. Given
    /// <paramref name="sourceCount"/> chained scopes, the <c>k</c>-th level's own element is reached via
    /// <c>(sourceCount - k)</c> leading <c>hopNames[0]</c> hops followed by exactly one trailing
    /// <c>hopNames[1]</c> hop; the root scope is reached via exactly <paramref name="sourceCount"/>
    /// <c>hopNames[0]</c> hops and no <c>hopNames[1]</c> at all. <paramref name="scopeIndex"/> is <c>0</c> for
    /// the root, or <c>k</c> (1-based) for the <c>k</c>-th nested scope. Returns <see langword="false"/> —
    /// declining cleanly — for any chain that does not terminate exactly at <paramref name="rootParam"/>, is
    /// empty, exceeds <paramref name="sourceCount"/> hops, or does not match either valid shape.
    /// </summary>
    internal static bool TryResolveScopeDepth(
        Expression? scopeAccess, ParameterExpression rootParam, IReadOnlyList<string> hopNames, int sourceCount,
        out int scopeIndex)
    {
        scopeIndex = -1;
        var outerHop = hopNames[0];
        var innerHop = hopNames[1];

        var path = new List<string>();
        var current = scopeAccess;
        while (current is MemberExpression { Member.Name: { } name } hop && (name == outerHop || name == innerHop))
        {
            path.Add(name);
            current = hop.Expression;
        }

        if (current != rootParam || path.Count == 0 || path.Count > sourceCount)
            return false;

        path.Reverse(); // now ordered outward-from-root: path[0] is the first hop off rootParam.

        if (path[^1] == innerHop && path.Take(path.Count - 1).All(h => h == outerHop))
        {
            scopeIndex = sourceCount - path.Count + 1;
            return true;
        }

        if (path.Count == sourceCount && path.All(h => h == outerHop))
        {
            scopeIndex = 0;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Rewrites every scope-rooted member access in an expression onto the matching per-scope synthetic
    /// parameter, recording the single scope it resolves to (or flagging <see cref="CrossScope"/> if operands
    /// span more than one). A non-scope-rooted member is left untouched.
    /// </summary>
    internal sealed class ScopeRerootingVisitor(
        ParameterExpression rootParam, IReadOnlyList<string> hopNames, int sourceCount, ParameterExpression[] scopeParams)
        : ExpressionVisitor
    {
        public int? ResolvedScope { get; private set; }
        public bool CrossScope { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (TryResolveScopeDepth(node.Expression, rootParam, hopNames, sourceCount, out var scope))
            {
                if (ResolvedScope is { } prior && prior != scope)
                    CrossScope = true;
                ResolvedScope = scope;
                return Expression.MakeMemberAccess(scopeParams[scope], node.Member);
            }

            return base.VisitMember(node);
        }
    }
}
```

- [ ] **Step 5: Update `NativeSelectManyBinder` to delegate to the shared resolver instead of its own private copies**

Replace the private `TryResolveScopeDepth` method (lines 892-921) and the private `ScopeRerootingVisitor` class (lines 860-878) entirely — delete them. Update every call site in `NativeSelectManyBinder.cs` that referenced them:

```csharp
// Every call like:
//   TryResolveScopeDepth(member.Expression, ti, sources.Count, out var scopeIndex)
// becomes:
MongoTransparentScopeResolver.TryResolveScopeDepth(
    member.Expression, ti, hopNames: ["Outer", "Inner"], sources.Count, out var scopeIndex)

// Every construction of the old ScopeRerootingVisitor like:
//   var scopes = new ScopeRerootingVisitor(ti, sources.Count, scopeParams);
// becomes:
var scopes = new MongoTransparentScopeResolver.ScopeRerootingVisitor(
    ti, hopNames: ["Outer", "Inner"], sources.Count, scopeParams);
```

Apply this at all four call sites: `TryBindTransparentIdentifierProjection`'s member loop, `TryTranslateCrossScopeComputedLeaf`, `TryTranslateScopedOperand`, and `TryTranslateScopedSubtree`.

- [ ] **Step 6: Run the new unit test and the full SelectMany test suites to confirm zero behavior change**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoTransparentScopeResolverTests|FullyQualifiedName~NativeSelectManyBinderTests"`
Expected: PASS, all green.

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeSelectManyTests"`
Expected: PASS, all green, identical MQL to before (this is a pure refactor).

- [ ] **Step 7: Build all three EF configs and commit**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoTransparentScopeResolver.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSelectManyBinder.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoTransparentScopeResolverTests.cs
git commit -m "EF-392: extract MongoTransparentScopeResolver from NativeSelectManyBinder for reuse by Join"
```

---

