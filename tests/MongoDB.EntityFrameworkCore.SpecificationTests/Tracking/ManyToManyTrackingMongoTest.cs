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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Tracking;

public class ManyToManyTrackingMongoTest(ManyToManyTrackingMongoTest.ManyToManyTrackingMongoFixture fixture)
    : ManyToManyTrackingTestBase<ManyToManyTrackingMongoTest.ManyToManyTrackingMongoFixture>(fixture)
{
    public class ManyToManyTrackingMongoFixture : ManyToManyTrackingFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("ManyToManyTracking");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
        {
            _initFailure.ThrowIfFailed();
            return base.AddOptions(builder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: false);
        }
    }

#if EF11
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_Attach_or_Update_a_many_to_many_with_mixed_set_and_unset_keys(bool useUpdate, bool async)
        => SpecFails.ThrowsAsync(() => base.Can_Attach_or_Update_a_many_to_many_with_mixed_set_and_unset_keys(useUpdate, async));
#else
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_Attach_or_Update_a_many_to_many_with_mixed_set_and_unset_keys(bool useUpdate, bool async)
        =>
            ((global::System.Convert.ToString(useUpdate) == "True" && global::System.Convert.ToString(async) == "True"))
                ? SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_Attach_or_Update_a_many_to_many_with_mixed_set_and_unset_keys(useUpdate, async), "The instance of entity type 'GeneratedKeysLeft' cannot be tracked because another instance")
                :
            ((global::System.Convert.ToString(useUpdate) == "False" && global::System.Convert.ToString(async) == "False") || (global::System.Convert.ToString(useUpdate) == "False" && global::System.Convert.ToString(async) == "True") || (global::System.Convert.ToString(useUpdate) == "True" && global::System.Convert.ToString(async) == "False"))
                ? SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_Attach_or_Update_a_many_to_many_with_mixed_set_and_unset_keys(useUpdate, async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic")
                :
            base.Can_Attach_or_Update_a_many_to_many_with_mixed_set_and_unset_keys(useUpdate, async);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_add_and_remove_a_new_relationship(bool modifyLeft, bool modifyRight, bool useJoin, bool useNavs)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship(modifyLeft, modifyRight, useJoin, useNavs), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Can_add_and_remove_a_new_relationship(bool modifyLeft, bool modifyRight, bool useJoin, bool useNavs)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship(modifyLeft, modifyRight, useJoin, useNavs), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_add_and_remove_a_new_relationship_composite_additional_pk_with_navs(bool modifyLeft, bool modifyRight, bool useJoin, bool useNavs)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_composite_additional_pk_with_navs(modifyLeft, modifyRight, useJoin, useNavs), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Can_add_and_remove_a_new_relationship_composite_additional_pk_with_navs(bool modifyLeft, bool modifyRight, bool useJoin, bool useNavs)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_composite_additional_pk_with_navs(modifyLeft, modifyRight, useJoin, useNavs), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_add_and_remove_a_new_relationship_composite_with_navs(bool modifyLeft, bool modifyRight, bool useJoin, bool useNavs)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_composite_with_navs(modifyLeft, modifyRight, useJoin, useNavs), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Can_add_and_remove_a_new_relationship_composite_with_navs(bool modifyLeft, bool modifyRight, bool useJoin, bool useNavs)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_composite_with_navs(modifyLeft, modifyRight, useJoin, useNavs), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_add_and_remove_a_new_relationship_self(bool modifyLeft, bool modifyRight, bool useJoin)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_self(modifyLeft, modifyRight, useJoin), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Can_add_and_remove_a_new_relationship_self(bool modifyLeft, bool modifyRight, bool useJoin)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_self(modifyLeft, modifyRight, useJoin), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_add_and_remove_a_new_relationship_shared(bool modifyLeft, bool modifyRight, bool useJoin)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_shared(modifyLeft, modifyRight, useJoin), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Can_add_and_remove_a_new_relationship_shared(bool modifyLeft, bool modifyRight, bool useJoin)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_shared(modifyLeft, modifyRight, useJoin), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_add_and_remove_a_new_relationship_shared_with_payload(bool modifyLeft, bool modifyRight, bool useJoin)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_shared_with_payload(modifyLeft, modifyRight, useJoin), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Can_add_and_remove_a_new_relationship_shared_with_payload(bool modifyLeft, bool modifyRight, bool useJoin)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_shared_with_payload(modifyLeft, modifyRight, useJoin), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_add_and_remove_a_new_relationship_with_inheritance(bool modifyLeft, bool modifyRight, bool useJoin)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_with_inheritance(modifyLeft, modifyRight, useJoin), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Can_add_and_remove_a_new_relationship_with_inheritance(bool modifyLeft, bool modifyRight, bool useJoin)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_with_inheritance(modifyLeft, modifyRight, useJoin), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Cross-document navigation access issue EF-216
    public override Task Can_add_and_remove_a_new_relationship_with_payload(bool modifyLeft, bool modifyRight, bool useJoin, bool useNavs)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_with_payload(modifyLeft, modifyRight, useJoin, useNavs), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#elif EF8
    // Fails: Cross-document navigation access issue EF-216
    public override void Can_add_and_remove_a_new_relationship_with_payload(bool modifyLeft, bool modifyRight, bool useJoin, bool useNavs)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_and_remove_a_new_relationship_with_payload(modifyLeft, modifyRight, useJoin, useNavs), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.TestModels.ManyToMany");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_delete_with_many_to_many()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_delete_with_many_to_many()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_delete_with_many_to_many_composite_additional_pk_with_navs()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_composite_additional_pk_with_navs(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_delete_with_many_to_many_composite_additional_pk_with_navs()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_composite_additional_pk_with_navs(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_delete_with_many_to_many_composite_additional_pk_with_navs_unidirectional()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_composite_additional_pk_with_navs_unidirectional(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_delete_with_many_to_many_composite_additional_pk_with_navs_unidirectional()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_composite_additional_pk_with_navs_unidirectional(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_delete_with_many_to_many_composite_shared_with_navs()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_composite_shared_with_navs(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_delete_with_many_to_many_composite_shared_with_navs()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_composite_shared_with_navs(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_delete_with_many_to_many_composite_with_navs()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_composite_with_navs(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_delete_with_many_to_many_composite_with_navs()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_composite_with_navs(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_delete_with_many_to_many_composite_with_navs_unidirectional()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_composite_with_navs_unidirectional(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_delete_with_many_to_many_composite_with_navs_unidirectional()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_composite_with_navs_unidirectional(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_delete_with_many_to_many_with_navs()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_with_navs(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_delete_with_many_to_many_with_navs()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_delete_with_many_to_many_with_navs(), "Including navigation 'Navigation' is not supported.");
#endif

    // Fails: Many-to-many join entity is modelled as owned, cannot be saved or queried on its own EF-X112
    public override Task Can_insert_many_to_many(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many(async), "Cannot save instance of 'JoinOneToTwo' because it is an owned entity without any reference");

#if EF11
#if EF11
    // Fails: Unexpected exception on EF11 (not yet triaged) EF-X605
    public override Task Can_insert_many_to_many_composite_additional_pk_with_navs(bool async)
        => SpecFails.ThrowsAsync(() => base.Can_insert_many_to_many_composite_additional_pk_with_navs(async));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_insert_many_to_many_composite_additional_pk_with_navs(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_insert_many_to_many_composite_additional_pk_with_navs(async));
#endif
#else
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_composite_additional_pk_with_navs(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_composite_additional_pk_with_navs(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#endif

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_composite_additional_pk_with_navs_unidirectional(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_composite_additional_pk_with_navs_unidirectional(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

#if EF11
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_insert_many_to_many_composite_shared_with_navs(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_insert_many_to_many_composite_shared_with_navs(async));
#else
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_insert_many_to_many_composite_shared_with_navs(bool async)
        =>
            ((global::System.Convert.ToString(async) == "False"))
                ? SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_composite_shared_with_navs(async), "Including navigation 'Navigation' is not supported.")
                :
            ((global::System.Convert.ToString(async) == "True"))
                ? SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_composite_shared_with_navs(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic")
                :
            base.Can_insert_many_to_many_composite_shared_with_navs(async);
#endif

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_composite_with_navs(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_composite_with_navs(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

#if EF11
#if EF11
    // Fails (some data rows): Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_composite_with_navs_unidirectional(bool async)
        => SpecFails.AsyncSomeRows<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_insert_many_to_many_composite_with_navs_unidirectional(async), string.Empty);
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_insert_many_to_many_composite_with_navs_unidirectional(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_insert_many_to_many_composite_with_navs_unidirectional(async));
#endif
#else
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105 (some data rows only)
    public override Task Can_insert_many_to_many_composite_with_navs_unidirectional(bool async)
        =>
            ((global::System.Convert.ToString(async) == "False"))
                ? SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_composite_with_navs_unidirectional(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic")
                :
            base.Can_insert_many_to_many_composite_with_navs_unidirectional(async);
#endif

#if EF11
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_insert_many_to_many_fully_by_convention(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_insert_many_to_many_fully_by_convention(async));
#else
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_insert_many_to_many_fully_by_convention(bool async)
        =>
            ((global::System.Convert.ToString(async) == "True"))
                ? SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_fully_by_convention(async), "Including navigation 'Navigation' is not supported.")
                :
            ((global::System.Convert.ToString(async) == "False"))
                ? SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_fully_by_convention(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic")
                :
            base.Can_insert_many_to_many_fully_by_convention(async);
#endif

    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_insert_many_to_many_fully_by_convention_generated_keys(bool async)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_fully_by_convention_generated_keys(async), "The instance of entity type 'GeneratedKeysLeft' cannot be tracked because another instance");

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_self_shared(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_self_shared(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

#if EF11
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_insert_many_to_many_self_shared_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Can_insert_many_to_many_self_shared_unidirectional(async));
#else
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_self_shared_unidirectional(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_self_shared_unidirectional(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#endif

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_self_with_payload(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_self_with_payload(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_self_with_payload_unidirectional(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_self_with_payload_unidirectional(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_shared(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_shared(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_shared_unidirectional(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_shared_unidirectional(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_shared_with_payload(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_shared_with_payload(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

#if EF11
#if EF11
    // Fails: Unexpected exception on EF11 (not yet triaged) EF-X605
    public override Task Can_insert_many_to_many_shared_with_payload_unidirectional(bool async)
        => SpecFails.ThrowsAsync(() => base.Can_insert_many_to_many_shared_with_payload_unidirectional(async));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_insert_many_to_many_shared_with_payload_unidirectional(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_insert_many_to_many_shared_with_payload_unidirectional(async));
#endif
#else
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_shared_with_payload_unidirectional(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_shared_with_payload_unidirectional(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#endif

    // Fails: Many-to-many join entity is modelled as owned, cannot be saved or queried on its own EF-X112
    public override Task Can_insert_many_to_many_with_dangling_join(bool async, bool useTrackGraph, bool useDetectChanges)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_with_dangling_join(async, useTrackGraph, useDetectChanges), "Cannot save instance of 'JoinOneToTwo' because it is an owned entity without any reference");

    // Fails: Many-to-many join entity is modelled as owned, cannot be saved or queried on its own EF-X112
    public override Task Can_insert_many_to_many_with_dangling_join_unidirectional(bool async, bool useTrackGraph, bool useDetectChanges)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_with_dangling_join_unidirectional(async, useTrackGraph, useDetectChanges), "Cannot save instance of 'UnidirectionalJoinOneToTwo' because it is an owned entity without");

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_with_inheritance(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_with_inheritance(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_with_inheritance_unidirectional(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_with_inheritance_unidirectional(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

#if EF11
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_insert_many_to_many_with_navs(bool async)
        => SpecFails.ThrowsAsync(() => base.Can_insert_many_to_many_with_navs(async));
#else
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_insert_many_to_many_with_navs(bool async)
        =>
            ((global::System.Convert.ToString(async) == "True"))
                ? SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_with_navs(async), "Including navigation 'Navigation' is not supported.")
                :
            ((global::System.Convert.ToString(async) == "False"))
                ? SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_with_navs(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic")
                :
            base.Can_insert_many_to_many_with_navs(async);
#endif

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_with_navs_by_join_entity(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_with_navs_by_join_entity(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

#if EF11
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_insert_many_to_many_with_navs_by_join_entity_unidirectional(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_insert_many_to_many_with_navs_by_join_entity_unidirectional(async));
#else
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_insert_many_to_many_with_navs_by_join_entity_unidirectional(bool async)
        =>
            ((global::System.Convert.ToString(async) == "False"))
                ? SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_with_navs_by_join_entity_unidirectional(async), "Including navigation 'Navigation' is not supported.")
                :
            ((global::System.Convert.ToString(async) == "True"))
                ? SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_with_navs_by_join_entity_unidirectional(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic")
                :
            base.Can_insert_many_to_many_with_navs_by_join_entity_unidirectional(async);
#endif

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_many_to_many_with_payload(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_with_payload(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

#if EF11
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Can_insert_many_to_many_with_suspected_dangling_join(bool async, bool useTrackGraph, bool useDetectChanges)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_with_suspected_dangling_join(async, useTrackGraph, useDetectChanges));
#else
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_insert_many_to_many_with_suspected_dangling_join(bool async, bool useTrackGraph, bool useDetectChanges)
        =>
            ((global::System.Convert.ToString(async) == "True" && global::System.Convert.ToString(useTrackGraph) == "False" && global::System.Convert.ToString(useDetectChanges) == "True"))
                ? SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_with_suspected_dangling_join(async, useTrackGraph, useDetectChanges), "Including navigation 'Navigation' is not supported.")
                :
            ((global::System.Convert.ToString(async) == "False" && global::System.Convert.ToString(useTrackGraph) == "False" && global::System.Convert.ToString(useDetectChanges) == "False") || (global::System.Convert.ToString(async) == "True" && global::System.Convert.ToString(useTrackGraph) == "True" && global::System.Convert.ToString(useDetectChanges) == "False") || (global::System.Convert.ToString(async) == "False" && global::System.Convert.ToString(useTrackGraph) == "True" && global::System.Convert.ToString(useDetectChanges) == "False") || (global::System.Convert.ToString(async) == "False" && global::System.Convert.ToString(useTrackGraph) == "False" && global::System.Convert.ToString(useDetectChanges) == "True") || (global::System.Convert.ToString(async) == "True" && global::System.Convert.ToString(useTrackGraph) == "False" && global::System.Convert.ToString(useDetectChanges) == "False") || (global::System.Convert.ToString(async) == "True" && global::System.Convert.ToString(useTrackGraph) == "True" && global::System.Convert.ToString(useDetectChanges) == "True") || (global::System.Convert.ToString(async) == "False" && global::System.Convert.ToString(useTrackGraph) == "True" && global::System.Convert.ToString(useDetectChanges) == "True"))
                ? SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_with_suspected_dangling_join(async, useTrackGraph, useDetectChanges), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic")
                :
            base.Can_insert_many_to_many_with_suspected_dangling_join(async, useTrackGraph, useDetectChanges);
#endif

#if EF11
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Can_insert_many_to_many_with_suspected_dangling_join_unidirectional(bool async, bool useTrackGraph, bool useDetectChanges)
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_with_suspected_dangling_join_unidirectional(async, useTrackGraph, useDetectChanges));
#else
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_insert_many_to_many_with_suspected_dangling_join_unidirectional(bool async, bool useTrackGraph, bool useDetectChanges)
        =>
            ((global::System.Convert.ToString(async) == "False" && global::System.Convert.ToString(useTrackGraph) == "True" && global::System.Convert.ToString(useDetectChanges) == "True"))
                ? SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_insert_many_to_many_with_suspected_dangling_join_unidirectional(async, useTrackGraph, useDetectChanges), "Including navigation 'Navigation' is not supported.")
                :
            ((global::System.Convert.ToString(async) == "True" && global::System.Convert.ToString(useTrackGraph) == "False" && global::System.Convert.ToString(useDetectChanges) == "False") || (global::System.Convert.ToString(async) == "True" && global::System.Convert.ToString(useTrackGraph) == "True" && global::System.Convert.ToString(useDetectChanges) == "True") || (global::System.Convert.ToString(async) == "True" && global::System.Convert.ToString(useTrackGraph) == "True" && global::System.Convert.ToString(useDetectChanges) == "False") || (global::System.Convert.ToString(async) == "False" && global::System.Convert.ToString(useTrackGraph) == "False" && global::System.Convert.ToString(useDetectChanges) == "False") || (global::System.Convert.ToString(async) == "False" && global::System.Convert.ToString(useTrackGraph) == "False" && global::System.Convert.ToString(useDetectChanges) == "True") || (global::System.Convert.ToString(async) == "False" && global::System.Convert.ToString(useTrackGraph) == "True" && global::System.Convert.ToString(useDetectChanges) == "False") || (global::System.Convert.ToString(async) == "True" && global::System.Convert.ToString(useTrackGraph) == "False" && global::System.Convert.ToString(useDetectChanges) == "True"))
                ? SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_many_to_many_with_suspected_dangling_join_unidirectional(async, useTrackGraph, useDetectChanges), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic")
                :
            base.Can_insert_many_to_many_with_suspected_dangling_join_unidirectional(async, useTrackGraph, useDetectChanges);
#endif

#if (EF10 || EF11) || EF9
    // Fails: Indexer (shared-type entity) property access is not translated by the driver EF-X113
    public override Task Can_insert_update_delete_proxyable_shared_type_entity_type()
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Can_insert_update_delete_proxyable_shared_type_entity_type(), "Expression not supported: p.get_Item(\"Id\").");
#elif EF8
    // Fails: Indexer (shared-type entity) property access is not translated by the driver EF-X113
    public override void Can_insert_update_delete_proxyable_shared_type_entity_type()
        => SpecFails.Throws<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Can_insert_update_delete_proxyable_shared_type_entity_type(), "Expression not supported: p.get_Item(\"Id\").");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_insert_update_delete_proxyable_shared_type_entity_type_unidirectional()
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_update_delete_proxyable_shared_type_entity_type_unidirectional(), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#elif EF8
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override void Can_insert_update_delete_proxyable_shared_type_entity_type_unidirectional()
        => SpecFails.Throws<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_update_delete_proxyable_shared_type_entity_type_unidirectional(), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Indexer (shared-type entity) property access is not translated by the driver EF-X113
    public override Task Can_insert_update_delete_shared_type_entity_type()
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Can_insert_update_delete_shared_type_entity_type(), "Expression not supported: j.get_Item(\"OneId\") because dictionary serializer class MongoDB.");
#elif EF8
    // Fails: Indexer (shared-type entity) property access is not translated by the driver EF-X113
    public override void Can_insert_update_delete_shared_type_entity_type()
        => SpecFails.Throws<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Can_insert_update_delete_shared_type_entity_type(), "Expression not supported: j.get_Item(\"OneId\") because dictionary serializer class MongoDB.");
#endif

    // Fails: Many-to-many join entity is modelled as owned, cannot be saved or queried on its own EF-X112
    public override void Can_load_entities_in_any_order(int[] order)
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_load_entities_in_any_order(order), "Cannot create a DbSet for 'JoinOneToTwo' because it is configured as an owned entity type ");

#if EF11
#if EF11
    // Fails: Unexpected exception on EF11 (not yet triaged) EF-X605
    public override Task Can_replace_dependent_with_many_to_many(bool createNewCollection, bool async)
        => SpecFails.ThrowsAsync(() => base.Can_replace_dependent_with_many_to_many(createNewCollection, async));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_replace_dependent_with_many_to_many(bool createNewCollection, bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_replace_dependent_with_many_to_many(createNewCollection, async));
#endif
#else
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_replace_dependent_with_many_to_many(bool createNewCollection, bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_replace_dependent_with_many_to_many(createNewCollection, async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_composite_additional_pk_with_navs()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_composite_additional_pk_with_navs(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_composite_additional_pk_with_navs()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_composite_additional_pk_with_navs(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_composite_additional_pk_with_navs_unidirectional()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_composite_additional_pk_with_navs_unidirectional(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_composite_additional_pk_with_navs_unidirectional()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_composite_additional_pk_with_navs_unidirectional(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_composite_shared_with_navs()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_composite_shared_with_navs(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_composite_shared_with_navs()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_composite_shared_with_navs(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_composite_with_navs()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_composite_with_navs(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_composite_with_navs()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_composite_with_navs(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_composite_with_navs_unidirectional()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_composite_with_navs_unidirectional(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_composite_with_navs_unidirectional()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_composite_with_navs_unidirectional(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_self()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_self(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_self()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_self(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_self_unidirectional()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_self_unidirectional(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_self_unidirectional()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_self_unidirectional(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_self_with_payload()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_self_with_payload(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_self_with_payload()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_self_with_payload(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_self_with_payload_unidirectional()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_self_with_payload_unidirectional(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_self_with_payload_unidirectional()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_self_with_payload_unidirectional(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_shared()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_shared(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_shared()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_shared(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_shared_unidirectional()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_shared_unidirectional(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_shared_unidirectional()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_shared_unidirectional(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_shared_with_payload()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_shared_with_payload(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_shared_with_payload()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_shared_with_payload(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_shared_with_payload_unidirectional()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_shared_with_payload_unidirectional(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_shared_with_payload_unidirectional()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_shared_with_payload_unidirectional(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_with_inheritance()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_with_inheritance(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_with_inheritance()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_with_inheritance(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_with_inheritance_unidirectional()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_with_inheritance_unidirectional(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_with_inheritance_unidirectional()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_with_inheritance_unidirectional(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_with_navs()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_with_navs(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_with_navs()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_with_navs(), "Including navigation 'Navigation' is not supported.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override Task Can_update_many_to_many_with_payload()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_with_payload(), "Including navigation 'Navigation' is not supported.");
#elif EF8
    // Fails: Include of a skip (many-to-many) navigation is not supported EF-X110
    public override void Can_update_many_to_many_with_payload()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_update_many_to_many_with_payload(), "Including navigation 'Navigation' is not supported.");
#endif

#if EF11
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Initial_tracking_uses_skip_navigations(bool async)
        => SpecFails.ThrowsAsync(() => base.Initial_tracking_uses_skip_navigations(async));
#else
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Initial_tracking_uses_skip_navigations(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Initial_tracking_uses_skip_navigations(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#endif
}
