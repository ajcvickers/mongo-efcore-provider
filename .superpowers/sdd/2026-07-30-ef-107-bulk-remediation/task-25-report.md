# Task 25 report — Correct the atomicity claim and document the real limitations

**Status:** DONE_WITH_CONCERNS (finished; concerns are doc-accuracy findings that went beyond the brief — see "Concerns")
**Commit:** `fd65bc3` — `docs: correct the bulk atomicity claim and document the setter and strategy boundaries`
**Branch:** EF-361 (was at `7c58035`)

All nine corrections were applied. The brief's own wrong claims (Corrections 1 and 2) were **not** copied.

---

## 1. Final README bullet text

`README.md:98-102`:

```markdown
- Bulk `ExecuteUpdate` and `ExecuteDelete` (EF 9+) on a single collection. Setters must target mapped root scalar properties, and may use constant, parameter or self-referencing values; nested/owned properties, key properties (which map to the immutable `_id` element) and the type discriminator cannot be set. Bulk operations bypass the change tracker, so concurrency tokens are neither checked nor protected — a bulk update may set one explicitly
  - `Where`-scoped sources — optionally with `OrderBy`/`ThenBy`/`Distinct`, which do not affect which documents are matched — execute as a single `deleteMany` / `updateMany` server command, without starting a transaction. **This is not an all-or-nothing multi-document operation:** MongoDB guarantees atomicity per document only, so concurrent readers may observe partially-applied results, and a mid-operation failure leaves already-written documents changed with no rollback. Wrap the call in an explicit transaction if you need all-or-nothing semantics
  - Sources that use `Skip`/`Take`, or `OfType` narrowing to a derived type, are executed as a transactional two-phase operation (phase 1 collects the target `_id`s; phase 2 acts on them via `{ _id: { $in: [...] } }`); this requires a transaction-capable deployment (replica set or sharded cluster), and under `AutoTransactionBehavior.Never` the caller must open an explicit transaction. Very large target sets are split across several phase-2 commands issued inside that one transaction. `OfType` to the collection's own root type is not a narrowing and stays on the single-command path
  - If a transaction is already open on the context, the operation enlists in it rather than starting its own — MongoDB does not support nested transactions, so a two-phase bulk op cannot open an inner transaction the way some relational providers do
  - Not supported: joins, `GroupBy`, `SelectMany`, `Cast`, set operations, cross-document navigation predicates, or multiple-collection updates
```

Correction compliance:
- **C1** — kept "concurrency tokens are neither checked nor protected — a bulk update may set one explicitly" verbatim in meaning. The word "writable" as a negative is **not** used anywhere.
- **C2** — `Cast` is in the *not supported* list; two-phase is `Skip`/`Take` + `OfType` narrowing only.
- **C3** — worded as `OfType` narrowing to a *derived* type, with an explicit sentence that root-type `OfType` stays single-command.
- **C4** — the transaction-enlistment bullet is retained unchanged (5 bullets, not 4).

---

## 2. Every "atomic" site and its disposition

