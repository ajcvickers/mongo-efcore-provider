# EF-107 Bulk ExecuteUpdate/ExecuteDelete — Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the correctness, data-integrity, observability, performance, security and coverage findings from five reviews of the EF-107 bulk `ExecuteUpdate`/`ExecuteDelete` implementation, without changing its supported query subset except where a review identified a gap.

**Architecture:** The feature is a translation-time classifier (`ClassifyBulkSource` in `MongoQueryableMethodTranslatingExpressionVisitor`) producing a `MongoNonQueryExpression` marker node, consumed by eight static executors in `MongoShapedQueryCompilingExpressionVisitor`. Remediation keeps that shape. It narrows the two-phase strategy to shapes that actually need it, adds guards where setters can corrupt provider-owned fields, makes the transaction and translation ordering fail closed, adds redaction-gated command logging so behaviour becomes assertable, and then closes the performance and coverage gaps that logging unlocks.

**Tech Stack:** C# / .NET 8 (`net8.0` for `src`, `net10.0` for tests), EF Core 9 and 10 (all bulk code is `#if !EF8`), MongoDB C# Driver LINQ v3, xUnit.

## Global Constraints

- All bulk code stays inside `#if !EF8`. EF8 must continue to reject bulk operations with EF's canonical "could not be translated" message.
- The solution builds and tests under three configurations: `Debug EF9`, `Debug EF10`, and `Debug EF8`. Every task must pass all three.
- `MongoEventId` members are append-only. Do not reorder, renumber, or remove existing members (`src/MongoDB.EntityFrameworkCore/Diagnostics/AGENTS.md`).
- Redaction is single-pointed: query/command content is only ever printed in full when `IDiagnosticsLogger.ShouldLogSensitiveData()` is true; otherwise emit `"?"`. Never format command content into a log path that bypasses that gate (`Diagnostics/AGENTS.md`).
- Respect the Query↔Storage bulk seam as `main` now defines it: Query builds a `MongoBulkPlan` carrying deferred translation delegates, Storage's `MongoBulkOperationExecutor` invokes them. Translation stays in Query, execution stays in Storage. Both `Query/AGENTS.md` and `Storage/AGENTS.md` document why the bulk seam differs from the read seam and say not to "fix" it — don't. Query still must not call `IMongoCollection<>.Aggregate(...)` directly.
- Top-level `Queryable` method dispatch matches canonical `QueryableMethods` constants, never bare `Method.Name` (`src/MongoDB.EntityFrameworkCore/Query/AGENTS.md`).
- Public API additions are additive only. `MongoBulkWriteEventData`'s existing constructors and properties must keep working.
- Test command used throughout: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "<filter>"`. Substitute `Debug EF9` / `Debug EF8` where a task says so.
- Functional and specification tests need a transaction-capable deployment (replica set / `atlas-local`). Unit tests must need no server.
- Commit after every task. One task, one commit, message prefixed `fix:`, `feat:`, `test:`, `perf:` or `docs:`.

## Phasing and merge gate

| Phase | Theme | Tasks | Gate |
|---|---|---|---|
| 1 | Observability foundation (unlocks assertions for everything after) | 1–6 | Tasks 1–3 are merge blockers |
| 2 | Data integrity — setters | 7–9 | **merge blocker** |
| 3 | Fail-closed correctness | 10–12 | **merge blocker** |
| 4 | Classification | 13–15 | **merge blocker** |
| 5 | Performance | 16–19 | Task 16 strongly recommended; 17–19 recommended |
| 6 | Coverage completion | 20–24 | **Task 20 is a merge blocker**; 21–24 recommended |
| 7 | Docs and cleanups | 25–26 | **Task 25 is a merge blocker** |

Task 20 is a merge blocker because a bulk operation that ignored a global query filter would destroy or expose data across tenants, and nothing currently verifies that it does not. Task 25 is a merge blocker because the README currently asserts an atomicity guarantee MongoDB does not provide.

---

## Reconciliation with `main` (checked 2026-07-30, `upstream/main` at 58e05a0)

This plan was written against the initial submission of PR #311 (`7753997`). The version that merged (`8c1247d`) carried further fixes, and later commits touched the area again. Everything below was verified against `upstream/main`, not inferred.

### Already fixed in `main` — drop these from the plan

| Plan item | What `main` does instead |
|---|---|
| Task 4, `TargetCount` is `int?` among `long` siblings | `MongoBulkWriteEventData.TargetCount` was **removed**. Bulk events now use dedicated `Diagnostics/MongoBulkDeleteEventData.cs` and `MongoBulkUpdateEventData.cs`, where `TargetCount` is already `long?`. |
| Task 4, all-zero counts make the payload a poor fit | Solved by the same split: the per-operation types carry only the counts that apply (`ModifyCount` + `TargetCount` for update, delete's equivalents for delete) instead of reusing the `SaveChanges` three-count shape. |
| Task 11 Step 5, stale `ValidateBulkSource` comment and silent predicate-dropping in `BuildFilter` | Already done. The comment is gone and `BuildFilter` now throws when a non-`Where` `Queryable` operator remains in the chain ("Fail loud rather than silently drop"). |
| Task 26, unused `nonQuery` parameter on `RunInBulkTransaction` | Already dropped — the signature is now `RunInBulkTransaction(QueryContext, Func<int>)`. |
| — (not in this plan) | `MongoEventId`'s four bulk ids are now correctly inside `#if !EF8`. |
| — (not in this plan) | New test `ExecuteUpdate_two_phase_auto_transaction_logs_transaction_lifecycle` covers the auto-started transaction's begin/commit events on both sync and async. |
| — (not in this plan) | `README.md` gained a bullet explaining that an already-open transaction is enlisted rather than nested. |
| — (not in this plan) | `docs/failing-spec-tests.md` was reconciled against EF-117: the simple `Join`/`LeftJoin`/`GroupJoin` bulk shapes became read-translatable and moved from EF-X016 to EF-X002. Task 25's doc edit must preserve that reconciliation rather than reverting it. |

### Structural change that re-targets most tasks

Execution was extracted out of the query visitor into Storage, which is the single biggest difference from the reviewed state:

- **`Storage/MongoBulkPlan.cs`** (new) — a DTO carrying `Kind`, `Strategy`, `CollectionName` and three deferred translation delegates: `Func<QueryContext, FilterDefinition<BsonDocument>>? BuildFilter`, `Func<QueryContext, UpdateDefinition<BsonDocument>>? BuildUpdate`, `Func<QueryContext, IQueryable<BsonDocument>>? BuildTargetIdQuery`.
- **`Storage/MongoBulkOperationExecutor.cs`** (new) — `Execute` / `ExecuteAsync` plus everything that used to live in the compiling visitor: the six executors, `DeleteByIds`/`UpdateByIds` (+async), `MaterializeIds`/`MaterializeIdsAsync`, `RunInBulkTransaction`(+async), `SafeRollback`(+async), `NoAutoTransactionError`, `StandaloneTransactionError`, `IsTransactionsUnsupported`, `GetBulkCollectionAndSession`, `GetUpdateLogger`.
- **Query keeps translation only** — `CreateBulkPlan<TSource>`, `TranslateBulkOrThrow`, `EnsureBulkKeyOrThrow`, `BuildFilter`, `BuildUpdate`, `BuildIdDocumentQuery`, `SerializeConstant`, `RenderSelfReferencingValue`, `EvaluateToConstant`, `CompileAndEvaluate`, `ParameterRebindingExpressionVisitor`.

Both `Query/AGENTS.md` and `Storage/AGENTS.md` now document this seam explicitly, and Query's note ends: *"Don't 'fix' the bulk seam to look like the read seam — they differ for this reason."* **Honour that.** Every task below that touches execution must land in `Storage/MongoBulkOperationExecutor.cs`, and every task that touches translation stays in `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`.

**File remap — apply to every task's `Files:` block:**

| Task body says | Read as |
|---|---|
| `MongoShapedQueryCompilingExpressionVisitor.cs` → `RunInBulkTransaction`, `SafeRollback*`, `IsTransactionsUnsupported`, `NoAutoTransactionError`, `StandaloneTransactionError`, `GetBulkCollectionAndSession`, `GetUpdateLogger`, the six `Execute*` methods, `*ByIds` | `Storage/MongoBulkOperationExecutor.cs` |
| `SelectTargetIds` / `SelectTargetIdsAsync` | `MaterializeIds` / `MaterializeIdsAsync` in `Storage/MongoBulkOperationExecutor.cs` |
| `MongoShapedQueryCompilingExpressionVisitor.cs` → `BuildFilter`, `BuildUpdate`, `BuildIdDocumentQuery`, `SerializeConstant`, `RenderSelfReferencingValue`, `EvaluateToConstant`, `TranslateBulkOrThrow`, `EnsureBulkKeyOrThrow` | unchanged — still `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` |
| `MongoBulkWriteEventData` (bulk events) | `Diagnostics/MongoBulkDeleteEventData.cs` / `MongoBulkUpdateEventData.cs` |

Consequences worth calling out per task:

- **Task 5** (command logging, phase-spanning stopwatch) — the stopwatch now lives in the executor's `*ByIds` methods, so the fix is the same but in Storage. `RenderBulkCommand` has to go where the `FilterDefinition`/`UpdateDefinition` are available: that is the executor, since `MongoBulkPlan` hands over already-rendered definitions. Phase 1's pipeline string, however, is only reachable from `BuildTargetIdQuery`'s result, which the executor also holds — so both belong in the executor.
- **Task 6** (logger via `MongoQueryContext`) — `GetUpdateLogger` still uses `Context.GetService<>()`, now at `MongoBulkOperationExecutor.cs:414`. The fix is unchanged.
- **Task 16** (`{_id: 1}` projection) — `BuildIdDocumentQuery` is still in Query, so the projection is appended there; only the `PhaseOneResult` threading moves to the executor. Simpler alternative given the new seam: have `MongoBulkPlan` expose the rendered phase-1 pipeline string alongside `BuildTargetIdQuery`.
- **Task 18** (`$in` batching) — belongs entirely in the executor's four `*ByIds` methods.
- **Task 11** (translate before transaction) — `MongoBulkPlan.BuildUpdate` is a deferred delegate the executor invokes, so "hoist `BuildUpdate` out of the transaction body" becomes "invoke `plan.BuildUpdate(queryContext)` before `RunInBulkTransaction`". Note `BuildFilter` is already `null` for two-phase plans, and `BuildTargetIdQuery` is **not** wrapped in `TranslateBulkOrThrow` — so the phase-1 wrapping half of Task 11 still stands, and the cleanest place for it is `CreateBulkPlan`'s delegate, not the executor.

### Still open — verified present in `main`

Every one of these was re-checked against `upstream/main`:

- **All of Phase 2 and Phase 4.** `Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` and `Query/Expressions/MongoNonQueryExpression.cs` are **byte-identical** to the reviewed state. `ClassifyBulkSource` still routes bare `OrderBy`/`Distinct` to two-phase and still matches on `Method.Name`; `BuildSetter` still has no key/discriminator/concurrency-token guard, still uses `FindProperty(string)`, still matches `"SetProperty"` by name and still permits duplicate setters; `UnwrapBulkOperator` still strips any `EntityFrameworkQueryableExtensions` call.
- **Task 3** — `IsTransactionsUnsupported` still matches `MongoCommandException { Code: 20 }`, and still has no test.
- **Task 4 (remaining parts)** — `MongoLoggingDefinitions` still declares `LogExecutingBulkDeleteTwoPhase` and `LogExecutingBulkUpdateTwoPhase`, so two definitions of different arity still share one `EventId`. `MongoBulkUpdateEventData` exposes `ModifyCount` and `TargetCount` but **no matched count**, and `ExecutedBulkUpdate(...)` is still called with `result.ModifiedCount` while the method returns `result.MatchedCount`.
- **Task 5** — no `ShouldLogSensitiveData` call anywhere in the bulk path; no command is logged. Two-phase stopwatches are still started inside `UpdateByIds`/`DeleteByIds`, after phase 1 has run.
- **Task 7** — `BuildUpdate` still keys `$set` with `setter.Property.GetElementName()` at three sites.
- **Task 8** — `ParameterRebindingExpressionVisitor` still rebinds by type with the precondition only in a comment.
- **Task 10** — `GetBulkCollectionAndSession` still uses `as MongoTransaction`, and `RunInBulkTransaction` still trusts a non-null `CurrentTransaction` without checking it is one.
- **Task 12** — `SafeRollbackAsync(IDbContextTransaction, CancellationToken)` still takes and forwards the caller's token; no `CancellationToken.None` anywhere.
- **Tasks 16–19** — no `$project` on the phase-1 pipeline; `MaterializeIdsAsync` still calls `ToListAsync`; no batching of the `$in`; `CompileAndEvaluate` and the `MakeGenericMethod`/`Invoke` renderer are unchanged.
- **Task 1** — `InvalidQueryTranslationTests.cs` does not exist on `main`; only `BulkOperationsUnsupportedOnEf8Tests.cs`, still `#if EF8` and still covering `ExecuteDelete` only. The `FakeQueryableExtensions` helper is still deleted.
- **Task 20** — no test anywhere combines `HasQueryFilter` with a bulk operation, and `FiltersInheritanceBulkUpdatesTestBase<>` is still on the compliance ignore list under the blanket justification.
- **Task 25** — `README.md` still says "a single atomic `deleteMany` / `updateMany` server command", and "atomic" still appears three times across the two new Storage files.

### Net effect on the plan

26 tasks → **24**. Task 26 shrinks to the unreachable-`break` and `VisitChildren` items. Task 4 loses two of its four sub-findings but keeps the `EventId` collision and the matched-count gap. Task 11 loses Step 5. Nothing else is removed, and no new finding was introduced by the merged fixes.

These phases are **not** independent subsystems, so this stays one plan: Phase 1 exists to make Phases 2–5 assertable, Phase 4 removes work that Phase 5 would otherwise have to optimise, and Phase 6's tests depend on guards added in Phases 2–4. Do not reorder phases.

## File Structure

**Production — modified**

| File | Responsibility after this plan |
|---|---|
| `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs` | Marker node. `UnwrapBulkOperator` narrowed to the two bulk operators. Gains a cache slot for compiled setter values. |
| `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` | Classification and setter parsing. `ClassifyBulkSource` splits ordering from paging and matches `QueryableMethods`. `BuildSetter` gains key/discriminator guards and `FindProperty(MemberInfo)`. |
| `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` | Translation only (post-refactor). Phase 1 projects `_id`, `$set` keys use the serialization path, the setter-rebinding precondition is enforced, the compiled-value fallback is cached, and `BuildTargetIdQuery`'s delegate is wrapped in `TranslateBulkOrThrow`. |
| `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` | Execution. Transaction handling fails closed, the update is translated ahead of transaction start, async streams the cursor, `$in` batches, elapsed spans both phases, the command is logged behind the redaction gate, and the logger comes from `MongoQueryContext`. |
| `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkPlan.cs` | May gain the rendered phase-1 pipeline string alongside `BuildTargetIdQuery` (see Task 16 in the reconciliation notes). |
| `src/MongoDB.EntityFrameworkCore/Query/MongoQueryContext.cs` | Gains `UpdateLogger` so executors stop using `Context.GetService<>()`. |
| `src/MongoDB.EntityFrameworkCore/Query/Factories/MongoQueryContextFactory.cs` | Supplies the update logger to `MongoQueryContext`. |
| `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkUpdateEventData.cs`, `MongoBulkDeleteEventData.cs` | Gain `MatchedCount` (update), `Command` and `LogSensitiveData`. `TargetCount` is already `long?` on `main`. |
| `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerUpdateExtensions.cs` | Bulk event definitions carry the redaction-gated command string and both counts. |
| `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggingDefinitions.cs` | Definition fields for the reshaped bulk events. |
| `README.md` | Corrected atomicity wording; nested-setter and key/discriminator limitations stated. |
| `docs/failing-spec-tests.md` | `EF-X016` updated for the narrowed two-phase set and the newly supported `OfType`. |

**Tests — created**

| File | Responsibility |
|---|---|
| `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkTransactionErrorClassificationTests.cs` | Server-free unit tests for `IsTransactionsUnsupported`. |
| `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs` | Server-free unit tests for `UnwrapBulkOperator`. |
| `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsQueryFilterTests.cs` | `HasQueryFilter` × bulk (soft-delete / multi-tenant). |
| `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsInheritanceTests.cs` | TPH × bulk, including `OfType`. |
| `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs` | Asserts the rendered filter / update document via the new event. |
| `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs` | Bulk × CSFLE / Queryable Encryption. |

**Tests — modified**

| File | Change |
|---|---|
| `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/InvalidQueryTranslationTests.cs` | Restored (un-deleted) — foreign-queryable-extension regression test, all EF versions. |
| `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsUnsupportedOnEf8Tests.cs` | Adds the `ExecuteUpdate` EF8 case. |
| `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs` | `[Theory]`-ified for async; new shape/guard/cancellation tests. |
| `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` | `[Theory]`-ified for async; new setter guard and shape tests. |
| `tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoComplianceTest.cs` | Per-suite exclusion reasons; `FiltersInheritanceBulkUpdatesTestBase` implemented. |
| `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindBulkUpdatesMongoTest.cs` | `OfType`/ordering reclassifications; MQL baselines. |

---

## Phase 1 — Observability foundation

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

### Task 2: Unit-test and narrow `UnwrapBulkOperator`

`UnwrapBulkOperator` strips *any* outermost method declared on `EntityFrameworkQueryableExtensions`, not just the two bulk operators. It is the only method on `MongoNonQueryExpression` with logic and the only one without a test.

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `MongoNonQueryExpression.UnwrapBulkOperator(Expression?)` keeps its existing signature `static Expression? UnwrapBulkOperator(Expression? capturedExpression)`. Behaviour narrows: it now only unwraps calls named `ExecuteDelete`, `ExecuteDeleteAsync`, `ExecuteUpdate` or `ExecuteUpdateAsync` declared on `EntityFrameworkQueryableExtensions`.

- [ ] **Step 1: Write the failing tests**

Create `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs`:

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

#if !EF8

using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions;

public static class MongoNonQueryExpressionUnwrapTests
{
    private class Product
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; } = null!;
    }

    private static IQueryable<Product> Source => new Product[0].AsQueryable();

    [Fact]
    public static void Unwraps_ExecuteDelete_returning_the_source_chain()
    {
        var chain = Source.Where(p => p.Name == "x");
        var captured = Expression.Call(
            typeof(EntityFrameworkQueryableExtensions)
                .GetMethod(nameof(EntityFrameworkQueryableExtensions.ExecuteDelete))!
                .MakeGenericMethod(typeof(Product)),
            chain.Expression);

        var actual = MongoNonQueryExpression.UnwrapBulkOperator(captured);

        Assert.Same(chain.Expression, actual);
    }

    [Fact]
    public static void Returns_expression_unchanged_when_not_a_bulk_operator()
    {
        var chain = Source.Where(p => p.Name == "x");

        var actual = MongoNonQueryExpression.UnwrapBulkOperator(chain.Expression);

        Assert.Same(chain.Expression, actual);
    }

    [Fact]
    public static void Does_not_unwrap_a_non_bulk_EntityFrameworkQueryableExtensions_call()
    {
        // AsNoTracking is declared on EntityFrameworkQueryableExtensions but is not a bulk operator.
        // Unwrapping it would silently discard it from the chain the classifier walks.
        var chain = Source.Where(p => p.Name == "x");
        var captured = Expression.Call(
            typeof(EntityFrameworkQueryableExtensions)
                .GetMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTracking), new[] { typeof(IQueryable<Product>) })!,
            chain.Expression);

        var actual = MongoNonQueryExpression.UnwrapBulkOperator(captured);

        Assert.Same(captured, actual);
    }

    [Fact]
    public static void Returns_null_for_null()
        => Assert.Null(MongoNonQueryExpression.UnwrapBulkOperator(null));
}

#endif
```

- [ ] **Step 2: Run the tests to verify the third one fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~MongoNonQueryExpressionUnwrapTests"
```

Expected: `Does_not_unwrap_a_non_bulk_EntityFrameworkQueryableExtensions_call` FAILS (`Assert.Same` — the current implementation unwraps it and returns `chain.Expression`). The other three PASS.

If `AsNoTracking`'s reflection lookup throws because the overload set differs on your EF version, substitute `nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters)` with `new[] { typeof(IQueryable<Product>) }`.

- [ ] **Step 3: Narrow the implementation**

