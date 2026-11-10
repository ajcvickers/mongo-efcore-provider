### Task 8: Retire the superseded decline test; regression pass; multi-EF-version check

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionAllTests.cs:518-543`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionFilteredCountTests.cs` (check for an analogous correlated-decline test; read the file first)

**Interfaces:** None new — this task only updates test expectations to match the now-correct (native) behavior, and runs the full multi-version regression suite.

- [ ] **Step 1: Read the current state of the superseded test**

Read `NativeOwnedCollectionAllTests.cs`'s `All_with_a_correlated_element_predicate_declines_and_falls_back_to_correct_rows` (around line 518-543, shown in full in this plan's research — reproduced here for reference):

```csharp
    [Fact]
    public void All_with_a_correlated_element_predicate_declines_and_falls_back_to_correct_rows()
    {
        // Post.Title collides with Blog.Title, so a mis-scoped owner-rooted condition would select DIFFERENT
        // rows — which is what makes this decline test discriminating rather than vacuous.
        // ...
        var collection = Seed(nameof(All_with_a_correlated_element_predicate_declines_and_falls_back_to_correct_rows),
            Row("match", new BsonArray { PostDoc(rank: 9, heading: "a", title: "other") }),
            Row("other", new BsonArray { PostDoc(rank: 9, heading: "a", title: "match") }));

        var titles = AssertDeclinesCleanly(collection, q => q.Where(b => b.Posts.All(p => b.Title == "match")));
        Assert.Equal(new[] { "match" }, titles);
    }
```

- [ ] **Step 2: Rewrite it to assert the new (native) behavior**

Replace the method with:

```csharp
    [Fact]
    public void All_with_a_correlated_element_predicate_now_goes_native_since_EF421()
    {
        // SUPERSEDED by EF-421: this shape used to decline outright (see git history for the pre-EF-421
        // version of this test, which asserted AssertDeclinesCleanly). It is now natively representable via
        // a two-scope translator + $allElementsTrue — same seed, same expected rows (the correlated
        // predicate `b.Title == "match"` does not depend on p at all, so All reduces to whether the OWNER's
        // Title is "match" AND the blog has at least one post — "match" qualifies on both counts; "other"
        // fails the Title check). Post.Title collides with Blog.Title, so a mis-scoped (element-rooted)
        // resolution would select DIFFERENT rows, keeping this test discriminating rather than vacuous.
        var collection = Seed(nameof(All_with_a_correlated_element_predicate_now_goes_native_since_EF421),
            Row("match", new BsonArray { PostDoc(rank: 9, heading: "a", title: "other") }),
            Row("other", new BsonArray { PostDoc(rank: 9, heading: "a", title: "match") }));

        var titles = AssertNativeOnlyMatches(collection, q => q.Where(b => b.Posts.All(p => b.Title == "match")));
        Assert.Equal(new[] { "match" }, titles);
    }
```

- [ ] **Step 3: Check `NativeOwnedCollectionFilteredCountTests.cs` for an analogous case**

Run: `grep -n "correlat" tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionFilteredCountTests.cs`

If a test asserting a decline for a correlated `Count(pred)` exists, apply the same transformation as Step 2 (swap `AssertDeclinesCleanly` for `AssertNativeOnlyMatches`/`AssertNativeAndParity`, whichever helper that file defines, and update the doc comment). If none exists, note in the commit message that no analogous test was found there (the correlated-Count coverage lives entirely in the new `NativeOwnedCollectionCorrelatedTests.cs` file from Task 3).

- [ ] **Step 4: Also check `NativeOwnedCollectionPredicateTests.cs` (the `Any` sibling) for the same pattern**

Run: `grep -n "correlat" tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionPredicateTests.cs`

Apply the same transformation if found.

- [ ] **Step 5: Run the full FunctionalTests suite for EF10**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests -c "Debug EF10" --no-build`
Expected: 100% pass, zero failures, zero skips beyond pre-existing documented skips (e.g. encryption tests without `CRYPT_SHARED_LIB_PATH`).

- [ ] **Step 6: Run the full UnitTests suite for EF10**

Run: `dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests -c "Debug EF10" --no-build`
Expected: 100% pass.

- [ ] **Step 7: Run the `/test-all` skill for full multi-EF-version confirmation**

Invoke the `/test-all` skill (per this repo's `AGENTS.md`: "Build + test all three EF versions in parallel"). Expected: EF8, EF9, and EF10 all pass with an IDENTICAL pass/fail set (no `#if` was added anywhere in this change, so no version-specific divergence is expected — if one appears, investigate before proceeding rather than assuming it's unrelated).