| Site | Action |
|---|---|
| `src/.../Storage/MongoBulkPlan.cs:36` (enum doc) | **Reworded.** "a single atomic command" → "a single command with no transaction of its own", plus an explicit note that "single command" is not "atomic across documents". Contrast single-vs-two-phase preserved. |
| `src/.../Storage/MongoBulkPlan.cs:65` (`Strategy` summary) | **Reworded.** "a single atomic command" → "a single command". |
| `src/.../Storage/MongoBulkOperationExecutor.cs:69` (region banner) | **Reworded + expanded.** "one atomic deleteMany/updateMany, no transaction" → "one deleteMany/updateMany, no transaction started here", followed by a paragraph stating it is NOT atomic across documents, that a mid-operation failure has no rollback, and that a caller's own transaction is joined via `GetBulkCollectionAndSession` / `Database.CurrentTransaction`. |
| `src/.../Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs:430` | **Reworded.** "the atomic single-command path" → "the single-command path". |
| `MongoQueryableMethodTranslatingExpressionVisitor.cs:487-488` | **Reworded, contrast kept.** "would become non-atomic and require a transaction … stays a single atomic deleteMany" → "would be split into two phases and require a transaction … stays one deleteMany needing none". This is exactly the "one command, no transaction" vs "two phases, requires a transaction" contrast Correction 5 asked for. |
| `MongoBulkOperationExecutor.cs:232` (now `:237`) | **LEFT ALONE — accurate.** "Batching does not weaken atomicity: every batch is issued on the same session/transaction …". True: both phases share the session read from `Database.CurrentTransaction`, so batches commit/abort together. Task 18 verified with a one-`commitTransaction` assertion. |
| `MongoShapedQueryCompilingExpressionVisitor.cs:737` | **LEFT ALONE — accurate and unrelated.** About a volatile delegate-slot publication ("the reference assignment is atomic"), not multi-document writes. |
| `src/.../Storage/AGENTS.md:69-70` | **LEFT ALONE.** Both are about the *`SaveChanges`* `BulkWrite` path and are correct as written (`:69` warns against treating `BulkWrite` as the unit of atomicity; `:70` is about the row-version increment under `AutoTransactionBehavior.Never`). Not touched. |

Post-change sweep (`grep -rn atomic src/ README.md docs/failing-spec-tests.md --include=*.cs --include=*.md`) shows only the above; every remaining occurrence is accurate. **No expression was edited — `src/` changes are 100% comment/XML-doc text.** Verified: `git diff` on `src/` contains only comment lines.

Note: the earlier `grep` in the brief also matched `src/MongoDB.EntityFrameworkCore/bin/**/*.xml` build artifacts. Those are generated doc-XML and regenerate from the source comments; not edited.

---

## 3. The three Task 11 tags — what they ACTUALLY say vs. what the doc claimed

I read the tags rather than trusting either source. All three are in `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindBulkUpdatesMongoTest.cs`:

| Test | Guard | Actual tag + assertion |
|---|---|---|
| `Update_with_LeftJoin` (`:366`) | `#if EF10` | `// Fails: … joins / correlated subqueries unsupported EF-X016` (now `EF-X022`), `AssertTranslationFailed(...)` |
| `Update_with_LeftJoin_via_flattened_GroupJoin` (`:373`) | `#if EF10` | same tag, `AssertTranslationFailed(...)` |
| `Update_with_join_set_constant` (`:419`) | none (EF9+EF10) | same tag, `AssertTranslationFailed(...)` |

Each also carries an explicit re-tagging comment, e.g.:

> `// Re-tagged from EF-X002 (EF-361 task 11): this shape's captured chain ends in a non-entity Select projecting the join's TransparentIdentifier, which ClassifyBulkSource rejects as an unsupported top-level operator before any translation of the join itself is attempted -- so it already produced EF's canonical "could not be translated" message independently of task 11's phase-1/transaction ordering fix. The EF-X002 tag (a non-translation exception from the conformance asserter's snapshot read) was stale; verified empirically against both the pre- and post-fix code.`

