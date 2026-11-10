### Task 1: Wire the Include bridge and get the Outer-leaf test green

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Test (already exists, uncommitted): `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`

**Interfaces:**
- Produces: `MongoProjectionBindingExpressionVisitor.VisitIncludeExpression(MongoQueryExpression, IncludeExpression) : Expression` — Task 2/3 don't call this directly (they exercise it transitively through the same `BindResultMember` fix), but any FUTURE arm needing the same bridge should reuse this method rather than re-deriving it.
- Consumes: `MongoProjectionBindingExpressionVisitor`'s existing private `_queryExpression` field and `VisitExtension`'s existing `case IncludeExpression` (line ~507 today) and `case StructuralTypeShaperExpression structuralTypeShaperExpression` (line ~461 today, the "already bound by index" arm) — unchanged by this task, just reached from a new caller.

- [ ] **Step 1: Confirm the two existing functional tests are present and red**

Run:
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~Whole_outer_entity_leaf_that_is_also_reference_included_goes_native|FullyQualifiedName~Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join"
```
Expected: both FAIL. The Outer test fails with `System.FormatException: Element '_lookup_Orders' does not
match any field or property of class Owner`. The Inner test fails with an `Assert.Equal` collection mismatch
(`Alice`'s `Total` reads as `null` instead of `10`). If either fails differently (e.g. compiles differently,
throws something else), STOP and use `superpowers:systematic-debugging` — the starting state this plan
assumes has changed since the design doc was written.

- [ ] **Step 2: Add the `VisitIncludeExpression` bridge method**

In `MongoProjectionBindingExpressionVisitor.cs`, immediately after the closing brace of `Translate` (the method
ending `return MatchTypes(result, expression.Type); }`, right before the `/// <inheritdoc />` comment on
`Visit`), add:

```csharp
    /// <summary>
    /// Visits a single, already correctly re-bound <see cref="IncludeExpression"/> subtree — produced by
    /// <c>MongoQueryableMethodTranslatingExpressionVisitor.BindResultMember</c> re-wrapping a rebound
    /// join-scope whole-entity shaper — WITHOUT going through <see cref="Translate"/>'s wholesale
    /// <see cref="MongoQueryExpression.ReplaceProjectionMapping"/> side effect. The caller's own projection
    /// mapping (already correctly built via index-based <see cref="MongoQueryExpression.AddToProjection"/>
    /// calls) must survive untouched; only <see cref="VisitExtension"/>'s existing <see cref="IncludeExpression"/>
    /// handling — registering the Include's own collection <c>$lookup</c> and rewriting the node into a
    /// reducible shape via <c>RewriteCollectionIncludeForLookup</c> — is needed here. See
    /// docs/superpowers/specs/2026-09-25-native-join-scope-include-materialization-design.md, "Design options",
    /// Option B (confirmed correct by reading <c>RewriteCollectionIncludeForLookup</c>'s own
    /// "already wrapped in one or more IncludeExpressions" handling, which anticipates exactly this shape).
    /// </summary>
    internal Expression VisitIncludeExpression(MongoQueryExpression queryExpression, IncludeExpression includeExpression)
    {
        var previousQueryExpression = _queryExpression;
        _queryExpression = queryExpression;
        try
        {
            return Visit(includeExpression);
        }
        finally
        {
            _queryExpression = previousQueryExpression;
        }
    }
```

- [ ] **Step 3: Thread the visitor instance through `BuildSelectManyWrappedShaper` and `BuildSelectManyResultShaper`**

In `MongoQueryableMethodTranslatingExpressionVisitor.cs`:

Find `private static ShapedQueryExpression BuildSelectManyWrappedShaper(` (currently line 3302) and change its
signature and its one internal call to `BuildSelectManyResultShaper`:

```csharp
    private static ShapedQueryExpression BuildSelectManyWrappedShaper(
        ShapedQueryExpression source, MongoQueryExpression mongoQueryExpression, LambdaExpression collectionSelector,
        LambdaExpression resultSelector, MongoProjectionBindingExpressionVisitor projectionBindingExpressionVisitor)
```

and inside its body, change:
```csharp
        var innerShaper = BuildSelectManyResultShaper(mongoQueryExpression, innerLambda.Body);
```
to:
```csharp
        var innerShaper = BuildSelectManyResultShaper(mongoQueryExpression, innerLambda.Body, projectionBindingExpressionVisitor);
```