In `MongoNonQueryExpression.cs`, replace the `UnwrapBulkOperator` body:

```csharp
    private static readonly string[] BulkOperatorNames =
    [
        "ExecuteDelete", "ExecuteDeleteAsync", "ExecuteUpdate", "ExecuteUpdateAsync"
    ];

    /// <summary>
    /// Unwraps the leading ExecuteDelete / ExecuteUpdate marker call (declared on
    /// <see cref="EntityFrameworkQueryableExtensions"/>) from a captured method chain, returning the underlying
    /// source query (e.g. <c>root.Where(...)</c>). EF captures the full chain including the bulk operator as the
    /// <see cref="MongoQueryExpression.CapturedExpression"/>; walkers that scope the operation need only the source.
    /// Only the four bulk operators are unwrapped — other methods on that type (AsNoTracking,
    /// IgnoreQueryFilters, TagWith, ...) must stay in the chain so the classifier can see and reject them
    /// rather than having them silently discarded.
    /// </summary>
    public static Expression? UnwrapBulkOperator(Expression? capturedExpression)
        => capturedExpression is MethodCallExpression { Method: { DeclaringType: var declaringType, Name: var name } } call
           && declaringType == typeof(EntityFrameworkQueryableExtensions)
           && Array.IndexOf(BulkOperatorNames, name) >= 0
            ? call.Arguments[0]
            : capturedExpression;
```

Add `using System;` to the file's using block if it is not already present.

- [ ] **Step 4: Run the tests to verify all pass**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~MongoNonQueryExpressionUnwrapTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~MongoNonQueryExpression"
```

Expected: PASS, 4 tests on EF10; the pre-existing `MongoNonQueryExpressionTests` still pass on EF9.

- [ ] **Step 5: Run the whole bulk suite to confirm no regression**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS, no change in counts.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs
git commit -m "fix: unwrap only the four bulk operators, not every EntityFrameworkQueryableExtensions call"
```

---

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

### Task 4: Reshape the bulk event payload and collapse the duplicate event definitions

> ### AMENDED FOR `main` (2026-07-30) — read this before the steps below
>
> Two of this task's four original findings are **already fixed** in `main` and are dropped: `TargetCount` is already `long?`, and the all-zero-counts payload mismatch was solved by splitting `MongoBulkWriteEventData` into per-operation types. The step bodies below were written against the old shared `MongoBulkWriteEventData` and **do not match the code**. Use the step bodies for their *intent and message wording*; use this block for the actual types and signatures.
>
> **Actual state in `main`:**
> - `Diagnostics/MongoBulkDeleteEventData.cs` — `MongoBulkDeleteEventData(EventDefinitionBase, Func<EventDefinitionBase, EventData, string>, TimeSpan elapsed, CollectionNamespace collectionNamespace, long deleteCount, long? targetCount)`, properties `Elapsed`, `CollectionNamespace`, `DeleteCount`, `TargetCount`.
> - `Diagnostics/MongoBulkUpdateEventData.cs` — same shape with `long modifyCount` / `ModifyCount`.
> - `Diagnostics/MongoLoggerUpdateExtensions.cs` aliases: `BulkExecutingEventDefinition = EventDefinition<string, CollectionNamespace>`, `BulkExecutingTwoPhaseEventDefinition = EventDefinition<string, CollectionNamespace, long>`, `BulkExecutedEventDefinition = EventDefinition<string, CollectionNamespace, long>`.
> - `Diagnostics/MongoLoggingDefinitions.cs` still declares six bulk fields including `LogExecutingBulkDeleteTwoPhase` and `LogExecutingBulkUpdateTwoPhase`.
> - Call sites are in `Storage/MongoBulkOperationExecutor.cs`, not the query visitor.
>
> **Remaining scope — exactly two changes:**
>
> **(A) One `EventDefinition` per `EventId`.** Today `LogExecutingBulkDelete` and `LogExecutingBulkDeleteTwoPhase` are two definitions of different arity registered under the single id `MongoEventId.ExecutingBulkDelete` (same for update), so a `DiagnosticSource` consumer cannot tell the shapes apart and has to null-check `TargetCount` to find out which it received. Collapse each pair into one definition:
> - Replace the two `Executing` aliases with one: `BulkExecutingEventDefinition = EventDefinition<string, CollectionNamespace, string, string>` — elapsed, namespace, strategy, target count.
> - Delete `LogExecutingBulkDeleteTwoPhase` and `LogExecutingBulkUpdateTwoPhase` from `MongoLoggingDefinitions.cs`, leaving four bulk fields.
> - Delete `ExecutingBulkDeleteTwoPhaseMessage` and `ExecutingBulkUpdateTwoPhaseMessage`; the single message generator handles both.
> - Message text: `"Executing Bulk Delete ({elapsed} ms) Collection='{collectionNamespace}', Strategy={strategy}, Targets={targetCount}"` (and the `Update` equivalent).
> - `strategy` is `"SingleCommand"` or `"TwoPhase"`, from `plan.Strategy.ToString()`. `targetCount` renders as the number for two-phase and `"-"` for single-command, via a shared `private static string FormatTargetCount(long? targetCount) => targetCount?.ToString(CultureInfo.InvariantCulture) ?? "-";`.
> - Public signatures become `ExecutingBulkDelete(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, string strategy, long? targetCount)` and the `Update` equivalent — one method each, no overload pair.
>
> **(B) Carry the matched count on update.** `ExecutedBulkUpdate` is called with `result.ModifiedCount` while the executor returns `result.MatchedCount`, so the event cannot be reconciled with the value `ExecuteUpdate` gives the caller.
> - Add `long matchedCount` to `MongoBulkUpdateEventData`'s constructor (after `modifyCount`) and a `public long MatchedCount { get; }` property, documented as "The number of documents matched — the value returned to the caller; `ModifyCount` is the genuinely-modified subset."
> - Add alias `BulkExecutedUpdateEventDefinition = EventDefinition<string, CollectionNamespace, long, long>`; `LogExecutedBulkUpdate` uses it with message `"Executed Bulk Update ({elapsed} ms) Collection='{collectionNamespace}', Matched={matched}, Modified={modified}"`.
> - Signature becomes `ExecutedBulkUpdate(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, long matchedCount, long modifyCount)`.
> - `ExecutedBulkDelete` and `MongoBulkDeleteEventData` keep their arity — a delete has no matched-vs-modified distinction.
>
> **Not in this task:** `Command` and `LogSensitiveData` on the event data, and the phase-spanning stopwatch. Those are Task 5.
>
> **Also apply:** replace `duration.TotalMilliseconds.ToString()` with `ToString(CultureInfo.InvariantCulture)` at every bulk call site you touch, adding `using System.Globalization;` if absent. Leave the pre-existing non-bulk `ExecutingBulkWrite`/`ExecutedBulkWrite` alone.

Two findings in one change, because they touch the same public types and the same call sites and cannot land independently without churning them twice: `ExecutedBulkUpdate` reports only `ModifiedCount` so the event cannot be reconciled with the value `ExecuteUpdate` returns; and two `EventDefinition`s of different arity share one `EventId`, so a `DiagnosticSource` consumer cannot tell the two message shapes apart.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkWriteEventData.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggingDefinitions.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Diagnostics/MongoLoggerUpdateExtensions.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces, and relied on by Tasks 5, 6, 22 and 24:
  - `MongoBulkWriteEventData` gains `long? TargetCount` (was `int?`), `long MatchedCount`, `string? Command`, `bool LogSensitiveData`.
  - New constructor:
    `MongoBulkWriteEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, TimeSpan elapsed, CollectionNamespace collectionNamespace, long insertCount, long deleteCount, long modifyCount, long matchedCount, long? targetCount, string? command, bool logSensitiveData)`
  - Logger extension signatures become:
    - `ExecutingBulkDelete(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, string strategy, long? targetCount, string command)`
    - `ExecutedBulkDelete(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, long deleteCount)`
    - `ExecutingBulkUpdate(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, string strategy, long? targetCount, string command)`
    - `ExecutedBulkUpdate(this IDiagnosticsLogger<DbLoggerCategory.Update>, TimeSpan duration, CollectionNamespace collectionNamespace, long matchedCount, long modifyCount)`
  - `MongoLoggingDefinitions` keeps exactly one field per bulk event: `LogExecutingBulkDelete`, `LogExecutedBulkDelete`, `LogExecutingBulkUpdate`, `LogExecutedBulkUpdate`. The `*TwoPhase` fields are deleted.
  - The `command` argument is supplied already-redacted by the caller in Task 5. In this task callers pass `"?"`.
  - Strategy strings are exactly `"SingleCommand"` and `"TwoPhase"`, produced by `nonQuery.Strategy.ToString()`.

- [ ] **Step 1: Write the failing test that pins matched-vs-modified in the event**

In `ExecuteUpdateTests.cs`, add:

```csharp
    [Fact]
    public void ExecuteUpdate_event_reports_both_matched_and_modified_counts()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(
            nameof(ExecuteUpdate_event_reports_both_matched_and_modified_counts));
        collection.InsertMany(
        [
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 },
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 }
        ]);

        using var db = SingleEntityDbContext.Create(collection, loggerFactory: loggerFactory);

        // Both documents match; only the one whose Status differs is actually modified.
        var updated = db.Entities.Where(o => o.Status == "open").ExecuteUpdate(s => s.SetProperty(o => o.Status, "open"));

        Assert.Equal(2, updated);

        var executed = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedBulkUpdate);
        Assert.Contains("Matched=2", executed);
        Assert.Contains("Modified=0", executed);
    }
```

Match the `SingleEntityDbContext.Create(...)` overload already used by `ExecuteUpdate_logs_ExecutedBulkUpdate_event_with_count` in this file — copy its argument list rather than inventing one.

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdate_event_reports_both_matched_and_modified_counts"
```

Expected: FAIL — the current message is `"… Modified={modified}"` with no `Matched=` segment.

- [ ] **Step 3: Widen the event data**

In `MongoBulkWriteEventData.cs`, keep both existing constructors and have them chain into a new one. Replace the second constructor and the `TargetCount` property with:

```csharp
    /// <summary>
    /// Constructs the event payload for a bulk operation, including two-phase and command detail.
    /// </summary>
    /// <param name="eventDefinition">The event definition.</param>
    /// <param name="messageGenerator">A delegate that generates a log message for this event.</param>
    /// <param name="elapsed">The time elapsed since the operation began, spanning every phase.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> being operated upon.</param>
    /// <param name="insertCount">The number of documents to insert in this bulk write operation.</param>
    /// <param name="deleteCount">The number of documents deleted by this bulk write operation.</param>
    /// <param name="modifyCount">The number of documents whose stored values actually changed.</param>
    /// <param name="matchedCount">
    /// The number of documents matched by the operation's filter. This is the value
    /// <c>ExecuteUpdate</c> returns; <paramref name="modifyCount"/> is the genuinely-modified subset.
    /// </param>
    /// <param name="targetCount">
    /// For a two-phase bulk delete/update — the number of documents identified in phase one to be acted on
    /// by <c>_id</c>; <see langword="null" /> for single-command bulk writes.
    /// </param>
    /// <param name="command">
    /// The rendered MongoDB command (filter, and update document for an update), or <c>"?"</c> when
    /// sensitive-data logging is disabled.
    /// </param>
    /// <param name="logSensitiveData">Indicates whether the application allows logging of sensitive data.</param>
    public MongoBulkWriteEventData(
        EventDefinitionBase eventDefinition,
        Func<EventDefinitionBase, EventData, string> messageGenerator,
        TimeSpan elapsed,
        CollectionNamespace collectionNamespace,
        long insertCount,
        long deleteCount,
        long modifyCount,
        long matchedCount,
        long? targetCount,
        string? command,
        bool logSensitiveData)
        : base(eventDefinition, messageGenerator)
    {
        Elapsed = elapsed;
        CollectionNamespace = collectionNamespace;
        InsertCount = insertCount;
        DeleteCount = deleteCount;
        ModifyCount = modifyCount;
        MatchedCount = matchedCount;
        TargetCount = targetCount;
        Command = command;
        LogSensitiveData = logSensitiveData;
    }

    /// <summary>
    /// The number of documents matched by this operation's filter. For a bulk update this is the value
    /// returned to the caller; <see cref="ModifyCount"/> is the subset whose stored values changed.
    /// </summary>
    public long MatchedCount { get; }

    /// <summary>
    /// For a two-phase bulk delete/update — the number of documents identified in phase one to be acted on
    /// by <c>_id</c>; <see langword="null" /> for single-command bulk writes.
    /// </summary>
    public long? TargetCount { get; }

    /// <summary>
    /// The rendered MongoDB command for this operation, or <c>"?"</c> when sensitive-data logging is disabled.
    /// </summary>
    public string? Command { get; }

    /// <summary>
    /// Indicates whether the application allows logging of sensitive data.
    /// </summary>
    public bool LogSensitiveData { get; }
```

Update both pre-existing constructors to chain with the new trailing arguments — the three-count `SaveChanges` constructor becomes:

```csharp
        : this(eventDefinition, messageGenerator, elapsed, collectionNamespace, insertCount, deleteCount, modifyCount,
            matchedCount: 0, targetCount: null, command: null, logSensitiveData: false)
```

and delete the intermediate `int? targetCount` constructor added by the original PR — nothing outside this assembly can be depending on it yet.

- [ ] **Step 4: Collapse to one definition per event and add the new message fields**

In `MongoLoggingDefinitions.cs`, delete `LogExecutingBulkDeleteTwoPhase` and `LogExecutingBulkUpdateTwoPhase`, leaving four fields.

In `MongoLoggerUpdateExtensions.cs`, replace the three `using` aliases added by the PR with two:

```csharp
#if !EF8
using BulkExecutingEventDefinition = EventDefinition<string, CollectionNamespace, string, string, string>;
using BulkExecutedDeleteEventDefinition = EventDefinition<string, CollectionNamespace, long>;
using BulkExecutedUpdateEventDefinition = EventDefinition<string, CollectionNamespace, long, long>;
#endif
```

Replace `ExecutingBulkDelete` and its two definition factories and two message generators with a single set:

```csharp
    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutingBulkDelete" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The time elapsed since the operation began, spanning every phase.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="strategy">Either <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="targetCount">
    /// For a two-phase operation, the number of ids collected in phase one; <see langword="null"/> otherwise.
    /// </param>
    /// <param name="command">The rendered command, already redacted to <c>"?"</c> when not permitted.</param>
    public static void ExecutingBulkDelete(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        string strategy,
        long? targetCount,
        string command)
    {
        var definition = LogExecutingBulkDelete(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics, duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture), collectionNamespace,
                strategy, FormatTargetCount(targetCount), command);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkWriteEventData(
                definition,
                ExecutingBulkDeleteMessage,
                duration,
                collectionNamespace,
                insertCount: 0,
                deleteCount: 0,
                modifyCount: 0,
                matchedCount: 0,
                targetCount: targetCount,
                command: command,
                logSensitiveData: diagnostics.ShouldLogSensitiveData());

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string FormatTargetCount(long? targetCount)
        => targetCount?.ToString(CultureInfo.InvariantCulture) ?? "-";

    private static string ExecutingBulkDeleteMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkExecutingEventDefinition)definition;
        var p = (MongoBulkWriteEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture), p.CollectionNamespace,
            p.TargetCount.HasValue ? "TwoPhase" : "SingleCommand", FormatTargetCount(p.TargetCount),
            p.Command ?? "?");
    }

    private static BulkExecutingEventDefinition LogExecutingBulkDelete(IDiagnosticsLogger logger)
        => (BulkExecutingEventDefinition)
            (((MongoLoggingDefinitions)logger.Definitions).LogExecutingBulkDelete ?? NonCapturingLazyInitializer.EnsureInitialized(
                ref ((MongoLoggingDefinitions)logger.Definitions).LogExecutingBulkDelete,
                logger,
                static logger => new BulkExecutingEventDefinition(
                    logger.Options,
                    MongoEventId.ExecutingBulkDelete,
                    LogLevel.Information,
                    "MongoEventId.ExecutingBulkDelete",
                    level => LoggerMessage.Define<string, CollectionNamespace, string, string, string>(
                        level,
                        MongoEventId.ExecutingBulkDelete,
                        "Executing Bulk Delete ({elapsed} ms) Collection='{collectionNamespace}', "
                        + "Strategy={strategy}, Targets={targetCount}, Command={command}"))));
```

Apply the same shape to `ExecutingBulkUpdate` (message text `"Executing Bulk Update …"`, definition field `LogExecutingBulkUpdate`, event id `MongoEventId.ExecutingBulkUpdate`, message generator `ExecutingBulkUpdateMessage`).

Change `ExecutedBulkUpdate` to carry both counts:

```csharp
    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutedBulkUpdate" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The time elapsed since the operation began, spanning every phase.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="matchedCount">The number of documents matched — the value returned to the caller.</param>
    /// <param name="modifyCount">The number of documents whose stored values actually changed.</param>
    public static void ExecutedBulkUpdate(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        long matchedCount,
        long modifyCount)
    {
        var definition = LogExecutedBulkUpdate(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics, duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture), collectionNamespace,
                matchedCount, modifyCount);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkWriteEventData(
                definition,
                ExecutedBulkUpdateMessage,
                duration,
                collectionNamespace,
                insertCount: 0,
                deleteCount: 0,
                modifyCount: modifyCount,
                matchedCount: matchedCount,
                targetCount: null,
                command: null,
                logSensitiveData: diagnostics.ShouldLogSensitiveData());

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string ExecutedBulkUpdateMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkExecutedUpdateEventDefinition)definition;
        var p = (MongoBulkWriteEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture), p.CollectionNamespace,
            p.MatchedCount, p.ModifyCount);
    }
```

with the definition's message text `"Executed Bulk Update ({elapsed} ms) Collection='{collectionNamespace}', Matched={matched}, Modified={modified}"` and `LoggerMessage.Define<string, CollectionNamespace, long, long>`.

`ExecutedBulkDelete` keeps its arity; only change `duration.TotalMilliseconds.ToString()` to `ToString(CultureInfo.InvariantCulture)` and pass `matchedCount: deleteCount` into its event data.

Add `using System.Globalization;` to the file.

- [ ] **Step 5: Update the six call sites**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, every `ExecutingBulk*` call gains the strategy, target count and a placeholder command (Task 5 replaces the placeholder with the real one), and every `ExecutedBulkUpdate` call gains the matched count. For example, in `UpdateByIds`:

```csharp
        updateLogger.ExecutingBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, nonQuery.Strategy.ToString(), ids.Count, "?");

        var result = session == null
            ? collection.UpdateMany(filter, update)
            : collection.UpdateMany(session, filter, update);

        updateLogger.ExecutedBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, result.MatchedCount, result.ModifiedCount);
```

and in the single-command `ExecuteUpdate`:

```csharp
        updateLogger.ExecutingBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, nonQuery.Strategy.ToString(), targetCount: null, "?");
```

Apply the equivalent edits to `UpdateByIdsAsync`, `DeleteByIds`, `DeleteByIdsAsync`, `ExecuteDelete`, `ExecuteDeleteAsync`, `ExecuteUpdate` and `ExecuteUpdateAsync`.

- [ ] **Step 6: Fix the existing diagnostics tests' expectations**

The pre-existing tests assert on message substrings that have changed shape. Update `ExecuteDelete_two_phase_logs_ExecutingBulkDelete_with_target_count` and `ExecuteUpdate_two_phase_logs_ExecutingBulkUpdate_with_target_count` to assert `Assert.Contains("Strategy=TwoPhase", executingMessage)` and `Assert.Contains("Targets=2", executingMessage)` (substituting the target count the test seeds), and update `ExecuteUpdate_logs_ExecutedBulkUpdate_event_with_count` to expect `Matched=` alongside `Modified=`.

- [ ] **Step 7: Run the bulk suites on EF9 and EF10**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```

Expected: PASS on EF9/EF10 including the new matched/modified test; EF8 builds.

- [ ] **Step 8: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Diagnostics/ \
        src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/
