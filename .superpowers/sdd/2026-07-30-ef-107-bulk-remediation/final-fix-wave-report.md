# EF-361 — final fix wave report

Branch `EF-361`, starting from `f883aaf` (clean tree). One fix wave, findings 1–8, nothing from the
out-of-scope list.

---

## Finding 1 (Important) — the encryption suite tested an operation Queryable Encryption forbids

`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs` was rewritten.

**Model change (the second, independent cause).** `MedicalContext.OnModelCreating` — this file's own context,
not `AutoEncryptionTestsBase` — now calls `Navigation(...).AutoInclude(false)` for all four of `Patient`'s owned
navigations (`BloodPressureReadings`, `WeightMeasurements`, `LongTermCarePlan`, `BillingAddress`).

This is the one thing in Finding 1 I *could* verify here, and I did, with a throwaway probe (added, run, deleted
— not in the diff) that built the model from the real `AutoEncryptionTestsBase.Patient` and ran a plain-setter
`ExecuteUpdate`:

| Configuration | Without `AutoInclude(false)` | With `AutoInclude(false)` |
|---|---|---|
| `Debug EF9` | `InvalidOperationException: The LINQ expression 'DbSet<Patient>().Select(p0 => Include(Entity: Include(...` | `IsEagerLoaded=False` on all four; update succeeds |
| `Debug EF10` | `IsEagerLoaded=True` on all four; update succeeds | `IsEagerLoaded=False` on all four; update succeeds |

So the defect is real on EF9, the workaround is correct and sufficient, and all four `Navigation(...)` calls are
legal against the convention-discovered owned navigations (they are *not* declared with `OwnsOne`/`OwnsMany`
anywhere, which was the risk — had `Navigation()` thrown "navigation not found", every test in the file would
have failed at model build and I could not have seen it, because they are all skipped here).

Siblings are untouched: `AutoEncryptionTests`, `QueryableEncryptionTests` and `ExplicitEncryptionTests` build
their own models and `MedicalContext` is private to this file. Verified by enumeration parity (below).

**Tests 1 and 2 inverted.** They now assert the operation is *rejected*:

- `Bulk_delete_filtered_on_an_encrypted_equality_field_deletes_the_matching_documents`
  → `Bulk_delete_on_a_queryable_encrypted_collection_is_rejected_and_destroys_nothing`
- `Bulk_update_setting_an_encrypted_field_stores_it_encrypted`
  → `Bulk_update_of_an_encrypted_field_is_rejected_and_writes_no_plaintext`

**Exception typing (all four tests).** Every test now uses `Assert.ThrowsAny<MongoException>(() => <the bulk
call only>)`. There are no `catch (Exception)` blocks left in the file — the lenient try/catch shape is gone
entirely, so there is nothing left that could swallow an `Xunit.Sdk.EqualException` or a provider
`NullReferenceException` and report it as the expected rejection.

`MongoException` is the driver's *base* exception, not a specific rejection type. The justification I can make
from here: the rejection has to originate in libmongocrypt, the driver or the server, and the provider's
single-command bulk path wraps neither `DeleteMany` nor `UpdateMany` in a `try`/`catch`
(`MongoBulkOperationExecutor.cs:87` and `:126`), so a driver failure propagates raw. What it excludes is an
in-process failure that is not a driver failure. The concrete subtype (`MongoEncryptionException` vs a server
`MongoCommandException`) is deliberately **not** asserted and is called out in the file header as unverified.

**Vacuous assertions fixed.**

- Test 4 (`..._fails_closed`) previously asserted only `raw["Sequence"].BsonType == Binary`. It now (a) compares
  every stored document, whole, against a pre-operation snapshot; (b) asserts no element name appeared that was
  not there before — the exact shape a wrong-element-name write takes, which is the `GetSetElementKey` bug
  class; and (c) scans every `Int32` value at any depth for the incremented plaintext `11`/`21`. I used a typed
  value scan rather than the suggested `raw.ToJson()` substring search here specifically: `"11"` and `"21"`
  occur constantly inside ObjectId hex and base64 ciphertext, so a substring assertion would have been a
  false-positive generator. In test 2, where the literal is the distinctive `"000-00-0000"` (a base64/hex
  alphabet cannot contain `-`), I did use the `raw.ToJson()` form as suggested.
