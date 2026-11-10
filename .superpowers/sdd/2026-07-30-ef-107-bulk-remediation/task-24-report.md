# Task 24 report: Cover bulk operations against encrypted collections

## Status: DONE_WITH_CONCERNS

The concern is not with the work itself but with the nature of the task: the four new tests never
executed their bodies in this environment (no crypt provider available), so everything below the
"compiles and is gated correctly" line is asserted from source-reading and cross-referencing sibling
tests, not from a green run.

## What was written

`tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs` (new
file, 245 lines, `#if !EF8` guarded, BOM added to match the majority of the folder).

Four `[QueryableEncryptionTheory]` tests, each parametrised over
`[InlineData(CryptProvider.Mongocryptd)]` / `[InlineData(CryptProvider.AutoEncryptSharedLibrary)]`:

1. `Bulk_delete_filtered_on_an_encrypted_equality_field_deletes_the_matching_documents` — deletes via
   `db.Patients.Where(p => p.SSN == targetSsn).ExecuteDelete()` against an `IsEncryptedForEquality`
   SSN, then asserts exactly one document was removed and the sibling documents are untouched.
2. `Bulk_update_setting_an_encrypted_field_stores_it_encrypted` — `ExecuteUpdate` with a literal
   `SetProperty(p => p.SSN, "000-00-0000")` against an `IsEncrypted` SSN; asserts the EF-side read
   shows plaintext but the raw driver-level document shows `BsonType.Binary`.
3. `Bulk_update_with_a_self_referencing_setter_on_an_encrypted_collection_does_not_write_plaintext` —
   a pipeline-form `SetProperty(p => p.Sequence, p => p.Sequence + 1)` on an *unencrypted* `Sequence`,
   with SSN encrypted as the bystander; asserts the update succeeds and SSN is still ciphertext.
4. `Bulk_update_with_a_self_referencing_setter_on_an_encrypted_field_fails_closed` — the same
   self-referencing update, but now `Sequence` itself is encrypted; asserts a confidentiality
   invariant that holds whether the driver throws or not (see "Correction 1" below).

`CreateContext(cryptProvider, modelBuilderAction)`, `CreateEncryptedCollections(IModel)`, and a private
nested `MedicalContext : DbContext` were **written locally in this file** — see "Brief assumptions that
turned out wrong" (#1) for why they couldn't be inherited.

## Skip count / gating verification (Priority 2 — the load-bearing check)

Environment confirmed: `CRYPT_SHARED_LIB_PATH` unset, `MONGODB_URI`/`ATLAS_URI` unset (TestContainers
booted `mongodb/mongodb-atlas-local` as expected).

**My filter** (`FullyQualifiedName~BulkOperationsEncryptionTests`, Debug EF10):

```
Total tests: 4
    Skipped: 4
```

All four report the identical skip reason verbatim:

```
These Queryable Encryption tests require MongoDB 8.0 or later as declared by the VERSION environment variable.
```

**Sibling comparison 1** — `FullyQualifiedName~AutoEncryptionTests` (uses a *different* gating
attribute, `[EncryptionTheory]`, defined in that same file):

```
Total tests: 4
    Skipped: 4
```
Reasons: `Requires encryption library to be present.` (×3) and `Failing only on Windows Latest in the
CI` (×1, an unrelated pre-existing `Skip` on one test, not gating-related).

**Sibling comparison 2** — `FullyQualifiedName~QueryableEncryptionTests.Server_schema_is_used_when_mode_is_ignore`
(uses the *same* `[QueryableEncryptionTheory]` attribute my file uses, inherited from the same base):

```
Total tests: 2
    Skipped: 2
```
Reason, verbatim, identical to mine: `These Queryable Encryption tests require MongoDB 8.0 or later as
declared by the VERSION environment variable.`

**Conclusion: this is genuine gating, not silent absence.** In every case xUnit's discovery phase
enumerated the specific test method + `InlineData` case by name before marking it skipped (visible in
`--logger "console;verbosity=detailed"` output), and my file's skip text is byte-for-byte identical to
the sibling that shares its gating attribute. Repeated identically on Debug EF9.

**What this does NOT verify, stated plainly: none of the four test bodies ran.** `[Theory]`'s `Skip`
property short-circuits before the method is ever invoked. Whether the equality filter matches
server-side, whether the literal `$set` produces `BsonType.Binary` ciphertext, whether the
self-referencing update on the unencrypted field leaves the encrypted sibling alone, and above all
whether the self-referencing update on the *encrypted* field genuinely fails closed rather than
quietly writing something wrong — all of that is **unexecuted and unverified** in this environment.
Only compilation-level correctness (member existence, signatures, types) has been checked here.