git commit -m "feat: report matched and modified counts, widen TargetCount to long?, one definition per bulk event"
```

---

### Task 5: Emit the redaction-gated command, and make elapsed span both phases

> ### AMENDED FOR `main` (2026-07-30) — read this before the steps below
>
> The findings are unchanged and both still present, but the code moved. The step bodies below place everything in `MongoShapedQueryCompilingExpressionVisitor`; **use these locations instead:**
>
> - **`RenderBulkCommand` and `CombineBulkCommand` go in `Storage/MongoBulkOperationExecutor.cs`.** That is where the rendered `FilterDefinition<BsonDocument>` and `UpdateDefinition<BsonDocument>` exist — `MongoBulkPlan` hands them over as the results of its `BuildFilter`/`BuildUpdate` delegates. Do not move rendering into Query.
> - **The stopwatch fix is in the executor.** `ExecuteTwoPhaseDelete`, `ExecuteTwoPhaseDeleteAsync`, `ExecuteTwoPhaseUpdate` and `ExecuteTwoPhaseUpdateAsync` currently let `DeleteByIds`/`UpdateByIds` (+async) start their own `Stopwatch`, so elapsed covers only phase 2. Start it in the four `ExecuteTwoPhase*` methods before the phase-1 call and pass it into the `*ByIds` methods, exactly as the step body describes — just in the executor's methods.
> - **`SelectTargetIds`/`SelectTargetIdsAsync` are named `MaterializeIds`/`MaterializeIdsAsync`** in the executor, and they take the already-built `IQueryable<BsonDocument>` rather than building it. Phase 1's query comes from `plan.BuildTargetIdQuery(queryContext)`, invoked in the `ExecuteTwoPhase*` methods.
> - **Event data:** add `string? Command` and `bool LogSensitiveData` to **both** `MongoBulkDeleteEventData` and `MongoBulkUpdateEventData` (Task 4 already added `MatchedCount` to the update one). Both get a new trailing constructor parameter pair; document `Command` as "The rendered MongoDB command for this operation, or `\"?\"` when sensitive-data logging is disabled."
> - **Definition arity:** Task 4 collapsed the `Executing` definitions to `EventDefinition<string, CollectionNamespace, string, string>` (elapsed, namespace, strategy, targets). Extend that to `EventDefinition<string, CollectionNamespace, string, string, string>` with `command` last, and append `, Command={command}` to both `Executing` message templates. The `Executed` definitions do not carry the command.
> - **`SingleEntityDbContext.Create`:** check its real overloads before writing the tests. Earlier tasks in this plan found the argument order is `(collection, loggerFactory, configureModel)` in some call sites — copy a working call from `ExecuteUpdateTests.cs` rather than trusting the step body's `sensitiveDataLogging:` named argument, and add the parameter only if it genuinely does not exist.
>
> Everything else in this task — the three-plus-one tests, the redaction gate, the `"?"` default — is unchanged.

The bulk events record namespace, elapsed and counts but never the rendered filter or update document, so nothing can assert what command was produced — the conformance suite's own comment cites this as the reason it has no MQL baseline. And the two-phase stopwatch is created inside `UpdateByIds`/`DeleteByIds`, *after* phase 1 has finished, so the reported duration omits the expensive half.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs`

**Interfaces:**
- Consumes: the logger signatures from Task 4.
- Produces, relied on by Tasks 8, 14, 15, 17 and 21:
  - `private static string RenderBulkCommand(IDiagnosticsLogger<DbLoggerCategory.Update> logger, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument>? update)` — returns `"?"` unless `logger.ShouldLogSensitiveData()`, else `{ "filter": <filter>, "update": <update> }` as a single-line JSON string with `update` omitted for deletes.
  - Both two-phase executors create their `Stopwatch` before phase 1 and pass it down, so `Executing`/`Executed` elapsed covers phase 1 + phase 2.

- [ ] **Step 1: Write the failing test**

Create `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs` with the standard licence header, then:

```csharp
// Pins the command the bulk path actually sends. Counts and before/after state cannot distinguish a
// correct $set from one written to the wrong element, which is what these assertions are for.
// Command content is sensitive, so it only appears when EnableSensitiveDataLogging is on — these tests
// opt in explicitly and one asserts the redacted default.

#if !EF8

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

[XUnitCollection("QueryTests")]
public class BulkOperationsCommandTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!;
        public int Quantity { get; set; }
    }

    [Fact]
    public void Single_command_delete_logs_its_filter_when_sensitive_logging_enabled()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(
            nameof(Single_command_delete_logs_its_filter_when_sensitive_logging_enabled));
        collection.InsertOne(new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 });

        using var db = SingleEntityDbContext.Create(
            collection, loggerFactory: loggerFactory, sensitiveDataLogging: true);

        db.Entities.Where(o => o.Status == "open").ExecuteDelete();

        var executing = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete);
        Assert.Contains("Strategy=SingleCommand", executing);
        Assert.Contains("\"Status\" : \"open\"", executing);
    }

    [Fact]
    public void Command_is_redacted_when_sensitive_logging_disabled()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(nameof(Command_is_redacted_when_sensitive_logging_disabled));
        collection.InsertOne(new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 });

        using var db = SingleEntityDbContext.Create(collection, loggerFactory: loggerFactory);

        db.Entities.Where(o => o.Status == "open").ExecuteDelete();

        var executing = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete);
        Assert.Contains("Command=?", executing);
        Assert.DoesNotContain("\"Status\"", executing);
    }

    [Fact]
    public void Update_logs_its_set_document()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(nameof(Update_logs_its_set_document));
        collection.InsertOne(new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 });

        using var db = SingleEntityDbContext.Create(
            collection, loggerFactory: loggerFactory, sensitiveDataLogging: true);

        db.Entities.Where(o => o.Status == "open").ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));

        var executing = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkUpdate);
        Assert.Contains("$set", executing);
        Assert.Contains("\"Status\" : \"shipped\"", executing);
    }
}

#endif
```

If `SingleEntityDbContext.Create` has no `sensitiveDataLogging` parameter, add one that calls `optionsBuilder.EnableSensitiveDataLogging()` — follow the existing parameter style in `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/SingleEntityDbContext.cs` and default it to `false` so no existing caller changes.

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsCommandTests"
```

Expected: the two sensitive-logging tests FAIL (no filter or `$set` text in the message); `Command_is_redacted_when_sensitive_logging_disabled` PASSES already because Task 4 hardcoded `"?"`.

- [ ] **Step 3: Add the renderer**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, next to `GetUpdateLogger`:

```csharp
    // Renders the command for diagnostics. Command content includes predicate values and the values being
    // written, so it is gated on ShouldLogSensitiveData() exactly like MQL is on the read path — see
    // Diagnostics/AGENTS.md ("Redaction is single-pointed").
    private static string RenderBulkCommand(
        IDiagnosticsLogger<DbLoggerCategory.Update> logger,
        FilterDefinition<BsonDocument> filter,
        UpdateDefinition<BsonDocument>? update)
    {
        if (!logger.ShouldLogSensitiveData())
        {
            return "?";
        }

        var registry = BsonSerializer.SerializerRegistry;
        var documentSerializer = registry.GetSerializer<BsonDocument>();
        var rendered = new BsonDocument("filter", filter.Render(new RenderArgs<BsonDocument>(documentSerializer, registry)));

        if (update != null)
        {
            rendered.Add("update", update.Render(new RenderArgs<BsonDocument>(documentSerializer, registry)));
        }

        return rendered.ToJson();
    }
```

If `UpdateDefinition<BsonDocument>.Render` returns a type other than `BsonValue` on the pinned driver (pipeline-form updates render as a `BsonArray`), assign it to `BsonValue` via `rendered.Add("update", (BsonValue)update.Render(...))`; if the overload set differs, use the same `RenderArgs` overload that `BuildFilter` already uses in this file.

- [ ] **Step 4: Pass the command and a phase-spanning stopwatch through**

Change the four `*ByIds` methods to accept the stopwatch and the command from their caller rather than starting their own. `UpdateByIds` becomes:

```csharp
    private static int UpdateByIds(
        QueryContext queryContext, MongoNonQueryExpression nonQuery, List<BsonValue> ids,
        UpdateDefinition<BsonDocument> update, Stopwatch stopwatch)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, nonQuery);
        var filter = Builders<BsonDocument>.Filter.In("_id", ids);

        var updateLogger = GetUpdateLogger(queryContext);
        updateLogger.ExecutingBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, nonQuery.Strategy.ToString(), ids.Count,
            RenderBulkCommand(updateLogger, filter, update));

        var result = session == null
            ? collection.UpdateMany(filter, update)
            : collection.UpdateMany(session, filter, update);

        updateLogger.ExecutedBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, result.MatchedCount, result.ModifiedCount);
        return checked((int)result.MatchedCount);
    }
```

and its caller starts the clock before phase 1:

```csharp
    private static int ExecuteTwoPhaseUpdate<TSource>(
        QueryContext queryContext, IReadOnlyEntityType entityType, BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
        => RunInBulkTransaction(queryContext, nonQuery, () =>
        {
            // The stopwatch spans phase 1 (the read that harvests target ids) as well as phase 2, because
            // phase 1 is normally the expensive half.
            var stopwatch = Stopwatch.StartNew();
            var ids = SelectTargetIds<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery);
            if (ids.Count == 0)
            {
                return 0;
            }

            var update = TranslateBulkOrThrow(nonQuery,
                () => BuildUpdate<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery));
            return UpdateByIds(queryContext, nonQuery, ids, update, stopwatch);
        });
```

Apply the same change to `ExecuteTwoPhaseUpdateAsync`/`UpdateByIdsAsync`, `ExecuteTwoPhaseDelete`/`DeleteByIds` and `ExecuteTwoPhaseDeleteAsync`/`DeleteByIdsAsync` (deletes pass `update: null` to `RenderBulkCommand`).

In the four single-command executors, replace the `"?"` placeholder from Task 4 with `RenderBulkCommand(updateLogger, filter, update)` (or `null` for the update argument in the delete executors).

- [ ] **Step 5: Run the new and existing tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsCommandTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~BulkOperationsCommandTests"
```

Expected: PASS. If an assertion fails only on BSON spacing (`"Status" : "open"` vs `"Status": "open"`), relax that assertion to `Assert.Contains("Status", executing)` plus `Assert.Contains("open", executing)` rather than guessing at `ToJson` formatting.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/
git commit -m "feat: log the bulk command behind the sensitive-data gate; time both two-phase phases"
```

---

### Task 6: Resolve the update logger once via `MongoQueryContext`

`GetUpdateLogger` calls `queryContext.Context.GetService<IDiagnosticsLogger<DbLoggerCategory.Update>>()` inside a compiled-query executor. That is a service-locator lookup on the execution path and it bypasses the plumbing the read path uses (`QueryContext.QueryLogger`, supplied by the context factory).

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/MongoQueryContext.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Factories/MongoQueryContextFactory.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `MongoQueryContext.UpdateLogger` of type `IDiagnosticsLogger<DbLoggerCategory.Update>`, set from the constructor. `GetUpdateLogger(QueryContext queryContext)` keeps its signature and returns `((MongoQueryContext)queryContext).UpdateLogger`.

- [ ] **Step 1: Add the property**

In `MongoQueryContext.cs`, extend the constructor and add the property:

```csharp
    /// <param name="updateLogger">The <see cref="DbLoggerCategory.Update"/> logger used by bulk operations.</param>
    public MongoQueryContext(
        QueryContextDependencies dependencies,
        IMongoClientWrapper mongoClient,
        IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger)
        : base(dependencies)
    {
        MongoClient = mongoClient;
        UpdateLogger = updateLogger;
    }

    /// <summary>
    /// The <see cref="DbLoggerCategory.Update"/> logger used to emit bulk delete/update diagnostics. Bulk
    /// operations bypass SaveChanges, so MongoDatabaseWrapper's logger never fires for them.
    /// </summary>
    public virtual IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger { get; }
```

Add `using Microsoft.EntityFrameworkCore.Diagnostics;` if absent.

- [ ] **Step 2: Supply it from the factory**

In `MongoQueryContextFactory.cs`, inject `IDiagnosticsLogger<DbLoggerCategory.Update>` alongside the existing dependencies (follow the constructor-injection style already in that file) and pass it into the `MongoQueryContext` construction.

- [ ] **Step 3: Point the executors at it**

Replace `GetUpdateLogger`:

```csharp
    // Bulk operations bypass SaveChanges, so MongoDatabaseWrapper's bulk-write logging never fires for them.
    // The Update-category logger is supplied by MongoQueryContextFactory, matching how the read path gets
    // its query logger — rather than resolving it from the context's service provider per execution.
    private static IDiagnosticsLogger<DbLoggerCategory.Update> GetUpdateLogger(QueryContext queryContext)
        => ((MongoQueryContext)queryContext).UpdateLogger;
```

Remove the now-unused `using Microsoft.EntityFrameworkCore.Infrastructure;` only if nothing else in the file needs it.

- [ ] **Step 4: Run the bulk and logging suites**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~LoggingTests"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```

Expected: PASS; EF8 builds (the `UpdateLogger` property and factory change are outside `#if !EF8`, so they compile on every configuration — verify this, and if the EF8 build breaks because nothing consumes the property, leave the property unguarded and only guard its use).

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/
git commit -m "refactor: supply the bulk update logger via MongoQueryContext instead of service location"
```

---

## Phase 2 — Data integrity (setters)

### Task 7: Guard provider-owned setter targets, and derive the `$set` key from the serialization info

`BuildSetter` accepts any mapped root scalar, so a setter can target the discriminator (element `_t` — silently rewriting a document's type so the model can no longer materialize it), the primary key (element `_id` — rejected by the server as an immutable-field violation, surfacing as a raw driver exception), or a concurrency token (defeating optimistic concurrency for subsequent `SaveChanges`).

Separately, `BuildUpdate` keys `$set` with `property.GetElementName()`, while `BsonSerializerFactory.GetPropertySerializationInfo` maps composite-primary-key parts to the path `["_id", elementName]`. The two disagree. The guard above closes today's only reachable instance of that disagreement (composite-key parts are primary-key properties), so this step is defence-in-depth: it makes one function authoritative for "where does this property live in the document" so a future property kind with a path cannot silently write to the wrong field.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `private static string GetSetElementKey(IProperty property)` in `MongoShapedQueryCompilingExpressionVisitor`, used by both branches of `BuildUpdate`. `BuildSetter` gains rejections; its signature is unchanged.

> ### RESOLVED (2026-07-30) — concurrency-token setters are PERMITTED
>
> The judgement call below was put to Arthur and answered: **allow concurrency-token setters — this is a valid use case.** Relational EF permits it, and deliberately stamping a token during a bulk update is legitimate.
>
> So this task ships **two** guards, not three: primary key and type discriminator. **Do not add an `IsConcurrencyToken` guard**, and if you are reading this after one was added, it has been removed — do not reinstate it.
>
> Consequences carried into the rest of the plan:
> - `BuildSetter` has no `IsConcurrencyToken` rejection.
> - There is a *positive* test proving a concurrency token can be set by a bulk update, rather than a rejection test.
> - The README must say that bulk operations neither check **nor protect** concurrency tokens — a bulk update can set one explicitly — instead of only "concurrency tokens are not checked". Task 25 owns the full README rewrite and must preserve this wording.

**Original judgement call, kept for the record:** rejecting a setter on a concurrency token / row version is a behaviour decision, not a bug fix. Relational EF permits it. The argument for rejecting is that bulk operations already skip concurrency *checking* (documented in the README), so permitting a bulk *write* to the token silently disarms optimistic concurrency for every later `SaveChanges` on that document. If the team prefers to permit it, delete that one guard and its test and record the decision in the README bullet instead.

- [ ] **Step 1: Write the failing tests**

In `ExecuteUpdateTests.cs` add three tests. `Order` in this file already has an `ObjectId _id`; the discriminator and row-version cases need a small dedicated model, declared as nested types next to the existing test models in the file:

```csharp
    private class VersionedOrder
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!;
        public byte[] Version { get; set; } = null!;
    }

    private static void ConfigureVersionedModel(ModelBuilder mb)
        => mb.Entity<VersionedOrder>().Property(e => e.Version).IsRowVersion();

    private abstract class Animal
    {
        public ObjectId _id { get; set; }
        public string Kind { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private class Cat : Animal;

    private class Dog : Animal;

    private static void ConfigureHierarchyModel(ModelBuilder mb)
        => mb.Entity<Animal>()
            .HasDiscriminator(a => a.Kind)
            .HasValue<Cat>("cat")
            .HasValue<Dog>("dog");
```

```csharp
    [Fact]
    public void ExecuteUpdate_targeting_the_primary_key_throws()
    {
        using var db = CreateSeededContext();

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o._id, ObjectId.GenerateNewId())));

        Assert.Contains("primary key", ex.Message);
    }

    [Fact]
    public void ExecuteUpdate_targeting_the_discriminator_throws()
    {
        var collection = database.CreateCollection<Animal>();
        using var db = SingleEntityDbContext.Create(collection, ConfigureHierarchyModel);

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(a => a.Kind, "dog")));

        Assert.Contains("discriminator", ex.Message);
    }

    [Fact]
    public void ExecuteUpdate_targeting_a_concurrency_token_throws()
    {
        var collection = database.CreateCollection<VersionedOrder>();
        using var db = SingleEntityDbContext.Create(collection, ConfigureVersionedModel);

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Version, new byte[8])));

        Assert.Contains("concurrency token", ex.Message);
    }
```

- [ ] **Step 2: Run them to verify they fail**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdate_targeting_the_primary_key_throws|FullyQualifiedName~ExecuteUpdate_targeting_the_discriminator_throws|FullyQualifiedName~ExecuteUpdate_targeting_a_concurrency_token_throws"
```

Expected: all three FAIL. The primary-key one fails with a `MongoCommandException` about the immutable `_id` field rather than an `InvalidOperationException`; the other two fail because no exception is thrown at all.

- [ ] **Step 3: Add the guards**

In `MongoQueryableMethodTranslatingExpressionVisitor.BuildSetter`, immediately after the `if (property == null)` block:

```csharp
        // Reject setters on fields the provider owns. Without these guards a bulk update can rewrite a
        // document's type (the discriminator maps to _t), attempt an immutable-_id write that only fails at
        // the server, or silently disarm optimistic concurrency for every later SaveChanges on the document.
        if (property.IsPrimaryKey())
        {
            RejectSetterTarget(
                mongoQueryExpression, property,
                "it is part of the primary key, which maps to the immutable '_id' element");
        }

        if (property == entityType.FindDiscriminatorProperty())
        {
            RejectSetterTarget(
                mongoQueryExpression, property,
                "it is the type discriminator; changing it would change the entity type of the stored document");
        }

        if (property.IsConcurrencyToken)
        {
            RejectSetterTarget(
                mongoQueryExpression, property,
                "it is a concurrency token. Bulk operations do not check concurrency tokens, so writing one "
                + "would disable optimistic concurrency for later SaveChanges calls on the same document");
        }
```

and add the helper next to `ThrowBulkSourceNotSupported`:

```csharp
    [DoesNotReturn]
    private void RejectSetterTarget(MongoQueryExpression mongoQueryExpression, IProperty property, string reason)
    {
        AddTranslationErrorDetails(
            $"The property '{property.DeclaringType.DisplayName()}.{property.Name}' cannot be updated by a bulk "
            + $"update because {reason}.");
        throw new InvalidOperationException(
            CoreStrings.NonQueryTranslationFailedWithDetails(
                mongoQueryExpression.CapturedExpression?.Print(), TranslationErrorDetails));
    }
```

- [ ] **Step 4: Make the `$set` key authoritative**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, add next to `SerializeConstant`:

```csharp
    /// <summary>
    /// Returns the dotted document path a property is stored at, so a <c>$set</c> key always agrees with where
    /// the serializer reads the value from. Most properties are a single element name, but
    /// <see cref="BsonSerializerFactory.GetPropertySerializationInfo"/> maps some — composite-primary-key parts —
    /// to a path such as <c>_id.OrderID</c>. Using <c>GetElementName()</c> alone would create a new top-level
    /// field instead of updating the stored one.
    /// </summary>
    private static string GetSetElementKey(IProperty property)
    {
        var info = BsonSerializerFactory.GetPropertySerializationInfo(property);
        return info.ElementPath is { Count: > 0 } path ? string.Join('.', path) : info.ElementName;
    }
```

Then replace all four uses of `setter.Property.GetElementName()` in `BuildUpdate` with `GetSetElementKey(setter.Property)`.

If `BsonSerializationInfo` on the pinned driver has no `ElementPath` property, use whichever member `CreateWithPath` populates — check with `grep -rn "ElementPath\|CreateWithPath" ~/.nuget/packages/mongodb.driver/*/lib/**/*.xml` or by inspecting the type in the IDE — and keep the same fallback shape.

