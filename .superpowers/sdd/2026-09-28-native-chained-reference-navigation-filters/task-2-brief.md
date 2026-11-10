### Task 2: Bare root-entity leaf over a chained scope (gap 2)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeTranslator.cs` (new public
  wrapper next to `TryRerootToBareScope`, around line 256)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
  (`TranslateSelect`, the depth-1 bare-leaf arm around lines 414–443)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/JoinScopeWhereSlotPopulationTests.cs`

**Interfaces:**
- Consumes: Task 1's chained `Where` arm (the unit tests compose a Where before the Select);
  `NativeJoinScopeProjectionBinder.ConfirmEntireChain(MongoQueryExpression, MongoJoinScope)`;
  `IsSingleEligibleNativeJoinScope(MongoQueryExpression, out JoinInfo?)`; `MongoSelectDefinition.{HasPaging, HasPagingRecordedAfterAJoin}`.
- Produces: `public static bool NativeJoinScopeTranslator.TryResolveBareScopeLeaf(MongoJoinScope scope, ParameterExpression rootParam, Expression node, out int scopeIndex)`.

- [ ] **Step 1: Write the failing unit tests**

Append to `JoinScopeWhereSlotPopulationTests`:

```csharp
    /// <summary>
    /// The full nav-expansion shape of a multi-hop filter: chained join, Where on the second level, then the
    /// root-entity leaf <c>Select(x =&gt; x.o)</c> (arrives as <c>ti =&gt; ti.Outer.Outer</c>). The new arm confirms
    /// the whole chain, so Route is native and every candidate join is confirmed.
    /// </summary>
    [Fact]
    public void Root_entity_leaf_after_second_level_where_over_chained_join_goes_native()
    {
        var mongoQ = TranslateThreeSourceJoinQuery((owners, orders, lines) =>
            owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Join(lines, e => e.r.Id, l => l.OrderId, (e, l) => new { e.o, e.r, l })
                .Where(x => x.l.Sku == "A")
                .Select(x => x.o));

        Assert.False(mongoQ.Select.HasUnsupportedOperator);
        Assert.False(mongoQ.Select.HasUnconfirmedCandidateJoin);
        Assert.True(mongoQ.Select.HasConfirmedJoinLookup);
        Assert.Equal(NativeRoute.WholeEntity, mongoQ.Select.Route);
    }

    /// <summary>
    /// A non-root whole-entity leaf (<c>x.l</c>) is NOT taken by the new arm: reading an Inner entity needs the
    /// HasBareJoinInnerEntityLeaf read-side handling, which only exists at depth 1. Must stay Fallback.
    /// </summary>
    [Fact]
    public void Inner_entity_leaf_over_chained_join_is_not_confirmed()
    {
        var mongoQ = TranslateThreeSourceJoinQuery((owners, orders, lines) =>
            owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Join(lines, e => e.r.Id, l => l.OrderId, (e, l) => new { e.o, e.r, l })
                .Select(x => x.l));

        Assert.Equal(NativeRoute.Fallback, mongoQ.Select.Route);
    }

    /// <summary>
    /// Paging written BETWEEN the two joins sits in PipelineOps flagged "after a join"; confirming the chain would
    /// defer it past BOTH $lookups (the known chain-paging gap), so the arm must decline.
    /// </summary>
    [Fact]
    public void Root_entity_leaf_over_chained_join_with_paging_between_joins_declines()
    {
        var mongoQ = TranslateThreeSourceJoinQuery((owners, orders, lines) =>
            owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Take(1)
                .Join(lines, e => e.r.Id, l => l.OrderId, (e, l) => new { e.o, e.r, l })
                .Select(x => x.o));

        Assert.Equal(NativeRoute.Fallback, mongoQ.Select.Route);
    }
```

`NativeRoute` is declared in `MongoDB.EntityFrameworkCore.Query.Expressions` (bottom of `MongoSelectDefinition.cs`),
already imported by this file.

- [ ] **Step 2: Build and run to verify the first test fails**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~JoinScopeWhereSlotPopulationTests"
```

