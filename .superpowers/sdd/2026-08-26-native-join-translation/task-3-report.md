# Task 3 report: MongoJoinBinder

## What was implemented

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs` — `internal static class MongoJoinBinder` with `internal static bool TryBindJoin(MongoQueryExpression outerQueryExpression, MongoQueryExpression innerQueryExpression, LambdaExpression resultSelector, JoinInfo joinInfo)`, implemented exactly as specified in the task brief (Step 3), plus the private `TryReadMembers` helper for reading `NewExpression`/`MemberInit` result-selector bodies.
  - Eligibility gates, in order: GroupBy/Distinct source on either side → decline; no resolved `joinInfo.Navigation` → decline; non-bare-collection-scan inner → decline; a `JoinScope` already set on the outer select (chained/nested join) → decline; result selector not exactly 2 parameters → decline; result-selector body not a `NewExpression`(with `Members`)/parameterless-`MemberInit` → decline.
  - Member-classification loop: for each result-selector member, if its argument is `ReferenceEquals` to the outer or inner lambda parameter, records that member's name as `outerMemberName`/`innerMemberName` (ambiguous double-capture of the same side declines). Any other member is left untouched — no inline projection translation, per the task's explicit "not this task's job" instruction (that's Task 6).
  - On success: always registers a forced-unwind `LookupExpression` via `outerQueryExpression.AddLookup(...)`, with `PreserveNullAndEmptyArrays` set from `joinInfo.IsLeftOuter`. Only sets `outerQueryExpression.Select.JoinScope` (a `MongoJoinScope`) when at least one side was captured whole.

## Discrepancies between brief's assumed signatures and the real ones

None. Verified before implementing:
- `JoinInfo` (`Query/Expressions/JoinInfo.cs`): `Navigation` is `INavigation?` (settable), `Alias` is `string` (settable, defaults to `""`), `IsLeftOuter` is `bool` (get-only, set via primary constructor) — matches the brief exactly.
- `LookupExpression` (`Query/Expressions/LookupExpression.cs:37`): `LookupExpression(INavigation navigation, bool forceUnwind = false)` exists; `PreserveNullAndEmptyArrays` is an `init`-only `bool` property (default `true`) — settable via object-initializer syntax at construction, exactly as the brief's code does.
- `MongoQueryExpression.AddLookup` (`Expressions/MongoQueryExpression.Lookup.cs`): `public void AddLookup(LookupExpression lookup)` — matches.
- `MongoSelectDefinition.IsBareCollectionScan`/`IsGroupBy`/`IsDistinct`/`JoinScope` — all `internal`, all present with the expected semantics (`JoinScope` is `internal MongoJoinScope? JoinScope { get; set; }`, already folded into `HasTerminalOperator` from Task 2).
- `MongoJoinScope` constructor order (`outerMemberName, innerMemberName, outerEntityType, innerEntityType, innerPrefix, isLeftOuter`) matches the brief's call site exactly.
- `MongoQueryExpression.CollectionExpression.EntityType` and `MongoQueryExpression.AddJoin(IEntityType, bool)` (returns `JoinInfo`) used in tests match the real API.

No adaptation was needed — the brief's code compiled against the real APIs as written.

## What was tested and results

Implemented all four brief-specified test cases in `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoJoinBinderTests.cs`, using a two-entity fixture (`Order`/`Customer`, FK-based reference navigation `Order.Customer`) built via `SingleEntityDbContext.Create<Order>(...)` — the same construction pattern `NativeSelectManyBinderTests.cs` and `StreamingReferenceLookupsTests.cs` use (real `IEntityType`/`INavigation` from an actual EF model, not hand-mocked). A generic `Build<TResult>` helper preserves the real anonymous-type `NewExpression`/`Members` (avoiding a boxing `Convert` node that erases them), mirroring `NativeSelectManyBinderTests.Build<TResult>`.

- `Declines_when_outer_select_is_group_by`: sets `outer.Select.IsGroupBy = true`, full otherwise-valid join info + `(o, i) => new { o, i }` selector → asserts `false`, `JoinScope` stays null.
- `Declines_when_join_has_no_resolved_navigation`: `joinInfo.Navigation` left null → asserts `false`.
- `Classifies_whole_side_capture_and_sets_JoinScope`: `(o, i) => new { o, i }` → asserts `true`, `JoinScope.OuterMemberName == "o"`, `JoinScope.InnerMemberName == "i"`.
- `Already_flattened_result_selector_needs_no_join_scope`: `(o, i) => new { o.Reference, i.Name }` → asserts `true`, `JoinScope` stays null.

RED: confirmed by temporarily moving `MongoJoinBinder.cs` out of the tree and rebuilding the test project — build failed with `CS0103: The name 'MongoJoinBinder' does not exist in the current context` at all four call sites. File was restored immediately after.

GREEN: after restoring the implementation,
```
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoJoinBinderTests"
```
→ `Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4`.

Full-solution builds for all three EF configs (`Debug EF8`, `Debug EF9`, `Debug EF10`) all succeeded with 0 errors. The only warnings present (`NU1903` SSH.NET advisory on test projects, and `CS1574` unresolved cref on `MongoJoinScope.cs` referencing `MongoTransparentScopeResolver`) are pre-existing from prior tasks/dependencies, not introduced by this change — none are warnings-as-errors violations.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs` (new)
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoJoinBinderTests.cs` (new)

## Self-review findings

- All four brief test cases are implemented with real assertions against a real `IEntityType`/`INavigation`-backed fixture, not superficial mocks.
- Implementation matches the brief's code exactly (no signature adaptation was needed), preserves existing project conventions (license header, `internal` visibility, XML doc style, `#nullable` discipline via `?`/`!` matching surrounding code).
- No premature inline projection translation: the member-classification loop only records whole-side captures on `MongoJoinScope`; already-flattened leaves (`o.Reference`, `i.Name`) are read but discarded, per instructions — that's explicitly Task 6's job.
- Did not touch `TranslateJoinCore` (Task 4) or any other call site — `MongoJoinBinder.TryBindJoin` is not yet wired into the QMTEV pipeline, exactly as scoped for this task.

