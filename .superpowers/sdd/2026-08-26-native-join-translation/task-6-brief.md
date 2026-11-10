### Task 6 (REVISED 2026-08-26 — see below): Document dormant scaffolding and verify graceful fallback

**This task's original scope ("resolve join-scope member access in Where/OrderBy/Select") is abandoned.** Its implementer discovered, via real investigation (not guesswork), that EF Core's `NavigationExpandingExpressionVisitor` normalizes every `Join`/`LeftJoin` call — genuine and Include-generated alike — into the identical `TransparentIdentifier(Outer, Inner)` shape before `TranslateJoinCore` ever runs. The "flattened vs. whole-side-capture" distinction Tasks 2/3/5 were built on does not exist at that point; both shapes are indistinguishable there. This is an EF Core limitation. The human maintainer ruled: do not build a provider-side disambiguation mechanism (no candidate/confirm handshake) — genuine two-sided joins stay on the driver-LINQ fallback indefinitely. See `docs/superpowers/specs/2026-08-26-native-join-translation-design.md`'s "Blocker found during implementation" section and [EF-439](https://jira.mongodb.org/browse/EF-439) (the follow-up ticket) for full detail.

Tasks 1–5 remain committed as-is (Task 1 is a live, valuable refactor; Tasks 2/3/5 are now provably unreachable dead code, kept intentionally as dormant scaffolding per an explicit decision, not reverted). **This task's real, revised job:**

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs`, `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs`, the new arm in `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs`, and the `JoinScope` property on `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs` — add a doc comment to each pointing at the spec's "Blocker found during implementation" section and EF-439, explaining the code is currently unreachable and why, so a future reader doesn't mistake it for live, exercised logic.
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs` — revise the existing smoke test(s): a genuine two-sided `Join`/`LeftJoin` must (a) succeed and return CORRECT results under the default `MongoQueryMode.Native` (via the driver-LINQ fallback — this already worked before this plan started and must keep working), and (b) throw `NativeTranslationNotSupportedException` cleanly under `MongoQueryMode.NativeOnly` (the expected, permanent decline — not a bug to fix). Do NOT assert success under `NativeOnly` — that was this task's original (now-abandoned) goal.

**Interfaces:** none new — this task only adds documentation comments and revises test expectations to match the ruled-on permanent behavior.

- [ ] **Step 1: Add the dormant-code doc comments**

To each of the four locations above, add an XML doc `<remarks>` block (or extend the existing one) stating: this member/type is currently unreachable — `MongoJoinBinder.TryBindJoin` declines every real join before this code can run, because EF Core's `NavigationExpandingExpressionVisitor` makes a genuine join and an Include-generated join indistinguishable at bind time. Point to `docs/superpowers/specs/2026-08-26-native-join-translation-design.md` and EF-439. Keep it short — one paragraph, not a re-derivation of the whole investigation.

- [ ] **Step 2: Revise the functional smoke test(s) in `NativeJoinTests.cs`**

