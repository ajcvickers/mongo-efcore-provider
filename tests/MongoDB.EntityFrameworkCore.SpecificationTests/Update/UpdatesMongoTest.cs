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
using Microsoft.EntityFrameworkCore.TestModels.UpdatesModel;
using Microsoft.EntityFrameworkCore.TestUtilities;
#if !EF8
using Microsoft.EntityFrameworkCore.Update;
#endif
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Update;

public class UpdatesMongoTest(UpdatesMongoTest.UpdatesMongoFixture fixture)
    : UpdatesTestBase<UpdatesMongoTest.UpdatesMongoFixture>(fixture)
{
#if EF11
    private readonly object? _ef11Skips = Ef11TestSkips.SkipIfListed("xUnit v3 (EF11) cannot hide this non-virtual upstream test; it fails here for the reasons in EF-X101, EF-X600, EF-X605 (see the New-method override on EF8 to EF10)", "Ignore_before_save_property_is_still_generated", "Ignore_before_save_property_is_still_generated_graph", "SaveChanges_false_processes_all_tracked_entities_without_calling_AcceptAllChanges", "SaveChanges_processes_all_tracked_entities");
#endif

    protected override string UpdateConcurrencyMessage
        => "The database operation was expected to affect 1 row(s), but actually affected 0 row(s)";

    protected override string UpdateConcurrencyTokenMessage
        => "The database operation was expected to affect 1 row(s), but actually affected 0 row(s)";

    public class UpdatesMongoFixture : UpdatesFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("Updates");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override UpdatesContext CreateContext()
        {
            _initFailure.ThrowIfFailed();
            return base.CreateContext();
        }

#if EF11
        // EF11 reports this model validation warning before the relationship errors the other versions hit first.
        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ShadowForeignKeyPropertyCreated));

#endif
        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: false);
        }
    }

#if (EF10 || EF11) || EF9
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override Task Can_add_and_remove_self_refs()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_add_and_remove_self_refs(), "The instance of entity type 'Person' cannot be tracked because another instance with the k");
#elif EF8
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101
    public override void Can_add_and_remove_self_refs()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Can_add_and_remove_self_refs(), "The instance of entity type 'Person' cannot be tracked because another instance with the k");
#endif