**Verdict: the tags agree with your summary and contradict the doc.** They are tagged to the bulk ticket (not EF-X002) and assert with `AssertTranslationFailed` (exact type + message), and the stated mechanism is `ClassifyBulkSource`'s default arm at query-compile time — no `MongoBulkPlan` produced. The doc's claim (EF-117 made the join read-translatable; the asserter's snapshot read threw a driver exception before the bulk rejection) was wrong. It gives a specific mechanism the tags don't: rejection is triggered by the **trailing non-entity `Select` over the join's `TransparentIdentifier`**, not by the join operator itself. I used that mechanism in the corrected prose.

All three doc locations reconciled:
- `:84` — EF-X002 count `46` → `45` (verified tag count, see §6).
- `:113` — the EF-X002 prose listing now says these three **no longer sit here**, names `EF-X022`, states the stronger assertion, and explicitly labels the old explanation as wrong with the real cause.
- `:174` (now inside the moved `EF-X022` section) — the joins-bullet parenthetical rewritten: they belong to the joins group, were re-tagged from `EF-X002` in task 11, and are rejected at the classifier's default arm at query-compile time so no `MongoBulkPlan` is built.

---

## 4. The id renumbering

**Renumbered to `EF-X022`** (next free: the table ran `EF-X001`–`EF-X021`, with `EF-X016` used twice).

The **bulk** ticket was renumbered; the **GroupJoin** `EF-X016` (9 tests, `NorthwindJoinQueryMongoTest.cs`) keeps its number.

Files touched:
1. `tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/NorthwindBulkUpdatesMongoTest.cs` — all **47** `EF-X016` → `EF-X022`. Verified 0 `EF-X016` remain in that file, 47 `EF-X022` present, and that the file's `git diff` touches **comment lines only**. (The file has no BOM — it begins `/* ` — and the byte-level rewrite left the leading bytes unchanged.)
2. `docs/failing-spec-tests.md` — table row `:104`, the section heading, and every cross-reference (`:113` EF-X002 prose, the joins-bullet parenthetical, the closing count line).

I additionally **moved** the bulk section from between `EF-X015` and the GroupJoin `EF-X016` down to after the `EF-X021` section, so the `###` sections are in numeric order again (`…X019, X020, EF-216, X021, X022`). The table row needed no reordering — the bulk row was already last. Whitespace at both the cut and insert sites was checked.

`docs/failing-spec-tests.md:27` still references `EF-X016` inside the EF-117 row's re-categorization list; that reference is to the **GroupJoin** ticket and is correct as-is.

---

## 5. The verified count command output, quoted

Run after **all** edits (test-file renumbering included):

```
$ grep -rEho "(EF-X?[0-9]+|CSHARP-[0-9]+)" tests/MongoDB.EntityFrameworkCore.SpecificationTests --include="*.cs" | sort | uniq -c | sort -rn | wc -l
      57
```

EF-X placeholders actually present in the sources (20):

```
EF-X001 EF-X002 EF-X003 EF-X004 EF-X005 EF-X006 EF-X007 EF-X008 EF-X009 EF-X010
EF-X011 EF-X013 EF-X014 EF-X015 EF-X016 EF-X017 EF-X018 EF-X019 EF-X020 EF-X022
```

The doc's closing line said "50 distinct ticket ids, including 16 temporary `EF-X###` placeholders". **The doc was already stale before I touched it**: the pre-change baseline was **56** distinct ids / 19 placeholders. My renumbering added exactly one distinct id (`EF-X022` appears while `EF-X016` survives on the GroupJoin tests), taking it 56 → 57 and 19 → 20. Per your instruction I fixed the doc, not the command. New text:

> returns **57** distinct ticket ids, including **20** temporary `EF-X###` placeholders (`EF-X022` was added for the `NorthwindBulkUpdatesMongoTest` out-of-subset rejections — originally numbered `EF-X016`, which collided with the GroupJoin ticket of the same number, and was renumbered in EF-361 task 25). The 20 placeholders are `EF-X001`–`EF-X011`, `EF-X013`–`EF-X020` and `EF-X022`: `EF-X012` is obsolete (see its section above) and `EF-X021` tracks a shape no specification test currently exercises, so neither appears in the test sources.

Arithmetic check on that sentence: X001–X011 = 11, X013–X020 = 8, X022 = 1 → **20**. ✓ Matches the sorted list exactly, and both stated absences (X012, X021) are confirmed by the grep.

---

## 6. How the Task 22 comment was made true (Correction 6)

The comment is **not** at `ExecuteUpdateTests.cs:181` as the brief said — it is at **`:777-778`**, inside `ExecuteUpdate_targeting_an_owned_nested_property_throws`. (Line 181 is `ExecuteUpdate_returns_matched_count_even_when_values_already_match`, unrelated.)

It said: *"Nested/owned setter targets are outside the supported subset; the rejection is part of the contract and is stated in the README."* The README said nothing about it. Two-sided fix:
- The README first bullet now states it: "Setters must target mapped root scalar properties … nested/owned properties, key properties … and the type discriminator cannot be set."
- The comment now **quotes the README wording** so the cross-reference is checkable and will visibly rot if the README changes:

```csharp
// Nested/owned setter targets are outside the supported subset; the rejection is part of the contract
// and is stated in the README ("Setters must target mapped root scalar properties ... nested/owned
// properties, key properties ... and the type discriminator cannot be set").
```

Verified afterwards, not assumed: the quoted fragments appear verbatim in `README.md:98`.

---

## 7. Every factual claim written, with its substantiation

**README / doc claims about behaviour:**

| Claim | Substantiation |
|---|---|
| Setters must target mapped root scalar properties | `MongoQueryableMethodTranslatingExpressionVisitor.BuildSetter` `:296-304` — `property == null` → "Only mapped root scalar properties can be updated by a bulk update" |
| Nested/owned properties cannot be set | Same guard; test `ExecuteUpdateTests.ExecuteUpdate_targeting_an_owned_nested_property_throws` (`:771`) asserts that message for `o => o.Shipping.City` |
| Key properties cannot be set; map to immutable `_id` | `BuildSetter` `:310-315` — `property.IsPrimaryKey()` → "it is part of the primary key, which maps to the immutable '_id' element"; test `ExecuteUpdate_targeting_the_primary_key_throws` (`:526`) |
| Type discriminator cannot be set | `BuildSetter` `:317-322` — `property == entityType.FindDiscriminatorProperty()`; tests `ExecuteUpdate_targeting_the_discriminator_throws` (`:537`) and `…_when_scoped_to_a_derived_type` (`:549`) |
| Constant setter values | `ExecuteUpdate_constant_set_updates_matching_and_returns_count` (`:168`) |
| **Parameter** setter values (word I added) | `ExecuteUpdate_with_captured_variable_set_value` (`:211`); also `BuildSetter`'s parameter-dependent deferred-translation design (Query/AGENTS.md bulk-seam note) |
| Self-referencing setter values | `ExecuteUpdate_self_referencing_increments_matched_docs` (`:266`) |
| Concurrency tokens neither checked nor protected; a bulk update may set one | `BuildSetter` `:307-309` comment ("Concurrency tokens are deliberately NOT guarded here"), i.e. **no** guard exists; test `ExecuteUpdate_can_set_a_concurrency_token` (`:569`) asserts the **persisted** value, not just the count |
| `Where` + ordering/`Distinct` → single command | `ClassifyBulkSource` `:469-476` — `Where` and `IsAffectedSetNeutral(definition)` both leave `requiresTwoPhase` false |
| Single command starts no transaction but joins an open one | `MongoBulkOperationExecutor.GetBulkCollectionAndSession` `:713-719` reads `Database.CurrentTransaction`; `ExecuteDelete` `:82` uses `session == null ? DeleteMany(filter) : DeleteMany(session, filter)` |
| Single command is not atomic across documents | MongoDB per-document atomicity guarantee (the premise of this task); nothing in the code opens a transaction on this path |
| `Skip`/`Take` → two-phase | `ClassifyBulkSource` `:477-480` |
| `OfType` narrowing to derived → two-phase; root `OfType` stays single-command | `ClassifyBulkSource` `:481-499` — compares `method.GetGenericArguments()[0]` against `CollectionExpression.EntityType.ClrType`; test `BulkOperationsInheritanceTests.ExecuteDelete_with_OfType_of_the_root_type_stays_on_the_single_command_path` (`:180`) |
| `Cast` not supported, rejected by the classifier | `ClassifyBulkSource` `:500-504` default arm (with the "Cast falls through to here" comment) + the XML doc's rationale; test `BulkOperationsInheritanceTests.ExecuteDelete_with_Cast_is_rejected_by_the_bulk_source_classifier` (`:168`) asserting `"The 'Cast' operator is not supported in a bulk delete or update"` |
| The classifier's message names the operator and lists alternatives (unlike the driver's) | `ThrowBulkSourceNotSupported` `:548-557` — interpolates `{operatorName}` then lists `Where`/`OrderBy`/…/`Skip`/`Take`/`Distinct`/`OfType` |
| Two-phase requires a transaction-capable deployment; `AutoTransactionBehavior.Never` needs an explicit transaction | Pre-existing README text retained; `RunInBulkTransaction` / `IsTransactionsUnsupported` per Storage/AGENTS.md |
| Very large target sets split across several phase-2 commands inside the one transaction | `MongoBulkOperationExecutor` `:224-237` batching comment + `DefaultMaxIdsPerBulkCommand = 50_000` |
| Transaction enlistment / no nested transactions | Pre-existing README bullet retained (Correction 4) |
| `OfType` **is** translated on the read path | `TranslateOfType` exists in `MongoQueryableMethodTranslatingExpressionVisitor` (referenced by `ClassifyBulkSource`'s own comment at `:483-485`) |
| `OfType_Select` / `OfType_Select_OfType_Select` fail only on EF8/EF9, tagged EF-X020, pass on EF10 | Read directly: `NorthwindAggregateOperatorsQueryMongoTest.cs:1658-1690` — `#if EF8 || EF9` branch has the `EF-X020` tag + `AssertTranslationFailed`; `#else` calls `base.*` with a `$lookup` MQL baseline |
| Those two are cross-collection projections | The EF10 baseline MQL in that override contains `{"$lookup": {"from": "Customers", …}}` and projects `_v._inner.City` |
| Ordering/paging/`Distinct` conformance tests all call `base.*` | Read each: `Delete_Where_Distinct:156`, `Delete_Where_OrderBy:159`, `Delete_Where_Skip_Take_Skip_Take_causing_subquery:177`, `Update_Where_Distinct_set_constant:271`, `Update_Where_OrderBy*:303-316`, `Update_Where_Skip_set_constant:329`, `Update_Where_Take_set_constant:332` — all `=> base.…` |
| EF-X002 = 45 tags, 3 inside an `#if` | Scripted count over the SpecificationTests sources (see §8) |
| 47 bulk tags / 9 GroupJoin tags | `grep -c EF-X022` = 47 in `NorthwindBulkUpdatesMongoTest.cs`; `grep -c EF-X016` = 9 in `NorthwindJoinQueryMongoTest.cs` |
| 57 distinct ids / 20 placeholders | The documented count command, quoted in §5 |

---

## 8. Additional doc inconsistency found and fixed

The `EF-X002` section prose said "Affected: **~36** tests" while the table row said 46 (now 45) — a pre-existing contradiction that my table edit would have made more conspicuous. I reconciled it with a verified, precisely-stated number rather than leaving two numbers disagreeing:

> Affected: 45 `// Fails:` tags (3 of them inside an `#if` guard, so slightly fewer tests actually run on any single EF configuration).

Both figures come from a script that tracks `#if` nesting depth while scanning for `// Fails:` lines containing `EF-X002`: **45 tags, 3 guarded**. This also explains the table's `46`: raw `grep EF-X002` returns 49, of which **4** are prose mentions inside the three task-11 re-tagging comments (`Update_with_LeftJoin`'s comment mentions it twice), leaving 45 real tags. The old `46` was consistent with a 3-prose-mention count taken before task 11 added the fourth.