Update its one call site (currently line 3258):
```csharp
        return BuildSelectManyWrappedShaper(source, mongoQueryExpression, collectionSelector, resultSelector);
```
to:
```csharp
        return BuildSelectManyWrappedShaper(source, mongoQueryExpression, collectionSelector, resultSelector, _projectionBindingExpressionVisitor);
```

Now find `private static Expression BuildSelectManyResultShaper(` (currently line 3325) and change its
signature (the new parameter goes before the existing optional `foldedBody = null`, since optional parameters
must stay last):

```csharp
    private static Expression BuildSelectManyResultShaper(
        MongoQueryExpression mongoQueryExpression, Expression projectionBody,
        MongoProjectionBindingExpressionVisitor projectionBindingExpressionVisitor, Expression? foldedBody = null)
```

Inside its body, change the `BindResultMember` call:
```csharp
            boundValues[i] = BindResultMember(
                mongoQueryExpression, members[i].MemberName, members[i].Value,
                foldedMembers is not null && i < foldedMembers.Count ? foldedMembers[i].Value : null);
```
to:
```csharp
            boundValues[i] = BindResultMember(
                mongoQueryExpression, members[i].MemberName, members[i].Value, projectionBindingExpressionVisitor,
                foldedMembers is not null && i < foldedMembers.Count ? foldedMembers[i].Value : null);
```

Update its other two call sites. First, in the bare/wrapped `SelectMany` branch (currently around line 413):
```csharp
                : BuildSelectManyResultShaper(mongoQueryExpression, selector.Body);
```
to:
```csharp
                : BuildSelectManyResultShaper(mongoQueryExpression, selector.Body, _projectionBindingExpressionVisitor);
```

Second, in `TranslateSelect`'s wrapped-leaf-join arm (currently around line 618):
```csharp
            return source.UpdateShaperExpression(
                BuildSelectManyResultShaper(mongoQueryExpression, selector.Body, foldedJoinBody));
```
to:
```csharp
            return source.UpdateShaperExpression(
                BuildSelectManyResultShaper(mongoQueryExpression, selector.Body, _projectionBindingExpressionVisitor, foldedJoinBody));
```

- [ ] **Step 4: Rewrite `BindResultMember` to unwrap, rebuild, re-wrap, and bridge**

Find `private static Expression BindResultMember(` (currently line 3377). Replace its full body:

