### Task 1: Chained-scope `Where` over a non-root level (gap 1)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeTranslator.cs`
  (`TryTranslateSingleScope`, around lines 78–112)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs` (`Where` arm, around
  lines 157–253; new helper next to `TryTranslateJoinScopeConjunction`, around line 350)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/JoinScopeWhereSlotPopulationTests.cs`

**Interfaces:**
- Consumes: `NativeJoinScopeTranslator.TryMatchScopeNullCheck(MongoJoinScope, ParameterExpression, Expression, out int scopeIndex, out bool isNotNull)`,
  `MongoSelectDefinition.MarkJoinInnerAccessConfirmed()`, `MongoSelectDefinition.AddPredicateConjunct(MongoExpression)`,
  `MongoLookupNullCheckExpression(string lookupAlias, bool isNotNull)`, `MongoJoinScopeLevel.{InnerPrefix, IsLeftOuter}`.
- Produces:
  - `public static bool NativeJoinScopeTranslator.TryTranslateSingleScopePredicate(MongoJoinScope scope, ParameterExpression rootParam, Expression body, out int scopeIndex, [NotNullWhen(true)] out MongoExpression? result)`
  - `private static bool NativeSlotPopulator.TryTranslateChainedScopeConjunction(MongoQueryExpression mongoQ, MongoJoinScope scope, ParameterExpression rootParam, Expression body, [NotNullWhen(true)] out List<MongoExpression>? conjuncts)`

- [ ] **Step 1: Write the failing unit tests**

Append to `JoinScopeWhereSlotPopulationTests` (it already has the three-source `TranslateThreeSourceJoinQuery`
helper and the `Owner`/`Order`/`OrderLine` fixture; `x.o` is scope 0, `x.r` is level 1, `x.l` is level 2):

```csharp
    /// <summary>
    /// A chained join's <c>Where</c> reading the SECOND level's Inner side (<c>x.l.Sku</c>) translates via the chained
    /// arm and defers into <see cref="MongoSelectDefinition.PostJoinOps"/>, so it lowers after both
    /// <c>$lookup</c>/<c>$unwind</c> blocks. This is nav-expansion's shape for a multi-hop reference-navigation filter
    /// (<c>od.Order.Customer.City == "Seattle"</c>).
    /// </summary>
    [Fact]
    public void Where_reading_second_level_inner_scope_of_chained_join_populates_predicate_in_post_join_ops()
    {
        var mongoQ = TranslateThreeSourceJoinQuery((owners, orders, lines) =>
            owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Join(lines, e => e.r.Id, l => l.OrderId, (e, l) => new { e.o, e.r, l })
                .Where(x => x.l.Sku == "A"));

        Assert.Equal(2, mongoQ.Select.JoinScope!.Levels.Count);
        Assert.False(mongoQ.Select.HasUnsupportedOperator);
        Assert.True(mongoQ.Select.JoinInnerAccessConfirmed);
        Assert.Empty(mongoQ.Select.PipelineOps);
        var matchOp = Assert.IsType<MongoMatchOp>(Assert.Single(mongoQ.Select.PostJoinOps));
        Assert.IsType<MongoBinaryExpression>(matchOp.Predicate);
    }

    /// <summary>Same, for the FIRST level's Inner side of a two-level chain (<c>x.r.Total</c>, scope index 1).</summary>
    [Fact]
    public void Where_reading_first_level_inner_scope_of_chained_join_populates_predicate_in_post_join_ops()
    {
        var mongoQ = TranslateThreeSourceJoinQuery((owners, orders, lines) =>
            owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Join(lines, e => e.r.Id, l => l.OrderId, (e, l) => new { e.o, e.r, l })
                .Where(x => x.r.Total > 0));

        Assert.False(mongoQ.Select.HasUnsupportedOperator);
        Assert.True(mongoQ.Select.JoinInnerAccessConfirmed);
        Assert.Empty(mongoQ.Select.PipelineOps);
        Assert.IsType<MongoMatchOp>(Assert.Single(mongoQ.Select.PostJoinOps));
    }

    /// <summary>
    /// A top-level <c>&amp;&amp;</c> whose conjuncts each read ONE scope (root and second level) is split and translated
    /// per conjunct; AddPredicateConjunct folds them into one <c>$match</c> in PostJoinOps.
    /// </summary>
    [Fact]
    public void Where_conjunction_over_root_and_second_level_of_chained_join_populates_one_post_join_match()
    {
        var mongoQ = TranslateThreeSourceJoinQuery((owners, orders, lines) =>
            owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Join(lines, e => e.r.Id, l => l.OrderId, (e, l) => new { e.o, e.r, l })
                .Where(x => x.o.Name == "Alice" && x.l.Sku == "A"));

        Assert.False(mongoQ.Select.HasUnsupportedOperator);
        Assert.Empty(mongoQ.Select.PipelineOps);
        var matchOp = Assert.IsType<MongoMatchOp>(Assert.Single(mongoQ.Select.PostJoinOps));
        Assert.Equal(
            MongoBinaryOperator.AndAlso,
            Assert.IsType<MongoBinaryExpression>(matchOp.Predicate).Operator);
    }

    /// <summary>A single comparison spanning two scopes (root vs. second level) has no single-scope translation: decline.</summary>
    [Fact]
    public void Where_comparing_two_scopes_of_chained_join_declines()
    {
        var mongoQ = TranslateThreeSourceJoinQuery((owners, orders, lines) =>
            owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Join(lines, e => e.r.Id, l => l.OrderId, (e, l) => new { e.o, e.r, l })
                .Where(x => x.o.Name == x.l.Sku));

        Assert.True(mongoQ.Select.HasUnsupportedOperator);
        Assert.Empty(mongoQ.Select.PostJoinOps);
    }

    /// <summary>
    /// A null check on an INNER-join level is constant (the $unwind already dropped unmatched rows), so the chained arm
    /// declines it rather than emitting a vacuous $match — same rule as the depth-1 null-check arm.
    /// </summary>
    [Fact]
    public void Where_null_check_on_inner_join_level_of_chained_join_declines()
    {
        var mongoQ = TranslateThreeSourceJoinQuery((owners, orders, lines) =>
            owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Join(lines, e => e.r.Id, l => l.OrderId, (e, l) => new { e.o, e.r, l })
                .Where(x => x.l == null));

        Assert.True(mongoQ.Select.HasUnsupportedOperator);
        Assert.Empty(mongoQ.Select.PostJoinOps);
    }
```

