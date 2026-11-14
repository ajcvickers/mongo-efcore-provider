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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Tracking;

public class OptimisticConcurrencyMongoTest(OptimisticConcurrencyMongoTest.OptimisticConcurrencyMongoFixture fixture)
    : OptimisticConcurrencyTestBase<OptimisticConcurrencyMongoTest.OptimisticConcurrencyMongoFixture, long>(fixture)
{
    // EF isolates each test by beginning a transaction on one context and never committing it; a second context the test creates joins
    // it through this hook (#77, #87) and only participates, so disposing it does not end the transaction. Nothing is reseeded.
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction);

    // The provider generates row versions only for int, uint, long and ulong (RowVersion.cs). The upstream suite uses byte[], which nothing
    // would ever change, so no conflict would be detected; hence long here and in the test base's type argument.
    public class OptimisticConcurrencyMongoFixture : F1FixtureBase<long>
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("OptimisticConcurrency");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override TestHelpers TestHelpers
            => MongoTestHelpers.Instance;

        // The F1 model is built outside the normal pipeline (UseModel), so OnModelCreating is not part of it.
        protected override void BuildModelExternal(ModelBuilder modelBuilder)
        {
            base.BuildModelExternal(modelBuilder);

            // EF-X201: single-property keys must be mapped to _id. EF-X214: concurrency tokens are supported only on the document root.
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
            modelBuilder.FixExternalModel(dropOwnedConcurrencyTokens: true);

            // EF-X207: a unique index on a nullable foreign key treats every null as a duplicate.
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
            modelBuilder.RelaxUniqueForeignKeyIndexes();
        }

        // From EF9 the F1 data is added through UseSeeding / UseAsyncSeeding, which the provider's EnsureCreated does not run, so seed it here.
#if !EF8
        protected override async Task SeedAsync(F1Context context)
        {
            F1Context.AddSeedData(context);
            await context.SaveChangesAsync();
        }