## Issues or concerns

None. All real dependency signatures matched the brief's assumptions with no adaptation required.

## Fix report: reviewer-flagged missing decline tests

The task reviewer approved the original Task 3 work but flagged one Important finding: two of the four
named out-of-scope decline shapes from the task's global constraints (`!innerQueryExpression.Select.
IsBareCollectionScan` and `outerQueryExpression.Select.JoinScope != null`) had guards already shipped in
`MongoJoinBinder.cs` but no corresponding unit test. Added both, reusing the existing `BuildFixture()`/
`Build<TResult>()` fixture helpers already in `MongoJoinBinderTests.cs` — no new fixture style introduced.

### What was changed

Added two `[Fact]` tests to `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoJoinBinderTests.cs`:

- `Declines_when_inner_is_not_bare_collection_scan` — makes the inner select non-bare by recording a
  projection on it (`inner.Select.AddProjection(new MongoProjection("Name", new MongoConstantExpression(1,
  forSerialization: null)))`), which flips `IsBareCollectionScan` to `false` without tripping the earlier
  GroupBy/Distinct guard (a distinct guard from a distinct code path, deliberately). Otherwise a fully valid
  join (resolved navigation, `(o, i) => new { o, i }` selector). Asserts `TryBindJoin` returns `false`,
  `outer.Select.JoinScope` stays `null`, and `outer.Lookups` stays empty (no partial `$lookup` registration).
- `Declines_when_outer_JoinScope_already_set` — pre-sets `outer.Select.JoinScope` to a `MongoJoinScope`
  built directly (simulating a prior join already having claimed the outer select, i.e. a chained/nested
  join), then attempts a second, otherwise-valid `TryBindJoin` call. Asserts it returns `false`, that
  `outer.Select.JoinScope` is `Assert.Same` as the pre-existing instance (untouched/not replaced), and that
  `outer.Lookups` stays empty.

No changes were made to `MongoJoinBinder.cs` itself — both guards were already correctly implemented; only
test coverage was missing.

### Tests run

```
dotnet build tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10"
dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/MongoDB.EntityFrameworkCore.UnitTests.csproj -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoJoinBinderTests"
```

Output: `Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 62 ms`.

Also rebuilt `Debug EF8` and `Debug EF9` for the full solution to confirm no regressions: both succeeded with
0 errors (same pre-existing `NU1903`/`CS1574` warnings as before, unrelated to this change).

### Files changed (this fix)

- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoJoinBinderTests.cs` (2 tests added)

Committed separately as `EF-392: add missing decline tests for Task 3 (non-bare-scan inner, chained join)`
(new commit, not amended onto the original).
