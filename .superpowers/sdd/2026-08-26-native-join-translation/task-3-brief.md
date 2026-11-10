### Task 3: `MongoJoinBinder` — eligibility, native `$lookup` registration, and scope classification

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs`
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoJoinBinderTests.cs` (new)

**Interfaces:**
- Consumes:
  - `MongoTransparentScopeResolver.TryResolveScopeDepth`/`ScopeRerootingVisitor` (Task 1).
  - `MongoJoinScope` (Task 2).
  - Existing `JoinInfo` (`Navigation`, `Alias`, `IsLeftOuter` — `src/MongoDB.EntityFrameworkCore/Query/Expressions/JoinInfo.cs`), `LookupExpression(INavigation, bool forceUnwind)` (`src/MongoDB.EntityFrameworkCore/Query/Expressions/LookupExpression.cs:37`), `MongoQueryExpression.AddLookup` (`Expressions/MongoQueryExpression.Lookup.cs`), `MongoSelectDefinition.IsBareCollectionScan`/`IsGroupBy`/`IsDistinct` (existing).
- Produces:
  - `internal static bool TryBindJoin(MongoQueryExpression outerQueryExpression, MongoQueryExpression innerQueryExpression, LambdaExpression resultSelector, JoinInfo joinInfo)` — called from `TranslateJoinCore` (Task 4) immediately after `RebindInnerShaperToOuterQuery` returns a non-null shaper (i.e. the join itself is representable at all) and before the final `newResultSelector` substitution. Returns `false` (no mutation) for any shape outside this chunk's scope, matching the existing graceful-fallback contract for `Join` (never a hard decline — `Join` always has a working driver-LINQ bridge).

- [ ] **Step 1: Write failing unit tests for the two core decisions `MongoJoinBinder` must make: eligibility, and result-selector classification**

```csharp
// tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoJoinBinderTests.cs
using System.Linq.Expressions;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

// Uses this project's existing helpers for building a minimal IModel/IEntityType/MongoQueryExpression fixture —
// follow the construction pattern in NativeSelectManyBinderTests.cs (same directory) for the exact
// ModelBuilder/OnModelCreating setup this project already uses to get real IEntityType instances rather than
// mocking IEntityType by hand, since MongoExpressionTranslator reads real IProperty/IEntityType metadata.
public class MongoJoinBinderTests
{
    [Fact]
    public void Declines_when_outer_select_is_group_by()
    {
        // Arrange a MongoQueryExpression whose Select.IsGroupBy is true (mirrors the existing
        // TranslateJoinCore GroupBy-source guard already covered by NativeSelectManyBinder-adjacent tests).
        // Assert MongoJoinBinder.TryBindJoin returns false and mutates nothing (Select.JoinScope stays null).
    }

    [Fact]
    public void Declines_when_join_has_no_resolved_navigation()
    {
        // joinInfo.Navigation is null (key-equality join with no model navigation) — out of scope for this
        // chunk. Assert TryBindJoin returns false.
    }

    [Fact]
    public void Classifies_whole_side_capture_and_sets_JoinScope()
    {
        // resultSelector: (o, i) => new { o, i } — both parameters captured whole, by reference identity.
        // Assert TryBindJoin returns true, and Select.JoinScope.OuterMemberName == "o" &&
        // Select.JoinScope.InnerMemberName == "i".
    }

    [Fact]
    public void Already_flattened_result_selector_needs_no_join_scope()
    {
        // resultSelector: (o, i) => new { o.Name, i.Total } — no whole-side capture.
        // Assert TryBindJoin returns true, and Select.JoinScope stays null (nothing to track — every member
        // is already a resolved scalar leaf).
    }
}
```

