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

// Bulk ExecuteUpdate / ExecuteDelete against QUERYABLE ENCRYPTION collections, as observed against a real server with libmongocrypt
// (both the mongocryptd and the crypt_shared providers behave identically):
//
//  * ExecuteDelete WORKS. deleteMany is accepted, an equality filter on an encrypted field is encrypted correctly and matches exactly the
//    intended documents. A delete that reported success while matching nothing would be a data-integrity failure, hence the checks on the
//    raw stored documents.
//  * ExecuteUpdate is REJECTED, cleanly, with a MongoEncryptionException ("Multi-document updates are not allowed with Queryable
//    Encryption"), because every bulk update is an updateMany. SaveChanges (per-document writes) is the supported route. The contract
//    pinned here is that the rejection destroys nothing, changes nothing and writes no plaintext: every stored document is compared
//    before and after.
//
// All tests are Queryable-Encryption-only: the collections carry EncryptedFields and the client has KMS providers but no schemaMap.
// Client-Side Field Level Encryption (schemaMap) is NOT covered here, and whether it permits bulk writes is unverified.

#if !EF8

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Encryption;

[XUnitCollection("Encryption")]
public class BulkOperationsEncryptionTests(TemporaryDatabaseFixture database)
    : AutoEncryptionTestsBase(database)
{
    private readonly TemporaryDatabaseFixture _database = database;

    private void CreateEncryptedCollections(IModel model)
    {
        var schemas = QueryableEncryptionSchemaGenerator.GenerateSchemas(model);
        foreach (var entityType in model.GetEntityTypes().Where(e => e.IsDocumentRoot()))
        {
            var collectionName = entityType.GetCollectionName();
            if (schemas.TryGetValue(collectionName, out var value))
            {
                _database.MongoDatabase.CreateCollection(collectionName, new CreateCollectionOptions { EncryptedFields = value });
            }
        }
    }

    private MedicalContext CreateContext(CryptProvider cryptProvider, Action<ModelBuilder> modelBuilderAction)
    {
        var mongoOptions = new MongoOptionsExtension()
            .WithClientSettings(_database.Client.Settings)
            .WithDatabaseName(_database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .WithKeyVaultNamespace(KeyVaultNamespace)
            .WithKmsProviders(KmsProviders);

        mongoOptions = cryptProvider switch
        {
            CryptProvider.AutoEncryptSharedLibrary => mongoOptions.WithCryptProvider(
                CryptProvider.AutoEncryptSharedLibrary, Environment.GetEnvironmentVariable("CRYPT_SHARED_LIB_PATH")),
            CryptProvider.Mongocryptd => mongoOptions.WithCryptProvider(
                CryptProvider.Mongocryptd, Environment.GetEnvironmentVariable("MONGODB_BINARIES")),
            _ => mongoOptions
        };

        var optionsBuilder = new DbContextOptionsBuilder<MedicalContext>()
            .UseMongoDB(mongoOptions)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>();

        return new MedicalContext(optionsBuilder.Options, modelBuilderAction);
    }

    // Patient's four owned navigations are auto-included by convention, and on EF9 an auto-included owned navigation makes ExecuteUpdate
    // fail with a confusing "Select" translation error for ANY setter. They are switched off here so the tests exercise the encryption
    // behaviour rather than that defect.
    private sealed class MedicalContext(DbContextOptions options, Action<ModelBuilder> modelBuilder) : DbContext(options)
    {
        public DbSet<Patient> Patients { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
            mb.Entity<Patient>().HasKey(k => k.Id);
            mb.Entity<Patient>().Navigation(p => p.BloodPressureReadings).AutoInclude(false);
            mb.Entity<Patient>().Navigation(p => p.WeightMeasurements).AutoInclude(false);
            mb.Entity<Patient>().Navigation(p => p.LongTermCarePlan).AutoInclude(false);
            mb.Entity<Patient>().Navigation(p => p.BillingAddress).AutoInclude(false);
            modelBuilder.Invoke(mb);
        }
    }

    // Pinned to the documented reason, so an unrelated encryption failure (missing key, bad schema) cannot masquerade as the rejection.
    private static void ThrowsMultiDocumentUpdateRejection(Action bulkUpdate)
    {
        var ex = Assert.ThrowsAny<MongoEncryptionException>(bulkUpdate);
        Assert.Contains("Multi-document updates are not allowed", ex.Message);
    }

    private List<BsonDocument> RawDocuments(string collectionName)
        => _database.MongoDatabase.GetCollection<BsonDocument>(collectionName).Find(FilterDefinition<BsonDocument>.Empty).ToList();

    // The strongest claim for a rejected operation: every stored document is exactly as it was, including no new element name.
    private static void AssertRawDocumentsUnchanged(List<BsonDocument> before, List<BsonDocument> after)
    {
        Assert.Equal(before.Count, after.Count);
        foreach (var expected in before)
        {
            var actual = after.Single(d => d["_id"] == expected["_id"]);
            Assert.Equal(expected.Names.OrderBy(n => n), actual.Names.OrderBy(n => n));
            Assert.Equal(expected, actual);
        }
    }

    private (MedicalContext Db, string CollectionName, Patient[] Patients) Setup(
        CryptProvider cryptProvider, Action<EntityTypeBuilderHolder> configure, [CallerMemberName] string? caller = null)
    {
        var collectionName = TemporaryDatabaseFixture.CreateCollectionName(caller, cryptProvider);
        var patients = CreateSamplePatients;
        var db = CreateContext(cryptProvider, mb => mb.Entity<Patient>(p =>
        {
            p.ToCollection(collectionName);
            configure(new EntityTypeBuilderHolder(p, CreateDataKey()));
        }));

        CreateEncryptedCollections(db.Model);
        db.Database.EnsureCreated();
        db.Patients.AddRange(patients);
        db.SaveChanges();
        return (db, collectionName, patients);
    }

    private sealed record EntityTypeBuilderHolder(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Patient> Builder, Guid DataKeyId);

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_delete_filtered_on_an_encrypted_equality_field_deletes_exactly_the_matching_document(CryptProvider cryptProvider)
    {
        var (db, collectionName, patients) = Setup(cryptProvider, h => h.Builder.Property(x => x.SSN).IsEncryptedForEquality(h.DataKeyId));
        using var _ = db;
        var targetSsn = patients[0].SSN;
        var before = RawDocuments(collectionName);

        // A zero-match here would mean the delete reports success while the data is still there.
        var deleted = db.Patients.Where(p => p.SSN == targetSsn).ExecuteDelete();

        Assert.Equal(1, deleted);
        var after = RawDocuments(collectionName);
        Assert.Equal(patients.Length - 1, after.Count);
        Assert.Equal(1, before.Count - after.Count);
        Assert.DoesNotContain(db.Patients.ToList(), p => p.SSN == targetSsn);
        Assert.Contains(db.Patients.ToList(), p => p.SSN == patients[1].SSN);
    }

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Two_phase_bulk_delete_on_a_queryable_encrypted_collection_deletes_exactly_the_targeted_document(CryptProvider cryptProvider)
    {
        // Skip/Take goes through the two-phase path: phase 1 reads the target _ids, phase 2 deletes by { _id: { $in: [...] } }.
        var (db, collectionName, patients) = Setup(cryptProvider, h => h.Builder.Property(x => x.SSN).IsEncryptedForEquality(h.DataKeyId));
        using var _ = db;

        var deleted = db.Patients.OrderBy(p => p.Sequence).Take(1).ExecuteDelete();

        Assert.Equal(1, deleted);
        var after = RawDocuments(collectionName);
        Assert.Equal(patients.Length - 1, after.Count);
        Assert.DoesNotContain(db.Patients.ToList(), p => p.Sequence == patients.Min(x => x.Sequence));
    }

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_update_of_an_encrypted_field_is_rejected_and_writes_no_plaintext(CryptProvider cryptProvider)
    {
        var (db, collectionName, _) = Setup(cryptProvider, h => h.Builder.Property(x => x.SSN).IsEncrypted(h.DataKeyId));
        using var _ = db;
        var before = RawDocuments(collectionName);

        ThrowsMultiDocumentUpdateRejection(() => db.Patients.ExecuteUpdate(s => s.SetProperty(p => p.SSN, "000-00-0000")));

        var after = RawDocuments(collectionName);
        AssertRawDocumentsUnchanged(before, after);
        Assert.All(after, raw => Assert.DoesNotContain("000-00-0000", raw.ToJson()));
    }

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_update_with_a_self_referencing_setter_does_not_expose_an_encrypted_sibling_field(CryptProvider cryptProvider)
    {
        // Sequence is NOT encrypted here; SSN is. The self-referencing setter renders as a pipeline-form update. Whether the rejection
        // is scoped to the encrypted field or to the whole collection is not assumed: either way the encrypted sibling must stay
        // ciphertext and the plaintext Sequence must still hold its seeded value.
        var (db, collectionName, patients) = Setup(cryptProvider, h => h.Builder.Property(x => x.SSN).IsEncrypted(h.DataKeyId));
        using var _ = db;
        var before = RawDocuments(collectionName);

        ThrowsMultiDocumentUpdateRejection(() => db.Patients.ExecuteUpdate(s => s.SetProperty(p => p.Sequence, p => p.Sequence + 1)));

        var after = RawDocuments(collectionName);
        AssertRawDocumentsUnchanged(before, after);
        Assert.All(after, raw => Assert.Equal(BsonType.Binary, raw["ssn"].BsonType));
        Assert.Equal(
            patients.Select(p => p.Sequence).OrderBy(x => x),
            after.Select(raw => raw["Sequence"].AsInt32).OrderBy(x => x));
    }

    [QueryableEncryptionTheory]
    [InlineData(CryptProvider.Mongocryptd)]
    [InlineData(CryptProvider.AutoEncryptSharedLibrary)]
    public void Bulk_update_with_a_self_referencing_setter_on_an_encrypted_field_is_rejected_and_writes_no_plaintext(
        CryptProvider cryptProvider)
    {
        var (db, collectionName, patients) = Setup(cryptProvider, h => h.Builder.Property(x => x.Sequence).IsEncrypted(h.DataKeyId));
        using var _ = db;
        var before = RawDocuments(collectionName);

        ThrowsMultiDocumentUpdateRejection(() => db.Patients.ExecuteUpdate(s => s.SetProperty(p => p.Sequence, p => p.Sequence + 1)));

        var after = RawDocuments(collectionName);
        AssertRawDocumentsUnchanged(before, after);

        // The seeded values are 10 and 20; their incremented plaintext (11, 21) must appear nowhere as an int, at any depth.
        var incremented = patients.Select(p => p.Sequence + 1).ToHashSet();
        Assert.All(after, raw => Assert.DoesNotContain(
            Flatten(raw).Where(v => v.BsonType == BsonType.Int32).Select(v => v.AsInt32),
            incremented.Contains));
    }

    private static IEnumerable<BsonValue> Flatten(BsonValue value)
    {
        switch (value)
        {
            case BsonDocument document:
                foreach (var element in document.Elements)
                {
                    foreach (var inner in Flatten(element.Value))
                    {
                        yield return inner;
                    }
                }

                break;
            case BsonArray array:
                foreach (var item in array)
                {
                    foreach (var inner in Flatten(item))
                    {
                        yield return inner;
                    }
                }

                break;
            default:
                yield return value;
                break;
        }
    }
}

#endif
