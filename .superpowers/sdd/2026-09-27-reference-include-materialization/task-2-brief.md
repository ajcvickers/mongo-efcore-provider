### Task 2: Verify and, if needed, fix Shape 2 (Inner-leaf staging/materialization symmetry)

Independent of Task 1's finding — this closes a narrower, already-understood gap: the join-scope *staging*
side (`NativeJoinScopeProjectionBinder.cs`) has the same unguarded `IncludeExpression` unwrap that the
*materialization* side (`BindResultMember`) already got a guard for in commit `0b0a7a6d`.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs:231-235`
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs`

**Interfaces:**
- Consumes: nothing from Task 1 (independent).
- Produces: a proven answer to "is Shape 2 reachable, and if so does it now decline cleanly or crash/silently
  wrong-data" — Task 3 does not depend on this, but the design doc's Review Focus item about not touching the
  root-cause-A2 area without verification applies here.

- [ ] **Step 1: Write a test reproducing Shape 2**

Add to `NativeJoinTests.cs` (mirroring `Whole_entity_leaf_that_is_also_reference_included_declines_rather_than_silently_returning_null`'s
own "throws OR correct, never silently wrong" assertion style):

```csharp
[Fact]
public void Reference_included_Inner_leaf_of_a_join_scope_never_returns_silently_wrong_data()
{
    // Design doc Shape 2 (UNVERIFIED, PARKED at A2's final review): NativeJoinScopeProjectionBinder's own
    // staging-side unwrap (this file's own remarks on the Inner-leaf recognition arm) has the SAME unguarded
    // IncludeExpression unwrap that BindResultMember (the materialization side) already got a guard for in
    // commit 0b0a7a6d. If this Inner leaf gets staged as native while BindResultMember declines it, the two
    // halves disagree. This test proves which of "clean decline" / "correct" / "silently wrong" actually
    // happens today.
    var seed = SeedOwnersAndOrders();
    using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
        nameof(Reference_included_Inner_leaf_of_a_join_scope_never_returns_silently_wrong_data));

    static List<(string OwnerName, Order Order)> Run(JoinTestDbContext db) =>
        db.Owners.Join(db.Orders.Include(r => r.Owner), o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r })
            .AsEnumerable().Select(x => (x.Name, x.r)).ToList();

    List<(string OwnerName, Order Order)>? results = null;
    var threw = false;
    try
    {
        results = Run(db);
    }
    catch
    {
        threw = true;
    }

    // Mirrors Shape 1's own test contract: EITHER a clean decline (throws) OR fully correct data — silently
    // null/empty on the Inner leaf's OWN Include (r.Owner) must never happen.
    if (!threw)
    {
        Assert.NotEmpty(results!);
        Assert.All(results!, x => Assert.NotNull(x.Order.Owner));
    }
}
```

- [ ] **Step 2: Run it against the CURRENT (pre-fix) code to observe today's behavior**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Reference_included_Inner_leaf_of_a_join_scope_never_returns_silently_wrong_data"
```

Record the outcome. If it PASSES today (either a clean throw, or genuinely correct data), the gap the design
doc flagged as "may exist" does not manifest for this shape — skip Step 3, note in the commit message that
Shape 2 was verified unreachable/already-safe, and commit just the test.

If it FAILS with `Assert.NotNull` failing (i.e., `Order.Owner` came back null with no exception — silently
wrong data), proceed to Step 3.

- [ ] **Step 3: If Step 2 failed, apply the symmetric guard**

In `NativeJoinScopeProjectionBinder.cs`, mirror `0b0a7a6d`'s `BindResultMember` guard. Change:

```csharp
var unwrappedLeafBody = leafBody;
while (unwrappedLeafBody is IncludeExpression include)
{
    unwrappedLeafBody = include.EntityExpression;
}
```

to:

```csharp
var unwrappedLeafBody = leafBody;
var allCollectionIncludesInLeaf = true;
while (unwrappedLeafBody is IncludeExpression include)
{
    if (include.Navigation is not { IsCollection: true })
    {
        allCollectionIncludesInLeaf = false;
    }

    unwrappedLeafBody = include.EntityExpression;
}
```

Then gate the recognition that follows (the `MongoTransparentScopeResolver.TryResolveScopeDepth(unwrappedLeafBody, ...)`
call a few lines below) so a `false` `allCollectionIncludesInLeaf` falls through to decline this leaf, exactly
as `BindResultMember`'s own guard falls through to its pre-existing fallback — do not stage a
`MongoProjection` for a leaf containing any reference Include.

- [ ] **Step 4: Re-run the test to confirm it now passes (as a clean decline)**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Reference_included_Inner_leaf_of_a_join_scope_never_returns_silently_wrong_data"
```

Expected: PASS.

- [ ] **Step 5: Run the full native join-scope and reference-Include suites to confirm no regression**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NativeJoinTests|FullyQualifiedName~NativeReferenceIncludeTests|FullyQualifiedName~NativeJoinScopeProjectionBinderTests"
```

Expected: all PASS, including the two already-passing bare reference-Include tests.

- [ ] **Step 6: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeJoinScopeProjectionBinder.cs
git commit -m "$(cat <<'EOF'
EF-322: guard NativeJoinScopeProjectionBinder's Inner-leaf unwrap against reference Includes

Design doc Shape 2 (parked, unverified at A2's final review): the staging side's
IncludeExpression unwrap had no IsCollection check, mirroring the gap
BindResultMember (the materialization side) already got a guard for in 0b0a7a6d.
[Verified unreachable today — no code change needed beyond the pinning test. /
Verified reachable and silently wrong — same allCollectionIncludes-style guard
applied here, symmetric with 0b0a7a6d.]
EOF
)"
```

(Pick the bracketed sentence matching what Step 2 actually found; delete the other.)

---

