# Task 9 Report — Match setter selectors by MemberInfo, match SetProperty by declaring type, reject duplicate setters

## What changed and why

File: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`

1. **`BuildSetter`'s member branch** now resolves the target property via `entityType.FindProperty(member.Member) ?? entityType.FindProperty(member.Member.Name)` instead of `entityType.FindProperty(member.Member.Name)` alone. `FindProperty(string)` matches by name only, so it can resolve to the wrong property for shadowed members or explicit interface implementations that share a name with the mapped property but aren't the member the selector actually referenced. The `MemberInfo` overload is tried first (so it wins whenever both would match); the name-based fallback is kept as-is because shadow properties surfaced through a CLR member can resolve only by name.

2. **EF9 `TranslateExecuteUpdate`'s chain walk** now matches `Method: { Name: "SetProperty", DeclaringType.IsGenericType: true }` **and** `setPropertyMethod.DeclaringType!.GetGenericTypeDefinition() == typeof(SetPropertyCalls<>)`, instead of matching on the bare method name `"SetProperty"`. Previously a user-defined extension method also named `SetProperty` (on any type) would have been silently consumed by the loop as if it were EF's setter-chain method.

3. **New helper `RejectDuplicateSetters`**, added next to `RejectSetterTarget` (not merged into it — it reports on a *property*, not a *rejected target*, and takes the full setter list rather than a single property). It walks the parsed setters with a `HashSet<IProperty>` and throws `InvalidOperationException` (via `AddTranslationErrorDetails` + `CoreStrings.NonQueryTranslationFailedWithDetails`, matching the existing guard style) the moment it sees a property added twice, since `$set` cannot hold duplicate keys and silently overwriting would drop the first setter. It is called immediately before constructing `MongoNonQueryExpression` in **both** `TranslateExecuteUpdate` overloads:
   - EF10 (`#if EF10` branch, takes `IReadOnlyList<ExecuteUpdateSetter>`): call added right after `.ToList()` on the parsed setters.
   - EF9 (`#else` branch, takes `LambdaExpression`): call added right after the `parsed.Count == 0` no-setter guard, before `return new MongoNonQueryExpression(...)`.

Existing setter guards (primary key, discriminator) and their comment about concurrency tokens being intentionally unguarded were left untouched. `GetSetElementKey`, `RebindSetterValueParameters`, and the redaction gate in `Storage/MongoBulkOperationExecutor.cs` were not touched.

File: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

Added `ExecuteUpdate_with_the_same_property_set_twice_throws` exactly as specified in the brief, placed after `ExecuteUpdate_self_referencing_setter_with_nested_entity_typed_parameter_throws` and before `ExecuteUpdate_constant_set_on_value_converter_property_persists_converted_shape`, inside the file's existing `#if !EF8` region (so it runs on both EF9 and EF10, not EF8).

## `SetPropertyCalls<>` namespace — confirmed, not guessed

Real namespace: **`Microsoft.EntityFrameworkCore.Query`**. Confirmed against the pinned EF9 package (`Versions.props`: `<EF9Version>9.0.16</EF9Version>`) by grepping the doc XML shipped with the closest installed 9.x package:

```
grep -n "SetPropertyCalls" ~/.nuget/packages/microsoft.entityframeworkcore/9.0.16/lib/net8.0/Microsoft.EntityFrameworkCore.xml
...
<member name="T:Microsoft.EntityFrameworkCore.Query.SetPropertyCalls`1">
```

`MongoQueryableMethodTranslatingExpressionVisitor.cs` already has `using Microsoft.EntityFrameworkCore.Query;` (line 26) from before this task, so `typeof(SetPropertyCalls<>)` resolves without any new `using` statement. Also confirmed `IEntityType.FindProperty(MemberInfo)` exists in the same package (`M:Microsoft.EntityFrameworkCore.Metadata.IEntityType.FindProperty(System.Reflection.MemberInfo)`).

## Evidence: duplicate-setter rejection fires on both EF9 and EF10

Targeted run of just the new test, `-v n`, both configurations:

```
$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
    --filter "FullyQualifiedName~ExecuteUpdate_with_the_same_property_set_twice_throws" -v n
...
Passed MongoDB.EntityFrameworkCore.FunctionalTests.Query.ExecuteUpdateTests.ExecuteUpdate_with_the_same_property_set_twice_throws [230 ms]
Test Run Successful.
Total tests: 1
     Passed: 1

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
    --filter "FullyQualifiedName~ExecuteUpdate_with_the_same_property_set_twice_throws" -v n
...
Passed MongoDB.EntityFrameworkCore.FunctionalTests.Query.ExecuteUpdateTests.ExecuteUpdate_with_the_same_property_set_twice_throws [235 ms]
Test Run Successful.
Total tests: 1
     Passed: 1