- Test 3 (`..._does_not_expose_an_encrypted_sibling_field`) now asserts `Sequence`'s actual values — `10`/`20`,
  the seeded values, on the rejection path — instead of only the encrypted bystander's BSON type.

**File header fixed** — it now states plainly that all four tests are QE-only, why (`EncryptedFields` on the
collections, KMS with no `schemaMap` on the client), that CSFLE is not covered, and that nothing in the file has
ever executed here.

**No CSFLE suite was written**, per instruction. Noting it for a ticket: CSFLE may well permit multi-document
writes (its restriction model is different — it encrypts fields under a `schemaMap` rather than maintaining
server-side metadata), in which case bulk operations against a CSFLE collection could be a supported and
untested combination. That premise is unverified. Writing a second suite that cannot execute here would have
repeated exactly the mistake this finding is about.

---

## Finding 2 (Important) — README advertised QE and bulk adjacently

`README.md`, two bullets added to the bulk-operations list:

- Queryable Encryption: stated as unsupported, with the reason (every bulk operation is an
  `updateMany`/`deleteMany`; QE forbids multi-document writes), a link to the QE limitations page, the
  workaround (`SaveChanges`, which writes per document), and CSFLE's status stated as **unverified** rather
  than asserted either way.
- The derived-`DbSet` rejection from Finding 3, since it is now a user-visible provider error.

No up-front provider error for encrypted-collection targets was implemented — that is a feature change and
belongs in a ticket, as instructed.

---

## Finding 3 (Important) — derived TPH `DbSet<T>` — **REPRODUCED**, guard added

**It reproduces.** I wrote the test first, ran it against unmodified `f883aaf` code, and instrumented it to
print what actually happened rather than just "no exception thrown". Verbatim probe output, `Debug EF10`,
against a context with both `DbSet<Animal>` and `DbSet<Cat>` seeded with two Cats and one Dog:

```
PROBE deleted=3 remaining=0 kinds=[]
PROBE updated=3 names=[cat:renamed,cat:renamed,dog:renamed]
```

`db.Cats.ExecuteDelete()` destroyed all three documents including the Dog. `db.Cats.ExecuteUpdate(...)` renamed
all three including the Dog. Exactly what the reviewer predicted from code reading.

**Guard added.** `MongoShapedQueryCompilingExpressionVisitor.EnsureBulkRootTypeOrThrow`, called unconditionally
at the top of `VisitNonQuery` (before the two-phase key check), rejects when
`CollectionExpression.EntityType.BaseType != null`. The message goes through
`CoreStrings.NonQueryTranslationFailedWithDetails` like the sibling guard, names the derived type and the shared
collection, and points at `OfType<T>()`.

**`OfType` is not affected**, verified rather than assumed: all seven pre-existing
`BulkOperationsInheritanceTests` still pass, including
`ExecuteDelete_with_OfType_phase1_query_carries_the_discriminator_filter_on_the_wire`. `TranslateOfType` swaps
only the shaper's structural type and leaves `CollectionExpression.EntityType` as the root, so `BaseType` is
null on that path.

**Tests kept**, in `BulkOperationsInheritanceTests`:
`ExecuteDelete_on_a_derived_DbSet_does_not_destroy_the_rest_of_the_hierarchy` and
`ExecuteUpdate_on_a_derived_DbSet_does_not_modify_the_rest_of_the_hierarchy`, plus the
`RootAndDerivedDbSetContext` they need (the file comment records why
`DiscriminatorTests.MultiEntityContext` is not this shape).