## Sibling-suite regression check (Correction 3)

No shared file was touched — `git status --porcelain` before commit showed only the one new file as
untracked. The "additive protected member" scenario in Correction 3 did not arise (see brief-assumption
#2 below), so there was nothing to regress. Ran anyway, as instructed:

| Filter | Before (without my file) | After (with my file) |
|---|---|---|
| `~Encryption` (EF10) | 48 total / 2 passed / 46 skipped | 52 total / 2 passed / 50 skipped |
| `~Encryption` (EF9) | not separately isolated | 52 total / 2 passed / 50 skipped |

Purely additive (+4 total, +4 skipped, passed count unchanged). The 2 passing tests are both
`ExplicitEncryptionTests.Explicit_encrypted_properties_round_trip_with_encryption` (a plain `[Fact]`
doing manual/explicit `ClientEncryption` calls that don't need `mongocryptd`/`crypt_shared`) — unrelated
to my change, present before and after.

## Three-configuration build

| Configuration | Result | Warnings |
|---|---|---|
| Debug EF8 | Build succeeded | 0 (file is compiled out entirely by `#if !EF8`; confirmed by `--filter "FullyQualifiedName~BulkOperationsEncryptionTests"` reporting "No test matches" — 0 discovered, not skipped, which is correct for EF8) |
| Debug EF9 | Build succeeded | 0 |
| Debug EF10 | Build succeeded | 0 |

The first EF10 build surfaced `CS9107` (a primary-constructor parameter captured both into the derived
type's state *and* passed to the base constructor) — see "Self-review / fixes" below. After the fix,
all three configurations are clean.

## Brief assumptions that turned out wrong

1. **`AutoEncryptionTestsBase` does not supply `CreateContext`, `CreateEncryptedCollections`, or a
   `Patients` DbSet.** The brief states the base "supplies ... `CreateContext(cryptProvider,
   modelBuilder)`, `CreateEncryptedCollections(db.Model)` ... and the `Patient` model." Reading the base
   class in full: it supplies `CreateDataKey()`, `CreateSamplePatients`, the `Patient` model (and
   friends), and `QueryableEncryptionTheory`/`ShouldRunQueryableEncryptionTests` — nothing else.
   `CreateContext`, `CreateEncryptedCollections`, and the `MedicalContext` DbContext (with its
   `DbSet<Patient> Patients`) are all **private members defined locally inside `QueryableEncryptionTests`**,
   not on the base, and private members aren't inherited regardless of which class you derive from.
   I wrote local equivalents in the new file — which, on inspection, is exactly the established
   convention: `AutoEncryptionTests.cs` *also* redefines its own `SetupEncryptedTestData`,
   `CreateSchemaMap`, and `EncryptionTheory` despite deriving from the same base. This is not a
   workaround; it's how this codebase already does it.
2. **No `protected` accessor was needed on any base class.** `TemporaryDatabaseFixtureBase.MongoDatabase`
   is already `public` (confirmed by reading the class and by its existing use as `_database.MongoDatabase`
   in both `AutoEncryptionTests.cs` and `QueryableEncryptionTests.cs`). Zero `src/` or shared-test-base
   changes were made.
3. **`IsEncrypted<T>` does support `int`.** Confirmed both by its generic signature and by the existing
   `IsEncrypted_round_trips_integer` sibling test, which encrypts `Patient.Sequence` (an `int`) the same
   way. No substitution to a string property was needed for test 4.
4. **`CreateSamplePatients` is a get-only property returning `Patient[]`**, confirmed by reading
   `AutoEncryptionTestsBase.cs` directly (`protected static Patient[] CreateSamplePatients => [...]`).
   The brief's usage (`var patients = CreateSamplePatients; patients[0].SSN; patients.Length`) is
   correct as written; no fix needed.
5. **`XUnitCollection` name: the sibling files don't agree with each other.** `AutoEncryptionTests.cs`
   uses `"EncryptionTests"`; `QueryableEncryptionTests.cs` and `ExplicitEncryptionTests.cs` both use
   `"Encryption"` — and `"Encryption"` is also the name documented as canonical in
   `tests/MongoDB.EntityFrameworkCore.SpecificationTests/AGENTS.md` ("Encryption tests live in their own
   collection (`[XUnitCollection("Encryption")]`)"). I used `"Encryption"` (2-of-3 precedent + the
   documented convention + the closer sibling in shape, `QueryableEncryptionTests`). I did not touch
   `AutoEncryptionTests.cs`'s outlier value — that's a pre-existing inconsistency in a sibling file, out
   of scope for this task, but worth flagging: it currently means `AutoEncryptionTests` does **not**
   serialize into the same xUnit collection as the other two encryption suites, which may or may not be
   intentional.

## Two bugs found in the brief's own code snippet (fixed)

1. **Test 1's filter would have matched nothing, for exactly the confidentiality-failure reason the
   brief itself warns about.** The snippet configured `SSN` with plain `.IsEncrypted(dataKeyId)`
   (`QueryableEncryptionType.NotQueryable`) and then filtered on it by equality
   (`p.SSN == targetSsn`) for the delete. `QueryableEncryptionBuilderExtensions.IsEncrypted`'s own XML
   doc says "without query ability," and per the Queryable Encryption spec, unindexed fields are always
   *randomly* encrypted — the client-side encryption of the query literal would never match the
   randomly-encrypted stored ciphertext. Run as written, this test would either fail its own
   `Assert.Equal(1, deleted)` (0 actually deleted) or throw from the driver — either way, not what the
   test name/docstring claims to demonstrate. **Fixed** by changing `SSN` to
   `.IsEncryptedForEquality(dataKeyId)` in test 1 only (tests 2–4 correctly keep plain `.IsEncrypted`
   since they only ever *write* to the encrypted field, never filter on it).
2. **Raw-document field-name casing.** `Patient.SSN` carries `[BsonElement("ssn")]` (lowercase), but
   the brief's raw-document assertions read `raw["SSN"]` (uppercase) — `BsonDocument`'s indexer is
   case-sensitive, so this would have thrown `KeyNotFoundException`, not just been a style nit. **Fixed**
   to `raw["ssn"]` in tests 2 and 3. `Sequence` has no `[BsonElement]` override, so `raw["Sequence"]`
   (as the brief had it) is correct as-is in test 4.

## How Correction 1 (fail-closed test) was strengthened

Original snippet: `try { ExecuteUpdate(...); } catch (Exception) { return; }` — any exception at all
(including a setup failure) counted as success, and the catch path asserted nothing.

Rewritten: the `try`/`catch` now wraps **only** the `ExecuteUpdate` call (setup — `CreateContext`,
`CreateEncryptedCollections`, `EnsureCreated`, `AddRange`, `SaveChanges` — is *not* inside the try, so a
setup failure still fails the test normally, not silently). The `catch (Exception) { }` body is now
empty (a comment only), and **both** the exception path and the no-exception path fall through to the
same unconditional assertion:

```csharp
var raw = _database.MongoDatabase.GetCollection<BsonDocument>(collectionName)
    .Find(FilterDefinition<BsonDocument>.Empty).First();
Assert.Equal(BsonType.Binary, raw["Sequence"].BsonType);
```

This is the actual confidentiality claim, checked on both branches of the disjunction: a run that
threw *and* wrote plaintext now fails; a run that succeeded but wrote plaintext also fails. The catch
is deliberately kept broad (`Exception`, not a specific driver exception type) — the driver's actual
rejection type for an aggregation-expression update over an encrypted field is **not knowable without
running against a real crypt provider**, which this environment doesn't have. This is called out in a
comment in the file and here, per the instruction to say so explicitly rather than guess.

## `IsEncrypted` support and property choice for test 4

Confirmed `IsEncrypted<T>`/`IsEncryptedForEquality<T>`/`IsEncryptedForRange<T>` are all generic over the
property's CLR type with no type constraint, and `int` is exercised elsewhere in the suite
(`IsEncrypted_round_trips_integer`, `IsEncryptedForEquality_queries_equality_on_integer`). Used
`Patient.Sequence` (an `int`) directly, as the brief intended — no substitution or dropped test needed.

## Build warning found and fixed

The first EF10 build produced:
```
warning CS9107: Parameter 'TemporaryDatabaseFixture database' is captured into the state of the
enclosing type and its value is also passed to the base constructor. The value might be captured by
the base class as well.
```
Cause: I initially referenced the primary-constructor parameter `database` directly inside method
bodies (`CreateContext`, `CreateEncryptedCollections`, the raw-read assertions) while also passing it to
`: AutoEncryptionTestsBase(database)`. Fixed by following the exact pattern both `AutoEncryptionTests.cs`
and `QueryableEncryptionTests.cs` already use: capture it once into `private readonly
TemporaryDatabaseFixture _database = database;` and use `_database` everywhere in the class body. All
three configurations now build with **0 warnings, 0 errors**.

## Additional verification run

`NorthwindBulkUpdatesMongoTest` on Debug EF10, `--no-build`, no filter:
```
Total tests: 181
     Passed: 181
```
Matches the required 181/181 baseline — confirms nothing in the existing bulk-operations spec suite
regressed.

## Files changed

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs` (new,
  245 lines). No `src/` changes. No changes to any shared test-base file.

## Commit

`fcea873` — `test: cover bulk operations against CSFLE and Queryable Encryption collections`

## Self-review

- **Every member referenced exists with that signature** — verified by direct source reading for each:
  `CreateDataKey()`, `CreateSamplePatients`, `KeyVaultNamespace`/`KmsProviders` (protected on
  `EncryptionTestsBase`), `QueryableEncryptionTheory` (protected nested class on
  `AutoEncryptionTestsBase`), `CryptProvider` enum, `ToCollection`/`IsEncrypted`/`IsEncryptedForEquality`
  extensions, `ExecuteUpdate`/`ExecuteDelete`/`SetProperty`, `MongoOptionsExtension`'s fluent `With*`
  methods, `UseMongoDB<TContext>(MongoOptionsExtension, ...)`, `ReplaceService`, the shared
  `IgnoreCacheKeyFactory` (global-usinged, not redefined), `CoreEventId.ManyServiceProvidersCreatedWarning`,
  `QueryableEncryptionSchemaGenerator.GenerateSchemas` (public), `IsDocumentRoot()`/`GetCollectionName()`,
  `CreateCollectionOptions.EncryptedFields`, `FilterDefinition<BsonDocument>.Empty`,
  `TemporaryDatabaseFixture.CreateCollectionName`/`.MongoDatabase`/`.Client`. Confirmed by 0-warning
  clean builds across all three configurations, and by discovery/enumeration in the test-run output
  (proves the class, methods, and attributes are all well-formed CLR members, not just syntactically
  parseable).
- **Would each assertion be meaningful if it ran?** Yes, after the two brief-snippet bugs above were
  fixed (equality-queryable SSN for test 1; correct raw-document key casing for tests 2–3). Each test
  asserts something that would fail under the "wrong" outcome the test is meant to catch (see next
  bullet).
- **Is any test capable of passing for the wrong reason?**
  - Test 1: guarded by both the exact-count assertion (`Assert.Equal(1, deleted)`) and a follow-up
    `Count()`/`DoesNotContain` check — a broken filter (0 or 2+ matches) fails visibly.
  - Test 2: the raw-ciphertext assertion is exactly the check that prevents "encryption wasn't applied
    but the test still looks green" — without it, only the EF-side round-trip would be checked, which
    would pass even if nothing were encrypted at all.
  - Test 3: `Assert.Equal(patients.Length, updated)` guards against a silently-no-op update.
  - Test 4: post-fix, this is the one I scrutinized hardest per Correction 1 — it can no longer pass
    from the catch block alone; both branches converge on the same ciphertext assertion, and the
    unrelated-setup-failure loophole is closed by narrowing the `try` to just the `ExecuteUpdate` call.
- **Is the file honest about being CI-only coverage?** Yes — a comment block at the top of the file
  states plainly that these tests are gated on a real crypt provider, will be skipped without one, and
  that the value is realized in the CI leg that has encryption configured.

## Concerns / things worth a second pair of eyes

1. **Nothing in this file has actually run.** Compilation and gating are solid; the four
   confidentiality claims themselves (especially test 4's "fails closed") are unverified until CI (or a
   local run with `CRYPT_SHARED_LIB_PATH` pointed at a real crypt_shared library) exercises them. If
   test 4 turns out to pass because the driver silently *succeeds* with a wrong-but-still-encrypted
   value rather than throwing, the current assertion (ciphertext BsonType only) would not catch that —
   catching it would require decrypting and checking the value is unchanged, which needs a second,
   *decrypting* client and is more machinery than this task's scope suggested. I judged that out of
   scope given "test-only, minimal" framing, but flagging it as a residual gap in the confidentiality
   claim.
2. **`CreateEncryptedCollections` + `EnsureCreated()` before insert, for all four tests, including the
   plain-`IsEncrypted` ones.** Several sibling round-trip tests for plain `IsEncrypted` fields (e.g.
   `IsEncrypted_round_trips_string`) don't call `CreateEncryptedCollections` at all and just
   `AddRange`+`SaveChanges`. I kept the brief's calls in all four tests anyway (defensive/uniform setup,
   harmless if unnecessary) rather than diverging further from the brief without being able to verify
   which behavior is actually required server-side. Noting the inconsistency with some siblings in case
   it turns out to matter.
3. **The `XUnitCollection` name inconsistency** (`"Encryption"` vs. `"EncryptionTests"`) in the sibling
   files pre-dates this change and I didn't fix it (out of scope), but it may be worth a follow-up: if
   it's unintentional, `AutoEncryptionTests` currently doesn't serialize with the other two encryption
   suites for crypto-state purposes.

---

## Round 2: fix report (post-review)

Review came back **spec ✅ on all three amendments, quality Approved**, with one Important and one
Minor. Both are addressed below. The `"Encryption"` vs. `"EncryptionTests"` naming question was
confirmed genuinely inert (no `[CollectionDefinition]` exists anywhere, `XUnitCollection` is a bare
alias for `Xunit.CollectionAttribute`, and parallelisation is disabled assembly-wide) — left alone, as
instructed.

### Important 1 — test 3 no longer asserts a field-level premise as fact

**Problem:** `Bulk_update_with_a_self_referencing_setter_on_an_encrypted_collection_does_not_write_plaintext`
asserted the pipeline-form update "must simply work" on the theory that libmongocrypt's restriction on
aggregation-expression updates is scoped to the specific field the `$set` expression touches (Sequence,
unencrypted in that test) rather than to the collection as a whole (which — via the sibling SSN field —
does carry an `encryptedFieldsMap`). Neither the brief nor I can verify which scope is correct without a
live crypt provider, and if the restriction turns out to be collection-wide, the test would throw
uncaught and fail in CI for a reason that has nothing to do with what it's meant to test.

**Fix:** Renamed to `Bulk_update_with_a_self_referencing_setter_does_not_expose_an_encrypted_sibling_field`
— the new name states the invariant (the encrypted sibling is never exposed as plaintext), not a
mechanism (whether the update "works"). Gave it the same disjunction structure as test 4: the `try` is
scoped to only the `db.Patients.ExecuteUpdate(...)` call (so a setup failure — `CreateContext`,
`CreateEncryptedCollections`, `EnsureCreated`, `AddRange`, `SaveChanges` — still fails the test normally,
never swallowed); the `Assert.Equal(patients.Length, updated)` row-count check now lives *inside* the
try, since it's only meaningful on the success branch; the `catch (Exception) { }` body is empty (comment
only); and both branches converge on the same unconditional raw-ciphertext assertion. The comment above
the try no longer states "must simply work" — it now says plainly that whether the restriction is
field-level or collection-wide is unverified here, and that the test accepts either outcome.