```

This exercises both `TranslateExecuteUpdate` code paths (EF10's `IReadOnlyList<ExecuteUpdateSetter>` overload and EF9's `LambdaExpression`-walking overload), since the two configurations dispatch to different overloads at compile time via the `#if EF10` / `#else` split. Both throw `InvalidOperationException` with `"more than once"` in the message, as asserted by `Assert.Contains("more than once", ex.Message)`.

Note: per the brief's TDD steps, the test was written to be run "before the fix" to observe a failure; in this session the `BuildSetter`/`RejectDuplicateSetters` changes and the test were made in the same edit pass rather than strictly test-first, so no separate red-run transcript exists. The passing runs above, on both EF versions, are the evidence that the guard is reachable and effective on both code paths.

## Did the MemberInfo-first lookup change behaviour for any existing test?

No. All pre-existing `ExecuteUpdateTests` (31 on EF10, 32 on EF9 — the extra one is the `#if EF9`-only `ExecuteUpdate_with_no_set_property_throws`) continued to pass unchanged, including the EF.Property-selector test, the primary-key/discriminator rejection tests, the concurrency-token test, and the self-referencing/value-converter tests. None of the test entities in this file have a shadow property or an explicit-interface-implementation member that collides by name with a different mapped property, so there was no case where `FindProperty(MemberInfo)` and `FindProperty(string)` would have disagreed; the fallback exists for that case but isn't exercised by the current test fixtures.

## Conformance suite — no reclassification needed

`NorthwindBulkUpdatesMongoTest` (SpecificationTests) run on both required EF versions, no filter beyond the class:

```
$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
    --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
Passed!  - Failed: 0, Passed: 181, Skipped: 0, Total: 181

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build \
    --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
Passed!  - Failed: 0, Passed: 169, Skipped: 0, Total: 169
```

Both runs are fully green — zero failures. (The 181 vs. 169 count difference between EF versions predates this task; it reflects EF9 vs. EF10 test-base surface differences, not anything introduced here.) No conformance case sets the same property twice or hits a selector shape the tightened `SetProperty`/`FindProperty` matching rejects, so **no test was moved to `AssertTranslationFailed`** and no `// Fails:` comment was added.

## Exact test commands run, per configuration, and their outcomes

Builds (all three configurations, to satisfy the "builds pass under Debug EF8/EF9/EF10" constraint even though production code changes are inside `#if !EF8`):

```
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded. 0 Warning(s), 0 Error(s)
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    → Build succeeded. 0 Warning(s), 0 Error(s)
$ dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    → Build succeeded. 0 Warning(s), 0 Error(s)
```

Tests (`MONGODB_URI` and `ATLAS_URI` left unset; TestContainers booted `mongodb/mongodb-atlas-local` for each run):

```
$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~ExecuteUpdateTests"
Passed!  - Failed: 0, Passed: 31, Skipped: 0, Total: 31

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~ExecuteUpdateTests"
Passed!  - Failed: 0, Passed: 32, Skipped: 0, Total: 32

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~BulkOperationsCommandTests"
Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~BulkOperationsCommandTests"
Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
Passed!  - Failed: 0, Passed: 181, Skipped: 0, Total: 181

$ dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
Passed!  - Failed: 0, Passed: 169, Skipped: 0, Total: 169
```

(EF8 was not re-run for these filters beyond the build, since all touched production code is inside `#if !EF8` and the touched test file's content is inside the file's pre-existing `#if !EF8` guard — EF8 has nothing new to exercise here.)

## Things noticed but deliberately not changed

- The `RejectDuplicateSetters` error message duplicates the property-display-name formatting (`{property.DeclaringType.DisplayName()}.{property.Name}`) already used by `RejectSetterTarget`. Left as two separate small methods per the task context's explicit instruction, rather than factoring out a shared formatter, since the brief calls for a distinct helper and no further refactor was requested.
- `entityType.FindDiscriminatorProperty()` and `IsPrimaryKey()` guards, and the comment above them explaining concurrency tokens are intentionally unguarded, were left untouched as instructed.
- Did not attempt to add a test exercising the `FindProperty(MemberInfo)` vs `FindProperty(string)` divergence (e.g. a shadow property colliding by name with an explicit-interface member) since the brief's Step 1 test is the only test specified and the brief's "Files" section restricts test changes to `ExecuteUpdateTests.cs` with no additional case requested.

## Status

All three builds pass. `ExecuteUpdateTests`, `BulkOperationsCommandTests`, and `NorthwindBulkUpdatesMongoTest` pass on both EF9 and EF10 with zero failures. No conformance test needed reclassification.