---

## 9. Test and build results

**Builds** — all three configurations, full solution, **0 warnings / 0 errors** each:

| Configuration | Result |
|---|---|
| `Debug EF8` | Build succeeded, 0 Warning(s), 0 Error(s) |
| `Debug EF9` | Build succeeded, 0 Warning(s), 0 Error(s) |
| `Debug EF10` | Build succeeded, 0 Warning(s), 0 Error(s) |

**Tests** — run with `MONGODB_URI` and `ATLAS_URI` unset (never set by me):

| Run | Result |
|---|---|
| `NorthwindBulkUpdatesMongoTest`, EF10 | **181/181 passed**, 0 failed — matches baseline |
| `MongoComplianceTest`, EF10 | **1/1 passed** |
| `NorthwindBulkUpdatesMongoTest`, EF9 | **169/169 passed**, 0 failed — matches baseline |
| `MongoComplianceTest`, EF9 | **1/1 passed** |
| Full `SpecificationTests`, EF10 | **4571 passed, 0 failed, 28 skipped** (4599 total) |

Two run-labelling oddities I chased down rather than assuming (both benign):
- The combined filter reports **182** on EF10, one more than the stated 181 baseline. Splitting the filter shows `NorthwindBulkUpdatesMongoTest` = 181 (exact baseline match) and `MongoComplianceTest` = 1. The 181 baseline is the Northwind count; compliance is the separate extra test.
- The EF9 runs report `(net10.0)`. That is correct: `MongoDB.EntityFrameworkCore.SpecificationTests.csproj:3` pins `<TargetFramework>net10.0</TargetFramework>` for **all** configurations (only `src/` drops to `net8.0` for EF8/EF9); the EF version is selected by the `EF9` define constant and the EF9 package references. The distinct 169-vs-181 count confirms EF9 really ran.