(These are written as scaffolds with the real assertions and arrange-comments — fill in the exact `MongoQueryExpression`/`IEntityType` construction using this project's existing fixture-building helpers, which `NativeSelectManyBinderTests.cs` in the same directory already demonstrates; do not guess at a different construction pattern.)

- [ ] **Step 2: Run the tests to verify they fail (type doesn't exist yet)**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoJoinBinderTests"`
Expected: build error, `MongoJoinBinder` does not exist.

- [ ] **Step 3: Implement `MongoJoinBinder`**

```csharp
// src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs
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

using System.Linq.Expressions;
using MongoDB.EntityFrameworkCore.Query.Expressions;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Binds a genuine two-sided <c>Join</c>/<c>LeftJoin</c> (a real relational join whose result selector
/// combines fields from both sides — NOT the Include-sugar shape <c>TryConfirmReferenceInclude</c> already
/// handles) to a native <c>$lookup</c>/<c>$unwind</c>. Scope for this first chunk: one join level, a
/// resolvable model navigation, a bare-collection-scan inner (the same single-level eligibility Include
/// already enforces). <see cref="TryBindJoin"/> returns <see langword="false"/> (no mutation) for anything
/// outside that scope — GroupJoin's own array-result shape, a chained/nested join, a navigation-less
/// key-equality join, a query-filtered or non-bare-scan inner — and the caller falls back to the existing
/// driver-LINQ bridge, which already produces correct results for all of these. See
/// <c>docs/superpowers/specs/2026-08-26-native-join-translation-design.md</c>.
/// </summary>
internal static class MongoJoinBinder
{
    internal static bool TryBindJoin(
        MongoQueryExpression outerQueryExpression,
        MongoQueryExpression innerQueryExpression,
        LambdaExpression resultSelector,
        JoinInfo joinInfo)
    {
        // Out of scope: GroupBy-sourced side (wrong-data hazard, already marked fallback-unsafe by the
        // caller) or Distinct-sourced side (correct-but-non-native fallback, already marked by the caller).
        // Both leave the query non-native via the caller's own MarkGroupByFallbackUnsafe/
        // MarkNotNativelyRepresentable calls — this binder must not try to additionally register a $lookup
        // on top of an already-declined select.
        if (outerQueryExpression.Select.IsGroupBy || innerQueryExpression.Select.IsGroupBy
            || outerQueryExpression.Select.IsDistinct || innerQueryExpression.Select.IsDistinct)
        {
            return false;
        }

        // Out of scope for this chunk: no resolved model navigation (a pure key-equality join with no
        // corresponding INavigation — LookupExpression's navigation-less constructor exists for EF-377's
        // multi-hop flattening, but wiring it into this binder is deferred).
        if (joinInfo.Navigation is not { } navigation)
        {
            return false;
        }

        // Single-level eligibility, mirroring TryConfirmReferenceInclude's own constraints: the inner side
        // must be a bare collection scan (no filter/projection/paging of its own) — widening this is
        // EF-368/chunk-B territory, not this chunk.
        if (!innerQueryExpression.Select.IsBareCollectionScan)
        {
            return false;
        }

        // Chained/nested joins are out of scope for this chunk: a join composed onto an already-joined
        // outer, or with a JoinScope already set from a prior join on this same select, declines here.
        if (outerQueryExpression.Select.JoinScope != null)
        {
            return false;
        }

        if (resultSelector.Parameters.Count != 2)
        {
            return false;
        }

        var outerParam = resultSelector.Parameters[0];
        var innerParam = resultSelector.Parameters[1];

        if (!TryReadMembers(resultSelector.Body, out var members))
        {
            return false;
        }

        string? outerMemberName = null;
        string? innerMemberName = null;

        foreach (var (alias, argExpr) in members)
        {
            if (ReferenceEquals(argExpr, outerParam))
            {
                if (outerMemberName != null) return false; // whole outer captured twice — ambiguous, decline
                outerMemberName = alias;
            }
            else if (ReferenceEquals(argExpr, innerParam))
            {
                if (innerMemberName != null) return false; // whole inner captured twice — ambiguous, decline
                innerMemberName = alias;
            }
            // A member that is neither param verbatim is an already-flattened scalar leaf (e.g. o.Name) —
            // it needs no scope tracking here; it is translated later by the ordinary single-scope path
            // exactly as any other projected Select member is, once this join is confirmed native below.
        }

        var lookup = new LookupExpression(navigation, forceUnwind: true)
        {
            PreserveNullAndEmptyArrays = joinInfo.IsLeftOuter
        };
        outerQueryExpression.AddLookup(lookup);

        if (outerMemberName != null || innerMemberName != null)
        {
            outerQueryExpression.Select.JoinScope = new MongoJoinScope(
                outerMemberName, innerMemberName,
                outerQueryExpression.CollectionExpression.EntityType, navigation.TargetEntityType,
                joinInfo.Alias, joinInfo.IsLeftOuter);
        }

        return true;
    }

    // new {...} (NewExpression with Members) or a parameterless MemberInit — mirrors the identical helper
    // already duplicated in NativeSelectManyBinder.TryReadProjection and NativeProjectionBinder. Not
    // extracted into a shared helper in this chunk (out of scope — flagged for a future cleanup pass rather
    // than bundled into this feature's diff).
    private static bool TryReadMembers(Expression body, out System.Collections.Generic.IReadOnlyList<(string Alias, Expression Arg)> members)
    {
        members = null!;
        var list = new System.Collections.Generic.List<(string, Expression)>();
        switch (body)
        {
            case NewExpression ne when ne.Members != null && ne.Members.Count == ne.Arguments.Count && ne.Arguments.Count > 0:
                for (var i = 0; i < ne.Arguments.Count; i++) list.Add((ne.Members[i].Name, ne.Arguments[i]));
                break;
            case MemberInitExpression mi when mi.NewExpression.Arguments.Count == 0 && mi.Bindings.Count > 0:
                foreach (var b in mi.Bindings)
                {
                    if (b is not MemberAssignment ma) return false;
                    list.Add((b.Member.Name, ma.Expression));
                }
                break;
            default:
                return false;
        }
        members = list;
        return true;
    }
}
```

- [ ] **Step 4: Run the unit tests to verify they pass**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoJoinBinderTests"`
Expected: PASS.

- [ ] **Step 5: Build all three EF configs and commit**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoJoinBinderTests.cs
git commit -m "EF-392: add MongoJoinBinder for native single-level Join/LeftJoin eligibility and scope classification"
```

---

