# Final review fix report — EF-392/EF-439 native-join dormancy findings

## Important #1 — GroupJoin could still reach `MongoJoinBinder.TryBindJoin` and mutate state

Fixed in `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs`:

- Added an unconditional `return false;` as the very first statement in `TryBindJoin`, before the
  `GroupBy`/`Distinct` check, before the navigation check, before everything — including before the old
  `TransparentIdentifier`-shape check. This makes the decline structural (fires for `Join`, `LeftJoin`, and
  `GroupJoin` alike) rather than a heuristic keyed on EF's `TransparentIdentifier` normalization, which only
  covers `Join`/`LeftJoin` and never fires for `GroupJoin` (whose result selector body is the user's own
  anonymous type).
- Kept the rest of the method body (including the now-dead `TransparentIdentifier` check) intentionally, per
  the instructions — it's harmless dead code, exercised only via the CS0162 "unreachable code" warning
  (confirmed non-fatal: no `TreatWarningsAsErrors` anywhere in the repo's `.csproj`/`Directory.Build.props`).
- Updated the class-level `<remarks>` doc comment to describe the decline as unconditional/structural, not
  shape-dependent, and to explain *why* the old shape-based check was insufficient (GroupJoin's own shape
  slips past it).
- Fixed the "elsewhere in this file" comment (Minor #2) to correctly name
  `MongoQueryableMethodTranslatingExpressionVisitor.IsTransparentIdentifierMemberAccessSelector` as the
  precedent, since it doesn't live in this file.

Updated `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoJoinBinderTests.cs`:

- Renamed and inverted `Classifies_whole_side_capture_and_sets_JoinScope` →
  `Declines_unconditionally_for_whole_side_capture_shape`: now asserts `TryBindJoin` returns `false` and
  performs no mutation (`JoinScope` stays null, `Lookups` stays empty), for the exact shape that used to bind
  successfully (and is the shape a `GroupJoin` call can produce without ever hitting the old guard).
- Renamed and inverted `Already_flattened_result_selector_needs_no_join_scope` →
  `Declines_unconditionally_for_already_flattened_result_selector`: same treatment for the already-flattened
  shape.
- Left the four `Declines_when_*` tests (which already expected `false`) unchanged — they still pass, now
  via the new unconditional guard rather than their originally-tested guard, which is fine since the
  unconditional decline subsumes every other guard's effect.
- Updated the class-level test-file summary comment to describe the new unconditional-decline behavior and
  why these pinning tests matter.

Also updated `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs`'s doc comments (Minor #1 +
consistency with the Important #1 fix):
- Qualified the previously-unqualified `<see cref="MongoTransparentScopeResolver"/>` to
  `NativeTranslation.MongoTransparentScopeResolver` (file is in `Query.Expressions`, the resolver is in
  `Query.NativeTranslation`) — verified the type exists at that namespace.
- Updated the `<remarks>` to describe the unconditional decline instead of the old shape-based one.

## Important #2 — `MongoSelectLowerer.cs`'s "only reached for MongoJoinBinder" comment was false

Fixed in `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs`, the
`lookup.Navigation is {IsCollection: false} && lookup.ForceUnwind && query.IsJoinQuery` arm in
`AppendLookupStages`. Rewrote the comment block to state:

- This arm is live for **both** the native reference-Include shape (the only reachable one, confirmed by
  `TryConfirmReferenceInclude`) and the (now permanently unreachable per Important #1) join-binder-registered
  lookup shape — both satisfy the same three-part condition because EF's nav-expansion lowers a reference
  Include into an internal Join too.
- For the Include case, `isLeftOuter` is read from `query.Joins`'s matching `JoinInfo.IsLeftOuter`
  (operator-derived), **not** from `lookup.PreserveNullAndEmptyArrays` (which
  `TryConfirmReferenceInclude` sets from `!navigation.ForeignKey.IsRequired`).
- These two values agree for this one admitted shape **by construction**, not coincidence — pointed to
  `TryConfirmReferenceInclude`'s own comment in
  `MongoQueryableMethodTranslatingExpressionVisitor.cs` (around its `LookupExpression` construction, near
  line 918), which explains EF's nav-expansion emits `Queryable.Join` for a required navigation and
  `LeftJoin` for an optional one, so "the operator and IsRequired coincide by construction" for this shape.
- `NativeReferenceIncludeTests`' MQL assertions on `preserveNullAndEmptyArrays` are the regression guard for
  this invariant — no new test added (per instructions).

## Important #3 — `query.Joins.First(...)` crash risk

Same arm: changed
```csharp
var isLeftOuter = query.Select.JoinScope is { } joinScope && joinScope.InnerPrefix == lookup.As
    ? joinScope.IsLeftOuter
    : query.Joins.First(j => j.Alias == lookup.As).IsLeftOuter;
```
to
```csharp
var isLeftOuter = query.Select.JoinScope is { } joinScope && joinScope.InnerPrefix == lookup.As
    ? joinScope.IsLeftOuter
    : query.Joins.FirstOrDefault(j => j.Alias == lookup.As)?.IsLeftOuter
        ?? lookup.PreserveNullAndEmptyArrays;
```
This degrades gracefully to the value the sibling `IsStreamableReference` arm would have used, instead of a
bare `InvalidOperationException`, if no matching `JoinInfo` is ever found.

## Important #4 — unordered oracle-theory assertion

Fixed `Join_result_matches_in_memory_oracle_including_unmatched_rows` in
`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`: both `actualResult` and
`oracleResult` are now materialized via `.AsEnumerable()` then
`.OrderBy(x => ((dynamic)x).Name).ThenBy(x => ((dynamic)x).Total)` before `.ToList()`/comparison, mirroring
the sibling test's ordering pattern. `dynamic` (rather than a typed lambda) is required here because this
test's shared delegate signature (`Func<IQueryable<Owner>, IQueryable<Order>, IQueryable<object>>`) erases
the anonymous result type to `object`; `dynamic` also required using `.AsEnumerable()` first on the oracle
side too, since `IQueryable<T>.OrderBy` takes an `Expression<Func<...>>` and C# forbids a dynamic operation
inside an expression tree (`CS1963`) — caught by the build, fixed by ordering the already-materialized
`IEnumerable<object>` instead.

## Minor sweep

1. `MongoJoinScope.cs` cref qualified — see Important #1 section above.
2. `MongoJoinBinder.cs` "elsewhere in this file" comment corrected to name
   `MongoQueryableMethodTranslatingExpressionVisitor.IsTransparentIdentifierMemberAccessSelector` and its
   file — see Important #1 section above.
3. `NativeJoinTests.cs`: deleted `CreateContextWithLogging`, the `ILoggerFactory`-taking `JoinTestDbContext`
   constructor overload and its `BuildOptions` parameter, and the now-unused
   `using Microsoft.Extensions.Logging;` directive. `Genuine_two_sided_join_returns_correct_results_via_fallback`
   now uses plain `CreateContext`.
4. `docs/superpowers/specs/2026-08-26-native-join-translation-design.md`: added
   `*(superseded — see "Blocker found during implementation" above)*` immediately under the `## Scope`,
   `## Design`, `### Data flow`, and `### Testing` headings. Content of those sections left untouched, per
   instructions (`### Error handling` and `## Coordination with concurrent EF-411 work` were not in the
   list and were left alone).
5. `src/MongoDB.EntityFrameworkCore/Query/AGENTS.md`: appended one sentence to the end of the `Include`
   bullet noting genuine two-sided `Join`/`LeftJoin`/`GroupJoin` always falls back to driver-LINQ by design
   (EF-439), disambiguation from Include-shaped joins isn't possible given current EF Core behavior, and the
   dormant scaffolding (`MongoJoinBinder`, `MongoJoinScope`, a `MongoSelectLowerer` arm) is intentionally
   never reachable.
6. `NativeSelectManyBinder.cs`: removed a stray double blank line at line 855-856 (between `TryTranslateValue`'s
   caller and the next method's doc comment).

## Test results

All runs on `-c "Debug EF10"` unless noted; build also verified clean for `Debug EF8` and `Debug EF9`.

| Suite | Before (baseline, not re-run) | After |
|---|---|---|
| Build (EF10) | — | Build succeeded, 0 errors (1 pre-existing CS0162 warning at the new unconditional `return false;`, and pre-existing NU1903 SSH.NET advisory warnings — both non-fatal, no `TreatWarningsAsErrors` in repo) |
| Build (EF8) | — | Build succeeded, 0 errors |
| Build (EF9) | — | Build succeeded, 0 errors |
| `MongoJoinBinderTests` (unit) | — | 6/6 passed |
| `NativeJoinTests` (functional) | — | 8/8 passed |
| Functional `~Query` filter | — | 1774 passed, 0 failed, 45 skipped (unrelated encryption skips — `CRYPT_SHARED_LIB_PATH` unset) |
| Specification `~Query` filter | — | 4536 passed, 0 failed, 13 skipped |
| Full `UnitTests` project | — | 945 passed, 0 failed |

No regressions found in any suite.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs`
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs`
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs`
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeSelectManyBinder.cs`
- `src/MongoDB.EntityFrameworkCore/Query/AGENTS.md`
- `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoJoinBinderTests.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`
- `docs/superpowers/specs/2026-08-26-native-join-translation-design.md`

## Issues or concerns

- None. The reviewer's Important #1 finding was real and reproducible in principle (the old guard's
  reliance on `TransparentIdentifier`-shape detection genuinely doesn't cover `GroupJoin`); the fix is a
  minimal, unconditional early return, and the affected unit tests were updated to match and pass.
- The CS0162 "unreachable code" warning in `MongoJoinBinder.cs` is expected and intentional (the rest of the
  method body is dead by construction, as instructed) — it's a warning, not an error, and does not fail the
  build in this repo's configuration.