- [ ] **Step 8: Update `Query/AGENTS.md`'s capability summary**

Add a short paragraph to the "Owned navigations" bullet (or a new bullet, matching this file's existing terse-but-precise style) documenting: correlated `Count(pred)`/`Any`/`All` now go native via a two-scope translator (`MongoOuterFieldExpression`) for `Count(pred)` (rendered inside the existing `$filter`) and a new `$anyElementTrue`/`$allElementsTrue`-over-`$map` dialect (`MongoQuantifierExpression`) for quantifiers, since `$elemMatch` cannot reference the enclosing document at all. Reference EF-421. Follow the file's established convention of noting what's now native, what still isn't (correlation nested two-or-more scopes deep), and the one durable invariant worth keeping (`ReferenceEquals` against `SelfParam`, never by name).

- [ ] **Step 9: Final commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionAllTests.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionFilteredCountTests.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/NativeOwnedCollectionPredicateTests.cs \
        src/MongoDB.EntityFrameworkCore/Query/AGENTS.md
git commit -m "EF-421: update superseded decline tests and capability docs for correlated element predicates"
```

---

## Self-Review

**Spec coverage:**
- §1 (`_selfParam` capture) → Task 1, Task 7.
- §2 (identifying the correlation target) → Task 1 (`FreeParameterVisitor`), Task 3/5 (the `ReferenceEquals` branch at both call sites).
- §3 (`MongoOuterFieldExpression`) → Task 2, Task 3.
- §4 (correlated `Count(pred)`) → Task 3.
- §5 (`MongoQuantifierExpression`, correlated quantifiers) → Task 4, Task 5.
- §6 (relativizing `TryResolveOwnedFieldPath`/`TryResolveOwnedCollectionPath`) → Task 6.
- §7 (`MongoFieldPrefixRewriter` pass-through) — **investigated during plan-writing, not a separate task**: `MongoFieldPrefixRewriter` (referenced in `MongoElemMatchExpression`'s/`MongoFilteredSizeExpression`'s own doc comments) is only ever invoked by `NativeSelectManyBinder` to prefix an INNER-scope tree with an unwind path; the new correlated Count/quantifier paths never call it (their two-scope translator resolves prefixes internally, and `innerPrefix: ""` for `Count(pred)` means there is nothing to rewrite). Confirmed via `grep -rn "MongoFieldPrefixRewriter" src/` — only `NativeSelectManyBinder.cs` calls it. No plan task touches this file; §7 is a non-issue as the spec itself anticipated.
- Testing section → Tasks 3, 5, 6, 7 (differential/NativeOnly tests), Task 8 (regression + multi-version).
- Risks/invariants → called out inline in Tasks 3, 5 (decline-else-build-two-scope discipline) and enforced by Global Constraints.

**Placeholder scan:** no TBD/TODO; the one open judgment call (Task 7's aggregate-test expected value) is resolved with an explicit worked-out instruction, not left vague — flagged with "Author note" precisely because it's arithmetic that must be hand-verified against the literal seed, not because the design is undecided.

**Type consistency:** `MongoOuterFieldExpression(IProperty Property, string ElementName)` — used consistently across Tasks 2, 3, 6. `MongoQuantifierExpression(MongoElementRefExpression ArrayPath, MongoExpression ElementPredicate, MongoQuantifierKind Kind)` — used consistently across Tasks 4, 5. `TryResolveMember(..., out bool isOuter)` and `TryResolveOwnedCollectionPath(..., out bool isOuter)` / `TryResolveOwnedFieldPath(..., out bool isOuter)` signatures introduced in Tasks 3/6 are used identically at every call site listed in those same tasks. `SelfParam` (not `_selfParam` publicly — the backing storage is a plain auto-property) is named consistently in Tasks 1, 3, 5, 7.
