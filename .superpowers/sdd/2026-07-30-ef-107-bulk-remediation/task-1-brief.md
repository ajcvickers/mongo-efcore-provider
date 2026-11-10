### Task 1: Restore the deleted foreign-queryable-extension regression test

The PR renamed `InvalidQueryTranslationTests.cs` to `BulkOperationsUnsupportedOnEf8Tests.cs`, wrapped it in `#if EF8`, and deleted its `FakeQueryableExtensions` helper. That helper was the only test of the `AllowedQueryableExtensions` gate — the guard the new `EntityFrameworkQueryableExtensions` early-return in `VisitMethodCall` now sits directly above. It must run on all EF versions.

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/InvalidQueryTranslationTests.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsUnsupportedOnEf8Tests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing consumed by later tasks. This is an independent regression guard.

- [ ] **Step 1: Recreate the test file with the shim, unconditioned on EF version**

Create `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/InvalidQueryTranslationTests.cs`:

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

// MongoQueryableMethodTranslatingExpressionVisitor.VisitMethodCall admits only Queryable,
// MongoQueryableExtensions and MongoDB.Driver.Linq.MongoQueryable (plus, on EF9+, the
// EntityFrameworkQueryableExtensions bulk operators, which it forwards to the base). A queryable
// extension method declared anywhere else must still produce EF's canonical translation failure.
// This pins that gate on every EF version — it is deliberately NOT #if'd, because the gate exists
// on every version and the EF9+ bulk early-return sits directly above it.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

[XUnitCollection("QueryTests")]
public class InvalidQueryTranslationTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class SimpleEntity
    {
        public ObjectId _id { get; set; }
        public string name { get; set; }
    }

    [Fact]
    public void Foreign_queryable_extension_throws_translation_failure()
    {
        using var db = SingleEntityDbContext.Create(database.CreateCollection<SimpleEntity>());

        var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.FakeExecuteDelete());

        Assert.Contains("could not be translated", ex.Message);
        Assert.Contains("FakeExecuteDelete", ex.Message);
    }
}

internal static class FakeQueryableExtensions
{
    internal static int FakeExecuteDelete<TSource>(this IQueryable<TSource> source)
        => source.Provider.Execute<int>(
            Expression.Call(FakeExecuteDeleteMethodInfo.MakeGenericMethod(typeof(TSource)), source.Expression));

    private static readonly MethodInfo FakeExecuteDeleteMethodInfo
        = typeof(FakeQueryableExtensions).GetTypeInfo().GetDeclaredMethod(nameof(FakeExecuteDelete))!;
}
```

- [ ] **Step 2: Run the test on all three configurations to see it pass or learn the real message**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~InvalidQueryTranslationTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~InvalidQueryTranslationTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" \
  --filter "FullyQualifiedName~InvalidQueryTranslationTests"
```

Expected: PASS on all three. If the second assertion fails because EF prints the expression tree rather than the method name, replace `Assert.Contains("FakeExecuteDelete", ex.Message)` with `Assert.Contains("FakeExecuteDelete", ex.Message, StringComparison.OrdinalIgnoreCase)` and, if it still fails, delete that assertion and record why in a comment above the remaining one. Do not weaken the `"could not be translated"` assertion.

- [ ] **Step 3: Add the missing EF8 `ExecuteUpdate` case**

In `BulkOperationsUnsupportedOnEf8Tests.cs`, after `ExecuteDelete_throws_translation_failure`, add:

```csharp
    [Fact]
    public void ExecuteUpdate_throws_translation_failure()
    {
        using var db = SingleEntityDbContext.Create(database.CreateCollection<SimpleEntity>());

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(e => e._id, ObjectId.Empty)));

        Assert.Contains("could not be translated", ex.Message);
    }
```

- [ ] **Step 4: Run the EF8 suite**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8" \
  --filter "FullyQualifiedName~BulkOperationsUnsupportedOnEf8Tests"
```

Expected: PASS, 2 tests.

- [ ] **Step 5: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/InvalidQueryTranslationTests.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsUnsupportedOnEf8Tests.cs
git commit -m "test: restore foreign-queryable-extension regression test, add EF8 ExecuteUpdate case"
```

---