- [ ] **Step 5: Run the new tests and the whole bulk suite**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. The conformance suite is the one at risk here — if a `NorthwindBulkUpdates` case sets a key or discriminator property it will now fail. If that happens, that case moves from `base.*` to `AssertTranslationFailed` with a `// Fails: bulk setter targets a provider-owned property EF-X016` comment, and `docs/failing-spec-tests.md` is updated in Task 25.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/ \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs
git commit -m "fix: reject bulk setters on key, discriminator and concurrency-token properties"
```

---

### Task 8: Enforce the single-`TSource`-parameter precondition when rebinding setter values

`ParameterRebindingExpressionVisitor` rebinds *every* parameter whose type matches `TSource` onto the shared entity parameter, and its comment states the precondition it relies on: "the value body has exactly one TSource-typed parameter (the setter's own), so type-based rebinding is safe for the supported single-setter value shapes." Nothing enforces that. If a second `TSource`-typed parameter ever appears — a self-referencing owned type, or a widening of the supported setter shapes — every one of them collapses onto the outer parameter and the update renders **silently wrong**, with no exception. On a write path, converting that into a loud rejection is the right trade.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs` (add a sibling test class file instead — see Step 1)
- Create: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkSetterRebindingTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `internal static Expression RebindSetterValueParameters(Expression body, ParameterExpression target)` in `MongoShapedQueryCompilingExpressionVisitor` — replaces the `ParameterRebindingExpressionVisitor` instantiation in `RenderSelfReferencingValue`, and throws `InvalidOperationException` when the body contains more than one distinct parameter of `target.Type`.

- [ ] **Step 1: Write the failing test**

Create `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkSetterRebindingTests.cs` with the standard licence header, then:

```csharp
// The self-referencing setter renderer rebinds parameters by type because translation discards parameter
// identity. That is only sound while the value body has exactly one parameter of the entity type. This
// pins the guard: two distinct entity-typed parameters must be rejected, not silently merged.

#if !EF8

using System;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Visitors;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query;

public static class BulkSetterRebindingTests
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public int Quantity { get; set; }
    }

    [Fact]
    public static void Rebinds_a_single_entity_parameter_onto_the_target()
    {
        var target = Expression.Parameter(typeof(Order), "e");
        var other = Expression.Parameter(typeof(Order), "o");
        var body = Expression.Add(
            Expression.Property(other, nameof(Order.Quantity)), Expression.Constant(1));

        var actual = MongoShapedQueryCompilingExpressionVisitor.RebindSetterValueParameters(body, target);

        var rebound = Assert.IsAssignableFrom<BinaryExpression>(actual);
        Assert.Same(target, ((MemberExpression)rebound.Left).Expression);
    }

    [Fact]
    public static void Leaves_parameters_of_other_types_alone()
    {
        var target = Expression.Parameter(typeof(Order), "e");
        var unrelated = Expression.Parameter(typeof(int), "i");
        var body = Expression.Add(unrelated, Expression.Constant(1));

        var actual = MongoShapedQueryCompilingExpressionVisitor.RebindSetterValueParameters(body, target);

        Assert.Same(unrelated, ((BinaryExpression)actual).Left);
    }

    [Fact]
    public static void Throws_when_the_body_has_two_distinct_entity_parameters()
    {
        var target = Expression.Parameter(typeof(Order), "e");
        var first = Expression.Parameter(typeof(Order), "a");
        var second = Expression.Parameter(typeof(Order), "b");
        var body = Expression.Add(
            Expression.Property(first, nameof(Order.Quantity)),
            Expression.Property(second, nameof(Order.Quantity)));

        var ex = Assert.Throws<InvalidOperationException>(
            () => MongoShapedQueryCompilingExpressionVisitor.RebindSetterValueParameters(body, target));

        Assert.Contains("more than one", ex.Message);
    }
}

#endif
```

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkSetterRebindingTests"
```

Expected: does not compile — `RebindSetterValueParameters` does not exist. That counts as a failing test; proceed.

- [ ] **Step 3: Replace the visitor with a guarded entry point**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, replace the `ParameterRebindingExpressionVisitor` class with:

```csharp
    /// <summary>
    /// Rebinds the <see cref="ParameterExpression"/> in a translated self-referencing setter body to the shared
    /// entity parameter, so the assembled value lambda has exactly one parameter as required by the renderer.
    /// Rebinding is by type rather than identity because translation through
    /// <see cref="MongoEFToLinqTranslatingExpressionVisitor"/> discards the original parameter identity — which is
    /// only sound while there is exactly one parameter of the entity type. More than one is rejected rather than
    /// silently merged, because merging renders a wrong update with no error.
    /// </summary>
    internal static Expression RebindSetterValueParameters(Expression body, ParameterExpression target)
    {
        var collector = new EntityParameterCollector(target.Type);
        collector.Visit(body);

        if (collector.Parameters.Count > 1)
        {
            throw new InvalidOperationException(
                $"A self-referencing bulk update setter value referenced more than one '{target.Type.ShortDisplayName()}' "
                + "parameter, which the MongoDB provider cannot render as a single aggregation expression.");
        }

        return new ParameterRebindingExpressionVisitor(target).Visit(body)!;
    }

    private sealed class EntityParameterCollector(Type entityType) : System.Linq.Expressions.ExpressionVisitor
    {
        public HashSet<ParameterExpression> Parameters { get; } = [];

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Type == entityType)
            {
                Parameters.Add(node);
            }

            return base.VisitParameter(node);
        }
    }

    private sealed class ParameterRebindingExpressionVisitor(ParameterExpression target)
        : System.Linq.Expressions.ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node.Type == target.Type ? target : base.VisitParameter(node);
    }
```

Then in `RenderSelfReferencingValue`, replace the rebinding line:

```csharp
        translatedBody = RebindSetterValueParameters(translatedBody, entityParameter);
```

`ShortDisplayName()` comes from `Microsoft.EntityFrameworkCore.Internal`; if that using is not already present in the file, use `target.Type.Name` instead rather than adding an internal-namespace using.

- [ ] **Step 4: Run the tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkSetterRebindingTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS, 3 new unit tests, and no regression in the self-referencing functional tests.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkSetterRebindingTests.cs
git commit -m "fix: reject setter values with multiple entity parameters instead of merging them silently"
```

---

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

### Task 10: Require a real `MongoTransaction` when joining an ambient transaction

`RunInBulkTransaction` trusts a non-null `Database.CurrentTransaction` and runs the body directly, but the session is fetched with `as MongoTransaction` in two places (`GetBulkCollectionAndSession` and `TranslateQuery`). If the ambient transaction is not a `MongoTransaction`, both casts yield null and **both phases run with no session — outside any transaction** — while `RunInBulkTransaction` believes it is inside one.

That matters because the transaction is what makes two-phase safe: phase 2 filters on `_id` alone and never re-evaluates the user's predicate, so if that predicate is an authorization or tenancy check, only transaction isolation prevents acting on a document that no longer satisfies it. Silently losing the session silently loses that guarantee.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `private static IClientSessionHandle RequireBulkSession(DatabaseFacade database)` — returns the session of the ambient `MongoTransaction`, throwing `InvalidOperationException` when `CurrentTransaction` is not a usable `MongoTransaction`. Used by `RunInBulkTransaction` and `RunInBulkTransactionAsync`.

- [ ] **Step 1: Write the failing test**

In `ExecuteDeleteTests.cs`:

```csharp
    private sealed class NotAMongoTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public void Commit() { }
        public void Rollback() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public void ExecuteDelete_two_phase_with_a_foreign_ambient_transaction_throws_rather_than_running_unprotected()
    {
        using var db = CreateSeededContext();
        var manager = (MongoTransactionManager)db.Database.GetService<IDbContextTransactionManager>();
        manager.CurrentTransaction = new NotAMongoTransaction();

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDelete());

        Assert.Contains("transaction", ex.Message);
        Assert.Equal(3, db.Entities.Count());
    }
```

The `MongoTransactionManager.CurrentTransaction` setter is reachable from tests via `InternalsVisibleTo` — `NorthwindBulkUpdatesMongoFixture.UseTransaction` already uses exactly this. Copy its cast and property access. Add `using Microsoft.EntityFrameworkCore.Storage;`, `using Microsoft.EntityFrameworkCore.Infrastructure;`, `using System.Threading;` and `using System.Threading.Tasks;` as needed.

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDelete_two_phase_with_a_foreign_ambient_transaction"
```

Expected: FAIL — no exception is thrown and one document is deleted, because the operation silently ran with `session == null`.

- [ ] **Step 3: Add the guard**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, add next to `RunInBulkTransaction`:

```csharp
    // Two-phase correctness depends on phase 1 and phase 2 sharing one transaction: phase 2 filters on _id
    // alone and never re-evaluates the user's predicate, so isolation is what stops it acting on a document
    // that no longer matches. The session is read from CurrentTransaction with an `as MongoTransaction` cast,
    // which yields null for any other IDbContextTransaction implementation — and a null session means both
    // phases quietly run outside the transaction. Fail closed instead.
    private static IClientSessionHandle RequireBulkSession(DatabaseFacade database)
        => (database.CurrentTransaction as MongoTransaction)?.Session
           ?? throw new InvalidOperationException(
               "This bulk delete or update requires a MongoDB transaction, but the ambient transaction on this "
               + "DbContext is not a MongoDB transaction with an active session. Open the transaction with "
               + "Database.BeginTransaction() on a MongoDB-backed context.");
```

In both `RunInBulkTransaction` and `RunInBulkTransactionAsync`, replace the ambient-transaction fast path:

```csharp
        var database = queryContext.Context.Database;
        if (database.CurrentTransaction != null)
        {
            // Validate the ambient transaction is usable before running either phase; we join it and never
            // commit it, because the caller owns it.
            RequireBulkSession(database);
            return body();
        }
```

(`RunInBulkTransactionAsync` uses `return await body().ConfigureAwait(false);`.)

Add `using Microsoft.EntityFrameworkCore.Infrastructure;` for `DatabaseFacade` if it is not already imported.

- [ ] **Step 4: Run the test and the transaction suites**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. The conformance suite is the important check — its fixture enlists a real `MongoTransaction` via `UseTransaction`, so `RequireBulkSession` must accept it. If the suite now fails, the fixture is assigning something whose `Session` is null and the guard has found a genuine problem in the fixture; fix the fixture, not the guard.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "fix: require a usable MongoDB session when joining an ambient transaction for bulk ops"
```

---

### Task 11: Translate before opening the transaction, and give phase 1 the canonical failure

Two ordering problems in the two-phase path:

1. `TranslateBulkOrThrow` wraps `BuildFilter` and `BuildUpdate` but **not** `SelectTargetIds` / `BuildIdDocumentQuery`. A source that only fails when phase 1 is translated — `docs/failing-spec-tests.md` cites a `GroupBy` subquery inside the `Where` — surfaces a raw driver or `ExpressionNotSupportedException` instead of EF's canonical non-query translation failure. That is the `EF-X002` defect class this repo already tracks.
2. Everything happens inside `RunInBulkTransaction`, whose catch maps *any* transaction-unsupported-looking failure to `StandaloneTransactionError`. On a standalone deployment `BeginTransaction` fails first, so an untranslatable query reports "the current MongoDB deployment does not support multi-document transactions" — an actively misleading diagnostic for a query that was never translatable.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: `RequireBulkSession` from Task 10.
- Produces: `BuildUpdate` is invoked by the two-phase executors *before* `RunInBulkTransaction`, so the `update` is passed into the body as a captured local. `RunInBulkTransaction` and `RunInBulkTransactionAsync` keep their signatures.

- [ ] **Step 1: Write the failing test**

In `ExecuteUpdateTests.cs`:

```csharp
    [Fact]
    public void ExecuteUpdate_two_phase_with_an_invalid_setter_fails_before_starting_a_transaction()
    {
        using var db = CreateSeededContext();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        // AutoTransactionBehavior.Never makes transaction startup impossible, so if the setter is validated
        // first the message must be about the setter, not about transactions.
        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.OrderBy(o => o.Quantity).Take(1)
                .ExecuteUpdate(s => s.SetProperty(o => o._id, ObjectId.GenerateNewId())));

        Assert.Contains("primary key", ex.Message);
        Assert.DoesNotContain("AutoTransactionBehavior", ex.Message);
    }
```

Note: the primary-key setter is rejected at *translation* time by Task 7, so this test also passes today. Verify that before relying on it — if it passes, replace the setter with one that only fails when the update document is built, and if no such shape exists in the supported subset, keep this test as a regression guard and rely on Step 3's restructure being verified by the conformance suite instead. Record whichever choice you make in a comment on the test.

- [ ] **Step 2: Run it and record the outcome**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdate_two_phase_with_an_invalid_setter_fails_before_starting_a_transaction"
```

- [ ] **Step 3: Give `BeginTransaction` its own mapped try, so body failures propagate untouched**

Replace `RunInBulkTransaction`:

```csharp
    private static int RunInBulkTransaction(QueryContext queryContext, MongoNonQueryExpression nonQuery, Func<int> body)
    {
        var database = queryContext.Context.Database;
        if (database.CurrentTransaction != null)
        {
            RequireBulkSession(database);
            return body();
        }

        if (database.AutoTransactionBehavior == AutoTransactionBehavior.Never)
        {
            throw NoAutoTransactionError();
        }

        // Transaction startup gets its own try so that only startup failures are mapped to
        // StandaloneTransactionError. Failures from the body — including translation failures raised when
        // phase 1 is built — must propagate with their own message rather than being relabelled as
        // "this deployment does not support transactions".
        IDbContextTransaction transaction;
        try
        {
            transaction = database.BeginTransaction();
        }
        catch (Exception exception) when (IsTransactionsUnsupported(exception))
        {
            throw StandaloneTransactionError(exception);
        }

        try
        {
            var result = body();
            transaction.Commit();
            return result;
        }
        catch
        {
            SafeRollback(transaction);
            throw;
        }
        finally
        {
            transaction.Dispose();
        }
    }
```

Apply the same restructure to `RunInBulkTransactionAsync`, using `await database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)`, `await transaction.CommitAsync(cancellationToken)`, `await SafeRollbackAsync(transaction)` (see Task 12 for its signature change) and `await transaction.DisposeAsync()`.

- [ ] **Step 4: Wrap phase 1 and hoist `BuildUpdate` out of the transaction**

Replace `ExecuteTwoPhaseUpdate` and `ExecuteTwoPhaseDelete` (and their async twins) so the update document is built first and phase 1 is wrapped:

```csharp
    private static int ExecuteTwoPhaseUpdate<TSource>(
        QueryContext queryContext, IReadOnlyEntityType entityType, BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        // Built before the transaction: an unsupported setter is a translation error and must not be reported
        // as a transaction problem, nor cost a transaction round trip.
        var update = TranslateBulkOrThrow(nonQuery,
            () => BuildUpdate<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery));

        return RunInBulkTransaction(queryContext, nonQuery, () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var ids = TranslateBulkOrThrow(nonQuery,
                () => SelectTargetIds<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery));
            return ids.Count == 0 ? 0 : UpdateByIds(queryContext, nonQuery, ids, update, stopwatch);
        });
    }
```

`ExecuteTwoPhaseDelete` gets the same `TranslateBulkOrThrow` wrapper around `SelectTargetIds` and has no update to hoist. The async twins wrap `SelectTargetIdsAsync`; because `TranslateBulkOrThrow` is synchronous, wrap the *awaited result* rather than the task — extract the body into a local async function and wrap the `await` in a try/catch that mirrors `TranslateBulkOrThrow`'s mapping, or add an async overload:

```csharp
    private static async Task<T> TranslateBulkOrThrowAsync<T>(MongoNonQueryExpression nonQuery, Func<Task<T>> translate)
    {
        try
        {
            return await translate().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                CoreStrings.NonQueryTranslationFailedWithDetails(
                    nonQuery.SourceQuery.CapturedExpression?.Print(), exception.Message),
                exception);
        }
    }
```

Use the async overload in `ExecuteTwoPhaseDeleteAsync` and `ExecuteTwoPhaseUpdateAsync`.

- [ ] **Step 5: Update the stale `BuildFilter` invariant comment and assert the strategy**

`BuildFilter`'s comment names `ValidateBulkSource`, which does not exist, and claims the classifier rejects every operator but `Where` — no longer true. `BuildFilter` walks only `Where` nodes and stops at the first non-`Where`, silently dropping the inner predicates, so `Where(a).OrderBy(k).Where(b)` would filter on `b` alone and delete more than requested. It is unreachable today only because of strategy dispatch. Make that explicit:

```csharp
        // Only the single-command strategy may reach here. The walk below stops at the first non-Where node and
        // would silently drop every predicate inside it (Where(a).OrderBy(k).Where(b) would filter on b alone),
        // so a two-phase chain must never be routed through this method — it uses the read path instead.
        Debug.Assert(
            nonQuery.Strategy == MongoNonQueryExpression.BulkStrategy.SingleCommand,
            "BuildFilter is only valid for the SingleCommand strategy.");

        var sharedParameter = Expression.Parameter(typeof(TSource), "e");
```

and after the walk, before the `combinedBody == null` check:

```csharp
        if (expression is MethodCallExpression unexpected)
        {
            throw new InvalidOperationException(
                CoreStrings.NonQueryTranslationFailedWithDetails(
                    nonQuery.SourceQuery.CapturedExpression?.Print(),
                    $"The '{unexpected.Method.Name}' operator cannot scope a single-command bulk operation."));
        }
```

- [ ] **Step 6: Run everything**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. Several conformance cases currently tagged `EF-X002` ("throws a different exception") may now produce the canonical message; if so, change their assertion from `Assert.ThrowsAnyAsync<Exception>` to `AssertTranslationFailed` and note the move for Task 25's `docs/failing-spec-tests.md` update.

- [ ] **Step 7: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/
git commit -m "fix: translate before opening the bulk transaction and give phase 1 the canonical failure"
```

---

### Task 12: Fix cancellation cleanup and add cancellation coverage

`RunInBulkTransactionAsync`'s catch calls `SafeRollbackAsync(transaction, cancellationToken)` with **the token that just cancelled the operation**. `RollbackAsync` on an already-cancelled token faults immediately, that fault is swallowed by the bare `catch { }`, and the transaction is therefore never aborted client-side — it lingers until `transactionLifetimeLimitSeconds`. Cleanup must not observe the caller's cancellation. Separately, both `SafeRollback` helpers catch absolutely everything with no filter.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `SafeRollbackAsync` loses its `CancellationToken` parameter — new signature `private static async Task SafeRollbackAsync(IDbContextTransaction transaction)`.

- [ ] **Step 1: Write the failing test**

In `ExecuteDeleteTests.cs`:

```csharp
    [Fact]
    public async Task ExecuteDeleteAsync_two_phase_cancelled_leaves_data_unchanged()
    {
        using var db = CreateSeededContext();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDeleteAsync(cts.Token));

        Assert.Equal(3, db.Entities.Count());
    }
```

- [ ] **Step 2: Run it to verify behaviour**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteAsync_two_phase_cancelled_leaves_data_unchanged"
```

Expected: FAIL or hang. If it passes, it is passing for the wrong reason — the data is unchanged because nothing was written, not because the transaction was aborted. Either way proceed to Step 3; the assertion is worth keeping as the regression guard, and Step 3 is the substantive fix.

- [ ] **Step 3: Stop passing the caller's token into cleanup, and filter the catches**

```csharp
    private static void SafeRollback(IDbContextTransaction transaction)
    {
        try
        {
            transaction.Rollback();
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or StackOverflowException))
        {
            // A failed or non-transactional session may not be rollback-able; the original error wins. The
            // transaction is never durable while uncommitted, so the worst case is that the server reclaims it
            // at transactionLifetimeLimitSeconds.
        }
    }

    // Deliberately takes no CancellationToken: this runs on the failure path, and when the failure *is*
    // cancellation, passing the caller's already-cancelled token would make RollbackAsync fault immediately
    // and leave the transaction open until the server's lifetime limit expires.
    private static async Task SafeRollbackAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or StackOverflowException))
        {
            // See SafeRollback.
        }
    }
```

Update the one call site in `RunInBulkTransactionAsync` to `await SafeRollbackAsync(transaction).ConfigureAwait(false);`.

Also change the `finally` disposal so cleanup does not observe cancellation — `await transaction.DisposeAsync().ConfigureAwait(false)` already takes no token, so no change is needed there; and change `await transaction.CommitAsync(cancellationToken)` to keep the token, since commit *should* honour cancellation.

- [ ] **Step 4: Run the async bulk tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS, including the new cancellation test.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "fix: roll back bulk transactions with an uncancelled token and filter the rollback catches"
```