*What would have to break for these to fail:* the guard stops rejecting entity types with a `BaseType` (or its
message drifts off "derived entity type"); or, if a real discriminator filter is added later and the guard
removed with it, the filter fails to scope the operation — caught by the surviving-rows assertions (3 documents
remain, the Dog among them, still named Rex). They cannot pass vacuously: the pre-guard behaviour leaves zero
rows for the delete and a renamed Dog for the update, and both were observed.

This is a **behaviour change for a shape that previously "worked"** — `db.Cats.ExecuteDelete()` now throws
instead of silently destroying the collection. Per `AGENTS.md`, a guard rejecting something the provider doesn't
support is not a breaking change; and the prior behaviour was data loss.

---

## Finding 4 (Important) — empty `[Fact(Skip)]` deleted, extraction **landed cleanly**

**Placeholder deleted.** `ExecuteDeleteTests.ExecuteDelete_two_phase_on_standalone_throws_actionable_error` is
gone, replaced by a comment in the same position explaining why there is no test there and where the coverage
actually lives (the file's existing convention for absent tests — see the `Order()`/`OrderDescending()` note
just above it). Its Skip reason also pointed at the wrong path (`UnitTests/Query/...`; the file is under
`UnitTests/Storage/`); the replacement comment is correct.

**Extraction: clean, no revert.** `MongoBulkOperationExecutor.MapBulkTransactionFailure` is now the single
decision point, called from all four sites (sync/async × startup/body). The move was mechanical: no signature
changed, no `catch` widened, and `IsTransactionsUnsupported` is unchanged and still `internal` for the existing
tests.

One deliberate deviation from the reviewer's suggested `static Exception MapBulkTransactionFailure(Exception)`:
mine returns `InvalidOperationException?` — null when unmatched. A total `Exception -> Exception` mapping would
force the callers to `throw mapped;` on the unmatched path, and rethrowing the caught exception object resets
its stack trace. On the body-side path that stack trace is the only record of where inside phase 1 or phase 2
the failure came from. The nullable return lets the callers keep a bare `throw;`. The startup sites use
`catch (Exception e) when (MapBulkTransactionFailure(e) is { } mapped) { throw mapped; }`, which preserves the
exception-filter semantics they had (non-matching failures propagate without unwinding) and constructs the
mapped error exactly once.

**Unit tests added** (`UnitTests/Storage/BulkTransactionErrorClassificationTests`), 5 new, 9 total:

- `Does_not_classify_the_transaction_numbers_message_on_a_non_Mongo_exception` — an
  `InvalidOperationException("Transaction numbers are only allowed…")` must be `false`.
- `Does_not_classify_the_does_not_support_transactions_message_on_a_MongoException` — a
  `MongoException("… does not support transactions")` must be `false`.

  These are the two missing conjunction negatives. Until now only the *message* half of each arm was ever
  varied, so widening either arm's *type* half passed the entire existing suite — and the type half is exactly
  what the safety argument at `MongoBulkOperationExecutor.cs:570-582` rests on. Arm 2's restoration without a
  test is covered by these too.
- `Maps_a_classified_failure_to_the_actionable_standalone_error` and
  `Maps_the_transaction_numbers_MongoException_to_the_actionable_standalone_error` — the mapping produces the
  actionable "does not support multi-document transactions / replica set or sharded cluster is required"
  message and preserves the original as `InnerException`.
- `Leaves_an_unrelated_failure_unmapped` — null, so the caller's `throw;` survives.

*What would have to break for these to fail:* either arm of `IsTransactionsUnsupported` losing or widening its
type test, or its message test; the mapped error losing its actionable wording or its inner exception; or the
mapping starting to rewrite unrelated failures. The file header was also corrected — it claimed
`ExecuteDeleteTests` "skips" a test that no longer exists.

---

## Bundled minors

**5. `MongoComplianceTest.cs`** — the note now says `OrderBy`/`Distinct`-scoped cases stay on the
single-command path (because they do not affect which documents match) and only `Skip`/`Take`/`OfType`-scoped
cases go two-phase. Matches post-Task-13 reality.