#endif
    }

    // Fails: inserting a duplicate key surfaces as a raw MongoBulkWriteException instead of DbUpdateException EF-X117
    public override Task Adding_the_same_entity_twice_results_in_DbUpdateException()
        => SpecFails.ThrowsAsync(() => base.Adding_the_same_entity_twice_results_in_DbUpdateException(), "duplicate key");

    // Fails: Include of a skip navigation is not supported EF-X110
    public override Task Attempting_to_add_same_relationship_twice_for_many_to_many_results_in_independent_association_exception()
        => SpecFails.ThrowsAsync(() => base.Attempting_to_add_same_relationship_twice_for_many_to_many_results_in_independent_association_exception(), "Including navigation");

    // Fails: Include of a skip navigation is not supported EF-X110
    public override Task Attempting_to_delete_same_relationship_twice_for_many_to_many_results_in_independent_association_exception()
        => SpecFails.ThrowsAsync(() => base.Attempting_to_delete_same_relationship_twice_for_many_to_many_results_in_independent_association_exception(), "Including navigation");

    // Fails: NullReferenceException during query execution or materialization EF-X529
    public override Task Calling_GetDatabaseValues_on_owned_entity_works(bool async)
        => SpecFails.ThrowsAsync(() => base.Calling_GetDatabaseValues_on_owned_entity_works(async));

    public override Task Calling_Reload_on_a_Deleted_entity_makes_the_entity_unchanged(bool async)
        => base.Calling_Reload_on_a_Deleted_entity_makes_the_entity_unchanged(async);

    public override Task Calling_Reload_on_a_Deleted_entity_that_is_not_in_database_detaches_it(bool async)
        => base.Calling_Reload_on_a_Deleted_entity_that_is_not_in_database_detaches_it(async);

    public override Task Calling_Reload_on_a_Detached_entity_makes_the_entity_unchanged(bool async)
        => base.Calling_Reload_on_a_Detached_entity_makes_the_entity_unchanged(async);

    public override Task Calling_Reload_on_a_Detached_entity_that_is_not_in_database_detaches_it(bool async)
        => base.Calling_Reload_on_a_Detached_entity_that_is_not_in_database_detaches_it(async);

    public override Task Calling_Reload_on_a_Modified_entity_makes_the_entity_unchanged(bool async)
        => base.Calling_Reload_on_a_Modified_entity_makes_the_entity_unchanged(async);

    public override Task Calling_Reload_on_a_Modified_entity_that_is_not_in_database_detaches_it(bool async)
        => base.Calling_Reload_on_a_Modified_entity_that_is_not_in_database_detaches_it(async);

    public override Task Calling_Reload_on_an_Added_entity_that_is_not_in_database_is_no_op(bool async)
        => base.Calling_Reload_on_an_Added_entity_that_is_not_in_database_is_no_op(async);

    public override Task Calling_Reload_on_an_Added_entity_that_was_saved_elsewhere_makes_the_entity_unchanged(bool async)
        => base.Calling_Reload_on_an_Added_entity_that_was_saved_elsewhere_makes_the_entity_unchanged(async);

    public override Task Calling_Reload_on_an_Unchanged_entity_makes_the_entity_unchanged(bool async)
        => base.Calling_Reload_on_an_Unchanged_entity_makes_the_entity_unchanged(async);

    public override Task Calling_Reload_on_an_Unchanged_entity_that_is_not_in_database_detaches_it(bool async)
        => base.Calling_Reload_on_an_Unchanged_entity_that_is_not_in_database_detaches_it(async);

    // Fails: Reload of an owned entity returns 10 where the base expects 5 EF-X600
    public override Task Calling_Reload_on_owned_entity_works(bool async)
        => SpecFails.ThrowsAsync(() => base.Calling_Reload_on_owned_entity_works(async), "Values differ");

    public override Task Change_in_independent_association_after_change_in_different_concurrency_token_results_in_independent_association_exception()
        => base.Change_in_independent_association_after_change_in_different_concurrency_token_results_in_independent_association_exception();

    public override Task Change_in_independent_association_results_in_independent_association_exception()
        => base.Change_in_independent_association_results_in_independent_association_exception();

    public override Task Concurrency_issue_where_the_FK_is_the_concurrency_token_can_be_handled()
        => base.Concurrency_issue_where_the_FK_is_the_concurrency_token_can_be_handled();

    public override Task Deleting_the_same_entity_twice_results_in_DbUpdateConcurrencyException()
        => base.Deleting_the_same_entity_twice_results_in_DbUpdateConcurrencyException();

    public override Task Deleting_then_updating_the_same_entity_results_in_DbUpdateConcurrencyException()
        => base.Deleting_then_updating_the_same_entity_results_in_DbUpdateConcurrencyException();

    public override Task Deleting_then_updating_the_same_entity_results_in_DbUpdateConcurrencyException_which_can_be_resolved_with_store_values()
        => base.Deleting_then_updating_the_same_entity_results_in_DbUpdateConcurrencyException_which_can_be_resolved_with_store_values();

    public override void Nullable_client_side_concurrency_token_can_be_used()
        => base.Nullable_client_side_concurrency_token_can_be_used();

    public override Task Simple_concurrency_exception_can_be_resolved_with_client_values()
        => base.Simple_concurrency_exception_can_be_resolved_with_client_values();

    public override Task Simple_concurrency_exception_can_be_resolved_with_new_values()
        => base.Simple_concurrency_exception_can_be_resolved_with_new_values();

    public override Task Simple_concurrency_exception_can_be_resolved_with_store_values()
        => base.Simple_concurrency_exception_can_be_resolved_with_store_values();

    public override Task Simple_concurrency_exception_can_be_resolved_with_store_values_using_Reload()
        => base.Simple_concurrency_exception_can_be_resolved_with_store_values_using_Reload();

    public override Task Simple_concurrency_exception_can_be_resolved_with_store_values_using_equivalent_of_accept_changes()
        => base.Simple_concurrency_exception_can_be_resolved_with_store_values_using_equivalent_of_accept_changes();

    public override Task Two_concurrency_issues_in_one_to_many_related_entities_can_be_handled_by_dealing_with_dependent_first()
        => base.Two_concurrency_issues_in_one_to_many_related_entities_can_be_handled_by_dealing_with_dependent_first();

    public override Task Two_concurrency_issues_in_one_to_one_related_entities_can_be_handled_by_dealing_with_dependent_first()
        => base.Two_concurrency_issues_in_one_to_one_related_entities_can_be_handled_by_dealing_with_dependent_first();

    public override Task Updating_then_deleting_the_same_entity_results_in_DbUpdateConcurrencyException()
        => base.Updating_then_deleting_the_same_entity_results_in_DbUpdateConcurrencyException();

    public override Task Updating_then_deleting_the_same_entity_results_in_DbUpdateConcurrencyException_which_can_be_resolved_with_store_values()
        => base.Updating_then_deleting_the_same_entity_results_in_DbUpdateConcurrencyException_which_can_be_resolved_with_store_values();
}