```csharp
    private static Expression BindResultMember(
        MongoQueryExpression mongoQueryExpression, string alias, Expression valueExpression,
        MongoProjectionBindingExpressionVisitor projectionBindingExpressionVisitor, Expression? foldedExpression)
    {
        // EF-322 Phase 2 Group B (root cause A2 — materialization half): a whole-entity leaf that is ALSO the
        // target of an Include/ThenInclude arrives here as IncludeExpression { EntityExpression: <the folded
        // shaper>, ... } rather than the bare StructuralTypeShaperExpression itself. Unwrap down to the
        // innermost EntityExpression before the shape check below (mirroring
        // NativeJoinScopeProjectionBinder.TryBindProjection's own unwrap for the SAME reason), rebuild the
        // shaper exactly as before, then re-wrap the REBUILT shaper back inside the SAME chain of
        // IncludeExpressions (innermost first) — preserving each wrapper's own, untouched NavigationExpression
        // — and route the re-wrapped chain through VisitIncludeExpression so the Include's own $lookup
        // registration and node-rewrite (MongoProjectionBindingExpressionVisitor.VisitExtension's
        // IncludeExpression case) actually runs. Without this, the shape check below fails on the wrapped
        // leaf, execution falls through to BindSelectManyMember, and the RAW (Include-wrapped, unfolded) leaf
        // gets registered under the member's own alias — a field the native $project never emits under (the
        // Outer leaf emits under $$ROOT, the Inner leaf under its own fixed InnerPrefix, never the member's
        // alias) — silently misreading the whole leaf. See the design doc's "Root cause, precisely" section
        // for the full trace (docs/superpowers/specs/2026-09-25-native-join-scope-include-materialization-design.md).
        var includeWrappers = new List<IncludeExpression>();
        var unwrappedFoldedExpression = foldedExpression;
        while (unwrappedFoldedExpression is IncludeExpression includeToUnwrap)
        {
            includeWrappers.Add(includeToUnwrap);
            unwrappedFoldedExpression = includeToUnwrap.EntityExpression;
        }

        if (unwrappedFoldedExpression is StructuralTypeShaperExpression shaper
            && shaper.ValueBufferExpression is ProjectionBindingExpression shaperBinding)
        {
            var entityProjection = shaperBinding.Index is int existingIndex
                                   && shaperBinding.QueryExpression == mongoQueryExpression
                ? (EntityProjectionExpression)mongoQueryExpression.Projection[existingIndex].Expression
                : (EntityProjectionExpression)mongoQueryExpression.GetMappedProjection(shaperBinding.ProjectionMember!);

            var entityIndex = mongoQueryExpression.AddToProjection(entityProjection, alias);

            Expression rebound = shaper.Update(
                new ProjectionBindingExpression(mongoQueryExpression, entityIndex, typeof(ValueBuffer)));

            if (includeWrappers.Count == 0)
            {
                return rebound;
            }

            for (var i = includeWrappers.Count - 1; i >= 0; i--)
            {
                rebound = includeWrappers[i].Update(rebound, includeWrappers[i].NavigationExpression);
            }

            return projectionBindingExpressionVisitor.VisitIncludeExpression(mongoQueryExpression, (IncludeExpression)rebound);
        }

        // A NESTED wrapped leaf sourced from a join scope (native-join-scope-nested-projection ticket):
        // NativeJoinScopeProjectionBinder.TryBindProjection already translated this alias into a
        // MongoDocumentConstructionExpression and staged it into mongoQueryExpression.Select.Projection (the
        // native IR list — separate from mongoQueryExpression.Projection, the EF-facing list this method builds
        // the shaper against). Falling through to BindSelectManyMember below would register the RAW, untranslated
        // valueExpression (the original `new { Name = o.Customer!.Name }` NewExpression) under this alias
        // instead — MongoProjectionBindingRemovingExpressionVisitor's MongoDocumentConstructionExpression case
        // (VisitExtension) would then never match, and the shaper would fall through to an ordinary alias read
        // that hands the WHOLE anonymous member type to BsonBinding.GetElementValue<T>, which has no serializer
        // for an anonymous type and throws (MEASURED: "Unsupported collection type '<>f__AnonymousTypeN<...>'").
        // Registering the ALREADY-TRANSLATED MongoDocumentConstructionExpression instead (not the raw
        // valueExpression) is what makes the downstream MongoDocumentConstructionExpression case fire and read
        // each member back via its own dotted alias.memberName path.
        //
        // The lookup goes through MongoSelectDefinition.TryGetDocumentConstructionProjection — the SAME method
        // MongoProjectionBindingExpressionVisitor.TryGetNativeDocumentConstructionLeaf uses for the plain-root
        // EF-447 leaf — rather than a local alias scan. Final-review Finding 3: the two used to be near-
        // identical scans with different admission rules (this one omitted the Route == Projection check, the
        // CLR-type check and the alias-override mapping), which is exactly how a looser lookup ends up matching
        // a staged node the stricter one refused and reading it back under a member it does not describe.
        if (mongoQueryExpression.Select.TryGetDocumentConstructionProjection(
                alias, valueExpression.Type, out var construction))
        {
            var constructionIndex = mongoQueryExpression.AddToProjection(construction, alias);
            return new ProjectionBindingExpression(mongoQueryExpression, constructionIndex, valueExpression.Type);
        }

        return BindSelectManyMember(mongoQueryExpression, alias, valueExpression);
    }
```

Only the top comment block and the new unwrap/rewrap/bridge logic (through the `return
projectionBindingExpressionVisitor.VisitIncludeExpression(...)` line) are new. The
`TryGetDocumentConstructionProjection` fallback block and the final `BindSelectManyMember` call are byte-for-byte
unchanged from before — do not alter them.

