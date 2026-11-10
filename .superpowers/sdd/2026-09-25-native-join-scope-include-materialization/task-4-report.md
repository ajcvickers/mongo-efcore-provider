# Task 4 report — regenerate the two Northwind spec baselines

## Status: BLOCKED at Step 1

The brief's Step 1 guard fired. Both target tests do NOT fail with a clean `AssertMql` mismatch; they fail
with a **data-correctness assertion from EF Core's own base test body**, which the brief explicitly calls out
as the STOP condition ("that would mean Tasks 1–3 didn't actually fix the general case, only the two specific
functional-test shapes").

## What I ran

### Pre-flight

- `docker info` → OK (Docker Desktop, server 29.4.3).
- `git status` → clean except pre-existing untracked plan/spec docs (not touched).
- Confirmed branch is `EF-322-Native-LINQ-rebased` at `4d22a90d` (did not switch branches).

### Step 1 — build + run under `MONGODB_EF_NATIVE_ONLY=1`, EF10

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
→ Build succeeded, 0 errors.

```
MONGODB_EF_NATIVE_ONLY=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest.Include_reference_when_entity_in_projection|FullyQualifiedName~NorthwindIncludeQueryMongoTest.Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join" \
  --logger "console;verbosity=normal"
```

Result: **4/4 failed**, all as data-correctness failures, not `AssertMql` mismatches:

- `Include_reference_when_entity_in_projection` (async: True/False):
  ```
  Assert.Equal() Failure: Strings differ
  Expected: "FOLKO"
  Actual:   null
  ```
  at `Microsoft.EntityFrameworkCore.Query.NorthwindQueryFixtureBase.<>c.<.ctor>b__18_14` →
  `QueryAsserter.AssertIncludeEntity` — this is EF Core's own base-test assertion machinery, invoked from
  inside `await base.Include_reference_when_entity_in_projection(async)`, i.e. **before** our override's
  `AssertMql(...)` call is ever reached.

- `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join` (async: True/False):
  ```
  Assert.Equal() Failure: Values differ
  Expected: 2
  Actual:   0
  ```
  at `QueryAsserter.AssertIncludeCollection`, again inside `base.Outer_identifier_correctly_determined_...(async)`,
  before our override's `AssertMql(...)`.

Per `tests/MongoDB.EntityFrameworkCore.SpecificationTests/AGENTS.md`: "**Data-gated by construction** —
`AssertMql(...)` is the last call, after `await base.SomeTest(...)`, so a data/behavior failure never reaches
the rewriter." This confirms `EF_TEST_REWRITE_BASELINES=1` could not do anything useful here even if I ran it —
the failure happens upstream of the rewrite hook.

### Sanity check — same failure without `MONGODB_EF_NATIVE_ONLY`

To rule out this being an artifact of forcing `NativeOnly` specifically, I re-ran the identical filter with no
env var set (default query mode):

```
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest.Include_reference_when_entity_in_projection|FullyQualifiedName~NorthwindIncludeQueryMongoTest.Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join" \
  --logger "console;verbosity=normal"
```

Result: **identical failures** (same "FOLKO"/null and 2/0 mismatches). Since native is the default query mode
on this branch (per `AGENTS.md`: "the native translator being the default is not a break"), this means the
native path — the one Tasks 1–3's fix targets — is producing genuinely wrong data for these two spec-suite
shapes, not just a stale-baseline mismatch, and not specific to forcing `NativeOnly`.

I did not proceed to Steps 2–7 (no baseline rewrite attempted, no other EF version tried) because the guard
condition in Step 1 was hit on the very first test.

## Files touched

None. `git status --short` after all runs shows only the pre-existing untracked plan/spec docs that were
present before I started; no test file was modified (confirmed the `AssertMql` rewriter never fired, since the
failures occur inside `base.*(async)` before our override's `AssertMql(...)` call).

## Diagnosis (not fixed — out of scope for Task 4)

Both failing tests exercise the same "join-scope Include" shape family that Tasks 1–3 targeted
(`BindResultMember` / `MongoProjectionBindingExpressionVisitor` / `LookupExpression` / `MongoSelectLowerer`),
confirmed by the current (stale) baselines in `NorthwindIncludeQueryMongoTest.cs`:

- `Include_reference_when_entity_in_projection` (line ~554): `Orders` → `$match` → `$project {_outer:$$ROOT}` →
  `$lookup` Customers → `$unwind` → `$project {_outer,_inner}` — a whole-entity join-scope leaf, structurally
  like the fixed functional-test shapes but reached via a *projection* (`Select(o => new { o.OrderID, o.Customer })`-style)
  rather than a plain `Include`.
- `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join` (line ~635): `Customers`
  → `$lookup` Orders → `$unwind` (preserveNull) → `$project {_outer,_inner}` → `$match` City=Seattle → a
  **second** `$lookup` (OrderDetails) nested under `_inner._lookup_OrderDetails` — an Include applied on the
  *right side of a left join*, i.e. a join-scope Include chained a level deeper/differently than the three
  proven functional tests.

Both fail with `null`/`0` — i.e. the navigation/collection comes back empty rather than materializing the
included entity — which looks like the same failure family Tasks 1–3 fixed (whole-entity join-scope leaf not
recognized/materialized), but apparently not fully covered by the fix as landed. This needs
`superpowers:systematic-debugging` on the production code (likely back in
`MongoQueryableMethodTranslatingExpressionVisitor.BindResultMember` / `MongoProjectionBindingExpressionVisitor` /
`LookupExpression` / `MongoSelectLowerer`), which is beyond this task's scope (baseline regeneration only).

## Recommendation

Do not attempt to regenerate baselines for these two tests until the underlying production fix is extended to
cover these two shapes (projection-wrapped join-scope Include, and Include on the right side of a left join
with a second/nested lookup). Re-run this task's Step 1 after that fix lands; if it then produces a clean
`AssertMql`-only mismatch, proceed with Steps 2–7 as written.
