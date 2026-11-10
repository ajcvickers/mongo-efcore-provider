### Task 24: Cover bulk operations against encrypted collections

The README advertises CSFLE and Queryable Encryption compatibility. There are five test files under `tests/.../Encryption/` and none reference `ExecuteUpdate` or `ExecuteDelete`. The specific risk worth verifying rather than assuming: the self-referencing setter path emits a **pipeline update** (`Builders.Update.Pipeline`), and aggregation-expression updates are generally unsupported against auto-encrypted collections. The expected outcome is a clean driver-level rejection; the alternatives — a value written unencrypted, or a filter over an encrypted field silently matching nothing on a *delete* — are confidentiality failures.

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing.

- [ ] **Step 1: Write the tests**

Derive from `AutoEncryptionTestsBase` so the key vault, crypt-provider parametrisation and environment gating come for free. The base supplies `CreateDataKey()`, `CreateContext(cryptProvider, modelBuilder)`, `CreateEncryptedCollections(db.Model)`, `CreateSamplePatients`, the `Patient` model, and the `[QueryableEncryptionTheory]` attribute that skips when encryption is unavailable — this is the same shape as `QueryableEncryptionTests.Server_schema_is_used_when_mode_is_ignore`.

```csharp
/* Copyright 2023-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// The README advertises CSFLE / Queryable Encryption compatibility, and bulk operations reach the driver by a
// different route from the rest of the provider: BuildFilter renders a FilterDefinition<BsonDocument> and hands
// it to DeleteMany/UpdateMany on a GetCollection<BsonDocument> handle. These tests confirm auto-encryption still
// applies on that route, and that the pipeline-form update either works or fails closed — never writing
// plaintext and never silently matching nothing.

#if !EF8

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Encryption;

[XUnitCollection("EncryptionTests")]
public class BulkOperationsEncryptionTests(TemporaryDatabaseFixture database)
    : AutoEncryptionTestsBase(database)
{
    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_delete_filtered_on_an_encrypted_equality_field_deletes_the_matching_documents(
        CryptProvider cryptProvider)
    {
        var collectionName = TemporaryDatabaseFixture.CreateCollectionName(values: cryptProvider);
        var dataKeyId = CreateDataKey();
        var patients = CreateSamplePatients;
        var targetSsn = patients[0].SSN;

        using var db = CreateContext(cryptProvider, mb => mb.Entity<Patient>(p =>
        {
            p.ToCollection(collectionName);
            p.Property(x => x.SSN).IsEncrypted(dataKeyId);
        }));

        CreateEncryptedCollections(db.Model);
        db.Database.EnsureCreated();
        db.Patients.AddRange(patients);
        db.SaveChanges();

        // Equality on a queryable-encrypted field must match through the bulk path exactly as it does through a
        // read. A zero-match here would mean the delete reports success while the data is still there.
        var deleted = db.Patients.Where(p => p.SSN == targetSsn).ExecuteDelete();

        Assert.Equal(1, deleted);
        Assert.Equal(patients.Length - 1, db.Patients.Count());
        Assert.DoesNotContain(db.Patients.ToList(), p => p.SSN == targetSsn);
    }

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_update_setting_an_encrypted_field_stores_it_encrypted(CryptProvider cryptProvider)
    {
        var collectionName = TemporaryDatabaseFixture.CreateCollectionName(values: cryptProvider);
        var dataKeyId = CreateDataKey();
        var patients = CreateSamplePatients;

        using var db = CreateContext(cryptProvider, mb => mb.Entity<Patient>(p =>
        {
            p.ToCollection(collectionName);
            p.Property(x => x.SSN).IsEncrypted(dataKeyId);
        }));

        CreateEncryptedCollections(db.Model);
        db.Database.EnsureCreated();
        db.Patients.AddRange(patients);
        db.SaveChanges();

        var updated = db.Patients.ExecuteUpdate(s => s.SetProperty(p => p.SSN, "000-00-0000"));

        Assert.Equal(patients.Length, updated);

        // Through the encrypted client the value round-trips as plaintext...
        Assert.All(db.Patients.ToList(), p => Assert.Equal("000-00-0000", p.SSN));

        // ...and read directly, without auto-encryption, it must be ciphertext rather than the literal we set.
        var raw = database.MongoDatabase.GetCollection<BsonDocument>(collectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).First();
        Assert.Equal(BsonType.Binary, raw["SSN"].BsonType);
    }

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_update_with_a_self_referencing_setter_on_an_encrypted_collection_does_not_write_plaintext(
        CryptProvider cryptProvider)
    {
        var collectionName = TemporaryDatabaseFixture.CreateCollectionName(values: cryptProvider);
        var dataKeyId = CreateDataKey();
        var patients = CreateSamplePatients;

        using var db = CreateContext(cryptProvider, mb => mb.Entity<Patient>(p =>
        {
            p.ToCollection(collectionName);
            p.Property(x => x.SSN).IsEncrypted(dataKeyId);
        }));

        CreateEncryptedCollections(db.Model);
        db.Database.EnsureCreated();
        db.Patients.AddRange(patients);
        db.SaveChanges();

        // A self-referencing setter renders as a pipeline-form update ($set with an aggregation expression).
        // Sequence is NOT encrypted here, so this is the benign case and must simply work; the assertion that
        // matters is that the encrypted sibling field is untouched and still ciphertext.
        var updated = db.Patients.ExecuteUpdate(s => s.SetProperty(p => p.Sequence, p => p.Sequence + 1));

        Assert.Equal(patients.Length, updated);

        var raw = database.MongoDatabase.GetCollection<BsonDocument>(collectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).First();
        Assert.Equal(BsonType.Binary, raw["SSN"].BsonType);
    }

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_update_with_a_self_referencing_setter_on_an_encrypted_field_fails_closed(
        CryptProvider cryptProvider)
    {
        var collectionName = TemporaryDatabaseFixture.CreateCollectionName(values: cryptProvider);
        var dataKeyId = CreateDataKey();
        var patients = CreateSamplePatients;

        using var db = CreateContext(cryptProvider, mb => mb.Entity<Patient>(p =>
        {
            p.ToCollection(collectionName);
            p.Property(x => x.Sequence).IsEncrypted(dataKeyId);
        }));

        CreateEncryptedCollections(db.Model);
        db.Database.EnsureCreated();
        db.Patients.AddRange(patients);
        db.SaveChanges();

        // Aggregation-expression updates over an encrypted field cannot be evaluated server-side. The only
        // acceptable outcomes are a clean failure, or a correct encrypted result — never a plaintext write.
        try
        {
            db.Patients.ExecuteUpdate(s => s.SetProperty(p => p.Sequence, p => p.Sequence + 1));
        }
        catch (Exception)
        {
            // Fail-closed: acceptable.
            return;
        }

        var raw = database.MongoDatabase.GetCollection<BsonDocument>(collectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).First();
        Assert.Equal(BsonType.Binary, raw["Sequence"].BsonType);
    }
}

#endif
```

