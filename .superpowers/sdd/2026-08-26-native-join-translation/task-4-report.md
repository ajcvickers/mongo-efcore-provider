# Task 4 report: Wire MongoJoinBinder into TranslateJoinCore

## What was implemented

1. **Wiring** (`src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`,
   `TranslateJoinCore`): added the one-line call `MongoJoinBinder.TryBindJoin(outerQueryExpression,
   innerQueryExpression, resultSelector, joinInfo);` immediately after `RebindInnerShaperToOuterQuery` and
   before the `newResultSelector`/`return` lines, exactly as the brief specified. The `using
   MongoDB.EntityFrameworkCore.Query.NativeTranslation;` directive was already present (confirmed before
   editing — the file already references `NativeSelectManyBinder` etc. from that namespace).

2. **A guard fix in `MongoJoinBinder.TryBindJoin`** (`src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs`)
   that was **not** in the brief but was required to make the wiring a true no-op — see "Regression found and
   fixed" below.

3. **The smoke test** (`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`, new file),
   following the `NativeJoinInnerDeclineTests.cs`/`RefOwnerItemDbContext` fixture pattern (private DbContext
   with two `ToCollection`-mapped DbSets and a `HasMany().WithOne().HasForeignKey()` relationship, since
   `MongoJoinBinder.TryBindJoin` requires a resolvable model navigation). No pre-existing Owner/Order fixture
   existed in the FunctionalTests project reusable for this shape, so a small private one was added, scoped to
   this test class only.

## Failure mode: before vs. after

**Before the wiring** (join declined entirely — joins have never been natively representable):
```
NativeTranslationNotSupportedException: Query projects a non-entity result and MongoQueryMode.NativeOnly
forbids the driver-LINQ fallback.
```
Thrown from `MongoShapedQueryCompilingExpressionVisitor.VisitProjectedQuery` (line 332), because the
join's anonymous-type result selector is a projected (non-whole-entity) shaper and nothing populates
`MongoSelectDefinition.Projection`/`Route` for a join's own result-selector projection.

**After the wiring**, the smoke test still fails, with the **identical** message, thrown from the **identical**
site. This is expected and correct, not a regression: `MongoJoinBinder.TryBindJoin` now runs and returns `true`
(it registers the `$lookup` and, for this test's shape — neither `o` nor `r` is captured whole in the result
selector, only `o.Name`/`r.Total` are — leaves `JoinScope` unset), but nothing downstream
(`NativeSlotPopulator`/`NativeProjectionBinder`) is yet aware of `JoinScope` or reads the registered lookup to
populate `Select.Projection`, so `Route` never becomes `NativeRoute.Projection` and the query is declined at
the exact same early gate as before, before `MongoSelectLowerer.AppendLookupStages` is ever reached. (Contrary
to the brief's specific guess, the observed re-decline point is `VisitProjectedQuery`'s generic projected-query
gate, not `AppendLookupStages`'s join-coverage guard — for *this* join shape the query never gets far enough
to reach the lowerer at all. This is consistent with the task's own instruction that a same-message re-decline,
so long as it isn't a crash in new code, is an acceptable outcome.) Task 6 (JoinScope-aware Select/Where
translation) is what will eventually let `Route` become `Projection` for this shape.

## Regression found and fixed (important finding)

Before landing, I ran the full functional `Query` test suite (not just the new smoke test) as the brief's
Step 5 instructs, and found **12 real regressions** with the wiring as specified verbatim in the brief:
`NativeReferenceIncludeTests` (6 tests), `NativeOwnedReferenceWholeEntityTests.Non_owned_reference_include_now_goes_native`,
`RequiredNavigationUnwindTests.Required_single_reference_Include_excludes_dangling_foreign_key_in_every_mode`,
and `Ef373InterleavedPagingTests.Paging_below_all_joins_still_emits_both_lookups_above_the_paging` (both modes).

