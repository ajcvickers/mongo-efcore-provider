### Task 3: `TryResolveMember` reports `isOuter`; correlated `Count(pred)` goes native

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.Members.cs:55-132` (`TryResolveMember`)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs` — every call site of `TryResolveMember` (listed in Step 2 below), plus the `Count(pred)` arm (~1037-1094)
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionCorrelatedTests.cs` (create)

**Interfaces:**
- Consumes (Task 1): `MongoExpressionTranslator.SelfParam`; `ReferencesEnclosingScope(body, elementParameter, out ParameterExpression? found)`.
- Consumes (Task 2): `MongoOuterFieldExpression`.
- Produces: `TryResolveMember(Expression node, out IProperty? property, out string? fieldPath, out bool isOuter)` — every existing caller updated.

- [ ] **Step 1: Write the failing FunctionalTest**

Create `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionCorrelatedTests.cs`, modeled directly on `NativeOwnedCollectionAllTests.cs`'s fixture/helper pattern (same `Blog`/`Post` shape, same `AssertNativeOnlyMatches`/`AssertDeclinesCleanly` idiom — copy those private helpers verbatim rather than sharing them across files, matching this codebase's existing per-file duplication convention for these test fixtures):

```csharp
/* Copyright 2023-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// EF-421: a correlated owned-collection element predicate — one referencing the immediately enclosing
/// entity, e.g. <c>b.Posts.Count(p =&gt; p.Title == b.Title) &gt; 0</c> — now goes native via a two-scope
/// translator, instead of declining to driver-LINQ.
/// </summary>
[XUnitCollection("QueryTests")]
public class NativeOwnedCollectionCorrelatedTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private static SingleEntityDbContext<T> CreateContext<T>(
        IMongoCollection<T> collection, MongoQueryMode mode, Action<ModelBuilder>? modelBuilderAction)
        where T : class
        => SingleEntityDbContext.Create(
            collection,
            modelBuilderAction: modelBuilderAction,
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

    private string UniqueCollectionName(string name)
        => TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];

    public class Blog
    {
        public ObjectId Id { get; set; }
        public string Title { get; set; } = "";
        public List<Post> Posts { get; set; } = [];
    }

    public class Post
    {
        // DELIBERATELY COLLIDES with Blog.Title — a mis-scoped (element-scoped) resolution of `b.Title` would
        // retarget at Post.Title and return the wrong rows, making this seed discriminating rather than
        // vacuous. Mirrors NativeOwnedCollectionAllTests' identical seeding rationale.
        public string Title { get; set; } = "";
        public int? Rank { get; set; }
    }

    private static readonly Action<ModelBuilder> BlogModel = mb => mb.Entity<Blog>().OwnsMany(b => b.Posts);

    private IMongoCollection<Blog> Seed(string name, params (string BlogTitle, (string PostTitle, int? Rank)[] Posts)[] rows)
    {
        var coll = database.MongoDatabase.GetCollection<BsonDocument>(UniqueCollectionName(name));
        coll.InsertMany(rows.Select(r => new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Title", r.BlogTitle },
            { "Posts", new BsonArray(r.Posts.Select(p => new BsonDocument
                {
                    { "Title", p.PostTitle },
                    { "Rank", p.Rank.HasValue ? p.Rank.Value : BsonNull.Value }
                }))
            }
        }));
        return database.MongoDatabase.GetCollection<Blog>(coll.CollectionNamespace.CollectionName);
    }

    private List<string> AssertNativeOnlyMatches(
        IMongoCollection<Blog> collection, Func<IQueryable<Blog>, IQueryable<Blog>> query)
    {
        using var db = CreateContext(collection, MongoQueryMode.NativeOnly, BlogModel);
        return query(db.Entities.AsNoTracking()).ToList().Select(b => b.Title).OrderBy(t => t).ToList();
    }

    [Fact]
    public void Correlated_filtered_Count_predicate_goes_native()
    {
        // "match": a post whose Title equals the OWNER's Title -> Count(pred) == 1 -> > 0 -> included.
        // "other": no post's Title equals the owner's Title ("other" vs post titles "x"/"y") -> excluded.
        var collection = Seed(nameof(Correlated_filtered_Count_predicate_goes_native),
            ("match", [("match", 1), ("x", 2)]),
            ("other", [("x", 1), ("y", 2)]));

        var titles = AssertNativeOnlyMatches(
            collection, q => q.Where(b => b.Posts.Count(p => p.Title == b.Title) > 0));

        Assert.Equal(new[] { "match" }, titles);
    }

    [Fact]
    public void Correlated_Any_goes_native()
    {
        var collection = Seed(nameof(Correlated_Any_goes_native),
            ("match", [("match", 1), ("x", 2)]),
            ("other", [("x", 1), ("y", 2)]));

        var titles = AssertNativeOnlyMatches(
            collection, q => q.Where(b => b.Posts.Any(p => p.Title == b.Title)));

        Assert.Equal(new[] { "match" }, titles);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails for the right reason**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollectionCorrelatedTests.Correlated_filtered_Count_predicate_goes_native"`
Expected: FAIL with `NativeTranslationNotSupportedException` (the current decline) — `Correlated_Any_goes_native` will also fail the same way; only run/fix `Count` first, `Any` is finished in Task 5.

- [ ] **Step 3: Give `TryResolveMember` an `out bool isOuter`**

In `MongoExpressionTranslator.Members.cs`, replace the signature and body:

```csharp
    private bool TryResolveMember(
        Expression node, [NotNullWhen(true)] out IProperty? property, [NotNullWhen(true)] out string? fieldPath)
    {
        property = null;
        fieldPath = null;
```

with:

```csharp
    private bool TryResolveMember(
        Expression node, [NotNullWhen(true)] out IProperty? property, [NotNullWhen(true)] out string? fieldPath,
        out bool isOuter)
    {
        property = null;
        fieldPath = null;
        isOuter = false;
```

and later in the same method, replace:

```csharp
            default:
                return TryResolveOwnedFieldPath(node, out property, out fieldPath);
        }

        // Two-scope mode: a member rooted on the outer param resolves against the outer entity type at document
        // root; every other member is inner-scoped. Identity (ReferenceEquals), never name — so a member name
        // shared between the two scopes cannot be mis-routed.
        var isOuter = _outerParam is not null && ReferenceEquals(param, _outerParam);
        var scopeType = isOuter ? _outerEntityType! : _entityType;
```

with:

```csharp
            default:
                return TryResolveOwnedFieldPath(node, out property, out fieldPath, out isOuter);
        }

        // Two-scope mode: a member rooted on the outer param resolves against the outer entity type at document
        // root; every other member is inner-scoped. Identity (ReferenceEquals), never name — so a member name
        // shared between the two scopes cannot be mis-routed.
        isOuter = _outerParam is not null && ReferenceEquals(param, _outerParam);
        var scopeType = isOuter ? _outerEntityType! : _entityType;
```

(The local `var isOuter` declaration further down is removed since it's now the out-parameter being assigned.)

- [ ] **Step 4: Update `TryResolveOwnedFieldPath`'s signature (mechanical only for this task — full relativization is Task 6)**

For this task, `TryResolveOwnedFieldPath` keeps its EXISTING two-scope decline (Task 6 relaxes it) but must accept and always set the new out-parameter to satisfy the compiler. In `MongoExpressionTranslator.Members.cs`, change:

```csharp
    private bool TryResolveOwnedFieldPath(
        Expression node, [NotNullWhen(true)] out IProperty? property, [NotNullWhen(true)] out string? fieldPath)
    {
        property = null;
        fieldPath = null;

        if (_outerParam is not null || _innerPrefix is not null)
            return false; // two-scope mode: owned dotted paths are out of scope (declined, falls back)
```

to:

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
```

- [ ] **Step 5: Update every `TryResolveMember`/`TryTranslateField` call site in `MongoExpressionTranslator.cs`**

Run `grep -n "TryResolveMember(" src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs` and update each. There are seven sites; apply exactly these rules — **decline (return null) rather than silently building a wrongly-scoped node** at every site except the two in `TranslateComparison`, which are the ones that matter for the ticket's shapes and get the real `MongoOuterFieldExpression` treatment:

**5a. `TryTranslateDateTimeMember` (~line 177):**

```csharp
        if (TryResolveMember(receiver, out var property, out var fieldPath))
        {
            receiverExpr = new MongoFieldExpression(property, fieldPath!);
        }
```

→

```csharp
        if (TryResolveMember(receiver, out var property, out var fieldPath, out var dateTimeIsOuter))
        {
            // A correlated outer-scoped DateTime/DateTimeOffset chain is out of EF-421's scope — decline
            // rather than silently building an inner-scoped field reference for an outer property.
            if (dateTimeIsOuter)
                return false;

            receiverExpr = new MongoFieldExpression(property, fieldPath!);
        }
```

**5b. Array-Contains receiver (~line 521):**

```csharp
            case MethodCallExpression call
                when TryMatchContainsMethod(call, out var arrayReceiver, out var containsItem)
                     && Unwrap(containsItem) is ConstantExpression
                     && TryResolveMember(Unwrap(arrayReceiver), out var arrayProperty, out var arrayFieldPath)
                     && GetEnumerableElementType(arrayProperty.ClrType) is not null:
```

→

```csharp
            case MethodCallExpression call
                when TryMatchContainsMethod(call, out var arrayReceiver, out var containsItem)
                     && Unwrap(containsItem) is ConstantExpression
                     && TryResolveMember(Unwrap(arrayReceiver), out var arrayProperty, out var arrayFieldPath, out var arrayIsOuter)
                     && !arrayIsOuter // an outer-scoped array receiver is out of EF-421's scope — decline
                     && GetEnumerableElementType(arrayProperty.ClrType) is not null:
```

**5c. Collection-membership item (~line 536):**

```csharp
            case MethodCallExpression call when TryMatchContainsMethod(call, out var collectionExpr, out var itemExpr):
            {
                if (!TryResolveMember(Unwrap(itemExpr), out var property, out var fieldPath))
                    return null; // item must resolve to a bare field
```

→

```csharp
            case MethodCallExpression call when TryMatchContainsMethod(call, out var collectionExpr, out var itemExpr):
            {
                if (!TryResolveMember(Unwrap(itemExpr), out var property, out var fieldPath, out var itemIsOuter)
                    || itemIsOuter) // an outer-scoped item is out of EF-421's scope — decline
                    return null; // item must resolve to a bare field
```

**5d. Regex receiver (~line 551):**

```csharp
            case MethodCallExpression call when TryMatchRegexMethod(call, out var kind, out var receiver, out var termExpr):
            {
                if (!TryResolveMember(Unwrap(receiver), out var property, out var fieldPath))
                    return null; // receiver must resolve to a bare string field
```

→

```csharp
            case MethodCallExpression call when TryMatchRegexMethod(call, out var kind, out var receiver, out var termExpr):
            {
                if (!TryResolveMember(Unwrap(receiver), out var property, out var fieldPath, out var receiverIsOuter)
                    || receiverIsOuter) // an outer-scoped receiver is out of EF-421's scope — decline
                    return null; // receiver must resolve to a bare string field
```

**5e. `HasValue` (~line 642):**

```csharp
            case MemberExpression { Member.Name: nameof(Nullable<int>.HasValue), Expression: { } hasValueReceiver }
                when Nullable.GetUnderlyingType(hasValueReceiver.Type) is not null:
            {
                if (!TryResolveMember(Unwrap(hasValueReceiver), out var nullableProperty, out var nullablePath))
                    return null;
```

→

```csharp
            case MemberExpression { Member.Name: nameof(Nullable<int>.HasValue), Expression: { } hasValueReceiver }
                when Nullable.GetUnderlyingType(hasValueReceiver.Type) is not null:
            {
                if (!TryResolveMember(Unwrap(hasValueReceiver), out var nullableProperty, out var nullablePath, out var hasValueIsOuter)
                    || hasValueIsOuter) // an outer-scoped nullable HasValue check is out of EF-421's scope — decline
                    return null;
```

**5f. Bare boolean member default (~line 654):**

```csharp
            default:
                if (TryResolveMember(node, out var boolProp, out var boolPath))
                {
                    // Accept only non-nullable bools; a nullable bool bare access could diverge.
                    if (boolProp!.ClrType != typeof(bool) || boolProp.IsNullable)
                        return null;
                    return new MongoFieldExpression(boolProp, boolPath!);
                }

                return null;
```

→

```csharp
            default:
                if (TryResolveMember(node, out var boolProp, out var boolPath, out var boolIsOuter))
                {
                    // Accept only non-nullable bools; a nullable bool bare access could diverge.
                    if (boolProp!.ClrType != typeof(bool) || boolProp.IsNullable)
                        return null;
                    return boolIsOuter
                        ? new MongoOuterFieldExpression(boolProp, boolPath!)
                        : new MongoFieldExpression(boolProp, boolPath!);
                }

                return null;
```

(This one DOES build `MongoOuterFieldExpression` on a match — a bare correlated bool, `b.Posts.Any(p => b.IsActive)`, is a real shape worth supporting since it costs nothing extra here.)

**5g/5h. `TranslateComparison`'s two call sites (~line 711 and ~line 744) — the ones that matter most:**

```csharp
        if (TryResolveMember(leftUnwrapped, out var leftProperty, out var leftPath) && IsSimpleValue(rightUnwrapped))
```

→

```csharp
        if (TryResolveMember(leftUnwrapped, out var leftProperty, out var leftPath, out var leftIsOuter)
            && IsSimpleValue(rightUnwrapped))
```

and further down in that same branch, replace:

```csharp
                return new MongoBinaryExpression(
                    mongoOp.Value, new MongoFieldExpression(leftProperty, leftPath!), valueExpr);
```

with:

```csharp
                MongoExpression leftField = leftIsOuter
                    ? new MongoOuterFieldExpression(leftProperty, leftPath!)
                    : new MongoFieldExpression(leftProperty, leftPath!);
                return new MongoBinaryExpression(mongoOp.Value, leftField, valueExpr);
```

Symmetrically for the mirrored branch:

```csharp
        else if (TryResolveMember(rightUnwrapped, out var rightProperty, out var rightPath)
                 && IsSimpleValue(leftUnwrapped))
```

→

```csharp
        else if (TryResolveMember(rightUnwrapped, out var rightProperty, out var rightPath, out var rightIsOuter)
                 && IsSimpleValue(leftUnwrapped))
```

and:

```csharp
                return new MongoBinaryExpression(
                    mongoOp.Value, new MongoFieldExpression(rightProperty, rightPath!), valueExpr);
```

→

```csharp
                MongoExpression rightField = rightIsOuter
                    ? new MongoOuterFieldExpression(rightProperty, rightPath!)
                    : new MongoFieldExpression(rightProperty, rightPath!);
                return new MongoBinaryExpression(mongoOp.Value, rightField, valueExpr);
```

> Note: `RenderComparison`/`RenderNode`'s `IsQueryNativeComparison` (`MongoQueryLanguageRenderer.cs:104`) checks `b.Left is MongoFieldExpression` — this is INTENTIONALLY unaffected: a comparison whose left side is now a `MongoOuterFieldExpression` is correctly no longer "query-native" (an outer field has no meaning as a bare `$match` clause), so it correctly routes to the `$expr`/aggregation path via `RenderNode`'s catch-all. No change needed there.

**5i. `TranslateOperand`'s own `TryResolveMember` call (~line 1016):**

```csharp
        if (TryResolveMember(node, out var property, out var fieldPath))
            return new MongoFieldExpression(property, fieldPath!);
```

→

```csharp
        if (TryResolveMember(node, out var property, out var fieldPath, out var operandIsOuter))
        {
            return operandIsOuter
                ? new MongoOuterFieldExpression(property, fieldPath!)
                : new MongoFieldExpression(property, fieldPath!);
        }
```

(This is the operand path used by arithmetic/field-to-field comparisons and computed values — e.g. `p.Rank == b.Threshold + 1` — so it also needs the real branch, not a decline.)

- [ ] **Step 6: Update `TryTranslateField` (sort-key path) — mechanical only**

In `MongoExpressionTranslator.cs`, `TryTranslateField` (~line 108-116):

```csharp
    public bool TryTranslateField(Expression keySelectorBody, [NotNullWhen(true)] out MongoFieldExpression? result)
    {
        result = null;
        if (!TryResolveMember(UnwrapOrderPreserving(keySelectorBody), out var property, out var path))
            return false;

        result = new MongoFieldExpression(property, path);
        return true;
    }
```

→

```csharp
    public bool TryTranslateField(Expression keySelectorBody, [NotNullWhen(true)] out MongoFieldExpression? result)
    {
        result = null;
        if (!TryResolveMember(UnwrapOrderPreserving(keySelectorBody), out var property, out var path, out var isOuter)
            || isOuter) // a sort key is never inside a $filter/$map scope, so an outer-scoped one has no
                         // meaning here — this translator is always the ROOT translator for a sort key
                         // (never itself the two-scope child), so isOuter is always false in practice; the
                         // guard exists so a future caller cannot silently regress this.
            return false;

        result = new MongoFieldExpression(property, path);
        return true;
    }
```

- [ ] **Step 7: Now wire the real correlation branch at the `Count(pred)` call site**

In `MongoExpressionTranslator.cs`, `TranslateOperand`'s `Count(pred)` arm (~line 1037-1094), replace:

```csharp
            if (countPredicate is null)
                return new MongoSizeExpression(arrayPath, node.Type, nullSafe: true);

            // A FILTERED count. The element predicate is translated exactly as a quantifier's is — same
            // correlated-scope guard, same element-scoped child translator.
            //
            // The correlated guard is load-bearing: single-scope TryResolveMember resolves a member by NAME
            // with no parameter-identity check, so an enclosing-scoped access whose name also exists on the
            // element would be silently retargeted at the element — wrong rows under the default Native mode.
            // A $filter cond CAN legally reference the enclosing document (unlike $elemMatch, which cannot at
            // all), so correlated support here is a deferrable capability, not an impossibility.
            if (ReferencesEnclosingScope(countPredicate.Body, countPredicate.Parameters[0]))
                return null;

            var countElementTranslator = new MongoExpressionTranslator(countElementType);
            if (!countElementTranslator.TryTranslate(countPredicate.Body, out var elementPredicate))
                return null;
```

with:

```csharp
            if (countPredicate is null)
                return new MongoSizeExpression(arrayPath, node.Type, nullSafe: true);

            // A FILTERED count. The element predicate is translated exactly as a quantifier's is — same
            // correlated-scope guard, same element-scoped child translator.
            //
            // The correlated guard is load-bearing: single-scope TryResolveMember resolves a member by NAME
            // with no parameter-identity check, so an enclosing-scoped access whose name also exists on the
            // element would be silently retargeted at the element — wrong rows under the default Native mode.
            // A $filter cond CAN legally reference the enclosing document (unlike $elemMatch, which cannot at
            // all), so correlated support here is a deferrable capability, not an impossibility.
            //
            // EF-421: a correlation whose free parameter is IDENTICAL (ReferenceEquals) to THIS translator's
            // own SelfParam is no longer declined outright — it is translated with a two-scope child
            // translator instead, mirroring NativeSelectManyBinder's existing pattern. Any other free
            // parameter (correlation reaching past the immediate enclosing scope) still declines exactly as
            // before — SelfParam is null for a translator that is itself already a two-scope child, so a
            // two-level-deep correlation can never match here.
            MongoExpressionTranslator countElementTranslator;
            if (ReferencesEnclosingScope(countPredicate.Body, countPredicate.Parameters[0], out var countFreeParam))
            {
                if (SelfParam is null || !ReferenceEquals(countFreeParam, SelfParam))
                    return null;

                countElementTranslator = new MongoExpressionTranslator(
                    countElementType, outerParam: SelfParam, outerEntityType: _entityType, innerPrefix: "");
            }
            else
            {
                countElementTranslator = new MongoExpressionTranslator(countElementType);
            }

            if (!countElementTranslator.TryTranslate(countPredicate.Body, out var elementPredicate))
                return null;
```

- [ ] **Step 8: Build**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`
Expected: builds clean.

- [ ] **Step 9: Run the new test to verify it passes**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollectionCorrelatedTests.Correlated_filtered_Count_predicate_goes_native"`
Expected: PASS. (`Correlated_Any_goes_native` still fails — expected, fixed in Task 5.)

- [ ] **Step 10: Run the full existing native-translation FunctionalTests suite to confirm no regression**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollection"`
Expected: every PRE-EXISTING test still passes (only the new file's `Correlated_Any_goes_native` fails, expected).

- [ ] **Step 11: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.Members.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionCorrelatedTests.cs
git commit -m "EF-421: correlated filtered Count(pred) goes native via a two-scope translator"
```

---