Three details to confirm against the base class rather than assume: the `XUnitCollection` name used by the sibling encryption tests (copy it exactly); whether `database.MongoDatabase` is the accessible unencrypted handle on `TemporaryDatabaseFixture` (if `_database` is private in the base, add a `protected` accessor for the raw database rather than reaching around it); and whether `IsEncrypted(dataKeyId)` supports `int` properties for the last test — if it does not, substitute a string property such as `Doctor` and adjust the `+ 1` expression to a string concatenation, or drop that test and record the limitation.

If the environment gating means these skip locally, that is expected: `[QueryableEncryptionTheory]` reports the same skip reason as the sibling suites, and the value is in the CI leg that has encryption configured.

- [ ] **Step 2: Run them, including checking they are gated not silently absent**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsEncryptionTests" --logger "console;verbosity=detailed"
```

Expected: PASS, or SKIP with the same gating reason the sibling encryption tests report. A skip is only acceptable if the sibling tests also skip in the same environment — verify by running `--filter "FullyQualifiedName~AutoEncryptionTests"` and comparing.

- [ ] **Step 3: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/BulkOperationsEncryptionTests.cs
git commit -m "test: cover bulk operations against CSFLE and Queryable Encryption collections"
```

---

## Phase 7 — Documentation and cleanups

