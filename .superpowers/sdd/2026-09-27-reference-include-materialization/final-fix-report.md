# Final fix wave report — 2026-09-27

Three trivial, review-recommended cleanups applied to `EF-322-Native-LINQ-rebased`. No production logic or
test logic changed — formatting, naming, and doc-prose only.

## Fix 1 — Rewrap overlong comment line

File: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`,
`BindResultMember` header comment, originally line 3388 (173 chars).

**Before:**
```
    // node-rewrite (MongoProjectionBindingExpressionVisitor.VisitExtension's IncludeExpression case) actually runs. Without this, the shape check below fails on the wrapped
    // leaf, execution falls through to BindSelectManyMember, and the RAW (Include-wrapped, unfolded) leaf
```

**After:**
```
    // node-rewrite (MongoProjectionBindingExpressionVisitor.VisitExtension's IncludeExpression case) actually
    // runs. Without this, the shape check below fails on the wrapped leaf, execution falls through to
    // BindSelectManyMember, and the RAW (Include-wrapped, unfolded) leaf
```

No wording changed, only where the line breaks. All lines in the block now fall in the ~90-115 char range
matching the rest of the comment.

## Fix 2 — Rename out-of-date test

File: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`

**Before:**
```csharp
public void Reference_included_whole_entity_leaf_goes_native_and_matches_driver_linq(string shape)
{
    // EF-322 (reference-Include materialization): variations on the shape above that also reach the join-scope
    // arm with a reference-Include-wrapped whole-entity leaf. Each must go native (NativeOnly never throws) and
    // produce exactly the in-memory oracle's rows (every Owner populated — all seeded FKs match), as must
    // driver-LINQ.
```

**After:**
```csharp
public void Reference_included_whole_entity_leaf_goes_native_and_matches_the_oracle(string shape)
{
    // EF-322 (reference-Include materialization): variations on the shape above that also reach the join-scope
    // arm with a reference-Include-wrapped whole-entity leaf. Each must go native (NativeOnly never throws) and
    // produce exactly the in-memory oracle's rows (every Owner populated — all seeded FKs match), as must
    // driver-LINQ — both asserted against the oracle independently, not against each other.
```

Also updated the two `nameof(...)` database-name literals used inside the test body to reference the new
method name (`_nativeOnly` / `_driverLinq` suffixes unchanged). Confirmed via repo-wide grep that no other
file referenced the old test name.

## Fix 3 — Correct stale prose in design doc

File: `docs/superpowers/specs/2026-09-25-reference-include-materialization-design.md`, "Shape 4" section.

The section header still claimed Shape 4 has "no join scope involved at all" and is "likely the SAME bug as
Shape 3's Hypothesis B." `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md`'s own Shape 4
section (probe output + Recommendation) confirms the opposite: Shape 4 DOES reach the join-scope arms
(`IsSingleEligibleNativeJoinScope` / `NativeJoinScopeProjectionBinder.TryBindProjection` /
`BindResultMember`) — Hypothesis A was confirmed, not B, and it's the same code path as Shapes 1-3, not a
separate bug.

**Note added verbatim**, immediately after the `### Shape 4 — ...` heading:

```
> **Correction (post-investigation):** the claims below that Shape 4 has "no join scope involved at all" and
> is "likely the SAME bug as Shape 3's Hypothesis B" were disproven. `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md`'s
> Shape 4 section confirms Shape 4 DOES reach the join-scope arms (`IsSingleEligibleNativeJoinScope` /
> `NativeJoinScopeProjectionBinder.TryBindProjection` / `BindResultMember`) — Hypothesis A, not B — the same
> code path as Shapes 1-3, not a separate bug.
```

The rest of the section (heading text, code block, remaining prose) was left untouched per instructions —
only a correction note was added, not a rewrite.

## Verification

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
Build succeeded, 0 errors (13 pre-existing warnings, unrelated: NU1903 SSH.NET advisory, one CS1574 cref
warning in `MongoFieldExpression.cs`).

```
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeJoinTests"
```
Result: **Passed! - Failed: 0, Passed: 65, Skipped: 0, Total: 65**, Duration 3s (FunctionalTests.dll,
net10.0). (Unit/Specification test assemblies reported "no test matches the filter" — `NativeJoinTests` only
exists in FunctionalTests, expected.)

## Files changed

- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`
- `docs/superpowers/specs/2026-09-25-reference-include-materialization-design.md`

## Commit

`89c9bab9` — "EF-322: final review-wave cleanups (comment rewrap, test rename, stale doc note)"