The full EF10 spec run was included specifically because I renumbered ids in test comments; it confirms nothing parses those tags at runtime.

---

## 10. Self-review

- Every factual claim written or kept is tabulated in §7 against a code site or test. Nothing is asserted from memory of the brief.
- **Both brief errors avoided.** "writable" never appears as a negative; `Cast` appears only in not-supported lists (README bullet, `EF-X022` strategy list). Root-type `OfType` is called out as staying single-command in both README and doc.
- **Nothing correct was deleted.** The transaction-enlistment bullet survives (Correction 4). The `MongoBulkPlan`/executor comments were reworded, keeping the single-vs-two-phase contrast intact; only the false guarantee was removed.
- **Newly stale?** Checked. No doc or comment now implies concurrency tokens are rejected (swept for "concurrency token" — remaining hits are all `SaveChanges`-path and correct). No doc still says `OfType` is unsupported in bulk. The phrase "restricted to a single collection scoped by `Where`" is gone from live docs (it survives only in `docs/agentplans/**` and `docs/superpowers/plans/**` historical records, which I left alone as point-in-time artifacts).
- **Count line arithmetic:** 11 + 8 + 1 = 20 placeholders; 57 distinct — both re-verified against the grep output after the commit. ✓
- `src/` diff is comment-text-only; no expression, signature, or resource string touched. `MongoEventId` untouched; no redaction or Query↔Storage seam change.