`MongoBinaryExpression.Operator` and `MongoBinaryOperator` are in `MongoDB.EntityFrameworkCore.Query.Expressions`,
which the file already imports.

- [ ] **Step 2: Build and run the new tests to verify they fail**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~JoinScopeWhereSlotPopulationTests"
```

Expected: the three `…populates…` tests FAIL (`HasUnsupportedOperator` is true). The two `…declines` tests already
PASS; they're regression pins for the new arm.

- [ ] **Step 3: Add `TryTranslateSingleScopePredicate` to `NativeJoinScopeTranslator`**

Refactor `TryTranslateSingleScope` into a private core that also reports the scope index. Keep the existing public
signature unchanged for its current callers:

```csharp
    public static bool TryTranslateSingleScope(
        MongoJoinScope scope, ParameterExpression rootParam, Expression body, bool valueMode,
        [NotNullWhen(true)] out MongoExpression? result)
        => TryTranslateSingleScopeCore(scope, rootParam, body, valueMode, out _, out result);

    /// <summary>
    /// Predicate form of <see cref="TryTranslateSingleScope"/> that also reports which scope the body resolved to
    /// (0 = root, <c>k</c> = <c>scope.Levels[k-1]</c>'s Inner), so the caller can apply per-level gates.
    /// </summary>
    public static bool TryTranslateSingleScopePredicate(
        MongoJoinScope scope, ParameterExpression rootParam, Expression body,
        out int scopeIndex, [NotNullWhen(true)] out MongoExpression? result)
        => TryTranslateSingleScopeCore(scope, rootParam, body, valueMode: false, out scopeIndex, out result);

    private static bool TryTranslateSingleScopeCore(
        MongoJoinScope scope, ParameterExpression rootParam, Expression body, bool valueMode,
        out int scopeIndex, [NotNullWhen(true)] out MongoExpression? result)
    {
        result = null;
        if (!TryRerootToSingleScope(scope, rootParam, body, out scopeIndex, out var rewritten))
        {
            return false;
        }

        // ...the existing body of TryTranslateSingleScope from `MongoExpressionTranslator translator;` onward,
        // unchanged...
    }