---

## Phase 4 — Classification

### Task 13: Only require two-phase when paging is present

`ClassifyBulkSource` routes any `OrderBy`/`OrderByDescending`/`ThenBy`/`ThenByDescending`/`Distinct` to two-phase even with no `Skip`/`Take`. Ordering cannot change *which* documents a bulk operation affects without paging, and `Distinct` over a single collection's entities is a no-op — documents are already distinct by `_id`. The PR's own test says so: `ExecuteDelete_with_OrderBy_and_no_Take_deletes_everything` is commented "semantically equivalent to the single-command path".

The cost of the misclassification is not just speed. It makes the operation require a transaction-capable deployment, makes it throw under `AutoTransactionBehavior.Never`, reads every matching document to the client, and forces the server to perform a `$sort` that cannot affect the result — where an unindexed blocking sort over 100 MB fails outright with `QueryExceededMemoryLimitNoDiskUseAllowed`. A no-op `.OrderBy(...)` can turn a working delete into a hard error.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: the `Strategy=` message segment from Task 4 and the command tests' `SpyLoggerProvider` pattern from Task 5.
- Produces: `ClassifyBulkSource` returns `SingleCommand` for chains of `Where`/`OrderBy`/`OrderByDescending`/`ThenBy`/`ThenByDescending`/`Distinct` with no `Skip`/`Take`, and `TwoPhase` only when `Skip` or `Take` is present. Its signature is unchanged.

- [ ] **Step 1: Write the failing tests**

In `ExecuteDeleteTests.cs`:

```csharp
    [Fact]
    public void ExecuteDelete_with_OrderBy_and_no_paging_uses_the_single_command_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        using var db = CreateSeededContext(loggerFactory);

        var deleted = db.Entities.Where(o => o.Status == "open").OrderBy(o => o.Quantity).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Contains("Strategy=SingleCommand", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
    }

    [Fact]
    public void ExecuteDelete_with_Distinct_and_no_paging_uses_the_single_command_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        using var db = CreateSeededContext(loggerFactory);

        var deleted = db.Entities.Where(o => o.Status == "open").Distinct().ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Contains("Strategy=SingleCommand", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
    }

    [Fact]
    public void ExecuteDelete_with_OrderBy_and_no_paging_works_under_AutoTransactionBehavior_Never()
    {
        using var db = CreateSeededContext();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        // Ordering without paging does not change the affected set, so it must not demand a transaction.
        var deleted = db.Entities.Where(o => o.Status == "open").OrderBy(o => o.Quantity).ExecuteDelete();

        Assert.Equal(2, deleted);
    }

    [Fact]
    public void ExecuteDelete_with_OrderBy_Take_still_uses_the_two_phase_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        using var db = CreateSeededContext(loggerFactory);

        var deleted = db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Contains("Strategy=TwoPhase", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
    }
```

`CreateSeededContext` currently takes no arguments. Add an optional `ILoggerFactory? loggerFactory = null` parameter to it and forward it to `SingleEntityDbContext.Create`, so no existing caller changes.

- [ ] **Step 2: Run them to verify three fail**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDelete_with_OrderBy_and_no_paging|FullyQualifiedName~ExecuteDelete_with_Distinct_and_no_paging|FullyQualifiedName~ExecuteDelete_with_OrderBy_Take_still"
```

Expected: the three new `no_paging` tests FAIL (`Strategy=TwoPhase` is logged, and the `AutoTransactionBehavior.Never` one throws); `ExecuteDelete_with_OrderBy_Take_still_uses_the_two_phase_path` PASSES.

- [ ] **Step 3: Split ordering from paging in the classifier**

Replace `ClassifyBulkSource`:

```csharp
    /// <summary>
    /// Classifies the captured source chain of a bulk delete/update.
    /// <para>
    /// <c>Where</c> predicates scope the operation directly. <c>OrderBy</c>/<c>OrderByDescending</c>/
    /// <c>ThenBy</c>/<c>ThenByDescending</c>/<c>Distinct</c> do not change *which* documents are affected —
    /// ordering only matters in combination with paging, and <c>Distinct</c> over one collection's entities is a
    /// no-op because documents are already distinct by <c>_id</c> — so on their own they stay on the atomic
    /// single-command path.
    /// </para>
    /// <para>
    /// <c>Skip</c>/<c>Take</c> do change the affected set and cannot be expressed as a single
    /// <c>deleteMany</c>/<c>updateMany</c>, so they require the two-phase (query target <c>_id</c>s, then act by
    /// <c>$in</c>) path. Any other operator is not expressible as a server-side bulk scope and produces EF's
    /// canonical non-query translation failure. A TPH discriminator filter rides along as a Where.
    /// </para>
    /// </summary>
    private MongoNonQueryExpression.BulkStrategy ClassifyBulkSource(MongoQueryExpression mongoQueryExpression)
    {
        var expression = MongoNonQueryExpression.UnwrapBulkOperator(mongoQueryExpression.CapturedExpression);
        var requiresTwoPhase = false;

        while (expression is MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType != typeof(Queryable))
            {
                ThrowBulkSourceNotSupported(mongoQueryExpression, methodCallExpression.Method.Name);
            }

            switch (methodCallExpression.Method.Name)
            {
                // Scope the operation, or are irrelevant to which documents it affects.
                case nameof(Queryable.Where):
                case nameof(Queryable.OrderBy):
                case nameof(Queryable.OrderByDescending):
                case nameof(Queryable.ThenBy):
                case nameof(Queryable.ThenByDescending):
                case nameof(Queryable.Distinct):
                    break;

                // Change the affected set; not expressible as one command.
                case nameof(Queryable.Skip):
                case nameof(Queryable.Take):
                    requiresTwoPhase = true;
                    break;

                default:
                    ThrowBulkSourceNotSupported(mongoQueryExpression, methodCallExpression.Method.Name);
                    break;
            }

            expression = methodCallExpression.Arguments[0];
        }

        return requiresTwoPhase
            ? MongoNonQueryExpression.BulkStrategy.TwoPhase
            : MongoNonQueryExpression.BulkStrategy.SingleCommand;
    }
```

- [ ] **Step 4: Make `BuildFilter` skip the ordering operators it may now see**

`BuildFilter` is now reachable for chains containing `OrderBy`/`Distinct`, and its walk currently stops at the first non-`Where`, which would silently drop inner predicates — exactly the hazard Task 11 Step 5 made explicit. Change the walk to step over the ordering operators instead of stopping:

```csharp
        Expression? combinedBody = null;
        var expression = MongoNonQueryExpression.UnwrapBulkOperator(nonQuery.SourceQuery.CapturedExpression);

        while (expression is MethodCallExpression { Method: { DeclaringType: var declaringType } method } call
               && declaringType == typeof(Queryable))
        {
            switch (method.Name)
            {
                case nameof(Queryable.Where):
                    var predicate = call.Arguments[1].UnwrapLambdaFromQuote();
                    var translatedBody = translator.Visit(predicate.Body)!;
                    translatedBody = ReplacingExpressionVisitor.Replace(
                        predicate.Parameters[0], sharedParameter, translatedBody);
                    combinedBody = combinedBody == null
                        ? translatedBody
                        : Expression.AndAlso(combinedBody, translatedBody);
                    break;

                // Do not affect which documents match; skip over them and keep collecting predicates from
                // further in. Stopping here would silently drop every inner Where.
                case nameof(Queryable.OrderBy):
                case nameof(Queryable.OrderByDescending):
                case nameof(Queryable.ThenBy):
                case nameof(Queryable.ThenByDescending):
                case nameof(Queryable.Distinct):
                    break;

                default:
                    throw new InvalidOperationException(
                        CoreStrings.NonQueryTranslationFailedWithDetails(
                            nonQuery.SourceQuery.CapturedExpression?.Print(),
                            $"The '{method.Name}' operator cannot scope a single-command bulk operation."));
            }

            expression = call.Arguments[0];
        }
```

Delete the trailing `if (expression is MethodCallExpression unexpected)` block added in Task 11 Step 5 — the `default` arm now covers it — but keep the `Debug.Assert` on the strategy.

- [ ] **Step 5: Add the interleaved-`Where` test that this makes reachable**

In `ExecuteDeleteTests.cs`:

```csharp
    [Fact]
    public void ExecuteDelete_with_Where_either_side_of_OrderBy_applies_both_predicates()
    {
        using var db = CreateSeededContext();

        // Both predicates must survive: the walk steps over OrderBy rather than stopping at it.
        var deleted = db.Entities
            .Where(o => o.Status == "open")
            .OrderBy(o => o.Quantity)
            .Where(o => o.Quantity > 1)
            .ExecuteDelete();

        Assert.Equal(1, deleted);
        Assert.Equal(2, db.Entities.Count());
    }
```

Adjust the expected counts to the seed data in `CreateSeededContext` — read it and compute the correct values rather than assuming.

- [ ] **Step 6: Run the full bulk and conformance suites on EF9 and EF10**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. Existing tests that assert *outcomes* for bare-`OrderBy`/`Distinct` shapes keep passing because the affected set is identical — that equivalence is the whole justification for this change, so if any outcome assertion changes, stop and investigate rather than editing the assertion.

- [ ] **Step 7: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/ \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "perf: keep ordering-only and Distinct bulk operations on the atomic single-command path"
```

---

### Task 14: Match canonical `QueryableMethods` definitions and accept `Order`/`OrderDescending`

`ClassifyBulkSource` switches on bare `Method.Name`, which `src/MongoDB.EntityFrameworkCore/Query/AGENTS.md` lists as a pitfall:

> **Reference-equality on `MethodInfo`.** Translators that match by `MethodInfo` must use canonical constants — `QueryableMethods` for the top-level dispatch in `MongoQueryableMethodTranslatingExpressionVisitor` […]

Every other dispatch in the same file pairs `nameof(...)` with `when methodDefinition == QueryableMethods.X`. Name-only matching silently admits overloads with different semantics — indexed `Where`, `Distinct(IEqualityComparer<T>)`, `OrderBy(keySelector, IComparer<T>)`, `Take(Range)` — which then fail deeper with a driver-shaped error instead of the canonical rejection. The indexed `Where` is the worst: `BuildFilter` rebinds only `Parameters[0]`, leaving the index parameter unbound in the lambda it builds.

`Queryable.Order()` and `Queryable.OrderDescending()` (.NET 7, available on `net8.0`) are also missing, so they are rejected despite being exactly equivalent to `OrderBy(x => x)`.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: the classifier shape from Task 13.
- Produces: no signature changes. `ClassifyBulkSource` now compares `method.GetGenericMethodDefinition()` against `QueryableMethods` constants and a small local set for the operators `QueryableMethods` does not expose.

- [ ] **Step 1: Write the failing tests**

```csharp
    [Fact]
    public void ExecuteDelete_with_indexed_Where_throws_the_canonical_failure()
    {
        using var db = CreateSeededContext();

        // Queryable.Where(source, (item, index) => ...) is a different operator with different semantics.
        // It must be rejected cleanly, not admitted and then rendered with an unbound index parameter.
        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.Where((o, i) => i == 0).ExecuteDelete());

        Assert.Contains("could not be translated", ex.Message);
        Assert.Equal(3, db.Entities.Count());
    }

    [Fact]
    public void ExecuteDelete_with_Order_deletes_matching()
    {
        using var db = CreateSeededContext();

        // Queryable.Order() is keyless ordering, equivalent to OrderBy(x => x) — and equally irrelevant to
        // which documents a delete affects.
        var deleted = db.Entities.Where(o => o.Status == "open").Order().ExecuteDelete();

        Assert.Equal(2, deleted);
    }
```

`Order()` requires `Order` to be comparable. If the test entity is not `IComparable`, use `db.Entities.Select(o => o).Order()` — no; that introduces a `Select`. Instead make the test model implement `IComparable<Order>` by `_id`, or if that is intrusive, replace this test with one over a keyless-orderable projection-free shape. Read the model first and pick whichever needs no `Select` in the chain; if neither works, drop this test and keep only the classifier entry plus a comment recording that it is unreachable from C# for this model.

- [ ] **Step 2: Run them to verify they fail**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDelete_with_indexed_Where|FullyQualifiedName~ExecuteDelete_with_Order_deletes_matching"
```

Expected: both FAIL. The indexed-`Where` case throws something other than the canonical message (or deletes documents); `Order()` throws "The 'Order' operator is not supported in a bulk delete or update".

- [ ] **Step 3: Match on method definitions**

Replace the `switch` inside `ClassifyBulkSource` with definition comparisons:

```csharp
        while (expression is MethodCallExpression methodCallExpression)
        {
            var method = methodCallExpression.Method;
            if (method.DeclaringType != typeof(Queryable))
            {
                ThrowBulkSourceNotSupported(mongoQueryExpression, method.Name);
            }

            var definition = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;

            if (definition == QueryableMethods.Where)
            {
                // Scopes the operation; BuildFilter and the read path both handle it.
            }
            else if (IsOrderingOperator(definition))
            {
                // Does not change which documents are affected.
            }
            else if (definition == QueryableMethods.Skip || definition == QueryableMethods.Take)
            {
                requiresTwoPhase = true;
            }
            else
            {
                ThrowBulkSourceNotSupported(mongoQueryExpression, method.Name);
            }

            expression = methodCallExpression.Arguments[0];
        }
```

and add the helper plus the definition set:

```csharp
    // QueryableMethods exposes Where/Skip/Take/Distinct but not the ordering operators or the .NET 7 keyless
    // Order/OrderDescending, so those are resolved once from Queryable itself. Matching definitions rather than
    // names keeps overloads with different semantics — indexed Where, Distinct(IEqualityComparer<T>),
    // OrderBy(keySelector, IComparer<T>), Take(Range) — out of the supported set (Query/AGENTS.md).
    private static readonly HashSet<MethodInfo> OrderingOperatorDefinitions =
        typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.IsGenericMethodDefinition
                        && m.GetParameters().Length == 2
                        && m.Name is nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending)
                            or nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending))
            .Concat(
                typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.IsGenericMethodDefinition
                                && m.GetParameters().Length == 1
                                && m.Name is nameof(Queryable.Order) or nameof(Queryable.OrderDescending)))
            .Append(QueryableMethods.Distinct)
            .ToHashSet();

    private static bool IsOrderingOperator(MethodInfo definition)
        => OrderingOperatorDefinitions.Contains(definition);
```

Add `using System.Collections.Generic;`, `using System.Reflection;` and `using System.Linq;` if any are missing. `QueryableMethods` lives in `Microsoft.EntityFrameworkCore.Query`, which the file already imports.

Verify the reflection actually resolves `Order`/`OrderDescending` on the pinned framework — add a temporary `Debug.Assert(OrderingOperatorDefinitions.Count >= 7)` while developing, and remove it before committing.

- [ ] **Step 4: Run the tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. If a conformance case used an overload that name-matching previously admitted and that now rejects cleanly, move it to `AssertTranslationFailed` with a `// Fails:` comment and note it for Task 25.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "fix: classify bulk sources by canonical method definition; support Order/OrderDescending"
```

---

### Task 15: Support `OfType` and `Cast` by routing them through the two-phase read path

`OfType<T>()` is the idiomatic way to scope a polymorphic query in this provider — it is what `DiscriminatorTests` actually uses (`Returns_correct_entity_with_OfType_query`, `OfType_does_not_break_entity_serializer_association`), it is a first-class read-path operator (`TranslateOfType`), and the driver generates the `{_t: …}` filter for it via `MongoEFDiscriminator`. The classifier rejects it, so users must rewrite `db.Entities.OfType<Customer>().ExecuteDelete()` as `Where(e => e is Customer)`.

Classify it as **two-phase** rather than single-command: phase 1 reuses the read-path translation, so the discriminator filter comes for free. Making it single-command would require `BuildFilter` to synthesize `_t` itself — more work, easy to get wrong, and getting it wrong on a delete destroys the wrong documents.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsInheritanceTests.cs`

**Interfaces:**
- Consumes: the classifier shape from Tasks 13 and 14.
- Produces: `ClassifyBulkSource` returns `TwoPhase` for any chain containing `QueryableMethods.OfType` or `QueryableMethods.Cast`.

- [ ] **Step 1: Write the failing tests**

Create `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsInheritanceTests.cs` with the standard licence header, then:

```csharp
// Bulk operations against a TPH hierarchy. This is the case where a classification mistake destroys the
// wrong documents rather than returning the wrong rows, so it is covered explicitly.

#if !EF8

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

[XUnitCollection("QueryTests")]
public class BulkOperationsInheritanceTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private abstract class Animal
    {
        public ObjectId _id { get; set; }
        public string Kind { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private class Cat : Animal;

    private class Dog : Animal;

    private static void Configure(ModelBuilder mb)
        => mb.Entity<Animal>()
            .HasDiscriminator(a => a.Kind)
            .HasValue<Cat>("cat")
            .HasValue<Dog>("dog");

    private SingleEntityDbContext<Animal> CreateSeeded(string name)
    {
        var collection = database.CreateCollection<Animal>(name);
        using (var seed = SingleEntityDbContext.Create(collection, Configure))
        {
            seed.AddRange(
                new Cat { _id = ObjectId.GenerateNewId(), Name = "Tom" },
                new Cat { _id = ObjectId.GenerateNewId(), Name = "Felix" },
                new Dog { _id = ObjectId.GenerateNewId(), Name = "Rex" });
            seed.SaveChanges();
        }

        return SingleEntityDbContext.Create(collection, Configure);
    }

    [Fact]
    public void ExecuteDelete_with_OfType_deletes_only_that_type()
    {
        using var db = CreateSeeded(nameof(ExecuteDelete_with_OfType_deletes_only_that_type));

        var deleted = db.Entities.OfType<Cat>().ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Single(db.Entities);
        Assert.IsType<Dog>(db.Entities.Single());
    }

    [Fact]
    public void ExecuteUpdate_with_OfType_updates_only_that_type()
    {
        using var db = CreateSeeded(nameof(ExecuteUpdate_with_OfType_updates_only_that_type));

        var updated = db.Entities.OfType<Cat>().ExecuteUpdate(s => s.SetProperty(a => a.Name, "renamed"));

        Assert.Equal(2, updated);
        Assert.Equal("Rex", db.Entities.OfType<Dog>().Single().Name);
    }

    [Fact]
    public void ExecuteDelete_with_an_is_predicate_deletes_only_that_type_on_the_single_command_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Animal>(
            nameof(ExecuteDelete_with_an_is_predicate_deletes_only_that_type_on_the_single_command_path));
        using (var seed = SingleEntityDbContext.Create(collection, Configure))
        {
            seed.AddRange(
                new Cat { _id = ObjectId.GenerateNewId(), Name = "Tom" },
                new Dog { _id = ObjectId.GenerateNewId(), Name = "Rex" });
            seed.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, Configure, loggerFactory: loggerFactory);

        var deleted = db.Entities.Where(a => a is Cat).ExecuteDelete();

        Assert.Equal(1, deleted);
        Assert.IsType<Dog>(db.Entities.Single());
        Assert.Contains("Strategy=SingleCommand", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
    }
}

#endif
```

Match the `SingleEntityDbContext.Create` overloads that exist — copy the call shapes used in `DiscriminatorTests.cs` and adapt. The `SingleEntityDbContext<Animal>` return type must match whatever `Create` returns in this repo; read it and use the real type.

- [ ] **Step 2: Run them to verify the `OfType` ones fail**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsInheritanceTests"
```

Expected: the two `OfType` tests FAIL with "The 'OfType' operator is not supported in a bulk delete or update"; the `is`-predicate test PASSES.

- [ ] **Step 3: Admit `OfType` and `Cast` as two-phase**

In `ClassifyBulkSource`, add a branch before the `else` that throws:

```csharp
            else if (definition == QueryableMethods.OfType || definition == QueryableMethods.Cast)
            {
                // Type scoping is expressed by the driver as a { _t: ... } filter, which only the read path
                // knows how to build. Route through two-phase so phase 1 reuses that translation rather than
                // reimplementing discriminator filtering in BuildFilter — getting that wrong on a delete would
                // destroy documents of the wrong type.
                requiresTwoPhase = true;
            }