Expected: `Root_entity_leaf_after_second_level_where_over_chained_join_goes_native` FAILS
(`HasUnsupportedOperator` true, from TranslateSelect's projected-Select branch). The two decline pins PASS.

- [ ] **Step 3: Add `TryResolveBareScopeLeaf` to `NativeJoinScopeTranslator`**

Directly above the private `TryRerootToBareScope`:

```csharp
    /// <summary>
    /// Resolves a bare scope leaf — a pure <c>Outer</c>/<c>Inner</c> hop chain with no trailing member, e.g.
    /// <c>ti.Outer.Outer</c> — to its scope index (0 = root). Structural only; the caller decides which index is
    /// acceptable.
    /// </summary>
    public static bool TryResolveBareScopeLeaf(
        MongoJoinScope scope, ParameterExpression rootParam, Expression node, out int scopeIndex)
        => TryRerootToBareScope(scope, rootParam, node, out scopeIndex);
```

- [ ] **Step 4: Add the chained root-leaf arm to `TranslateSelect`**

Insert it **directly after** the depth-1 bare-leaf arm (the
`else if (IsTransparentIdentifierMemberAccessSelector(selector) && mongoQueryExpression.Select.JoinScope is { Levels.Count: 1 } && IsSingleEligibleNativeJoinScope(…, out var bareLeafJoin))`
block, which ends with `MarkJoinLookupConfirmed();`) and **before** the wrapped-projection arm
(`else if (IsSingleEligibleNativeJoinScope(mongoQueryExpression, out var wrappedLeafJoin) && NativeJoinScopeProjectionBinder.TryBindProjection(…))`):

```csharp
        // A bare ROOT-entity leaf over a chained join scope (`ti => ti.Outer.Outer`): what nav-expansion leaves after
        // a multi-hop reference-navigation filter (`od.Order.Customer.City == "Seattle"`). Confirms every level, as
        // the chained projection arms do. Root only: a non-root whole-entity leaf needs HasBareJoinInnerEntityLeaf's
        // read-side handling, which exists only at depth 1.
        //
        // Paging in PipelineOps recorded after a join may sit BETWEEN two joins of the chain, and confirming would
        // defer it past both $lookups (see the chain-paging gap at the bare-value arm below), so decline. Checked
        // before IsSingleEligibleNativeJoinScope, which may itself defer PipelineOps. Paging recorded after the Where
        // flipped to PostJoinOps is unaffected (it's not in PipelineOps), and paging recorded before any join keeps
        // IsSingleEligibleNativeJoinScope's existing handling.
        else if (mongoQueryExpression.Select.JoinScope is { Levels.Count: > 1 } chainedLeafScope
                 && NativeJoinScopeTranslator.TryResolveBareScopeLeaf(
                     chainedLeafScope, selector.Parameters[0], selector.Body, out var chainedLeafScopeIndex)
                 && chainedLeafScopeIndex == 0
                 && !(mongoQueryExpression.Select.HasPaging && mongoQueryExpression.Select.HasPagingRecordedAfterAJoin)
                 && IsSingleEligibleNativeJoinScope(mongoQueryExpression, out _))
        {
            NativeJoinScopeProjectionBinder.ConfirmEntireChain(mongoQueryExpression, chainedLeafScope);
        }
```

Like the depth-1 arm, it doesn't `return`: the generic shaper fold at the end of `TranslateSelect` resolves
`ti.Outer.Outer` against the join's shaper.

Also update the depth-1 arm's comment: replace the sentence
`Possible follow-up: use NativeJoinScopeProjectionBinder.ConfirmEntireChain for chains.` with
`A bare root leaf over a chain is handled by the next arm.`

- [ ] **Step 5: Build and run the unit tests to verify they pass**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build
```

Expected: PASS. **If `Root_entity_leaf_after_second_level_where_over_chained_join_goes_native` fails only on
`HasUnconfirmedCandidateJoin`** (more candidates than levels), stop and report the candidate/confirmation counts.
Do not change the counting or `ConfirmEntireChain`.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeTranslator.cs \
        src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/JoinScopeWhereSlotPopulationTests.cs
git commit -m "EF-322: confirm a bare root-entity leaf over a chained join scope natively"
```

---

