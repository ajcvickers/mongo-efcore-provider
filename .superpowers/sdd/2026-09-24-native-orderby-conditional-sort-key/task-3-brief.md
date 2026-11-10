### Task 3: Implement the conditional ORDER BY sort-key arm (GREEN)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs` (add a new private helper method, and one new `else if` arm inside `PopulateSortSlot`, immediately after the existing Inner-access arm ending at line ~493)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/JoinScopeOrderBySlotPopulationTests.cs` (from Task 2 — add one more test for the chain-ordering Review Focus item)

**Interfaces:**
- Consumes: `NativeJoinScopeTranslator.TryMatchScopeNullCheck(MongoJoinScope, ParameterExpression, Expression test, out int scopeIndex, out bool isNotNull)` (existing, unchanged); `NativeJoinScopeProjectionBinder.TryTranslateConditionalBranch(MongoJoinScope, ParameterExpression, Expression branch, out MongoExpression? result)` (widened to `internal` in Task 1); `MongoSelectDefinition.DeferTrailingSortPastConfirmedJoin()` / `MarkJoinInnerAccessConfirmed()` (existing, unchanged, already used by the sibling Inner-access value-sort-key arm).
- Produces: `NativeSlotPopulator`'s new private `TryTranslateConditionalSortKey(MongoQueryExpression mongoQ, MongoJoinScope scope, ParameterExpression rootParam, Expression body, out MongoExpression? result)` — used only within this file, no other task depends on its exact signature.

- [ ] **Step 1: Add the new helper method**