```

If `QueryableMethods.Cast` does not exist on the pinned EF version, resolve it the same way as the ordering operators (reflection over `typeof(Queryable)` for the single-parameter generic `Cast`), and note in a comment which versions needed the fallback.

- [ ] **Step 4: Run the tests, including the discriminator regression suite**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsInheritanceTests|FullyQualifiedName~DiscriminatorTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~BulkOperationsInheritanceTests"
```

Expected: PASS. If phase 1 does not in fact emit the `_t` filter — the `OfType` delete removes the `Dog` too — **stop and do not weaken the test**. That would mean the read path's discriminator filtering does not survive the `CreateQuery<BsonDocument>` reinterpretation in `BuildIdDocumentQuery`, which is a much more serious finding than this task; revert the classifier change so `OfType` stays rejected, keep the tests as `Assert.Throws` documenting the limitation, and raise it separately.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsInheritanceTests.cs
git commit -m "feat: support OfType/Cast in bulk operations via the two-phase read path"
```

---

## Phase 5 — Performance

### Task 16: Project `{_id: 1}` in phase 1

`BuildIdDocumentQuery` emits `[$match, $sort, $skip, $limit]` with no `$project`, so phase 1 transfers **whole documents** and keeps one field. The in-code comment calls a projection "a future optimization … to reduce transfer", which undersells it: besides wire bytes and client-side BSON parsing, the absence of a projection forecloses a covered index scan. With `{_id: 1}` and a supporting index, `Where(o => o.Status == "open").OrderBy(o => o.Quantity).Take(10)` can be satisfied from the index alone, because MongoDB index entries carry `_id`. Today the server must fetch every candidate document to return a field it already had.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs`

**Interfaces:**
- Consumes: `RenderBulkCommand` from Task 5.
- Produces:
  - `BuildIdDocumentQuery` appends a `{ $project: { _id: 1 } }` stage before returning.
  - `private static string RenderPhaseOnePipeline(IDiagnosticsLogger<DbLoggerCategory.Update> logger, IQueryable<BsonDocument> query)` — returns `"?"` unless sensitive logging is enabled, else the driver's rendered pipeline string.
  - `SelectTargetIds` / `SelectTargetIdsAsync` gain an `out string phaseOnePipeline` / return a tuple so the caller can pass it to the `Executing` event. Use a small holder to keep the async signature clean:
    `private sealed record PhaseOneResult(List<BsonValue> Ids, string Pipeline);`
    and both methods return `PhaseOneResult`.

- [ ] **Step 1: Write the failing test**

In `BulkOperationsCommandTests.cs`:

```csharp
    [Fact]
    public void Two_phase_delete_projects_only_the_id_in_phase_one()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(nameof(Two_phase_delete_projects_only_the_id_in_phase_one));
        collection.InsertMany(
        [
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 },
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 2 }
        ]);

        using var db = SingleEntityDbContext.Create(
            collection, loggerFactory: loggerFactory, sensitiveDataLogging: true);

        db.Entities.OrderBy(o => o.Quantity).Take(1).ExecuteDelete();

        var executing = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete);
        Assert.Contains("Strategy=TwoPhase", executing);
        Assert.Contains("$project", executing);
        Assert.Contains("_id", executing);
    }
```

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~Two_phase_delete_projects_only_the_id_in_phase_one"
```

Expected: FAIL — the message contains no `$project` because phase 1 is not logged at all and no projection is emitted.

- [ ] **Step 3: Append the projection and surface the pipeline**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, replace `BuildIdDocumentQuery`'s return and add the renderer:

```csharp
    // Phase 1 only needs each matched document's _id. Appending a { $project: { _id: 1 } } stage keeps whole
    // documents off the wire and — with a supporting index — lets the server answer from the index alone,
    // because index entries carry _id. The stage is appended last, after any Distinct/Skip/Take, so it cannot
    // change which documents are selected.
    private static readonly BsonDocument IdOnlyProjectionStage = new("$project", new BsonDocument("_id", 1));

    private static IQueryable<BsonDocument> BuildIdDocumentQuery<TSource>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        var (_, executableQuery) = TranslateQuery<TSource>(
            queryContext, entityType, bsonSerializerFactory, nonQuery.SourceQuery, ResultCardinality.Enumerable,
            (translator, expression) =>
                translator.Translate(MongoNonQueryExpression.UnwrapBulkOperator(expression)!, ResultCardinality.Enumerable));

        var documents = executableQuery.Provider.CreateQuery<BsonDocument>(executableQuery.Query);

        return documents is IMongoQueryable<BsonDocument> mongoQueryable
            ? mongoQueryable.AppendStage<BsonDocument, BsonDocument, BsonDocument>(IdOnlyProjectionStage)
            : documents;
    }

    private static string RenderPhaseOnePipeline(
        IDiagnosticsLogger<DbLoggerCategory.Update> logger, IQueryable<BsonDocument> query)
        => logger.ShouldLogSensitiveData() ? query.ToString() ?? "?" : "?";
```

`AppendStage`'s exact generic arity and containing type vary across driver versions. Verify with:

```bash
grep -rn "AppendStage" ~/.nuget/packages/mongodb.driver/*/lib/net*/MongoDB.Driver.xml | head
```

If `IMongoQueryable<T>.AppendStage` is unavailable, fall back to a LINQ projection that the driver can translate — `documents.Select(d => new BsonDocument("_id", d["_id"]))` — and if *that* does not translate either, leave `BuildIdDocumentQuery` unprojected, mark this task blocked, record the driver limitation in a comment, and keep Task 17 (which delivers most of the memory win independently). Do not silently ship a projection you have not confirmed appears in the pipeline: the test in Step 1 is the gate.

- [ ] **Step 4: Thread the pipeline into the `Executing` event**

Change `SelectTargetIds`/`SelectTargetIdsAsync` to return the holder:

```csharp
    private sealed record PhaseOneResult(List<BsonValue> Ids, string Pipeline);

    private static PhaseOneResult SelectTargetIds<TSource>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        var documents = BuildIdDocumentQuery<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery);
        var pipeline = RenderPhaseOnePipeline(GetUpdateLogger(queryContext), documents);

        var ids = new List<BsonValue>();
        foreach (var document in documents)
        {
            ids.Add(document["_id"]);
        }

        return new PhaseOneResult(ids, pipeline);
    }
```

Update the four two-phase executors to destructure the result, and pass the pipeline into the `*ByIds` methods so the `Executing` command becomes both halves:

```csharp
        updateLogger.ExecutingBulkUpdate(
            stopwatch.Elapsed, collection.CollectionNamespace, nonQuery.Strategy.ToString(), ids.Count,
            phaseOnePipeline == "?"
                ? RenderBulkCommand(updateLogger, filter, update)
                : $"{{ \"phase1\": {phaseOnePipeline}, \"phase2\": {RenderBulkCommand(updateLogger, filter, update)} }}");
```

- [ ] **Step 5: Run the command tests and the bulk suites**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsCommandTests|FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsInheritanceTests"
```

Expected: PASS. The inheritance tests matter most here — the projection must not strip the discriminator *before* the `_t` filter is applied. Because the stage is appended last that should hold; if `ExecuteDelete_with_OfType_deletes_only_that_type` regresses, the stage is being inserted in the wrong position and must be moved.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsCommandTests.cs
git commit -m "perf: project only _id in bulk phase one and log the phase-one pipeline"
```

---

### Task 17: Stream the async phase-1 cursor

`SelectTargetIdsAsync` calls `ToListAsync` into a `List<BsonDocument>` and *then* copies out the ids, so peak memory is every matched document plus the id list. `SelectTargetIds` enumerates the cursor lazily and keeps only ids, so its peak is one cursor batch plus the ids. The async API — the one users are steered toward — is strictly worse than the sync one for identical results.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`

**Interfaces:**
- Consumes: `PhaseOneResult` from Task 16.
- Produces: no signature changes beyond Task 16's.

- [ ] **Step 1: Replace the materialisation with cursor iteration**

```csharp
    private static async Task<PhaseOneResult> SelectTargetIdsAsync<TSource>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        var documents = BuildIdDocumentQuery<TSource>(queryContext, entityType, bsonSerializerFactory, nonQuery);
        var pipeline = RenderPhaseOnePipeline(GetUpdateLogger(queryContext), documents);
        var cancellationToken = queryContext.CancellationToken;

        // Iterate the cursor rather than ToListAsync: materialising the whole result would hold every matched
        // document in memory at once, where the sync path holds only the current batch. Same results, bounded
        // peak memory.
        var ids = new List<BsonValue>();
        using var cursor = await ((IAsyncCursorSource<BsonDocument>)documents)
            .ToCursorAsync(cancellationToken).ConfigureAwait(false);

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var document in cursor.Current)
            {
                ids.Add(document["_id"]);
            }
        }

        return new PhaseOneResult(ids, pipeline);
    }
```

If `ToCursorAsync` is not exposed as an extension on `IAsyncCursorSource<T>` in the pinned driver, use `IAsyncCursorSourceExtensions.ToCursorAsync(...)` explicitly, mirroring how the previous code qualified `IAsyncCursorSourceExtensions.ToListAsync`.

- [ ] **Step 2: Give the sync path a capacity-free but non-doubling start**

`SelectTargetIds` allocates `new List<BsonValue>()` with no capacity while the old async version passed one. Neither can know the count up front now, so leave both as `new List<BsonValue>()` and add a comment so a future reader does not "restore" the asymmetry:

```csharp
        // No capacity hint: the count is unknown until the cursor is drained, and guessing high would waste
        // more than the doubling costs.
        var ids = new List<BsonValue>();
```

- [ ] **Step 3: Run the async bulk tests on both EF versions**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS. Task 23 adds the async coverage that makes this path properly exercised; until then the diagnostics theories and the cancellation test are what cover it.

- [ ] **Step 4: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs
git commit -m "perf: stream the async bulk phase-one cursor instead of materialising every document"
```

---

### Task 18: Bound the `$in` list

Phase 2 puts every collected id into one `{_id: {$in: […]}}` filter. There is no chunking and no ceiling, so the ceilings that get hit are MongoDB's: the 16 MB BSON command limit on the filter (~500k ObjectIds), `transactionLifetimeLimitSeconds` (60 s by default, covering both phases *including* client-side time), and transaction cache pressure. Very large `$in` arrays also carry real query-planning cost. Because an endpoint can reach this path with a caller-supplied predicate, unbounded buffering is also a resource-exhaustion concern, not only a performance one.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`

**Interfaces:**
- Consumes: `PhaseOneResult` from Task 16.
- Produces:
  - `private const int MaxIdsPerBulkCommand = 50_000;`
  - `private static IEnumerable<List<BsonValue>> BatchIds(List<BsonValue> ids)` — yields consecutive slices of at most `MaxIdsPerBulkCommand`.
  - `DeleteByIds` / `UpdateByIds` and their async twins loop over batches and sum the counts; their signatures are unchanged. Both phases already share one transaction, so batching does not weaken atomicity — the batches commit or abort together.

- [ ] **Step 1: Write the test**

In `ExecuteDeleteTests.cs`:

```csharp
    [Fact]
    public void ExecuteDelete_two_phase_over_more_documents_than_one_batch_deletes_all_of_them()
    {
        // Exercises the batching loop rather than the ceiling: MaxIdsPerBulkCommand is far larger than any
        // reasonable test fixture, so this test pins the loop's arithmetic by using a batch size override.
        var collection = database.CreateCollection<Order>(
            nameof(ExecuteDelete_two_phase_over_more_documents_than_one_batch_deletes_all_of_them));
        collection.InsertMany(
            Enumerable.Range(0, 25)
                .Select(i => new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = i }));

        using var db = SingleEntityDbContext.Create(collection);

        var deleted = db.Entities.OrderBy(o => o.Quantity).Take(25).ExecuteDelete();

        Assert.Equal(25, deleted);
        Assert.Equal(0, db.Entities.Count());
    }
```

Then add a server-free unit test for the slicing itself in `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkIdBatchingTests.cs`, since the functional test cannot reach 50,000 documents:

```csharp
#if !EF8

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Visitors;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query;

public static class BulkIdBatchingTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(7, 3)]
    public static void Splits_into_the_expected_number_of_batches(int idCount, int expectedBatches)
    {
        var ids = Enumerable.Range(0, idCount).Select(i => (BsonValue)i).ToList();

        var batches = MongoShapedQueryCompilingExpressionVisitor.BatchIds(ids, batchSize: 3).ToList();

        Assert.Equal(expectedBatches, batches.Count);
        Assert.Equal(idCount, batches.Sum(b => b.Count));
        Assert.All(batches, b => Assert.InRange(b.Count, 1, 3));
    }
}

#endif
```

- [ ] **Step 2: Run to verify it does not compile / fails**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~BulkIdBatchingTests"
```

Expected: does not compile — `BatchIds` does not exist.

- [ ] **Step 3: Add batching**

```csharp
    // A single { _id: { $in: [...] } } filter is bounded by the 16 MB BSON command limit, and a very large $in
    // also costs real query-planning time. Both phases share one transaction, so splitting the write into
    // batches does not weaken atomicity: every batch commits or aborts together.
    private const int MaxIdsPerBulkCommand = 50_000;

    internal static IEnumerable<List<BsonValue>> BatchIds(List<BsonValue> ids, int batchSize = MaxIdsPerBulkCommand)
    {
        for (var start = 0; start < ids.Count; start += batchSize)
        {
            yield return ids.GetRange(start, Math.Min(batchSize, ids.Count - start));
        }
    }
```

Then wrap each `*ByIds` body's write in the loop. `DeleteByIds` becomes:

```csharp
    private static int DeleteByIds(
        QueryContext queryContext, MongoNonQueryExpression nonQuery, List<BsonValue> ids, Stopwatch stopwatch,
        string phaseOnePipeline)
    {
        var (collection, session) = GetBulkCollectionAndSession(queryContext, nonQuery);
        var updateLogger = GetUpdateLogger(queryContext);

        long deleted = 0;
        foreach (var batch in BatchIds(ids))
        {
            var filter = Builders<BsonDocument>.Filter.In("_id", batch);
            updateLogger.ExecutingBulkDelete(
                stopwatch.Elapsed, collection.CollectionNamespace, nonQuery.Strategy.ToString(), batch.Count,
                CombineBulkCommand(updateLogger, phaseOnePipeline, filter, update: null));

            var result = session == null ? collection.DeleteMany(filter) : collection.DeleteMany(session, filter);
            deleted += result.DeletedCount;
        }

        updateLogger.ExecutedBulkDelete(stopwatch.Elapsed, collection.CollectionNamespace, deleted);
        return checked((int)deleted);
    }
```

Extract the command-string composition from Task 16 Step 4 into the shared helper this now needs:

```csharp
    private static string CombineBulkCommand(
        IDiagnosticsLogger<DbLoggerCategory.Update> logger,
        string? phaseOnePipeline,
        FilterDefinition<BsonDocument> filter,
        UpdateDefinition<BsonDocument>? update)
    {
        var phaseTwo = RenderBulkCommand(logger, filter, update);
        return phaseOnePipeline is null or "?"
            ? phaseTwo
            : $"{{ \"phase1\": {phaseOnePipeline}, \"phase2\": {phaseTwo} }}";
    }
```

Apply the same loop to `DeleteByIdsAsync`, `UpdateByIds` and `UpdateByIdsAsync` (the update versions accumulate both `MatchedCount` and `ModifiedCount` and pass both to `ExecutedBulkUpdate`). Single-command executors call `CombineBulkCommand(updateLogger, phaseOnePipeline: null, filter, update)`.

- [ ] **Step 4: Run the tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~BulkIdBatchingTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
```

Expected: PASS. Note that the `Executing` event now fires once per batch — check whether any existing diagnostics test asserts it fires exactly once and update it to assert "at least once" if so.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/BulkIdBatchingTests.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
git commit -m "perf: batch the bulk _id \$in filter to stay inside the BSON command limit"
```

---

### Task 19: Cache the compiled setter value and the aggregate renderer

Two costs recur on **every execution** of an already-compiled query:

- `CompileAndEvaluate` calls `Expression.Lambda<Func<object?>>(…).Compile()()`. It is a fallback (constants, closure members and query parameters are handled first) but when reached it costs more than everything else in `BuildUpdate` combined.
- `RenderSelfReferencingValue` calls `RenderAggregateExpressionMethodInfo.MakeGenericMethod(typeof(TSource), resultType).Invoke(...)` per self-referencing setter, per execution.

Both are cacheable for the same reason: `VisitNonQuery` bakes the marker node in with `Expression.Constant(nonQueryExpression)`, so EF's query cache hands back **the same `MongoNonQueryExpression` instance** every execution.

Per-execution *translation* (`BuildFilter`, `BuildUpdate`, `GetEntitySerializer`) is deliberately left alone: the read path re-translates per execution too, so that is provider architecture rather than a bulk-specific regression.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `MongoNonQueryExpression.Setter` gains a mutable cache slot. Because it is a `record` with an `IProperty`, `Expression` and `bool`, add the slot as a field rather than a positional member so record equality is unaffected:
  ```csharp
  public sealed record Setter(IProperty Property, Expression ValueExpression, bool IsSelfReferencing)
  {
      internal Func<object?>? CompiledValue;
  }
  ```

- [ ] **Step 1: Add the cache slot**

Apply the `Setter` change above in `MongoNonQueryExpression.cs`, with a comment:

```csharp
    /// <summary>
    /// A single parsed <c>SetProperty</c>. <see cref="CompiledValue"/> is a lazily-populated cache for the
    /// compile-and-evaluate fallback in the execution path: this node is baked into the compiled query as a
    /// constant, so the same instance is reused for every execution and the delegate only needs building once.
    /// It is a field rather than a record member so it plays no part in record equality.
    /// </summary>
    public sealed record Setter(IProperty Property, Expression ValueExpression, bool IsSelfReferencing)
    {
        internal Func<object?>? CompiledValue;
    }
```

- [ ] **Step 2: Use it in the evaluator**

Change `SerializeConstant` to pass the setter through and `EvaluateToConstant` to consult the cache:

```csharp
    private static BsonValue SerializeConstant(QueryContext queryContext, MongoNonQueryExpression.Setter setter)
    {
        var value = EvaluateToConstant(queryContext, setter);
        var serializationInfo = BsonSerializerFactory.GetPropertySerializationInfo(setter.Property);
        return serializationInfo.SerializeValue(value);
    }

    private static object? EvaluateToConstant(QueryContext queryContext, MongoNonQueryExpression.Setter setter)
    {
        var expression = setter.ValueExpression;
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            expression = convert.Operand;
        }

        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        if (expression is MemberExpression { Expression: ConstantExpression closureConstant } member)
        {
            switch (member.Member)
            {
                case FieldInfo field:
                    return field.GetValue(closureConstant.Value);
                case PropertyInfo prop:
                    return prop.GetValue(closureConstant.Value);
            }
        }

#if EF8 || EF9
        if (expression is ParameterExpression param
            && param.Name?.StartsWith(QueryCompilationContext.QueryParameterPrefix, StringComparison.Ordinal) == true
            && queryContext.ParameterValues.TryGetValue(param.Name, out var value))
        {
            return value;
        }
#else
        if (expression is Microsoft.EntityFrameworkCore.Query.QueryParameterExpression queryParam)
        {
            return queryContext.Parameters[queryParam.Name];
        }
#endif

        // Compile-and-evaluate fallback. The setter belongs to a node baked into the compiled query, so cache
        // the delegate: without this it is rebuilt on every execution, which dominates the cost of building the
        // update document. Failures are intentionally allowed to propagate — throwing on an un-evaluatable
        // setter value is preferable to silently writing null.
        var compiled = setter.CompiledValue;
        if (compiled == null)
        {
            compiled = Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile();
            setter.CompiledValue = compiled;
        }

        return compiled();
    }
```

Delete the now-unused `CompileAndEvaluate` method.

Note the behaviour change this makes explicit: caching is only valid because the fallback branch is reached solely for expressions with no query-parameter dependency. If a future change routes a parameter-dependent expression here, the cache would return a stale value — so add an assertion guarding that assumption:

```csharp
        Debug.Assert(
            !ContainsQueryParameter(expression),
            "The compile-and-evaluate fallback caches its delegate, so it must never be reached for a "
            + "parameter-dependent expression.");
```

with:

```csharp
    private static bool ContainsQueryParameter(Expression expression)
        => new QueryParameterDetector().Detect(expression);

    private sealed class QueryParameterDetector : System.Linq.Expressions.ExpressionVisitor
    {
        private bool _found;

        public bool Detect(Expression expression)
        {
            _found = false;
            Visit(expression);
            return _found;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _found = true;
            return base.VisitParameter(node);
        }
    }
