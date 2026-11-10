### Task 3: Unit-test `IsTransactionsUnsupported`

`ExecuteDeleteTests.cs` skips the standalone-deployment test with the justification *"The error mapping is covered by `IsTransactionsUnsupported`"* — but nothing covers that predicate. It is a pure static function over an `Exception` and needs no server, so the skip reason does not apply to it. The false-positive direction matters most: `MongoCommandException` code 20 is `IllegalOperation`, which is raised for plenty of things other than "transactions unsupported".

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkTransactionErrorClassificationTests.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `MongoShapedQueryCompilingExpressionVisitor.IsTransactionsUnsupported(Exception)` changes from `private static` to `internal static` so unit tests can reach it. Signature unchanged: `internal static bool IsTransactionsUnsupported(Exception exception)`.

- [ ] **Step 1: Make the predicate reachable from tests**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, change the accessibility of the predicate only:

```csharp
    // Multi-document transactions are rejected when the deployment doesn't support them, and the failure can
    // surface in three shapes: (1) the provider's own transaction startup (MongoTransaction.Start) intercepts the
    // driver's "Standalone servers do not support transactions." and rethrows a NotSupportedException whose message
    // contains "does not support transactions"; (2) a raw driver MongoCommandException (code 20 / IllegalOperation)
    // whose message names the transaction-number restriction; (3) a "Transaction numbers are only allowed on a
    // replica set member or mongos" message on any MongoException. Match conservatively so unrelated failures —
    // including other IllegalOperation errors raised inside a transaction — propagate untouched.
    internal static bool IsTransactionsUnsupported(Exception exception)
```

Confirm `tests/MongoDB.EntityFrameworkCore.UnitTests` already has `InternalsVisibleTo` access:

```bash
grep -rn "InternalsVisibleTo" src/MongoDB.EntityFrameworkCore/ | grep -i unittest
```

If it returns nothing, add `UnitTests` alongside the existing entries in whichever file declares `InternalsVisibleTo` for the other test projects — do not create a new mechanism.

- [ ] **Step 2: Write the failing tests**

Create `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkTransactionErrorClassificationTests.cs`:

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

// Server-free coverage for the transaction-unsupported classification used by two-phase bulk
// operations. ExecuteDeleteTests skips the end-to-end standalone-deployment test because CI runs a
// replica set; this predicate is a pure function and has no such constraint.

#if !EF8

using System;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Query.Visitors;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query;

public static class BulkTransactionErrorClassificationTests
{
    private static MongoCommandException CommandException(int code, string message)
        => new(
            new MongoConnectionId(new MongoServerId(new MongoDB.Driver.Core.Clusters.ClusterId(), null!)),
            message,
            new BsonDocument("ok", 0),
            new BsonDocument { { "ok", 0 }, { "code", code }, { "errmsg", message } });

    [Fact]
    public static void Classifies_NotSupportedException_from_provider_transaction_startup()
    {
        var exception = new NotSupportedException(
            "The current MongoDB deployment does not support transactions.");

        Assert.True(MongoShapedQueryCompilingExpressionVisitor.IsTransactionsUnsupported(exception));
    }

    [Fact]
    public static void Classifies_transaction_numbers_message_on_a_MongoException()
    {
        var exception = CommandException(
            20, "Transaction numbers are only allowed on a replica set member or mongos");

        Assert.True(MongoShapedQueryCompilingExpressionVisitor.IsTransactionsUnsupported(exception));
    }

    [Fact]
    public static void Does_not_classify_an_unrelated_IllegalOperation()
    {
        // Code 20 is IllegalOperation generally. An unrelated illegal operation inside a transaction
        // must NOT be relabelled as "your deployment does not support transactions".
        var exception = CommandException(
            20, "Stage not supported inside of a multi-document transaction: $unionWith");

        Assert.False(MongoShapedQueryCompilingExpressionVisitor.IsTransactionsUnsupported(exception));
    }

    [Fact]
    public static void Does_not_classify_an_unrelated_exception()
        => Assert.False(
            MongoShapedQueryCompilingExpressionVisitor.IsTransactionsUnsupported(
                new InvalidOperationException("something else entirely")));
}

#endif
```

If the `MongoCommandException` constructor arguments do not compile against the pinned driver version, replace the `CommandException` helper with the smallest constructor the driver exposes that lets you set `Code` and `Message`; check `grep -rn "new MongoCommandException" tests/` for an existing pattern in this repo and copy it.

- [ ] **Step 3: Run the tests to verify the third fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkTransactionErrorClassificationTests"
```

Expected: `Does_not_classify_an_unrelated_IllegalOperation` FAILS — the current predicate matches on `Code: 20` alone, so it returns `true`. The other three PASS.

- [ ] **Step 4: Tighten the predicate so code 20 also requires the transaction-number message**

Replace the predicate body:

```csharp
    internal static bool IsTransactionsUnsupported(Exception exception)
        => (exception is MongoException
            && exception.Message.Contains("Transaction numbers are only allowed", StringComparison.Ordinal))
           || (exception is NotSupportedException
               && exception.Message.Contains("does not support transactions", StringComparison.Ordinal));
```

The `MongoCommandException { Code: 20 }` arm is removed: the transaction-number failure it was there to catch is a `MongoException` and is already matched by the first arm on its message, and code 20 on its own is too broad.

- [ ] **Step 5: Run the tests to verify all pass**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkTransactionErrorClassificationTests"
```

Expected: PASS, 4 tests.

- [ ] **Step 6: Correct the skipped test's justification**

In `ExecuteDeleteTests.cs`, replace the `Skip` reason on `ExecuteDelete_two_phase_on_standalone_throws_actionable_error` so it no longer claims coverage that only now exists elsewhere:

```csharp
    [Fact(Skip = "Requires a standalone (non-replica-set) MongoDB to exercise the end-to-end path; CI/local use "
        + "a replica set (atlas-local). The error classification itself is unit-tested server-free in "
        + "UnitTests/Query/BulkTransactionErrorClassificationTests.cs. EF-107")]
```

- [ ] **Step 7: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkTransactionErrorClassificationTests.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "fix: narrow transaction-unsupported classification and unit-test it server-free"
```

---

