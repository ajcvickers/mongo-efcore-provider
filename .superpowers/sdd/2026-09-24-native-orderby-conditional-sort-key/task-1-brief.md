### Task 1: Widen `TryTranslateConditionalBranch`'s visibility for reuse, with zero behavior change

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs:559-568`

**Interfaces:**
- Consumes: nothing new.
- Produces: `NativeJoinScopeProjectionBinder.TryTranslateConditionalBranch(MongoJoinScope scope, ParameterExpression rootParam, Expression branch, out MongoExpression? result)` — same signature, now `internal static` instead of `private static`, callable from `NativeSlotPopulator` in Task 3 (both classes live in `MongoDB.EntityFrameworkCore.Query.NativeTranslation`, so no new `using` is needed anywhere).

This task is a pure visibility widen — proving it changes no behavior is the whole point, so there is no new test to write; the existing test suite is the test.

- [ ] **Step 1: Confirm the existing SELECT-side conditional-projection tests pass before touching anything (baseline)**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinScopeProjectionBinderTests"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinScopeConditionalProjectionTests"
```

Expected: both **PASS** in full (24/24 for the unit test class per Phase 1's own final count; 6/6 for the functional class — 5 original theories/facts + 1 pin test added in Phase 1's final-review fix pass). Run with `MONGODB_URI`/`ATLAS_URI` unset for the functional run (Docker-backed testcontainer).

- [ ] **Step 2: Widen the method's visibility**

Edit `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs`. Find:

```csharp
    private static bool TryTranslateConditionalBranch(
        MongoJoinScope scope, ParameterExpression rootParam, Expression branch,
        [NotNullWhen(true)] out MongoExpression? result)
```

Replace with:

```csharp
    /// <summary>
    /// Made <c>internal</c> (from <c>private</c>) so <see cref="NativeTranslation.NativeSlotPopulator"/>'s
    /// conditional ORDER BY sort-key arm can reuse it for the identical branch-translation need (EF-322,
    /// Phase 2 Group A) — this method's own logic is unchanged; only its visibility widened.
    /// </summary>
    internal static bool TryTranslateConditionalBranch(
        MongoJoinScope scope, ParameterExpression rootParam, Expression branch,
        [NotNullWhen(true)] out MongoExpression? result)
```

- [ ] **Step 3: Rebuild and re-run the same tests to confirm zero behavior change**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinScopeProjectionBinderTests"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinScopeConditionalProjectionTests"
```

Expected: identical counts to Step 1 — **PASS**, no change.

- [ ] **Step 4: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs
git commit -m "EF-322: widen TryTranslateConditionalBranch visibility for ORDER BY reuse"
```

---