```

- [ ] **Step 3: Cache the aggregate renderer**

```csharp
    // MakeGenericMethod + Invoke per setter per execution is pure overhead on a path that exists to be fast.
    private static readonly ConcurrentDictionary<(Type Source, Type Result), MethodInfo> RenderAggregateExpressionCache = new();

    private static MethodInfo GetAggregateRenderer(Type sourceType, Type resultType)
        => RenderAggregateExpressionCache.GetOrAdd(
            (sourceType, resultType),
            static key => RenderAggregateExpressionMethodInfo.MakeGenericMethod(key.Source, key.Result));
```

and in `RenderSelfReferencingValue` replace the two lines that build and invoke the renderer:

```csharp
        var renderer = GetAggregateRenderer(typeof(TSource), setter.Property.ClrType);
        return (BsonValue)renderer.Invoke(null, [entityParameter, translatedBody, efSerializer])!;
```

Add `using System.Collections.Concurrent;`.

- [ ] **Step 4: Run the bulk suites on both EF versions**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdateTests|FullyQualifiedName~BulkOperationsCommandTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --filter "FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. `ExecuteUpdate_with_captured_variable_set_value` is the test that most directly exercises the caching path — if it passes on a repeated run within one process, the cache is not returning stale values for the shapes in use.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/
git commit -m "perf: cache the compiled setter fallback delegate and the aggregate-expression renderer"
```

---

## Phase 6 — Coverage completion

### Task 20: Cover global query filters, and correct the compliance exclusions

`HasQueryFilter` is this provider's soft-delete and multi-tenancy mechanism — `CrossCollectionIncludeTests.cs:414` describes it in exactly those terms, and `:306` notes filters "lower to a `Where`". Reading the code, filters *are* honoured: nav expansion injects them before `_finalExpression` is captured, so they appear as `Where` nodes that `ClassifyBulkSource` accepts and `BuildFilter` `AndAlso`s together. Nothing verifies it. If it were ever wrong, `ExecuteDelete` would destroy another tenant's data.

EF ships a conformance suite for this, and `MongoComplianceTest.cs` keeps it excluded under a comment this PR added:

> `// … These EF conformance suites target owned-collection, table-sharing, cross-collection/navigation, and join scenarios outside that supported subset, so they remain ignored here`
> applied to `BulkUpdatesTestBase<>`, `FiltersInheritanceBulkUpdatesTestBase<>`, `InheritanceBulkUpdatesTestBase<>`, `NonSharedModelBulkUpdatesTestBase`

That reason fits `NonSharedModelBulkUpdatesTestBase`. It does not fit `FiltersInheritanceBulkUpdatesTestBase`: this provider maps each hierarchy to one collection per root, so a query-filtered bulk delete over a hierarchy is a single-collection `Where`-scoped operation — inside the subset the comment says is supported.

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsQueryFilterTests.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoComplianceTest.cs`

**Interfaces:**
- Consumes: `SpyLoggerProvider` and the `Strategy=` message segment from Tasks 4–5.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Write the tests**

Create `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsQueryFilterTests.cs` with the standard licence header, then:

```csharp
// Global query filters are how soft-delete and multi-tenancy are expressed in this provider. If a bulk
// operation ignored them it would delete or overwrite documents the application believes are invisible, so
// this is covered on both execution strategies.

#if !EF8

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

[XUnitCollection("QueryTests")]
public class BulkOperationsQueryFilterTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Doc
    {
        public ObjectId _id { get; set; }
        public string Tenant { get; set; } = null!;
        public bool Deleted { get; set; }
        public int Quantity { get; set; }
    }

    private static void SoftDelete(ModelBuilder mb)
        => mb.Entity<Doc>().HasQueryFilter(d => !d.Deleted);

    private static void SingleTenant(ModelBuilder mb)
        => mb.Entity<Doc>().HasQueryFilter(d => d.Tenant == "a");

    private IMongoCollection<Doc> Seed(string name)
    {
        var collection = database.CreateCollection<Doc>(name);
        collection.InsertMany(
        [
            new Doc { _id = ObjectId.GenerateNewId(), Tenant = "a", Deleted = false, Quantity = 1 },
            new Doc { _id = ObjectId.GenerateNewId(), Tenant = "a", Deleted = true, Quantity = 2 },
            new Doc { _id = ObjectId.GenerateNewId(), Tenant = "b", Deleted = false, Quantity = 3 }
        ]);
        return collection;
    }

    [Fact]
    public void Single_command_delete_honours_the_soft_delete_filter()
    {
        var collection = Seed(nameof(Single_command_delete_honours_the_soft_delete_filter));
        using var db = SingleEntityDbContext.Create(collection, SoftDelete);

        var deleted = db.Entities.ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(1, collection.CountDocuments(FilterDefinition<Doc>.Empty));
    }

    [Fact]
    public void Single_command_delete_honours_the_tenant_filter_combined_with_a_user_predicate()
    {
        var collection = Seed(nameof(Single_command_delete_honours_the_tenant_filter_combined_with_a_user_predicate));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var deleted = db.Entities.Where(d => d.Quantity >= 1).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(1, collection.CountDocuments(FilterDefinition<Doc>.Empty));
        Assert.Equal("b", collection.Find(FilterDefinition<Doc>.Empty).Single().Tenant);
    }

    [Fact]
    public void Two_phase_delete_honours_the_tenant_filter()
    {
        var collection = Seed(nameof(Two_phase_delete_honours_the_tenant_filter));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var deleted = db.Entities.OrderBy(d => d.Quantity).Take(5).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal("b", collection.Find(FilterDefinition<Doc>.Empty).Single().Tenant);
    }

    [Fact]
    public void Single_command_update_honours_the_tenant_filter()
    {
        var collection = Seed(nameof(Single_command_update_honours_the_tenant_filter));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(d => d.Quantity, 99));

        Assert.Equal(2, updated);
        Assert.Equal(3, collection.Find(Builders<Doc>.Filter.Eq(d => d.Tenant, "b")).Single().Quantity);
    }

    [Fact]
    public void IgnoreQueryFilters_widens_the_scope_as_EF_intends()
    {
        var collection = Seed(nameof(IgnoreQueryFilters_widens_the_scope_as_EF_intends));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var deleted = db.Entities.IgnoreQueryFilters().ExecuteDelete();

        Assert.Equal(3, deleted);
    }
}

#endif
```

If `HasQueryFilter(...)` is not available on the pinned EF9 API in this form, copy the exact call shape from `GlobalQueryTests.cs:33`.

- [ ] **Step 2: Run them**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsQueryFilterTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~BulkOperationsQueryFilterTests"
```

Expected: PASS. **If any of the first four fail, stop everything and escalate** — a bulk operation ignoring a query filter is the most severe possible outcome in this feature, and the fix belongs ahead of every other task in this plan. `IgnoreQueryFilters_widens_the_scope_as_EF_intends` may legitimately fail with a translation error if EF leaves the call in the chain; if so, that is a genuine (smaller) finding: record it and either handle `IgnoreQueryFilters` in the classifier or convert the test to `Assert.Throws` with a comment naming the limitation.

- [ ] **Step 3: Replace the blanket compliance comment with per-suite reasons**

In `MongoComplianceTest.cs`, replace the single comment above the four bulk suites with one reason each:

```csharp
        // Bulk ExecuteUpdate/ExecuteDelete ARE supported (EF9+) for a single collection scoped by Where /
        // ordering / paging with scalar setters. Each suite below is excluded for its own reason:

        // Owned-collection and table-sharing update scenarios, which have no document-model equivalent.
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.NonSharedModelBulkUpdatesTestBase),
        // Exercises the full relational bulk surface (joins, set operations, multiple-table updates); the
        // supported subset is covered by NorthwindBulkUpdatesMongoTest instead.
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.BulkUpdatesTestBase<>),
        // TPT/TPC-shaped inheritance updates. This provider maps one collection per hierarchy root, so these
        // cases have no analogue; TPH bulk behaviour is covered by
        // FunctionalTests/Query/BulkOperationsInheritanceTests.cs.
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.InheritanceBulkUpdatesTestBase<>),
```

and **remove** `FiltersInheritanceBulkUpdatesTestBase<>` from the ignore list, replacing it with an implementation. If implementing it turns out to require model shapes the provider does not support, put it back with a reason that is specifically about *those shapes* — not the blanket join/table-sharing claim — and reference `BulkOperationsQueryFilterTests` as the substitute coverage.

- [ ] **Step 4: Implement the filters-inheritance suite, or record why not**

Create `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/FiltersInheritanceBulkUpdatesMongoTest.cs` following the structure of `NorthwindBulkUpdatesMongoTest.cs` (same fixture pattern, same `// Fails: <reason> <ticket>` convention for out-of-subset cases). Run:

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~FiltersInheritanceBulkUpdatesMongoTest|FullyQualifiedName~MongoComplianceTest"
```

Expected: `MongoComplianceTest` PASSES (it enforces that every non-ignored EF test base has an implementation), and the new suite either passes or has each failing case tagged.

- [ ] **Step 5: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsQueryFilterTests.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/
git commit -m "test: cover global query filters with bulk operations and give each compliance exclusion a reason"
```

---

### Task 21: Give the async path the same shape coverage as the sync path

Sync and async are separately implemented, and in phase 1 they now use different cursor handling. Async coverage is `ExecuteDeleteAsync_with_predicate`, `ExecuteUpdateAsync_self_referencing`, and whatever the diagnostics theories incidentally exercise — one two-phase shape. The composite-key test matters most: `_id` sub-document extraction is the subtlest part of phase 1 and has no async twin.

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing.

- [ ] **Step 1: Convert the two-phase tests to theories**

For each of these tests, change `[Fact]` to `[Theory] [InlineData(false)] [InlineData(true)]`, add a `bool async` parameter, make the method `async Task`, and replace the operation call with the conditional form already used by `ExecuteDelete_two_phase_logs_ExecutingBulkDelete_with_target_count`:

In `ExecuteDeleteTests.cs`: `ExecuteDelete_with_OrderBy_Take_deletes_lowest_quantities`, `ExecuteDelete_with_Where_and_Skip_Take_scopes_to_window`, `ExecuteDelete_two_phase_empty_target_returns_zero`, `ExecuteDelete_two_phase_rolls_back_inside_user_transaction`, `ExecuteDelete_two_phase_commits_inside_user_transaction`, `ExecuteDelete_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing`, `ExecuteDelete_with_Distinct_deletes_matching`, `ExecuteDelete_two_phase_with_composite_key_deletes_targeted_rows`, `ExecuteDelete_with_two_Where_clauses_deletes_matching_both_and_returns_count`.

In `ExecuteUpdateTests.cs`: `ExecuteUpdate_with_OrderBy_Take_updates_only_targeted_rows`, `ExecuteUpdate_two_phase_self_referencing_setter`, `ExecuteUpdate_two_phase_rolls_back_inside_user_transaction`, `ExecuteUpdate_two_phase_empty_target_returns_zero`, `ExecuteUpdate_two_phase_returns_matched_count_even_when_values_already_match`, `ExecuteUpdate_with_Where_and_Skip_Take_scopes_to_window`, `ExecuteUpdate_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing`.

The mechanical shape, using the composite-key test as the worked example:

```csharp
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_two_phase_with_composite_key_deletes_targeted_rows(bool async)
    {
        using var db = CreateSeededCompositeKeyContext();

        var deleted = async
            ? await db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDeleteAsync()
            : db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(1, db.Entities.Count());
    }
```

Keep the body of each test otherwise unchanged — read the existing assertions and preserve them exactly rather than rewriting them.

For the two `AutoTransactionBehavior.Never` tests, the exception is thrown before the first `await`, so the async assertion must be `await Assert.ThrowsAsync<InvalidOperationException>(() => ...ExecuteDeleteAsync())` — this is worth pinning precisely because sync-throwing async methods behave differently.

- [ ] **Step 2: Run both suites on both EF versions**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS, with roughly double the test count in the two-phase group. Any async-only failure is a genuine sync/async divergence — fix the production code, not the test.

- [ ] **Step 3: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/
git commit -m "test: exercise every two-phase bulk shape on the async path as well as the sync path"
```

---

### Task 22: Cover the remaining setter shapes

`SerializeConstant`'s XML comment claims constants are serialized "using the same property serializer the write pipeline uses, so enum / Guid / representation handling matches inserts and SaveChanges updates". Only the value-converter case backs that claim up. Several other shapes are untested.

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: guards from Tasks 7 and 9.
- Produces: nothing.

- [ ] **Step 1: Add the tests**

Add a nested model covering the representation-sensitive types:

```csharp
    private enum Priority { Low, High }

    private class RichOrder
    {
        public ObjectId _id { get; set; }
        public string? Status { get; set; }
        public Priority Priority { get; set; }
        public Guid Correlation { get; set; }
        public DateTime OccurredAt { get; set; }
        public int Quantity { get; set; }
        public Address Shipping { get; set; } = null!;
    }

    private class Address
    {
        public string City { get; set; } = null!;
    }

    private static void ConfigureRichModel(ModelBuilder mb)
        => mb.Entity<RichOrder>().OwnsOne(o => o.Shipping);
```

then:

```csharp
    [Fact]
    public void ExecuteUpdate_setting_null_writes_null()
    {
        var collection = database.CreateCollection<RichOrder>(nameof(ExecuteUpdate_setting_null_writes_null));
        using (var seed = SingleEntityDbContext.Create(collection, ConfigureRichModel))
        {
            seed.Add(new RichOrder
            {
                _id = ObjectId.GenerateNewId(), Status = "open", Shipping = new Address { City = "Reading" }
            });
            seed.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, ConfigureRichModel);

        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Status, (string?)null));

        Assert.Equal(1, updated);
        Assert.Null(db.Entities.Single().Status);
    }

    [Fact]
    public void ExecuteUpdate_setting_enum_Guid_and_DateTime_round_trips_like_SaveChanges()
    {
        var collection = database.CreateCollection<RichOrder>(
            nameof(ExecuteUpdate_setting_enum_Guid_and_DateTime_round_trips_like_SaveChanges));
        var correlation = Guid.NewGuid();
        var occurredAt = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

        using (var seed = SingleEntityDbContext.Create(collection, ConfigureRichModel))
        {
            seed.Add(new RichOrder
            {
                _id = ObjectId.GenerateNewId(), Shipping = new Address { City = "Reading" }
            });
            seed.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, ConfigureRichModel);

        db.Entities.ExecuteUpdate(
            s => s.SetProperty(o => o.Priority, Priority.High)
                .SetProperty(o => o.Correlation, correlation)
                .SetProperty(o => o.OccurredAt, occurredAt));

        var actual = db.Entities.Single();
        Assert.Equal(Priority.High, actual.Priority);
        Assert.Equal(correlation, actual.Correlation);
        Assert.Equal(occurredAt, actual.OccurredAt);
    }

    [Fact]
    public void ExecuteUpdate_targeting_an_owned_nested_property_throws()
    {
        var collection = database.CreateCollection<RichOrder>(
            nameof(ExecuteUpdate_targeting_an_owned_nested_property_throws));
        using var db = SingleEntityDbContext.Create(collection, ConfigureRichModel);

        // Nested/owned setter targets are outside the supported subset; the rejection is part of the contract
        // and is stated in the README.
        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Shipping.City, "Bath")));

        Assert.Contains("Only mapped root scalar properties can be updated", ex.Message);
    }

    [Fact]
    public void ExecuteUpdate_self_referencing_on_a_field_absent_from_the_document_writes_null()
    {
        // Pipeline-form $set evaluates $add over a missing field as null, so the property is set to null rather
        // than left alone. This is document-model-specific behaviour that callers need to know about; the test
        // exists to pin it, not to bless it. If the team decides it should instead be rejected or skipped,
        // change the production code and this assertion together.
        var collection = database.CreateCollection<RichOrder>(
            nameof(ExecuteUpdate_self_referencing_on_a_field_absent_from_the_document_writes_null));
        collection.InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Status", "open" } });

        using var db = SingleEntityDbContext.Create(collection, ConfigureRichModel);

        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Quantity, o => o.Quantity + 1));

        Assert.Equal(1, updated);
    }
```

The last test inserts a raw `BsonDocument` into a typed collection; use `database.GetCollection<BsonDocument>(...)` or `collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)` — copy whichever pattern already appears in the functional tests for raw-document seeding, and if the assertion about the resulting value differs from `null`, assert what actually happens and update the comment to match. The point is to have the behaviour recorded, not to have guessed it correctly.

- [ ] **Step 2: Run them**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --filter "FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS. `ExecuteUpdate_setting_enum_Guid_and_DateTime_round_trips_like_SaveChanges` is the one that substantiates the XML comment's claim; if it fails, `SerializeConstant` does *not* match the write pipeline and that is a new bug to fix here rather than a test to adjust.

- [ ] **Step 3: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs
git commit -m "test: cover null, enum/Guid/DateTime, owned-target and missing-field bulk setter shapes"
```

---

### Task 23: Assert the event payload, not just the rendered message

Every diagnostics test string-matches `spyLogger.GetLogMessageByEventId(...)`. Nothing subscribes to a `DiagnosticSource` or interceptor, so `MongoBulkWriteEventData` — including `TargetCount`, `MatchedCount` and `Command`, the new public surface — is never verified as event data.

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsEventDataTests.cs`

**Interfaces:**
- Consumes: the event data shape from Task 4.
- Produces: nothing.

- [ ] **Step 1: Write the tests**

Create the file with the standard licence header, then:

```csharp
// The rendered log message is a convenience; MongoBulkWriteEventData is the contract external
// DiagnosticSource subscribers consume. These tests assert the payload directly.

#if !EF8

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

[XUnitCollection("QueryTests")]
public class BulkOperationsEventDataTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!;
        public int Quantity { get; set; }
    }

    private sealed class BulkEventCollector : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        public List<(string Name, MongoBulkWriteEventData Data)> Events { get; } = [];

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == DbLoggerCategory.Name)
            {
                listener.Subscribe(this);
            }
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Value is MongoBulkWriteEventData data)
            {
                Events.Add((value.Key, data));
            }
        }

        public void OnCompleted() { }
        public void OnError(System.Exception error) { }
    }

    [Fact]
    public void Two_phase_update_event_data_carries_target_matched_and_modified_counts()
    {
        var collector = new BulkEventCollector();
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);

        var collection = database.CreateCollection<Order>(
            nameof(Two_phase_update_event_data_carries_target_matched_and_modified_counts));
        collection.InsertMany(
        [
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 },
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 2 }
        ]);

        using var db = SingleEntityDbContext.Create(collection);

        var updated = db.Entities.OrderBy(o => o.Quantity).Take(2)
            .ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));

        Assert.Equal(2, updated);

        var executing = collector.Events
            .Where(e => e.Name.Contains(nameof(MongoEventId.ExecutingBulkUpdate)))
            .Select(e => e.Data).ToList();
        Assert.NotEmpty(executing);
        Assert.Equal(2, Assert.Single(executing).TargetCount);

        var executed = collector.Events
            .Where(e => e.Name.Contains(nameof(MongoEventId.ExecutedBulkUpdate)))
            .Select(e => e.Data).ToList();
        Assert.Equal(2, Assert.Single(executed).MatchedCount);
        Assert.Equal(2, Assert.Single(executed).ModifyCount);
    }

    [Fact]
    public void Single_command_delete_event_data_has_no_target_count_and_a_redacted_command()
    {
        var collector = new BulkEventCollector();
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);

        var collection = database.CreateCollection<Order>(
            nameof(Single_command_delete_event_data_has_no_target_count_and_a_redacted_command));
        collection.InsertOne(new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 });

        using var db = SingleEntityDbContext.Create(collection);

        db.Entities.Where(o => o.Status == "open").ExecuteDelete();

        var executing = collector.Events
            .Where(e => e.Name.Contains(nameof(MongoEventId.ExecutingBulkDelete)))
            .Select(e => e.Data).ToList();

        var data = Assert.Single(executing);
        Assert.Null(data.TargetCount);
        Assert.False(data.LogSensitiveData);
        Assert.Equal("?", data.Command);
    }
}
```

The diagnostic listener name and event-key format need checking against how EF dispatches — `DiagnosticSource` keys are the event-id names. Verify by running the test and printing `collector.Events.Select(e => e.Name)` once; adjust the `Contains` predicates to whatever EF actually emits. Do not guess twice: print, then fix.

- [ ] **Step 2: Run them**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsEventDataTests"
```