- [ ] **Step 5: Build and run the Outer-leaf test**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~Whole_outer_entity_leaf_that_is_also_reference_included_goes_native"
```

Three possible outcomes:

**(a) PASS.** The bridge alone was sufficient — the alias-collision hypothesis in the design doc did not
manifest (most likely because `AddLookup`'s alias-based dedup, or `RewriteCollectionIncludeForLookup`'s own
handling, tolerated the coincidence harmlessly). Skip Step 6, go to Step 7.

**(b) FAILS with a data mismatch specifically on `OwnerOrderCount`** (the expected tuple's third element) —
e.g. it comes back `0` instead of the real count, or some other wrong-but-plausible number, while `Name`/`Total`
are correct. This is the alias-collision hypothesis manifesting: the join's own `$lookup` (registered under
alias `_lookup_Orders`, `ForceUnwind: true`, one document per row) and the Include's own attempted `$lookup`
registration (same alias, since `Owner.Orders` is what the join's key resolved to) collided in
`MongoQueryExpression.AddLookup` (`src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoQueryExpression.Lookup.cs`,
`AddLookup`, dedups by `l.As`) — the join's single-document lookup won, and the Include's collection shaper is
now reading a single document as if it were an array. Go to Step 6.

**(c) FAILS any other way** (an exception, a different assertion failure, a compile error). STOP — this is new
information the design doc did not anticipate. Use `superpowers:systematic-debugging` before writing any more
code; do not guess at Step 6's fix for a different failure than the one it's written for.

- [ ] **Step 6 (ONLY if Step 5 produced outcome (b)): confirm and fix the alias collision**

First confirm the hypothesis directly — add a temporary diagnostic to the test (or a throwaway `[Fact]` right
next to it) that runs the query and logs the MQL via a `CreateContext(seed, MongoQueryMode.Native, name, out
var spyLogger)` overload (see any existing test in the same file using `out var spyLogger)` and
`spyLogger.GetLogMessagesByEventId(MongoEventId.ExecutedMqlQuery)` for the pattern), and check whether the
pipeline has only ONE `$lookup` targeting the Orders collection (confirms the collision) or two (rules it out —
if it's two, this is a different bug; STOP and use `superpowers:systematic-debugging` instead of applying the
fix below). Delete the diagnostic once you've confirmed either way — it must not survive to the commit.

If confirmed, the fix is in `MongoQueryableMethodTranslatingExpressionVisitor.TranslateSelect`'s wrapped-leaf-join
arm (the `else if (IsSingleEligibleNativeJoinScope(mongoQueryExpression, out var wrappedLeafJoin) &&
NativeJoinScopeProjectionBinder.TryBindProjection(...))` branch, currently starting at line 582): mirror the
EXISTING rename trick in the sibling `TryGetCollectionIncludeOverJoinScope` arm above it (currently lines
498–516: `if (collectionJoin.Lookup!.As == LookupExpression.GetLookupAlias(includeNavigation)) { var renamedAlias
= $"{collectionJoin.Lookup.As}_join"; collectionJoin.Lookup.As = renamedAlias; }`), but — unlike that arm, which
never reads the join's Inner side — this arm's own leaf (`r.Total` in the acceptance test) DOES read the Inner
side via `wrappedLeafJoin`'s registered `Levels[k].InnerPrefix`, which `NativeJoinScopeProjectionBinder
.TryBindProjection` already staged BEFORE this point using the ORIGINAL (pre-rename) alias. Renaming
`wrappedLeafJoin.Lookup.As` here without also updating `mongoQueryExpression.Select.JoinScope!.Levels[...]
.InnerPrefix` to the SAME new value would silently desynchronize the two — the emitted `$lookup`'s `as` would
say one thing, the already-staged `MongoElementRefExpression` reading `r.Total` would say another, and the
Inner leaf would break instead of the Outer one. There is no existing test pinning this specific interaction
(Include on the Outer entity's own collection nav, where that SAME nav is ALSO the join's key, in a projection
that ALSO reads the join's Inner side) — write one as part of this step, in
`tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeProjectionBinderTests.cs`,
mirroring `Binds_a_whole_outer_entity_leaf_that_is_also_include_wrapped`'s shape but asserting on
`mongoQ.Select.JoinScope!.Levels[0].InnerPrefix` equalling whatever the renamed alias ends up being, and on the
Inner leaf's own `MongoElementRefExpression.Path` matching it — before touching production code, per
`superpowers:test-driven-development`.

- [ ] **Step 7: Run the whole `NativeJoinTests.cs` file to confirm no regressions**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"
```

Expected: PASS for every test in the file except
`Whole_inner_entity_leaf_that_is_also_collection_included_goes_native_under_a_left_join` (Task 2's job, not
this task's) — confirm that ONE test's failure looks the same as it did before this task started (still the
`Alice`/`null`-`Total` mismatch, not something new).

- [ ] **Step 8: Run the whole unit test project to confirm no regressions there either**

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build
```

Expected: PASS, same total as before this task (plus one more if Step 6 added a test).

- [ ] **Step 9: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs \
  src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
  tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs
# If Step 6 ran, also:
git add tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/NativeJoinScopeProjectionBinderTests.cs
git commit -m "EF-322: materialize an Include-wrapped whole-entity join-scope leaf correctly"
```

---

