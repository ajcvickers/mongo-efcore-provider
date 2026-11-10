### Task 5: Correlated `Any`/`All` go native

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs:567-628` (quantifier arm in `TranslateNode`)
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionCorrelatedTests.cs` (extend)

**Interfaces:**
- Consumes (Task 1): `SelfParam`, `ReferencesEnclosingScope(..., out found)`.
- Consumes (Task 2, 4): `MongoOuterFieldExpression`, `MongoQuantifierExpression`.

- [ ] **Step 1: `Correlated_Any_goes_native` (already written in Task 3) should still be failing — confirm**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~Correlated_Any_goes_native"`
Expected: FAIL (`NativeTranslationNotSupportedException`) — this is the RED for this task.

- [ ] **Step 2: Add a correlated `All` test to the same file**

Append to `NativeOwnedCollectionCorrelatedTests.cs`:

```csharp
    [Fact]
    public void Correlated_All_goes_native()
    {
        // "allMatch": every post's Title equals the owner's Title -> All true -> included.
        // "notAll": one post's Title does NOT equal the owner's Title -> All false -> excluded.
        var collection = Seed(nameof(Correlated_All_goes_native),
            ("same", [("same", 1), ("same", 2)]),
            ("mixed", [("mixed", 1), ("other", 2)]));

        var titles = AssertNativeOnlyMatches(
            collection, q => q.Where(b => b.Posts.All(p => p.Title == b.Title)));

        Assert.Equal(new[] { "same" }, titles);
    }

    [Fact]
    public void Correlated_Any_and_All_are_correct_against_an_in_memory_oracle()
    {
        // Differential check, mirroring NativeOwnedCollectionAllTests' own matrix pattern: the SAME
        // expression evaluated in memory over materialized rows must agree with the native (NativeOnly)
        // result — proving this isn't merely "doesn't throw" but answers the correct rows, including the
        // empty-Posts / no-match / all-match / one-mismatch states.
        var collection = Seed(nameof(Correlated_Any_and_All_are_correct_against_an_in_memory_oracle),
            ("same", [("same", 1), ("same", 2)]),
            ("mixed", [("mixed", 1), ("other", 2)]),
            ("nomatch", [("x", 1), ("y", 2)]),
            ("empty", []));

        System.Linq.Expressions.Expression<Func<Blog, bool>>[] predicates =
        [
            b => b.Posts.Any(p => p.Title == b.Title),
            b => b.Posts.All(p => p.Title == b.Title),
            b => b.Posts.Count(p => p.Title == b.Title) > 0
        ];

        foreach (var predicate in predicates)
        {
            List<string> expected;
            using (var db = CreateContext(collection, MongoQueryMode.Native, BlogModel))
            {
                expected = db.Entities.AsNoTracking().ToList()
                    .Where(predicate.Compile()).Select(b => b.Title).OrderBy(t => t).ToList();
            }

            List<string> actual;
            using (var db = CreateContext(collection, MongoQueryMode.NativeOnly, BlogModel))
            {
                actual = db.Entities.AsNoTracking().Where(predicate).ToList()
                    .Select(b => b.Title).OrderBy(t => t).ToList();
            }

            Assert.Equal(expected, actual);
        }
    }
```

- [ ] **Step 3: Run to verify RED**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollectionCorrelatedTests"`
Expected: `Correlated_filtered_Count_predicate_goes_native` PASSES (from Task 3); `Correlated_Any_goes_native`, `Correlated_All_goes_native`, `Correlated_Any_and_All_are_correct_against_an_in_memory_oracle` FAIL.

- [ ] **Step 4: Implement the correlated quantifier branch**

In `MongoExpressionTranslator.cs`, replace the quantifier arm's correlation check and everything through the `MongoElemMatchExpression` return:

```csharp
                // A CORRELATED element predicate — one reaching outside the element into the enclosing entity —
                // must be declined BEFORE the element-scoped translator ever sees it. See the helper's remarks:
                // the element-scoped translator resolves a member by NAME alone, so an enclosing-scoped access
                // whose name also exists on the element would be silently retargeted at the element. This
                // applies to All exactly as it does to Any.
                if (ReferencesEnclosingScope(elementLambda.Body, elementLambda.Parameters[0]))
                    return null;

                // Translate the element predicate with an ELEMENT-SCOPED translator: its field paths come out
                // element-relative, which is what $elemMatch requires. This is the mirror image of
                // NativeSelectManyBinder.TryBuildOwnedInnerFilter, which translates the same way and then
                // PREFIXES the result with the unwind path.
                var elementTranslator = new MongoExpressionTranslator(elementType);
                if (!elementTranslator.TryTranslate(elementLambda.Body, out var translated))
                    return null;

                MongoExpression child = translated;
                var negated = false;

                if (quantifier is MongoQuantifierKind.All)
                {
                    // All(pred) is true exactly when NO element satisfies ¬pred, i.e. a negated $elemMatch
                    // over the EXACT complement. That form is also correct for an empty, missing, or
                    // explicitly-null array: nothing satisfies the $elemMatch, so the enclosing $not matches
                    // and All is true — which is what LINQ's All over an empty sequence returns.
                    //
                    // A predicate with no exact complement declines the whole quantifier (clean fallback to
                    // driver-LINQ) rather than emitting an approximation, which would return wrong rows.
                    if (!MongoExpressionNegator.TryNegate(child, out var complement))
                        return null;

                    child = complement;
                    negated = true;
                }

                // $expr is not usable inside $elemMatch, and RenderNode's catch-all would silently wrap a
                // non-query-dialect child in $expr. Decline here (translate time) so the query falls back to
                // driver-LINQ instead. For All this is belt-and-braces — the negator gates on the same
                // classifier — but it stays because it is the invariant the renderer's contract depends on.
                if (!MongoQueryLanguageRenderer.IsQueryDialectRenderable(child))
                    return null;

                return new MongoElemMatchExpression(arrayPath, child, negated);
```