---

## 11. Concerns

1. **`EF-X012` is more broken than Correction 7 assumed — and I did not do what it literally said.** Correction 7 asked me to "correct the cause to describe what actually fails". In fact **`EF-X012` no longer appears anywhere in the test sources** (`grep -r EF-X012 tests/` → nothing), and the two tests **pass on EF10**; on EF8/EF9 they are tagged `EF-X020` (cross-collection Include/join). The real cause was never `OfType` at all — it is the cross-collection `$lookup` from `OfType<Order>().Select(o => o.Customer.City)`. So there is no live failure left to describe a cause for. Honouring "do not re-tag or re-enable those tests" (they were already re-tagged by earlier EF-117/EF-X020 work, not by me), I marked the ticket **obsolete** with the correction recorded, and set its count to 0. **If you wanted the row deleted outright rather than kept as an obsolete marker, say so and I'll change it** — I kept it so the numbering stays stable and the correction stays discoverable, matching the precedent of `EF-X021` (also count 0).

2. **The doc's counts are stale well beyond the three places Correction 8 named.** The closing count was 50 vs an actual pre-change 56. Spot-checking the table: `EF-X001` says 144 (raw grep 165), `EF-X003` says 19 (18), `EF-X020` says 168 (172). I corrected only what the corrections named (`EF-X002`, `EF-X012`, the bulk row, the closing count) plus the one contradiction my own edit exposed (§8). **The other table counts remain stale and I did not touch them** — out of scope, and each needs the tag-vs-`#if` disambiguation of §8 to state honestly. Worth a follow-up.

3. **"Count" in that table is not a well-defined quantity.** It sits between "number of `// Fails:` tags" and "number of tests that fail on a given EF configuration", and `#if`-guarded overrides make those differ. I made the one number I changed self-describing ("45 `// Fails:` tags (3 … inside an `#if` guard)"), but the column as a whole is ambiguous.

4. **I moved the `EF-X022` section** to restore numeric ordering of the `###` sections. That is a larger diff than a pure rename and was not requested; revert that hunk if you would rather it stayed put between `EF-X015` and the GroupJoin `EF-X016`.

