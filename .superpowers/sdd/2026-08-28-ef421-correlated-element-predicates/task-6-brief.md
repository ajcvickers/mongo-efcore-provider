### Task 6: Relativize `TryResolveOwnedFieldPath`/`TryResolveOwnedCollectionPath`

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.Members.cs:171-238` (`TryResolveOwnedFieldPath`), `:310-361` (`TryResolveOwnedCollectionPath`)
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionCorrelatedTests.cs` (extend)

**Interfaces:**
- Produces: `TryResolveOwnedCollectionPath(Expression source, out string? arrayPath, out IEntityType? elementType, out bool isOuter)` (new out-param) — check callers (`MongoExpressionTranslator.cs`'s quantifier and `Count`/`TryTranslateOwnedCollectionArray` sites) and thread through with the same decline-unless-vetted discipline as Task 3.

- [ ] **Step 1: Write the failing test — a correlated element predicate reaching through an OUTER owned single-reference hop**

Append to `NativeOwnedCollectionCorrelatedTests.cs`:

```csharp
    public class RefBlog
    {
        public ObjectId Id { get; set; }
        public string Title { get; set; } = "";
        public RefOwner Owner { get; set; } = null!;
        public List<RefPost> Posts { get; set; } = [];
    }

    public class RefOwner
    {
        public string City { get; set; } = "";
    }

    public class RefPost
    {
        public string City { get; set; } = "";
    }

    private static readonly Action<ModelBuilder> RefBlogModel = mb =>
    {
        mb.Entity<RefBlog>().OwnsOne(b => b.Owner);
        mb.Entity<RefBlog>().OwnsMany(b => b.Posts);
    };

    [Fact]
    public void Correlated_Any_through_an_outer_owned_single_reference_hop_goes_native()
    {
        var raw = database.MongoDatabase.GetCollection<BsonDocument>(
            UniqueCollectionName(nameof(Correlated_Any_through_an_outer_owned_single_reference_hop_goes_native)));
        var collection = database.MongoDatabase.GetCollection<RefBlog>(raw.CollectionNamespace.CollectionName);

        using (var seedDb = CreateContext(collection, MongoQueryMode.DriverLinq, RefBlogModel))
        {
            seedDb.Entities.Add(new RefBlog
            {
                Title = "match", Owner = new RefOwner { City = "Springfield" },
                Posts = [new RefPost { City = "Springfield" }, new RefPost { City = "Shelbyville" }]
            });
            seedDb.Entities.Add(new RefBlog
            {
                Title = "nomatch", Owner = new RefOwner { City = "Springfield" },
                Posts = [new RefPost { City = "Shelbyville" }]
            });
            seedDb.SaveChanges();
        }

        using var db = CreateContext(collection, MongoQueryMode.NativeOnly, RefBlogModel);

        var titles = db.Entities.AsNoTracking()
            .Where(b => b.Posts.Any(p => p.City == b.Owner.City))
            .ToList().Select(b => b.Title).ToList();

        Assert.Equal(new[] { "match" }, titles);
    }
```

- [ ] **Step 2: Run to verify RED**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~Correlated_Any_through_an_outer_owned_single_reference_hop_goes_native"`
Expected: FAIL with `NativeTranslationNotSupportedException` — `TryResolveOwnedFieldPath` still declines two-scope mode outright, so `b.Owner.City` (a 2-hop chain on the OUTER parameter) fails to resolve inside the correlated element translator built in Task 5, and `MongoAggregationExpressionRenderer.CanRender` (or the inner `TryTranslate`) fails.

- [ ] **Step 3: Relativize `TryResolveOwnedFieldPath`**

Replace:

```csharp
    private bool TryResolveOwnedFieldPath(
        Expression node, [NotNullWhen(true)] out IProperty? property, [NotNullWhen(true)] out string? fieldPath,
        out bool isOuter)
    {
        property = null;
        fieldPath = null;
        isOuter = false;

        if (_outerParam is not null || _innerPrefix is not null)
            return false; // two-scope mode: owned dotted paths are out of scope (declined, falls back) — Task 6 relativizes this

        // Collect hop names from the outer (leaf) hop inward; the root must be the query parameter.
        var names = new List<string>();
        var current = node;
        while (TryGetMemberOrEFProperty(current, out var inner, out var name))
        {
            names.Add(name);
            current = inner;
        }

        if (current is not ParameterExpression)
            return false;

        // A single top-level member is handled by TryResolveMember's fast path, never here.
        if (names.Count < 2)
            return false;

        names.Reverse(); // now root-first: [firstNav, ..., leaf]

        var scopeType = _entityType;
        var segments = new List<string>(names.Count);
```

with:

```csharp
    private bool TryResolveOwnedFieldPath(
        Expression node, [NotNullWhen(true)] out IProperty? property, [NotNullWhen(true)] out string? fieldPath,
        out bool isOuter)
    {
        property = null;
        fieldPath = null;
        isOuter = false;

        // A dotted chain reached from an INNER-prefixed scope (SelectMany's unwind prefix) still declines: that
        // shape is a different, still-out-of-scope combination this ticket does not address (a dotted owned
        // path reached from inside a SelectMany element, itself further correlated). Only the "root is the
        // OUTER param" two-scope case is relativized here — see EF-421 §6.
        if (_innerPrefix is not null)
            return false;

        // Collect hop names from the outer (leaf) hop inward; the root must be a parameter.
        var names = new List<string>();
        var current = node;
        while (TryGetMemberOrEFProperty(current, out var inner, out var name))
        {
            names.Add(name);
            current = inner;
        }

        if (current is not ParameterExpression rootParam)
            return false;

        // A single top-level member is handled by TryResolveMember's fast path, never here.
        if (names.Count < 2)
            return false;

        // EF-421: resolve the hop's ROOT identity, mirroring TryResolveMember's own isOuter check — never by
        // name. A two-scope translator whose root is the OUTER param resolves against the outer entity type,
        // at the outer document's OWN root (segments accumulate relative to _outerEntityType, exactly as the
        // existing code below already does relative to _entityType — the scopeType seed is the only change).
        // A single-scope translator (_outerParam is null) is unaffected: isOuter is always false there.
        isOuter = _outerParam is not null && ReferenceEquals(rootParam, _outerParam);
        if (_outerParam is not null && !isOuter)
            return false; // two-scope mode, but rooted on neither known parameter — not reachable in practice,
                           // decline rather than guess which scope it belongs to

        names.Reverse(); // now root-first: [firstNav, ..., leaf]

        var scopeType = isOuter ? _outerEntityType! : _entityType;
        var segments = new List<string>(names.Count);
```

The remainder of the method (the hop-walking loop and leaf construction) is UNCHANGED — it already operates on the local `scopeType`/`segments` variables, which now correctly seed from either scope.

- [ ] **Step 4: Update `TryResolveMember`'s default-branch call to pass through `isOuter`** (already done in Task 3, Step 3 — confirm no further change needed by re-reading that call site)

- [ ] **Step 5: Relativize `TryResolveOwnedCollectionPath`**

Replace:

```csharp
    private bool TryResolveOwnedCollectionPath(
        Expression source,
        [NotNullWhen(true)] out string? arrayPath,
        [NotNullWhen(true)] out IEntityType? elementType)
    {
        arrayPath = null;
        elementType = null;

        if (_outerParam is not null || _innerPrefix is not null)
            return false; // two-scope mode: cross-scope quantifiers are out of scope (declined, falls back)

        // Collect hop names from the outer hop inward; the root must be the query parameter.
        var names = new List<string>();
        var current = source;
        while (TryGetMemberOrEFProperty(current, out var inner, out var name))
        {
            names.Add(name);
            current = inner;
        }

        if (current is not ParameterExpression || names.Count == 0)
            return false;

        names.Reverse(); // now root-first: [ownedRefNav, ..., collectionNav]

        var scopeType = _entityType;
        var segments = new List<string>(names.Count);
```

with:

```csharp
    private bool TryResolveOwnedCollectionPath(
        Expression source,
        [NotNullWhen(true)] out string? arrayPath,
        [NotNullWhen(true)] out IEntityType? elementType,
        out bool isOuter)
    {
        arrayPath = null;
        elementType = null;
        isOuter = false;

        // Same restriction as TryResolveOwnedFieldPath: an inner-prefixed (SelectMany unwind) scope still
        // declines outright — only the "root is the OUTER param" case is relativized.
        if (_innerPrefix is not null)
            return false;

        // Collect hop names from the outer hop inward; the root must be a parameter.
        var names = new List<string>();
        var current = source;
        while (TryGetMemberOrEFProperty(current, out var inner, out var name))
        {
            names.Add(name);
            current = inner;
        }

        if (current is not ParameterExpression rootParam || names.Count == 0)
            return false;

        isOuter = _outerParam is not null && ReferenceEquals(rootParam, _outerParam);
        if (_outerParam is not null && !isOuter)
            return false; // two-scope mode, rooted on neither known parameter — decline

        names.Reverse(); // now root-first: [ownedRefNav, ..., collectionNav]

        var scopeType = isOuter ? _outerEntityType! : _entityType;
        var segments = new List<string>(names.Count);
```

The remainder of the method is UNCHANGED.

- [ ] **Step 6: Update `TryResolveOwnedCollectionPath`'s TWO callers in `MongoExpressionTranslator.cs`**

Run `grep -n "TryResolveOwnedCollectionPath(" src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs`. There are three call sites:

**6a. `TryTranslateOwnedCollectionArray` (~line 249):**

```csharp
        var source = UnwrapAsQueryable(expression);
        if (TryResolveOwnedCollectionPath(source, out var arrayPath, out _))
```

→

```csharp
        var source = UnwrapAsQueryable(expression);
        if (TryResolveOwnedCollectionPath(source, out var arrayPath, out _, out var arrayIsOuter) && !arrayIsOuter)
```

(A projection leaf is built by a root-scoped `NativeProjectionBinder` translator, which is never itself a two-scope child — `isOuter` is always false there in practice; the guard exists defensively so a future caller cannot silently regress this, matching the pattern from Task 3's `TryTranslateField`.)

**6b. Quantifier arm (~line 570):**

```csharp
                if (!TryResolveOwnedCollectionPath(Unwrap(quantifierSource), out var arrayPath, out var elementType))
                    return null; // not an owned-collection source rooted at the query parameter
```

→

```csharp
                if (!TryResolveOwnedCollectionPath(Unwrap(quantifierSource), out var arrayPath, out var elementType, out var sourceIsOuter))
                    return null; // not an owned-collection source rooted at the query parameter

                if (sourceIsOuter)
                    return null; // the ARRAY itself being reached through the outer scope is a separate, not-yet-supported shape (e.g. a quantifier over an outer sibling collection) — out of EF-421's scope
```

**6c. `Count(pred)` arm (~line 1038):**

```csharp
        if (TryMatchCountExpression(node, out var countSource, out var countPredicate)
            && TryResolveOwnedCollectionPath(countSource, out var arrayPath, out var countElementType))
        {
```

→

```csharp
        if (TryMatchCountExpression(node, out var countSource, out var countPredicate)
            && TryResolveOwnedCollectionPath(countSource, out var arrayPath, out var countElementType, out var countSourceIsOuter)
            && !countSourceIsOuter) // same out-of-scope note as the quantifier arm above
        {
```

- [ ] **Step 7: Build**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`
Expected: builds clean.

- [ ] **Step 8: Run to verify GREEN**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~Correlated_Any_through_an_outer_owned_single_reference_hop_goes_native"`
Expected: PASS.

- [ ] **Step 9: Run the full `NativeOwnedCollection*` and `NativeSelectMany*` suites to confirm no regression**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollection|FullyQualifiedName~NativeSelectMany"`
Expected: every PRE-EXISTING test passes (the one already-known exception from Task 5, `All_with_a_correlated_element_predicate_declines_and_falls_back_to_correct_rows`, is still expected to fail here — resolved in Task 8). `NativeSelectMany*` in particular must be UNCHANGED — this is the regression check for Task 6's edit to a method `NativeSelectManyBinder` does NOT call (only `MongoExpressionTranslator`'s own quantifier/count arms call `TryResolveOwnedCollectionPath`/`TryResolveOwnedFieldPath`), but re-run it anyway since `TryResolveMember`'s two-scope branch (shared code) was touched in Task 3.

- [ ] **Step 10: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.Members.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionCorrelatedTests.cs
git commit -m "EF-421: relativize TryResolveOwnedFieldPath/TryResolveOwnedCollectionPath for the outer scope"
```

---

