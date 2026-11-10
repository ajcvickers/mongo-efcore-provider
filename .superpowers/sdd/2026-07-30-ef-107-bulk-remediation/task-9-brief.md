### Task 9: Match setter selectors by `MemberInfo`, match `SetProperty` by declaring type, reject duplicate setters

Three small correctness items in `BuildSetter` and the EF9 setter-chain walk:

1. `entityType.FindProperty(member.Member.Name)` resolves by string. EF's `FindProperty(MemberInfo)` overload exists precisely to match the declaring member, which matters for shadowed members and explicit interface implementations.
2. The EF9 chain walk matches `Method.Name: "SetProperty"` with no declaring-type check, so a user-defined extension method of that name would be silently consumed.
3. Two setters on the same property overwrite in the `$set` document — `setDoc[key] = …` — so the first assignment is silently dropped. `$set` cannot hold duplicate keys, so overwriting is the only mechanical option; the right behaviour is a clean error.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: `RejectSetterTarget` from Task 7.
- Produces: no signature changes.

- [ ] **Step 1: Write the failing test**

In `ExecuteUpdateTests.cs`:

```csharp
    [Fact]
    public void ExecuteUpdate_with_the_same_property_set_twice_throws()
    {
        using var db = CreateSeededContext();

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(
                s => s.SetProperty(o => o.Status, "first").SetProperty(o => o.Status, "second")));

        Assert.Contains("more than once", ex.Message);
    }
```

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdate_with_the_same_property_set_twice_throws"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteUpdate_with_the_same_property_set_twice_throws"
```

Expected: FAIL on both — today the update succeeds and silently applies `"second"`.

- [ ] **Step 3: Resolve by `MemberInfo` and tighten the `SetProperty` match**

In `BuildSetter`, replace the member branch:

```csharp
        IProperty? property = null;
        if (selectorBody is MemberExpression { Expression: var memberSource } member
            && memberSource != null
            && propertySelector.Parameters.Count == 1
            && IsParameterReference(memberSource, propertySelector.Parameters[0]))
        {
            // Match on the declaring member, not the name: FindProperty(string) would also match a
            // same-named property that is not the one the selector actually referenced.
            property = entityType.FindProperty(member.Member) ?? entityType.FindProperty(member.Member.Name);
        }
```

The name-based call is kept as a fallback because shadow properties surfaced through a CLR member can resolve by name only; the `MemberInfo` overload is tried first so it wins whenever both would match.

In the EF9 `TranslateExecuteUpdate`, tighten the loop condition:

```csharp
        while (body is MethodCallExpression
               {
                   Method: { Name: "SetProperty", DeclaringType.IsGenericType: true } setPropertyMethod
               } call
               && setPropertyMethod.DeclaringType!.GetGenericTypeDefinition() == typeof(SetPropertyCalls<>))
```

Add `using Microsoft.EntityFrameworkCore.Query;` if `SetPropertyCalls<>` is not already resolvable. If the type lives elsewhere on the pinned EF9 package, find it with `grep -rn "SetPropertyCalls" ~/.nuget/packages/microsoft.entityframeworkcore/9.*/lib/**/*.xml` and use the namespace you find.

- [ ] **Step 4: Reject duplicate setter targets**

In both the EF9 and EF10 `TranslateExecuteUpdate` bodies, immediately before constructing the `MongoNonQueryExpression`, add:

```csharp
        RejectDuplicateSetters(mongoQueryExpression, parsed);
```

and add the helper next to `RejectSetterTarget`:

```csharp
    /// <summary>
    /// A MongoDB <c>$set</c> document cannot carry the same key twice, so two setters on one property would
    /// silently drop the first. Reject rather than pick one.
    /// </summary>
    private void RejectDuplicateSetters(
        MongoQueryExpression mongoQueryExpression, IReadOnlyList<MongoNonQueryExpression.Setter> setters)
    {
        var seen = new HashSet<IProperty>();
        foreach (var setter in setters)
        {
            if (!seen.Add(setter.Property))
            {
                AddTranslationErrorDetails(
                    $"The property '{setter.Property.DeclaringType.DisplayName()}.{setter.Property.Name}' is set "
                    + "more than once in a single bulk update. Each property may be set at most once.");
                throw new InvalidOperationException(
                    CoreStrings.NonQueryTranslationFailedWithDetails(
                        mongoQueryExpression.CapturedExpression?.Print(), TranslationErrorDetails));
            }
        }
    }
```

- [ ] **Step 5: Run the tests on both EF versions**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --filter "FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. If a conformance case legitimately sets the same property twice, move it to `AssertTranslationFailed` with a `// Fails:` comment as described in Task 7 Step 5.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs
git commit -m "fix: resolve setter targets by MemberInfo, scope SetProperty matching, reject duplicate setters"
```

---

## Phase 3 — Fail-closed correctness