**6. Public XML docs on `MongoBulkDeleteEventData` and `MongoBulkUpdateEventData`** — `LogSensitiveData`'s
summary now ties it to `Command`, and a `<remarks>` states that it is always `false` on the `Executed` event
regardless of configuration, with the reason (no `Command` on that payload; reading the setting would
gratuitously trip EF's one-shot `SensitiveDataLoggingEnabledWarning`) and the warning not to read it as the
context's configuration. `Command`'s `<remarks>` now documents that `phase1Pipeline` is JSON `null` when the
driver's logged stages are unavailable, and why `null` rather than an empty array. Both files' BOMs preserved.

**7. `MongoShapedQueryCompilingExpressionVisitor.cs`** — the `CompiledValue` comment no longer claims
`Compile()`'s throw is canonicalized by `TranslateBulkOrThrow`. It now says the opposite and why: on EF9 it is
an `InvalidOperationException`, which the filter deliberately excludes on the assumption that such an exception
already carries EF's canonical message — which this one does not — so it escapes raw. Flagged as a diagnostics
gap, not a correctness one. Comment only.

**8. `BulkOperationsEventDataTests.cs`** — `Assert.DoesNotContain("SingleCommand", executedMessage)` deleted;
the comment in its place says why an absence assertion could never fail on a regression and points at the
payload assertion that does the work. The now-unused `spyLogger` became a discard, and the test was renamed from
`..._and_the_strategy_the_log_message_omits` to `..._and_the_strategy` so the name no longer advertises the
assertion that was removed.

---

## Builds — three configurations, zero warnings

`dotnet build MongoDB.EFCoreProvider.sln --no-incremental`, run sequentially (parallel builds race on the
shared `obj/project.assets.json`):

| Configuration | Errors | Warnings |
|---|---|---|
| `Debug EF8` | 0 | 0 |
| `Debug EF9` | 0 | 0 |
| `Debug EF10` | 0 | 0 |

One warning did appear during development (`xUnit2029`, `Assert.Empty` used as a does-not-contain) and was
fixed before the final run.

## Full suite — `MONGODB_URI` and `ATLAS_URI` unset, `CRYPT_SHARED_LIB_PATH` unset, atlas-local via TestContainers

| Configuration | UnitTests | SpecificationTests | FunctionalTests |
|---|---|---|---|
| `Debug EF8` | 283 / 0 failed / 0 skipped | 4686 / 0 / 25 skipped | 1684 / 0 / 51 skipped |
| `Debug EF9` | 310 / 0 / 0 | 4999 / 0 / 25 | 1811 / 0 / 55 |
| `Debug EF10` | 303 / 0 / 0 | 4571 / 0 / 28 | 1833 / 0 / 55 |

**Zero failures on all three.**

Conformance baselines, re-run explicitly:

| | EF9 | EF10 |
|---|---|---|
| `NorthwindBulkUpdatesMongoTest` | **169 / 169** | **181 / 181** |
| `MongoComplianceTest` | pass | pass |

**Encryption suite enumeration parity.** `--filter FullyQualifiedName~Encryption`, `Debug EF10`, before
(stashed working tree) and after:

```
before:  Failed: 0, Passed: 2, Skipped: 50, Total: 52
after:   Failed: 0, Passed: 2, Skipped: 50, Total: 52
```

Identical — the four bulk theories (× 2 `CryptProvider` cases = 8) still enumerate and still skip, and the
sibling encryption suites are unaffected by the `AutoInclude(false)` model change.

---

## What I could not verify

- **Nothing in `BulkOperationsEncryptionTests` has executed.** No crypt provider here, and per instruction I did
  not set `CRYPT_SHARED_LIB_PATH`. Specifically unverified: (1) that the rejection arrives as a
  `MongoException` subtype rather than some other envelope; (2) that the rejection is atomic, leaving the
  documents byte-identical — `AssertRawDocumentsUnchanged` asserts this, and if a real run disagrees that is a
  finding about the feature, not the test; (3) that QE rejects rather than silently permitting these operations
  at all. All three are called out in the file itself, at the header and at the assertions that depend on them.
- **CSFLE's behaviour** with bulk operations. Not asserted anywhere, stated as unverified in both the README and
  the test file header.
- The EF9 auto-include defect itself, the read-path TPH gap, and everything else on the deferred list — out of
  scope, untouched.

## Files changed

```
README.md
src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkDeleteEventData.cs
src/MongoDB.EntityFrameworkCore/Diagnostics/MongoBulkUpdateEventData.cs
src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs
src/MongoDB.EntityFrameworkCore/Storage/MongoBulkOperationExecutor.cs
tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs
tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsEventDataTests.cs
tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsInheritanceTests.cs
tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteDeleteTests.cs
tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoComplianceTest.cs
tests/MongoDB.EntityFrameworkCore.UnitTests/Storage/BulkTransactionErrorClassificationTests.cs
```

## Self-review

- **BOMs.** Two files drifted mid-work (my scripted edits stripped one and added one). Caught by diffing the
  first three bytes of every changed file against `HEAD` and restored; re-verified clean afterwards. No BOM in
  the final diff differs from `HEAD`.
- **Redaction stays single-pointed.** No new `ShouldLogSensitiveData()` call. Minor 6 is documentation only; the
  one gate inside `RenderBulkCommand` is untouched.
- **`MongoEventId` untouched**; no restructuring of the Query ↔ Storage bulk seam. The Finding 3 guard lives on
  the Query side next to the existing `EnsureBulkKeyOrThrow`, and Finding 4's mapping stays entirely inside
  Storage.
- **Nullable / `ConfigureAwait`.** No new `src/` async code. The one new `src/` member is a nullable-returning
  static, annotated.
- **Comment placement.** My first insertion of `EnsureBulkRootTypeOrThrow` orphaned
  `EnsureBulkKeyOrThrow`'s doc comment above the wrong method; caught on diff review and fixed.
- **Scope.** Nothing from the out-of-scope list was touched. I did make two small changes beyond the literal
  instructions: renaming the minor-8 test (its name asserted the thing I deleted) and adding the Finding 3
  README bullet (the guard is user-visible). Both are consequences of the instructed changes.
- **Test count deltas are accounted for**: +2 functional (Finding 3), +5 unit (Finding 4), −1 skipped
  (Finding 4's placeholder), 2 encryption tests renamed.

## Concerns

1. **The `MongoException` assertion in the encryption tests is a reasoned bet, not an observation.** The
   reasoning is sound as far as it goes (the provider does not wrap `DeleteMany`/`UpdateMany`), but if the QE
   rejection surfaces in another envelope, the first CI leg with a crypt provider goes red. The file says
   exactly which assertion to relax and which one not to. I judged a red test with a clear message better than a
   catch-all that can't distinguish a rejection from a bug — that being the third-recurrence lesson this wave
   was meant to close — but it is a bet.
2. **`AssertRawDocumentsUnchanged` is the strictest thing in the file.** It is the right claim for a rejected
   operation, and it is what makes the confidentiality assertions non-vacuous, but it is also the assertion most
   likely to be the *interesting* failure if QE half-applies. That would be a genuine finding; I would rather it
   surface than be assumed away.
3. **The Finding 3 guard rejects a shape that previously silently destroyed data.** Anyone who was relying on
   `db.DerivedSet.ExecuteDelete()` gets an exception where they used to get a wiped collection. I believe that
   is unambiguously right, and it matches the reviewer's and the human partner's framing, but it is a
   behaviour change and worth naming.
4. **The read-path half of Finding 3 is still open** — `db.Cats.ToList()` still reads the whole hierarchy. That
   is explicitly out of scope (ticket), but the guard now makes the write path stricter than the read path,
   which is a slightly odd asymmetry a reader may trip over. The README bullet and the guard's comment both say
   the limitation is "not yet", not "never".