Edit `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs`. Find the end of `PopulateSortSlot` (the `else` `MarkNotNativelyRepresentable()` line and the method's closing brace, currently around line 521):

```csharp
        else
            mongoQ.Select.MarkNotNativelyRepresentable();
    }
```

Leave that exact block in place (it stays the final fallback) but insert a new private helper method immediately **after** `PopulateSortSlot`'s closing brace:

```csharp
        else
            mongoQ.Select.MarkNotNativelyRepresentable();
    }

    /// <summary>
    /// Attempts to translate a BARE nav-null-check ternary ORDER BY/THEN BY sort key reaching a single-level
    /// join scope's Inner side — <c>o =&gt; o.Customer != null ? o.Customer.City : ""</c> — mirroring
    /// <see cref="NativeJoinScopeProjectionBinder.TryBindConditionalProjection"/>'s SELECT-side recognizer for
    /// the identical shape (structural null-check match via
    /// <see cref="NativeJoinScopeTranslator.TryMatchScopeNullCheck"/>, branch translation via
    /// <see cref="NativeJoinScopeProjectionBinder.TryTranslateConditionalBranch"/>), but NOT extracted into a
    /// shared method with it: the two call sites confirm the join differently (
    /// <see cref="NativeJoinScopeProjectionBinder.ConfirmEntireChain"/> for the SELECT case vs.
    /// <see cref="MongoSelectDefinition.DeferTrailingSortPastConfirmedJoin"/> +
    /// <see cref="MongoSelectDefinition.MarkJoinInnerAccessConfirmed"/> here), so the caller must stay in
    /// control of the commit step. Declines (returns <see langword="false"/>, never throws) for a plain inner
    /// <c>Join</c>'s degenerate "always true" null check and for a collection navigation's "no single
    /// is-it-null answer" case — the same two guards <c>TryBindConditionalProjection</c> applies, re-derived
    /// here since this is a different call site (see <see cref="JoinScopeOrderBySlotPopulationTests"/> for
    /// both decline cases proven independently).
    /// </summary>
    private static bool TryTranslateConditionalSortKey(
        MongoQueryExpression mongoQ, MongoJoinScope scope, ParameterExpression rootParam, Expression body,
        [NotNullWhen(true)] out MongoExpression? result)
    {
        result = null;

        if (body is not ConditionalExpression conditional
            || !NativeJoinScopeTranslator.TryMatchScopeNullCheck(
                scope, rootParam, conditional.Test, out var scopeIndex, out var isNotNull))
        {
            return false;
        }

        // scopeIndex is 1-based over Levels (see TryMatchScopeNullCheck's own contract: 0 is the root, never
        // returned); mongoQ.Joins is the parallel 0-based list TranslateJoinCore built the scope from.
        var checkedJoin = mongoQ.Joins[scopeIndex - 1];
        var level = scope.Levels[scopeIndex - 1];

        // Degenerate-check guard: a plain inner Join drops an unmatched row entirely rather than unwinding it
        // as an explicit null, so "Inner != null" is unconditionally true there (and "== null" unconditionally
        // false) — not a real check. A collection navigation is a different shape (many joined rows, not a
        // single nullable one) with no single "is it null" answer. Mirrors
        // NativeJoinScopeProjectionBinder.TryBindConditionalProjection's identical guard.
        if (!level.IsLeftOuter || checkedJoin.Navigation is { IsCollection: true })
        {
            return false;
        }

        if (!NativeJoinScopeProjectionBinder.TryTranslateConditionalBranch(scope, rootParam, conditional.IfTrue, out var ifTrue)
            || !NativeJoinScopeProjectionBinder.TryTranslateConditionalBranch(scope, rootParam, conditional.IfFalse, out var ifFalse))
        {
            return false;
        }

        result = new MongoConditionalExpression(
            new MongoLookupNullCheckExpression(level.InnerPrefix, isNotNull), ifTrue, ifFalse);
        return true;
    }
```

- [ ] **Step 2: Add the new arm to `PopulateSortSlot`**

In the same file, find the existing Inner-access value-sort-key arm (the one calling `DeferTrailingSortPastConfirmedJoin()` + `MarkJoinInnerAccessConfirmed()`, currently ending around line 494):

```csharp
        else if (mongoQ.Select.JoinScope is { Levels.Count: 1 } innerSortScope
                 && mongoQ.Joins.Count == 1
                 && NativeJoinScopeTranslator.TryTranslateValue(
                     innerSortScope, keySelector.Parameters[0], keySelector.Body, out var innerSortKey))
        {
            // Relocate BEFORE flipping/recording: an earlier Outer-only key in this SAME chain (e.g.
            // `OrderBy(o.OrderID).ThenBy(o.Customer.CustomerID)`) is still sitting in PipelineOps as a
            // MongoSortOp — it must move into PostJoinOps too, so both keys end up in ONE $sort stage. See
            // DeferTrailingSortPastConfirmedJoin's own remarks for why splitting them across two stages is a
            // silent wrong-order bug, not just a cosmetic difference.
            mongoQ.Select.DeferTrailingSortPastConfirmedJoin();
            mongoQ.Select.MarkJoinInnerAccessConfirmed();
            record(new MongoOrdering(innerSortKey, ascending));
        }
```

Immediately after this arm's closing `}` (and before the `else if (mongoQ.Select.JoinScope is { Levels.Count: > 1 } chainedScope ...` arm that follows it), insert:

```csharp
        // A CONDITIONAL sort key reaching a single-level join's Inner side (EF-322, Phase 2 Group A), e.g.
        // `Orders.OrderBy(o => o.Customer != null ? o.Customer.City : "")`. The plain-value arm immediately
        // above never matches a ConditionalExpression body (TryTranslateValue has no ternary handling), so
        // this arm is checked next, not in place of it.
        else if (mongoQ.Select.JoinScope is { Levels.Count: 1 } conditionalSortScope
                 && mongoQ.Joins.Count == 1
                 && TryTranslateConditionalSortKey(
                     mongoQ, conditionalSortScope, keySelector.Parameters[0], keySelector.Body,
                     out var conditionalSortKey))
        {
            // Same relocate-then-confirm sequence as the plain-value Inner-access arm above, for the same
            // reason: an earlier Outer-only key in this SAME chain must land in the same $sort stage.
            mongoQ.Select.DeferTrailingSortPastConfirmedJoin();
            mongoQ.Select.MarkJoinInnerAccessConfirmed();
            record(new MongoOrdering(conditionalSortKey, ascending));
        }
```

- [ ] **Step 3: Rebuild and run the Task 2 unit tests to confirm all pass**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~JoinScopeOrderBySlotPopulationTests"
```

Expected: all 3 tests **PASS** (the previously-failing `Conditional_sort_key_over_an_optional_reference_left_join_goes_native` now passes; the two decline tests still pass, unchanged).

- [ ] **Step 4: Write and verify the chain-ordering Review Focus test (RED then GREEN)**

Add a fourth test to `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/JoinScopeOrderBySlotPopulationTests.cs`, right after `Conditional_sort_key_over_an_optional_reference_left_join_goes_native`:

```csharp
    [Fact]
    public void Outer_key_then_conditional_inner_key_land_in_one_sort_stage()
    {
        // An earlier Outer-only key (r.Total) recorded in PipelineOps as a MongoSortOp, followed by a
        // ThenBy reaching the Inner side via the new conditional arm, must relocate the WHOLE existing sort
        // into PostJoinOps rather than leaving two separate $sort stages — see
        // DeferTrailingSortPastConfirmedJoin's own remarks for why splitting them is a silent wrong-order bug.
        var mongoQ = TranslateJoinQuery((owners, orders) =>
            orders.GroupJoin(owners, r => r.OwnerId, o => o.Id, (r, os) => new { r, os })
                .SelectMany(x => x.os.DefaultIfEmpty(), (x, o) => new { x.r, o })
                .OrderBy(x => x.r.Total)
                .ThenBy(x => x.o != null ? x.o.Name : ""));

        Assert.Empty(mongoQ.Select.PipelineOps);
        var sortOp = Assert.IsType<MongoSortOp>(Assert.Single(mongoQ.Select.PostJoinOps));
        Assert.Equal(2, sortOp.Orderings.Count);
        Assert.IsType<MongoConditionalExpression>(sortOp.Orderings[1].KeySelector);
    }
```

Run it before implementing anything further:

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~Outer_key_then_conditional_inner_key_land_in_one_sort_stage"
```

Expected: **PASS** already — Step 2's arm reuses the exact same `DeferTrailingSortPastConfirmedJoin()` call the sibling plain-value arm already used successfully for this exact scenario, so no additional implementation is needed here. This step exists to prove that claim with a real test, not to assume it. If it fails, the relocate-then-confirm sequence in Step 2 was copied incorrectly — compare it line-for-line against the plain-value arm immediately above it in the source.

- [ ] **Step 5: Run the full unit test project to confirm no other regression**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build
```

Expected: **all PASS**.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/JoinScopeOrderBySlotPopulationTests.cs
git commit -m "EF-322: translate a nav-null-check conditional ORDER BY sort key natively"
```

---