Replace `Flattened_scalar_join_goes_native_with_correct_results_and_mql` (and drop the second, whole-side-capture smoke test from Task 4/6's original plan — it tested a shape that can never go native) with two tests:

```csharp
[Fact]
public void Genuine_two_sided_join_returns_correct_results_via_fallback()
{
    var seed = SeedOwnersAndOrders();
    using var db = CreateContextWithLogging(seed, MongoQueryMode.Native,
        nameof(Genuine_two_sided_join_returns_correct_results_via_fallback), out _);

    var result = db.Owners
        .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
        .AsEnumerable()
        .OrderBy(x => x.Name).ThenBy(x => x.Total)
        .ToList();

    var expected = seed.Owners
        .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
        .OrderBy(x => x.Name).ThenBy(x => x.Total)
        .ToList();

    Assert.Equal(expected, result);
}

[Fact]
public void Genuine_two_sided_join_declines_cleanly_under_NativeOnly()
{
    // Permanent, correct behavior per EF-439 — a genuine two-sided join can never be
    // distinguished from an Include-generated one at bind time (EF Core limitation), so
    // it must always decline to the driver-LINQ fallback, never attempt native translation.
    var seed = SeedOwnersAndOrders();
    using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
        nameof(Genuine_two_sided_join_declines_cleanly_under_NativeOnly));

    Assert.Throws<NativeTranslationNotSupportedException>(() =>
        db.Owners
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .AsEnumerable()
            .ToList());
}
```

- [ ] **Step 3: Run both tests, confirm they pass**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"`
Expected: PASS, both tests.

- [ ] **Step 4: Run the full functional Query regression suite**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query"`
Expected: PASS, no regressions (in particular `NativeSelectManyTests`, all reference-Include tests).

- [ ] **Step 5: Build all three EF configs and commit**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git add src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs \
        src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs
git commit -m "EF-392: document dormant join-scaffolding as blocked on EF-439; verify genuine two-sided joins fall back correctly"
```

---

### ORIGINAL Task 6 text (abandoned — kept for historical record only, DO NOT EXECUTE)

<details>
<summary>Original plan text, superseded by the revision above</summary>

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeProjectionBinder.cs` (add a join-scope-aware entry point alongside its existing bare/wrapped projection binding)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs` (add a join-scope-aware predicate/sort-key translation path, called from `PopulateNativeSlots` for `Where`/`OrderBy`/`ThenBy` when `mongoQ.Select.JoinScope != null`)
- Test: extend `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`

**Interfaces:**
- Consumes: `MongoJoinScope` (Task 2), `MongoTransparentScopeResolver` (Task 1), `MongoExpressionTranslator`'s existing two-scope constructor (`MongoExpressionTranslator(IEntityType innerEntityType, ParameterExpression outerParam, IEntityType outerEntityType, string innerPrefix)` — `NativeTranslation/MongoExpressionTranslator.cs:72-79`, unchanged).
- Produces: `Where`/`OrderBy`/`Select` lambdas whose single parameter's member access resolves against `MongoSelectDefinition.JoinScope`'s `OuterMemberName`/`InnerMemberName` translate correctly; an already-flattened (no-`JoinScope`) join's `Select` continues through the existing single-scope `NativeProjectionBinder` path unchanged, since every member is already a plain scalar leaf resolvable with no scope tracking at all.

- [ ] **Step 1: Confirm the exact current failure from Task 5 Step 3 and design the fix around it**

Run the `NativeJoinTests` smoke test again and read the exact `NativeTranslationNotSupportedException` (or wrong-result) it now produces. For the specific smoke test written in Task 4 (`(o, r) => new { o.Name, r.Total }` — an ALREADY-FLATTENED result selector, no `JoinScope` set), the failure is most likely in `NativeSlotPopulator`/`NativeProjectionBinder`'s trailing-`Select` handling: after `TranslateJoinCore` substitutes `resultSelector.Body` directly (Task 4's unchanged `newResultSelector` line), the shaper carries a `NewExpression` whose arguments are `o.Name`/`r.Total` member expressions *already resolved against the outer/inner shapers* — but there is no SEPARATE trailing `Select` call for `NativeProjectionBinder` to bind against (unlike SelectMany's deferred-Select pattern), because a fluent `.Join(...)` with an inline result selector produces the projection as part of the join call itself, with no additional LINQ operator. **This means the flattened-join projection needs its own binder entry point, invoked directly from `TranslateJoinCore` (in `MongoJoinBinder.TryBindJoin`, Task 3) rather than from a later `TranslateSelect`.**

Revise Task 3's `MongoJoinBinder.TryBindJoin` (this task modifies it further) so that for each already-flattened member (`argExpr` is neither `outerParam` nor `innerParam` verbatim), it immediately translates the leaf via the two-scope `MongoExpressionTranslator(innerEntityType, outerParam, outerEntityType, joinInfo.Alias)` constructor and adds it to `outerQueryExpression.Select.Projection` directly — mirroring exactly how `NativeSelectManyBinder.TryBind`'s inner-Select form builds `MongoProjection`s inline (`NativeSelectManyBinder.cs:98-113`), rather than deferring to a later call.

- [ ] **Step 2: Update `MongoJoinBinder.TryBindJoin` to translate and register flattened leaves inline**

```csharp
// In MongoJoinBinder.cs, replace the member-classification loop from Task 3 with:

        var outerTranslator = new MongoExpressionTranslator(outerQueryExpression.CollectionExpression.EntityType);
        var twoScopeTranslator = new MongoExpressionTranslator(
            navigation.TargetEntityType, outerParam, outerQueryExpression.CollectionExpression.EntityType, joinInfo.Alias);

        string? outerMemberName = null;
        string? innerMemberName = null;
        var flattenedProjections = new System.Collections.Generic.List<MongoProjection>();

        foreach (var (alias, argExpr) in members)
        {
            if (ReferenceEquals(argExpr, outerParam))
            {
                if (outerMemberName != null) return false;
                outerMemberName = alias;
                continue;
            }

            if (ReferenceEquals(argExpr, innerParam))
            {
                if (innerMemberName != null) return false;
                innerMemberName = alias;
                continue;
            }

            // Already-flattened scalar leaf (e.g. o.Name or r.Total): translate now, against whichever
            // side it's rooted on, using the SAME two-scope translator SelectMany's correlated filters use
            // (parameter identity, never member name) so a member name shared between outer and inner
            // entity types can never mis-scope.
            if (!twoScopeTranslator.TryTranslate(argExpr, out var translated)
                && !outerTranslator.TryTranslateField(argExpr, out _)) // outer-rooted leaves also need a value, not just a field-existence check
            {
                return false;
            }

            if (!twoScopeTranslator.TryTranslateValue(argExpr, out var value))
            {
                return false;
            }

            flattenedProjections.Add(new MongoProjection(alias, value));
        }
```

**Note for the implementer:** the exact translator call (`TryTranslate` vs `TryTranslateValue` vs `TryTranslateField`) needs to be re-checked against `MongoExpressionTranslator`'s real public surface in `NativeTranslation/MongoExpressionTranslator.cs` before finalizing this step — the sketch above calls `TryTranslate` speculatively as a probe and then re-translates with `TryTranslateValue`, which is redundant and should be collapsed to a single call once confirmed. Read `MongoExpressionTranslator.cs` in full (only lines 1-150 were read while writing this plan) to settle on the one correct method for "translate a projection leaf value, two-scope" before writing this code for real, and simplify accordingly — do not ship the redundant double-call above as-is.

Then, at the end of the method (replacing Task 3's final block):

```csharp
        var lookup = new LookupExpression(navigation, forceUnwind: true)
        {
            PreserveNullAndEmptyArrays = joinInfo.IsLeftOuter
        };
        outerQueryExpression.AddLookup(lookup);

        foreach (var projection in flattenedProjections)
        {
            outerQueryExpression.Select.AddProjection(projection);
        }

        if (outerMemberName != null || innerMemberName != null)
        {
            outerQueryExpression.Select.JoinScope = new MongoJoinScope(
                outerMemberName, innerMemberName,
                outerQueryExpression.CollectionExpression.EntityType, navigation.TargetEntityType,
                joinInfo.Alias, joinInfo.IsLeftOuter);
        }

        return true;
```

- [ ] **Step 3: Run the smoke test again**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"`
Expected: PASS for the flattened-scalar smoke test. If it still fails, the failure is now isolated to how `NativeSlotPopulator`'s existing `else` catch-all (`Query/AGENTS.md`'s "native catch-all whitelist must stay in sync" pitfall) treats a `Select` whose `Route` was already set by this direct registration — check whether `TranslateSelect`'s own dispatch needs a guard added (mirroring `IsTransparentIdentifierSelector`/`IsSingleLevelCollectionIncludeSelector`) so it does not re-decide a projection this binder already populated. This is the single highest-uncertainty step in this plan — budget real investigation time here, not a quick guess.

