# Task 6 (revised) report — document dormant join scaffolding, verify graceful fallback

## What was implemented

1. **Dormant-code doc comments** — added a short `<remarks>` block to each of the four locations named
   in the brief, each pointing at
   `docs/superpowers/specs/2026-08-26-native-join-translation-design.md`'s "Blocker found during
   implementation" section and EF-439, and stating the one-sentence reason (EF Core's
   `NavigationExpandingExpressionVisitor` normalizes a genuine join and an Include-generated join into
   the identical shape before the code in question ever runs, so it's unreachable in practice):
   - `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs` — remarks on the
     `MongoJoinScope` class.
   - `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs` — remarks on the
     `MongoJoinBinder` class.
   - `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs` — an inline comment
     block immediately preceding the "new arm" (`lookup.Navigation is { IsCollection: false } &&
     lookup.ForceUnwind && query.IsJoinQuery` branch inside `AppendLookupStages`), left the existing
     explanatory comment in place below it.
   - `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs` — remarks on the
     `JoinScope` property.

2. **Revised `NativeJoinTests.cs`** — replaced the single (now-obsolete) smoke test
   `Flattened_scalar_join_goes_native_with_correct_results_and_mql` with the two tests specified verbatim
   in the brief's Step 2:
   - `Genuine_two_sided_join_returns_correct_results_via_fallback` — asserts a genuine two-sided `Join`
     under the default `MongoQueryMode.Native` returns results equal to an in-memory LINQ oracle over the
     same seed data (real correctness assertion, not just "doesn't throw").
   - `Genuine_two_sided_join_declines_cleanly_under_NativeOnly` — asserts the same join throws
     `NativeTranslationNotSupportedException` under `MongoQueryMode.NativeOnly`.

   The class doc comment was rewritten to describe the permanent, ruled-on behavior instead of the
   abandoned original goal. Supporting helper methods (`CreateContext`/`CreateContextWithLogging`) were
   adapted to take a `Seed` record (returned by a new `SeedOwnersAndOrders()` helper) so the same seed
   data can be shared between the actual-query call and the in-memory expected-result computation, per
   the brief's test bodies. The now-unused `MongoDB.EntityFrameworkCore.Diagnostics` using (for
   `MongoEventId`, no longer referenced) was removed and `MongoDB.EntityFrameworkCore.Query.NativeTranslation`
   (for `NativeTranslationNotSupportedException`, which is `internal`) was added.

## Test results

- `NativeJoinTests` (EF10, Debug): both new tests pass.
  ```
  Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: 373 ms
  ```
- Full functional `Query` regression suite (EF10, Debug): no regressions.
  ```
  Passed!  - Failed: 0, Passed: 1768, Skipped: 45, Total: 1813, Duration: 41 s
  ```
- Builds: `Debug EF8`, `Debug EF9`, `Debug EF10` all succeeded, 0 errors. The only warning present
  (`CS1574` on `MongoJoinScope.cs` line 24, an unresolved `cref` to `MongoTransparentScopeResolver`) is
  pre-existing — confirmed by stashing this change and rebuilding, which reproduced the identical warning
  on the pre-existing summary text (not something this task introduced).

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoJoinScope.cs`
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoJoinBinder.cs`
- `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoSelectLowerer.cs`
- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoSelectDefinition.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`

Commit: `9a33d3d` — "EF-392: document dormant join-scaffolding as blocked on EF-439; verify genuine
two-sided joins fall back correctly"

## Self-review findings

- Doc comments are one short paragraph each, point at the spec section + EF-439, state the mechanism
  (indistinguishable at bind time) without re-deriving the whole investigation. Consistent with the
  brief's "keep it short" instruction.
- The revised smoke test genuinely asserts *correct results* (equality against an in-memory LINQ oracle
  over the same seed), not merely absence of an exception, satisfying the self-review checklist item.
- The `NativeOnly` test asserts the specific exception type `NativeTranslationNotSupportedException`
  is thrown — a clean decline, not a crash or wrong-result path.
- Regression suite is fully green (1768 passed, 0 failed, 45 skipped — skips are all
  encryption-related, gated on `CRYPT_SHARED_LIB_PATH`, unrelated to this change).
- No design judgment was required beyond what the brief specified — this task was purely mechanical
  (doc comments + a verbatim test replacement + supporting helper-signature plumbing to make the brief's
  test bodies compile).

## Issues or concerns

None. The task was fully mechanical as expected; the only adaptation beyond the brief's literal text was
reshaping the existing `CreateContext`/`CreateContextWithLogging` test helpers to accept a `Seed` object
(the brief's test bodies assumed a `SeedOwnersAndOrders()` helper and a `(seed, mode, name, ...)` call
shape that didn't match the pre-existing helper signatures) — a mechanical plumbing change, not a design
decision.
