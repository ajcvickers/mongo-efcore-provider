### Task 5: Lower a bound join scope into `$lookup`/`$unwind` stages

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs:342-395` (`AppendLookupStages`)

**Interfaces:**
- Consumes: `MongoSelectDefinition.JoinScope` (Task 2), existing `MongoQueryExpression.Lookups`/`IsJoinQuery`/`InnerCollections`.
- Produces: `AppendLookupStages` now recognizes a lowerable native-join `$lookup` (one whose `LookupExpression` is the one `MongoJoinBinder` registered) and emits `MongoLookupStage` + `MongoUnwindStage(preserveNullAndEmptyArrays: <join's own IsLeftOuter>)`, instead of only handling the `IsStreamableReference` (Include)/`IsNativeCollectionLookup`/`ForceUnwind`-navigation-collection branches already there.

- [ ] **Step 1: Re-run the Task 4 smoke test to confirm the exact current failure**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"`
Expected: FAIL with `NativeTranslationNotSupportedException: Native pipeline does not support this join shape (only single-level reference includes).` — this is the exact guard to fix.

- [ ] **Step 2: Fix `AppendLookupStages`'s join-coverage guard and add the join-scope branch**

The current code (`MongoSelectLowerer.cs:342-395`):

```csharp
    private static void AppendLookupStages(MongoQueryExpression query, List<MongoPipelineStage> stages)
    {
        var lookups = query.Lookups;

        // Join-coverage guard: if this is a join query and there are fewer lookups than inner
        // collections, emitting a partial pipeline would silently drop a join and return wrong results.
        if (query.IsJoinQuery && lookups.Count < query.InnerCollections.Count)
        {
            throw new NativeTranslationNotSupportedException(
                "Native pipeline does not support this join shape (only single-level reference includes).");
        }

        foreach (var lookup in lookups)
        {
            if (lookup.IsStreamableReference)
            {
                stages.Add(new MongoLookupStage(lookup));
                stages.Add(new MongoUnwindStage(lookup, lookup.PreserveNullAndEmptyArrays));
            }
            else if (lookup.IsNativeCollectionLookup)
            {
                stages.Add(new MongoLookupStage(lookup));
            }
            else if (lookup.Navigation is { IsCollection: true } && lookup.ForceUnwind)
            {
                stages.Add(new MongoLookupStage(lookup));
                stages.Add(new MongoUnwindStage(lookup, preserveNullAndEmptyArrays: false));
            }
            else
            {
                throw new NativeTranslationNotSupportedException(/* ... */);
            }
        }
    }
```

This chunk's native join is registered as a `forceUnwind: true` lookup over a REFERENCE (non-collection, in the LINQ-`Join`-target sense — the navigation resolved from the outer key selector) navigation. Add a new arm that recognizes it — a `ForceUnwind` lookup whose navigation is a single (non-collection) reference, distinct from the existing `lookup.Navigation is { IsCollection: true } && lookup.ForceUnwind` arm (which is the reference-collection-`SelectMany`-flatten case, always inner-unwind). The join case must honor `MongoJoinScope.IsLeftOuter`/`joinInfo`'s left-outer-ness for its `preserveNullAndEmptyArrays`, not a fixed `false`:

```csharp
            else if (lookup.Navigation is { IsCollection: false } && lookup.ForceUnwind
                     && query.Select.JoinScope is { } joinScope && joinScope.InnerPrefix == lookup.As)
            {
                // A native two-sided Join/LeftJoin (MongoJoinBinder, EF-392): unwind semantics follow the
                // LINQ operator itself (Join => inner, LeftJoin/GroupJoin => left-outer), carried on
                // MongoJoinScope.IsLeftOuter — NOT a fixed default, unlike the reference-SelectMany-flatten
                // arm above (which is always inner-join by construction).
                stages.Add(new MongoLookupStage(lookup));
                stages.Add(new MongoUnwindStage(lookup, preserveNullAndEmptyArrays: joinScope.IsLeftOuter));
            }
```

Insert this new arm **before** the existing `lookup.Navigation is { IsCollection: true } && lookup.ForceUnwind` arm in the `if`/`else if` chain (order matters only in that both are `ForceUnwind`-gated but discriminate on `IsCollection`, so either order is actually safe here — but placing the join arm first keeps the two `ForceUnwind` arms visually adjacent for a future reader).

Note this arm only fires when `JoinScope` is set — Task 3's `MongoJoinBinder.TryBindJoin` only sets `JoinScope` when the result selector captures a whole side. For the **already-fully-flattened** case (`(o, i) => new { o.Name, i.Total }`, `JoinScope` stays `null`), this new arm's guard is false, and the lookup instead needs to fall through to... check: today's `else` branch throws unconditionally for anything not matching the first three arms. A flattened-only join's lookup would hit this same new condition's `IsCollection: false && ForceUnwind` half but fail the `JoinScope is { }` half (since no scope was set) — it would then fall through to the existing collection-flatten arm's `else if` (also false, since `IsCollection: false`) and hit the final `else` throw. **Fix:** broaden the new arm's guard to also match on `query.IsJoinQuery` generally, not just on `JoinScope` being non-null, since the flattened-only case is equally eligible for the same lowering — only the *shape of what runs afterward* (whether later operators can resolve fields against a scope) differs, not whether the `$lookup`/`$unwind` themselves should be emitted:

```csharp
            else if (lookup.Navigation is { IsCollection: false } && lookup.ForceUnwind && query.IsJoinQuery)
            {
                var isLeftOuter = query.Select.JoinScope is { } joinScope && joinScope.InnerPrefix == lookup.As
                    ? joinScope.IsLeftOuter
                    : query.Joins.First(j => j.Alias == lookup.As).IsLeftOuter;
                stages.Add(new MongoLookupStage(lookup));
                stages.Add(new MongoUnwindStage(lookup, preserveNullAndEmptyArrays: isLeftOuter));
            }
```

This reads `IsLeftOuter` from `MongoJoinScope` when a scope was recorded (whole-side capture), or directly from the matching `JoinInfo` on `query.Joins` otherwise (flattened-only case) — both are populated by the time lowering runs, and `query.Joins` (Task 3's consumed `JoinInfo` list) is already `IReadOnlyList<JoinInfo>` on `MongoQueryExpression` (`Expressions/MongoQueryExpression.Lookup.cs:169`), so no new state is needed for this fallback read.

- [ ] **Step 3: Run the Task 4 smoke test again**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"`
Expected: Still likely FAIL, but check the NEW failure mode — the `$project` following the `$unwind` must still resolve `o.Name`/`r.Total` to real field refs. If `NativeProjectionBinder`/`NativeSlotPopulator` don't yet know how to read a member off the inner-scope-prefixed lookup result for an already-flattened join projection, this is expected and is exactly the seam Task 6 must close (see Task 6 Step 1's investigation). Record the exact exception message here before proceeding, so Task 6 starts from a known, reproduced failure rather than a guess.

- [ ] **Step 4: Build all three EF configs and commit**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs
git commit -m "EF-392: lower a native Join/LeftJoin's registered lookup into \$lookup/\$unwind stages"
```

---

