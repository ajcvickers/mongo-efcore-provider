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
using Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Tracking;

public class OptimisticConcurrencyMongoTest(OptimisticConcurrencyMongoTest.OptimisticConcurrencyMongoFixture fixture)
    : OptimisticConcurrencyTestBase<OptimisticConcurrencyMongoTest.OptimisticConcurrencyMongoFixture, byte[]>(fixture)
{
    public class OptimisticConcurrencyMongoFixture : F1FixtureBase<byte[]>
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("OptimisticConcurrency");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override TestHelpers TestHelpers
            => MongoTestHelpers.Instance;

        public override F1Context CreateContext()
        {
            _initFailure.ThrowIfFailed();
            return base.CreateContext();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: false);
        }
    }

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Adding_the_same_entity_twice_results_in_DbUpdateException()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Adding_the_same_entity_twice_results_in_DbUpdateException(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Attempting_to_add_same_relationship_twice_for_many_to_many_results_in_independent_association_exception()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Attempting_to_add_same_relationship_twice_for_many_to_many_results_in_independent_association_exception(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Attempting_to_delete_same_relationship_twice_for_many_to_many_results_in_independent_association_exception()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Attempting_to_delete_same_relationship_twice_for_many_to_many_results_in_independent_association_exception(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_GetDatabaseValues_on_owned_entity_works(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_GetDatabaseValues_on_owned_entity_works(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_a_Deleted_entity_makes_the_entity_unchanged(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_a_Deleted_entity_makes_the_entity_unchanged(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_a_Deleted_entity_that_is_not_in_database_detaches_it(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_a_Deleted_entity_that_is_not_in_database_detaches_it(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_a_Detached_entity_makes_the_entity_unchanged(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_a_Detached_entity_makes_the_entity_unchanged(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_a_Detached_entity_that_is_not_in_database_detaches_it(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_a_Detached_entity_that_is_not_in_database_detaches_it(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_a_Modified_entity_makes_the_entity_unchanged(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_a_Modified_entity_makes_the_entity_unchanged(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_a_Modified_entity_that_is_not_in_database_detaches_it(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_a_Modified_entity_that_is_not_in_database_detaches_it(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_an_Added_entity_that_is_not_in_database_is_no_op(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_an_Added_entity_that_is_not_in_database_is_no_op(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_an_Added_entity_that_was_saved_elsewhere_makes_the_entity_unchanged(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_an_Added_entity_that_was_saved_elsewhere_makes_the_entity_unchanged(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_an_Unchanged_entity_makes_the_entity_unchanged(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_an_Unchanged_entity_makes_the_entity_unchanged(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_an_Unchanged_entity_that_is_not_in_database_detaches_it(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_an_Unchanged_entity_that_is_not_in_database_detaches_it(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Calling_Reload_on_owned_entity_works(bool async)
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Calling_Reload_on_owned_entity_works(async), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Change_in_independent_association_after_change_in_different_concurrency_token_results_in_independent_association_exception()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Change_in_independent_association_after_change_in_different_concurrency_token_results_in_independent_association_exception(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Change_in_independent_association_results_in_independent_association_exception()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Change_in_independent_association_results_in_independent_association_exception(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Concurrency_issue_where_the_FK_is_the_concurrency_token_can_be_handled()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Concurrency_issue_where_the_FK_is_the_concurrency_token_can_be_handled(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Deleting_the_same_entity_twice_results_in_DbUpdateConcurrencyException()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Deleting_the_same_entity_twice_results_in_DbUpdateConcurrencyException(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Deleting_then_updating_the_same_entity_results_in_DbUpdateConcurrencyException()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Deleting_then_updating_the_same_entity_results_in_DbUpdateConcurrencyException(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Deleting_then_updating_the_same_entity_results_in_DbUpdateConcurrencyException_which_can_be_resolved_with_store_values()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Deleting_then_updating_the_same_entity_results_in_DbUpdateConcurrencyException_which_can_be_resolved_with_store_values(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void Nullable_client_side_concurrency_token_can_be_used()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Nullable_client_side_concurrency_token_can_be_used(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Simple_concurrency_exception_can_be_resolved_with_client_values()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Simple_concurrency_exception_can_be_resolved_with_client_values(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Simple_concurrency_exception_can_be_resolved_with_new_values()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Simple_concurrency_exception_can_be_resolved_with_new_values(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Simple_concurrency_exception_can_be_resolved_with_store_values()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Simple_concurrency_exception_can_be_resolved_with_store_values(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Simple_concurrency_exception_can_be_resolved_with_store_values_using_Reload()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Simple_concurrency_exception_can_be_resolved_with_store_values_using_Reload(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Simple_concurrency_exception_can_be_resolved_with_store_values_using_equivalent_of_accept_changes()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Simple_concurrency_exception_can_be_resolved_with_store_values_using_equivalent_of_accept_changes(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Two_concurrency_issues_in_one_to_many_related_entities_can_be_handled_by_dealing_with_dependent_first()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Two_concurrency_issues_in_one_to_many_related_entities_can_be_handled_by_dealing_with_dependent_first(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Two_concurrency_issues_in_one_to_one_related_entities_can_be_handled_by_dealing_with_dependent_first()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Two_concurrency_issues_in_one_to_one_related_entities_can_be_handled_by_dealing_with_dependent_first(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Updating_then_deleting_the_same_entity_results_in_DbUpdateConcurrencyException()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Updating_then_deleting_the_same_entity_results_in_DbUpdateConcurrencyException(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");

    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Updating_then_deleting_the_same_entity_results_in_DbUpdateConcurrencyException_which_can_be_resolved_with_store_values()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Updating_then_deleting_the_same_entity_results_in_DbUpdateConcurrencyException_which_can_be_resolved_with_store_values(), "Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it");
}