5. **One claim rests on external knowledge, not this repo:** that MongoDB guarantees atomicity per document only. That is the task's own premise and is not verifiable from the codebase; it is the basis of the corrected wording throughout.

---

# Fix report — review round 1

**Commit:** `5f4fcb1` — `docs: drop the last two atomicity claims from tests and align the bulk subset wording`

All five review items addressed. Comment-only again: `git diff -U0 -- src/ tests/ | grep '^+' | grep -vE '^\+\s*(//|///)'` returns **empty**, i.e. every added line in `src/` and `tests/` is a `//` or `///` comment. Only `docs/failing-spec-tests.md` changed outside comments (one prose line).

The reviewer was right that my §2/§10 completeness claims rested on a grep that structurally could not see `tests/`. That was a real gap in the verification, not just in the wording — two instances of the exact phrase this task exists to remove were sitting in a file I had touched 47 times.

## Important 1 — the two surviving "atomic" claims, plus the stale header

| Site | Fix |
|---|---|
| `tests/…/SpecificationTests/Query/NorthwindBulkUpdatesMongoTest.cs:38` | "stay on the **atomic** single-command path instead" → "stay on the single-command path instead". Exactly the phrase retired from `MongoQueryableMethodTranslatingExpressionVisitor.cs:430`. |
| `tests/…/FunctionalTests/Query/BulkOperationsInheritanceTests.cs:185` | "would needlessly **become non-atomic** and fail outright under `AutoTransactionBehavior.Never`" → "would needlessly be split into two phases and require a transaction -- failing outright under `AutoTransactionBehavior.Never` or on a standalone deployment -- unlike the equivalent `db.Entities.ExecuteDelete()`, which stays one deleteMany needing none." Now mirrors the `src/` comment at `:487-488` that this test pins. |
| `NorthwindBulkUpdatesMongoTest.cs:30` header | Was "a single collection, scoped by Where, with constant / parameter / self-referencing scalar setters" — the wording retired from the `EF-X022` heading. Now states the full subset: mapped root scalar setter targets with constant/parameter/self-referencing values; sources scoped by `Where`, ordering or `Distinct` (single-command), or `Skip`/`Take` or `OfType` narrowing to a derived type (two-phase); nested/owned, key and discriminator targets unsettable; `Cast` unsupported. |

## Minor 3 — disambiguation applied to the row I own

`docs/failing-spec-tests.md:232` — "Affected: 47 tests" → "Affected: 47 `// Fails:` tags in `NorthwindBulkUpdatesMongoTest.cs` (9 inside per-EF `#if EF9`/`#if EF10` guards, on top of the file-level `#if !EF8`, so no single EF configuration runs all 47)."

Both numbers scripted with `#if`-depth tracking, not eyeballed: **47 tags, 9 per-EF-guarded** — 7 under `#if EF10` (tag lines 219, 225, 231, 362, 374, 381, 387) and 2 under `#if EF9` (249, 426). These are the same nine sites the reviewer cited at `:216/:222/:228/:246/:359/:371/:378/:384/:423`; the reviewer's numbers are the `#if` directive lines, mine are the tag comment lines three lines below each.

## Minor 5 — `MongoComplianceTest.cs:118`

"scoped by Where / ordering / paging with scalar setters" → "scoped by Where / ordering / Distinct / paging / OfType narrowing, with scalar setters". Added `Distinct` alongside `OfType`, since `Distinct` was equally absent from that enumeration.

## Minor 6 — the contradicting `src/` XML doc

`MongoQueryableMethodTranslatingExpressionVisitor.cs:436-438` flatly said "`OfType` also changes the affected set … and requires the same two-phase path". Rewritten to carry the carve-out the inline comment at `:471-476` and both documents already state:

```
/// An <c>OfType</c> that <i>narrows</i> — i.e. whose generic argument differs from the
/// queried collection's entity type — also changes the affected set and requires the same two-phase path.
/// (<c>OfType&lt;TRoot&gt;()</c> naming the root type itself narrows nothing and deliberately stays on the
/// single-command path; see the inline comment on that check below.) The reason <c>OfType</c> needs two
/// phases differs from paging's: phase 1 reuses the read path's translation of the operator …
```

