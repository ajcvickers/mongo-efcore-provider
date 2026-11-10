# Task 26: Minor cleanups — report

## Step 1 — unused `nonQuery` parameter: already done

Read `src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs` (not the Query visitors the
brief's Files list points at — that list is stale). Confirmed:

- `private static int RunInBulkTransaction(QueryContext queryContext, Func<int> body)` — line 530.
- `private static async Task<int> RunInBulkTransactionAsync(QueryContext queryContext, Func<Task<int>> body)` — line 595.

Neither takes a `MongoNonQueryExpression nonQuery` parameter. All four call sites pass exactly two arguments:
`ExecuteTwoPhaseDelete` (line 157), `ExecuteTwoPhaseDeleteAsync` (line 168), `ExecuteTwoPhaseUpdate` (line 195),
`ExecuteTwoPhaseUpdateAsync` (line 216) — each calls `RunInBulkTransaction(queryContext, () => {...})` /
`RunInBulkTransactionAsync(queryContext, async () => {...})` with no third argument. There is no reference to
`MongoNonQueryExpression` anywhere in `MongoBulkOperationExecutor.cs` (`grep` confirms). Task 11's restructure
already dropped it. **Changed nothing for Step 1.**

## Step 2 — unreachable `break`: already done

Read `ClassifyBulkSource` in `MongoQueryableMethodTranslatingExpressionVisitor.cs`. Both
`ThrowBulkSourceNotSupported` call sites (inside the `while (expression is MethodCallExpression ...)` loop, one
for `method.DeclaringType != typeof(Queryable)`, one in the final `else` for unrecognized/rejected operators
like `Cast`) sit inside an if/else chain with normal fall-through to `expression = methodCallExpression.Arguments[0];`
— no `break` anywhere, because there is no `switch` left post-Task-14. Ran the brief's exact check:

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" -warnaserror 2>&1 | grep -i "unreachable" || echo "none"
```

Result: build succeeded, 0 warnings, 0 errors — no "unreachable" hit (the `grep` found nothing, confirmed by
inspecting full build output directly rather than trusting the pipe). **Changed nothing for Step 2.**

## Step 3 — `VisitChildren` documentation + test

### Documentation added

`src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs:172` now reads:

```csharp
/// <summary>
/// This node is an opaque marker: its source query and setters are consumed directly by
/// <see cref="Visitors.MongoShapedQueryCompilingExpressionVisitor"/> and must not be rewritten by generic
/// visitors, so no children are visited.
/// </summary>
protected override Expression VisitChildren(System.Linq.Expressions.ExpressionVisitor visitor) => this;
```

Exactly the brief's text — verified true against the actual codebase: `MongoShapedQueryCompilingExpressionVisitor`
is indeed the consumer of `SourceQuery`/`Setters` for this node type.

### Test added, and why it is not vacuous

Added to `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs`
(inside the file's existing `#if !EF8` guard, since `MongoNonQueryExpression` is only ever instantiated on
EF9+):

```csharp
[Fact]
public static void VisitChildren_returns_the_same_node()
{
    using var db = new NonQueryDbContext();
    var node = new MongoNonQueryExpression(new MongoQueryExpression(db.Model.GetEntityTypes().First()));

    var actual = new NoOpVisitor().Visit(node);

    Assert.Same(node, actual);
}

private sealed class NoOpVisitor : ExpressionVisitor;
```

Constructor shape verified against the real type: `MongoNonQueryExpression(MongoQueryExpression sourceQuery,
BulkStrategy strategy = BulkStrategy.SingleCommand)` (the Delete overload) exists unchanged, and
`MongoQueryExpression(IEntityType entityType)` exists unchanged — Task 19's `Setter` record→class change did
not touch these constructors, so the brief's snippet is accurate as written and matches the pattern the sibling
`MongoNonQueryExpressionTests.cs` already uses (`new MongoQueryExpression(entityType)`).

