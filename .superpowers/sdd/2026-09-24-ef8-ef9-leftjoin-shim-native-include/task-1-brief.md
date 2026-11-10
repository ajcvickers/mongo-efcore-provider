### Task 1: Fix the native candidate-join recognizer and prove it with the existing optional-reference-Include test

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs:355-361`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeReferenceIncludeTests.cs:973-991` (remove the `#if !EF8 && !EF9` / `#endif` guard around `Optional_reference_Include_goes_native_with_a_left_outer_unwind` so the test runs on every EF version)

**Interfaces:**
- Consumes: `MongoQueryableMethodTranslatingExpressionVisitor.IsEf8Ef9LeftJoinShim(MethodInfo method)` (already public/internal-static, defined at `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs:108-110`) — takes the raw (possibly closed-generic) `MethodInfo` and normalizes internally, so pass `call.Method`, not `methodDefinition`.
- Produces: no new public surface. `NativeSlotPopulator.PopulateNativeSlots` now also calls `mongoQ.Select.MarkSawCandidateReferenceIncludeJoin()` when the method is the EF8/EF9 `LeftJoin` shim, under `#if EF8 || EF9`.

- [ ] **Step 1: Remove the version guard on the regression test so it runs (and fails) on EF8/EF9**

Edit `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeReferenceIncludeTests.cs`. Find:

```csharp
#if !EF8 && !EF9
    [Fact]
    public void Optional_reference_Include_goes_native_with_a_left_outer_unwind()
    {
        using var db = CreateContext(MongoQueryMode.NativeOnly,
            nameof(Optional_reference_Include_goes_native_with_a_left_outer_unwind), out var spyLogger);

        var results = db.Orders.Include(o => o.Carrier).ToList();

        // Left-outer: rows with no FK and rows with a DANGLING FK both survive, navigation null. All 4
        // seeded orders survive.
        Assert.Equal(4, results.Count);
        Assert.Contains(results, o => o.Carrier == null);

        spyLogger.AssertExecutedMqlContains("{ \"$lookup\" : { \"from\" : \"" + db.CarriersCollectionName +
            "\", \"localField\" : \"CarrierId\", \"foreignField\" : \"_id\", \"as\" : \"_lookup_Carrier\" } }, " +
            "{ \"$unwind\" : { \"path\" : \"$_lookup_Carrier\", \"preserveNullAndEmptyArrays\" : true } }");
    }
#endif
```

Replace with the same body, minus the `#if`/`#endif`:

```csharp
    [Fact]
    public void Optional_reference_Include_goes_native_with_a_left_outer_unwind()
    {
        using var db = CreateContext(MongoQueryMode.NativeOnly,
            nameof(Optional_reference_Include_goes_native_with_a_left_outer_unwind), out var spyLogger);

        var results = db.Orders.Include(o => o.Carrier).ToList();

        // Left-outer: rows with no FK and rows with a DANGLING FK both survive, navigation null. All 4
        // seeded orders survive.
        Assert.Equal(4, results.Count);
        Assert.Contains(results, o => o.Carrier == null);

        spyLogger.AssertExecutedMqlContains("{ \"$lookup\" : { \"from\" : \"" + db.CarriersCollectionName +
            "\", \"localField\" : \"CarrierId\", \"foreignField\" : \"_id\", \"as\" : \"_lookup_Carrier\" } }, " +
            "{ \"$unwind\" : { \"path\" : \"$_lookup_Carrier\", \"preserveNullAndEmptyArrays\" : true } }");
    }
```

- [ ] **Step 2: Build EF9 and run the test to confirm it fails without the source fix**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~NativeReferenceIncludeTests.Optional_reference_Include_goes_native_with_a_left_outer_unwind"
```

Expected: **FAIL** with `MongoDB.EntityFrameworkCore.Query.NativeTranslation.NativeTranslationNotSupportedException` (the query declines to native and `MongoQueryMode.NativeOnly` forbids the fallback). Run with `MONGODB_URI` and `ATLAS_URI` unset so Docker provides the test server.

- [ ] **Step 3: Apply the source fix**

Edit `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs`. Find:

```csharp
        else if (methodDefinition == QueryableMethods.Join
                 || methodDefinition == QueryableMethods.GroupJoin
#if !EF8 && !EF9
                 || methodDefinition == QueryableMethods.LeftJoin
#endif
                )
        {
            // Might be EF's nav-expansion of a single-level reference Include. Record a candidate rather
            // than marking non-native; TranslateSelect confirms it when the trailing IncludeExpression
            // matches the recognizer. Unconfirmed candidates route to Fallback, so this is default-deny and
            // a user join is unaffected. See MongoSelectDefinition §Reference-Include candidate join.
            mongoQ.Select.MarkSawCandidateReferenceIncludeJoin();
        }
```

Replace with:

```csharp
        else if (methodDefinition == QueryableMethods.Join
                 || methodDefinition == QueryableMethods.GroupJoin
#if !EF8 && !EF9
                 || methodDefinition == QueryableMethods.LeftJoin
#else
                 // EF8/EF9 lower a GroupJoin+DefaultIfEmpty pair — including EF's own nav-expansion of an
                 // OPTIONAL reference Include — onto this same private shim rather than a public LeftJoin
                 // method (see Ef8Ef9LeftJoinMethod's remarks). Recognize it here exactly like the EF10
                 // public LeftJoin above, or every EF8/EF9 optional reference Include is marked non-native
                 // before the join-scope binder ever runs (EF-322).
                 || MongoQueryableMethodTranslatingExpressionVisitor.IsEf8Ef9LeftJoinShim(call.Method)
#endif
                )
        {
            // Might be EF's nav-expansion of a single-level reference Include. Record a candidate rather
            // than marking non-native; TranslateSelect confirms it when the trailing IncludeExpression
            // matches the recognizer. Unconfirmed candidates route to Fallback, so this is default-deny and
            // a user join is unaffected. See MongoSelectDefinition §Reference-Include candidate join.
            mongoQ.Select.MarkSawCandidateReferenceIncludeJoin();
        }
```

`MongoQueryableMethodTranslatingExpressionVisitor` lives in `MongoDB.EntityFrameworkCore.Query.Visitors`, a different namespace than `NativeSlotPopulator` (`MongoDB.EntityFrameworkCore.Query.NativeTranslation`). Add the using directive at the top of the file (alongside the existing `using MongoDB.EntityFrameworkCore.Query.Expressions;`):

```csharp
using MongoDB.EntityFrameworkCore.Query.Visitors;
```

- [ ] **Step 4: Rebuild and confirm the test passes on EF9, then verify EF8 and EF10 too**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF9" --no-build \
  --filter "FullyQualifiedName~NativeReferenceIncludeTests"
```

Expected: all tests in the class **PASS**, including `Optional_reference_Include_goes_native_with_a_left_outer_unwind`.

Repeat for EF8:

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF8" --no-build \
  --filter "FullyQualifiedName~NativeReferenceIncludeTests"
```

Expected: all tests **PASS**.

Then confirm no regression on EF10 (the branch this fix doesn't change, since `#if !EF8 && !EF9` still takes the original path there):

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/MongoDB.EntityFrameworkCore.FunctionalTests.csproj \
  -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NativeReferenceIncludeTests"
```

Expected: all tests **PASS** (unchanged from before this task).

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSlotPopulator.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeReferenceIncludeTests.cs
git commit -m "EF-322: recognize the EF8/EF9 LeftJoin shim as a candidate reference-Include join"
```

---