**My prior, for whoever reads the first CI run:** I'd expect the **success branch** (update proceeds,
row count matches, SSN stays ciphertext) rather than a rejection. My reasoning: libmongocrypt's
mark/encrypt pass over a pipeline-form update needs to identify, for each stage, which paths correspond
to encrypted fields (per the client's `encryptedFieldsMap`/schema) so it can encrypt/decrypt around them;
an aggregation expression that references only unencrypted paths (`Sequence`) gives libmongocrypt nothing
it needs to encrypt or reason about cryptographically, even though the collection has other encrypted
fields. This mirrors ordinary CSFLE/QE behavior elsewhere in the schema (only the *referenced* paths
matter for encryption processing, not everything else declared on the collection). I hold this loosely —
it's exactly the kind of assumption that turned out wrong once already in this task (test 1's filter), so
treat CI's actual result as authoritative over my prior, not the other way around.

### Minor 1 — raw ciphertext checks now cover every document

**Problem:** the raw-document assertions in tests 2, 3 (renamed), and 4 used `.Find(...).First()`,
checking only one of the two `CreateSamplePatients` documents. A partial-batch failure could leave a
plaintext write on the second document undetected.

**Fix:** all three now do `.Find(FilterDefinition<BsonDocument>.Empty).ToList()` followed by
`Assert.All(rawDocuments, raw => Assert.Equal(BsonType.Binary, raw["<field>"].BsonType))` — mirroring the
`Assert.All` pattern test 2 already used on the EF-decrypted side. Two extra lines each, per document
coverage on the confidentiality guarantee.

