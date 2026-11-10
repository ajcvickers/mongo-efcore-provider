### Task 3: Fix Shape 3 (branches on Task 1's finding — execute exactly one of 3A/3B)

Read `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md` (Task 1's output) before starting.
Execute **3A** if it names Hypothesis A, **3B** if it names Hypothesis B. If it names neither, stop and write
a new short design note describing what it found instead — do not force the evidence into one of these two
branches.

**Files (both branches touch these; each branch's steps say which):**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs` (3A)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
  (both branches, different regions: `BindResultMember` for 3A, `TryConfirmReferenceIncludeChain`'s
  post-confirmation fold for 3B)
- Test: `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs` and its
  3 siblings (`NorthwindStringIncludeQueryMongoTest.cs`, `NorthwindEFPropertyIncludeQueryMongoTest.cs`,
  `NorthwindIncludeNoTrackingQueryMongoTest.cs`) — baselines regenerate, do not hand-write `AssertMql` bodies
- Test: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs` (promote Shape 1's spike
  to a real passing test once it stops declining)

**Interfaces:**
- Consumes: `docs/superpowers/findings/2026-09-27-shape3-code-path-findings.md` (Task 1).
- Produces: `Include_reference_when_entity_in_projection` and
  `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join` passing with correct
  data, across all 4 Northwind sibling classes, in the default query mode.

#### Branch 3A — Hypothesis A holds (Shape 3 shares BindResultMember/join-scope machinery with Shapes 1/2)

- [ ] **Step 1: Read the collection-Include branch this fix must mirror**

Read `MongoProjectionBindingExpressionVisitor.cs`'s `case IncludeExpression` block, specifically the
`if (!includableNavigation.IsEmbedded() && includableNavigation.IsCollection)` branch and its
`LookupExpression`/`AddLookup` call. This is the shape the reference-Include branch (currently a no-op besides
visiting children) must mirror.

- [ ] **Step 2: Add lookup registration for a reference Include reached through this visitor**

In the same `case IncludeExpression` block's non-collection branch (currently just
`_includedNavigations.Push(...); base.VisitExtension(includeExpression); ...` with no lookup call), add the
equivalent `LookupExpression`/`AddLookup` registration for a reference navigation — same pattern as the
collection branch, using the reference navigation's target entity type and foreign key instead of the
collection navigation's. Rewrite the `IncludeExpression` into a shape
`MongoProjectionBindingRemovingExpressionVisitor`/`MongoStreamingEntityMaterializerRewriter` can already read
for an ordinary (non-join-scope) reference Include — confirm by reading how the two ALREADY-PASSING bare
reference-Include tests' code path builds that same shape (Task 1's Step 6 findings tell you whether this is
the same visitor method or a different one; if different, mirror that method's output shape exactly, not this
one's guess).

- [ ] **Step 3: Remove (or widen) `BindResultMember`'s `allCollectionIncludes` guard for the reference case**

`BindResultMember` (`MongoQueryableMethodTranslatingExpressionVisitor.cs` ~line 3390) currently declines
(falls through to `BindSelectManyMember`'s fallback) whenever `allCollectionIncludes` is `false`. Now that
Step 2 gives a reference Include its own lookup, change this to rebind a reference-Include wrapper against
that new lookup instead of unconditionally declining — keep the existing collection-Include rebind-by-index
path intact for `IsCollection: true` wrappers, add a parallel rebind-by-lookup-alias path for
`IsCollection: false` wrappers, and only fall through to the pre-existing fallback if NEITHER path applies
(e.g. an embedded/owned reference nav, which Step 2's `IsEmbedded()` check already excludes upstream).

- [ ] **Step 4: Promote Shape 1's existing decline-only test to a correctness test**

`Whole_entity_leaf_that_is_also_reference_included_declines_rather_than_silently_returning_null`
(`NativeJoinTests.cs:1797`) currently accepts either a throw or correct data. Once Steps 2-3 land, it must
NEVER throw for this shape — tighten the assertion to require `threw == false` and the populated-Owner
assertions unconditionally. Rename it to drop "declines_rather_than" from the name (e.g.
`Whole_entity_leaf_that_is_also_reference_included_materializes_correctly`) since the decline is no longer the
expected behavior; update the doc comment to match.

- [ ] **Step 5: Add a permanent test for Shape 4 (the no-join control repro)**

Add to `NativeJoinTests.cs` (or, if Task 1's findings show Shape 4 does not touch join-scope machinery at
all, to `NativeReferenceIncludeTests.cs` instead — match the file to the arm actually exercised):

```csharp
[Fact]
public void Reference_Include_with_no_join_in_the_query_materializes_correctly()
{
    // Design doc Shape 4: db.Orders.Include(o => o.Owner).Select(o => new { o, o.OwnerId }) has no .Join()/
    // .LeftJoin() at all, grouped with Shape 3 as "likely the same bug, isolated". Must materialize a real
    // Owner, never null, now that Step 2/3's fix (or 3B's fix) is in place.
    var seed = SeedOwnersAndOrders();
    using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
        nameof(Reference_Include_with_no_join_in_the_query_materializes_correctly));

    var results = db.Orders.Include(o => o.Owner).Select(o => new { o, o.OwnerId })
        .AsEnumerable().ToList();

    Assert.NotEmpty(results);
    Assert.All(results, x => Assert.NotNull(x.o.Owner));
}
```

- [ ] **Step 6: Regenerate the two target spec tests' baselines and run them, plus Shape 4**

```bash
EF_TEST_REWRITE_BASELINES=1 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Include_reference_when_entity_in_projection|FullyQualifiedName~Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join"
```

Then re-run without the rewrite flag to confirm they pass and the diff to the 4 sibling `.cs` files' committed
`AssertMql` bodies is the expected new pipeline shape (see the spec-conformance-reviewer's own baseline
conventions in `tests/MongoDB.EntityFrameworkCore.SpecificationTests/AGENTS.md` if the rewrite behaves
unexpectedly).

```bash
dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj \
  -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NorthwindIncludeQueryMongoTest|FullyQualifiedName~NorthwindStringIncludeQueryMongoTest|FullyQualifiedName~NorthwindEFPropertyIncludeQueryMongoTest|FullyQualifiedName~NorthwindIncludeNoTrackingQueryMongoTest"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Reference_Include_with_no_join_in_the_query_materializes_correctly"
```

Expected: all 4 sibling classes' full suites PASS (not just the two target tests — this area's shared
machinery can regress siblings), and Shape 4's new test PASSES.

- [ ] **Step 7: Run the full regression sweep**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~Query"
```

Expected: PASS, including `NativeReferenceIncludeTests`, `NativeJoinTests`, `NativeJoinScopeProjectionBinderTests`.

- [ ] **Step 8: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoProjectionBindingExpressionVisitor.cs \
        src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeJoinTests.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeReferenceIncludeTests.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeQueryMongoTest.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindStringIncludeQueryMongoTest.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindEFPropertyIncludeQueryMongoTest.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindIncludeNoTrackingQueryMongoTest.cs
git commit -m "$(cat <<'EOF'
EF-322: materialize a reference Include on a native join-scope leaf correctly

Task 1's investigation confirmed Hypothesis A: Include_reference_when_entity_in_projection
and the mixed left-join Include test both reach BindResultMember's wrapped-leaf-join
arm, the same machinery root cause A2 fixed for collection Includes. Gives a reference
Include reached through MongoProjectionBindingExpressionVisitor its own $lookup
registration (mirroring the collection branch) and widens BindResultMember to rebind
against it instead of unconditionally declining.
EOF
)"
```

#### Branch 3B — Hypothesis B holds (Shape 3 is `TryConfirmReferenceIncludeChain`'s own, separate bug)

- [ ] **Step 1: Read `TryConfirmReferenceIncludeChain`'s full body and what runs after it**

Read `MongoQueryableMethodTranslatingExpressionVisitor.cs:1771` onward, and — since the method does not
`return` after confirming — trace what `TranslateSelect` does with a confirmed chain afterward (the generic
shaper-fold logic at the end of that method). Identify the exact point where a confirmed chain's reference
navigation should be, but isn't, resolved to a real value.

- [ ] **Step 2: Identify why a CONFIRMED chain still materializes null**

Likely candidates to check first (in order of how the design doc frames the mechanism): (a) confirmation
registers the join/lookup metadata but the shaper-fold path never builds a
`StructuralTypeShaperExpression`/`ProjectionBindingExpression` pointing at it — compare against how the
collection-Include lookup path (root cause A2, `MongoProjectionBindingExpressionVisitor`'s `IsCollection`
branch) both registers AND rewrites the shaper; (b) the lookup registers under one alias but the shaper reads
a different one (the `"_outer"`/`"_inner"` naming vs. `UsesDriverJoinFields` mismatch the design doc's Shape 3
section flags) — check `MongoQueryExpression.UsesDriverJoinFields`'s computed value at this point in
execution against what the shaper-fold path assumes.

- [ ] **Step 3: Fix the identified gap**

Write the fix once Step 2 names the exact mismatch — this cannot be pre-specified further without Task 1/this
branch's own Step 1-2 findings in hand. Whatever the fix, it must not touch `BindResultMember`'s
`allCollectionIncludes` guard or `MongoProjectionBindingExpressionVisitor`'s `IsCollection` branch (both
committed, root-cause-A2 territory, out of scope per this plan's Global Constraints) unless Step 1's trace
proves they are the SAME code executing for this shape too (in which case, stop, re-read the design doc's
Hypothesis A framing — this would mean the finding was wrong and 3A applies instead).

- [ ] **Step 4: Add a permanent test for Shape 4 (the no-join control repro)**

Same test and rationale as 3A Step 5 — add
`Reference_Include_with_no_join_in_the_query_materializes_correctly` to whichever test file matches the arm
Step 1 traced (likely `NativeReferenceIncludeTests.cs` under Hypothesis B, since Shape 4 has no join at all).

- [ ] **Step 5: Regenerate the two target spec tests' baselines and run them, plus Shape 4**

Same commands as 3A Step 6.

- [ ] **Step 6: Run the full regression sweep**

Same command as 3A Step 7.

- [ ] **Step 7: Commit**

```bash
git add <files Step 3 touched>
git commit -m "$(cat <<'EOF'
EF-322: fix TryConfirmReferenceIncludeChain's post-confirmation materialization gap

Task 1's investigation confirmed Hypothesis B: Include_reference_when_entity_in_projection
and the mixed left-join Include test reach TryConfirmReferenceIncludeChain's own,
separate machinery, unrelated to the join-scope BindResultMember arm root cause A2
touched. <fill in the specific mismatch Step 2 found>.
EOF
)"
```

---

