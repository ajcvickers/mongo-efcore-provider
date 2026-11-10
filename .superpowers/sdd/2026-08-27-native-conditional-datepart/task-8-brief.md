### Task 8: Non-goal regression tests — `$elemMatch` exclusion and negator decline

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoQueryLanguageRenderer.cs:446-489` (`IsQueryDialectRenderable`) — explicit decline arms for documentation clarity (the catch-all already returns `false`; this makes the decision visible, matching the existing `MongoConvertExpression => false` precedent)
- Test: append to `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/` — either a new small file or an addition to an existing `MongoQueryLanguageRendererTests`/`MongoExpressionNegatorTests` file (check which already exists via `ls tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/ | grep -i "querylanguage\|negator"` before deciding)

**Interfaces:**
- Consumes: `MongoConditionalExpression`, `MongoDatePartExpression` from Task 1; `MongoQueryLanguageRenderer.IsQueryDialectRenderable`, `MongoExpressionNegator.TryNegate` (existing).

- [ ] **Step 1: Write the failing test**

```csharp
    [Fact]
    public void IsQueryDialectRenderable_rejects_a_conditional_expression()
    {
        var conditional = new MongoConditionalExpression(
            new MongoConstantExpression(true, forSerialization: null),
            new MongoConstantExpression(1, forSerialization: null),
            new MongoConstantExpression(2, forSerialization: null));

        Assert.False(MongoQueryLanguageRenderer.IsQueryDialectRenderable(conditional));
    }

    [Fact]
    public void IsQueryDialectRenderable_rejects_a_date_part_expression()
    {
        var datePart = new MongoDatePartExpression(
            new MongoConstantExpression(DateTime.UtcNow, forSerialization: null), MongoDatePart.Year);

        Assert.False(MongoQueryLanguageRenderer.IsQueryDialectRenderable(datePart));
    }

    [Fact]
    public void Negator_declines_a_conditional_expression_rather_than_mis_negating_it()
    {
        var conditional = new MongoConditionalExpression(
            new MongoConstantExpression(true, forSerialization: null),
            new MongoConstantExpression(1, forSerialization: null),
            new MongoConstantExpression(2, forSerialization: null));

        Assert.False(MongoExpressionNegator.TryNegate(conditional, out _));
    }
```

**Note:** these tests already pass today (the catch-all `_ => false` in `IsQueryDialectRenderable`, and `TryNegate`'s own `IsQueryDialectRenderable` gate, already produce this result with zero code changes) — this task is a **pinning/regression** task, not a bug fix. Write the test, confirm it passes immediately, then proceed straight to making the decline explicit for documentation.

- [ ] **Step 2: Run test to verify it already passes**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~<the test class you added these to>"`
Expected: PASS immediately (see the note in Step 1 — there is nothing to "make" pass here, only to pin).

- [ ] **Step 3: Make the decline explicit (documentation-only change)**

In `MongoQueryLanguageRenderer.cs`, add two explicit arms to `IsQueryDialectRenderable` right after the existing `MongoConvertExpression => false` arm (around line 471-474):

```csharp
            // Explicit rather than left to the catch-all, matching MongoConvertExpression above: neither has a
            // query-dialect form, so admitting either here would put $expr inside $elemMatch, a hard server
            // error.
            MongoConditionalExpression => false,
            MongoDatePartExpression => false,
            MongoDateTimeOffsetLocalExpression => false,
```

- [ ] **Step 4: Run the tests again to confirm nothing broke**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"`
Expected: PASS, zero regressions (this change only makes an already-true default explicit).

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoQueryLanguageRenderer.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/
git commit -m "Pin that conditional/date-part expressions decline (not mis-negate) in query-dialect/elemMatch position"
```

---

## Final Self-Review Checklist (for whoever executes this plan)

- [ ] Every task's tests pass on EF10 before moving to the next task.
- [ ] Task 7's full three-EF-version sweep shows zero `Passed → Failed` regressions.
- [ ] `MongoDateTimeOffsetLocalExpression.Operand` stayed typed as `MongoFieldExpression` (not widened to `MongoExpression`) — this is a compile-time guarantee the design relies on, not a runtime check to relax later without re-deriving why it's safe.
- [ ] `TimeOfDay` was NOT implemented — confirm the `TimeOfDay_declines` test (Task 4) is still present and still passing after all later tasks land, so a future change can't silently start guessing at its translation without a deliberate decision.
- [ ] No commit message references a JIRA ticket key — file one before merging, per this plan's own Global Constraints note.
