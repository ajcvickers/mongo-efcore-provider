# Native join-scope nav-null-check conditional projection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make a bare (non-wrapped) `Select` ternary that null-checks a join scope's Inner side and dereferences
it (`ti => ti.Inner != null ? ti.Inner.City : null`) go native, at any depth of a join chain — fixing
`Manual_expression_tree_typed_null_equality`, which today falls back to driver-LINQ.

**Architecture:** Add a depth-agnostic null-check matcher to `NativeJoinScopeTranslator` (built on the existing
safe `TryRerootToSingleScope` machinery, not the hazardous flat one), a new bare-Conditional recognizer in
`NativeJoinScopeProjectionBinder` that combines that matcher with the existing `TryTranslateSingleScope` branch
translator into a `MongoConditionalExpression`/`MongoLookupNullCheckExpression` pair, a new routing arm in
`MongoQueryableMethodTranslatingExpressionVisitor.TranslateSelect`, and the previously-missing
aggregation-expression rendering for `MongoLookupNullCheckExpression`.

**Tech Stack:** C#, EF Core provider internals, xUnit (plain `Assert.*`), MongoDB aggregation pipeline.

**Spec:** `docs/superpowers/specs/2026-09-23-native-join-scope-nav-null-conditional-projection-design.md`

## Global Constraints

- Tests run serially (`[assembly: CollectionBehavior(DisableTestParallelization = true)]`) — no parallelization
  assumptions in new tests.
- `src/` is nullable-enabled — annotate new members accordingly.
- Build/test against `"Debug EF10"` while iterating; run all three configurations (EF8/EF9/EF10) before the
  final commit (Task 4).
