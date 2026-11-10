### Task 25: Correct the atomicity claim and document the real limitations

`README.md` says the `Where`-scoped path executes as "a single atomic `deleteMany` / `updateMany` server command", and the code comment says "Atomic at the server". **MongoDB's multi-document writes are atomic per document only** — concurrent readers can observe partial results, and a mid-operation failure leaves earlier documents modified with no rollback. That is a materially weaker guarantee than the relational `ExecuteDelete` users are porting from, where a single DML statement runs in an implicit transaction. Asserting the opposite is worse than saying nothing.

The README also does not mention that nested/owned setter targets, key properties, the discriminator and concurrency tokens are rejected, or that ordering/paging/`OfType` behaviour changed in this plan.

**Files:**
- Modify: `README.md`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `docs/failing-spec-tests.md`

**Interfaces:**
- Consumes: the behaviour established by Tasks 7, 9, 13, 14, 15, 18.
- Produces: nothing.

- [ ] **Step 1: Rewrite the README bullets**

Replace the four bullets added under "What is supported" with:

```markdown
- Bulk `ExecuteUpdate` and `ExecuteDelete` (EF 9+) on a single collection. Setters must target mapped root scalar properties and may use constant, parameter or self-referencing values. Bulk operations bypass the change tracker, so concurrency tokens are neither checked nor writable, and nested/owned properties, key properties and the type discriminator cannot be set
  - `Where`-scoped sources — optionally with `OrderBy`/`ThenBy`/`Distinct`, which do not affect which documents are matched — execute as a single `deleteMany` / `updateMany` server command. **This is not an atomic multi-document operation:** MongoDB guarantees atomicity per document only, so concurrent readers may observe partial results and a mid-operation failure leaves already-written documents changed. Wrap the call in an explicit transaction if you need all-or-nothing semantics
  - Sources that use `Skip`/`Take`, or `OfType`/`Cast`, are executed as a transactional two-phase operation (phase 1 collects the target `_id`s, phase 2 acts on them via `{ _id: { $in: [...] } }`); this requires a transaction-capable deployment (replica set or sharded cluster), and under `AutoTransactionBehavior.Never` the caller must open an explicit transaction. Large target sets are written in batches within that single transaction
  - Not supported: joins, `GroupBy`, `SelectMany`, set operations, cross-document navigation predicates, or multiple-collection updates
```

- [ ] **Step 2: Fix the in-code claim**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, find the architecture comment at the head of the bulk region and the single-command executor comments, and replace every occurrence of "atomic" with an accurate statement:

```csharp
    // Single-command path: one deleteMany/updateMany, executed entirely server-side with no transaction and no
    // documents transferred. Note this is NOT atomic across documents — MongoDB guarantees atomicity per
    // document only — so a partial failure leaves earlier documents written. Callers who need all-or-nothing
    // semantics must open their own transaction, which this path will then join.
```

Search for stragglers:

```bash
grep -rn "atomic" src/MongoDB.EntityFrameworkCore/ README.md
```

Every remaining hit must either be accurate or be reworded.

- [ ] **Step 3: Update `docs/failing-spec-tests.md`**

In the `EF-X016` section:
- Correct the "unifying cause" paragraph: two-phase is now selected by `Skip`/`Take`/`OfType`/`Cast`, not by ordering or `Distinct`.
- Move `OfType` out of the unsupported list (Task 15) and note that ordering-only and `Distinct`-only shapes are now single-command (Task 13).
- Add any conformance cases that moved in Tasks 7, 9, 11, 13 or 14, each with its root cause.
- Fix the duplicate ticket id: the table has **two** `EF-X016` rows — one for bulk sources and one for "GroupJoin shapes not translated" — and the count line at the end says "50 distinct ticket ids, including 16 temporary `EF-X###` placeholders". Renumber the bulk one to the next free id and update both the table and the closing count.

Verify the count with the command the document itself documents:

```bash
grep -rEho "(EF-X?[0-9]+|CSHARP-[0-9]+)" tests/MongoDB.EntityFrameworkCore.SpecificationTests \
  --include=*.cs | sort | uniq -c | sort -rn | wc -l
```

- [ ] **Step 4: Run the doc-adjacent tests**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~MongoComplianceTest|FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add README.md docs/failing-spec-tests.md \
        src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.SpecificationTests/
git commit -m "docs: correct the bulk atomicity claim and document the setter and strategy boundaries"
```

---