```

Move the existing body verbatim; the only change is that `scopeIndex` is now an `out` parameter instead of a local.

- [ ] **Step 4: Add `TryTranslateChainedScopeConjunction` to `NativeSlotPopulator`**

Place it directly after `TryTranslateJoinScopeConjunction`:

```csharp
    /// <summary>
    /// Translates a <c>Where</c> body over a CHAINED join scope (<c>Levels.Count &gt; 1</c>) that reads a non-root
    /// level — nav-expansion's shape for a multi-hop reference-navigation filter (<c>od.Order.Customer.City ==
    /// "Seattle"</c>). Each top-level <c>&amp;&amp;</c> conjunct must be either a bare-level null check
    /// (<see cref="NativeJoinScopeTranslator.TryMatchScopeNullCheck"/>) on a left-outer level, or a predicate resolving to
    /// exactly one scope. Splitting is exact since a <c>$match</c> has no short-circuit to preserve. All-or-nothing
    /// and pure: nothing is recorded on decline.
    /// </summary>
    /// <remarks>
    /// Declines when no conjunct reads a non-root level: a root-only body belongs to the root-scope arm, which records
    /// into PipelineOps ahead of the $lookup. A non-root conjunct needs its level's Lookup to be ForceUnwind, like
    /// the depth-1 Inner arm, so by the time PostJoinOps lower there is one row per (Outer, Inner) pair.
    /// </remarks>
    private static bool TryTranslateChainedScopeConjunction(
        MongoQueryExpression mongoQ, MongoJoinScope scope, ParameterExpression rootParam, Expression body,
        [NotNullWhen(true)] out List<MongoExpression>? conjuncts)
    {
        conjuncts = null;
        if (mongoQ.Joins.Count != scope.Levels.Count)
        {
            return false;
        }

        var translated = new List<MongoExpression>();
        var readsNonRootScope = false;
        var pending = new Stack<Expression>();
        pending.Push(body);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso)
            {
                // Right pushed first so conjuncts are emitted in source order.
                pending.Push(andAlso.Right);
                pending.Push(andAlso.Left);
                continue;
            }

            if (NativeJoinScopeTranslator.TryMatchScopeNullCheck(scope, rootParam, node, out var nullCheckIndex, out var isNotNull))
            {
                // An inner join drops unmatched rows, so the check would be constant. Same guard as
                // TryTranslateConditionalSortKey.
                var level = scope.Levels[nullCheckIndex - 1];
                if (!level.IsLeftOuter || mongoQ.Joins[nullCheckIndex - 1].Lookup is not { ForceUnwind: true })
                {
                    return false;
                }

                translated.Add(new MongoLookupNullCheckExpression(level.InnerPrefix, isNotNull));
                readsNonRootScope = true;
            }
            else if (NativeJoinScopeTranslator.TryTranslateSingleScopePredicate(
                         scope, rootParam, node, out var scopeIndex, out var conjunct))
            {
                if (scopeIndex > 0)
                {
                    if (mongoQ.Joins[scopeIndex - 1].Lookup is not { ForceUnwind: true })
                    {
                        return false;
                    }

                    readsNonRootScope = true;
                }

                translated.Add(conjunct);
            }
            else
            {
                return false;
            }
        }

        if (!readsNonRootScope)
        {
            return false;
        }

        conjuncts = translated;
        return true;
    }
```

- [ ] **Step 5: Add the chained `Where` arm**

In `PopulateNativeSlots`'s `Where` branch, insert the new arm **directly after** the existing chained root-only
arm (`else if (mongoQ.Select.JoinScope is { Levels.Count: > 1 } chainedScope && NativeJoinScopeTranslator.TryTranslateRootScopeOnly(…))`)
and before the `// A reference-Include null check …` arm:

```csharp
            // Chained join scope reading a non-root level (`od.Order.Customer.City == "Seattle"` after nav-expansion):
            // each conjunct resolves to one scope and defers into PostJoinOps, after every level's $lookup/$unwind.
            // Tried after the root-only arm, so a root-only body still records ahead of the $lookup. Join
            // confirmation is left to the trailing Select, as in the depth-1 Inner arms.
            else if (mongoQ.Select.JoinScope is { Levels.Count: > 1 } chainedInnerScope
                     && TryTranslateChainedScopeConjunction(
                         mongoQ, chainedInnerScope, predicate.Parameters[0], predicate.Body, out var chainedConjuncts))
            {
                // Flip before AddPredicateConjunct so the conjuncts land in PostJoinOps.
                mongoQ.Select.MarkJoinInnerAccessConfirmed();
                foreach (var chainedConjunct in chainedConjuncts)
                {
                    mongoQ.Select.AddPredicateConjunct(chainedConjunct);
                }
            }
```

- [ ] **Step 6: Build and run the unit tests to verify they pass**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~JoinScope"
```

Expected: all PASS, including the existing `JoinScope*SlotPopulationTests` (no regression in the depth-1 or
root-only arms).

- [ ] **Step 7: Run the whole unit test project**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build
```

Expected: PASS. Note that `MongoExpressionNodeCoverageTests` needs no change: no new node type is added.

- [ ] **Step 8: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeTranslator.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/JoinScopeWhereSlotPopulationTests.cs
git commit -m "EF-322: native Where over a non-root level of a chained join scope"
```

---