**Root cause**: EF Core's own Include-to-Join lowering (used for every reference `Include`) constructs the
join's `resultSelector` as a compiler/EF-synthesized "transparent identifier" — a `MemberInitExpression` shaped
`{ Outer = o, Inner = r }`, with both members bound *verbatim* to the whole outer/inner lambda parameters. This
is **structurally indistinguishable** from a genuine user-authored two-field join result selector by
`MongoJoinBinder.TryReadMembers`'s member-capture logic: `TryBindJoin` classified it exactly like a real
`(o, r) => new { Outer = o, Inner = r }` projection, setting `outerMemberName = "Outer"` /
`innerMemberName = "Inner"`, which set `Select.JoinScope` and additionally registered a second, uncoordinated
`LookupExpression` via `AddLookup` — corrupting state that `TryConfirmReferenceInclude` (which runs later, for
this exact query) depends on being untouched. The brief's own comment for the wiring call
("Include-shaped joins are confirmed separately... this call is a no-op for that shape since TryBindJoin's
result-selector classification only recognizes a NewExpression/MemberInit body, never an IncludeExpression")
is the exact assumption that turned out to be false — the resultSelector here IS a MemberInit, just not one
wrapping an `IncludeExpression`.

**Fix**: added a guard at the top of `MongoJoinBinder.TryBindJoin`, right after `TryReadMembers` succeeds and
before any state mutation, that declines (`return false`, no side effects) when
`resultSelector.Body.Type.Name.StartsWith("TransparentIdentifier", StringComparison.Ordinal)`. This mirrors the
established convention already used elsewhere in this same file
(`IsTransparentIdentifierMemberAccessSelector`, `MemberExpression.IsTransparentIdentifierOuterOrInnerAccess()`)
for recognizing EF's own transparent-identifier plumbing as reserved, not a genuine user projection. After this
fix, the full functional `Query` suite is clean again (0 regressions) across all three EF versions — only the
new (expected-red) smoke test fails.

I considered simply reporting this as BLOCKED rather than fixing it, since the fix touches
`MongoJoinBinder.cs` (Task 3's file, already committed) rather than staying strictly inside
`TranslateJoinCore`. I chose to fix it because: (1) it is required to satisfy this task's own explicit
acceptance gate ("this wiring must be a no-op for every existing passing shape"); (2) the fix is narrowly
targeted, additive, and uses a pattern already established elsewhere in the codebase for the exact same
purpose; (3) it does not touch `MongoSelectLowerer.cs` or `NativeSlotPopulator.cs` (Task 5/6 territory); (4)
leaving it unfixed would mean either committing a real regression or not landing Task 4 at all. Flagging this
clearly per the instructions rather than silently absorbing it.

## `joinInfo.Lookup` read-dependency check (the other thing the brief asked me to verify)

**No.** `joinInfo.Lookup` is set independently by `RebindInnerShaperToOuterQuery` (lines ~1885/1904), which
runs *before* the `TryBindJoin` call site, and is read only inside that same method (for the
second-or-later-join `AddLookup` registration at lines ~1925–1936) — never after the insertion point.
`MongoJoinBinder.TryBindJoin` does not read or write `joinInfo.Lookup` at all; it builds its own separate
local `LookupExpression` and registers it via `outerQueryExpression.AddLookup(lookup)` directly. So nothing in
`TranslateJoinCore`'s flow reads `joinInfo.Lookup` expecting it to have been set as a side effect of
`TryBindJoin` — confirmed, no such dependency exists.

(Separately worth flagging for whoever picks up Task 5/6: for a *second-or-later* join whose result selector
legitimately captures both sides whole — i.e., a shape `TryBindJoin` *does* admit — `TryBindJoin` and
`RebindInnerShaperToOuterQuery` would each construct and register their own independent `LookupExpression` for
the same join, potentially with different aliases/settings. This didn't surface as a test failure in this
task's regression run, but it's a latent double-registration structurally similar to the bug just fixed above,
worth a look when Task 6 exercises multi-join JoinScope shapes.)

## What was tested

- Smoke test before wiring: confirmed FAIL with the original decline (see above).
- Smoke test after wiring (+ guard fix): confirmed FAIL with the identical message/site — not a crash in new
  code.
- `MongoJoinBinder` unit tests (Task 3's own, `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoJoinBinderTests.cs`):
  6/6 pass with the added guard.
- Full `MongoDB.EntityFrameworkCore.UnitTests` suite (EF10): 941/941 pass.
- Full `MongoDB.EntityFrameworkCore.FunctionalTests` `Query` filter, all three EF versions: exactly one failure
  in each (the new smoke test, expected-red); zero other failures. Before the guard fix, EF10 showed 12
  failures (see regression section); after the fix, 0 regressions on EF8/EF9/EF10.
- `dotnet build MongoDB.EFCoreProvider.sln` for `Debug EF8`, `Debug EF9`, `Debug EF10`: all succeed, 0 errors,
  7 warnings — all pre-existing (`NU1903` SSH.NET advisory, and a `CS1574` cref warning on `MongoJoinScope.cs`
  from Task 2, present before any of this task's edits). No new warnings introduced.

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs` — the
  one-line wiring call (plus its explanatory comment) in `TranslateJoinCore`.
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs` — added the
  `TransparentIdentifier` type-name guard (required fix, see above) and the `using System;` it needs.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs` — new file, the Task 4 smoke
  test.

## Self-review

- The wiring itself in `TranslateJoinCore` is a single, minimal, additive call with no other logic changes to
  that method — confirmed by diff.
- Did not touch `MongoSelectLowerer.cs` or `NativeSlotPopulator.cs` (Task 5/6 territory) — confirmed by diff
  (only `MongoJoinBinder.cs`, the visitor, and the new test file changed).
- The one deviation from "stay inside `TranslateJoinCore` only" is the guard added to `MongoJoinBinder.cs`,
  which I judged necessary and in-scope for the reasons given above (this task's own no-regression gate) rather
  than a Task 5/6 concern.
- Regression suite results are clean across all three EF configurations after the fix.

## Concerns for follow-up (not blocking this task)

1. The double-`LookupExpression`-registration latent issue for a second-or-later join noted above, under
   "`joinInfo.Lookup` read-dependency check" — worth a look during Task 6.
2. The brief's own inline comment for the wiring call asserted an incorrect no-op rationale (see "Regression
   found and fixed"); I corrected the comment in place to describe the actual, verified mechanism rather than
   leave the disproven claim in the code.