with:

```csharp
                // A CORRELATED element predicate — one reaching outside the element into the enclosing entity.
                //
                // EF-421: no longer an outright decline. If the free parameter found is IDENTICAL
                // (ReferenceEquals) to THIS translator's own SelfParam, build a two-scope child translator and
                // emit a MongoQuantifierExpression ($anyElementTrue/$allElementsTrue over $map) instead of the
                // ordinary $elemMatch path below — $elemMatch cannot reference the enclosing document at all,
                // so that dialect is unusable here regardless of scope handling. Any OTHER free parameter
                // (correlation reaching past the immediate enclosing scope) still declines exactly as before.
                if (ReferencesEnclosingScope(elementLambda.Body, elementLambda.Parameters[0], out var quantifierFreeParam))
                {
                    if (SelfParam is null || !ReferenceEquals(quantifierFreeParam, SelfParam))
                        return null;

                    var correlatedElementTranslator = new MongoExpressionTranslator(
                        elementType, outerParam: SelfParam, outerEntityType: _entityType, innerPrefix: "");
                    if (!correlatedElementTranslator.TryTranslate(elementLambda.Body, out var correlatedPredicate))
                        return null;

                    // No negation for All here, unlike the uncorrelated path below: $allElementsTrue is
                    // itself a "for all" operator, so the predicate translates DIRECTLY for both Any and All.
                    if (!MongoAggregationExpressionRenderer.CanRender(correlatedPredicate))
                        return null; // no equivalent graceful degrade exists for this node — decline cleanly

                    return new MongoQuantifierExpression(
                        new MongoElementRefExpression(arrayPath, quantifierSource.Type), correlatedPredicate, quantifier);
                }

                // Translate the element predicate with an ELEMENT-SCOPED translator: its field paths come out
                // element-relative, which is what $elemMatch requires. This is the mirror image of
                // NativeSelectManyBinder.TryBuildOwnedInnerFilter, which translates the same way and then
                // PREFIXES the result with the unwind path.
                var elementTranslator = new MongoExpressionTranslator(elementType);
                if (!elementTranslator.TryTranslate(elementLambda.Body, out var translated))
                    return null;

                MongoExpression child = translated;
                var negated = false;

                if (quantifier is MongoQuantifierKind.All)
                {
                    // All(pred) is true exactly when NO element satisfies ¬pred, i.e. a negated $elemMatch
                    // over the EXACT complement. That form is also correct for an empty, missing, or
                    // explicitly-null array: nothing satisfies the $elemMatch, so the enclosing $not matches
                    // and All is true — which is what LINQ's All over an empty sequence returns.
                    //
                    // A predicate with no exact complement declines the whole quantifier (clean fallback to
                    // driver-LINQ) rather than emitting an approximation, which would return wrong rows.
                    if (!MongoExpressionNegator.TryNegate(child, out var complement))
                        return null;

                    child = complement;
                    negated = true;
                }

                // $expr is not usable inside $elemMatch, and RenderNode's catch-all would silently wrap a
                // non-query-dialect child in $expr. Decline here (translate time) so the query falls back to
                // driver-LINQ instead. For All this is belt-and-braces — the negator gates on the same
                // classifier — but it stays because it is the invariant the renderer's contract depends on.
                if (!MongoQueryLanguageRenderer.IsQueryDialectRenderable(child))
                    return null;

                return new MongoElemMatchExpression(arrayPath, child, negated);
```

- [ ] **Step 5: Build**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`
Expected: builds clean.

- [ ] **Step 6: Run to verify GREEN**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollectionCorrelatedTests"`
Expected: all 5 tests PASS.

- [ ] **Step 7: Run the full existing native-translation FunctionalTests suite to confirm no regression**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollection"`
Expected: every test passes, including `NativeOwnedCollectionAllTests.All_with_a_correlated_element_predicate_declines_and_falls_back_to_correct_rows` — **check this one specifically**: it currently asserts a DECLINE (`AssertDeclinesCleanly`) for `b.Posts.All(p => b.Title == "match")`. After this task, that shape now GOES NATIVE, so this specific test will now FAIL (its `NativeOnly` leg no longer throws). This is expected — proceed to Task 8, which retires/rewrites this specific test; do not attempt to "fix" it in this task.

- [ ] **Step 8: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionCorrelatedTests.cs
git commit -m "EF-421: correlated Any/All quantifiers go native via \$anyElementTrue/\$allElementsTrue"
```

---