#if (EF10 || EF11)
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_change_type_of__dependent_by_replacing_with_new_dependent(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_change_type_of__dependent_by_replacing_with_new_dependent(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#elif EF9 || EF8
#if EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_change_type_of__dependent_by_replacing_with_new_dependent(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_change_type_of__dependent_by_replacing_with_new_dependent(async));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_change_type_of__dependent_by_replacing_with_new_dependent(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_change_type_of__dependent_by_replacing_with_new_dependent(async));
#endif
#endif

#if (EF10 || EF11)
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_change_type_of_pk_to_pk_dependent_by_replacing_with_new_dependent(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_change_type_of_pk_to_pk_dependent_by_replacing_with_new_dependent(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#elif EF9 || EF8
#if EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_change_type_of_pk_to_pk_dependent_by_replacing_with_new_dependent(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_change_type_of_pk_to_pk_dependent_by_replacing_with_new_dependent(async));
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Can_change_type_of_pk_to_pk_dependent_by_replacing_with_new_dependent(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Can_change_type_of_pk_to_pk_dependent_by_replacing_with_new_dependent(async));
#endif
#endif

    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105
    public override Task Can_delete_and_add_for_same_key(bool async)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_delete_and_add_for_same_key(async), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");

#if (EF10 || EF11)
#if !EF11
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X108")]
    public new Task Ignore_before_save_property_is_still_generated(bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF9
#if !EF11
    // Fails: Cross-collection query not translated on EF8/EF9 (works on EF10) EF-X020 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X020")]
    public new Task Ignore_before_save_property_is_still_generated(bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11)
#if !EF11
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X105")]
    public new Task Ignore_before_save_property_is_still_generated_graph(bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF9
#if !EF11
    // Fails: Cross-collection query not translated on EF8/EF9 (works on EF10) EF-X020 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X020")]
    public new Task Ignore_before_save_property_is_still_generated_graph(bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108
    public override Task Mutation_of_tracked_values_does_not_mutate_values_in_store()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.Mutation_of_tracked_values_does_not_mutate_values_in_store(), "Assert.Equal() Failure: Values differ");
#elif EF8
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108
    public override void Mutation_of_tracked_values_does_not_mutate_values_in_store()
        => SpecFails.Throws<global::Xunit.Sdk.XunitException>(() => base.Mutation_of_tracked_values_does_not_mutate_values_in_store(), "Assert.Equal() Failure: Values differ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Concurrency exception message differs from the relational wording EF-X115
    public override Task Remove_partial_on_concurrency_token_original_value_mismatch_throws()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.Remove_partial_on_concurrency_token_original_value_mismatch_throws(), "Strings differ");
#elif EF8
    // Fails: Concurrency exception message differs from the relational wording EF-X115
    public override void Remove_partial_on_concurrency_token_original_value_mismatch_throws()
        => SpecFails.Throws<global::Xunit.Sdk.XunitException>(() => base.Remove_partial_on_concurrency_token_original_value_mismatch_throws(), "Strings differ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Concurrency exception message differs from the relational wording EF-X115
    public override Task Remove_partial_on_missing_record_throws()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.Remove_partial_on_missing_record_throws(), "Strings differ");
#elif EF8
    // Fails: Concurrency exception message differs from the relational wording EF-X115
    public override void Remove_partial_on_missing_record_throws()
        => SpecFails.Throws<global::Xunit.Sdk.XunitException>(() => base.Remove_partial_on_missing_record_throws(), "Strings differ");
#endif

#if !EF11
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task SaveChanges_false_processes_all_tracked_entities_without_calling_AcceptAllChanges(bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif

#if !EF11
    // Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X101")]
    public new Task SaveChanges_processes_all_tracked_entities(bool async)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif

#if (EF10 || EF11) || EF9
    // Fails: Partial update of a tracked entity reports a concurrency conflict EF-X114
    public override Task Save_partial_update()
        => SpecFails.ThrowsAsync<global::Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() => base.Save_partial_update(), "Conflicts were detected when performing updates to 'EF");
#elif EF8
    // Fails: Partial update of a tracked entity reports a concurrency conflict EF-X114
    public override void Save_partial_update()
        => SpecFails.Throws<global::Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() => base.Save_partial_update(), "Conflicts were detected when performing updates to 'EF");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Concurrency exception message differs from the relational wording EF-X115
    public override Task Save_partial_update_on_concurrency_token_original_value_mismatch_throws()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.Save_partial_update_on_concurrency_token_original_value_mismatch_throws(), "Strings differ");
#elif EF8
    // Fails: Concurrency exception message differs from the relational wording EF-X115
    public override void Save_partial_update_on_concurrency_token_original_value_mismatch_throws()
        => SpecFails.Throws<global::Xunit.Sdk.XunitException>(() => base.Save_partial_update_on_concurrency_token_original_value_mismatch_throws(), "Strings differ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Concurrency exception message differs from the relational wording EF-X115
    public override Task Save_partial_update_on_missing_record_throws()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.Save_partial_update_on_missing_record_throws(), "Strings differ");
#elif EF8
    // Fails: Concurrency exception message differs from the relational wording EF-X115
    public override void Save_partial_update_on_missing_record_throws()
        => SpecFails.Throws<global::Xunit.Sdk.XunitException>(() => base.Save_partial_update_on_missing_record_throws(), "Strings differ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108
    public override Task Save_replaced_principal()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.Save_replaced_principal(), "Assert.Equal() Failure: Values differ");
#elif EF8
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108
    public override void Save_replaced_principal()
        => SpecFails.Throws<global::Xunit.Sdk.XunitException>(() => base.Save_replaced_principal(), "Assert.Equal() Failure: Values differ");
#endif
}