**Why the test is not vacuous (empirically verified, not just reasoned about):** I built a standalone
throwaway console app (in the scratchpad, not committed) that models the actual shape:
`MongoQueryExpression` is itself an `Expression` with `NodeType == Extension`, `CanReduce == false`, and *no*
`VisitChildren` override — meaning the base `Expression.VisitChildren` implementation applies to it, which
throws when `CanReduce` is false and the node hasn't overridden `VisitChildren`. I reproduced this shape with
stand-in classes (`ChildNode` mirroring `MongoQueryExpression`, `MarkerRecurses` mirroring a hypothetical
"fixed" `MongoNonQueryExpression.VisitChildren` that calls `visitor.Visit(Source)` instead of returning `this`)
and ran it:

```
No-recurse version: same instance = True
Recursing version threw: ArgumentException: must be reducible node
```

So: today's implementation (return `this`, no recursion) passes trivially. If `VisitChildren` were changed to
actually visit `SourceQuery` — the natural, "more correct-looking" fix a well-meaning refactor might apply —
`ExpressionVisitor.Visit(SourceQuery)` throws `ArgumentException: must be reducible node` before the method
even returns, because `MongoQueryExpression` is non-reducible and has no `VisitChildren` override of its own.
The test would fail (via an uncaught exception, not silently) the moment someone "fixed" the marker to recurse.
This also explains *why* the deliberate no-op is not just a style choice but a practical necessity given
`MongoQueryExpression`'s shape — a fact worth knowing even though it isn't part of the documented rationale
(the documented rationale is the semantic one: the node is a marker and must not be rewritten).

I also reasoned through, but did not need to further verify, a second mutation shape (a `VisitChildren` that
rebuilds a *new* `MongoNonQueryExpression` from visited setters without a same-node fast path): that would fail
`Assert.Same` directly on reference-inequality, independent of the exception path above. Either plausible
"fix" is caught.

### Test context option chosen

`MongoNonQueryExpressionTests.cs`'s `NonQueryDbContext` is a `private` nested class inside a `public static
class` — inaccessible from the sibling file without either widening it to `internal` (awkward: the type would
then need to be referenced as `MongoNonQueryExpressionTests.NonQueryDbContext`, reaching across an unrelated
test class for a four-line helper) or duplicating it. Per the brief's own preference order and Correction 3's
guidance, I **duplicated** the four-line `DbContext` subclass into `MongoNonQueryExpressionUnwrapTests.cs`
(reusing that file's own existing `Product` class, which already has the right shape — `ObjectId _id` +
`string Name`). Left a one-line comment explaining the duplication so a future reader doesn't "fix" it into a
cross-class reference. `MongoNonQueryExpressionTests.cs` itself is untouched.

## Step 4 — build and test

All three configurations build clean, 0 warnings / 0 errors:

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"   → Build succeeded. 0 Warning(s). 0 Error(s).
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"   → Build succeeded. 0 Warning(s). 0 Error(s).
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"  → Build succeeded. 0 Warning(s). 0 Error(s).
```

Brief's Step 4 test command (EF10):

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~Bulk|FullyQualifiedName~Execute"
```

Result: UnitTests 18/18 passed; FunctionalTests 124 passed, 5 skipped (0 failed) — the 5 skips are the
standalone-transaction test (`ExecuteDelete_two_phase_on_standalone_throws_actionable_error`, environment-gated)
and the 4 Task-24 encryption tests (environment-gated on `CRYPT_SHARED_LIB_PATH`), matching the expected
skip set exactly; SpecificationTests 181/181 passed.

`MongoNonQueryExpression`-scoped unit tests directly, per EF version:
- EF8: 6/6 passed (the `#if !EF8`-guarded `MongoNonQueryExpressionUnwrapTests` — including the new test and its
  `NonQueryDbContext` — correctly compiles out entirely; only `MongoNonQueryExpressionTests.cs`'s 6 tests run).
- EF9/EF10: 11/11 passed (4 pre-existing unwrap tests + 1 new `VisitChildren` test + 6 from the sibling file).

Full `MongoDB.EntityFrameworkCore.UnitTests` project on EF10: 298/298 passed.

### Northwind bulk baselines (requested insurance)

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
→ SpecificationTests: 181/181 passed. (matches expected baseline)

dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --no-build --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
→ SpecificationTests: 169/169 passed. (matches expected baseline)
```

`MONGODB_URI` and `ATLAS_URI` were confirmed unset for the whole session (`echo` check showed both empty)
before any test run.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs` — comment-only: removed the
  false "this whole type is `#if !EF8`" parenthetical (line 128, now reads "On EF9+ (the only versions where
  bulk operations exist),"); added the `VisitChildren` XML doc (line ~172).
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/Expressions/MongoNonQueryExpressionUnwrapTests.cs` — added
  a duplicated `NonQueryDbContext` helper, the `VisitChildren_returns_the_same_node` fact, and the `NoOpVisitor`
  helper class; added one `using Microsoft.EntityFrameworkCore.Diagnostics;`.

No other files touched. `git diff --stat`: 2 files changed, 32 insertions(+), 1 deletion(-).

BOMs preserved: verified byte-for-byte before and after edit — `MongoNonQueryExpression.cs` still starts with
`EF BB BF` (UTF-8 BOM); `MongoNonQueryExpressionUnwrapTests.cs` still has no BOM (it never had one).

## Correction 4 — anything else untrue in that comment block?

Re-read the full `AffectedSetNeutralDefinitions`/`IsAffectedSetNeutral` doc block (lines ~119–166). Beyond the
fixed parenthetical, the remaining claims check out:

- "this set — and this whole member — must not compile there" (line ~131, about EF8) — accurate; only the
  member is `#if !EF8`, and the doc now correctly says *member*, not *type*.
- "`Order`/`OrderDescending` (added to `QueryableMethods` in EF9; absent on EF8...)" — verified empirically by
  loading the actual `Microsoft.EntityFrameworkCore.dll` assemblies from the local NuGet cache
  (`8.0.23`/`9.0.12`, both `net8.0`) via reflection and checking `QueryableMethods` for an `Order` /
  `OrderDescending` static property: EF8 has neither, EF9 has both. Claim is true.
- The `OfType`/`Cast` exclusion rationale, and the "unreachable unless normalization is removed" note about
  `Order`/`OrderDescending` — these are design rationale statements about this provider's own code
  (`ClassifyBulkSource`, `TranslateCast`/`TranslateOfType`), consistent with what I read in
  `MongoQueryableMethodTranslatingExpressionVisitor.cs` in the Step 2 investigation above. No further
  falsehoods found.

## Self-review

- **Test genuinely falsifiable:** yes — empirically demonstrated with a standalone reproduction (not just
  argued) that a "fixed" `VisitChildren` which visits `SourceQuery` throws `ArgumentException: must be
  reducible node`, and a "fixed" version that rebuilds a new node fails `Assert.Same` on reference identity.
  Either realistic mutation is caught.
- **Every comment touched now true:** yes — the parenthetical fix removed the only false claim (verified
  false by inspecting the four call sites and `RunInBulkTransaction`/`Async` signatures having no third
  parameter, contradicting "this whole type is `#if !EF8`" since the type visibly compiles and is used outside
  any `#if` on all three EF versions); the `Order`/`OrderDescending` claim next to it was independently
  verified true via reflection against real EF assemblies; the `VisitChildren` doc I added is accurate against
  `MongoShapedQueryCompilingExpressionVisitor`'s actual consumption of `SourceQuery`/`Setters`.
- **Avoided scope creep:** yes — `git diff --stat` shows exactly the two files the corrected scope predicted
  (comment fix + doc in `MongoNonQueryExpression.cs`, one new test + helper in
  `MongoNonQueryExpressionUnwrapTests.cs`). Did not touch `MongoBulkOperationExecutor.cs`,
  `MongoQueryableMethodTranslatingExpressionVisitor.cs`, `MongoShapedQueryCompilingExpressionVisitor.cs`, or
  `MongoNonQueryExpressionTests.cs`, despite all four being listed as candidates in the brief or as tempting
  touch points (e.g. widening `NonQueryDbContext`'s visibility).

## Concerns

None. Steps 1 and 2 were verified as already complete with direct evidence (file contents + a clean
`-warnaserror` build), not assumed. The one real piece of work (Step 3's doc/test, plus Correction 4's fix) is
small, tested, and its non-vacuousness was checked empirically rather than asserted.