## The completeness sweep — exactly what was covered this time

```
grep -rn "atomic" --include="*.cs" --include="*.md" --include="*.csproj" \
  --include="*.props" --include="*.xml" --include="*.json" --include="*.yml" . \
  | grep -v "/bin/" | grep -v "/obj/" \
  | grep -vE "docs/(agentplans|superpowers)/" | grep -v "^\./\.superpowers/"
```

Repository root, all subdirectories — `src/`, `tests/` (both projects), `docs/`, `benchmarks/`, `evergreen/`, `.claude/`, and the root `*.md`. Excluded: `bin/`/`obj/` build output (generated doc-XML, regenerates from source comments) and the point-in-time plan archives under `docs/agentplans/`, `docs/superpowers/` and `.superpowers/` (historical records of what was decided when; deliberately not rewritten).

**14 occurrences remain, every one accurate.** The four I authored (`README.md:99`, `docs/failing-spec-tests.md:242`, `MongoBulkPlan.cs:38`, `MongoBulkOperationExecutor.cs:71`) all state per-document atomicity as a *limitation*. The other ten:

| Occurrence | Why it is accurate |
|---|---|
| `MongoBulkOperationExecutor.cs:237` | Batching claim, left alone per Correction 5 — both phases share the session from `Database.CurrentTransaction`, so batches commit/abort together. Task 18 verified. |
| `MongoShapedQueryCompilingExpressionVisitor.cs:737` | Volatile delegate-slot publication; unrelated to writes. |
| `ExecuteDeleteTests.cs:540` | Cross-reference: "the atomicity claim in `MongoBulkOperationExecutor`'s comment". Read it to confirm the reference still resolves — it describes the one-`commitTransaction` batching assertion, i.e. the `:237` comment I left intact, not the region banner I reworded. Still valid. |
| `TransactionTests.cs:313`, `:340` | Test names: `Multiple_SaveChanges_inside_single_explicit_transaction_are_atomic_until_commit(_async)`. Atomicity from an explicit transaction, which is real. |
| `CrossCollectionRelationshipTests.cs:172` | `Write_multi_collection_atomicity_rolls_back_principal_on_dependent_failure` — transaction-provided, and the test asserts the rollback. |
| `Storage/AGENTS.md:69`, `:70` | `SaveChanges` `BulkWrite` path; `:69` explicitly warns *against* treating `BulkWrite` as the unit of atomicity. Left per Correction 5. |
| `.claude/agents/storage-reviewer.md:20`, `:21` | Mirrors the above two for the reviewer agent; `:21` says cross-collection atomicity is the transaction's job, not `BulkWrite`'s. Consistent. |

So the completeness claim now holds over the paths listed above, and the two paths excluded are named rather than silently omitted.

## Verification

| Check | Result |
|---|---|
| `Debug EF8` build | 0 Warning(s), 0 Error(s) |
| `Debug EF9` build | 0 Warning(s), 0 Error(s) |
| `Debug EF10` build | 0 Warning(s), 0 Error(s) |
| `NorthwindBulkUpdatesMongoTest`, EF10 | **181/181 passed**, 0 failed |
| `MongoComplianceTest`, EF10 | **1/1 passed** |
| `NorthwindBulkUpdatesMongoTest`, EF9 | **169/169 passed**, 0 failed |
| `MongoComplianceTest`, EF9 | **1/1 passed** |
| Count command | **`57`** — unchanged, as expected: this round edited comment prose only and touched no ticket id |
| Comment-only check | `git diff -U0 -- src/ tests/` adds only `//` / `///` lines |

`MONGODB_URI` and `ATLAS_URI` remained unset throughout.

## Concerns

None new. The scoped-out items (`EF-X001`/`EF-X003`/`EF-X020` stale table counts) were not touched, per instruction. The five carried-over concerns from the original report all stand as written, and the coordinator has confirmed the `EF-X012` obsolete-row treatment and the section move should both stay.