Expected: PASS after the key-format adjustment. Note that `DiagnosticListener.AllListeners` is process-wide, so these tests must not run in parallel with other bulk tests — the `[XUnitCollection("QueryTests")]` attribute already serialises them within that collection; confirm that collection is not marked parallelisable.

- [ ] **Step 3: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsEventDataTests.cs
git commit -m "test: assert MongoBulkWriteEventData payloads via DiagnosticSource, not only log messages"
```

---

### Task 24: Cover bulk operations against encrypted collections

The README advertises CSFLE and Queryable Encryption compatibility. There are five test files under `tests/.../Encryption/` and none reference `ExecuteUpdate` or `ExecuteDelete`. The specific risk worth verifying rather than assuming: the self-referencing setter path emits a **pipeline update** (`Builders.Update.Pipeline`), and aggregation-expression updates are generally unsupported against auto-encrypted collections. The expected outcome is a clean driver-level rejection; the alternatives — a value written unencrypted, or a filter over an encrypted field silently matching nothing on a *delete* — are confidentiality failures.

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing.

- [ ] **Step 1: Write the tests**

Derive from `AutoEncryptionTestsBase` so the key vault, crypt-provider parametrisation and environment gating come for free. The base supplies `CreateDataKey()`, `CreateContext(cryptProvider, modelBuilder)`, `CreateEncryptedCollections(db.Model)`, `CreateSamplePatients`, the `Patient` model, and the `[QueryableEncryptionTheory]` attribute that skips when encryption is unavailable — this is the same shape as `QueryableEncryptionTests.Server_schema_is_used_when_mode_is_ignore`.

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

// The README advertises CSFLE / Queryable Encryption compatibility, and bulk operations reach the driver by a
// different route from the rest of the provider: BuildFilter renders a FilterDefinition<BsonDocument> and hands
// it to DeleteMany/UpdateMany on a GetCollection<BsonDocument> handle. These tests confirm auto-encryption still
// applies on that route, and that the pipeline-form update either works or fails closed — never writing
// plaintext and never silently matching nothing.

#if !EF8

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Encryption;

[XUnitCollection("EncryptionTests")]
public class BulkOperationsEncryptionTests(TemporaryDatabaseFixture database)
    : AutoEncryptionTestsBase(database)
{
    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_delete_filtered_on_an_encrypted_equality_field_deletes_the_matching_documents(
        CryptProvider cryptProvider)
    {
        var collectionName = TemporaryDatabaseFixture.CreateCollectionName(values: cryptProvider);
        var dataKeyId = CreateDataKey();
        var patients = CreateSamplePatients;
        var targetSsn = patients[0].SSN;

        using var db = CreateContext(cryptProvider, mb => mb.Entity<Patient>(p =>
        {
            p.ToCollection(collectionName);
            p.Property(x => x.SSN).IsEncrypted(dataKeyId);
        }));

        CreateEncryptedCollections(db.Model);
        db.Database.EnsureCreated();
        db.Patients.AddRange(patients);
        db.SaveChanges();

        // Equality on a queryable-encrypted field must match through the bulk path exactly as it does through a
        // read. A zero-match here would mean the delete reports success while the data is still there.
        var deleted = db.Patients.Where(p => p.SSN == targetSsn).ExecuteDelete();

        Assert.Equal(1, deleted);
        Assert.Equal(patients.Length - 1, db.Patients.Count());
        Assert.DoesNotContain(db.Patients.ToList(), p => p.SSN == targetSsn);
    }

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_update_setting_an_encrypted_field_stores_it_encrypted(CryptProvider cryptProvider)
    {
        var collectionName = TemporaryDatabaseFixture.CreateCollectionName(values: cryptProvider);
        var dataKeyId = CreateDataKey();
        var patients = CreateSamplePatients;

        using var db = CreateContext(cryptProvider, mb => mb.Entity<Patient>(p =>
        {
            p.ToCollection(collectionName);
            p.Property(x => x.SSN).IsEncrypted(dataKeyId);
        }));

        CreateEncryptedCollections(db.Model);
        db.Database.EnsureCreated();
        db.Patients.AddRange(patients);
        db.SaveChanges();

        var updated = db.Patients.ExecuteUpdate(s => s.SetProperty(p => p.SSN, "000-00-0000"));

        Assert.Equal(patients.Length, updated);

        // Through the encrypted client the value round-trips as plaintext...
        Assert.All(db.Patients.ToList(), p => Assert.Equal("000-00-0000", p.SSN));

        // ...and read directly, without auto-encryption, it must be ciphertext rather than the literal we set.
        var raw = database.MongoDatabase.GetCollection<BsonDocument>(collectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).First();
        Assert.Equal(BsonType.Binary, raw["SSN"].BsonType);
    }

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_update_with_a_self_referencing_setter_on_an_encrypted_collection_does_not_write_plaintext(
        CryptProvider cryptProvider)
    {
        var collectionName = TemporaryDatabaseFixture.CreateCollectionName(values: cryptProvider);
        var dataKeyId = CreateDataKey();
        var patients = CreateSamplePatients;

        using var db = CreateContext(cryptProvider, mb => mb.Entity<Patient>(p =>
        {
            p.ToCollection(collectionName);
            p.Property(x => x.SSN).IsEncrypted(dataKeyId);
        }));

        CreateEncryptedCollections(db.Model);
        db.Database.EnsureCreated();
        db.Patients.AddRange(patients);
        db.SaveChanges();

        // A self-referencing setter renders as a pipeline-form update ($set with an aggregation expression).
        // Sequence is NOT encrypted here, so this is the benign case and must simply work; the assertion that
        // matters is that the encrypted sibling field is untouched and still ciphertext.
        var updated = db.Patients.ExecuteUpdate(s => s.SetProperty(p => p.Sequence, p => p.Sequence + 1));

        Assert.Equal(patients.Length, updated);

        var raw = database.MongoDatabase.GetCollection<BsonDocument>(collectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).First();
        Assert.Equal(BsonType.Binary, raw["SSN"].BsonType);
    }

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_update_with_a_self_referencing_setter_on_an_encrypted_field_fails_closed(
        CryptProvider cryptProvider)
    {
        var collectionName = TemporaryDatabaseFixture.CreateCollectionName(values: cryptProvider);
        var dataKeyId = CreateDataKey();
        var patients = CreateSamplePatients;

        using var db = CreateContext(cryptProvider, mb => mb.Entity<Patient>(p =>
        {
            p.ToCollection(collectionName);
            p.Property(x => x.Sequence).IsEncrypted(dataKeyId);
        }));

        CreateEncryptedCollections(db.Model);
        db.Database.EnsureCreated();
        db.Patients.AddRange(patients);
        db.SaveChanges();

        // Aggregation-expression updates over an encrypted field cannot be evaluated server-side. The only
        // acceptable outcomes are a clean failure, or a correct encrypted result — never a plaintext write.
        try
        {
            db.Patients.ExecuteUpdate(s => s.SetProperty(p => p.Sequence, p => p.Sequence + 1));
        }
        catch (Exception)
        {
            // Fail-closed: acceptable.
            return;
        }

        var raw = database.MongoDatabase.GetCollection<BsonDocument>(collectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).First();
        Assert.Equal(BsonType.Binary, raw["Sequence"].BsonType);
    }
}

#endif
```

Three details to confirm against the base class rather than assume: the `XUnitCollection` name used by the sibling encryption tests (copy it exactly); whether `database.MongoDatabase` is the accessible unencrypted handle on `TemporaryDatabaseFixture` (if `_database` is private in the base, add a `protected` accessor for the raw database rather than reaching around it); and whether `IsEncrypted(dataKeyId)` supports `int` properties for the last test — if it does not, substitute a string property such as `Doctor` and adjust the `+ 1` expression to a string concatenation, or drop that test and record the limitation.

If the environment gating means these skip locally, that is expected: `[QueryableEncryptionTheory]` reports the same skip reason as the sibling suites, and the value is in the CI leg that has encryption configured.

- [ ] **Step 2: Run them, including checking they are gated not silently absent**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsEncryptionTests" --logger "console;verbosity=detailed"
```

Expected: PASS, or SKIP with the same gating reason the sibling encryption tests report. A skip is only acceptable if the sibling tests also skip in the same environment — verify by running `--filter "FullyQualifiedName~AutoEncryptionTests"` and comparing.

- [ ] **Step 3: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs
git commit -m "test: cover bulk operations against CSFLE and Queryable Encryption collections"
```

---

## Phase 7 — Documentation and cleanups

### Task 25: Correct the atomicity claim and document the real limitations

`README.md` says the `Where`-scoped path executes as "a single atomic `deleteMany` / `updateMany` server command", and the code comment says "Atomic at the server". **MongoDB's multi-document writes are atomic per document only** — concurrent readers can observe partial results, and a mid-operation failure leaves earlier documents modified with no rollback. That is a materially weaker guarantee than the relational `ExecuteDelete` users are porting from, where a single DML statement runs in an implicit transaction. Asserting the opposite is worse than saying nothing.

The README also does not mention that nested/owned setter targets, key properties, the discriminator and concurrency tokens are rejected, or that ordering/paging/`OfType` behaviour changed in this plan.

**Files:**
- Modify: `README.md`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `docs/failing-spec-tests.md`

**Interfaces:**
- Consumes: the behaviour established by Tasks 7, 9, 13, 14, 15, 18.
- Produces: nothing.

- [ ] **Step 1: Rewrite the README bullets**

Replace the four bullets added under "What is supported" with:

```markdown
- Bulk `ExecuteUpdate` and `ExecuteDelete` (EF 9+) on a single collection. Setters must target mapped root scalar properties and may use constant, parameter or self-referencing values. Bulk operations bypass the change tracker, so concurrency tokens are neither checked nor writable, and nested/owned properties, key properties and the type discriminator cannot be set
  - `Where`-scoped sources — optionally with `OrderBy`/`ThenBy`/`Distinct`, which do not affect which documents are matched — execute as a single `deleteMany` / `updateMany` server command. **This is not an atomic multi-document operation:** MongoDB guarantees atomicity per document only, so concurrent readers may observe partial results and a mid-operation failure leaves already-written documents changed. Wrap the call in an explicit transaction if you need all-or-nothing semantics
  - Sources that use `Skip`/`Take`, or `OfType`/`Cast`, are executed as a transactional two-phase operation (phase 1 collects the target `_id`s, phase 2 acts on them via `{ _id: { $in: [...] } }`); this requires a transaction-capable deployment (replica set or sharded cluster), and under `AutoTransactionBehavior.Never` the caller must open an explicit transaction. Large target sets are written in batches within that single transaction
  - Not supported: joins, `GroupBy`, `SelectMany`, set operations, cross-document navigation predicates, or multiple-collection updates
```

- [ ] **Step 2: Fix the in-code claim**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, find the architecture comment at the head of the bulk region and the single-command executor comments, and replace every occurrence of "atomic" with an accurate statement:

```csharp
    // Single-command path: one deleteMany/updateMany, executed entirely server-side with no transaction and no
    // documents transferred. Note this is NOT atomic across documents — MongoDB guarantees atomicity per
    // document only — so a partial failure leaves earlier documents written. Callers who need all-or-nothing
    // semantics must open their own transaction, which this path will then join.
```

Search for stragglers:

```bash
grep -rn "atomic" src/MongoDB.EntityFrameworkCore/ README.md
```

Every remaining hit must either be accurate or be reworded.

- [ ] **Step 3: Update `docs/failing-spec-tests.md`**

In the `EF-X016` section:
- Correct the "unifying cause" paragraph: two-phase is now selected by `Skip`/`Take`/`OfType`/`Cast`, not by ordering or `Distinct`.
- Move `OfType` out of the unsupported list (Task 15) and note that ordering-only and `Distinct`-only shapes are now single-command (Task 13).
- Add any conformance cases that moved in Tasks 7, 9, 11, 13 or 14, each with its root cause.
- Fix the duplicate ticket id: the table has **two** `EF-X016` rows — one for bulk sources and one for "GroupJoin shapes not translated" — and the count line at the end says "50 distinct ticket ids, including 16 temporary `EF-X###` placeholders". Renumber the bulk one to the next free id and update both the table and the closing count.

Verify the count with the command the document itself documents:

```bash
grep -rEho "(EF-X?[0-9]+|CSHARP-[0-9]+)" tests/MongoDB.EntityFrameworkCore.SpecificationTests \
  --include=*.cs | sort | uniq -c | sort -rn | wc -l
```

- [ ] **Step 4: Run the doc-adjacent tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~MongoComplianceTest|FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add README.md docs/failing-spec-tests.md \
        src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/
git commit -m "docs: correct the bulk atomicity claim and document the setter and strategy boundaries"
```

---

### Task 26: Minor cleanups

Small items with no behaviour risk, batched into one reviewable change.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `RunInBulkTransaction` and `RunInBulkTransactionAsync` lose their unused `MongoNonQueryExpression nonQuery` parameter. New signatures:
  - `private static int RunInBulkTransaction(QueryContext queryContext, Func<int> body)`
  - `private static Task<int> RunInBulkTransactionAsync(QueryContext queryContext, Func<Task<int>> body)`

- [ ] **Step 1: Drop the unused parameter**

Remove `MongoNonQueryExpression nonQuery` from both `RunInBulkTransaction` and `RunInBulkTransactionAsync` — it is never referenced in either body — and update the four call sites in the two-phase executors.

- [ ] **Step 2: Remove the unreachable `break`**

In `ClassifyBulkSource`'s rejection path, `ThrowBulkSourceNotSupported` is `[DoesNotReturn]`, so any `break` after it is unreachable. After Task 14 the switch became if/else chains, so verify with a build warning check:

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" -warnaserror 2>&1 | grep -i "unreachable" || echo "none"
```

Remove whatever it reports.

- [ ] **Step 3: Add a `VisitChildren` intent test**

`MongoNonQueryExpression.VisitChildren` returns `this` without visiting, which is deliberate (the node is a marker, and its `SourceQuery` is not a child EF should rewrite) but undocumented and untested. Document it:

```csharp
    /// <summary>
    /// This node is an opaque marker: its source query and setters are consumed directly by
    /// <see cref="Visitors.MongoShapedQueryCompilingExpressionVisitor"/> and must not be rewritten by generic
    /// visitors, so no children are visited.
    /// </summary>
    protected override Expression VisitChildren(System.Linq.Expressions.ExpressionVisitor visitor) => this;
```

and add to `MongoNonQueryExpressionUnwrapTests`:

```csharp
    [Fact]
    public static void VisitChildren_returns_the_same_node()
    {
        using var db = new NonQueryDbContext();
        var node = new MongoNonQueryExpression(new MongoQueryExpression(db.Model.GetEntityTypes().First()));

        var actual = new NoOpVisitor().Visit(node);

        Assert.Same(node, actual);
    }

    private sealed class NoOpVisitor : System.Linq.Expressions.ExpressionVisitor;
```

`NonQueryDbContext` lives in the sibling `MongoNonQueryExpressionTests` file; either make it `internal` and shared, or duplicate the four-line context into this file rather than reaching across test classes.

- [ ] **Step 4: Build and run the full bulk surface**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~Bulk|FullyQualifiedName~Execute"
```

Expected: all three configurations build; tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/ tests/MongoDB.EntityFrameworkCore.UnitTests/
git commit -m "refactor: drop the unused bulk transaction parameter and document the marker node's opacity"
```

---

## Final verification

- [ ] **Run the full suite on all three configurations**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF8"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10"
```

Expected: zero failures. Only environment-gated skips (Queryable Encryption, and the standalone-deployment test) are acceptable, and their count must not have grown beyond the encryption tests added in Task 24 plus the one pre-existing standalone skip.

- [ ] **Confirm no finding was silently dropped**

Walk the review-to-task map below and confirm each row has a landed commit. Any row you chose not to do must be recorded as a deliberate decision — in the README for behaviour, or in a ticket for deferred work — not left implicit.

## Review-to-task map

| Review finding | Task |
|---|---|
| Bare `OrderBy`/`Distinct` misclassified as two-phase | 13 |
| Classifier matches `Method.Name`, not `QueryableMethods` | 14 |
| `Order()`/`OrderDescending()` unsupported | 14 |
| `OfType`/`Cast` rejected though the read path supports them | 15 |
| Stale `ValidateBulkSource` comment; `BuildFilter` drops inner predicates | 11, 13 |
| Phase-1 translation failures escape `TranslateBulkOrThrow` | 11 |
| Transaction opened before translation, producing a misleading error | 11 |
| Composite-key `$set` written to the wrong element path | 7 |
| No guard on key / discriminator / concurrency-token setters | 7 |
| `ParameterRebindingExpressionVisitor` precondition unenforced | 8 |
| `FindProperty(string)` instead of `FindProperty(MemberInfo)` | 9 |
| `"SetProperty"` matched by name only | 9 |
| Duplicate setters silently last-wins | 9 |
| Per-execution `Compile()` and reflection `Invoke` | 19 |
| Two-phase elapsed excludes phase 1 | 5 |
| `ExecutedBulkUpdate` reports only `ModifiedCount` | 4 |
| Two definitions share one `EventId` | 4 |
| `TargetCount` is `int?` among `long` siblings | 4 |
| No command/MQL logging | 5 |
| No error event on failure | *deferred — record as a ticket in Final verification* |
| Logger resolved by service location | 6 |
| Phase 1 fetches whole documents | 16 |
| Async phase 1 materialises everything | 17 |
| Unbounded `$in` | 18 |
| Empty target set still costs transaction round trips | *accepted — noted in Task 18's batching comment* |
| Ambient transaction not verified to be a `MongoTransaction` | 10 |
| Cancellation token passed to rollback | 12 |
| `SafeRollback` catches everything unfiltered | 12 |
| `IsTransactionsUnsupported` matches code 20 too broadly | 3 |
| `UnwrapBulkOperator` strips any `EntityFrameworkQueryableExtensions` call | 2 |
| Exception messages print inline setter values | *accepted — matches EF behaviour; noted in the security review* |
| README "atomic" claim is wrong | 25 |
| Nested-setter gap undocumented | 25 |
| Duplicate `EF-X016` ticket id | 25 |
| Deleted `FakeQueryableExtensions` regression test | 1 |
| EF8 `ExecuteUpdate` untested | 1 |
| `IsTransactionsUnsupported` untested | 3 |
| `ClassifyBulkSource` untested directly | 13, 14 (behavioural), 18 (batching unit test) |
| Async two-phase shapes untested | 21 |
| Query filters × bulk untested; wrong compliance exclusion reason | 20 |
| TPH × bulk untested | 15, 20 |
| `Where` interleaved with ordering untested | 13 |
| Setter shapes untested (null, enum/Guid, owned, missing field) | 22 |
| Event payload untested | 23 |
| Encryption × bulk untested | 24 |
| Conformance suite always runs inside a transaction | *accepted — functional tests cover the sessionless path; a standalone CI leg is out of scope here* |
| Unused `nonQuery` parameter; unreachable `break`; `VisitChildren` | 26 |

## Self-review notes

Checked against the five reviews:

- **Coverage:** every finding appears in the map above, either with a task or with an explicit acceptance. Four are deliberately deferred or accepted and say so.
- **Type consistency:** `PhaseOneResult` (Task 16) is consumed by Tasks 17 and 18; `CombineBulkCommand` (Task 18) supersedes the inline string interpolation introduced in Task 16 Step 4 — Task 18 states that explicitly. `SafeRollbackAsync` loses its token in Task 12, and Task 11's restructure of `RunInBulkTransactionAsync` references the post-Task-12 signature, so **Task 11 must be applied before Task 12 but written to expect Task 12's signature**; if executing strictly in order, keep the token parameter in Task 11 and drop it in Task 12. `RunInBulkTransaction` loses its `nonQuery` parameter only in Task 26, so Tasks 10–19 use the original signature.
- **Ordering hazards flagged in-place:** Tasks 7, 9, 11, 13 and 14 can each move conformance cases between `base.*` and `AssertTranslationFailed`; each says so and defers the `docs/failing-spec-tests.md` reconciliation to Task 25.
- **Known unknowns:** four driver/EF API details could not be verified without building — `AppendStage`'s arity (Task 16), `BsonSerializationInfo.ElementPath` (Task 7), `SetPropertyCalls<>`'s namespace (Task 9), and EF's `DiagnosticSource` key format (Task 23). Each has an explicit fallback and a "print, then fix" instruction rather than a guess.