- [ ] **Step 4: Add a second functional test for the whole-side-capture (`JoinScope`-populated) case, with a subsequent `Where`**

```csharp
    [Fact]
    public void Whole_side_capture_with_trailing_Where_goes_native_with_correct_results_and_mql()
    {
        var seed = SeedOwnersAndOrders();
        using var db = CreateContextWithLogging(seed, MongoQueryMode.NativeOnly,
            nameof(Whole_side_capture_with_trailing_Where_goes_native_with_correct_results_and_mql), out var spyLogger);

        var result = db.Owners
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Where(x => x.r.Total > 100)
            .Select(x => new { x.o.Name, x.r.Total })
            .AsEnumerable()
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Where(x => x.r.Total > 100)
            .Select(x => new { x.o.Name, x.r.Total })
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        Assert.Equal(expected, result);
        var message = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        Assert.Contains("\"$lookup\"", message);
        Assert.Contains("\"$unwind\"", message);
    }
```

This exercises `NativeSlotPopulator`'s `Where` translation reading `x.r.Total` through `MongoJoinScope` — implement the resolution path in `NativeSlotPopulator.PopulateNativeSlots`'s `Where` arm: when `mongoQ.Select.JoinScope is { } scope`, re-root the predicate the same way `NativeSelectManyBinder.TryBindTransparentIdentifierProjection` re-roots (`MongoTransparentScopeResolver`, hop names `[scope.OuterMemberName!, scope.InnerMemberName!]` fed as a **single-hop-each** lookup — since a Join's scope, unlike SelectMany's nested nesting, is always exactly one flat member access `x.o`/`x.r`, not a chain — so this reuses the resolver's `sourceCount: 1` case directly: `x.<OuterMemberName>` matches hop name `OuterMemberName` at scope 0 is WRONG for this shape (the resolver's hop-name convention is positional — `hopNames[0]` is the "step toward root" name and `hopNames[1]` is the "step to this scope" name, which doesn't fit a Join's two SIBLING member names naturally). **Do not force-fit `MongoTransparentScopeResolver`'s exact algorithm here** — for Join's flat (non-nested) two-member shape, write a simpler direct check instead: a member access `MemberExpression { Expression: ParameterExpression p, Member.Name: var name }` where `p` is the current lambda's own parameter resolves to the outer scope when `name == scope.OuterMemberName`, the inner scope when `name == scope.InnerMemberName`, translating the REST of the access chain (everything after peeling that one hop) against the matching entity type via the existing two-scope `MongoExpressionTranslator` constructor exactly as Task 6 Step 2 already does for the flattened case. Implement this as a small dedicated helper in `NativeSlotPopulator.cs` (or `MongoJoinBinder.cs`, since it's join-specific, not a generalization of the SelectMany walker) — e.g. `MongoJoinBinder.TryTranslateScopedPredicate(MongoJoinScope scope, ParameterExpression param, Expression body, out MongoExpression? result)` — rather than routing through `MongoTransparentScopeResolver`, since that resolver's nested-chain algorithm does not match this flat shape. Revise Task 1's docstring/remarks if this narrows its intended reuse — record that deviation in the PR description when this task lands.

- [ ] **Step 5: Run all `NativeJoinTests`, then the full functional Query suite for regressions**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"`
Expected: PASS, both tests.

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query"`
Expected: PASS, no regressions (in particular `NativeSelectManyTests`, all reference-Include tests, `QueryModeGateTests`/`QueryModeGateIncludeTests`).

- [ ] **Step 6: Build all three EF configs and commit**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs
git commit -m "EF-392: resolve join-scope member access in Where/Select; close out flattened and whole-side-capture Join shapes"
```

</details>

---