### Re-verification

- **Build, all three configurations:** Debug EF8 / EF9 / EF10 — all `Build succeeded`, **0 Warning(s), 0
  Error(s)** on each.
- **My filter** (`~BulkOperationsEncryptionTests`, EF10 and EF9): `Total tests: 4, Skipped: 4`, same skip
  text verbatim (`These Queryable Encryption tests require MongoDB 8.0 or later as declared by the
  VERSION environment variable.`) on all four, including the renamed test — confirms the rename didn't
  break discovery.
- **Sibling comparison** (`~Encryption`, EF10 and EF9): `Total tests: 52, Passed: 2, Skipped: 50` on
  both — unchanged from round 1 (this filter also sweeps in `Mapping/EncryptionBuilderExtensionTests`,
  which is why the total is 52 rather than 48 + 4; that composition is identical before and after this
  round's edits since no shared file changed).
- **Sibling gating comparison** (`~AutoEncryptionTests`, EF10): `Total tests: 4, Skipped: 4`, same skip
  reasons as round 1.
- **`NorthwindBulkUpdatesMongoTest`, EF10:** `Total tests: 181, Passed: 181` — unchanged baseline.
- **Honesty check:** as in round 1, none of the four test bodies executed in this environment — only
  discovery, gating, and compilation were exercised. Nothing above should be read as the encryption
  assertions having run.

### Files changed (round 2)

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs`
  (modified only; 33 insertions / 16 deletions). No other files touched.

---

## Round 3: fix report (post-review)

Review confirmed Minor 1 fully addressed and Important 1's core requirements met (rename states the
invariant, comment says the field-vs-collection scope question is unsettled, both branches converge
unconditionally with no early `return`) — but flagged that the round-2 fix reintroduced a subtler version
of the same hole: `Assert.Equal` throws `Xunit.Sdk.EqualException`, which derives from `Exception`, so
putting the row-count assertion *inside* the `try` meant a genuine defect on the success branch (wrong
row count, partial/no-op write) would be caught and silently reclassified as "rejected outright:
acceptable" — worse than round 0, where that assertion had no try/catch around it at all and would have
failed visibly.

**Fix:** in
`Bulk_update_with_a_self_referencing_setter_does_not_expose_an_encrypted_sibling_field`
(`BulkOperationsEncryptionTests.cs`), took the first of the two options offered — capture the result in a
nullable outside the `try` (`int? updated = null;`), assign it inside the `try` (the *only* statement now
inside the `try` is the bare `db.Patients.ExecuteUpdate(...)` call), and assert the row count afterward,
guarded by `if (updated is { } updatedCount)` so it's only checked on the branch where the update actually
ran. Chose this over dropping the count check because the count is worth verifying when the update
succeeds — test 4 has no analogous check only because it has no success-branch value worth checking (it
never asserts anything about the update's own outcome, only about the raw document afterward), not
because success-branch checks are undesirable in general.

Added a comment directly above the `try` spelling out why the assertion had to move: `Assert.Equal`
throws a type that derives from `Exception`, so any assertion sharing scope with a broad `catch` isn't
really an assertion.

**Test 4 re-examined per the reviewer's request:** `Bulk_update_with_a_self_referencing_setter_on_an_encrypted_field_fails_closed`'s
`try` block contains exactly one statement — the bare `db.Patients.ExecuteUpdate(...)` call, no
assertion, no other code. Nothing inside it can throw for a reason unrelated to the driver/server
rejecting the operation. No change was needed there; confirmed by inspection, not by execution.

### Re-verification (round 3)

- **Build, all three configurations:** Debug EF8 / EF9 / EF10 — all `Build succeeded`, **0 Warning(s), 0
  Error(s)**.
- **My filter** (`~BulkOperationsEncryptionTests`, EF10 and EF9): `Total tests: 4, Skipped: 4` on both,
  same verbatim skip reason as every round before.
- **Sibling comparison** (`~Encryption`, EF10 and EF9): `Total tests: 52, Passed: 2, Skipped: 50` on
  both — unchanged.
- **`NorthwindBulkUpdatesMongoTest`, EF10:** `Total tests: 181, Passed: 181` — unchanged.
- `grep -n "\.First()"` over the file: no matches (Minor 1 fix from round 2 confirmed still intact).
  Both `try` blocks in the file now enclose exactly one statement (the bare `ExecuteUpdate` call) with no
  assertion in scope.
- **Honesty check, unchanged from every prior round:** none of the four test bodies executed in this
  environment. Everything above is discovery, gating, and compile-time verification only — the
  confidentiality assertions themselves remain unexercised until a crypt provider is available.

### Files changed (round 3)

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs`
  (modified only). No other files touched in any round.
