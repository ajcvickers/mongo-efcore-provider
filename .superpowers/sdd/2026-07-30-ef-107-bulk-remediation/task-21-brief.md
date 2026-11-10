### Task 21: Give the async path the same shape coverage as the sync path

Sync and async are separately implemented, and in phase 1 they now use different cursor handling. Async coverage is `ExecuteDeleteAsync_with_predicate`, `ExecuteUpdateAsync_self_referencing`, and whatever the diagnostics theories incidentally exercise — one two-phase shape. The composite-key test matters most: `_id` sub-document extraction is the subtlest part of phase 1 and has no async twin.

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing.

- [ ] **Step 1: Convert the two-phase tests to theories**

For each of these tests, change `[Fact]` to `[Theory] [InlineData(false)] [InlineData(true)]`, add a `bool async` parameter, make the method `async Task`, and replace the operation call with the conditional form already used by `ExecuteDelete_two_phase_logs_ExecutingBulkDelete_with_target_count`:

In `ExecuteDeleteTests.cs`: `ExecuteDelete_with_OrderBy_Take_deletes_lowest_quantities`, `ExecuteDelete_with_Where_and_Skip_Take_scopes_to_window`, `ExecuteDelete_two_phase_empty_target_returns_zero`, `ExecuteDelete_two_phase_rolls_back_inside_user_transaction`, `ExecuteDelete_two_phase_commits_inside_user_transaction`, `ExecuteDelete_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing`, `ExecuteDelete_with_Distinct_deletes_matching`, `ExecuteDelete_two_phase_with_composite_key_deletes_targeted_rows`, `ExecuteDelete_with_two_Where_clauses_deletes_matching_both_and_returns_count`.

In `ExecuteUpdateTests.cs`: `ExecuteUpdate_with_OrderBy_Take_updates_only_targeted_rows`, `ExecuteUpdate_two_phase_self_referencing_setter`, `ExecuteUpdate_two_phase_rolls_back_inside_user_transaction`, `ExecuteUpdate_two_phase_empty_target_returns_zero`, `ExecuteUpdate_two_phase_returns_matched_count_even_when_values_already_match`, `ExecuteUpdate_with_Where_and_Skip_Take_scopes_to_window`, `ExecuteUpdate_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing`.

The mechanical shape, using the composite-key test as the worked example:

```csharp
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_two_phase_with_composite_key_deletes_targeted_rows(bool async)
    {
        using var db = CreateSeededCompositeKeyContext();

        var deleted = async
            ? await db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDeleteAsync()
            : db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(1, db.Entities.Count());
    }
```

Keep the body of each test otherwise unchanged — read the existing assertions and preserve them exactly rather than rewriting them.

For the two `AutoTransactionBehavior.Never` tests, the exception is thrown before the first `await`, so the async assertion must be `await Assert.ThrowsAsync<InvalidOperationException>(() => ...ExecuteDeleteAsync())` — this is worth pinning precisely because sync-throwing async methods behave differently.

- [ ] **Step 2: Run both suites on both EF versions**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteDeleteTests|FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS, with roughly double the test count in the two-phase group. Any async-only failure is a genuine sync/async divergence — fix the production code, not the test.

- [ ] **Step 3: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/
git commit -m "test: exercise every two-phase bulk shape on the async path as well as the sync path"
```

---