- Nativity must be proven via `MongoQueryMode.NativeOnly` (`MONGODB_EF_NATIVE_ONLY=1`), never by inspecting MQL
  shape alone (`Query/AGENTS.md`'s "MQL shape cannot prove nativity").
- A recognizer must stage into locals and commit only once every check passes (no mutate-then-decline).
- Per-level null-check eligibility requires that level's `IsLeftOuter == true` and its navigation is not a
  collection — re-checked per level for a chain, never assumed once for the whole chain.

---

### Task 1: `NativeJoinScopeTranslator.TryMatchScopeNullCheck`

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeTranslator.cs`
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeTranslatorTests.cs`

**Interfaces:**
- Consumes: the existing `private static bool TryRerootToSingleScope(MongoJoinScope scope, ParameterExpression
  rootParam, Expression body, out int scopeIndex, [NotNullWhen(true)] out Expression? rewritten)` already in
  this file (added by the `2026-09-18-native-chained-join-scalar-projection` plan) — no signature change.
- Produces: `public static bool TryMatchScopeNullCheck(MongoJoinScope scope, ParameterExpression rootParam,
  Expression test, out int scopeIndex, out bool isNotNull)` — consumed by Task 3.

- [ ] **Step 1: Write the failing tests**

Add to `NativeJoinScopeTranslatorTests.cs` (same file, same `NewScope`/`NewRootParam` helpers already defined
there — see lines 70-78 quoted in the spec's research):

```csharp
[Fact]
public void Matches_bare_Inner_not_equal_null_at_depth_one()
{
    var scope = NewScope(isLeftOuter: true);
    var x = NewRootParam();
    Expression test = Expression.NotEqual(
        Expression.PropertyOrField(x, "Inner"), Expression.Constant(null, typeof(InnerEntity)));

    var matched = NativeJoinScopeTranslator.TryMatchScopeNullCheck(scope, x, test, out var scopeIndex, out var isNotNull);

    Assert.True(matched);
    Assert.Equal(1, scopeIndex);
    Assert.True(isNotNull);
}

[Fact]
public void Matches_null_equal_bare_Inner_operand_order_reversed()
{
    var scope = NewScope(isLeftOuter: true);
    var x = NewRootParam();
    Expression test = Expression.Equal(
        Expression.Constant(null, typeof(InnerEntity)), Expression.PropertyOrField(x, "Inner"));

    var matched = NativeJoinScopeTranslator.TryMatchScopeNullCheck(scope, x, test, out var scopeIndex, out var isNotNull);

    Assert.True(matched);
    Assert.Equal(1, scopeIndex);
    Assert.False(isNotNull);
}

[Fact]
public void Declines_null_check_against_the_root_scope()
{
    // rootParam.Outer == null is never a meaningful join-scope null check — only an Inner side (index > 0)
    // can be absent after a left-outer $lookup+$unwind.
    var scope = NewScope(isLeftOuter: true);
    var x = NewRootParam();
    Expression test = Expression.Equal(
        Expression.PropertyOrField(x, "Outer"), Expression.Constant(null, typeof(OuterEntity)));

    var matched = NativeJoinScopeTranslator.TryMatchScopeNullCheck(scope, x, test, out _, out _);

    Assert.False(matched);
}

[Fact]
public void Declines_a_member_access_beyond_the_bare_scope_leaf()
{
    // ti.Inner.Name != null is a null-check on a FIELD of the joined entity, not on the join itself — the
    // matcher must require the OPERAND to be the bare synthetic scope parameter, no further member access.
    var scope = NewScope(isLeftOuter: true);
    var x = NewRootParam();
    Expression test = Expression.NotEqual(
        Expression.PropertyOrField(Expression.PropertyOrField(x, "Inner"), "Name"),
        Expression.Constant(null, typeof(string)));

    var matched = NativeJoinScopeTranslator.TryMatchScopeNullCheck(scope, x, test, out _, out _);

    Assert.False(matched);
}

[Fact]
public void Declines_a_self_compare_with_no_null_operand()
{
    var scope = NewScope(isLeftOuter: true);
    var x = NewRootParam();
    Expression test = Expression.Equal(
        Expression.PropertyOrField(x, "Inner"), Expression.PropertyOrField(x, "Inner"));

    var matched = NativeJoinScopeTranslator.TryMatchScopeNullCheck(scope, x, test, out _, out _);

    Assert.False(matched);
}

[Fact]
public void Declines_a_non_equality_test()
{
    var scope = NewScope(isLeftOuter: true);
    var x = NewRootParam();
    Expression test = Expression.AndAlso(Expression.Constant(true), Expression.Constant(true));

    var matched = NativeJoinScopeTranslator.TryMatchScopeNullCheck(scope, x, test, out _, out _);

    Assert.False(matched);
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~NativeJoinScopeTranslatorTests.Matches_bare_Inner_not_equal_null_at_depth_one|FullyQualifiedName~NativeJoinScopeTranslatorTests.Matches_null_equal_bare_Inner_operand_order_reversed|FullyQualifiedName~NativeJoinScopeTranslatorTests.Declines_null_check_against_the_root_scope|FullyQualifiedName~NativeJoinScopeTranslatorTests.Declines_a_member_access_beyond_the_bare_scope_leaf|FullyQualifiedName~NativeJoinScopeTranslatorTests.Declines_a_self_compare_with_no_null_operand|FullyQualifiedName~NativeJoinScopeTranslatorTests.Declines_a_non_equality_test"`

Expected: compile error — `TryMatchScopeNullCheck` does not exist yet. (This IS the correct "fails for the
right reason" signal for a brand-new method; if it fails any other way, fix the test first.)

- [ ] **Step 3: Implement `TryMatchScopeNullCheck`**

In `NativeJoinScopeTranslator.cs`, add alongside `TryMatchInnerNullCheck` (after its closing brace, before
`IsBareInnerAccess`):

```csharp
/// <summary>
/// Depth-agnostic generalization of <see cref="TryMatchInnerNullCheck"/>: recognizes
/// <c>rootParam.«Outer/Inner hop chain» == null</c> / <c>!= null</c> (either operand order) for ANY single
/// scope level (never the root — only an Inner side can be missing after a left-outer <c>$lookup</c>).
/// Resolves the null-checked operand via <see cref="TryRerootToSingleScope"/> — the same safe,
/// member-name-chain-based mechanism <see cref="TryTranslateSingleScope"/> uses — never by CLR-type or
/// <c>ReferenceEquals</c> comparison the way <see cref="TryMatchInnerNullCheck"/>'s flat, depth-1-only
/// <see cref="IsBareInnerAccess"/> does. Structural recognition only: callers must separately verify the
/// resolved level's <c>IsLeftOuter</c>/non-collection eligibility (a plain inner <c>Join</c> or a collection
/// navigation makes the null test degenerate — always true or always false).
/// </summary>
public static bool TryMatchScopeNullCheck(
    MongoJoinScope scope, ParameterExpression rootParam, Expression test,
    out int scopeIndex, out bool isNotNull)
{
    scopeIndex = -1;
    isNotNull = false;

    if (test is not BinaryExpression { NodeType: ExpressionType.Equal or ExpressionType.NotEqual } binary)
    {
        return false;
    }

    var leftIsBareScope = TryRerootToBareScope(scope, rootParam, binary.Left, out var leftIndex);
    var rightIsBareScope = TryRerootToBareScope(scope, rootParam, binary.Right, out var rightIndex);

    // Exactly one side must be a bare scope leaf — neither side (not this shape) or both sides (a degenerate
    // self-compare, e.g. `ti.Inner == ti.Inner`, no null involved) decline alike.
    if (leftIsBareScope == rightIsBareScope)
    {
        return false;
    }

    var (matchedIndex, otherSide) = leftIsBareScope ? (leftIndex, binary.Right) : (rightIndex, binary.Left);

    // scopeIndex 0 is the root scope, which can never be "missing" — only an Inner side (index > 0) can be.
    if (matchedIndex == 0 || otherSide is not ConstantExpression { Value: null })
    {
        return false;
    }

    scopeIndex = matchedIndex;
    isNotNull = binary.NodeType == ExpressionType.NotEqual;
    return true;
}

// Reroots `node` via TryRerootToSingleScope and additionally requires the REWRITTEN expression to be the
// bare synthetic scope parameter itself — no further member access — i.e. `node` was exactly
// `rootParam.Outer*.Inner?` with no trailing `.Something`.
private static bool TryRerootToBareScope(
    MongoJoinScope scope, ParameterExpression rootParam, Expression node, out int scopeIndex)
{
    scopeIndex = -1;
    if (!TryRerootToSingleScope(scope, rootParam, node, out var index, out var rewritten)
        || rewritten is not ParameterExpression)
    {
        return false;
    }

    scopeIndex = index;
    return true;
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the same filter as Step 2.
Expected: all six PASS.

- [ ] **Step 5: Run the whole file to check for regressions**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~NativeJoinScopeTranslatorTests"`
Expected: PASS, no regressions (this task only adds new methods; nothing existing is touched).

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeTranslator.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeTranslatorTests.cs
git commit -m "EF-322: add depth-agnostic join-scope null-check matcher"
```

---

### Task 2: Aggregation-expression rendering for `MongoLookupNullCheckExpression`

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoLookupNullCheckExpression.cs` (stale
  doc-comment fix only)
- Modify: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoExpressionNodeCoverageTests.cs`
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs`

**Interfaces:**
- Consumes: `MongoLookupNullCheckExpression.LookupAlias`/`IsNotNull` (existing, `Expressions/MongoLookupNullCheckExpression.cs`);
  the renderer's own `FieldRef(path, elementVariable)` helper (existing, same file as the new case).
- Produces: `MongoAggregationExpressionRenderer.Render`/`CanRender` now handle `MongoLookupNullCheckExpression` —
  consumed by Task 3/4's end-to-end pipeline (this node only ever appears as a `MongoConditionalExpression.Test`
  rendered inside `$project`).

- [ ] **Step 1: Write the failing test**

Add to `MongoAggregationExpressionRendererTests.cs` (match that file's existing `Render`/`PlaceholderTable`
call convention — check the top of the file for the exact helper names used by neighboring tests, e.g. how a
`MongoFieldExpression` test builds its `PlaceholderTable` and asserts the returned `BsonValue`, and mirror that
exactly rather than inventing a new helper):

```csharp
[Fact]
public void Renders_not_equal_null_lookup_check_as_ne_against_the_alias_field()
{
    var node = new MongoLookupNullCheckExpression("_lookup_Manager", isNotNull: true);
    var placeholders = new PlaceholderTable();

    var rendered = MongoAggregationExpressionRenderer.Render(node, placeholders);

    Assert.Equal(
        new BsonDocument("$ne", new BsonArray { "$_lookup_Manager", BsonNull.Value }),
        rendered);
}

[Fact]
public void Renders_equal_null_lookup_check_as_eq_against_the_alias_field()
{
    var node = new MongoLookupNullCheckExpression("_lookup_Manager", isNotNull: false);
    var placeholders = new PlaceholderTable();

    var rendered = MongoAggregationExpressionRenderer.Render(node, placeholders);

    Assert.Equal(
        new BsonDocument("$eq", new BsonArray { "$_lookup_Manager", BsonNull.Value }),
        rendered);
}

[Fact]
public void CanRender_reports_true_for_a_lookup_null_check()
{
    Assert.True(MongoAggregationExpressionRenderer.CanRender(
        new MongoLookupNullCheckExpression("_lookup_Manager", isNotNull: true)));
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~Renders_not_equal_null_lookup_check_as_ne_against_the_alias_field|FullyQualifiedName~Renders_equal_null_lookup_check_as_eq_against_the_alias_field|FullyQualifiedName~CanRender_reports_true_for_a_lookup_null_check"`

Expected: FAIL with `NativeTranslationNotSupportedException: MongoAggregationExpressionRenderer does not
support node type 'MongoLookupNullCheckExpression'.` (for the two `Render` tests) and an assertion failure
(`Assert.True` given `false`) for the `CanRender` test.

- [ ] **Step 3: Implement the renderer cases**

In `MongoAggregationExpressionRenderer.cs`, add a case to `Render`'s switch, right after the
`MongoOuterFieldExpression outer` arm (so it sits beside the other simple field-referencing cases):

```csharp
MongoOuterFieldExpression outer => FieldRef(outer.ElementName, elementVariable: null),
// EF-322: the ONLY consumer today is a Select-side join-scope null-check ternary
// (`ti.Inner != null ? ti.Inner.City : null`) whose Test is rendered here as the MongoConditionalExpression's
// "if" — see NativeJoinScopeProjectionBinder.TryBindConditionalProjection. LookupAlias is a plain top-level
// field name (the $lookup's own "as"), so it renders through the same FieldRef helper as any other field.
MongoLookupNullCheckExpression lookupNullCheck
    => new BsonDocument(lookupNullCheck.IsNotNull ? "$ne" : "$eq",
        new BsonArray { FieldRef(lookupNullCheck.LookupAlias, elementVariable), BsonNull.Value }),
MongoConstantExpression or MongoParameterExpression => MongoValueRenderer.RenderValue(node, placeholders),
```

(Insert the new case between the existing `MongoOuterFieldExpression` arm and the existing
`MongoConstantExpression or MongoParameterExpression` arm — do not reorder or duplicate either of those two.)

Add the matching `CanRender` arm, extending the existing simple-field-types line:

```csharp
MongoFieldExpression or MongoElementRefExpression or MongoOuterFieldExpression or MongoLookupNullCheckExpression => true,
```

(Replace the existing `MongoFieldExpression or MongoElementRefExpression or MongoOuterFieldExpression => true,`
line at the top of `CanRender`'s switch with the line above — do not add a second, separate arm.)

Then fix the stale doc-comment in `MongoLookupNullCheckExpression.cs`: replace

```
/// Produced only by <c>NativeJoinScopeTranslator.TryTranslateReferenceIncludeNullCheck</c>, recognizing the
```

with

```
/// Produced by <c>NativeJoinScopeTranslator.TryMatchInnerNullCheck</c> (the flat, depth-1, Where-only shape)
/// and <c>TryMatchScopeNullCheck</c> (the depth-agnostic, Select-side shape), recognizing the
```

and update the paragraph's closing sentence — it currently ends "...never nested under a `Not`, quantifier, or
`$elemMatch`, so neither ... need to recognize it" and the summary's "Query-dialect only ... there is no
aggregation-expression form because there is nothing else this node needs to express" is now WRONG (Task 2
just added one) — replace that sentence with: "Has both a query-dialect form (`RenderLookupNullCheck`, for the
`Where`-position shape) and an aggregation-expression form (`MongoAggregationExpressionRenderer`, for the
Select-side conditional-Test shape)."

- [ ] **Step 4: Run the tests to verify they pass**

Run the same filter as Step 2.
Expected: all three PASS.

- [ ] **Step 5: Update the reflection-based coverage matrix**

`MongoExpressionNodeCoverageTests.cs` reflection-discovers every `MongoExpression` subtype and pins each
dispatcher's answer for it (`Query/AGENTS.md`'s "Negator ↔ query-dialect classifier ↔ both renderers must
agree" invariant) — this test will now FAIL for `MongoLookupNullCheckExpression`'s `Agg.CanRender`/`Agg.Render`
rows since Step 3 changed those two answers. Update the two rows (around the existing block quoted below) from:

```csharp
["MongoLookupNullCheckExpression|Agg.CanRender"] = "false",
["MongoLookupNullCheckExpression|Agg.Render"] = "declined",
```

to:

```csharp
["MongoLookupNullCheckExpression|Agg.CanRender"] = "true",
["MongoLookupNullCheckExpression|Agg.Render"] = "rendered",
```

and update the comment immediately above that block (currently: *"Query-dialect-only by design (see the node's
own remarks): it is produced only for the exact `ti.Inner == null`/`!= null` top-level Where shape, never
nested under Not/a quantifier/$elemMatch, so none of the other six dispatchers need an arm for it..."*) to:

```csharp
// EF-322: now has BOTH a query-dialect form (Where-position, produced by TryMatchInnerNullCheck) and an
// aggregation-expression form (Select-position conditional Test, produced by TryMatchScopeNullCheck) — see
// the node's own remarks. Still never nested under Not/a quantifier/$elemMatch (the shapes that produce it
// never place it there), so the remaining four dispatchers are unaffected.
```

- [ ] **Step 6: Run the coverage test and the whole renderer test file to verify no regressions**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~MongoExpressionNodeCoverageTests|FullyQualifiedName~MongoAggregationExpressionRendererTests"`
Expected: all PASS.

- [ ] **Step 7: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoAggregationExpressionRenderer.cs \
        src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoLookupNullCheckExpression.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoExpressionNodeCoverageTests.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoAggregationExpressionRendererTests.cs
git commit -m "EF-322: render MongoLookupNullCheckExpression in the aggregation-expression dialect"
```

---

### Task 3: `NativeJoinScopeProjectionBinder.TryBindConditionalProjection` + `TranslateSelect` wiring

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeProjectionBinderTests.cs`

**Interfaces:**
- Consumes: `NativeJoinScopeTranslator.TryMatchScopeNullCheck` (Task 1);
  `NativeJoinScopeTranslator.TryTranslateSingleScope(MongoJoinScope, ParameterExpression, Expression, bool
  valueMode, out MongoExpression?)` (existing); `NativeJoinScopeProjectionBinder.ConfirmEntireChain` (existing,
  same file, line ~511); `NativeProjectionBinder.SyntheticBareProjectionAlias` (existing, `"_v"`);
  `IsSingleEligibleNativeJoinScope(MongoQueryExpression, out JoinInfo)` (existing, QMTEV);
  `BindSelectManyMember(MongoQueryExpression, string alias, Expression valueExpression)` (existing, QMTEV,
  line ~3228).
- Produces: `internal static bool TryBindConditionalProjection(MongoQueryExpression mongoQ, LambdaExpression
  selector, JoinInfo joinInfo)`, and `TranslateSelect` now routes this shape natively end-to-end — consumed by
  Task 4 (baseline regeneration).

**Why these two are one task, not two:** `NativeJoinScopeProjectionBinderTests.cs`'s established convention
(see its own class remarks) is to drive the REAL EF Core preprocessor +
`MongoQueryableMethodTranslatingExpressionVisitor` end to end (via the file's `TranslateJoinQuery` helper) and
assert on the resulting `MongoQueryExpression` — never to hand-build expression trees or call an internal
binder method directly. A new binder method is only reachable through that pipeline once `TranslateSelect`
itself calls it, so testing the binder in isolation before the wiring exists would mean writing a test that
cannot pass yet for reasons unrelated to the binder's own correctness — and testing it only after both pieces
exist is indistinguishable, at this granularity, from testing them together. Splitting them would produce a
Task 3 whose "GREEN" step cannot actually turn green. Since `TranslateJoinQuery`'s fixtures (`Owner`/`Order`)
use a plain `Join`, and this feature specifically needs a `LeftJoin` (only a left-outer join can produce a null
Inner to check), this task also adds a `LeftJoin`-based query-building overload alongside the existing one.

- [ ] **Step 1: Write the failing tests**

Add to `NativeJoinScopeProjectionBinderTests.cs`, immediately after `TranslateJoinQuery` (reuses its exact
body, swapping `Join` for `LeftJoin`):

```csharp
private static MongoQueryExpression TranslateLeftJoinQuery(
    Func<IQueryable<Owner>, IQueryable<Order>, IQueryable> buildQuery)
{
    using var db = SingleEntityDbContext.Create<Owner>(mb => mb.Entity<Order>());

    var query = buildQuery(db.Set<Owner>(), db.Set<Order>());

    var ccFactory = db.GetService<IQueryCompilationContextFactory>();
    var compilationContext = ccFactory.Create(async: false);

    var preprocessor = db.GetService<IQueryTranslationPreprocessorFactory>().Create(compilationContext);
    var preprocessed = preprocessor.Process(query.Expression);

    var visitor = db.GetService<IQueryableMethodTranslatingExpressionVisitorFactory>().Create(compilationContext);
    var result = visitor.Visit(preprocessed);

    Assert.NotNull(result);
    var shaped = Assert.IsAssignableFrom<ShapedQueryExpression>(result);
    return Assert.IsType<MongoQueryExpression>(shaped.QueryExpression);
}

[Fact]
public void Binds_a_bare_nav_null_check_ternary_over_a_left_join()
{
    // `(o, r) => r != null ? r.Total : 0m` after a LeftJoin — mirrors Manual_expression_tree_typed_null_equality's
    // nav-expanded shape (`ti.Inner != null ? ti.Inner.City : null`), using a decimal default instead of a
    // typed null so the test doesn't depend on Task 1-5's null-branch handling specifically — TranslateOperand's
    // generic ConstantExpression fall-through already handles either.
    var mongoQ = TranslateLeftJoinQuery((owners, orders) =>
        owners.GroupJoin(orders, o => o.Id, r => r.OwnerId, (o, rs) => new { o, rs })
            .SelectMany(x => x.rs.DefaultIfEmpty(), (x, r) => new { x.o, r })
            .Select(x => x.r != null ? x.r.Total : 0m));

    Assert.NotNull(mongoQ.Select.JoinScope);
    Assert.True(mongoQ.Select.JoinScope!.Levels[0].IsLeftOuter);
    Assert.Equal(
        [NativeProjectionBinder.SyntheticBareProjectionAlias],
        mongoQ.Select.Projection.Select(p => p.Alias).ToArray());

    var leaf = Assert.IsType<MongoConditionalExpression>(mongoQ.Select.Projection[0].Expression);
    var test = Assert.IsType<MongoLookupNullCheckExpression>(leaf.Test);
    Assert.True(test.IsNotNull);
    Assert.Equal(mongoQ.Select.JoinScope!.Levels[0].InnerPrefix, test.LookupAlias);

    Assert.Single(mongoQ.Lookups);
    Assert.False(mongoQ.Select.HasUnconfirmedCandidateJoin);
}

[Fact]
public void Declines_when_the_checked_level_is_an_inner_not_left_outer_join()
{
    // A plain Join never produces a null Inner (an unmatched row is DROPPED, not unwound-as-null), so the
    // null check is degenerate there — must decline, not silently admit an always-true/always-false test.
    var mongoQ = TranslateJoinQuery((owners, orders) =>
        owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Select(x => x.r != null ? x.r.Total : 0m));

    Assert.Equal(NativeRoute.Fallback, mongoQ.Select.Route);
}

[Fact]
public void Declines_a_conditional_whose_test_is_not_a_scope_null_check()
{
    var mongoQ = TranslateLeftJoinQuery((owners, orders) =>
        owners.GroupJoin(orders, o => o.Id, r => r.OwnerId, (o, rs) => new { o, rs })
            .SelectMany(x => x.rs.DefaultIfEmpty(), (x, r) => new { x.o, r })
            .Select(x => x.o.Name == "Alice" ? 1m : 0m));

    Assert.Equal(NativeRoute.Fallback, mongoQ.Select.Route);
}
```

Add the necessary `using MongoDB.EntityFrameworkCore.Query.NativeTranslation;` if not already present in this
file (needed for `NativeProjectionBinder.SyntheticBareProjectionAlias`).

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~Binds_a_bare_nav_null_check_ternary_over_a_left_join|FullyQualifiedName~Declines_when_the_checked_level_is_an_inner_not_left_outer_join|FullyQualifiedName~Declines_a_conditional_whose_test_is_not_a_scope_null_check"`

Expected: `Binds_a_bare_nav_null_check_ternary_over_a_left_join` FAILs (`mongoQ.Select.Projection` is empty — the
binder doesn't exist and nothing calls it yet, so `TranslateSelect` falls through to the generic path and
marks non-native); the two `Declines_*` tests currently PASS already (the shape already falls back today) —
that's expected and fine, they exist to pin the decline behavior going forward, not to prove new failure.

- [ ] **Step 3: Implement `TryBindConditionalProjection`**

In `NativeJoinScopeProjectionBinder.cs`, add a new public method after `TryBindProjection`'s closing brace
(before `ConfirmEntireChain`):

```csharp
/// <summary>
/// Attempts to populate the native <c>$project</c> slot for a BARE (non-wrapped) <c>Select</c> body that is
/// exactly a ternary null-checking a join scope's Inner side and dereferencing it —
/// <c>ti =&gt; ti.Inner != null ? ti.Inner.City : null</c>, at any depth of a (possibly chained) join scope.
/// Sibling to <see cref="TryBindProjection"/> (which only ever handles a wrapped <c>new {}</c>/<c>MemberInit</c>
/// body — <c>selector.Body.TryGetProjectionMembers</c> never recognizes a bare <see cref="ConditionalExpression"/>,
/// so that method is never reached for this shape). See
/// docs/superpowers/specs/2026-09-23-native-join-scope-nav-null-conditional-projection-design.md.
/// </summary>
internal static bool TryBindConditionalProjection(
    MongoQueryExpression mongoQ, LambdaExpression selector, JoinInfo joinInfo)
{
    if (mongoQ.Select.JoinScope is not { } scope
        || joinInfo.Lookup is null
        || mongoQ.Select.Projection.Count > 0
        || selector.Parameters.Count != 1
        || selector.Body is not ConditionalExpression conditional)
    {
        return false;
    }

    var rootParam = selector.Parameters[0];

    if (!NativeJoinScopeTranslator.TryMatchScopeNullCheck(scope, rootParam, conditional.Test, out var scopeIndex, out var isNotNull))
    {
        return false;
    }

    // scopeIndex is 1-based over Levels (see TryMatchScopeNullCheck's own contract: 0 is the root, never
    // returned); mongoQ.Joins is the parallel 0-based list TranslateJoinCore built the scope from.
    var checkedJoin = mongoQ.Joins[scopeIndex - 1];
    var level = scope.Levels[scopeIndex - 1];

    // Degenerate-check guard: a plain inner Join drops an unmatched row entirely rather than unwinding it as
    // an explicit null, so "Inner != null" is unconditionally true there (and "== null" unconditionally
    // false) — not a real check. A collection navigation is a different shape (many joined rows, not a
    // single nullable one) with no single "is it null" answer. Mirrors NativeSlotPopulator's Where-arm
    // conjunct (`Lookup: { Navigation.IsCollection: false }` + `IsLeftOuter: true`), re-derived PER LEVEL here
    // since a chain can mix Join/LeftJoin levels.
    if (!level.IsLeftOuter || checkedJoin.Navigation is { IsCollection: true })
    {
        return false;
    }

    if (!NativeJoinScopeTranslator.TryTranslateSingleScope(scope, rootParam, conditional.IfTrue, valueMode: true, out var ifTrue)
        || !NativeJoinScopeTranslator.TryTranslateSingleScope(scope, rootParam, conditional.IfFalse, valueMode: true, out var ifFalse))
    {
        return false;
    }

    var testExpr = new MongoLookupNullCheckExpression(level.InnerPrefix, isNotNull);
    var leaf = new MongoConditionalExpression(testExpr, ifTrue, ifFalse);

    mongoQ.Select.AddProjection(new MongoProjection(NativeProjectionBinder.SyntheticBareProjectionAlias, leaf));
    ConfirmEntireChain(mongoQ, scope);
    return true;
}
```

Add `using System.Linq.Expressions;` if not already present at the top of the file (it already is, per the
file's current `using` block).

- [ ] **Step 4: Wire the new arm into `TranslateSelect`**

In `MongoQueryableMethodTranslatingExpressionVisitor.cs`'s `TranslateSelect`, add a new `else if` immediately
after the wrapped-projection arm's closing brace (the block ending at line 619, `return
source.UpdateShaperExpression(BuildSelectManyResultShaper(mongoQueryExpression, selector.Body,
foldedJoinBody));`) and before the `else if (!IsTransparentIdentifierSelector(selector) && ...)` projected-Select
branch at line 620:

```csharp
// A BARE (non-wrapped) `Select` body that is exactly a ternary null-checking a join scope's Inner side and
// dereferencing it — `ti => ti.Inner != null ? ti.Inner.City : null` (EF-322, native join-scope
// nav-null-check conditional projection). Sibling to the wrapped-projection arm above, structurally disjoint
// from it (a bare ConditionalExpression is never a NewExpression/MemberInit) and from the bare-leaf
// whole-entity arm above it (that recognizer matches only an UNADORNED `x.Outer`/`x.Inner` member access, a
// ConditionalExpression is neither). No fold/BuildSelectManyResultShaper needed: the staged leaf is a single
// bare scalar/computed value, not a wrapped anonymous/DTO construction, so it is bound exactly the way the
// GroupBy/SelectMany bare-leaf branches above bind their own single reserved alias — BindSelectManyMember,
// which registers the RAW selector.Body under the SAME "_v" alias TryBindConditionalProjection just staged
// into the native IR, and returns a ProjectionBindingExpression the DOM shaper reads by index.
else if (IsSingleEligibleNativeJoinScope(mongoQueryExpression, out var conditionalLeafJoin)
         && NativeJoinScopeProjectionBinder.TryBindConditionalProjection(mongoQueryExpression, selector, conditionalLeafJoin))
{
    var boundBareLeaf = BindSelectManyMember(
        mongoQueryExpression, NativeProjectionBinder.SyntheticBareProjectionAlias, selector.Body);
    return source.UpdateShaperExpression(boundBareLeaf);
}
```

(`NativeProjectionBinder.SyntheticBareProjectionAlias` requires no new `using` — `NativeProjectionBinder` is
already referenced elsewhere in this file, e.g. the existing call to
`NativeProjectionBinder.TryPopulateNativeProjection` at line 651.)

- [ ] **Step 5: Run the tests to verify they pass**

Run the same filter as Step 2.
Expected: all three PASS.

- [ ] **Step 6: Run the whole file to check for regressions**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~NativeJoinScopeProjectionBinderTests"`
Expected: PASS, no regressions.

- [ ] **Step 7: Prove nativity end-to-end via `NativeOnly` on a real, non-hand-built query**

Add a functional test (not unit) confirming the SPEC test's own shape now goes native. Add to
`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/` — check whether an existing
`NativeJoinScopeNestedProjectionTests.cs`-style file is the natural home, or add a new
`NativeJoinScopeConditionalProjectionTests.cs` file mirroring that one's structure (model, `TestServer`
fixture, `[Fact]` calling the query under `MongoQueryMode.NativeOnly` and asserting no exception / correct
rows). Since this file's exact fixture/harness conventions depend on reading
`NativeJoinScopeNestedProjectionTests.cs` directly (not reproduced here), the concrete step is:

1. Read `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinScopeNestedProjectionTests.cs` in
   full.
2. Add a new `[Fact]` (or a new sibling file, following that one's structure exactly) that runs, under
   `MongoQueryMode.NativeOnly`:
   ```csharp
   dbContext.Orders
       .Where(o => o.OrderID < 10300)
       .Select(o => o.Customer != null ? o.Customer.City : null)
       .ToList();
   ```
   against that file's own fixture entities (or the closest equivalent — a required/optional reference
   navigation), and asserts the call does NOT throw `NativeTranslationNotSupportedException`, with the result
   compared against an in-memory LINQ oracle over the same query (per `Query/AGENTS.md`'s "Differential
   correctness" testing guidance for a native shape that can affect results) — construct the oracle via
   `.AsEnumerable()` or an equivalent in-memory `List<T>` fixture, not by re-running the same LINQ against the
   provider.
3. Add a second `[Fact]` for the chained-scope case (two joins, null-check at the second level) using whatever
   two-navigation fixture is most natural to add to that file's model, or a new minimal one.

- [ ] **Step 8: Run the new functional tests**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~NativeJoinScopeConditionalProjection"`
(adjust the filter to match whatever class name Step 7 actually used)
Expected: PASS.

- [ ] **Step 9: Run the full Query unit + functional suites for regressions**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query"`
Expected: PASS, no regressions. Pay particular attention to any existing test asserting `Route == Fallback`
for a join-scope Select this new arm might now (correctly) flip — if one exists and its assertion changes from
`Fallback` to a native route, that is an intentional, expected change from this feature, not a regression;
verify with `git diff` that the changed assertion is one this plan's scope covers before accepting it.

- [ ] **Step 10: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs \
        src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeProjectionBinderTests.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/
git commit -m "EF-322: go native for join-scope nav-null-check conditional projections"
```

---

### Task 4: Flip `Manual_expression_tree_typed_null_equality` and regenerate its baseline

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindMiscellaneousQueryMongoTest.cs`
  (baseline only — regenerated, not hand-edited)

**Interfaces:** none new — this task only proves the spec test itself now goes native and regenerates its
recorded MQL baseline.

- [ ] **Step 1: Confirm the shape now goes native under `NativeOnly`**

Run: `MONGODB_EF_NATIVE_ONLY=1 dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~Manual_expression_tree_typed_null_equality"`
Expected: PASS for both `async: true`/`async: false` variants (previously threw
`NativeTranslationNotSupportedException`).

- [ ] **Step 2: Regenerate the MQL baseline**

Run:

```bash
EF_TEST_REWRITE_BASELINES=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindMiscellaneousQueryMongoTest.Manual_expression_tree_typed_null_equality"
```

This rewrites the `AssertMql` override in `NorthwindMiscellaneousQueryMongoTest.cs` in place from the captured
MQL (the test itself still reports as failed when the var is set — that's expected, per
`SpecificationTests/AGENTS.md`). Inspect the diff: confirm the new baseline reflects the NATIVE pipeline's own
`$lookup`+`$unwind`+`$project{$cond:...}` shape (which may differ from today's fallback-shaped baseline quoted
in the design doc's Problem section, or may coincidentally match it — `Query/AGENTS.md`'s "MQL shape cannot
prove nativity" cuts both ways, so do not assume either outcome ahead of running it). Re-run the same filter
WITHOUT `EF_TEST_REWRITE_BASELINES` afterward to confirm it now passes normally.

- [ ] **Step 3: Run the full spec suite for regressions**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~SpecificationTests"`
Expected: PASS, no regressions elsewhere (this feature only ADDS a new native-eligible shape; any other test's
baseline changing is a red flag — investigate before accepting).

- [ ] **Step 4: Run all three EF version configurations**

Run (or invoke the `/test-all` skill, per this repo's `AGENTS.md`):
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" --no-build
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build
```
Expected: PASS on all three. If EF8/EF9 decline this shape for a reason unrelated to this plan (e.g. the
`Ef8Ef9LeftJoinMethod` shim gap `NativeJoinScopeProjectionBinder.cs`'s own remarks describe for nested
projections — check whether the same gap applies here), that is an acceptable, pre-existing, family-wide
limitation to note in the commit message, not a blocker — confirm it is that SAME gap (measured, not assumed)
before treating it as expected.

- [ ] **Step 5: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindMiscellaneousQueryMongoTest.cs
git commit -m "EF-322: regenerate baseline now that Manual_expression_tree_typed_null_equality goes native"
```
